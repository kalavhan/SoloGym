# Fasting — arcane clock window concept

Follows merged PR #30. The user requested a complete fasting window with a
pixel-art fantasy magic clock, elapsed time, an indication of the current
stage and restrained animation. This pack is a **visual proposal**, not an
approved Unity implementation or a working health tracker.

The room is generated first with PixelLab, then the clock as an independent
transparent sprite. A complete screen reference uses these assets and the
existing native UI style. The final window will keep the room, clock, animation,
text and controls separate. No character generation is included.

## Art direction

- Frontal guild alcove, slate stone, quiet window light and aged brass.
- A large astrolabe is the focal point; its center remains clear for a live timer.
- Slow rune motion, a small change of accent/pattern by time range and steady
  overall brightness. Longer fasting does not make the clock more powerful.
- Timer, stage label and buttons remain still and readable. No bloom, flashing,
  screen shake, reward reveal or dense particles. Respect reduced motion.
- Reuse the existing pixel frames, teal actions and parchment surface. The
  primary action while running is always **Finalizar ahora**.

## Time ranges, not measured metabolic states

The proposed display uses five continuous elapsed-time bands: 0–4, 4–16,
16–24, 24–48 and 48+ hours. The last band closes the 48–72-hour gap in the
submitted list. These are interface groupings, not clinical thresholds or
recommended durations. Use neutral labels and a clear “Etapa orientativa”
caption; do not display ketosis, autophagy, hormone boosts or immune repair as
unlocked or detected states. See [evidence notes](evidence-notes.md).

The existing MVP supported-plan cap remains 20 hours. It is not a safety claim
or a target. Later bands only describe an actual/corrected elapsed record;
they do not add selectable extended plans. Passing a chosen end keeps an
honest elapsed time, offers ending/correction, and never extends the plan or
invents a finish automatically.

The example in the concept is 08:24:16 elapsed, starting yesterday at 22:00,
with a user-selected end today at 10:00. It is fictional, not a recommendation.

## Complete-window implementation after visual approval

Keep adult suitability/enablement, optional/off-by-default behavior and teen
exclusion. Implement start, elapsed timer, correction, ending, private history,
deletion and disabling together in one window PR. No XP, buffs, rankings,
streaks or training effects. Stopping early carries no penalty.

No new app code is part of this concept pass. Source requests, provider job IDs,
quoted costs and asset hashes are retained with the art for the subsequent PR.

## Review pack

`fasting-clock-concept.png` is the complete landscape reference, rendered with
built-in imagegen using the actual PixelLab room and clock plus the existing
native workout UI. `review/index.html` shows the render and a separate motion
proof with live labels and five selectable fictional time examples. It is a
review utility, not the app or a working fasting tracker.

The original clock stays visible in the motion proof. The PixelLab animation
returned nine 256×256 frames; its whole-frame playback is rejected because two
frames make the center transparent and some rune/gem highlights become too
bright. The proof clips playback to the rune ring, reduces saturation/brightness
and caps its overlay opacity at 0.32. No source PNG is modified. The center,
casing and pedestal remain the original static art. Reduced motion removes the
decorative overlay. `motion-atlas.json` records the frame order, original hashes,
authored timing and presentation mask; PixelLab did not supply frame durations.

Quoted PixelLab cost for this pass: **68 generations** (40 room + 20 static
clock + 8 motion). The imagegen render uses its separate tool allowance.

To reopen the isolated review:

```sh
python3 -m http.server 8907 --bind 127.0.0.1 --directory design/fantasy-fasting-r1/review
```

Then open `http://127.0.0.1:8907/`. The server exposes only this review folder.
