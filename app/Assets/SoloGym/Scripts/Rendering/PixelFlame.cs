using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>A small pixel flame for the braziers: a few hand-banded frames drawn at startup and flipped like a flipbook.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelFlame : MonoBehaviour
    {
        public float framesPerSecond = 7f;
        public Color outer = new Color(1f, 0.38f, 0.08f), middle = new Color(1f, 0.70f, 0.18f), core = new Color(1f, 0.95f, 0.62f);
        Sprite[] frames;
        SpriteRenderer sr;
        float offset;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            offset = Random.value * 10f;
            frames = new Sprite[4];
            for (int i = 0; i < frames.Length; i++) frames[i] = Draw(i);
            sr.sprite = frames[0];
        }

        void OnDestroy() { if (frames != null) foreach (var f in frames) if (f != null) { Destroy(f.texture); Destroy(f); } }

        Sprite Draw(int frame)
        {
            const int w = 14, h = 22;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);                    // 0 base .. 1 tip
                float sway = Mathf.Sin(v * 5f + frame * 1.7f) * 1.2f * v;
                float tipCut = 1f - Mathf.Pow(v, 1.6f);           // teardrop
                float half = (w * 0.5f - 1.2f) * Mathf.Sqrt(Mathf.Max(0f, tipCut)) * (0.85f + 0.15f * Mathf.Sin(frame * 2.1f + v * 3f));
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - w * 0.5f - sway);
                    Color c = Color.clear;
                    if (d <= half) c = outer;
                    if (d <= half * 0.66f && v < 0.85f) c = middle;
                    if (d <= half * 0.34f && v < 0.6f) c = core;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px); tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);   // 1 unit tall at scale 1
        }

        void Update()
        {
            int f = (int)((Time.unscaledTime + offset) * framesPerSecond) % frames.Length;
            sr.sprite = frames[f];
        }
    }
}
