# SoloGym native Welcome, onboarding and Home

Unity **6000.3.24f1**, built-in renderer and uGUI **2.0.0**. Open this `app` directory in Unity, then open `Assets/SoloGym/Scenes/Welcome.unity` and press Play. **SoloGym → Configure Entry and Home Scenes** reproduces the scene setup with Welcome first and Home second. The current Linux review player is **0.5.0**, including WIN-006/007/009; visual evidence and passed focused checks are retained in the [onboarding report](../docs/windows/WIN-006-007-implementation.md). The existing Android review build remains **0.2.1**, version code **3**, with both providers configured. No APK is generated or replaced in this iteration, as requested. Historical Welcome captures are retained from **0.2.0**.

Welcome includes persistent EN/ES selection, native email/password input, validation, password visibility and localized request/error states. Firebase Email/Password and Google are enabled. The user authorized the public support contact, and the Android Google Credential Manager integration uses the actual web/Android OAuth client configuration. See [Firebase setup](../docs/engineering/firebase-auth-setup.md) and the [Welcome implementation report](../docs/windows/WIN-002-implementation.md) for build/connection status and retained screenshots. The user subsequently reported the Android 0.2.1 build working; the report does not isolate a provider or device. Verified sign-in and Create Account now enter WIN-006, and Welcome’s Privacy/Terms links open the shared document reader. Signup, recovery, final legal content and production profile routing remain later work; the sample Home is not an authenticated profile.

