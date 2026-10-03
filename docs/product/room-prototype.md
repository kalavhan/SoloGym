# Room prototype (Octopath-style camera)

Scene: `app/Assets/SoloGym/Scenes/Prototypes/RoomOctopath.unity`
Target look: `docs/product/reference/room_hall_target.png`
Current render: `docs/product/reference/room_hall_alive.png`

Approach (HD-2D): a real 3D room (floor, walls, pillars, torch lights) seen by a fixed,
tilted, narrow-FOV perspective camera. Characters stay 2D sprites, upright, facing the camera,
with a flat blob shadow under their feet.

- `Scripts/Rendering/SpriteBillboard.cs`: keeps a sprite facing the camera.
- `Scripts/Rendering/RoomCameraRig.cs`: pitch 20, yaw 0, FOV 30 (camera angle approved); backs the camera off so a
  fixed world width (`fitWidth`, 25) always fits.
- The room is landscape (the game view is set to 1920x1080). Hall style: brick walls, flagstone floor, glowing portal
  with stone frame, banners, torch pillars, gold-edged rug, cool ambient light with warm torch lights.
- Actors are a root (billboard) plus a child sprite offset by the sprite's foot row so feet sit on the floor.

Hades-style camera for the future game: same idea with a steeper pitch (about 50-60 degrees),
wider room and a follow target; not built yet.

Note: the actor sprite .meta files the scene uses (Barbarian idle frames, Slugvex idle) are committed, so a fresh
clone opens the scene with both fighters in place.

## Hall art (pixel style)

Art lives in `Scenes/Prototypes/Hall/`. Sprites import at 40 px per unit, point filter, bottom-centre pivot,
so the hall reads a little chunkier than the characters (like the target image).

- PixelLab (create_image_pixflux, transparent): `portal_arch`, `banner_red`, `banner_navy`, `brazier`, `pilaster`, `rug`.
  The rug's empty centre was filled red and the portal's grey background removed after download.
- Drawn by `tools/generate_hall_textures.py`: `wall_brick` and `floor_slab` (tiling, gold diamond inlays).
- `floor_streak` plus the `Streak*` additive materials fake the polished-floor reflections under the portal,
  braziers and pilasters.
- Sorting order: wall pieces -10, reflections -8, braziers -6, rug -5, shadows 1, actors 5.

## Life in the room

- Camera is closer (`fitWidth` 20, focus y 3.6) and the fighters stand further forward (z -3 / -2) so they read bigger
  while the portal stays in frame.
- `SpriteFlipbook`: brazier flames (PixelLab `animate_image`, 8 frames, 10 fps) and the Barbarian breathing idle
  (PixelLab template, 4 frames, 5 fps, in `Resources/Game/Animations/male-medium/idle`, listed in `tools/pixellab_animations.json`).
