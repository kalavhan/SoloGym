# SoloGym

Mobile fitness RPG for Android and iOS, ages 15+, in English and Spanish. The overall manhua System visual direction is approved. This repository currently contains research, training data, an offline generation reference, concept renders, and proposed window milestones—not a working mobile app.

**Current milestone: WIN-001 System / Home — G0 brief and rendered proposal v2 review.** The proposal now embeds retained English and Spanish previews for 1:1 visual matching. The training data shape and milestone plan are accepted for continued design; exercise content review is still pending. Automatic generation creates editable workout plans for bosses; players enter repetitions, sets, load and time manually.

## Start here

| Deliverable | Contents |
| --- | --- |
| [System / Home — current approval sheet](docs/windows/WIN-001-system-home-g0.md) | Embedded EN/ES proposal renders, flow, states, data, wording and next asset gate |
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

One window is active at a time. Approve its flow, data and rendered proposal first, then its background alone, environment props, character base, customization, wearable gear, motion/export, UI components, and assembled production composite—in that order, with a separate stop for approval at each stage. The composite must match the approved proposal or receive approval as a revised target. Implementation and visual verification follow. Shared assets require approval of the exact reused version.

The first window is **WIN-001 System / Home**. Its [G0 brief with rendered proposal v2](docs/windows/WIN-001-system-home-g0.md) is awaiting approval. The exact PNGs, prompts, dimensions and hashes are retained in [the reference manifest](design/reference-manifests/WIN-001-system-home-v2.json). After approval, generate its background alone and stop for G1 review. Separate production assets and app implementation have not begun. Approval state is recorded in [the ledger](design/approval-ledger.json).

For consistent customization, the proposed art workflow uses a modular source character with approved body fits, skin masks, separate hair and fitted equipment. A small rig/sprite export proof will establish whether rendered sprites or live 3D best meet the design before producing a full asset library. Neither the engine nor a local AI toolchain has been chosen. The [local tool inventory](docs/design/local-image-tool-inventory.md) records what was actually found.

## Inspect the training reference

Use Python with the dependency in `requirements-data.txt`, then run from this directory:

```bash
python3 tools/training_reference.py validate
python3 tools/training_reference.py examples
python3 -m unittest discover -s tests -v
```

The current reference passes 22 behavior tests covering age/readiness, supervision, equipment, recovery spacing, timing, edits, duplicate logs and progression. It is an authoring prototype, not the app backend or a production reward service.

The training content and exact prescription defaults remain drafts requiring exercise-professional and youth review before release. Technical validation does not supply that review. Missing equipment produces explicit coverage gaps; body appearance and fasting do not determine workout prescriptions or rewards. Exercise demonstration records remain unassigned pending rights and technique review.

The target remote is [kalavhan/SoloGym](https://github.com/kalavhan/SoloGym). This package is saved locally; no commit or push has been made.
