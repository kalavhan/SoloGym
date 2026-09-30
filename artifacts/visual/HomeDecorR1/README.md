# Home decorations R1 evidence

Unity 6000.3.24f1 Linux player with software OpenGL under private Xvfb displays,
2026-09-30. Build method: `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`.

| Capture | Safe inset | Character | Smoke |
| --- | --- | --- | --- |
| 1280×720, es | 0px | male-medium | 161/161 |
| 854×480, es | 24px | male-medium | 161/161 |
| 1844×853, en | 64px | female-fat | 161/161 |

Each size has the assembled room and four captures with only the named new
object hidden: lantern, torch, banner or plant. All other props remain visible.
Per-check JSON reports record zero failures. The visibility audit records twelve
comparisons: changes exceeding 10 RGB levels stay within the toggled object's
projected image bounds, with two pixels allowed for raster rounding.

Reproduce with `tools/check_home_ring_captures.py --prop lantern --captures artifacts/visual/HomeDecorR1`
and repeat for `torch`, `banner`, `plant`. The source audit passes for this batch;
both prior furniture batches also pass the generalized source audit. Older-prop
and room diagnostic captures remain in ignored local artifacts.

All three assembled views were inspected for transparency, depth, proportions
and safe-area bounds. New wall/ceiling objects are upright and the plant retains
its pot silhouette. Earlier furniture still visibly differs in perspective from
the approved composition; this batch does not resolve that art issue. Position
and scale remain deliberately rough for the user's manual alignment pass.

These are integration captures, not final screen approval or physical mobile
performance evidence. The live Home interface and placement editor remain pending.
