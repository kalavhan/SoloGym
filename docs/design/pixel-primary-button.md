# Primary pixel button

Component 01 implements one reusable native uGUI button and an isolated landscape review fixture. Its forest-teal surface, stepped antique-gold border and live ivory label begin the [fantasy component plan](fantasy-pixel-component-plan.md). This is a component review candidate, not a replacement login or Home screen.

The separate local screen-render pack and eight user-created Barbarian exports are outside this PR. Existing application scenes, auth behavior, character assets and training data are unchanged. The fixture does not make network requests, save player data or issue workout rewards.

## Component contract

[`PixelPrimaryButton`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelPrimaryButton.cs) derives from `UnityEngine.UI.Button`, so callers use normal uGUI navigation, `interactable`, `onClick` and parent `CanvasGroup` gates. It creates a rectangular touch target, sliced sprite face and separate label/icon children.

| API | Use |
| --- | --- |
| `PixelPrimaryButton.Create(parent, text, onClick)` | Create a button beneath an existing UI transform; default preferred size is 320×64 logical units |
| `SetLabel(text)` | Replace the live caption, including while loading; callers supply the localized text |
| `SetFontSize(size)` | Request a size from 20–48; fitting may reduce it to the readable 20-unit floor |
| `SetIcon(sprite)` | Add an optional separate icon; pass `null` to remove it |
| `SetLoading(value, loadingLabel)` | Show a live busy caption and block pointer/keyboard activation without changing the caller’s `interactable` value |
| `IsLoading`, `VisualState` | Inspect busy state and current visual state for binding/review |

Call `SetLoading(true, localizedCaption)` synchronously inside the action handler, before beginning an asynchronous operation. This guards subsequent UI activations while the caller owns the request. The caller must clear loading on success, failure or cancellation. It does not replace request idempotency or service-side validation. Direct `onClick.Invoke()` is a UnityEvent call and is not an input-gated submission API.

Normal, hovered, pressed, keyboard-focused, disabled and loading appearances share one sprite. The component uses immediate tint changes, a visible focus outline and a two-unit pressed-content offset; it has no breathing, animated loading indicator or idle motion. Disabled and loading controls retain readable labels. There is no baked text, icon or form content in the button PNG.

The default button is 64 logical units tall. The fixture includes a compact 176-unit width and a wider caption example. Labels may wrap, but callers must provide enough space when text exceeds the fitting floor; clipping long text is not an acceptable localization fallback.

## Sprite and font provenance

The [asset manifest](../../assets/sprites/autosprite/primary-button-r1/manifest.json) pins AutoSprite asset `cmuo398u4000ah2ehbm10hjfe`, the exact prompt, generation options, source/export hashes, font hashes and two credits: one generation plus one background removal. The original and transparent export remain separate files. The Unity PNG is byte-identical to the transparent export.

[`PixelButtonTextureImporter`](../../app/Assets/SoloGym/Editor/PixelButtonTextureImporter.cs) scopes its settings to this button only. The sprite selects `Rect(32, 372, 960, 278)` from the 1024×1024 provider canvas, using Unity’s bottom-left texture coordinates. It applies 72-pixel borders on each side, a centered pivot, 400 pixels per unit, point filtering, clamp wrapping, no mipmaps and uncompressed texture import. `Image.Type.Sliced` preserves corner regions while extending the center. This metadata crop does not rewrite or rescale PNG pixels.

Point filtering and the metadata crop do not establish that a generated source has a perfectly uniform native pixel grid. The review must assess the rendered border at actual device sizes; mobile-device appearance is not established by Linux captures alone.

