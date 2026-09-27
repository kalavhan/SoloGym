using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>
    /// Draws a registered illustration as one continuous, deformable UI surface.
    /// Bind points and posed vertices use the parent rig's local coordinates: x right, y up.
    /// The graphic stays at the rig origin; its RectTransform follows the mesh bounds for UI clipping.
    /// </summary>
    [AddComponentMenu("SoloGym/Avatar Registered Mesh")]
    public sealed class AvatarRegisteredMesh : RawImage
    {
        // 251 * 251 = 63,001 vertices, safely below Unity UI's 16-bit vertex limit.
        const int MaximumCellsPerAxis = 250;
        const float MinimumExtent = 0.001f;

        Vector2[] bindPoints;
        Vector3[] posedPoints;
        Func<Vector2, Vector3> poseVertex;
        Rect bindRect;
        Bounds meshBounds;
        int columns;
        int rows;
        int invalidVertexCount;

        public bool IsConfigured => bindPoints != null;
        public int GridColumns => columns;
        public int GridRows => rows;
        public Rect BindRect => bindRect;
        public Bounds MeshBounds => meshBounds;
        public int InvalidVertexCount => invalidVertexCount;

        /// <summary>
        /// Configures a direct child of the rig. sourceUV is a normalized, bottom-left-origin
        /// texture rectangle. bindRect is its corresponding rig-local rectangle, also with a
        /// bottom-left origin and positive dimensions. Cropped overlays can therefore share
        /// the same registration and deformation as the complete illustration.
        /// A null pose callback draws the bind pose. Tint, material, masking, and texture access
        /// retain their standard RawImage behavior.
        /// </summary>
        public void Configure(Texture2D source, Rect sourceUV, Rect bindRect,
            Func<Vector2, Vector3> poseVertex, int gridColumns = 40, int gridRows = 70)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!IsFinite(sourceUV) || sourceUV.width == 0 || sourceUV.height == 0)
                throw new ArgumentException("Source UV must be finite and have nonzero dimensions.", nameof(sourceUV));
            if (!IsFinite(bindRect) || bindRect.width <= 0 || bindRect.height <= 0)
                throw new ArgumentException("Bind rectangle must be finite and have positive dimensions.", nameof(bindRect));

            texture = source;
            uvRect = sourceUV;
            raycastTarget = false;
            this.bindRect = bindRect;
            this.poseVertex = poseVertex;
            columns = Mathf.Clamp(gridColumns, 1, MaximumCellsPerAxis);
            rows = Mathf.Clamp(gridRows, 1, MaximumCellsPerAxis);

            int vertexCount = (columns + 1) * (rows + 1);
            if (bindPoints == null || bindPoints.Length != vertexCount)
            {
                bindPoints = new Vector2[vertexCount];
                posedPoints = new Vector3[vertexCount];
            }

            for (int y = 0; y <= rows; y++)
            {
                float fy = (float)y / rows;
                for (int x = 0; x <= columns; x++)
                {
                    float fx = (float)x / columns;
                    bindPoints[y * (columns + 1) + x] = new Vector2(
                        bindRect.xMin + bindRect.width * fx,
                        bindRect.yMin + bindRect.height * fy);
                }
            }

            var parentRect = rectTransform.parent as RectTransform;
            var originAnchor = parentRect != null ? parentRect.pivot : Vector2.zero;
            rectTransform.anchorMin = rectTransform.anchorMax = originAnchor;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            UpdatePose();
        }

        /// <summary>
        /// Evaluates the current pose and invalidates the UI mesh. Call after changing bone
        /// transforms or deformation parameters; there is no per-frame work when the pose is idle.
        /// The bind-point and position arrays are reused between updates.
        /// </summary>
        public void UpdatePose()
        {
            if (!IsConfigured) return;

            invalidVertexCount = 0;
            var minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < bindPoints.Length; i++)
            {
                Vector3 point = poseVertex != null ? poseVertex(bindPoints[i]) : (Vector3)bindPoints[i];
                if (!IsFinite(point))
                {
                    // Keep one bad deformation value from invalidating the entire Canvas.
                    // CheckMesh still reports the failure instead of hiding it from verification.
                    invalidVertexCount++;
                    point = bindPoints[i];
                }
                posedPoints[i] = point;
                minimum = Vector3.Min(minimum, point);
                maximum = Vector3.Max(maximum, point);
            }

            meshBounds.SetMinMax(minimum, maximum);
            UpdateClippingBounds();
            SetVerticesDirty();
        }

        /// <summary>
        /// Reports the most recently evaluated mesh's rig-local bounds and a readable failure.
        /// It does not reevaluate the pose or force a Canvas render.
        /// </summary>
        public bool CheckMesh(out Bounds bounds, out string problem)
        {
            bounds = meshBounds;
            if (!IsConfigured)
                problem = "The registered mesh has not been configured.";
            else if (texture == null)
                problem = "The registered mesh has no source texture.";
            else if (invalidVertexCount != 0)
                problem = invalidVertexCount + " pose vertices were non-finite and fell back to their bind positions.";
            else if (!IsFinite(bounds.min) || !IsFinite(bounds.max))
                problem = "The registered mesh bounds are non-finite.";
            else if (bounds.size.x < MinimumExtent || bounds.size.y < MinimumExtent)
                problem = "The registered mesh collapsed to an empty visible area: " + bounds;
            else
            {
                problem = null;
                return true;
            }
            return false;
        }

        void UpdateClippingBounds()
        {
            // MaskableGraphic's CPU culling uses RectTransform corners. Keep those corners
            // aligned with the posed mesh while preserving its rig-local origin and bind points.
            var minimum = meshBounds.min;
            var size = meshBounds.size;
            float width = Mathf.Max(MinimumExtent, size.x);
            float height = Mathf.Max(MinimumExtent, size.y);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.pivot = new Vector2(-minimum.x / width, -minimum.y / height);
            rectTransform.localPosition = Vector3.zero;
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (!IsConfigured || texture == null) return;

            var uv = uvRect;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            for (int y = 0; y <= rows; y++)
            {
                float v = uv.y + uv.height * ((float)y / rows);
                for (int x = 0; x <= columns; x++)
                {
                    vertex.position = posedPoints[y * (columns + 1) + x];
                    vertex.uv0 = new Vector2(uv.x + uv.width * ((float)x / columns), v);
                    helper.AddVert(vertex);
                }
            }

            int stride = columns + 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int bottomLeft = y * stride + x;
                    int topLeft = bottomLeft + stride;
                    // Match RawImage's clockwise UI winding; each cell shares its corners.
                    helper.AddTriangle(bottomLeft, topLeft, topLeft + 1);
                    helper.AddTriangle(topLeft + 1, bottomLeft + 1, bottomLeft);
                }
            }
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        static bool IsFinite(Rect value) => IsFinite(value.x) && IsFinite(value.y)
            && IsFinite(value.width) && IsFinite(value.height)
            && IsFinite(value.xMax) && IsFinite(value.yMax);
    }
}
