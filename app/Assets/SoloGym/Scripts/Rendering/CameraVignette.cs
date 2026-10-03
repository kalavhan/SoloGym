using UnityEngine;

namespace SoloGym.Rendering
{
    /// <summary>
    /// Pixel-art vignette for the hall camera: a quad just in front of the near plane with a coarse,
    /// point-filtered radial texture in a few hard bands, so the corners fall into shadow like a lit stage.
    /// One transparent quad instead of a post-process pass keeps it cheap on phones.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class CameraVignette : MonoBehaviour
    {
        [Tooltip("Transparent unlit material (Unlit/Transparent). The texture is made at runtime.")]
        public Material material;
        public Color color = new Color(0.02f, 0.01f, 0.04f, 1f);
        [Range(0f, 1f)] public float strength = 0.7f;
        [Tooltip("Distance from the centre (0..1, to the corner) where the darkening starts.")]
        [Range(0f, 1f)] public float start = 0.45f;
        [Range(1, 8)] public int bands = 4;
        [Tooltip("Texture size: small = chunkier bands.")]
        public Vector2Int resolution = new Vector2Int(64, 36);

        Camera cam;
        Transform quad;
        Material instance;
        Texture2D texture;
        int builtHash;

        void OnEnable() { cam = GetComponent<Camera>(); Rebuild(); }

        void OnDisable()
        {
            if (quad != null) Destroy(quad.gameObject);
            if (instance != null) Destroy(instance);
            if (texture != null) Destroy(texture);
            quad = null; instance = null; texture = null; builtHash = 0;
        }

        static void Destroy(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        int Hash() => (color, strength, start, bands, resolution, material).GetHashCode();

        void Rebuild()
        {
            if (material == null) return;
            if (quad == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "Vignette";
                go.hideFlags = HideFlags.DontSave;
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                quad = go.transform;
                quad.SetParent(transform, false);
                var r = go.GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sortingOrder = 1000;
            }
            if (instance == null) { instance = new Material(material) { hideFlags = HideFlags.DontSave, renderQueue = 3900 }; }
            int w = Mathf.Max(4, resolution.x), h = Mathf.Max(4, resolution.y);
            if (texture == null || texture.width != w || texture.height != h)
            {
                if (texture != null) Destroy(texture);
                texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            }
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v) / Mathf.Sqrt(2f);
                    float t = Mathf.Clamp01((d - start) / Mathf.Max(0.001f, 1f - start));
                    t = Mathf.Ceil(t * bands) / bands; // hard bands
                    var c = color; c.a = t * t * strength;
                    px[y * w + x] = c;
                }
            texture.SetPixels32(px);
            texture.Apply(false);
            instance.mainTexture = texture;
            quad.GetComponent<MeshRenderer>().sharedMaterial = instance;
            builtHash = Hash();
        }

        void LateUpdate()
        {
            if (material == null) return;
            if (quad == null || builtHash != Hash()) Rebuild();
            if (cam == null) cam = GetComponent<Camera>();
            float dist = cam.nearClipPlane + 0.02f;
            float hgt = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            quad.localPosition = new Vector3(0f, 0f, dist);
            quad.localRotation = Quaternion.identity;
            quad.localScale = new Vector3(hgt * cam.aspect * 1.02f, hgt * 1.02f, 1f);
        }
    }
}
