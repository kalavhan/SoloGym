#!/usr/bin/env python3
"""Check that toggling a prop changes only its visible image region in real player captures."""
import argparse
import json
import math
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    resources = {"ring": "TrainingRingR1", "bag": "HangingBagR1", "rack": "WeightRackR1",
                 "bench": "TrainingBenchR1", "bed": "BedR1", "chest": "StorageChestR1",
                 "desk": "WritingDeskR1", "stool": "StoolR1", "shelf": "WallShelfR1", "rug": "FloorRugR1",
                 "lantern": "HangingLanternR1", "torch": "WallTorchR1", "banner": "BannerR1", "plant": "PottedPlantR1",
                 "trophy": "TrophyR1", "book": "OpenBookR1", "bottle": "TrainingBottleR1", "towel": "TrainingTowelR1"}
    parser.add_argument("--prop", choices=tuple(resources), default="ring")
    parser.add_argument("--captures", type=Path, default=ROOT / "artifacts/visual/HomeRing")
    args = parser.parse_args()
    resource = resources[args.prop]
    definition = json.loads((ROOT / f"app/Assets/SoloGym/Resources/Rooms/Props/{resource}/item.json").read_text())
    room = json.loads((ROOT / "app/Assets/SoloGym/Resources/Rooms/RefugeR1/room.json").read_text())
    slot = next(a["point"] for a in room["anchors"] if a["id"] == definition["supportedSlots"][0])
    alpha = Image.open(ROOT / f"app/Assets/SoloGym/Resources/Rooms/Props/{resource}/Base.png").getchannel("A")
    source_bounds = alpha.point(lambda value: 255 if value > 10 else 0).getbbox()
    art_scale = definition["sourceScale"]
    left = slot["x"] - definition["pivot"]["x"] * definition["sourceSize"]["x"] * art_scale
    top = slot["y"] - (1 - definition["pivot"]["y"]) * definition["sourceSize"]["y"] * art_scale
    report = []
    for width, height, inset, locale in [(1280, 720, 0, "es"), (854, 480, 24, "es"), (1844, 853, 64, "en")]:
        name = f"{width}x{height}-{locale}"
        shown = Image.open(args.captures / (name + ".png")).convert("RGB")
        hidden = Image.open(args.captures / (name + f"-{args.prop}-hidden.png")).convert("RGB")
        assert shown.size == hidden.size == (width, height)
        scale = min((width - 2 * inset) / 1672, (height - 2 * inset) / 941)
        origin_x, origin_y = (width - 1672 * scale) / 2, (height - 941 * scale) / 2
        expected = (math.floor(origin_x + (left + source_bounds[0] * art_scale) * scale) - 2,
                    math.floor(origin_y + (top + source_bounds[1] * art_scale) * scale) - 2,
                    math.ceil(origin_x + (left + source_bounds[2] * art_scale) * scale) + 2,
                    math.ceil(origin_y + (top + source_bounds[3] * art_scale) * scale) + 2)
        channels = ImageChops.difference(shown, hidden).split()
        maximum = ImageChops.lighter(ImageChops.lighter(channels[0], channels[1]), channels[2])
        actual = maximum.point(lambda value: 255 if value > 10 else 0).getbbox()
        assert actual is not None, name + f": {args.prop} did not appear"
        assert actual[0] >= expected[0] and actual[1] >= expected[1] and actual[2] <= expected[2] and actual[3] <= expected[3], (name, actual, expected)
        report.append(dict(capture=name,changedRegion=actual,allowedRegion=expected,channelTolerance=10))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
