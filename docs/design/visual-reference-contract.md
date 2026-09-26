# Retained renders and 1:1 implementation matching

Every window proposal includes a rendered example of its principal state. Keep separate English and Spanish images with identical sample identity, appearance and data. More states are added as that window's review requires. This implements the user's request to keep the rendering inside the proposal and use it throughout production.

## What gets retained

Save the original-size PNG, exact generation/edit prompts, source-image references, fictional fixture revision and a manifest. The manifest records the window, gate, locale, scenario, dimensions, SHA-256 hashes, appearance definition, intended safe-area treatment, and approval status. A filename alone is insufficient when its contents can change.

Files presented for approval are versioned and must not be overwritten. A changed image receives a new version and is presented again. Approval records the exact filenames and hashes; silence or requesting a render does not approve the result. Earlier concepts and intermediate edits stay identified as historical inputs, not alternate build targets.

WIN-001's retained references are [English v2](../../design/renders/WIN-001-system-home-en-proposal-v2.png) and [Spanish v2](../../design/renders/WIN-001-system-home-es-proposal-v2.png), with [their manifest](../../design/reference-manifests/WIN-001-system-home-v2.json). The user accepted the installed Home appearance; retained hashes and measured implementation differences remain unchanged.

## What 1:1 requires

At the approved baseline canvas, locale and fixture, match the background composition, character identity and pose, gear fit and position, layer order, frame geometry, typography hierarchy, icon placement, colors, text and displayed values. A successful match is to the accepted picture, not merely to its general mood.

Generated raster art is a design target. It does not identify a licensed font, supply editable control layers, prove correct progress-bar mathematics or define valid touch regions. Use the current complete-window TDR workflow: compose independent artwork with reusable code containers, controls and editable text, record component/asset versions and selected fonts, then compare the complete result to the accepted proposal. A flattened screen or panel crop is not a production component. Follow the [layered UI architecture](../engineering/layered-ui-and-avatar-architecture.md) when migrating the existing image-dependent windows. Correct differences or present a revised reference for explicit approval. Do not silently replace the target with whatever the asset pipeline produced.

The same background, character, items and icons are used in both production locales. Text can wrap within the approved language-specific layout. A separate AI localization image is a visual proposal; it does not justify duplicated or subtly different runtime artwork for each language.

Runtime numbers and progress indicators must follow the approved data. If a generated fill is approximate, correct its exact geometry in the editable composite and include that correction in G5 approval. Do not preserve a math error solely to duplicate raster pixels.

## Verification before accepting implementation

1. Load the exact fixture, locale, theme, equipped appearance and UI state used by the accepted composite. Freeze the character/particles at the recorded comparison frame. Disable incidental clocks, random loot and variable animation during capture.
2. Capture the app content at the approved image's dimensions with the agreed safe-area treatment. Keep original image files intact; compare at their native dimensions without arbitrary stretching or cropping.
3. Produce a side-by-side comparison, a 50% opacity overlay and a difference image. Inspect both the entire screen and its text, panel corners, character/gear edges and control positions. Retain new evidence for each migration; earlier captures do not verify a later refactor.
4. Check the actual controls as well as pixels: labels, routes, touch areas, readable contrast and text scaling must satisfy the approved behavior. A static screenshot is not functional verification.
5. Correct visible mismatches. Record any unavoidable platform font rasterization differences explicitly; do not use a broad similarity percentage to hide moved elements or changed art. A design change requires user approval and a new immutable reference.

“1:1” applies to the agreed baseline state and canvas. Other aspect ratios, operating-system insets, large text, recovery, teen, loading and offline states require their own approved adaptations. Preserve hierarchy and interaction access; never stretch the reference image over the entire app as a substitute for building the window.

## How this fits the staged workflow

G0 includes the visual proposal and flow/data sheet. The user’s later TDR instruction supersedes the original per-button/UI-asset approval loop: approve the window target, assemble the full window from reusable components, then perform focused visual and interaction checks. Retained renders remain the reference, not runtime window textures. The latest layering request adds a shared UI migration before WIN-010. A new window or visible redesign still needs target approval. Modular character production keeps its separately scoped base → customization → gear → motion reviews.
