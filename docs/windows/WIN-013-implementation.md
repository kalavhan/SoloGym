# WIN-013 — Character customization implementation

Status: **G3 studio + R1 reference package + rig layout refactor (visual golden pending re-approval)**. Date: 2026-09-26.

- G0: [WIN-013-character-customization-g0.md](WIN-013-character-customization-g0.md)
- G1 workshop (historical): [WIN-013-character-customization-g1.md](WIN-013-character-customization-g1.md)
- R1 reference: [WIN-013-character-reference-r1.md](WIN-013-character-reference-r1.md)
- G3 studio: [WIN-013-character-studio-g3.md](WIN-013-character-studio-g3.md)

## What was achieved

### Product flow

- Schedule review checkpoint **`REVIEW:WIN-013`** opens **`CharacterStudioScreen`** from [`OnboardingScreen.cs`](../../app/Assets/SoloGym/Scripts/OnboardingScreen.cs) (not the legacy scroll workshop).
- Continue fires **`REVIEW:SETUP_COMPLETE`** → setup-complete notice → exit to Welcome (review path).
- Linux review entry: `-sologym-window character` on [`WelcomeScreen.cs`](../../app/Assets/SoloGym/Scripts/WelcomeScreen.cs).

### Character studio (G3)

- Full-screen CAS-style UI: hero stage, category dock (skin · face · body · gear · rotate), overlay carousel, manifest **tap hit regions**.
- Single compositor: [`AvatarViewHost`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarViewHost.cs) mounts one [`LayeredAvatar`](../../app/Assets/SoloGym/Scripts/Avatar/LayeredAvatar.cs); recipe updates in place (no full UI rebuild).
- Catalog-driven options: [`Catalog.json`](../../app/Assets/SoloGym/Resources/AvatarCustomization/Catalog.json) + [`AvatarCustomizationCatalog.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarCustomizationCatalog.cs) + [`CharacterCustomizationState.cs`](../../app/Assets/SoloGym/Scripts/CharacterCustomizationState.cs).
- Shared UI affordances: skin swatches and option thumbnails in [`SystemUI.cs`](../../app/Assets/SoloGym/Scripts/UI/SystemUI.cs); portal tokens in [`PortalWindowFrame.cs`](../../app/Assets/SoloGym/Scripts/UI/PortalWindowFrame.cs).

### Reference package (R1)

- [`ReferenceManifest.json`](../../app/Assets/SoloGym/Resources/AvatarReference/proof_male_athletic/ReferenceManifest.json): stage anchor, camera presets (`fullBody` / `faceCloseUp`), hit regions, golden recipe, `expected_sha256`.
- [`CharacterReference.cs`](../../app/Assets/SoloGym/Scripts/Avatar/CharacterReference.cs) loads manifest by `fit_family_id`.
- [`AvatarReferenceVerification.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarReferenceVerification.cs): Linux `-sologym-avatar-reference-check` (attachments + SHA vs golden).

### Proof rig (alignment pass)

- Rig registration moved out of `LayeredAvatar.Initialize` into [`AvatarRigLayout.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarRigLayout.cs): joint chain, per-part display rects (tuned to `AvatarProof/Parts.json` UVs), explicit **draw order** (far limb → torso/head → near limb → shirt → gloves).
- Equipment display scale: shirt aligned to torso width; gloves sized to hands (atlas slices are much larger than on-screen rects).
- Studio preview stability: when `freeze && idle`, no vertical bob and **zero** limb rotation (stable capture pose).
- **Not done:** per-part anchors in manifest / atlas metadata; some shoulder and leg gaps remain until art or `rig_layout` JSON tuning.

Legacy scroll workshop [`CharacterCustomizationScreen.cs`](../../app/Assets/SoloGym/Scripts/CharacterCustomizationScreen.cs) remains in tree for G1 artifacts; **not wired** from onboarding.

## Navigation

Schedule `REVIEW:WIN-013` → `OpenCharacter` (studio). Continue → `REVIEW:SETUP_COMPLETE`.

## Verification

Run captures **without** `-batchmode -nographics` (those flags produce empty/black screenshots on Linux).

```bash
# Studio + smoke
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-es-g3.png" -sologym-smoke

# Isolated avatar proof (rig tuning)
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window avatar \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/avatar-rig-fix2.png"

# Reference check (requires -sologym-window character)
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-avatar-reference-check
```

| Evidence | Path | Notes |
| --- | --- | --- |
| Studio ES (G3) | `artifacts/visual/WIN-013/studio-es-g3.png` | Design baseline capture |
| Studio after rig pass | `artifacts/visual/WIN-013/studio-es-rig-fix3.png` | Post–`AvatarRigLayout` |
| Avatar proof | `artifacts/visual/WIN-013/avatar-rig-fix2.png` | Engineering proof screen |
| Studio smoke | `artifacts/visual/WIN-013/studio-es-g3.smoke.json` | `passed: true` |
| Reference runtime | `artifacts/visual/AvatarReference/proof_male_athletic_runtime.png` | Golden recipe, no gear |
| Reference JSON | `artifacts/visual/AvatarReference/reference-check.json` | Attachments OK; **update `expected_sha256` in manifest after visual approval** of new compositor |

## Follow-ups

1. Human visual acceptance of compositor → set `golden_capture.expected_sha256` to runtime hash from `reference-check.json`.
2. Optional: move socket offsets and display rects from `AvatarRigLayout.cs` into `ReferenceManifest` (`rig_layout`).
3. Production art, female fit, persistence, and Home hero swap remain out of scope for this window.
