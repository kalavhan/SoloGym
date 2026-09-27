#!/usr/bin/env python3
"""Check separate static equipment, hairstyle, revision and fitting provenance."""
import hashlib
import json
import re
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "assets/sprites/autosprite"
PRESETS = {f"{gender}-{body}" for gender in ("male", "female")
           for body in ("slim", "medium", "overweight", "obese", "muscular")}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text())


def image_record(record):
    path = (SPRITES / record["path"]).resolve()
    assert path.is_relative_to(SPRITES.resolve())
    assert digest(path) == record["sha256"], f"Changed image: {path}"
    with Image.open(path) as image:
        assert image.size == (record["width"], record["height"]), path
        assert image.mode == "RGBA" and image.getchannel("A").getextrema() == (0, 255), path


def placement(fit, image):
    source, dest = fit["sourceRect"], fit["destinationRect"]
    for rect in (source, dest):
        assert all(isinstance(rect[k], int) for k in ("x", "y", "width", "height"))
        assert min(rect["x"], rect["y"]) >= 0 and min(rect["width"], rect["height"]) > 0
    assert source["x"] + source["width"] <= image["width"]
    assert source["y"] + source["height"] <= image["height"]
    assert dest["x"] + dest["width"] <= 1024 and dest["y"] + dest["height"] <= 1024


def imported_review(data):
    record = data.get("reviewRecord")
    if not record:
        return
    path = ROOT / record["path"]
    assert digest(path) == record["sha256"], "Changed submitted review"
    review = read(path)
    status = read(SPRITES / data["reviewStatusPath"])
    snapshot = status["reviewedFitting"]
    assert digest(ROOT / snapshot["path"]) == snapshot["sha256"], "Changed reviewed fitting snapshot"
    original = read(ROOT / snapshot["path"])
    assert status["sourceReview"] == record
    assert review["schema"] == "sologym.modular-character-review.v1"
    assert review["batch"] == original["id"] == status["batch"]
    expected = {
        "bodies": {b["id"]: b["image"]["sha256"] for b in original["bodies"]},
        "fits": {b["id"]: b["fit"]["sha256"] for b in original["bodies"]},
        "assets": {a["id"]: a["image"]["sha256"] for a in original["items"] + original["hairstyles"]},
    }
    for kind, hashes in expected.items():
        rows = review[kind]
        assert len(rows) == len(hashes) and {r["id"] for r in rows} == set(hashes)
        assert status["decisions"][kind] == {r["id"]: r for r in rows}
        for row in rows:
            assert row["sha256"] == hashes[row["id"]], (kind, row["id"])
            assert row["decision"] in ("approved", "changes_requested", "pending")
            if kind == "fits":
                assert row["baseSha256"] == expected["bodies"][row["id"]]
        assert status["counts"][kind] == {
            decision: sum(r["decision"] == decision for r in rows)
            for decision in ("approved", "changes_requested", "pending")}
    assert status["approvedFitIds"] == [r["id"] for r in review["fits"] if r["decision"] == "approved"]
    assert status["pendingWithActionableNotes"] == [
        {"kind": kind, "id": r["id"]} for kind in expected for r in review[kind]
        if r["decision"] == "pending" and r["notes"].strip()]
    print("PASS: submitted review preserved; all 25 reviewed hashes and verbatim decisions match the archived fitting snapshot.")


