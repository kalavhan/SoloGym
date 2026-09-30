# Fantasy pixel UI: reusable components and delivery plan

This is the proposed implementation approach for the [confirmed product direction](../product/fantasy-mvp-direction.md), recorded 2026-09-29. The user requested complete visual references first, then **one component per PR/MR**, with each component matching the chosen style. No runtime changes are implemented by this plan.

## Current Home priority

The user approved `design/fantasy-home-r2/home-approved.png` on 2026-09-30.
Its `IMPLEMENTATION.md` requires matching the assembled Home composition with
separate architecture, exterior, props, character and live interface. Continue
component-sized PRs **inside that Home composition**, ahead of unrelated galleries.
The [room-shell iteration](pixel-home-room.md) starts this work. The [ring/room-object
component](pixel-room-object.md) follows the merged room shell (PR #20); the
remaining HUD and props are still pending.

## Screen references before component production

Seven rendered landscape Spanish concepts were prepared in separate local work under `design/fantasy-mvp-r1/`. The render pack is not included in the primary-button PR. They await screen review; none is an approved screen implementation. Their names are:

| Reference | Purpose | Components to reuse |
| --- | --- | --- |
| `01-login` | Email/password and Google entry | Primary button, form field, framed panel, text action |
| `02-create-account` | Account creation | The same form system and validation messages |
| `03-character` | Eight Barbarian appearances | Choice control, character viewport, primary button |
| `04-home` | Character's personal home gym | Room objects, navigation, routine summary, decoration controls |
| `05-workouts` | Current/completed/missed routines and editing entry | Week strip, status row, difficulty choice, navigation |
| `06-boss-workout` | Planned exercise and manual set logging as a battle | Boss progress, exercise card, quantity input, rest timer, pause/end |
| `07-fasting` | Optional adult-only neutral timer and history | Timer, history row, start/end correction, navigation |

Use the approved concepts to establish materials, hierarchy, spacing and composition. Their painted labels and backgrounds are visual references, not runtime controls or automatically separable production assets. Account/onboarding, recovery, consent and error flows still require their own functional states.

The proposed visual language is crisp retro pixel clusters, dark stone panels, warm timber, aged brass, ivory text, forest-teal primary actions and restrained oxblood/turquoise accents. The user's “18 pixel” phrase is not a fixed 18-pixel screen or 18-bit specification. Final source pixel density and typography must be decided from a readable phone-sized sample. Keep fantasy styling around generous content space; avoid tiny ornament competing with exercise instructions.

## Use native Unity components with sprite artwork

Keep Unity 6000.3.24f1/uGUI 2.0.0. Existing `SystemUI`, `SystemTheme`, text fitting and state controllers provide reusable behavior. The new art style needs a new presentation layer and landscape layout, not a CSS/webview conversion or a new application engine.

A pixel button should be **a real Unity Button with reusable artwork**: an Image containing an authored border/fill sprite, a separate localized label and optional icon, and normal native input/focus behavior. Use nine-sliced borders where stretching preserves the design, or tiled centers/edges where a pixel texture should repeat. Do not bake button labels or form contents into PNGs. Unity supports sliced, tiled and filled UI Images alongside live Text components. [Unity visual components](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UIVisualComponents.html).

Provide normal, pressed, disabled and visible focus/highlight treatment as a coherent control family. Use sprite swaps for changed pixel edges and a tint only where it preserves the intended palette and contrast. Keep loading state and duplicate-submission prevention in control logic. Native Selectable transitions already support sprite swapping and color changes. [Unity transition options](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-SelectableTransition.html).

Use point filtering for authored pixel sprites, with explicit import settings and consistent source scale; it samples the nearest pixel rather than blending neighboring pixels. Point filtering alone does not correct uneven scaling or convert a large generated image into clean native-resolution pixel art. [Unity FilterMode.Point](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FilterMode.Point.html). Sprite atlases, padding, compression and texture sizes must be verified on the target device rather than assumed from concept renders.

Keep the following shared contracts:

- **Theme tokens:** palette roles, pixel border widths, spacing, typography, interaction states and component size variants. One changed token must not require regenerated screen images.
- **Typography:** live localized labels, numbers and dates; a licensed readable font covering Spanish accents, inverted punctuation and English. Use a pixel display face only where legibility survives. Do not stretch glyphs or rely on tiny auto-fit text.
- **Layout:** landscape safe-area anchors, comfortable touch targets, predictable wrapping and scrolling. Reuse the same components for wide phones and tablets. Select a logical art grid after review; favor integer sprite scaling and deliberate letterboxing/reflow over uncontrolled fractional stretching.
- **Art metadata:** stable asset ID, source/derivative relationship, native size, pivot, slicing borders, intended scale, padding and state names. Pack compatible sprites into atlases only after individual assets are verified.
- **Data binding:** labels, selected state, progress, validation and visibility come from state/controllers. No concept image supplies real account, fitness or purchase state.
- **Accessibility:** visible focus, clear selected/disabled distinctions beyond color alone, readable exercise text, and labels for meaningful icons. Styling must not hide End, Back or recovery actions.

## Room and seasonal composition

Home is a personal space containing the selected character. Build it as a stable room shell with independent items, not a single flattened background. The shell may contain fixed wall/floor architecture; adjustable props and seasonal decoration are separate sprites and data.

| Layer | Independent content |
| --- | --- |
| Room shell | Floor, walls, window/arch and stable architectural trim |
| Training props | Ring, bench, punching bag and weight rack as separate objects |
| Home props | Chest, rug, lights, shelves and collectible decorations |
| Character | Selected full-body appearance with stable feet anchor and independent shadow if needed |
| Seasonal treatment | Snow caps, garland, lights and themed overlays or paired variants of the same object |
| Interface | HUD, navigation and dialogs, separate from all scene artwork |

Each placeable item should declare an ID, placement anchor, footprint, draw-order rule, supported location and variant ID. Its winter treatment must retain the same object identity, scale, pivot and intended footprint: add snow to the existing ring, rather than generate an unrelated snow-themed ring in a newly composed room. A seasonal overlay must respect rope/post geometry and the item's silhouette. Inventory and placement save stable IDs/positions; decorations are not hardcoded into Home screenshots.

**Implementation recommendation:** begin with a small set of placement slots and a few props, then expand after the first room works. Free placement, collision rules, large inventories and theme catalogs are later scope decisions. Each new object or its seasonal variation remains independently reviewable.

## Character source contract

The [static character viewport](pixel-character-viewport.md) imports the eight exact user-created sources under `assets/sprites/autosprite/barbarian-user-r1/` and preserves their original manifest and hashes. Its separate `barbarian-viewport-r1` derivatives use AutoSprite background removal only. The component fixture demonstrates transparency, shared framing and stable feet placement; final art/screen approval remains separate.

The first character viewport supports full static appearances and fixed source colors. It does not include hair/skin/eye controls, garment fitting, breathing, rigging or per-item animation. Maintain clear differences in body and facial fullness, and preserve the user's authored clothing. Do not make every build occupy the same width through stretching. Future palette regions and selected-character exercise animations have separate authoring/review scope. Read a provider's atlas JSON before implementing any future sprite animation.

## One component per PR/MR

The **primary pixel button** was merged in PR #12. The [framed content panel](pixel-content-panel.md) was merged in PR #13. The [labeled form field](pixel-form-field.md) was merged in PR #14. The [secondary and text actions](pixel-secondary-action.md) were merged in PR #15. The [icon buttons](pixel-icon-button.md) were merged in PR #16. The [single-choice control](pixel-choice-control.md) was merged in PR #17, including the requirement that difficulty remains editable during a boss routine. The [static character viewport](pixel-character-viewport.md) was merged in PR #18 with all eight user-created Barbarians. The [navigation tabs](pixel-navigation-tabs.md) were merged in PR #19, with caller-confirmed current state and optional adult-only fasting visibility. Further components remain reviewable individually, but their visual fixture now belongs inside the approved Home composition.

Each component PR includes:

1. Its isolated artwork and provenance, reusable component/prefab, required state behavior and narrowly necessary support code.
2. A small integration fixture showing it at its intended size and alongside an already accepted component where available.
3. Relevant visual evidence at a baseline landscape phone size plus a materially different aspect ratio; include safe areas and long localized text where applicable.
4. Focused interaction checks for its real risks: sizing, navigation/focus, state transitions, editing, cancellation or duplicate submission. Do not write tests that merely mirror constants.
5. A concise PR description stating what changed, why, validation, and any remaining visible mismatch. Keep unrelated window rewrites and asset cleanup out of the same PR.

Approval of a reference does not certify an implementation. Review the component against the accepted style in context; avoid repeatedly regenerating an entire screen to adjust one control. Once a component is accepted, reuse it across screens and fix shared behavior in one place.

## Ordered component backlog

Each numbered row is one component-sized candidate PR. Large domain features such as authentication, routine storage or entitlement services remain separate tasks, not hidden inside a visual component PR.

| Order | Component | Acceptance focus / first consumer |
| --- | --- | --- |
| 01 | Primary action button | Reusable pixel border, live label, states and minimal landscape fixture; login |
| 02 | Framed content panel | Repeated material, nine-slice/tile behavior, narrow/wide content without distorted corners |
| 03 | Labeled form field | Email/password types, keyboard, focus, validation and show/hide support; account screens |
| 04 | Secondary/text action | Clear hierarchy, back/recovery action and disabled/focus state; account screens |
| 05 | Icon button | Independent icon, accessible label, adequate touch target; Back/settings |
| 06 | Choice control | Selected state for body/presentation/difficulty, keyboard/touch support and wrapping; difficulty remains changeable during the boss-battle routine |
| 07 | Static character viewport | Exact user sources, transparent derivative when ready, feet anchor and consistent scale |
| 08 | Navigation tab | Selected/current state, label/icon reuse and optional adult-only fasting visibility |
| 09a | Room shell / exterior | Independent artwork, uniform landscape composition and exact character in context; see [Home room](pixel-home-room.md) |
| 09b | [Room object / training ring](pixel-room-object.md) | Stable identity, supported slot, pivot/footprint, draw order and JSON state; first independent Home prop |
| 10 | Routine summary row | Current/completed/missed/rest state, clear status and edit entry without punitive wording |
| 11 | Week selector | Dates, selected day and routine status; workout area |
| 12 | Exercise prescription card | Sets/reps/time, per-side semantics, equipment and substitution action |
| 13 | Numeric workout input | Reps/load/time units, correction and validation; manual logging |
| 14 | Progress meter | Reusable frame/fill/label with capped value; boss work progress |
| 15 | Timer readout/control | Reliable time display, pause/end behavior appropriate to its domain; rest first |
| 16 | Confirmation dialog | Clear action/cancel, unsaved changes and focus handling; routine edit/exit |
| 17 | History entry row | Dates, neutral summaries and correction action; workouts, then adapted fasting history |
| 18 | Empty/error/loading notice | Honest unavailable/offline states and retry, shared across completed screens |
| 19 onward | Individual room props and seasonal overlays | One item/variant per PR using the approved room-object contract |

Integrate accepted components into the seven reference screens in small vertical steps. Preserve existing auth and training gates during migration. A workout timer and a fasting timer can share presentation without sharing eligibility, rewards or domain rules. Under-18 navigation must omit the fasting destination rather than merely gray out an entry.

The current application explicitly forces portrait in several screens and uses 853×1844 layouts. Move one screen at a time onto the accepted landscape foundation; do not claim landscape support merely by rotating the game window. Public release still requires live data/services, professional content review and device testing beyond the component gallery.
