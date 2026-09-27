## Art pipeline

When this game needs 2D character sprites or
equipment, use the AutoSprite MCP tools. Use
create_character or reference-based generate_pose for
body-base authoring, and the asset tools
for clothing/accessories (generate_asset_preview,
create_asset, remove_asset_background). Animation
generation is deferred beyond the MVP.
Do not draw programmer art or embed SVGs.
Save exports under assets/sprites/ and read
the JSON atlas before writing animation code.

For the MVP, use static, full-body, front-facing
character artwork in both configuration and Home.
Focus on generated art quality, consistent proportions
and static equipment fit. Isometric views are deferred.

The MVP sprite body types are slim, medium, overweight,
and muscular for each gender (eight selections). Obese was
removed from selection for both genders; keep its source art
as historical reference.
Preserve all 14 approved illustrated designs as
references, including the extra mixed-muscle types.
Use docs/design/character-sprite-v1-plan.md and its
reference catalog; new sprites require their own review.

Equipment tiers are base, rare, ultra_rare, and premium.
Base is simple static gear. Rare uses stronger modern styling.
Ultra Rare may use glow. Premium is detailed, carefully designed,
stylish and otherworldly, with glow; it must not be an ordinary
outfit with glow pasted on. Historical `basic` names mean Base.
These tier directions do not authorize animation for the MVP.
The current outfit pass is Base: static boxing
sportswear with no glow, animated effects, or generic
fantasy armor. Follow docs/design/boxing-equipment-art-direction.md.

Keep six equipment slots: head, torso, hands, legs,
feet, and back, with one item per slot. The back slot
holds back-mounted accessories such as backpacks,
tails, or wings. Basic currently uses a compact fabric
boxing-gym backpack; the shoulder-towel concept is
superseded. The current concept request is under
assets/sprites/autosprite/boxer-basic-r3/; it does not
prove fitted, interchangeable equipment layers.

Basic clothing must give more coverage: a full-length
crew-neck training top with short sleeves and relaxed
above-knee boxing shorts. No exposed midriff, cleavage,
or chest-focused tailoring/shading. Keep the existing
palette, gloves, headband, boots, backpack, and style.
The user accepted the r2 direction overall with these
mandatory coverage changes and authorized continuing
to one equipment-swap proof without another approval
gate. This does not pre-approve the new r3 artwork.

Character animation is outside the MVP: no breathing,
micro-movements, bobbing, rigging, mesh deformation or
per-item animation. Do not make motion tests a prerequisite
for character art or equipment work. Do not generate full
animation sets per body, item or outfit combination.
Preserve existing idle experiments as future references;
the earlier shared-breathing proposal is deferred, not
the next task. Revisit motion only as a separate later scope.

Build customization in this order: body base, independent
equipment assets, local composition. Keep one canonical
static body base per supported preset; freeze its pose,
canvas and checksum before fitting equipment. The user's
2026-09-27 base exception permits plain boxer briefs on
men (no top), and plain matching bottoms plus an opaque
sports bra on women. These minimal, non-removable base
garments may be baked into the body so later gear covers
them. Keep them unadorned, with bare feet and hands and
no equipment/accessories. All additional clothing and
accessories remain separate assets. Do not use a fully
dressed outfit as the production base.

For equipment work, never call create_character or
upload_character, and never use generate_pose to redraw
the wearer in an outfit. Generate/save the isolated item
through the asset tools, with no person or stray body
parts. Do not create character records as clothing-edit
references, even if uploading them is free. Existing
full-outfit renders are historical references only; new
outfit previews must compose the base and assets locally.

Equipment edits change only that item's files and fit
metadata. Equipping/removing it must leave the base checksum
unchanged. If the provider cannot produce a usable isolated
item, report that limitation instead of substituting a
fully dressed character. The user authorized the five
static frontal body bases per gender before more equipment.
Reuse valid exports and track credits; shapes may need asset-fit
variants. The old dressed-base glove proof is not eligible
as the canonical production base.

All ten frontal bases in assets/sprites/autosprite/body-bases-front-r1/
were explicitly approved on 2026-09-27. The batch manifest and
reviews/user-approval-all-10.json pin the approved source hashes.
Preserve these originals. Transparent exports must derive from
those exact images without redrawing, repositioning or resizing
the body. Use each frozen export's canvas and checksum for future
equipment fitting; approval of the body does not verify item fit.

The user subsequently authorized hair-free base revisions and separate
hairstyles on 2026-09-27. Keep these under body-bases-hairfree-r1/
without replacing the approved originals. Reference-based body revisions
may use generate_pose and free body-reference uploads for this explicit
hair-removal task only; they are not clothing-generation references.
Generated revisions need their own review because the provider can redraw
details. Hair is a separate cosmetic layer, independent of the head
equipment slot. Generate hairstyle items with asset tools, never by
generating another dressed body.

