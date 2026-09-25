#!/usr/bin/env python3
"""Compare a full-screen reference with an actual capture without resizing.

Requires Pillow. Produces descriptive metrics and review images, never a visual
acceptance verdict. Supply an actual application capture, not reference-mode
output or a second copy of the proposal.

Example:
    python3 tools/compare_home.py --reference reference.png \
        --actual unity-capture.png --output-prefix review/home-en
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


def source_info(path: Path, image: Image.Image) -> dict:
    return {
        "path": str(path.resolve()),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "width": image.width,
        "height": image.height,
        "mode": image.mode,
    }


def compare(reference_path: Path, actual_path: Path, output_prefix: Path) -> dict:
    if reference_path.resolve() == actual_path.resolve():
        raise ValueError("Reference and actual must be different files; self-comparison is not a capture review.")

    with Image.open(reference_path) as reference_source, Image.open(actual_path) as actual_source:
        reference_source.load()
        actual_source.load()
        if reference_source.size != actual_source.size:
            raise ValueError(
                f"Exact dimensions required: reference {reference_source.size}, "
                f"actual {actual_source.size}. No rescaling or cropping is performed."
            )
        reference_info = source_info(reference_path, reference_source)
        actual_info = source_info(actual_path, actual_source)
        reference = reference_source.convert("RGB")
        actual = actual_source.convert("RGB")

    width, height = reference.size
    pixel_count = width * height
    difference = ImageChops.difference(reference, actual)
    channel_images = difference.split()
    absolute_error_sum = sum(
        value * count
        for channel in channel_images
        for value, count in enumerate(channel.histogram())
    )
    max_channel = ImageChops.lighter(ImageChops.lighter(channel_images[0], channel_images[1]), channel_images[2])
    max_channel_histogram = max_channel.histogram()
    max_difference = max(channel.getextrema()[1] for channel in channel_images)
    within_eight = sum(max_channel_histogram[:9])

    suffixes = {
        "side_by_side": ".side-by-side.png",
        "overlay": ".overlay.png",
        "difference": ".difference-amplified.png",
        "metrics": ".metrics.json",
    }
    outputs = {key: Path(str(output_prefix) + suffix) for key, suffix in suffixes.items()}
    input_paths = {reference_path.resolve(), actual_path.resolve()}
    if any(path.resolve() in input_paths for path in outputs.values()):
        raise ValueError("Output filenames must not overwrite either source image.")
    output_prefix.parent.mkdir(parents=True, exist_ok=True)

    header_height = 32
    side_by_side = Image.new("RGB", (width * 2, height + header_height), (15, 18, 24))
    side_by_side.paste(reference, (0, header_height))
    side_by_side.paste(actual, (width, header_height))
    labels = ImageDraw.Draw(side_by_side)
    labels.text((12, 10), "REFERENCE", fill=(255, 255, 255))
    labels.text((width + 12, 10), "ACTUAL CAPTURE", fill=(255, 255, 255))
    side_by_side.save(outputs["side_by_side"])

    Image.blend(reference, actual, 0.5).save(outputs["overlay"])
    amplification = 4
    difference.point([min(255, value * amplification) for value in range(256)] * 3).save(outputs["difference"])

    report = {
        "reference": reference_info,
        "actual": actual_info,
        "dimensions_match": True,
        "metrics": {
            "MAE_RGB_0_255": absolute_error_sum / (pixel_count * 3),
            "maxdiff": max_difference,
            "percentage_pixels_maxchannel_diff_lte_8": within_eight * 100 / pixel_count,
        },
        "interpretation": {
            "descriptive_only": True,
            "acceptance": "No automatic 1:1 pass or failure is assigned. Inspect the complete images and the running UI.",
            "provenance": "The caller must supply an actual application capture. This tool cannot detect reference-mode output or prove runtime provenance.",
            "identical_source_bytes": reference_info["sha256"] == actual_info["sha256"],
            "alignment": "Stored pixel coordinates compared directly; no resizing, cropping, registration, EXIF orientation or color-profile transformation.",
            "channels": "RGB only; inputs converted to RGB. Alpha and other non-RGB channels are not scored.",
            "tolerance_metric": "The percentage within 8 counts pixels whose largest absolute RGB-channel difference is at most 8. It is not an acceptance threshold.",
        },
        "review_images": {
            "side_by_side": "Reference on the left, actual on the right; 32-pixel label strip added above unscaled sources.",
            "overlay": "Equal 50% reference and 50% actual at original dimensions.",
            "difference": f"Absolute RGB difference multiplied by {amplification}, clipped to 255; metrics use the unamplified difference.",
        },
        "outputs": {key: str(path.resolve()) for key, path in outputs.items()},
    }
    outputs["metrics"].write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    return report


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--reference", type=Path, required=True, help="Approved full-screen reference image.")
    parser.add_argument("--actual", type=Path, required=True, help="Actual full-screen application capture with identical dimensions.")
    parser.add_argument("--output-prefix", type=Path, required=True, help="Output directory and filename prefix, without an extension.")
    args = parser.parse_args()
    try:
        report = compare(args.reference, args.actual, args.output_prefix)
    except (OSError, ValueError) as error:
        parser.error(str(error))
    print(json.dumps({"metrics": report["metrics"], "outputs": report["outputs"], "acceptance": "visual review required"}, indent=2))


if __name__ == "__main__":
    main()
