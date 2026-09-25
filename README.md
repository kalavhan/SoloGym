# SoloGym

Mobile fitness RPG for Android and iOS, ages 15+, in English and Spanish. This repository contains the native Unity Home preview, approved manhua System references, research, training data, an offline generation reference and the window milestone plan.

**Current milestone: WIN-001 System / Home — implemented and ready for visual review.** The complete native screen uses measured reference coordinates, live text and controls, and persistent English/Spanish selection. [Retained player captures and comparisons](docs/windows/WIN-001-implementation.md#current-visual-evidence) show the result against the approved v2 renders. This is a Home preview with fictional local data; destination windows, account services and modular avatar customization remain future milestones. The comparison does not certify a pixel-identical match.

The training data shape and milestone plan are accepted for continued design; exercise content review is still pending. Automatic generation creates editable workout plans for bosses; players enter repetitions, sets, load and time manually.

## Start here

| Deliverable | Contents |
| --- | --- |
| [Native Home app](app/README.md) | Unity project, build/run commands, language and state review controls |
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

One window is active at a time. For **WIN-001**, the user's latest instruction supersedes the earlier per-asset stops: use target-render development, map the approved image, assemble the complete screen, then perform a small number of whole-screen visual and interaction checks. No per-button rendering loop is used. The original staged asset plan remains recorded for later windows and modular character work.

The approved Home target's exact PNGs, prompts, dimensions and hashes are retained in [the reference manifest](design/reference-manifests/WIN-001-system-home-v2.json). The full-screen comparison preserves remaining differences from native fonts, reconstructed glass and the shared artwork across locales. Runtime visual acceptance is still separate from approval of the source target; the source renders have not been replaced. Approval state is recorded in [the ledger](design/approval-ledger.json).

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

The repository is [kalavhan/SoloGym](https://github.com/kalavhan/SoloGym). The research and approved-reference foundation is published on `main` at [`b0acc47`](https://github.com/kalavhan/SoloGym/commit/b0acc472d6f660d0cd6f7246eda7f7035dce0efe). Home implementation is maintained on `codex/system-home`; generated player builds, signing material and Unity caches are excluded from Git.
