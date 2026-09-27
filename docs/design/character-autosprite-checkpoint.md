# Frontal AutoSprite checkpoint

Historical idle checkpoint. Superseded for the MVP by the [static modular integration](character-modular-mvp.md), with four builds per gender and no animation.

Character configuration and Home can now display the same complete, front-facing
AutoSprite character with an authored idle loop. This review uses the user's
existing female character and training outfit. It replaces procedural limb
deformation within the AutoSprite preview with provider-authored sprite frames.

## Review scope

- One front-facing character, 25 complete frames over the atlas's 2.333-second
  clip. Frame rectangles, timing weights and transparency come from the export.
- Configuration → Home → configuration preserves idle play/pause. Returning
  without changing language preserves Home's automatic-language preference.
- English and Spanish use the existing game UI and full-body framing. Capturing
  a screenshot leaves the preview open unless an explicit quit flag is given.
- The runtime catalog packages only the frontal sheet. The isometric export is
  retained outside runtime resources for later review.
- Hair, skin, body variants, separate equipment swaps, profile persistence and
  gameplay actions are not implemented by this flattened idle sheet.

The next sprite plan is **five body types per gender**: slim, medium, overweight,
obese and muscular. All fourteen approved illustrated masters remain intact.
The additional skinny-muscular and fat-muscular appearances remain supplementary
references. See the [production plan](character-sprite-v1-plan.md) and its exact
source/hash mapping. This plan does not claim that ten new sprites exist.

## Open the current preview

From the repository root:

```bash
app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer autosprite
```

Pause/play idle and choose **Continuar al inicio**. Tap the character on Home to
return. Use `-sologym-window home` to open Home directly. See the
[export contract](../../assets/sprites/autosprite/female-studio-home-r1/README.md)
for atlas details, generation provenance, capture flags and verification commands.

The explicit renderer flag keeps this appearance review separate from saved
onboarding data. The existing schedule → character route still uses the earlier
studio; its renderer dependencies remain until that route is migrated together
with a working customization/save contract.

## Cleanup decisions

- Removed the unused G1 `CharacterCustomizationScreen` and its Unity metadata.
  No route or scene referenced it; the older live studio is a different class.
- Removed the unreferenced `AvatarProof/BodyMaster-v2.png` runtime experiment and
  metadata. Both removed experiments remain recoverable from checkpoint commit
  `19fea4d362818b9df5aa234c0e725d33b5e294c7`.
- Excluded abandoned, untracked PixelLab and split-layer trials from this PR.
  Their 44 original files were archived locally with verified SHA-256 hashes at
  `artifacts/local/character-cleanup-2026-09-26/`. The active pipeline is AutoSprite.
- Deleted approximately 1.15 GB of reproducible, ignored illustrated-motion
  capture frames. The compact prior review, its 24 videos, feedback and provenance
  remain available in `design/character-2-5d/playable-proof-r1/review/`.
- Retained two current full-screen screenshots and the navigation report as PR
  evidence. Routine regression captures go to ignored `artifacts/local/`.
- Preserved all approved masters, earlier revisions, style/identity anchors,
  review records and hashes. Retained live older renderer dependencies to avoid
  breaking existing onboarding; their prior motion failures are still recorded.

No new art generations were needed for this cleanup. The original AutoSprite
trial cost 15 credits, including its corrected frontal render; its saved responses
record that expenditure.

## Evidence and checks

- [Frontal configuration](../../artifacts/visual/AutoSprite/studio-home-r1/studio-front.png)
- [Frontal Home](../../artifacts/visual/AutoSprite/studio-home-r1/home-front.png)
- [Navigation and playback checks](../../artifacts/visual/AutoSprite/studio-home-r1/navigation.smoke.json)
- [Atlas integrity and preserved export hashes](../../assets/sprites/autosprite/female-studio-home-r1/validation.json)

Build with Unity 6000.3.24f1, run `python3 tools/verify_autosprite_assets.py` and
`python3 tools/verify_character_art_batch.py`, then run the navigation and review
commands in the export contract. The latter checks that interactive captures stay
open and that the existing default Studio/Home controls still pass their smoke
checks. Structural success does not extend the original art approvals to new
sprite variants or equipment.
