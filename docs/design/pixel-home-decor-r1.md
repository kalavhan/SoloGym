# Home iteration 6: lantern, torch, banner and plant

This four-sprite batch uses the existing `PixelRoomObject` component. Each object
has its own exact PNG, identity, anchor, pivot, occupancy envelope and draw order.

| Object | Identity / slot | Layer | Review toggle |
| --- | --- | --- | --- |
| Hanging lantern | `home.hanging-lantern` / `lantern.hook` | Behind character | 5 / `-sologym-hide-lantern` |
| Wall torch | `home.wall-torch` / `torch.wall` | Behind character | 6 / `-sologym-hide-torch` |
| Banner | `home.banner` / `banner.wall` | Behind character | 7 / `-sologym-hide-banner` |
| Potted plant | `home.potted-plant` / `plant.floor` | Foreground | 8 / `-sologym-hide-plant` |

Number keys refer to the top row. Existing F2–F10 and 1–4 review controls remain.
The new wall/ceiling objects draw before the furniture; the foreground plant can
overlap the rack. These are review toggles, not product decoration controls or an
account save. All four objects are static. Existing anchors/artwork stay unchanged.

The lantern pivot is its top suspension ring, the banner pivot its mounting loop,
the torch pivot its backplate, and the plant pivot its pot bottom. Full source
canvases remain intact. Custom pivots and uniform scaling place the objects;
the runtime does not crop, warp or stretch each axis independently.

## Perspective feedback and authoring rule

After merging PR #24, the user flagged mismatched object perspectives and positions.
Treat these as two separate issues. A placement editor can help with position and
scale; a sprite with the wrong visible faces or foreshortening needs art correction.
Earlier merged furniture is not visually approved on that basis.

This batch uses the actual `RefugeR1/RoomShell.png` as the first image reference;
`design/fantasy-home-r2/home-approved.png` supplies the desired design and materials.
Both reference hashes are preserved. Prompts describe the object's location/height
instead of prescribing a generic rotated isometric view for every object:

- Ceiling/wall decorations stay upright and near frontal, with little visible top
  surface. The banner's crossbar stays horizontal.
- The floor plant shows a shallow pot rim/soil view consistent with a lower object.
- Warm materials and light direction follow the Home concept. A flame inside an
  object does not authorize painting a wall, cast halo or scene into its export.

This is visual guidance, not a calibrated 3D camera or a guarantee that generated
art is perspective-correct. Existing ring/furniture/rug camera mismatches remain
for a scoped art pass. Keep placement rough until the user has the promised manual
editor; do not spend repeated iterations aligning coordinates before that pass.

## Review and verification

Build with `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`, then launch the
[Home review player](pixel-home-room.md). The [single-image handoffs](../../design/fantasy-home-decor-r1/index.html)
include the full briefs. [Provenance](../../assets/sprites/autosprite/home-decor-r1/README.md)
records four built-in imagegen calls, exact AutoSprite exports and zero AutoSprite
credits. Character sources, room shell and previous props are preserved.

The shared smoke suite covers independent visibility/save restoration, depth,
point sampling and stable placement/proportions after resizing for all four new
objects. Read-only source audits verify PNG byte identity, alpha gaps, near-opaque
interiors, hashes and import settings. Capture comparisons must confine a prop's
visibility change to its own projected bounds.

```sh
python3 tools/check_home_furniture_assets.py --batch decor-r1
python3 tools/check_home_ring_captures.py --prop lantern --captures artifacts/visual/HomeDecorR1
```

Repeat the capture comparison for `torch`, `banner` and `plant`. Runtime results
and three landscape compositions are recorded under `artifacts/visual/HomeDecorR1/`.
Unity 6000.3.24f1 Linux build succeeded. **161/161 smoke checks passed at each**
of 1280×720, 854×480 with 24px safe insets, and 1844×853 with 64px insets. The wide
capture uses the existing female-fat Barbarian. All three assembled views were
inspected; all twelve new-object shown/hidden comparisons pass. The new source
audit and both prior furniture-batch audits pass.

PR #25 was merged. The [second decoration batch](pixel-home-decor-r2.md) adds the
trophy, open book, training bottle and towel. Visual approval, perspective
correction, physical mobile testing, the alignment editor and live Home interface
remain pending. Prioritize the editor after these core accents.
