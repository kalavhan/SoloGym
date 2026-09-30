# Home iteration 4: four independent furniture sprites

The user changed room-art delivery to **four sprites per MR** on 2026-09-30.
This batch adds a weight rack, training bench, bed and storage chest through
the existing `PixelRoomObject` component. No new placement framework is needed.

| Object | Identity / slot | Layer | Review toggle |
| --- | --- | --- | --- |
| Weight rack | `home.weight-rack` / `rack.floor` | Foreground | F7 / `-sologym-hide-rack` |
| Training bench | `home.training-bench` / `bench.floor` | Foreground, after rack | F8 / `-sologym-hide-bench` |
| Bed | `home.bed` / `bed.floor` | Behind character | F9 / `-sologym-hide-bed` |
| Storage chest | `home.storage-chest` / `chest.floor` | Behind character, after bed | F10 / `-sologym-hide-chest` |

Each item has its own sprite, identity, floor pivot, occupancy quadrilateral,
uniform scale and saved visibility/variant state. Only the static base variant
is authored. The ring, hanging bag, architecture, exterior and eight user-created
Barbarians are unchanged. Existing F2–F6 review controls still apply.

**These are rough placements.** The user will do a manual alignment pass after
the room items exist. The future editor must support individual position/scale
adjustment, reference comparison and layout export/import. Current fixed-slot
state is not a placement editor or account/inventory persistence.

Build with `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`, then run the existing
`app/Builds/FantasyHomeRoom/SoloGymHomeRoom.x86_64` landscape review player.
See [room commands](pixel-home-room.md).

## Artwork

[Four single-object handoffs](../../design/fantasy-home-furniture-r1/index.html)
include an image, copyable short description, detailed brief and exact prompts
for each sprite. The [source pack](../../assets/sprites/autosprite/home-furniture-r1/README.md)
preserves originals, AutoSprite exports and hashes. Built-in imagegen used five
calls, including the rack correction; saving existing transparent art through
AutoSprite used **0 credits**. Original, export and runtime PNGs are byte-identical.

## Verification

Run `python3 tools/check_home_furniture_assets.py` for source preservation,
alpha gaps and import settings. The Home player smoke checks also verify that
each item can hide/restore independently and retains its pivot/proportions
after parent resizing. Capture comparisons cover actual shown/hidden pixels.
Evidence belongs under `artifacts/visual/HomeFurniture/`.

Unity 6000.3.24f1 Linux build succeeded. **96/96 checks pass at each** of
1280×720, 854×480 with 24px safe insets, and 1844×853 with 64px insets.
The wide capture uses the existing female-fat Barbarian. All three compositions
were visually inspected; source audits and all twelve furniture shown/hidden
comparisons pass. Larger pixel differences (over 10 RGB levels) stay within the
independently toggled object's projected bounds.

Visual approval and physical mobile testing remain pending. Live Home UI and
further room objects are still separate work. The next [four-sprite batch](pixel-home-furniture-r2.md)
adds desk, stool, shelf and rug.
