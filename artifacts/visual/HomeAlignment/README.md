# Home alignment evidence — 2026-09-30

The layout editor reuses the 18 existing Home sprites, room layers and eight
Barbarian sources. This evidence changes no sprite artwork or source hashes.
Neither fixture nor browser snapshot is an approved product default.

| Capture | Input and verification |
| --- | --- |
| `1280x720-es.png` | `edited-layout.json`; 59/59 Unity checks; no safe inset |
| `854x480-es.png` | Same layout; 59/59; 24px simulated safe insets |
| `1844x853-en.png` | Same layout; 59/59; 64px insets; female-fat preview |
| `browser-export-unity.png` | Actual copied browser export; 59/59; 1280×720 |
| `editor.png` | In-app browser editor at its normal viewport; current draft and female-muscular preview |

Matching JSON files enumerate the Unity checks. The browser export has fractional
positions and is kept separately from the deterministic test fixture. The browser
preview appearance is deliberately absent from layout JSON; Unity's default
appearance is male-medium unless selected through its character argument.

`default-layout.json` is the unchanged authored placement expressed in the new
portable format. `edited-layout.json` moves/resizes the bench and contents, ring
and character, and hides the trophy. The Node command below can reproduce both.

```sh
node tools/tests/home-layout.test.mjs --write-fixtures
```

All 22 model checks pass. Unity Linux build succeeds under 6000.3.24f1. Existing
default Home regression checks pass 194/194 (`baseline-checks.json`). The ordinary
`-sologym-room-layout` startup path was also captured outside the test entry point:
its pixels match `browser-export-unity.png` exactly at 1280×720.

Browser interactions verified numeric position entry, proportional sizing,
bench/content grouping, a restored local draft, file import, hidden-object
selection, undo, reference overlay and invalid JSON rejection. Exported JSON was
copied through the tool's visible copy control and loaded by Unity. The in-app
browser did not emit a confirmed download event, so the copyable JSON panel is
provided as a fallback. No automatic download success is claimed.

The local server returned 200 for the editor/module, 404 for repository/private
or traversal paths, and 501 for POST. It only serves its explicit asset allowlist.

The user's manual placement approval, perspective corrections to existing artwork,
the live Home HUD and device testing remain outstanding. See the
[editor guide](../../../docs/design/pixel-home-alignment-editor.md) for use and
the Unity launch command.
