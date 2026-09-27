#!/usr/bin/env python3
"""Read-only integrity check for approved static bases before equipment fitting."""
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
BATCH = ROOT / "assets/sprites/autosprite/body-bases-front-r1"
EXPECTED = {f"{gender}-{body}" for gender in ("male", "female")
            for body in ("slim", "medium", "overweight", "obese", "muscular")}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def batch_file(relative):
    path = (BATCH / relative).resolve()
    assert path.is_relative_to(BATCH.resolve()), "File outside the batch"
    return path


def main():
    manifest = json.loads((BATCH / "manifest.json").read_text())
    assert manifest["approval"] == "approved"
    assert manifest["view"] == "front" and manifest["animated"] is False
    variants = manifest["variants"]
    assert len(variants) == 10 and {v["id"] for v in variants} == EXPECTED
    record = manifest["approvalRecord"]
    approval_path = batch_file(record["path"])
    assert digest(approval_path) == record["sha256"], "Approval record changed"
    review = json.loads(approval_path.read_text())
    assert review["schema"] == "sologym.body-base-review.v1"
    assert review["batch"] == BATCH.name and len(review["variants"]) == 10
    approvals = {a["id"]: a for a in review["variants"]}
    assert set(approvals) == EXPECTED
    for v in variants:
        label = v["id"]
        source, export, approval = v["image"], v["transparentExport"], approvals[label]
        assert v["sourceArtworkFrozen"] and v["canonicalBaseFrozen"], label
        assert v["approval"] == approval["decision"] == "approved", label
        assert approval["gender"] == v["gender"] and approval["bodyType"] == v["bodyType"], label
        assert approval["image"] == source["path"], label
        assert digest(batch_file(source["path"])) == source["sha256"] == approval["sha256"], label
        assert digest(batch_file(export["path"])) == export["sha256"], label
        assert export["sourceSha256"] == source["sha256"], label
        assert not export["trimmed"] and not export["resized"], label
        assert export["coordinateOrigin"] == "top-left", label
        with Image.open(batch_file(source["path"])) as original, Image.open(batch_file(export["path"])) as image:
            assert image.size == original.size == (1024, 1024), label
            assert (export["width"], export["height"]) == image.size, label
            assert image.mode == "RGBA", label
            alpha = image.getchannel("A")
            assert alpha.getextrema() == (0, 255), label
            assert list(alpha.getbbox()) == export["contentBounds"], label
            # Ignore extremely faint alpha residue; visible body must have safe margins.
            visible = alpha.point(lambda a: 255 if a >= 128 else 0).getbbox()
            assert visible and min(visible[:2]) > 0 and max(visible[2:]) < 1024, label
            assert list(visible) == export["qa"]["contentBoundsAlpha128"], label
        assert export["tool"] == "remove_asset_background", label
        assert export["assetId"] == v["transparentExportAssetId"], label
    assert manifest["creditsUsed"] == manifest["generationCreditsUsed"] + manifest["backgroundRemovalCreditsUsed"]
    print("PASS: 10 approvals, unchanged originals, frozen transparent exports, canvas and alpha framing.")


if __name__ == "__main__":
    main()
