using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Plays PixelLab frames (named 0, 1, 2, ...) on a uGUI Image, or holds one still sprite.</summary>
    public sealed class PixelSpriteLoop : MonoBehaviour
    {
        Image target;
        Sprite still;
        Sprite[] frames;
        float rate = 8f, clock;
        public bool Playing => frames != null && frames.Length > 1;
        public static PixelSpriteLoop Attach(Image image, Sprite stillSprite)
        {
            var loop = image.gameObject.AddComponent<PixelSpriteLoop>(); loop.target = image; loop.still = stillSprite; loop.Hold(); return loop;
        }
        /// <summary>Frames from Resources/<folder>, in numeric order. Empty when the art has not been added yet.</summary>
        public static Sprite[] Load(string folder)
        {
            var all = Resources.LoadAll<Sprite>(folder);
            return all.OrderBy(s => { int n; return int.TryParse(s.name, out n) ? n : int.MaxValue; }).ThenBy(s => s.name, StringComparer.Ordinal).ToArray();
        }
        public void Play(Sprite[] clip, float framesPerSecond = 8f)
        {
            if (clip == null || clip.Length < 2) { Hold(); return; }
            // The generated clip ends on its own start pose, so the last frame is dropped to keep the loop seamless.
            frames = clip.Length > 2 ? clip.Take(clip.Length - 1).ToArray() : clip; rate = Mathf.Max(1f, framesPerSecond); clock = 0;
            target.sprite = frames[0];
        }
        public void Hold() { frames = null; if (still != null) target.sprite = still; }
        void Update()
        {
            if (!Playing) return;
            clock += Time.unscaledDeltaTime;
            target.sprite = frames[(int)(clock * rate) % frames.Length];
        }
    }
}
