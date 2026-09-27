#!/usr/bin/env python3
"""Read vendored CC0 MakeHuman data without importing MakeHuman application code.

The returned positions preserve the original hm08 vertex indices. Only ``faces``
are rendered; joint and clothing helper vertices remain available for fitting.
Coordinates are Unity-compatible meters: X character-left, Y up, Z front. Ground
is Y=0. The Blender builder converts these with (x, -z, y).

This loader and its reduction rules are SoloGym code. The unmodified graphical
source files have their own CC0 provenance in vendor/makehuman/SOURCE.json.
"""

from __future__ import annotations

import argparse
from collections import defaultdict
import hashlib
import json
import math
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_VENDOR = REPO_ROOT / "design/character-source/vendor/makehuman"
MALE_TARGET = "caucasian-male-young.target"
MUSCLE_TARGET = "universal-male-young-maxmuscle-averageweight.target"


def _read_obj(path: Path) -> dict:
    vertices, uvs, faces, face_uvs, groups = [], [], [], [], []
    group = ""
    for line in path.read_text().splitlines():
        fields = line.split()
        if not fields or fields[0].startswith("#"):
            continue
        if fields[0] == "v":
            vertices.append([float(x) for x in fields[1:4]])
        elif fields[0] == "vt":
            uvs.append([float(x) for x in fields[1:3]])
        elif fields[0] == "g":
            group = " ".join(fields[1:])
        elif fields[0] == "f":
            tokens = [token.split("/") for token in fields[1:]]
            faces.append([int(token[0]) - 1 for token in tokens])
            face_uvs.append([int(token[1]) - 1 if len(token) > 1 and token[1] else -1 for token in tokens])
            groups.append(group)
    return dict(vertices=vertices, uvs=uvs, faces=faces, face_uvs=face_uvs, groups=groups)


def _target(path: Path, count: int) -> list:
    result = [[0.0, 0.0, 0.0] for _ in range(count)]
    for line in path.read_text().splitlines():
        fields = line.split()
        if not fields or fields[0].startswith("#"):
            continue
        if len(fields) != 4:
            raise ValueError(f"Malformed target line in {path.name}: {line!r}")
        index = int(fields[0])
        if not 0 <= index < count:
            raise ValueError(f"Target index {index} outside source topology")
        result[index] = [float(value) for value in fields[1:]]
    return result


def _mean(points: list) -> list:
    return [sum(point[axis] for point in points) / len(points) for axis in range(3)]


def _collapse_bone(name: str) -> str:
    side = name[-2:] if name.endswith((".L", ".R")) else ""
    if name == "root" or name.startswith("pelvis."):
        return "pelvis"
    if name in ("spine05", "spine04", "spine03"):
        return "spine"
    if name in ("spine02", "spine01") or name.startswith("breast."):
        return "chest"
    if name.startswith("neck"):
        return "neck"
    for prefix, destination in (
        ("clavicle", "clavicle"), ("shoulder", "clavicle"),
        ("upperarm", "upper_arm"), ("lowerarm", "forearm"),
        ("wrist", "hand"), ("finger", "hand"), ("metacarpal", "hand"),
        ("upperleg", "thigh"), ("lowerleg", "shin"),
        ("foot", "foot"), ("toe", "toe"),
    ):
        if name.startswith(prefix):
            return destination + side
    # All remaining default-rig groups are facial, eye, jaw or tongue groups.
    return "head"


def _skeleton(joints: dict) -> list:
    def joint(bone: str, end: str = "head") -> list:
        return joints[f"{bone}____{end}"]

    bones = [dict(name="root", parent=None, position=[0.0, 0.0, 0.0], tail=[0.0, 0.1, 0.0])]

    def add(name: str, parent: str, head: list, tail: list):
        bones.append(dict(name=name, parent=parent, position=head, tail=tail))

    add("pelvis", "root", joint("root"), joint("spine05"))
    add("spine", "pelvis", joint("spine05"), joint("spine02"))
    add("chest", "spine", joint("spine02"), joint("neck01"))
    add("neck", "chest", joint("neck01"), joint("head"))
    add("head", "neck", joint("head"), joint("head", "tail"))
    for side in (".L", ".R"):
        add("clavicle" + side, "chest", joint("clavicle" + side), joint("upperarm01" + side))
        add("upper_arm" + side, "clavicle" + side, joint("upperarm01" + side), joint("lowerarm01" + side))
        add("forearm" + side, "upper_arm" + side, joint("lowerarm01" + side), joint("wrist" + side))
        add("hand" + side, "forearm" + side, joint("wrist" + side), joint("finger3-3" + side, "tail"))
        add("thigh" + side, "pelvis", joint("upperleg01" + side), joint("lowerleg01" + side))
        add("shin" + side, "thigh" + side, joint("lowerleg01" + side), joint("foot" + side))
        add("foot" + side, "shin" + side, joint("foot" + side), joint("toe3-1" + side))
        add("toe" + side, "foot" + side, joint("toe3-1" + side), joint("toe3-3" + side, "tail"))
    return bones


