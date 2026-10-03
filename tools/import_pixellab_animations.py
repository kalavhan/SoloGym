#!/usr/bin/env python3
"""Download the approved PixelLab animation frames into the Unity Resources folder.

Run it on a computer that can reach PixelLab (the cloud workspace cannot):

    python3 tools/import_pixellab_animations.py            # everything listed in the manifest
    python3 tools/import_pixellab_animations.py --only slugvex
    python3 tools/import_pixellab_animations.py --dry-run  # print the URLs only
    python3 tools/import_pixellab_animations.py --force    # re-download existing frames

Frames land as 0.png .. 8.png under app/Assets/SoloGym/Resources/Game/<target>/, which is
what PixelSpriteLoop expects. Unity then imports them as crisp sprites on its own
(Editor/PixelGameSpriteImporter). Add new animations to tools/pixellab_animations.json.
Uses only the Python standard library.
"""
import argparse, json, struct, sys, time, urllib.error, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools" / "pixellab_animations.json"
DEST = ROOT / "app" / "Assets" / "SoloGym" / "Resources" / "Game"
PNG = b"\x89PNG\r\n\x1a\n"


def fetch(url, tries=3):
    last = None
    for attempt in range(tries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "SoloGym-importer/1.0"})
            with urllib.request.urlopen(req, timeout=30) as r:
                data = r.read()
            if not data.startswith(PNG):
                raise ValueError("not a PNG")
            return data
        except urllib.error.HTTPError as e:
            if e.code in (401, 403, 404):  # will not get better by retrying
                raise
            last = e
        except Exception as e:  # network blip: retry
            last = e
        time.sleep(1 + attempt)
    raise last


def size(png):
    return struct.unpack(">II", png[16:24])


def jobs(manifest):
    base, n = manifest["base_url"].rstrip("/"), manifest["frames"]
    for a in manifest["animations"]:
        urls = [f"{base}/{a['character']}/animations/{a['animation']}/{a['direction']}/{i}.png" for i in range(n)]
        yield a["target"], a["label"], [(f"{i}.png", u) for i, u in enumerate(urls)]
    for s in manifest.get("stills", []):
        yield s["target"], s["label"], [(Path(s["target"]).name, f"{base}/{s['character']}/rotations/{s['rotation']}.png")]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--only", help="only targets whose path or label contains this text")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--force", action="store_true", help="overwrite files that already exist")
    ap.add_argument("--dest", default=str(DEST), help="Resources/Game folder (default: the Unity project)")
    args = ap.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    dest, failed, saved, skipped = Path(args.dest), [], 0, 0
    for target, label, files in jobs(manifest):
        if args.only and args.only.lower() not in (target + " " + label).lower():
            continue
        is_still = target.endswith(".png")
        folder = (dest / target).parent if is_still else dest / target
        print(f"{label} -> {target}")
        for name, url in files:
            out = folder / name
            if args.dry_run:
                print(f"  {url}")
                continue
            if out.exists() and not args.force:
                skipped += 1
                continue
            try:
                data = fetch(url)
            except Exception as e:
                failed.append((label, name, url, e))
                print(f"  FAILED {name}: {e}")
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            out.write_bytes(data)
            saved += 1
        if not args.dry_run:
            w = [size((folder / n).read_bytes()) for n, _ in files if (folder / n).exists()]
            if w and len(set(w)) > 1:
                print(f"  WARNING: frames differ in size {sorted(set(w))}")
    if args.dry_run:
        return 0
    print(f"\nsaved {saved}, already present {skipped}, failed {len(failed)}")
    if failed:
        print("\nCould not download these (the link may have changed or PixelLab is blocked here).")
        print("Fallback: in PixelLab use the character's download button, unzip, and copy the frames in by hand.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
