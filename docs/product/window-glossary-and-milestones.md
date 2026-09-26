# SoloGym window glossary and milestone plan

**Current status · 2026-09-25:** WIN-001 Home and WIN-002 access are accepted by user report. WIN-006/007 native UI is implemented and Linux-verified; their production policy, legal-document and storage dependencies remain pending. The user approved [WIN-009 Private Fitness Profile](../windows/WIN-009-private-fitness-profile-g0.md) with “si perfecto”. Its complete native implementation is the current milestone, with no APK requested. [WIN-010 Goals / Experience](../windows/WIN-010-goals-experience-g0.md) will be prepared as the next proposal after verification.

User acceptance and retained targets do not certify every device, live services or mathematical pixel identity. The original reference hashes and prior test evidence remain unchanged.

Android and iOS; ages 15+; US, Canada, and Latin America as target regions, with the exact country release list and policies still to be reviewed. English and Spanish follow the device initially and can be switched manually. Automatic means **workout plan generation only**. Sets, repetitions, load, and timers are logged manually.

## M0 comes first: shape and review training data

M0 is a shared prerequisite, not a window. The current work prepares the exercise catalog, data shape, reviewed-content workflow, starter templates, and bounded generation rules before designing screens in depth. A research-informed draft is not an exercise-professional sign-off.

Acceptance for M0:

- Stable exercise identifiers, bilingual names and cues, reviewed age applicability, movement patterns, equipment, dosage fields, substitution links and media provenance.
- Reviewed starter templates and bounded generation/progression/recovery rules, with explicit rationale and provenance.
- Example plans for adults and teens, bodyweight and equipment, Light/Medium/Hard and stop/recovery cases.
- Clear separation of research evidence, product defaults, and exercise-professional review still required; a draft dataset is not clinical validation.
- User approval of the data shape before deeper window work; production background and sprite generation follows the window gates below.

## How a window milestone proceeds

The current workflow follows the user's latest target-render development instruction: prepare and approve one retained bilingual window proposal, map its corners/centers and shared assets, build the complete window, then perform focused full-screen visual and interaction checks. Do not create an individual render/test instance or approval loop for every button or shared UI asset. A new window still requires its own visual-target approval; WIN-009 is approved for construction and WIN-010 remains a subsequent proposal.

The original G0–G6 asset plan below is retained as production context. Its individual UI asset stop points are superseded by the current complete-window workflow. Separately scoped modular characters, customization, gear fitting and motion still require their own explicit deliverables before an asset library is expanded.

| Gate | Deliverable and stop point |
| --- | --- |
| G0 — Flow/data and rendered proposal approval | Present the window purpose, step-by-step user flow, required fields, exact EN/ES copy, teen/adult states, data examples, orientation and interaction zones together with English and Spanish renders of the principal state. Retain immutable versioned reference files, fixture/canvas metadata and checksums. Obtain approval of the brief and exact visual target before producing separate assets. |
| G1 — Background alone approval | Generate only the window background, without characters, UI, icons, or text. Stop and obtain approval. For an existing background, present the exact asset and obtain reuse approval. |
| G2 — Environment props/foreground approval | Generate only the approved next props/foreground batch with placement/occlusion notes. Stop and obtain approval; explicitly approve reuse or record N/A when there are no such assets. |
| G3 — Character/enemy/gear sprites approved | Complete G3a base → G3b customization → G3c wearable equipment → G3d motion/export as separate serial approvals. Generate only the smallest batch needed for each subgate, then stop. Approve exact reuse or record N/A explicitly; a combined sprite batch does not bypass the subgates. |
| G4 — UI frame/icon/control assets approved | Generate or design the required frame, icons and controls as separate assets. Keep functional EN/ES text editable in the eventual interface. Stop and obtain approval; explicitly approve shared-asset reuse or record N/A. |
| G5 — Composite bilingual screen approval | Assemble the approved production assets into complete English and Spanish renders and the required mobile/interaction states. Compare the actual asset composite against the approved G0 proposal at its recorded baseline using overlays and difference images. Resolve discrepancies; any changed visual target requires user approval and a new retained revision, never silent replacement. Obtain approval of the exact composite. |
| G6 — Implementation + visual verification | Only after G5 approval, implement this window using its accepted data and assets, verify the user flow, and compare actual output with the accepted composite at the same canvas, locale, fixture and state. Retain overlay/difference evidence, resolve discrepancies and obtain milestone acceptance before beginning the next window. |

G3 contains four separate stop points, matching the more detailed [asset-production workflow](../design/asset-production-and-animation-sources.md):

| Subgate | Approve before proceeding | Asset workflow stage |
| --- | --- | --- |
| G3a — Base | Base character/enemy proportions, views and neutral clothing; no equipment yet. | 3 |
| G3b — Customization | Skin treatment, hair, supported body/muscle combinations and headgear compatibility. | 4 |
| G3c — Wearable equipment | Item concepts, slots, attachments, fitting, masks and equipped samples. | 5 |
| G3d — Motion/export | Necessary motion, layering, fit, frame registration and export proof. | 6 |

The original mapping is G0 = asset stage 0; G1 = 1; G2 = 2; G3a–G3d = 3–6; G4 = 7; G5 = 8; G6 = 9. The [avatar contract](../../data/art/avatar-contract.json) retains this staged approach for separately scoped modular character and gear production. The current shared UI workflow uses the complete-window override above.

For reused assets, present the exact existing version in its new context and record **reuse approved**. Do not regenerate it automatically. When a layer is irrelevant, record **N/A** with the reason and approve that decision. Each approval record names the window, gate, asset IDs/versions, decision, date, and corrections. A rejection sends the affected gate back for revision; it does not invalidate unrelated approved assets.

The [visual-reference contract](../design/visual-reference-contract.md) defines the retained reference, baseline matching and responsive review requirements. G0 preview approval establishes the composition target; shared UI production follows the current complete-window override above. Keep the approved proposal accessible throughout implementation. English and Spanish share the same appearance, items and data fixture. Other phone sizes and text scales require responsive treatment rather than stretching the reference.

Only one window is active at a time. Shared asset work belongs to that window and uses the recorded reference and reuse decisions. Do not produce every background first or all sprites at once. Implementation follows approval of the current window's retained bilingual visual target, then a focused complete-screen review.

## Proposed sequence and character feasibility

1. Complete and approve M0: training data shape, starter content, and generation rules.
2. Review the character/gear feasibility specification: supported silhouettes, skin palettes, hair layers, clothing slots, animation coverage, and a small representative fit sample. This specification does not authorize a full sprite batch. A generated sample still follows background/prop approvals and G3.
3. **WIN-001 System / Home** and **WIN-002 Welcome / Sign in** have user acceptance. Home still uses fictional local data and a static illustrated avatar. WIN-006/007 UI has been implemented with production dependencies explicitly pending; the current approved window is **WIN-009 Private Fitness Profile**.
4. Continue through the listed window order, one completed milestone at a time. Deep layout, exact copy, and asset prompts are decided only when that window becomes current.

**Current next activity:** build and verify [WIN-009](../windows/WIN-009-implementation.md) against its approved ES/EN v1 targets without generating an APK. Afterward prepare WIN-010 with its own retained renders. Regional-policy, guardian and authentication dependencies remain active; review mode does not bypass them in production.

The numeric order after System is proposed and can be changed by the user. Account/setup dependencies can use fictional approved fixtures for renders; dependent data contracts must be resolved before an implemented flow is called complete. Shared language, confirmations, and error states may be required by the first consumer; they retain their own milestones and approval records. A destination pictured in an early hub render is not evidence that its feature is built.

Character appearance must use a consistent layered asset contract rather than independent flat images of every outfit: body/build variants; skin treatment applied consistently to all exposed regions; face; back/front hair; equipment per slot; and explicit headgear/hair rules. The same character and equipped items appear in status, training, tower, and gym contexts. Approve fit and layer order on representative poses before multiplying variations. If rendered sprites are chosen, controls expose only the finite approved fit combinations; source-model sliders do not imply unrendered combinations exist. The sprite-production/tool choice belongs to the shared asset feasibility work; this inventory does not choose an engine or promise that AI can produce implementation-ready animation sheets in one pass.

Exercise demonstrations are a separate content family from fantasy character sprites. Existing animation catalogs can be evaluated, but every used item needs documented permission, attribution, matching exercise variant, reviewed technique, and a readable text/static fallback. No external catalog is assumed free to copy.

## Glossary boundaries

