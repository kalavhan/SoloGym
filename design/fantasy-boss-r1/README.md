# Boss workout — render review

PR #29 merged the approved Home and Workouts journal into main. This next window presents the reviewed routine as a dungeon encounter. The agreed delivery sequence remains PixelLab background, complete screen render, visual review, then one implementation PR for the whole Unity window.

The proposed main state has the existing Barbarian and a stone guardian on a shared frontal floor, a boss-progress bar, one set-recording panel, pause/end, and persistent Easy/Medium/Hard controls. The guardian is a concept inside the screen render; no separate production boss or animation batch has been generated or approved.

The example uses the existing intermediate-adult Base A prescription: one squat set has earned 100 of its fixed 200-point exercise budget, leaving 900/1000 boss health. The second set is ready for manual entry. The optional weight field stays blank; the avatar never prescribes a load. Values in the concept are examples, not connected workout data.

`window-scope.json` records the full later implementation: explicit start from a reviewed journal entry, set logging, rest, pause/resume, changes to remaining difficulty, corrections, local resume/stop/completion and failure states. Difficulty changes must preserve completed records and capped progress. Rest carries no enemy attacks or punishment. Character exercise demonstrations and production reward/cloud services remain outside this pass.

## Art separation

- `assets/sprites/pixellab/boss-chamber-r1/native/chamber.png`: PixelLab room architecture/floor only.
- `boss-workout-concept.png`: built-in imagegen full-screen reference, using the actual room and existing approved character/UI references.
- `screen-prompt.txt` and `production/background-prompt.txt`: exact generation briefs.
- `manifest.json` and the source art manifest: source hashes, PixelLab job/cost, review status and references.

The full-screen concept must never be used as the runtime background. Existing native uGUI controls remain the implementation method; the room, characters, sprite borders and live text remain independent.
