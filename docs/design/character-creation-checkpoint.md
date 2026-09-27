# Character creation checkpoint

This checkpoint preserves the approved illustrated direction, character studio
work, and animation experiments for review and merge. Further character creation
work starts after the user reviews and merges this PR. Gameplay animation comes
after character creation.

## Appearance and runtime status

- All fourteen selected appearance masters are approved: seven male and seven
  female body presets. Their source images, hashes and review history remain
  locked in `design/character-2-5d/approval-r1/` and `style-lock.json`.
- The opt-in illustrated Unity preview supports only normal male/female, studio
  and one authored elevated view, skin/hair recoloring and complete outfit swaps.
  It does not save an onboarding character or implement independent equipment.
- The user said the idle looks great. Walk and jab need substantial re-authoring:
  the walk lacks convincing stride phases and weight transfer, clothing fragments
  can move with an arm, and the jab does not connect naturally to the shoulder.
  These are unresolved defects, not an accepted gameplay animation foundation.
- The resting illustrations are not complete layered animation assets. Production
  requires complete overlapping anatomy/garments, deliberate key poses and
  replacement drawings where perspective changes. Repeated mask/rotation fixes
  alone are not the planned solution.
- The source3d path is retained as an opt-in, unapproved technical experiment and
  possible pose guide. Its mannequin appearance is not the selected art direction.
- The existing registered-piece renderer remains the default. This checkpoint
  does not promote either experiment into the production flow.

## Review

From the repository root, serve the self-contained review packages:

```bash
python3 -m http.server 8897 --bind 127.0.0.1 --directory design/character-2-5d
```

- Appearance masters: <http://127.0.0.1:8897/approval-r1/>
- Runtime comparisons and 24 motion videos:
  <http://127.0.0.1:8897/playable-proof-r1/review/>
- [Recorded runtime feedback](../../design/character-2-5d/playable-proof-r1/user-feedback.json)
- [Proof contract and launch command](../../design/character-2-5d/playable-proof-r1/README.md)

The compact runtime package contains sixteen static comparisons, twenty-four
motion videos, two full studio screenshots, a registration snapshot and reports.
Raw iteration/capture frames and Blender backup files remain local and ignored;
they can be regenerated with the supplied tools. Source assets and provenance
are included, along with the earlier renderer/source3d work needed to reproduce
the current project.

## Validation evidence

- Linux player built with Unity 6000.3.24f1.
- Appearance integrity: fourteen proposals, thirty-one recorded revisions and
  sixty-nine unique files checked for hashes, dimensions and selected revisions.
- Illustrated proof: 592 sampled states, including 24 motion sequences across
  both outfits, both characters and both views; structural validation passed.
- Illustrated studio: 31 control checks passed; capture-without-quit stays open.
- Existing studio regression: Spanish, English alternate, face view, avatar jab
  and capture-without-quit checks passed against the current player.

Structural success means valid files, meshes, state transitions and the measured
ground/weight constraints. It does not mean the animation is visually correct.
The user's motion feedback remains unresolved despite those successful checks.

## Stage after merge

Finish the character creation experience and stable body, skin, hair and fitted
equipment assets first. Retain the approved art and successful idle presentation.
Later, author and approve one character's complete animation workflow before
expanding to fourteen bodies, additional views or gameplay actions. No next-stage
implementation is included in this checkpoint.
