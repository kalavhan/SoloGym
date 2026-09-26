using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    public enum EquipmentStep { Environment, Equipment, Review }

    [Serializable]
    public sealed class EquipmentCatalogEntry
    {
        public string id, en, es;
        public string Label(string language) => language == "es" ? es : en;
    }

    [Serializable]
    public sealed class EnvironmentCatalogEntry
    {
        public string id, en, es;
        public string Label(string language) => language == "es" ? es : en;
    }

    [Serializable]
    sealed class EquipmentCatalogFile
    {
        public EnvironmentCatalogEntry[] environments;
        public EquipmentCatalogEntry[] equipment;
        public string[] visible_home;
        public string[] visible_gym;
        public string[] visible_outdoor;
    }

    public sealed class EquipmentViewModel
    {
        public string Language, EnvironmentId, Error, ErrorKey, Status;
        public EquipmentStep Step;
        public bool ReviewMode, CanContinue, BodyweightOnly;
        public EnvironmentCatalogEntry[] Environments;
        public EquipmentCatalogEntry[] VisibleEquipment;
        public string[] SelectedEquipmentIds;
        public string EnvironmentLabel, EquipmentSummary;
        public string Copy(string key) => EquipmentCopy.Get(key, Language);
    }

    /// <summary>
    /// Memory-only environment and equipment draft. Outdoor uses explicit bodyweight-only when the derived list is empty.
    /// </summary>
    public sealed class EquipmentController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        readonly bool reviewMode;
        readonly Dictionary<string, EquipmentCatalogEntry> equipmentById = new Dictionary<string, EquipmentCatalogEntry>();
        readonly Dictionary<string, string[]> visibleByEnvironment = new Dictionary<string, string[]>();
        EnvironmentCatalogEntry[] environments;
        string language, languagePreference, environmentId = "", errorKey = "";
        readonly HashSet<string> selected = new HashSet<string>();
        bool bodyweightOnly, disposed;
        EquipmentStep step = EquipmentStep.Environment;

        public EquipmentViewModel Model { get; private set; }
        public event Action<EquipmentViewModel> Changed;
        public event Action ExitRequested;
        public event Action<string> CheckpointRequested;

        public EquipmentController(bool reviewMode = false)
        {
            this.reviewMode = reviewMode;
            LoadCatalog();
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            Refresh();
        }

        void LoadCatalog()
        {
            var asset = Resources.Load<TextAsset>("Equipment/Catalog");
            if (asset == null) throw new InvalidOperationException("Equipment/Catalog.json is missing.");
            var catalog = JsonUtility.FromJson<EquipmentCatalogFile>(asset.text);
            environments = catalog.environments ?? Array.Empty<EnvironmentCatalogEntry>();
            foreach (var entry in catalog.equipment ?? Array.Empty<EquipmentCatalogEntry>())
                equipmentById[entry.id] = entry;
            visibleByEnvironment["home"] = catalog.visible_home ?? Array.Empty<string>();
            visibleByEnvironment["gym"] = catalog.visible_gym ?? Array.Empty<string>();
            visibleByEnvironment["outdoor"] = catalog.visible_outdoor ?? Array.Empty<string>();
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

        public void SelectEnvironment(string id)
        {
            if (disposed || step != EquipmentStep.Environment) return;
            if (!IsEnvironment(id)) { Fail("environment_invalid"); return; }
            if (environmentId != id)
            {
                environmentId = id;
                selected.Clear();
                bodyweightOnly = false;
            }
            errorKey = "";
            Refresh();
        }

        public void ContinueEnvironment()
        {
            if (disposed || step != EquipmentStep.Environment) return;
            if (environmentId.Length == 0) { Fail("environment_required"); return; }
            errorKey = "";
            step = EquipmentStep.Equipment;
            Refresh();
        }

        public void SetBodyweightOnly(bool value)
        {
            if (disposed || step != EquipmentStep.Equipment) return;
            bodyweightOnly = value;
            if (value) selected.Clear();
            errorKey = "";
            Refresh();
        }

        public void ToggleEquipment(string id)
        {
            if (disposed || step != EquipmentStep.Equipment) return;
            if (!IsVisible(id)) { Fail("equipment_invalid"); return; }
            bodyweightOnly = false;
            if (selected.Contains(id)) selected.Remove(id);
            else selected.Add(id);
            errorKey = "";
            Refresh();
        }

        public void ContinueEquipment()
        {
            if (disposed || step != EquipmentStep.Equipment) return;
            if (!ValidateEquipmentStep()) return;
            errorKey = "";
            step = EquipmentStep.Review;
            Refresh();
        }

        public void EditEnvironment()
        {
            if (disposed) return;
            step = EquipmentStep.Environment;
            errorKey = "";
            Refresh();
        }

        public void EditEquipment()
        {
            if (disposed) return;
            step = EquipmentStep.Equipment;
            errorKey = "";
            Refresh();
        }

        public void ContinueReview()
        {
            if (disposed || step != EquipmentStep.Review) return;
            if (!reviewMode) { Fail("training_setup_pending"); return; }
            if (!ValidateEquipmentStep() || environmentId.Length == 0) { Fail("review_incomplete"); return; }
            errorKey = "";
            CheckpointRequested?.Invoke("REVIEW:WIN-012");
            Refresh();
        }

        public void Back()
        {
            if (disposed) return;
            errorKey = "";
            switch (step)
            {
                case EquipmentStep.Equipment:
                    step = EquipmentStep.Environment;
                    break;
                case EquipmentStep.Review:
                    step = EquipmentStep.Equipment;
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

        bool ValidateEquipmentStep()
        {
            if (bodyweightOnly) return true;
            var visible = VisibleIds();
            if (visible.Length == 0)
            {
                Fail("outdoor_bodyweight_required");
                return false;
            }
            if (selected.Count == 0)
            {
                Fail("equipment_required");
                return false;
            }
            return true;
        }

        bool IsEnvironment(string id) => id == "home" || id == "gym" || id == "outdoor";

        bool IsVisible(string id)
        {
            if (!visibleByEnvironment.TryGetValue(environmentId, out string[] ids)) return false;
            return Array.IndexOf(ids, id) >= 0;
        }

        string[] VisibleIds()
        {
            if (!visibleByEnvironment.TryGetValue(environmentId, out string[] ids))
                return Array.Empty<string>();
            return ids;
        }

        EquipmentCatalogEntry[] VisibleEquipment()
        {
            var list = new List<EquipmentCatalogEntry>();
            foreach (string id in VisibleIds())
            {
                if (equipmentById.TryGetValue(id, out EquipmentCatalogEntry entry))
                    list.Add(entry);
            }
            return list.ToArray();
        }

        string EnvironmentLabel()
        {
            foreach (var entry in environments)
            {
                if (entry.id == environmentId) return entry.Label(language);
            }
            return environmentId;
        }

        string EquipmentSummary()
        {
            if (bodyweightOnly) return EquipmentCopy.Get("bodyweight_review", language);
            return string.Join(", ", selected.OrderBy(id => id).Select(id =>
                equipmentById.TryGetValue(id, out EquipmentCatalogEntry entry) ? entry.Label(language) : id));
        }

        void ResetDraft()
        {
            environmentId = "";
            selected.Clear();
            bodyweightOnly = false;
            errorKey = "";
            step = EquipmentStep.Environment;
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
            foreach (string id in selected.ToArray())
            {
                if (!IsVisible(id)) selected.Remove(id);
            }
            var visible = VisibleEquipment();
            bool outdoorEmpty = environmentId == "outdoor" && visible.Length == 0;
            bool equipmentComplete = bodyweightOnly || (visible.Length > 0 && selected.Count > 0);
            if (outdoorEmpty) equipmentComplete = bodyweightOnly;
            Model = new EquipmentViewModel
            {
                Language = language,
                EnvironmentId = environmentId,
                Step = step,
                ErrorKey = errorKey,
                Error = EquipmentCopy.Get(errorKey, language),
                ReviewMode = reviewMode,
                Status = reviewMode ? EquipmentCopy.Get("review_mode", language) : "",
                BodyweightOnly = bodyweightOnly,
                Environments = environments,
                VisibleEquipment = visible,
                SelectedEquipmentIds = selected.OrderBy(id => id).ToArray(),
                EnvironmentLabel = environmentId.Length == 0 ? "" : EnvironmentLabel(),
                EquipmentSummary = step == EquipmentStep.Review ? EquipmentSummary() : "",
                CanContinue = step == EquipmentStep.Environment ? environmentId.Length != 0
                    : step == EquipmentStep.Equipment ? equipmentComplete
                    : step == EquipmentStep.Review && environmentId.Length != 0 && equipmentComplete
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

    public static class EquipmentCopy
    {
        static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            { "window_title", new[] { "AVAILABLE EQUIPMENT", "EQUIPO DISPONIBLE" } },
            { "environment_title", new[] { "YOUR SPACE", "TU ESPACIO" } },
            { "environment_subtitle", new[] { "Choose where you usually train", "Elige dónde entrenas habitualmente" } },
            { "environment_footer", new[] { "You can update this when your setup changes.", "Puedes actualizarlo cuando cambie tu espacio." } },
            { "equipment_title", new[] { "WHAT YOU HAVE", "LO QUE TIENES" } },
            { "equipment_subtitle", new[] { "Select everything you can use safely today", "Selecciona todo lo que puedas usar con seguridad hoy" } },
            { "bodyweight_only", new[] { "Bodyweight only", "Solo peso corporal" } },
            { "outdoor_helper", new[] { "Outdoor sessions in this catalog use open-space and bodyweight movements. Confirm bodyweight only to continue.", "Las sesiones al aire libre en este catálogo usan movimientos en espacio abierto y peso corporal. Confirma solo peso corporal para continuar." } },
            { "review_title", new[] { "REVIEW AND CONTINUE", "REVISAR Y CONTINUAR" } },
            { "review_environment", new[] { "Where you train", "Dónde entrenas" } },
            { "review_equipment", new[] { "Equipment", "Equipo" } },
            { "bodyweight_review", new[] { "Bodyweight only", "Solo peso corporal" } },
            { "change_environment", new[] { "Change location", "Cambiar lugar" } },
            { "change_equipment", new[] { "Change equipment", "Cambiar equipo" } },
            { "review_body", new[] { "You'll still set your schedule before sessions are prepared. Nothing is saved to your account in this preview.", "Todavía configurarás tu horario antes de preparar las sesiones. En esta vista previa no se guarda nada en tu cuenta." } },
            { "continue", new[] { "CONTINUE", "CONTINUAR" } },
            { "back", new[] { "Back", "Volver" } },
            { "not_now", new[] { "Not now", "Ahora no" } },
            { "review_mode", new[] { "REVIEW · fictional data; nothing saved", "REVISIÓN · datos ficticios; no se guardan" } },
            { "environment_required", new[] { "Choose where you train to continue.", "Elige dónde entrenas para continuar." } },
            { "environment_invalid", new[] { "That location is not available.", "Ese lugar no está disponible." } },
            { "equipment_required", new[] { "Select at least one item or choose bodyweight only.", "Selecciona al menos un elemento o elige solo peso corporal." } },
            { "equipment_invalid", new[] { "That equipment is not available for this location.", "Ese equipo no está disponible para este lugar." } },
            { "outdoor_bodyweight_required", new[] { "Confirm bodyweight only for outdoor training.", "Confirma solo peso corporal para entrenar al aire libre." } },
            { "review_incomplete", new[] { "Choose a location and equipment before continuing.", "Elige un lugar y el equipo antes de continuar." } },
            { "training_setup_pending", new[] { "Training setup is not available yet. The private profile service and production gates must be ready before saving choices.", "La configuración del entrenamiento aún no está disponible. El servicio de perfil privado y las condiciones de producción deben estar listos antes de guardar las elecciones." } },
            { "next_window_notice_title", new[] { "Next step", "Siguiente paso" } },
            { "next_window_notice_body", new[] { "Schedule and session time is the next window. Nothing was saved to your account.", "El horario y la duración de sesión es la siguiente ventana. No se guardaron datos en tu cuenta." } }
        };

        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }

    public static class EquipmentStateChecks
    {
        public static string Run()
        {
            const string languageKey = "SoloGym.Home.Language.v1";
            string originalLanguage = PlayerPrefs.GetString(languageKey, "auto");
            int passed = 0;
            try
            {
                using (var normal = new EquipmentController())
                {
                    string checkpoint = null;
                    normal.CheckpointRequested += id => checkpoint = id;
                    normal.SelectEnvironment("home");
                    normal.ContinueEnvironment();
                    normal.ToggleEquipment("chair");
                    normal.ContinueEquipment();
                    normal.ContinueReview();
                    Require(normal.Model.ErrorKey == "training_setup_pending" && checkpoint == null,
                        "production cannot checkpoint"); passed++;
                }
                using (var review = new EquipmentController(true))
                {
                    review.ContinueEnvironment();
                    Require(review.Model.ErrorKey == "environment_required", "environment required"); passed++;
                    review.SelectEnvironment("home");
                    review.ContinueEnvironment();
                    review.ContinueEquipment();
                    Require(review.Model.ErrorKey == "equipment_required", "equipment required"); passed++;
                    review.SetBodyweightOnly(true);
                    review.ContinueEquipment();
                    Require(review.Model.Step == EquipmentStep.Review && review.Model.BodyweightOnly, "bodyweight review"); passed++;
                    string checkpoint = null;
                    review.CheckpointRequested += id => checkpoint = id;
                    review.ContinueReview();
                    Require(checkpoint == "REVIEW:WIN-012", "schedule checkpoint"); passed++;
                    review.EditEquipment();
                    review.ToggleEquipment("wall");
                    review.ContinueEquipment();
                    Require(!review.Model.BodyweightOnly && review.Model.SelectedEquipmentIds.Length == 1, "toggle equipment"); passed++;
                    review.EditEnvironment();
                    review.SelectEnvironment("outdoor");
                    review.ContinueEnvironment();
                    review.ContinueEquipment();
                    Require(review.Model.ErrorKey == "outdoor_bodyweight_required", "outdoor requires bodyweight"); passed++;
                    review.SetBodyweightOnly(true);
                    review.ContinueEquipment();
                    review.Decline();
                    Require(review.Model.EnvironmentId == "" && review.Model.Step == EquipmentStep.Environment,
                        "exit clears draft"); passed++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(languageKey, originalLanguage);
                PlayerPrefs.Save();
            }
            return passed + " equipment state checks passed";
        }

        static void Require(bool condition, string context)
        {
            if (!condition) throw new InvalidOperationException("Equipment check failed: " + context);
        }
    }
}
