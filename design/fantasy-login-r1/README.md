# Login — native guild entrance

The user approved `login-concept.png` with **“go”** after merged PR #31. The
complete landscape entry now uses the independent PixelLab architecture and
live native uGUI controls. The main Welcome scene routes to this window by
default and with `-sologym-window login`. Legacy explicitly named review routes
remain available; no character art or animations were regenerated.

## Behavior and integration boundaries

- Email/password and Google entry reuse `WelcomeController` and
  `FirebaseWelcomeAuthService`. Capture, smoke and keyboard-probe modes never
  contact an identity provider. Ordinary launches use the existing configured
  service, which reports unavailable when its SDK/configuration/platform is
  missing. Google identity currently requires the configured Android bridge.
- Local input validation, password visibility, keyboard Tab/Shift+Tab, email
  Return-to-password and password Return-to-submit. Losing focus does not send
  credentials. Only the language preference is persisted by this window.
- Pending requests disable duplicate/provider submissions and keep Cancel
  available. Cancellation/backgrounding clears the password. A cancelled late
  result cannot replace a newer request. Google may temporarily background the
  app for its native provider flow. Offline, invalid, provider-error, unavailable
  and rate-limited states use the existing controller copy and retry rules.
- Successful identity reaches an explicit pending-onboarding notice. It never
  enters fictional Home or claims age, consent or a completed profile. The
  connected account/profile pipeline is still unfinished.
- Account creation and password recovery links show their actual unavailable
  status and provide a return action. They do not create an account or claim an
  email was sent. Their complete connected windows are following work.
- Privacy and Terms read the existing `OnboardingController` document source in
  read-only mode. The supplied catalog currently marks final documents as
  unavailable. Nothing fabricates policy content or records legal acceptance.
- Spanish/English, safe-area fitting and a scrollable form. A software keyboard
  shortens the viewport while keeping normal type scale. Longer errors expose a
  scrollbar instead of overlapping the next field. Native phone IME/provider
  integration still needs device verification; desktop geometry tests are not
  proof of mobile keyboard behavior.

## Separate art and controls

The 640×360 background in `assets/sprites/pixellab/guild-entrance-r1/` is copied
byte-for-byte into `Resources/Rooms/LoginR1/`, imported with point filtering and
no compression. The concept is not included in Unity Resources.

The panel uses the existing gold border and separate dark fill. Email/password
use `PixelFormField`, the primary action uses `PixelPrimaryButton`, and links use
`PixelSecondaryAction`. Labels remain localized native text. The Google mark is
the existing original `Welcome/GoogleG` export with a native light provider
surface and licensed Google Sans label. Its provider branding intentionally
remains recognizable. Existing Pixelify Sans supplies the other live labels.

Small shared-control additions allow callers to choose horizontal padding;
existing callers retain their original defaults. No replacement control art
was made.

## Build and review

From this worktree:

```sh
/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath "$PWD/app" -executeMethod SoloGym.Editor.PixelLoginBuild.BuildLinux \
  -logFile /tmp/sologym-login-build.log
app/Builds/Login/SoloGymLogin.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -sologym-locale es
```

The focused build starts through the real Welcome router. The main app builder
also now sets landscape defaults instead of the superseded portrait layout.
Normal entry contains no sample credentials. Use explicit capture/smoke mode
for visual evidence, with an isolated local preference directory:

```sh
XDG_CONFIG_HOME="$PWD/artifacts/local/login-es-prefs" xvfb-run -a \
  -s '-screen 0 1280x720x24' app/Builds/Login/SoloGymLogin.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-review -sologym-smoke -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/Login/window-es-1280.png" \
  -logFile /tmp/sologym-login-es.log
python3 tools/check_pixel_field_keyboard.py --login
```

Failed smoke/probe checks return a nonzero exit status. The OS keyboard tool
creates its own Xvfb display and isolated preferences and uses only fictional
input. It never injects input into the user's desktop. Add `-sologym-stay-open`
when manually reviewing a capture build. Evidence and source hashes are recorded
in `verification.json`; native captures live under `artifacts/visual/Login/`.

## Provenance

`production/` preserves the original PixelLab prompt, request, job result,
style-reference hash and before/after balances. The single background cost 40
PixelLab generations. `screen-prompt.txt` and `login-concept.png` preserve the
built-in imagegen concept (not CLI). Implementation used no new generations.
The original 1672×941 concept is a reference for the native landscape layout;
the actual player uses the 640×360 architectural source, not pixels cropped
from the flattened mockup. `manifest.json` records the original references.