- **System / Home:** the player’s main status window and navigation hub. “Status” is a character detail window, not a second name for the whole app.
- **Training:** real exercise plans, manual logging, workout bosses, recovery, and personal history.
- **Interdimensional Gym:** the lobby and shared social room where avatars walk, use preset chat/emotes, and manage friends. It is distinct from Training.
- **Tower:** separately played real-time action combat. A “room” is an encounter area; a “floor” can contain multiple rooms.
- **Equipment:** the owned item inventory, item details, and equip preview. Physical workout equipment is the private setup window, not the avatar armory.
- **Window:** a full screen or significant modal/overlay with its own user decision, data, or approval needs. Tabs, empty states, and minor transient variations stay inside the parent milestone unless listed separately.

Teen accounts use private profiles and restricted social defaults. Public teen ranking scopes stay unavailable until a reviewed policy explicitly permits them. Recovery protects consistency. Fasting is absent under 18 and neutral for adults, without buffs or fasting rewards. Purchases provide cosmetics, with no paid fitness XP or combat advantage.

## Window glossary

There are 64 milestones. **WIN-009 is the current approved implementation; WIN-010 is next for proposal.** Individual statuses are recorded in the machine-readable inventory; shared controls and conditional routes do not automatically complete their separate milestones. The inventory covers the currently defined V1 scope; unspecified additional mini-games and disability-specific exercise programming are later discovery work, not hidden commitments. Important conditional windows remain listed even when many players will never see them.

### System and account entry

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-001 | System / Home / Sistema / Inicio | Show the character, appropriate next activity, and earned game progress at a glance. |
| WIN-002 | Welcome / Sign in / Bienvenida / Iniciar sesión | Enter an existing account or start the account journey. |
| WIN-003 | Language Selection / Selección de idioma | Choose automatic device language or a persistent English/Spanish override. |
| WIN-004 | Create Account / Verify Email / Crear cuenta / Verificar correo | Create an email account and verify its ownership. |
| WIN-005 | Password Recovery / Recuperar contraseña | Recover access to an email account. |
| WIN-006 | Age / Region Eligibility / Edad / Región de acceso | Collect the minimum private eligibility information needed for the selected release region. |
| WIN-007 | Privacy / Required Consent / Privacidad / Consentimientos | Explain the data uses and obtain the acknowledgements required for the user’s region and age. |
| WIN-008 | Guardian Consent / Pending Access / Consentimiento del tutor / Acceso pendiente | Handle a guardian route only when the reviewed regional policy requires it. |

### Private setup and character

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-009 | Private Fitness Profile / Perfil físico privado | Record useful training inputs without equating body measurements with appearance or health. |
| WIN-010 | Goals / Experience / Objetivos / Experiencia | Choose training aims and establish a suitable starting level. |
| WIN-011 | Available Equipment / Equipo disponible | Record the actual equipment and location available for a workout. |
| WIN-012 | Schedule / Session Time / Horario / Duración de sesión | Set a realistic routine with protected recovery and editable session time. |
| WIN-013 | Character Customization / Personalización del personaje | Choose the avatar independently of private measurements and fit later gear to approved body variants. |
| WIN-014 | Character Status / Estado del personaje | Inspect the chosen character, class, game stats, and appearance. |

### Real training and progress

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-015 | Training Hub / Entrenamiento | Select the next real-world workout or a reviewed preset. |
| WIN-016 | Readiness / Adjust Today / Estado de hoy / Ajustar sesión | Use a short self-report to support an appropriate plan or rest option. |
| WIN-017 | Generated Plan Review / Revisar plan generado | Explain and review the automatically assembled plan before exercise starts. |
| WIN-018 | Preset Library / Biblioteca de rutinas | Choose a reviewed routine appropriate to age group, ability, time, and equipment. |
| WIN-019 | Routine Editor / Editar rutina | Edit a proposed workout while keeping its exercise purpose and manageable dose visible. |
| WIN-020 | Exercise Substitution / Cambiar ejercicio | Replace an exercise with an appropriate reviewed alternative. |
| WIN-021 | Exercise Guide / Animation / Guía de ejercicio / Animación | Show a reviewed demonstration, setup, cues, and common mistakes. |
| WIN-022 | Light / Medium / Hard / Ligero / Medio / Intenso | Select a bounded adjustment to the appropriate baseline workout. |
| WIN-023 | Workout Boss Briefing / Preparación del jefe de entrenamiento | Present the physical session as a boss encounter before logging starts. |
| WIN-024 | Workout Boss / Manual Logging / Jefe de entrenamiento / Registro manual | Advance the workout boss by manually logging each exercise set. |
| WIN-025 | Rest Timer / Temporizador de descanso | Support sufficient rest between exercise sets. |
| WIN-026 | Pause / End Workout / Pausar / Terminar entrenamiento | Pause or stop a physical session while preserving completed work. |
| WIN-027 | Workout Complete / Loot / Entrenamiento completado / Botín | Summarize the completed session and its earned game rewards. |
| WIN-028 | Recovery Day / Día de recuperación | Make scheduled recovery and suitable gentle activity a legitimate part of the routine. |
| WIN-029 | Training History / Progress / Historial / Progreso del entrenamiento | Review sessions and useful personal progress without body comparison. |
| WIN-030 | Session Details / Correction / Detalle de sesión / Corrección | Inspect an individual training record and correct an honest entry error. |
| WIN-031 | Consistency / Recovery Calendar / Calendario de constancia / Recuperación | Show adherence to the agreed schedule, protected recovery, and pauses. |

### Equipment and cosmetics

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-032 | Inventory / Inventario | Browse owned gear and cosmetic rewards. |
| WIN-033 | Item Details / Detalles del objeto | Explain an item’s slot, appearance, ownership, and acquisition. |
| WIN-034 | Equip / Appearance Preview / Equipar / Vista previa de apariencia | Try owned gear on the character and resolve slot compatibility. |
| WIN-035 | Cosmetic Shop / Tienda de cosméticos | Offer clearly priced cosmetic items and themes. |
| WIN-036 | Purchase / Restore Result / Compra / Resultado de restauración | Confirm an exact cosmetic purchase and explain pending, cancelled, or restored results. |

### Action tower

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-037 | Tower Entrance / Loadout / Entrada a la torre / Equipo | Prepare the Boxer for a separate action-game run. |
| WIN-038 | Tower Floor / Room Route / Piso de la torre / Ruta de salas | Understand the current floor, room connections, and available paths. |
| WIN-039 | Tower Combat / Combate en la torre | Move freely and fight with Boxer attacks in a readable isometric arena. |
| WIN-040 | Room Clear / Tower Loot / Sala despejada / Botín de la torre | Resolve a cleared room and allow a deliberate reward or exit action. |
| WIN-041 | Tower Defeat / Retry / Derrota en la torre / Reintentar | Explain a lost run and offer fair recovery options. |
| WIN-042 | Tower Run Summary / Resumen de la incursión | Summarize a completed or voluntarily ended tower run. |

### Social gym and rankings

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-043 | Interdimensional Gym / Lobby / Gimnasio interdimensional / Vestíbulo | Choose a permitted social gym instance and review basic interaction rules. |
| WIN-044 | Interdimensional Gym / Shared Room / Gimnasio interdimensional / Sala compartida | Walk around a shared social gym, use emotes, and meet permitted players. |
| WIN-045 | Player Card / Interaction / Ficha de jugador / Interacción | Inspect a permitted public identity and choose safe social actions. |
| WIN-046 | Friends / Requests / Amigos / Solicitudes | Manage friendships and requests without exposing private identity. |
| WIN-047 | Gym Public Preset Chat / Chat público del gimnasio / Frases | Communicate in a gym room using localized approved phrases and emotes. |
| WIN-048 | Private Preset Chat / Chat privado / Frases | Exchange approved localized phrases with permitted friends. |
| WIN-049 | Mute / Block / Report / Silenciar / Bloquear / Reportar | Let a player stop unwanted interaction and report behavior or identity content. |
| WIN-050 | Rankings / Scope Selection / Clasificaciones / Selección de alcance | Browse recreational fitness XP and protected-consistency standings in permitted scopes. |

### Optional adult fasting

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-051 | Adult Fasting / Optional Setup / Ayuno para adultos / Configuración opcional | Offer an optional neutral log to eligible adults who choose to enable it. |
| WIN-052 | Adult Fasting Timer / Temporizador de ayuno para adultos | Display an optional adult’s chosen schedule without rewarding duration. |
| WIN-053 | End / Correct Fasting Log / Terminar / Corregir registro de ayuno | End or correct an adult fasting entry without judgment. |
| WIN-054 | Adult Fasting History / Historial de ayuno para adultos | Review private neutral fasting records. |

