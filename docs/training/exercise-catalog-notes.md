# Exercise catalog — draft content, 25 September 2026

`data/training/exercises.json` contains **49 original bilingual exercise records**: 31 strength, 7 cardio, and 11 mobility or movement-practice options. It is content for shaping the training system, not a clinically approved library or a prescription for an individual. The document is explicitly marked `draft_requires_professional_review`. Qualified exercise-professional and youth-specific review are required before production use. The English and Spanish instructions also require technique, translation, and user-comprehension review alongside the eventual demonstrations.

The catalog supports manual logging. It contains no camera-based repetition counting, pose estimation, calorie targets, fasting targets, body-shape classification, or automatic loading from height and weight.

## What the research supports

The adult guidance supports using bodyweight, elastic resistance, and conventional equipment, with an individualized program and regular participation. It does not require failure training or a complicated routine for the average healthy adult. That supports this small catalog's equipment variety; it does not prove the suitability of every exercise or cue here. [ACSM 2026 guidance](https://acsm.org/resistance-training-guidelines-update-2026/)

Youth resistance training needs age-appropriate instruction, technique, and supervision. Chronological age alone does not establish training competence. The catalog therefore treats all resistance exercises, including bodyweight and static core work, as requiring suitable supervision for teen programming. This is a conservative product rule derived from the guidance, not a claim that the AAP specifies this exact database flag. [AAP 2020 guidance](https://www.healthychildren.org/English/news/Pages/Guidance-on-Resistance-Training-for-Children.aspx)

Aerobic programming should account for relative effort. Walking speed or machine resistance alone does not establish intensity for a particular person. The talk-test cue in the draft cardio content follows CDC's intensity guidance; it is not interchangeable with a strength-training effort scale. [CDC intensity guidance, December 2025](https://www.cdc.gov/physical-activity-basics/measuring/index.html)

The WHO recommendations address physical activity across age groups, including balance-oriented multicomponent activity for older adults. The supported tandem-balance record is a starting content option; one exercise is not a complete falls-prevention program, and this draft does not claim specialized older-adult programming. [WHO 2020 guidelines](https://www.who.int/publications/i/item/9789240015128)

`source_ids` identify these programming foundations and resolve through the shared source registry. **They are not exercise-specific safety certifications or evidence that an institution reviewed SoloGym's wording.** All exercise names, cues, classifications, experience levels, setup estimates, and substitution links are original proposed content decisions. Mobility choices are not established therapeutic interventions by virtue of citing AAP or WHO.

## Field semantics the generator must preserve

| Field | Meaning and constraint |
| --- | --- |
| `id` | Stable content identifier; labels may change without changing saved workout references. |
| `name` / `cues` | English and general Latin American Spanish; all records have three short cues per language. A cue preview does not replace complete instruction or supervision. |
| `category` / `movement_pattern` | Selection and balance tags. These are coarse categories, not exhaustive muscle maps. `mobility` includes movement practice and supported balance. |
| `equipment_all` | Every listed item is required. An empty list means no dedicated equipment; a clear, suitable floor or walking route is still necessary. |
| `environments` | Places where the exercise can be offered when its actual space and equipment needs are met. `gym` walking means a clear walking route, not an implicitly available treadmill. |
| `min_experience` | Conservative eligibility floor, not a diagnosis or proof of technique. Beginner does not mean universally suitable. |
| `age_groups` | `teen` means ages 15–17 in this product; `adult` means 18+. Inclusion does not bypass supervision, readiness, or other suitability checks. |
| `supervision_required_for_teens` | All strength exercises are `true`. False on cardio/mobility does not promise that unsupervised activity is suitable in every context. |
| `prescription_unit` | Work is logged in repetitions, seconds, or minutes. It does not define sets, load, rest, intensity, or progression. |
| `unilateral` | When true, the assigned amount is **per side**, including holds. The UI must say “each side” / “por lado.” A set is complete after both sides; do not double XP. |
| `setup_seconds` | Unvalidated time-budget estimate per exercise change, separate from work and rest. It is not a countdown forcing the user to hurry. Machine adjustment may take longer. |
| `regressions` / `progressions` | Candidate alternatives or learning steps, not an automatic upgrade path. Re-run equipment, experience, age, supervision, purpose, and dose checks after any change. |
| `media` | All records are `unassigned`, with null asset/license identifiers. No demonstration is licensed, downloaded, generated, or approved by this catalog. |

Alternating core exercises such as dead bug and bird dog use repetitions **per side**, not total alternating repetitions. The side plank and tandem stance use seconds per side. Bilateral exercises receive the written amount once per set. `standing_calf_raise` is specifically the supported single-leg version; it has `unilateral: true` and an intermediate floor.

The hip hinge practice is a mobility/skill record, not a loaded hinge exercise. Its link to the dumbbell Romanian deadlift describes learning progression, not equal strength stimulus. Reducing a loaded exercise to skill practice requires a clear plan change; it must not silently count as unchanged strength work. Regression candidates can require equipment the original does not need, such as a wall or fixed raised surface.

The walk/jog record requires explicit walking and jogging segments in a future interval-capable prescription. A single undifferentiated minute value does not provide those timings; do not select it until that prescription is available. All-out intervals are absent.

## Equipment and selection boundaries

The equipment vocabulary currently used is: `chair`, `wall`, `stable_surface`, `dumbbells`, `resistance_band`, `band_anchor`, `low_step`, `cable_machine`, `lat_pulldown_machine`, `leg_press_machine`, `chest_press_machine`, `shoulder_press_machine`, `stationary_bike`, `elliptical_machine`, and `stretch_strap`.

- `chair` means a firm, non-wheeled chair secured against sliding. It is not a climbing platform.
- `stable_surface` means a fixed support suitable for the intended load; an incline push-up can put substantial bodyweight through it. User inventory must confirm the actual use, not merely the presence of furniture.
- `band_anchor` means a rated compatible anchor installed and used according to the manufacturer's instructions. Do not substitute a loose chair, unverified door, or improvised attachment.
- `low_step` means a stable, non-slip exercise step at a manageable height. Do not substitute a chair.
- `cable_machine` for the row means access to a seated-row configuration with its intended seat, foot support, and handle. A generic pulley alone is insufficient.
- Machine exercises require appropriate adjustment and equipment that fits the user. “Gym access” is not proof that every machine is available.
- A comfortable clean floor and optional mat/padding are environmental needs for floor exercises. Padding mentioned in a cue is not a load-bearing fitness device.

The no-equipment selection deliberately lacks a resisted pulling exercise. Shoulder circles, arm waving, and upper-back rotation must not masquerade as a row. A plan using no equipment must acknowledge this coverage gap or request appropriate reviewed equipment; do not silently call it equivalent to a complete push/pull strength plan.

No Olympic lifts, maximum tests, forced failure, ballistic lifting, loaded jumping, unsafe furniture rows, or automatically escalated resistance are included. These omissions are scope choices for this first draft, not claims that the activities are inherently unsafe for everyone.

## Review and media handoff

Review each record for technique, reasonable suitability boundaries, equipment fit, age experience, substitution purpose, unilateral notation, and localization before approving it. The minimal schema is intentionally not a contraindication engine. It does not yet encode condition-specific exclusions, pregnancy adaptations, disability-specific adaptations, pain-location filtering, or rehabilitation protocols. A generator cannot infer those from movement patterns.

For every eventual demonstration, match the exact variant, setup, support, range, and bilateral/unilateral behavior. Record the provider, asset ID, redistribution/app-embedding rights, attribution, modification rights, and reviewer approval before assigning `media.asset_id` or `media.license_id`. A video appearing on a public webpage does not supply app-reuse permission. Anatomical motion and instruction should be verified by qualified reviewers; a visually convincing AI animation alone is insufficient.

Validation at creation checked unique identifiers, all required fields, bilingual cues, enumeration values, reference integrity, unilateral units, and the unassigned media state. These checks verify data consistency, not exercise safety or real-world effectiveness.
