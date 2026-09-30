# Training ring, revision 1

One independent static prop for Home. `original.png` is the single-object
imagegen reference. It was uploaded unchanged into AutoSprite, then its background
was removed through AutoSprite MCP for **1 credit**. `transparent.png` is that
export; its RGB matches the original and Unity uses the exact exported bytes.
See [provenance](provenance.json) for hashes and the asset ID. The first imagegen
pass was corrected to give the four posts three rope levels; both prompts are
preserved. No characters, equipment outfits or animations were generated.

The [handoff](../../../../design/fantasy-home-ring-r1/index.html) provides the
single-object reference, short description with copy/count, detailed brief and
transparent export. This asset and its Home integration await visual review.

The runtime [definition](../../../../app/Assets/SoloGym/Resources/Rooms/Props/TrainingRingR1/item.json)
uses the full 1536×1024 canvas, a shared floor pivot `(0.5, 121/1024)` and a single
uniform scale. The opaque silhouette is approximately 490 room units wide.
The four-point projected floor footprint excludes the ropes/posts above it.
It is placement metadata, not a physics collider or proof of free-placement
collision handling. Seasonal exports must keep this canvas, pivot, camera and
base footprint; only the `base` variant exists now.

The ring is reconstructed to fit the approved composition. Its four corner
posts replace the concept's ambiguous extra posts; its mat ornament and rope
details are not a pixel-exact crop of that concept. The complete hidden parts
exist, so moving/hiding the character cannot uncover holes in the asset.

Run `python3 tools/check_home_ring_assets.py` for the read-only export audit.
