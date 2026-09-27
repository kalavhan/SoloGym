#!/usr/bin/env python3
"""Capture whole character states and verify interactive capture stays alive after saving."""
import json
import os
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/visual/WIN-013'
PLAYER = ROOT / 'app/Builds/Linux/SoloGym.x86_64'
BASE = [str(PLAYER), '-screen-fullscreen', '0', '-screen-width', '853', '-screen-height', '1844', '-sologym-review']


def run(name, args, interactive=False):
    image = (Path('/tmp') if interactive else OUT) / (name + '.png')
    log = Path('/tmp') / ('sologym-' + name + '.log')
    image.unlink(missing_ok=True)
    command = BASE + args + ['-sologym-capture', str(image), '-logFile', str(log)]
    with open(os.devnull, 'w') as sink:
        process = subprocess.Popen(command, cwd=ROOT, stdout=sink, stderr=sink)
        try:
            if interactive:
                deadline = time.monotonic() + 30
                while not image.exists() and process.poll() is None and time.monotonic() < deadline:
                    time.sleep(.2)
                if not image.exists():
                    raise RuntimeError(f'{name}: capture missing; see {log}')
                time.sleep(3)
                if process.poll() is not None:
                    raise RuntimeError(f'{name}: player exited after interactive capture')
                result = {'capture_written': True, 'stayed_open_after_capture': True}
            else:
                code = process.wait(timeout=45)
                if code or not image.exists():
                    raise RuntimeError(f'{name}: exit {code}; see {log}')
                result = {'capture_written': True, 'exit_code': code}
            text = log.read_text()
            for marker in ['NullReferenceException', 'ArgumentException', 'InvalidOperationException', 'Shader error']:
                if marker in text:
                    raise RuntimeError(f'{name}: {marker}; see {log}')
            return result
        finally:
            if process.poll() is None:
                # Only stop the exact player launched by this check; never an existing user review session.
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    cases = [
        ('studio-es-registration-v5', ['-sologym-window', 'character', '-sologym-locale', 'es', '-sologym-smoke'], False),
        ('studio-en-alternate-v5', ['-sologym-window', 'character', '-sologym-locale', 'en', '-sologym-avatar-variant', 'alternate', '-sologym-quit-after-capture'], False),
        ('studio-es-face-v5', ['-sologym-window', 'character', '-sologym-locale', 'es', '-sologym-character-view', 'face', '-sologym-quit-after-capture'], False),
        ('avatar-jab-v5', ['-sologym-window', 'avatar', '-sologym-avatar-action', 'jab'], False),
        ('studio-es-interactive-v5', ['-sologym-window', 'character', '-sologym-locale', 'es'], True),
    ]
    results = {}
    for name, args, interactive in cases:
        results[name] = run(name, args, interactive)
        print(name + ': passed', flush=True)
    (OUT / 'registration-v5-verification.json').write_text(json.dumps(results, indent=2) + '\n')


if __name__ == '__main__':
    main()
