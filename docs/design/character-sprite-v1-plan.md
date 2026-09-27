# Character sprites: first-version body plan

The next AutoSprite pass plans **five body types for men and five for women**:
slim, medium, overweight, obese and muscular. The user confirmed this scope
while asking to preserve all fourteen approved illustrated designs. This is
a production plan; these ten new body variants have not been generated,
approved or added to the runtime selector.

Configuration and Home use the same **front-facing character** in the first
version. Isometric views and gameplay animation are deferred. The existing
AutoSprite preview is evidence for frontal presentation and idle playback;
it does not establish the ten body variants or interchangeable equipment.

## References and mapping

The [body reference catalog](../../design/character-autosprite/references/body-types-v1.json)
links each planned type to the exact selected revision and SHA-256 in the
[approved illustration manifest](../../design/character-2-5d/approval-r1/manifest.json).
These new labels do not rename the archived asset IDs.

| Planned type, each gender | Primary illustrated reference | Appearance to retain |
| --- | --- | --- |
| `slim` | `skinny` | Narrow silhouette and softer limbs with low muscle volume. |
| `medium` | `normal` | Moderate volume, softly flat abdomen and ordinary limbs, without a carved six-pack. |
| `overweight` | `chubby` | Fuller, softer torso and limbs, with a clear silhouette difference from medium. |
| `obese` | `fat` | Broad soft torso, chest and belly; preserve the approved limb proportions. |
| `muscular` | `muscular` | Visible muscle form while preserving the approved proportions. |

Retain `skinny_muscular` and `fat_muscular` for each gender as supplementary
references. They remain approved illustrations even though they are outside
the first sprite selection. Preserve all source masters, earlier revisions,
style and identity anchors, prompts, hashes and review records. Do not merge,
overwrite or delete them as part of reducing the production list.

Keep the established manhwa linework, hair shapes, authored shading and
charcoal/cyan/silver material language. Preserve a consistent adult identity
within each gender. Female muscle volume remains smaller than the male
counterpart, with proportionate arms and legs. Use the saved masters to
judge these choices; repeated prompts do not guarantee identical pixels.

Body selection describes appearance only. Do not infer it from weight,
measurements, health, fitness or workout history, and do not map it to
strength, ability or combat stats. The five names are the requested visual
options, not clinical classifications.

## Production sequence

1. Use the AutoSprite MCP tools (`create_character`, `generate_spritesheet`)
   and saved references to author coherent front-facing characters. Do not
   draw programmer art, embed SVGs or build bodies from independently
   generated limbs.
2. Export sprites and the provider JSON atlas under `assets/sprites/` in a
   new versioned folder. Retain exact prompts, provider character/job IDs,
   source and export checksums, and actual generation/review status. Keep
   credentials and expiring signed URLs out of the repository.
3. Read the JSON atlas before writing animation code. Preserve its frame
   rectangles, trim offsets, timing and canvas registration. Check complete
   hair and feet, transparent edges, stable ground contact and consistent
   nominal height in both configuration and Home.
4. Compare the ten frontal silhouettes at the actual displayed size. Keep
   the planned set unless visual review shows that a distinction cannot be
   retained at that resolution. Record any later scope change explicitly;
   do not silently collapse types or inherit the illustration approvals.
5. Validate one equipment swap before multiplying outfits. Equipment must
   fit the exact body, view, pose and frame coordinates. A flattened sprite
   or an asset gallery alone does not prove modular equipment support.

Only verified, reviewed exports should become selectable body options.
The existing frontend preview and the preserved illustration archive remain
separate from this next generation pass.
