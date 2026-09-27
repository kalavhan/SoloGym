# Base equipment: fitted garment revision brief

Current disposition: the user accepted the existing art for temporary MVP use, removed obese from both genders, and deferred sleeve/design polish. See [runtime integration](character-modular-mvp.md). The brief and review history below remain as provenance; they are not a new approval gate.

The user's r2 review approves every body and hair fit, but requests changes to
eight clothing fits. This is an artwork/cut problem, not an attachment-position
problem. Rectangular scaling preserved the top's motif but erased body form.
R3 candidates are now under `assets/sprites/autosprite/basic-clothing-fit-r3/`.
Review them at `tools/review/garment-fit-r3.html`; the before toggle preserves
the exact r2 fit. New artwork and fitting remain pending user approval.

## Frozen inputs

Use `assets/sprites/autosprite/body-bases-hairfree-r1/approved-base-hair-lock.json`.
Keep all body PNGs, pose, canvas, head position and both hair placements unchanged.
Male slim and medium clothing remain approved. Keep the medium review note
about similarity to slim for comparison, without silently revoking its approval.
The old helmet note on the muscular body does not reopen its explicit hair-fit
approval. Preserve both original submitted JSON reviews and fitting snapshots.

## Garment changes

| Wearer | Required change | Keep |
| --- | --- | --- |
| All five female presets | Torso fit variants with appropriate shoulders, sleeve openings, waist and hip shape; fabric drape follows each approved body. | Full-length opaque crew-neck coverage, short sleeves, modest styling. |
| Male overweight | Rounded torso volume, belly drape and believable fold placement. | Same top/vest identity; do not scale a slim waist wider and longer. |
| Male overweight and obese | Proportional waistband, crotch depth, leg openings, hem and trim thickness. | Above-knee boxing shorts; no excessive flare or stretched border. |
| Male muscular | A cut that follows the approved shoulders, upper arms and torso; distinguish the build from slim/medium in a clothed side-by-side view. | Existing approved anatomy and covered Base-tier sportswear. |

These are fit variants of the same kit, not different outfit designs. Match the
navy/charcoal palette, cream side panels, petrol piping, vest opening and trim
layout. Keep the simple matte Base treatment; do not add glow, premium detail,
random pockets, different closures, cleavage or exposed midriff to distinguish
body presets. Do not change skin or anatomy to make the clothes fit.

## Authoring and acceptance

Use AutoSprite asset tools for isolated garments only. Never generate or upload a
fully dressed body as an equipment reference. An exported item must have no
head, face, torso skin, hands, legs or other body fragments. Preserve provider
provenance, hashes and credits. Keep one equipment identity with explicit fit
variants; share a variant only where its silhouette and drape actually fit.

Start with a representative female medium top and a male overweight top/shorts
fit before expanding the remaining variants. Compare both against the regular
kit artwork and their exact locked bodies at full-body and close-up sizes. If
the asset tool cannot preserve the garment design while fitting it, report the
limitation instead of redrawing the wearer or repeatedly generating unrelated
outfits. Do not assume a text-only prompt guarantees a matching garment family.

Review each new item both in isolation and locally layered over the unchanged
base. Verify base-garment coverage, seams, neckline, hand occlusion, crotch and
hem. Compare slim, medium, overweight, obese and muscular side by side: body
shape should remain legible without changing the scale of the whole character.
No animation, breathing, isometric views or runtime integration in this pass.

## R3 execution record

AutoSprite's current asset generator accepts text, not a base-image reference.
Five isolated exports provide female shaped and fuller tops, a rounded male
top, a muscular male top, and heavier-fit shorts. The regular approved male
kit and existing female shorts are reused. Each fit records source and
canvas rectangles; body images and hair placement are unchanged. This is
static composition, not automatic garment fitting or animation-ready rigging.

Two drafts were discarded before library save/background removal: one included
unrequested trousers, and one included a large unrequested logo. Seven returned
previews and five background removals account for 12 confirmed credits. A lost
female-top request timed out without an image or recovery ID; its possible
one-credit charge is recorded separately, for a maximum of 13. The muscular
request had not started in that interrupted sequence. One replacement female
preview was requested, with no automatic paid retries.

The new assets vary in fold and panel details because generation was text-only.
User review must check whether they still read as one Base kit. The candidate
status does not claim that every garment is production-approved. The review
exports only new asset design decisions, eight pending clothing fits, and the
existing approved body/hair and two male clothing decisions. Approved clothing
controls are read-only. The previous independent asset decisions stay in the
r2 ledger.

Run `python3 tools/verify_fitted_clothing_r3.py` alongside the two source
verifiers. Hash and geometry checks protect approved inputs and exact review
identity; they do not replace visual judgment.
