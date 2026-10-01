# SoloGym: landscape fantasy fitness direction

Recorded 2026-09-29 from the user's product pivot and pricing clarification. Repository: [kalavhan/SoloGym](https://github.com/kalavhan/SoloGym). This document distinguishes confirmed requirements from implementation recommendations and unresolved decisions. It does not claim that the screens or services below are implemented.

## Confirmed direction

SoloGym is a **landscape-only pixel-art fantasy fitness app**, with a personal home gym, editable training presented as dungeon encounters, and a later competitive game. The current concept pass interprets “18 pixel” as the established retro, 16-bit-inspired appearance with readable mobile text; it is not an 18-bit hardware or asset specification.

The first version uses **eight complete Barbarian character appearances**: male and female, each with skinny, medium, fat and muscular builds. It uses their existing hair, skin and eye colors. It does not expose color controls or assemble interchangeable clothing. Future character classes and color customization remain part of the product vision. Selecting a body is an appearance choice, independent of training prescription and combat power.

This replaces the active portrait/manhua presentation and the modular boxing-clothing MVP direction. Preserve their source art, reviews, user-created items and provenance as historical work; useful application logic and fitness research remain relevant. Replace obsolete runtime dependencies through focused changes, rather than deleting the current app before its replacement exists.

## MVP scope

| Area | Required experience | Supporting work |
| --- | --- | --- |
| Account access | Log in and create an account; email/password and Google entry | Recovery/verification, age and consent flow, trusted account/profile state, error/offline states |
| Personal Home | The character lives in a personal home gym, with today's routine and clear navigation | Static character selection, a room assembled from separate objects, local decoration placement as an initial implementation candidate |
| Workouts | See current, completed and missed workouts; review and edit routines | Training profile, equipment, schedule, readiness, Easy/Medium/Hard choices, substitutions, history and persistence |
| Workout dungeon | Present the routine as a battle; completing prescribed work advances the encounter and defeats the enemy/boss | Manual set logging, rest, pause/end, completion and correction handling; preserve useful records when stopped |
| Fasting | Optional neutral adult fasting tracker with timer and history | Suitability/enablement, start/end correction, private storage, always-available End, and under-18 exclusion |

Character selection and fitness onboarding support these five areas; they are not additional game modes. Easy/Medium/Hard is the intended user-facing choice. The existing training foundation uses `light`/`medium`/`hard`; preserve its meaning and adjustments when mapping labels.

**Confirmed 2026-09-30:** training difficulty remains selectable at any time during the boss-battle routine, including an exercise, rest and pause; it is not locked to onboarding or the pre-battle briefing. Keep the control available without restarting the routine. Existing readiness/experience eligibility still applies. Integration must preserve already-recorded work and bounded boss/reward progress; changing a selection must not rewrite completed logs or award progress. Applying an allowed change to remaining work requires the session controller and existing prescription rules, not the appearance selector.

The separately prepared local render pack, `design/fantasy-mvp-r1/`, is not included in the primary-button PR. Its seven visual proposals cover login, create account, character selection, Home, workouts, boss workout and fasting. They are **concepts awaiting visual review**, not approved implementations. They show representative states rather than every recovery, privacy, empty, error or account-support state.

## Full product after the MVP

| Capability | Confirmed intent | Boundary still to design |
| --- | --- | --- |
| Character roster | Eventually three free classes; Barbarian first, with two additional free classes later. Further fantasy classes can include Knight and Aztec Warrior | Which two classes complete the free roster, class kits and release order |
| Appearance | Future skin, hair and eye color customization, with different body builds | Per-region palette/mask production and validation; no promise of automatic recoloring from generated images |
| Character demonstrations | Exercise examples performed by the selected character | Exercise-by-character animation coverage, professionally reviewed movement, costs and delivery order; animation is outside the current static MVP |
| 3v3 MOBA | A separate landscape match game inspired by the format of Mobile Legends/LoL | Combat prototype, original mechanics/art, networking, matchmaking, anti-cheat and balance |
| Fitness progression | Running contributes to character speed; consistent weeks following the routine contribute to strength | Exact formulas, time windows, caps, recovery protection and trusted records |
| Rankings | Separate MOBA standings, weekly running-distance standings and routine-consistency standings | Eligibility, privacy, seasons, corrections, fraud handling and provider permission |
| Activity themes | Later thematic bonuses, such as swimming for an Atlantis-themed warrior | Supported activity evidence and balanced bonus design |
| Social | Moderated chat and an intergalactic gym as a shared meeting place | Moderation operations, reporting/blocking, teen controls and live-presence infrastructure |
| Personal space | Earn/acquire objects, arrange the home gym and apply themes | Inventory, placement rules, sync and economy; the same apparatus and furniture must remain recognizable in their seasonal variants |

