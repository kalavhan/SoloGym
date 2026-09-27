#!/usr/bin/env python3
"""Capture and verify the illustrated character proof; never infer visual approval.

Run under a graphical display, for example:
  xvfb-run -a -s '-screen 0 1200x2000x24' python3 tools/verify_illustrated_character_proof.py

Requires the built Unity player and, unless --no-videos is used, ffmpeg. Python
uses only its standard library. --verify-only checks an existing output folder
without starting the player, writing reports, or encoding videos.
Captures and encodes all 24 base and equipped motion sequences.
"""

import argparse
from datetime import datetime, timezone
import hashlib
import itertools
import json
import math
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import time


ROOT = Path(__file__).resolve().parents[1]
PRESENTATIONS = ("male", "female")
VIEWS = ("studio", "gameplay")
ACTIONS = ("idle", "walk", "jab")
OUTFITS = ("base", "equipped")
ERROR_MARKERS = (
    "NullReferenceException", "ArgumentException", "InvalidOperationException",
    "Shader error", "SOLOGYM_ILLUSTRATED_PROOF_FAILED", "SOLOGYM_ILLUSTRATED_CHARACTER_LOAD",
    "SOLOGYM_ILLUSTRATED_LOAD", "SOLOGYM_ILLUSTRATED_PROOF_WRITE",
)


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def inside(directory, relative):
    require(isinstance(relative, str) and relative, "Missing output path")
    path = Path(relative)
    require(not path.is_absolute(), f"Expected relative output path: {relative}")
    result = (directory / path).resolve()
    require(result.is_relative_to(directory.resolve()), f"Output path escapes proof directory: {relative}")
    return result


