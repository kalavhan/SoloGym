# Single-choice pixel control

Component 06 provides one selected value across labeled options, using native uGUI Toggles and a ToggleGroup. The teal selected skin and independent checkmark distinguish selection from keyboard focus and from the charcoal unselected skin. A responsive layout wraps options while preserving readable labels and touch targets.

## Difficulty during the battle

The user confirmed on 2026-09-30 that **training difficulty must remain selectable at any time during the boss-battle routine**. This is recorded in [the product direction](../product/fantasy-mvp-direction.md) and `AGENTS.md`. It is not an onboarding-only or pre-battle setting.

The offline fixture demonstrates Easy/Medium/Hard while cycling through warm-up, an active exercise, rest and pause. Changing the selection preserves the current phase and its fictional recorded-set count. A separate body selector demonstrates that appearance does not prescribe training difficulty. The appearance example can be disabled independently to inspect the disabled state; this does not lock battle difficulty.

This PR implements the shared control and a review fixture, not the production boss-battle/session controller. That later integration must use existing readiness/experience eligibility, validate allowed changes for remaining work, retain completed logs, and preserve bounded boss/reward progress. Selection alone must not award damage/rewards or restart a routine. Existing rules for illness, pain, injury and youth/experience restrictions remain applicable; the component does not decide those rules. The UI label Easy maps to the existing stable ID `light`.

## Use

```csharp
var difficulty = PixelChoiceControl.Create(parent,
    new[] { "light", "medium", "hard" },
    new[] { "Fácil", "Medio", "Difícil" }, "medium");
difficulty.onValueChanged.AddListener(id => controller.RequestDifficulty(id));
difficulty.SetOptionInteractable("hard", controller.CanUseHard);
difficulty.SetTraversal(previousControl, nextControl);
```

[`PixelChoiceControl`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelChoiceControl.cs) owns a fixed set of unique, nonempty IDs and localized labels. It requires an explicit valid initial value and keeps exactly one selected option; activating the selected option does not clear it or emit another control-level change event. The raw Toggles are implementation surfaces: use the control's event and binding methods for screen state, and do not change their ToggleGroup or allow-switch-off policy.

| API | Contract |
| --- | --- |
| `Value`, `Options`, `Option(id)` | Inspect stable selection and option controls; unknown IDs are rejected |
| `SetValueWithoutNotify(id)` | Bind existing caller-owned state and refresh all selection visuals without emitting a user change; does not enforce domain eligibility |
| `SetLabel(id, localizedText)` | Localize without replacing IDs or changing selection |
| `SetOptionInteractable(id, enabled)` | Gate one choice without silently choosing a replacement |
| `SetInteractable(enabled)` | Gate the whole group through CanvasGroup; preserve its selection and checkmark |
| `SetTraversal(previous, next)` | Complete the screen's keyboard order; Tab skips unavailable/inactive controls |
| `BindAccessibility(hierarchy, groupLabel)` | Register a named native container and labeled Toggle nodes in a screen-owned hierarchy |
| `PreferredHeight`, `RefreshLayout()` | Expose the height required by the current width and wrapped labels |

Pointer, keyboard submit and accessibility activation respect option and inherited CanvasGroup gates. Disabled selected choices retain their selection marker and native selected/disabled state. If a selected difficulty becomes ineligible, the session controller must resolve that model change and bind the allowed value; the presentation control does not guess a prescription. Hidden controls do not activate or remain available to accessibility. Reopening preserves the selected value while clearing stale focus.

Tab follows option order. Left/Right move through adjacent options; Up/Down follow columns in the wrapped layout. Navigation skips unavailable entries and exits through the supplied preceding/following controls at group boundaries. Focus alone does not change selection; Enter/Space commits. Provide matching traversal links on neighboring components.

The native accessibility nodes use a Toggle role and selected/disabled flags, live localized labels, screen bounds and guarded activation. The screen owns the global hierarchy, reading order and full-screen registration. Clearing a hierarchy before controls are destroyed is supported; bind again when building a new hierarchy. Linux metadata checks are not a substitute for TalkBack/VoiceOver device verification.

## Layout and artwork

Options request at least 180 logical units of width and 64 of height, with 12-unit gaps and 24-unit live Pixelify Sans text. Columns derive from the available width. Every row grows to accommodate its longest wrapped caption; the root LayoutElement reports the resulting preferred/minimum height. A manual parent layout must allocate `PreferredHeight` or sufficient scrolling space. Reserve at least 2 units around controls for the focus outline and keep live text outside panel borders. Exceptionally long unbroken labels should be localized concisely for a useful result.

The [AutoSprite marker manifest](../../assets/sprites/autosprite/choice-control-r1/manifest.json) preserves original/transparent exports, prompt, provider ID, exact hashes and a **2-credit** cost. The runtime PNG is byte-identical to the transparent export. Import metadata crops `(169,232,699,612)` in bottom-left texture coordinates, with a centered pivot, zero slice borders, point filtering, no mipmaps or compression, and 400 pixels/unit. The 24-unit marker preserves aspect ratio. The accepted primary and secondary skins are reused; no character or equipment art was generated.

## Build and review

Use Unity 6000.3.24f1 with Linux Standalone support, from this checkout:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelChoiceBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/choice-build.log"

app/Builds/FantasyChoice/SoloGymChoice.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

Capture and native input checks:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/choice-review-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyChoice/SoloGymChoice.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-safe-inset 0 -sologym-smoke \
  -sologym-capture "$PWD/artifacts/visual/ChoiceControl/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/choice-player.log"

python3 tools/check_pixel_field_keyboard.py --choices
```

Smoke mode writes the capture and sibling JSON, then exits with the result. Capture-only mode stays open unless `-sologym-quit-after-capture` is supplied. The existing keyboard harness gains `--choices`; previous modes remain available. It uses an isolated Xvfb display and `libX11`/`libXtst`, never the user's desktop.

## Verification

Verified 2026-09-30; the Unity Linux build succeeded.

| Evidence | Safe inset | Result |
| --- | --- | --- |
| [1280×720 ES](../../artifacts/visual/ChoiceControl/1280x720-es.png) / [report](../../artifacts/visual/ChoiceControl/1280x720-es.smoke.json) | 0 px | 139/139 passed |
| [1844×853 EN](../../artifacts/visual/ChoiceControl/1844x853-en.png) / [report](../../artifacts/visual/ChoiceControl/1844x853-en.smoke.json) | 64 px | 139/139 passed |
| [854×480 ES](../../artifacts/visual/ChoiceControl/854x480-es.png) / [report](../../artifacts/visual/ChoiceControl/854x480-es.smoke.json) | 32 px | 139/139 passed |
| [Real OS keyboard report](../../artifacts/visual/ChoiceControl/keyboard.smoke.json) | 0 px | 60/60 passed |

Checks cover single selection, repeat-activation suppression, quiet data binding, separate focus/selection, pointer target padding, native submit, disabled/inactive/CanvasGroup gates, stable IDs, independent appearance/difficulty, localization, row-aware navigation, wide/narrow wrapping, long-label height, accessibility selection/lifecycle and safe-area containment. Difficulty can change in all four fictional battle phases without changing the phase or logged-set count. OS testing drives arrows/Tab/Shift+Tab/Enter across active, resting and paused examples.

All three final captures were visually inspected. Source/runtime hashes, alpha, import metadata and documentation links were verified. User visual approval and physical mobile touch/font/screen-reader checks remain pending. Existing product scenes, training generation, auth, real logs and reward issuance are unchanged; this fixture is not production workout integration.
