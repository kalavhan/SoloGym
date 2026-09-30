using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>One uniformly fitted room plane. Architecture, exterior and occupants remain independent.</summary>
    public sealed class PixelHomeRoom : MonoBehaviour
    {
        [Serializable] public sealed class Anchor { public string id; public Vector2 point; }
        [Serializable] public sealed class Definition
        {
            public string id, architecture, exterior;
            public Vector2 referenceSize;
            public Rect exteriorRect;
            public Anchor[] anchors;
        }

        public string RoomId { get; private set; }
        public Vector2 ReferenceSize { get; private set; }
        public RectTransform Plane { get; private set; }
        public RectTransform BackObjects { get; private set; }
        public RectTransform Objects { get; private set; }
        public RectTransform Foreground { get; private set; }
        public Image Architecture { get; private set; }
        public Image Exterior { get; private set; }
        public float FitScale { get; private set; }
        readonly Dictionary<string, Vector2> anchors = new Dictionary<string, Vector2>();

        public static PixelHomeRoom Create(Transform parent, string definitionResource = "Rooms/RefugeR1/room")
        {
            var json = Resources.Load<TextAsset>(definitionResource);
            if (json == null) throw new InvalidOperationException("Missing Home room definition: " + definitionResource);
            var data = JsonUtility.FromJson<Definition>(json.text);
            Validate(data);
            var architecture = LoadSprite(data.architecture);
            var exterior = LoadSprite(data.exterior);
            if (architecture.rect.size != data.referenceSize)
                throw new InvalidOperationException("Room architecture must match its reference canvas.");

            var root = Rect("Home room", parent);
            Stretch(root);
            var room = root.gameObject.AddComponent<PixelHomeRoom>();
            room.RoomId = data.id; room.ReferenceSize = data.referenceSize;
            foreach (var anchor in data.anchors) room.anchors.Add(anchor.id, anchor.point);
            room.Plane = Rect("Room coordinate plane", root);
            room.Plane.anchorMin = room.Plane.anchorMax = room.Plane.pivot = new Vector2(.5f, .5f);
            room.Plane.sizeDelta = data.referenceSize;
            room.Exterior = Art("Exterior — replaceable behind window", room.Plane, exterior);
            Place(room.Exterior.rectTransform, data.exteriorRect.position, data.exteriorRect.size, new Vector2(0, 1));
            room.Exterior.preserveAspect = true;
            room.Architecture = Art("Architecture — empty shell only", room.Plane, architecture);
            Stretch(room.Architecture.rectTransform);
            room.BackObjects = Rect("Independent furniture behind character", room.Plane); Stretch(room.BackObjects);
            room.Objects = Rect("Independent occupants and furniture", room.Plane); Stretch(room.Objects);
            room.Foreground = Rect("Independent foreground objects", room.Plane); Stretch(room.Foreground);
            room.RefreshLayout();
            return room;
        }

        static void Validate(Definition data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.id) || !Finite(data.referenceSize)
                || data.referenceSize.x <= 0 || data.referenceSize.y <= 0 || data.anchors == null)
                throw new InvalidOperationException("Invalid Home room definition.");
            var ids = new HashSet<string>();
            foreach (var anchor in data.anchors)
                if (anchor == null || string.IsNullOrWhiteSpace(anchor.id) || !ids.Add(anchor.id)
                    || !Finite(anchor.point) || anchor.point.x < 0 || anchor.point.y < 0
                    || anchor.point.x > data.referenceSize.x || anchor.point.y > data.referenceSize.y)
                    throw new InvalidOperationException("Room anchors require unique IDs and finite in-canvas coordinates.");
            var r = data.exteriorRect;
            if (!Finite(r.position) || !Finite(r.size) || r.width <= 0 || r.height <= 0
                || r.xMin < 0 || r.yMin < 0 || r.xMax > data.referenceSize.x || r.yMax > data.referenceSize.y)
                throw new InvalidOperationException("Exterior placement must fit inside the room canvas.");
        }

        static bool Finite(Vector2 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y);
        static Sprite LoadSprite(string resource)
        {
            var sprite = Resources.Load<Sprite>(resource);
            if (sprite == null) throw new InvalidOperationException("Missing Home sprite: " + resource);
            return sprite;
        }
        public Vector2 AnchorPoint(string id)
        {
            if (!anchors.TryGetValue(id, out var point)) throw new ArgumentException("Unknown room anchor: " + id, nameof(id));
            return point;
        }
        /// <summary>Places a child in source-art pixels, measured right/down from the room's upper-left.</summary>
        public static void Place(RectTransform child, Vector2 point, Vector2 size, Vector2 pivot)
        {
            child.anchorMin = child.anchorMax = new Vector2(0, 1); child.pivot = pivot;
            child.anchoredPosition = new Vector2(point.x, -point.y); child.sizeDelta = size;
        }
        public void SetExterior(Sprite sprite)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            // Preserve placement/identity. A new theme must retain the same aspect ratio.
            if (Mathf.Abs(sprite.rect.width / sprite.rect.height - Exterior.sprite.rect.width / Exterior.sprite.rect.height) > .001f)
                throw new ArgumentException("Exterior variants must preserve the authored aspect ratio.", nameof(sprite));
            Exterior.sprite = sprite;
        }
        public void RefreshLayout()
        {
            if (Plane == null) return;
            var size = ((RectTransform)transform).rect.size;
            FitScale = Mathf.Max(0, Mathf.Min(size.x / ReferenceSize.x, size.y / ReferenceSize.y));
            Plane.localScale = new Vector3(FitScale, FitScale, 1);
            // Pixel-adjusted local vertices depend on ancestor scale. Rebuild them now,
            // rather than waiting for an unrelated prop visibility change to dirty the Canvas.
            foreach (var graphic in Plane.GetComponentsInChildren<Graphic>(true)) graphic.SetVerticesDirty();
        }
        void OnRectTransformDimensionsChange() => RefreshLayout();
        static Image Art(string name, Transform parent, Sprite sprite)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false; image.color = Color.white;
            return image;
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
