# Base garments — fit revision 3

Review: <http://127.0.0.1:8899/tools/review/garment-fit-r3.html>

Five new isolated garment PNGs revise the eight clothing fits rejected in r2.
The male slim and medium fits retain approval. Every body PNG, hairstyle PNG,
and hair placement matches the approved base/hair lock. The review page has
before/after, garment toggles, full-body previews and close-ups. The user subsequently accepted eight retained presets for temporary MVP use;
see `mvp-adoption.json`. Historical quality decisions remain intact. Obese is
excluded from selection and the eight retained presets are integrated into the app.

| New asset | Candidate fits |
| --- | --- |
| female-medium-top-r2 | Female slim, medium, muscular |
| female-fuller-top | Female overweight, obese |
| male-overweight-top | Male overweight, obese |
| male-muscular-top | Male muscular |
| male-heavy-shorts-r2 | Male overweight, obese |

Other garments are reused from the reviewed r2 set. All five exports are Base
static assets. No new characters, uploads of bodies, animation, body edits or
hair edits were made. The same kit palette and panel concept are retained;
fold and trim differences need the user's design judgment.

## Provenance and cost

Each item folder records the exact text request, sanitized response metadata,
source PNG, hashes, and (where accepted for fitting) provider library asset ID
and transparent export. PNG downloads are unchanged provider bytes. Local
composition uses source/destination rectangles and existing neck/hand masks.
It is not automatic attachment generation or an animation-ready modular rig.

The first female-medium draft included trousers; the first heavy-shorts draft
included an unrequested logo. Both were rejected before saving to the provider
library or background removal. Their source records remain for cost audit only.

Seven returned previews cost 7 credits; five background removals cost 5.
Confirmed total: **12 credits**. One additional preview timed out without a
result or recovery ID and may have cost 1 credit: **13 maximum**. The manifest
records that uncertainty separately; it does not claim the lost request was free.
There were no automatic paid retries. One explicit replacement preview succeeded.

## Checks

- `python3 tools/verify_static_body_bases.py`
- `python3 tools/verify_modular_character_assets.py`
- `python3 tools/verify_fitted_clothing_r3.py`

Visual QA checks all eight candidates at full-body scale, neckline/waist/shorts
close-ups, the exact r2 before view, and read-only retained approvals. The two
heavier male shorts were also checked without the top for base-clothing coverage.
Technical checks preserve source identity and review hashes; they do not grant
art approval. See `validation.json` for the recorded result.