### Settings and shared states

| ID | English / Español | Purpose |
| --- | --- | --- |
| WIN-055 | Settings / Configuración | Provide a clear index for user preferences and account controls. |
| WIN-056 | Account / Linked Sign-in / Cuenta / Métodos de acceso | Review account access and manage supported sign-in methods. |
| WIN-057 | Privacy / Social Permissions / Privacidad / Permisos sociales | Control visibility, discovery, contact, and optional data use. |
| WIN-058 | Accessibility / Controls / Accesibilidad / Controles | Make the approved art usable through readable interfaces, adaptable controls, and reduced effects. |
| WIN-059 | Themes / Personalization / Temas / Personalización | Preview approved interface themes without changing gameplay. |
| WIN-060 | Notifications / Reminders / Notificaciones / Recordatorios | Configure optional reminders for the agreed routine and permitted social activity. |
| WIN-061 | Help / Support / Credits / Ayuda / Soporte / Créditos | Explain the product, access support, and show policy and asset attributions. |
| WIN-062 | Export Data / Delete Account / Exportar datos / Eliminar cuenta | Give users deliberate control over their data and account lifecycle. |
| WIN-063 | Shared Confirmation / Unsaved Changes / Confirmación compartida / Cambios sin guardar | Provide a consistent reusable confirmation pattern for non-specialized actions. |
| WIN-064 | Shared Loading / Offline / Error / Empty / Estados de carga / Sin conexión / Error / Vacío | Provide coherent reusable transient and recovery states across all windows. |

## Per-window flow, data, assets, and acceptance

The steps below describe the user journey separately from production gates. Their original G0–G6 acceptance wording is read with the current complete-window TDR override above. Asset needs are unique assets or explicit reuse candidates, not authorization to generate all windows now. Dependency IDs identify related prerequisite contracts/windows; data-domain names are stable conceptual boundaries to map to the data package as it is reviewed.

### WIN-001 — System / Home / Sistema / Inicio

**Stage:** native Home preview appearance accepted on Android, 2026-09-25. See the [implementation evidence and scope](../windows/WIN-001-implementation.md).

**Purpose:** Show the character, appropriate next activity, and earned game progress at a glance.

**User steps:** 1. Open System → 2. Review character and today’s plan or protected recovery → 3. Choose Training, Tower, Equipment, or Interdimensional Gym → 4. Return here after an activity.

**Dependencies:** WIN-009, WIN-013.
**Data:** `account`, `training_profile`, `session_plan`, `progression_policy`, `recovery_policy`, `avatar_catalog`, `inventory`, `localization`.
**Asset needs:** Original cosmic chamber background; Reusable character pedestal and atmospheric foreground; Equipped character idle sample; System frame, navigation sigils, XP and consistency indicators.

**Acceptance:**

- Use fictional or approved private data in renders
- Recovery appears as a valid next action
- Fitness progression is distinct from tower progress
- Height, weight, age, and fasting are absent from the public character view
- Approved v2 mapped into the complete native Home, then focused whole-screen EN/ES and interaction checks; installed appearance accepted by the user. Preserve the measured differences and the documented limits.

### WIN-002 — Welcome / Sign in / Bienvenida / Iniciar sesión

**Stage:** active [G0 proposal](../windows/WIN-002-welcome-sign-in-g0.md), not yet approved. Retain and approve this window's own EN/ES reference before complete-window implementation.

**Purpose:** Enter an existing account or start the account journey.

**User steps:** 1. Select detected or preferred language → 2. Choose account sign-in or Google → 3. Complete the chosen authentication flow → 4. Resume setup or enter System.

**Dependencies:** WIN-003.
**Data:** `account`, `localization`, `authentication_policy`.
**Asset needs:** Original portal threshold background; Portal frame foreground; Title mark, authentication controls, provider-approved marks.

**Acceptance:**

- Account and Google login are represented
- Proposed iOS equivalent sign-in remains a platform-review decision
- Cancellation and invalid credentials preserve a recoverable route
- Approve the retained bilingual G0 target, assemble the complete window, then perform focused full-screen and interaction checks. No per-button rendering or approval loop.

### WIN-003 — Language Selection / Selección de idioma

**Purpose:** Choose automatic device language or a persistent English/Spanish override.

**User steps:** 1. View detected language → 2. Choose System default, English, or Español → 3. Preview localized labels → 4. Save and return to the originating window.

**Dependencies:** M0 and the approved shared product rules; no other window prerequisite.
**Data:** `localization`, `account_preferences`.
**Asset needs:** Reuse entry or settings background with explicit approval; Language selector frame and check states.

**Acceptance:**

- Language is available before sign-in and later in Settings
- Changing language does not reset training or account data
- Unsupported device languages have a stated English fallback
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-004 — Create Account / Verify Email / Crear cuenta / Verificar correo

**Purpose:** Create an email account and verify its ownership.

**User steps:** 1. Enter email and password → 2. Review required acknowledgements from consent flow → 3. Submit and verify email → 4. Continue private setup or resolve an existing account.

**Dependencies:** WIN-002, WIN-006, WIN-007.
**Data:** `account`, `consent_policy`, `authentication_policy`.
**Asset needs:** Reuse entry background with explicit approval; Account form, password visibility control, verification state art.

**Acceptance:**

- No public profile exposes email or birth date
- Pending, expired, resend, and existing-account states are included
- Success resumes the correct setup step
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-005 — Password Recovery / Recuperar contraseña

**Purpose:** Recover access to an email account.

**User steps:** 1. Enter account email → 2. Receive neutral next-step guidance → 3. Follow recovery link and choose new password → 4. Return to sign-in.

**Dependencies:** WIN-002.
**Data:** `account`, `authentication_policy`, `localization`.
**Asset needs:** Reuse entry background with explicit approval; Recovery panel and link status icons.

**Acceptance:**

- Confirmation does not disclose whether an address belongs to another user
- Invalid and expired links offer a clear retry
- OAuth accounts receive suitable provider guidance
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-006 — Age / Region Eligibility / Edad / Región de acceso

**Purpose:** Collect the minimum private eligibility information needed for the selected release region.

**User steps:** 1. Choose country and broad region where needed → 2. Provide age eligibility information → 3. Resolve supported, unsupported, or under-15 status → 4. Continue to the applicable consent path.

**Dependencies:** WIN-003.
**Data:** `account`, `consent_policy`, `region_catalog`, `localization`.
**Asset needs:** Reuse entry background with explicit approval; Private-data cue, region picker, eligibility status panel.

**Acceptance:**

- 15+ is the product minimum, not a universal consent rule
- No GPS is required for broad regional selection
- Unsupported-country and underage exits are explicit
- Exact country eligibility requires a reviewed policy before launch
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-007 — Privacy / Required Consent / Privacidad / Consentimientos

**Purpose:** Explain the data uses and obtain the acknowledgements required for the user’s region and age.

**User steps:** 1. Read concise localized explanation → 2. Open full policy details → 3. Choose optional permissions separately → 4. Accept required terms or leave setup.

**Dependencies:** WIN-006.
**Data:** `consent_policy`, `account`, `localization`.
**Asset needs:** Reuse entry background with explicit approval; Layered consent panels, optional switches, document links.

**Acceptance:**

- Health-data use, public game identity, and optional features are explained separately
- Optional consent is not preselected
- A consent version and decision can be recorded
- Google sign-in is not treated as health-data consent
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-008 — Guardian Consent / Pending Access / Consentimiento del tutor / Acceso pendiente

**Purpose:** Handle a guardian route only when the reviewed regional policy requires it.

**User steps:** 1. Explain why a guardian step applies → 2. Start the reviewed guardian process → 3. Show pending or declined status → 4. Resume permitted setup after approval.

**Dependencies:** WIN-006, WIN-007.
**Data:** `consent_policy`, `account`, `authentication_policy`.
**Asset needs:** Reuse entry background with explicit approval; Guardian status panel and pending/approved/declined state icons.

**Acceptance:**

- Country and age policy decides whether this appears
- No invented universal guardian threshold
- Restricted features remain unavailable while approval is pending
- Resend, expiry, and declined paths are specified
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-009 — Private Fitness Profile / Perfil físico privado

