using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SoloGym.Training;
using UnityEngine;

namespace SoloGym
{
    /// <summary>
    /// Where the journal, readiness gate and dungeon obtain prescriptions.
    /// Keys are "profile:minutes:readiness:supervised" (+ ":difficulty" for dungeon variants).
    /// </summary>
    public interface ITrainingPlans
    {
        /// <summary>Saved-data context tag ("review-fixtures-only" or "account").</summary>
        string Context { get; }
        TrainingProfile[] Profiles { get; }
        TrainingProfile Profile(string id);
        int[] Durations { get; }
        TrainingPlan Session(string key);
        JournalOption[] Options(string key);
        BossVariant Variant(string key);
    }

    public static class TrainingKeys
    {
        public static string Make(string profile, int minutes, string readiness, bool supervisedBit) => profile + ":" + minutes + ":" + readiness + ":" + (supervisedBit ? "1" : "0");
        public static bool TryParse(string key, out string profile, out int minutes, out string readiness, out bool supervised, out string difficulty)
        {
            profile = readiness = difficulty = null; minutes = 0; supervised = false;
            var parts = (key ?? "").Split(':');
            if (parts.Length != 4 && parts.Length != 5) return false;
            profile = parts[0]; readiness = parts[2]; supervised = parts[3] == "1";
            if (parts.Length == 5) difficulty = parts[4];
            return int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minutes) && (parts[3] == "0" || parts[3] == "1");
        }
    }

    /// <summary>The explicitly fictional review fixtures (unchanged behavior for review captures and smokes).</summary>
    public sealed class FixtureTrainingPlans : ITrainingPlans
    {
        public const string FixtureContext = "review-fixtures-only";
        readonly TrainingCatalog catalog;
        readonly JournalOptions journalOptions;
        BossCatalog bosses;
        public JournalOptions JournalOptions => journalOptions;
        public string Context => FixtureContext;
        public TrainingProfile[] Profiles => catalog.profiles;
        public int[] Durations { get; } = { 15, 25, 40 };
        public FixtureTrainingPlans(TrainingCatalog training, JournalOptions options, BossCatalog dungeon = null)
        {
            catalog = training ?? throw new ArgumentNullException(nameof(training));
            journalOptions = options ?? new JournalOptions { options = Array.Empty<JournalOption>(), mobility = Array.Empty<JournalMobility>() };
            bosses = dungeon;
            if (catalog.profiles == null || catalog.profiles.Length == 0 || catalog.entries == null)
                throw new ArgumentException("Training review catalog is incomplete.");
        }
        public TrainingProfile Profile(string id) => catalog.profiles.FirstOrDefault(p => p.id == id);
        public TrainingPlan Session(string key) => catalog.entries.FirstOrDefault(e => e.key == key)?.session;
        public JournalOption[] Options(string key) => (journalOptions.options ?? Array.Empty<JournalOption>()).Where(o => o.key == key).ToArray();
        public BossVariant Variant(string key)
        {
            if (bosses == null)
            {
                var asset = Resources.Load<TextAsset>("Training/BossOptions");
                bosses = asset == null ? new BossCatalog { entries = Array.Empty<BossVariant>() } : JsonUtility.FromJson<BossCatalog>(asset.text);
            }
            return bosses.entries?.FirstOrDefault(v => v.key == key);
        }
        public void UseBosses(BossCatalog dungeon) { if (dungeon != null) bosses = dungeon; }
    }

    /// <summary>
    /// A signed-in person's prescriptions, generated on-device from their saved setup by
    /// <see cref="TrainingEngine"/>. One catalog profile per template ("u.foundation_a"…).
    /// </summary>
    public sealed class LiveTrainingPlans : ITrainingPlans
    {
        public const string AccountContext = "account";
        public const string AccountProfile = "u";
        public static readonly int[] SessionDurations = { 15, 25, 40, 60 };
        readonly TrainingEngine engine;
        readonly TrainingInput input;
        readonly TrainingProfile[] profiles;
        readonly TrainingProfile account;
        readonly Dictionary<string, TrainingPlan> sessions = new Dictionary<string, TrainingPlan>();
        readonly Dictionary<string, JournalOption[]> options = new Dictionary<string, JournalOption[]>();
        readonly Dictionary<string, BossVariant> variants = new Dictionary<string, BossVariant>();
        public string Context => AccountContext;
        public TrainingProfile[] Profiles => profiles;
        public int[] Durations => SessionDurations;
        public TrainingInput Input => input.Copy();
        public TrainingEngine Engine => engine;

        public LiveTrainingPlans(TrainingEngine source, TrainingInput setup)
        {
            engine = source ?? throw new ArgumentNullException(nameof(source));
            input = (setup ?? throw new ArgumentNullException(nameof(setup))).Copy();
            input.readiness = "ready"; input.teen_supervision_available = false; input.professional_guidance_needed = false;
            if (!SessionDurations.Contains(input.session_minutes)) input.session_minutes = Nearest(input.session_minutes);
            engine.Validate(input);
            var names = EquipmentNames();
            account = new TrainingProfile { id = AccountProfile, teen = input.IsTeen, name = new TrainingText { en = "My training", es = "Mi entrenamiento" }, equipment = names };
            profiles = engine.Templates.templates.Select(t => new TrainingProfile
            {
                id = AccountProfile + "." + t.id, teen = input.IsTeen,
                name = new TrainingText { en = t.name.en, es = t.name.es }, equipment = names
            }).ToArray();
        }

        public static int Nearest(int minutes) => SessionDurations.OrderBy(d => Math.Abs(d - minutes)).ThenBy(d => d).First();
        TrainingText[] EquipmentNames() => (input.equipment ?? Array.Empty<string>()).Select(id => new TrainingText { en = id.Replace('_', ' '), es = id.Replace('_', ' ') }).ToArray();
        public static string TemplateOf(string profileId) => profileId != null && profileId.StartsWith(AccountProfile + ".", StringComparison.Ordinal) ? profileId.Substring(AccountProfile.Length + 1) : null;
        public TrainingProfile Profile(string id) => id == AccountProfile ? account : profiles.FirstOrDefault(p => p.id == id);

        TrainingInput Contextual(string key, out string template)
        {
            template = null;
            if (!TrainingKeys.TryParse(key, out var profile, out int minutes, out var readiness, out bool supervised, out var difficulty)) return null;
            template = TemplateOf(profile);
            if (template == null || Profile(profile) == null || !SessionDurations.Contains(minutes)) return null;
            var p = input.Copy();
            p.session_minutes = minutes; p.readiness = readiness;
            p.teen_supervision_available = input.IsTeen && supervised;
            if (difficulty != null) p.difficulty = difficulty;
            try { engine.Validate(p); } catch (ArgumentException) { return null; }
            return p;
        }

        public TrainingPlan Session(string key)
        {
            if (sessions.TryGetValue(key ?? "", out var cached)) return cached;
            var p = Contextual(key, out var template);
            if (p == null) return null;
            if (TrainingKeys.TryParse(key, out _, out _, out _, out _, out var d) && d != null) return null; // journal keys carry no difficulty
            p.difficulty = "medium";
            var plan = engine.GenerateSession(p, template);
            sessions[key] = plan; return plan;
        }

        public JournalOption[] Options(string key)
        {
            if (options.TryGetValue(key ?? "", out var cached)) return cached;
            var plan = Session(key);
            var p = Contextual(key, out _);
            var result = plan == null || p == null ? Array.Empty<JournalOption>() : engine.SwapOptions(p, plan, key);
            options[key] = result; return result;
        }

        public BossVariant Variant(string key)
        {
            if (variants.TryGetValue(key ?? "", out var cached)) return cached;
            var p = Contextual(key, out var template);
            if (p == null || !TrainingKeys.TryParse(key, out _, out _, out _, out _, out var difficulty) || difficulty == null) return null;
            var plan = engine.GenerateSession(p, template);
            var variant = new BossVariant { key = key, plan = plan, options = engine.SwapOptions(p, plan, key) };
            variants[key] = variant; return variant;
        }

        /// <summary>Planned journal days (date → catalog profile) for the user's chosen weekdays.</summary>
        public IEnumerable<KeyValuePair<DateTime, string>> Schedule(int[] weekdays, DateTime from, DateTime through)
        {
            var days = (weekdays ?? Array.Empty<int>()).Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d).ToArray();
            var templates = engine.WeekTemplates(input, days);
            for (var day = from.Date; day <= through.Date; day = day.AddDays(1))
            {
                int weekday = ((int)day.DayOfWeek + 6) % 7, index = Array.IndexOf(days, weekday);
                if (index >= 0) yield return new KeyValuePair<DateTime, string>(day, AccountProfile + "." + templates[index]);
            }
        }

        /// <summary>The session type for an extra routine created on a given day.</summary>
        public string DefaultProfileFor(DateTime day, int[] weekdays)
        {
            foreach (var pair in Schedule(weekdays, day, day)) return pair.Value;
            return AccountProfile + "." + (input.environment == "outdoor" ? "aerobic_base" : input.goal == "mobility" ? "mobility_reset" : "aerobic_base");
        }
    }
}
