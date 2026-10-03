# Room prototype (Octopath-style camera)

Scene: `app/Assets/SoloGym/Scenes/Prototypes/RoomOctopath.unity`
Reference render: `docs/product/reference/room_octopath_v3.png`

Approach (HD-2D): a real 3D room (floor, walls, pillars, torch lights) seen by a fixed,
tilted, narrow-FOV perspective camera. Characters stay 2D sprites, upright, facing the camera,
with a flat blob shadow under their feet.

- `Scripts/Rendering/SpriteBillboard.cs`: keeps a sprite facing the camera.
- `Scripts/Rendering/RoomCameraRig.cs`: pitch 20, yaw 0, FOV 30; backs the camera off so a
  fixed world width (`fitWidth`) always fits, so portrait and landscape both work.
- Actors are a root (billboard) plus a child sprite offset by the sprite's foot row so feet sit on the floor.

Hades-style camera for the future game: same idea with a steeper pitch (about 50-60 degrees),
wider room and a follow target; not built yet.

Note: the scene references the actor sprites by GUID. The animation frame `.meta` files are not
committed, so after a fresh clone reassign the two sprites (Barbarian squat frame 0, Slugvex idle).
