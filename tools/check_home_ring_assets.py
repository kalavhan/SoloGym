#!/usr/bin/env python3
"""Read-only ring export/provenance audit; never edits artwork."""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "assets/sprites/autosprite/home-ring-r1"


def main():
    manifest = json.loads((PACK / "provenance.json").read_text())
    for source in manifest["files"]:
        data = (PACK / source["path"]).read_bytes()
        assert hashlib.sha256(data).hexdigest() == source["sha256"]
    runtime = ROOT / manifest["runtime"]
    assert runtime.read_bytes() == (PACK / "transparent.png").read_bytes()
    original = Image.open(PACK / "original.png").convert("RGB")
    sprite = Image.open(runtime)
    assert sprite.mode == "RGBA" and sprite.size == original.size == (1536, 1024)
    assert ImageChops.difference(original, sprite.convert("RGB")).getbbox() is None, "Background removal changed source RGB"
    # Read actual gaps between the ropes, not just the exterior corners.
    for point in [(0, 0), (700, 300), (720, 380), (220, 460), (1535, 750)]:
        assert sprite.getpixel(point)[3] == 0, (point, "Expected transparent air")
    for point in [(800, 600), (1490, 560), (250, 500)]:
        assert sprite.getpixel(point)[3] >= 250, (point, "Missing solid mat/post/rope")
    meta = Path(str(runtime) + ".meta").read_text()
    for setting in ("textureType: 8", "filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
        assert setting in meta, setting
    assert "overridden: 1" not in meta
    definition = json.loads((runtime.parent / "item.json").read_text())
    assert definition["sourceSize"] == dict(x=sprite.width, y=sprite.height)
    assert len(definition["variants"]) == 1 and definition["variants"][0]["id"] == "base"
    prompt = json.loads((ROOT / "design/fantasy-home-ring-r1/prompts.json").read_text())
    assert len(prompt["autospriteShortDescription"]) <= 200
    print("PASS: hashes, unchanged source RGB, exact runtime export, rope-gap alpha, solid prop, sprite imports and short handoff description")


if __name__ == "__main__":
    main()