**Stage:** v1 target approved; native implementation and focused verification in progress. No APK.

**Purpose:** Record useful training inputs without equating body measurements with appearance or health.

**User steps:** 1. Review the private-data notice and applicable separate decision → 2. Enter or omit optional height and bodyweight with units → 3. Declare current readiness or pause → 4. Continue when production gates permit. Experience and goals belong to WIN-010.

**Dependencies:** WIN-007.
**Data:** `training_profile`, `account`, `unit_preferences`, `consent_policy`.
**Asset needs:** Private dossier background or approved entry reuse; Measurement fields, unit controls, suitability panel.

**Acceptance:**

- Appearance and medical assumptions are not inferred from sex presentation or measurements
- No required somatotype or BMI-based character classification
- Pain, illness, injury, or uncertain suitability can route to pause or appropriate guidance
- Adult and teen inputs are distinguishable
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-010 — Goals / Experience / Objetivos / Experiencia

**Stage:** next proposal brief prepared; retained renders and approval pending after WIN-009 verification.

**Purpose:** Choose training aims and establish a suitable starting level.

**User steps:** 1. Choose general fitness or another supported focus → 2. Describe training experience and current ability → 3. Review what the selection changes → 4. Save and continue.

**Dependencies:** WIN-009.
**Data:** `training_profile`, `training_templates`, `generation_rules`, `localization`.
**Asset needs:** Reuse private dossier background with explicit approval; Goal emblems and experience-choice panels.

**Acceptance:**

- A muscular avatar does not select advanced training
- Teen choices use reviewed teen content
- Users can change goals without losing past progress
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-011 — Available Equipment / Equipo disponible

**Purpose:** Record the actual equipment and location available for a workout.

**User steps:** 1. Choose home, gym, or custom location → 2. Select available equipment including bodyweight only → 3. Add relevant load ranges or constraints → 4. Save reusable equipment profiles.

**Dependencies:** WIN-009.
**Data:** `equipment_catalog`, `training_profile`, `generation_rules`.
**Asset needs:** Equipment room background or approved dossier reuse; Equipment category pictograms and selected/unavailable states.

**Acceptance:**

- Bodyweight-only training is a complete path
- Equipment availability filters generated plans and substitutions
- A unavailable item is never silently required
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-012 — Schedule / Session Time / Horario / Duración de sesión

**Purpose:** Set a realistic routine with protected recovery and editable session time.

**User steps:** 1. Choose available training days → 2. Set typical time per session → 3. Review planned training and recovery → 4. Save or revise schedule.

**Dependencies:** WIN-010, WIN-011.
**Data:** `training_profile`, `training_templates`, `recovery_policy`, `generation_rules`.
**Asset needs:** Reuse private dossier background with explicit approval; Weekly schedule tiles and duration control.

**Acceptance:**

- Daily hard training is not a default
- Recovery days preserve consistency
- School, sports, and other activity can inform the schedule
- Estimates include rest and warm-up
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-013 — Character Customization / Personalización del personaje

**Purpose:** Choose the avatar independently of private measurements and fit later gear to approved body variants.

**User steps:** 1. Choose male or female presentation → 2. Adjust build and muscle definition independently → 3. Select skin tone, hairstyle, hair color, and face options → 4. Preview supported gear and movement → 5. Save appearance.

**Dependencies:** WIN-009.
**Data:** `avatar_catalog`, `gear_catalog`, `account_preferences`, `localization`.
**Asset needs:** Neutral character workshop background; Turntable/pedestal foreground; Approved body layers, skin masks or swatches, hair variants, face variants, gear fitting samples; Customization controls.

**Acceptance:**

- Skin tone stays consistent across face, body, hands, and animation poses
- Hair has defined back/front layers and headgear rules
- Gear follows approved body variants without floating or clipping
- No body shape grants training prescriptions or combat power
- A representative character/gear feasibility sample is approved before large sprite batches
- Customization controls expose only approved renderable fit combinations; source-model sliders do not imply unrendered sprite combinations exist.
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-014 — Character Status / Estado del personaje

**Purpose:** Inspect the chosen character, class, game stats, and appearance.

**User steps:** 1. Open personal status → 2. Inspect Boxer abilities and earned stats → 3. Preview equipped items → 4. Open customization or equipment.

**Dependencies:** WIN-013.
**Data:** `avatar_catalog`, `progression_policy`, `gear_catalog`, `inventory`, `tower_content`.
**Asset needs:** Reuse System or workshop background with explicit approval; Full equipped character pose; Status stat glyphs and Boxer ability icons.

**Acceptance:**

- Game stats are visually separate from private fitness measurements
- Three normal attacks and one special are represented
- Paid cosmetics do not add combat advantage
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-015 — Training Hub / Entrenamiento

**Purpose:** Select the next real-world workout or a reviewed preset.

**User steps:** 1. Review today’s plan and recent training → 2. Choose generated plan or preset → 3. Open readiness check → 4. Continue to plan review.

**Dependencies:** WIN-010, WIN-011, WIN-012.
**Data:** `training_profile`, `session_plan`, `training_templates`, `recovery_policy`, `session_log`.
**Asset needs:** Training portal background; Quest plinth foreground; Training and recovery emblems.

**Acceptance:**

- Training means real exercise; the Interdimensional Gym means social play
- An appropriate rest path is as available as starting a boss
- Automatic generation never implies automatic repetition counting
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-016 — Readiness / Adjust Today / Estado de hoy / Ajustar sesión

**Purpose:** Use a short self-report to support an appropriate plan or rest option.

**User steps:** 1. Report energy, soreness, and relevant concerns → 2. Review normal, reduced, or pause guidance → 3. Choose an available session option → 4. Apply the result to today’s plan.

**Dependencies:** WIN-015.
**Data:** `readiness`, `generation_rules`, `recovery_policy`, `training_profile`.
**Asset needs:** Reuse training background with explicit approval; Readiness choices and rest/stop cue.

**Acceptance:**

- Pain, injury, illness, or concerning responses do not force a workout
- Light is a valid choice with no competitive punishment
- The app does not claim to diagnose a condition
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-017 — Generated Plan Review / Revisar plan generado

**Purpose:** Explain and review the automatically assembled plan before exercise starts.

**User steps:** 1. Choose available time and equipment profile → 2. Generate from reviewed content → 3. Review warm-up, exercises, dose, rest, and cool-down → 4. Accept, edit, or choose an alternative.

**Dependencies:** WIN-016.
**Data:** `exercise_catalog`, `training_templates`, `generation_rules`, `session_plan`, `training_profile`.
**Asset needs:** Reuse training background with explicit approval; Plan scroll, rationale labels, exercise thumbnails.

**Acceptance:**

- The generator selects reviewed exercises and templates
- Plan includes a readable explanation of fit to inputs
- Regeneration does not randomly reset useful progression
- Only plan generation is automatic; logs are manual
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-018 — Preset Library / Biblioteca de rutinas

**Purpose:** Choose a reviewed routine appropriate to age group, ability, time, and equipment.

**User steps:** 1. Filter supported presets → 2. Inspect purpose and expected duration → 3. Check fit to equipment and ability → 4. Use the preset and review the session.

**Dependencies:** WIN-015.
**Data:** `training_templates`, `exercise_catalog`, `training_profile`, `generation_rules`.
**Asset needs:** Archive chamber background or approved training reuse; Preset banners, filter controls, goal emblems.

**Acceptance:**

- Teen and adult applicability is visible
- Unsuitable or unavailable presets explain why
- A preset remains editable within supported rules
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-019 — Routine Editor / Editar rutina

**Purpose:** Edit a proposed workout while keeping its exercise purpose and manageable dose visible.

**User steps:** 1. Review ordered blocks → 2. Adjust supported sets, repetitions, duration, or rest → 3. Replace or remove an exercise → 4. Review recalculated duration and plan checks → 5. Save the edited plan.

**Dependencies:** WIN-017, WIN-018.
**Data:** `session_plan`, `exercise_catalog`, `generation_rules`, `training_templates`.
**Asset needs:** Reuse training background with explicit approval; Editable exercise cards, reorder controls, validation markers.

**Acceptance:**

- Edits preserve warm-up and recovery intent or explain the change
- Conflicts with equipment or reviewed bounds are identified
- No load is prescribed from bodyweight, avatar, or game difficulty alone
- Save and cancel states are shown
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-020 — Exercise Substitution / Cambiar ejercicio

**Purpose:** Replace an exercise with an appropriate reviewed alternative.

