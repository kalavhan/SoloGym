# Secondary and text pixel actions

Component 04 gives Back and account recovery a quieter hierarchy than the teal primary button. The framed variant uses a charcoal stone face with a muted brass border. The text variant has a live underlined caption and reveals the same frame on focus or press. Both are native uGUI buttons with full rectangular hit areas.

## Use

[`PixelSecondaryAction`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelSecondaryAction.cs) exposes two appearances through one component:

```csharp
var back = PixelSecondaryAction.Create(parent,
    PixelSecondaryAction.Appearance.Framed, "Volver", GoBack);
var recovery = PixelSecondaryAction.Create(parent,
    PixelSecondaryAction.Appearance.Text, "¿Olvidaste tu contraseña?", OpenRecovery);
back.SetTraversal(previousControl, nextControl);
recovery.interactable = canRecover;
```

The caller owns localization, routing, focus after a view change, and request completion. `SetLabel` updates the normal caption. Use `SetPending(true, "Enviando…")` synchronously inside an async handler to prevent repeat pointer/keyboard activation, then `SetPending(false)` when it finishes. Pending state preserves the underlying `interactable` and inherited CanvasGroup gates; completion does not override an explicit disabled state. There is no automatic request, timeout, retry or account-policy decision inside the component.

`SetTraversal(previous, next)` links Tab/Shift+Tab through the existing field navigation helper and configures native directional navigation. Tab skips inactive, disabled and pending entries. It requires linked neighboring controls, including primary buttons, to complete an order. Native explicit directional links should point at appropriate available destinations; the helper's skip behavior applies to Tab. Hiding the selected action clears its selection, so reopening a view does not retain an invisible focus target. Screen controllers should select a suitable destination after navigation; Escape/Back routing is caller-owned.

Allocate at least 160×64 logical units and leave room for the 2-unit focus outline within any clipped parent. Labels use live Pixelify Sans at 24 units, wrap, and render markup literally. The LayoutElement requests a larger preferred height for wrapped text; a manual layout must allocate that space itself. The fixture uses a 90-unit height for its two-line localized example. Text actions keep their entire rectangle clickable, including padding outside the caption. Their underline is presentation, not a separate hit target. A wrapped caption uses one underline beneath the text block.

## Art and integration

[The source manifest](../../assets/sprites/autosprite/secondary-action-r1/manifest.json) preserves AutoSprite asset `cmuo5x70q000n1ynln6x97acj`, its original/transparent PNGs, prompt and SHA-256 hashes. Cost: **2 AutoSprite credits** (one generation and one background removal). No characters were generated.

The Unity runtime PNG is byte-identical to the transparent export. Import metadata alone crops `(24,384,976,258)` in bottom-left coordinates and slices 64px borders at 400 pixels/unit (16 logical units per corner at the default Canvas reference scale). The importer is scoped to this asset and uses point filtering, no mipmaps and no compression. Native text and the underline are live UI; captions and states are not painted into the sprite. The generated stone fill stretches in the center of the nine-slice; fractional display scaling is not a promise of perfect native pixel spacing on every device.

[`PixelActionGallery`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelActionGallery.cs) reuses the accepted panel, form field and primary button. Recovery switches to an offline email example; Back restores the prior example and focuses its recovery entry without losing the address. The fixture never sends email or authenticates. Its other panel demonstrates an available secondary action, disabled text, pending state and a long locale-switch caption. Normal application scenes, authentication code, project settings and scene registration are unchanged.

## Build and review

From this checkout, with Unity 6000.3.24f1 and Linux Standalone support:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelActionBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/action-build.log"

app/Builds/FantasyAction/SoloGymAction.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

For deterministic handler checks and a capture:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/action-review-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyAction/SoloGymAction.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-safe-inset 0 -sologym-smoke \
  -sologym-capture "$PWD/artifacts/visual/SecondaryAction/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/action-player.log"

python3 tools/check_pixel_field_keyboard.py --actions
```

`-sologym-smoke` saves the PNG and sibling JSON and exits with the verification result. Capture alone leaves the player open; add `-sologym-quit-after-capture` to close it. The keyboard harness retains its default field mode and adds `--actions` for this fixture. It opens a private Xvfb display and sends real X11 Tab/Shift+Tab/Enter events only to that player. It requires Linux `xvfb-run`, `libX11` and `libXtst`.

## Verification

Verified 2026-09-30. The Unity Linux build succeeded.

| Evidence | Safe inset | Result |
| --- | --- | --- |
| [1280×720 ES capture](../../artifacts/visual/SecondaryAction/1280x720-es.png) / [report](../../artifacts/visual/SecondaryAction/1280x720-es.smoke.json) | 0 px | 119/119 passed |
| [1844×853 EN capture](../../artifacts/visual/SecondaryAction/1844x853-en.png) / [report](../../artifacts/visual/SecondaryAction/1844x853-en.smoke.json) | 64 px | 119/119 passed |
| [854×480 ES capture](../../artifacts/visual/SecondaryAction/854x480-es.png) / [report](../../artifacts/visual/SecondaryAction/854x480-es.smoke.json) | 32 px | 119/119 passed |
| [Real OS keyboard report](../../artifacts/visual/SecondaryAction/keyboard.smoke.json) | 0 px | 53/53 passed |

Checks exercise pointer raycasts including text padding, recovery/Back callbacks and focus transfer, pressed/released presentation, right-click rejection, disabled/inactive/CanvasGroup gates, duplicate activation while pending, restoration of caller state, Tab traversal, native directional navigation, localization and caption/panel/safe-area fit. The OS run verifies actual Tab/Shift+Tab/Enter navigation through recovery, Back and the locale action while skipping unavailable controls.

All three final captures were visually inspected. Source/runtime hashes, alpha and import metadata were checked. User visual approval and physical Android/iOS touch/font verification remain pending; 48 captured Linux pixels do not establish density-independent mobile touch sizing. This component fixture does not claim production account-recovery integration.
