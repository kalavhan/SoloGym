using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Fits live glyph geometry to the measured reference box, not to a screenshot.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class ReferenceTextLayout : BaseMeshEffect
    {
        public bool FitWidth = true;
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            UIVertex vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                minX = Mathf.Min(minX, vertex.position.x); maxX = Mathf.Max(maxX, vertex.position.x);
                minY = Mathf.Min(minY, vertex.position.y); maxY = Mathf.Max(maxY, vertex.position.y);
            }
            if (maxX - minX < .01f || maxY - minY < .01f) return;
            Rect target = ((RectTransform)transform).rect;
            float scaleY = target.height / (maxY - minY);
            float scaleX = FitWidth ? target.width / (maxX - minX) : Mathf.Min(scaleY, target.width / (maxX - minX));
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.position = new Vector3(target.xMin + (vertex.position.x - minX) * scaleX,
                    target.yMax + (vertex.position.y - maxY) * scaleY, vertex.position.z);
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
