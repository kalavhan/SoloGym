# Home accents R2 evidence

Unity 6000.3.24f1 Linux player with software OpenGL under private Xvfb displays,
2026-09-30. Build method: `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`.

| Capture | Safe inset | Character | Smoke |
| --- | --- | --- | --- |
| 1280×720, es | 0px | male-medium | 194/194 |
| 854×480, es | 24px | male-medium | 194/194 |
| 1844×853, en | 64px | female-fat | 194/194 |

Each size includes the assembled room and four captures with only the named
new accent hidden: trophy, book, bottle or towel. All other objects remain visible.
The per-check reports contain zero failures, including support/overlap ordering.
The twelve shown/hidden comparisons in `decor-visibility-audit.json` confine
changes above 10 RGB levels to the toggled object's projected bounds, allowing
two pixels for raster rounding.

Reproduce with `tools/check_home_ring_captures.py --prop trophy --captures artifacts/visual/HomeDecorR2`
and repeat for `book`, `bottle`, `towel`. Source/hash/import/alpha checks pass via
`tools/check_home_furniture_assets.py --batch decor-r2`. Other room/prop diagnostic
captures remain under ignored local artifacts.

All three assembled views were inspected for transparency, depth, proportions
and safe-area bounds. The trophy rests on the shelf, the book occupies the desk,
and bottle/towel sit on the bench; the towel's hanging portion overlays its front.
Placement remains provisional. Existing furniture perspective mismatches are
unresolved and are not certified by these technical checks.

This is a room integration fixture, not final Home UI approval or physical mobile
performance evidence. Next work is the user's alignment editor, then the live Home
interface. Additional decorative detail can wait until after the placement pass.
