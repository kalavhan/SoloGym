#!/usr/bin/env python3
"""Copy an explicitly selected, verified capture run into this local review."""

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import struct
import tempfile


REVIEW = Path(__file__).resolve().parent
ROOT = REVIEW.parents[3]
REGISTRATION = ROOT / "app/Assets/SoloGym/Resources/AvatarIllustrated/Registration.json"
PRESENTATIONS = ("male", "female")
VIEWS = ("studio", "gameplay")
ACTIONS = ("idle", "walk", "jab")
OUTFITS = ("equipped", "base")
SUFFIXES = ("base-bind", "equipped-bind", "skin-deep", "hair-silver")
EXPECTED_SEQUENCES = 24
EXPECTED_FRAMES = 16 + EXPECTED_SEQUENCES * 24


def digest(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def relative(path):
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return str(path.resolve())


def package(source, registration):
    source = source.resolve()
    proof_bytes = (source / "illustrated-proof.json").read_bytes()
    verification_bytes = (source / "verification.json").read_bytes()
    proof = json.loads(proof_bytes)
    verification = json.loads(verification_bytes)
    registration_bytes = registration.read_bytes()
    registration_hash = digest(registration_bytes)
    require(proof.get("schema") == "sologym.illustrated-character-proof.v1", "Unexpected proof schema.")
    require(proof.get("structural_passed") is True and not proof.get("failures"), "Runtime proof has not passed.")
    require(verification.get("structural_passed") is True, "Runtime verification has not passed.")
    require(verification.get("visual_acceptance_inferred") is False, "Verification must not infer visual approval.")
    require(verification.get("runtime_proof_sha256") == digest(proof_bytes), "Verification describes a different proof report.")
    require(proof.get("source_manifest_sha256") == registration_hash, "Registration snapshot does not match the captured run.")
    require(verification.get("source_manifest_sha256") == registration_hash, "Verification registration hash does not match.")
    frames = proof.get("frames", [])
    sequences = proof.get("motion_sequences", [])
    require(len(frames) == EXPECTED_FRAMES and len(sequences) == EXPECTED_SEQUENCES,
            "Expected the complete 592-frame / 24-sequence proof, including base outfit motion.")
    require(all(frame.get("structural_passed") is True for frame in frames), "A captured frame failed structural validation.")

    static_names = {
        f"static/{presentation}-{view}-{suffix}.png"
        for presentation in PRESENTATIONS for view in VIEWS for suffix in SUFFIXES
    }
    videos = {
        f"{presentation}-{view}{'-base' if outfit == 'base' else ''}-{action}.mp4"
        for presentation in PRESENTATIONS for view in VIEWS for outfit in OUTFITS for action in ACTIONS
    }
    menus = {f"{presentation}-studio-menu.png" for presentation in PRESENTATIONS}
    require({frame["png"] for frame in frames if not frame.get("sequence_id")} == static_names,
            "Static frame names do not match the review page.")
    require({item["png"] for item in proof.get("studio_screenshots", [])} == menus,
            "Full mobile studio captures are incomplete.")
    video_records = {item["path"]: item for item in verification.get("videos", [])}
    require(set(video_records) == videos, "Verified video names do not match the review page.")
    captured = {name: (source / name).read_bytes() for name in sorted(static_names | videos | menus)}
    for name in videos:
        require(digest(captured[name]) == video_records[name]["sha256"], f"Verified video changed: {name}")
    for name in static_names:
        record = verification.get("captures", {}).get(Path(name).stem, {})
        require(digest(captured[name]) == record.get("sha256"), f"Verified static capture changed: {name}")
    for name in static_names | menus:
        require(captured[name][:8] == b"\x89PNG\r\n\x1a\n", f"Not a PNG: {name}")
        require(len(captured[name]) >= 128, f"Empty PNG: {name}")
        width, height = struct.unpack(">II", captured[name][16:24])
        require(width > 0 and height > 0, f"Invalid PNG dimensions: {name}")
    captured["illustrated-proof.json"] = proof_bytes
    captured["verification.json"] = verification_bytes
    captured["Registration.snapshot.json"] = registration_bytes
    maximum_ground = max(float(frame["ground_error"]) for frame in frames)
    maximum_registration = max(float(frame.get("registration_ground_error", 0)) for frame in frames)

    output = REVIEW / "runtime"
    if output.exists():
        unexpected = {path.relative_to(output).as_posix() for path in output.rglob("*") if path.is_file()} - set(captured) - {"package-manifest.json"}
        require(not unexpected, "Unexpected files in runtime; preserve or relocate them before packaging: " + ", ".join(sorted(unexpected)))
    metadata = {
        "schema": "sologym.illustrated-review-package.v1",
        "packaged_at_utc": datetime.now(timezone.utc).isoformat(),
        "source_capture_directory": relative(source),
        "structural_passed": True,
        "visual_review_status": "pending",
        "production_ready": False,
        "scope": "Normal man/woman; studio/gameplay; complete base/equipped states and procedural idle/walk/jab; skin/hair tint samples.",
        "known_limits": [
            "Shoulder joins and clothing behind moving arms still require production art polish.",
            "Whole outfit states do not establish independent mix-and-match gear.",
            "Motion covers only the authored studio and elevated gameplay projections.",
            "Structural verification is not visual approval or production readiness."
        ],
        "registration": {
            "original_path": relative(registration),
            "snapshot": "Registration.snapshot.json",
            "sha256": registration_hash
        },
        "capture_counts": {"static_pngs": 16, "motion_mp4s": 24, "full_menu_pngs": 2,
                           "verified_frames": EXPECTED_FRAMES, "verified_motion_sequences": EXPECTED_SEQUENCES},
        "raw_motion_frames_packaged": False,
        "files": [{"path": name, "bytes": len(data), "sha256": digest(data)} for name, data in sorted(captured.items())]
    }
    with tempfile.TemporaryDirectory(prefix=".runtime-package-", dir=REVIEW) as temp:
        staging = Path(temp)
        for name, data in captured.items():
            target = staging / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        (staging / "package-manifest.json").write_text(json.dumps(metadata, indent=2) + "\n")
        output.mkdir(exist_ok=True)
        for path in sorted(staging.rglob("*")):
            if not path.is_file():
                continue
            target = output / path.relative_to(staging)
            target.parent.mkdir(parents=True, exist_ok=True)
            path.replace(target)
    for record in metadata["files"]:
        require(digest((output / record["path"]).read_bytes()) == record["sha256"], "Copied file hash mismatch: " + record["path"])

    qa = f"""# Runtime review package QA

Capture run: {relative(source)}

Packaged at: {metadata['packaged_at_utc']}

This package contains real Unity proof outputs: 16 static comparisons, 24
motion videos and two full mobile studio screenshots. It covers only the normal
man and woman in studio and elevated gameplay views, fixed hairstyles, skin/hair
tints and whole outfit swaps. Motion is procedural. The videos show both base
and equipped characters so their shoulder and clothing continuity can be
reviewed separately.

The capture verifier reported structural success for all {len(frames)} sampled frames and
{len(sequences)} sequences. The maximum reported source alpha sole-contour pose error was
{maximum_ground:.8f} source pixels. The separate maximum metadata-to-alpha
registration offset was {maximum_registration:.8f} source pixels. The verifier
records interactive capture staying open as {verification.get('interactive_capture_stays_open')}.
These checks do not establish visual quality or production readiness.

Shoulder joins and clothing behind moving arms still require production art
polish. Independent interchangeable gear, new hairstyles, the other body presets,
unseen directions and release-ready animation are outside this proof.
**Visual review remains pending; no approval is inferred.**

## Registration snapshot

- Original: {relative(registration)}
- Frozen copy: runtime/Registration.snapshot.json
- SHA-256: {registration_hash}
- Runtime report SHA-256: {digest(proof_bytes)}
- Verification report SHA-256: {digest(verification_bytes)}

runtime/package-manifest.json records every packaged file's exact bytes and
SHA-256. Packaging copied media without pixel edits or re-encoding and did not
alter approved masters. The full reports are retained unchanged; their raw
motion-frame PNG references still point into the original capture run. Those
576 PNGs are intentionally omitted from this compact review package.
"""
    (REVIEW / "QA.md").write_text(qa)
    print(f"Packaged {len(captured)} source files plus integrity manifest into {relative(output)}")
    print(f"Registration SHA-256: {registration_hash}; visual review: pending")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True, help="Explicit completed verifier output directory.")
    parser.add_argument("--registration", type=Path, default=REGISTRATION, help="Registration JSON matching that run.")
    args = parser.parse_args()
    try:
        package(args.source, args.registration)
    except (OSError, KeyError, ValueError, TypeError) as error:
        parser.exit(1, "Packaging stopped: " + str(error) + "\n")


if __name__ == "__main__":
    main()
