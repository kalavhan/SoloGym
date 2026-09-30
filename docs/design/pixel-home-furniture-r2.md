# Home iteration 5: desk, stool, shelf and rug

The second four-sprite batch adds independently exported room objects through
the existing `PixelRoomObject` component.

| Object | Identity / slot | Layer | Review toggle |
| --- | --- | --- | --- |
| Desk | `home.writing-desk` / `desk.floor` | Behind character | 1 / `-sologym-hide-desk` |
| Stool | `home.stool` / `stool.floor` | Foreground | 2 / `-sologym-hide-stool` |
| Wall shelf | `home.wall-shelf` / `shelf.wall` | Behind character | 3 / `-sologym-hide-shelf` |
| Rug | `home.floor-rug` / `rug.floor` | Behind all props and character | 4 / `-sologym-hide-rug` |

Number keys refer to the keyboard's top row. F2–F10 retain their existing
review functions. One new `stool.floor` slot is added; existing anchors,
sprites and character sources stay unchanged. The desk and shelf are bare
so books, lamps and other decorations can be independent assets.

The rug is created last but sorts first within the rear prop layer, below
the character and all current props. No extra Canvas or raster composition
is required. The shelf uses a wall occupancy envelope; its pivot and all
other prop sizes/positions are provisional for the user's manual alignment
pass. These controls are for review, not a placement editor or account save.

Build with `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`, then use the
[Home review player](pixel-home-room.md). [Single-object handoffs](../../design/fantasy-home-furniture-r2/index.html)
contain an image, short copyable description and detailed brief for each.
[Source provenance](../../assets/sprites/autosprite/home-furniture-r2/README.md)
records four built-in imagegen calls and **0 AutoSprite credits**; original,
export and runtime bytes match.

## Verification

The existing smoke suite now includes these four objects' independent
visibility/save restoration, depth, transparency imports and unchanged pivot/
proportions after parent resizing. An additional check verifies rug ordering
independent of creation order. Evidence is under `artifacts/visual/HomeFurnitureR2/`.

```sh
python3 tools/check_home_furniture_assets.py --batch r2
python3 tools/check_home_ring_captures.py --prop desk --captures artifacts/visual/HomeFurnitureR2
```

Repeat the capture comparison with `stool`, `shelf` and `rug`.
Unity 6000.3.24f1 Linux build succeeded. **129/129 smoke checks pass at each**
of 1280×720, 854×480 with 24px safe insets, and 1844×853 with 64px insets.
The wide capture uses the existing female-fat Barbarian. All three assembled
compositions were visually inspected. Both furniture-batch source audits,
the room source audit and all twelve new-prop shown/hidden comparisons pass.
Visual approval, manual alignment and physical mobile testing remain pending.

The next four-sprite batch is a hanging lantern, wall torch, banner and potted
plant. Desk/shelf decorations and the live Home interface are also still pending.