def verify_sources(vendor_dir: str | Path = DEFAULT_VENDOR) -> dict:
    vendor = Path(vendor_dir)
    manifest = json.loads((vendor / "SOURCE.json").read_text())
    if manifest["asset_license"] != "CC0-1.0":
        raise ValueError("Unexpected source license")
    for item in manifest["files"]:
        data = (vendor / item["local_file"]).read_bytes()
        if len(data) != item["bytes"] or hashlib.sha256(data).hexdigest() != item["sha256"]:
            raise ValueError(f"Vendored source differs from manifest: {item['local_file']}")
    return manifest


def load_base(vendor_dir: str | Path = DEFAULT_VENDOR, muscle: float = 0.5) -> dict:
    """Return morphed base, 22-bone rig, weights, eye mesh and muscle endpoints.

    ``positions`` and each shape preserve every source index. Render ``faces``
    only. ``weights[index]`` contains at most four ``[bone_name, weight]`` pairs,
    normalized after reducing twist/facial/finger groups. Empty helper weights
    are allowed, but every rendered body vertex must be weighted. ``bones`` use
    absolute joint coordinates; consumers compute parent-relative transforms.
    Body shape endpoints share topology and the same ground origin. Their source
    joint coordinates are supplied separately to expose any fit changes.
    """
    if not math.isfinite(muscle) or not 0.0 <= muscle <= 1.0:
        raise ValueError("muscle must be finite and within 0..1")
    vendor = Path(vendor_dir)
    source = verify_sources(vendor)
    obj = _read_obj(vendor / "base.obj")
    original = obj["vertices"]
    male = _target(vendor / MALE_TARGET, len(original))
    muscle_delta = _target(vendor / MUSCLE_TARGET, len(original))
    body_faces = [index for index, group in enumerate(obj["groups"]) if group == "body"]
    faces = [obj["faces"][index] for index in body_faces]
    used = sorted({index for face in faces for index in face})

    def morph(strength: float) -> list:
        return [[point[axis] + male[i][axis] + muscle_delta[i][axis] * strength
                 for axis in range(3)] for i, point in enumerate(original)]

    source_positions = morph(muscle)
    source_ground = min(source_positions[index][1] for index in used)

    def convert(points: list) -> list:
        return [[p[0] * 0.1, (p[1] - source_ground) * 0.1, p[2] * 0.1] for p in points]

    positions = convert(source_positions)
    source_rig = json.loads((vendor / "default.mhskel").read_text())

    def joint_positions(points: list) -> dict:
        return {name: _mean([points[index] for index in indices])
                for name, indices in source_rig["joints"].items()}

    joints = joint_positions(positions)
    bones = _skeleton(joints)
    bone_names = {bone["name"] for bone in bones}
    source_weights = json.loads((vendor / "default_weights.mhw").read_text())["weights"]
    accumulated = [defaultdict(float) for _ in original]
    for source_name, entries in source_weights.items():
        name = _collapse_bone(source_name)
        if name not in bone_names:
            raise ValueError(f"Unmapped bone {source_name}")
        for index, weight in entries:
            accumulated[index][name] += weight
    weights = []
    for entries in accumulated:
        strongest = sorted(entries.items(), key=lambda pair: (-pair[1], pair[0]))[:4]
        total = sum(weight for _, weight in strongest)
        weights.append([[name, weight / total] for name, weight in strongest] if total else [])

    eye_obj = _read_obj(vendor / "low-poly.obj")
    eye_map = []
    reading = False
    for line in (vendor / "low-poly.mhclo").read_text().splitlines():
        fields = line.split()
        if fields == ["verts", "0"]:
            reading = True
            continue
        if reading and fields and not fields[0].startswith("#"):
            if len(fields) != 1 or not fields[0].isdigit():
                raise ValueError("Unexpected eye proxy format; single-index map required")
            eye_map.append(int(fields[0]))
    eyes = dict(
        positions=[positions[index] for index in eye_map],
        faces=eye_obj["faces"], uvs=eye_obj["uvs"], face_uvs=eye_obj["face_uvs"],
        source_indices=eye_map, weights=[[["head", 1.0]] for _ in eye_map],
        texture=str(vendor / "brown_eye.png"),
    )
    if len(eye_map) != len(eye_obj["vertices"]):
        raise ValueError("Eye fitting map does not match eye topology")
    shapes = {}
    for name, strength in (("muscle025", 0.25), ("muscle100", 1.0)):
        shape_positions = convert(morph(strength))
        shapes[name] = dict(muscle=strength, positions=shape_positions,
                            joint_positions=joint_positions(shape_positions))
    landmarks = {name: joints[f"{name}____head"] for name in (
        "eye.L", "eye.R", "head", "jaw", "neck01", "root",
        "upperarm01.L", "upperarm01.R", "lowerarm01.L", "lowerarm01.R",
        "wrist.L", "wrist.R", "upperleg01.L", "upperleg01.R",
        "lowerleg01.L", "lowerleg01.R", "foot.L", "foot.R",
    )}
    result = dict(
        schema_version=1, source_commit=source["commit"], muscle=muscle,
        coordinates="Unity meters: X character-left, Y up, Z front; ground Y=0",
        positions=positions, faces=faces, source_positions=source_positions,
        source_ground=source_ground, source_vertex_indices=list(range(len(original))),
        body_vertex_indices=used, body_vertex_count=len(used),
        uvs=obj["uvs"], face_uvs=[obj["face_uvs"][index] for index in body_faces],
        body_groups=["body"] * len(faces), bones=bones, weights=weights,
        joint_positions=joints, landmarks=landmarks, eyes=eyes, shapes=shapes,
        bounds=dict(min=[min(positions[i][a] for i in used) for a in range(3)],
                    max=[max(positions[i][a] for i in used) for a in range(3)]),
    )
    validate_base(result)
    return result


