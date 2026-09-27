# Character art approval — round 1

Open `index.html` in a modern browser. The gallery runs locally without installation, a framework, a network connection, or a server.

The review contains **14 illustrated art proposals**: men and women, each with these seven body presets in this exact order:

1. Skinny
2. Normal
3. Chubby
4. Fat
5. Skinny muscular
6. Muscular
7. Fat muscular

Each image is a complete artboard containing a base body, equipped character and gameplay view. The original approved visual style is available from the reference card. Artboard approval does not establish separate sprite layers, animation quality or runtime readiness.

## Reviewing

- Filter by men, women or body preset. Select **Needs review** to show pending proposals and proposals with requested changes; approved images are hidden by this filter.
- Select **Compare two** to place any two proposals side by side.
- Select an artboard to open its full-size viewer. Use `+` / `−` to zoom, `0` to return to fit width, arrow keys to step through proposals, and `Esc` to close. The viewer scrolls when artwork exceeds the viewport.
- Imported decisions from the verified user review appear in every browser. Leave notes and select a local decision to continue that review. New image revisions begin **Pending approval**, with the previous requested change shown beside the review fields.
- Use **Export review JSON** to preserve or share the decisions and notes. Browser storage can be unavailable for local files or private browsing, so exported JSON is the durable record.

Decisions are stored in this browser under `sologym-character-art-review-r1`. Each review is keyed by the stable proposal ID **and the exact image SHA-256**. A changed image therefore starts pending; it cannot inherit a prior image's approval. Earlier reviews remain in browser storage under their original keys, including legacy reviews that did not record a hash. Legacy unbound decisions are not applied to current artwork.

For the current image, a matching imported `review` from the source catalog is the default. A matching local review overrides it only when its `updatedAt` is newer. Equal timestamps prefer the imported record. Both sources must name the exact current image hash; records for another hash never apply. Imported records are displayed without being written into browser storage. A catalog `previous_review` is shown only as prior-image feedback, never used as the current decision. The **Approved** badge marks an imported approval; **Approved locally** marks a newer decision awaiting export.

The source catalog in `assets.js` is generated from `manifest.json`. It selects each proposal's exact image path, asset ID, revision and hash. The gallery disables review fields if this metadata is missing or invalid. The browser uses these recorded hashes; it does not rehash PNG bytes itself. The asset preflight must verify that the catalog, manifest and actual files agree before this gallery is shared.

The schema-v2 JSON export records the selected source metadata and the source attached to each saved review, plus the manifest and original style reference hashes. It does not change the canonical asset manifest. When approved images are promoted, reconcile the exported review against the exact source hashes. An older revision's stored review is shown again only if that same image hash is selected; exports identify both the currently selected source and the source originally reviewed.

## Files

- `index.html`, `styles.css`, `review.js`: standalone review application.
- `assets.js`: generated revision and hash catalog, loaded before `review.js` without a network fetch so local-file viewing remains supported.
- `manifest.json`: authoritative batch source metadata.
- `images/male-{preset}.png` and `images/female-{preset}.png`: initial source artboards, using lowercase IDs and underscores for multiword presets. Revised artwork has an explicit suffix such as `-r2.png`; `assets.js` selects the revision displayed.
- `../references/illustrated-style-r1.png`: original style reference.

To share the gallery, preserve this relative layout and include the reference image. The image files remain unmodified by the gallery.

## Verify source integrity

From the repository root, run `python3 tools/verify_character_art_batch.py` before
sharing the gallery or after selecting another revision. This read-only,
standard-library check verifies all fourteen IDs and their order, saved
references and exact prompts, current and retained output hashes, byte sizes,
PNG dimensions, and agreement between the manifest, style lock and `assets.js`.
It exits nonzero for stale or missing records. It does not judge anatomy,
approve artwork or establish runtime readiness.
