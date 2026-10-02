using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SoloGym;
using SoloGym.Training;
namespace Tests
{
    public static class Program
    {
        static readonly JsonSerializerOptions O = new JsonSerializerOptions { IncludeFields = true };
        public static int Main()
        {
            string root = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("SOLOGYM_ROOT"), "app/Assets/SoloGym/Resources/Training/Rules/");
            var engine = new TrainingEngine(
                JsonSerializer.Deserialize<ExerciseCatalogData>(File.ReadAllText(root + "exercises.json"), O),
                JsonSerializer.Deserialize<TemplateData>(File.ReadAllText(root + "templates.json"), O),
                JsonSerializer.Deserialize<TrainingRulesData>(File.ReadAllText(root + "rules.json"), O));
            using var doc = JsonDocument.Parse(File.ReadAllText(System.Environment.GetEnvironmentVariable("SOLOGYM_PARITY_JSON")));
            int n = 0, bad = 0;
            foreach (var c in doc.RootElement.EnumerateArray())
            {
                var p = c.GetProperty("profile");
                var input = new TrainingInput {
                    age = p.GetProperty("age").GetInt32(), experience = p.GetProperty("experience").GetString(), goal = p.GetProperty("goal").GetString(),
                    environment = p.GetProperty("environment").GetString(), equipment = p.GetProperty("equipment").EnumerateArray().Select(x => x.GetString()).ToArray(),
                    session_minutes = p.GetProperty("session_minutes").GetInt32(), difficulty = p.GetProperty("difficulty").GetString(), readiness = p.GetProperty("readiness").GetString(),
                    teen_supervision_available = p.GetProperty("teen_supervision_available").GetBoolean() };
                var t = c.GetProperty("template").GetString();
                var s = c.GetProperty("session");
                var mine = engine.GenerateSession(input, t, new DateTime(2026, 9, 28), Array.Empty<string>());
                string Expect() {
                    var blocks = string.Join("|", s.GetProperty("blocks").EnumerateArray().Select(b => $"{b.GetProperty("id").GetString()},{b.GetProperty("exercise_id").GetString()},{b.GetProperty("sets").GetInt32()},{b.GetProperty("quantity_min").GetInt32()},{b.GetProperty("quantity_max").GetInt32()},{b.GetProperty("rest_seconds").GetInt32()},{b.GetProperty("estimated_seconds").GetInt32()},{b.GetProperty("boss_share").GetInt32()},{b.GetProperty("per_side").GetBoolean()}"));
                    var msgs = string.Join(",", s.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("code").GetString()));
                    var gaps = s.TryGetProperty("coverage_gaps", out var g) ? string.Join(",", g.EnumerateArray().Select(x => x.GetString())) : "";
                    return $"{s.GetProperty("status").GetString()};{s.GetProperty("kind").GetString()};{s.GetProperty("difficulty_effective").GetString()};{s.GetProperty("estimated_seconds").GetInt32()};{blocks};{msgs};{gaps}";
                }
                string Actual() {
                    var blocks = string.Join("|", mine.blocks.Select(b => $"{b.id},{b.exercise_id},{b.sets},{b.quantity_min},{b.quantity_max},{b.rest_seconds},{b.estimated_seconds},{b.boss_share},{b.per_side}"));
                    return $"{mine.status};{mine.kind};{mine.difficulty_effective};{mine.estimated_seconds};{blocks};{string.Join(",", mine.messages.Select(m => m.code))};{string.Join(",", mine.coverage_gaps)}";
                }
                n++;
                string e = Expect(), a = Actual();
                if (e != a) { if (bad++ < 5) Console.WriteLine($"MISMATCH {t} {p}\n  py: {e}\n  cs: {a}"); }
            }
            Console.WriteLine($"engine parity: {n - bad}/{n} sessions identical");
            return bad == 0 ? 0 : 1;
        }
    }
}
