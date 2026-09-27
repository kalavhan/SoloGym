# Separate Base clothing and static fitting

This pass produces isolated equipment assets, never dressed character images.
The current inventory has two items: a training top/vest and boxing shorts.
The top has regular and wider-waist fit sprites; one inventory item selects the
appropriate sprite for the chosen body. The first shorts export remains under
`legs/` as superseded history; its excessive hem flare was corrected in `legs-r2/`.

All garment PNGs were generated with AutoSprite `generate_asset_preview`, saved
with `create_asset`, and exported with `remove_asset_background`. Source images,
requests, provider IDs, hashes and costs are retained. No character reference
upload or character/pose generation was used to produce clothing.

## Review

Latest decision: revision 2 at **2026-09-27T17:01:40Z** approves all ten bodies,
all ten hair fits and male slim/medium clothing. Eight clothing fits request
changes and four standalone asset-design decisions remain pending.
`reviews/user-review-r2-2026-09-27T170140Z.json` preserves the submitted file;
`reviews/fitting-reviewed-r2-2026-09-27T170140Z.json` pins what was reviewed.
All 34 reviewed checksums matched. `review-status-r2.json` is the current ledger.
The approved bodies/hair are frozen separately; the next clothing pass needs
authored fit variants, not further uniform or nonuniform scaling of one sprite.
See `docs/design/boxing-equipment-fit-r3.md`. No r3 artwork has been generated.

The following describes the earlier review and preparation of r2:

Open `http://127.0.0.1:8899/tools/review/modular-character-front.html` with the
repository-root server running on port 8899. The page previews all ten requested
hair-free body revisions with independent garment and hairstyle switches.
Review the body revisions, item designs and individual fits separately.
It does not imply approval of the new hair-free bodies or replace Unity assets.

The submitted review is saved byte-for-byte in
`reviews/user-review-2026-09-27T130559Z.json`, alongside the exact fitting
snapshot it reviewed. All 25 body/fit/asset checksums matched. See
`review-status.json` for decisions and open corrections. Five clothing fits
are approved; three request changes and two remain pending with actionable
notes. All ten body entries flag hair-fit problems, and five asset designs
remain pending. Hair requests do not imply that body anatomy should be redrawn.
The wide-top design mismatch, oversized heavier-female shorts and muscular
female leg overlap required corrections. Import changed no images or placements.

The next candidate is `fitting-r2.json`, reviewed through
`tools/review/modular-character-front-r2.html`. It preserves every PNG, replaces
the mismatching wide-top selection with the regular artwork, revises hair
placement per head, narrows the two heavier female shorts, and corrects the
muscular woman's hand occlusion plus the obese man's neck occlusion. No new
generation credits were used. Before/after comparison retains the submitted
fit exactly. Five unchanged clothing approvals carry forward; the other five
and all revised hair fits remain pending. Body decisions are preserved and
standalone asset designs remain pending. See `validation-r2.json` for checks.

`fitting.json` records each base checksum, selected item fit sprite, source and
destination rectangles, and draw order on the unchanged 1024×1024 body canvas.
The preview draws the body at (0,0), then independent shorts and top images.
Small clipping polygons restore the same body's neck and hands above clothing
where necessary; those pixels are copied from that body, never painted or
generated. Hair is a separate cosmetic layer, independent of the head slot.
These are static clipping/placement rules, not a skeleton or animation system.

Changing clothing only changes item selection or fitting metadata. No body file
is altered. Fits reference exact body and item hashes; changing either requires
rechecking the fit. Do not assume automatic fit for newly generated items or use
this proof to claim that all six equipment slots are implemented.

## Cost and validation

Four isolated garment generations plus four transparent exports: **8 credits**.
This includes the superseded shorts and the wider-waist top. The separate
hair-free revision batch used 40 credits; two hairstyles used 4. **52 credits**
for the combined pass, including revisions, with no new fully dressed character.
Background-removal downloads remain unmodified provider outputs.

Run `python3 tools/verify_static_body_bases.py` to verify approved originals, then
`python3 tools/verify_modular_character_assets.py` for the separate layers,
revision provenance, fitting references, imported review and credit ledger.
Technical checks do not override the user's requested visual corrections.
