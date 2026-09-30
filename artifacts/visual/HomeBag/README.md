# Home hanging-bag review evidence

Unity 6000.3.24f1 Linux player, software OpenGL under Xvfb, 2026-09-30.
Build method: `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`.

| Capture | Safe inset | Character | Smoke |
| --- | --- | --- | --- |
| 1280×720, es | 0px | male-medium | 64/64 |
| 854×480, es | 24px | male-medium | 64/64 |
| 1844×853, en | 64px | female-fat | 64/64 |

Each resolution has the assembled room and two independent visibility checks:
`-bag-hidden.png` and `-ring-hidden.png`. The other prop stays visible.
JSON reports contain individual checks; the two visibility audit reports
compare shown/hidden pixel differences against the projected artwork bounds.
Room-only diagnostic captures and build logs remain local.

All three compositions were inspected for alpha, independent object rendering,
safe-area bounds and proportions. Position and scale are deliberately provisional
for the user's later manual alignment pass. These captures do not approve the
final composition or prove physical phone performance. This is the Home review
fixture; the full Home interface and product routing are still pending.
