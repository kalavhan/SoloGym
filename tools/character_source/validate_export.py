#!/usr/bin/env python3
"""Validate the shared character export contract; visual acceptance is separate."""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

from prepare_base import DEFAULT_VENDOR, REPO_ROOT, load_base, verify_sources

DEFAULT_EXPORT = REPO_ROOT / "app/Assets/SoloGym/Resources/AvatarSource3D/Character.json"


class ContractError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise ContractError(message)


def finite_vector(values, count, label):
    require(isinstance(values, list) and len(values) == count, f"{label}: expected {count} values")
    require(all(isinstance(v, (float, int)) and not isinstance(v, bool) and math.isfinite(v)
                for v in values), f"{label}: non-finite or non-numeric value")


def validate_export(payload, *, source_base=None, require_lineage=False, ground_tolerance=0.00015):
    """Return a report or raise ContractError; do not mutate the supplied export.

    Shape deltas must exist for every mesh, including rigid attachments whose
    deltas are zero. Body-derived meshes can supply sourceVertexIndices, enabling
    an exact weight lineage check against the original shared source. Geometric
    self-intersection, silhouette quality and motion quality need render review.
    """
    require(payload.get("schema") == "sologym.character-source3d.v1", "Unsupported character schema")
    require(payload.get("skeletonId") == "sologym_humanoid_22_v1", "Unexpected skeletonId")
    revision = payload.get("sourceRevision", "")
    require(isinstance(revision, str) and len(revision) == 64 and all(c in "0123456789abcdef" for c in revision), "Invalid source revision")
    source_base = source_base or load_base(muscle=0.25)
    expected_bones = source_base["bones"]
    bones = payload.get("bones", [])
    require(len(bones) == len(expected_bones), "Source skeleton bone count changed")
    names = [b.get("name") for b in bones]
    require(len(set(names)) == len(names), "Duplicate bone name")
    for index, (bone, source) in enumerate(zip(bones, expected_bones)):
        name = bone.get("name")
        require(name == source["name"], f"Bone {index}: source name/order changed")
        expected_parent = names.index(source["parent"]) if source["parent"] else -1
        require(bone.get("parent") == expected_parent, f"Bone {name}: source parent changed")
        require(-1 <= expected_parent < index, f"Bone {name}: invalid hierarchy")
        finite_vector(bone.get("position"), 3, f"Bone {name} position")
        finite_vector(bone.get("rotation"), 4, f"Bone {name} rotation")
        require(abs(sum(v*v for v in bone["rotation"]) - 1) < 0.0001, f"Bone {name}: invalid quaternion")

    materials = payload.get("materials", [])
    require(bool(materials), "Missing materials")
    require(len({m.get("id") for m in materials}) == len(materials), "Duplicate material ID")
    for material in materials:
        finite_vector(material.get("color"), 4, f"Material {material.get('id')} color")
        require(all(0 <= c <= 1 for c in material["color"]), "Material color outside 0..1")
    meshes = payload.get("meshes", [])
    require(bool(meshes), "Missing meshes")
    require(len({m.get("id") for m in meshes}) == len(meshes), "Duplicate mesh ID")
    required_ids = {"body", "covered_torso", "bare_hands", "training_top", "training_shorts"}
    require(required_ids <= {m.get("id") for m in meshes}, "Missing proof body/garment mesh")
    require(any(m.get("id", "").startswith("training_boots") for m in meshes), "Missing proof footwear")
    known_slots = {"body", "top", "hands", "hair"}
    totals = dict(vertices=0, triangles=0, lineage_vertices=0, body_derived_meshes=0)
    meshes_without_lineage = []
    min_y = [math.inf, math.inf]
    sole_min_y = [math.inf, math.inf]
    max_y = [-math.inf, -math.inf]

    for mesh in meshes:
        name = mesh.get("id", "<unnamed>")
        positions = mesh.get("vertices", [])
        require(len(positions) > 0 and len(positions) % 3 == 0, f"{name}: invalid vertex count")
        count = len(positions) // 3
        require(mesh.get("slot") in known_slots, f"{name}: unknown appearance slot")
        require(mesh.get("hideWithSlot", "") in ("", "top", "hands"), f"{name}: unknown coverage mask")
        for field in ("vertices", "normals", "shapeDelta", "shapeNormalDelta"):
            finite_vector(mesh.get(field), count * 3, f"{name}.{field}")
        finite_vector(mesh.get("boneWeights"), count * 4, f"{name}.boneWeights")
        indices = mesh.get("boneIndices", [])
        require(len(indices) == count * 4 and all(type(v) is int and 0 <= v < len(bones) for v in indices),
                f"{name}: invalid bone indices")
        weights = mesh["boneWeights"]
        for vertex in range(count):
            values = weights[vertex*4:vertex*4+4]
            require(all(0 <= w <= 1 for w in values) and abs(sum(values) - 1) <= 0.00001,
                    f"{name}: unnormalized weights at vertex {vertex}")
            active = [indices[vertex*4+i] for i, weight in enumerate(values) if weight > 0]
            require(len(active) == len(set(active)), f"{name}: duplicate active bone at vertex {vertex}")
            for endpoint in (0, 1):
                point = [positions[vertex*3+a] + mesh["shapeDelta"][vertex*3+a] * endpoint for a in range(3)]
                require(all(math.isfinite(v) and abs(v) < 10 for v in point), f"{name}: invalid shape endpoint geometry")
                min_y[endpoint] = min(min_y[endpoint], point[1])
                max_y[endpoint] = max(max_y[endpoint], point[1])
                if name.startswith("training_boots"):
                    sole_min_y[endpoint] = min(sole_min_y[endpoint], point[1])
        submeshes = mesh.get("submeshes", [])
        require(bool(submeshes), f"{name}: missing submeshes")
        for submesh in submeshes:
            material = submesh.get("material")
            require(type(material) is int and 0 <= material < len(materials), f"{name}: invalid material index")
            triangles = submesh.get("triangles", [])
            require(len(triangles) > 0 and len(triangles) % 3 == 0, f"{name}: invalid triangle list")
            require(all(type(v) is int and 0 <= v < count for v in triangles), f"{name}: triangle index outside mesh")
            require(all(len(set(triangles[i:i+3])) == 3 for i in range(0, len(triangles), 3)),
                    f"{name}: triangle repeats a vertex")
            totals["triangles"] += len(triangles) // 3
        if name in {"body", "covered_torso", "training_top", "training_shorts"} or name.startswith(("top_binding", "shorts_binding")):
            require(any(abs(v) > 0.000001 for v in mesh["shapeDelta"]), f"{name}: fitted deforming mesh lacks body-shape support")
        if mesh["slot"] == "hair":
            require(mesh.get("variant") in ("spiky", "swept"), f"{name}: unknown hair variant")
            require(all(names[b] == "head" for b, w in zip(indices, weights) if w > 0), f"{name}: hair must share head attachment")
        if mesh["slot"] == "hands":
            require(all(names[b] in {"hand.L", "hand.R", "forearm.L", "forearm.R"}
                        for b, w in zip(indices, weights) if w > 0), f"{name}: glove attached outside hand/forearm")

        lineage = mesh.get("sourceVertexIndices")
        mode = mesh.get("fitMode")
        if lineage is not None:
            require(len(lineage) == count and all(type(v) is int and -1 <= v < len(source_base["positions"]) for v in lineage),
                    f"{name}: invalid source vertex lineage")
            if mode == "body-derived":
                require(all(v >= 0 for v in lineage), f"{name}: body-derived mesh has unmapped vertices")
                totals["body_derived_meshes"] += 1
            for vertex, source_index in enumerate(lineage):
                if source_index < 0:
                    continue
                expected = dict(source_base["weights"][source_index])
                actual = {names[indices[vertex*4+i]]: weights[vertex*4+i] for i in range(4) if weights[vertex*4+i] > 0}
                require(all(abs(expected.get(b, 0) - actual.get(b, 0)) <= 0.00001 for b in expected.keys() | actual.keys()),
                        f"{name}: vertex {vertex} weights differ from master source index {source_index}")
                totals["lineage_vertices"] += 1
        elif mode == "body-derived" or name in required_ids:
            meshes_without_lineage.append(name)
        totals["vertices"] += count

    for endpoint in (0, 1):
        require(abs(sole_min_y[endpoint]) <= ground_tolerance,
                f"Soles must meet Y=0 at shape endpoint {endpoint}; min Y={sole_min_y[endpoint]:.6f}")
        require(min_y[endpoint] >= -ground_tolerance, f"Geometry below ground at shape endpoint {endpoint}")
        require(1.4 <= max_y[endpoint] <= 2.1, f"Character scale outside proof contract at endpoint {endpoint}")
    require(not require_lineage or not meshes_without_lineage,
            "Missing source vertex lineage: " + ", ".join(meshes_without_lineage))
    return dict(structural_passed=True, visual_review_status="pending", source_revision=revision,
                mesh_count=len(meshes), bone_count=len(bones), **totals,
                sole_min_y=sole_min_y, height=max_y, meshes_without_weight_lineage=meshes_without_lineage,
                limits="Does not certify garment collision clearance, silhouette quality or animation quality.")


