# Rutinas / Workouts — complete landscape window

The user approved `workouts-concept.png` with “this looks good, build it.” This implementation follows that composition: a frontal PixelLab room, one two-page journal, shared Home HUD and navigation, and live native uGUI labels and controls. Background, parchment, leather border, button skins and text remain independent assets.

## Window behavior

- Week navigation and date selection; history filters for completed and missed examples.
- Full routine with doses, per-side quantities, rest and existing training messages.
- Editable pending routines: optional name, date, 15/25/40-minute budget and compatible exercise substitutions. Changing duration resets substitutions. Combined substitutions must still fit the budget and contain no duplicate exercises.
- Draft save/cancel with discard confirmation. Failed writes preserve the draft and previous saved document. Completed history retains its original prescription snapshot; missed sessions do not create catch-up debt. A separate new draft can be created without rewriting either.
- Today’s pending routine goes through fresh readiness and teen supervision checks. Recovery paths cannot accept a strength plan. Acknowledgment stores only a local review, never completion, rewards or an active battle.
- Home opens this window through Rutinas or the training action and receives the chosen language on return. Character appearance is carried across without affecting training.
- English/Spanish, safe-area scaling, pointer/keyboard controls, scrollable content, and loading/empty/retry states.

These are **fictional local examples**, visibly labelled on every page. They are not a live account service, a production personal-plan generator or a workout logger. The existing training fixtures and rules remain the prescription source. Boss battle execution, in-session difficulty changes and real completion logging belong to the next complete-window stage.

## Build and run

From this checkout:

```bash
/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelWorkoutBuild.BuildLinux \
  -logFile /tmp/sologym-workouts-build.log

app/Builds/Workouts/SoloGymWorkouts.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-review -sologym-locale es -sologym-stay-open
```

The regular Welcome entry also accepts `-sologym-window workouts`; Home opens the same class. For reproducible visual review use `-sologym-date 2026-09-30`. `-sologym-teen` exercises the teen gates, and `-sologym-workout-profile adult_home_beginner` reviews the no-equipment fixture. Production onboarding/profile binding remains future work.

Local examples use a separate versioned file per fixture profile under `Application.persistentDataPath`. `-sologym-journal-file /tmp/my-review.json` overrides it. `-sologym-empty` starts empty only when that file does not exist. Corrupt/incompatible files are retained and show Retry; the app never silently replaces them. The preview namespace is not a migration of real user history.

`-sologym-smoke -sologym-capture /absolute/path/window.png` runs the focused native suite, exports state screenshots and a `.smoke.json` report, then exits. Without an explicit file path, smoke uses a unique temporary journal. Run with an isolated `XDG_CONFIG_HOME` as well to avoid changing normal language preferences.

## Art provenance

See `assets/sprites/pixellab/workouts-room-r1/manifest.json` and `production/` for source jobs, prompts, hashes and costs. All native source bytes are preserved. The initial journal export lost its blank pages during background removal; it is retained and reused only as the transparent leather/spine overlay. A second opaque export supplies blank parchment beneath it. The outer brass frame and all buttons reuse existing component sprites. The approved full-screen render is a reference, never a runtime background.

Quoted PixelLab use: 40 generations for the room at concept stage, 40 for each of the two journal exports (120 total including concept; 80 during implementation). No characters or animations were generated.

## Verification

See `verification.json` and `artifacts/visual/Workouts/` for native captures and detailed checks. The fixture and eligible-swap exporters are deterministic; the existing Python training suite remains unchanged. Source/native/runtime image hashes and sprite import settings are recorded separately from visual approval.
