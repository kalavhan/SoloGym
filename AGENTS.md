## Art pipeline

When this game needs 2D character sprites or
animations, use the AutoSprite MCP tools
(create_character, generate_spritesheet).
Do not draw programmer art or embed SVGs.
Save exports under assets/sprites/ and read
the JSON atlas before writing animation code.

For the first version, use the front-facing character
in both configuration and Home. Isometric views are
deferred to a later version.

The planned sprite body types are slim, medium,
overweight, obese, and muscular for each gender.
Preserve all 14 approved illustrated designs as
references, including the extra mixed-muscle types.
Use docs/design/character-sprite-v1-plan.md and its
reference catalog; new sprites require their own review.