def validate_base(data: dict) -> None:
    """Contract checks for source drift, indices, rig hierarchy and skin coverage."""
    positions = data["positions"]
    if len(positions) != 19158 or data["body_vertex_count"] != 13380 or len(data["faces"]) != 13378:
        raise ValueError("Unexpected hm08 topology; review source revision before using it")
    if not all(math.isfinite(value) for point in positions for value in point):
        raise ValueError("Non-finite position")
    if not 1.4 < data["bounds"]["max"][1] < 2.1 or abs(data["bounds"]["min"][1]) > 1e-8:
        raise ValueError("Unexpected scale or ground origin")
    if any(len(face) != 4 or min(face) < 0 or max(face) >= 13380 for face in data["faces"]):
        raise ValueError("Body includes non-body helper faces or unexpected topology")
    known = set()
    for bone in data["bones"]:
        if bone["name"] in known or (bone["parent"] is not None and bone["parent"] not in known):
            raise ValueError("Invalid skeleton hierarchy")
        if sum((a - b) ** 2 for a, b in zip(bone["position"], bone["tail"])) < 1e-10:
            raise ValueError(f"Zero-length bone {bone['name']}")
        known.add(bone["name"])
    if len(known) != 22:
        raise ValueError("Unexpected reduced rig")
    for index in data["body_vertex_indices"]:
        entries = data["weights"][index]
        if not 1 <= len(entries) <= 4 or abs(sum(w for _, w in entries) - 1.0) > 1e-8:
            raise ValueError(f"Invalid weights at body vertex {index}")
        if any(name not in known or w <= 0 for name, w in entries):
            raise ValueError(f"Invalid weight assignment at body vertex {index}")
    for shape in data["shapes"].values():
        if len(shape["positions"]) != len(positions):
            raise ValueError("Shape topology changed")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--vendor", type=Path, default=DEFAULT_VENDOR)
    parser.add_argument("--muscle", type=float, default=0.5)
    parser.add_argument("--output", type=Path, help="Optional intermediate data JSON")
    args = parser.parse_args()
    data = load_base(args.vendor, args.muscle)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(data, separators=(",", ":")) + "\n")
    print(json.dumps(dict(source_commit=data["source_commit"],
        source_vertices=len(data["positions"]), body_vertices=data["body_vertex_count"],
        body_faces=len(data["faces"]), bones=len(data["bones"]),
        eye_vertices=len(data["eyes"]["positions"]), bounds=data["bounds"],
        landmarks=data["landmarks"], checks="passed"), indent=2))


if __name__ == "__main__":
    main()
