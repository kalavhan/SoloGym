using System;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    [Serializable] public sealed class BossEntry { public string id; public bool art_ready; public TrainingText name, stands_for; }
    [Serializable] public sealed class BossLine { public string key; public TrainingText text; }
    [Serializable] public sealed class BossAnimationMap { public string exercise_id, animation; }
    [Serializable] public sealed class BossMockery { public string animation; public TrainingText text; }
    [Serializable] public sealed class BossContentData { public int version; public BossEntry[] bosses; public BossLine[] lines; public BossAnimationMap[] animations; public BossMockery[] mockery; }

    /// <summary>Boss names, lines and exercise-to-animation keys. All of it lives in data/game/content/bosses.json.</summary>
    public sealed class BossContent
    {
        public const string ChallengeLine = "challenge", LastRepLine = "last_rep", DefeatedLine = "defeated", WonLine = "won";
        readonly BossContentData data;
        public BossContent(BossContentData source) { data = source ?? throw new ArgumentNullException(nameof(source)); Validate(data); }
        public static BossContent Parse(string json) => new BossContent(JsonUtility.FromJson<BossContentData>(json));
        public static BossContent Load()
        {
            var asset = Resources.Load<TextAsset>("Game/Bosses");
            return asset == null ? null : Parse(asset.text);
        }
        public static void Validate(BossContentData d)
        {
            if (d.version != 1 || d.bosses == null || d.bosses.Length == 0 || d.lines == null || d.animations == null) throw new ArgumentException("Invalid boss content.");
            if (d.bosses.Any(b => string.IsNullOrEmpty(b.id) || b.name == null || string.IsNullOrEmpty(b.name.en) || string.IsNullOrEmpty(b.name.es) || b.stands_for == null)) throw new ArgumentException("Invalid boss entry.");
            if (d.bosses.Select(b => b.id).Distinct().Count() != d.bosses.Length) throw new ArgumentException("Duplicate boss id.");
            foreach (var key in new[] { ChallengeLine, LastRepLine, DefeatedLine, WonLine })
            {
                var line = d.lines.FirstOrDefault(l => l.key == key);
                if (line?.text == null || string.IsNullOrEmpty(line.text.en) || string.IsNullOrEmpty(line.text.es)) throw new ArgumentException("Missing boss line: " + key);
            }
        }
        /// <summary>The boss for a session. Only bosses with finished art are summoned; the choice is stable per session.</summary>
        public BossEntry Pick(string sessionId)
        {
            var ready = data.bosses.Where(b => b.art_ready).ToArray();
            if (ready.Length == 0) ready = data.bosses;
            int hash = 0; foreach (char c in sessionId ?? "") hash = (hash * 31 + c) & 0x7fffffff;
            return ready[hash % ready.Length];
        }
        public BossEntry Find(string id) => data.bosses.FirstOrDefault(b => b.id == id);
        public string Line(string key, string language) => data.lines.FirstOrDefault(l => l.key == key)?.text.Get(language) ?? "";
        /// <summary>The boss's lazy take on the current exercise for the VS card, e.g. "3x10 jacks that forgot to jump".</summary>
        public string Mockery(string exerciseId, string dose, string exerciseName, string language)
        {
            if (data.mockery == null || data.mockery.Length == 0) return "";
            string key = Animation(exerciseId);
            var entry = data.mockery.FirstOrDefault(m => key != null && m.animation == key) ?? data.mockery.FirstOrDefault(m => m.animation == "*");
            string name = string.IsNullOrEmpty(exerciseName) ? "" : char.ToLowerInvariant(exerciseName[0]) + exerciseName.Substring(1);
            return entry?.text == null ? "" : entry.text.Get(language).Replace("{dose}", dose ?? "").Replace("{exercise}", name).Trim();
        }
        /// <summary>Animation key for an exercise, or null when it has no animation yet.</summary>
        public string Animation(string exerciseId) => data.animations.FirstOrDefault(a => a.exercise_id == exerciseId)?.animation;
    }
}
