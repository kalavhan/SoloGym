# SoloGym

Mobile fitness RPG for Android and iOS, ages 15+, in English and Spanish. This repository contains native Unity Welcome, age/region, privacy/consent and Home interfaces, approved manhua System references, research, training data, an offline generation reference and the window milestone plan.

**Current milestone: WIN-009 Private Fitness Profile — native UI implemented and verified in Linux 0.4.0.** Optional metric/imperial measurements, privacy notice, readiness and pause steps use the approved bilingual references. Back preserves the in-memory draft across the previous window. [Implementation and captures](docs/windows/WIN-009-implementation.md). Production data-use policy and server persistence remain unavailable; review mode never saves measurements or authorizes a finished profile. No APK was rebuilt. [WIN-010 Goals / Experience](docs/windows/WIN-010-goals-experience-g0.md) has retained EN/ES renders awaiting approval.

The user reported the existing Android 0.2.1 Welcome build working (“perfect, is working great, next window”). This is user acceptance, not independent provider-by-provider verification.

The user installed WIN-001 on Android and accepted its appearance on 2026-09-25: “logre instalarla, se ve exactamente como lo esperaba. cual es el siguiente punto? sigamos”. [Home captures and comparisons](docs/windows/WIN-001-implementation.md#current-visual-evidence) remain unchanged. Home still presents fictional local data and a static avatar; this acceptance does not certify pixel-identical output or exhaustive device/feature testing.

The training data shape and milestone plan are accepted for continued design; exercise content review is still pending. Automatic generation creates editable workout plans for bosses; players enter repetitions, sets, load and time manually.

## Start here

| Deliverable | Contents |
| --- | --- |
| [Private profile implementation](docs/windows/WIN-009-implementation.md) | Optional measurements, feet/inches and lb conversion, staged privacy/readiness, native captures and focused checks |
| [Goals / Experience proposal](docs/windows/WIN-010-goals-experience-g0.md) | Retained ES/EN renders and generator-compatible choices, awaiting approval |
| [Age/region and privacy implementation](docs/windows/WIN-006-007-implementation.md) | Native two-window UI, in-memory drafts, review entry, pending production dependencies and focused verification |
| [Approved onboarding targets](docs/windows/WIN-006-007-onboarding-proposal.md) | Four retained EN/ES renders, approved flow and data contracts |
| [Next window: private fitness profile](docs/windows/WIN-009-private-fitness-profile-g0.md) | Retained EN/ES renders, staged flow and private data shape; target approval pending |
| [Welcome implementation and visual evidence](docs/windows/WIN-002-implementation.md) | Native entry and email form, retained captures, focused checks and current delivery limits |
| [Welcome / Sign in — approved target](docs/windows/WIN-002-welcome-sign-in-g0.md) | Bilingual entry flow, data, states and retained v1 renders |
| [Native Unity app](app/README.md) | Unity project, build/run commands, Welcome entry and Home preview |
| [Firebase integration setup](docs/engineering/firebase-auth-setup.md) | Reproducible SDK installer, project configuration and real-device verification requirements |
| [Home implementation and visual evidence](docs/windows/WIN-001-implementation.md) | Scope, final EN/ES player captures, reference comparisons and focused verification |
| [System / Home — approved target](docs/windows/WIN-001-system-home-g0.md) | Retained EN/ES renders, flow, states, data and wording |
| [Visual reference contract](docs/design/visual-reference-contract.md) | Versioned image references, exact approval scope and screenshot comparison for 1:1 matching |
| [Training data and generation specification](docs/training/generation-spec.md) | Input/output contracts, selection, difficulty, recovery, substitutions, boss progress, limitations and commands |
| [Exercise catalog](data/training/exercises.json) | 49 original bilingual records: 31 strength, 7 cardio and 11 mobility |
| [Starter templates](data/training/templates.json) | Six structures for home, gym, aerobic activity and mobility |
| [Example plans](data/training/examples/) | Seven fictional adult/teen profiles, including recovery and review states |
| [Window glossary and milestone plan](docs/product/window-glossary-and-milestones.md) | 64 windows, each with user steps, data, assets, dependencies and acceptance conditions |
| [Asset production and animation sources](docs/design/asset-production-and-animation-sources.md) | Serial approval gates, customizable characters, visible gear, sprite feasibility and media licensing research |
| [Avatar asset contract](data/art/avatar-contract.json) | Proposed skin/hair layers, body fits, equipment attachments and synchronized actions; no production assets yet |
| [Research](docs/research/2026-09-25-fitness-game-research.md) | Evidence and product implications, with source links |
| [Accepted visual direction](design/README.md) | Three concept renders; individual assets and windows still need approval |

## How we will build each window

Work follows the currently approved window or explicitly grouped iteration. Continue the user's target-render development workflow: retain and approve the window proposal, map the approved image, assemble the complete screen, then perform a small number of whole-screen visual and interaction checks. No per-button rendering loop is used. **WIN-006/007 v1 targets are approved together; their native output is checked separately against those retained targets.** The original staged asset plan remains recorded for separately scoped modular character work.

The approved Home target's exact PNGs, prompts, dimensions and hashes are retained in [the reference manifest](design/reference-manifests/WIN-001-system-home-v2.json). The full-screen comparison preserves remaining differences from native fonts, reconstructed glass and the shared artwork across locales. The user has accepted the installed Home's appearance; the source renders and measured differences have not been replaced or relabeled as byte-identical. Approval state is recorded in [the ledger](design/approval-ledger.json).

The Home preview uses Unity 6000.3.24f1 and a static illustrated character. The planned customization workflow uses a modular source character with body fits, skin masks, separate hair and fitted equipment. A small rig/sprite export proof must establish the avatar approach before a full asset library is produced. The [local tool inventory](docs/design/local-image-tool-inventory.md) records available imaging tools; it is not evidence that an avatar pipeline has been built.

## Inspect the training reference

Use Python with the dependency in `requirements-data.txt`, then run from this directory:

```bash
python3 tools/training_reference.py validate
python3 tools/training_reference.py examples
python3 -m unittest discover -s tests -v
```

The current reference passes 22 behavior tests covering age/readiness, supervision, equipment, recovery spacing, timing, edits, duplicate logs and progression. It is an authoring prototype, not the app backend or a production reward service.

The training content and exact prescription defaults remain drafts requiring exercise-professional and youth review before release. Technical validation does not supply that review. Missing equipment produces explicit coverage gaps; body appearance and fasting do not determine workout prescriptions or rewards. Exercise demonstration records remain unassigned pending rights and technique review.

The repository is [kalavhan/SoloGym](https://github.com/kalavhan/SoloGym). The Home implementation is merged into `main`; Welcome and authentication work is maintained on `codex/welcome-auth`. Generated player builds, signing material, environment-specific Firebase configuration and Unity caches are excluded from Git.
