# Room prototype (Octopath-style camera)

Scene: `app/Assets/SoloGym/Scenes/Prototypes/RoomOctopath.unity`
Target look: `docs/product/reference/room_hall_target.png`
Current render: `docs/product/reference/room_hall_pixel.png`

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

Note: the scene references the actor sprites by GUID. The animation frame `.meta` files are not
committed, so after a fresh clone reassign the two sprites (Barbarian squat frame 0, Slugvex idle).

## Hall art (pixel style)

Art lives in `Scenes/Prototypes/Hall/`. Sprites import at 40 px per unit, point filter, bottom-centre pivot,
so the hall reads a little chunkier than the characters (like the target image).

- PixelLab (create_image_pixflux, transparent): `portal_arch`, `banner_red`, `banner_navy`, `brazier`, `pilaster`, `rug`.
  The rug's empty centre was filled red and the portal's grey background removed after download.
- Drawn by `tools/generate_hall_textures.py`: `wall_brick` and `floor_slab` (tiling, gold diamond inlays).
- `floor_streak` plus the `Streak*` additive materials fake the polished-floor reflections under the portal,
  braziers and pilasters.
- Sorting order: wall pieces -10, reflections -8, braziers -6, rug -5, shadows 1, actors 5.