**User steps:** 1. Select the exercise to replace → 2. Choose reason such as equipment or difficulty → 3. Review compatible alternatives and dose changes → 4. Confirm and return to the editor.

**Dependencies:** WIN-019.
**Data:** `exercise_catalog`, `generation_rules`, `equipment_catalog`, `session_plan`.
**Asset needs:** Reuse training background with explicit approval; Comparison sheet, movement-pattern icons, alternative thumbnails.

**Acceptance:**

- Alternatives preserve the intended movement or disclose a broader plan adjustment
- Equipment and teen applicability are checked
- Pain-related requests can lead to stop guidance rather than a forced substitute
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-021 — Exercise Guide / Animation / Guía de ejercicio / Animación

**Purpose:** Show a reviewed demonstration, setup, cues, and common mistakes.

**User steps:** 1. Open an exercise from a plan or library → 2. Review setup and demonstration → 3. Read brief cues and stop guidance → 4. Return to the current plan or session.

**Dependencies:** WIN-017.
**Data:** `exercise_catalog`, `exercise_media`, `localization`, `accessibility_preferences`.
**Asset needs:** Neutral demonstration stage; Licensed or original exercise demonstration media; Replay, pause, view-angle, caption and cue assets.

**Acceptance:**

- Media source, license, attribution, and variant match are recorded
- Demonstration does not claim to verify the user’s form
- A text/static fallback exists
- Instructions use separate aerobic and resistance effort scales appropriately
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-022 — Light / Medium / Hard / Ligero / Medio / Intenso

**Purpose:** Select a bounded adjustment to the appropriate baseline workout.

**User steps:** 1. Read Medium baseline → 2. Compare available Light and Hard adjustments → 3. Review changed dose, variations, and rest → 4. Apply choice or return unchanged.

**Dependencies:** WIN-016, WIN-017.
**Data:** `generation_rules`, `readiness`, `session_plan`, `progression_policy`.
**Asset needs:** Reuse training background with explicit approval; Three difficulty sigils and change-summary panel.

**Acceptance:**

- Medium is default
- Hard is available only when supported by readiness, experience, and reviewed rules
- No XP multiplier rewards harder exercise
- Rest and stop remain accessible
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-023 — Workout Boss Briefing / Preparación del jefe de entrenamiento

**Purpose:** Present the physical session as a boss encounter before logging starts.

**User steps:** 1. Review session blocks and exercise instructions → 2. Inspect the boss and reward preview → 3. Confirm equipment and readiness → 4. Begin warm-up and the session.

**Dependencies:** WIN-017, WIN-022.
**Data:** `session_plan`, `exercise_catalog`, `boss_catalog`, `progression_policy`.
**Asset needs:** Workout arena background; Arena foreground props; Approved boss idle sprite and equipped character idle sprite; Session objective frame and start control.

**Acceptance:**

- Rewards reflect the approved session rather than absolute kilograms
- User can edit or return before starting
- Fantasy combat visuals do not obscure exercise instructions
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-024 — Workout Boss / Manual Logging / Jefe de entrenamiento / Registro manual

**Purpose:** Advance the workout boss by manually logging each exercise set.

**User steps:** 1. Review current exercise and target → 2. Perform the set away from interactive combat controls → 3. Enter completed reps, load, duration, and optional effort → 4. Save the set and show boss progress → 5. Move to rest or next exercise.

**Dependencies:** WIN-023, WIN-021.
**Data:** `session_plan`, `session_log`, `exercise_catalog`, `progression_policy`, `boss_catalog`.
**Asset needs:** Reuse approved workout arena and foreground; Approved boss damage/idle and character encouragement states; Repetition/load/duration inputs, set ledger, boss progress HUD.

**Acceptance:**

- No camera, wearable, detection, or automatic rep counting is implied
- User can correct a log without duplicating rewards
- Timed and repetition-based exercises both fit
- Stop and exercise guidance remain accessible
- An interrupted session preserves confirmed logs
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-025 — Rest Timer / Temporizador de descanso

**Purpose:** Support sufficient rest between exercise sets.

**User steps:** 1. Start suggested rest after a saved set → 2. Review upcoming exercise → 3. Extend, pause, or skip rest → 4. Return to logging when ready.

**Dependencies:** WIN-024.
**Data:** `session_plan`, `session_log`, `timer_state`, `recovery_policy`.
**Asset needs:** Reuse workout arena with explicit approval; Subdued boss/character idle reuse; Timer ring, extend and continue controls.

**Acceptance:**

- Extending rest is not penalized
- Timer completion does not auto-log a set
- Reduced-motion and quiet states are designed
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-026 — Pause / End Workout / Pausar / Terminar entrenamiento

**Purpose:** Pause or stop a physical session while preserving completed work.

**User steps:** 1. Pause from the session → 2. Review resume, adjust, or stop options → 3. Confirm early end when needed → 4. Save completed work and return to an appropriate state.

**Dependencies:** WIN-024.
**Data:** `session_log`, `session_plan`, `progression_policy`, `recovery_policy`.
**Asset needs:** Reuse subdued workout arena; Pause overlay, reason choices, saved-progress confirmation.

**Acceptance:**

- Illness, pain, or inability to continue is not framed as failure
- Saved sets survive exit
- Partial completion is described honestly and cannot be repeatedly rewarded
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-027 — Workout Complete / Loot / Entrenamiento completado / Botín

**Purpose:** Summarize the completed session and its earned game rewards.

**User steps:** 1. View completed work and boss result → 2. Review coins, cosmetics, and fitness XP → 3. Optionally record how the session felt → 4. Return to System or history.

**Dependencies:** WIN-024, WIN-026.
**Data:** `session_log`, `progression_policy`, `inventory`, `economy_policy`, `boss_catalog`.
**Asset needs:** Approved victory arena variant; Defeated boss or reward chest assets; Earned item sprites and reward reveal UI.

**Acceptance:**

- Rewards are granted once
- Light and adapted sessions receive appropriate recognition
- No leaderboard incentive for excessive volume
- Loot can be reviewed with reduced motion
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-028 — Recovery Day / Día de recuperación

**Purpose:** Make scheduled recovery and suitable gentle activity a legitimate part of the routine.

**User steps:** 1. Review why today is recovery → 2. Choose rest or an available gentle option → 3. Record the choice if useful → 4. Continue to social gym, tower, or System.

**Dependencies:** WIN-012, WIN-016.
**Data:** `recovery_policy`, `session_log`, `progression_policy`, `training_templates`.
**Asset needs:** Calm sanctuary background; Recovery props; Character resting pose if approved; Protected-consistency emblem.

**Acceptance:**

- Recovery protects consistency under the reviewed policy
- Fasting is never a recovery quest
- Tower and social play remain available without extra physical exercise
- Rest is not described as a lost day
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-029 — Training History / Progress / Historial / Progreso del entrenamiento

**Purpose:** Review sessions and useful personal progress without body comparison.

**User steps:** 1. Choose time range → 2. Review consistency, completed sessions, and personal exercise progress → 3. Open a session → 4. Adjust display units or filters.

**Dependencies:** WIN-024.
**Data:** `session_log`, `progression_policy`, `recovery_policy`, `unit_preferences`.
**Asset needs:** Training archive background; Timeline, trend and filter assets.

**Acceptance:**

- No public disclosure of weight or private training logs
- Progress is personal rather than a raw-lift competition
- Empty, partial, and mixed-unit histories are handled
- No health improvement is claimed from game XP
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-030 — Session Details / Correction / Detalle de sesión / Corrección

**Purpose:** Inspect an individual training record and correct an honest entry error.

**User steps:** 1. Open a past session → 2. Inspect exercises, logs, and adjustments → 3. Correct permitted values or add notes → 4. Review saved correction and unchanged reward handling.

**Dependencies:** WIN-029.
**Data:** `session_log`, `progression_policy`, `unit_preferences`.
**Asset needs:** Reuse training archive background; Detailed set table, edit controls, correction state.

**Acceptance:**

- History distinguishes planned from completed work
- Editing a past record cannot farm rewards
- Units and exercise variant remain unambiguous
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-031 — Consistency / Recovery Calendar / Calendario de constancia / Recuperación

**Purpose:** Show adherence to the agreed schedule, protected recovery, and pauses.

**User steps:** 1. Inspect the calendar → 2. Open a training or recovery day → 3. Review protected streak rules → 4. Adjust future schedule or record an allowed pause.

