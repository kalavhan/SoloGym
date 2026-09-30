using System;
using System.Linq;

namespace SoloGym
{
    public static class TrainingSmoke
    {
        static void Check(bool passed, string message)
        { if (!passed) throw new InvalidOperationException("Training smoke: " + message); }
        public static void Verify(TrainingCatalog catalog)
        {
            Check(catalog.entries.Length == 60, "all fixture combinations exported");
            var controller = new TrainingController(catalog);
            controller.BeginReadiness();
            Check(!controller.CanReview && !controller.Review(), "readiness cannot be skipped");
            controller.SetReadiness("ready"); controller.Review();
            Check(controller.Plan.status == "draft_ready", "ready adult has a reviewable plan");
            Check(!controller.Accept(), "explicit review acknowledgment required");
            Check(controller.Plan.blocks.Any(b => b.role == "warmup") && controller.Plan.blocks.Any(b => b.role == "cooldown"), "full session preserved");
            Check(controller.Plan.messages.Any(m => m.code == "pull_coverage_gap"), "equipment gap remains visible");
            controller.Acknowledge(true); Check(controller.Accept(), "review can be accepted");
            Check(controller.AcceptedKey != null && controller.Step == TrainingStep.Hub, "accept returns to hub without workout");
            controller.BeginReadiness(); Check(controller.AcceptedKey == null && !controller.CanReview, "readiness rechecked on every visit");
            controller.SetReadiness("low_energy"); controller.Review();
            Check(controller.Plan.difficulty_effective == "light", "low energy reduces intensity");
            controller.Acknowledge(true); controller.Back();
            Check(controller.Plan == null && !controller.CanAccept, "editing invalidates acceptance");
            foreach (var stop in new[] { "pain", "injury", "ill" })
            {
                controller.BeginReadiness(); controller.SetReadiness(stop); controller.Review();
                Check(controller.Step == TrainingStep.Rest && controller.Plan.blocks.Length == 0 && !controller.CanAccept, "stop gate: " + stop);
            }
            controller.SelectProfile(Array.FindIndex(catalog.profiles, p => p.teen));
            controller.BeginReadiness(); controller.SetReadiness("ready");
            Check(!controller.CanReview, "teen supervision must be answered");
            controller.SetSupervision(false); controller.Review();
            Check(controller.Plan.status == "needs_review" && !controller.CanAccept, "unsupervised teen blocked");
            controller.Back(); controller.SetSupervision(true); controller.Review();
            Check(controller.Plan.status == "draft_ready", "supervised teen fixture reviewable");
            controller.Rest(); Check(controller.Step == TrainingStep.Rest && controller.Plan == null, "voluntary rest clears draft");
            foreach (var entry in catalog.entries)
            {
                Check(entry.session.status != "draft_ready" || entry.session.estimated_seconds <= entry.session.budget_seconds, "time budget: " + entry.key);
                Check(entry.session.blocks.All(b => !string.IsNullOrEmpty(b.name.en) && !string.IsNullOrEmpty(b.name.es)), "bilingual exercise labels");
            }
        }
    }
}
