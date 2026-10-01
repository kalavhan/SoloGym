using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SoloGym
{
    public enum GoalsExperienceStep { Goal, Experience, Review }

    [Serializable]
    public sealed class GoalCatalogEntry
    {
        public string id, en, es;
        public string Label(string language) => language == "es" ? es : en;
    }

    [Serializable]
    public sealed class ExperienceCatalogEntry
    {
        public string id, en, es;
        public string Label(string language) => language == "es" ? es : en;
    }

    [Serializable]
    sealed class GoalsExperienceCatalogFile
    {
        public GoalCatalogEntry[] goals;
        public ExperienceCatalogEntry[] experience_choices;
        public string[] teen_visible_goals;
    }

    public sealed class GoalsExperienceViewModel
    {
        public string Language, GoalId, ExperienceId, Error, ErrorKey, Status;
        public GoalsExperienceStep Step;
        public bool ReviewMode, CanContinue, IsTeenAudience, UncertaintyDialogOpen;
        public GoalCatalogEntry[] VisibleGoals;
        public ExperienceCatalogEntry[] ExperienceChoices;
        public string GoalLabel, ExperienceLabel;
        public string Copy(string key) => GoalsExperienceCopy.Get(key, Language);
    }

    /// <summary>
    /// Memory-only goal and experience draft. Teen audiences see a filtered goal list.
    /// Uncertainty requires an explicit beginner confirmation. No remote persistence.
    /// </summary>
    public sealed class GoalsExperienceController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        readonly bool reviewMode;
        readonly bool teenAudience;
        readonly GoalCatalogEntry[] allGoals;
        readonly ExperienceCatalogEntry[] experienceChoices;
        readonly HashSet<string> teenGoals;
        string language, languagePreference, goalId = "", experienceId = "", errorKey = "";
        GoalsExperienceStep step = GoalsExperienceStep.Goal;
        bool uncertaintyDialogOpen, disposed;

        public GoalsExperienceViewModel Model { get; private set; }
        public event Action<GoalsExperienceViewModel> Changed;
        public event Action ExitRequested;
        public event Action<string> CheckpointRequested;

        public GoalsExperienceController(bool reviewMode = false, bool teenAudience = false)
        {
            this.reviewMode = reviewMode;
            this.teenAudience = teenAudience;
            var catalog = LoadCatalog();
            allGoals = catalog.goals ?? Array.Empty<GoalCatalogEntry>();
            experienceChoices = catalog.experience_choices ?? Array.Empty<ExperienceCatalogEntry>();
            teenGoals = new HashSet<string>(catalog.teen_visible_goals ?? Array.Empty<string>());
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            Refresh();
        }

        static GoalsExperienceCatalogFile LoadCatalog()
        {
            var asset = Resources.Load<TextAsset>("GoalsExperience/Catalog");
            if (asset == null) throw new InvalidOperationException("GoalsExperience/Catalog.json is missing.");
            return JsonUtility.FromJson<GoalsExperienceCatalogFile>(asset.text);
        }

        public void SetLanguage(string value)
        {
            if (disposed) return;
            if (value != "es" && value != "en" && value != "auto")
                throw new ArgumentException("Language must be en, es or auto.", nameof(value));
            languagePreference = value;
            language = ResolveLanguage(value);
            PlayerPrefs.SetString(LanguageKey, value);
            PlayerPrefs.Save();
            Refresh();
        }

        public void SelectGoal(string id)
        {
            if (disposed || step != GoalsExperienceStep.Goal) return;
            if (!IsGoalVisible(id)) { Fail("goal_invalid"); return; }
            goalId = id;
            errorKey = "";
            Refresh();
        }

        public void ContinueGoal()
        {
            if (disposed || step != GoalsExperienceStep.Goal) return;
            if (goalId.Length == 0) { Fail("goal_required"); return; }
            errorKey = "";
            step = GoalsExperienceStep.Experience;
            Refresh();
        }

        public void SelectExperience(string id)
        {
            if (disposed || step != GoalsExperienceStep.Experience) return;
            if (id != "beginner" && id != "intermediate") { Fail("experience_invalid"); return; }
            experienceId = id;
            errorKey = "";
            uncertaintyDialogOpen = false;
            Refresh();
        }

        public void OpenUncertainty()
        {
            if (disposed || step != GoalsExperienceStep.Experience) return;
            uncertaintyDialogOpen = true;
            errorKey = "";
            Refresh();
        }

        public void ConfirmUncertaintyBeginner()
        {
            if (disposed || step != GoalsExperienceStep.Experience || !uncertaintyDialogOpen) return;
            experienceId = "beginner";
            uncertaintyDialogOpen = false;
            errorKey = "";
            step = GoalsExperienceStep.Review;
            Refresh();
        }

        public void CancelUncertainty()
        {
            if (disposed) return;
            uncertaintyDialogOpen = false;
            Refresh();
        }

        public void ContinueExperience()
        {
            if (disposed || step != GoalsExperienceStep.Experience) return;
            if (experienceId.Length == 0) { Fail("experience_required"); return; }
            errorKey = "";
            step = GoalsExperienceStep.Review;
            Refresh();
        }

        public void EditGoal()
        {
            if (disposed) return;
            uncertaintyDialogOpen = false;
            step = GoalsExperienceStep.Goal;
            errorKey = "";
            Refresh();
        }

        public void EditExperience()
        {
            if (disposed) return;
            uncertaintyDialogOpen = false;
            step = GoalsExperienceStep.Experience;
            errorKey = "";
            Refresh();
        }

        public void ContinueReview()
        {
            if (disposed || step != GoalsExperienceStep.Review) return;
            if (!reviewMode) { Fail("training_setup_pending"); return; }
            if (goalId.Length == 0 || experienceId.Length == 0)
            {
                Fail("review_incomplete");
                return;
            }
            errorKey = "";
            CheckpointRequested?.Invoke("REVIEW:WIN-011");
            Refresh();
        }

        public void Back()
        {
            if (disposed) return;
            errorKey = "";
            uncertaintyDialogOpen = false;
            switch (step)
            {
                case GoalsExperienceStep.Experience:
                    step = GoalsExperienceStep.Goal;
                    break;
                case GoalsExperienceStep.Review:
                    step = GoalsExperienceStep.Experience;
                    break;
                default:
                    Decline();
                    return;
            }
            Refresh();
        }

        public void Decline()
        {
            if (disposed) return;
            ResetDraft();
            Refresh();
            ExitRequested?.Invoke();
        }

        bool IsGoalVisible(string id)
        {
            foreach (var goal in VisibleGoals())
            {
                if (goal.id == id) return true;
            }
            return false;
        }

        GoalCatalogEntry[] VisibleGoals()
        {
            if (!teenAudience) return allGoals;
            var list = new List<GoalCatalogEntry>();
            foreach (var goal in allGoals)
            {
                if (teenGoals.Contains(goal.id)) list.Add(goal);
            }
            return list.ToArray();
        }

        string LabelForGoal(string id)
        {
            foreach (var goal in allGoals)
            {
                if (goal.id == id) return goal.Label(language);
            }
            return id;
        }

        string LabelForExperience(string id)
        {
            foreach (var entry in experienceChoices)
            {
                if (entry.id == id) return entry.Label(language);
            }
            return id;
        }

        public void Reset()
        {
            if (disposed) return;
            ResetDraft();
            Refresh();
        }

        void ResetDraft()
        {
            goalId = experienceId = errorKey = "";
            step = GoalsExperienceStep.Goal;
            uncertaintyDialogOpen = false;
        }

        static string ResolveLanguage(string preference)
        {
            if (preference == "en" || preference == "es") return preference;
            return Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
        }

        void Fail(string key)
        {
            errorKey = key;
            Refresh();
        }

        void Refresh()
        {
            if (disposed) return;
            var visible = VisibleGoals();
            if (teenAudience && goalId.Length != 0 && !teenGoals.Contains(goalId))
                goalId = "";
            Model = new GoalsExperienceViewModel
            {
                Language = language,
                GoalId = goalId,
                ExperienceId = experienceId,
                Step = step,
                ErrorKey = errorKey,
                Error = GoalsExperienceCopy.Get(errorKey, language),
                ReviewMode = reviewMode,
                Status = reviewMode ? GoalsExperienceCopy.Get("review_mode", language) : "",
                IsTeenAudience = teenAudience,
                UncertaintyDialogOpen = uncertaintyDialogOpen,
                VisibleGoals = visible,
                ExperienceChoices = experienceChoices,
                GoalLabel = goalId.Length == 0 ? "" : LabelForGoal(goalId),
                ExperienceLabel = experienceId.Length == 0 ? "" : LabelForExperience(experienceId),
                CanContinue = step == GoalsExperienceStep.Goal ? goalId.Length != 0
                    : step == GoalsExperienceStep.Experience ? experienceId.Length != 0
                    : step == GoalsExperienceStep.Review && goalId.Length != 0 && experienceId.Length != 0
            };
            Changed?.Invoke(Model);
        }

        public void Dispose()
        {
            if (disposed) return;
            ResetDraft();
            disposed = true;
            Model = null;
            Changed = null;
            ExitRequested = null;
            CheckpointRequested = null;
        }
    }

    public static class GoalsExperienceCopy
    {
        static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            { "window_title", new[] { "GOALS & EXPERIENCE", "OBJETIVOS Y EXPERIENCIA" } },
            { "goal_title", new[] { "YOUR PATH", "TU RUTA" } },
            { "goal_subtitle", new[] { "Choose your training focus", "Elige tu enfoque de entrenamiento" } },
            { "goal_footer", new[] { "You can change this at any time.", "Podrás cambiarlo cuando quieras." } },
            { "experience_title", new[] { "YOUR STARTING POINT", "TU PUNTO DE PARTIDA" } },
            { "experience_subtitle", new[] { "Tell us about your training experience", "Cuéntanos tu experiencia con el entrenamiento" } },
            { "uncertainty_link", new[] { "I'm not sure", "No estoy seguro/a" } },
            { "uncertainty_title", new[] { "Start with the basics?", "¿Empezar con la base?" } },
            { "uncertainty_body", new[] { "We can begin with a beginner starting point. You can adjust this later. This does not assess your ability.", "Podemos comenzar con un punto de partida para principiantes. Podrás ajustarlo más adelante. Esto no evalúa tu capacidad." } },
            { "uncertainty_confirm", new[] { "USE BEGINNER STARTING POINT", "USAR BASE PARA PRINCIPIANTES" } },
            { "uncertainty_cancel", new[] { "GO BACK", "VOLVER" } },
            { "review_title", new[] { "REVIEW AND CONTINUE", "REVISAR Y CONTINUAR" } },
            { "review_focus", new[] { "Focus", "Enfoque" } },
            { "review_experience", new[] { "Experience", "Experiencia" } },
            { "change_goal", new[] { "Change focus", "Cambiar enfoque" } },
            { "change_experience", new[] { "Change experience", "Cambiar experiencia" } },
            { "review_body", new[] { "You'll still choose equipment and schedule before sessions are prepared. Nothing is saved to your account in this preview.", "Todavía elegirás equipo y horario antes de preparar las sesiones. En esta vista previa no se guarda nada en tu cuenta." } },
            { "review_progress", new[] { "Changing your focus later won't erase your progress.", "Cambiar el enfoque más adelante no borra tu progreso." } },
            { "teen_helper", new[] { "For your age, these training focuses are available.", "Para tu edad, estos enfoques están disponibles." } },
            { "continue", new[] { "CONTINUE", "CONTINUAR" } },
            { "back", new[] { "Back", "Volver" } },
            { "not_now", new[] { "Not now", "Ahora no" } },
            { "review_mode", new[] { "REVIEW · fictional data; nothing saved", "REVISIÓN · datos ficticios; no se guardan" } },
            { "goal_required", new[] { "Choose a training focus to continue.", "Elige un enfoque de entrenamiento para continuar." } },
            { "experience_required", new[] { "Choose an experience level, or use the not-sure option.", "Elige un nivel de experiencia o usa la opción de no estar seguro/a." } },
            { "goal_invalid", new[] { "That focus is not available for your profile.", "Ese enfoque no está disponible para tu perfil." } },
            { "experience_invalid", new[] { "Choose one of the listed experience options.", "Elige una de las opciones de experiencia listadas." } },
            { "review_incomplete", new[] { "Choose both a focus and experience before continuing.", "Elige un enfoque y una experiencia antes de continuar." } },
            { "training_setup_pending", new[] { "Training setup is not available yet. The private profile service and production gates must be ready before saving choices.", "La configuración del entrenamiento aún no está disponible. El servicio de perfil privado y las condiciones de producción deben estar listos antes de guardar las elecciones." } },
            { "next_window_notice_title", new[] { "Next step", "Siguiente paso" } },
            { "next_window_notice_body", new[] { "Available equipment is the next window. Nothing was saved to your account.", "El equipo disponible es la siguiente ventana. No se guardaron datos en tu cuenta." } }
        };

        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }

    public static class GoalsExperienceStateChecks
    {
        public static string Run()
        {
            const string languageKey = "SoloGym.Home.Language.v1";
            string originalLanguage = PlayerPrefs.GetString(languageKey, "auto");
            int passed = 0;
            try
            {
                using (var normal = new GoalsExperienceController())
                {
                    string checkpoint = null;
                    normal.CheckpointRequested += id => checkpoint = id;
                    normal.SelectGoal("general_fitness");
                    normal.ContinueGoal();
                    normal.SelectExperience("beginner");
                    normal.ContinueExperience();
                    normal.ContinueReview();
                    Require(normal.Model.ErrorKey == "training_setup_pending" && checkpoint == null,
                        "production flow cannot authorize checkpoint"); passed++;
                }
                using (var adult = new GoalsExperienceController(true))
                {
                    Require(adult.Model.GoalId == "" && adult.Model.Step == GoalsExperienceStep.Goal,
                        "initial goal empty"); passed++;
                    adult.ContinueGoal();
                    Require(adult.Model.ErrorKey == "goal_required", "goal required"); passed++;
                    adult.SelectGoal("strength");
                    adult.ContinueGoal();
                    adult.ContinueExperience();
                    Require(adult.Model.ErrorKey == "experience_required", "experience required"); passed++;
                    adult.OpenUncertainty();
                    Require(adult.Model.UncertaintyDialogOpen, "uncertainty opens");
                    adult.CancelUncertainty();
                    adult.SelectExperience("intermediate");
                    adult.ContinueExperience();
                    Require(adult.Model.Step == GoalsExperienceStep.Review
                        && adult.Model.GoalId == "strength" && adult.Model.ExperienceId == "intermediate",
                        "review state"); passed++;
                    adult.EditGoal();
                    adult.SelectGoal("endurance");
                    adult.ContinueGoal();
                    adult.EditExperience();
                    adult.OpenUncertainty();
                    adult.ConfirmUncertaintyBeginner();
                    Require(adult.Model.ExperienceId == "beginner" && adult.Model.Step == GoalsExperienceStep.Review,
                        "uncertainty sets beginner explicitly"); passed++;
                    string checkpoint = null;
                    adult.CheckpointRequested += id => checkpoint = id;
                    adult.ContinueReview();
                    Require(checkpoint == "REVIEW:WIN-011", "review checkpoint"); passed++;
                    adult.Back();
                    Require(adult.Model.Step == GoalsExperienceStep.Experience && adult.Model.GoalId == "endurance",
                        "back preserves draft"); passed++;
                    adult.SetLanguage("es");
                    Require(adult.Model.Language == "es", "language switch"); passed++;
                    adult.Decline();
                    Require(adult.Model.GoalId == "" && adult.Model.Step == GoalsExperienceStep.Goal,
                        "exit clears draft"); passed++;
                }
                using (var teen = new GoalsExperienceController(true, true))
                {
                    Require(teen.Model.VisibleGoals.Length == 2, "teen sees two goals");
                    teen.SelectGoal("strength");
                    Require(teen.Model.ErrorKey == "goal_invalid", "teen cannot select strength");
                    teen.SelectGoal("mobility");
                    teen.ContinueGoal();
                    teen.SelectExperience("beginner");
                    teen.ContinueExperience();
                    Require(teen.Model.GoalId == "mobility", "teen mobility path"); passed++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(languageKey, originalLanguage);
                PlayerPrefs.Save();
            }
            return passed + " goals experience state checks passed";
        }

        static void Require(bool condition, string context)
        {
            if (!condition) throw new InvalidOperationException("Goals experience check failed: " + context);
        }
    }
}
