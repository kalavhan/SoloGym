# Home furniture batch R1

Four independent static furniture sprites: weight rack, training bench, bed and
storage chest. Built-in imagegen authored each object from the approved Home
reference. AutoSprite MCP saved each image and returned an export identical to
the original bytes, including alpha. **No background-removal operation was
needed: 0 AutoSprite credits.** AutoSprite did not generate or redraw this art.

Five imagegen calls produced four selected assets. The initial rack had incorrect
one-ended weights; the retained revision makes each dumbbell's center grip and
two plate stacks visible. Its discarded source is preserved under
`rack/iterations/`. All source/export hashes and AutoSprite IDs are in
[provenance.json](provenance.json).

Every export is one complete object on a 1536×1024 RGBA canvas. Each runtime
Base.png is an unchanged copy of its export. The wood/metal rack and its weights
form one prop; the bed and bedding form another. The bench is bare so a towel
or bottle can be separate later. No chest contents, character or animation was
generated. RGB beneath alpha can appear dark in viewers that ignore transparency.

The [handoff](../../../../design/fantasy-home-furniture-r1/index.html) provides
one downloadable image, a <=200-character copyable description and detailed
brief per object. [Component notes](../../../../docs/design/pixel-home-furniture.md)
cover the Unity fixture and controls.

Position and scale are provisional for the user's later manual alignment pass.
Visual approval and mobile performance testing remain pending.