Labels use [Pixelify Sans](../../app/Assets/SoloGym/Resources/Fonts/PixelifySans.ttf), imported from the [official Google Fonts variable TTF](https://raw.githubusercontent.com/google/fonts/main/ofl/pixelifysans/PixelifySans%5Bwght%5D.ttf). Its [SIL Open Font License 1.1](../../app/Assets/SoloGym/Resources/Fonts/PixelifySans-OFL.txt) is stored alongside the font. Noto Sans is the runtime fallback. The gallery includes Spanish accents and English captions for verification.

## Landscape review fixture

[`PixelButtonGallery`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelButtonGallery.cs) presents the six visual states, compact/wide sizes, locale switching and a loading-lock interaction example. A plain dark review surface keeps the component visible without presenting a flattened screen concept as a finished application.

The fixture targets landscape windows **854×480 and larger**, with a 1280×720 baseline and a second, wider aspect ratio. Its Canvas uses native pixel-size scaling, a minimum 0.75 scale factor and safe-area anchors. At that minimum scale, 64-unit buttons retain a 48-screen-pixel target. The optional symmetric safe inset is a test input; the fixture intersects it with `Screen.safeArea`. Extremely small safe areas and portrait layouts are outside this fixture’s supported contract.

The renderer permits fractional scale factors to keep the landscape fixture readable across sizes. It does not yet promise integer scaling for every source pixel or establish a global art grid for all future screens.

The interactive example counts fictional sets only in memory. Pressing its action enters loading; a separate completion button releases the lock. It is an input-behavior fixture, not training logging or backend integration.

## Build and review

[`PixelButtonBuild.BuildLinux`](../../app/Assets/SoloGym/Editor/PixelButtonBuild.cs) builds only `PixelButtonReview.unity` and restores temporary player settings and editor scene setup. It does not register the fixture as a normal application entry screen. Unity 6000.3.24f1 with Linux Standalone support is required; Firebase configuration and ignored SDK binaries are unnecessary for this fixture.

Run these commands from the repository root:

```bash
mkdir -p artifacts/local artifacts/visual/PrimaryButton
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelButtonBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/pixel-button-build.log"
```

Open the interactive player:

```bash
app/Builds/FantasyButton/SoloGymButton.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

Capture and run focused native interaction checks with an isolated local preference directory:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/pixel-button-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyButton/SoloGymButton.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-safe-inset 0 -sologym-smoke \
  -sologym-capture "$PWD/artifacts/visual/PrimaryButton/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/pixel-button-player.log"
```

The player writes a sibling `.smoke.json` and exits with success/failure when `-sologym-smoke` is present. Capture alone leaves the window open; add `-sologym-quit-after-capture` for capture-only automation. Use a graphics display for player captures: editor build flags `-batchmode -nographics` must not be copied into the player capture command.

## Verification results

Verified 2026-09-30 using Unity 6000.3.24f1 and the Linux review player:

| Capture / report | Locale | Simulated safe inset | Result |
| --- | --- | --- | --- |
| [1280×720](../../artifacts/visual/PrimaryButton/1280x720-es.png) / [report](../../artifacts/visual/PrimaryButton/1280x720-es.smoke.json) | Spanish | 0 px | 118/118 checks passed |
| [1844×853](../../artifacts/visual/PrimaryButton/1844x853-en.png) / [report](../../artifacts/visual/PrimaryButton/1844x853-en.smoke.json) | English | 64 px | 118/118 checks passed |
| [854×480](../../artifacts/visual/PrimaryButton/854x480-es.png) / [report](../../artifacts/visual/PrimaryButton/854x480-es.smoke.json) | Spanish | 32 px | 118/118 checks passed |

The standalone build passed. The transparent export and Unity PNG have identical hashes, and the imported sprite's crop, point filtering and slicing metadata match the manifest. No existing scene registration or project settings changed in the Git diff.

[`PixelButtonSmoke`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelButtonSmoke.cs) exercises uGUI raycasts and pointer/keyboard event handlers. Coverage includes single activation, right-click rejection, disabled/loading gates, duplicate submission, CanvasGroup interaction/raycast gates, navigation, localized restoration after loading, wrapped labels without unnecessary shrinking, overflow detection, safe-area containment, target size and six visual states.

All three native captures were visually inspected: live captions remain legible, borders retain their corners, focus adds an outline, pressing moves the caption down, and no overlap or clipping is visible. This is implementation verification, not user visual approval. The 48px assertion measures capture pixels, not Android density-independent units. Real Android/iOS touch, safe areas, font rendering and physical target sizes still require device verification during screen integration.
