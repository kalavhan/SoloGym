#!/usr/bin/env python3
"""Capture the shared character source and studio; verify structure, never visual acceptance."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
ERRORS = (
    "NullReferenceException", "ArgumentException", "InvalidOperationException",
    "Shader error", "SOLOGYM_SOURCE_PROOF_FAILED", "SOLOGYM_SOURCE_CHARACTER_LOAD",
    "SOLOGYM_CHARACTER_CHECK_FAILED",
)


def png_info(path):
    data = path.read_bytes()
    if len(data) < 128 or data[:8] != b"\x89PNG\r\n\x1a\n":
        raise RuntimeError(f"Missing or invalid PNG: {path}")
    width, height = struct.unpack(">II", data[16:24])
    if width < 64 or height < 64:
        raise RuntimeError(f"Unexpectedly small PNG: {path} ({width}x{height})")
    return {"width": width, "height": height, "sha256": hashlib.sha256(data).hexdigest()}


def run_player(base, name, args, output, wait_for=None):
    log = output / (name + ".log")
    command = base + args + ["-logFile", str(log)]
    with open(os.devnull, "w") as sink:
        process = subprocess.Popen(command, cwd=ROOT, stdout=sink, stderr=sink)
        try:
            if wait_for is None:
                code = process.wait(timeout=120)
                if code:
                    raise RuntimeError(f"{name}: player exited {code}; see {log}")
            else:
                deadline = time.monotonic() + 45
                while not wait_for.exists() and process.poll() is None and time.monotonic() < deadline:
                    time.sleep(.2)
                if not wait_for.exists():
                    raise RuntimeError(f"{name}: capture missing; see {log}")
                time.sleep(3)
                if process.poll() is not None:
                    raise RuntimeError(f"{name}: interactive studio exited after capture; see {log}")
            text = log.read_text(errors="replace")
            for marker in ERRORS:
                if marker in text:
                    raise RuntimeError(f"{name}: {marker}; see {log}")
        finally:
            if process.poll() is None:
                # Own only this verifier's player process; do not touch an existing review session.
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)
    return {"log": str(log), "stayed_open_after_capture": wait_for is not None}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path, default=ROOT / "app/Builds/Linux/SoloGym.x86_64")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/visual/CharacterSource3D")
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    base = [str(args.player.resolve()), "-screen-fullscreen", "0", "-screen-width", "853",
            "-screen-height", "1844", "-sologym-review", "-sologym-window", "character",
            "-sologym-avatar-renderer", "source3d"]
    manifest_path = output / "source-proof.json"
    manifest_path.unlink(missing_ok=True)
    result = {"structural_passed": False, "visual_review_status": "pending", "captures": {}}
    run_player(base, "source-proof", ["-sologym-character-proof", str(output)], output)
    proof = json.loads(manifest_path.read_text())
    if proof.get("renderer") != "source3d" or not proof.get("structural_passed"):
        raise RuntimeError(f"Source proof failed: {proof.get('failures')}")
    if proof.get("visual_review_status") != "pending":
        raise RuntimeError("Automated source proof must leave visual approval pending")
    source = ROOT / "app/Assets/SoloGym/Resources/AvatarSource3D/Character.json"
    if source.exists() and hashlib.sha256(source.read_bytes()).hexdigest() != proof.get("source_sha256"):
        raise RuntimeError("Player source hash differs from the workspace source; rebuild the player")
    frames = proof.get("frames", [])
    if len(frames) != 22 or len({frame["id"] for frame in frames}) != 22:
        raise RuntimeError("Expected 22 distinct proof cases")
    for frame in frames:
        if not frame.get("structural_passed"):
            raise RuntimeError(f"Failed frame: {frame}")
        if frame.get("ground_contact_error_metres", float("inf")) > .002:
            raise RuntimeError(f"Source ground contact failed: {frame['id']}")
        result["captures"][frame["id"]] = png_info(output / frame["png"])
    pairs = [
        ("bare-front", "bare-side"), ("bare-front", "bare-back"),
        ("equipped-front", "equipped-gameplay"), ("bare-front", "equipped-front"),
        ("equipped-face", "alternate-face"), ("equipped-gameplay", "walk-gameplay"),
        ("walk-gameplay", "jab-gameplay"), ("shape-min-equipped", "shape-mid-equipped"),
        ("shape-mid-equipped", "shape-max-equipped"),
        ("bare-gameplay", "bare-walk-gameplay"), ("bare-walk-gameplay", "bare-jab-gameplay"),
        ("equipped-side", "walk-side"), ("walk-side", "jab-side"),
    ]
    for left, right in pairs:
        if result["captures"][left]["sha256"] == result["captures"][right]["sha256"]:
            raise RuntimeError(f"Expected different rendered states: {left} / {right}")
    print("source proof: 22 structural cases and distinct rendered states verified; visual review pending", flush=True)

    cases = [
        ("studio-es-source", ["-sologym-locale", "es", "-sologym-smoke"], False),
        ("studio-es-gameplay", ["-sologym-locale", "es", "-sologym-character-camera", "gameplay"], False),
        ("studio-en-face", ["-sologym-locale", "en", "-sologym-character-view", "face"], False),
        ("studio-es-alternate", ["-sologym-locale", "es", "-sologym-avatar-variant", "alternate"], False),
        ("studio-es-interactive", ["-sologym-locale", "es"], True),
    ]
    for name, flags, interactive in cases:
        image = output / (name + ".png")
        image.unlink(missing_ok=True)
        flags += ["-sologym-capture", str(image)]
        if not interactive:
            flags += ["-sologym-quit-after-capture"]
        run_player(base, name, flags, output, image if interactive else None)
        result["captures"][name] = png_info(image)
        if (result["captures"][name]["width"], result["captures"][name]["height"]) != (853, 1844):
            raise RuntimeError(f"Studio capture must be 853x1844: {image}")
        print(name + ": captured" + ("; remained open" if interactive else ""), flush=True)
    smoke = json.loads((output / "studio-es-source.smoke.json").read_text())
    if not smoke.get("passed") or smoke.get("renderer") != "source3d":
        raise RuntimeError(f"Source studio smoke failed: {smoke}")
    result["structural_passed"] = True
    result["source_sha256"] = proof["source_sha256"]
    result["studio_checks"] = smoke["checks"]
    result["interactive_capture_stays_open"] = True
    (output / "verification.json").write_text(json.dumps(result, indent=2) + "\n")
    print("Structural verification passed. Review the PNGs before accepting the character visually.", flush=True)


if __name__ == "__main__":
    main()
