# Approved Home target — 2026-09-30

The user approved [this render](home-approved.png), then explicitly required the
background and buttons to remain separate. This is the visual target for Home.
The [manifest](manifest.json) preserves approval, hashes and scope; [prompts](prompts.json)
record the built-in imagegen calls. The initial version is retained in `iterations/`.

## Composition to preserve

The personal gym fills the screen. The character stands on the central rug with
clear space around the silhouette. Small profile/title controls stay at the upper
corners. A compact routine card and the main Train action occupy the lower right.
The short navigation dock sits at the bottom center; Decorate is at bottom left.
Match this hierarchy, relative scale, spacing, materials and colors when building
the real window. Verify assembled screenshots against this image, not just isolated
control galleries. Keep landscape safe-area behavior at other aspect ratios.

## Required separation

| Layer | Independent assets or components |
| --- | --- |
| Room architecture | Floor, walls, ceiling beams and window opening; no UI, character or movable props baked in |
| Exterior | Castle/sky view behind the window; a separate layer for later environment/theme changes |
| Training objects | Ring, hanging bag, weight rack and bench, each with its own identity, sprite and placement |
| Home furniture | Bed, storage chest, desk/stool and shelving, independently placed |
| Decorations | Rug, banners, plants, lamps and tabletop/shelf objects where they need independent theme or placement changes |
| Character | Existing approved Barbarian sprite, feet anchored to the room floor; no regeneration to imitate this render |
| UI artwork | Panel/frame materials, primary-button skin, dock skin and individual navigation/settings/decorate icons |
| UI behavior and text | Native uGUI controls with live localized labels, profile values, routine values, selection/focus/press/disabled states |

Never import `home-approved.png` as an in-game background, a clickable image map or
a complete screen texture. It is a reference. The project does not yet possess the
independent production assets depicted here. A transparent cutout with hidden holes
is not a complete prop: overlap/occluded areas need proper source artwork.

A clean room-shell image may contain fixed architecture, but must exclude the
character, interface and independently themeable/placeable objects. Props retain
stable IDs, pivots, footprints, placement anchors and draw-order rules so a seasonal
skin changes the same object rather than replacing the whole room composition.

## Controls and domain boundaries

- `MI REFUGIO`: localized title, separate from its decorative plaque.
- Profile HUD: real profile values; the sample portrait/name/level/bar are illustrative.
- Settings: independent icon button with an accessible name.
- Routine card: bind an actual accepted plan; `Cuerpo completo · 25 min` is sample
  content, not a new hardcoded prescription.
- `ENTRENAR`: workout entry action retaining readiness and session rules.
- Navigation: Home / Workouts / Dungeon; Fasting appears only for eligible adults
  who enabled it. This render depicts that optional adult state.
- Decorate: an independent action; placement/inventory work retains the room-object
  contract rather than editing a screenshot.

The functionality built in component PRs can be reused, but those review fixtures
are not the approved final presentation. PR #19's existing tab strip is not a visual
substitute for this dock. Rework and integrate controls against this Home target.
No further isolated-component visual approval should be inferred from this render.

## Delivery focus

Prioritize matching this Home window. The user updated room-art delivery on
2026-09-30 to four independent sprites per MR. Other UI component changes remain
component-sized. Keep object placement rough until the user's later manual pass
with an alignment editor after room items exist. First prepare
the room shell and establish the composition; then place independently exported
props and the exact existing character, and fit the live HUD/actions/navigation.
Use AutoSprite for production sprite work and separate review; the approved concept
is not a license to generate new characters or animations. No new character colors,
clothing layers, breathing or gameplay animations are part of this Home pass.

Each delivery needs an assembled Home capture at the reference aspect ratio and a
smaller/wider landscape check. Differences should be corrected against the approved
reference before progressing to unrelated windows. Art decomposition, production
exports and the implemented Home are not claimed complete by this document.

## First implementation checkpoint

The [room environment iteration](../../docs/design/pixel-home-room.md) now
provides independently imported architecture and exterior references, plus the
existing character in the Home composition. These assets are pending visual
review. Furniture, independent lamps/decorations and the live HUD remain to do.
