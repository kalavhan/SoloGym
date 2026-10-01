using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoloGym.UI
{
    /// <summary>Portable authoring layout. Validate every entry before changing the live room.</summary>
    public static class PixelHomeLayout
    {
        [Serializable] public sealed class CatalogEntry { public string resource, label, supportObjectId; }
        [Serializable] public sealed class Catalog { public CatalogEntry[] objects; }
        [Serializable] public sealed class CharacterPlacement { public Vector2 position; public float scale; public bool visible; }
        [Serializable] public sealed class Document
        {
            public string format, roomId;
            public int version;
            public Vector2 referenceSize;
            public PixelRoomObject.State[] objects;
            public CharacterPlacement character;
        }
        public static Catalog ObjectCatalog => JsonUtility.FromJson<Catalog>(
            Resources.Load<TextAsset>("Rooms/RefugeR1/objects").text);
        public static Document Capture(PixelHomeRoomReview review)
        {
            var props = review.Room.GetComponentsInChildren<PixelRoomObject>(true);
            Array.Sort(props, (a, b) => string.CompareOrdinal(a.ObjectId, b.ObjectId));
            var states = new PixelRoomObject.State[props.Length];
            for (int i = 0; i < props.Length; i++) states[i] = props[i].PlacementState();
            return new Document { format = "sologym-home-layout", version = 1, roomId = review.Room.RoomId,
                referenceSize = review.Room.ReferenceSize, objects = states,
                character = new CharacterPlacement { position = review.CharacterPosition, scale = review.CharacterScale, visible = review.ShowCharacter } };
        }
        public static string Export(PixelHomeRoomReview review) => JsonUtility.ToJson(Capture(review), true);
        static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        public static void Apply(PixelHomeRoomReview review, string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 200000) throw new ArgumentException("Missing or oversized room layout.");
            var data = JsonUtility.FromJson<Document>(json);
            if (data == null || data.format != "sologym-home-layout" || data.version != 1
                || data.roomId != review.Room.RoomId || data.referenceSize != review.Room.ReferenceSize || data.objects == null)
                throw new ArgumentException("Unsupported layout format, version or room.");
            var props = review.Room.GetComponentsInChildren<PixelRoomObject>(true);
            if (data.objects.Length != props.Length) throw new ArgumentException("A layout must contain every current room object exactly once.");
            var byId = new Dictionary<string, PixelRoomObject>();
            foreach (var prop in props) byId.Add(prop.ObjectId, prop);
            var seen = new HashSet<string>(); var changes = new List<Action>();
            foreach (var state in data.objects)
            {
                if (state == null || state.version != 2 || string.IsNullOrEmpty(state.objectId)
                    || !seen.Add(state.objectId) || !byId.TryGetValue(state.objectId, out var prop))
                    throw new ArgumentException("Unknown, duplicate or unsupported layout object.");
                changes.Add(prop.PrepareRestore(JsonUtility.ToJson(state)));
            }
            var c = data.character;
            if (c == null || !Finite(c.position.x) || !Finite(c.position.y) || !Finite(c.scale) || c.scale < .25f || c.scale > 3
                || c.position.x - 150 * c.scale < 0 || c.position.x + 150 * c.scale > data.referenceSize.x
                || c.position.y - 500 * c.scale < 0 || c.position.y > data.referenceSize.y)
                throw new ArgumentException("Character framing must fit the room with uniform scale 0.25–3.");
            foreach (var change in changes) change();
            review.SetCharacterPlacement(c.position, c.scale); review.SetCharacterVisible(c.visible);
            review.Relayout();
        }
    }
}