**Dependencies:** WIN-012, WIN-029.
**Data:** `recovery_policy`, `progression_policy`, `session_log`, `training_profile`.
**Asset needs:** Reuse System or archive background; Training/recovery/pause day markers and streak glyph.

**Acceptance:**

- Ranking is based on protected consistency, not consecutive hard workouts
- Illness and recovery states have explicit treatment
- Changing a schedule does not silently rewrite completed history
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-032 — Inventory / Inventario

**Purpose:** Browse owned gear and cosmetic rewards.

**User steps:** 1. Filter items by slot, rarity, or ownership state → 2. Select an item → 3. Inspect details or preview on the character → 4. Equip supported items.

**Dependencies:** WIN-013.
**Data:** `inventory`, `gear_catalog`, `economy_policy`, `avatar_catalog`.
**Asset needs:** Armory background; Gear racks and inventory foreground; Item icons and optional equipped character preview; Inventory grid and rarity borders.

**Acceptance:**

- Basic, Rare, and Ultra Rare describe earned rarity
- Premium denotes cosmetic availability or style, not superior power
- Filter and empty-slot states are included
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-033 — Item Details / Detalles del objeto

**Purpose:** Explain an item’s slot, appearance, ownership, and acquisition.

**User steps:** 1. Open an item → 2. Inspect visual and description → 3. Review source and cosmetic status → 4. Preview, equip, or return.

**Dependencies:** WIN-032.
**Data:** `gear_catalog`, `inventory`, `economy_policy`, `localization`.
**Asset needs:** Reuse armory background; Large item sprite and rarity frame; Source/ownership indicators.

**Acceptance:**

- Actual appearance and slot restrictions are clear
- Price and earnable source are distinct
- Paid items have no hidden fitness XP or combat advantage
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-034 — Equip / Appearance Preview / Equipar / Vista previa de apariencia

**Purpose:** Try owned gear on the character and resolve slot compatibility.

**User steps:** 1. Choose an equipment slot → 2. Compare equipped and candidate item → 3. Inspect supported views and representative motion → 4. Apply or discard the change.

**Dependencies:** WIN-013, WIN-032, WIN-033.
**Data:** `avatar_catalog`, `gear_catalog`, `inventory`, `account_preferences`.
**Asset needs:** Reuse character workshop or armory; Full layered equipped character, hair/headgear alternatives and body-fit variants; Slot map and apply/cancel controls.

**Acceptance:**

- Items visibly follow the character in status, tower, and gym
- Skin tone and hair customization survive gear swaps
- Headgear hide/show rules are explicit
- No clipping, missing hands, misordered layers, or floating items in approved samples
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-035 — Cosmetic Shop / Tienda de cosméticos

**Purpose:** Offer clearly priced cosmetic items and themes.

**User steps:** 1. Browse categories → 2. Inspect an exact cosmetic preview → 3. Review price and ownership → 4. Start purchase or return.

**Dependencies:** WIN-033.
**Data:** `gear_catalog`, `theme_catalog`, `inventory`, `economy_policy`, `purchase_catalog`.
**Asset needs:** Merchant alcove background; Cosmetic display props; Item/theme preview art; Price and ownership labels.

**Acceptance:**

- No purchasable fitness XP or combat power
- No paid randomized loot in V1
- Availability and teen purchase requirements follow the reviewed platform/region policy
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-036 — Purchase / Restore Result / Compra / Resultado de restauración

**Purpose:** Confirm an exact cosmetic purchase and explain pending, cancelled, or restored results.

**User steps:** 1. Review exact item and total displayed price → 2. Enter the platform purchase flow → 3. Handle success, cancellation, pending, or error → 4. Verify entitlement or restore purchases.

**Dependencies:** WIN-035.
**Data:** `purchase_catalog`, `inventory`, `economy_policy`, `account`.
**Asset needs:** Reuse shop background; Purchase summary and entitlement status icons.

**Acceptance:**

- The approved item and price stay visible before purchase
- A cancelled or pending purchase never appears owned prematurely
- Restore and duplicate-ownership states are covered
- Platform-owned purchase UI is documented as external, not a generated imitation
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-037 — Tower Entrance / Loadout / Entrada a la torre / Equipo

**Purpose:** Prepare the Boxer for a separate action-game run.

**User steps:** 1. Review tower progress → 2. Inspect gear and four abilities → 3. Choose supported starting floor or run option → 4. Enter the tower.

**Dependencies:** WIN-014, WIN-034.
**Data:** `tower_content`, `progression_policy`, `inventory`, `avatar_catalog`.
**Asset needs:** Tower gate background; Entrance props; Equipped Boxer idle sprite; Run controls and ability/loadout frame.

**Acceptance:**

- Rest days do not lock tower play
- Three attacks and one special are visible
- Exercise load does not automatically set avatar strength
- Tower progression is distinct from fitness XP
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-038 — Tower Floor / Room Route / Piso de la torre / Ruta de salas

**Purpose:** Understand the current floor, room connections, and available paths.

**User steps:** 1. Enter a generated floor → 2. Inspect explored rooms and exits → 3. Choose an accessible room → 4. Track the requirement to clear enemies before advancing.

**Dependencies:** WIN-037.
**Data:** `tower_content`, `tower_run_state`.
**Asset needs:** Modular floor/room backgrounds and door props; Room-map tiles and discovered/locked/cleared indicators.

**Acceptance:**

- Procedural variation uses approved room pieces
- The route is readable without spoiling unexplored rooms
- Floor difficulty does not prescribe more physical exercise
- Infinite tower means new floors generated as needed, not an infinite asset batch
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-039 — Tower Combat / Combate en la torre

**Purpose:** Move freely and fight with Boxer attacks in a readable isometric arena.

**User steps:** 1. Move into a room → 2. Use free movement and three normal attacks → 3. Build and activate the special → 4. Defeat every required enemy → 5. Continue through the exit.

**Dependencies:** WIN-038.
**Data:** `tower_content`, `tower_run_state`, `avatar_catalog`, `gear_catalog`, `accessibility_preferences`.
**Asset needs:** Approved modular arena backgrounds; Collision-readable foreground props; Layered Boxer directional movement/attack/hit sprites; enemy sprites; Ability effects, joystick, four action buttons and health HUD.

**Acceptance:**

- View resembles the approved isometric combat concept without copying another game’s assets
- Movement and attack controls fit mobile reach zones
- Gear and hair remain coherent across approved motions
- Enemies, damage feedback, and interactable exits remain readable
- Orientation is confirmed in G0; landscape is a proposal
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-040 — Room Clear / Tower Loot / Sala despejada / Botín de la torre

**Purpose:** Resolve a cleared room and allow a deliberate reward or exit action.

**User steps:** 1. Confirm all required enemies are defeated → 2. Inspect reward → 3. Collect once → 4. Choose an unlocked exit or continue.

**Dependencies:** WIN-039.
**Data:** `tower_run_state`, `inventory`, `economy_policy`, `progression_policy`.
**Asset needs:** Reuse approved cleared room; Chest/drop and cleared-door props; Loot sprites and compact reward controls.

**Acceptance:**

- Reward collection is idempotent
- Physical workout data is not changed by tower loot
- The next exit and current run state are clear
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-041 — Tower Defeat / Retry / Derrota en la torre / Reintentar

**Purpose:** Explain a lost run and offer fair recovery options.

**User steps:** 1. Review defeat summary → 2. Inspect kept and lost run progress → 3. Retry the allowed checkpoint or leave → 4. Return to entrance or the run.

**Dependencies:** WIN-039.
**Data:** `tower_run_state`, `progression_policy`, `economy_policy`.
**Asset needs:** Reuse approved arena with subdued overlay; Boxer defeat pose and loss/result panel.

**Acceptance:**

- Defeat never removes completed physical workouts
- No paid combat-power rescue is offered
- Retry and leave outcomes are explicit
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-042 — Tower Run Summary / Resumen de la incursión

**Purpose:** Summarize a completed or voluntarily ended tower run.

**User steps:** 1. Review highest floor and rooms cleared → 2. Inspect earned tower rewards → 3. See relevant game achievements → 4. Return to System or start another run.

**Dependencies:** WIN-040, WIN-041.
**Data:** `tower_run_state`, `progression_policy`, `inventory`, `economy_policy`.
**Asset needs:** Tower overlook or approved entrance reuse; Run medal and reward summary art.

**Acceptance:**