def png_info(path):
    data = path.read_bytes()
    require(len(data) >= 128 and data[:8] == b"\x89PNG\r\n\x1a\n" and data[12:16] == b"IHDR",
            f"Missing or invalid PNG: {path}")
    width, height = struct.unpack(">II", data[16:24])
    require(width >= 64 and height >= 64, f"Unexpectedly small PNG: {path} ({width}x{height})")
    return {"width": width, "height": height, "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest()}


def metric(frame, key):
    value = frame.get(key)
    require(type(value) in (int, float) and math.isfinite(value),
            f"{frame.get('id', '?')}: missing/nonfinite {key}")
    return value


def validate_frame(frame, directory):
    name = frame.get("id", frame.get("png", "?"))
    require(frame.get("structural_passed") is True, f"{name}: {frame.get('error', 'structural check failed')}")
    require(metric(frame, "invalid_vertex_count") == 0, f"{name}: invalid mesh vertices")
    require(metric(frame, "mesh_vertex_count") > 0, f"{name}: empty mesh")
    require(abs(metric(frame, "weight_sum_error")) <= .0001, f"{name}: skin weights not normalized")
    require(abs(metric(frame, "ground_error")) <= .05, f"{name}: transformed sole contour error exceeds .05 source pixels")
    require(metric(frame, "max_vertex_displacement") >= 0, f"{name}: invalid displacement")
    require(isinstance(frame.get("entry_id"), str) and frame["entry_id"], f"{name}: missing entry provenance")
    require(frame.get("source_revision"), f"{name}: missing source revision")
    info = png_info(inside(directory, frame.get("png")))
    require((info["width"], info["height"]) == (frame.get("width"), frame.get("height")),
            f"{name}: reported PNG dimensions differ")
    return info


def verify(directory):
    report_path = directory / "illustrated-proof.json"
    proof = json.loads(report_path.read_text())
    require(proof.get("schema") == "sologym.illustrated-character-proof.v1", "Unexpected proof schema")
    require(proof.get("renderer") == "illustrated", "Proof did not use illustrated renderer")
    require(proof.get("structural_passed") is True and not proof.get("failures"),
            f"Runtime proof failed: {proof.get('failures')}")
    require(proof.get("capture_scope") in ("runtime_stage", "runtime_character"),
            "Character comparisons require isolated runtime captures, excluding changing UI labels")
    require(proof.get("ground_error_units") == "source_cell_pixels", "Unknown ground-error units")
    resource = proof.get("source_manifest_resource", "AvatarIllustrated/Registration")
    require(isinstance(resource, str) and not Path(resource).is_absolute() and ".." not in Path(resource).parts,
            "Invalid registration resource path")
    candidates = [ROOT / "app/Assets/SoloGym/Resources" / (resource + suffix) for suffix in (".json", ".bytes")]
    source = next((path for path in candidates if path.is_file()), None)
    require(source is not None, f"Workspace registration source missing: {resource}")
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    require(proof.get("source_manifest_sha256") == source_hash,
            "Loaded registration hash differs from workspace; rebuild the player")
    registration = json.loads(source.read_text())
    entry_ids = {(entry["presentation"], entry["view"]): entry["id"] for entry in registration["entries"]}

    frames = proof.get("frames", [])
    require(isinstance(frames, list) and len(frames) == 592, "Expected 592 individual frame records")
    for frame in frames:
        require(frame.get("entry_id") == entry_ids.get((frame.get("presentation"), frame.get("camera"))),
                f"{frame.get('id')}: selected entry does not match registration")
        require(frame.get("source_revision") == registration["sourceRevision"],
                f"{frame.get('id')}: selected source revision does not match registration")
    static = [frame for frame in frames if not frame.get("sequence_id")]
    expected = set()
    for presentation, view in itertools.product(PRESENTATIONS, VIEWS):
        for outfit in OUTFITS:
            expected.add((presentation, view, outfit, "source", "black", "bind"))
        expected.add((presentation, view, "equipped", "deep", "black", "bind"))
        expected.add((presentation, view, "equipped", "source", "silver", "bind"))
    keys = ("presentation", "camera", "outfit", "skin", "hair", "action")
    require(len(static) == 16 and {tuple(frame.get(key) for key in keys) for frame in static} == expected,
            "Expected 16 bind/outfit/independent-tint states for both presentations and views")
    require(len({frame.get("id") for frame in frames}) == len(frames), "Duplicate frame IDs")
    captures = {}
    state_hashes = {}
    for frame in static:
        info = validate_frame(frame, directory)
        require(metric(frame, "max_vertex_displacement") <= .05,
                f"{frame['id']}: bind pose deforms the registered source")
        captures[frame["id"]] = info
        state_hashes[tuple(frame[key] for key in keys)] = info["sha256"]
    for presentation, view in itertools.product(PRESENTATIONS, VIEWS):
        default = state_hashes[(presentation, view, "equipped", "source", "black", "bind")]
        for outfit, skin, hair in (("base", "source", "black"), ("equipped", "deep", "black"),
                                  ("equipped", "source", "silver")):
            require(default != state_hashes[(presentation, view, outfit, skin, hair, "bind")],
                    f"No rendered character change: {presentation}/{view}/{outfit}/{skin}/{hair}")

    sequences = proof.get("motion_sequences", [])
    require(isinstance(sequences, list) and len(sequences) == 24, "Expected 24 motion sequences")
    require({(sequence.get("presentation"), sequence.get("camera"), sequence.get("outfit"), sequence.get("action"))
             for sequence in sequences} == set(itertools.product(PRESENTATIONS, VIEWS, OUTFITS, ACTIONS)),
            "Incomplete base/equipped motion state matrix")
    require(len({sequence.get("id") for sequence in sequences}) == len(sequences), "Duplicate motion sequence IDs")
    motions = {}
    motion_hashes = {}
    for sequence in sequences:
        name = sequence.get("id")
        expected_id = (sequence["presentation"] + "-" + sequence["camera"] +
                       ("-base-" if sequence["outfit"] == "base" else "-") + sequence["action"])
        require(name == expected_id, f"{name}: unexpected motion sequence ID; expected {expected_id}")
        require(sequence.get("png_pattern") == f"motion/{name}/frame-%03d.png",
                f"{name}: unexpected motion filename pattern")
        require(sequence.get("fps") == 12 and sequence.get("frame_count") == 24,
                f"{name}: expected 24 deterministic frames at 12 fps")
        require((sequence.get("skin"), sequence.get("hair")) == ("source", "black"),
                f"{name}: unexpected motion appearance")
        motion_frames = [frame for frame in frames if frame.get("sequence_id") == name]
        require(isinstance(motion_frames, list) and len(motion_frames) == 24,
                f"{name}: animated phases need individual validation records")
        require([frame.get("frame_index") for frame in motion_frames] == list(range(24)),
                f"{name}: missing or unordered frame indices")
        hashes = []
        displacements = []
        dimensions = set()
        for frame in motion_frames:
            require(all(frame.get(key) == sequence.get(key) for key in keys),
                    f"{frame.get('id')}: frame state does not match motion sequence")
            require(frame.get("png") == sequence["png_pattern"] % frame["frame_index"],
                    f"{frame.get('id')}: frame path does not match motion sequence")
            info = validate_frame(frame, directory)
            hashes.append(info["sha256"])
            displacements.append(metric(frame, "max_vertex_displacement"))
            dimensions.add((info["width"], info["height"]))
            require(abs(metric(frame, "pose_seconds") - frame["frame_index"] / 12) < .0001,
                    f"{name}: frame timing is not deterministic")
        require(len(dimensions) == 1, f"{name}: frame dimensions changed during motion")
        require(len(set(hashes)) > 1 and max(displacements) > .001,
                f"{name}: animation produced no visible/mesh change")
        motion_hashes[(sequence["presentation"], sequence["camera"], sequence["outfit"], sequence["action"])] = hashes
        motions[name] = {"frame_count": 24, "fps": 12, "outfit": sequence["outfit"],
                         "distinct_frame_hashes": len(set(hashes)),
                         "width": next(iter(dimensions))[0], "height": next(iter(dimensions))[1]}
    for presentation, view, action in itertools.product(PRESENTATIONS, VIEWS, ACTIONS):
        require(motion_hashes[(presentation, view, "base", action)] !=
                motion_hashes[(presentation, view, "equipped", action)],
                f"{presentation}/{view}/{action}: base and equipped motion render identically")
    screenshots = proof.get("studio_screenshots", [])
    require(isinstance(screenshots, list) and len(screenshots) >= 2, "Missing full studio screenshots")
    for screenshot in screenshots:
        info = png_info(inside(directory, screenshot["png"]))
        require((info["width"], info["height"]) == (853, 1844), "Full studio screenshot must be 853×1844")
    result = {"structural_passed": True, "source_manifest_sha256": source_hash,
              "runtime_proof_sha256": hashlib.sha256(report_path.read_bytes()).hexdigest(),
              "captures": captures, "motion_sequences": motions,
              "visual_acceptance_inferred": False,
              "limits": ["Grounding measures transformed source alpha contours; it does not establish convincing foot shape or contact during motion.",
                         "Changed rendered hashes do not establish tint-mask isolation or visual quality.",
                         "Static equipment states do not establish independently interchangeable equipment layers.",
                         "Motion PNGs sample procedural poses; they are not recordings of user interaction."]}
    return proof, result


def run_player(base, flags, directory, name, timeout, stay_open_capture=None):
    log = directory / (name + ".log")
    env = os.environ.copy()
    env["XDG_CONFIG_HOME"] = str(directory / "local-config")
    with (directory / (name + "-boot.log")).open("w") as boot:
        process = subprocess.Popen(base + flags + ["-logFile", str(log)], cwd=ROOT, env=env,
                                   stdout=boot, stderr=subprocess.STDOUT)
        try:
            if stay_open_capture is None:
                code = process.wait(timeout=timeout)
                require(code == 0, f"{name}: player exited {code}; see {log}")
            else:
                deadline = time.monotonic() + min(timeout, 60)
                while not stay_open_capture.exists() and process.poll() is None and time.monotonic() < deadline:
                    time.sleep(.2)
                require(stay_open_capture.exists(), f"{name}: screenshot missing; see {log}")
                time.sleep(3)
                require(process.poll() is None, f"{name}: interactive studio exited after capture")
            text = log.read_text(errors="replace")
            for marker in ERROR_MARKERS:
                require(marker not in text, f"{name}: {marker}; see {log}")
        finally:
            if process.poll() is None:
                # Terminate only this verifier's own player, never another user session.
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)


