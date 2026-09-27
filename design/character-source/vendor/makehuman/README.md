# MakeHuman graphical source

These are unmodified core graphical assets from the official MakeHuman repository,
pinned to commit `a8bc2d54ff0ac92e78ff71431b1023eda42bf482`. The exact download URLs,
byte counts and SHA-256 hashes are recorded in `SOURCE.json`.

The assets are **CC0-1.0**. The upstream license explicitly includes the base mesh,
targets, textures, rigs and graphical output. No MakeHuman application Python or
AGPL code is included. The upstream `LICENSE.md` and `LICENSE.ASSETS.md` are kept
verbatim. The mesh, targets, eye proxy and material contain their original CC0
notices; the rig and skin weights declare `license: CC0` in their JSON.

Primary sources:

- [Official asset license](https://github.com/makehumancommunity/makehuman/blob/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/LICENSE.md)
- [CC0 legal text](https://github.com/makehumancommunity/makehuman/blob/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/LICENSE.ASSETS.md)
- [Community license explanation](https://static.makehumancommunity.org/about/license.html)

## Loading contract

`tools/character_source/prepare_base.py` is a standalone, project-owned data
reader. It does not require MakeHuman, its application code, an add-on, or network
access after these files are vendored.

```python
from prepare_base import load_base
base = load_base(muscle=0.5)
```

The source has 19,158 indexed vertices. Its `body` group contains 13,378 quads and
uses vertices 0 through 13,379. Other vertices define joints, eyes and garment
helpers. They remain in the indexed arrays for morph and fitting consistency;
they must not be rendered as body geometry.

`positions` are in meters with Unity axes: X toward the character's left, Y up,
Z forward, soles at Y=0. Blender converts a point with `(x, -z, y)`. The original
source arrays are retained in `source_positions` in MakeHuman units. The loader
adds the official adult male target and the selected muscle delta before scaling.
This is a fixed proof body, not an implemented general character generator.

`bones` contains 22 names in parent-first order, with absolute `position` and
`tail`. Facial and finger weights are deliberately reduced into head and hand
bones for this body-motion proof. Detailed facial expressions and finger poses
need a later version of the rig contract. Source twist bones are combined into
the corresponding full limb. The loader renormalizes weights and retains the
strongest four influences per vertex.

`eyes` contains a 96-vertex mesh fitted to the morphed source helpers, its UVs and
the official brown eye texture. `shapes.muscle025` and `shapes.muscle100` retain
the exact source indices and base ground origin. Their helper joint positions
are provided for fit checks. Clothing should use the same body-derived surface,
weights and shape deltas rather than infer a fit from an unrelated image.

Run the bounded source checks or optionally export intermediate data:

```bash
python3 tools/character_source/prepare_base.py
python3 tools/character_source/prepare_base.py --output /tmp/sologym-character-base.json
```

The checks verify source hashes, body topology, real-world scale, a valid rig
hierarchy, complete normalized skinning coverage and shared morph topology.
They do not establish that a posed character or garment looks correct; that
requires Blender and Unity renders.
