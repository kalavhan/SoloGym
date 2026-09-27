#!/usr/bin/env python3
"""Read-only integrity checks for the illustrated character review batch.

Uses only the Python standard library. This verifies records and saved files,
not visual anatomy, approval decisions, production registration or animation.
"""

import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import sys


PRESETS = (
    "skinny", "normal", "chubby", "fat", "skinny_muscular", "muscular",
    "fat_muscular",
)
PRESENTATIONS = ("male", "female")
PAIRS = [(presentation, preset) for presentation in PRESENTATIONS for preset in PRESETS]
DEFAULT_MANIFEST = "design/character-2-5d/approval-r1/manifest.json"


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def parse_json(text):
    return json.loads(text, object_pairs_hook=unique_object)


class Verification:
    def __init__(self, root):
        self.root = root.resolve()
        self.errors = []
        self.files = {}
        self.revision_count = 0

    def require(self, condition, message):
        if not condition:
            self.errors.append(message)
        return condition

    def equal(self, actual, expected, label):
        self.require(actual == expected, f"{label}: expected {expected!r}, got {actual!r}")

    def path(self, value, label, relative_to=None):
        if not self.require(isinstance(value, str) and bool(value), f"{label}: missing path"):
            return None
        candidate = Path(value)
        if not self.require(not candidate.is_absolute(), f"{label}: path must be repository-relative"):
            return None
        candidate = ((relative_to or self.root) / candidate).resolve()
        try:
            candidate.relative_to(self.root)
        except ValueError:
            self.errors.append(f"{label}: path escapes repository")
            return None
        return candidate

    def contents(self, path, label):
        if path is None:
            return None
        if path not in self.files:
            try:
                self.files[path] = path.read_bytes()
            except OSError as error:
                self.errors.append(f"{label}: cannot read {path}: {error}")
                return None
        return self.files[path]

    def object_file(self, path, label):
        data = self.contents(path, label)
        if data is None:
            return {}
        try:
            value = parse_json(data.decode("utf-8"))
        except (UnicodeError, ValueError) as error:
            self.errors.append(f"{label}: invalid JSON: {error}")
            return {}
        return self.object(value, label)

    def object(self, value, label):
        if self.require(isinstance(value, dict), f"{label}: expected object"):
            return value
        return {}

    def array(self, value, label):
        if self.require(isinstance(value, list), f"{label}: expected array"):
            return value
        return []

    def file_record(self, record, label, png=False, require_bytes=True):
        record = self.object(record, label)
        path = self.path(record.get("path"), label)
        data = self.contents(path, label)
        digest = record.get("sha256")
        self.require(isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest),
                     f"{label}: missing or invalid SHA-256")
        if data is None:
            return None
        self.equal(digest, hashlib.sha256(data).hexdigest(), f"{label} SHA-256")
        if require_bytes or "bytes" in record:
            self.require(type(record.get("bytes")) is int, f"{label}: bytes must be an integer")
            self.equal(record.get("bytes"), len(data), f"{label} bytes")
        if png or "width" in record or "height" in record:
            valid_header = (len(data) >= 33 and data[:8] == b"\x89PNG\r\n\x1a\n"
                            and data[8:12] == b"\x00\x00\x00\r" and data[12:16] == b"IHDR")
            if self.require(valid_header, f"{label}: missing valid PNG signature/IHDR"):
                width, height = struct.unpack(">II", data[16:24])
                self.require(width > 0 and height > 0, f"{label}: invalid PNG dimensions")
                for key, actual in (("width", width), ("height", height)):
                    self.require(type(record.get(key)) is int, f"{label}: {key} must be an integer")
                    self.equal(record.get(key), actual, f"{label} {key}")
        return path

    def references(self, values, label):
        refs = self.array(values, label)
        self.require(bool(refs), f"{label}: at least one reference is required")
        for index, value in enumerate(refs):
            self.file_record(value, f"{label}[{index}]", png=True)
        return refs

    def revision(self, value, label):
        value = self.object(value, label)
        revision = value.get("revision")
        self.require(type(revision) is int and revision > 0, f"{label}: revision must be a positive integer")
        self.file_record(value.get("output"), f"{label}.output", png=True)
        self.file_record(value.get("exact_prompt"), f"{label}.exact_prompt")
        self.references(value.get("references"), f"{label}.references")
        self.revision_count += 1
        return revision

    def catalog(self, path):
        data = self.contents(path, "assets.js")
        if data is None:
            return {}
        try:
            source = data.decode("utf-8")
            match = re.fullmatch(
                r"\s*(?://[^\n]*\n\s*)*window\.SOLOGYM_APPROVAL_ASSETS\s*=\s*(\{[\s\S]*\})\s*;\s*",
                source,
            )
            if not match:
                raise ValueError("expected a single window.SOLOGYM_APPROVAL_ASSETS = JSON; assignment")
            return self.object(parse_json(match.group(1)), "assets.js catalog")
        except (UnicodeError, ValueError) as error:
            self.errors.append(f"assets.js: {error}")
            return {}

    def verify(self, manifest_path, catalog_path):
        manifest = self.object_file(manifest_path, "manifest")
        self.equal(manifest.get("asset_count"), len(PAIRS), "manifest asset_count")
        self.equal(manifest.get("presets"), list(PRESETS), "manifest preset order")
        refs = self.references(manifest.get("references"), "manifest.references")
        style_path = self.file_record(manifest.get("style_lock"), "manifest.style_lock")
        style = self.object_file(style_path, "style lock") if style_path else {}
        batch_manifest = self.path(style.get("batch_manifest"), "style lock batch_manifest")
        self.equal(batch_manifest, manifest_path, "style lock manifest selection")
        rules = self.object(style.get("body_preset_rules"), "style lock body_preset_rules")
        self.equal(rules.get("order"), list(PRESETS), "style lock preset order")

        assets = [self.object(value, f"manifest.assets[{index}]") for index, value in
                  enumerate(self.array(manifest.get("assets"), "manifest.assets"))]
        self.equal([(asset.get("presentation"), asset.get("preset")) for asset in assets],
                   PAIRS, "manifest presentation/preset order")
        expected_ids = [f"{presentation}-{preset}" for presentation, preset in PAIRS]
        expected_asset_ids = [f"illustrated_{presentation}_{preset}_r1" for presentation, preset in PAIRS]
        self.equal([asset.get("id") for asset in assets], expected_ids, "manifest proposal IDs/order")
        self.equal([asset.get("asset_id") for asset in assets], expected_asset_ids,
                   "manifest stable asset IDs/order")
        self.equal(style.get("review_asset_ids"), expected_asset_ids, "style lock review IDs/order")
        for asset in assets:
            label = f"manifest asset {asset.get('id', '?')}"
            self.equal(asset.get("fit_family_id"),
                       f"{asset.get('presentation')}_{asset.get('preset')}_r1", f"{label} fit ID")
            selected_revision = self.revision(asset, label)
            priors = self.array(asset.get("prior_revisions"), f"{label}.prior_revisions")
            numbers = [self.revision(prior, f"{label}.prior_revisions[{index}]")
                       for index, prior in enumerate(priors)]
            if type(selected_revision) is int and selected_revision > 0:
                self.equal(numbers, list(range(1, selected_revision)), f"{label} retained prior revisions")
            paths = [self.object(prior, label).get("output", {}).get("path") for prior in priors
                     if isinstance(self.object(prior, label).get("output"), dict)]
            selected_output = self.object(asset.get("output"), f"{label}.output")
            self.require(selected_output.get("path") not in paths,
                         f"{label}: selected output overwrites a prior revision path")

        self.verify_style_selection(style, assets, refs)
        self.verify_catalog(self.catalog(catalog_path), manifest, manifest_path, catalog_path, assets, refs)

    def verify_style_selection(self, style, assets, manifest_refs):
        boards = [self.object(value, f"style requested_boards[{index}]") for index, value in
                  enumerate(self.array(style.get("requested_boards"), "style requested_boards"))]
        self.equal([board.get("asset_id") for board in boards],
                   [asset.get("asset_id") for asset in assets], "style candidate IDs/order")
        for index, (board, asset) in enumerate(zip(boards, assets)):
            label = f"style requested_boards[{index}]"
            for key in ("asset_id", "fit_family_id", "presentation"):
                self.equal(board.get(key), asset.get(key), f"{label} {key}")
            self.equal(board.get("build_preset_id"), asset.get("preset"), f"{label} preset")
            self.equal(board.get("candidate_revision"), asset.get("revision"), f"{label} selected revision")
            output = self.object(asset.get("output"), f"asset[{index}].output")
            self.equal(board.get("candidate_path"), output.get("path"), f"{label} selected path")

        style_refs = self.array(style.get("references"), "style.references")
        self.equal([self.object(ref, "style reference").get("asset_id") for ref in style_refs],
                   [self.object(ref, "manifest reference").get("asset_id") for ref in manifest_refs],
                   "style/manifest reference IDs/order")
        for index, (style_ref, manifest_ref) in enumerate(zip(style_refs, manifest_refs)):
            style_ref = self.object(style_ref, "style reference")
            manifest_ref = self.object(manifest_ref, "manifest reference")
            self.file_record(style_ref, f"style.references[{index}]", require_bytes=False)
            for key in ("path", "sha256"):
                self.equal(style_ref.get(key), manifest_ref.get(key), f"style.references[{index}] {key}")

    def verify_catalog(self, catalog, manifest, manifest_path, catalog_path, assets, references):
        manifest_bytes = self.contents(manifest_path, "manifest")
        if manifest_bytes is not None:
            self.equal(catalog.get("manifest_sha256"), hashlib.sha256(manifest_bytes).hexdigest(),
                       "catalog manifest SHA-256")
        self.equal(catalog.get("batch_id"), manifest.get("batch_id"), "catalog batch ID")
        styles = [ref for ref in references if isinstance(ref, dict) and ref.get("asset_id") == "illustrated_style_r1"]
        self.require(len(styles) == 1, "manifest references: expected one illustrated_style_r1")
        if len(styles) == 1:
            self.equal(catalog.get("style_reference_sha256"), styles[0].get("sha256"),
                       "catalog style reference SHA-256")
        proposals = self.object(catalog.get("proposals"), "catalog.proposals")
        self.equal(list(proposals), [asset.get("id") for asset in assets], "catalog IDs/order")
        for asset in assets:
            label = f"catalog proposal {asset.get('id', '?')}"
            proposal = self.object(proposals.get(asset.get("id")), label)
            output = self.object(asset.get("output"), f"{label} manifest output")
            for key in ("asset_id", "revision"):
                self.equal(proposal.get(key), asset.get(key), f"{label} {key}")
            self.equal(proposal.get("sha256"), output.get("sha256"), f"{label} SHA-256")
            image_path = self.path(proposal.get("image"), label, relative_to=catalog_path.parent)
            selected_path = self.path(output.get("path"), f"{label} selected output")
            self.equal(image_path, selected_path, f"{label} selected image")
            for key in ("bytes", "width", "height"):
                if key in proposal:
                    self.equal(proposal[key], output.get(key), f"{label} {key}")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1],
                        help="Repository root (defaults to this script's repository).")
    parser.add_argument("--manifest", default=DEFAULT_MANIFEST,
                        help="Repository-relative batch manifest path.")
    parser.add_argument("--catalog", help="Repository-relative assets.js path; defaults beside manifest.")
    args = parser.parse_args(argv)
    verification = Verification(args.root)
    manifest_path = verification.path(args.manifest, "manifest CLI argument")
    catalog_path = (verification.path(args.catalog, "catalog CLI argument") if args.catalog else
                    manifest_path.parent / "assets.js" if manifest_path else None)
    if manifest_path and catalog_path:
        verification.verify(manifest_path, catalog_path)
    if verification.errors:
        print(f"Character art verification FAILED ({len(verification.errors)} issues):", file=sys.stderr)
        for error in verification.errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print(f"Character art verification passed: {len(PAIRS)} ordered proposals, "
          f"{verification.revision_count} current/prior revisions, "
          f"{len(verification.files)} unique files; hashes, sizes, PNG dimensions and selected revisions agree.")
    print("Read-only integrity check; visual quality, user approval and runtime readiness are not inferred.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