- `BreathingBob`: subtle squash and stretch for Slugvex (the PixelLab humanoid idle template distorted him).
- `SwayRotation`: banners swing from a pivot at their rod (PixelLab's banner animation lost the cloth fill).
- `LightFlicker`: torch and portal lights.
- Particle systems (needs `com.unity.modules.particlesystem`): brazier embers, portal sparkles, floating dust,
  all using a 4x4 white pixel with an additive material.
- Source art and provenance: `assets/sprites/pixellab/hall-room-r1/manifest.json`.
- Grounding: PixelLab canvases have transparent margins under the art (portal 13 px, pilaster 9 px, brazier 7 px), so each
  wall piece is lowered by margin / 40 ppu x scale. Portal and pilasters stand flush against the wall (z 5.97, the stone
  ledge is disabled) so their bases meet the wall-floor seam like the target image; only the free-standing braziers get a
  `contact_shadow`, centred under the base and drawn beneath it so no gap shows.
- Braziers stand forward at z 2.2; each torch light sits just in front of its flame with an additive `glow_halo` quad.
- Dust only floats in a band behind the fighters (z about 2.7 to 5.3) so nothing drifts across the characters.

## In the app: boss stand-off

The hall is a prefab, `Resources/Rooms/BossHall3D/BossHall.prefab`, made from the prototype scene (which now holds an
instance of it). `PixelBossWindow` spawns it with `BossStage3D.Spawn()` and draws its uGUI overlay on top; when the prefab
is missing, or with `-sologym-flat-boss`, it falls back to the painted 2D room.

- `BossStage3D` drives the two fighters: resting pose (Barbarian breathing idle when the appearance has one, otherwise the
  still export; the boss idle still) and the exercise clips the window already picks. Every PixelLab canvas is scaled to the
  same 3.4-unit height and stood on the floor by its foot margin, so idle and exercise frames line up.
- Composition: hero in front on the left (z -3.4), boss one row back (z 0.2), camera focus shifted right (x 2.8) so both
  fighters sit in the open area left of the workout card and above the difficulty bar.
- The stage applies the room's ambient light and fog while shown and restores the previous values when hidden.
- Renders: `docs/product/reference/boss_standoff_3d.png`, `docs/product/reference/boss_standoff_3d_squat.png`.

## Stand-off redesign (review)

Target: `docs/product/reference/boss_standoff_target.png` (user reference). Current: `docs/product/reference/boss_standoff_redesign.png`.

- Fighters stand side by side in the centre of the rug; the camera is centred on them.
- The routine moved off the overlay onto a PixelLab stone quest board in the room (`BossQuestBoard`, world-space text on the
  slate): today's exercises, the current one in glowing gold inside a ring of fire particles, done ones dimmed, stage name below.
- One action area at the bottom: VS cards (you: the exercise and set; the boss: its lazy version from `mockery` in
  `bosses.json`, e.g. "3x10 jacks that forgot to jump"; on the final set "Gave up. All yours.") and one big COMPLETE, with
  "Other amount" and "Finish all" beside it. Rest swaps the cards for a timer and I'M READY.
- Edges only: duel plate top left; difficulty, pause and stop top right; "View routine" bottom right.
- Pause, stop, finish, other amount, readiness recheck, summary, routine and corrections open as one centred card over a dimmed hall.

## 3D props with pixel textures

- `Shaders/PixelTriplanar.shader`: lit (Lambert, shadows) world-space projection along each face's main axis, point
  filtered, so any scaled primitive keeps the same chunky pixel density (`_TileSize` world units per repeat, 0.8 = 40 px/unit
  like the walls).
- Textures: PixelLab `create_tiles_pro` (16 variations, 4 picked: stone, slate, gold, wood) in `Hall/Props/`, archived in
  `assets/sprites/pixellab/hall-room-r1/textures/`. Materials `PropStone`, `PropSlate`, `PropGold`, `PropWood`.
- Quest board is now a real 3D frame built from boxes (posts, beams, slate, gold trim, crest), turned 28 degrees towards the
  fighters; it casts and receives shadows and a warm light follows the current row. Text sits on the slate in perspective.
- Foreground stone railings at the bottom corners (z -6.6) frame the scene for depth.
- Rounded or detailed props (statues, plants, weight rack) are the candidates for Blender later.

## Tripo quest board

- Made in Tripo from the PixelLab sprite (`/mnt` handoff: quest_board_front), exported low poly (9,398 triangles), FBX, 1K,
  sent into Unity with the Tripo Bridge (local dev tool, not in the repo). Lives in `Hall/Props/QuestBoard/`.
- `Shaders/PixelToon.shader`: lighting in 3 hard steps plus an inverted-hull dark outline. `Board_PixelToon.mat` uses a
  512 px, point-filtered, palette-reduced copy of the base colour (`BaseColor_pixel.png`).
- The model replaces the box frame under `QuestBoard/Model`; it sits so the slate surface is just behind the world-space text.
  Slate rect (-1.35, 1.3, 2.7, 4.15) local, 4 rows.
