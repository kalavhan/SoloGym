using System;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    [Serializable] public sealed class TrainingText
    {
        public string en, es;
        public string Get(string language) => language == "es" ? es : en;
    }
    [Serializable] public sealed class TrainingProfile
    {
        public string id;
        public TrainingText name;
        public bool teen;
        public TrainingText[] equipment;
    }
    [Serializable] public sealed class TrainingBlock
    {
        public string id, role, exercise_id, unit;
        public TrainingText name;
        public int sets, quantity_min, quantity_max, rest_seconds, boss_share, estimated_seconds, additional_set_seconds;
        public string category, load_entry;
        public int[] aerobic_effort_0_to_10;
        public string[] allowed_patterns;
        public bool per_side;
        public object CloneShallow() => MemberwiseClone();
    }
    [Serializable] public sealed class TrainingMessage { public string code; public TrainingText text; }
    [Serializable] public sealed class TrainingPlan
    {
        public string status, kind, difficulty_effective, template_id, date;
        public TrainingText name;
        public string[] coverage_gaps;
        public int estimated_seconds, budget_seconds;
        public TrainingBlock[] blocks;
        public TrainingMessage[] messages;
    }
    [Serializable] public sealed class TrainingEntry { public string key; public TrainingPlan session; }
    [Serializable] public sealed class TrainingCatalog
    {
        public TrainingProfile[] profiles;
        public TrainingEntry[] entries;
    }
    public enum TrainingStep { Hub, Readiness, Plan, Rest }

    /// <summary>Review-only fixture selection. Never starts exercise or issues rewards.</summary>
    public sealed class TrainingController
    {
        readonly ITrainingPlans catalog;
        public TrainingStep Step { get; private set; }
        public int ProfileIndex { get; private set; }
        public int Minutes { get; private set; } = 25;
        public string Readiness { get; private set; }
        public bool? Supervised { get; private set; }
        public bool Acknowledged { get; private set; }
        public string AcceptedKey { get; private set; }
        public TrainingProfile Profile => catalog.Profiles[ProfileIndex];
        public TrainingProfile[] Profiles => catalog.Profiles;
        public TrainingPlan Plan { get; private set; }
        public string Key => TrainingKeys.Make(Profile.id, Minutes, Readiness, Profile.teen && Supervised == true);
        public bool CanReview => Readiness != null && (!Profile.teen || Supervised.HasValue);
        public bool CanAccept => Step == TrainingStep.Plan && Plan != null && Plan.status == "draft_ready" && Acknowledged;
        public TrainingController(TrainingCatalog source) : this(new FixtureTrainingPlans(source ?? throw new ArgumentNullException(nameof(source)), null)) { }
        public TrainingController(ITrainingPlans source)
        {
            catalog = source ?? throw new ArgumentNullException(nameof(source));
            if (catalog.Profiles == null || catalog.Profiles.Length == 0) throw new ArgumentException("Training catalog is incomplete.");
            Minutes = catalog.Durations.Contains(25) ? 25 : catalog.Durations[0];
        }
        public void SelectProfile(int index)
        {
            if (index < 0 || index >= catalog.Profiles.Length) throw new ArgumentOutOfRangeException(nameof(index));
            ProfileIndex = index; ResetReadiness();
        }
        public void SetMinutes(int value)
        {
            if (!catalog.Durations.Contains(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Minutes = value; Invalidate();
        }
        public void BeginReadiness() { ResetReadiness(); Step = TrainingStep.Readiness; }
        public void SetReadiness(string value)
        {
            if (!new[] { "ready", "low_energy", "ill", "pain", "injury" }.Contains(value))
                throw new ArgumentException("Unknown readiness.");
            Readiness = value; Invalidate();
        }
        public void SetSupervision(bool value) { Supervised = value; Invalidate(); }
        void Invalidate() { Plan = null; Acknowledged = false; AcceptedKey = null; }
        void ResetReadiness() { Readiness = null; Supervised = null; Invalidate(); }
        public bool Review()
        {
            if (!CanReview) return false;
            Plan = catalog.Session(Key);
            Acknowledged = false;
            if (Plan == null) throw new InvalidOperationException("No training review fixture for " + Key);
            Step = Plan.status == "recovery" ? TrainingStep.Rest : TrainingStep.Plan;
            return true;
        }
        public void Acknowledge(bool value) { Acknowledged = value; }
        public bool Accept()
        {
            if (!CanAccept) return false;
            AcceptedKey = Key; Step = TrainingStep.Hub;
            return true;
        }
        public void Rest() { Invalidate(); Step = TrainingStep.Rest; }
        public void Back()
        {
            if (Step == TrainingStep.Plan) { Invalidate(); Step = TrainingStep.Readiness; }
            else { ResetReadiness(); Step = TrainingStep.Hub; }
        }
    }
}
