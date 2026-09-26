# WIN-010 — Goals / Experience implementation

Status: **native UI implemented; Linux verification and user acceptance pending**. Date: 2026-09-26.

G0 flow, EN/ES copy, teen subset and uncertainty behavior are recorded in [WIN-010-goals-experience-g0.md](WIN-010-goals-experience-g0.md). Step 1 retains bilingual proposal PNGs as hash-pinned targets; steps 2–3 use the shared portal frame and live text only.

## Scope

`GoalsExperienceState.cs` holds an in-memory draft of `goal` and `experience` aligned with [training-profile.schema.json](../../data/schemas/training-profile.schema.json). Catalog entries load from `Resources/GoalsExperience/Catalog.json` (mirrors [data/windows/WIN-010-goals-experience.json](../../data/windows/WIN-010-goals-experience.json)).

`GoalsExperienceScreen.cs` composes the window with `SystemUI.PortalPage(718, 965)`, choice rows matching WIN-009 patterns, an uncertainty modal, and a review step with change links.

## Navigation

- Onboarding review: WIN-009 checkpoint `REVIEW:WIN-010` opens this window via `OnboardingScreen.OpenGoals`.
- Continue on review emits `REVIEW:WIN-011` and shows an honest equipment-window notice (WIN-011 not implemented).
- Back from step 1 returns to private profile when opened from that chain; language changes preserve the draft; exit clears unsaved choices.
- Non-review mode blocks the final checkpoint with `training_setup_pending` (no fake save).

## Teen and uncertainty

- Teen audience: goals filtered to `general_fitness` and `mobility` (fixture age &lt; 18 from onboarding, or `-sologym-audience teen`).
- “Not sure” opens a confirmation dialog; beginner is applied only after explicit confirm.

## Verification

Focused state checks live in `GoalsExperienceStateChecks.Run()`. Capture with `-sologym-window goals -sologym-review` and optional `-sologym-smoke`. No APK was built in this iteration.

| Evidence | Path |
| --- | --- |
| Goal step ES | `artifacts/visual/WIN-010/goals-es-final.png` |
| Goal step EN | `artifacts/visual/WIN-010/goals-en-final.png` |
| Smoke output | `artifacts/visual/WIN-010/goals-es-final.smoke.json` (`passed: true`) |

## Out of scope

Firestore persistence, plan generation, equipment (WIN-011), schedule (WIN-012), avatar feedback, and APK rebuild.
