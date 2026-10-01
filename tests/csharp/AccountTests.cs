using System;
using System.IO;
using System.Linq;
using SoloGym;
using SoloGym.Training;
namespace Tests
{
    public static class Program
    {
        static int fails, passes;
        static void Check(bool ok, string what) { if (ok) passes++; else { fails++; Console.WriteLine("FAIL " + what); } }
        public static int Main()
        {
            UnityEngine.Resources.Loader = path => {
                var f = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("SOLOGYM_ROOT"), "app/Assets/SoloGym/Resources/" + path + ".json");
                return File.Exists(f) ? File.ReadAllText(f) : null; };
            string root = Path.Combine(Path.GetTempPath(), "sologym-account-test-" + Guid.NewGuid().ToString("N"));
            var user = new IdentityUser { UserId = "uid-123", Email = "josue@example.com" };
            var s = AccountSession.Begin(user, root);
            Check(!s.SetupComplete && s.Profile.accountId == "uid-123", "new profile incomplete");
            Check(s.Profile.PreferredName == "josue", "name from email: " + s.Profile.PreferredName);
            var p = s.Profile;
            p.age = 29; p.countryCode = "MX"; p.consentUtc = UserProfile.NowUtc(); p.setupReadiness = "ready"; p.characterId = "female-muscular";
            p.goal = "strength"; p.experience = "intermediate"; p.environment = "home"; p.equipment = new[] { "dumbbells", "wall", "stable_surface", "chair" };
            p.weekdays = new[] { 4, 0, 2 }; p.sessionMinutes = 40;
            Check(p.MissingSetup() == "", "complete setup: " + p.MissingSetup());
            var monday = new DateTime(2026, 10, 5);
            Check(s.AcceptPlan(monday, out var err), "accept plan " + err);
            Check(File.Exists(s.ProfilePath) && File.Exists(s.JournalPath), "profile + journal saved");
            AccountSession.End(false);
            var again = AccountSession.Begin(user, root);
            Check(again.SetupComplete && again.Profile.characterId == "female-muscular" && again.Profile.weekdays.SequenceEqual(new[] { 0, 2, 4 }), "profile reloads, weekdays normalized");
            var j = again.OpenJournal(monday);
            Check(j.Loaded, "journal loads " + j.Error);
            Check(j.Entries.Length == 6, "2 weeks of 3 days: " + j.Entries.Length);
            var today = j.TodayEntry; Check(today != null && j.Plan(today).status == "draft_ready" && j.Plan(today).budget_seconds == 2400, "today's 40-min plan ready");
            Check(j.Plan(today).blocks.Any(b => b.exercise_id.StartsWith("dumbbell")), "uses dumbbells");
            // complete today's session
            j.SelectDay(monday); j.BeginPrepare(); j.Gate.SetReadiness("ready"); j.ReviewReadiness(); j.Gate.Acknowledge(true);
            j.SaveBoss(BossController.Create(j)); double t = 0; var b = new BossController(j.ActiveSession, j.Plans, j.SaveBoss, false, () => t);
            while (!b.Closed) { b.Log(b.NextId, b.Current.quantity_min, 10); t += 999; }
            j.ArchiveBoss(); Check(j.CompletedCount == 1, "completed");
            // user changes equipment and days: history kept, future replaced
            again.Profile.equipment = new string[0]; again.Profile.bodyweightOnly = true; again.Profile.weekdays = new[] { 1, 3 }; again.Profile.sessionMinutes = 25;
            Check(again.AcceptPlan(monday, out err), "re-accept " + err);
            var j2 = again.OpenJournal(monday);
            Check(j2.Loaded && j2.CompletedCount == 1, "history kept after setup change: " + j2.Error);
            var future = j2.Entries.Where(e => e.status == "planned").ToArray();
            Check(future.All(e => { var d = WorkoutJournal.ParseDate(e.date).DayOfWeek; return d == DayOfWeek.Tuesday || d == DayOfWeek.Thursday; }), "future follows new days: " + string.Join(",", future.Select(e => e.date)));
            Check(future.All(e => j2.Plan(e).blocks.All(x => !x.exercise_id.StartsWith("dumbbell"))), "future uses bodyweight only");
            // a different account gets a separate folder
            AccountSession.End(false);
            var other = AccountSession.Begin(new IdentityUser { UserId = "uid-999" }, root);
            Check(!other.SetupComplete && other.Folder != again.Folder, "accounts are separated");
            // registration service gates on on-device setup and Firebase config
            var reg = new FirebaseAccountRegistrationService(() => true);
            var r = reg.CheckSetupAsync(System.Threading.CancellationToken.None).Result;
            Check(r.Status == RegistrationStatus.Unavailable, "unconfigured Firebase => unavailable: " + r.Status);
            Directory.Delete(root, true);
            Console.WriteLine($"account tests: {passes} passed, {fails} failed");
            return fails == 0 ? 0 : 1;
        }
    }
}
