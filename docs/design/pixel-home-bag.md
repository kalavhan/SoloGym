# Home iteration 3: hanging punching bag

Adds one independent hook/chain/bag assembly outside the training ring, using
the existing `PixelRoomObject` component. It has its own transparent sprite,
`home.hanging-bag` identity, `bag.hook` slot, top attachment pivot and visibility
state. It draws behind the character and after the ring on the prop layer.

**Placement and scale are provisional.** The user requested rough placement now
and will do a manual pass in an alignment editor after all items exist.
The editor is deferred; no manual placement controls are claimed in this PR.
The current fixed-slot state remains unchanged.

The bag's quadrilateral describes projected assembly occupancy, not a floor
contact area or physical collision shape. Variants must retain its canvas and
pivot. Only the static base variant exists.

## Review

Build with `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`, then run:

```sh
app/Builds/FantasyHomeRoom/SoloGymHomeRoom.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720
```

F6 toggles the bag; `-sologym-hide-bag` starts it hidden. F5 still toggles the ring.
All other [room review options](pixel-home-room.md) remain available.
These are review controls, not inventory or account persistence.

The [source/export pack](../../assets/sprites/autosprite/home-bag-r1/README.md)
records one built-in imagegen call and 1 AutoSprite background-removal credit.
The existing character and ring artwork are unchanged. The
[handoff](../../design/fantasy-home-bag-r1/index.html) includes a single-object
image, copyable description under 200 characters, detailed brief and prompts.

## Verification

Evidence is recorded in `artifacts/visual/HomeBag/`. Smoke checks cover the
top attachment after parent resizing, aspect preservation, independent
visibility/save restoration and rejection of a floor-slot assignment.
The existing room/ring/character checks still run. Capture comparisons check
both props independently, with the other item visible:

```sh
python3 tools/check_home_room_assets.py
python3 tools/check_home_ring_assets.py
python3 tools/check_home_bag_assets.py
python3 tools/check_home_ring_captures.py --prop ring --captures artifacts/visual/HomeBag
python3 tools/check_home_ring_captures.py --prop bag --captures artifacts/visual/HomeBag
```

Unity 6000.3.24f1 Linux build succeeded. **64/64 smoke checks pass at each** of
1280×720, 854×480 with 24px safe insets, and 1844×853 with 64px insets.
The wide capture uses the existing female-fat Barbarian. All three compositions
were inspected; both independent-prop capture audits and all source audits pass.
The screenshot checks allow up to 10 RGB levels of rounding and confine larger
changes to the toggled prop's projected bounds.

Visual approval and physical mobile testing remain pending. In particular, the
bag's hook and scale need the user's later placement pass; its current location
is not a finished ceiling attachment. Further furniture, room objects and live
Home UI remain separate work; the next prop is the weight rack.