def fit_payload_hash(payload):
    return hashlib.sha256(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def clothing_payload(fit):
    return {k: fit[k] for k in ("baseSha256", "torso", "legs", "itemHashes", "neckOcclusion", "handOcclusion")}


def local_revision(previous):
    revision = read(SPRITES / "basic-clothing-front-r1/fitting-r2.json")
    assert revision["additionalCreditsUsed"] == 0
    assert revision["baseMutationAllowed"] is False and revision["animated"] is False
    assert revision["canvas"] == previous["canvas"]
    old_bodies = {b["id"]: b for b in previous["bodies"]}
    items = {i["id"]: i for i in revision["items"]}
    hairstyles = {h["id"]: h for h in revision["hairstyles"]}
    for collection in ("items", "hairstyles"):
        assert {a["id"]: a["image"] for a in revision[collection]} == {
            a["id"]: a["image"] for a in previous[collection]}
    assert len(revision["bodies"]) == 10 and {b["id"] for b in revision["bodies"]} == PRESETS
    changed, retained = set(), set()
    for body in revision["bodies"]:
        old = old_bodies[body["id"]]
        assert body["image"] == old["image"], "Body image changed during fitting"
        assert body["reviewedFit"] == old["fit"] and body["reviewedHairFits"] == old["hairFits"]
        fit = body["fit"]
        assert fit["baseSha256"] == body["image"]["sha256"]
        assert fit["sha256"] == fit_payload_hash({k: v for k, v in fit.items() if k != "sha256"})
        assert fit["hairFits"] == body["hairFits"]
        assert body["clothingFitSha256"] == fit_payload_hash(clothing_payload(fit))
        assert body["hairFitSha256"] == fit_payload_hash({
            "baseSha256": body["image"]["sha256"], "hairFits": body["hairFits"], "hairHashes": fit["hairHashes"]})
        for slot in ("torso", "legs"):
            item = items[fit[slot]["assetId"]]
            assert item.get("active", True) and item["slot"] == slot
            assert fit["itemHashes"][slot] == item["image"]["sha256"]
            placement(fit[slot], item["image"])
        assert fit["torso"]["assetId"] == "base-training-vest-r1", "Top design must remain consistent across sizes"
        for hid, position in body["hairFits"].items():
            assert fit["hairHashes"][hid] == hairstyles[hid]["image"]["sha256"]
            placement(position, hairstyles[hid]["image"])
        for polygon in fit["neckOcclusion"] + fit["handOcclusion"]:
            assert len(polygon) >= 3 and all(0 <= x <= 1024 and 0 <= y <= 1024 for x, y in polygon)
        different = clothing_payload(fit) != clothing_payload(old["fit"])
        assert body["clothingChangedFromReviewed"] == different
        assert body["hairChangedFromReviewed"] and body["hairFits"] != old["hairFits"]
        decision = body["initialClothingReview"]
        assert decision["sha256"] == body["clothingFitSha256"]
        if different:
            changed.add(body["id"])
            assert decision["decision"] == "pending", "Changed fit cannot inherit approval"
        else:
            retained.add(body["id"])
            assert decision["decision"] == old["userFitReview"]["decision"] == "approved"
            assert decision["sourceFitSha256"] == old["fit"]["sha256"]
        assert body["userBodyReview"] == old["userBodyReview"], "Do not silently approve a body"
    assert changed == {"female-overweight", "female-obese", "female-muscular", "male-overweight", "male-obese"}
    assert len(retained) == 5
    print("PASS: revision 2's archived changes and original approval carry-over are intact, with 0 additional credits.")


def revision_two_review():
    data = read(SPRITES / "basic-clothing-front-r1/fitting-r2.json")
    status = read(SPRITES / data["latestReviewStatusPath"])
    record = data["latestReviewRecord"]
    assert status["sourceReview"] == record
    assert digest(ROOT / record["path"]) == record["sha256"]
    review = read(ROOT / record["path"])
    snapshot = status["reviewedFitting"]
    assert digest(ROOT / snapshot["path"]) == snapshot["sha256"]
    reviewed = read(ROOT / snapshot["path"])
    assert review["schema"] == "sologym.modular-character-review.v2"
    assert review["batch"] == data["id"] == reviewed["id"]
    bodies = {b["id"]: b for b in reviewed["bodies"]}
    expected = {
        "bodies": {bid: b["image"]["sha256"] for bid, b in bodies.items()},
        "fits": {bid: b["clothingFitSha256"] for bid, b in bodies.items()},
        "hairFits": {bid: b["hairFitSha256"] for bid, b in bodies.items()},
        "assets": {a["id"]: a["image"]["sha256"] for a in reviewed["items"] + reviewed["hairstyles"] if a.get("active", True)},
    }
    for kind, hashes in expected.items():
        rows = review[kind]
        assert len(rows) == len(hashes) and {r["id"] for r in rows} == set(hashes)
        assert status["decisions"][kind] == {r["id"]: r for r in rows}
        for row in rows:
            assert row["sha256"] == hashes[row["id"]]
            assert row["decision"] in ("approved", "changes_requested", "pending")
            if kind in ("fits", "hairFits"):
                assert row["baseSha256"] == bodies[row["id"]]["image"]["sha256"]
        assert status["counts"][kind] == {
            decision: sum(r["decision"] == decision for r in rows)
            for decision in ("approved", "changes_requested", "pending")}
    assert set(status["approvedBodyIds"]) == set(status["approvedHairFitIds"]) == PRESETS
    assert set(status["approvedClothingFitIds"]) == {"male-slim", "male-medium"}
    assert set(status["clothingChangesRequestedIds"]) == PRESETS - {"male-slim", "male-medium"}
    manifest = read(SPRITES / "body-bases-hairfree-r1/manifest.json")
    lock_ref = manifest["approvedBaseHairLock"]
    assert digest(ROOT / lock_ref["path"]) == lock_ref["sha256"]
    lock = read(ROOT / lock_ref["path"])
    assert lock["sourceReview"] == record and lock["reviewedFitting"] == snapshot
    assert manifest["approval"] == "approved" and manifest["canonicalBaseFrozen"]
    assert lock["canvas"] == data["canvas"]
    assert lock["bodyMutationAllowed"] is False and lock["hairFitMutationAllowed"] is False
    locks = {b["id"]: b for b in lock["bodies"]}
    variants = {v["id"]: v for v in manifest["variants"]}
    assert len(locks) == 10 and set(locks) == PRESETS
    for body in data["bodies"]:
        bid = body["id"]
        frozen = locks[bid]
        assert body["image"] == frozen["image"] == bodies[bid]["image"], "Approved body changed"
        assert body["hairFits"] == frozen["hairFits"] == bodies[bid]["hairFits"], "Approved hair placement changed"
        assert body["hairFitSha256"] == frozen["hairFitSha256"]
        for field, kind in (("latestBodyReview", "bodies"), ("latestClothingReview", "fits"), ("latestHairReview", "hairFits")):
            assert body[field] == status["decisions"][kind][bid]
        assert frozen["bodyApproval"] == body["latestBodyReview"]
        assert frozen["hairApproval"] == body["latestHairReview"]
        assert frozen["bodyApproval"]["decision"] == frozen["hairApproval"]["decision"] == "approved"
        assert variants[bid]["approval"] == "approved" and variants[bid]["canonicalBaseFrozen"]
    assert lock["hairstyles"] == [{"id": h["id"], "image": h["image"]} for h in data["hairstyles"]]
    assert status["additionalCreditsUsed"] == 0 and status["artOrPlacementChangesInThisImport"] is False
    print("PASS: r2 review has 34 matching hashes; 10 bodies and 10 hair fits frozen, 2 clothing approvals, 8 clothing change requests, 4 pending asset designs.")


def main():
    data = read(SPRITES / "basic-clothing-front-r1/fitting.json")
    assert data["animated"] is False and data["baseMutationAllowed"] is False
    assert data["canvas"] == {"width": 1024, "height": 1024, "origin": "top-left"}
    bodies = data["bodies"]
    assert len(bodies) == 10 and {b["id"] for b in bodies} == PRESETS
    items = {i["id"]: i for i in data["items"]}
    hair = {h["id"]: h for h in data["hairstyles"]}
    assert len(items) == 3 and len({i["equipmentId"] for i in items.values()}) == 2
    assert len(hair) == 2 and {i["slot"] for i in items.values()} == {"torso", "legs"}
    for asset in [*items.values(), *hair.values()]:
        image_record(asset["image"])
    for body in bodies:
        image_record(body["image"])
        fit = body["fit"]
        assert fit["baseSha256"] == body["image"]["sha256"], body["id"]
        payload = {k: v for k, v in fit.items() if k != "sha256"}
        assert fit["sha256"] == hashlib.sha256(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
        for slot in ("torso", "legs"):
            asset = items[fit[slot]["assetId"]]
            assert asset["slot"] == slot and asset["tier"] == "base"
            assert fit["itemHashes"][slot] == asset["image"]["sha256"]
            placement(fit[slot], asset["image"])
        assert fit["hairFits"] == body["hairFits"]
        for hid, position in body["hairFits"].items():
            assert fit["hairHashes"][hid] == hair[hid]["image"]["sha256"]
            placement(position, hair[hid]["image"])
        for polygon in fit["neckOcclusion"] + fit["handOcclusion"]:
            assert len(polygon) >= 3 and all(0 <= x <= 1024 and 0 <= y <= 1024 for x, y in polygon)
    revisions = read(SPRITES / "body-bases-hairfree-r1/manifest.json")
    originals = read(SPRITES / "body-bases-front-r1/manifest.json")
    original_map = {v["id"]: v for v in originals["variants"]}
    for revision in revisions["variants"]:
        source = revision["sourceImage"]
        assert digest(ROOT / source["path"]) == source["sha256"] == original_map[revision["id"]]["image"]["sha256"]
        assert revision["transparentExport"]["sha256"] == next(b["image"]["sha256"] for b in bodies if b["id"] == revision["id"])
    credits = 0
    for directory in ("basic-clothing-front-r1", "hairstyles-front-r1", "body-bases-hairfree-r1"):
        folder = SPRITES / directory
        for path in folder.rglob("*.json"):
            text = path.read_text()
            read(path)
            assert not re.search(r"X-Amz-Signature|Bearer |Authorization|[?&]sig=", text), path
        for filename in ("generation-response.json", "background-removal.json"):
            credits += sum(read(p)["creditsUsed"] for p in folder.glob("*/" + filename))
        if directory != "body-bases-hairfree-r1":
            for path in folder.glob("*/request.json"):
                request = read(path)
                assert request["tool"] == "generate_asset_preview", path
                assert request["arguments"]["category"] == "item", path
    assert credits == 52, f"Unexpected credit ledger: {credits}"
    imported_review(data)
    local_revision(data)
    revision_two_review()
    print("PASS: 10 unchanged revision exports, preserved approved sources, 3 separate garment fits, 2 hairstyles, all placements/hash links, 52-credit ledger.")
    print("Historical r1/r2 checks preserve the submitted quality decisions. See the r3 MVP adoption record for current runtime scope.")


if __name__ == "__main__":
    main()
