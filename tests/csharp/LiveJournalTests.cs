using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SoloGym;
using SoloGym.Training;
namespace Tests
{
    sealed class Memory : IJournalStorage { public string Data; public int Writes; public string Read() => Data; public void Write(string json) { Data = json; Writes++; } }
    public static class Program
    {
        static int fails, passes;
        static void Check(bool ok, string what) { if (ok) passes++; else { fails++; Console.WriteLine("FAIL " + what); } }
        static readonly JsonSerializerOptions O = new JsonSerializerOptions { IncludeFields = true };
        static TrainingEngine Engine()
        {
            string root = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("SOLOGYM_ROOT"), "app/Assets/SoloGym/Resources/Training/Rules/");
            return new TrainingEngine(JsonSerializer.Deserialize<ExerciseCatalogData>(File.ReadAllText(root + "exercises.json"), O),
                JsonSerializer.Deserialize<TemplateData>(File.ReadAllText(root + "templates.json"), O),
                JsonSerializer.Deserialize<TrainingRulesData>(File.ReadAllText(root + "rules.json"), O));
        }
        public static int Main()
        {
            var engine = Engine();
            var today = new DateTime(2026, 10, 5); // Monday
            int[] days = { 0, 2, 4 };
            var input = new TrainingInput { age = 30, experience = "beginner", goal = "general_fitness", environment = "home", equipment = new[] { "wall", "chair", "stable_surface" }, session_minutes = 25 };
            var plans = new LiveTrainingPlans(engine, input);
            Check(plans.Profiles.Length == engine.Templates.templates.Length, "one profile per template");
            Check(string.Join(",", engine.WeekTemplates(input, days)) == "foundation_a,aerobic_base,foundation_b", "3-day week: strength/cardio/strength: " + string.Join(",", engine.WeekTemplates(input, days)));
            var mem = new Memory();
            var j = new WorkoutJournal(plans, mem, LiveTrainingPlans.AccountProfile, today);
            j.DefaultProfile = d => plans.DefaultProfileFor(d, days); j.DefaultMinutes = 25;
            j.Load(true);
            Check(j.Loaded && j.Entries.Length == 0, "empty live journal: " + j.Error);
            Check(j.EnsureScheduled(plans.Schedule(days, today, today.AddDays(13)), 25, today), "schedule added");
            Check(j.Entries.Length == 6, "two weeks x 3 days = 6 entries: " + j.Entries.Length);
            Check(!j.EnsureScheduled(plans.Schedule(days, today, today.AddDays(13)), 25, today), "schedule idempotent");
            j.SelectDay(today);
            var e = j.Selected;
            Check(e != null && e.profile == "u.foundation_a", "today is Foundation A");
            var plan = j.Plan(e);
            Check(plan.status == "draft_ready" && plan.blocks.Length >= 4 && plan.blocks.Where(b => b.role == "main").Sum(b => b.boss_share) == 1000, "plan ready with 1000 boss shares");
            // edit: swap squat, change type
            j.BeginEdit();
            var choices = j.Choices("squat");
            Check(choices.Length >= 2, "squat has alternatives: " + choices.Length);
            j.SetSwap("squat", choices.First(c => c.block.exercise_id != plan.blocks.First(b => b.id == "squat").exercise_id).block.exercise_id);
            Check(j.SaveEdit(out var err), "save edit " + err);
            Check(j.Selected.swaps.Length == 1, "swap persisted");
            // prepare
            Check(j.CanPrepare, "can prepare today");
            j.BeginPrepare(); j.Gate.SetReadiness("ready");
            Check(j.ReviewReadiness() && j.PreparedPlan.status == "draft_ready", "readiness review");
            j.Gate.Acknowledge(true);
            Check(j.CanSaveReview, "can save review");
            var session = BossController.Create(j);
            j.SaveBoss(session);
            var boss = new BossController(j.ActiveSession, j.Plans, j.SaveBoss, false, () => 0);
            Check(boss.CanLog, "boss can log");
            double t = 0;
            boss = new BossController(j.ActiveSession, j.Plans, j.SaveBoss, false, () => t);
            int guard = 0;
            while (!boss.Closed && guard++ < 60)
            {
                var b = boss.Current; Check(boss.Log(boss.NextId, b.quantity_max + 5, null), "log " + b.id + " " + boss.Error);
                t += 1000;
            }
            Check(boss.Data.state == "completed" && boss.Damage == 1000, "completed with exactly 1000 damage (extra reps add nothing): " + boss.Damage);
            j.ArchiveBoss();
            Check(j.Selected.status == "completed" && j.CompletedCount == 1, "archived completed");
            // "new draft" from a completed entry keeps a valid session type (regression)
            j.DefaultProfile = d => plans.DefaultProfileFor(d, days); j.DefaultMinutes = 25;
            j.BeginCreate(today, true);
            Check(!string.IsNullOrEmpty(j.Draft.profile) && j.Draft.profile != "u", "draft from completed entry has a session type");
            Check(j.SaveEdit(out err), "save draft copied from completed entry " + err);
            // reload from storage
            var j2 = new WorkoutJournal(plans, mem, LiveTrainingPlans.AccountProfile, today); j2.Load();
            Check(j2.Loaded && j2.CompletedCount == 1, "reload ok: " + j2.Error);
            // stopping early leaves health and no debt
            var j3mem = new Memory(); var j3 = new WorkoutJournal(plans, j3mem, "u", today); j3.Load(true); j3.EnsureScheduled(plans.Schedule(days, today, today.AddDays(6)), 25, today);
            j3.SelectDay(today); j3.BeginPrepare(); j3.Gate.SetReadiness("low_energy"); j3.ReviewReadiness(); j3.Gate.Acknowledge(true);
            Check(j3.PreparedPlan.difficulty_effective == "light", "low energy => light");
            j3.SaveBoss(BossController.Create(j3)); var b3 = new BossController(j3.ActiveSession, j3.Plans, j3.SaveBoss, false, () => 0);
            b3.Log(b3.NextId, 3, null);
            Check(b3.SetDifficulty("medium") || b3.Error != null, "difficulty change handled: " + b3.Error);
            Check(b3.Stop("tired") && b3.Damage < 1000, "stopped keeps partial damage");
            j3.ArchiveBoss(); Check(j3.Selected.status == "stopped", "stopped archived");
            // later day missed => no catch-up
            var later = new WorkoutJournal(plans, j3mem, "u", today.AddDays(3)); later.Load();
            Check(later.Loaded && later.Entries.Count(x => later.Status(x) == "missed") == 1, "Wednesday shows as missed, no extra entries: " + later.Error);
            Check(later.ConsistencyStreak == 0, "missed breaks streak");
            // teen without supervision
            var teenInput = input.Copy(); teenInput.age = 16;
            var teenPlans = new LiveTrainingPlans(engine, teenInput);
            var tj = new WorkoutJournal(teenPlans, new Memory(), "u", today); tj.Load(true); tj.EnsureScheduled(teenPlans.Schedule(days, today, today), 25, today);
            tj.SelectDay(today); tj.BeginPrepare(); tj.Gate.SetReadiness("ready"); tj.Gate.SetSupervision(false); tj.ReviewReadiness();
            Check(tj.PreparedPlan.status == "needs_review", "teen strength without supervision is not prescribed");
            tj.BeginPrepare(); tj.Gate.SetReadiness("ready"); tj.Gate.SetSupervision(true); tj.ReviewReadiness();
            Check(tj.PreparedPlan.status == "draft_ready" && tj.PreparedPlan.blocks.Where(b => b.role == "main").All(b => b.sets <= 2), "supervised teen plan <= 2 sets");
            // pain => recovery
            var pj = new WorkoutJournal(plans, new Memory(), "u", today); pj.Load(true); pj.EnsureScheduled(plans.Schedule(days, today, today), 25, today);
            pj.SelectDay(today); pj.BeginPrepare(); pj.Gate.SetReadiness("pain"); pj.ReviewReadiness();
            Check(pj.PreparedPlan.status == "recovery" && !pj.CanSaveReview, "pain => recovery, cannot enter dungeon");
            // create on rest day, change type
            var cj = new WorkoutJournal(plans, new Memory(), "u", today); cj.DefaultProfile = d => plans.DefaultProfileFor(d, days); cj.DefaultMinutes = 40; cj.Load(true);
            cj.BeginCreate(today.AddDays(1)); Check(cj.Draft.profile == "u.aerobic_base" && cj.Draft.minutes == 40, "rest-day extra = aerobic 40");
            cj.SetDraftProfile("u.mobility_reset"); Check(cj.SaveEdit(out err), "save new mobility " + err);
            // every durations x templates x setups resolvable
            var setups = new[] {
                input,
                new TrainingInput { age = 40, experience = "intermediate", goal = "strength", environment = "gym", equipment = new[] { "wall","stable_surface","dumbbells","cable_machine","lat_pulldown_machine","leg_press_machine","stationary_bike","bench" }, session_minutes = 60 },
                new TrainingInput { age = 22, experience = "beginner", goal = "endurance", environment = "outdoor", equipment = new string[0], session_minutes = 40 },
                new TrainingInput { age = 17, experience = "beginner", goal = "mobility", environment = "home", equipment = new[] { "wall", "stable_surface" }, session_minutes = 15 },
                new TrainingInput { age = 35, experience = "intermediate", goal = "muscle_growth", environment = "home", equipment = new string[0], session_minutes = 25 } };
            foreach (var s in setups)
            {
                var lp = new LiveTrainingPlans(engine, s);
                foreach (var w in new[] { new[]{0,3}, new[]{0,2,4}, new[]{0,1,3,5}, new[]{0,1,2,3,5}, new[]{0,1,2,3,4} })
                {
                    var tpl = engine.WeekTemplates(s, w);
                    for (int i = 1; i < w.Length; i++) if (tpl[i] != "aerobic_base" && tpl[i] != "mobility_reset" && tpl[i - 1] != "aerobic_base" && tpl[i - 1] != "mobility_reset") Check(w[i] - w[i - 1] >= 2, "no consecutive strength " + string.Join(",", tpl));
                    foreach (var tid in tpl.Distinct())
                        foreach (var m in LiveTrainingPlans.SessionDurations)
                        {
                            var key = TrainingKeys.Make("u." + tid, m, "ready", s.age < 18);
                            var sp = lp.Session(key);
                            Check(sp != null, "session " + key);
                            if (sp.status == "draft_ready")
                                foreach (var d in new[] { "light", "medium", "hard" }) Check(lp.Variant(key + ":" + d)?.plan != null, "variant " + key + d);
                        }
                    // scheduled kinds should usually be ready for these setups
                    var readyCount = tpl.Count(x => lp.Session(TrainingKeys.Make("u." + x, LiveTrainingPlans.Nearest(s.session_minutes), "ready", s.age < 18)).status == "draft_ready");
                    Console.WriteLine($"  setup age{s.age}/{s.goal}/{s.environment}/{s.session_minutes}m days[{string.Join(",", w)}] => {string.Join(",", tpl)} ready {readyCount}/{tpl.Length}");
                }
            }
            Console.WriteLine($"live journal tests: {passes} passed, {fails} failed");
            return fails == 0 ? 0 : 1;
        }
    }
}
