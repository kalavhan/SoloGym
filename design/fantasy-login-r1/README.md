# Login — visual target for review

Next complete window after merged PR #31. The user accepted the proposed login
workflow with “do it”: PixelLab background first, complete landscape concept,
visual review, then a complete Unity window PR. This pack is a proposal; it is
not an approved implementation or a functioning authentication screen.

The guild entrance uses the accepted Home palette and a frontal camera. Its
open doorway remains visible on the left; a single readable login panel occupies
the right. There is no new character generation or animation in this scope.

## Separate production pieces

| Piece | Source / intended implementation |
| --- | --- |
| Entrance architecture | New PixelLab 640×360 opaque background, preserved under `assets/sprites/pixellab/guild-entrance-r1/` |
| Login panel | Existing thin gold frame with separate dark fill; native uGUI layout |
| Email/password inputs | Existing `PixelFormField`, native focus, keyboard and validation |
| Primary action | Existing `PixelPrimaryButton`, independent live label and state |
| Password visibility/recovery/account links | Existing native secondary/text actions |
| Google entry | Existing `Welcome/GoogleG` source and provider-compatible live button, not a generated rune/logo |
| Brand, headings, labels, footer and locale | Live localized text; use established licensed fonts |

The reference contains no signed-in HUD, character portrait, app navigation,
stats or arbitrary guest entry. The background contains no controls or text.
The complete rendered concept is never the shipped background.

## Implementation after visual review

- Landscape native uGUI using the existing auth controller/service contracts.
  Preserve configured email/Google identity behavior and explicit unavailable
  states. A visual approval does not configure a provider or verify a backend.
- Email/password validation, show/hide, keyboard traversal/submission,
  submitting/cancel/retry behavior, duplicate-submit prevention, provider
  cancellation and generic credential/offline errors.
- Recovery and account-creation entry points with honest destination status.
  Complete new fantasy account/recovery/onboarding windows are following work;
  do not claim account creation, recovery or trusted profile storage is already
  connected just because their links appear in the render.
- Preserve age/consent/onboarding checkpoints. Signing in is not proof of a
  completed fitness profile or eligibility for any workout.
- Spanish/English, readable compact landscape layout, safe areas and keyboard
  behavior. Reuse current controls rather than generating replacement skins.
- Verify a real Unity capture against the approved target before opening one
  window PR. Do not upload credentials or contact identity services from smoke
  fixtures; never save passwords in local review files.

## Evidence

`production/background-prompt.txt` and `background-request.json` preserve the
PixelLab instruction and style-reference hash. Provider submission/result,
source hashes and quoted cost are recorded alongside the export. The accepted
native Home screenshot supplies UI style only. `screen-prompt.txt` is the exact
built-in imagegen prompt for the composed concept.

Status: `login-concept.png` is ready for visual review. No runtime/auth code has
changed. The original render is preserved at 1672×941; it is a visual reference
for the native 1280×720 landscape layout, not a pixel-perfect exported UI.

PixelLab quoted 40 generations for the single 640×360 background. The concept
was generated with built-in imagegen (not the CLI/API fallback). `manifest.json`
records the exact source and reference hashes.