The Home window has editable runtime text and values, English/Spanish detection and persistent language choice, real UI controls, and training/recovery/saved/completed/setup/review/loading/error presentation. The illustrated character is the approved static Home portrait; modular customization and tower gameplay remain later windows. Profile/game values are fictional local fixtures. Destination buttons dispatch the correct window ID and show an honest unavailable notice until that destination is implemented. See the [final captures and reference comparisons](../docs/windows/WIN-001-implementation.md#current-visual-evidence) for the current visual result and remaining differences.

The user installed the earlier Home build on Android and accepted its appearance on 2026-09-25. The device model/OS version were not supplied; the later Welcome acceptance is recorded separately. The current onboarding UI has not been delivered in a new APK.

## Current character MVP

The default character screen uses eight static body presets with separate hair,
top and shorts. Confirming saves the appearance on this device and opens Home;
Back discards edits. Art polish is deferred. No renderer flag is required.

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es
```

Use `-sologym-window home` to open the saved appearance directly. Optional
`-sologym-capture /absolute/path.png` saves a screenshot and keeps the app open;
`-sologym-quit-after-capture` exits intentionally. The old registered-piece
proof requires `-sologym-avatar-renderer legacy`; the illustrated proof remains
explicitly selectable. See [integration and validation](../docs/design/character-modular-mvp.md).

## Rendering architecture correction

The existing windows now use independent background, logo, portrait and icon assets plus shared code containers and controls. `SystemTheme` is an editable ScriptableObject in `Resources/UI/DefaultTheme.asset`. `SystemPanel`, `SystemUI`, `SystemTextFit` and `SystemViewport` provide the shared rendering/layout kit. The old source images and clean plates moved outside Unity to `design/legacy-runtime-plates` and no longer ship as Resources. State controllers and Firebase integration are retained. See the [implementation report and captures](../docs/engineering/layered-ui-and-avatar-implementation.md).

Use `-sologym-window components` to inspect resizing, language and theme changes, or `-sologym-window avatar` for the isolated modular character proof. The proof has one body fit, two skin palettes, two hairstyles, torso/glove equipment and idle/walk/jab. It has visible art/rig limitations and does not replace the static Home illustration.

## Age/region and privacy

`OnboardingScreen.cs` assembles the two approved windows with live native UI. WIN-006 has numeric age entry, country and optional subdivision pickers, validation and shared EN/ES choice. WIN-007 has initially unchecked decisions, a reusable document reader, Back and Not now. Changes of country reset incompatible subdivisions; returning and switching language preserve the in-memory draft. Leaving onboarding discards its draft and does not delete a Firebase account.

`OnboardingState.cs` keeps all age, residence and decision data in memory. The only persisted local preference is language. `Resources/Onboarding/RegionCatalog.json` is a display catalog, not permission to launch in a country. `Documents.json` explicitly marks the final documents unavailable. The normal flow fails closed while reviewed regional policy, final documents and authenticated server storage are absent; there are no saved consent receipts, Firestore profile writes or completed onboarding flags.

The language menu exposes an explicit onboarding preview only in review mode. Its fictional 21/MX fixture and visible preview label let reviewers inspect both windows without authorizing a real account. The preview continues to WIN-009 and never records legal acceptance. See the [implementation report](../docs/windows/WIN-006-007-implementation.md) for source and evidence.

## Target-render development

The Home and Welcome pixel maps retain baseline layout coordinates only. All frames, editable controls and text are composed at runtime; no source-screen or clean-plate regions are sampled. Both locales share artwork. Native typography is uniformly sized, with wrapped content where appropriate, rather than stretching glyph vertices.

The shared viewport preserves the approved baseline composition inside `Screen.safeArea` and lifts focused forms above the keyboard. Different aspect ratios can letterbox. Full large-text reflow and native screen-reader coverage remain future accessibility work. Final comparisons record differences from the original target rather than claiming mathematical identity.

## Local builds

Set `SOLOGYM_UNITY` to your Unity 6000.3.24f1 executable and run from the repository root:

```bash
"$SOLOGYM_UNITY" -batchmode -nographics -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.SoloGymBuild.BuildLinux -quit -logFile /tmp/sologym-linux.log

"$SOLOGYM_UNITY" -batchmode -nographics -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.SoloGymBuild.BuildAndroid -quit -logFile /tmp/sologym-android.log
```

For configured Firebase builds, first run `python3 tools/install_firebase_unity.py`, then follow the [project configuration and dependency resolution steps](../docs/engineering/firebase-auth-setup.md). SDK binaries and client configuration are deliberately excluded from Git. A clone without them builds an unavailable-auth preview.

Outputs:

- `Builds/Linux/SoloGym.x86_64` plus adjacent runtime files.
- `Builds/Android/SoloGym-debug.apk`: ARM64/IL2CPP, local debug signing, minimum Android API 26. It is not a store release.

The builds use normal player rendering without the Development Build watermark. The Android module uses the installed editor's bundled SDK/JDK/NDK. iOS remains a target but requires the iOS support module and a macOS/Xcode signing/build host; this Linux environment has neither.

Local builds with `SOLOGYM_REVIEW` expose the labeled sample Home and onboarding options even when Firebase is configured. In the Linux player, `-sologym-review` also enables review controls. `-sologym-window home` opens Home directly; with review enabled, `-sologym-window age` or `-sologym-window consent` opens the corresponding fictional onboarding preview. Home's review menu supports **F8** and sample scenario/connection states. These are local presentation scenarios, not proof of authentication; omit the review define from a future production build.

## Focused verification

After assembling the whole screen, capture Welcome with:

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-locale en -sologym-capture "$PWD/artifacts/visual/WIN-002/welcome-en-final.png" -sologym-smoke
```

Welcome's `-sologym-smoke` runs five focused checks after capture: language, email validation, password visibility, clearing the password on Back, and unavailable authentication never authorizing Home. Capture/smoke mode uses an unconfigured auth service and makes no provider calls. Use `-sologym-locale es` for Spanish and `-sologym-view email` for the form. No screenshot is generated per control.

Capture each new onboarding window with review explicitly enabled:

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window age -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-006/age-es-final.png" -sologym-smoke

app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window consent -sologym-locale en \
  -sologym-capture "$PWD/artifacts/visual/WIN-007/consent-en-final.png"
```

Repeat only the locale/window combinations needed for the four whole-screen references; no per-control captures are required. Onboarding smoke covers its state and UI interactions without real authentication or consent storage. Final screenshots, counts and remaining differences belong in the [onboarding report](../docs/windows/WIN-006-007-implementation.md).

To capture the earlier Home instead, add `-sologym-window home` and use an output under `artifacts/visual/WIN-001`. In Home, `-sologym-mode Recovery` selects that sample state and `-sologym-smoke` checks its original five Home interactions.

[`tools/compare_home.py`](../tools/compare_home.py) creates full-screen overlays/difference evidence without resizing for the retained whole-window targets. Metrics describe differences; they never automatically certify a pixel-identical result. The [Welcome report](../docs/windows/WIN-002-implementation.md) and [Home report](../docs/windows/WIN-001-implementation.md) retain their respective evidence. Linux player checks do not replace Android or iOS device verification.

Font license notices are stored beside the bundled fonts. Unity caches, APKs, player builds, credentials and signing keys are excluded from Git.

## Private profile (WIN-009)

The onboarding review route opens the private-profile notice, optional measurements and readiness steps. Metric cm/kg or imperial feet/inches/lb inputs preserve canonical precision across unit switches. Blank measurements are permitted. Back retains the draft across WIN-007/009; explicit exit clears it. Unwell/pain/injury/uncertain answers pause setup, while ready/low energy reaches the next review checkpoint. No backend profile write or live health-data consent is implemented.

Capture the approved principal state with `-sologym-window profile -sologym-review -sologym-locale es -sologym-capture <absolute-output.png>`. Add `-sologym-profile-view notice`, `imperial`, `readiness` or `paused` for complete supporting states. `-sologym-smoke` on the default measurements view runs profile checks; the existing onboarding smoke now also checks Back/reentry draft preservation. These flags use fictional review data, never a screenshot-only reference mode. [Evidence and limits](../docs/windows/WIN-009-implementation.md).

## Goals and experience (WIN-010)

Review mode opens a three-step flow: training focus, experience (with an explicit not-sure confirmation), then review. Teen preview uses `-sologym-audience teen` to show only general fitness and mobility. Production saves remain blocked until the private profile service exists.

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window goals -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-010/goals-es-final.png" -sologym-smoke
```

Add `-sologym-goals-view experience` or `review` for later steps. The onboarding profile checkpoint opens this window automatically in review. See [WIN-010 implementation](../docs/windows/WIN-010-implementation.md).

## Available equipment (WIN-011)

Review mode: environment (home / gym / outdoor), scrollable equipment multi-select or bodyweight-only, then review. Outdoor uses bodyweight-only when the catalog list is empty.

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window equipment -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-011/equipment-es-final.png" -sologym-smoke
```

Goals review checkpoint opens this window in the onboarding chain. See [WIN-011 implementation](../docs/windows/WIN-011-implementation.md).

## Schedule / session time (WIN-012)

Review mode: one setup screen (2–5 weekdays + inline scroll **wheels** for hours **0–12** and minutes **00–60**, labels to the right).

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window schedule -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-012/schedule-es-final.png" -sologym-smoke
```

Add `-sologym-schedule-view review` to prefill setup fields for captures (still one screen). Schedule continue opens WIN-013 in the onboarding chain. See [WIN-012 implementation](../docs/windows/WIN-012-implementation.md).

## Character customization (WIN-013)

The current [AutoSprite checkpoint](../docs/design/character-autosprite-checkpoint.md)
uses one **front-facing** character and a complete sprite idle loop in configuration
and Home. Launch from the repository root:

```bash
app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer autosprite
```

Continue opens Home; tapping the character returns to configuration. Idle can be
paused or played, and captures stay open unless `-sologym-quit-after-capture` is
provided. This preview does not save a profile or provide equipment/body swaps.
Isometric is deferred. The [next body plan](../docs/design/character-sprite-v1-plan.md)
uses five types per gender and preserves all fourteen approved appearance masters.

### Earlier character experiments

See the [character creation checkpoint](../docs/design/character-creation-checkpoint.md)
for approved appearance sources, unresolved motion defects and the stage after
PR review and merge. Idle received positive user feedback; walk/jab require
re-authoring and are not approved gameplay animation.

The illustrated 2.5D playable proof covers the normal man and woman in studio
and elevated gameplay views, skin/hair colors, a complete outfit swap and
idle/walk/jab previews. Launch it from the repository root:

```bash
app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer illustrated
```

This opt-in preview does not save a character. See the
[proof contract, provenance and validation](../design/character-2-5d/playable-proof-r1/README.md)
before deriving additional bodies or equipment. Approval of the fourteen
appearance masters does not approve their runtime derivatives.

The following command opens the earlier registered-piece proof:

Review mode: **full-screen character studio** (CAS-style dock + overlay carousel, single avatar host).

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer legacy \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-es-g3.png" -sologym-smoke
```

Do not pass `-batchmode -nographics` for character captures (Linux screenshots come out blank).

For interactive character review, omit `-sologym-smoke`: the screenshot is saved once and the studio stays open for customization. Add `-sologym-quit-after-capture` when an automated screenshot command should exit. `-sologym-smoke` still runs the checks and exits with their result.

Legacy review-only variants: `-sologym-character-view face`, `gear` or `body` opens that category. `-sologym-avatar-variant alternate` selects deep skin, swept hair, bare torso and bare hands through the regular customization controller. The character smoke checks pointer priority, camera changes, hair sprites, equipment visibility, mirrored poses and attachment registration before the setup checkpoint.

Reference compositor check: same window plus `-sologym-avatar-reference-check` (writes `artifacts/visual/AvatarReference/reference-check.json`). Rig tuning: `-sologym-window avatar` with `-sologym-capture`.

Schedule review now opens the modular MVP character screen and Continue opens Home. The explicit legacy renderer retains the old setup-complete notice. See [WIN-013 implementation](../docs/windows/WIN-013-implementation.md).

## Layered UI verification

Run `python3 tools/capture_layered_ui.py` from the repository root after a Linux build. It captures the five migrated windows in both languages, selected supporting states, a phone-sized viewport, the component gallery and the avatar proof. See [the current report](../docs/engineering/layered-ui-and-avatar-implementation.md) for results and the avatar’s production limits.
