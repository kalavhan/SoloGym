# User-created Barbarian source exports

Eight existing character images imported from the user's AutoSprite account for
the fantasy pixel-art redesign: male and female skinny, medium, fat and muscular.
The original AutoSprite names and character IDs are recorded in `manifest.json`.
The local `medium` name maps to the provider's `mid` name.

`originals/` contains the exact downloaded PNG bytes. No image was regenerated,
recolored, resized, cropped, repositioned or processed to remove its background.
The manifest records dimensions, alpha status, byte counts and SHA-256 hashes.
Import used zero art-generation or background-removal calls and zero credits.
Signed download tokens are deliberately omitted from the persisted metadata.

These are preserved user-authored sources, not a runtime import or an approval of
in-game framing, transparency, pixel-grid dimensions or equipment fitting. The
first redesign uses the baked-in hair, skin and eye colors; color customization
is deferred. Existing runtime character assets remain unchanged.

The static viewport imports these exact preserved sources. Its separately recorded
transparent derivatives and runtime metadata are in
[barbarian-viewport-r1](../barbarian-viewport-r1/README.md). The original manifest
above remains an unchanged record of source preservation.
