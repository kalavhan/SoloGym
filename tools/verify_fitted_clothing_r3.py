#!/usr/bin/env python3
"""Verify fitted equipment provenance and preservation of approved bodies/hair."""
import re

from verify_modular_character_assets import (
    ROOT, SPRITES, PRESETS, clothing_payload, digest, fit_payload_hash,
    image_record, placement, read,
)


def main():
    folder = SPRITES / "basic-clothing-fit-r3"
    manifest, data = read(folder / "manifest.json"), read(folder / "fitting.json")
    assert data["id"] == "modular-front-r3"
    assert not data["animated"] and not data["baseMutationAllowed"]
    assert manifest["fullCharacterGenerations"] == manifest["characterUploads"] == 0
    assert manifest["bodyImagesChanged"] == manifest["hairFitsChanged"] == 0
    adoption = read(folder / manifest["mvpAdoption"])
    assert manifest["runtimeIntegrated"] and adoption["status"] == "accepted_for_mvp_with_known_issues"
    assert set(adoption["activeBodyIds"]) == PRESETS - {"female-obese", "male-obese"}
    assert not adoption["qualityApproval"] and adoption["additionalGenerationCredits"] == 0
    assert digest(folder / "fitting.json") == adoption["sourceFittingSha256"]

    base_manifest = read(SPRITES / "body-bases-hairfree-r1/manifest.json")
    lock_ref = base_manifest["approvedBaseHairLock"]
    assert digest(ROOT / lock_ref["path"]) == lock_ref["sha256"]
    lock = read(ROOT / lock_ref["path"])
    frozen = {b["id"]: b for b in lock["bodies"]}
    before = read(ROOT / lock["reviewedFitting"]["path"])
    old_bodies = {b["id"]: b for b in before["bodies"]}
    items = {i["id"]: i for i in data["items"]}
    assert len([i for i in items.values() if i.get("newInRevision")]) == 5
    for asset in [*items.values(), *data["hairstyles"]]:
        image_record(asset["image"])
    assert [{"id": h["id"], "image": h["image"]} for h in data["hairstyles"]] == lock["hairstyles"]
    assert len(data["bodies"]) == 10 and {b["id"] for b in data["bodies"]} == PRESETS
    changed = set()
    for body in data["bodies"]:
        bid, fit = body["id"], body["fit"]
        assert body["image"] == frozen[bid]["image"]
        image_record(body["image"])
        assert body["hairFits"] == frozen[bid]["hairFits"] == fit["hairFits"]
        assert body["hairFitSha256"] == frozen[bid]["hairFitSha256"]
        assert body["latestBodyReview"] == frozen[bid]["bodyApproval"]
        assert body["latestHairReview"] == frozen[bid]["hairApproval"]
        assert body["reviewedFit"] == old_bodies[bid]["fit"]
        assert body["reviewedHairFits"] == frozen[bid]["hairFits"]
        assert fit["baseSha256"] == body["image"]["sha256"]
        assert fit["sha256"] == fit_payload_hash({k: v for k, v in fit.items() if k != "sha256"})
        assert body["clothingFitSha256"] == fit_payload_hash(clothing_payload(fit))
        for slot in ("torso", "legs"):
            item = items[fit[slot]["assetId"]]
            assert item["slot"] == slot and item["tier"] == "base"
            assert fit["itemHashes"][slot] == item["image"]["sha256"]
            placement(fit[slot], item["image"])
        for polygon in fit["neckOcclusion"] + fit["handOcclusion"]:
            assert len(polygon) >= 3 and all(0 <= x <= 1024 and 0 <= y <= 1024 for x, y in polygon)
        review = body["initialClothingReview"]
        assert review["sha256"] == body["clothingFitSha256"]
        if clothing_payload(fit) != clothing_payload(old_bodies[bid]["fit"]):
            changed.add(bid)
            assert review["decision"] == "pending", "Do not approve revised clothing automatically"
        else:
            assert bid in ("male-slim", "male-medium") and review["decision"] == "approved"
    assert changed == PRESETS - {"male-slim", "male-medium"}

    credits = 0
    rejected = []
    for entry in manifest["items"]:
        item_folder = folder / entry["id"]
        request = read(item_folder / "request.json")
        assert request["tool"] == "generate_asset_preview" and request["arguments"]["category"] == "item"
        generation = read(item_folder / "generation-response.json")
        assert digest(folder / generation["image"]["path"]) == generation["image"]["sha256"]
        credits += generation["creditsUsed"]
        if entry["status"] == "rejected_source":
            rejected.append(entry["id"])
            assert entry["rejectionReason"] and not entry.get("export")
            assert not (item_folder / "item.png").exists()
        else:
            exported = read(item_folder / "background-removal.json")
            assert digest(folder / exported["export"]["path"]) == exported["export"]["sha256"]
            assert entry["approval"] == "pending_review"
            credits += exported["creditsUsed"]
    assert len(manifest["items"]) == 7 and len(rejected) == 2
    assert credits == manifest["creditsUsed"] == data["creditsUsed"] == 12
    assert sum(x["possibleCredits"] for x in manifest["unresolvedGenerationCharges"]) == 1
    assert manifest["maximumRevisionCredits"] == 13
    for path in folder.rglob("*.json"):
        assert not re.search(r"X-Amz-Signature|Bearer |Authorization|[?&]sig=", path.read_text()), path
    print("PASS: 10 approved bodies/hair fits unchanged; 8 revised clothing fits pending, 2 clothing approvals retained.")
    print("PASS: 5 separate garment exports; 2 rejected drafts excluded; 12 confirmed credits + 1 uncertain timeout charge recorded. Eight retained bodies adopted for MVP with known art issues; historical quality decisions preserved.")


if __name__ == "__main__":
    main()
