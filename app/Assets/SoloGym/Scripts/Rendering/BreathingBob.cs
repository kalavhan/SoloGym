using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>Subtle vertical squash-and-stretch from the feet, for characters without an idle animation.</summary>
    public class BreathingBob : MonoBehaviour
    {
        public float amount = 0.02f;
        public float speed = 2.2f;

        Vector3 baseScale;
        float phase;

        void Awake()
        {
            baseScale = transform.localScale;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            float s = Mathf.Sin(Time.time * speed + phase) * amount;
            transform.localScale = new Vector3(baseScale.x * (1f - s * 0.5f), baseScale.y * (1f + s), baseScale.z);
        }
    }
}
