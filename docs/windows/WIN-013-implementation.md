# WIN-013 — Character customization implementation

The new opt-in **illustrated 2.5D playable proof** is documented in the
[proof package](../../design/character-2-5d/playable-proof-r1/README.md).
It uses the normal man/woman, authored studio/gameplay projections and a separate
registration contract. Launch with `-sologym-avatar-renderer illustrated`.
It is a visual and motion review preview, with no recipe persistence or onboarding
completion. The sections below describe the earlier registered-piece renderer,
which remains the default path while the illustrated proof is reviewed.

Status: **Registered studio pass built and verified in the Linux review player; visual acceptance pending**. Date: 2026-09-26. This is an engineering revision of the existing proof character. It does not record visual approval or replace the approved/reference golden.

The current pass corrects the character registration and gives the avatar a clear full-body stage above a compact, native Unity customization dock. It retains the illustrated portal, cyan frames, serif headings, and game presentation used by the preceding windows.

Historical design and implementation references:

- [G0 flow](WIN-013-character-customization-g0.md)
- [G1 workshop](WIN-013-character-customization-g1.md) — historical scroll workshop
- [R1 reference package](WIN-013-character-reference-r1.md) — initial reference/golden procedure
- [G3 studio](WIN-013-character-studio-g3.md) — initial full-screen studio

The current registration described below supersedes older notes that socket and piece placement still live in C# or that migration to `rig_layout` remains a follow-up.

## Current registration

[`ReferenceManifest.json`](../../app/Assets/SoloGym/Resources/AvatarReference/proof_male_athletic/ReferenceManifest.json), schema `1.1.0`, is the authoritative placement source for the `proof_male_athletic` fit and `front_proof` view. [`CharacterReference.cs`](../../app/Assets/SoloGym/Scripts/Avatar/CharacterReference.cs) loads it, and [`AvatarRigLayout.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarRigLayout.cs) builds the skeleton and attachments from it.

- `rig_layout.joints` defines the parented head, shoulder, elbow, wrist, hip, and knee sockets in top-left pixel coordinates.
- `rig_layout.pieces` defines each part's socket, local offset, and one uniform source scale. Width and height come from the atlas cutout multiplied by that same scale, preserving the source proportions.
- The piece array is also the explicit back-to-front draw order. All rendered pieces are flat siblings under the render rig, while their positions and rotations follow the appropriate skeletal sockets. This separates animation hierarchy from draw order, so a child forearm or glove cannot unexpectedly render through another layer merely because it belongs to a nested joint.
- `rig_layout.hair_variants` gives spiky and swept hair their own offsets and dimensions. Changing hairstyle applies the matching registration as well as the atlas rectangle.
- Atlas registration removes the torso’s duplicate hollow neck cap and the open forearm/calf caps. The head owns the neck, with the torso layered over its lower cutout; a shorts-fabric pelvis underlay closes the gap between the independent thighs.
- Shirt and glove placement use the same registered sockets and scaling contract. Equipped gloves replace the visible bare hands; attachment checks compare each rendered piece with its socket transform after animation.

[`LayeredAvatar.cs`](../../app/Assets/SoloGym/Scripts/Avatar/LayeredAvatar.cs) keeps the existing `AvatarAppearance` recipe and updates one compositor in place. A frozen idle pose remains stable for review captures. Idle, walk, and jab use the same attachments; this is still a cutout animation proof.

[`AvatarSkin.shader`](../../app/Assets/SoloGym/Resources/AvatarProof/AvatarSkin.shader) retains masked skin tinting and now supports Unity UI stencil, rectangle clipping, and alpha clipping. The enlarged face view must respect the same stage clip as the un-tinted equipment pieces.

## Studio layout and interaction

[`CharacterStudioScreen.cs`](../../app/Assets/SoloGym/Scripts/CharacterStudioScreen.cs) uses the existing 853 × 1844 logical page. The title sits above the hero, the stage is `(94, 400, 665, 740)`, the category dock starts at `y = 1188`, and the compact option panel occupies `y = 1254..1496`, above Continue.

[`AvatarViewHost.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarViewHost.cs) fills that stage with a `RectMask2D` and uses the manifest's stage pivot and full-body/face presets. The manifest currently places the rig pivot at `(0.5, 0.26)` within the stage. Camera changes and recipe changes reuse the existing avatar.

- The dock offers Skin, Face, Body, Gear, and **Mirror / Reflejar**. Mirroring describes the actual operation; it does not claim free 3D rotation.
- Skin uses eight native swatches and a localized selected-tone label.
- Face separates Hair, Eyes, and Mouth. Hair/face choices and equipment use actual atlas cutout thumbnails with preserved proportions, replacing anonymous colored placeholders.
- Gear uses compact slot tabs and explicit equipment/none choices. Unavailable body fits remain visibly locked.
- Manifest hit regions are created from broadest to most specific so the head target wins over the enclosing skin target. The full-body hit map is disabled while the face camera is active; the dock remains available to return to another category.
- Changing language refreshes the title, navigation, dock, selected labels, review message, Continue, and secondary action. Changing categories clears obsolete option controls before creating the current set.

## Product flow

Schedule review checkpoint **`REVIEW:WIN-013`** opens the studio through [`OnboardingScreen.cs`](../../app/Assets/SoloGym/Scripts/OnboardingScreen.cs). Continue fires **`REVIEW:SETUP_COMPLETE`**, followed by the setup-complete notice and the review exit to Welcome. Linux can enter the same screen through `-sologym-window character` in [`WelcomeScreen.cs`](../../app/Assets/SoloGym/Scripts/WelcomeScreen.cs).

