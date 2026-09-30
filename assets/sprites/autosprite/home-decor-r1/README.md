# Home decorations R1

Four independent static sprites: hanging lantern, wall torch, cloth banner and
potted plant. Built-in imagegen authored one image per object, using the actual
room shell for viewpoint and the approved Home concept for design/materials.
AutoSprite MCP saved and returned the exact images without redrawing them.
**Four imagegen calls; 0 AutoSprite credits.** No background removal was needed.

Original, export and runtime PNGs are byte-identical, including generated alpha.
Each is 1024×1536 RGBA. [provenance.json](provenance.json) records source/reference
hashes, AutoSprite asset IDs, creation times and individual placement metadata.
Dark/colored RGB beneath transparent pixels is invisible in an alpha-aware viewer;
do not treat the generation preview's background as runtime artwork. Sampled
object interiors have near-opaque alpha (250–254); generated edges are preserved.

The lantern and torch contain static light/flame artwork only. They do not cast
dynamic light, animate or add a room-wide glow. The banner is a connected cloth
and crossbar assembly; the plant is a single pot-and-leaves object. Wall/hanging
envelopes describe assembly occupancy, while the pot has a floor footprint.
These are placement metadata, not colliders.

See the [individual-image handoffs](../../../../design/fantasy-home-decor-r1/index.html)
for PNG downloads, <=200-character copy descriptions and detailed briefs.
[Integration notes](../../../../docs/design/pixel-home-decor-r1.md) describe controls
and checks. Visual approval is pending. Existing furniture still needs a separate
perspective correction pass; moving/scaling objects cannot fix baked projection.
All placement/scale values remain rough for the user's manual alignment pass.
