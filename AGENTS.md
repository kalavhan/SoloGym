# SoloGym: active direction

The user's 2026-09-29 fantasy pivot supersedes the manhua/boxing UI, modular
clothing pipeline. Current requirements are in
`docs/product/fantasy-mvp-direction.md`; component work follows
`docs/design/fantasy-pixel-component-plan.md`.

## Scope

- Goals & Experience (WIN-010) follows merged PR #36. The user approved all three
  landscape renders in `design/fantasy-goals-r1/` with “good, do it.” The native
  goal, experience and summary/edit states now connect from the profile checkpoint
  in one complete-window PR. Reuse the original PixelLab guild entrance and controls.
  Keep catalog IDs, empty initial choices, explicit beginner confirmation, teen
  filtering and upstream age/consent/readiness gates. Unknown age cannot enter
  this review. Experience is separate from battle difficulty; appearance never
  determines it. Back/readers/locale retain choices; exit or eligibility/consent
  changes discard them. Available equipment (WIN-011), then schedule, is next;
  the current equipment destination is an honest pending checkpoint. This flow
  saves no profile, creates no workout and grants no production authorization.

- Next window after merged PR #35: private fitness profile (WIN-009), with notice,
  optional measurements and readiness in one complete window. Landscape concepts
  and exact prompts are in `design/fantasy-profile-r1/`; the user approved with
  “do it”. The native window is implemented with a memory-only review draft.
  Signed-in review enters from consent; provisional email registration provides
  an explicit profile-preview link that clears secrets and creates no account.
  Preserve reader returns, draft resets and the next-setup checkpoint.
  Reuse the existing PixelLab guild entrance and native controls. Preserve optional
  measurements, unit conversion, appearance independence, pause routes and current
  policy gates. A provisional registration preview cannot create a live profile.
  No new character/background generation or animation batch is part of this work.

- Merged consent window (2026-10-01): the user explicitly authorized Lorem ipsum
  for terms and privacy until the final text is supplied. Use clearly identified,
  replaceable review documents; do not make final copy a prerequisite for the
  render or review-flow implementation. Concepts and ES/EN placeholder fixtures
  are in `design/fantasy-consent-r1/`; the user approved them with “implement.”
  The native consent/reader now replaces the pending message. Both review choices
  enable a registration preview; its valid submission makes no service calls and
  clears secrets. Signed-in users reach the profile review described above. Keep
  missing-document states, draft/reader returns and age/country decision resets.
  Review checkbox
  decisions are not production legal receipts or registration permits. Reuse
  the existing PixelLab guild background and native controls; no new art batch.

- Latest batch instruction (2026-10-01): deliver age/country and character
  selection together in ONE onboarding PR. Prepare both complete renders first,
  then implement the connected steps as a unit after visual review. This batch
  supersedes the one-window-per-PR limit below. Selection means choosing a whole
  approved male/female Barbarian appearance from the eight existing PixelLab
  exports, with fixed colors/clothing; no cosmetic editor or new character art.
  Both renders were approved (“great, love it, build it”) and implemented in the
  guild login shell. Create account and trusted WIN-006 enter an empty private
  draft. Final Continue now opens the provisional consent screen described above;
  no enrollment permit, profile save or Home entry is established. The account
  form retains its trusted preflight outside the explicit provisional preview.
  Choosing a character must not bypass consent, trusted eligibility or private
  fitness setup. References: `design/fantasy-onboarding-r1/`.

- Landscape-only pixel-art fantasy fitness app in the existing Unity project.
  Latest delivery workflow (2026-09-30): ONE COMPLETE WINDOW per PR/MR, after
  its full-screen render. Produce the background with PixelLab first, then
  render the complete window against it, implement the reviewed design with
  separate assets/live controls, and verify the working assembled window.
  This supersedes component-only PRs and four/eight-sprite delivery batches.
  Reuse existing components within the window; do not generate new artwork
  for controls that already match the accepted style.
- The boss-dungeon render in `design/fantasy-boss-r1/` was approved with
  "it is okay for now, let's go". Its complete native window follows merged
  PR #29: explicit reviewed entry, local set records, rest, difficulty changes,
  pause/resume and completed/stopped history. One static PixelLab stone guardian
  is in scope; existing Barbarians stay unchanged. No animation batch.
- Optional adult fasting is delivered in merged PR #31, with
  its approved render in `design/fantasy-fasting-r1/` ("i like it. do it."). The user explicitly
  requested a fantasy magic clock with restrained animation and a time-stage
  indication. PixelLab room/clock art and one clock animation proof are in scope;
  character animation remains out of scope. Show elapsed-time ranges without
  claiming to measure ketosis, autophagy, hormones or immune regeneration.
  The complete native window includes adult opt-in, local persistence, correction,
  history/deletion, Home navigation and reduced motion. Preserve the original
  static clock and restrict animated frames to the dim rune annulus.
- The login/account-entry render in `design/fantasy-login-r1/` was approved with
  "go". Its complete native landscape window follows PR #31: separate PixelLab
  guild entrance, existing form/button controls, email/Google service adapter,
  cancellation/retry, localization and keyboard handling. The default Welcome
  route now opens it. Identity success still requires connected onboarding;
  production registration/recovery and final legal documents remain explicitly
  unavailable. Never route successful identity to fictional Home or imply that
  a screen redesign establishes trusted profiles. No new character art or
  animations were generated. Deliver this complete window in one PR.
- The account-creation render in `design/fantasy-account-r1/` was approved with
  "good, do it". The login shell now contains its native email/password/confirmation
  form. Registration preflight must authorize age/region/consent before credentials
  reach any creation adapter; the current default explicitly reports missing setup
  and creates nothing. The form is available for review, not proof of connected
  registration or completed onboarding. Keep password drafts memory-only, preserve
  ambiguous completion after interrupted submission, and never open sample Home.
  All background/control assets are reused; no new art generation.
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
