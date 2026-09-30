# Four-sprite Home furniture evidence

Unity 6000.3.24f1 Linux player, software OpenGL under Xvfb, 2026-09-30.
Build method: `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`.

| Capture | Safe inset | Character | Smoke |
| --- | --- | --- | --- |
| 1280×720, es | 0px | male-medium | 96/96 |
| 854×480, es | 24px | male-medium | 96/96 |
| 1844×853, en | 64px | female-fat | 96/96 |

Each size has the assembled room and four captures with only the named new
prop hidden: rack, bench, bed and chest. All other props remain visible.
The per-resolution JSON records every smoke check. `furniture-visibility-audit.json`
records twelve shown/hidden pixel comparisons, each bounded to the relevant
prop's projected image area. Reproduce a comparison with:

```sh
python3 tools/check_home_ring_captures.py --prop rack --captures artifacts/visual/HomeFurniture
```

Repeat with `bench`, `bed` and `chest`. Additional room/ring/bag diagnostic
captures and build logs remain local; existing runtime smoke checks still cover
them. Original source audits also pass.

All three final compositions were inspected. Object positions/scales are
deliberately provisional for the user's later manual alignment pass. This is
the Home integration fixture, not a finished screen or a physical-device test.
