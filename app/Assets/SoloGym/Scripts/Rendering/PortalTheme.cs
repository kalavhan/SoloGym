using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>
    /// The summoning arch announces who is coming: a glowing sigil floats in the opening, and the glow, light and
    /// sparkles take the boss's colour. Everything is drawn in a few hard bands to match the pixel style.
    /// </summary>
    public sealed class PortalTheme : MonoBehaviour
    {
        [System.Serializable]
        public struct Look { public string boss; public Color color; }

        [Header("Parts")]
        public SpriteRenderer glow;
        public SpriteRenderer emblem;
        public Light portalLight;
        public ParticleSystem sparkles;

        [Header("Colours")]
        public Color defaultColor = new Color(0.35f, 0.62f, 1f);
        public Look[] looks =
        {
            new Look { boss = "slugvex", color = new Color(0.45f, 0.95f, 0.35f) },
            new Look { boss = "maybmor", color = new Color(0.72f, 0.45f, 1f) },
            new Look { boss = "snoozmoth", color = new Color(0.40f, 0.45f, 1f) },
            new Look { boss = "glutgrub", color = new Color(1f, 0.52f, 0.2f) },
            new Look { boss = "velshade", color = new Color(1f, 0.55f, 0.85f) },
        };

        [Header("Glow shape")]
        public Vector2 openingSize = new Vector2(3f, 4.8f);
        public int bands = 5;
        public float pulseSpeed = 1.6f, pulseAmount = 0.12f, emblemBob = 0.12f;

        Color color;
        float lightBase;
        Vector3 emblemHome;
        Sprite glowSprite;

        void Awake()
        {
            color = defaultColor;
            if (portalLight != null) lightBase = portalLight.intensity;
            if (emblem != null) emblemHome = emblem.transform.localPosition;
            BuildGlow();
            Apply(null);
        }

        void OnDestroy() { if (glowSprite != null) { Destroy(glowSprite.texture); Destroy(glowSprite); } }

        /// <summary>Takes the boss's colour and sigil. Unknown or empty id = the plain blue portal with no sigil.</summary>
        public void Apply(string bossId)
        {
            color = defaultColor;
            if (!string.IsNullOrEmpty(bossId) && looks != null)
                foreach (var l in looks) if (l.boss == bossId) color = l.color;
            Sprite sigil = string.IsNullOrEmpty(bossId) ? null : Resources.Load<Sprite>("Rooms/Sigils/" + bossId);
            if (emblem != null) { emblem.sprite = sigil; emblem.enabled = sigil != null; }
            if (portalLight != null) portalLight.color = Color.Lerp(color, Color.white, 0.15f);
            if (sparkles != null) { var m = sparkles.main; m.startColor = Color.Lerp(color, Color.white, 0.35f); }
            Tint(1f);
        }

        void Tint(float pulse)
        {
            if (glow != null) glow.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(pulse));
            if (emblem != null) emblem.color = Color.Lerp(Color.white, color, 0.25f);
        }

        void Update()
        {
            float s = Mathf.Sin(Time.unscaledTime * pulseSpeed);
            Tint(0.85f + s * pulseAmount);
            if (emblem != null) emblem.transform.localPosition = emblemHome + Vector3.up * (Mathf.Round(s * emblemBob * 8f) / 8f);
        }

        /// <summary>Banded glow in the shape of the arch opening: brightest at the floor, fading upward.</summary>
        void BuildGlow()
        {
            if (glow == null) return;
            const int w = 24, h = 36;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f;               // -1..1
                    float v = (y + 0.5f) / h;                         // 0 bottom .. 1 top
                    // arched top: a half circle over the upper part
                    float archY = 0.68f;
                    bool inside = v <= archY ? Mathf.Abs(u) <= 1f : (u * u + Mathf.Pow((v - archY) / (1f - archY), 2f)) <= 1f;
                    float a = 0f;
                    if (inside)
                    {
                        float edge = 1f - Mathf.Abs(u) * 0.55f;
                        a = Mathf.Clamp01((1.05f - v * 0.75f) * edge);
                        a = Mathf.Ceil(a * bands) / bands * 0.9f;
                    }
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply(false);
            glowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
            glow.sprite = glowSprite;
            glow.transform.localScale = new Vector3(openingSize.x / (w / (float)h), openingSize.y, 1f);
        }
    }
}
