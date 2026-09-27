# Illustrated 2.5D character direction

The character must preserve the original **Kai manhwa illustration** and its
game presentation. The user rejected the generic rendered 3D look. The shared
3D source remains an optional anatomy, rig and camera reference; its successful
technical checks do not make its appearance an accepted art target.

The user approved the illustrated style of
[`illustrated-style-r1.png`](../../design/character-2-5d/references/illustrated-style-r1.png)
and requested all seven presets for both men and women for review. That image
is the style target; its body reads as muscular and must not define the
`normal` preset. The original
[`KaiPortrait-v1.png`](../../app/Assets/SoloGym/Resources/Art/KaiPortrait-v1.png)
remains the male identity reference. Preserve the stylized eyes, hair masses,
drawn contours, authored shadows and material highlights across full-body and
elevated gameplay views. Keep the existing game menu direction; this work
concerns the character and its registered parts.

The machine-readable [style lock](../../design/character-2-5d/style-lock.json)
records the fixed references, revision policy and comparison requirements.
The [fourteen-board manifest](../../design/character-2-5d/approval-r1/manifest.json)
records which requested candidates actually exist, their hashes and prompts.
The user's [final approval](../../design/character-2-5d/approval-r1/reviews/user-approval-all-14.json)
approves **all fourteen selected appearances**, including `male-chubby` r3,
`male-fat` r3 and `female-fat` r4. There are no remaining art corrections. The
female identity is approved through `female-normal` r1. The manifest records
the exact approved source revisions and SHA-256 hashes; preserve those files
unchanged. Future derived assets must not overwrite the approved masters or
inherit approval merely by reusing an asset ID.

Keep the [first exported review](../../design/character-2-5d/approval-r1/reviews/user-review-2026-09-26T233529Z.json)
and its [chat supplement](../../design/character-2-5d/approval-r1/reviews/user-review-supplement-2026-09-26.json),
plus the [second exported review](../../design/character-2-5d/approval-r1/reviews/user-review-2026-09-26T235342Z.json),
unchanged as history. Appearance approval does not establish production
sprites, fitted layers or animation.

## Accepted body controls

Use seven **discrete presets**, with the user's labels preserved verbatim:

| Stable ID | Label | Visual requirement |
| --- | --- | --- |
| `skinny` | skinny | Narrow frame and limbs; low visible muscle volume. |
| `normal` | normal | Moderate body volume, softly flat abdomen and ordinary limb volume; no carved six-pack or bodybuilder shoulders. |
| `chubby` | chubby | Fuller, softer torso and limbs. |
| `fat` | fat | Large visible body-fat volume; broad, soft torso and limbs. |
| `skinny_muscular` | skinny muscular | Narrow silhouette with visible muscular form and definition. |
| `muscular` | muscular | Large, visibly developed muscle volume. |
| `fat_muscular` | fat muscular | **Large fat mass and visible muscular build together.** Show a substantial soft belly and fuller waist alongside thick muscular shoulders and limbs; not merely a bulky bodybuilder. |

These describe appearance only. In particular, “normal” is the user's label,
not a health classification. Do not infer a preset from measurements or attach
health, fitness, ability, judgment, workout prescriptions or combat stats.

Apply the user's **SoloGym-specific female proportion direction** when reading
the preset descriptions: female muscle volume stays smaller than the
corresponding male preset. Arms and legs must remain proportionate to the head
and body, with restrained definition rather than oversized bodybuilding limbs.
This is the chosen art direction, not a universal statement about anatomy or
biology.

In review, the user rejected the amplified `female-fat_muscular` r2 arms and
legs as oversized, specifically noting arms that appeared bigger than the
head. Keep this preset's substantial soft belly, full waist and softer legs;
use moderate strength cues to distinguish it from `fat`. Do not solve that
distinction by adding more shoulder, upper-arm or leg bulk. The `female-muscular`
preset likewise needs restrained muscle volume and definition. This feedback
supersedes the earlier review suggestion to amplify the female muscular forms.

The approved masters incorporate these historical feedback rules:

- Nonmuscular skinny/chubby arms should be softer, with less muscular form and
  definition. Keep the already approved female skinny source unchanged.
- Male normal should be mildly fuller, without visible abs.
- Male fat should have a fuller soft chest and a larger belly.
- Male fat muscular should retain both traits with smaller, proportionate arms.
- Female fat should have softer, slightly slimmer arms while keeping a
  protruding belly; do not flatten the stomach while slimming the arms.
- Female fat muscular should have a modestly smaller bust while preserving its
  established body shape and restrained muscle treatment.
- Female muscular uses moderate muscle volume with proportionate limbs and
  less muscle volume than the male muscular preset.

The final accepted corrections reduced actual contours, not only shading:

- Male chubby: narrower upper arms than the rejected r2.
- Male fat: narrower arms and soft chest volume instead of muscular pectoral
  forms, retaining the full belly.
- Female fat: smaller, softer thighs and calves, retaining her fat silhouette
  and protruding belly.

These achieved rules guide later derived assets. They are not instructions to
continue revising any of the fourteen approved masters.

The fourteen presentation/preset combinations in
[the authoring contract](../../data/art/avatar-contract.json) have proposed fit
IDs and board asset IDs. Appearance decisions are tracked separately from
production fit readiness; all fourteen source appearances are approved, while
none is selectable or runtime usable. A board is a drawing of a fit, not an
exported production fit. Existing proof IDs stay separate; do not silently
rename the athletic proof to one of these presets.

