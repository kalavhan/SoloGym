# WIN-002 official Google resources

Retrieved 2026-09-25 for the native Welcome / Sign-in window.

The approved concept contains an AI-rendered Google mark. Runtime replaces that mark with Google's official current gradient G. This is an intentional provider-brand correction to the concept, while retaining the approved control position and overall composition.

## Google G

- Runtime file: `app/Assets/SoloGym/Resources/Welcome/GoogleG.png`
- Official source: [Google G PNG](https://developers.google.com/static/identity/images/g-logo.png), linked directly from [Sign in with Google Branding Guidelines](https://developers.google.com/identity/branding-guidelines).
- Original dimensions: 200 × 204, RGBA PNG. File copied unchanged; no recoloring, redrawing, cropping, rasterization, or image generation.
- SHA-256: `d1ce9c2af0b10a7333abc99bc706f9a6a199e5b65bf3e3009624f076b8638e6a`.
- Runtime usage: render with its 200:204 aspect ratio on the white provider button and pair with localized “Continue with Google” / “Continuar con Google”. The map places it at 58.823529 × 60 reference pixels.
- The [pre-approved asset bundle](https://developers.google.com/static/identity/images/signin-assets.zip) was also inspected. Its files are not redistributed here because the directly linked official G is sufficient.

The Google mark is a Google brand feature. Its inclusion is governed by the linked branding guidelines; no general open-source license or ownership of the mark is claimed.

## Google Sans Medium

- Runtime file: `app/Assets/SoloGym/Resources/Fonts/GoogleSans-Medium.ttf`.
- Version: v14.000, downloaded from the [official Google Fonts project release](https://github.com/googlefonts/googlesans/releases/tag/v14.000).
- [Original release archive](https://github.com/googlefonts/googlesans/releases/download/v14.000/GoogleSans-v14.000.zip), exact entry `build/GoogleSans/static/GoogleSans-Medium.ttf`.
- SHA-256: `1c87b72912ef81b48ab4852976f3d5bf75c7205e0a58a97ffca947d171c722a7`.
- Font extracted unchanged; use it for Google provider text. The current branding guideline specifies Google Sans Medium.
- License: SIL Open Font License 1.1, without Reserved Font Names. The complete [versioned OFL notice](https://github.com/googlefonts/googlesans/blob/v14.000/OFL.txt) is included beside the font as `GoogleSans-OFL.txt`.

## Implementation constraints

Use the official mark at its natural aspect ratio with no tint. The light provider control uses white, border `#747775`, and text `#1F1F1F`. Keep the action phrase and recognizable Google branding intact in both supported locales. The game-specific metallic font and glow belong to the other controls.

The positional map records the proposal's geometry, not a certification of branding compliance or a recovered typeface. Layout and text remain native and editable. No provider logo should be retained from the generated source artwork or clean plate.
