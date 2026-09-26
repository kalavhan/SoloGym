# Layered UI migration and avatar engineering proof

2026-09-25 · Unity 6000.3.24f1 · Linux review player 0.5.0. Implements UI-01 through UI-04 of the [architecture plan](layered-ui-and-avatar-architecture.md). The existing window state controllers and Firebase adapters are retained. WIN-010 remains the next new window, subject to its recorded target approval; its implementation is not included here.

## What is now editable

Welcome (WIN-002), age/region (WIN-006), privacy/consent (WIN-007), private profile (WIN-009), and Home (WIN-001) now assemble independent artwork with reusable Unity components. No migrated screen loads the old full-screen source/clean plates. Those files moved unchanged to [legacy-runtime-plates](../../design/legacy-runtime-plates), outside Unity Resources. Original approved targets remain untouched in `design/renders`.

- `SystemPanel` draws angular panels, corners, borders, glow, selectors, input frames and progress tracks/fills as mesh geometry.
- `SystemUI` creates shared labels, captions, buttons, artwork and icons. Existing input and state bindings continue to use native Unity controls.
- `SystemTheme` stores colors, fonts, spacing, border width, corner size and glow. The editable asset is `app/Assets/SoloGym/Resources/UI/DefaultTheme.asset`; the build creates it only when missing and preserves subsequent edits.
- `SystemTextFit` uses native font sizes and wrapping without deforming glyph vertices. The old `ReferenceTextLayout` has been removed.
- `SystemViewport` centralizes safe-area fitting and keyboard lift. The current phone adaptation scales the complete baseline composition; it is not a fully reflowing accessibility layout.
- Backgrounds, logo, portrait and an illustrated icon atlas are separate assets. Both languages use the same artwork. Small utility symbols use vector geometry. No labels or control frames are baked into the new art.

The [component gallery](../../artifacts/visual/UI-migration/final/components-default.png) is an interactive engineering view: change language, theme and panel width; edit a name; advance the live progress bar. Its [alternate capture](../../artifacts/visual/UI-migration/final/components-alternate.png) reuses the same textures. This is not a new customer-facing feature or theme-shop implementation.

## Window evidence

| Window | English | Spanish |
| --- | --- | --- |
| Welcome | [Capture](../../artifacts/visual/UI-migration/final/welcome-en.png) | [Capture](../../artifacts/visual/UI-migration/final/welcome-es.png) |
| Age / region | [Capture](../../artifacts/visual/UI-migration/final/age-en.png) | [Capture](../../artifacts/visual/UI-migration/final/age-es.png) |
| Privacy / consent | [Capture](../../artifacts/visual/UI-migration/final/consent-en.png) | [Capture](../../artifacts/visual/UI-migration/final/consent-es.png) |
| Private profile | [Capture](../../artifacts/visual/UI-migration/final/profile-en.png) | [Capture](../../artifacts/visual/UI-migration/final/profile-es.png) |
| Home | [Capture](../../artifacts/visual/UI-migration/final/home-en.png) | [Capture](../../artifacts/visual/UI-migration/final/home-es.png) |

Supporting captures cover email entry, imperial measurements, readiness, and a 390 × 844 profile viewport. Native labels, procedural frames, extracted icon detail and reconstructed background areas differ visibly from the earlier flattened targets. The [comparison artifacts](../../artifacts/visual/UI-migration/comparisons) preserve those differences; this work does not claim pixel identity or user acceptance of the new native output.

## Avatar proof, separate from Home

`AvatarAppearance` records appearance IDs independently of fitness measurements. `LayeredAvatar` composes a registered parts atlas on a small joint hierarchy. The proof supports two skin palettes, two hairstyles, torso equipment, paired gloves, idle/walk/jab and two explicitly mirrored views. Equipment follows the wrist/torso attachments; unsupported appearance/fit IDs are rejected. Head, eyes/brows and mouth are separate rendering parts, although only one face set is available in this proof. The shader changes defined skin regions and preserves separate clothing and hair.

[Equipped proof](../../artifacts/visual/UI-migration/final/avatar-equipped.png) · [alternate skin/hair and unequipped proof](../../artifacts/visual/UI-migration/final/avatar-alternate.png) · [motion recording](../../artifacts/visual/UI-migration/final/avatar-jab.mp4).

The proof establishes editable composition and attachment behavior. It **does not pass production art acceptance**: joint silhouettes need cleanup, skin masks are coarse authored regions, hair/head and shoulder fitting need refinement, and the two views are mirrored rather than independently authored isometric directions. Female presentation, other body fits, eight directions, final animations, coverage masks and gear compatibility remain unproduced. Home uses a separate static cutout derived from its retained character reference; this proof does not replace it. Do not expand the avatar library until this source/rig approach is visually accepted or revised.

The built-in imagegen tool produced the independent artwork and proof atlases. [Asset manifest](../../design/reference-manifests/UI-migration-assets-v1.json) records source references, exact saved prompts, dimensions, hashes and pending visual status. Generated PNGs remain unchanged; atlas registration is metadata. No local model installation or training was performed.

## Verification and limits

The Linux build succeeded. Existing focused checks passed for Welcome, Home, onboarding and private profile. They cover email validation/password behavior, unavailable auth, consent choices, drafts across Back/reentry, optional measurements/unit conversion, readiness pauses, saved-session routing, teen supervision and offline notices. The underlying 11 onboarding and 12 private-profile state checks passed. Avatar checks confirm attachment ownership and rejection of an unsupported fit; they do not certify visual quality.

The full capture batch records 18 complete-screen states. Whole-screen inspection found and corrected text wrapping and the Spanish Home fallback for missing translated layout boxes. Native EN/ES captures and comparisons are retained, with no per-button render cycle. A final targeted check covers the corrected Home layout and engineering previews. The capture tool isolates local preferences and uses unavailable auth for captures; no identity-provider calls or remote profile writes occur.

No APK was generated. The previous Android build remains 0.2.1, SHA-256 `f56b0451b23c12cedff4948ea0e24b34de9624750731b23da646376774643e23`. These checks do not substitute for device keyboard, Android/iOS rendering or screen-reader verification. The previously documented production policy, final legal content and server persistence limitations are unchanged.

## Run and reproduce

Build Linux using the command in [app/README.md](../../app/README.md). Launch the normal player with `-sologym-review -sologym-window components` for the gallery or `-sologym-review -sologym-window avatar` for the character proof. Normal entry continues to Welcome.

```bash
python3 tools/capture_layered_ui.py
```

This script captures whole windows in a virtual display, runs the focused checks once per relevant window, and writes a JSON result summary. `--only home-es components-default components-alternate` limits a follow-up run to affected screens. Logs/local preferences and motion frames are local artifacts; the retained final PNGs, compact results, comparisons and video are review evidence.
