# AutoSprite · character configuration and Home trial

This first version uses the **front view only** for character configuration and Home, with whole-character idle playback of the user's existing AutoSprite **female body** character, ID `cmuj70eg8000910qlnap76av1`. Isometric presentation is deferred to a later version. This is a review candidate, not an approval of new body presets or equipment. The 14 approved illustrated masters are unchanged.

## Selected exports

| View | Status | PNG and provider JSON atlas | AutoSprite sheet |
| --- | --- | --- | --- |
| Front | Used in configuration and Home | `front-r2-idle.*` | `cmuj8ajhz00016bkxkui8unv7` |
| Isometric southeast | Retained source only; deferred | `iso-idle.*` | `cmuj86n78000kptivmo2b6t17` |

`female-front-source.png` is the user's existing original portrait. `front-idle.*` is an **unused, rejected side view**: the first generation turned sideways despite the front-facing prompt. R2 anchors both ends to original pose `cmuj70eh8000b10qltzxfcom3` with a custom animation and preserves the front view. Keep the rejected and deferred isometric exports as generation history; neither is imported into Unity for this version.

The three renders cost **15 credits** (10 for the initial pair, 5 for the correction). The last generation response reported 1,485 credits remaining. No sounds, run, attack, new characters, or 3D assets were generated. Request and response JSON files preserve prompts, job IDs, and costs. Export URL signatures and credentials are excluded.

## Atlas contract

The original exports remain byte-for-byte intact. Both atlases contain 25 numeric-keyed frame rectangles, 768 × 768 each, in 3840 × 3840 PNGs. PNGs use palette transparency; the green RGB value behind zero-alpha pixels is not a visible background.

Read the JSON atlas before changing playback. Numeric frame keys must sort numerically. `meta.duration_s` is 2.333 seconds for each complete clip; individual `duration: 1` values are relative weights, **not one second per frame**. Texture UVs convert the provider's top-left origin to Unity's bottom-left origin. No grid coordinates, limb joints, or procedural spring motion are invented.

`tools/verify_autosprite_assets.py --sync` validates the active and deferred atlases and transparent content, copies the unmodified front PNG into `app/Assets/SoloGym/Resources/AvatarAutoSprite/female-studio-home-r1/`, and derives a front-only `catalog.json`. The sync step removes an old isometric Unity copy only after confirming it is byte-identical to the retained provider export. Normal verification checks that runtime metadata and PNG hashes still match the front export and that the deferred isometric texture is absent from runtime resources. `validation.json` records active and deferred views separately. One union of all frame alpha bounds fixes scale and placement for the entire loop; frames are never individually cropped or recentered. Measured bottom-edge variation is 1 source pixel for front and 0 for the retained isometric export.

Unity uses bilinear filtering, full texture dimensions, no mipmaps, and uncompressed RGBA for this review. Production mobile texture memory/quality needs a separate import pass before shipping.

## Open the local game

Run from the repository root after building the Linux player:

```bash
app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer autosprite
```

The character uses **Frontal** throughout this version. Pause/play idle, then select **Continuar al inicio**. Tapping the character on Home reopens the configuration preview. The playback choice survives navigation within this session. Appearance choices are not written to the player profile. The existing Home screen still uses fictional review data and its existing settings behavior.

To start on Home, replace `-sologym-window character` with `-sologym-window home`. Add `-sologym-autosprite-still` to pause. The former `-sologym-autosprite-view isometric` argument no longer selects an isometric view; both screens remain frontal. Without `-sologym-avatar-renderer autosprite`, existing screens and renderers retain their behavior.

`-sologym-capture /absolute/output.png` saves a screenshot and keeps either AutoSprite screen open. Add `-sologym-quit-after-capture` for automated captures.

## Verification

```bash
python3 tools/verify_autosprite_assets.py
xvfb-run -a -s '-screen 0 1200x2000x24' \
  app/Builds/Linux/SoloGym.x86_64 \
  -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window character -sologym-locale es \
  -sologym-avatar-renderer autosprite -sologym-autosprite-smoke \
  -sologym-capture "$PWD/artifacts/visual/AutoSprite/studio-home-r1/review.png" \
  -logFile /tmp/sologym-autosprite-smoke.log
xvfb-run -a -s '-screen 0 1200x2000x24' python3 tools/verify_autosprite_review.py
```

The navigation smoke checks real buttons, the front view on Home, return navigation, frame advance, pause, and preservation of automatic Home language choice. It produces two full-screen captures and `navigation.smoke.json` under `artifacts/visual/AutoSprite/studio-home-r1/`. The Python review runner checks English interactive captures stay open and runs the existing default Studio/Home smoke checks; these routine captures and its report go to ignored `artifacts/local/autosprite-review/`. Visual approval of the generated appearance and motion remains with the user.

## Next asset boundary

This is one complete outfit on one character, using the front idle view. Hair, skin, body variants, and swappable fitted equipment are not implemented by this flattened sheet. The [confirmed next plan](../../../../docs/design/character-sprite-v1-plan.md) uses slim, medium, overweight, obese and muscular for each gender, preserving all 14 original illustrated references. Test one clothing change against this exact character and front pose before expanding the catalog. Add isometric presentation in a later stage using the retained export as a candidate. Use AutoSprite MCP for new sprite/animation assets, preserve IDs and prompts, inspect the returned atlas, and retain every previously approved export unchanged.
