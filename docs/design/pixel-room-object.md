# Home iteration 2: training ring and room-object contract

The Home review now places one independently exported training ring at the
approved `ring.floor` anchor. It is drawn behind the character on its own Image,
without changing the architecture or exterior. F5 hides/shows the ring, and
`-sologym-hide-ring` starts with it hidden. Other [Home review commands](pixel-home-room.md)
still apply. This is the next component-sized Home iteration; the full Home
screen and product route are not finished.

`PixelRoomObject` loads a definition and creates a noninteractive, point-filtered
sprite. The definition fixes the stable object/room IDs, supported placement
slots, canvas size, pivot, uniform scale, projected quadrilateral footprint,
layer and draw order. The scene has explicit behind-character and foreground
prop layers; ties within a layer sort by object ID. The ring uses the former.

`SaveState` / `RestoreState` round-trip a versioned JSON value containing object,
room, slot, variant and visibility. They do not write to the user's account,
PlayerPrefs or an inventory. Unsupported versions/identities/slots/variants are
rejected before mutating the displayed prop. Duplicate item IDs and occupied
slots are rejected before creating another GameObject, including hidden items.
This MVP component uses fixed supported slots, not a drag editor or arbitrary
free placement. Footprints are exposed as copies for later placement logic;
collision handling is not implemented here.

Variants share the same definition and must load sprites with the same canvas
and pivot. Changing a variant leaves the room identity, placement and footprint
unchanged. Only the base artwork is registered; this PR does not claim that a
winter/snow version has been authored or tested visually.

The [source and export record](../../assets/sprites/autosprite/home-ring-r1/README.md)
documents built-in imagegen authoring followed by AutoSprite background removal
(1 credit). The ring's RGB is unchanged; source colors, dimensions and exported
PNG bytes are preserved. Thin gaps between ropes were checked for actual alpha.
The character sprites are unchanged.

## Verification

The existing Home player smoke checks now also exercise independent ring
visibility, save/restore, immutable footprint copies, atomic invalid-state
rejection, unsupported variant rejection, duplicate prevention, depth order,
and anchor/aspect preservation after resizing the parent. Evidence belongs in
`artifacts/visual/HomeRing/`; asset audits are `tools/check_home_ring_assets.py`
and the existing `tools/check_home_room_assets.py`.

Unity 6000.3.24f1 Linux build succeeded. **54/54 checks pass at each** of
1280×720, 854×480 with 24px safe insets, and 1844×853 with 64px insets. The wide
capture uses the existing female-fat Barbarian. All three final compositions
were inspected. Both source audits pass. `tools/check_home_ring_captures.py`
compares actual shown/hidden captures at each size and confirms changes above
10 RGB levels are confined to the prop's projected image bounds.

Resizing now explicitly invalidates the room's pixel-adjusted vertex geometry;
otherwise a later visibility change could rebuild it at the new scale and shift
unrelated pixels. The capture sequence also waits for a full layout/render cycle.
The runtime vertex-stability regression and screenshot comparisons cover this.
No global quality/filtering override or per-prop Canvas is needed.

Visual approval and physical mobile testing remain pending. The ring is a
reconstructed standalone object rather than a pixel-exact crop of the concept.

## Next Home piece

Add the hanging punching bag as another independent prop outside the ring,
at `bag.hook`, reusing this room-object contract. Further furniture, lamps, rug
and live UI remain separate work. Keep comparing the assembled Home against
the approved reference as those objects arrive.
