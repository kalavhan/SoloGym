# Home furniture batch R2 evidence

Unity 6000.3.24f1 Linux player with software OpenGL under Xvfb, 2026-09-30.
Build method: `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`.

| Capture | Safe inset | Character | Smoke |
| --- | --- | --- | --- |
| 1280×720, es | 0px | male-medium | 129/129 |
| 854×480, es | 24px | male-medium | 129/129 |
| 1844×853, en | 64px | female-fat | 129/129 |

Each size includes the assembled room and four captures with only the named
new prop hidden: desk, stool, shelf or rug. All other props remain visible.
JSON reports contain individual smoke checks. The visibility audit records
twelve comparisons: changes exceeding 10 RGB levels stay inside the toggled
prop's projected image bounds, allowing two pixels for raster rounding.

Reproduce with `tools/check_home_ring_captures.py --prop desk --captures artifacts/visual/HomeFurnitureR2`
and repeat for `stool`, `shelf`, `rug`. Both furniture source-batch audits and
the room source audit pass. Older-prop and room diagnostic captures remain local.

The three assembled views were inspected for alpha, depth order, proportions
and safe-area bounds. Placement and scale are deliberately rough for the user's
manual alignment pass. These are integration-review captures, not final screen
approval or physical mobile performance evidence. The live Home UI is still pending.
