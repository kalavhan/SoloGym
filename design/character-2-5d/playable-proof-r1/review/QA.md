# Runtime review package QA

Capture run: artifacts/visual/IllustratedCharacterProof/r6-final

Packaged at: 2026-09-27T01:32:32.841469+00:00

This package contains real Unity proof outputs: 16 static comparisons, 24
motion videos and two full mobile studio screenshots. It covers only the normal
man and woman in studio and elevated gameplay views, fixed hairstyles, skin/hair
tints and whole outfit swaps. Motion is procedural. The videos show both base
and equipped characters so their shoulder and clothing continuity can be
reviewed separately.

The capture verifier reported structural success for all 592 sampled frames and
24 sequences. The maximum reported source alpha sole-contour pose error was
0.00000763 source pixels. The separate maximum metadata-to-alpha
registration offset was 6.50000000 source pixels. The verifier
records interactive capture staying open as True.
These checks do not establish visual quality or production readiness.

Shoulder joins and clothing behind moving arms still require production art
polish. Independent interchangeable gear, new hairstyles, the other body presets,
unseen directions and release-ready animation are outside this proof.
**Visual review remains pending; no approval is inferred.**

## Registration snapshot

- Original: app/Assets/SoloGym/Resources/AvatarIllustrated/Registration.json
- Frozen copy: runtime/Registration.snapshot.json
- SHA-256: 007b3950ed15444e9377b0e3a05e796d6df4ddfae10d48ed6c07966a76ae00bb
- Runtime report SHA-256: 9c97c90bdefe9f5364f6b2cb77dd476346d1745d740844c78952a6eb3c7a665f
- Verification report SHA-256: 8be75ad3c7f29fca026e813990e5ff946af3898e68cbc04e25333a7829aef3b9

runtime/package-manifest.json records every packaged file's exact bytes and
SHA-256. Packaging copied media without pixel edits or re-encoding and did not
alter approved masters. The full reports are retained unchanged; their raw
motion-frame PNG references still point into the original capture run. Those
576 PNGs are intentionally omitted from this compact review package.
