# SoloGym native Welcome and Home

Unity **6000.3.24f1**, built-in renderer and uGUI **2.0.0**. Open this `app` directory in Unity, then open `Assets/SoloGym/Scenes/Welcome.unity` and press Play. **SoloGym → Configure Entry and Home Scenes** reproduces the scene setup with Welcome first and Home second. The verified Android review build is **0.2.1**, version code **3**, with both providers configured. Retained visual captures and the Linux player remain **0.2.0**; the UI is unchanged.

Welcome includes persistent EN/ES selection, native email/password input, validation, password visibility and localized request/error states. Firebase Email/Password and Google are enabled. The user authorized the public support contact, and the Android Google Credential Manager integration uses the actual web/Android OAuth client configuration. See [Firebase setup](../docs/engineering/firebase-auth-setup.md) and the [Welcome implementation report](../docs/windows/WIN-002-implementation.md) for build/connection status and retained screenshots. No real-device sign-in or acceptance of this new build is recorded. Signup, recovery, onboarding, legal content and production profile routing are future milestones; the sample Home is not an authenticated profile.

The Home window has editable runtime text and values, English/Spanish detection and persistent language choice, real UI controls, and training/recovery/saved/completed/setup/review/loading/error presentation. The illustrated character is the approved static Home portrait; modular customization and tower gameplay remain later windows. Profile/game values are fictional local fixtures. Destination buttons dispatch the correct window ID and show an honest unavailable notice until that destination is implemented. See the [final captures and reference comparisons](../docs/windows/WIN-001-implementation.md#current-visual-evidence) for the current visual result and remaining differences.

The user installed the earlier Home build on Android and accepted its appearance on 2026-09-25. The device model/OS version were not supplied; that acceptance does not cover the new Welcome build or authentication.

## Target-render development

`Resources/Home/PixelMap.json` defines source-pixel geometry from the approved 853×1844 renders. `HomeScreen.cs` assembles the entire screen from shared source artwork, mapped clean text regions, native text, a data-driven XP bar and controls. `ReferenceTextLayout.cs` aligns live glyph bounds with the measured reference. Both languages share the same runtime art.

No button-specific scene or image-generation loop is used. One text-free plate was generated for the whole interface; only its relevant mapped regions replace baked reference typography. The full approved screenshot is not used as a hidden screenshot-only test mode. The art is still a fixed illustration, not transparent character/equipment layers or a finished avatar rig.

The project preserves artwork aspect ratio within `Screen.safeArea`. Different aspect ratios can letterbox. Larger-text reflow and native screen-reader coverage still require the accessibility milestone; this baseline does not claim those adaptations are complete.

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

Local builds with `SOLOGYM_REVIEW` expose the labeled sample Home option even when Firebase is configured. In the Linux player, `-sologym-review` also enables review controls, and `-sologym-window home` opens Home directly. Home's review menu supports **F8** and sample scenario/connection states. These are local presentation scenarios, not proof of authentication; omit the review define from a future production build.

## Focused verification

After assembling the whole screen, capture Welcome with:

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-locale en -sologym-capture "$PWD/artifacts/visual/WIN-002/welcome-en-final.png" -sologym-smoke
```

Welcome's `-sologym-smoke` runs five focused checks after capture: language, email validation, password visibility, clearing the password on Back, and unavailable authentication never authorizing Home. Capture/smoke mode uses an unconfigured auth service and makes no provider calls. Use `-sologym-locale es` for Spanish and `-sologym-view email` for the form. No screenshot is generated per control.

To capture the earlier Home instead, add `-sologym-window home` and use an output under `artifacts/visual/WIN-001`. In Home, `-sologym-mode Recovery` selects that sample state and `-sologym-smoke` checks its original five Home interactions.

[`tools/compare_home.py`](../tools/compare_home.py) creates full-screen overlays/difference evidence without resizing for either window. Metrics describe differences; they never automatically certify a pixel-identical result. The [Welcome report](../docs/windows/WIN-002-implementation.md) and [Home report](../docs/windows/WIN-001-implementation.md) retain their respective evidence. Linux player checks do not replace Android or iOS device verification.

Font license notices are stored beside the bundled fonts. Unity caches, APKs, player builds, credentials and signing keys are excluded from Git.
