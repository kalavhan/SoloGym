# SoloGym: active direction

The user's 2026-09-29 fantasy pivot supersedes the manhua/boxing UI, modular
clothing pipeline. Current requirements are in
`docs/product/fantasy-mvp-direction.md`; component work follows
`docs/design/fantasy-pixel-component-plan.md`.

## Scope

- Landscape-only pixel-art fantasy fitness app in the existing Unity project.
  Latest delivery workflow (2026-09-30): ONE COMPLETE WINDOW per PR/MR, after
  its full-screen render. Produce the background with PixelLab first, then
  render the complete window against it, implement the reviewed design with
  separate assets/live controls, and verify the working assembled window.
  This supersedes component-only PRs and four/eight-sprite delivery batches.
  Reuse existing components within the window; do not generate new artwork
  for controls that already match the accepted style.
- Next window: routine-driven boss dungeon, following merged PR #29. Prepare a
  PixelLab background and complete landscape render in `design/fantasy-boss-r1/`
  for visual review before implementing it. Existing characters stay unchanged;
  a boss in the concept is a proposed design, not a production sprite approval.
- Workouts/Rutinas is the approved complete journal window, with current/completed/missed sessions,
  routine review/editing, and a readiness entry before training. The proposed
  visual is a fantasy guild training journal. Its reference and implementation pack is
  `design/fantasy-workouts-r1/`; the render is approved ("this looks good, build it.").
  Local example edits/history and fresh readiness checks are implemented together. Sample data remains clearly labelled; visual
  approval is not proof of connected live workout data.
- The current approved Home is the straight-on fantasy training hall in
  `design/fantasy-home-training-hall-r1/`. The user approved it with "love this,
  do it." It supersedes the boxing/bedroom and rejected isometric/orthographic
  experiments. Deliver room, props and live Home UI together in this integration.
- PixelLab MCP is the first choice for new window backgrounds, and is authorized
  for scoped window backgrounds and the approved Home/Workouts artwork. The eight static
  PixelLab Barbarians are approved in `design/pixellab-barbarian-eight-256-r1/`.
  Keep every character's TOTAL source canvas at most 256x256, including margins.
  Use the original Downloads references, not AutoSprite recolorings. Do not
  regenerate characters to fit screens.
- Eight user-created directional sets are preserved with provider metadata in
  `assets/sprites/pixellab/barbarian-directions-r1/`. They passed canvas checks;
  Home still uses the approved frontal exports. Directional visual approval and
  future animation implementation are separate. Do not duplicate generations.
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

- Home uses `assets/sprites/pixellab/training-hall-r1/`: separate architecture,
  window exterior, 14 placeable props, approved static characters and native
  uGUI controls. Compare actual Unity captures to the approved concept.
  Preserve the old room/boxing assets as evidence and legacy review resources.
- Composition and perspective belong to the art. An alignment editor cannot
  correct foreshortening baked into a sprite. Keep the straight-on view and
  plausible prop scale; no boxing ring, punching bag or bedroom furnishings.
- Local decoration saves belong to the room ID. The new hall uses its own
  versioned layout and does not overwrite old alignment-editor exports. Preserve
  user placement drafts. The rug's authored fabric and trim move as one object.

- Preserve exact user-created AutoSprite exports and manifest in
  `assets/sprites/autosprite/barbarian-user-r1/`. Transparent derivatives live in
  `assets/sprites/autosprite/barbarian-viewport-r1/`; the viewport review fixture
  preserves proportions and a shared feet anchor. Source preservation and
  component verification are not final runtime-art or screen approval.
- Built-in imagegen is authorized for requested screen concepts/references.
  Current Home/Barbarian production uses PixelLab MCP as scoped above; earlier
  AutoSprite exports remain preserved. New generation batches need scope/review.
  Do not regenerate the user's characters to fit a screen.
- The separately prepared local `design/fantasy-mvp-r1/` render pack contains
  review proposals, not decomposed production assets or approved implementations.
  It is not included in the primary-button PR; preserve its prompts and hashes
  when importing it in a later change.
- Use native uGUI controls with pixel sprite skins. Borders, textures and icons
  are art; labels, values, states and layout are live components. Use nine-slice
  or tiled images where suitable. Do not bake screens/forms/text into buttons.
- Separate room shell, training apparatus, bench, rack, furniture and decoration objects.
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
  and tests. Current Home/training data is a fictional review fixture; this
  integration does not establish live personal plans or production rewards.
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
