# Static frontal body bases R1

Ten approved AutoSprite body bases: slim, medium, overweight, obese and muscular
for men and women. Men wear plain boxer bottoms with no top; women wear matching
bottoms and a sports bra. Bare hands/feet, no additional equipment, no animation.
The user explicitly requested these minimal garments as baked-in underlayers.
All later outer clothing and accessories remain separate assets.

## Approval and exports

All ten source designs were explicitly approved on 2026-09-27. The original
downloaded review is preserved at `reviews/user-approval-all-10.json` and every
reviewed SHA-256 matches its unchanged `source.png`.

Each variant also contains `body.png`, the untouched transparent export from
AutoSprite's `remove_asset_background`. Saving the existing pose as an export
asset was free; no new character or pose was generated. Original artwork,
1024×1024 canvas and pixel positions are preserved. The manifest freezes both
source and export checksums. `background-removal.json` records provenance and
export checks; it contains no expiring signed URL.

Serve the repository root with `python3 -m http.server 8899 --bind 127.0.0.1` and
open `http://127.0.0.1:8899/tools/review/body-bases-front.html`. Switch between the
approved original and transparent export, with dark/light/checkerboard backdrops.
Further feedback can be entered and downloaded as a new review JSON.
Reviews are bound to each output checksum and are not inferred from silence or
from approvals of the older fourteen illustration boards.

## Provenance and cost

Each variant folder contains its exact prompt, MCP request, generation response,
original source PNG and sanitized provenance. `manifest.json` pins source hashes.
The medium bases established the pose/identity references used for the remaining
four variants within each gender. Historical body boards guided silhouette
instructions; `imageReference` identifies the actual image sent to AutoSprite.

Ten reference-based `generate_pose` requests at 3 credits each: **30 credits**.
Ten background-removal calls at 1 credit each: **10 credits**, **40 total**.
Three body-reference uploads and ten saves of existing approved images were
free. No paid retries, new equipment generation or animations were made.

## Stable equipment fitting contract

Use `body.png` as the canonical static base, referenced by its SHA-256 from
`manifest.json`. Coordinates use the full 1024×1024 canvas with top-left origin;
neither the source nor transparent export is cropped, scaled or repositioned.
Content bounds are measured alpha extents, not a command to trim the image.
Align equipment to this full canvas; record each fit against the exact base
checksum. Keep every outer garment/accessory separate. Changing equipment must
not change body pixels. Body-dependent attachment placement and garment fitting
remain to be verified; the old dressed-base glove placement does not transfer
automatically. The approved artwork is not yet integrated into Unity.

Run `python3 tools/verify_static_body_bases.py` from the repository root before
using these bases in equipment work. It checks the saved approval, both image
hashes, dimensions, transparency, framing and the frozen source/export linkage.
