# Home iteration 1: independent room environment

This component starts assembling the [approved Home target](../../design/fantasy-home-r2/home-approved.png).
It supplies architecture and a replaceable castle view, with the exact existing
Barbarian in the planned floor position. It is a Home composition review scene,
not the finished Home screen or a replacement for the live app's Home route.

`PixelHomeRoom` fits a 1672×941 art plane uniformly inside its parent. Wide or
smaller viewports get a dark matte where needed instead of stretching the room
or cropping away placement anchors. `PixelHomeRoomReview` constrains that parent
to the safe area; a separate live-interface root is outside the scaled art plane.
The current art is crisp-filtered raster reference work, not an asserted fixed
native pixel grid. Fractional downscaling remains subject to physical phone review.

The draw order is exterior → architecture → occupants/furniture → foreground.
All room Images are non-raycasting. The architecture's real alpha opening reveals
the independently imported exterior. Hiding the character leaves both intact;
hiding the architecture exposes just the rectangular exterior. Buttons and text
will be live uGUI on the independent interface root when that Home iteration is
implemented. The full approved render is kept under `design/`, never imported
into the runtime asset tree.

The [room definition](../../app/Assets/SoloGym/Resources/Rooms/RefugeR1/room.json)
holds a stable room ID and named composition anchors in source-image pixels,
measured right/down from the upper-left. `hero.feet` matches the approved render's
floor position. Future prop artwork must fit its intended anchor and perspective;
the listed anchors do not claim that prop sprites or placement/inventory systems
already exist. Per-item footprints, pivots, seasonal variants and serialization
belong with the next room-object component.

All eight existing Barbarian sources use their common viewport scale and boot
anchor. `-sologym-character female-fat`, for example, selects that existing source
without generating or stretching it. The illustrated character inside the concept
is not a replacement for the user's actual artwork.

## Review

Build with Unity 6000.3.24f1:

```sh
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelHomeRoomBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/home-room-build.log"
```

From this checkout, launch:

```sh
app/Builds/FantasyHomeRoom/SoloGymHomeRoom.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720
```

F2 toggles the existing character, F3 the exterior, F4 the architecture. These are
desktop review shortcuts, not product controls. `-sologym-room-only` starts with
the character hidden. Automated captures accept the established `-sologym-smoke`,
`-sologym-capture /absolute/path.png`, `-sologym-safe-inset` and locale arguments.
The smoke run writes JSON and separate-layer captures before restoring the scene.

The next Home iteration should add the independent training ring using the room
object contract. Remaining props, HUD, routine binding, navigation styling and
product routing still need implementation against the approved target. None of
the old isolated galleries counts as final Home visual approval.

## Recorded verification

Unity Linux build succeeded. The focused player checks passed **35/35 at each**
of 1280×720, 854×480 with 24px simulated safe insets, and 1844×853 with 64px
insets. They exercise parent resizing, all eight existing characters, layer
visibility, aspect-ratio rejection and room/hero anchoring. The wide capture
uses the existing female-fat source. Screenshots and per-check JSON are under
`artifacts/visual/HomeRoom/`.

The read-only asset audit passes for hashes, unchanged runtime PNGs, sprite
imports, transparent window pixels and solid sampled architecture. Comparing
the actual 1280×720 room-only/exterior-hidden captures confines changes above
10 grayscale levels to `(672, 46)–(846, 305)`, inside the window opening. Desktop
captures were inspected at all three sizes. Physical mobile review and the
complete Home comparison remain pending.
