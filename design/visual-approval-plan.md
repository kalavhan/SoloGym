**SoloGym — high-level concept and visual approval plan**

Status: the user approved the overall visual style on 2026-09-25. The current next phase is training data, followed by the staged window milestones in docs/product/window-glossary-and-milestones.md. This initial concept plan is historical context; the new milestone and asset plans govern production approvals.

SoloGym is an Android/iOS fitness RPG for ages 15+, targeting the US, Canada, and Latin America. The interface supports English and Spanish, initially follows the device language, and preserves a manual language choice. Users create an account or sign in with Google; the proposed iOS login also includes Sign in with Apple. A private age/region and consent flow accommodates regional requirements.

The core loop is: receive an automatically assembled workout or choose a reviewed preset; edit it to fit equipment, time, and appropriate difficulty; perform the exercises and log sets, repetitions, and load; defeat the workout boss; collect rewards; use the same character in the separately played action tower and social gym. **Automatic means plan generation. This version does not count repetitions using cameras or wearables.**

Character appearance supports male/female options, multiple builds, separate body-size and muscle-definition adjustments, and gear. Physical measurements remain private. Appearance does not determine strength, health, or battle power. The Boxer is the initial playable class, with free movement, three ordinary attacks, and a special.

The tower contains connected rooms with enemies, player movement, ability use, rewards, and procedurally varied future floors. The interdimensional gym supports walking around, emotes, localized preset messages, friend requests, and limited private chat. These are separate play spaces from workout logging.

Progression recognizes an appropriate routine and personal progress. Recovery days protect consistency. Difficulty defaults to Medium, with Light and Hard adaptations and a legitimate rest/stop option. Paid gear and themes are cosmetic; fitness XP and combat advantages cannot be purchased. Fitness rankings and tower progress remain distinguishable. Friends/state/country/world scopes remain part of the concept, with restricted private teen defaults.

Fasting is unavailable below 18. Adults can optionally use a neutral tracker with supported plans up to the requested 20:4 limit, appropriate information, and no fasting XP, buffs, or ranking. The limit is a product scope boundary, not a medical safety claim.

The following is the initial **render coverage inventory**, not an implementation backlog. Several entries need more than one visual state. The inventory can be refined while designing; no page should enter development without an accepted render.

| Area | Screens or windows to render |
| --- | --- |
| Entry | Welcome/login; account creation; password recovery; language selection |
| Private setup | Age/region and consent; basic fitness profile; goals, experience, equipment, and schedule; conditional guardian flow where applicable |
| Character | Male/female appearance selection; build and muscle controls; character status |
| Main hub | System/home; today's quest; recovery-day state |
| Training setup | Generated plan; preset selection; routine editor; exercise details; Light/Medium/Hard choice |
| Workout boss | Session overview; active exercise with manual logging; rest timer; pause/stop; completed boss and loot |
| Progress | Training history and personal progress; consistency and recovery calendar |
| Gear | Inventory; item details; equip/appearance preview; cosmetic shop and purchase confirmation |
| Tower | Entrance; floor/room route; isometric combat HUD; room clear/loot; defeat/retry |
| Social gym | Shared gym scene; player interaction window; friend requests/list; preset public/private chat; mute/block/report |
| Rankings | XP and consistency boards with permitted friends/region/country/world scopes |
| Adult wellness | Optional fasting setup, timer, information, early end, and history; absent from teen navigation |
| Settings | Language; units; themes; accessibility; privacy; linked login methods; account deletion; support |
| Common states | Loading, connection loss, empty state, confirmation, and error windows |

The visual target is a modern manhua System: deep indigo environments, luminous cyan/violet frames, sharp engraved panel edges, illustrated characters, original gear iconography, and a readable game HUD. Menus and workouts are proposed in portrait; tower combat is proposed in landscape. These orientations are design proposals, not previously confirmed requirements.

The first style gate uses three representative screens: System/status in English, a workout boss in Spanish, and landscape tower combat. They demonstrate the window materials, character art, navigation, typography, localization, and combat controls. First-round images are visual proposals with fictional demo values; they are not working software or accepted specifications.

The user has accepted the visual direction. Render each remaining window through the new serial asset approval process and its important states in that style. Demonstrate equivalent English/Spanish screens during that pass to verify text fit. Refine layout and exact copy before calling any individual render final. Then develop one accepted page at a time and compare the actual result against its approved image, including mobile sizes and both languages. No backend or engine work begins during this approval stage.

Regional release readiness still needs country-specific consent/privacy assessment, including potential French requirements for Quebec and a Portuguese decision for Brazil. These findings are documented in the research brief; the present visual language remains English/Spanish as requested.
