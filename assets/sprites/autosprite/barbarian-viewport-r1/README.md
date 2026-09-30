# Barbarian viewport derivatives

Eight transparent static appearances derived from the exact user-created
[originals](../barbarian-user-r1/README.md). Each original was uploaded as an
AutoSprite **asset copy** for `remove_asset_background`; no character was created
or regenerated and no animation was requested. This cost **8 credits**, one per
background removal. Copies do not alter the user's existing character records.

The [manifest](manifest.json) records source character IDs, derivative asset IDs,
credit cost, hashes, alpha bounds and runtime framing. Every RGB pixel matches its
original. Only alpha changed. The male-skinny export's maximum alpha is 254 rather
than 255; the unmodified provider export is retained. Runtime PNG files are exact
copies of the transparent exports. Source and derivative records contain no
expiring signed download tokens.

Unity crops and pivots are metadata only. Each crop adds two source pixels around
the alpha bounds. The foot X coordinate is the midpoint of the two boots' outer
extents in the last 64 source rows at alpha >=128; Y is the bottom alpha extent.
One shared framing envelope preserves the relative body widths, source heights
and feet placement across the full roster. Images are never stretched by axis or
rescaled individually to equal widths.

These are **review candidates**. See [component contract and visual evidence](../../../../docs/design/pixel-character-viewport.md).
No pixel-grid conversion, palette replacement, new outfits or rigging is included.
