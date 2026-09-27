# Visual design references — R2
These are NEW images rendered with built-in imagegen on 2026-09-27 following the user's correction. The earlier handoff only collected existing references and text; this pack supplies the visual design first.

## What is inside
- top-male.png and top-female.png: ONE tee/vest design shown in slim, medium, overweight and muscular cuts.
- shorts-male.png and shorts-female.png: ONE shorts design shown in the same four cuts.
- gloves.png, boots.png, headband.png: standalone object design references.
- backpack.png: two views of ONE bag showing its outer face and wearer side.
- bodies/: eight unchanged approved fitting references.
- base-kit-concept.png: the historical Base style reference.
- prompts/: exact built-in generation prompts; manifest.json records provenance and source hashes.
The initial male top sheet is preserved as top-male-initial.png; use top-male-belly-r2.png. Use top-female-belly-r2.png for the female top. Earlier male/female sheets are preserved for comparison.

## Corrected workflow
1. Establish one rendered item design. Keep its panels, colors, edging and materials consistent.
2. Show deliberate cuts for actual wearer builds. Change shoulder, waist, hip and thigh relationships and cloth drape; do not just zoom/stretch.
3. In an image-guided creation UI, attach the new render as the DESIGN reference and the selected body as the FIT reference. Request only the selected garment. Do not generate a fully dressed character.
4. Inspect actual output against the chosen image and body. Image guidance improves specificity but does not guarantee exact copying, registration or transparency.
5. Export one isolated wearable item/part as a transparent RGBA PNG on the body's 1024x1024 canvas. Fit locally and compose with the unchanged body.

## AutoSprite: upload versus generation
Official Asset Library docs document uploading a base image. The Assets API documents direct upload and text generation as different operations.
- https://www.autosprite.io/docs/guide-asset-library
- https://www.autosprite.io/docs/api-assets

Uploading an existing isolated image preserves it as the asset source; it does not automatically create a correctly fitted overlay. The availability and fidelity of an image-guided *redraw* control in the user's UI has not been verified here. The written prompts on this page are instructions for an image-guided editor if one is available, not a claim that AutoSprite's text-only MCP takes image references.

Comparison boards are not upload-ready single equipment assets. For direct asset upload, first isolate the selected garment from its sheet and remove labels/background, or export that garment separately in your image editor. Do not upload a whole four-cut board as one wearable item. Likewise the backpack's two-view board is design reference, not the two gameplay layering parts.

## Written construction
Top: opaque full-length crew-neck tee under open hip-length vest, dark navy, cream side panels, narrow muted petrol piping, simple short sleeves. Preserve the same collar and panel map. Sleeves follow resting upper arms without pointed wings. Maintain modest coverage, natural cloth ease and avoid exaggerated chest shading.
Shorts: relaxed above-knee navy trunks, broad gathered waistband with two muted petrol end tabs, cream outer stripe and thin cream hem. Same design on every cut; adjust waist/hips/thighs and folds.
Gloves: rounded charcoal padded leather, attached thumbs, cream wrist cuff with plain petrol patch. Fit both wrists and hide underlying fingers.
Boots: navy mid-calf lace-up boxing boots, cream laces and side stripe, slim flexible sole. Fit actual left/right foot angles and floor contact.
Headband: plain muted petrol elastic cloth with subtle stitched edges and folds. Fit the forehead; hide its rear section behind the head. Check both hairstyles.
Backpack: compact navy bag, petrol front pocket, cream zipper trim and practical padded straps. In the frontal game view the bag is behind the body; straps go in front. The product outer face must not be pasted onto the chest.

## Fit targets
Slim: narrow shoulders/waist/hips with modest cloth volume.
Medium: moderate torso/hip volume and natural relaxed drape.
Overweight: rounded abdomen/hips, wider hem/waist and correct folds, not simply a larger zoom.
Muscular: broader shoulders/upper sleeves, tapered waist and greater thigh allowance. Female muscular proportions follow the approved female body, not the male volume.
The reference sheets suggest these shapes; they are not exact patterns or pixel-registered overlays. Fine drape and silhouette remain subject to visual review.

## Scope
Base tier only, no glow or animation. No new body or fully dressed character renders. No new hairstyles required. No AutoSprite requests or credits in this pass. Runtime artwork and approved body/hair sources unchanged. Eleven built-in image calls produced eight selected references plus three superseded top sheets. None is user-approved or fit-verified yet.


## User correction: fuller overweight bellies
The user requested clearer belly volume for both genders. The selected top-*-belly-r2.png sheets have rounder fuller abdomens, less waist taper, bowed vest openings and additional drape folds. Other fit designs remain the references from the preceding pass. This is not authorization to redraw the approved body bases.