**Recommendation, not an approved formula:** cap and balance fitness-derived speed/strength, protect planned rest and illness, and offer meaningful matches for newcomers and people with different abilities. Unlimited kilometers or extra sets should not create unlimited competitive advantage. Paid classes should offer balanced alternatives rather than a direct power purchase. The user has confirmed the activity-to-stat direction, not its numeric balance.

## Commercial model

| Item | Confirmed requirement |
| --- | --- |
| Free tier | Includes ads and, eventually, three free character classes; the first MVP roster is the eight Barbarian appearances |
| Subscription | Target **MX$45/month**; removes ads and grants access to all in-game content while active |
| Additional characters without subscription | **MX$20 per character**, paid purchase only; the user explicitly ruled out buying these characters with fitness points |
| Fitness points | Retain the intended earned game currency; uses such as decoration are candidates, with exact uses and earning/spending rules still to define |

Prices are product targets, not configured store products or verified net revenue. Subscription expiry, restoration, already-purchased content and permanent-unlock behavior must be decided before implementing entitlements; subscription access must not silently become a promise of permanent ownership. Confirm whether a paid character purchase covers all supported body/presentation variants. Ad placement, store billing and subscription delivery are separate work; the screen concepts do not implement or finalize them. Keep health/fitness data out of advertising targeting.

## Research and behavior to retain

The [research brief](../research/2026-09-25-fitness-game-research.md), [training specification](../training/generation-spec.md), `data/training/` and `tools/training_reference.py` remain the fitness foundation. The catalog and dose rules are draft content requiring exercise-professional and youth review before release; technical tests do not provide that review.

- Keep the established age-15+ product boundary, separate adult/teen programming, privacy and consent requirements, and appropriate supervision handling. Private fitness data stays separate from the public character.
- Assemble plans from reviewed structured content using experience, goals, equipment, time, recent training and reported readiness. Changing these inputs invalidates an old prescription. Character class/body, purchased content and appearance colors do not prescribe physical difficulty.
- Preserve bounded difficulty, warm-up/cool-down, recovery and rest. Pain, illness or injury does not become a forced easier workout. Missed sessions do not create exercise debt or punishment.
- Keep routine edits available while validating replacements and dose. Manual logs are user reports, not proof of exercise or technique. Corrections replace prior entries; retries must not double-count rewards.
- Preserve capped progress from planned work. Existing `boss_progress` already limits damage by prescribed set and prevents extra reps/load from increasing its budget. Map completion to the new dungeon presentation without adding real-time combat controls during exercise.
- Fasting remains adult-only, optional and off by default, with no XP, combat buffs, streak rewards or rankings. Ending early loses no progress. The existing 20-hour supported-plan cap is not a safety claim or progression target; actual records may be corrected without encouraging longer fasting.
- Fasting visual direction (2026-10-01): a restrained animated pixel-art magic
  clock, elapsed time and an approximate time-range indication. The render and
  motion proof in `design/fantasy-fasting-r1/` were accepted for implementation
  ("i like it. do it."). The complete native window implements local tracking,
  corrections, history/deletion, adult Home entry and reduced motion. Decorative
  stage changes do not certify ketosis, autophagy, hormone boosts or immune
  regeneration, and do not unlock rewards or extended plans.
- Keep English/Spanish with a persistent language choice, readable text, and existing teen privacy/moderation principles. The Spanish concepts are not a decision to remove English.

## Activity integrations: investigate before committing

