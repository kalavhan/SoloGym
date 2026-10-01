# Historical Home pipeline instructions

Snapshot before the approved training-hall integration. Superseded by current AGENTS.md. Earlier experimental files remain preserved locally; this is history, not an active generation instruction.

# SoloGym: active direction

The user's 2026-09-29 fantasy pivot supersedes the manhua/boxing UI, modular
clothing pipeline and per-window delivery workflow. Current requirements are in
`docs/product/fantasy-mvp-direction.md`; component work follows
`docs/design/fantasy-pixel-component-plan.md`.

## Scope

- Landscape-only pixel-art fantasy fitness app in the existing Unity project.
  Render targets first. Batch eight independent room sprites per PR/MR
  (user direction, 2026-09-30), reusing the room-object component. Other reusable
  UI components retain component-sized PRs with art, interaction states,
  integration fixtures and focused verification.
- Perspective correction exception (2026-09-30): first review four concept props
  (ring, bench, bed, shelf) and the corrected room. Once that proof is accepted,
  complete the remaining perspective corrections together in one PR; do not
  open a partial art PR or generate new characters as part of this correction.
  The user REJECTED this first perspective proof: the bed still does not fit,
  the room lost the approved render's composition/depth/atmosphere, and the
  character became too small. Stop the remaining generation batch. Reassess
  production feasibility against the approved Home render before implementation;
  no replacement camera or workflow has been approved.
  The user subsequently authorized a PixelLab room feasibility trial and asked
  that its MCP documentation be read first. PixelLab is permitted for this
  scoped experiment despite the earlier AutoSprite-only production rule.
  Preserve the approved Home composition and exact existing character; do not
  reinterpret this as permission to adopt an isometric map camera or regenerate
  characters. Connection availability and kit/reference limitations must be
  resolved before spending generations. See the rejected proof README's
  PixelLab follow-up and `pixellab-trial.json` for the prepared assessment.
  PixelLab MCP is reachable using the configured endpoint and credentials via
  Streamable HTTP; the configured npm launcher is locally blocked. The active
  bounded test is `design/fantasy-home-pixellab-r1/trial.json`: one referenced
  empty room, then one bed authored with room context. PixelLab's own guidance
  confirms its building kit cannot match this perspective. Do not substitute a
  tile-map camera or treat changes-only output as proof of a clean alpha cutout.
  The user subsequently liked the PixelLab room/bed proof ("i love it. this
  looks nice.") and explicitly requested ONE obese male Barbarian generated
  with PixelLab using the existing AutoSprite male-fat as the reference, for
  comparison. That scoped character exception is recorded in
  `design/pixellab-barbarian-male-fat-r1/trial.json`. It does not authorize other
  bodies, rotations, animations or replacement of the approved source roster.
  The user then preferred PixelLab and explicitly expanded this to ALL EIGHT
  Barbarians, using `/home/josue/Downloads/barbarian-*.png` original references
  because AutoSprite changed the skin color. Those eight exact files match
  `design/character-handoff/barbarian-visible-legs-r1/` by hash. The current
  batch is `design/pixellab-barbarian-eight-r1/batch.json`. Original reference
  complexion is authoritative; retain distinct body builds and complete
  silhouettes with margins. This is a static front-view review batch, not
  permission for animation, rotations, recoloring or runtime roster replacement.
  Latest hard requirement: each character's TOTAL PNG canvas, including all
  transparent margins, is at most 256x256. Pending 384px requests were
  superseded; record cancellations and preserve any completed originals as
  evidence only. The corrected batch is
  `design/pixellab-barbarian-eight-256-r1/batch.json`. Validate complete hair,
  hands and boots within that size; do not deliver larger padded sprites.
  The user approved keeping all eight 256px PixelLab frontal sprites on
  2026-09-30. Approval and exact hashes are recorded in
  `design/pixellab-barbarian-eight-256-r1/approval.json` and the asset manifest.
  These are the accepted static character art set; runtime integration remains
  to be verified. The user is creating eight-direction variants in PixelLab;
  do not duplicate those generations. Retrieve and validate their exports when
  integrating, retaining the 256x256 maximum per individual character image.
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

- Latest Home correction (2026-09-30): the user rejected a boxing ring in the
  personal room and asked for a setting for general fitness, gym exercise and
  calisthenics. Retain the straight-on camera. The new concept is
  `design/fantasy-home-training-hall-r1/home-training-hall-concept.png`; it is
  approved by the user ("love this, do it."). Implement it with PixelLab room
  and prop production, approved static Barbarians and live Unity UI, in one
  Home integration PR. This supersedes the old boxing/bedroom Home target. The
  proposed training hall has open floor, a pull-up station, weights, a bench,
  exercise mats and storage. Keep existing artwork as evidence; do not delete
  old runtime assets. Preserve the source render and compare assembled captures.

- Home visual target approved on 2026-09-30: `design/fantasy-home-r2/home-approved.png`.
  Follow `design/fantasy-home-r2/IMPLEMENTATION.md`; match the assembled Home
  composition before more unrelated component galleries. Keep room shell, props,
  character, sprite skins, icons and live UI/text separate. Never ship the
  flattened reference as the screen/background.
- Room prop positions and sizes are provisional. After all room items are added,
  provide an alignment editor so the user can do a manual placement pass.
  Until then, use rough placement and focus on independent assets/integration;
  do not spend iterations polishing alignment or treat current anchors as approved.
  The local editor is now documented in `docs/design/pixel-home-alignment-editor.md`.
  Preserve its exported layout files and apply the user's reviewed JSON; do not
  replace a manual placement pass with newly guessed coordinates.
- The user flagged inconsistent prop perspectives on 2026-09-30. Merged art is
  not camera/perspective approval. Use the actual room shell as the viewpoint
  reference and the approved Home concept for design/materials, accounting for
  each object's height. A placement editor cannot repair perspective baked into
  a sprite; keep the existing furniture correction pass separate and pending.
  The rejected correction proof is `design/fantasy-home-perspective-r1/`. Its
  arbitrary orthographic camera is NOT the new visual target. Preserve its
  geometry guides only as experiment evidence; do not apply them to more assets.
  Back-wall masonry also needs fixed-size modules; vague camera prompts alone
  did not prevent changing block sizes. These are unapproved concepts, not
  runtime replacements. Preserve the user's existing alignment-editor draft.

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
