# SoloGym native Home window

Unity **6000.3.24f1**, built-in renderer and uGUI **2.0.0**. Open this `app` directory in Unity, then open `Assets/SoloGym/Scenes/SystemHome.unity` and press Play. The scene is generated and reproducible through **SoloGym → Create or Open System Home**.

The Home window has editable runtime text and values, English/Spanish detection and persistent language choice, real UI controls, and training/recovery/saved/completed/setup/review/loading/error presentation. The illustrated character is the approved static Home portrait; modular customization and tower gameplay remain later windows. Profile/game values are fictional local fixtures. Destination buttons dispatch the correct window ID and show an honest unavailable notice until that destination is implemented. See the [final captures and reference comparisons](../docs/windows/WIN-001-implementation.md#current-visual-evidence) for the current visual result and remaining differences.

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

Outputs:

- `Builds/Linux/SoloGym.x86_64` plus adjacent runtime files.
- `Builds/Android/SoloGym-debug.apk`: ARM64/IL2CPP, local debug signing, minimum Android API 26. It is not a store release.

The builds use normal player rendering without the Development Build watermark. The Android module uses the installed editor's bundled SDK/JDK/NDK. iOS remains a target but requires the iOS support module and a macOS/Xcode signing/build host; this Linux environment has neither.

Run the Linux player with `-sologym-review`, or press **F8**, to inspect sample Home states. Settings offers language selection; the review menu additionally offers scenario and connection-state fixtures. These are local presentation scenarios, not network/account simulations.

## Focused verification

After assembling the whole screen, capture it with:

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-locale en -sologym-capture "$PWD/artifacts/visual/WIN-001/home-en-final.png" -sologym-smoke
```

`-sologym-smoke` runs five focused runtime interaction checks after capture: language control, saved-session readiness route, illness recovery, teen supervision and offline Gym messaging. It does not generate a screenshot per control. Use `-sologym-locale es` for Spanish and `-sologym-mode Recovery` for a state example.

[`tools/compare_home.py`](../tools/compare_home.py) creates full-screen overlays/difference evidence without resizing. Metrics describe differences; they never automatically certify a pixel-identical result. The [retained verification report](../docs/windows/WIN-001-implementation.md) records EN/ES captures and all five passing interaction checks. These Linux player checks do not replace Android or iOS device verification.

Font license notices are stored beside the bundled fonts. Unity caches, APKs, player builds, credentials and signing keys are excluded from Git.
