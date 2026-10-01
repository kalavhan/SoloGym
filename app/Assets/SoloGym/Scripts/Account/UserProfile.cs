using System;
using System.Globalization;
using System.Linq;
using SoloGym.Training;

namespace SoloGym
{
    /// <summary>
    /// A signed-in person's private SoloGym profile and training setup, stored on this device
    /// under their account. Appearance (characterId) never feeds the training input.
    /// </summary>
    [Serializable]
    public sealed class UserProfile
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public string accountId, email, displayName, createdUtc, updatedUtc;
        // Eligibility and consent as chosen on this device (test build: no server receipt yet).
        public int age;
        public string countryCode = "", subdivisionCode = "";
        public string consentPrivacyRevision = "", consentTermsRevision = "", consentUtc = "";
        public bool consentDocumentsArePlaceholders = true;
        // Cosmetic only.
        public string characterId = "male-medium";
        // Optional private measurements.
        public bool hasHeight, hasWeight;
        public double heightCm, weightKg;
        public string unitSystem = "metric";
        public string setupReadiness = "";
        // Training setup.
        public string goal = "", experience = "";
        public string environment = "";
        public string[] equipment = Array.Empty<string>();
        public bool bodyweightOnly;
        public int[] weekdays = Array.Empty<int>();
        public int sessionMinutes = 25;
        public string planAcceptedUtc = "", planStartDate = "";
        public bool setupComplete;
        public bool teenSupervisionNoted;

        public bool IsTeen => age >= 15 && age < 18;
        public bool IsAdult => age >= 18;
        public string PreferredName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName)) return displayName.Trim().Split(' ')[0];
                if (!string.IsNullOrWhiteSpace(email)) { var local = email.Split('@')[0]; return local.Length > 14 ? local.Substring(0, 14) : local; }
                return "Aventurero";
            }
        }

        public TrainingInput ToTrainingInput() => new TrainingInput
        {
            age = age, goal = goal, experience = experience, environment = environment,
            equipment = bodyweightOnly ? Array.Empty<string>() : (equipment ?? Array.Empty<string>()).ToArray(),
            session_minutes = LiveTrainingPlans.Nearest(sessionMinutes), difficulty = "medium", readiness = "ready"
        };

        /// <summary>Empty when complete enough to generate a plan; otherwise the first missing field.</summary>
        public string MissingSetup()
        {
            if (age < 15 || age > 120) return "age";
            if (string.IsNullOrEmpty(countryCode)) return "country";
            if (string.IsNullOrEmpty(consentUtc)) return "consent";
            if (string.IsNullOrEmpty(goal)) return "goal";
            if (IsTeen && goal != "general_fitness" && goal != "mobility") return "goal";
            if (experience != "beginner" && experience != "intermediate") return "experience";
            if (environment != "home" && environment != "gym" && environment != "outdoor") return "environment";
            var days = weekdays ?? Array.Empty<int>();
            if (days.Length < 2 || days.Length > 5 || days.Any(d => d < 0 || d > 6) || days.Distinct().Count() != days.Length) return "schedule";
            if (!LiveTrainingPlans.SessionDurations.Contains(sessionMinutes)) return "duration";
            return "";
        }

        public void Normalize()
        {
            equipment = (equipment ?? Array.Empty<string>()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
            weekdays = (weekdays ?? Array.Empty<int>()).Distinct().OrderBy(x => x).ToArray();
            countryCode = countryCode ?? ""; subdivisionCode = subdivisionCode ?? ""; goal = goal ?? ""; experience = experience ?? ""; environment = environment ?? "";
            if (!LiveTrainingPlans.SessionDurations.Contains(sessionMinutes)) sessionMinutes = LiveTrainingPlans.Nearest(sessionMinutes);
        }

        public static string NowUtc() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    }
}