def encode_videos(proof, directory):
    require(shutil.which("ffmpeg"), "ffmpeg is missing; use --no-videos for PNG-only evidence")
    videos = []
    for sequence in proof["motion_sequences"]:
        pattern = sequence.get("png_pattern", "")
        require(re.fullmatch(r"[^%]*%0?\d*d\.png", pattern), f"Unsupported frame pattern: {pattern}")
        inside(directory, pattern % 0)
        destination = directory / (sequence["id"] + ".mp4")
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-n", "-framerate", "12",
                        "-start_number", "0", "-i", str(directory / pattern), "-frames:v", "24",
                        "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p",
                        "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-movflags", "+faststart", str(destination)],
                       check=True, timeout=60)
        videos.append({"path": destination.name, "sha256": hashlib.sha256(destination.read_bytes()).hexdigest(),
                       "description": "2 seconds of deterministic runtime pose samples at 12 fps"})
    return videos


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path, default=ROOT / "app/Builds/Linux/SoloGym.x86_64")
    parser.add_argument("--output", type=Path, help="Fresh run directory, or existing directory with --verify-only.")
    parser.add_argument("--timeout", type=float, default=480)
    parser.add_argument("--no-videos", action="store_true")
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    if args.verify_only and args.output is None:
        parser.error("--verify-only requires --output")
    output = (args.output or ROOT / "artifacts/visual/IllustratedCharacterProof" /
              datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")).resolve()
    try:
        if not args.verify_only:
            require(os.environ.get("DISPLAY"), "No graphical DISPLAY; run this verifier with xvfb-run")
            require(args.player.is_file(), f"Unity player missing: {args.player}")
            require(not output.exists() or not any(output.iterdir()),
                    f"Use a fresh output directory to avoid stale evidence: {output}")
            output.mkdir(parents=True, exist_ok=True)
            base = [str(args.player.resolve()), "-screen-fullscreen", "0", "-screen-width", "853",
                    "-screen-height", "1844", "-sologym-review", "-sologym-window", "character",
                    "-sologym-avatar-renderer", "illustrated", "-sologym-locale", "es"]
            run_player(base, ["-sologym-illustrated-proof", str(output)], output, "illustrated-proof", args.timeout)
        proof, result = verify(output)
        if not args.verify_only:
            interactive = output / "interactive-studio.png"
            run_player(base, ["-sologym-capture", str(interactive)], output, "interactive-studio", args.timeout,
                       stay_open_capture=interactive)
            png_info(interactive)
            result["interactive_capture_stays_open"] = True
            result["videos"] = [] if args.no_videos else encode_videos(proof, output)
            (output / "verification.json").write_text(json.dumps(result, indent=2) + "\n")
        print(f"Illustrated proof structure verified: 16 static states and 24×24 motion frames (base + equipped). Output: {output}")
        print("Visual review remains necessary; no appearance, mask quality or runtime release approval is inferred.")
        return 0
    except (OSError, ValueError, KeyError, TypeError, RuntimeError, subprocess.SubprocessError) as error:
        print(f"Illustrated proof verification FAILED: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
