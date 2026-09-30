# SoloGym: active direction

The user's 2026-09-29 fantasy pivot supersedes the manhua/boxing UI, modular
clothing pipeline and per-window delivery workflow. Current requirements are in
`docs/product/fantasy-mvp-direction.md`; component work follows
`docs/design/fantasy-pixel-component-plan.md`.

## Scope

- Landscape-only pixel-art fantasy fitness app in the existing Unity project.
  Render targets first; implement one reusable component per PR/MR, with its
  art, interaction states, integration fixture and focused verification.
- MVP: login/account creation, personal home gym, editable workouts/history,
  routine-driven dungeon bosses and optional adult fasting. Retain necessary
  onboarding, readiness and character-selection steps.
- Training difficulty remains changeable during the boss-battle routine, including
  exercise, rest and pause. Preserve recorded work and existing eligibility/reward
  limits; do not lock difficulty to onboarding or the pre-battle briefing.
- Start with eight user-created Barbarians: male/female and skinny, medium, fat,
  muscular. Fixed skin/hair/eye colors for this version. Body choice is cosmetic.
- Three free classes are the eventual goal; start with Barbarian and add two
  later. Non-subscriber additional characters cost MX$20, not fitness points.
  Target subscription MX$45/month removes ads and grants all game content while
  active; post-expiry ownership remains undecided. Do not guess entitlements.
- Later: 3v3 MOBA, running imports/speed progression, rankings, intergalactic
  social gym/chat, activity-themed bonuses, more classes, color palettes and
  per-character exercise demos. Do not turn these into current MVP prerequisites.
- Character animation/new generation batches require explicit scope. No new
  character generation is authorized merely by implementing a screen.

## Art and UI pipeline

- Home visual target approved on 2026-09-30: `design/fantasy-home-r2/home-approved.png`.
  Follow `design/fantasy-home-r2/IMPLEMENTATION.md`; match the assembled Home
  composition before more unrelated component galleries. Keep room shell, props,
  character, sprite skins, icons and live UI/text separate. Never ship the
  flattened reference as the screen/background.

- Preserve exact user-created AutoSprite exports and manifest in
  `assets/sprites/autosprite/barbarian-user-r1/`. Transparent derivatives live in
  `assets/sprites/autosprite/barbarian-viewport-r1/`; the viewport review fixture
  preserves proportions and a shared feet anchor. Source preservation and
  component verification are not final runtime-art or screen approval.
- Built-in imagegen is authorized for requested screen concepts/references.
  Final character/sprite production uses AutoSprite MCP and its own review.
  Do not regenerate the user's characters to fit a screen.
- The separately prepared local `design/fantasy-mvp-r1/` render pack contains
  review proposals, not decomposed production assets or approved implementations.
  It is not included in the primary-button PR; preserve its prompts and hashes
  when importing it in a later change.
- Use native uGUI controls with pixel sprite skins. Borders, textures and icons
  are art; labels, values, states and layout are live components. Use nine-slice
  or tiled images where suitable. Do not bake screens/forms/text into buttons.
- Separate room shell, ring, bag, bench, rack, furniture and decoration objects.
  Seasonal skins/overlays keep object IDs, footprints, pivots and placement.
  A flattened reference is not proof these independent assets exist.
- One object per asset export, never a contact sheet as generation input.
  AutoSprite handoffs need a <=200-character description, copy control/count
  and separate detailed brief.
- No programmer-art characters/equipment or embedded SVG substitutes. Save
  sprite exports under `assets/sprites/`. Read JSON atlases before later
  animation implementation; never infer frame metadata from a reference image.

## Preserve useful work

- Keep auth, privacy/onboarding states, training data, generation logic, research
  and tests. A training UI in separate local work uses fictional review fixtures;
  it is not included in this component PR and does not establish live personal
  plan generation or production reward issuance on main.
- Preserve researched readiness/recovery/youth requirements. Appearance does not
  prescribe training. Missed workouts create no punitive catch-up debt; extra
  reps or load must not farm unbounded damage/rewards.
- Fasting is optional/off by default, unavailable below 18, without XP, buffs,
  competitive ranking or fasting-streak rewards.
- Keep old art, user assets, approvals, costs, source hashes and fit snapshots as
  evidence. Do not delete runtime-referenced assets before replacements and a
  dependency check. Preserve unrelated local work.
- Former instructions are archived in
  `docs/archive/agents-modular-boxing-2026-09-27.md` and describe old batches.
- Repo Markdown can be opened in Obsidian. Do not move records into another
  vault or external workspace without an explicit request.
