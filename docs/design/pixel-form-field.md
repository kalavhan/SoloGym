# Labeled pixel form field

Component 03 adds native single-line editing to the accepted panel/button family. A recessed dark AutoSprite skin frames the editable value; labels, placeholders, helper/error text, focus and password visibility remain live uGUI controls. The dedicated fixture is offline and uses fictional values.

## Usage

[`PixelFormField`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelFormField.cs) is created under a UI transform:

```csharp
var email = PixelFormField.Create(parent, PixelFormField.Kind.Email,
    "Correo electrónico", "tu@ejemplo.com", "Correo de ejemplo.");
var password = PixelFormField.Create(parent, PixelFormField.Kind.Password,
    "Contraseña", "Contraseña");
password.SetLocalizedText("Contraseña", "Contraseña", "", "Ver", "Ocultar");
email.SetTraversal(previousControl, password.Input);
password.SetTraversal(email.Input, nextControl);
email.Input.onValueChanged.AddListener(value => { /* caller updates its model */ });
```

Use a native `VerticalLayoutGroup` or honor `PreferredHeight` in a manual layout. The LayoutElement requests at least 240 logical units wide for text/email and 400 for password (including the visibility control). Input rows are 64 units tall. Labels and messages wrap and increase preferred height; the component does not resize its parent. A screen must supply enough room or scrolling for unusually long content. Keep placeholders concise: they remain single-line and are clipped to the text viewport.

| API | Behavior |
| --- | --- |
| `Input` | Native `PixelFieldInput : InputField`; supports selection, caret, deletion, horizontal value scrolling, keyboard hints and native events |
| `SetValueWithoutNotify(value)` | Bind an existing value without sending change callbacks |
| `SetLocalizedText(label, placeholder, helper, show, hide)` | Update presentation without replacing typed values or discarding validation |
| `SetError(message)` | Show caller-owned validation with an explicit `!` and colored frame; empty clears it |
| `SetInteractable(enabled)` | Gate input and password visibility together; inherited CanvasGroup gates also apply |
| `SetTraversal(previous, next)` | Link Tab/Shift+Tab order; password inserts its visibility button before `next` |
| `TogglePasswordVisibility()` / `HidePassword()` | Reveal/mask without rewriting the value; pointer toggles preserve focus and selection |

`Kind.Email` requests the email keyboard but does not filter valid address characters with Unity's restrictive email character validator. `Kind.Password` starts masked, disables rich text like all field kinds, and resets to masked when the component closes. Password values remain in native InputField memory while the field exists; this control does not store or transmit them. Avoid logging field values in screen code.

[`PixelFieldTabNavigation`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelFieldTabNavigation.cs) handles explicit Tab links and skips inactive/disabled controls. Add the same navigation helper to neighboring buttons to complete a form's order. Arrow keys remain available for native caret movement. Enter/end-edit behavior belongs to the caller: bind `Input.onSubmit` for submission or advancement, rather than treating every blur as a form submission.

The component does not define production email/password policy or perform authentication. The fixture demonstrates simple required/format feedback, not account validation. Existing auth, onboarding and product scenes are unchanged.

## Art and behavior

[Source manifest](../../assets/sprites/autosprite/form-field-r1/manifest.json) records AutoSprite asset `cmuo4urc800017r2wg645tnqy`, prompt, exact image hashes and two credits (generation plus background removal). Source files are preserved and the Unity copy is identical to the transparent export. No character art was generated.

The importer scopes itself to `FormField.png`: metadata crop `(7,404,1010,216)` in bottom-left texture coordinates, 48px borders, 400 pixels/unit, point filtering, no mipmaps and no compression. Nine-slicing retains 12-unit corner regions at the default Canvas reference scale. It uses the existing Pixelify Sans font and its bundled license. Generated source texture and fractional UI scaling are not a promise of a perfect native pixel grid on every device.

The field has normal, focused, invalid and disabled presentation. Validation includes readable text, not just a changed border color. Values remain live text inside a clipped viewport, so long input scrolls while the stored string is retained. Password show/hide reuses the accepted primary button instead of adding an unreviewed icon component.

## Build and review

The fixture uses the existing shared review builder. Run from the repository root with Unity 6000.3.24f1 and Linux Standalone support:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelFieldBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/field-build.log"
```

Open the interactive player:

```bash
app/Builds/FantasyField/SoloGymField.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

For native handler checks and capture:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/field-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyField/SoloGymField.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-safe-inset 0 -sologym-smoke \
  -sologym-capture "$PWD/artifacts/visual/FormField/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/field-player.log"
```

`-sologym-smoke` writes a PNG and sibling JSON, then exits with the verification status. Capture alone keeps the window open; `-sologym-quit-after-capture` closes a capture-only run.

The additional [keyboard harness](../../tools/check_pixel_field_keyboard.py) opens a private Xvfb display and injects actual X11 typing/Tab/Shift+Tab/Enter events into its own review player. It does not operate the user's desktop:

```bash
python3 tools/check_pixel_field_keyboard.py
```

It requires Linux `xvfb-run`, `libX11` and `libXtst`; evidence is written under `artifacts/local/field-keyboard*`. The driver types only fixed fictional test values. JSON reports contain checks and display dimensions, not entered values.

## Verification

Verified 2026-09-30 with Unity 6000.3.24f1 and the Linux review player. The standalone build succeeded.

| Capture / report | Locale | Safe inset | Result |
| --- | --- | --- | --- |
| [1280×720](../../artifacts/visual/FormField/1280x720-es.png) / [report](../../artifacts/visual/FormField/1280x720-es.smoke.json) | ES | 0 px | 85/85 passed |
| [1844×853](../../artifacts/visual/FormField/1844x853-en.png) / [report](../../artifacts/visual/FormField/1844x853-en.smoke.json) | EN | 64 px | 85/85 passed |
| [854×480](../../artifacts/visual/FormField/854x480-es.png) / [report](../../artifacts/visual/FormField/854x480-es.smoke.json) | ES | 32 px | 85/85 passed |
| [Real OS keyboard report](../../artifacts/visual/FormField/keyboard.smoke.json) | EN | 0 px | 39/39 passed |

Checks cover raycast activation, native Unicode typing, selection replacement/backspace, horizontal scrolling without losing stored text, password masking and visibility with preserved selection/focus, disabled/CanvasGroup gates, traversal, caller-owned validation, localized value preservation and re-masking when closed. The X11 run additionally types through actual OS events, uses Tab/Shift+Tab, toggles visibility through keyboard submit and submits the fictional example once with Enter.

All three captures were visually inspected for labels, input text, focus/error states, wrapping and spacing. Long validation text increases preferred height and leaves space before the next field. Source/runtime hashes, import metadata and documentation links were verified. Existing application scenes, scene registration and project settings have no changes.

User visual approval remains pending. Device integration must still verify Android/iOS keyboard and secure-entry visibility behavior, keyboard occlusion, touch targets in physical units and font rendering. Linux capture pixels do not prove density-independent mobile target sizes.
