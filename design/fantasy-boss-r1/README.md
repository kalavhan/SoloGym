# Boss workout — complete native window

The user approved `boss-workout-concept.png` with **“it is okay for now, let's go.”** This window follows merged PR #29 and connects its landscape Rutinas journal to a routine-driven dungeon. The room, selected Barbarian, guardian and controls are independent assets/components.

Enter through Home → Rutinas → Prepare session. Review current readiness, supervision where applicable, the full prescription and equipment limitations; acknowledge and select **Enter dungeon**. Saving a review alone still does not start a workout. The screen covers warm-up, manual set logging, prescribed rest between sets, cooldown, pause/resume, stopping and a summary. Empty quantity fields require explicit entry; zero is valid. Reps/seconds/minutes and unilateral lower-side counts come from the prescription. Optional load never increases combat progress. The static character is not an exercise demonstration.

Easy/Medium/Hard remain available during exercise, rest and pause. `tools/export_boss_options.py` exports 42 eligible contexts and their substitutions through the existing research-backed reference rules. Changing difficulty preserves selected exercise identities and recorded prescriptions, changes only unfinished blocks/sets and rejects combinations exceeding the original estimated time budget. Readiness and beginner/teen caps remain in force. A short budget can result in identical doses for more than one difficulty. Estimated time is not a countdown deadline.

Each recorded set receives a fixed part of its exercise's original progress budget. Subsequent sets split only the still-unallocated portion. Progress uses the existing capped quantity/minimum ratio; extra reps, load, duplicate submissions and difficulty toggles cannot increase the 1,000-point budget. Corrections replace the quantity/load of a record, retaining its original prescription and share. Retired sets do not award unearned progress or reopen when difficulty is raised. A completed routine can therefore leave HP in the bar; the summary explicitly requires no extra exercise to empty it.

All writes are atomic through the journal's existing storage. A failed set save retains the input and active set for retry. An active session locks its own journal entry against edits and a second start. Saving and returning keeps the session paused. After a restart, resume requires fresh readiness; pain/injury/illness or unavailable teen supervision stop the prescription. Completion versus stopping is stored with the actual manual records and final plan. View full routine in journal history also shows the saved quantities and optional loads; failed archive writes preserve the summary for retry. Old v1 journals remain readable, with absent optional session fields normalized after Unity deserialization. Corrupt files are preserved, never silently reset.

These remain **fictional local examples**, separate from personal account data. No cloud synchronization, trusted rewards, rep detection, character animation, ads, billing, MOBA or fasting implementation is included. Training content remains pending exercise-professional/youth review. Screenshots and JSON smoke reports are under `artifacts/visual/Boss/`.

## Art provenance

- Original PixelLab room: `assets/sprites/pixellab/boss-chamber-r1/native/chamber.png`, 640×360, 40 quoted generations.
- One static stone guardian: `assets/sprites/pixellab/stone-guardian-r1/native/guardian.png`, full 256×256 transparent canvas, 20 quoted generations. Its prompt, submission and result metadata are preserved in `production/`.
- All eight approved Barbarian fronts remain unchanged. Home's selection and locale carry into this window.
- Runtime copies under `app/Assets/SoloGym/Resources/Rooms/BossR1/` preserve source bytes. The parchment reuses one page of the existing journal texture through UV coordinates; frames, fields and actions reuse existing pixel skins. Values, HP fill, labels, focus and controls are live uGUI.
- The flattened render is only a visual reference. The native room preserves the provider's actual geometry rather than the concept generator's reinterpretation.

## Build and review

From the repository checkout:

```sh
/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath "$PWD/app" -executeMethod SoloGym.Editor.PixelBossBuild.BuildLinux \
  -logFile /tmp/boss-build.log
app/Builds/Boss/SoloGymBoss.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-review -sologym-locale es
```

The standalone build opens readiness; it does not automatically start exercise. Add `-sologym-smoke -sologym-capture "$PWD/artifacts/visual/Boss/window-es-1280.png"` for isolated native review records, full flow checks and captures. The automated fixture supplies readiness answers only in that explicit smoke mode. Use `-sologym-workout-profile adult_home_beginner`, `-sologym-teen`, `-sologym-character female-muscular`, `-sologym-locale en` and `-sologym-safe-inset 24` for other fixtures. Regular app entry also supports `-sologym-window boss`; Home and Rutinas need no special flags.

Verification commands:

```sh
python3 -m unittest discover -s tests -v
python3 tools/export_training_preview.py --check
python3 tools/export_workout_journal_options.py --check
python3 tools/export_boss_options.py --check
python3 tools/check_boss_assets.py
```

Final verification: 452 Spanish dungeon checks at 1280×720, 450 English home/beginner checks at 854×480, 459 Spanish teen/safe-area checks at 1600×720, and all 513 existing journal regression checks pass. The 22 Python training tests, three deterministic fixture exports and source-art integrity check also pass. `verification.json` links the reports and native captures.
