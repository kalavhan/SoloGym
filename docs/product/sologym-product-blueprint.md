# SoloGym product vision status and roadmap

Updated 1 October 2026, after the merge of [PR 37](https://github.com/kalavhan/SoloGym/pull/37), with the connected MVP test build on branch `mvp/fitness-core` ([test guide](mvp-test-build.md)).
Repository: [kalavhan/SoloGym](https://github.com/kalavhan/SoloGym).

SoloGym is a landscape fantasy fitness app in which real training advances the player's character and life in the game. The intended result is a **AAA-quality pixel-art experience with Hades-inspired presentation and 2.5D depth**, built around our existing characters. The personal gym, workout dungeon, character progression and eventual playable combat should feel like parts of the same world.

We have a substantial native Unity interface and, with the 0.6.0 test build, a connected fitness loop for a real account: registration/sign-in, setup, an on-device plan, Home, journal, workout dungeon and history. We do **not** yet have cloud sync, a trusted eligibility/consent service, reviewed exercise content or the eventual action game. The immediate work is phone testing of this loop, then the release services. More characters and the larger game follow as the core becomes usable.

This document consolidates the current direction, the implementation through PR 37, missing work and the owner's latest decisions. **Confirmed** means requested or approved by the owner. **Proposed** means a recommendation awaiting a decision. A working review screen does not mean its backend or release requirements are complete.

## Product identity and quality target

### A game world built around fitness

The character should belong to the player across the entire experience: choosing an appearance, living in a personal gym, preparing a routine, defeating a workout boss and eventually playing a dungeon or competitive match. Fitness progress should have understandable consequences in that world.

The intended connection is:

**Choose a character → prepare training → complete real activity → record progress → develop the character → use that progress in the game → return to the personal gym.**

Routine consistency will contribute to strength. Running consistency will contribute to speed. Later, activities can support thematic characters, such as swimming and an Atlantis-inspired warrior. The exact strength and thematic formulas are still open. Appearance, body size and purchases do not prescribe a person's physical training.

Fasting is a deliberate exception to combat progression: it remains an optional adult tracker, without XP, strength, speed, rewards or competitive rankings.

### What AAA quality means here

AAA is the desired standard of presentation and finish, not a claim about the current budget, team size or release readiness. For SoloGym it means:

- One coherent art direction, with consistent perspective, scale, materials, palette and composition.
- A convincing sense of depth, character presence and atmosphere in every scene.
- Responsive controls, clear feedback, thoughtful sound and restrained effects as those systems are added.
- Readable exercise instructions and comfortable mobile interaction, including smaller landscape screens.
- Polished transitions and complete loading, empty, offline, error, return and recovery states.
- Stable performance and verified behavior on actual target phones, beyond Linux screenshots and automated checks.

Hades is a reference for the desired game feel, atmosphere and production care. SoloGym keeps its own fantasy setting, character designs and pixel-art language. This reference does not automatically settle the combat mechanics or authorize replacing the existing work.

### How the 2.5D direction fits the current art

**Confirmed target:** a fantasy game with 2.5D depth using the existing characters. This can combine 2D character sprites with carefully composed layers, depth ordering, occlusion, shadows and supporting scene geometry where useful. A final camera, projection and rendering technique have not been approved for the future gameplay areas.

The current approved Home is a **straight-on training hall**, not an isometric room. Keep that usable foundation. A future angled gameplay camera needs its own complete render and a small assembled scene proof before changing production assets. Characters, floors, walls and props must agree on the viewpoint; moving or scaling a sprite cannot repair perspective already painted into it.

The earlier attempts exposed the main risk: individually attractive generated assets can still make a poor scene when their camera angles and proportions disagree. Composition must be designed and reviewed as a whole, while the runtime assets remain separate.

## Characters and customization

For now, use the eight existing approved PixelLab Barbarian appearances:

| Presentation | Body appearances |
| --- | --- |
| Male Barbarian | Skinny, medium, fat and muscular |
| Female Barbarian | Skinny, medium, fat and muscular |

These are eight appearances of the starting class, not eight separate classes. Body selection is cosmetic and must retain recognizable differences in proportions and facial fullness. It does not grant strength, determine readiness or select a training difficulty.

The first version keeps the existing skin, hair, eye colors and outfits. It does not need a modular clothing fitter or a separate equipment layer for every body. The active frontal source canvases are at most **256×256 pixels in total**, including margins. Preserve their original bytes and provenance. Directional exports have also been preserved, but that does not mean complete gameplay or exercise animations have been implemented or visually approved.

**Later:** add more characters and classes as the product grows. The earlier goal of three free classes remains; start with Barbarian and add two more later. Knight, Aztec Warrior and other fantasy identities are roster ideas, not prerequisites for the first release. Palette customization and character-specific exercise demonstrations remain future work.

New movement, attacks and exercise demonstrations need an explicit animation scope and review. A static character next to an exercise instruction is not an animated demonstration of the movement.

## Running and character speed

**New confirmed direction:** running consistently improves the character's speed, with **100 consecutive running days reaching the maximum improvement** from this system.

The requirement describes a capped progression milestone. It does not mean a 100% speed increase, unlimited acceleration or faster gains from importing the same activity repeatedly. The cap amount, daily increments and progression curve have not been chosen. This system is not implemented yet.

A player's future progression view should explain the qualifying run, current consecutive-day count, progress toward day 100 and resulting game bonus. For example, reaching day 100 would mean the running-derived speed bonus has reached its designed cap; further activity would not push it beyond that cap. What is retained or lost after a later interruption is still undecided.

Before implementation, resolve these rules explicitly:

| Decision | What needs to be defined |
| --- | --- |
| A qualifying running day | Accepted activity sources, minimum qualifying activity, manual records and whether indoor running is included |
| A day and a sequence | Time zone, midnight boundaries, delayed imports, travel, duplicate records and corrected or deleted activities |
| Progression and cap | Maximum bonus, the curve over days 1–100 and whether the bonus is per account or per character |
| Breaks and recovery | What happens after a missed day, planned rest, illness or injury; whether progress pauses, decays or resets |
| Validation | Which records are trusted, how disputed records are handled and when a bonus is recalculated |
| Gameplay balance | Which game modes use the bonus and how it affects new players and competitive fairness |

The 100-day target is a game-design milestone, not a prescribed daily exercise program. We must reconcile the consecutive-day idea with the existing recovery requirements before shipping it. Do not silently replace it with “100 total runs,” and do not silently decide that one missed day removes all progress. A proposed recovery protection or alternative activity rule needs approval.

Strava, Health Connect, HealthKit and Xiaomi/Mi Fitness remain integration candidates. We have not selected or completed a provider integration. Device compatibility, permissions and permitted uses of imported data must be verified for the actual implementation.

## What is already built

This is the implementation status through the merged Goals and Experience window plus the connected MVP test build (`mvp/fitness-core`, 0.6.0). Review captures and smokes still use the fictional fixtures; a signed-in account uses its own data.

| Area | Working foundation | Remaining boundary |
| --- | --- | --- |
| Shared visual controls | Native Unity pixel buttons, panels, fields, choices, focus states and navigation; live ES/EN text | Continue refining them in complete windows and testing on phones |
| Login | Landscape guild entrance, email/Google sign-in, Firebase password-reset email, session restore on launch, sign out | Phone verification of the 0.6.0 build |
| Account creation | Firebase email registration after on-device age (15+), country and document choices; in-app account deletion | Trusted server eligibility/consent check before public release |
| Age and country | Private draft, age validation, country selection; saved with the account | Production eligibility policy and any required guardian handling |
| Character selection | Gender/body selection from the eight existing Barbarian appearances, saved per account and changeable in Home | — |
| Consent and documents | Independent choices and document reader; chosen revisions and time saved with the account | Documents still use authorized Lorem ipsum placeholders; final copy and server receipts are missing |
| Private fitness profile | Optional measurements, metric/imperial conversion, readiness and pause routes; saved privately on the device | Cloud storage and deletion across devices |
| Goals and experience | Goal selection, experience, beginner confirmation, teen filtering; continues to equipment | — |
| Equipment, schedule, plan review | Home/gym/outdoor, bodyweight-only or catalog equipment; 2–5 weekdays; 15/25/40/60 min; generated week reviewed and accepted; editable from Home | No separate full-screen renders; these steps reuse the approved panel controls |
| Personal Home | Approved training hall with the account's character, name, today's routine/rest day, streak and bosses defeated; per-account decorations | Account inventory and cloud synchronization |
| Workout journal | The person's scheduled week and history, edits (type, length, swaps), readiness before each session, no catch-up debt | Cloud synchronization |
| Workout boss | Manual set logging, rest, pause/resume, corrections, difficulty changes; completed/stopped records go to the account's history | Production rewards ledger |
| Fasting | Optional adult tracker, stored per account, hidden below 18 | Sync and release review; it is not a cloud health-record service |
| Training foundation | Bilingual catalog and rules; on-device C# engine identical to the Python reference on 32,400 sessions | Professional and youth content review before release |

The Goals and Experience delivery passed **1,403 reported checks** across its native layouts, keyboard flow and related regressions. This is evidence for that delivery, not proof that the entire mobile product is finished or that exercise content has received professional approval.

## What is missing for the MVP

### Finish the connected training setup

**Implemented in the 0.6.0 test build:** available equipment, schedule/session length and plan review/acceptance connect Goals to Home. Phone testing and visual review of these steps remain.

The connected journey is:

**Login or account creation → age/country → character → consent and identity as appropriate → private profile/readiness → goals/experience → equipment → schedule → plan review → personal Home → workout dungeon → history.**

Returning to edit setup must preserve useful drafts while invalidating any prescription whose inputs changed. Completing this journey should not require developer flags or a fictional sample profile.

### Connect accounts and durable personal data

Registration, sign-in/session restoration, password recovery and account deletion work through Firebase in the 0.6.0 test build; profile, plan and history are stored per account on the device. Still required: replace placeholder documents, a trusted eligibility/consent service with receipts, cloud private-profile storage and an email-verification policy. Passwords never enter saved drafts.

Bind the chosen appearance, training profile, plan, session records and appropriate preferences to the correct account. Define local/offline behavior, account switching, synchronization conflicts, deletion and recovery. Retried submissions or corrected logs must not create duplicate progress or rewards.

The existing Firebase identity adapter and local controllers are foundations to extend. A finished form alone does not supply the missing services.

### Make training work for a real user

Connect profile inputs to the structured training rules, including goal, experience, available equipment, time and readiness. Preserve substitutions and edits without losing the meaning of already recorded work.

Easy, Medium and Hard remain adjustable during the boss routine, including exercise, rest and pause, within eligibility rules. Changing difficulty must not erase completed sets or create free boss damage. Extra repetitions, extra load and repeated submissions must not farm unlimited rewards. Missed sessions create no catch-up exercise debt.

Keep a clear distinction between the **workout battle**, where completing prescribed real activity advances an encounter, and any later **real-time action game**. The workout experience should not demand reaction controls while the user is exercising. The current capped boss model can leave health in the bar after a shortened or stopped routine; never require extra exercise just to force a victory graphic.

Review exercise content, youth programming and readiness/recovery behavior before public release. The current product boundary remains age 15+, with separate teen rules; optional fasting stays unavailable below 18 and for unknown age.

### Prepare the mobile release

Run the complete journey on target phones in landscape. Verify touch targets, safe areas, keyboard behavior, readable text, backgrounding, resume, storage failures, interrupted networking and performance. Establish supported devices and frame-rate/memory targets rather than inventing a specification from desktop captures.

Finish the appropriate account/settings and data-deletion controls, final content, onboarding explanations, store configuration and release checks. Ads and subscription/purchase infrastructure remain separate missing work; decide their release timing before treating monetization as complete.

## Work after the core fitness release

| Stage | Intended outcome | Completion evidence |
| --- | --- | --- |
| Running progression | One supported activity source and a visible, capped speed-progression system | Approved 100-day rules; imports, duplicates, edits and interruptions behave predictably |
| First 2.5D gameplay proof | A small original gameplay area using an existing Barbarian | Consistent camera/scene art, readable movement and collision, reviewed animation and acceptable phone performance |
| Fitness linked to gameplay | Earned speed and strength have an understandable effect in the playable game | Tested caps and clear separation between training records and game balance |
| Roster and world expansion | More classes, environments, decoration and selected-character exercise examples | Approved production assets and bounded animation/content batches |
| Competitive and social systems | Rankings, the earlier proposed 3v3 game, moderated chat and a shared social gym | Networking, moderation, permissions, anti-cheat and balance validated for those modes |

This order is a proposed delivery sequence. These stages are not all prerequisites for the current five-area MVP: accounts, personal Home, workouts, workout bosses and optional adult fasting.

The previous longer-term competitive direction was a **3v3 MOBA**. The new Hades-inspired direction establishes a quality and 2.5D game reference, but does not explicitly cancel the 3v3 idea or approve a complete roguelike campaign. Decide the relationship between solo action gameplay and competitive matches before building a large combat or networking system.

Separate rankings are still intended for game performance and fitness activity, such as weekly distance or routine consistency. They need their own permissions, privacy, correction and verification rules. Fasting does not belong in them.

## Art and implementation process

Continue the working process: **one complete window after an approved render, then implementation and native verification in one PR**. Reuse accepted controls and artwork. For a new background, use PixelLab first; create the full render from realistic assets, rather than approving a composition the available sprites cannot reproduce.

- Use the existing Unity project and native uGUI controls with pixel sprite skins. Labels, values, states and layout remain live.
- Keep background/architecture, props, characters, effects and UI separate. Use stable object IDs, pivots, footprints and scale conventions for decoration and seasonal variants.
- Establish one camera and light direction for each scene. Review a small composed proof before producing an entire asset batch.
- Preserve character identity and the approved body variants. Do not generate replacement characters merely to fit a screen.
- Keep source images, prompts, job metadata, costs, approval records and hashes under the repository's asset/design structure. Read actual atlas metadata before implementing animation.
- Compare native output with the approved reference, including compact and wide landscape layouts, long Spanish/English text and interaction states.
- Keep effects restrained and support reduced motion where relevant. More glow does not substitute for good composition.

The former portrait/manhua, boxing-room and modular-clothing experiments remain historical evidence. Preserve useful logic, sources and approvals; remove obsolete runtime dependencies only after replacements and dependency checks. Do not restart the whole project to pursue a new art technique.

## Commercial direction

| Product | Confirmed target |
| --- | --- |
| Free access | Ads; Barbarian first, eventually three free classes |
| Subscription | MX$45 per month; removes ads and grants all game content while active |
| Additional characters without subscription | MX$20 per character, paid purchase only |
| Fitness points | Earned game currency remains intended; exact uses and economy are not finalized |

These are target prices, not completed store products. Additional characters are not purchased with fitness points under the current decision. Subscription expiry, permanent ownership, restoration and whether one purchase covers all body/presentation variants remain open. The game also needs a clear balance policy for paid classes; balanced alternatives rather than purchased competitive advantage remain the recommended approach.

## Decisions to make before their implementation

1. Equipment and schedule renders, then the final connected plan-review behavior.
2. The complete 100-day running rules, especially qualification, bonus size and recovery/interruption treatment.
3. The camera and technical approach for a composed 2.5D gameplay proof using the existing character art.
4. Whether the later action game is solo dungeon play, a step toward 3v3, or both.
5. The first supported running-data provider and its permitted data uses.
6. Strength progression, competitive caps and correction/recalculation rules.
7. Cloud ownership and synchronization for profiles, plans, history, fasting and home inventory.
8. Final legal/content review, purchase entitlements, ad placement and supported mobile devices.

## Implementation references

The status above was checked against the merged work and repository records. The links below identify the baseline at PR 37's merge, so older local checkouts do not silently present stale status. Historical notes inside older documents may describe earlier delivery stages; the newer owner decisions recorded here take precedence for future planning.

- [Merged goals and experience delivery](https://github.com/kalavhan/SoloGym/pull/37), including the next-equipment checkpoint and verification evidence.
- [Product requirements and retained fitness boundaries](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/docs/product/fantasy-mvp-direction.md).
- [Native UI, Home composition and delivery process](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/docs/design/fantasy-pixel-component-plan.md).
- [Training generation foundation](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/docs/training/generation-spec.md) and [original project research](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/docs/research/2026-09-25-fitness-game-research.md).
- [Workout journal implementation](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/design/fantasy-workouts-r1/README.md) and [workout boss implementation](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/design/fantasy-boss-r1/README.md).
- [Fasting implementation and limitations](https://github.com/kalavhan/SoloGym/blob/1a6600741e567c6daa0d70a19b63d152acd1fa31/design/fantasy-fasting-r1/README.md).

Keep this document in the repository and open it directly in Obsidian or the editor. Update its date and implementation status after later milestones; preserve the distinction between approved direction, recommendations and completed work.
