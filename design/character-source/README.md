# Shared character source

**Art-direction status:** this is a technical 3D experiment. The user rejected its generic mannequin appearance and reaffirmed the original illustrated 2.5D target with seven discrete body presets. It is not approved character art. Use `-sologym-avatar-renderer source3d` to inspect it explicitly; the source rig may still be useful for pose/depth guides and repeatable exports. This first experiment includes one limited muscle morph, no fatness control, and unfinished hair/materials.

This is the first reproducible character source for the customization screen and
the angled gameplay camera. Both views use the same anatomical mesh, skeleton,
appearance slots and body-shape data. Body pieces are no longer independent
generated images fitted with screen offsets.

The current result is an engineering and art proof. Automated checks establish
data consistency; actual Blender and Unity renders establish visual correctness.
Keep visual review pending until joints, silhouette, clothing and motion have
been inspected from both cameras.

## Rebuild the source

Run commands from the repository root. The local tool is portable **Blender 4.5.9
LTS**, build `8bf95cbd38d1`, at this exact path:

```bash
artifacts/local/blender/blender-4.5.9-linux-x64/blender \
  --background --python tools/character_source/build_character.py -- --render

python3 tools/character_source/validate_export.py \
  --require-lineage --check-source-hash --self-test \
  --report artifacts/visual/CharacterSource3D/export-validation.json
```

Omit `-- --render` for export without the Blender preview. Outputs:

- `SoloGymAthlete-v1.blend`: editable mesh objects, shared armature, shape keys,
  materials, lights and a reproducible studio camera.
- `export-manifest.json`: source revision, Blender version, counts and coordinates.
- `app/Assets/SoloGym/Resources/AvatarSource3D/Character.json`: runtime geometry,
  normalized weights, hierarchy, shape deltas and appearance metadata.
- `artifacts/visual/CharacterSource3D/blender-source-front.png`: optional preview.

The portable Blender archive was downloaded from the [official release
server](https://download.blender.org/release/Blender4.5/blender-4.5.9-linux-x64.tar.xz)
and verified against its official SHA-256:

```text
dcdc3eca6c9825bb35a8033b689c053f3cb5a9b0cd2a61b2eac2a49436b4ad3d
```

The checksum listing is retained at
`artifacts/local/blender/blender-4.5.9.sha256`. The archive was removed after
extraction to conserve disk space. The binary is a local tool, not a game asset.

## Authoring and provenance

The authoritative inputs are `tools/character_source/build_character.py`,
`tools/character_source/prepare_base.py` and the pinned graphical data under
`vendor/makehuman/`. The builder starts a clean scene and regenerates the `.blend`
and runtime export. **Manual edits made only in the `.blend` are overwritten by
the next rebuild.** Encode retained changes in the builder before regenerating.
An independent exporter for manually maintained `.blend` files is not part of
this first proof.

The base mesh, body targets, rig, skin weights and eye source are official
MakeHuman core graphical assets under CC0-1.0, pinned to commit
`a8bc2d54ff0ac92e78ff71431b1023eda42bf482`. `vendor/makehuman/SOURCE.json` records
the exact upstream paths, download URLs, byte counts and SHA-256 hashes.
Original license files and per-file notices are retained. No MakeHuman
application code is vendored. See [vendor provenance](vendor/makehuman/README.md)
and the [upstream asset license](https://github.com/makehumancommunity/makehuman/blob/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/LICENSE.md).

`prepare_base.py` reads the graphical formats directly. It preserves all source
indices, applies the adult male and muscle targets, reduces the original rig,
fits the eye geometry to the same morph and converts to meters. Source helpers
support fitting and joint placement; only the body face group is rendered.

The builder applies one continuous proportion adjustment and one rest-pose
change to body geometry, shape endpoints, eye helpers and joint locations. It
authors the fitted outfit, shoes, gloves and hair in that coordinate space.
Both exports and the editable scene are translated together so the shoe soles
meet ground Y=0.

## Stable contract

- Runtime coordinates are Unity meters: X character-left, Y up, Z forward.
  Blender converts `(x, y, z)` to `(x, -z, y)`.
- Skeleton ID is `sologym_humanoid_22_v1`. Bone names, order and parent links are
  fixed. Runtime bones contain parent-relative positions and rotations.
- Every mesh uses this skeleton, with no more than four normalized influences
  per vertex. Clothing never creates a separate copy of the body skeleton.
- `sourceVertexIndices` records direct body lineage; `-1` identifies authored
  geometry. `fitMode` distinguishes body-derived and authored weighted meshes.
  The validator compares body-derived weights to the pinned master data.
- `Athletic` is a shared shape control between source muscle strengths 0.25 and
  1.0. Body-derived clothing carries the corresponding vertex deltas. This is
  one appearance control, not a mapping from fitness measurements.
- Slots are `body`, `top`, `hands` and `hair`. Hair variants are `spiky` and
  `swept`. `hideWithSlot` masks covered skin when a corresponding item is worn.
- Every mesh supplies base normals and shape arrays, including zero deltas for
  rigid attachments. Material IDs describe the surface role, allowing shared
  runtime color changes without repainting separate pieces.

The source revision hashes the builder, base reader, vendor manifest and pinned
Blender version. The validator rejects exports stale relative to those inputs
and checks that the export manifest and runtime data agree. Exact artifact
SHA-256 values identify the runtime file tested.

## Add fitted gear

1. Start with the existing master body at the fixed bind pose. For close-fitting
   clothing, derive a surface patch or fit geometry to that surface; preserve
   original indices where there is a direct correspondence.
2. Copy or transfer weights from the master, retaining the same bone names.
   A rigid attachment uses its shared attachment bone. Normalize weights after
   transfer and retain at most four influences.
3. Fit the item at both body-shape endpoints. Derive matching shape deltas and
   retain an appropriate amount of clearance. Fit collars, cuffs and seams to
   their garment surface rather than position them independently at runtime.
4. Assign a slot, material role, optional variant and covered-body mask. Keep
   enough skin around openings that coverage masks do not expose holes.
5. Regenerate, validate and inspect the garment on the actual character at both
   shape endpoints. Check front, side, back, studio and gameplay views, then
   idle, walk and jab. Review bare and equipped states together.

Use AI for concept references, scripts, controlled edits and review assistance.
Keep dimensions, geometry, bones, cameras, weights and export metadata in these
deterministic sources. A new generated image is not a registered clothing asset.

## Verify the Unity result

After rebuilding the source, rebuild the player so it contains the new data:

```bash
/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.SoloGymBuild.BuildLinux -quit \
  -logFile /tmp/sologym-character-build.log

xvfb-run -a -s '-screen 0 1200x2000x24' python3 tools/verify_source_character.py
```

The runtime proof captures bare and equipped views, both cameras, appearance
variants, body-shape endpoints and motion. It also checks that the player uses
the workspace export hash. Review the resulting PNGs; a structural pass does not
approve their art. For interactive review:

```bash
app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer source3d
```

## Current limits

The 22-bone proof combines source facial and finger bones into head and hand
bones. It supports body motion but not detailed expressions or finger posing.
It contains one masculine base and one muscle control; additional body families
need their own fitted clothing and validation. The illustrated material style,
hair, footwear and glove silhouettes remain authored proof assets. Collision
clearance during motion, body-part intersections, visual seams and animation
quality require render inspection and can require further weight or mesh edits.
This proof does not establish final mobile performance or a complete asset
catalogue.
