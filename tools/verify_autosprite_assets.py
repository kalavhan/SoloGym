#!/usr/bin/env python3
"""Validate untouched AutoSprite exports; optionally sync PNG bytes and atlas metadata to Unity."""
import argparse
import hashlib
import json
import math
import shutil
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/sprites/autosprite/female-studio-home-r1"
UNITY = ROOT / "app/Assets/SoloGym/Resources/AvatarAutoSprite/female-studio-home-r1"
VIEWS = {"front": "front-r2-idle"}
# Keep original provider exports for a later isometric pass, outside the runtime package.
DEFERRED_VIEWS = {"isometric": "iso-idle"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def inspect(view, stem):
    png = SOURCE / f"{stem}.png"
    atlas_path = SOURCE / f"{stem}.json"
    atlas = json.loads(atlas_path.read_text())
    image = Image.open(png).convert("RGBA")
    meta = atlas["meta"]
    assert image.size == (meta["size"]["w"], meta["size"]["h"]), "Atlas/image size mismatch"
    duration = meta["duration_s"]
    assert math.isfinite(duration) and duration > 0, "Missing clip duration"
    # The provider uses a numeric-keyed object, not a list. Sort numerically, never lexically.
    raw = sorted(atlas["frames"].items(), key=lambda pair: int(pair[0]))
    assert [int(k) for k, _ in raw] == list(range(len(raw))), "Missing or duplicate frame index"
    assert len(raw) >= 2
    frames, bounds, hashes = [], [], []
    for _, f in raw:
        x, y, w, h = (f[k] for k in ("x", "y", "w", "h"))
        assert min(x, y) >= 0 and w > 0 and h > 0
        assert x + w <= image.width and y + h <= image.height
        assert (w, h) == (meta["frame_size"]["w"], meta["frame_size"]["h"])
        assert f["duration"] > 0 and math.isfinite(f["duration"])
        # Analysis only: neither pixels nor source exports are edited or re-encoded.
        frame = image.crop((x, y, x + w, y + h))
        alpha = frame.getchannel("A")
        assert alpha.getextrema() == (0, 255), "Character must have transparency and opaque content"
        box = alpha.getbbox()
        assert box and box[0] > 0 and box[1] > 0 and box[2] < w and box[3] < h, "Clipped character"
        bounds.append(box)
        hashes.append(hashlib.sha256(frame.tobytes()).hexdigest())
        frames.append(dict(x=x, y=y, width=w, height=h, weight=f["duration"]))
    union = (min(b[0] for b in bounds), min(b[1] for b in bounds), max(b[2] for b in bounds), max(b[3] for b in bounds))
    assert len(set(hashes)) > 1, "Export repeats a still image"
    clip = dict(id=view, resource=stem, textureWidth=image.width, textureHeight=image.height,
                durationSeconds=duration, bounds=dict(x=union[0], y=union[1], width=union[2]-union[0], height=union[3]-union[1]), frames=frames)
    report = dict(view=view, sheet=f"{stem}.png", atlas=f"{stem}.json", sheetSha256=digest(png),
                  atlasSha256=digest(atlas_path), frameCount=len(frames), durationSeconds=duration,
                  uniqueFrames=len(set(hashes)), contentBounds=list(union),
                  baselineRangePixels=max(b[3] for b in bounds)-min(b[3] for b in bounds))
    return clip, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sync", action="store_true", help="Copy unmodified PNGs and write normalized atlas metadata for Unity")
    args = parser.parse_args()
    clips, reports = zip(*(inspect(view, stem) for view, stem in VIEWS.items()))
    deferred_reports = [inspect(view, stem)[1] for view, stem in DEFERRED_VIEWS.items()]
    catalog = dict(schema="sologym.autosprite-atlas.v1", characterId="cmuj70eg8000910qlnap76av1", clips=clips)
    text = json.dumps(catalog, indent=2) + "\n"
    if args.sync:
        UNITY.mkdir(parents=True, exist_ok=True)
        for stem in VIEWS.values():
            shutil.copyfile(SOURCE / f"{stem}.png", UNITY / f"{stem}.png")
        for stem in DEFERRED_VIEWS.values():
            runtime_png = UNITY / f"{stem}.png"
            if runtime_png.exists():
                assert digest(runtime_png) == digest(SOURCE / f"{stem}.png"), "Deferred Unity image has changed; preserve it before removing"
                runtime_png.unlink()
                runtime_png.with_suffix(".png.meta").unlink(missing_ok=True)
        (UNITY / "catalog.json").write_text(text)
    else:
        assert (UNITY / "catalog.json").read_text() == text, "Runtime metadata differs from source atlases; run --sync"
        for stem in VIEWS.values():
            assert digest(SOURCE / f"{stem}.png") == digest(UNITY / f"{stem}.png"), "Unity copy differs from provider export"
    for stem in DEFERRED_VIEWS.values():
        assert not (UNITY / f"{stem}.png").exists(), "Deferred view is still packaged for runtime; run --sync"
        assert not (UNITY / f"{stem}.png.meta").exists(), "Deferred Unity texture metadata is still present"
    report = dict(passed=True, characterId=catalog["characterId"], scope="front-only", views=reports,
                  deferredViews=deferred_reports,
                  timing="meta.duration_s is clip length; frame.duration values are relative weights",
                  framing="One union of alpha bounds for the whole clip; no per-frame recentering",
                  artApproval="pending user review")
    (SOURCE / "validation.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
