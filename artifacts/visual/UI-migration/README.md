# Layered UI review evidence · Linux 0.5.0

These are captures of the running Unity player, not replacement target renders. The original targets remain in `design/renders`. See [implementation and limits](../../../docs/engineering/layered-ui-and-avatar-implementation.md).

`final/` contains 18 whole-screen states: EN/ES Welcome, age/region, consent, private profile and Home, plus email, imperial measurements, readiness, a smaller phone viewport, two component-gallery variants and two avatar variants. JSON files record the focused checks. `avatar-jab.mp4` contains 24 player frames at 12 fps; a one-pixel right pad makes its width compatible with video encoding.

`comparisons/` retains the ten baseline-window comparisons: unscaled side-by-side sources, 50% overlays, amplified differences and descriptive metrics. The left/source image is the earlier target, not the new implementation. Metrics are not an acceptance score. There are visible differences in native typography, frame decoration, generated icon/portrait extraction and reconstructed background details. The new native output is awaiting user review.

Whole-screen inspection corrected text wrapping, omitted Spanish Home layout rectangles, XP label spacing and the stats-heading border. Avatar motion inspection confirms the equipment follows its attachments, while also revealing unresolved joint silhouettes and fitting. The first modular character remains an engineering proof, not production-ready art.

Capture reproducibility: `python3 tools/capture_layered_ui.py`. Targeted runs with `--only` replace only selected records and keep the other capture results. Local preferences, logs, raw motion frames and superseded intermediate captures are ignored by Git. Captures use fictional in-memory review data and do not call identity providers or write remote profiles. No APK was rebuilt.