- Tower achievements and fitness achievements remain clearly labeled
- Exiting is a valid action
- Rewards do not duplicate after reconnect or reopen
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-043 — Interdimensional Gym / Lobby / Gimnasio interdimensional / Vestíbulo

**Purpose:** Choose a permitted social gym instance and review basic interaction rules.

**User steps:** 1. Review room availability and social rules → 2. Choose an available instance → 3. Confirm entry when needed → 4. Enter the shared gym.

**Dependencies:** WIN-013, WIN-057.
**Data:** `social_policy`, `social_room_state`, `account`, `preset_messages`.
**Asset needs:** Portal concourse background; Room portal props; Character preview reuse; Room list and occupancy icons.

**Acceptance:**

- This is a social space, distinct from real training plans
- Teen room access and discoverability follow reviewed policy
- Full, unavailable, and reconnect states are included
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-044 — Interdimensional Gym / Shared Room / Gimnasio interdimensional / Sala compartida

**Purpose:** Walk around a shared social gym, use emotes, and meet permitted players.

**User steps:** 1. Enter the instance → 2. Move freely among approved props → 3. Select a player or use a preset phrase/emote → 4. Open friends or limited chat → 5. Leave the room.

**Dependencies:** WIN-043.
**Data:** `social_room_state`, `social_policy`, `avatar_catalog`, `gear_catalog`, `preset_messages`.
**Asset needs:** Original interdimensional gym background pieces; Fitness-themed decor, portals and readable walkable foreground; Equipped player directional idle/walk sprites and emotes; Player markers and social HUD.

**Acceptance:**

- Other players cannot see private measurements, age, or fasting logs
- Gear appearance is consistent with equipment preview
- No free-text chat in V1
- Username and behavior reporting remain available
- Gym props do not imply automatic rep detection
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-045 — Player Card / Interaction / Ficha de jugador / Interacción

**Purpose:** Inspect a permitted public identity and choose safe social actions.

**User steps:** 1. Select a visible player → 2. View alias, avatar, and allowed achievements → 3. Send a permitted friend request or preset interaction → 4. Mute, block, report, or close.

**Dependencies:** WIN-044.
**Data:** `social_policy`, `account`, `avatar_catalog`, `progression_policy`.
**Asset needs:** Reuse approved gym background; Public avatar portrait and action panel.

**Acceptance:**

- Teen profiles are private by default
- Age, exact location, health data, and physical measurements are absent
- Actions obey friendship and blocked-user state
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-046 — Friends / Requests / Amigos / Solicitudes

**Purpose:** Manage friendships and requests without exposing private identity.

**User steps:** 1. Open friends or requests → 2. Review a permitted alias and avatar → 3. Accept, decline, remove, or invite where allowed → 4. Open a permitted private conversation.

**Dependencies:** WIN-045.
**Data:** `social_policy`, `friends_graph`, `account`.
**Asset needs:** Reuse System or gym background; Friend avatars, request badges, presence indicators.

**Acceptance:**

- Blocked players cannot send new requests or direct messages
- Teen discoverability and invitation defaults stay restricted
- Online presence visibility has a privacy setting
- Friend removal outcome is clear
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-047 — Gym Public Preset Chat / Chat público del gimnasio / Frases

**Purpose:** Communicate in a gym room using localized approved phrases and emotes.

**User steps:** 1. Open the room chat → 2. Pick a phrase category → 3. Preview and send an approved phrase → 4. Read locally translated replies → 5. Mute, report, or close.

**Dependencies:** WIN-044.
**Data:** `preset_messages`, `social_policy`, `social_room_state`, `localization`.
**Asset needs:** Reuse approved gym scene; Chat panel, phrase category icons, emote assets.

**Acceptance:**

- No keyboard/free-text field is offered
- Phrase identifiers localize to each recipient’s selected language
- Spam limits and report paths are represented
- Usernames remain reportable content
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-048 — Private Preset Chat / Chat privado / Frases

**Purpose:** Exchange approved localized phrases with permitted friends.

**User steps:** 1. Choose a permitted friend → 2. Open the conversation → 3. Select and send a preset phrase or emote → 4. Mute, block, report, or close.

**Dependencies:** WIN-046.
**Data:** `preset_messages`, `social_policy`, `friends_graph`, `localization`.
**Asset needs:** Reuse System or gym background; Private conversation panel and phrase picker.

**Acceptance:**

- Strangers cannot initiate private messages to teens
- Block status prevents delivery
- No free-text field is offered
- Empty, offline, and unavailable conversations have clear states
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-049 — Mute / Block / Report / Silenciar / Bloquear / Reportar

**Purpose:** Let a player stop unwanted interaction and report behavior or identity content.

**User steps:** 1. Choose the relevant player or message → 2. Select mute, block, or report → 3. Choose an approved report reason and review the effect → 4. Confirm and see outcome.

**Dependencies:** WIN-045, WIN-047, WIN-048.
**Data:** `social_policy`, `report_reason_catalog`, `friends_graph`.
**Asset needs:** Reuse originating background; Safety action sheet, reason icons, confirmation state.

**Acceptance:**

- Blocking is effective for friend requests and private contact
- Reports cover aliases, messages, and behavior
- A report receipt does not promise an unreviewed moderation outcome
- Restricted structured input is supported without unrestricted teen free text
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-050 — Rankings / Scope Selection / Clasificaciones / Selección de alcance

**Purpose:** Browse recreational fitness XP and protected-consistency standings in permitted scopes.

**User steps:** 1. Choose fitness XP or consistency → 2. Select permitted friends, state/region, country, or world scope → 3. Review own position and rule explanation → 4. Open only a permitted public player card.

**Dependencies:** WIN-031, WIN-046, WIN-057.
**Data:** `ranking_policy`, `progression_policy`, `social_policy`, `region_catalog`.
**Asset needs:** Hall of records background; Ranking banners, scope emblems, list frames.

**Acceptance:**

- Teen accounts default to private friends-only rankings; public teen scope is unavailable until a reviewed policy explicitly enables it
- No ranking by weight, body shape, fasting, or absolute lifted kilograms
- Regional scope uses broad volunteered region, not continuous GPS
- Self-reported workouts are described as recreational data
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-051 — Adult Fasting / Optional Setup / Ayuno para adultos / Configuración opcional

**Purpose:** Offer an optional neutral log to eligible adults who choose to enable it.

**User steps:** 1. Open adult-only wellness settings → 2. Read suitability and professional-supervision information → 3. Choose a supported plan no longer than 20 hours → 4. Confirm enablement or leave it off.

**Dependencies:** WIN-009, WIN-055.
**Data:** `fasting_policy`, `fasting_log`, `account_preferences`, `consent_policy`.
**Asset needs:** Quiet wellness background; Neutral clock symbols and informational panel.

**Acceptance:**

- Entire feature is absent below 18
- No XP, buffs, streak rewards, ranks, or progressive fasting targets
- 20:4 is a supported-plan cap, not a safety claim
- Suitability copy receives appropriate review before use
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-052 — Adult Fasting Timer / Temporizador de ayuno para adultos

**Purpose:** Display an optional adult’s chosen schedule without rewarding duration.

**User steps:** 1. Review current timer and planned end → 2. Start or correct the chosen start time → 3. View neutral progress and information → 4. End at any time or finish at the chosen limit.

**Dependencies:** WIN-051.
**Data:** `fasting_policy`, `fasting_log`, `timer_state`.
**Asset needs:** Reuse quiet wellness background; Neutral timer and stop control.

**Acceptance:**

- No projected fat-burning, autophagy, or health-benefit claims
- End is always prominent and penalty-free
- No supported plan above 20 hours or automatic extension
- A missed timer end records reality without encouraging continuation
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-053 — End / Correct Fasting Log / Terminar / Corregir registro de ayuno

**Purpose:** End or correct an adult fasting entry without judgment.

**User steps:** 1. Choose end or correction → 2. Review actual start and end → 3. Save or discard the log → 4. Return to history or System.

**Dependencies:** WIN-052.
**Data:** `fasting_log`, `fasting_policy`.
**Asset needs:** Reuse quiet wellness background; Neutral entry review and saved-state controls.

**Acceptance:**

- Early ending loses no game progress
- Actual timestamps may be corrected without turning over-20-hour entries into supported targets
- No celebratory duration ranking or failure label
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-054 — Adult Fasting History / Historial de ayuno para adultos

**Purpose:** Review private neutral fasting records.

**User steps:** 1. Open private history → 2. Inspect dates and recorded durations → 3. Open an entry to correct or delete → 4. Disable the feature if desired.

