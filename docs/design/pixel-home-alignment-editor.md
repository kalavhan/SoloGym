# Home alignment editor

This local authoring tool lets the user place all 18 existing room objects and
the shared character viewport against the approved Home reference. It reads the
same catalog, sprite definitions and PNGs as Unity. It is a desktop review tool;
the app's Home presentation remains native Unity uGUI.

## Open and align

From this checkout:

```sh
python3 tools/review/serve_home_alignment.py --port 8903
```

Open <http://127.0.0.1:8903/>. The server binds only to loopback and serves an
explicit list of editor files, reference artwork and runtime sprite inputs.
It has no file-writing endpoint. A broad repository web server is unnecessary.

1. Click a visible sprite or choose **Objeto**, including hidden objects.
2. Drag it, enter **X/Y**, or use arrows while the room is focused. Shift+arrow
   moves 10 reference pixels; an ordinary arrow moves one.
3. Adjust **Tamaño (%)** with one proportional scale control (25–300%). The
   authored pivot stays fixed while resizing; there is no independent stretch.
4. **Mover con su contenido** carries the bottle/towel with the bench, book with
   the desk, and trophy with the shelf. It also scales their relative offsets.
   Turn it off to adjust a support or accessory independently. Visibility and
   layer edits are individual; use **Capa** and **Orden en la capa** to arrange
   overlap. A higher order appears above a lower one within the same layer.
5. Compare **Superponer referencia** at adjustable opacity or **Solo referencia**.
   The reference includes its painted UI and is not imported into the player.
6. Use **Deshacer/Rehacer** for grouped transactions. **Restablecer objeto** uses
   its initial placement and can carry contents; **Restablecer distribución**
   resets the whole room after confirmation and can also be undone.
7. **Exportar JSON** prepares a download and exposes the complete JSON for
   copying. If the browser does not download it, use **Copiar JSON** and save it
   as `mi-refugio.json` or share the text. The panel also accepts pasted imports.
   **Importar JSON** opens an existing file. Invalid imports leave the room intact.

A draft is saved in this browser/origin's local storage. Export before clearing
browser data or moving to another port. Undo history lasts only for the current
page session. The eight character previews share the same placement; previewing
another body does not change the saved character or include an appearance choice
in the layout. A layout file contains object placement, not account data.

## Load the placement in Unity

Build the [Home review player](pixel-home-room.md), then launch from the checkout:

```sh
app/Builds/FantasyHomeRoom/SoloGymHomeRoom.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-room-layout "/absolute/path/mi-refugio.json"
```

The command reads the file once at startup. An invalid file exits with code 2
and reports the reason in the Unity log. Omitting the argument retains the
original provisional layout. Nothing is silently written into `room.json`,
PlayerPrefs, inventory or the user's account. Applying an approved layout as the
product default is a later, reviewable integration step.

## Shared contract

`Resources/Rooms/RefugeR1/objects.json` lists each definition, authoring label and
optional support-object identity. Both the browser and Unity consume it.
Positions use the room's 1672×941 reference plane, top-left origin, X right and Y
down. Prop pivots retain Unity's normalized bottom-left convention; the conversion
is explicit in both renderers. The scale multiplies each item's authored source
scale. No PNG, source pivot or footprint is changed by an edit.

The portable document is `format: sologym-home-layout`, `version: 1`. It includes
room identity/reference size, exactly one version-2 state per current object,
and the character's feet position, scale and visibility. An object's state keeps
its stable object/room/slot/variant IDs and adds placement, layer and draw order.
`PixelRoomObject` also accepts legacy version-1 state with fixed slot placement.

Imports validate all object identities, variants, layers and bounded footprints,
and the shared character envelope, before applying anything. Uniform scale is
limited to 0.25–3 and order to integer −10000…10000. Files are limited to 200 KB
in the editor. Unity additionally caps the input JSON string at 200,000 characters.
Adding another room object intentionally makes old complete-layout files
incompatible until migrated; missing objects are not silently assigned positions.

Collision handling, room navigation and perspective warping are not part of this
tool. Position/scale/depth cannot correct camera angles drawn into the sprites.
The existing perspective art pass remains pending. This PR generates no artwork
and consumes no AutoSprite credits. Future room art batches are eight independent
sprites per PR, per the user's latest direction.

## Verification

- `node tools/tests/home-layout.test.mjs`: 22 checks covering grouped transforms,
  bounds/identity rejection, undo/redo, hidden states and JSON round-trips.
- Unity 6000.3.24f1 Linux build succeeds. New layout checks pass 59/59 at each of
  1280×720, 854×480 with 24px safe insets, and 1844×853 with 64px insets.
- An actual copied browser export with fractional coordinates also passes 59/59
  in the Unity player. Existing default Home checks pass 194/194 at 1280×720.
- Browser checks cover grouped resizing, numeric placement, restored drafts,
  file import, copyable export, rejected JSON, undo and reference overlay.
  A file-download event could not be confirmed in the in-app browser; the copied
  JSON was verified through the clipboard and loaded in Unity.

Evidence is in `artifacts/visual/HomeAlignment/`. These layouts are verification
snapshots, not a replacement for the user's final reviewed placement.
