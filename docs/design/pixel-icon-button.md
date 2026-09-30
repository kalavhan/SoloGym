# Pixel icon button

Component 05 adds reusable Back and Settings controls. Each native uGUI button combines the accepted secondary-action skin with an independent icon, a required localized accessible name, and optional live companion text. Normal, hover/focus, pressed and disabled states match the existing pixel UI. The offline fixture demonstrates two target sizes and navigation between fictional Home/Settings views.

## Component contract

```csharp
var icon = Resources.LoadAll<Sprite>("UI/Pixel/IconSettings")[0];
var settings = PixelIconButton.Create(parent, icon, "Ajustes", OpenSettings);
settings.BindCaption(settingsCaption); // optional text owned and laid out by the screen
settings.SetTraversal(previousControl, nextControl);
settings.interactable = canOpenSettings;
```

[`PixelIconButton`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelIconButton.cs) derives from `Button`; native `onClick`, interactability, CanvasGroup gates and pointer semantics apply. `SetIcon` replaces only the symbol. `SetLabel` updates its accessible name and bound caption together. Both reject missing values without erasing existing content. Captions render markup literally and do not intercept input. There is no text baked into the artwork, no icon rotation, glow, idle motion or character generation.

The default target is **64×64 logical units**, with a centered 36×36 icon slot. Icons use `Image.Type.Simple` with preserved aspect ratio; the surrounding frame is nine-sliced independently. A transparent root Image catches the entire rectangle, including its corners and padding. The LayoutElement requests a minimum of 64×64; manual layouts must honor it and leave at least 2 units around the frame for focus. The fixture also demonstrates a 96×96 target with a 48×48 icon slot. Companion captions are separate display text, outside the button's hit area; their width and wrapping belong to the screen layout.

`SetTraversal` reuses explicit Tab/Shift+Tab links and native directional navigation. Tab skips disabled/inactive linked controls. Explicit directional destinations should be chosen by the caller; the Tab helper's skip behavior is not an arrow-key routing policy. `TryActivate` provides the guarded path used by keyboard submit and accessibility activation. Hiding a selected button clears its selection and focus treatment. Back routing, destination focus, request handling and settings persistence remain caller responsibilities.

## Native accessible names

The PR enables Unity's built-in `com.unity.modules.accessibility` module; it adds no third-party package. A screen can bind each icon to its own hierarchy:

```csharp
var hierarchy = new UnityEngine.Accessibility.AccessibilityHierarchy();
settings.BindAccessibility(hierarchy);
// Register the rest of the screen, then activate its completed hierarchy:
UnityEngine.Accessibility.AssistiveSupport.activeHierarchy = hierarchy;
```

The control supplies a button role, localized label, disabled/active state, guarded activation callback and a frame getter for its actual screen bounds. Accessibility focus also receives the visible focus frame. Disabling a GameObject hides its node; disabling interaction reports the disabled state. `UnbindAccessibility` and destruction remove the node. Clearing the screen-owned hierarchy before destroying the control is handled safely; bind again when constructing the next hierarchy.

The component never replaces the global hierarchy itself. The fixture owns an isolated hierarchy, includes its locale action and current destination text, and restores the previous hierarchy on destruction. Future screens must register their remaining content, manage reading order, and deactivate nodes for content they visually hide or clip without disabling its GameObject. An accessible name does not make the rest of the application accessible automatically. Unity documents the [native node model](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AccessibilityNode.html) and [screen-bounds contract](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Accessibility.AccessibilityNode-frame.html).

Linux verification covers native node metadata, frames and the activation callback's guarded code path. Actual TalkBack/VoiceOver announcements, screen-reader focus traversal and device coordinates still need physical Android/iOS testing; this PR does not claim that device validation.

## Art and provenance

[The manifest](../../assets/sprites/autosprite/icon-button-r1/manifest.json) preserves one original and one transparent AutoSprite export per icon, the provider IDs, short prompts, crop metadata and SHA-256 hashes. Cost: **4 AutoSprite credits** total (two generations and two background removals). The Settings cog's center is transparent. Runtime PNGs are byte-identical to the transparent exports.

| Icon | Crop in bottom-left texture pixels | Runtime resource |
| --- | --- | --- |
| Back | `(198,245,638,539)` | `UI/Pixel/IconBack` |
| Settings | `(63,63,898,908)` | `UI/Pixel/IconSettings` |

The scoped importer applies metadata crops, centered pivots, zero slicing borders, point filtering, 400 pixels/unit, no compression and no mipmaps. It does not redraw or resample the source files. The button frame reuses [secondary-action-r1](../../assets/sprites/autosprite/secondary-action-r1/manifest.json). Generated source pixels and fractional UI scaling still require device review; point filtering alone does not guarantee a perfect native pixel grid at every size.

## Build and review

From this checkout, using Unity 6000.3.24f1 with Linux Standalone support:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelIconBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/icon-build.log"

app/Builds/FantasyIcon/SoloGymIcon.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

Capture and native input checks:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/icon-review-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyIcon/SoloGymIcon.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-safe-inset 0 -sologym-smoke \
  -sologym-capture "$PWD/artifacts/visual/IconButton/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/icon-player.log"

python3 tools/check_pixel_field_keyboard.py --icons
```

The shared keyboard harness retains its field default and `--actions` mode; `--icons` runs this fixture. It opens a private Xvfb display and injects real X11 Tab/Shift+Tab/Enter events only into that player. It needs `xvfb-run`, `libX11` and `libXtst`. Smoke mode saves a capture and sibling JSON report, then exits with the verification status. Capture-only mode stays open unless `-sologym-quit-after-capture` is supplied.

## Verification

Verified 2026-09-30 with a successful Unity Linux build.

| Evidence | Safe inset | Result |
| --- | --- | --- |
| [1280×720 ES](../../artifacts/visual/IconButton/1280x720-es.png) / [report](../../artifacts/visual/IconButton/1280x720-es.smoke.json) | 0 px | 127/127 passed |
| [1844×853 EN](../../artifacts/visual/IconButton/1844x853-en.png) / [report](../../artifacts/visual/IconButton/1844x853-en.smoke.json) | 64 px | 127/127 passed |
| [854×480 ES](../../artifacts/visual/IconButton/854x480-es.png) / [report](../../artifacts/visual/IconButton/854x480-es.smoke.json) | 32 px | 127/127 passed |
| [Real OS keyboard report](../../artifacts/visual/IconButton/keyboard.smoke.json) | 0 px | 56/56 passed |

Checks cover full-target raycasts, navigation callbacks, pressed/released and focus presentation, right-click rejection, disabled/CanvasGroup/inactive gates, keyboard traversal, icon replacement, required names, localized captions, native accessibility metadata and lifecycle, safe-area/panel containment, icon aspect preservation and readable text. All final captures were visually inspected. Hashes, alpha (including the cog hole), import metadata and documentation links were verified.

User visual approval and physical mobile touch/font verification remain pending. The smallest Linux capture retains a 48-pixel target; that does not establish a device-independent mobile touch size. Existing product scenes, auth, training behavior and scene registration remain unchanged. The fixture saves no settings or user data.
