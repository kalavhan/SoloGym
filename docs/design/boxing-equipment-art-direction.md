# Boxing equipment: starter visual direction

The character should read as a stylish RPG boxer through recognizable boxing
equipment, a coherent silhouette and deliberate garment design. The user set
four tiers: **Base, Rare, Ultra Rare, Premium** (updated 2026-09-27).
This pass designs Base only; the higher tiers have no approved designs yet.
Historical `basic` asset paths refer to Base. Keep six equipment slots with one item per
slot: head, torso, hands, legs, feet and back.

| Tier ID | Player-facing name | Art direction |
| --- | --- | --- |
| `base` | Base | Simple, well-fitted static sprite; ordinary matte sportswear, no glow or animated effects. |
| `rare` | Rare | More stylish, modern and visually refined; stronger tailoring, materials and deliberate details. |
| `ultra_rare` | Ultra Rare | Distinctive high-tier design, with optional glow accents. |
| `premium` | Premium | Detailed, carefully designed, stylish, otherworldly equipment with glow. Unique silhouettes and cohesive materials, not ordinary clothing with effects added. |

Rarity describes visual design quality and treatment, not a requirement for
animation. The MVP remains static. Higher-tier glow can be authored into a static
item sprite or separate static overlay; animated effects require a later scope.
No higher-tier artwork is generated during the current Base clothing fit pass.

## Current modular fitting pass

The latest user review is **revision 2, 2026-09-27T17:01:40Z**. All ten hair-free
bodies and all ten hair fits are approved and frozen. Only male slim/medium
clothing fits are approved; all five female fits and male overweight/obese/
muscular need changes. Four independent asset-design decisions remain pending.
The exact review and fitting snapshot are under `basic-clothing-front-r1/reviews/`;
`review-status-r2.json` is authoritative. Use the [r3 garment brief](boxing-equipment-fit-r3.md)
for further work. The following paragraphs document how the reviewed passes
were produced; their earlier pending/rejected decisions are historical.

The approved static body set is preserved under `body-bases-front-r1`.
The user then explicitly requested hair-free revisions and separate hairstyles;
those ten new candidates are under `body-bases-hairfree-r1` and need their own
review because reference-based edits can redraw details. They are not new
fully dressed characters and include only the permitted minimal base garments.

`assets/sprites/autosprite/basic-clothing-front-r1/` contains isolated Base
clothing: one training top/vest item with regular and wider-waist fit sprites,
and one straight-cut boxing-shorts item. The superseded flared shorts are retained
as history. `hairstyles-front-r1/` contains Close crop and Short sweep as separate
cosmetic layers, independent of head equipment. All clothing and hair artwork
was created through asset tools with no person or stray body parts.

The local review is `tools/review/modular-character-front.html`. It composes
unchanged body and item images locally, toggles top/shorts/hair independently,
and offers separate decisions for base revisions, item designs and fit. The
fitting manifest pins both body and asset hashes, static placement and neck/hand
occlusion. Body pixels used for occlusion come from the same unchanged base.
New items or changed base files require another fit check. No character motion,
Unity integration, higher-tier item or additional equipment slot is included.

This pass used **52 credits**: 8 for isolated clothing and fit refinements,
40 for the explicitly requested hair-free body revisions and their transparency,
and 4 for two separate hairstyles and transparency. The review submitted at
2026-09-27T13:05:59Z approves five clothing fits: female slim/medium and male
slim/medium/muscular. The other five fits need follow-up; female overweight and
male obese retain their submitted **pending** decisions with actionable notes.
All ten body decisions request changes, with notes about hair fit rather than
body anatomy. All five individual asset decisions remain pending. The earlier
body approvals do not transfer to these revisions.

The exact review, checked against all 25 body/fit/asset hashes, and the fitting
snapshot are under `basic-clothing-front-r1/reviews/`. `review-status.json`
tracks four open corrections: head-specific hair placement and volume; matching
the regular top design on wider bodies; smaller leg openings for female
overweight/obese shorts; and the muscular woman's right-leg/shorts overlap.
The wide top was independently generated and does not preserve the regular
top's design closely enough. Do not treat it as an accepted size variant.
The bald bodies were generated with AutoSprite reference-based `generate_pose`;
using the same provider does not guarantee compatibility with isolated hair.
Review import changes no artwork or placement and spends no additional credits.

### Local fitting revision 2

