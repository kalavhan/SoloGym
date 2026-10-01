using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>The approved PixelLab catalog; legacy AutoSprite viewport assets are separate.</summary>
    public static class PixelLabRoster
    {
        [Serializable] public sealed class Entry
        {
            public string id, resource, en, es;
            public Vector2 feet;
            [NonSerialized] public Sprite Sprite;
        }
        [Serializable] sealed class Data { public Entry[] characters; }
        static readonly Dictionary<string, Entry> entries = Load();
        static Dictionary<string, Entry> Load()
        {
            var result = new Dictionary<string, Entry>();
            try
            {
                var asset = Resources.Load<TextAsset>("Characters/PixelLabR1/catalog");
                var data = asset == null ? null : JsonUtility.FromJson<Data>(asset.text);
                if (data?.characters == null) return result;
                foreach (var c in data.characters)
                {
                    if (c == null || !ValidId(c.id) || c.resource != "Characters/PixelLabR1/" + c.id || result.ContainsKey(c.id)) continue;
                    var sprite = Resources.Load<Sprite>(c.resource);
                    if (sprite == null || sprite.rect.width != 256 || sprite.rect.height != 256 ||
                        c.feet.x <= 0 || c.feet.x >= 256 || c.feet.y < 0 || c.feet.y >= 256) continue;
                    c.Sprite = sprite; result.Add(c.id, c);
                }
            }
            catch (ArgumentException) { /* Missing catalog/art remains unavailable, never replaced by generated art. */ }
            return result;
        }
        static bool ValidId(string id)
        {
            foreach (var gender in new[] { "male", "female" })
                foreach (var body in new[] { "skinny", "medium", "fat", "muscular" })
                    if (id == gender + "-" + body) return true;
            return false;
        }
        public static bool TryFind(string id, out Entry entry) => entries.TryGetValue(id ?? "", out entry);
        public static bool Place(Image image, string id, Vector2 feetPosition, float sourceScale)
        {
            if (!TryFind(id, out var entry)) { image.sprite = null; image.enabled = false; return false; }
            image.enabled = true; image.sprite = entry.Sprite; image.color = Color.white;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = entry.feet / 256f; rect.anchoredPosition = new Vector2(feetPosition.x, -feetPosition.y);
            rect.sizeDelta = Vector2.one * (256 * sourceScale); return true;
        }
    }
}
