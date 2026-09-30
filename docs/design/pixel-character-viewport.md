# Static character viewport

Component 07 displays the user's eight complete Barbarian appearances: female and
male, each skinny, medium, fat and muscular. It reuses the accepted panel, choice
and text-action controls in an offline landscape fixture. Body selection changes
appearance only; no fitness data, difficulty, progression or account state is
written. The smaller copy demonstrates reuse of the same viewport, not a finished
Home room or gameplay view.

## Artwork and framing

The [original-source manifest](../../assets/sprites/autosprite/barbarian-user-r1/manifest.json)
is preserved byte-for-byte from the separately prepared source batch. The
[derivative manifest](../../assets/sprites/autosprite/barbarian-viewport-r1/manifest.json)
records eight AutoSprite asset copies and background-removal results, at **8 credits
total**. No character generations or animations were requested. Every RGB pixel
matches the original; only alpha changed. Original PNGs remain unchanged, and
runtime PNGs are byte-identical to the transparent exports.

Each sprite has a metadata-only crop with two source pixels of padding and a feet
pivot. Its horizontal anchor is the midpoint of the boot extents in the last 64
source rows at alpha >=128; vertical anchor is the bottom of the full alpha bounds.
All eight use a shared maximum half-width/height envelope. `PixelCharacterViewport`
uses one scale factor for both axes and all appearances in a given viewport; a
skinny body stays narrow instead of expanding to fill the same width as a larger
body. Changing appearances keeps the feet in place. The anchor is eight logical
units above the viewport bottom, centered horizontally. The parent controls where
that viewport sits relative to a room floor.

Import uses a custom feet pivot, FullRect mesh, point filtering, clamp wrap, no
mipmaps, no compression and a 2048 texture limit (preserving each 1366×1366 PNG).
The transparent source crop is described in `Characters/BarbarianR1/catalog.json`;
its crop coordinates are bottom-left Unity coordinates. There is no animation atlas
because this is a static-image component.

These high-resolution sources are pixel-inspired artwork, not a newly authored
native low-resolution pixel grid. Point filtering and uniform downscaling preserve
the established look at the reviewed sizes but do not guarantee integer pixel
clusters at every device resolution. The compact sample intentionally has less
readable detail than the selection preview. The eight uncompressed RGBA source
textures occupy roughly 57 MiB if all are resident; the component loads selected
resources on demand but does not own global unloading. Mobile profiling, texture
packing/optimization and final device art approval remain later integration work.

## Component contract

```csharp
var viewport = PixelCharacterViewport.Create(parent);
viewport.Show("female-medium", "Bárbara, mujer, complexión media",
    "Personaje no disponible");
viewport.BindAccessibility(screenHierarchy);
```

`Show(id, localizedDescription, unavailableLabel)` binds one stable catalog ID and
returns success. Unknown IDs or missing/mismatched sprite resources clear the old
character and show the caller's localized fallback; stale artwork is not left on
screen. `Clear(label)` explicitly empties the view. `SetDescription(label)` updates
the accessible description without selecting a different character. All labels
must be nonempty localized text. The catalog is authored data, not player-editable
fitness state; callers must not mutate its entries.

The root is a clipped RectTransform. Resize it normally or call `RefreshLayout()`
after a manual parent layout. `SourcePixelScale`, `Character`, `HasCharacter`,
`CharacterImage` and `FeetWorld` support verification and integration. Insets are
reserved before scaling; extremely small containers collapse the image rather
than applying a negative scale. Consumer screens must still allocate legible space.

The viewport is noninteractive and its graphics do not intercept pointer events
or enter keyboard navigation. Reused choice controls own input; arrows/Tab move
focus and Enter/Space commits. One native accessibility image node describes the
current character; an unavailable view uses static text. Hidden viewports hide the
node, and clearing/rebinding the screen hierarchy is supported. Duplicate decorative
previews should not be announced twice. The screen owns the active hierarchy and
localized description. Linux metadata tests do not verify TalkBack/VoiceOver.

This component has no breathing, animation, recoloring, modular clothing, battle
behavior, profile persistence or entitlement handling. Existing production scenes
remain unchanged. The user-created source artwork is preserved for later screen
integration; the fixture is not final selection/Home approval.

## Build and review

Use Unity 6000.3.24f1 with Linux Standalone support from this checkout:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelCharacterBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/character-build.log"

app/Builds/FantasyCharacter/SoloGymCharacter.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-character female-medium
```

Any catalog ID can be passed to `-sologym-character`; the fixture defaults to
`female-medium` if this initial review argument is unknown. The component's own
unknown-ID behavior is the explicit unavailable state described above.

```bash
python3 tools/check_pixel_character_sources.py  # requires Pillow
python3 tools/check_pixel_field_keyboard.py --characters
XDG_CONFIG_HOME="$PWD/artifacts/local/character-review-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyCharacter/SoloGymCharacter.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-smoke -sologym-capture-all \
  -sologym-capture "$PWD/artifacts/visual/CharacterViewport/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/character-player.log"
```

`-sologym-smoke` saves a screenshot/report and exits with the verification result.
With smoke enabled, `-sologym-capture-all` saves one screenshot per appearance in
the sibling `roster/` directory. Capture-only mode stays open unless
`-sologym-quit-after-capture` is supplied. All keyboard injection uses private Xvfb.

## Verified evidence

Unity Linux build and checks passed on 2026-09-30.

| Profile | Safe inset | Result |
| --- | --- | --- |
| [1280×720 ES](../../artifacts/visual/CharacterViewport/1280x720-es.png) / [report](../../artifacts/visual/CharacterViewport/1280x720-es.smoke.json) | 0 | 223/223 |
| [1844×853 EN](../../artifacts/visual/CharacterViewport/1844x853-en.png) / [report](../../artifacts/visual/CharacterViewport/1844x853-en.smoke.json) | 64px | 223/223 |
| [854×480 ES](../../artifacts/visual/CharacterViewport/854x480-es.png) / [report](../../artifacts/visual/CharacterViewport/854x480-es.smoke.json) | 32px | 223/223 |
| [Real OS keyboard](../../artifacts/visual/CharacterViewport/keyboard.smoke.json) | 0 | 39/39 |
| [Source audit](../../artifacts/visual/CharacterViewport/source-verification.txt) | — | All eight hashes, RGB, alpha, framing and runtime copies |

Native pointer and submit checks switch every appearance in both viewport sizes.
They verify stable scale/feet, aspect ratio, complete containment, noninteractive
art, fallback/empty/reopen behavior, localized descriptions, accessibility lifecycle
and no idle movement. Narrow and short viewport sizing is also exercised. All three
profile captures and [all eight roster captures](../../artifacts/visual/CharacterViewport/roster/)
were visually inspected, including fur, hair, hands and boots against the dark
panel. Final user visual approval and mobile input/screen-reader/performance checks
remain pending.
