using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    public enum ScheduleStep { Setup, Review }

    [Serializable]
    public sealed class WeekdayCatalogEntry
    {
        public int index;
        public string en, es;
        public string Label(string language) => language == "es" ? es : en;
    }

    [Serializable]
    sealed class ScheduleConfigFile
    {
        public int sessions_per_week_min = 2;
        public int sessions_per_week_max = 5;
        public int session_minutes_min = 15;
        public int session_minutes_max = 60;
        public int availability_min_minutes = 15;
        public int availability_max_minutes = 720;
        public int availability_max_hours = 12;
        public int minute_max = 60;
        public WeekdayCatalogEntry[] weekdays;
    }

    public sealed class ScheduleDaySummary
    {
        public int Index;
        public string DayLabel;
        public bool IsTraining;
    }

    public sealed class ScheduleViewModel
    {
        public string Language, Error, ErrorKey, Status;
        public ScheduleStep Step;
        public bool ReviewMode, CanContinue;
        public int SessionMinutesDerived, SessionsPerWeek;
        public int AvailabilityHours, AvailabilityMinutes, AvailabilityBlockMinutes;
        public int[] SelectedDayIndices;
        public int[] HourOptions;
        public int[] MinuteOptions;
        public WeekdayCatalogEntry[] Weekdays;
        public ScheduleDaySummary[] WeekSummary;
        public string[] TrainingDayLabels;
        public string[] RecoveryDayLabels;
        public string AvailabilitySummary;
        public string Copy(string key) => ScheduleCopy.Get(key, Language);
    }

    /// <summary>Memory-only schedule draft: weekdays, availability window, derived session_minutes for generator placeholder.</summary>
    public sealed class ScheduleController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        readonly bool reviewMode;
        readonly HashSet<int> selectedDays = new HashSet<int>();
        WeekdayCatalogEntry[] weekdays = Array.Empty<WeekdayCatalogEntry>();
        int[] hourOptions = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        int[] minuteOptions = new int[61];
        int sessionsMin = 2, sessionsMax = 5;
        int derivedMin = 15, derivedMax = 60;
        int availabilityMin = 15, availabilityMax = 720, availabilityMaxHours = 12, minuteMax = 60;
        string language, languagePreference, errorKey = "";
        int availabilityHours = 1, availabilityMinutes;
        bool disposed;
        ScheduleStep step = ScheduleStep.Setup;

        public ScheduleViewModel Model { get; private set; }
        public event Action<ScheduleViewModel> Changed;
        public event Action ExitRequested;
        public event Action<string> CheckpointRequested;

        public ScheduleController(bool reviewMode = false)
        {
            this.reviewMode = reviewMode;
            for (int i = 0; i < minuteOptions.Length; i++) minuteOptions[i] = i;
            LoadConfig();
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            Refresh();
        }

        void LoadConfig()
        {
            var asset = Resources.Load<TextAsset>("Schedule/Config");
            if (asset == null) return;
            var config = JsonUtility.FromJson<ScheduleConfigFile>(asset.text);
            if (config == null) return;
            sessionsMin = config.sessions_per_week_min;
            sessionsMax = config.sessions_per_week_max;
            derivedMin = config.session_minutes_min;
            derivedMax = config.session_minutes_max;
            availabilityMin = config.availability_min_minutes;
            availabilityMax = config.availability_max_minutes;
            availabilityMaxHours = config.availability_max_hours;
            if (config.minute_max > 0) minuteMax = config.minute_max;
            minuteOptions = new int[minuteMax + 1];
            for (int i = 0; i <= minuteMax; i++) minuteOptions[i] = i;
            hourOptions = Enumerable.Range(0, availabilityMaxHours + 1).ToArray();
            weekdays = config.weekdays ?? Array.Empty<WeekdayCatalogEntry>();
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

        public void ToggleDay(int index)
        {
            if (disposed || step != ScheduleStep.Setup) return;
            if (index < 0 || index > 6) { Fail("day_invalid"); return; }
            if (selectedDays.Contains(index)) selectedDays.Remove(index);
            else
            {
                if (selectedDays.Count >= sessionsMax) { Fail("days_max"); return; }
                selectedDays.Add(index);
            }
            errorKey = "";
            Refresh();
        }

        public void SetAvailabilityHours(int hours)
        {
            if (disposed || step != ScheduleStep.Setup) return;
            if (hours < 0 || hours > availabilityMaxHours) { Fail("availability_invalid"); return; }
            if (hours == availabilityHours && string.IsNullOrEmpty(errorKey)) return;
            availabilityHours = hours;
            NormalizeAvailability();
            errorKey = "";
            Refresh();
        }

        public void SetAvailabilityMinutes(int minutes)
        {
            if (disposed || step != ScheduleStep.Setup) return;
            if (minutes < 0 || minutes > minuteMax) { Fail("availability_invalid"); return; }
            if (minutes == availabilityMinutes && string.IsNullOrEmpty(errorKey)) return;
            availabilityMinutes = minutes;
            NormalizeAvailability();
            errorKey = "";
            Refresh();
        }

        void NormalizeAvailability()
        {
            if (availabilityHours > availabilityMaxHours)
                availabilityHours = availabilityMaxHours;
            if (availabilityMinutes > minuteMax)
                availabilityMinutes = minuteMax;
            int total = availabilityHours * 60 + availabilityMinutes;
            if (total > availabilityMax)
            {
                availabilityHours = availabilityMax / 60;
                availabilityMinutes = availabilityMax % 60;
            }
        }

        public void ContinueSetup()
        {
            if (disposed || step != ScheduleStep.Setup) return;
            if (!ValidateDayCount() || !ValidateAvailability()) return;
            errorKey = "";
            if (!reviewMode) { Fail("training_setup_pending"); return; }
            CheckpointRequested?.Invoke("REVIEW:WIN-013");
            Refresh();
        }

        public void EditSchedule()
        {
            if (disposed) return;
            step = ScheduleStep.Setup;
            errorKey = "";
            Refresh();
        }

        public void ContinueReview()
        {
            ContinueSetup();
        }

        public void Back()
        {
            if (disposed) return;
            errorKey = "";
            Decline();
        }

        public void Decline()
        {
            if (disposed) return;
            ResetDraft();
            Refresh();
            ExitRequested?.Invoke();
        }

        bool ValidateDayCount()
        {
            int count = selectedDays.Count;
            if (count < sessionsMin) { Fail("days_min"); return false; }
            if (count > sessionsMax) { Fail("days_max"); return false; }
            return true;
        }

        bool ValidateAvailability()
        {
            int total = AvailabilityBlockMinutes();
            if (total < availabilityMin) { Fail("availability_min"); return false; }
            if (total > availabilityMax) { Fail("availability_max"); return false; }
            return true;
        }

        int AvailabilityBlockMinutes() => availabilityHours * 60 + availabilityMinutes;

        int SessionMinutesDerived() => Mathf.Clamp(AvailabilityBlockMinutes(), derivedMin, derivedMax);

        ScheduleDaySummary[] BuildWeekSummary()
        {
            var list = new List<ScheduleDaySummary>();
            foreach (var entry in weekdays.OrderBy(w => w.index))
            {
                list.Add(new ScheduleDaySummary
                {
                    Index = entry.index,
                    DayLabel = entry.Label(language),
                    IsTraining = selectedDays.Contains(entry.index)
                });
            }
            return list.ToArray();
        }

        string FormatAvailabilitySummary()
        {
            int total = AvailabilityBlockMinutes();
            int h = total / 60;
            int m = total % 60;
            if (language == "es")
            {
                if (h == 0) return string.Format(ScheduleCopy.Get("availability_summary_minutes_only", language), m);
                if (m == 0) return string.Format(ScheduleCopy.Get("availability_summary_hours_only", language), h);
                return string.Format(ScheduleCopy.Get("availability_summary", language), h, m);
            }
            if (h == 0) return string.Format(ScheduleCopy.Get("availability_summary_minutes_only", language), m);
            if (m == 0) return string.Format(ScheduleCopy.Get("availability_summary_hours_only", language), h);
            return string.Format(ScheduleCopy.Get("availability_summary", language), h, m);
        }

        void ResetDraft()
        {
            selectedDays.Clear();
            availabilityHours = 1;
            availabilityMinutes = 0;
            errorKey = "";
            step = ScheduleStep.Setup;
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
            selectedDays.RemoveWhere(i => i < 0 || i > 6);
            bool daysComplete = selectedDays.Count >= sessionsMin && selectedDays.Count <= sessionsMax;
            bool availabilityComplete = ValidateAvailabilitySilent();
            var summary = step == ScheduleStep.Review ? BuildWeekSummary() : Array.Empty<ScheduleDaySummary>();
            Model = new ScheduleViewModel
            {
                Language = language,
                Step = step,
                ErrorKey = errorKey,
                Error = ScheduleCopy.Get(errorKey, language),
                ReviewMode = reviewMode,
                Status = reviewMode ? ScheduleCopy.Get("review_mode", language) : "",
                AvailabilityHours = availabilityHours,
                AvailabilityMinutes = availabilityMinutes,
                AvailabilityBlockMinutes = AvailabilityBlockMinutes(),
                SessionMinutesDerived = SessionMinutesDerived(),
                SessionsPerWeek = selectedDays.Count,
                SelectedDayIndices = selectedDays.OrderBy(i => i).ToArray(),
                HourOptions = hourOptions,
                MinuteOptions = minuteOptions,
                Weekdays = weekdays,
                WeekSummary = summary,
                TrainingDayLabels = summary.Where(d => d.IsTraining).Select(d => d.DayLabel).ToArray(),
                RecoveryDayLabels = summary.Where(d => !d.IsTraining).Select(d => d.DayLabel).ToArray(),
                AvailabilitySummary = availabilityComplete ? FormatAvailabilitySummary() : "",
                CanContinue = step == ScheduleStep.Setup && daysComplete && availabilityComplete
            };
            Changed?.Invoke(Model);
        }

        bool ValidateAvailabilitySilent()
        {
            int total = AvailabilityBlockMinutes();
            return total >= availabilityMin && total <= availabilityMax;
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

    public static class ScheduleCopy
    {
        static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            { "window_title", new[] { "SCHEDULE", "HORARIO" } },
            { "days_title", new[] { "YOUR WEEK", "TU SEMANA" } },
            { "days_subtitle", new[] { "Choose the days you can realistically train", "Elige los días en los que puedes entrenar con realismo" } },
            { "time_title", new[] { "TIME AVAILABLE", "TIEMPO DISPONIBLE" } },
            { "time_subtitle", new[] { "Hours and minutes you can use for training on those days", "Horas y minutos que puedes dedicar al entrenamiento esos días" } },
            { "hours_label", new[] { "Hours", "Horas" } },
            { "minutes_label", new[] { "Minutes", "Minutos" } },
            { "setup_helper", new[] { "You can pause between sets and return later within this window. School, sports and daily movement still count separately.", "Puedes pausar entre series y volver más tarde dentro de este tiempo. La escuela, el deporte y el movimiento diario cuentan aparte." } },
            { "review_title", new[] { "REVIEW YOUR ROUTINE", "REVISA TU RUTINA" } },
            { "review_training", new[] { "Training", "Entrenamiento" } },
            { "review_recovery", new[] { "Recovery", "Recuperación" } },
            { "availability_summary", new[] { "About {0} h {1} min available on each training day", "Unas {0} h {1} min disponibles en cada día de entrenamiento" } },
            { "availability_summary_hours_only", new[] { "About {0} h available on each training day", "Unas {0} h disponibles en cada día de entrenamiento" } },
            { "availability_summary_minutes_only", new[] { "About {0} min available on each training day", "Unos {0} min disponibles en cada día de entrenamiento" } },
            { "review_body", new[] { "You'll still customize your character before training begins. Nothing is saved to your account in this preview.", "Todavía personalizarás tu personaje antes de empezar a entrenar. En esta vista previa no se guarda nada en tu cuenta." } },
            { "change_schedule", new[] { "Change schedule", "Cambiar horario" } },
            { "continue", new[] { "CONTINUE", "CONTINUAR" } },
            { "back", new[] { "Back", "Volver" } },
            { "not_now", new[] { "Not now", "Ahora no" } },
            { "review_mode", new[] { "REVIEW · fictional data; nothing saved", "REVISIÓN · datos ficticios; no se guardan" } },
            { "days_min", new[] { "Choose at least two training days.", "Elige al menos dos días de entrenamiento." } },
            { "days_max", new[] { "Choose at most five training days.", "Elige como máximo cinco días de entrenamiento." } },
            { "day_invalid", new[] { "That day is not available.", "Ese día no está disponible." } },
            { "availability_min", new[] { "Choose at least 15 minutes of available time.", "Elige al menos 15 minutos de tiempo disponible." } },
            { "availability_max", new[] { "Choose at most 12 hours of available time.", "Elige como máximo 12 horas de tiempo disponible." } },
            { "availability_invalid", new[] { "That time is not available.", "Ese tiempo no está disponible." } },
            { "review_incomplete", new[] { "Choose days and available time before continuing.", "Elige días y tiempo disponible antes de continuar." } },
            { "training_setup_pending", new[] { "Training setup is not available yet. The private profile service and production gates must be ready before saving choices.", "La configuración del entrenamiento aún no está disponible. El servicio de perfil privado y las condiciones de producción deben estar listos antes de guardar las elecciones." } },
            { "next_window_notice_title", new[] { "Next step", "Siguiente paso" } },
            { "next_window_notice_body", new[] { "Character customization is the next window. Nothing was saved to your account.", "La personalización del personaje es la siguiente ventana. No se guardaron datos en tu cuenta." } }
        };

        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }

    public static class ScheduleStateChecks
    {
        public static string Run()
        {
            const string languageKey = "SoloGym.Home.Language.v1";
            string originalLanguage = PlayerPrefs.GetString(languageKey, "auto");
            int passed = 0;
            try
            {
                using (var normal = new ScheduleController())
                {
                    string checkpoint = null;
                    normal.CheckpointRequested += id => checkpoint = id;
                    normal.ToggleDay(0);
                    normal.ToggleDay(2);
                    normal.SetAvailabilityHours(0);
                    normal.SetAvailabilityMinutes(30);
                    normal.ContinueSetup();
                    Require(normal.Model.ErrorKey == "training_setup_pending",
                        "production cannot checkpoint"); passed++;
                }
                using (var review = new ScheduleController(true))
                {
                    string checkpoint = null;
                    review.CheckpointRequested += id => checkpoint = id;
                    review.ContinueSetup();
                    Require(review.Model.ErrorKey == "days_min", "days min"); passed++;
                    review.ToggleDay(0);
                    review.ContinueSetup();
                    Require(review.Model.ErrorKey == "days_min", "still need two days"); passed++;
                    review.ToggleDay(2);
                    review.ToggleDay(4);
                    review.SetAvailabilityHours(3);
                    review.SetAvailabilityMinutes(30);
                    review.ContinueSetup();
                    Require(checkpoint == "REVIEW:WIN-013" && review.Model.Step == ScheduleStep.Setup
                        && review.Model.AvailabilityBlockMinutes == 210,
                        "setup continues to character"); passed++;
                    Require(review.Model.SessionMinutesDerived == 60, "derived session cap"); passed++;
                    review.ToggleDay(1);
                    Require(review.Model.SelectedDayIndices.Length == 4, "four days"); passed++;
                    review.Decline();
                    Require(review.Model.SelectedDayIndices.Length == 0 && review.Model.Step == ScheduleStep.Setup,
                        "exit clears draft"); passed++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(languageKey, originalLanguage);
                PlayerPrefs.Save();
            }
            return passed + " schedule state checks passed";
        }

        static void Require(bool condition, string context)
        {
            if (!condition) throw new InvalidOperationException("Schedule check failed: " + context);
        }
    }
}
