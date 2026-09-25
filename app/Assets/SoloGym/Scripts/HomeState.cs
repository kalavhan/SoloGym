using System;
using System.Globalization;
using UnityEngine;

namespace SoloGym
{
    public enum HomeMode { Training, Recovery, Saved, Completed, Setup, Review, Loading, Error, Rest, PlanNeeded }
    public enum HomeReadiness { Ready, LowEnergy, Ill, Pain, Injury }
    public enum HomeConnectivity { Online, Offline, Loading, Error }

    [Serializable]
    public sealed class HomeSnapshot
    {
        public HomeMode Mode = HomeMode.Training;
        public HomeReadiness Readiness = HomeReadiness.Ready;
        public HomeConnectivity Connectivity = HomeConnectivity.Online;
        public bool HasCachedData = true;
        public bool PendingChanges;
        public bool IsTeen;
        public bool SupervisionAvailable = true;
        public bool StrengthSession = true;
        public bool ProfessionalGuidanceNeeded;
        public string FirstMissingSetupWindow = "WIN-009";
        public string SavedSessionId = "fictional_session_001";
        public bool HasSavedSession;
        public int CompletedSets = 3;
        public int TotalSets = 8;
        public string CacheLastConfirmedAt = "2026-09-28 08:15";
        public string UserName = "KAI";
        public bool FemalePresentation;
        public int FitnessLevel = 12;
        public int ConfirmedXp = 1240;
        public int RequiredXp = 2000;
        public int ConfirmedCoins = 840;
        public int Power = 24;
        public int Guard = 18;
        public int Focus = 21;
        public int ConsistencyDays = 7;
    }

    [Serializable]
    public sealed class HomeNavigation
    {
        public string WindowId;
        public string Context;
        public string SessionId;
        public bool ReadOnly;
        public bool RequiresReadinessRecheck;

        public HomeNavigation(string windowId, string context = null, string sessionId = null,
            bool readOnly = false, bool requiresReadinessRecheck = false)
        {
            WindowId = windowId;
            Context = context;
            SessionId = sessionId;
            ReadOnly = readOnly;
            RequiresReadinessRecheck = requiresReadinessRecheck;
        }
    }

    [Serializable]
    public sealed class HomeNavItem
    {
        public string Label;
        public string WindowId;
        public string AccessibleName;

        public HomeNavItem(string label, string windowId, string accessibleName = null)
        { Label = label; WindowId = windowId; AccessibleName = accessibleName ?? label; }
    }

    [Serializable]
    public sealed class HomeGearSlot
    {
        public string Id;
        public string Label;
        public string AccessibleName;
        public bool Equipped;

        public HomeGearSlot(string id, string label, bool equipped, string accessibleName)
        { Id = id; Label = label; Equipped = equipped; AccessibleName = accessibleName; }
    }

    [Serializable]
    public sealed class HomeViewModel
    {
        public string Language, LanguagePreference;
        public HomeMode Mode;
        public HomeReadiness Readiness;
        public HomeConnectivity Connectivity;
        public string Title, UserName, ClassName, LevelLabel, FitnessLevelValue, XpLabel, XpProgressLabel;
        public int FitnessLevel;
        public string CoinsLabel, CoinsValue, CombatStatsLabel;
        public string PowerLabel, PowerValue, GuardLabel, GuardValue, FocusLabel, FocusValue;
        public string TodayLabel, PrimaryTitle, PrimaryBody, PrimaryAction, PrimaryTarget;
        public string PlanName, PlanDetail, PlanWarning, ConsistencyLabel, ConsistencyNote;
        public string StatusBanner, PrivateProfileLabel, SecondarySavedAction;
        public string SettingsLabel, LanguageLabel, CharacterLabel, FixtureNotice;
        public string RetryLabel, LoadingLabel;
        public float XpFraction;
        public bool PrimaryEnabled, ShowPlanSummary, IsPrivateProfile, IsOffline, HasConfirmedData;
        public bool HasCombatStats, ShowSecondarySavedAction, IsLoading, IsError;
        public HomeNavItem[] Navigation;
        public HomeGearSlot[] GearSlots;
    }

    /// <summary>
    /// Local Home presentation and routing only. Values are fictional fixtures, never a
    /// workout prescription, health clearance, reward ledger or network implementation.
    /// </summary>
    public sealed class HomeController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        const string ModeKey = "SoloGym.Home.Mode.v1";
        readonly HomeSnapshot snapshot;
        string languagePreference;
        string language;