Catalog and recipe validation remain in [`Catalog.json`](../../app/Assets/SoloGym/Resources/AvatarCustomization/Catalog.json), [`AvatarCustomizationCatalog.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarCustomizationCatalog.cs), and [`CharacterCustomizationState.cs`](../../app/Assets/SoloGym/Scripts/CharacterCustomizationState.cs). The legacy [`CharacterCustomizationScreen.cs`](../../app/Assets/SoloGym/Scripts/CharacterCustomizationScreen.cs) remains for historical G1 artifacts and is not the onboarding studio.

## Native verification

The Linux player was rebuilt with Unity 6000.3.24f1. Whole-screen ES, EN alternate, face-detail, and jab captures were checked at 853 × 1844 on a virtual graphics display. All 32 named studio smoke checks passed, including the five controller state checks; the jab attachment/unsupported-fit proof passed. The verification runner also observed the player staying alive for three seconds after an interactive capture and checked that automated captures exit successfully. These checks do not imply human visual acceptance or mobile-device verification.

Run visual captures with a graphics session, without `-batchmode -nographics`, which can produce empty or black Linux screenshots. For the character studio, `-sologym-capture` saves the screenshot and **keeps the review window open**. Add `-sologym-quit-after-capture` for a capture-only automated exit. `-sologym-smoke` performs the checks and exits once, with status `0` on success or `2` on failure.

Review-only entry controls include `-sologym-character-view face|gear|body` and `-sologym-avatar-variant alternate`. The alternate recipe uses deep skin, swept hair, and no torso or hand equipment.

```bash
# Interactive Spanish studio: capture, then keep the window open for review.
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-es-registration-v5.png"

# Automated studio verification: capture plus interaction/state smoke, then exit.
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-es-registration-v5.png" -sologym-smoke

# English alternate appearance, then exit after the screenshot.
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale en -sologym-avatar-variant alternate \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-en-alternate-v5.png" -sologym-quit-after-capture

# Face-detail framing and clipping, then exit after the screenshot.
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es -sologym-character-view face \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-es-face-v5.png" -sologym-quit-after-capture

# Isolated jab attachment proof. This engineering proof screen exits after capture.
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window avatar -sologym-avatar-action jab \
  -sologym-capture "$PWD/artifacts/visual/WIN-013/avatar-jab-v5.png"

# Reference comparison against the existing golden; no golden update is implied.
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-avatar-reference-check
```

| Current-pass evidence slot | Path | Status / purpose |
| --- | --- | --- |
| Spanish studio | `artifacts/visual/WIN-013/studio-es-registration-v5.png` | Captured and inspected; full body above controls |
| English alternate appearance | `artifacts/visual/WIN-013/studio-en-alternate-v5.png` | Captured and inspected; deep skin, swept hair, no equipment, EN controls |
| Spanish face detail | `artifacts/visual/WIN-013/studio-es-face-v5.png` | Captured and inspected; face registration, thumbnails, and consistent stage clipping |
| Jab pose | `artifacts/visual/WIN-013/avatar-jab-v5.png` | Captured and inspected; registered attachments, cutout-art limitations remain |
| Studio interaction smoke | `artifacts/visual/WIN-013/studio-es-registration-v5.smoke.json` | Passed: 32 named checks, no failures |
| Jab attachment proof | `artifacts/visual/WIN-013/avatar-jab-v5.proof.json` | Passed: attachment and unsupported-fit validation |

The repeatable runner is `tools/verify_character_review.py`; its process-lifecycle results are retained in `artifacts/visual/WIN-013/registration-v5-verification.json`. Run at the full logical resolution without desktop window clamping with:

```bash
xvfb-run -a -s '-screen 0 1200x2000x24' python3 tools/verify_character_review.py
```

Historical evidence remains useful for comparison and does not certify the current pass:

| Historical evidence | Path | Context |
| --- | --- | --- |
| Studio ES (G3) | `artifacts/visual/WIN-013/studio-es-g3.png` | Initial studio baseline |
| Prior rig pass | `artifacts/visual/WIN-013/studio-es-rig-fix3.png` | Previous C# registration |
| Avatar proof | `artifacts/visual/WIN-013/avatar-rig-fix2.png` | Earlier engineering proof |
| Prior studio smoke | `artifacts/visual/WIN-013/studio-es-g3.smoke.json` | Historical `passed: true`, not current validation |
| Reference runtime / result | `artifacts/visual/AvatarReference/proof_male_athletic_runtime.png`, `reference-check.json` | May be overwritten by a new reference run; compare its date and result with the current build |

The existing `golden_capture.expected_sha256` has not been promoted to a new value for this revision. Reference comparison may report a mismatch after intentional registration changes; any new golden still requires the project's visual acceptance procedure. No user approval is claimed here.

## Scope and next steps

This pass supports **one fit and one front/three-quarter source-art view**, plus horizontal mirroring. It does not create a 3D character, a free rotation camera, additional body fits, or coordinated directional animation artwork. The existing source atlas includes perspective cues that registration can align but cannot turn into a different camera view.

The same recipe IDs and registration structure can be reused when the character enters gameplay. The source still has visible cutout outlines at limb joins, rectangular skin masks, a fixed torso/arm occlusion order during motion, and mixed perspective cues in the clothing. These are production-art limitations, not a completed animation library. A later angled, Mobile Legends-style gameplay camera needs either coordinated directional atlases for the supported views and actions or an actual 3D rig with matching equipment. That production art and gameplay camera work has not been completed in this pass.

Next steps are visual review of the registered character, then consideration of a new golden. Production fits/art, persistence, and the Home hero integration remain separate work.
