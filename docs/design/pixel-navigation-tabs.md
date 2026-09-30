# Pixel navigation tabs

Component 08 provides a native uGUI navigation bar with stable destination IDs,
a current-page marker, separate keyboard focus and caller-owned route acceptance.
The offline fixture connects Home, Workouts, Dungeon and optional Fasting to
fictional page summaries. It reuses the existing character viewport, panel, choice
controls and text action. This is a component review, not completed product routing,
profile integration, a fasting tracker or a workout session.

## Route and state contract

```csharp
var navigation = PixelNavigationBar.Create(parent,
    new[] { "home", "workouts", "dungeon", "fasting" },
    new[] { "Hogar", "Rutinas", "Mazmorra", "Ayuno" },
    new[] { "home", "workouts", "dungeon" }, "home");
navigation.onNavigate.AddListener(id => {
    if (router.TryOpen(id)) navigation.SetCurrentWithoutNotify(id);
});
```

The component requests navigation through `onNavigate`; it does not mark a tab as
current merely because it was clicked or focused. The controller commits current
state with `SetCurrentWithoutNotify` after accepting the route. Re-selecting the
current destination emits no new request. Rejected/cancelled navigation retains the
old current marker. For asynchronous routing, the controller may call
`SetInteractable(false)` before awaiting, then restore availability when its request
finishes. It owns cancellation, duplicate in-flight protection and stale completions.
The bar itself performs no asynchronous I/O or automatic retry.

| API | Behavior |
| --- | --- |
| `CurrentId`, `Tabs`, `Tab(id)` | Inspect stable IDs and state; unknown IDs are rejected |
| `SetCurrentWithoutNotify(id)` | Confirm a visible destination without emitting a request |
| `BindVisibleRoutes(ids, confirmedCurrentId)` | Validate the full change before atomically applying visibility and current state; no request event |
| `SetLabel(id, localizedText)` | Update live text and accessible name without changing IDs |
| `SetTabInteractable(id, value)` | Keep a visible but unavailable destination; preserve any current marker |
| `SetInteractable(value)` | Gate all activation through a CanvasGroup without overwriting per-tab gates |
| `SetTraversal(before, after)` | Link the bar to neighboring screen controls |
| `PreferredHeight`, `Columns`, `RefreshLayout()` | Report/rebuild wrapped layout after sizing or localization |
| `BindAccessibility(hierarchy, name)` | Register native TabBar and TabButton nodes in a screen-owned hierarchy |

Visible IDs keep the catalog's declared order. Hiding the current route requires a
new confirmed current ID that remains visible; invalid changes leave the old state
intact. A hidden destination cannot become current through `SetCurrentWithoutNotify`.
Disabled current tabs can still describe an existing page; the caller decides how
to leave that page. Use the bar's API and event for integration rather than attaching
routing to the individual Button events or directly toggling child GameObjects.

## Optional adult fasting

Visibility is a caller-owned eligibility decision, not a character-body or tab-art
rule. Production routing must use the trusted profile's age, the user's adult opt-in
and the existing domain requirements. Merely hiding a button is not an authorization
boundary: route/deep-link/session entry must check eligibility independently.

The review fixture has four explicit fictional profiles:

| Profile | Fasting destination |
| --- | --- |
| Age unknown | Omitted |
| Age 15–17 | Omitted |
| Adult, default | Omitted: fasting is off by default |
| Adult with fasting enabled | Visible |

An omitted tab occupies no layout slot, accepts no pointer/submit/accessibility
activation and is inactive in the accessibility hierarchy. Keyboard traversal skips
it in both directions. When a fictional eligible adult is viewing Fasting and the
example profile changes to an ineligible one, the fixture clears the Fasting content,
confirms Home, removes the tab and rescues focus if necessary. It does not emit a
second navigation request. No real age/profile is edited and no fasting record is
created, started, deleted or rewarded.

The profile picker is a verification tool and must not be shipped as a bypass for
age or opt-in checks. Real session handling when eligibility changes is domain work,
not automatic deletion of user records by this component. Fasting remains optional,
private and without XP, buffs, rankings or streak rewards.

## Presentation, input and art

Each tab is a real native Button. The current destination uses the existing teal
primary skin and an independent checkmark; other tabs use the charcoal secondary
skin. Focus has a separate outline, press offsets live content, and disabled controls
keep their current marker while dimming. Text is live Pixelify Sans at 24 units with
rich text disabled. The entire rectangle is a pointer target, including padding.

