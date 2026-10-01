using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    [Serializable] public sealed class JournalSwap { public string slot, exercise; }
    [Serializable] public sealed class JournalEntry
    {
        public string id, date, title = "", status = "planned", profile;
        public int minutes = 25;
        public JournalSwap[] swaps = Array.Empty<JournalSwap>();
        public TrainingPlan completedPlan;
        public BossSession session;
    }
    [Serializable] public sealed class JournalReview
    {
        public string entryId, date, reviewedUtc;
        public TrainingPlan plan;
    }
    [Serializable] public sealed class JournalDocument
    {
        public int version = 1;
        public string context = "review-fixtures-only", profileId;
        public JournalEntry[] entries;
        public JournalReview lastReview;
        public BossSession activeSession;
    }
    [Serializable] public sealed class JournalOption { public string key; public TrainingBlock block; public int secondsDelta; }
    [Serializable] public sealed class JournalMobility { public string profileId; public TrainingPlan plan; }
    [Serializable] public sealed class JournalOptions { public JournalOption[] options; public JournalMobility[] mobility; }

    public interface IJournalStorage { string Read(); void Write(string json); }
    public sealed class JournalFileStorage : IJournalStorage
    {
        public string Path { get; }
        public JournalFileStorage(string path) { Path = System.IO.Path.GetFullPath(path); }
        public string Read()
        {
            if (!File.Exists(Path)) return null;
            if (new FileInfo(Path).Length > 2000000) throw new IOException("Journal file is oversized.");
            return File.ReadAllText(Path);
        }
        public void Write(string json)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temporary = Path + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(Path)) File.Replace(temporary, Path, null);
            else File.Move(temporary, Path);
        }
    }

    /// <summary>
    /// Local workout journal. With <see cref="FixtureTrainingPlans"/> it is the explicitly fictional
    /// review journal; with <see cref="LiveTrainingPlans"/> it holds the signed-in person's own plan
    /// and history. Missed days never create catch-up work; extra reps never add rewards.
    /// </summary>
    public sealed class WorkoutJournal
    {
        readonly ITrainingPlans plans;
        readonly IJournalStorage storage;
        JournalDocument document;
        string preparedKey, preparedEntryId;
        TrainingPlan reviewedGatePlan;
        public string ProfileId { get; }
        public ITrainingPlans Plans => plans;
        public bool IsLive => plans.Context == LiveTrainingPlans.AccountContext;
        public bool IsTeen => plans.Profile(ProfileId).teen;
        public int[] Durations => (int[])plans.Durations.Clone();
        public DateTime Today { get; }
        public DateTime WeekStart { get; private set; }
        public DateTime SelectedDay { get; private set; }
        public string SelectedId { get; private set; }
        public string Error { get; private set; }
        public bool Loaded => document != null && Error == null;
        public JournalEntry Draft { get; private set; }
        public TrainingController Gate { get; private set; }
        public TrainingPlan PreparedPlan { get; private set; }
        public bool ReadinessAdjusted { get; private set; }
        public bool Reviewed { get; private set; }
        public JournalEntry Selected => Clone(document?.entries.FirstOrDefault(x => x.id == SelectedId));
        public JournalEntry[] Entries => document == null ? Array.Empty<JournalEntry>() : document.entries.Select(x => Clone(x)).ToArray();
        public JournalReview LastReview => Clone(document?.lastReview);
        public BossSession ActiveSession => Clone(document?.activeSession);
        public static T Clone<T>(T value)
        {
            if (value == null) return default;
            var result = JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
            // Unity's inline serializer materializes absent nested classes. Restore optional fields.
            Normalize(result); return result;
        }
        static void Normalize(object value)
        {
            if (value is JournalEntry entry)
            {
                if (string.IsNullOrEmpty(entry.completedPlan?.status)) entry.completedPlan = null;
                if (string.IsNullOrEmpty(entry.session?.id)) entry.session = null;
            }
            if (value is JournalDocument data)
            {
                foreach (var row in data.entries ?? Array.Empty<JournalEntry>()) Normalize(row);
                if (string.IsNullOrEmpty(data.lastReview?.entryId)) data.lastReview = null;
                if (string.IsNullOrEmpty(data.activeSession?.id)) data.activeSession = null;
            }
        }
        public static string Date(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime ParseDate(string value) => DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime Monday(DateTime day) => day.Date.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        public string Key(int minutes, string readiness = "ready", bool supervised = true) => Key(ProfileId, minutes, readiness, supervised);
        public string Key(JournalEntry entry, int minutes, string readiness = "ready", bool supervised = true) => Key(EntryProfile(entry), minutes, readiness, supervised);
        string Key(string profile, int minutes, string readiness, bool supervised) => TrainingKeys.Make(profile, minutes, readiness, IsTeen && supervised);
        /// <summary>The catalog profile (session type) an entry resolves against.</summary>
        public string EntryProfile(JournalEntry entry) => string.IsNullOrEmpty(entry?.profile) ? ProfileId : entry.profile;

        public WorkoutJournal(TrainingCatalog source, JournalOptions edits, IJournalStorage target, string profile, DateTime today)
            : this(new FixtureTrainingPlans(source ?? throw new ArgumentNullException(nameof(source)), edits ?? throw new ArgumentNullException(nameof(edits))), target, profile, today) { }

        public WorkoutJournal(ITrainingPlans source, IJournalStorage target, string profile, DateTime today)
        {
            plans = source ?? throw new ArgumentNullException(nameof(source));
            storage = target ?? throw new ArgumentNullException(nameof(target));
            if (plans.Profile(profile) == null) throw new ArgumentException("Unknown journal profile.");
            ProfileId = profile; Today = today.Date; SelectedDay = Today; WeekStart = Monday(Today);
        }
        public void Load(bool empty = false)
        {
            try
            {
                string text = storage.Read();
                var next = text == null ? Seed(empty) : Deserialize(text);
                bool pruned = IsLive && PruneUnresolvable(next);
                Validate(next); document = next; Error = null;
                if (pruned) storage.Write(JsonUtility.ToJson(next, true));
                SelectDay(Today);
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException || e is FormatException)
            { document = null; Error = e.Message; }
        }
        /// <summary>
        /// Live setup can change (equipment, age group). Planned entries whose saved edits no longer
        /// resolve are dropped; completed/stopped history and an active session are never touched.
        /// </summary>
        bool PruneUnresolvable(JournalDocument data)
        {
            if (data?.entries == null) return false;
            var keep = new List<JournalEntry>(); bool changed = false;
            foreach (var entry in data.entries)
            {
                bool planned = entry != null && entry.status == "planned" && entry.completedPlan == null;
                bool active = data.activeSession != null && entry != null && data.activeSession.entryId == entry.id;
                if (planned && !active)
                {
                    try { Resolve(entry, Key(entry, entry.minutes)); }
                    catch (ArgumentException) { changed = true; continue; }
                }
                keep.Add(entry);
            }
            if (!changed) return false;
            data.entries = keep.ToArray();
            if (data.lastReview != null && !keep.Any(e => e.id == data.lastReview.entryId)) data.lastReview = null;
            return true;
        }
        JournalDocument Seed(bool empty)
        {
            var entries = new List<JournalEntry>();
            if (!empty && plans is FixtureTrainingPlans fixtures)
            {
                var options = fixtures.JournalOptions;
                var monday = Monday(Today);
                var completedDate = monday < Today ? monday : Today.AddDays(-2);
                var missedDate = Today.AddDays(-1);
                entries.Add(new JournalEntry { id = "sample-today", date = Date(Today) });
                entries.Add(new JournalEntry { id = "sample-completed", date = Date(completedDate), status = "completed", minutes = 15,
                    completedPlan = Clone(options.mobility.First(x => x.profileId == ProfileId).plan) });
                entries.Add(new JournalEntry { id = "sample-missed", date = Date(missedDate) });
            }
            return new JournalDocument { profileId = ProfileId, context = plans.Context, entries = entries.ToArray() };
        }
        public string Status(JournalEntry entry) => entry.status == "completed" ? "completed" : entry.status == "stopped" ? "stopped" : ParseDate(entry.date) < Today ? "missed" : "planned";
        public bool CanEdit(JournalEntry entry) => entry != null && Status(entry) == "planned" && document?.activeSession?.entryId != entry.id;
        public bool CanPrepare => Selected != null && Status(Selected) == "planned" && Selected.date == Date(Today) && Draft == null && document?.activeSession == null;
        public void MoveWeek(int delta)
        {
            var next = WeekStart.AddDays(delta * 7);
            if (next.Year < 2000 || next.Year > 2100) return;
            WeekStart = next; SelectDay(next);
        }
        public void SelectDay(DateTime date)
        {
            ClearGate(); SelectedDay = date.Date; WeekStart = Monday(date);
            SelectedId = document?.entries.FirstOrDefault(x => x.date == Date(date))?.id;
        }
        public void Select(string id)
        {
            var entry = document?.entries.FirstOrDefault(x => x.id == id);
            if (entry == null) throw new ArgumentException("Unknown journal entry.");
            ClearGate(); SelectedId = id; SelectedDay = ParseDate(entry.date); WeekStart = Monday(SelectedDay);
        }
        public JournalEntry[] Visible(bool history, string filter = "all")
        {
            var query = Entries.AsEnumerable();
            if (history) query = query.Where(e => Status(e) != "planned" && (filter == "all" || Status(e) == filter)).OrderByDescending(e => e.date);
            else query = query.Where(e => ParseDate(e.date) >= WeekStart && ParseDate(e.date) < WeekStart.AddDays(7))
                .OrderBy(e => e.id == SelectedId ? 0 : 1).ThenBy(e => e.date);
            return query.ToArray();
        }
        public TrainingPlan Plan(JournalEntry entry) => (entry.status == "completed" || entry.status == "stopped") ? Clone(entry.completedPlan) : Resolve(entry, Key(entry, entry.minutes));
        TrainingPlan Resolve(JournalEntry entry, string key)
        {
            var basePlan = plans.Session(key);
            if (basePlan == null) throw new ArgumentException("Unknown plan context.");
            var result = Clone(basePlan);
            if (result.status != "draft_ready") return result;
            var used = new HashSet<string>();
            foreach (var swap in entry.swaps ?? Array.Empty<JournalSwap>())
            {
                if (swap == null || !used.Add(swap.slot)) throw new ArgumentException("Duplicate exercise slot.");
                var option = plans.Options(key).FirstOrDefault(x => x.block.id == swap.slot && x.block.exercise_id == swap.exercise);
                int i = Array.FindIndex(result.blocks, b => b.id == swap.slot && b.role == "main");
                if (option == null || i < 0) throw new ArgumentException("Exercise is not eligible for this plan context.");
                result.blocks[i] = Clone(option.block); result.estimated_seconds += option.secondsDelta;
            }
            if (result.estimated_seconds > result.budget_seconds || result.blocks.Select(b => b.exercise_id).Distinct().Count() != result.blocks.Length)
                throw new ArgumentException("Combined edits duplicate an exercise or exceed the time budget.");
            return result;
        }
        public JournalOption[] Choices(string slot) => plans.Options(Key(Draft, Draft.minutes)).Where(x => x.block.id == slot).ToArray();
        public void BeginEdit()
        {
            if (!CanEdit(Selected)) throw new InvalidOperationException("Closed history is immutable; copy it to a new draft.");
            Draft = Selected; ClearGate();
        }
        public void BeginCreate(DateTime day, bool copySelected = false)
        {
            if (day.Date < Today) throw new ArgumentException("New plans cannot be backdated.");
            var selected = Selected;
            Draft = copySelected && selected != null && selected.status != "completed" ? selected : new JournalEntry();
            Draft.id = Guid.NewGuid().ToString("N"); Draft.status = "planned"; Draft.completedPlan = null; Draft.session = null; Draft.date = Date(day);
            bool fresh = !(copySelected && selected != null && selected.status != "completed");
            if (fresh && DefaultMinutes > 0 && plans.Durations.Contains(DefaultMinutes)) Draft.minutes = DefaultMinutes;
            if (!plans.Durations.Contains(Draft.minutes)) Draft.minutes = plans.Durations[0];
            if (DefaultProfile != null && (fresh || string.IsNullOrEmpty(Draft.profile))) Draft.profile = DefaultProfile(day);
            ClearGate();
        }
        /// <summary>Live mode: session type and length for a routine created on a given day.</summary>
        public Func<DateTime, string> DefaultProfile;
        public int DefaultMinutes;
        public void SetDuration(int minutes)
        {
            if (Draft == null || !plans.Durations.Contains(minutes)) throw new ArgumentException("Invalid duration.");
            Draft.minutes = minutes; Draft.swaps = Array.Empty<JournalSwap>();
        }
        /// <summary>Change the draft's session type (live journals offer every template).</summary>
        public void SetDraftProfile(string profile)
        {
            if (Draft == null || plans.Profile(profile) == null || profile == LiveTrainingPlans.AccountProfile && IsLive) throw new ArgumentException("Unknown session type.");
            var candidate = Clone(Draft); candidate.profile = profile; candidate.swaps = Array.Empty<JournalSwap>();
            Resolve(candidate, Key(candidate, candidate.minutes)); Draft = candidate;
        }
        public void SetSwap(string slot, string exercise)
        {
            var candidate = Clone(Draft); if (candidate == null) throw new InvalidOperationException("No edit draft.");
            candidate.swaps = candidate.swaps.Where(x => x.slot != slot).Concat(new[] { new JournalSwap { slot = slot, exercise = exercise } }).ToArray();
            Resolve(candidate, Key(candidate, candidate.minutes)); Draft = candidate;
        }
        public void CancelEdit() { Draft = null; }
        public bool SaveEdit(out string error)
        {
            error = null;
            try
            {
                if (!Loaded || Draft == null) throw new InvalidOperationException("No editable journal.");
                var original = document.entries.FirstOrDefault(x => x.id == Draft.id);
                if (original != null && !CanEdit(original)) throw new ArgumentException("Completed/missed history must be preserved.");
                if (ParseDate(Draft.date) < Today || Draft.status != "planned" || Draft.completedPlan != null) throw new ArgumentException("Invalid draft date or status.");
                var next = Clone(document); var rows = next.entries.ToList(); int i = rows.FindIndex(x => x.id == Draft.id);
                if (i < 0) rows.Add(Clone(Draft)); else rows[i] = Clone(Draft);
                next.entries = rows.ToArray(); next.lastReview = null; Validate(next); Persist(next);
                string id = Draft.id; Draft = null; Select(id); ClearGate(); return true;
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is InvalidOperationException || e is IOException || e is UnauthorizedAccessException)
            { error = e.Message; return false; }
        }
        public void BeginPrepare()
        {
            if (!CanPrepare) throw new InvalidOperationException("Prepare only today's pending routine.");
            Gate = new TrainingController(plans); Gate.SelectProfile(Array.FindIndex(plans.Profiles, x => x.id == EntryProfile(Selected)));
            Gate.SetMinutes(Selected.minutes); Gate.BeginReadiness(); PreparedPlan = null; Reviewed = false; ReadinessAdjusted = false;
        }
        public bool ReviewReadiness()
        {
            PreparedPlan = null; preparedKey = preparedEntryId = null; reviewedGatePlan = null; ReadinessAdjusted = false;
            if (Gate == null || !Gate.Review()) return false;
            PreparedPlan = Clone(Gate.Plan); preparedKey = Gate.Key; preparedEntryId = SelectedId; reviewedGatePlan = Gate.Plan;
            if (PreparedPlan.status == "draft_ready")
            {
                try { PreparedPlan = Resolve(Selected, Gate.Key); }
                catch (ArgumentException) { ReadinessAdjusted = true; } // Explicitly show the new eligible base prescription.
            }
            return true;
        }
        public bool SaveReview(out string error)
        {
            error = null;
            try
            {
                if (!CanSaveReview) return false;
                var next = Clone(document);
                next.lastReview = new JournalReview { entryId = SelectedId, date = Date(Today), reviewedUtc = DateTime.UtcNow.ToString("O"), plan = Clone(PreparedPlan) };
                Validate(next); Persist(next); Gate.Accept(); Reviewed = true; return true;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException)
            { error = e.Message; return false; }
        }
        public bool CanSaveReview => Gate != null && Gate.CanAccept && PreparedPlan?.status == "draft_ready"
            && preparedKey == Gate.Key && preparedEntryId == SelectedId && ReferenceEquals(reviewedGatePlan, Gate.Plan);
        public void ClearGate() { Gate = null; PreparedPlan = null; preparedKey = preparedEntryId = null; reviewedGatePlan = null; Reviewed = false; ReadinessAdjusted = false; }
        public void SaveBoss(BossSession session)
        {
            var owner = document?.entries.FirstOrDefault(x => x.id == session?.entryId);
            if (!Loaded || session == null || owner == null || session.profile != EntryProfile(owner) || session.context != plans.Context) throw new ArgumentException("Invalid session profile.");
            if (document.activeSession == null)
            {
                if (!CanSaveReview || session.entryId != SelectedId || session.date != Date(Today)) throw new InvalidOperationException("A fresh acknowledged review is required.");
            }
            else if (document.activeSession.id != session.id || document.activeSession.entryId != session.entryId) throw new InvalidOperationException("Another session is already saved.");
            var next = Clone(document); next.activeSession = Clone(session); Validate(next); Persist(next);
        }
        public void ArchiveBoss()
        {
            var active = document?.activeSession;
            if (active == null) return; // Retry after a successful return is idempotent.
            if (active.state == "active") throw new InvalidOperationException("Pause or finish the active session first.");
            var next = Clone(document); var entry = next.entries.First(x => x.id == active.entryId);
            entry.status = active.state; entry.completedPlan = Clone(active.plan); entry.session = Clone(active);
            next.activeSession = null; next.lastReview = null; Validate(next); Persist(next); Select(entry.id);
        }
        public string Export() => JsonUtility.ToJson(document, true);

        /// <summary>
        /// Live mode: add planned entries for scheduled days that have none. Existing entries,
        /// history and edits are never changed; past days are never back-filled.
        /// </summary>
        public bool EnsureScheduled(IEnumerable<KeyValuePair<DateTime, string>> plannedDays, int minutes, DateTime notBefore)
        {
            if (!Loaded || plannedDays == null) return false;
            if (!plans.Durations.Contains(minutes)) throw new ArgumentException("Invalid duration.");
            var next = Clone(document); var rows = next.entries.ToList(); bool changed = false;
            foreach (var pair in plannedDays)
            {
                if (pair.Key.Date < notBefore.Date || pair.Key.Date < Today) continue;
                string day = Date(pair.Key);
                if (rows.Any(e => e.date == day) || plans.Profile(pair.Value) == null) continue;
                rows.Add(new JournalEntry { id = "plan-" + day, date = day, minutes = minutes, profile = pair.Value });
                changed = true;
            }
            if (!changed) return false;
            next.entries = rows.OrderBy(e => e.date, StringComparer.Ordinal).ToArray();
            Validate(next); Persist(next); SelectDay(SelectedDay); return true;
        }

        /// <summary>Live mode: replace future, untouched planned entries after the user changes setup.</summary>
        public void ReplaceFuturePlan(IEnumerable<KeyValuePair<DateTime, string>> plannedDays, int minutes)
        {
            if (!Loaded) return;
            if (!plans.Durations.Contains(minutes)) throw new ArgumentException("Invalid duration.");
            var next = Clone(document);
            // Keep history, today's entry if a session is active, and anything the user edited or titled.
            next.entries = next.entries.Where(e => e.status != "planned" || ParseDate(e.date) < Today
                || (next.activeSession != null && next.activeSession.entryId == e.id)
                || !e.id.StartsWith("plan-", StringComparison.Ordinal) || (e.swaps?.Length ?? 0) > 0 || !string.IsNullOrEmpty(e.title)).ToArray();
            next.lastReview = null; Validate(next); Persist(next);
            EnsureScheduled(plannedDays, minutes, Today);
        }

        public int CompletedCount => document?.entries.Count(e => e.status == "completed") ?? 0;
        /// <summary>Consecutive scheduled sessions completed or stopped (rest days and stops never break it).</summary>
        public int ConsistencyStreak
        {
            get
            {
                if (document == null) return 0;
                int streak = 0;
                foreach (var e in document.entries.Where(x => ParseDate(x.date) < Today || x.status != "planned").OrderByDescending(x => x.date, StringComparer.Ordinal))
                {
                    if (e.status == "completed" || e.status == "stopped") streak++;
                    else break;
                }
                return streak;
            }
        }
        public JournalEntry TodayEntry => Clone(document?.entries.FirstOrDefault(x => x.date == Date(Today) && x.status == "planned"))
            ?? Clone(document?.entries.FirstOrDefault(x => x.date == Date(Today)));
        public JournalEntry NextPlanned => Clone(document?.entries.Where(x => x.status == "planned" && ParseDate(x.date) >= Today).OrderBy(x => x.date, StringComparer.Ordinal).FirstOrDefault());
        void Persist(JournalDocument next) { storage.Write(JsonUtility.ToJson(next, true)); document = next; }
        JournalDocument Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 2000000) throw new ArgumentException("Invalid journal data.");
            var result = JsonUtility.FromJson<JournalDocument>(text); Normalize(result); return result;
        }
        static bool ValidText(TrainingText text) => text != null && !string.IsNullOrWhiteSpace(text.en) && !string.IsNullOrWhiteSpace(text.es)
            && text.en.Length < 2000 && text.es.Length < 2000;
        static void ValidateSnapshot(TrainingPlan plan)
        {
            if (plan == null || !ValidText(plan.name) || plan.blocks == null || plan.blocks.Length > 30 || plan.messages == null || plan.messages.Length > 30
                || plan.estimated_seconds < 0 || plan.budget_seconds < 0
                || plan.blocks.Any(b => b == null || !ValidText(b.name) || string.IsNullOrEmpty(b.id) || string.IsNullOrEmpty(b.exercise_id)
                    || !new[] { "minutes", "reps", "seconds" }.Contains(b.unit) || b.sets < 1 || b.quantity_min < 1 || b.quantity_max < b.quantity_min || b.rest_seconds < 0)
                || plan.messages.Any(m => m == null || !ValidText(m.text)))
                throw new ArgumentException("Invalid immutable history snapshot.");
        }
        void Validate(JournalDocument data)
        {
            if (data == null || data.version != 1 || data.context != plans.Context || data.profileId != ProfileId || data.entries == null || data.entries.Length > 1500)
                throw new ArgumentException("Unsupported journal version or profile.");
            var ids = new HashSet<string>();
            foreach (var entry in data.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.id.Length > 80 || !ids.Add(entry.id)
                    || entry.title == null || entry.title.Length > 24 || entry.title.Any(char.IsControl)
                    || !plans.Durations.Contains(entry.minutes) || entry.swaps == null || entry.swaps.Length > 12
                    || (!string.IsNullOrEmpty(entry.profile) && (plans.Profile(entry.profile) == null || entry.profile == ProfileId)))
                    throw new ArgumentException("Invalid journal entry.");
                var day = ParseDate(entry.date); if (day.Year < 2000 || day.Year > 2100) throw new ArgumentException("Date outside supported calendar.");
                if (entry.status == "completed" || entry.status == "stopped")
                {
                    ValidateSnapshot(entry.completedPlan);
                    if (entry.session != null) BossSession.Validate(entry.session);
                }
                else if (entry.status == "planned" && entry.completedPlan == null) Resolve(entry, Key(entry, entry.minutes));
                else throw new ArgumentException("Unknown journal status.");
            }
            if (data.activeSession != null)
            {
                BossSession.Validate(data.activeSession);
                if (data.activeSession.context != plans.Context || !data.entries.Any(e => e.id == data.activeSession.entryId && e.status == "planned" && EntryProfile(e) == data.activeSession.profile)) throw new ArgumentException("Orphaned active session.");
            }
            if (data.lastReview != null && (!ids.Contains(data.lastReview.entryId) || data.lastReview.plan?.status != "draft_ready"))
                throw new ArgumentException("Invalid saved review.");
            if (data.lastReview != null) ValidateSnapshot(data.lastReview.plan);
        }
    }
}
