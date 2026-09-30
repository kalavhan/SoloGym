# Training Hub, readiness and plan review

The user authorized continuing windows that require no new sprites on 2026-09-27, while they author replacement character equipment. This pass reuses the existing portal background, wordmark, frames, icons and native Unity controls. No new sprites or animation were generated. Visual review is pending; this is not a claim of G6 approval.

## Implemented local flow

- WIN-015: training hub with explicit fictional home, gym and teen profiles; review and voluntary rest paths.
- WIN-016: unanswered readiness by default, five responses, teen supervision answer, and 15/25/40-minute budget. Readiness must be checked on each new visit.
- WIN-017: bilingual warm-up/main/cool-down, sets, quantity, per-side instructions, rest and declared equipment gaps. Saving requires explicit acknowledgment and stores a reviewed fixture key locally. It never starts a workout or grants rewards.
- Stop/recovery responses and missing teen supervision preserve the existing rule outcomes. Insufficient time remains a needs-changes result.
- Home Train and fresh readiness navigation open the flow in review mode. Existing saved-session routes are not substituted with a new sample plan. Production authentication/profile gates are unchanged.

The catalog is a deterministic export of `tools/training_reference.py`, with 60 combinations of the existing fictional profiles, time, readiness and supervision. `tools/export_training_preview.py --check` checks drift, including source hashes. The mobile app does not yet generate personal plans or connect to a training service. Draft exercise content still needs professional/youth review. Manual logging (WIN-024), live profile integration, cloud persistence and rewards are subsequent work.

## Verification

- Unity 6000.3.24f1 Linux build succeeds.
- Existing 22 Python training tests and data validation pass.
- Training smoke checks readiness/acceptance gates, light mode, all three stop responses, teen supervision, invalidation, time budgets, bilingual content and nine native UI interactions. Tests restore the previous local reviewed-draft preference.
- EN at 853×1844 and ES at 393×852 captured for readiness and plan review under `artifacts/visual/TrainingReview/`; Spanish hub also captured. Text and controls visually inspected. Android/iOS device verification is pending.
- Character interactive capture and legacy regression checks pass under a virtual display; captures remain open without explicit quit/smoke flags.

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window training -sologym-locale es
```

Use `readiness` or `plan` for direct review capture routes. `-sologym-training-profile teen` selects the teen fixture; the direct plan fixture requires `-sologym-supervision yes` to show its supervised version. `-sologym-readiness pain` demonstrates recovery. Direct plan routes preselect fictional inputs solely for review; interactive entry always requires the readiness choices. Add `-sologym-training-smoke` for state/UI checks and an intentional exit, or `-sologym-quit-after-capture` for capture-only exit.
