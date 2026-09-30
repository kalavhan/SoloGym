using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoloGym.UI
{
    /// <summary>Authored static appearances; body IDs are cosmetic, never training inputs.</summary>
    public static class PixelCharacterCatalog
    {
        [Serializable] public sealed class Entry
        {
            public string id, presentation, body, resource;
            public int width, height;
            public Vector2 feet;
        }
        [Serializable] sealed class Data { public Entry[] characters; }
        static Entry[] entries;
        static IReadOnlyList<Entry> view;
        public static IReadOnlyList<Entry> Entries
        {
            get
            {
                if (entries == null)
                {
                    var json = Resources.Load<TextAsset>("Characters/BarbarianR1/catalog");
                    if (json == null) throw new InvalidOperationException("Missing Barbarian viewport catalog.");
                    entries = JsonUtility.FromJson<Data>(json.text).characters;
                    if (entries == null || entries.Length == 0) throw new InvalidOperationException("Empty Barbarian viewport catalog.");
                    var ids = new HashSet<string>();
                    foreach (var entry in entries)
                        if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !ids.Add(entry.id)
                            || string.IsNullOrWhiteSpace(entry.resource) || entry.width <= 0 || entry.height <= 0
                            || entry.feet.x < 0 || entry.feet.x > entry.width || entry.feet.y < 0 || entry.feet.y > entry.height)
                            throw new InvalidOperationException("Invalid Barbarian viewport entry.");
                    view = Array.AsReadOnly(entries);
                }
                return view;
            }
        }
        public static bool TryFind(string id, out Entry found)
        {
            foreach (var entry in Entries) if (entry.id == id) { found = entry; return true; }
            found = null; return false;
        }
        // The same framing envelope is used for all eight appearances, including narrow bodies.
        public static Vector2 Envelope
        {
            get
            {
                float halfWidth = 0, height = 0;
                foreach (var entry in Entries)
                {
                    halfWidth = Mathf.Max(halfWidth, entry.feet.x, entry.width - entry.feet.x);
                    height = Mathf.Max(height, entry.height - entry.feet.y);
                }
                return new Vector2(halfWidth * 2, height);
            }
        }
    }
}
