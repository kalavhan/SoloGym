#!/usr/bin/env python3
"""Check that toggling a prop changes only its visible image region in real player captures."""
import json
import math
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
CAPTURES = ROOT / "artifacts/visual/HomeRing"


def main():
    definition = json.loads((ROOT / "app/Assets/SoloGym/Resources/Rooms/Props/TrainingRingR1/item.json").read_text())
    room = json.loads((ROOT / "app/Assets/SoloGym/Resources/Rooms/RefugeR1/room.json").read_text())
    slot = next(a["point"] for a in room["anchors"] if a["id"] == definition["supportedSlots"][0])
    alpha = Image.open(ROOT / "assets/sprites/autosprite/home-ring-r1/transparent.png").getchannel("A")
    source_bounds = alpha.point(lambda value: 255 if value > 10 else 0).getbbox()
    art_scale = definition["sourceScale"]
    left = slot["x"] - definition["pivot"]["x"] * definition["sourceSize"]["x"] * art_scale
    top = slot["y"] - (1 - definition["pivot"]["y"]) * definition["sourceSize"]["y"] * art_scale
    report = []
    for width, height, inset, locale in [(1280, 720, 0, "es"), (854, 480, 24, "es"), (1844, 853, 64, "en")]:
        name = f"{width}x{height}-{locale}"
        shown = Image.open(CAPTURES / (name + ".png")).convert("RGB")
        hidden = Image.open(CAPTURES / (name + "-ring-hidden.png")).convert("RGB")
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
        assert actual is not None, name + ": ring did not appear"
        assert actual[0] >= expected[0] and actual[1] >= expected[1] and actual[2] <= expected[2] and actual[3] <= expected[3], (name, actual, expected)
        report.append(dict(capture=name,changedRegion=actual,allowedRegion=expected,channelTolerance=10))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