The bar lays out equal-width visible tabs with a minimum width of 180 logical units,
minimum height 64 and 12-unit gaps. It wraps into rows when needed and grows rows for
long localized labels. Parents must allocate `PreferredHeight` or scrolling space;
reserve two extra units for outlines near clipped boundaries. Hiding a tab reflows
the remaining tabs. Narrow/shorter screen layouts still need a screen-level design.

Left/Right move focus through visible available tabs; Up/Down follow the wrapped
column. Enter/Space commits a request. Tab/Shift+Tab use the existing availability-aware
traversal helper. Moving focus alone never changes pages. The screen must maintain
its incoming links when the last visible tab changes. The fixture does this for the
locale action. If a focused tab is removed, the bar prefers its confirmed current
tab, then another available tab, then the preceding control. Hiding/reopening the
bar clears stale focus while preserving current state.

Accessibility exposes one native TabBar containing labeled TabButtons, selected and
disabled flags, screen bounds, guarded activation and visual reader focus. The caller
owns/activates the hierarchy; clearing and rebinding it is supported. Linux metadata
checks do not establish TalkBack or VoiceOver behavior on a device.

No new artwork was generated and **zero AutoSprite credits** were used. Existing
sources, hashes, sprite import settings and runtime bytes are unchanged:

- [Primary skin](../../assets/sprites/autosprite/primary-button-r1/manifest.json)
- [Secondary skin](../../assets/sprites/autosprite/secondary-action-r1/manifest.json)
- [Selected marker](../../assets/sprites/autosprite/choice-control-r1/manifest.json)
- [Barbarian viewport artwork](../../assets/sprites/autosprite/barbarian-viewport-r1/manifest.json), reused decoratively by the fixture

## Build and inspect

Use Unity 6000.3.24f1 with Linux Standalone support, from this checkout:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelNavigationBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/navigation-build.log"

app/Builds/FantasyNavigation/SoloGymNavigation.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

Capture and focused checks:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/navigation-review-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyNavigation/SoloGymNavigation.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-smoke -sologym-capture-states \
  -sologym-capture "$PWD/artifacts/visual/Navigation/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/navigation-player.log"
python3 tools/check_pixel_field_keyboard.py --navigation
```

Smoke saves its screenshot/report and exits with the test result. With smoke enabled,
`-sologym-capture-states` also captures disabled and audience examples. Capture-only
mode remains open unless `-sologym-quit-after-capture` is supplied. Keyboard injection
runs only on a private Xvfb display, never the user's desktop. Existing harness modes
remain available. Build settings are restored by the shared fixture builder.

## Verification

Verified 2026-09-30; Unity Linux build succeeded.

| Evidence | Safe inset | Result |
| --- | --- | --- |
| [1280×720 ES](../../artifacts/visual/Navigation/1280x720-es.png) / [report](../../artifacts/visual/Navigation/1280x720-es.smoke.json) | 0 | 193/193 |
| [1844×853 EN](../../artifacts/visual/Navigation/1844x853-en.png) / [report](../../artifacts/visual/Navigation/1844x853-en.smoke.json) | 64px | 193/193 |
| [854×480 ES](../../artifacts/visual/Navigation/854x480-es.png) / [report](../../artifacts/visual/Navigation/854x480-es.smoke.json) | 32px | 193/193 |
| [Real OS keyboard](../../artifacts/visual/Navigation/keyboard.smoke.json) | 0 | 29/29 |

Checks cover pointer padding, native submit, duplicate-current suppression, rejection,
quiet binding, disabled/inherited gates, visibility for all four fictional profiles,
stale hidden references, removal of the current destination, focus recovery, invalid
atomic updates, localization, wrapping/long labels and native accessibility lifecycle.
Real arrows/Tab/Shift+Tab/Enter navigate three destinations, change locale, then remove
the current fasting route by selecting the teen fixture.

All three landscape captures plus [adult opt-in](../../artifacts/visual/Navigation/states/adult-on.png),
[under-18](../../artifacts/visual/Navigation/states/teen.png),
[unknown-age](../../artifacts/visual/Navigation/states/unknown.png) and
[disabled](../../artifacts/visual/Navigation/states/disabled.png) captures were visually
inspected. Final user visual approval, physical mobile touch, font and screen-reader
checks remain pending. Existing auth, training, fasting logic and product scenes are
unchanged; this is not production eligibility or routing integration.