def regression_checks(payload, source_base):
    """Prove validators reject corrupted data instead of only testing the happy path."""
    checks = []
    def wrong_master_attachment(p):
        mesh = next(m for m in p["meshes"] if m.get("fitMode") == "body-derived")
        mesh["boneIndices"][:4] = [0, 0, 0, 0]
        mesh["boneWeights"][:4] = [1.0, 0.0, 0.0, 0.0]

    for label, mutate in (
        ("unnormalized skin weights", lambda p: p["meshes"][0]["boneWeights"].__setitem__(0, 2.0)),
        ("NaN geometry", lambda p: p["meshes"][0]["vertices"].__setitem__(0, float("nan"))),
        ("missing bone", lambda p: p["bones"].pop()),
        ("missing garment shape", lambda p: next(m for m in p["meshes"] if m["id"] == "training_top").update(shapeDelta=[])),
        ("normalized weights on wrong master bone", wrong_master_attachment),
    ):
        broken = copy.deepcopy(payload)
        mutate(broken)
        try:
            validate_export(broken, source_base=source_base)
        except ContractError:
            checks.append(label)
        else:
            raise AssertionError(f"Corrupt export was accepted: {label}")
    return checks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("export", type=Path, nargs="?", default=DEFAULT_EXPORT)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--check-source-hash", action="store_true", help="Require export revision to match builder, base reader, vendor manifest and pinned Blender version")
    parser.add_argument("--require-lineage", action="store_true", help="Require exact source weight provenance for all body-derived mesh records")
    parser.add_argument("--self-test", action="store_true", help="Also reject deliberately corrupted copies of the export")
    args = parser.parse_args()
    verify_sources(DEFAULT_VENDOR)
    raw = args.export.read_bytes()
    payload = json.loads(raw)
    source = load_base(muscle=0.25)
    report = validate_export(payload, source_base=source, require_lineage=args.require_lineage)
    if args.check_source_hash:
        export_manifest = json.loads((REPO_ROOT / "design/character-source/export-manifest.json").read_text())
        blender_version = export_manifest.get("blender_version")
        require(blender_version == "4.5.9 LTS", "Export Blender version differs from pinned authoring tool")
        digest = hashlib.sha256((REPO_ROOT / "tools/character_source/build_character.py").read_bytes()
                                + (REPO_ROOT / "tools/character_source/prepare_base.py").read_bytes()
                                + (DEFAULT_VENDOR / "SOURCE.json").read_bytes()
                                + blender_version.encode()).hexdigest()
        require(payload["sourceRevision"] == digest, "Export is stale relative to source inputs/pinned Blender; rebuild source")
        require(export_manifest.get("source_revision") == digest, "Export manifest and runtime source revision differ")
        report["source_hash_matches"] = True
        report["blender_version"] = blender_version
    report["export_sha256"] = hashlib.sha256(raw).hexdigest()
    if args.self_test:
        report["rejected_corruptions"] = regression_checks(payload, source)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
