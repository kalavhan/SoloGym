using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SoloGym.Training
{
    // C# port of tools/training_reference.py (generate_session / generate_week / swap_exercise).
    // Content remains draft_requires_professional_review; this engine applies the same bounded
    // rules on-device so a real user's setup produces their plan without a server.

    [Serializable] public sealed class ExerciseCues { public string[] en, es; }
    [Serializable] public sealed class ExerciseDef
    {
        public string id, category, movement_pattern, min_experience, prescription_unit;
        public TrainingText name;
        public string[] equipment_all, environments, age_groups, regressions, progressions;
        public bool supervision_required_for_teens, unilateral;
        public int setup_seconds;
        public ExerciseCues cues;
    }
    [Serializable] public sealed class ExerciseCatalogData { public string schema_version, content_status; public ExerciseDef[] exercises; }
    [Serializable] public sealed class TemplateSlot { public string id, category; public string[] patterns, exercise_ids; public bool optional; }
    [Serializable] public sealed class TemplateDef { public string id, kind; public TrainingText name; public TemplateSlot[] slots; }
    [Serializable] public sealed class TemplateData { public string schema_version; public string[] warmup_exercise_ids, cooldown_exercise_ids; public TemplateDef[] templates; }
    [Serializable] public sealed class DoseRule { public int beginner_sets, intermediate_sets, reps_min, reps_max, rest_seconds; }
    [Serializable] public sealed class DoseRules
    {
        public DoseRule general_fitness, strength, muscle_growth, endurance, mobility;
        public DoseRule Get(string goal)
        {
            switch (goal)
            {
                case "strength": return strength;
                case "muscle_growth": return muscle_growth;
                case "endurance": return endurance;
                case "mobility": return mobility;
                default: return general_fitness;
            }
        }
    }
    [Serializable] public sealed class AgeRule { public int max_sets; public bool hard_enabled, strength_supervision_required; public string goal_mode; }
    [Serializable] public sealed class AgeRules { public AgeRule teen, adult; }
    [Serializable] public sealed class TimingRule { public int warmup_minutes, cooldown_minutes, seconds_per_rep_budget, transition_seconds, timed_core_seconds, mobility_seconds; }
    [Serializable] public sealed class CardioRule { public int beginner_minutes, intermediate_minutes, minimum_minutes, maximum_minutes; public int[] aerobic_effort_medium, aerobic_effort_light; }
    [Serializable] public sealed class DifficultyRule { public int strength_sets_delta, cardio_minutes_delta, extra_sets_first_strength_slot; }
    [Serializable] public sealed class DifficultyRules
    {
        public DifficultyRule light, medium, hard;
        public DifficultyRule Get(string id) => id == "light" ? light : id == "hard" ? hard : medium;
    }
    [Serializable] public sealed class RecoveryRule { public int strength_calendar_day_gap; }
    [Serializable] public sealed class RewardRule { public int fitness_xp_per_completed_session, coins_per_completed_session; }
    [Serializable] public sealed class TrainingRulesData
    {
        public string schema_version, content_status;
        public int minimum_age, teen_upper_age;
        public string[] supported_goals, supported_experience, stop_readiness;
        public AgeRules age_specific;
        public DoseRules strength_doses;
        public TimingRule timing;
        public CardioRule cardio;
        public DifficultyRules difficulty;
        public RecoveryRule recovery;
        public RewardRule rewards;
    }

    /// <summary>The user's training inputs. Appearance/body never enters this object.</summary>
    [Serializable] public sealed class TrainingInput
    {
        public int age;
        public string experience = "beginner", goal = "general_fitness", environment = "home", difficulty = "medium", readiness = "ready";
        public string[] equipment = Array.Empty<string>(), excluded_exercises = Array.Empty<string>();
        public int session_minutes = 25;
        public bool teen_supervision_available, professional_guidance_needed;
        public bool IsTeen => age < 18;
        public TrainingInput Copy()
        {
            var c = (TrainingInput)MemberwiseClone();
            c.equipment = (string[])(equipment ?? Array.Empty<string>()).Clone();
            c.excluded_exercises = (string[])(excluded_exercises ?? Array.Empty<string>()).Clone();
            return c;
        }
    }

    public sealed class TrainingEngine
    {
        public const string IntervalExercise = "walk_jog_intervals";
        static readonly string[] Goals = { "general_fitness", "strength", "muscle_growth", "endurance", "mobility" };
        static readonly string[] Readiness = { "ready", "low_energy", "ill", "pain", "injury" };
        static readonly string[] Environments = { "home", "gym", "outdoor" };
        public TrainingRulesData Rules { get; }
        public TemplateData Templates { get; }
        readonly Dictionary<string, ExerciseDef> catalog;
        readonly HashSet<string> equipmentIds;
        public IReadOnlyDictionary<string, ExerciseDef> Exercises => catalog;

        public TrainingEngine(ExerciseCatalogData exercises, TemplateData templates, TrainingRulesData rules, IEnumerable<string> knownEquipment = null)
        {
            if (exercises?.exercises == null || templates?.templates == null || rules?.strength_doses == null || rules.timing == null || rules.cardio == null || rules.difficulty == null)
                throw new ArgumentException("Training content is incomplete.");
            Rules = rules; Templates = templates;
            catalog = new Dictionary<string, ExerciseDef>();
            foreach (var e in exercises.exercises)
            {
                if (e == null || string.IsNullOrEmpty(e.id) || catalog.ContainsKey(e.id)) throw new ArgumentException("Invalid or duplicate exercise id.");
                e.equipment_all = e.equipment_all ?? Array.Empty<string>(); e.environments = e.environments ?? Array.Empty<string>(); e.age_groups = e.age_groups ?? Array.Empty<string>();
                catalog[e.id] = e;
            }
            equipmentIds = knownEquipment == null ? null : new HashSet<string>(knownEquipment);
            foreach (var t in templates.templates)
                foreach (var slot in t.slots)
                    foreach (var id in slot.exercise_ids)
                    {
                        if (!catalog.TryGetValue(id, out var e) || e.category != slot.category || !slot.patterns.Contains(e.movement_pattern))
                            throw new ArgumentException("Wrong slot: " + t.id + "/" + id);
                    }
            foreach (var id in templates.warmup_exercise_ids.Concat(templates.cooldown_exercise_ids))
                if (!catalog.ContainsKey(id)) throw new ArgumentException("Unknown warm-up/cool-down exercise.");
        }

        public void Validate(TrainingInput p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (p.age < Rules.minimum_age || p.age > 120) throw new ArgumentException("Age outside the supported range.");
            if (!Goals.Contains(p.goal)) throw new ArgumentException("Unknown goal.");
            if (p.experience != "beginner" && p.experience != "intermediate") throw new ArgumentException("Unknown experience.");
            if (!Environments.Contains(p.environment)) throw new ArgumentException("Unknown environment.");
            if (p.session_minutes < 15 || p.session_minutes > 60) throw new ArgumentException("Session minutes must be 15-60.");
            if (p.difficulty != "light" && p.difficulty != "medium" && p.difficulty != "hard") throw new ArgumentException("Unknown difficulty.");
            if (!Readiness.Contains(p.readiness)) throw new ArgumentException("Unknown readiness.");
            if (equipmentIds != null && (p.equipment ?? Array.Empty<string>()).Any(x => !equipmentIds.Contains(x))) throw new ArgumentException("Unrecognized equipment.");
            if ((p.excluded_exercises ?? Array.Empty<string>()).Any(x => !catalog.ContainsKey(x))) throw new ArgumentException("Unrecognized excluded exercise.");
        }

        public static string AgeGroup(TrainingInput p) => p.age < 18 ? "teen" : "adult";
        public static string EffectiveGoal(TrainingInput p) => AgeGroup(p) == "teen" && p.goal != "mobility" ? "general_fitness" : p.goal;

        public bool Eligible(ExerciseDef e, TrainingInput p)
        {
            string group = AgeGroup(p);
            var equipment = p.equipment ?? Array.Empty<string>();
            return e.age_groups.Contains(group)
                && e.environments.Contains(p.environment)
                && e.equipment_all.All(x => equipment.Contains(x))
                && !(p.excluded_exercises ?? Array.Empty<string>()).Contains(e.id)
                && !(p.experience == "beginner" && e.min_experience == "intermediate")
                && !(group == "teen" && e.supervision_required_for_teens && !p.teen_supervision_available);
        }

        static TrainingMessage Message(string code, string en, string es) => new TrainingMessage { code = code, text = new TrainingText { en = en, es = es } };

        int WorkPerSet(TrainingBlock block, ExerciseDef e)
        {
            int perUnit = block.unit == "reps" ? Rules.timing.seconds_per_rep_budget : block.unit == "seconds" ? 1 : 60;
            double work = block.quantity_max * perUnit;
            // Quantities for unilateral movements are PER SIDE.
            if (e.unilateral) work *= 2;
            return (int)work;
        }

        public int Estimate(TrainingBlock block, ExerciseDef e)
        {
            int work = WorkPerSet(block, e);
            return (int)Math.Ceiling((double)work * block.sets) + (block.sets - 1) * block.rest_seconds
                + e.setup_seconds + (block.role == "main" ? Rules.timing.transition_seconds : 0);
        }

        TrainingBlock MakeBlock(ExerciseDef e, string slotId, string[] patterns, string role, int sets, int lo, int hi, int rest, int[] effort)
        {
            var b = new TrainingBlock
            {
                id = slotId, role = role, exercise_id = e.id, name = new TrainingText { en = e.name.en, es = e.name.es },
                category = e.category, allowed_patterns = (string[])patterns.Clone(), unit = e.prescription_unit,
                sets = sets, quantity_min = lo, quantity_max = hi, per_side = e.unilateral, rest_seconds = rest,
                load_entry = "user_selected_if_applicable", aerobic_effort_0_to_10 = effort == null ? null : (int[])effort.Clone(), boss_share = 0
            };
            Refresh(b, e);
            return b;
        }

        void Refresh(TrainingBlock b, ExerciseDef e)
        {
            b.estimated_seconds = Estimate(b, e);
            b.additional_set_seconds = WorkPerSet(b, e) + b.rest_seconds;
        }

        public TemplateDef Template(string id) => Templates.templates.FirstOrDefault(t => t.id == id) ?? throw new ArgumentException("Unknown template.");

        /// <summary>Port of generate_session. Date only matters for the recent-strength recovery rule.</summary>
        public TrainingPlan GenerateSession(TrainingInput profile, string templateId, DateTime? onDate = null, IEnumerable<string> recentStrengthDates = null)
        {
            Validate(profile);
            var template = Template(templateId);
            string group = AgeGroup(profile), goal = EffectiveGoal(profile), difficulty = profile.difficulty;
            var result = new TrainingPlan
            {
                template_id = templateId, name = new TrainingText { en = template.name.en, es = template.name.es }, kind = template.kind,
                status = "draft_ready", difficulty_effective = difficulty, budget_seconds = profile.session_minutes * 60,
                date = onDate.HasValue ? onDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null
            };
            var blocks = new List<TrainingBlock>(); var messages = new List<TrainingMessage>(); var gaps = new List<string>();
            TrainingPlan Done() { result.blocks = blocks.ToArray(); result.messages = messages.ToArray(); result.coverage_gaps = gaps.ToArray(); return result; }
            if (profile.professional_guidance_needed)
            {
                result.status = "needs_review";
                messages.Add(Message("individual_guidance_needed", "Use individualized professional guidance before this routine. No workout has been prescribed.", "Busca orientación profesional individual antes de seguir esta rutina. No se ha indicado un entrenamiento."));
                return Done();
            }
            if (Rules.stop_readiness.Contains(profile.readiness))
            {
                result.status = "recovery"; result.kind = "recovery";
                messages.Add(Message("stop_for_readiness", "Rest or stop; do not train through illness, pain or injury. Seek appropriate advice before resuming.", "Descansa o detente; no entrenes con enfermedad, dolor o lesión. Busca orientación adecuada antes de volver."));
                return Done();
            }
            if (template.kind == "strength" && group == "teen" && !profile.teen_supervision_available)
            {
                result.status = "needs_review";
                messages.Add(Message("supervision_needed", "Arrange appropriate supervision for this strength session. No workout has been prescribed.", "Organiza supervisión adecuada para esta sesión de fuerza. No se ha indicado un entrenamiento."));
                return Done();
            }
            if (template.kind == "strength" && onDate.HasValue && recentStrengthDates != null)
            {
                foreach (var d in recentStrengthDates)
                {
                    int days = (onDate.Value.Date - DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture)).Days;
                    if (days >= 0 && days < Rules.recovery.strength_calendar_day_gap)
                    {
                        result.status = "recovery"; result.kind = "recovery";
                        messages.Add(Message("recent_strength", "Keep this as recovery after recent strength work; do not make up missed volume.", "Mantén la recuperación tras el trabajo de fuerza reciente; no acumules el volumen pendiente."));
                        return Done();
                    }
                }
            }
            if (profile.readiness == "low_energy") difficulty = "light";
            else if (difficulty == "hard" && (group == "teen" || profile.experience == "beginner")) difficulty = "medium";
            result.difficulty_effective = difficulty;
            if (difficulty != profile.difficulty)
                messages.Add(Message("difficulty_adjusted", "Difficulty was reduced for readiness or training experience; reward value is unchanged.", "Se redujo la dificultad según tu preparación o experiencia; la recompensa no cambia."));
            var timing = Rules.timing;
            TrainingBlock warm = null, cool = null;
            foreach (var role in new[] { "warmup", "cooldown" })
            {
                var ids = role == "warmup" ? Templates.warmup_exercise_ids : Templates.cooldown_exercise_ids;
                int minutes = role == "warmup" ? timing.warmup_minutes : timing.cooldown_minutes;
                var e = ids.Select(i => catalog[i]).FirstOrDefault(x => Eligible(x, profile));
                if (e == null) { result.status = "needs_changes"; gaps.Add(role); }
                else
                {
                    var b = MakeBlock(e, role, new[] { "cardio" }, role, 1, minutes, minutes, 0, new[] { 2, 3 });
                    if (role == "warmup") warm = b; else cool = b;
                }
            }
            var main = new List<TrainingBlock>();
            foreach (var slot in template.slots)
            {
                var e = slot.exercise_ids.Select(i => catalog[i]).FirstOrDefault(x => Eligible(x, profile));
                if (e == null)
                {
                    gaps.Add(slot.id);
                    if (!slot.optional) result.status = "needs_changes";
                    continue;
                }
                int sets, lo, hi, rest; int[] effort = null;
                if (template.kind == "strength")
                {
                    var dose = Rules.strength_doses.Get(goal);
                    sets = profile.experience == "intermediate" ? dose.intermediate_sets : dose.beginner_sets;
                    if (difficulty == "light") sets += Rules.difficulty.light.strength_sets_delta;
                    else if (difficulty == "hard" && main.Count == 0) sets += Rules.difficulty.hard.extra_sets_first_strength_slot;
                    var ageRule = group == "teen" ? Rules.age_specific.teen : Rules.age_specific.adult;
                    sets = Math.Max(1, Math.Min(sets, Math.Min(ageRule.max_sets, profile.experience == "beginner" ? 2 : 3)));
                    lo = dose.reps_min; hi = dose.reps_max;
                    if (e.prescription_unit == "seconds") lo = hi = timing.timed_core_seconds;
                    rest = dose.rest_seconds;
                }
                else if (template.kind == "cardio")
                {
                    sets = 1; rest = 0;
                    int minutes = profile.experience == "intermediate" ? Rules.cardio.intermediate_minutes : Rules.cardio.beginner_minutes;
                    minutes += Rules.difficulty.Get(difficulty).cardio_minutes_delta;
                    lo = hi = Math.Min(Rules.cardio.maximum_minutes, Math.Max(Rules.cardio.minimum_minutes, minutes));
                    effort = difficulty == "light" ? Rules.cardio.aerobic_effort_light : Rules.cardio.aerobic_effort_medium;
                }
                else
                {
                    sets = 1; rest = 0;
                    lo = hi = e.prescription_unit == "reps" ? 6 : timing.mobility_seconds;
                }
                main.Add(MakeBlock(e, slot.id, slot.patterns, "main", sets, lo, hi, rest, effort));
            }
            if (warm != null) blocks.Add(warm);
            blocks.AddRange(main);
            if (cool != null) blocks.Add(cool);
            if (gaps.Contains("pull"))
                messages.Add(Message("pull_coverage_gap", "This setup lacks an eligible resisted pull. Add suitable equipment or review the plan; mobility is not a substitute.", "Este equipo no permite un tirón con resistencia adecuado. Añade equipo apropiado o revisa el plan; la movilidad no lo sustituye."));
            int Total() => blocks.Sum(b => b.estimated_seconds);
            bool reduced = false;
            while (Total() > result.budget_seconds)
            {
                TrainingBlock b;
                var reducible = main.Where(x => x.sets > 1).ToList();
                if (reducible.Count > 0)
                {
                    // max by (sets, estimated_seconds); first maximal element as in Python.
                    b = reducible[0];
                    foreach (var x in reducible) if (x.sets > b.sets || (x.sets == b.sets && x.estimated_seconds > b.estimated_seconds)) b = x;
                    b.sets -= 1;
                }
                else if (template.kind == "cardio" && main.Count > 0 && main[0].quantity_max > Rules.cardio.minimum_minutes)
                {
                    b = main[0]; b.quantity_min -= 1; b.quantity_max -= 1;
                }
                else
                {
                    result.status = "needs_changes";
                    messages.Add(Message("insufficient_time", "Choose more time or a different template; warm-up, rest and recovery have not been compressed.", "Elige más tiempo u otra plantilla; no se han reducido el calentamiento, las pausas ni la recuperación."));
                    break;
                }
                reduced = true;
                Refresh(b, catalog[b.exercise_id]);
            }
            if (reduced)
                messages.Add(Message("time_adjustment", "Work was reduced to fit the time budget while retaining rest and session structure.", "Se redujo el trabajo para respetar el tiempo disponible y conservar las pausas y la estructura."));
            result.estimated_seconds = Total();
            if (main.Count == 0) result.status = "needs_changes";
            if (result.status == "draft_ready")
            {
                // Equal share per planned exercise; heavier loads or extra reps never add damage.
                int per = 1000 / main.Count, remainder = 1000 % main.Count;
                for (int i = 0; i < main.Count; i++) main[i].boss_share = per + (i < remainder ? 1 : 0);
            }
            return Done();
        }

        /// <summary>
        /// Port of swap_exercise as an option list: every eligible replacement for a main slot,
        /// keeping the planned dose, unit and time budget. The current exercise is included first.
        /// </summary>
        public JournalOption[] SwapOptions(TrainingInput profile, TrainingPlan plan, string key)
        {
            var result = new List<JournalOption>();
            if (plan?.status != "draft_ready" || plan.blocks == null) return result.ToArray();
            foreach (var block in plan.blocks.Where(b => b.role == "main"))
            {
                var current = catalog[block.exercise_id];
                result.Add(new JournalOption { key = key, block = Clone(block), secondsDelta = 0 });
                foreach (var e in catalog.Values.OrderBy(x => x.id, StringComparer.Ordinal))
                {
                    if (e.id == block.exercise_id || e.id == IntervalExercise || !Eligible(e, profile) || e.category != block.category
                        || block.allowed_patterns == null || !block.allowed_patterns.Contains(e.movement_pattern) || e.prescription_unit != block.unit)
                        continue;
                    if (plan.blocks.Any(o => o.id != block.id && o.exercise_id == e.id)) continue;
                    var b = Clone(block);
                    b.exercise_id = e.id; b.name = new TrainingText { en = e.name.en, es = e.name.es }; b.per_side = e.unilateral;
                    Refresh(b, e);
                    int delta = b.estimated_seconds - block.estimated_seconds;
                    if (plan.estimated_seconds + delta > plan.budget_seconds) continue;
                    result.Add(new JournalOption { key = key, block = b, secondsDelta = delta });
                }
            }
            return result.ToArray();
        }

        static TrainingBlock Clone(TrainingBlock b)
        {
            var c = (TrainingBlock)b.CloneShallow();
            c.name = b.name == null ? null : new TrainingText { en = b.name.en, es = b.name.es };
            c.allowed_patterns = b.allowed_patterns == null ? null : (string[])b.allowed_patterns.Clone();
            c.aerobic_effort_0_to_10 = b.aerobic_effort_0_to_10 == null ? null : (int[])b.aerobic_effort_0_to_10.Clone();
            return c;
        }

        /// <summary>
        /// Weekly template assignment for the user's chosen weekdays (0=Mon..6=Sun), following
        /// generate_week's distribution while never placing strength on consecutive dates.
        /// </summary>
        public string[] WeekTemplates(TrainingInput profile, int[] weekdays)
        {
            var days = (weekdays ?? Array.Empty<int>()).Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d).ToArray();
            var result = new string[days.Length];
            if (days.Length == 0) return result;
            bool teen = AgeGroup(profile) == "teen";
            if (profile.environment == "outdoor") { for (int i = 0; i < days.Length; i++) result[i] = "aerobic_base"; return result; }
            if (profile.goal == "mobility") { for (int i = 0; i < days.Length; i++) result[i] = "mobility_reset"; return result; }
            bool focus = !teen && (profile.goal == "strength" || profile.goal == "muscle_growth");
            int n = days.Length;
            var strength = new HashSet<int>();
            if (n == 1) strength.Add(0);
            else if (n == 2) { strength.Add(0); strength.Add(1); }
            else if (n == 3) { strength.Add(0); strength.Add(2); if (focus) strength.Add(1); }
            else if (n == 4) { strength.Add(0); strength.Add(2); }
            else { strength.Add(0); if (focus) { strength.Add(2); strength.Add(4); } else strength.Add(3); }
            int gap = Rules.recovery.strength_calendar_day_gap, last = int.MinValue / 2, strengthIndex = 0;
            bool machines = profile.environment == "gym" && (profile.equipment ?? Array.Empty<string>()).Contains("leg_press_machine");
            for (int i = 0; i < n; i++)
            {
                if (strength.Contains(i) && days[i] - last >= gap)
                {
                    result[i] = machines ? "machine_foundation" : strengthIndex % 2 == 0 ? "foundation_a" : "foundation_b";
                    strengthIndex++; last = days[i];
                }
                else result[i] = n == 5 && i == 2 ? "mobility_reset" : "aerobic_base";
            }
            return result;
        }
    }
}
