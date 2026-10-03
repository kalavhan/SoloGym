using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>Plays a looping list of sprites on a SpriteRenderer (flames, banners, idle loops).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] frames;
        public float framesPerSecond = 8f;
        public bool randomStart = true;
        public bool pingPong;

        SpriteRenderer spriteRenderer;
        float time;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (randomStart && frames != null && frames.Length > 0 && framesPerSecond > 0f)
                time = Random.value * frames.Length / framesPerSecond;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || framesPerSecond <= 0f) return;
            time += Time.deltaTime;
            int count = frames.Length;
            int index = (int)(time * framesPerSecond);
            if (pingPong && count > 1)
            {
                int period = 2 * count - 2;
                index %= period;
                if (index >= count) index = period - index;
            }
            else
            {
                index %= count;
            }
            spriteRenderer.sprite = frames[index];
        }
    }
}
