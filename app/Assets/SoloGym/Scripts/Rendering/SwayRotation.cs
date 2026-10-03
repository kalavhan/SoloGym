using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>Gentle sine sway around the local Z axis (hanging banners swinging from their rod).</summary>
    public class SwayRotation : MonoBehaviour
    {
        public float angle = 1.5f;
        public float speed = 1.1f;
        public float phase;

        Quaternion baseRotation;

        void Awake()
        {
            baseRotation = transform.localRotation;
        }

        void Update()
        {
            float z = Mathf.Sin(Time.time * speed + phase) * angle;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, z);
        }
    }
}
