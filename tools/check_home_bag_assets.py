#!/usr/bin/env python3
"""Read-only audit of the hanging bag export; no artwork edits."""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "assets/sprites/autosprite/home-bag-r1"


def main():
    manifest = json.loads((PACK / "provenance.json").read_text())
    for source in manifest["files"]:
        assert hashlib.sha256((PACK / source["path"]).read_bytes()).hexdigest() == source["sha256"]
    runtime = ROOT / manifest["runtime"]
    assert runtime.read_bytes() == (PACK / "transparent.png").read_bytes()
    original = Image.open(PACK / "original.png").convert("RGB")
    sprite = Image.open(runtime)
    assert sprite.mode == "RGBA" and sprite.size == original.size == (1024, 1536)
    assert ImageChops.difference(original, sprite.convert("RGB")).getbbox() is None
    # Include open air inside the suspension assembly, not only canvas corners.
    for point in [(0, 0), (400, 300), (470, 400), (560, 400), (800, 800)]:
        assert sprite.getpixel(point)[3] < 10, (point, "Expected transparent air")
    for point in [(512, 30), (490, 140), (350, 570), (512, 800), (512, 1450)]:
        assert sprite.getpixel(point)[3] >= 250, (point, "Missing hook, band or leather")
    meta = Path(str(runtime) + ".meta").read_text()
    for setting in ("textureType: 8", "filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
        assert setting in meta, setting
    assert "overridden: 1" not in meta
    definition = json.loads((runtime.parent / "item.json").read_text())
    assert definition["sourceSize"] == dict(x=sprite.width, y=sprite.height)
    prompt = json.loads((ROOT / "design/fantasy-home-bag-r1/prompts.json").read_text())
    assert len(prompt["autospriteShortDescription"]) <= 200
    print("PASS: bag source hashes, RGB preservation, exact runtime export, suspension-gap alpha, solid artwork and sprite imports")


if __name__ == "__main__":
    main()