## Shared identity and clothing fit

- Preserve one recognizable adult identity per presentation across all seven
  presets. The male character keeps Kai's short, spiky black hair. The approved
  female character uses one consistent adult face, black high ponytail and
  spiky flyaways. Do not make each preset a different person. Intentional
  changes in facial fullness may follow a body preset.
- Keep warm medium skin, charcoal/black clothing, cyan accents and silver
  hardware for this comparison. Lighting, linework and material finish stay
  constant. Later skin/hair recolors must preserve the same authored shadows
  and highlights through registered material masks.
- Begin each preset with one coherent full-body master. Shoulder, neck, torso,
  hip and limb anatomy must connect before clothing or separate parts are made.
  Do not generate independent production arms, legs or collars.
- Fit each outfit to that preset's actual body and angle. Maintain the same
  garment design, seam placement and material language while adapting its
  volume. A wider body needs a fitted drawing, not a stretched normal-body shirt.
- Production equipment needs an authored fit for every supported body/view
  combination. Its layers, masks and animation frames share the body's
  registration, pose and frame coordinates. Static approval boards establish
  the intended appearance; they are not rig assets or interchangeable layers.
- Face features belong on one registered head surface. Hair uses that head's
  silhouette and attachment anchors. Keep skin, hair, iris and clothing masks
  separate so recoloring preserves linework and authored shading.
- Covered-body masks must follow the garment openings. Preserve hidden skin
  continuation at shoulders, elbows, knees and collars; no hollow joint caps,
  duplicate necks, disconnected limbs or exposed gaps.

## Fourteen-preset review batch

`approval-r1` contains the requested male and female version of each preset.
Every candidate uses the same three-panel comparison:

1. **Base body:** studio view with charcoal shorts for men, and a charcoal
   sports bra plus shorts for women. The drawing exposes enough torso and limb
   form to judge the intended body preset without equipment hiding it.
2. **Equipped body:** the same body and studio pose with the same high-neck
   black top, dark pants, gloves and boots, cyan accents and silver hardware.
   Clothing adapts to the body while keeping a recognizable shared design.
3. **Gameplay view:** that equipped character in an elevated three-quarter
   orthographic-style view, targeting 30 degrees of elevation. This is an
   illustrated view proposal; an image-generation prompt does not prove an
   exact camera matrix.

Keep the full head, hairstyle and feet in frame. Hold the studio baseline,
framing and nominal character height constant across the set. Do not enlarge a
skinny body to fill its panel or shrink a fat body to disguise its width. Any
contact sheet must use one uniform transform for all source boards; its layout
does not change the saved originals. Compare base and equipped panels to catch
clothing that silently changes body size. Check the seven silhouettes together
so adjacent presets remain distinct.

The manifest is the evidence of rendering, not this list of requested assets.
Do not mark an image as generated, reviewed or approved before its corresponding
file and review record exist. The user requested visual approval of this set;
that request does not require extra confirmation for making the review boards.

## Stable references and later production

The saved reference PNGs and their SHA-256 hashes are authoritative. Prompts
describe how a revision was made; reusing a prompt or a seed does not guarantee
the same pixels. Keep accepted revisions immutable. Create `r2`, `r3`, and so on
for changes and retain the earlier files and review records. Derive future
hairstyles, outfits and body views from the saved identity and body masters;
do not regenerate an accepted master merely to obtain another accessory.

For each generated candidate, retain its exact prompt, source references,
available generator/model metadata, output checksum and review state. Do not
invent a model identifier or seed when the image tool does not expose one.
The female identity anchor is the saved, byte-identical copy of the approved
normal female r1 source. Its approval comes from the recorded user decision,
not merely from repeated use across the batch. Preserve that anchor. Record
later decisions against exact source revisions and hashes instead of treating
silence or a filename as acceptance.

The appearance review is complete. Later production needs registered body, hair,
skin and clothing layers from the selected coherent masters. Show side/back
checks and hidden skin continuation. Test idle, walk and jab in both the studio
and gameplay representations, inspecting joints, overlap, foot contact and
facial readability. Extend to the eight proposed gameplay directions only when
the initial views hold together; each direction needs its own authored
projection and occlusion rules. A successful static illustration alone does
not prove skin recoloring, equipment interchangeability or motion quality.

## 2D mesh and registration rules

Use one versioned bind pose per fit/view and stable bone/socket names. A shared
anatomical structure does not require identical joint coordinates across all
body sizes. Bind every deforming layer to the appropriate preset's joints;
rigid accessories use explicit sockets. Keep hidden overlap art beneath joints
and enough mesh support for smooth bends without stretching painted anatomy.

Record the source revision, preset/fit ID, direction, camera, canvas size,
source rectangle, trim offset, pixel pivot and ground anchor. Choose these
settings once for each export family and hold them fixed across its body,
clothing, masks and animation passes. Trimming must preserve the original
registration. Never repair a source mismatch with unrelated runtime scale or
offset adjustments for individual limbs.

The gameplay angle needs an authored elevated projection. Rotating or mirroring
a frontal cutout does not create a new view. Mirroring is allowed only when the
asset explicitly supports it. Depth ordering and occlusion can change by view
and frame; an arm can cross in front of or behind clothing.

Automated checks should verify registered metadata, compatible skeleton/fit IDs,
skin coverage, valid mesh weights and consistent export dimensions. Visual
checks must still inspect silhouettes, painted shading, intersections and motion
in the game. Production readiness remains false until those assets and exports
exist and the resulting character is accepted visually.
