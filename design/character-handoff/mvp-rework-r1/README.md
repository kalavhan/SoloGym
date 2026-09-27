# SoloGym character art — rework pack

Open **index.html** for rendered references, written briefs, exact old prompts,
and one-click reference downloads. This pack contains existing images only;
no new art was generated and no generation credits were spent.

## How the current clothes were made

The current separate tops and shorts were requested with **text descriptions**
using AutoSprite `generate_asset_preview` (`category: item`, `style: anime`,
`quality: ultra`). Its schema at the time accepted a description of at most
200 characters and did not accept reference images. I did **not** attach the
wearer or the original garment image to those requests. I saved selected
results with `create_asset`, removed backgrounds with `remove_asset_background`,
and fitted the PNGs locally using manually chosen crop/placement rectangles
and neck/hand occlusion masks. Scaling did not change the cut of the clothes.
The independent requests did not lock design, sleeve angle or proportions;
that explains the inconsistent cuts and some of the fit problems.

The old glove prototype came from an earlier reference-edit experiment. Its
fully dressed wearer and fitting coordinates are historical only. It is an
isolated image reference, not a verified glove for the current bodies.

## Use the UI image attachments

For each garment, supply these references by role:

1. **Design:** `style/base-kit-concept.png`. Use the kit's palette, materials and
   garment construction. It is a flattened concept, not a body base or equipment
   layer. Choose one collar/panel/trim design and keep it consistent across cuts.
2. **Individual object:** the relevant PNG from `objects/`. These are actual
   existing isolated renders. Known sleeve/design defects are not constraints to
   reproduce. Headband and boots do not yet have isolated renders. The backpack
   only has a product inset in the concept; its wearable layers do not exist.
3. **Wearer, when fitting:** one exact PNG from `bodies/`. Its pose, camera,
   anatomy, canvas and scale are the placement authority. Do not redraw it.

The requested **written reference** is under each item in the page and in
`REQUIREMENTS.md`. `prompts/` has copy-ready descriptions for each item/body
combination. The 56 text files are templates, **not 56 required generations**.
They do not imply that all accessories need a separate drawing for each body.

## Efficient order

Choose a single final Base top design and fix its sleeves, first on male medium
and female medium. Use that same design when making the other body fits. Finish
the Base shorts next. Gloves, boots, headband and backpack complete the six-slot
kit, but are not prerequisites for further fitness-app work. Reuse an accessory
only when it actually fits the other bodies; do not stretch it into a new shape.

Keep **slim, medium, overweight and muscular** for both genders. No obese, no new
body generation, no animation, no isometric views. Existing body/hair artwork is
approved; replacing it is optional, not a missing MVP deliverable.

## What to send back

Transparent lossless PNGs plus the item name, slot, body preset and part (left,
right, front or rear where relevant). Preferred size is **1024×1024**, aligned to
the unchanged wearer canvas. Keep the full canvas; leave everything except the
item transparent. Do not bake the wearer into the export. Keep collar/armhole
openings transparent where the original skin must remain visible. A separate
on-body preview can help inspection, but it is not the equipment deliverable.

For a left/right pair, “left” means **wearer's left**, which appears on the
viewer's right in the frontal pose. A paired PNG is also acceptable if both
parts stay aligned to the body canvas. Keep the editable source if the UI allows.
If a tool cannot preserve the canvas, send the untrimmed source and its original
size too; fitting can be done afterward, but it will need a visual check.

Suggested name: `torso__female-medium__base__v2.png`. Use the filename suggested
by each item brief for split gloves, boots or backpack parts. You do not need to
write engine coordinates or modify JSON manually.

## Current implementation and later work

The app currently supports the eight static bodies, two hair choices plus no
hair, and top/shorts toggles with local saving. The original images in this pack
are preserved with source paths and hashes in `manifest.json`.

New gloves, boots, headband, backpack layers, front/rear long hair, item icons,
and higher tiers will still need fitting/import and game-side integration. A
render alone does not add those features. Skin-tone variants and separate facial
features are optional future scope; neither is implemented as modular artwork.
No art in this pack is a rig or an animation sheet.
