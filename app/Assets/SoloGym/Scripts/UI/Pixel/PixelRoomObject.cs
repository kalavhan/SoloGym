using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>A static, slot-placed room item. Variants change artwork, never the placement contract.</summary>
    public sealed class PixelRoomObject : MonoBehaviour
    {
        [Serializable] public sealed class Variant { public string id, resource; }
        [Serializable] public sealed class Definition
        {
            public string id, roomId, layer;
            public string[] supportedSlots;
            public int drawOrder;
            public Vector2 sourceSize, pivot;
            public float sourceScale;
            public Vector2[] footprint;
            public Variant[] variants;
        }
        [Serializable] public sealed class State
        {
            public int version;
            public string roomId, objectId, slotId, variantId;
            public bool visible;
        }
        public Image Artwork { get; private set; }
        public string ObjectId => definition.id;
        public string SlotId => current.slotId;
        public string VariantId => current.variantId;
        public bool Visible => current.visible;
        public int DrawOrder => definition.drawOrder;
        public Vector2 Pivot => definition.pivot;
        public Vector2 DisplaySize => definition.sourceSize * definition.sourceScale;
        Definition definition;
        State current;
        PixelHomeRoom room;

        public static PixelRoomObject Create(PixelHomeRoom room, string definitionResource, string savedJson = null)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            var source = Resources.Load<TextAsset>(definitionResource);
            if (source == null) throw new ArgumentException("Missing room-object definition: " + definitionResource);
            var data = JsonUtility.FromJson<Definition>(source.text);
            ValidateDefinition(data, room);
            var state = savedJson == null ? new State { version = 1, roomId = room.RoomId, objectId = data.id,
                slotId = data.supportedSlots[0], variantId = data.variants[0].id, visible = true } : ParseState(savedJson);
            ValidateState(state, data, room, null);
            var sprite = LoadVariant(data, state.variantId);
            var parent = data.layer == "behind-character" ? room.BackObjects : room.Foreground;
            var root = new GameObject(data.id, typeof(RectTransform), typeof(Image)); root.transform.SetParent(parent, false);
            var item = root.AddComponent<PixelRoomObject>(); item.definition = data; item.room = room;
            item.Artwork = root.GetComponent<Image>(); item.Artwork.raycastTarget = false; item.Artwork.color = Color.white;
            item.Apply(state, sprite); item.SortSiblings(); return item;
        }
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static bool Finite(Vector2 v) => Finite(v.x) && Finite(v.y);
        static void ValidateDefinition(Definition d, PixelHomeRoom room)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.id) || d.roomId != room.RoomId
                || (d.layer != "behind-character" && d.layer != "foreground")
                || !Finite(d.sourceSize) || d.sourceSize.x <= 0 || d.sourceSize.y <= 0
                || !Finite(d.sourceScale) || d.sourceScale <= 0 || !Finite(d.pivot)
                || d.pivot.x < 0 || d.pivot.x > 1 || d.pivot.y < 0 || d.pivot.y > 1
                || d.supportedSlots == null || d.supportedSlots.Length == 0 || d.variants == null || d.variants.Length == 0
                || d.footprint == null || d.footprint.Length != 4)
                throw new ArgumentException("Invalid room-object identity, placement, layer or artwork contract.");
            var slots = new HashSet<string>();
            foreach (string slot in d.supportedSlots)
            { if (string.IsNullOrWhiteSpace(slot) || !slots.Add(slot)) throw new ArgumentException("Duplicate/empty room slot."); room.AnchorPoint(slot); }
            var variants = new HashSet<string>();
            foreach (var variant in d.variants)
                if (variant == null || string.IsNullOrWhiteSpace(variant.id) || string.IsNullOrWhiteSpace(variant.resource) || !variants.Add(variant.id))
                    throw new ArgumentException("Variant IDs must be unique and name an authored sprite.");
            float sign = 0;
            for (int i = 0; i < 4; i++)
            {
                var p = d.footprint[i];
                if (!Finite(p) || p.x < 0 || p.y < 0 || p.x > d.sourceSize.x || p.y > d.sourceSize.y)
                    throw new ArgumentException("Footprint must use finite coordinates inside the source canvas.");
                var a = d.footprint[(i + 1) % 4] - p; var b = d.footprint[(i + 2) % 4] - d.footprint[(i + 1) % 4];
                float cross = a.x * b.y - a.y * b.x;
                if (Mathf.Abs(cross) < .001f || (sign != 0 && Mathf.Sign(cross) != sign))
                    throw new ArgumentException("Footprint must be a non-degenerate convex quadrilateral.");
                sign = Mathf.Sign(cross);
            }
        }
        static State ParseState(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Missing room-object state.");
            return JsonUtility.FromJson<State>(json);
        }
        static void ValidateState(State state, Definition data, PixelHomeRoom room, PixelRoomObject self)
        {
            if (state == null || state.version != 1 || state.roomId != room.RoomId || state.objectId != data.id
                || Array.IndexOf(data.supportedSlots, state.slotId) < 0
                || !Array.Exists(data.variants, variant => variant.id == state.variantId))
                throw new ArgumentException("Room-object state has an unsupported version, identity, slot or variant.");
            foreach (var other in room.GetComponentsInChildren<PixelRoomObject>(true))
                if (other != self && (other.ObjectId == data.id || other.SlotId == state.slotId))
                    throw new ArgumentException("Room object identity or placement slot is already occupied.");
            Vector2 anchor = room.AnchorPoint(state.slotId);
            Vector2 topLeft = anchor - new Vector2(data.pivot.x, 1 - data.pivot.y) * data.sourceSize * data.sourceScale;
            foreach (var p in data.footprint)
            {
                Vector2 point = topLeft + p * data.sourceScale;
                if (point.x < 0 || point.y < 0 || point.x > room.ReferenceSize.x || point.y > room.ReferenceSize.y)
                    throw new ArgumentException("The footprint would extend outside this room.");
            }
        }
        static Sprite LoadVariant(Definition data, string id)
        {
            var variant = Array.Find(data.variants, entry => entry.id == id);
            var sprite = Resources.Load<Sprite>(variant.resource);
            if (sprite == null || sprite.rect.size != data.sourceSize || sprite.pivot != data.pivot * data.sourceSize)
                throw new ArgumentException("Variant sprite must preserve the source canvas and pivot.");
            return sprite;
        }
        void Apply(State state, Sprite sprite)
        {
            current = state; Artwork.sprite = sprite; Artwork.enabled = state.visible;
            PixelHomeRoom.Place((RectTransform)transform, room.AnchorPoint(state.slotId), DisplaySize, definition.pivot);
        }
        public string SaveState() => JsonUtility.ToJson(current);
        public void RestoreState(string json)
        {
            var state = ParseState(json);
            // Validate and resolve artwork before changing the live item, so invalid saves cannot corrupt it.
            ValidateState(state, definition, room, this); var sprite = LoadVariant(definition, state.variantId);
            Apply(state, sprite);
        }
        public void SetVisible(bool visible) { current.visible = visible; Artwork.enabled = visible; }
        public void SetVariant(string id)
        {
            var next = ParseState(SaveState()); next.variantId = id;
            ValidateState(next, definition, room, this); var sprite = LoadVariant(definition, id); Apply(next, sprite);
        }
        public Vector2[] FootprintInRoom()
        {
            var topLeft = room.AnchorPoint(SlotId) - new Vector2(Pivot.x, 1 - Pivot.y) * DisplaySize;
            var points = new Vector2[definition.footprint.Length];
            for (int i = 0; i < points.Length; i++) points[i] = topLeft + definition.footprint[i] * definition.sourceScale;
            return points;
        }
        void SortSiblings()
        {
            var items = new List<PixelRoomObject>();
            foreach (Transform sibling in transform.parent)
                if (sibling.TryGetComponent<PixelRoomObject>(out var item)) items.Add(item);
            items.Sort((a, b) => a.DrawOrder != b.DrawOrder ? a.DrawOrder.CompareTo(b.DrawOrder) : string.CompareOrdinal(a.ObjectId, b.ObjectId));
            for (int i = 0; i < items.Count; i++) items[i].transform.SetSiblingIndex(i);
        }
    }
}
