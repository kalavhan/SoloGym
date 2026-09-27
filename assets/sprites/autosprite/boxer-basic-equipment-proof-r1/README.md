# Basic static glove proof

**Historical prototype only. Do not use its dressed base as the canonical
production body.** The corrected creation workflow makes the body first and
all clothing/accessories as separate assets, then composes previews locally.
Only the isolated glove art is a candidate for reuse after fitting the new base.

The covered Basic outfit in `../boxer-basic-r3/` keeps the accepted style while
adding a full-length crew-neck top, short sleeves and relaxed above-knee shorts.

This proof passes a **single static hands-slot equip/remove test**. AutoSprite
produced the wrapped-hand base and glove-only image from that covered reference;
its background-removal tool produced the transparent exports. The PNG exports
are preserved unchanged and pinned by SHA-256 in `manifest.json`.

`proof.json` is the renderer contract: top-left coordinates on a 1024×1024 canvas,
one fixed base, and two source/destination rectangles for the left and right
gloves. The provider enlarged and repositioned the gloves, so the fit uses
explicit measured registration. No hidden body regeneration occurs on a toggle.

## Review

From the repository root, serve the files locally:

```sh
python3 -m http.server 8899 --bind 127.0.0.1
```

Open `http://127.0.0.1:8899/tools/review/boxing-equipment-proof.html`.
Toggle **Gloves equipped**. Use **100% pixels** to inspect cuffs and **Fit character**
to return to the full silhouette. Dark/light/checker backgrounds reveal alpha
edges. Fit guides show destination rectangles, not inferred anatomical anchors.

## Limits

Only the hands slot is independently layered. Headband, torso clothing, shorts,
boots and backpack remain in the base. This verifies one frozen female frontal
pose, not animation, other body types or Unity equipment integration. Backpack
layering will require separate rear bag and front strap occlusion. User visual
approval of the final revised artwork remains distinct from this technical fit
check; continuation was authorized with the coverage feedback.
