**SoloGym training-data foundation — M0**

**Design status:** the user accepted this data shape for continued design on 25 September 2026. Content review and release approval remain pending. The active design review is now [WIN-001 System / Home G0](../windows/WIN-001-system-home-g0.md).

This first package contains original bilingual content, structured draft rules, and an executable offline reference that produces example plans. It does not implement the mobile app. The research informs its principles; the exact exercises, dose ranges, time estimates and scheduling choices remain draft product content requiring exercise-professional and youth review before public release. Technical checks do not supply that review.

Automatic means **assembling an editable workout plan for the session's boss**. Players manually enter sets, repetitions, time, and equipment load. Camera counting, wearable detection, body-fat inference, and technique certification are outside this version.

| File | What it contains |
| --- | --- |
| `data/training/exercises.json` | 49 original EN/ES records: movement pattern, category, equipment, environment, experience, age applicability, supervision, unit, per-side semantics, cues and variation references |
| `data/training/equipment.json` | 16 stable physical-equipment identifiers with names and setup notes |
| `data/training/templates.json` | Six curated session structures: Foundation A/B, gym foundation, bodyweight foundation, aerobic base and movement practice |
| `data/training/rules.json` | Draft age, difficulty, dose, readiness, recovery, progression and reward policies |
| `data/training/sources.json` | Five primary/authoritative source references and the limited claims they support |
| `data/training/media-registry.json` | One unresolved demonstration record per exercise; no copied video or vendor data |
| `data/schemas/` | JSON Schema contracts for exercise content and training inputs |
| `data/training/example-profiles.json` | Seven fictional adult/teen, home/gym, reduced-readiness and supervision examples |
| `data/training/examples/` | Generated weekly draft JSON for those examples |
| `tools/training_reference.py` | Reference generation, edit validation, boss-progress preview and progression suggestions |

The session generator follows this order:

1. Validate the input and recognized identifiers. Age must be at least 15. Require explicit environment, actual available equipment, experience, goal, session time, weekly frequency, current readiness, supervision availability, exclusions, preferences, and reported need for individualized guidance.
2. Evaluate readiness and restrictions. Illness, pain or injury produces recovery rather than a lighter forced workout. A reported need for professional guidance produces a review state. Teen strength sessions require appropriate supervision in this draft policy.
3. Choose a structured week and session template. Ordinary adult goals include general fitness, strength, muscle growth, endurance and mobility. Teen strength/muscle/endurance goals use a general-fitness foundation; an explicitly chosen mobility-only plan is labeled honestly as mobility.
4. Filter exercises by their pattern, category, equipment, environment, experience, exclusions and supervision. Use stable ordered choices and compatible preferences. The same inputs and content version produce the same plan; do not shuffle a novice's exercises every visit.
5. Apply a bounded dose and difficulty. Medium is the baseline. Light reduces work. Hard is limited to a ready intermediate adult and does not automatically increase load or require failure. Beginner and teen requests for Hard are reduced to Medium; low readiness uses Light. The interface must explain these adjustments.
6. Budget time using both sides for unilateral work, set count, the upper rep target, between-set rests, setup, transitions, warm-up and cooldown. If needed, reduce sets or cardio duration within the draft bounds. Preserve exercise purpose and rest. If the plan still cannot fit, return `needs_changes`; do not silently squeeze rest or tell the player to rush.
7. Preserve recovery between scheduled strength dates, including recent logged sessions. The current scheduling heuristic avoids consecutive strength dates. It is not a claim that two calendar days guarantees individual recovery. A missed session does not create extra work to repay.
8. Return the exercises, units, rationale messages, declared coverage gaps, time estimate and boss mapping for user review. Recheck readiness on the actual training day; a week generated today cannot know next week's health state.

The default two-session week places strength on Monday/Thursday. General-fitness three-session weeks use strength Monday/Friday with aerobic activity Wednesday. An intermediate adult strength-focused three-session week uses Monday/Wednesday/Friday strength. Four/five-session variants include suitable aerobic or mobility sessions and recovery. These are transparent starting patterns, not a complete scheduling service or a claim that the app's sessions alone satisfy population activity recommendations. Users' other sport, school, walking and activity matter. The first reference supports two to five selected sessions a week and 15–60 minute session budgets.

**An honest coverage gap is better than a false substitution.** A home setup without suitable pulling equipment can receive a usable partial routine with `pull` recorded as a coverage gap. Mobility drills are never counted as resisted pulling. A preference or edit must preserve the allowed movement pattern, category, unit and eligibility. A hinge-learning drill can be a useful regression in the catalog without being an equivalent replacement for a loaded hinge. Walk/jog intervals are cataloged for future use but blocked from unsegmented templates and overrides.

