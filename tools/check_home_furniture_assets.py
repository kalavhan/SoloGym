#!/usr/bin/env python3
"""Read-only preservation and transparency audit for the four Home furniture sprites."""
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "assets/sprites/autosprite/home-furniture-r1"


def main():
    manifest = json.loads((PACK / "provenance.json").read_text())
    prompts = json.loads((ROOT / "design/fantasy-home-furniture-r1/prompts.json").read_text())
    air = {"rack": [(700, 470), (590, 530), (500, 800)], "bench": [(700, 800)],
           "bed": [(900, 920)], "chest": [(200, 500)]}
    solid = {"rack": [(100, 500), (400, 265), (760, 290)], "bench": [(700, 600), (200, 600)],
             "bed": [(700, 500)], "chest": [(700, 500)]}
    guids = set()
    for item in manifest["sprites"]:
        key = item["key"]
        for source in item["files"]:
            assert hashlib.sha256((PACK / source["path"]).read_bytes()).hexdigest() == source["sha256"]
        runtime = ROOT / item["runtime"]
        assert runtime.read_bytes() == (PACK / item["export"]).read_bytes() == (PACK / item["original"]).read_bytes()
        im = Image.open(runtime)
        assert im.mode == "RGBA" and im.size == (1536, 1024)
        assert im.getchannel("A").histogram()[0] > im.width * im.height * .1
        for point in [(0, 0), (1535, 1023)] + air[key]:
            assert im.getpixel(point)[3] < 10, (key, point, "Expected transparent air")
        for point in solid[key]:
            assert im.getpixel(point)[3] >= 250, (key, point, "Missing solid artwork")
        meta = Path(str(runtime) + ".meta").read_text()
        for setting in ("textureType: 8", "filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
            assert setting in meta, (key, setting)
        assert "overridden: 1" not in meta
        guid = next(line for line in meta.splitlines() if line.startswith("guid: "))
        assert guid not in guids
        guids.add(guid)
        definition = json.loads((runtime.parent / "item.json").read_text())
        assert definition["sourceSize"] == dict(x=im.width, y=im.height)
        assert len(prompts["shortDescriptions"][key]) <= 200
        print(f"PASS {key}: hashes, original/export/runtime byte identity, alpha gaps, solid artwork, sprite import and short handoff")


if __name__ == "__main__":
    main()