`tools/review/modular-character-front-r2.html` reads `fitting-r2.json` in the same
clothing folder. This revision changes placement only, with **0 additional
credits** and unchanged body/item PNGs. It fits both hairstyles to each head,
reduces crop volume, uses the regular top artwork on all ten bodies, narrows
the female overweight/obese shorts, and excludes thigh pixels from the muscular
woman's hand-restoration region. The obese man's neck region is fitted to the
regular collar, avoiding shoulder pixels appearing above it.

The page includes before/after comparison and head/shorts close-ups. The v2
review format separates body, clothing fit, hair fit and asset design decisions.
Five unchanged clothing approvals carry forward only after verifying an
identical clothing payload. Five changed clothing fits and every hair fit need
review. No body or asset design is automatically approved. The shared top's
static shading does not conform to each torso's volume; this revision needs
visual acceptance and does not prove every future garment can use one sprite.

The subsequent r2 review confirms that this limitation is visible: a universal
top hides female tailoring and distinctions between slim, medium and muscular;
heavier shirts/shorts look stretched. The next pass must author separate fitted
garment assets with consistent styling. Do not try another global scaling pass.
This import generated no new artwork and used zero additional credits.

## Basic kit

Use matte charcoal and midnight navy, cream fabric panels and restrained dull
petrol-blue accents. Style comes from tailoring, contrasting panels, stitching
and the boxing silhouette. Materials are ordinary jersey, cotton, canvas and
leather. There is no glow, emissive detail, particle effect or clothing animation.

Use practical sportswear with more coverage: the top covers the torso through
the waistband, has a crew neck and short sleeves, and the shorts end above
the knee with a relaxed fit. Avoid exposed midriff, cleavage, cutouts and
chest-focused tailoring or shading. Preserve the character's existing body
proportions; increase garment coverage rather than changing her anatomy.

| Slot | Proposed Basic item | Visual role |
| --- | --- | --- |
| Head | Cloth headband | Sport identity, keeping the face and hairstyle visible. |
| Torso | Full-length crew-neck training top and vest | Short sleeves and a practical fit covering the chest and midriff through the waistband; the hip-length vest retains the established contrasting fabric panels. |
| Hands | Padded boxing gloves | Rounded closed knuckles, attached thumbs, practical wrist straps and a small visible wrap edge. |
| Legs | Relaxed above-knee boxing shorts | Broad fabric waistband, longer loose legs and a clean side stripe. |
| Feet | Lightweight boxing boots | Slim mid-calf shape, laces, canvas/leather panels and flexible thin soles. |
| Back | Compact fabric boxing-gym backpack | A small pack mounted behind the torso, with practical shoulder straps and a restrained outline visible from the front. |

The back slot is for back-mounted equipment or accessories, such as backpacks,
tails and wings. The Basic boxing kit uses a compact fabric gym backpack;
it does not use a towel draped over the shoulder. Other accessory categories
describe the slot's purpose, not additional items for this Basic pass. Keep
the backpack behind the body, with straps following the shoulders and training top
without obscuring the gloves or changing the body's proportions.

Keep gloves proportional and recognizably padded leather. Avoid metal gauntlets,
plate shoulders, greaves, heavy tactical boots, random belts, weapon props and
unrelated fantasy armor. Preserve the reference character's adult identity and
body proportions; do not increase muscle volume to make the outfit look powerful.

## Current concept and next proof

The coverage-corrected concept request is under
`assets/sprites/autosprite/boxer-basic-r3/`, using the existing AutoSprite female
character as the visual reference. It targets one static frontal outfit concept.
Do not record the new generation as approved before it is reviewed. Six proposed
items do not establish six fitted equipment layers or prove modular equipment
interchangeability.

The user accepted the `boxer-basic-r2` direction overall with mandatory coverage
changes: less exposed skin and a less sexualized presentation. Keep its palette,
gloves, headband, boots, backpack and overall style. Replace the revealing torso
and shorts with the covered Basic garments above. Preserve r2 and its source
records as history; its unchanged clothing is superseded by the r3 direction.

The towel-based `boxer-basic-r1` concept is superseded by this back-slot
correction. Preserve it as exploration history; it is not the current Basic
outfit direction and must not be treated as approval of a fitted back item.

The user explicitly authorized continuing through the coverage correction to
one equipment-swap proof on this exact character. Proceed without adding
another approval gate; this authorization is not advance visual approval of
r3 or proof of successful modular equipment. Complete and inspect that swap
before expanding the five body types per gender or outfit library. Keep the
original body, all saved references and source art intact, and validate actual
garment fit, occlusion and registration. Keep this work static for the MVP;
character animation requires a separate later scope.

