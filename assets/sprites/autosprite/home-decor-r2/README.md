# Home accents R2

Four independent static sprites: trophy, open book, training bottle and towel.
Built-in imagegen authored one image per object. AutoSprite MCP saved/exported
the exact images without redrawing or removing backgrounds. **Four imagegen
calls; 0 AutoSprite credits.**

All originals, exports and runtime PNGs are byte-identical 1024×1536 RGBA images.
Generated alpha is preserved. The trophy handles and bottle carry-loop have
transparent openings; sampled artwork interiors are near-opaque. Colored RGB
beneath transparent pixels does not appear in Unity's alpha-aware rendering.

[provenance.json](provenance.json) records source hashes, AutoSprite IDs, creation
times, pivot/footprint data and reference hashes. All prompts use the room shell
for viewpoint and the approved Home concept for design. The book additionally
references the existing desk; the towel references the existing bench. The
furniture images are guides, not included in either new object's PNG.

`supportObjectId` in provenance describes the intended surface only. The existing
runtime uses independent room slots, without automatic parenting or following.
The towel's pivot is its front fold and its footprint is a draped-cloth occupancy
envelope; the other pivots mark base/contact points. These are approximate
placement guides, not physics colliders or approved alignment.

[Individual PNGs, copyable short descriptions and full briefs](../../../../design/fantasy-home-decor-r2/index.html)
are available in the handoff. [Integration notes](../../../../docs/design/pixel-home-decor-r2.md)
cover controls and verification. Perspective correction of earlier furniture,
manual alignment and final visual approval remain pending.
