using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>
    /// Octopath-style room camera: a fixed tilt, a narrow field of view and a set point of focus. It backs
    /// away until <see cref="fitWidth"/> world units of the room are visible across the screen, so the same
    /// room reads correctly on a portrait phone and on a landscape screen.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class RoomCameraRig : MonoBehaviour
    {
        [Tooltip("The point the camera looks at (floor height of the action).")]
        public Vector3 focus = new Vector3(0f, 1.2f, 0.4f);
        [Tooltip("Downward tilt in degrees. ~15-25 gives the Octopath look.")]
        [Range(0f, 60f)] public float pitch = 20f;
        [Range(-180f, 180f)] public float yaw = 0f;
        [Tooltip("Vertical field of view. Narrow = flatter, more diorama-like.")]
        [Range(10f, 70f)] public float fieldOfView = 30f;
        [Tooltip("World units of the room that must fit across the screen at the focus point.")]
        public float fitWidth = 9f;
        [Tooltip("Never come closer than this to the focus point.")]
        public float minDistance = 6f;

        Camera cam;

        void OnEnable() { cam = GetComponent<Camera>(); Apply(); }
        void LateUpdate() { Apply(); }

        public void Apply()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            cam.orthographic = false;
            cam.fieldOfView = fieldOfView;
            var aspect = Mathf.Max(0.1f, cam.aspect);
            var half = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            var distance = Mathf.Max(minDistance, fitWidth / (2f * aspect * half));
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rot;
            transform.position = focus - rot * Vector3.forward * distance;
        }
    }
}
