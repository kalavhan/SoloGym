using System.Linq;
using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>
    /// The 3D summoning hall shown behind the boss stand-off UI: room, lights, camera and the two
    /// fighters as camera-facing sprites. Lives in Resources so the dungeon window can spawn it.
    /// </summary>
    public sealed class BossStage3D : MonoBehaviour
    {
        public const string ResourcePath = "Rooms/BossHall3D/BossHall";

        [Header("Fighters")]
        public SpriteRenderer heroSprite;
        public SpriteRenderer bossSprite;
        [Tooltip("Transparent rows under the feet in the PixelLab canvas.")]
        public float heroFootPixels = 3f, bossFootPixels = 12f;
        [Tooltip("World height of a full 256 px PixelLab character canvas.")]
        public float canvasHeight = 3.4f, canvasPixels = 256f;
        public float framesPerSecond = 8f, idleFramesPerSecond = 5f;

        [Header("Room props")]
        [Tooltip("Stone board on the right that lists today's exercises.")]
        public BossQuestBoard board;

        [Header("Room lighting (applied while the stage is shown)")]
        public Color ambientLight = new Color(0.50f, 0.54f, 0.78f);
        public bool fog = true;
        public Color fogColor = new Color(0.09f, 0.09f, 0.15f);
        public float fogStart = 22f, fogEnd = 60f;

        sealed class Actor
        {
            public SpriteRenderer target;
            public float footPixels;
            public Sprite still;
            public Sprite[] idle, playing;
            public float rate, clock;
        }

        readonly Actor hero = new Actor(), boss = new Actor();
        UnityEngine.Rendering.AmbientMode savedMode;
        Color savedAmbient, savedFogColor;
        bool savedFog, applied;
        FogMode savedFogMode;
        float savedFogStart, savedFogEnd;

        /// <summary>Instantiates the hall, or returns null when the prefab is not in the build.</summary>
        public static BossStage3D Spawn()
        {
            var prefab = Resources.Load<BossStage3D>(ResourcePath);
            if (prefab == null) return null;
            var stage = Instantiate(prefab);
            stage.name = "Boss hall stage";
            return stage;
        }

        void Awake()
        {
            hero.target = heroSprite; hero.footPixels = heroFootPixels;
            boss.target = bossSprite; boss.footPixels = bossFootPixels;
        }

        /// <summary>Resting pose: an idle loop when there is one, otherwise a still frame.</summary>
        public void SetHero(Sprite still, Sprite[] idle) => SetRest(hero, still, idle);
        public void SetBoss(Sprite still, Sprite[] idle) => SetRest(boss, still, idle);

        /// <summary>Exercise clip, or null to go back to the resting pose.</summary>
        public void PlayHero(Sprite[] clip) => Play(hero, clip);
        public void PlayBoss(Sprite[] clip) => Play(boss, clip);

        void SetRest(Actor actor, Sprite still, Sprite[] idle)
        {
            actor.still = still;
            actor.idle = idle != null && idle.Length > 1 ? idle : null;
            if (actor.playing == null) Show(actor, Current(actor, 0f));
        }

        void Play(Actor actor, Sprite[] clip)
        {
            // Generated exercise clips end on their start pose, so the last frame is dropped for a seamless loop.
            actor.playing = clip == null || clip.Length < 2 ? null : clip.Length > 2 ? clip.Take(clip.Length - 1).ToArray() : clip;
            actor.clock = 0f;
            Show(actor, Current(actor, 0f));
        }

        Sprite Current(Actor actor, float clock)
        {
            if (actor.playing != null) return actor.playing[(int)(clock * framesPerSecond) % actor.playing.Length];
            if (actor.idle != null) return actor.idle[(int)(clock * idleFramesPerSecond) % actor.idle.Length];
            return actor.still;
        }

        /// <summary>Scales any PixelLab canvas to the shared character height and stands its feet on the floor.</summary>
        void Show(Actor actor, Sprite sprite)
        {
            if (actor.target == null || sprite == null || actor.target.sprite == sprite) return;
            actor.target.sprite = sprite;
            float unitsPerPixel = canvasHeight / canvasPixels;
            float scale = unitsPerPixel * sprite.pixelsPerUnit;
            var t = actor.target.transform;
            t.localScale = new Vector3(scale, scale, 1f);
            float bottom = sprite.bounds.min.y * scale;
            t.localPosition = new Vector3(t.localPosition.x, -bottom - actor.footPixels * unitsPerPixel, t.localPosition.z);
        }

        void Update()
        {
            Step(hero); Step(boss);
        }

        void Step(Actor actor)
        {
            if (actor.playing == null && actor.idle == null) return;
            actor.clock += Time.unscaledDeltaTime;
            Show(actor, Current(actor, actor.clock));
        }

        void OnEnable()
        {
            if (applied) return;
            savedMode = RenderSettings.ambientMode; savedAmbient = RenderSettings.ambientLight;
            savedFog = RenderSettings.fog; savedFogColor = RenderSettings.fogColor; savedFogMode = RenderSettings.fogMode;
            savedFogStart = RenderSettings.fogStartDistance; savedFogEnd = RenderSettings.fogEndDistance;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = ambientLight;
            RenderSettings.fog = fog; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
            applied = true;
        }

        void OnDisable()
        {
            if (!applied) return;
            RenderSettings.ambientMode = savedMode; RenderSettings.ambientLight = savedAmbient;
            RenderSettings.fog = savedFog; RenderSettings.fogColor = savedFogColor; RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogStartDistance = savedFogStart; RenderSettings.fogEndDistance = savedFogEnd;
            applied = false;
        }
    }
}