The earlier `boxer-style-r1` request was already submitted when the user specified
Basic-tier constraints. It is superseded exploration, not an approved Basic,
Rare or Ultra Rare design. It must not replace the current runtime art.

## Completed static fit proof

`assets/sprites/autosprite/boxer-basic-r3/basic-covered-concept.png` contains the
coverage correction. The technical continuation is under
`assets/sprites/autosprite/boxer-basic-equipment-proof-r1/`: one frozen wrapped-hand
base and a separate transparent glove image, both exported through AutoSprite.
The local review at `tools/review/boxing-equipment-proof.html` can equip or remove
the gloves without replacing the character image.

The single-pose hands-slot test passed visual inspection at native resolution
on light and dark backgrounds. `proof.json` records the measured source regions
and placement; AutoSprite did not preserve the requested glove scale and position.
`validation.json` records the checks, and the manifest pins the unchanged exports.
This passes a static glove-fit test only. The other five items remain baked into
the base; animated equipment, other body types and Unity integration are unproven.
No new animation or runtime replacement was made in this pass.

## Reusable base and generation cost

The intended customization system uses one canonical static body base per
supported body preset, with separately fitted equipment layers. Begin with one
body. **All equipment clothing and accessories are independent assets.** The
user's 2026-09-27 exception permits minimal plain base garments: boxer briefs
and no top for men; matching bottoms and an opaque sports bra for women. Those
non-removable underlayers may be included in each body image so later items can
cover them. Do not generate a dressed character to make or revise equipment.
Outer clothing and the backpack remain separate; the exception does not allow
them to be baked into the body. The user now prioritizes generating all ten
static frontal body bases before making further equipment.

The former workflow built a dressed result first and separated only the gloves.
That order is superseded. The dressed r1/r2/r3 images remain historical style
references; the glove proof establishes one removable slot, not a production
base. Its dressed `wrapped-base.png` must not become the canonical body in
configuration/Home. The isolated gloves can be reused if they fit the eventual
canonical base; their old placement is not automatically valid on another body.

The creation sequence is body base, isolated equipment, local composition:

1. Author one canonical body and pin its pose, canvas, baseline and checksum.
2. Create each clothing/accessory item with `generate_asset_preview`, save it
   with `create_asset`, and use `remove_asset_background` where needed. Request
   only the isolated item, without a person or unrelated body parts. Check the
   actual tool schema and export; do not assume reference input or automatic fit.
3. Save placement and draw order relative to the fixed body. A backpack may
   require rear-bag and front-strap assets; both belong to the same item.
4. Compose all outfit previews locally. Changing an item must not change the
   base image or make any full-character generation request.
5. Check fit and garment coverage at phone size. If an isolated asset fails,
   fix that asset or report the provider limitation; do not fall back to an
   AI-rendered dressed body and call it modular equipment.

Do not use `create_character`, `upload_character` or an on-body `generate_pose`
edit for clothing/accessory production. Do not create additional character
records to serve as outfit references. Clothing designs belong in the asset
gallery. Reuse valid existing exports and track generation costs. Other body
shapes may need garment-fit variants; one item is not assumed to fit every
silhouette. Neither changing equipment nor rendering a combination needs an
AI call once the appropriate base and item assets exist.

Two extra AutoSprite character entries were free `upload_character` operations
that promoted existing outfit exports into reference images for later edits:
`SoloGym Basic outfit reference R2` and
`SoloGym Basic covered R3 equipment reference`. Those uploads cost zero credits;
the subsequent `generate_pose` edits did cost credits. The saved responses record
12 credits for four outfit concept/revision generations, 6 for the wrapped-hand
base and isolated gloves, and 2 for their background removal: **20 credits for
this outfit exploration and static glove proof**, excluding earlier idle work.
No further provider request was made during this cost clarification.

## Static MVP scope

The user deferred all character animation, including subtle breathing, because
it adds unnecessary MVP complexity. Generate and fit static, full-body frontal
artwork for configuration and Home. Focus on visual quality, proportions,
coverage and equipment placement. The glove proof remains static; no movement
test, skeleton or deformation setup is required before continuing this work.

The earlier proposal for shared breathing is deferred and is not the next task.
Do not add idle bobbing, tiny motion details, animated gear or full animation
sets per body, item or outfit combination to the MVP. Preserve the existing
AutoSprite whole-character idle as a historical reference without extending it
to the new outfits. Motion and isometric presentation may be reconsidered in a
separate future scope; the current character work must not depend on either.
