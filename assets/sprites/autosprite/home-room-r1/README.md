# Refuge environment layers, revision 1

Two **independent static assets** for the first Home composition iteration:

- `room-shell.png`: empty stone/timber architecture, 1672×941, with a transparent window aperture.
- `castle-exterior.png`: 1024×1536 moonlit exterior, placed behind that aperture.

Authored with built-in imagegen using the approved Home concept as a reference,
then uploaded and saved through AutoSprite MCP. These are **imported references,
not AutoSprite-generated redraws**. AutoSprite returned byte-identical PNGs. The
[manifest](provenance.json) records asset IDs, source hashes, runtime paths, cost
and pending visual-review status. No new characters or animations were generated.
The [prompt record](../../../../design/fantasy-home-room-shell-r1/prompts.json)
preserves all four imagegen calls, including the transparency correction.

Both exports are copied unchanged into Unity's `Resources/Rooms/RefugeR1/`.
Explicit Sprite/Point/uncompressed imports avoid the legacy `Resources/Home/`
importer, which forces bilinear Default textures for old background references.
No raster editing, cropping or resampling script was used to manufacture these
assets. The source shell has alpha 244–253 on sampled floor pixels, and 0–4 in
the sampled window interior; it is not a strictly binary mask. The dark matte
and actual exterior render are checked in the assembled player capture.

The empty shell keeps warm ambient floor lighting. Lamps and all movable props
remain separate future assets. The exterior has been reconstructed and is not
an exact extraction of every castle pixel from the approved concept. These
differences require visual review; importing the concept does not imply approval
of the implemented window.

Audit: `python3 tools/check_home_room_assets.py` (Pillow required).
