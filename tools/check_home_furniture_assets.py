#!/usr/bin/env python3
"""Read-only preservation and transparency audit for a four-sprite Home prop batch."""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    packs = {"r1": "home-furniture-r1", "r2": "home-furniture-r2",
             "decor-r1": "home-decor-r1", "decor-r2": "home-decor-r2"}
    parser.add_argument("--batch", choices=tuple(packs), default="r1")
    args = parser.parse_args()
    pack_name = packs[args.batch]
    pack = ROOT / "assets/sprites/autosprite" / pack_name
    manifest = json.loads((pack / "provenance.json").read_text())
    prompts = json.loads((ROOT / "design" / ("fantasy-" + pack_name) / "prompts.json").read_text())
    air = {"rack": [(700, 470), (590, 530), (500, 800)], "bench": [(700, 800)],
           "bed": [(900, 920)], "chest": [(200, 500)],
           "desk": [(750, 700)], "stool": [(600, 550)], "shelf": [(768, 500)], "rug": [(200, 200)],
           "lantern": [(500, 325), (200, 1000)], "torch": [(475, 900), (700, 100)],
           "banner": [(512, 310), (200, 1000)], "plant": [(800, 1100), (200, 1000)],
           "trophy": [(150, 600), (880, 600)], "book": [(500, 450), (500, 1200)],
           "bottle": [(750, 350), (200, 1000)], "towel": [(800, 1000), (100, 800)]}
    solid = {"rack": [(100, 500), (400, 265), (760, 290)], "bench": [(700, 600), (200, 600)],
             "bed": [(700, 500)], "chest": [(700, 500)], "desk": [(700, 350)],
             "stool": [(700, 350)], "shelf": [(700, 350), (700, 700)], "rug": [(768, 600)],
             "lantern": [(512, 768), (512, 1300)], "torch": [(512, 768), (300, 800)],
             "banner": [(512, 768)], "plant": [(512, 1300), (500, 325)],
             "trophy": [(512, 700), (512, 1300)], "book": [(500, 750)],
             "bottle": [(512, 900)], "towel": [(500, 800), (500, 500)]}
    guids = set()
    for item in manifest["sprites"]:
        key = item["key"]
        for source in item["files"]:
            assert hashlib.sha256((pack / source["path"]).read_bytes()).hexdigest() == source["sha256"]
        runtime = ROOT / item["runtime"]
        assert runtime.read_bytes() == (pack / item["export"]).read_bytes() == (pack / item["original"]).read_bytes()
        im = Image.open(runtime)
        assert im.mode == "RGBA" and im.size == (item["size"]["width"], item["size"]["height"])
        assert im.getchannel("A").histogram()[0] > im.width * im.height * .1
        for point in [(0, 0), (im.width - 1, im.height - 1)] + air[key]:
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
