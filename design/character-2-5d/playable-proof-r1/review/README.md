# Local illustrated motion review

Open index.html in a modern browser. No installation, framework, external
service or approval storage is used. The page selects the normal man/woman,
studio/gameplay view, base/full motion outfit and looping idle/walk/jab video.
The motion player defaults to Full outfit. Base loops are equally available,
so joints and clothing coverage can be inspected in both states.
The page also shows base/equipped
rest states, deep skin, silver hair and links to full mobile studio screenshots.

The proof uses procedural motion and complete outfit states. The user praised
idle but identified substantial walk/jab defects: incomplete stride phases,
clothing moving with the arm and a mismatched shoulder. These motions require
re-authoring, not just polish. See [the recorded feedback](../user-feedback.json).
Viewing this page does not approve motion or establish independent gear swaps.

The original capture reports retain their status at capture time. The separate
[studio control report](studio-ui-smoke.json) records 31 passing checks; neither
those checks nor structural verification establish visual acceptance.

## Package a completed capture run

From the repository root, choose the exact run that completed validation:

    python3 design/character-2-5d/playable-proof-r1/review/package_runtime.py \
      --source artifacts/visual/IllustratedCharacterProof/CHOSEN_RUN

There is deliberately no default capture run. The script checks the proof,
verification and current registration hashes before copying any files. It then
copies 16 static PNGs, 24 MP4s, two menu PNGs and both reports into runtime/,
adds an exact registration snapshot and file integrity manifest, and writes
QA.md. It neither edits nor re-encodes source media. Raw motion-frame folders
are omitted; their original paths remain in the unchanged proof report.
The final report must cover all 592 frames and 24 sequences, including base
outfit motion. All twenty-four videos are available on this review page.
Equipped video filenames retain their existing IDs; base video IDs add
-base- before the action, for example female-gameplay-base-jab.mp4.

If the registration has changed since capture, supply the matching saved JSON
with --registration. A hash mismatch stops packaging. The final capture
snapshot and QA record are created only when this command succeeds.

To share the review, preserve index.html, runtime/, QA.md and this README
together. Missing runtime files show explanatory placeholders until captures
have been packaged; refresh the page after packaging.
