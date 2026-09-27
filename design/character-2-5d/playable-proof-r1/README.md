# Illustrated character playable proof r1

This bounded Unity proof derives the normal male and normal female from the
fourteen approved appearance masters. Source masters and their approval hashes
remain immutable. This directory's derived artwork needs its own visual review.

**User review:** idle received positive feedback. Walking and jabbing require
substantial re-authoring, including stride phases, shoulder continuity and
clothing separation. They are retained as experiments, not approved gameplay
animations. See [the recorded feedback](user-feedback.json) and
[the merge checkpoint](../../../docs/design/character-creation-checkpoint.md).

## What this proof establishes

- Four authored projections: male/female × studio/elevated gameplay.
- Eight coherent transparent surfaces: base/equipped in each projection.
- Pixel-registered joints, ground anchors and semantic skin/hair regions.
- A continuous weighted torso/leg mesh and two weighted arm regions from the
  same illustration, procedural idle/walk/jab previews, tint controls and a
  complete outfit state swap inside the existing game character studio.

The equipment proof uses complete registered surfaces. It does not establish
independent mix-and-match tops, trousers, gloves, new hairstyles, hidden joint
continuation for arbitrary motions, eight-direction locomotion or release-ready
animation. The elevated projection is authored artwork, not a rotated front view.

## Reproducible source chain

`manifest.json` records each generated source, exact approved reference hash,
exact prompt path, tool and resulting hash. Generated outputs were copied without
pixel edits. Undisclosed generator model IDs and seeds are explicitly unknown.
The built-in imagegen tool was used.

The runtime copies live in
`app/Assets/SoloGym/Resources/AvatarIllustrated/`. Their import settings preserve
original dimensions, alpha and uncompressed color. Registration uses top-left
cell coordinates, source-pixel pivots and named bones; `_l`/`_r` denote screen
sides. `tools/character_illustrated/register_proof.py` records the measured
landmarks and regenerates `Registration.json` without modifying any image.
The adjacent `male_regions.json` and `female_regions.json` retain pixel-space
anatomical boundaries. Arms have exclusive source ownership and cannot influence
torso/trouser vertices. The torso and legs keep shared topology across the hips.
`cloth_underlaps.json` records the small clothing continuations exposed by moving
hands. Each uses an explicit target polygon and a nearby source UV offset within
the same painted garment, follows the body rig, and renders beneath the character.
Far and near arms have explicit draw order for these authored projections.
Arm meshes use cropped, dense geometry shared by the UI and isolated captures.
The full jab uses a smooth, source-measured upper-arm volume correction, and
validation rejects inverted visible arm triangles. Disconnected clothing pixels
inside an arm boundary stay with the body. These corrections support this bounded
proof; they do not replace painting complete hidden joint surfaces for production.

Each derived image must pass alpha, registration, recoloring, rest-pose and
motion review. Changing a source image invalidates its registration and requires
a new revision. Outfit and base surfaces share the same bone names and animation
logic, while retaining their measured source coordinates.

## Open the interactive proof

From the repository root:

```bash
app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer illustrated
```

Use Body to choose man/woman, Skin and Hair for colors, Gear for base/equipped,
and the view button for studio/gameplay. Motion buttons play idle, walk or jab;
pause freezes the current pose. This preview does not save a legacy character
recipe. `-sologym-capture PATH.png` saves a screenshot and keeps the preview open.

Deterministic flags include `-sologym-character-presentation female`,
`-sologym-character-camera gameplay`, `-sologym-character-outfit base`,
`-sologym-character-skin deep`, `-sologym-character-hair silver`,
`-sologym-character-action jab`, `-sologym-character-time 0.25` and
`-sologym-character-freeze 1`.

## Validation

```bash
python3 tools/verify_character_art_batch.py
xvfb-run -a -s '-screen 0 1200x2000x24' \
  python3 tools/verify_illustrated_character_proof.py
```

The runtime verifier captures isolated character states and deterministic motion
samples using the real renderer. Reports distinguish structural validity from
visual acceptance. Review the full mobile studio captures, all tint/outfit
states, and complete motion loops before extending this method to other presets.
The capture matrix covers both base and equipped motion: 16 static states and
24 sequences of 24 frames, with one video per sequence. The local review page
lets the reviewer switch between both outfits, characters and views.
Any visible face distortion, partial skin tint, joint pinch, foot slide, garment
stretch or silhouette mismatch remains an open production issue even when a
structural check passes.

The final production pass must author clean shoulder continuations and garment
coverage behind moving hands. Additional body presets need their own measured
fits and the same review matrix; sharing bone names does not imply that artwork
or equipment can be stretched automatically across all fourteen bodies.
