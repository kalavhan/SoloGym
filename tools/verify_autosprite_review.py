#!/usr/bin/env python3
"""Run under xvfb-run or a real display after building the Linux player."""
import json
import os
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "artifacts/local/autosprite-review"
BASE = [str(ROOT / "app/Builds/Linux/SoloGym.x86_64"), "-screen-fullscreen", "0",
        "-screen-width", "853", "-screen-height", "1844", "-sologym-review"]


def run(name, args, interactive=False):
    capture = OUT / f"{name}.png"
    log = Path("/tmp") / f"sologym-autosprite-{name}.log"
    capture.unlink(missing_ok=True)
    command = BASE + args + ["-sologym-capture", str(capture), "-logFile", str(log)]
    with open(os.devnull, "w") as sink:
        process = subprocess.Popen(command, cwd=ROOT, stdout=sink, stderr=sink)
        try:
            if interactive:
                deadline = time.monotonic() + 40
                while not capture.exists() and process.poll() is None and time.monotonic() < deadline:
                    time.sleep(.2)
                assert capture.exists(), f"{name}: no screenshot, see {log}"
                time.sleep(3)
                assert process.poll() is None, f"{name}: interactive capture closed the window"
            else:
                assert process.wait(timeout=45) == 0, f"{name}: player failed, see {log}"
                assert capture.exists(), f"{name}: no screenshot"
                smoke = json.loads(capture.with_suffix(".smoke.json").read_text())
                assert smoke["passed"], f"{name}: smoke failed"
            text = log.read_text()
            for error in ("NullReferenceException", "ArgumentException", "InvalidOperationException", "SOLOGYM_AUTOSPRITE_LOAD", "Shader error"):
                assert error not in text, f"{name}: {error}, see {log}"
            return dict(passed=True, capture=str(capture.relative_to(ROOT)), staysOpen=interactive)
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    cases = [
        ("studio-en-interactive", ["-sologym-window", "character", "-sologym-locale", "en", "-sologym-avatar-renderer", "autosprite"], True),
        ("home-en-interactive", ["-sologym-window", "home", "-sologym-locale", "en", "-sologym-avatar-renderer", "autosprite"], True),
        ("legacy-studio", ["-sologym-window", "character", "-sologym-locale", "es", "-sologym-smoke"], False),
        ("legacy-home", ["-sologym-window", "home", "-sologym-locale", "es", "-sologym-smoke"], False),
    ]
    results = {}
    for name, args, interactive in cases:
        results[name] = run(name, args, interactive)
        print(name + ": passed", flush=True)
    (OUT / "review-verification.json").write_text(json.dumps(results, indent=2) + "\n")


if __name__ == "__main__":
    main()
