using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>
    /// Keeps a 2D sprite standing upright in a 3D room and facing the camera, the way HD-2D games
    /// (Octopath Traveler) place sprites. The sprite never rolls; it only turns to face the camera,
    /// optionally leaning back with the camera's pitch so it is not foreshortened by a tilted view.
    /// </summary>
    [ExecuteAlways]
    public sealed class SpriteBillboard : MonoBehaviour
    {
        [Tooltip("Camera to face. Empty = the main camera.")]
        public Camera target;
        [Tooltip("Lean back with the camera's pitch so the sprite looks exactly like its art. Off = perfectly vertical.")]
        public bool matchCameraPitch = true;
        [Tooltip("Extra backward lean in degrees when matchCameraPitch is on (0 = match exactly).")]
        public float extraPitch = 0f;

        void LateUpdate()
        {
            var cam = target != null ? target : Camera.main;
            if (cam == null) return;
            var forward = cam.transform.forward;
            var flat = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude < 1e-6f) return;
            var yaw = Quaternion.LookRotation(flat.normalized, Vector3.up);
            if (!matchCameraPitch && Mathf.Approximately(extraPitch, 0f)) { transform.rotation = yaw; return; }
            var pitch = matchCameraPitch ? cam.transform.eulerAngles.x : 0f;
            transform.rotation = yaw * Quaternion.Euler(pitch + extraPitch, 0f, 0f);
        }
    }
}
