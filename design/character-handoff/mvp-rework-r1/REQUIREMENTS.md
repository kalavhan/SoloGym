# Written art references and missing work

## Base design shared by every item

Stylish adult RPG boxer, crisp manhwa outlines and restrained cel shading.
Recognizable sportswear, not random fantasy armor. Matte navy/charcoal, cream
panels, small dull petrol accents; ordinary jersey, cotton, canvas and leather.
Same lighting, line thickness and design language across the kit. Static front
view. No glow, animation, logos, text, metal armor, sexualized tailoring or
extra weapons in the Base set.

The flattened concept is the **design reference**. Each hair-free body is the
**fitting reference**. They have different jobs. Use the same character canvas,
pose, head position and floor line; do not change the body to fit an item.

## Torso — rework first

**Design:** full-length opaque crew-neck short-sleeve tee under a simple open,
hip-length training vest. Cream side panels and restrained petrol piping. One
tee+vest combination occupies one torso slot; separating the two is unnecessary.
Choose one opening/collar/seam/panel design and preserve it across all cuts.

**Correction:** fix the pointed/flared sleeves and shoulder joins, especially
muscular male and almost every female. Sleeves follow the resting upper arm,
with natural armholes and enough cloth ease. Female cuts follow shoulders,
waist and hips without cleavage, exposed midriff or exaggerated bust shading.
Overweight cuts need belly volume and appropriate folds; muscular cuts need
room in shoulders/upper arms without changing anatomy. Do not simulate these
changes just by stretching a narrow cut.

**Deliverables:** eight fitted torso overlays (four builds per gender). The
current five isolated top images are provided as work-in-progress references.
They are not five different outfit designs to perpetuate.

## Legs — shorts refinement and fit

**Design:** relaxed navy boxing trunks ending above the knee; broad fabric
waistband, cream side stripe and narrow hem. Room for the thighs, ordinary
matte cloth, no high slits or exposed underlayers.

**Correction:** maintain one seam/stripe design; control waistband position,
hip room, crotch depth and leg opening. Avoid giant flared hems or stretched
trim. Check the overlap with the new top, including when it is removed. All
minimal baked underwear must be covered by the shorts.

**Deliverables:** eight fitted shorts overlays. Two current object renders
are supplied: regular and heavier. Reuse is valid only after fitting; the eight
outputs do not necessarily require eight AI generations.

## Hands — glove prototype, current fitting missing

**Design:** matched charcoal padded boxing gloves with rounded closed knuckles,
attached thumbs, cream wrist closures and restrained petrol details. Ordinary
leather, not gauntlets. Proportionate to the wearer.

**Correction:** the old isolated pair has no validated placements for the new
bodies. Match wrist angle and glove opening; fully cover the appropriate hand
silhouette without adding skin or requiring a new hand pose. Test both hands
on all eight wearers. Fingers or restored body patches must not appear on top
of the glove after integration.

**Deliverables:** one design, preferably wearer-left/right overlays. Reuse its
shape if it fits; author another cut only when required. One hands slot equips
the pair. Existing `gloves-prototype.png` is a real isolated reference.

## Feet — isolated boot artwork missing

**Design:** slim mid-calf lace-up boxing boots with navy canvas/leather panels,
cream laces, narrow cream accents and thin flexible soles. No high heels,
spikes, metal greaves or chunky tactical soles.

**Fit:** respect each foot's angle and ground contact. Cover bare toes, heels
and ankles; maintain believable width. The two feet are not guaranteed mirrors.
Check calves and nearby shorts. Start with one pair design, then verify reuse.

**Deliverables:** left/right wearable overlays. The only matching reference is
in the original outfit concept; there is no isolated boot PNG in this pack.
The old generic UI boot icon is not an equipment asset or the design authority.

## Head — isolated headband artwork missing

**Design:** narrow dark-petrol cloth headband, understated fabric folds, readable
at phone size. Preserve the character's face and hairstyle. No helmet or crown.

**Fit:** fit the forehead and curvature on each head; check no hair, crop and
sweep. Choose a consistent overlap with the fringe. If there are rear ties,
separate them only when layering demands it.

**Deliverables:** front band, optional rear tie layer, and per-head placement.
No isolated reference exists yet; use the headband detail of the concept.
Do not change or bake hair into the band.

## Back — product concept exists; wearable parts missing

**Design:** compact navy canvas gym backpack, cream zipper piping, muted petrol
front pocket. Practical shoulder straps; no towel, wings or tail in this Base
item. The concept's lower-right product inset shows the design.

**Fit:** the bag sits behind the torso. In front view, show only a plausible
outline around the torso and the straps on the chest/shoulders. Do not put the
product-front backpack on the chest. Straps should follow the chosen top/body
and remain distinct from vest seams.

**Deliverables:** rear bag PNG plus front-straps PNG. These equip together as
one back-slot item. Start with a shared bag shape; fit straps to the eight
wearers where needed. A finished full-backpack product illustration alone does
not supply those layers. Integration of these parts is still required.

## Hair and bodies — already present

Eight hair-free bases are included, with plain boxer briefs for men and plain
bottoms plus an opaque sports bra for women. These approved images already
work as the fitting bases. The obese references remain archived outside this
pack. Do not make new clothed body images for each item.

Close crop and side sweep are approved separate hairstyles, both included.
There is no mandatory hair rework for MVP. A future long hairstyle should have
rear lengths and front fringe separated when it crosses the shoulders. Each
new style needs checks against all head shapes; headband compatibility matters.
Changing a base pose/proportions would invalidate its current clothing fits.

## Optional assets and features not yet supplied

- Matching inventory thumbnails: one per final item design (six Base items),
  not one per body. Suggested 256×256 RGBA, item alone, no UI frame, rarity text
  or baked label. Derive these after the wearable design is settled.
- Additional hairstyles, hair colors, skin tones, tattoos, facial expressions
  or separate eyes/mouths: optional future scope. Current skin/face features are
  baked into the body. A global body tint also recolors underwear and shading;
  it is not a ready skin-customization solution.
- Rare, Ultra Rare and Premium: no coherent six-slot asset sets are ready.
  These are later design work, not current MVP blockers. Keep the same anatomy,
  fit and export rules. No animations are required for any tier at this stage.
- Isometric views, walk/jab animations and full rigs: explicitly deferred.
- Account synchronization: application work, not an art deliverable. The MVP
  currently saves only on this device.

| Tier | Written direction | Render availability |
| --- | --- | --- |
| Base | Matte ordinary sportswear, deliberate but simple construction, no glow. | Current partial kit and concept included. |
| Rare | More modern tailoring, refined materials, stronger deliberate styling. | No accepted complete set. |
| Ultra Rare | Distinctive silhouette/material details; optional restrained glow. | No accepted complete set. |
| Premium | Carefully designed otherworldly boxing identity, rich detail and glow; not Base clothes with glow added. | No accepted complete set. |

## Fit acceptance checklist

Inspect against the exact body on light and dark backgrounds, at 1024px and
phone size. Check silhouette, neckline, sleeve opening, wrist, waistband,
crotch, hems, footwear ground contact and back straps where relevant. Equipping
an item must not alter the body, hair or another item. No floating gaps, visible
base underwear, leaked limbs, fake transparency checkerboard, background box,
stray body fragments, clipped canvas edges or texture shadows covering unrelated
parts. Keep all overlays aligned to the wearer canvas and retain editable files.

A separate on-body preview is useful evidence; the actual deliverable must still
be the isolated transparent object or its named parts. Send the PNGs and body
IDs back; Codex can handle resource import, placement metadata, draw order and
runtime fitting checks afterward.
