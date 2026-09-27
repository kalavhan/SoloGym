# Static modular character MVP

The user accepted the current r3 art for temporary MVP use on 2026-09-27 and
asked to resume work on the fitness app. Obese is removed from both genders;
active builds are **slim, medium, overweight and muscular**. Historical source
art and approvals are preserved. This is acceptance for use, not a claim that
the art issues are resolved.

Deferred issues: awkward sleeves on muscular male/female and most female cuts,
and inconsistent garment design across some male variants. No further generation,
animation, isometric work, rigging, or wardrobe expansion was performed.

## What runs now

- The default character screen and Home render the same static modular avatar.
- Eight body presets, two hairstyles plus no hair, and independent top/shorts
  toggles. Base underwear remains part of the approved body.
- Continue saves cosmetic choices in local PlayerPrefs and opens Home. Back
  discards the current draft. Home's character and equipment controls reopen
  customization. Skin tones, further gear and account synchronization are absent.
- Schedule's existing review onboarding flow opens this character screen and
  continues to Home. Existing production authentication and fitness-setup gates
  are unchanged. This work does not implement the workout logger or live plans.
- Retired saved obese IDs resolve to overweight; invalid selections fall back
  safely. Appearance is independent of private measurements and fitness stats.

The Unity package contains **17 unchanged PNGs** and a compact catalog with the
reviewed source/destination rectangles and triangulated neck/hand masks. All
bodies use a single display frame, so selecting a larger build does not zoom it.
The renderer composes body, shorts, top, original neck/hands, and hair separately.
There are no generated fully clothed characters, baked outfit combinations,
per-asset animations, or new generation costs in this integration.

`assets/sprites/autosprite/basic-clothing-fit-r3/mvp-adoption.json` records the
user's scope decision and known issues separately from the old quality reviews.
The r3 fitting data and approved body/hair lock remain byte-for-byte unchanged.
The browser gallery shows the eight retained MVP builds without another approval
gate; submitted review files and all historical preset sources remain intact.

## Open the app

From the repository root:

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es
```

For Home directly, replace `character` with `home`. No renderer flag is needed.
A screenshot request stays open unless `-sologym-quit-after-capture` or a smoke
flag is supplied. `-sologym-avatar-renderer legacy` keeps the old proof route
available; historical AutoSprite motion sources remain outside the active flow.

## Verification

The installed Unity 6000.3.24f1 editor builds the Linux player successfully.
The runtime smoke checks cover all eight body selections, all hair choices,
independent equipment visibility, local preference reload, save/cancel behavior,
Home navigation, and preservation of automatic language preference.

```bash
python3 tools/sync_modular_avatar.py
python3 tools/verify_static_body_bases.py
python3 tools/verify_modular_character_assets.py
python3 tools/verify_fitted_clothing_r3.py

xvfb-run -a app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-modular-smoke \
  -sologym-capture "$PWD/artifacts/visual/ModularMVP/es/studio.png"
```

Run the same smoke at 393×852 with locale `en` for the phone layout. The smoke
restores appearance/language preferences after testing. Captures and JSON check
reports are under `artifacts/visual/ModularMVP/`; Spanish Studio/Home and English
phone captures were visually inspected. This is Linux validation, not Android
or account-sync certification. `tools/sync_modular_avatar.py --sync` reproduces
the runtime copies without image editing or generation.

The next product work should return to the fitness flow: readiness, plan review,
and manual workout logging. Character polish is deferred until explicitly reopened.