The latest review is basic-clothing-front-r1/reviews/
user-review-r2-2026-09-27T170140Z.json, recorded in review-status-r2.json.
It approves all ten hair-free bodies and all ten hair fits. Their exact PNGs,
canvas and hair placements are frozen in body-bases-hairfree-r1/
approved-base-hair-lock.json. These are the canonical modular bases for
subsequent clothing work; preserve the older approved originals as references.
Do not reopen the muscular man's old helmet note: its body and separate
hair-fit decisions are explicitly approved in this review.

Only male slim and male medium clothing fits are currently approved; eight
request changes. Keep the medium-vs-slim similarity note attached to its
approval. The four standalone asset-design decisions remain pending.
Revision 2's single top sprite scaled across every body was rejected as a
general fitting approach. Use separately authored garment fit variants that
preserve the kit's colors, panels and trim while following the wearer's actual
shoulders, waist, torso volume and folds. Female cuts must preserve the approved
female silhouettes with full coverage. Heavier bodies need rounded drape and
proportional shorts; muscular clothing must retain visible build differences.
Do not substitute stretching, body regeneration, exposed skin, or a different
outfit design for correct fit. Follow docs/design/boxing-equipment-fit-r3.md.

Revision 2 remains tools/review/modular-character-front-r2.html and
basic-clothing-front-r1/fitting-r2.json as reviewed evidence. It used zero
additional credits. R3 candidates now live under basic-clothing-fit-r3/ and
tools/review/garment-fit-r3.html: five new isolated garment exports, eight
revised clothing fits pending review, and two unchanged approved male fits.
Keep r2 as reviewed evidence. R3 records 12 confirmed credits and one possible
charge for a timed-out preview. Do not treat candidate art as approved.
Body, clothing-fit, hair-fit and asset-design decisions are separate in the
v2 review export. Keep both submitted reviews and their exact fitting snapshots.
Do not regenerate approved originals or generate outfit combinations.
Run tools/verify_static_body_bases.py and
tools/verify_modular_character_assets.py to check source preservation,
item separation, fit hashes and costs before extending this set.

Run tools/verify_fitted_clothing_r3.py when editing the r3 garment batch.

## Current MVP handoff (2026-09-27)

The user accepted the existing r3 garments for temporary MVP use and explicitly
asked to stop spending time on art polish. This supersedes the earlier pending
clothing gate for runtime integration, not the historical quality-review records.
Record known sleeve problems (muscular man, muscular woman, most women) and
inconsistent male garment designs for later. Do not generate more art or block
fitness-app work on these issues. See basic-clothing-fit-r3/mvp-adoption.json.

Default customization and Home now use static modular art with four builds per
gender, independent hairstyles and top/shorts toggles. Save appearance locally
on Continue; Back discards the draft. No account save is claimed. Preserve the
reviewed body/hair hashes, separate assets and shared framing across builds.
Run tools/sync_modular_avatar.py to verify Unity copies, or --sync after an
explicitly authorized asset/fit change. Existing auth and fitness setup gates
remain; cosmetic saving must not imply that training is medically cleared.

## User-authored character assets

The user is now creating character assets and requirements in the image UI.
The handoff is design/character-handoff/mvp-rework-r1/ (rendered references,
written briefs, exact old prompts and eight fitting bodies). Wait for the user's
replacement assets before replacing the current MVP art. Do not initiate more
character or equipment generation unless the user asks for it. Help with image
requirements, source preservation, import and fitting. Reference-only concept
details must never be reported as finished isolated equipment assets.

The user clarified that Codex should first render visual equipment references
with a consistent design and distinct body-build cuts, which the user can take
into AutoSprite's image UI. The reference pack is
`design/character-handoff/visual-design-r2/`. Built-in imagegen is authorized
for this concept/reference stage; final wearable assets remain separate items
in the AutoSprite asset pipeline. Do not return only old images and text prompts
when the user asks for new rendered design references. A comparison sheet is
reference material, not one wearable asset or a verified body fit. Preserve
colors, panel construction and trim across cuts; change tailoring, not zoom.
AutoSprite image upload is documented; exact reference-guided redraw is not
verified. Never promise that generation preserves all pixels or automatically
aligns a garment to a body. Current MVP art remains unchanged until replacements
are ready, and this does not authorize spending AutoSprite credits in this pass.

For the visual references, the user requested more obvious belly volume on both
overweight cuts. Use the selected `top-male-belly-r2.png` and
`top-female-belly-r2.png` sheets: full rounded abdomen, less waist taper and
belly-driven cloth drape, not only larger hips/chest. This reference correction
does not change the approved body PNGs or establish runtime fit.

The user rejected contact sheets as generation inputs. Deliver ONE isolated
asset per PNG, with one garment/body fit per file and no labels or comparison
grids. Use `design/character-handoff/individual-assets-r3/` as the handoff.
The single-reference image and its written brief must be directly downloadable;
do not ask the user to crop a sheet or instruct a provider to select a quadrant.

AutoSprite's asset description field is limited to 200 characters. Every asset
handoff must provide a validated <=200-character description with its own copy
button and character count. Keep the detailed art brief separate and clearly
label it as unsuitable for that field; never make the long brief the default
AutoSprite description. Include short descriptions in downloadable packs.