In the current examples, a beginner home strength session uses chair squats, glute bridges, wall pushups and dead bugs, and reports the missing resisted pull. The supervised teen example adds an anchored band row. The gym example selects machine/dumbbell movements only when the named equipment is present. Illness gives a recovery result with no boss prescription, while a teen strength request without supervision gives `needs_review`.

**Data details need to stay unambiguous.** Repetition ranges are per set. A unilateral record means the prescription applies to each side; timing includes both sides. `load_kg: null` means the user selects and enters an appropriate load, not zero kilograms and not an inferred load. The future UI may display pounds but should store a canonical unit and the entered unit. The aerobic effort scale is separate from resistance effort. Body type, avatar build, skin color, hair, bodyweight, fasting and purchased gear are not generator inputs.

The profile has a self-reported `professional_guidance_needed` flag to route existing restrictions or individualized needs to review. It does not diagnose medical conditions or declare someone medically cleared. Likewise, `teen_supervision_available` records a requirement for the proposed session, not verified supervision. Clinical, pregnancy-specific, rehabilitation, advanced athletic and fully tailored older-adult programming are not implemented by these starter templates. The adult age range is accepted for data testing; that should not be marketed as validated age-specific coverage. Older-adult balance content is present for later reviewed programming.

**Edits revalidate the plan.** Swapping a main exercise requires an eligible exercise with the same unit and an allowed movement pattern/category. The replacement must fit the time budget, preserve per-side meaning, and not duplicate another block. Changing readiness, equipment, age, time or another profile input invalidates the old draft's context; generate a revised draft before editing it. Arbitrary set/repetition editing and transferring already logged work into a revised routine still require the later editor/session design. The reference does not pretend those flows are complete.

**The boss reflects planned work.** Main exercises split a fixed 1,000-point boss budget. Each prescribed set earns at most its share when its minimum target is recorded. Warm-up/cooldown stay in the routine but are not damage farming opportunities. For unilateral exercises, the logged quantity represents the lower completed amount across both sides. Duplicate event IDs do not count twice, and a new correction for the same set replaces its earlier quantity. Additional reps and heavier loads do not raise the maximum damage or reward budget. Stopping preserves the record and does not impose a penalty.

The current 100 XP and 60 coins are fictional economy placeholders, equal across difficulty choices. They are not committed balance values. `boss_progress` is a pure preview, not a production reward service: authentication, trusted plan storage, concurrent edits, offline synchronization, reward issuance and cross-device idempotency remain future implementation work. Manual logs are not proof of exercise.

Progression is a review suggestion after two distinct sessions of the same exercise with an upper target completed, manageable effort, ready status, and no reported pain. It proposes reviewing one small change with the user or supervisor. It never returns a new kilogram value, orders maximum testing, or automatically upgrades the exercise. Personal capability feedback should refine these rules during the training-window design.

To inspect the current package from the repository root:

```bash
python3 tools/training_reference.py validate
python3 tools/training_reference.py examples
python3 -m unittest discover -s tests -v
```

The reference uses Python and `jsonschema` for local authoring checks; this does not choose the mobile application's language or engine. Use a single profile JSON with `generate --profile <path> --week-start 2026-09-28 --output <path>` to produce another draft. Start dates are Mondays. Sample dates and people are fictional. Dependencies are listed in `requirements-data.txt`.

M0 review should focus on the taxonomy, required inputs, understandable outputs, coverage gaps, edit rules and how a workout becomes a boss. The data has to remain explicitly draft until content review is recorded. Once this foundation is accepted for the design phase, the first window is System/Home: define its data/flow, then generate its background alone and wait for approval.

The source basis is [WHO activity guidance](https://www.who.int/publications/i/item/9789240015128), [ACSM adult resistance guidance](https://acsm.org/resistance-training-guidelines-update-2026/), [AAP youth resistance guidance](https://www.healthychildren.org/English/news/Pages/Guidance-on-Resistance-Training-for-Children.aspx), [CDC aerobic-intensity guidance](https://www.cdc.gov/physical-activity-basics/measuring/index.html), and [AAP recovery guidance](https://www.healthychildren.org/English/health-issues/injuries-emergencies/sports-injuries/Pages/Too-Much-Too-Soon-Overtraining.aspx). Exact implementation defaults are product proposals, not quotations or claims of endorsement by those organizations.