**Dependencies:** WIN-053.
**Data:** `fasting_log`, `fasting_policy`, `account_preferences`.
**Asset needs:** Reuse quiet wellness background; Neutral history list and edit/delete controls.

**Acceptance:**

- History stays private and never feeds ranking or training load escalation
- No streak or longest-fast celebration
- Disabling and deleting records have distinct effects
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-055 — Settings / Configuración

**Purpose:** Provide a clear index for user preferences and account controls.

**User steps:** 1. Open Settings → 2. Choose language/units, privacy, accessibility, themes, notifications, or account → 3. Update a preference in its window → 4. Return to the originating activity.

**Dependencies:** WIN-002.
**Data:** `account_preferences`, `localization`, `unit_preferences`.
**Asset needs:** System archive/settings background; Settings category sigils and preference controls.

**Acceptance:**

- Language is manually switchable after automatic selection
- Metric and imperial preferences preserve canonical values
- Adult-only wellness is absent for teen accounts
- Changes do not silently reset progress
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-056 — Account / Linked Sign-in / Cuenta / Métodos de acceso

**Purpose:** Review account access and manage supported sign-in methods.

**User steps:** 1. Review masked account identity → 2. Link or inspect a supported sign-in method → 3. Reauthenticate for sensitive changes → 4. Confirm result or sign out.

**Dependencies:** WIN-055.
**Data:** `account`, `authentication_policy`, `consent_policy`.
**Asset needs:** Reuse settings background; Linked-provider icons and account action panels.

**Acceptance:**

- Linking does not create duplicate characters or lose progress
- Provider disconnect cannot strand an account without an access path
- Sign-out and account deletion are distinct
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-057 — Privacy / Social Permissions / Privacidad / Permisos sociales

**Purpose:** Control visibility, discovery, contact, and optional data use.

**User steps:** 1. Review private/public data distinction → 2. Inspect age-appropriate visibility and contact options → 3. Change permitted options → 4. Save and review the effect.

**Dependencies:** WIN-007.
**Data:** `social_policy`, `consent_policy`, `account_preferences`, `ranking_policy`.
**Asset needs:** Reuse settings background; Privacy shields, visibility previews, blocked-user list.

**Acceptance:**

- Teen defaults remain private and restricted
- Health data never becomes public through a leaderboard switch
- Geolocation is not required
- Previously granted optional consents can be reviewed
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-058 — Accessibility / Controls / Accesibilidad / Controles

**Purpose:** Make the approved art usable through readable interfaces, adaptable controls, and reduced effects.

**User steps:** 1. Review text and contrast previews → 2. Set motion, flash, sound, haptic, and subtitle preferences → 3. Preview tower control placement where supported → 4. Save and return.

**Dependencies:** WIN-055.
**Data:** `accessibility_preferences`, `localization`.
**Asset needs:** Reuse settings background; Text/contrast previews and mobile control mockup; Reduced-effect variants of approved UI assets.

**Acceptance:**

- Text fit is verified in both languages
- Meaning is not conveyed by color alone
- Reduced motion avoids essential information loss
- Interface accessibility is included even though disability-specific exercise programming is deferred
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-059 — Themes / Personalization / Temas / Personalización

**Purpose:** Preview approved interface themes without changing gameplay.

**User steps:** 1. Browse owned/default themes → 2. Preview the theme on representative UI → 3. Apply or open the exact shop item → 4. Restore the default if desired.

**Dependencies:** WIN-055, WIN-035.
**Data:** `theme_catalog`, `inventory`, `account_preferences`.
**Asset needs:** Theme preview backgrounds and frames approved individually; Sample icons and color/material variants.

**Acceptance:**

- Themes preserve contrast and legibility
- Theme purchases are cosmetic
- Applying a theme does not change training, stats, or eligibility
- Each theme’s asset changes follow the same serial gates
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-060 — Notifications / Reminders / Notificaciones / Recordatorios

**Purpose:** Configure optional reminders for the agreed routine and permitted social activity.

**User steps:** 1. Choose reminder categories → 2. Select schedule and quiet hours → 3. Review the platform permission request if needed → 4. Save or disable reminders.

**Dependencies:** WIN-012, WIN-055.
**Data:** `account_preferences`, `recovery_policy`, `social_policy`, `localization`.
**Asset needs:** Reuse settings background; Reminder-category icons and quiet-hours controls.

**Acceptance:**

- Reminders respect recovery and pauses
- No pressure to extend fasting or complete excessive training
- Public lock-screen wording avoids private health details
- Platform-owned permission dialogs are documented as external
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-061 — Help / Support / Credits / Ayuda / Soporte / Créditos

**Purpose:** Explain the product, access support, and show policy and asset attributions.

**User steps:** 1. Choose gameplay or workout-app help → 2. Read relevant guidance or troubleshooting → 3. Open the supported help route → 4. Review credits and third-party media licenses.

**Dependencies:** WIN-055.
**Data:** `help_content`, `media_attribution`, `localization`.
**Asset needs:** Reuse settings/archive background; Help-category emblems and credits panels.

**Acceptance:**

- Exercise media attribution is discoverable where license requires it
- Help distinguishes manual logging from plan generation
- Support route and report-abuse route are not conflated
- No credentials or private training logs are automatically disclosed
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-062 — Export Data / Delete Account / Exportar datos / Eliminar cuenta

**Purpose:** Give users deliberate control over their data and account lifecycle.

**User steps:** 1. Choose export or account deletion → 2. Review scope, consequences, and retention explanation → 3. Reauthenticate where needed → 4. Confirm and view pending or completed outcome.

**Dependencies:** WIN-056.
**Data:** `account`, `consent_policy`, `data_lifecycle_policy`, `session_log`, `fasting_log`.
**Asset needs:** Reuse settings background; Distinct export and deletion panels, progress/result states.

**Acceptance:**

- Deletion is available inside the app
- Export and deletion are separate actions
- Retention and recovery claims match reviewed policy
- Destructive actions cannot be triggered by a stray tap
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-063 — Shared Confirmation / Unsaved Changes / Confirmación compartida / Cambios sin guardar

**Purpose:** Provide a consistent reusable confirmation pattern for non-specialized actions.

**User steps:** 1. Trigger an action that needs review → 2. Read its concrete effect → 3. Confirm, cancel, or keep editing → 4. Return to the correct originating state.

**Dependencies:** M0 and the approved shared product rules; no other window prerequisite.
**Data:** `localization`, `action_state`.
**Asset needs:** Reuse and dim the originating background with explicit approval; Shared confirmation frame, primary/secondary controls.

**Acceptance:**

- Copy names the actual action
- Default focus and control placement discourage accidental loss
- Purchase, deletion, workout stop, and fasting end keep their own domain-specific milestones
- No generic confirmation replaces a required consent flow
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

### WIN-064 — Shared Loading / Offline / Error / Empty / Estados de carga / Sin conexión / Error / Vacío

**Purpose:** Provide coherent reusable transient and recovery states across all windows.

**User steps:** 1. Encounter a loading, empty, offline, sync, or error state → 2. See what is known and what is still pending → 3. Retry, continue locally where supported, or return → 4. Confirm recovery without duplicate work.

**Dependencies:** M0 and the approved shared product rules; no other window prerequisite.
**Data:** `localization`, `connection_state`, `action_state`.
**Asset needs:** Reuse originating backgrounds with explicit approval; Shared loading sigil, empty-state illustration, offline/error/sync icons.

**Acceptance:**

- No invented successful save, purchase, or reward while status is uncertain
- Manual workout drafts and confirmed logs are visually distinguishable
- Loading respects reduced motion
- Every consuming window records which of these states it needs
- G0–G6 approved in sequence; reuse or N/A recorded; English/Spanish and relevant teen/adult/mobile states verified.

## Review outcome to record next

M0's data shape is accepted for design; content review remains pending. WIN-009's retained bilingual target and staged profile flow are approved. Complete its implementation and focused full-screen checks without an APK, then present WIN-010 Goals / Experience as the next retained proposal. WIN-010 requires its own target approval before implementation. Full modular character and gear production remain separate milestones.

Canonical machine-readable inventory: [`data/product/window-milestones.json`](../../data/product/window-milestones.json). Original concept context: [`design/visual-approval-plan.md`](../../design/visual-approval-plan.md). Research context: [`docs/research/2026-09-25-fitness-game-research.md`](../research/2026-09-25-fitness-game-research.md).
