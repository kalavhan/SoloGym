# Fasting — complete native clock window

The user approved `fasting-clock-concept.png` with **“i like it. do it.”**
This complete window follows merged PR #30. It preserves the frontal PixelLab
room and independent arcane clock, with live native uGUI text, parchment and
controls. No new character generation or character animation is included.

Open **Home → Settings → Optional adult fasting**. The feature starts off.
Read the suitability information and explicitly acknowledge adult eligibility
before enabling. Teen and unknown-age review profiles have no entry; direct
excluded routes do not load fasting data or artwork. Enabled adults gain an
Ayuno/Fasting navigation tab. Disabling removes it and preserves history.

The timer stores actual UTC timestamps and a separately chosen duration. It
continues across backgrounding and app restarts. Supported durations never
exceed 20 hours; that cap is not medical clearance or a target. Passing the
chosen end displays a neutral notice while preserving actual elapsed time:
no automatic end, extension or rewards. **End now** remains available in the
active clock, guide, history, correction and settings views. An immediate end
is valid. No XP, training effects, rankings or fasting streaks are connected.

Users can backdate/correct the start, finish immediately, correct both ends of
a saved record, delete individual entries, discard an active entry or separately
delete all history. Destructive actions have explicit confirmations. Reversed,
future and overlapping actual intervals are rejected. Corrections can truthfully
exceed the supported-plan duration. Inputs include a UTC offset so repeated DST
hours can be distinguished; offset-free local input is also accepted when the
local time is unambiguous. Device time earlier than the start is shown for
correction, without producing negative durations or invalid history.

Data is stored atomically in `fasting-local-v1.json` under Unity's local app data
directory. Failed writes preserve the prior saved state and unsaved form input
for retry. Corrupt, oversized and unsupported saves are preserved, never reset.
History is paged in groups of 20. This is a single local profile, with no account
synchronization or encrypted/cloud health-record service. Home's workouts remain
fictional fixtures; fasting records are user-entered local data except in the
explicit isolated smoke mode. Suitability/health copy remains subject to clinical
review before release. [Evidence and copy limits](evidence-notes.md) are preserved.

## Visual and motion behavior

The room, clock and original animation frames are copied byte-for-byte into
`app/Assets/SoloGym/Resources/Rooms/FastingR1/`. The frame JSON was read before
implementation. Original source assets, hashes, prompts and jobs remain under
`assets/sprites/pixellab/` and `production/`. Source canvases are 640×360 for
the room and 256×256 for the clock/frames. No flattened screen is used in Unity.

Raw whole-frame animation is intentionally rejected: two provider frames make
the center transparent, and some highlights are too bright. A native UI shader
clips a dim, desaturated overlay to the rune annulus specified in the atlas
(center 128,124; radii 61–82); its maximum alpha is 0.32. The original center,
casing and pedestal stay fixed. Nine source frames use authored 650 ms timings.
Reduced motion in Settings disables the decorative overlay and is persisted;
`-sologym-reduced-motion` also forces the static view. Timer text stays live.

Five neutral time bands change the label and restrained accent: 0–4, 4–16,
16–24, 24–48 and 48+ hours. These are not measured metabolic states or goals;
no ketosis, autophagy, hormone or immune-regeneration detection is claimed.
Brightness never escalates with duration. The later bands describe actual or
corrected records, not selectable extended plans.

The original `review/` browser utility and imagegen screen prompt/render remain
as design evidence. The final native captures and reports are in
`artifacts/visual/Fasting/`. Quoted PixelLab cost for the art pass was 68
generations (40 room + 20 clock + 8 motion); implementation generated no new art.

## Build and verification

```sh
/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath "$PWD/app" -executeMethod SoloGym.Editor.PixelFastingBuild.BuildLinux \
  -logFile /tmp/fasting-build.log
app/Builds/Fasting/SoloGymFasting.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -sologym-locale es
```

The normal build opens suitability setup or the saved local clock; it does not
seed or start a fast. The main app also accepts `-sologym-window fasting`.
Add `-sologym-review -sologym-smoke -sologym-capture "$PWD/artifacts/visual/Fasting/window-es-1280.png"`
for isolated example data, domain/interaction checks and screenshots. Add
`-sologym-teen` or `-sologym-age-unknown` for excluded profiles; `-sologym-locale en`
and `-sologym-safe-inset 24` exercise other display contexts. Failed checks exit
with a nonzero status. `-sologym-stay-open` keeps a capture build open.

```sh
python3 tools/check_fasting_assets.py
python3 -m unittest discover -s tests -v
```

The focused native suite verifies opt-in, timestamp/offset handling, actual
elapsed time beyond the chosen end, reloads, clock rollback, correction,
overlap rejection, deletion, disable/retain behavior, save-failure retries,
corrupt-file preservation, native pointer/keyboard submission, text fit,
reduced motion and Home integration. `verification.json` records final results.