Health Connect on Android and HealthKit on iOS are integration candidates; device/app support must be verified with representative accounts and records. Do not assume a Xiaomi/Mi Fitness device exports the necessary running data into either service. Required fields, permissions, source attribution, duplicate detection, revocation and deletion need a specific adapter contract.

Android's guidance includes games using fitness data for progression among its possible use cases, while still requiring appropriate permissions and restricting uses such as advertising. This does not approve SoloGym automatically. [Android health-permission guidance](https://support.google.com/googleplay/android-developer/answer/12991134?hl=en).

Strava is a candidate, not a selected unrestricted backend. Its policy limits disclosure of athlete data and imposes access/retention conditions; public distance rankings and derived multiplayer uses need explicit compatibility review before commitment. Do not assume that a cosmetic transformation of restricted data permits sharing it. [Strava API policy](https://www.strava.com/legal/api_policy), [developer portal](https://developers.strava.com/). Keep the intended activity and privacy model aligned with [HealthKit's privacy guidance](https://developer.apple.com/documentation/healthkit/protecting-user-privacy) when evaluating iOS.

## Existing implementation and asset status

The app is Unity 6000.3.24f1 with uGUI 2.0.0. Reuse auth boundaries, localization, onboarding state, data validation and training rules. The existing Firebase adapter implements identity sign-in; account creation/recovery and trusted profile persistence are incomplete. Existing Home uses fictional data. A training review UI exists in separate local work; it selects fictional fixtures and stores an accepted fixture key locally. That flow is not included in this component PR and must not be assumed to exist on main or to provide a live personal training service. Home has now been migrated to the approved landscape PixelLab training hall with live uGUI controls. Other legacy screens still use their earlier presentation. Workouts/Rutinas now follows its approved render as a complete landscape journal with local example persistence, eligible edits and readiness review. It does not record real workout completion or connect a cloud training account. See `design/fantasy-workouts-r1/README.md`.

Component 07 imports the user's eight AutoSprite sources at `assets/sprites/autosprite/barbarian-user-r1/`, including their preserved source manifest with exact bytes, IDs and hashes. They are 1366×1366 opaque RGB exports; retain these originals. The component-07 review fixture adds separately recorded AutoSprite transparent derivatives and a shared scale/feet anchor. It preserves every RGB pixel and does not regenerate characters. Pixel-grid production decisions, final display approval and integration with a saved profile/Home remain separate work.

Repository Markdown is the source of truth and can be opened in Obsidian. No separate vault or external documentation workspace is needed. See the [window delivery plan](../design/fantasy-pixel-component-plan.md): PixelLab background first, complete screen render, then implement and verify one full window per PR (latest user direction, 2026-09-30).

## Combined origin and character setup (2026-10-01)

The approved age/country and character screens are implemented together in the
guild login shell. Create account and a trusted WIN-006 sign-in destination open
the same private, empty draft. Players explicitly choose country, character gender
and one of the eight existing PixelLab appearances. Body choice stays cosmetic;
colors, equipment and animation remain fixed. Draft navigation and localization
preserve choices without storing age/country or replacing Home's saved appearance.

Final Continue stops at the unavailable privacy/consent setup checkpoint. This
window does not satisfy regional/guardian policy, accept documents, issue an
account-creation permit or save a personal profile. The prior credential form
remains available for isolated review. See
[implementation and verification](../../design/fantasy-onboarding-r1/README.md).

## Decisions still open

1. Visual approval of the seven proposals and the overall production pixel grid. The first primary-button fixture targets landscape viewports from 854×480, uses Pixelify Sans for its labels, and has its own review and validation; this does not settle every screen’s typography or minimum layout.
2. Subscription expiry/permanent-unlock rules, purchase variant coverage, final currency uses and ad placement; the target prices and paid-only extra-character rule are already settled.
3. Which two classes join Barbarian as free classes, and their later schedule.
4. Initial Home decoration inventory/placement scope and cloud-versus-local persistence for the first shippable version.
5. Running-provider compatibility, leaderboard permissions, verification policy and eventual balanced activity-to-stat formulas.
6. Backend completion and reviewed exercise content before public release; exercise-demo animation coverage is a later, separately sized project.