        public HomeViewModel Model { get; private set; }
        public event Action<HomeViewModel> Changed;
        public event Action<HomeNavigation> NavigationRequested;
        public event Action<string> NoticeRequested;
        public event Action RetryRequested;

        public HomeController(HomeSnapshot seed = null)
        {
            snapshot = seed ?? new HomeSnapshot();
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            if (seed == null && Enum.TryParse(PlayerPrefs.GetString(ModeKey, "Training"), out HomeMode savedMode)
                && Enum.IsDefined(typeof(HomeMode), savedMode))
                ApplyMode(savedMode);
            Refresh();
        }

        public void SetLanguage(string choice)
        {
            choice = (choice ?? "auto").Trim().ToLowerInvariant();
            if (choice != "en" && choice != "es" && choice != "auto")
                throw new ArgumentException("Language must be en, es or auto.", nameof(choice));
            languagePreference = choice;
            language = ResolveLanguage(choice);
            PlayerPrefs.SetString(LanguageKey, choice);
            PlayerPrefs.Save();
            Refresh();
        }

        public void ToggleLanguage() { SetLanguage(language == "en" ? "es" : "en"); }

        public void SetMode(HomeMode mode)
        {
            if (!Enum.IsDefined(typeof(HomeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            ApplyMode(mode);
            PlayerPrefs.SetString(ModeKey, mode.ToString());
            PlayerPrefs.Save();
            Refresh();
        }

        void ApplyMode(HomeMode mode)
        {
            snapshot.Mode = mode;
            if (mode == HomeMode.Saved) snapshot.HasSavedSession = true;
            // Loading and error are no-cache display scenarios; connectivity changes
            // through SetConnectivity remain independent of normal primary states.
            if (mode == HomeMode.Loading || mode == HomeMode.Error)
            {
                snapshot.Connectivity = mode == HomeMode.Loading ? HomeConnectivity.Loading : HomeConnectivity.Error;
                snapshot.HasCachedData = false;
            }
            else if (!snapshot.HasCachedData)
            {
                snapshot.Connectivity = HomeConnectivity.Online;
                snapshot.HasCachedData = true;
            }
        }

        public void SetReadiness(HomeReadiness readiness)
        {
            if (!Enum.IsDefined(typeof(HomeReadiness), readiness))
                throw new ArgumentOutOfRangeException(nameof(readiness));
            snapshot.Readiness = readiness;
            Refresh();
        }

        public void SetConnectivity(HomeConnectivity connectivity, bool hasCachedData = true, bool pendingChanges = false)
        {
            if (!Enum.IsDefined(typeof(HomeConnectivity), connectivity))
                throw new ArgumentOutOfRangeException(nameof(connectivity));
            snapshot.Connectivity = connectivity;
            snapshot.HasCachedData = hasCachedData;
            snapshot.PendingChanges = pendingChanges;
            // A fresh successful read exits the explicit no-cache sample states.
            if (connectivity == HomeConnectivity.Online && (snapshot.Mode == HomeMode.Loading || snapshot.Mode == HomeMode.Error))
                snapshot.Mode = HomeMode.Training;
            Refresh();
        }

        public void SetTeenProfile(bool isTeen, bool supervisionAvailable = true)
        {
            snapshot.IsTeen = isTeen;
            snapshot.SupervisionAvailable = supervisionAvailable;
            Refresh();
        }

        public void SetGuidanceRequired(bool required)
        {
            snapshot.ProfessionalGuidanceNeeded = required;
            Refresh();
        }

        public void ActivatePrimary()
        {
            if (Model.IsError)
            {
                RetryRequested?.Invoke();
                return;
            }
            if (!Model.PrimaryEnabled) return;
            if (snapshot.Mode == HomeMode.Saved && (Model.Mode == HomeMode.Saved || Model.Mode == HomeMode.Review))
            {
                // Resume always travels through a new readiness check. Home cannot
                // reopen an active boss, start a timer or accept old clearance.
                NavigationRequested?.Invoke(new HomeNavigation("WIN-016", "resume_existing_session",
                    snapshot.SavedSessionId, false, true));
                return;
            }
            if (Model.PrimaryTarget == "WIN-016")
            {
                NavigationRequested?.Invoke(new HomeNavigation("WIN-016",
                    Model.Mode == HomeMode.Review ? "review_readiness" : "check_today", null, false, true));
                return;
            }
            OpenWindow(Model.PrimaryTarget, Model.Mode == HomeMode.Review ? "review_readiness" : null);
        }

        public void OpenSavedRecord()
        {
            if (!Model.ShowSecondarySavedAction) return;
            NavigationRequested?.Invoke(new HomeNavigation("WIN-030", "read_only_saved_record", snapshot.SavedSessionId, true));
        }

        public void OpenGearSlot(string slotId)
        {
            if (Array.FindIndex(Model.GearSlots, item => item.Id == slotId) < 0)
                throw new ArgumentException("Unknown gear slot.", nameof(slotId));
            OpenWindow("WIN-032", "slot:" + slotId);
        }

        public void OpenWindow(string windowId, string context = null)
        {
            if (string.IsNullOrWhiteSpace(windowId)) return;
            if (windowId == "WIN-043" && snapshot.Connectivity != HomeConnectivity.Online)
            {
                NoticeRequested?.Invoke(T("Connect to enter the social Gym.", "Conéctate para entrar al gimnasio social."));
                return;
            }
            // Settings stays usable during loading/error. Other data destinations
            // require a real or cached fixture; future windows are handled by UI.
            if (!Model.HasConfirmedData && windowId != "WIN-001" && windowId != "WIN-055")
            {
                NoticeRequested?.Invoke(T("Your System is not available yet.", "Tu Sistema aún no está disponible."));
                return;
            }
            NavigationRequested?.Invoke(new HomeNavigation(windowId, context));
        }

        public string FutureWindowNotice(string windowId)
        {
            return T("This area is not available in this preview yet.",
                "Esta área aún no está disponible en esta vista previa.");
        }

        public void Refresh()
        {
            HomeMode mode = ResolvePrimary();
            bool noCache = !snapshot.HasCachedData && snapshot.Connectivity != HomeConnectivity.Online;
            bool loading = noCache && snapshot.Connectivity == HomeConnectivity.Loading;
            bool error = noCache && !loading;
            bool setup = mode == HomeMode.Setup;
            int level = setup ? 1 : snapshot.FitnessLevel;
            int xp = setup ? 0 : snapshot.ConfirmedXp;
            int coins = setup ? 0 : snapshot.ConfirmedCoins;
            int days = setup ? 0 : snapshot.ConsistencyDays;
            var numberCulture = CultureInfo.GetCultureInfo(language == "es" ? "es-MX" : "en-US");
            var model = new HomeViewModel
            {
                Language = language, LanguagePreference = languagePreference, Mode = mode,
                Readiness = snapshot.Readiness, Connectivity = snapshot.Connectivity,
                Title = T("SYSTEM", "SISTEMA"), UserName = snapshot.UserName,
                ClassName = T("Boxer", snapshot.FemalePresentation ? "Boxeadora" : "Boxeador"),
                LevelLabel = T("Fitness level ", "Nivel de entrenamiento ") + level,
                FitnessLevel = level, FitnessLevelValue = level.ToString(numberCulture),
                XpLabel = T("Fitness XP", "XP de entrenamiento"),
                XpProgressLabel = setup ? "0 / — XP" : xp.ToString("N0", numberCulture) + " / " + snapshot.RequiredXp.ToString("N0", numberCulture) + " XP",
                XpFraction = setup || snapshot.RequiredXp <= 0 ? 0f : Mathf.Clamp01((float)xp / snapshot.RequiredXp),
                CoinsLabel = T("Coins", "Monedas"), CoinsValue = coins.ToString("N0", numberCulture),
                CombatStatsLabel = T("Combat stats", "Estadísticas de combate"),
                PowerLabel = T("Power", "Poder"), PowerValue = setup ? "—" : snapshot.Power.ToString(),
                GuardLabel = T("Guard", "Defensa"), GuardValue = setup ? "—" : snapshot.Guard.ToString(),
                FocusLabel = T("Focus", "Concentración"), FocusValue = setup ? "—" : snapshot.Focus.ToString(),
                TodayLabel = T("Today", "Hoy"),
                PlanName = T("Foundation A", "Base A"),
                PlanDetail = T("About 24 min  ·  Medium", "Aprox. 24 min  ·  Medio"),
                PlanWarning = T("Pulling exercise missing. Review equipment.", "Falta un ejercicio de tracción. Revisa el equipo."),
                ConsistencyLabel = T("Consistency: ", "Constancia: ") + days +
                    (days == 1 ? T(" day", " día") : T(" days", " días")),
                ConsistencyNote = T("Recovery counts toward consistency.", "La recuperación cuenta para tu constancia."),
                IsPrivateProfile = snapshot.IsTeen,
                PrivateProfileLabel = T("Private profile", "Perfil privado"),
                IsOffline = snapshot.Connectivity == HomeConnectivity.Offline,
                IsLoading = loading, IsError = error, HasConfirmedData = !noCache,
                HasCombatStats = !setup && !noCache,
                PrimaryEnabled = !noCache,
                ShowPlanSummary = mode == HomeMode.Training && !noCache,
                ShowSecondarySavedAction = mode == HomeMode.Rest && (snapshot.Mode == HomeMode.Saved || snapshot.HasSavedSession)
                    && !string.IsNullOrEmpty(snapshot.SavedSessionId) && !noCache,
                SecondarySavedAction = T("View saved session", "Ver sesión guardada"),
                SettingsLabel = T("Settings", "Ajustes"), LanguageLabel = T("Change language", "Cambiar idioma"),
                CharacterLabel = T("View character", "Ver personaje"),
                RetryLabel = T("Retry", "Reintentar"), LoadingLabel = T("Loading your System…", "Cargando tu Sistema…"),
                FixtureNotice = T("Fictional preview data", "Datos ficticios de vista previa"),
                Navigation = new[]
                {
                    new HomeNavItem(T("System", "Sistema"), "WIN-001"),
                    new HomeNavItem(T("Train", "Entrenar"), "WIN-015"),
                    new HomeNavItem(T("Tower", "Torre"), "WIN-037"),
                    new HomeNavItem(T("Gear", "Equipo"), "WIN-032"),
                    new HomeNavItem(T("Gym", "Gimnasio"), "WIN-043", T("Interdimensional Gym, social room", "Gimnasio interdimensional, sala social"))
                },
                GearSlots = new[]
                {
                    Slot("head", "Head", "Cabeza"), Slot("torso", "Torso", "Torso"),
                    Slot("hands", "Hands", "Manos", true), Slot("legs", "Legs", "Piernas"),
                    Slot("feet", "Feet", "Pies"), Slot("back", "Back", "Espalda")
                }
            };
            SetPrimaryCopy(model, mode);
            if (loading || error)
            {
                model.Mode = loading ? HomeMode.Loading : HomeMode.Error;
                model.PrimaryTitle = loading ? model.LoadingLabel : T("Could not load your System", "No se pudo cargar tu Sistema");
                model.PrimaryBody = "";
                model.PrimaryAction = loading ? "" : model.RetryLabel;
                model.PrimaryTarget = "";
                model.PrimaryEnabled = error;
                model.LevelLabel = "—"; model.FitnessLevelValue = "—"; model.XpProgressLabel = "—"; model.CoinsValue = "—";
                model.PowerValue = "—"; model.GuardValue = "—"; model.FocusValue = "—";
                model.ConsistencyLabel = "—"; model.XpFraction = 0f;
            }
            model.StatusBanner = BuildConnectivityBanner();
            Model = model;
            Changed?.Invoke(model);
        }

        HomeMode ResolvePrimary()
        {
            if (snapshot.Readiness == HomeReadiness.Ill || snapshot.Readiness == HomeReadiness.Pain || snapshot.Readiness == HomeReadiness.Injury)
                return HomeMode.Rest;
            if (snapshot.Mode == HomeMode.Setup) return HomeMode.Setup;
            bool unfinished = snapshot.Mode == HomeMode.Training || snapshot.Mode == HomeMode.Saved || snapshot.Mode == HomeMode.Review;
            if (unfinished && (snapshot.ProfessionalGuidanceNeeded || (snapshot.IsTeen && snapshot.StrengthSession && !snapshot.SupervisionAvailable)))
                return HomeMode.Review;
            return snapshot.Mode == HomeMode.Loading || snapshot.Mode == HomeMode.Error ? HomeMode.Training : snapshot.Mode;
        }

        void SetPrimaryCopy(HomeViewModel model, HomeMode mode)
        {
            switch (mode)
            {
                case HomeMode.Rest:
                    Primary(model, T("Rest today", "Descansa hoy"),
                        T("Training is paused based on today's check-in.", "El entrenamiento está en pausa según tu estado de hoy."),
                        T("View recovery", "Ver recuperación"), "WIN-028"); break;
                case HomeMode.Recovery:
                    Primary(model, T("Recovery day", "Día de recuperación"),
                        T("Rest is part of your plan. Your consistency is protected.", "Descansar es parte de tu rutina. Tu constancia está protegida."),
                        T("View recovery", "Ver recuperación"), "WIN-028"); break;
                case HomeMode.Saved:
                    Primary(model, T("Session saved", "Sesión guardada"),
                        T(snapshot.CompletedSets + " of " + snapshot.TotalSets + " sets recorded. Review before continuing.",
                            snapshot.CompletedSets + " de " + snapshot.TotalSets + " series registradas. Revisa antes de continuar."),
                        T("Review session", "Revisar sesión"), "WIN-016"); break;
                case HomeMode.Completed:
                    Primary(model, T("Today's training complete", "Entrenamiento de hoy completado"),
                        T("Your session is recorded. View your progress.", "Tu sesión está registrada. Consulta tu progreso."),
                        T("View session", "Ver sesión"), "WIN-030"); break;
                case HomeMode.Setup:
                    string setupTarget = snapshot.FirstMissingSetupWindow;
                    if (setupTarget != "WIN-009" && setupTarget != "WIN-010" && setupTarget != "WIN-011" && setupTarget != "WIN-012" && setupTarget != "WIN-013")
                        setupTarget = "WIN-009";
                    Primary(model, T("Set up your training", "Configura tu entrenamiento"),
                        T("Add your experience, equipment and available time.", "Añade tu experiencia, equipo y tiempo disponible."),
                        T("Continue setup", "Continuar configuración"), setupTarget); break;
                case HomeMode.Review:
                    bool teenReview = snapshot.IsTeen && snapshot.StrengthSession && !snapshot.SupervisionAvailable;
                    Primary(model, T("Review your training", "Revisa tu entrenamiento"),
                        teenReview
                            ? T("This strength session needs appropriate supervision.", "Esta sesión de fuerza requiere supervisión adecuada.")
                            : T("Review your guidance needs before starting a session.", "Revisa tus necesidades de orientación antes de iniciar una sesión."),
                        T("Review readiness", "Revisar mi estado"), "WIN-016"); break;
                case HomeMode.PlanNeeded:
                    Primary(model, T("Choose your training", "Elige tu entrenamiento"),
                        T("Your plan needs an adjustment. Review time and equipment.", "Tu rutina necesita un ajuste. Revisa el tiempo y el equipo."),
                        T("Review training", "Revisar entrenamiento"), "WIN-015"); break;
                default:
                    Primary(model, T("Today's training", "Entrenamiento de hoy"),
                        T("Check how you feel, then review your plan.", "Revisa cómo te sientes y luego tu rutina."),
                        T("Check today", "Revisar mi estado"), "WIN-016"); break;
            }
        }

        static void Primary(HomeViewModel model, string title, string body, string action, string target)
        { model.PrimaryTitle = title; model.PrimaryBody = body; model.PrimaryAction = action; model.PrimaryTarget = target; }

        HomeGearSlot Slot(string id, string en, string es, bool equipped = false)
        {
            string label = T(en, es);
            string item = equipped ? T("Basic gauntlets concept", "Concepto de guanteletes básicos") : T("Empty", "Vacío");
            return new HomeGearSlot(id, label, equipped, label + ": " + item + T(". Open equipment.", ". Abrir equipo."));
        }

        string BuildConnectivityBanner()
        {
            string banner = "";
            if (snapshot.Connectivity == HomeConnectivity.Offline && snapshot.HasCachedData)
                banner = T("Offline · saved data", "Sin conexión · datos guardados");
            else if (snapshot.Connectivity == HomeConnectivity.Error && snapshot.HasCachedData)
                banner = T("Could not refresh · saved data", "No se pudo actualizar · datos guardados");
            else if (snapshot.Connectivity == HomeConnectivity.Loading && snapshot.HasCachedData)
                banner = T("Refreshing · saved data", "Actualizando · datos guardados");
            if (!string.IsNullOrEmpty(banner) && !string.IsNullOrEmpty(snapshot.CacheLastConfirmedAt))
                banner += "\n" + T("Last updated: ", "Última actualización: ") + snapshot.CacheLastConfirmedAt;
            if (snapshot.PendingChanges && snapshot.HasCachedData)
                banner += (banner.Length > 0 ? "\n" : "") + T("Changes pending", "Cambios pendientes");
            return banner;
        }

        static string ResolveLanguage(string preference)
        {
            if (preference == "en" || preference == "es") return preference;
            return Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
        }

        string T(string en, string es) { return language == "es" ? es : en; }

        public void Dispose()
        {
            Changed = null; NavigationRequested = null; NoticeRequested = null; RetryRequested = null;
        }
    }
}
