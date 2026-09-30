# Home furniture batch R2

Four independent sprites: writing desk, stool, wall shelf and floor rug.
Built-in imagegen authored one image per object using the approved Home
concept. AutoSprite MCP saved and returned each export without redrawing it.
**Four imagegen calls; 0 AutoSprite credits.** Generated transparency already
existed, so no background removal was requested.

Original, export and runtime PNG files are byte-identical, including alpha.
All images are 1536×1024 RGBA. [provenance.json](provenance.json) records
AutoSprite asset IDs, creation times, exact hashes and individual placement
metadata. Dark or colored RGB under transparent pixels is not painted into
the room; use an alpha-aware viewer.

The desk and shelving unit are empty to allow separate decorations later.
The shelf's quadrilateral describes wall occupancy; the desk/stool describe
floor occupancy and the rug uses its four corners. These are approximate
placement envelopes, not physics colliders. The rug is sorted under the other
props and character.

See the [four single-image handoffs](../../../../design/fantasy-home-furniture-r2/index.html)
for downloadable sprites, <=200-character copy descriptions and detailed
briefs, and [integration notes](../../../../docs/design/pixel-home-furniture-r2.md)
for review controls. Position and scale remain rough for the user's later
manual alignment pass. Visual approval and device testing are pending.
