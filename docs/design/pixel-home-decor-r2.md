# Home iteration 7: trophy, book, bottle and towel

Four independent accents fill the existing shelf, desk and bench. Each uses
`PixelRoomObject`, exact exported artwork, its own stable ID/slot, custom pivot,
uniform scale and visibility/save state. Existing props and characters are unchanged.

| Object | Identity / slot | Surface / layer | Review toggle |
| --- | --- | --- | --- |
| Trophy | `home.trophy` / `trophy.shelf` | Shelf / behind character | 9 / `-sologym-hide-trophy` |
| Open book | `home.open-book` / `book.desk` | Desk / behind character | 0 / `-sologym-hide-book` |
| Training bottle | `home.training-bottle` / `bottle.bench` | Bench / foreground | Minus / `-sologym-hide-bottle` |
| Towel | `home.training-towel` / `towel.bench` | Bench / foreground | Equals / `-sologym-hide-towel` |

Keys refer to the top keyboard row. Existing review toggles remain available.
All accents draw after their supporting furniture, with the bottle above the
towel. These are isolated review controls, not inventory/product actions. Hiding
the bench or desk leaves its independently placed accents visible for inspection;
runtime support parenting/group moves are not implemented by this asset batch.

The bottle and trophy stand upright. The book has decorative, illegible marks,
not real exercise instructions. The towel has one continuous top flap and hanging
front, with no bench pixels baked into it. The towel pivot marks its front fold;
its footprint describes the cloth's projected envelope. All four sprites are static.

## Art and placement

The actual room shell supplies viewpoint guidance, and the approved Home concept
supplies materials/design. Existing desk and bench artwork also guide the book
and towel respectively. This preserves an explicit reference chain without
regenerating supporting furniture. [Prompts and one-image handoffs](../../design/fantasy-home-decor-r2/index.html)
include <=200-character copy descriptions; [provenance](../../assets/sprites/autosprite/home-decor-r2/README.md)
records four built-in imagegen calls and zero AutoSprite credits.

Positions/scales remain rough for the user's manual pass. Earlier furniture
perspective mismatches are still unresolved: a placement editor cannot change
visible faces or foreshortening baked into artwork. New reference-guided assets
also await visual approval; merged integrations are not that approval.

With the core room objects and these accents present, prioritize the alignment
editor next, before extending the decoration catalog. It should offer individual
selection, position/uniform scale, reference comparison and layout export/import.
Account persistence, support grouping and optional extra decor require explicit
implementation rather than being inferred from the review's fixed room slots.
The live Home HUD/actions/navigation follow the composition pass.

## Verification

Build with `SoloGym.Editor.PixelHomeRoomBuild.BuildLinux`; launch the
[Home review player](pixel-home-room.md). The shared smoke suite covers each
accent's independent visibility/save restore, depth, point sampling and stable
pivot/proportions after resizing. An additional check verifies that accents draw
over their supporting furniture and the bottle draws over the towel.

```sh
python3 tools/check_home_furniture_assets.py --batch decor-r2
python3 tools/check_home_ring_captures.py --prop trophy --captures artifacts/visual/HomeDecorR2
```

Repeat capture comparisons for `book`, `bottle` and `towel`. Evidence belongs under
`artifacts/visual/HomeDecorR2/`. Unity 6000.3.24f1 Linux build succeeded.
**194/194 smoke checks passed at each** of 1280×720, 854×480 with 24px safe insets,
and 1844×853 with 64px insets. The wide capture uses the existing female-fat
Barbarian. All three assembled views were inspected, the new source audit passes,
and all twelve new-accent shown/hidden comparisons pass.

Final visual approval and physical mobile testing remain pending; the review
scene is not the completed Home product route.
