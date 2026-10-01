using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SoloGym
{
    public enum ProfileStep { Notice, Measurements, Readiness, Paused, Checkpoint }

    public sealed class PrivateProfileViewModel
    {
        public string Language, LanguagePreference, UnitSystem;
        public string HeightText, HeightFeetText, HeightInchesText, WeightText, Readiness;
        public string Error, ErrorKey, Status;
        public decimal? HeightCm, BodyweightKg;
        public ProfileStep Step;
        public bool ReviewMode, CanContinue;
        public string Copy(string key) => PrivateProfileCopy.Get(key, Language);
    }

    /// <summary>
    /// Memory-only private-profile draft. Normal mode cannot collect measurements
    /// until a server-approved health-data policy and receipt path exist. Review
    /// mode uses fictional values and emits only a REVIEW-prefixed checkpoint.
    /// Presentation changes never round canonical cm/kg values. The generous
    /// 1..300 cm / 1..1000 kg bounds are input guards, not medical classifications.
    /// </summary>
    public sealed class PrivateProfileController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        const decimal CmPerFoot = 30.48m, CmPerInch = 2.54m, KgPerPound = 0.45359237m;
        readonly bool reviewMode;
        string language, languagePreference, unitSystem = "metric";
        string heightText = "", heightFeetText = "", heightInchesText = "", weightText = "";
        string readiness = "", errorKey = "";
        decimal? heightCm, bodyweightKg;
        bool fixtureLoaded, disposed, heightValid = true, weightValid = true;
        ProfileStep step = ProfileStep.Notice;
        public PrivateProfileViewModel Model { get; private set; }
        public event Action<PrivateProfileViewModel> Changed;
        public event Action ExitRequested;
        public event Action<string> CheckpointRequested;

        public PrivateProfileController(bool reviewMode = false)
        {
            this.reviewMode = reviewMode;
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            Refresh();
        }

        public void SetLanguage(string value)
        {
            if (disposed) return;
            if (value != "es" && value != "en" && value != "auto")
                throw new ArgumentException("Language must be en, es or auto.", nameof(value));
            languagePreference = value;
            language = ResolveLanguage(value);
            // Language is the only persisted preference; no profile draft is saved.
            PlayerPrefs.SetString(LanguageKey, value);
            PlayerPrefs.Save();
            Refresh();
        }

        public void ContinueNotice()
        {
            if (disposed || step != ProfileStep.Notice) return;
            if (!reviewMode) { Fail("health_policy_pending"); return; }
            if (!fixtureLoaded)
            {
                heightCm = 170m;
                bodyweightKg = 70m;
                FormatMeasurements();
                fixtureLoaded = true;
            }
            step = ProfileStep.Measurements;
            errorKey = "";
            Refresh();
        }

        public void SetHeight(string value)
        {
            if (!CanEditMeasurements || unitSystem != "metric") return;
            value = value ?? "";
            if (value == heightText) return;
            heightText = value;
            RefreshEditedHeight();
        }

        public void SetHeightFeet(string value)
        {
            if (!CanEditMeasurements || unitSystem != "imperial") return;
            value = value ?? "";
            if (value == heightFeetText) return;
            heightFeetText = value;
            RefreshEditedHeight();
        }

        public void SetHeightInches(string value)
        {
            if (!CanEditMeasurements || unitSystem != "imperial") return;
            value = value ?? "";
            if (value == heightInchesText) return;
            heightInchesText = value;
            RefreshEditedHeight();
        }

        public void SetWeight(string value)
        {
            if (!CanEditMeasurements) return;
            value = value ?? "";
            if (value == weightText) return;
            weightText = value;
            weightValid = ParseWeight(out decimal? parsedWeight);
            bodyweightKg = weightValid ? parsedWeight : null;
            errorKey = "";
            Refresh();
        }

        public void SetUnits(string value)
        {
            if (disposed) return;
            if (value != "metric" && value != "imperial")
                throw new ArgumentException("Unit system must be metric or imperial.", nameof(value));
            if (!CanEditMeasurements || value == unitSystem) return;
            string validation = ValidateMeasurements();
            if (validation.Length != 0) { Fail(validation); return; }
            // Do not parse rounded display text back into canonical numbers here.
            unitSystem = value;
            FormatMeasurements();
            errorKey = "";
            Refresh();
        }

        public void ContinueMeasurements()
        {
            if (!CanEditMeasurements) return;
            string validation = ValidateMeasurements();
            if (validation.Length != 0) { Fail(validation); return; }
            step = ProfileStep.Readiness;
            errorKey = "";
            Refresh();
        }

        public void SelectReadiness(string value)
        {
            if (disposed || !reviewMode || step != ProfileStep.Readiness) return;
            if (value != "ready" && value != "low_energy" && value != "ill"
                && value != "pain" && value != "injury" && value != "unsure")
            {
                readiness = "";
                Fail("readiness_required");
                return;
            }
            readiness = value;
            errorKey = "";
            Refresh();
        }

        public void ContinueReadiness()
        {
            if (disposed || !reviewMode || step != ProfileStep.Readiness) return;
            if (readiness.Length == 0) { Fail("readiness_required"); return; }
            errorKey = "";
            if (readiness == "ready" || readiness == "low_energy")
            {
                step = ProfileStep.Checkpoint;
                Refresh();
                CheckpointRequested?.Invoke("REVIEW:WIN-010");
            }
            else
            {
                step = ProfileStep.Paused;
                Refresh();
            }
        }

        public void Back()
        {
            if (disposed) return;
            errorKey = "";
            switch (step)
            {
                case ProfileStep.Measurements: step = ProfileStep.Notice; break;
                case ProfileStep.Readiness: step = ProfileStep.Measurements; break;
                case ProfileStep.Paused:
                case ProfileStep.Checkpoint: step = ProfileStep.Readiness; break;
                default: Decline(); return;
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

        public void Reset()
        {
            if (disposed) return;
            ResetDraft();
            Refresh();
        }

        public void Pause()
        {
            if (disposed || !reviewMode || step != ProfileStep.Readiness) return;
            readiness = "unsure";
            step = ProfileStep.Paused;
            errorKey = "";
            Refresh();
        }

        bool CanEditMeasurements => !disposed && reviewMode && step == ProfileStep.Measurements;
        void RefreshEditedHeight()
        {
            // Invalid edits invalidate their canonical value rather than retaining
            // a previously valid number. Raw input remains available for correction.
            heightValid = ParseHeight(out decimal? parsedHeight);
            heightCm = heightValid ? parsedHeight : null;
            errorKey = "";
            Refresh();
        }

        string ValidateMeasurements()
        {
            if (!heightValid) return unitSystem == "imperial" ? "invalid_height_imperial" : "invalid_height";
            if (!weightValid) return "invalid_weight";
            return "";
        }

        bool ParseHeight(out decimal? value)
        {
            value = null;
            if (unitSystem == "metric")
            {
                if (string.IsNullOrWhiteSpace(heightText)) return true;
                if (!TryUnsignedDecimal(heightText, false, out decimal parsed)) return false;
                value = parsed;
            }
            else
            {
                bool feetBlank = string.IsNullOrWhiteSpace(heightFeetText);
                bool inchesBlank = string.IsNullOrWhiteSpace(heightInchesText);
                if (feetBlank && inchesBlank) return true;
                decimal feet = 0m, inches = 0m;
                if ((!feetBlank && !TryUnsignedDecimal(heightFeetText, true, out feet))
                    || (!inchesBlank && !TryUnsignedDecimal(heightInchesText, false, out inches))) return false;
                if (feet > 10m || inches >= 12m) return false;
                value = feet * CmPerFoot + inches * CmPerInch;
            }
            return value >= 1m && value <= 300m;
        }

        bool ParseWeight(out decimal? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(weightText)) return true;
            if (!TryUnsignedDecimal(weightText, false, out decimal parsed)) return false;
            // Check before multiplication to avoid decimal overflow from hostile input.
            if (parsed > 3000m) return false;
            value = unitSystem == "metric" ? parsed : parsed * KgPerPound;
            return value >= 1m && value <= 1000m;
        }

        static bool TryUnsignedDecimal(string raw, bool integerOnly, out decimal value)
        {
            value = 0m;
            string input = (raw ?? "").Trim();
            if (input.Length == 0 || input.Length > 32) return false;
            bool separator = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c >= '0' && c <= '9') continue;
                if (integerOnly || (c != '.' && c != ',') || separator || i == 0 || i == input.Length - 1) return false;
                separator = true;
            }
            // A single comma is a decimal mark, never a thousands separator.
            return decimal.TryParse(input.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value);
        }

        void FormatMeasurements()
        {
            heightText = Format(heightCm);
            heightFeetText = heightInchesText = "";
            if (heightCm.HasValue)
            {
                decimal feet = decimal.Floor(heightCm.Value / CmPerFoot);
                decimal inches = decimal.Round((heightCm.Value - feet * CmPerFoot) / CmPerInch, 2);
                if (inches >= 12m) { feet += 1m; inches = 0m; }
                heightFeetText = Format(feet);
                heightInchesText = Format(inches);
            }
            weightText = Format(bodyweightKg.HasValue && unitSystem == "imperial"
                ? bodyweightKg.Value / KgPerPound : bodyweightKg);
        }

        static string Format(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        static string ResolveLanguage(string value) => value == "en" || value == "es" ? value
            : Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
        void Fail(string key) { errorKey = key; Refresh(); }
        void ResetDraft()
        {
            heightText = heightFeetText = heightInchesText = weightText = readiness = errorKey = "";
            heightCm = bodyweightKg = null;
            unitSystem = "metric";
            fixtureLoaded = false;
            heightValid = weightValid = true;
            step = ProfileStep.Notice;
        }

        void Refresh()
        {
            if (disposed) return;
            Model = new PrivateProfileViewModel
            {
                Language = language, LanguagePreference = languagePreference, UnitSystem = unitSystem,
                HeightText = heightText, HeightFeetText = heightFeetText, HeightInchesText = heightInchesText,
                WeightText = weightText, HeightCm = heightCm, BodyweightKg = bodyweightKg,
                Readiness = readiness, Step = step, ErrorKey = errorKey,
                Error = PrivateProfileCopy.Get(errorKey, language), ReviewMode = reviewMode,
                Status = reviewMode ? PrivateProfileCopy.Get("review_mode", language) : "",
                CanContinue = reviewMode && (step == ProfileStep.Notice
                    || (step == ProfileStep.Measurements && ValidateMeasurements().Length == 0)
                    || (step == ProfileStep.Readiness && readiness.Length != 0))
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

    public static class PrivateProfileCopy
    {
        static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            { "back", new[] { "Back", "Volver" } },
            { "continue", new[] { "CONTINUE", "CONTINUAR" } },
            { "not_now", new[] { "Not now", "Ahora no" } },
            { "step_profile", new[] { "STEP 3 · PROFILE", "PASO 3 · PERFIL" } },
            { "profile_title", new[] { "PRIVATE PROFILE", "PERFIL PRIVADO" } },
            { "profile_subtitle", new[] { "Your measurements, your pace", "Tus medidas, a tu ritmo" } },
            { "metric", new[] { "Metric", "Métrico" } },
            { "imperial", new[] { "Imperial", "Imperial" } },
            { "height", new[] { "Height (optional)", "Altura (opcional)" } },
            { "weight", new[] { "Bodyweight (optional)", "Peso corporal (opcional)" } },
            { "measurements_optional", new[] { "You can continue without adding measurements.", "Puedes continuar sin añadir tus medidas." } },
            { "measurements_private", new[] { "Not shown on your public profile.", "No se muestran en tu perfil público." } },
            { "notice_title", new[] { "BEFORE YOUR DATA", "ANTES DE TUS DATOS" } },
            { "notice_body", new[] { "Height and bodyweight are optional private data. They are intended to help adapt your training. They do not define your appearance or a body type. You can skip both measurements.", "Altura y peso corporal son datos privados opcionales. Su finalidad es ayudar a adaptar tu entrenamiento. No definen tu apariencia ni un tipo de cuerpo. Puedes omitir ambas medidas." } },
            { "health_policy_pending", new[] { "Private-data setup is not available yet. The applicable policy and server authorization are required before entering measurements.", "La configuración de datos privados aún no está disponible. Se requiere la política aplicable y la autorización del servidor antes de introducir medidas." } },
            { "notice_review", new[] { "Visual review uses fictional data only. No health-data consent or profile is saved.", "La revisión visual usa solo datos ficticios. No se guarda consentimiento de datos de salud ni un perfil." } },
            { "readiness_title", new[] { "HOW ARE YOU TODAY?", "¿CÓMO LLEGAS HOY?" } },
            { "readiness_subtitle", new[] { "Choose how you feel before training", "Elige cómo te sientes antes de entrenar" } },
            { "ready", new[] { "Ready to train", "Con ganas de entrenar" } },
            { "low_energy", new[] { "Low energy", "Con poca energía" } },
            { "ill", new[] { "Feeling ill", "Me siento enfermo/a" } },
            { "pain", new[] { "In pain", "Tengo dolor" } },
            { "injury", new[] { "An injury", "Tengo una lesión" } },
            { "unsure", new[] { "Not sure / pause", "No estoy seguro/a / pausar" } },
            { "paused_title", new[] { "LET'S PAUSE", "HAGAMOS UNA PAUSA" } },
            { "paused_body", new[] { "Your training stays paused for now. You can change your response or leave. This screen does not assess or diagnose your condition.", "Por ahora dejamos el entrenamiento en pausa. Puedes cambiar tu respuesta o salir. Esta pantalla no evalúa ni diagnostica tu condición." } },
            { "checkpoint_title", new[] { "PROFILE REVIEW COMPLETE", "REVISIÓN DEL PERFIL LISTA" } },
            { "checkpoint_body", new[] { "Next: training goals and experience. This review has not saved a profile or generated a workout.", "Siguiente: objetivos y experiencia de entrenamiento. Esta revisión no guardó un perfil ni generó un entrenamiento." } },
            { "review_mode", new[] { "REVIEW · fictional data; nothing saved", "REVISIÓN · datos ficticios; no se guardan" } },
            { "readiness_required", new[] { "Choose how you feel, or select the pause option.", "Elige cómo te sientes o selecciona la opción de pausa." } },
            { "invalid_height", new[] { "Enter a height from 1 to 300 cm, or leave it blank. Use a decimal point or comma, without thousands separators.", "Introduce una altura de 1 a 300 cm o déjala vacía. Usa punto o coma decimal, sin separadores de miles." } },
            { "invalid_height_imperial", new[] { "Enter whole feet and inches below 12, or leave both blank. The total must be between 1 and 300 cm.", "Introduce pies enteros y pulgadas menores que 12 o deja ambos vacíos. El total debe estar entre 1 y 300 cm." } },
            { "invalid_weight", new[] { "Enter a bodyweight equivalent to 1–1000 kg, or leave it blank. Use a decimal point or comma, without thousands separators.", "Introduce un peso equivalente a 1–1000 kg o déjalo vacío. Usa punto o coma decimal, sin separadores de miles." } }
        };
        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }

    /// <summary>Focused checks of privacy gates, draft retention and numeric behavior.</summary>
    public static class PrivateProfileStateChecks
    {
        public static string Run()
        {
            const string languageKey = "SoloGym.Home.Language.v1";
            string originalLanguage = PlayerPrefs.GetString(languageKey, "auto");
            int passed = 0;
            try
            {
                using (var normal = new PrivateProfileController())
                {
                    int checkpoints = 0;
                    normal.CheckpointRequested += _ => checkpoints++;
                    normal.ContinueNotice(); normal.SetHeight("170"); normal.SetWeight("70");
                    normal.SetUnits("imperial"); normal.SetHeightFeet("5"); normal.SetHeightInches("8");
                    normal.ContinueMeasurements(); normal.SelectReadiness("ready"); normal.ContinueReadiness();
                    Require(normal.Model.Step == ProfileStep.Notice && normal.Model.ErrorKey == "health_policy_pending"
                        && normal.Model.HeightCm == null && normal.Model.BodyweightKg == null
                        && normal.Model.HeightText == "" && normal.Model.WeightText == ""
                        && normal.Model.HeightFeetText == "" && normal.Model.Readiness == ""
                        && checkpoints == 0, "real flow cannot collect or authorize via client calls"); passed++;
                }
                using (var review = new PrivateProfileController(true))
                {
                    Require(review.Model.HeightCm == null && review.Model.BodyweightKg == null
                        && review.Model.Step == ProfileStep.Notice, "notice precedes fictional measurements");
                    review.ContinueNotice();
                    review.SetHeight("170,125"); review.SetWeight("70.375");
                    decimal? cm = review.Model.HeightCm, kg = review.Model.BodyweightKg;
                    for (int i = 0; i < 100; i++) { review.SetUnits("imperial"); review.SetUnits("metric"); }
                    Require(review.Model.HeightCm == cm && review.Model.BodyweightKg == kg,
                        "unit presentation never rounds canonical values"); passed++;
                    review.SetUnits("imperial");
                    review.SetWeight("150");
                    Require(review.Model.HeightCm == cm, "editing weight does not reparse rounded height");
                    decimal? unchangedWeight = review.Model.BodyweightKg;
                    review.SetHeightFeet("5"); review.SetHeightInches("8");
                    Require(review.Model.BodyweightKg == unchangedWeight, "editing height does not reparse rounded weight");
                    review.SetUnits("metric"); review.SetHeight("1"); review.SetWeight("1");
                    review.SetUnits("imperial"); review.SetUnits("metric");
                    Require(review.Model.HeightCm == 1m && review.Model.BodyweightKg == 1m
                        && review.Model.UnitSystem == "metric", "display rounding cannot invalidate boundary values"); passed++;
                    review.SetUnits("imperial");
                    review.SetHeightFeet("5"); review.SetHeightInches("8,5"); review.SetWeight("150");
                    Require(review.Model.HeightCm == 173.99m && review.Model.BodyweightKg == 68.03885550m,
                        "imperial input normalizes exact units"); passed++;
                    review.SetHeightInches("12"); review.SetUnits("metric");
                    review.Back(); review.SetLanguage("es"); review.ContinueNotice(); review.ContinueMeasurements();
                    Require(review.Model.UnitSystem == "imperial" && review.Model.HeightInchesText == "12"
                        && review.Model.Step == ProfileStep.Measurements && review.Model.HeightCm == null
                        && review.Model.ErrorKey == "invalid_height_imperial", "invalid inches survive units/back/language"); passed++;
                    review.SetHeightFeet(""); review.SetHeightInches(""); review.SetWeight("");
                    review.SetUnits("metric"); review.SetUnits("imperial");
                    Require(review.Model.HeightCm == null && review.Model.BodyweightKg == null
                        && review.Model.HeightFeetText == "" && review.Model.HeightInchesText == ""
                        && review.Model.WeightText == "", "missing values stay missing across unit changes"); passed++;
                    review.SetHeightFeet("5");
                    Require(review.Model.HeightCm == 152.4m, "blank inches with explicit feet means zero inches");
                    review.SetHeightFeet(""); review.SetHeightInches("10");
                    Require(review.Model.HeightCm == 25.4m, "blank feet with explicit inches means zero feet"); passed++;
                    review.SetHeightInches(""); review.SetUnits("metric");
                    foreach (string invalid in new[] { "NaN", "Infinity", "-1", "+70", "7e1", "1,234.5", "1 000", "0", "1001" })
                    {
                        review.SetWeight(invalid); review.SetUnits("imperial"); review.ContinueMeasurements();
                        Require(review.Model.WeightText == invalid && review.Model.UnitSystem == "metric"
                            && review.Model.BodyweightKg == null && review.Model.Step == ProfileStep.Measurements
                            && review.Model.ErrorKey == "invalid_weight", "malformed weight cannot clear or advance: " + invalid);
                    }
                    passed++;
                    review.SetWeight(""); review.ContinueMeasurements();
                    Require(review.Model.Step == ProfileStep.Readiness, "both measurements can be omitted"); passed++;
                    string checkpoint = null;
                    int checkpointCount = 0, exits = 0;
                    review.CheckpointRequested += value => { checkpoint = value; checkpointCount++; };
                    review.ExitRequested += () => exits++;
                    review.ContinueReadiness();
                    Require(review.Model.ErrorKey == "readiness_required" && checkpoint == null, "readiness is never assumed");
                    foreach (string response in new[] { "ill", "pain", "injury", "unsure" })
                    {
                        review.SelectReadiness(response); review.ContinueReadiness();
                        Require(review.Model.Step == ProfileStep.Paused && checkpoint == null, "pause route: " + response);
                        review.Back();
                    }
                    passed++;
                    foreach (string response in new[] { "ready", "low_energy" })
                    {
                        review.SelectReadiness(response); review.ContinueReadiness(); review.ContinueReadiness();
                        Require(review.Model.Step == ProfileStep.Checkpoint && checkpoint == "REVIEW:WIN-010",
                            "explicit review-only destination: " + response);
                        review.Back();
                    }
                    Require(checkpointCount == 2, "repeated continue cannot duplicate checkpoint"); passed++;
                    review.SetLanguage("auto");
                    Require(review.Model.LanguagePreference == "auto" && PlayerPrefs.GetString(languageKey) == "auto",
                        "automatic language preference remains automatic");
                    review.Decline();
                    Require(exits == 1 && review.Model.HeightCm == null && review.Model.BodyweightKg == null
                        && review.Model.HeightText == "" && review.Model.HeightFeetText == ""
                        && review.Model.HeightInchesText == "" && review.Model.WeightText == ""
                        && review.Model.Readiness == "" && review.Model.Step == ProfileStep.Notice,
                        "exit clears every private draft field"); passed++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(languageKey, originalLanguage);
                PlayerPrefs.Save();
            }
            return passed + " private profile state checks passed";
        }
        static void Require(bool condition, string context)
        {
            if (!condition) throw new InvalidOperationException("Private profile check failed: " + context);
        }
    }
}
