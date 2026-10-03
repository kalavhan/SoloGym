using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>Noise-driven intensity flicker for torch and portal lights.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 3f;
        public float amount = 0.6f;
        public float speed = 6f;

        Light targetLight;
        float seed;

        void Awake()
        {
            targetLight = GetComponent<Light>();
            seed = Random.value * 100f;
            if (baseIntensity <= 0f) baseIntensity = targetLight.intensity;
        }

        void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed);
            targetLight.intensity = baseIntensity + (noise - 0.5f) * 2f * amount;
        }
    }
}
