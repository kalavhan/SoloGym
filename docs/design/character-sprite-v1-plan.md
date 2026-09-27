# Character sprites: first-version body plan

The active MVP has **four builds per gender**: slim, medium, overweight and
muscular. The user removed obese on 2026-09-27 because it was too similar to
overweight. Existing r3 garments are accepted for temporary use, with sleeve
and male design inconsistencies deferred. No further generation is planned.
See [MVP integration](character-modular-mvp.md) for the running app and checks.

The earlier five-build batch and all fourteen illustrated references remain
preserved as historical sources. All ten static bases under
`assets/sprites/autosprite/body-bases-front-r1/` were explicitly approved on
2026-09-27. The saved `reviews/user-approval-all-10.json` matches every source
checksum. The review page is `tools/review/body-bases-front.html`.
The batch used 30 generation credits plus 10 background-removal credits.
All ten transparent exports are saved as `body.png` in their preset folders,
derived from the approved originals with the full source canvas and pose
preserved. Source and export hashes are frozen in the batch manifest and
checked by `tools/verify_static_body_bases.py`. The eight active presets now have runtime selection and separate clothing layers.

The subsequent user request authorizes hair-free base revisions plus separate
hairstyles. Ten new revisions now live under `body-bases-hairfree-r1`; original
approved images remain intact. AutoSprite can redraw details during reference
edits, so the revisions require independent review. The modular review at
`tools/review/modular-character-front.html` demonstrates separate Base top/shorts
assets and two compact hairstyles over those candidates. Its fitting data pins
source checksums; no body is regenerated when clothing or hair is toggled.

The latest modular review, submitted at 2026-09-27T17:01:40Z, **approves all ten
hair-free bodies and all ten hair fits**. Their exact exports and hair placement
are frozen in `assets/sprites/autosprite/body-bases-hairfree-r1/approved-base-hair-lock.json`.
Eight of these canonical bases are now integrated into customization and Home. Preserve the old hair-bearing originals and both review snapshots.
In the historical r2 review, only male slim/medium clothing fits were approved; eight required garment variants
that retain the body's silhouette rather than rectangular scaling. Standalone
asset designs remain pending. See `basic-clothing-front-r1/review-status-r2.json`
for exact decisions and `boxing-equipment-fit-r3.md` for the correction brief.
Hair and clothing have separate fit checksums and review decisions.

Configuration and Home use the same **static, full-body, front-facing character**
in the MVP. Prioritize generated artwork, readable silhouettes, covered Basic
clothing and consistent equipment fit. Isometric views and all character motion
are deferred, including breathing, subtle idle movement, rigging and deformation.
The earlier shared-breathing proposal is not an MVP requirement or next step.

Do not produce a separate full animation set for every body preset, equipment
item or outfit combination. Body variants need fitted static artwork and placement
calibration. Keep existing AutoSprite idle experiments as later references; they
do not establish the ten body variants or interchangeable equipment. Motion can
be reconsidered under a separate future scope without blocking this static MVP.

## References and mapping

The [body reference catalog](../../design/character-autosprite/references/body-types-v1.json)
links each planned type to the exact selected revision and SHA-256 in the
[approved illustration manifest](../../design/character-2-5d/approval-r1/manifest.json).
These new labels do not rename the archived asset IDs.

| Planned type, each gender | Primary illustrated reference | Appearance to retain |
| --- | --- | --- |
| `slim` | `skinny` | Narrow silhouette and softer limbs with low muscle volume. |
| `medium` | `normal` | Moderate volume, softly flat abdomen and ordinary limbs, without a carved six-pack. |
| `overweight` | `chubby` | Fuller, softer torso and limbs, with a clear silhouette difference from medium. |
| `obese` (archived) | `fat` | Removed from MVP selection; preserve as a historical reference. |
| `muscular` | `muscular` | Visible muscle form while preserving the approved proportions. |

Retain `skinny_muscular` and `fat_muscular` for each gender as supplementary
references. They remain approved illustrations even though they are outside
the first sprite selection. Preserve all source masters, earlier revisions,
style and identity anchors, prompts, hashes and review records. Do not merge,
overwrite or delete them as part of reducing the production list.

Keep the established manhwa linework, hair shapes, authored shading and
charcoal/cyan/silver material language. Preserve a consistent adult identity
within each gender. Female muscle volume remains smaller than the male
counterpart, with proportionate arms and legs. Use the saved masters to
judge these choices; repeated prompts do not guarantee identical pixels.

Body selection describes appearance only. Do not infer it from weight,
measurements, health, fitness or workout history, and do not map it to
strength, ability or combat stats. The four active names are the requested visual
options, not clinical classifications.

## Production sequence

Historical authoring batch (completed 2026-09-27): generated the five frontal body types for
each gender before expanding equipment. Men wear only plain boxer briefs; women
wear matching plain bottoms and a simple opaque sports bra. These minimal base
garments are the explicit exception to the separate-equipment rule and remain
part of the reusable body image. Bare hands and feet, no wraps, gloves, shoes,
headband, backpack, outer clothing or effects. Keep the same identity and neutral
frontal pose within each gender. Review new outputs separately from the old
fourteen approved illustration boards. No animation or runtime integration.

1. Begin with one canonical static full-body frontal body base. Preserve its
   identity, pose and canvas, then freeze its checksum after review. Only the
   user-requested minimal base garments above are included. Additional clothing
   and accessories are assembled from separate equipment assets.
   Do not draw programmer art, embed SVGs, build bodies from independently
   generated limbs or generate animation sheets for the MVP.
2. Use AutoSprite asset tools to generate and save each isolated clothing or
   accessory design. Never generate another dressed character as an equipment
   step; do not create character records for outfit-edit references. All six
   equipment slots must have independent assets. Reuse existing isolated art
   where it passes fit checks on the canonical base.
3. Export under `assets/sprites/` in versioned folders. Record base checksums,
   item IDs, canvas/placement, draw order and any separate rear/front parts.
   Retain prompts, provider job/asset IDs, costs and generation/review status;
   exclude credentials and expiring signed URLs. Equipment changes may update
   item files and placement, never redraw or overwrite the canonical body.
4. Assemble outfit previews locally from the body and assets. Verify swaps,
   full head-to-foot framing, coverage, alpha edges and fit at phone size.
   A rendered full outfit is a derived preview, not a new generated character.
   The old dressed-base glove proof is historical fit evidence only; it is not
   the canonical modular base to integrate into configuration and Home.
5. The requested ten base silhouettes now precede further equipment work.
   Fit variants may be needed per body; do not assume universal
   fit or generate whole-character outfit combinations. Keep the planned set
   unless visual review justifies an explicit scope change. New exports do
   not inherit approvals from the illustrated references.

If animation is commissioned in a later scope, retain and read the provider
JSON atlas before writing playback code. This is not part of the MVP pass.

Only verified, reviewed exports should become selectable body options.
The existing frontend preview and the preserved illustration archive remain
separate from this next generation pass.
