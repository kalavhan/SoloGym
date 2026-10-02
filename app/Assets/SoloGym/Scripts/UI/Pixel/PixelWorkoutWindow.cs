using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SoloGym.UI.PixelJournalUI;

namespace SoloGym.UI
{
    /// <summary>The complete landscape journal: calendar, local examples, editing and readiness review.</summary>
    public sealed class PixelWorkoutWindow : MonoBehaviour
    {
        const string ArtRoot = "Rooms/WorkoutsR1/";
        public WorkoutJournal Journal { get; private set; }
        public RectTransform Composition { get; private set; }
        public string Language { get; private set; }
        public PixelNavigationBar Navigation { get; private set; }
        public readonly Dictionary<string, Selectable> Controls = new Dictionary<string, Selectable>();
        public string View { get; private set; } = "loading";
        public bool HasModal => modal != null;
        public Action Exited;
        public PixelBossWindow BossWindow { get; private set; }
        RectTransform canvasRoot, safe, page, modal;
        CanvasGroup pageGate;
        GameObject beforeModal;
        string character, profile, failure, editError, filter = "all";
        bool initialized, history, empty, captureDone, prepareOnLoad;
        public bool AutomaticReview = true;
        float inset;
        int lastWidth, lastHeight;
        Rect lastSafe;
        TrainingCatalog catalog;
        JournalOptions options;
        IJournalStorage storage;
        DateTime today;
        readonly List<Selectable> traversal = new List<Selectable>();
        string L(string en, string es) => Language == "es" ? es : en;
        public static string Arg(string key, string fallback = "") => PixelButtonGallery.Argument(key, fallback);
        public static bool Has(string key) => PixelButtonGallery.HasArgument(key);
        /// <summary>The signed-in person's account; null keeps the explicitly fictional review journal.</summary>
        public AccountSession Account { get; private set; }
        public bool Live => Account != null;
        public void Initialize(string locale, string appearance, bool teen, Action onExit, IJournalStorage testStorage = null, bool openReadiness = false, AccountSession account = null)
        {
            if (initialized) return; initialized = true; prepareOnLoad = openReadiness; Account = account;
            Language = locale == "es" ? "es" : "en"; character = appearance; Exited = onExit;
            profile = teen ? "teen_home_supervised" : Arg("-sologym-workout-profile", "adult_gym_intermediate");
            if (!teen && profile != "adult_gym_intermediate" && profile != "adult_home_beginner") profile = "adult_gym_intermediate";
            DateTime parsed; today = DateTime.TryParseExact(Arg("-sologym-date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed) ? parsed : DateTime.Today;
            float.TryParse(Arg("-sologym-safe-inset", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out inset); inset = Mathf.Clamp(inset, 0, 150);
            empty = Has("-sologym-empty");
            storage = testStorage ?? new JournalFileStorage(Has("-sologym-smoke") && !Has("-sologym-journal-file") ? Path.Combine(Application.temporaryCachePath, "workout-smoke-" + Guid.NewGuid().ToString("N") + ".json") : Arg("-sologym-journal-file", Path.Combine(Application.persistentDataPath, "workout-journal-preview-" + profile + "-v1.json")));
            Application.targetFrameRate = 60; Screen.orientation = ScreenOrientation.LandscapeLeft;
            if (EventSystem.current == null) new GameObject("Workout input", typeof(EventSystem), typeof(StandaloneInputModule));
            if (FindFirstObjectByType<Camera>() == null) { var camera = new GameObject("Workout camera").AddComponent<Camera>(); camera.cullingMask = 0; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(13, 18, 27, 255); }
            canvasRoot = Rect("Workout canvas", transform, new Rect());
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true; canvas.sortingOrder = 50;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>(); canvasRoot.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            safe = Rect("Safe area", canvasRoot, new Rect()); Composition = Rect("1280x720 workout composition", safe, new Rect(0, 0, 1280, 720));
            Composition.anchorMin = Composition.anchorMax = Composition.pivot = new Vector2(.5f, .5f); Composition.anchoredPosition = Vector2.zero;
            CoverBackdrop(canvasRoot, ArtRoot + "background");
            Render(); Relayout(); StartCoroutine(Reload());
        }
        IEnumerator Start()
        {
            if (!initialized) Initialize(Arg("-sologym-locale", "es"), Arg("-sologym-character", "male-medium"), Has("-sologym-teen"), OpenStandaloneHome, openReadiness: Arg("-sologym-window") == "boss");
            while (View == "loading") yield return null;
            for (int i = 0; i < 6; i++) yield return null;
            if (AutomaticReview && Has("-sologym-smoke")) yield return gameObject.AddComponent<PixelWorkoutSmoke>().Run(this);
            if (AutomaticReview && Has("-sologym-capture") && !captureDone)
            {
                captureDone = true; yield return null; yield return new WaitForEndOfFrame(); Capture(Arg("-sologym-capture"));
                if (!Has("-sologym-stay-open")) Application.Quit();
            }
        }
        public IEnumerator Reload()
        {
            View = "loading"; failure = null; Render(); yield return null;
            try
            {
                if (Live) { Journal = Account.OpenJournal(today); }
                else
                {
                    catalog = JsonUtility.FromJson<TrainingCatalog>(Resources.Load<TextAsset>("Training/Preview").text);
                    options = JsonUtility.FromJson<JournalOptions>(Resources.Load<TextAsset>("Training/JournalOptions").text);
                    Journal = new WorkoutJournal(catalog, options, storage, profile, today); Journal.Load(empty);
                }
                failure = Journal.Error; View = failure == null ? "hub" : "error";
            }
            catch (Exception e) { Debug.LogWarning("Workout journal could not load: " + e.Message); failure = e.Message; View = "error"; }
            Render();
            if (prepareOnLoad && Journal?.Loaded == true) { prepareOnLoad = false; Prepare(); }
        }
        void OpenStandaloneHome()
        {
            gameObject.SetActive(false); new GameObject("Home after journal").AddComponent<PixelTrainingHallHome>(); Destroy(gameObject);
        }
        public void Relayout()
        {
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = Screen.safeArea;
            var s = Screen.safeArea; var min = new Vector2(Mathf.Max(s.xMin, inset), Mathf.Max(s.yMin, inset));
            var max = new Vector2(Mathf.Min(s.xMax, Screen.width - inset), Mathf.Min(s.yMax, Screen.height - inset));
            safe.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height); safe.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
            float scale = Mathf.Max(.01f, Mathf.Min((max.x - min.x) / 1280, (max.y - min.y) / 720)); Composition.localScale = new Vector3(scale, scale, 1);
            Canvas.ForceUpdateCanvases(); Navigation?.RefreshLayout();
        }
        void Update()
        {
            if (!initialized || !Composition.gameObject.activeSelf) return;
            if (lastWidth != Screen.width || lastHeight != Screen.height || lastSafe != Screen.safeArea) Relayout();
            if (Input.GetKeyDown(KeyCode.Tab) && !HasModal && EventSystem.current.currentSelectedGameObject == null) traversal.FirstOrDefault(s => s != null && s.IsInteractable())?.Select();
            if (Input.GetKeyDown(KeyCode.Escape)) { if (HasModal) CloseModal(); else if (View == "picker") { View = "edit"; Render(); } else LeaveToHub(); }
        }
        public void Render()
        {
            if (Composition == null) return;
            var focusedObject = EventSystem.current.currentSelectedGameObject;
            string focused = focusedObject != null ? focusedObject.name : null;
            EventSystem.current.SetSelectedGameObject(null);
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            Controls.Clear(); traversal.Clear();
            page = Rect("Workout window", Composition, new Rect(0, 0, 1280, 720)); pageGate = page.gameObject.AddComponent<CanvasGroup>();
            var plaque = Frame(page, new Rect(24, 18, 306, 64), "Window title"); Text(plaque, new Rect(12, 8, 282, 48), L("WORKOUTS", "RUTINAS"), 32, false, TextAnchor.MiddleCenter);
            var player = Frame(page, new Rect(976, 16, 280, 78), "Player profile");
            var portrait = Art(player, new Rect(14, 12, 48, 54), "Characters/PixelLabR1/" + character + "-portrait"); portrait.preserveAspect = true;
            Text(player, new Rect(72, 8, 143, 34), Live ? Account.Profile.PreferredName : "Aventurero", 22, false); Text(player, new Rect(72, 40, 143, 30), L("Barbarian", character.StartsWith("female") ? "Bárbara" : "Bárbaro"), 20, false);
            var settings = Button(player, "settings", new Rect(218, 11, 52, 56), "", ShowSettings, true, true);
            Art(settings.transform, new Rect(12, 13, 28, 28), "UI/Pixel/IconSettings").sprite = Resources.LoadAll<Sprite>("UI/Pixel/IconSettings")[0];
            settings.interactable = Journal?.Loaded == true && View != "edit" && View != "readiness" && View != "review";
            Frame(page, new Rect(98, 120, 1084, 482), "Journal leather and brass cover");
            Art(page, new Rect(108, 130, 1064, 462), ArtRoot + "journal");
            Art(page, new Rect(108, 130, 1064, 462), ArtRoot + "journal-border");
            if (View == "loading" || View == "error") BuildUnavailable();
            else if (View == "edit") { if (Live) BuildLiveEditor(); else BuildEditor(); }
            else if (View == "briefing") BuildBriefing();
            else if (View == "picker") BuildPicker();
            else if (View == "readiness") BuildReadiness();
            else if (View == "review") BuildReadinessReview();
            else if (View == "settings") BuildSettings();
            else { BuildLeftPage(); BuildRightPage(); }
            Text(page, new Rect(150, 607, 980, 29), L("You can adjust difficulty during the session.", "Puedes ajustar la dificultad durante la sesión."), 21, false, TextAnchor.MiddleCenter);
            Text(page, new Rect(16, 686, 322, 24), Live ? L("Your journal · saved on this device", "Tu diario · guardado en este dispositivo") : L("Sample data · local changes", "Datos de ejemplo · cambios locales"), 16, false);
            Frame(page, new Rect(373, 645, 534, 70), "Navigation dock");
            Navigation = PixelNavigationBar.Create(page, new[] { "home", "workouts", "dungeon" }, new[] { L("Home", "Hogar"), L("Workouts", "Rutinas"), L("Dungeon", "Mazmorra") }, new[] { "home", "workouts", "dungeon" }, "workouts");
            Place((RectTransform)Navigation.transform, new Rect(380, 648, 520, 64)); Navigation.SetLayoutMetrics(125, 0);
            foreach (var tab in Navigation.Tabs) { tab.Label.fontSize = 22; tab.Background.pixelsPerUnitMultiplier = 2; Register("nav-" + tab.Id, tab); }
            Navigation.onNavigate.AddListener(id => { if (id == "home") TryLeave(() => Exited?.Invoke()); else if (id == "dungeon") TryLeave(Prepare); });
            WireTraversal(); Relayout();
            if (focused != null && Controls.TryGetValue(focused, out var target) && target.IsInteractable()) target.Select();
        }
        void BuildUnavailable()
        {
            Heading(170, L("YOUR JOURNAL", "TU DIARIO"));
            Text(page, new Rect(165, 240, 410, 170), View == "loading" ? L("Opening your journal…", "Abriendo tu diario…") : L("The saved journal could not be read. Your file has been kept unchanged.", "No se pudo leer el diario guardado. Tu archivo se conservó sin cambios."), 26);
            if (View == "error") Button(page, "retry", new Rect(175, 453, 375, 60), L("Retry", "Reintentar"), () => StartCoroutine(Reload()));
            Text(page, new Rect(687, 243, 410, 190), Live ? L("Your training records stay on this device. Retry, or contact support if this continues.", "Tus registros de entrenamiento se guardan en este dispositivo. Reintenta o contacta soporte si continúa.")
                : L("The journal uses local examples. It is separate from a live personal training account.", "El diario usa ejemplos locales. Es independiente de una cuenta de entrenamiento personal."), 25);
        }
        void BuildLeftPage()
        {
            Button(page, "week", new Rect(224, 92, 150, 45), L("WEEK", "SEMANA"), () => { history = false; View = "hub"; Render(); }, true, !history);
            Button(page, "history", new Rect(383, 92, 150, 45), L("HISTORY", "HISTORIAL"), () => { history = true; View = "hub"; Render(); }, true, history);
            Heading(170, history ? L("YOUR HISTORY", "TU HISTORIAL") : L("YOUR WEEK", "TU SEMANA"));
            if (!history)
            {
                Button(page, "week-prev", new Rect(186, 218, 48, 48), "<", () => { Journal.MoveWeek(-1); View = "hub"; Render(); });
                Text(page, new Rect(238, 218, 297, 48), WeekCaption(), 23, true, TextAnchor.MiddleCenter);
                Button(page, "week-next", new Rect(541, 218, 48, 48), ">", () => { Journal.MoveWeek(1); View = "hub"; Render(); });
                for (int i = 0; i < 7; i++)
                {
                    var day = Journal.WeekStart.AddDays(i); var names = Language == "es" ? new[] { "L", "M", "X", "J", "V", "S", "D" } : new[] { "M", "T", "W", "T", "F", "S", "S" };
                    Button(page, "day-" + i, new Rect(173 + i * 60, 276, 54, 62), names[i] + "\n" + day.Day, () => { Journal.SelectDay(day); View = "hub"; Render(); }, false, day == Journal.SelectedDay);
                }
            }
            else
            {
                string[] ids = { "all", "completed", "missed" }; string[] labels = { L("All", "Todo"), L("Completed", "Realizadas"), L("Missed", "Sin hacer") };
                for (int i = 0; i < 3; i++) { string id = ids[i]; Button(page, "filter-" + id, new Rect(162 + i * 148, 226, 140, 55), labels[i], () => { filter = id; Render(); }, true, filter == id, 20); }
            }
            var rows = Journal.Visible(history, filter); float top = history ? 309 : 350; float height = history ? 218 : 178;
            var list = Scroll(page, new Rect(165, top, 438, height), rows.Length * 64);
            if (rows.Length == 0) Text(list, new Rect(6, 5, 409, 115), history ? L("No entries in this filter.", "No hay registros en este filtro.") : L("No routine planned for this week.", "No hay rutinas esta semana."), 24);
            for (int i = 0; i < rows.Length; i++)
            {
                var entry = rows[i]; string status = Journal.Status(entry);
                var row = Button(list, "entry-" + entry.id, new Rect(0, i * 64, 420, 59), "", () => { Journal.Select(entry.id); View = "hub"; Render(); }, false, false);
                if (entry.id == Journal.SelectedId) { var mark = Rect("Selected entry", row.transform, new Rect(0, 8, 5, 43)).gameObject.AddComponent<Image>(); mark.color = new Color32(30, 89, 89, 255); mark.raycastTarget = false; }
                var plan = Journal.Plan(entry); string caption = (entry.date == WorkoutJournal.Date(Journal.Today) ? L("TODAY", "HOY") : DayCaption(entry.date)) + " · " + Name(entry, plan);
                row.AddInkText(Text(row.transform, new Rect(14, 4, 248, 50), caption, 21)); row.AddInkText(Text(row.transform, new Rect(269, 4, 146, 50), StatusLabel(status), 18, true, TextAnchor.MiddleRight));
                Rule(list, 6, i * 64 + 62, 405);
            }
            Text(page, new Rect(161, 532, 444, 32), L("Rest is part of the plan, too.", "Descansar también es parte del plan."), 20, true, TextAnchor.MiddleCenter);
        }
        void BuildRightPage()
        {
            var entry = Journal.Selected;
            if (entry == null)
            {
                Heading(692, L("YOUR ROUTINE", "TU RUTINA")); Text(page, new Rect(686, 248, 426, 144), L("Choose a day or schedule a routine. Missed days create no extra exercise debt.", "Elige un día o programa una rutina. Los días sin entrenar no crean una deuda de ejercicio."), 25);
                Button(page, "create", new Rect(699, 487, 393, 62), L("SCHEDULE ROUTINE", "PROGRAMAR RUTINA"), () => { Journal.BeginCreate(Journal.SelectedDay < Journal.Today ? Journal.Today : Journal.SelectedDay); OpenEditor(); }, true, true, 23); return;
            }
            var plan = Journal.Plan(entry); Text(page, new Rect(673, 164, 449, 48), Name(entry, plan).ToUpperInvariant(), 30, true, TextAnchor.MiddleCenter);
            Text(page, new Rect(674, 209, 448, 40), (entry.date == WorkoutJournal.Date(Journal.Today) ? L("Today", "Hoy") : DayCaption(entry.date)) + " · " + Mathf.CeilToInt(plan.estimated_seconds / 60f) + " min · " + plan.blocks.Length + L(" blocks", " bloques"), 22, true, TextAnchor.MiddleCenter);
            if (View == "full")
            {
                var scroll = Scroll(page, new Rect(674, 259, 451, 254), PlanHeight(plan)); DrawPlan(scroll, plan, 422);
                Button(page, "back-preview", new Rect(718, 512, 366, 52), L("BACK TO SUMMARY", "VOLVER AL RESUMEN"), () => { View = "hub"; Render(); }); return;
            }
            for (int i = 0; i < Math.Min(4, plan.blocks.Length); i++)
            {
                var block = plan.blocks[i]; Text(page, new Rect(675, 258 + i * 48, 326, 45), ShortName(block), 20); Text(page, new Rect(1004, 258 + i * 48, 122, 45), Dose(block, true), 21, true, TextAnchor.MiddleRight);
                if (i < 3) Rule(page, 675, 305 + i * 48, 449);
            }
            Button(page, "full", new Rect(729, 450, 342, 47), L("View full routine >", "Ver rutina completa >"), () => { View = "full"; Render(); }, false, false, 21);
            bool editable = Journal.CanEdit(entry);
            Button(page, "edit", new Rect(679, 512, 213, 52), editable ? L("EDIT ROUTINE", "EDITAR RUTINA") : L("NEW DRAFT", "NUEVO BORRADOR"), () => { if (editable) Journal.BeginEdit(); else Journal.BeginCreate(Journal.Today, true); OpenEditor(); }, true, false, 21);
            var prepare = Button(page, "prepare", new Rect(906, 512, 224, 52), (Journal.ActiveSession != null ? L("RESUME SESSION", "RETOMAR SESIÓN") : Live ? L("TO THE DUNGEON", "A LA MAZMORRA") : L("PREPARE SESSION", "PREPARAR SESIÓN")), Prepare, true, true, 21); prepare.interactable = Journal.CanPrepare || Journal.ActiveSession != null;
        }
        void OpenEditor() { editError = null; View = "edit"; Render(); }
        void BuildEditor()
        {
            Heading(170, L("EDIT ROUTINE", "EDITAR RUTINA"));
            var field = PixelFormField.Create(page, PixelFormField.Kind.Text, L("Name (optional)", "Nombre (opcional)"), Journal.Plan(Journal.Draft).name.Get(Language));
            Place((RectTransform)field.transform, new Rect(165, 222, 429, 104)); field.SetLabelColor(Ink); field.Background.pixelsPerUnitMultiplier = 2; field.Input.characterLimit = 24;
            field.SetValueWithoutNotify(Journal.Draft.title); field.Input.onValueChanged.AddListener(value => Journal.Draft.title = value); Register("edit-name", field.Input);
            Text(page, new Rect(168, 334, 426, 27), L("Planned date", "Fecha prevista"), 22);
            Button(page, "date-prev", new Rect(166, 369, 51, 52), "<", () => ChangeDraftDate(-1));
            Text(page, new Rect(224, 369, 307, 52), Journal.Draft.date, 23, true, TextAnchor.MiddleCenter);
            Button(page, "date-next", new Rect(541, 369, 51, 52), ">", () => ChangeDraftDate(1));
            Text(page, new Rect(168, 432, 425, 29), L("Time budget", "Tiempo disponible"), 22);
            var times = Journal.Durations; float width = (435f - (times.Length - 1) * 8) / times.Length;
            for (int i = 0; i < times.Length; i++) { int minutes = times[i]; Button(page, "duration-" + minutes, new Rect(166 + i * (width + 8), 468, width, 48), minutes + " min", () => { Journal.SetDuration(minutes); editError = null; Render(); }, true, minutes == Journal.Draft.minutes, times.Length > 3 ? 20 : 22); }
            Heading(692, L("EXERCISES", "EJERCICIOS"));
            var plan = Journal.Plan(Journal.Draft); var blocks = plan.blocks.Where(b => b.role == "main").ToArray();
            float listTop = 224;
            if (Live)
            {
                // Session type: strength foundations, aerobic base or movement practice.
                Button(page, "session-type", new Rect(675, 222, 455, 44), L("Type: ", "Tipo: ") + plan.name.Get(Language) + "  >", CycleType, true, false, 19);
                listTop = 272;
            }
            var scroll = Scroll(page, new Rect(673, listTop, 461, 470 - listTop), blocks.Length * 85);
            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i]; Text(scroll, new Rect(5, i * 85, 316, 56), block.name.Get(Language), 21);
                Text(scroll, new Rect(5, i * 85 + 53, 330, 27), Dose(block, false), 18);
                var optionsForSlot = Journal.Choices(block.id);
                var change = Button(scroll, "swap-" + block.id, new Rect(329, i * 85 + 10, 104, 52), L("Swap", "Cambiar"), () => CycleSwap(block.id, block.exercise_id), true, false, 17); change.interactable = optionsForSlot.Length > 1;
                Rule(scroll, 5, i * 85 + 83, 428);
            }
            Text(page, new Rect(675, 478, 451, 35), L("Warm-up and cool-down stay in the plan.", "Se conservan calentamiento y vuelta a la calma."), 18);
            Text(page, new Rect(165, 512, 435, 52), editError ?? L("Changing time resets exercise swaps.", "Cambiar el tiempo restablece los ejercicios."), 18);
            Button(page, "cancel-edit", new Rect(680, 512, 213, 52), L("CANCEL", "CANCELAR"), () => TryLeave(() => { Journal.CancelEdit(); View = "hub"; Render(); }));
            Button(page, "save-edit", new Rect(906, 512, 224, 52), L("SAVE", "GUARDAR"), SaveEdit, true, true);
        }
        void CycleType()
        {
            var types = Journal.Plans.Profiles.Select(p => p.id).ToArray(); int start = Array.IndexOf(types, Journal.EntryProfile(Journal.Draft));
            for (int i = 1; i <= types.Length; i++)
            {
                try { Journal.SetDraftProfile(types[(start + i) % types.Length]); editError = null; Render(); return; }
                catch (ArgumentException) { }
            }
            editError = L("No other session type fits your equipment.", "Ningún otro tipo de sesión es posible con tu equipo."); Render();
        }
        void ChangeDraftDate(int delta)
        { var next = WorkoutJournal.ParseDate(Journal.Draft.date).AddDays(delta); if (next < Journal.Today) return; Journal.Draft.date = WorkoutJournal.Date(next); Render(); }
        void CycleSwap(string slot, string current)
        {
            var choices = Journal.Choices(slot); int start = Array.FindIndex(choices, x => x.block.exercise_id == current);
            for (int i = 1; i <= choices.Length; i++)
            { try { Journal.SetSwap(slot, choices[(start + i) % choices.Length].block.exercise_id); editError = null; Render(); return; } catch (ArgumentException) { } }
            editError = L("No other swap fits this routine.", "Ningún otro cambio cabe en esta rutina."); Render();
        }
        public void SaveEdit()
        {
            if (Journal.SaveEdit(out var error)) { View = editReturn ?? "hub"; editReturn = null; history = false; Render(); }
            else { editError = L("Could not save. Check the name/date and try again. Your draft is kept.", "No se pudo guardar. Revisa nombre/fecha e inténtalo de nuevo. Conservamos el borrador."); Debug.LogWarning(error); Render(); }
        }
        public void Prepare()
        {
            if (Journal?.ActiveSession != null) { OpenBoss(); return; }
            if (Live && Journal != null && Journal.Loaded)
            {
                // One screen between the dungeon button and the fight.
                if (!Journal.CanPrepare) Journal.SelectDay(Journal.Today);
                var todays = Journal.Entries.Where(e => e.date == WorkoutJournal.Date(Journal.Today) && Journal.Status(e) == "planned").ToArray();
                if (!Journal.CanPrepare && todays.Length > 0) Journal.Select(todays[0].id);
                View = "briefing"; Render(); return;
            }
            if (Journal == null || !Journal.Loaded || !Journal.CanPrepare) { Notice(L("Choose today's pending routine first.", "Selecciona primero la rutina pendiente de hoy.")); return; }
            Journal.BeginPrepare(); View = "readiness"; Render();
        }
        void BuildReadiness()
        {
            Heading(170, L("HOW ARE YOU?", "¿CÓMO ESTÁS?"));
            string[] values = { "ready", "low_energy", "pain", "injury", "ill" };
            string[] labels = { L("Ready for my usual session", "Listo para mi sesión habitual"), L("Low energy / sore", "Poca energía / fatiga"), L("I have pain", "Tengo dolor"), L("I have an injury", "Tengo una lesión"), L("I feel ill", "Me siento enfermo") };
            for (int i = 0; i < values.Length; i++) { string value = values[i]; Button(page, "ready-" + value, new Rect(163, 220 + i * 60, 434, 52), labels[i], () => { Journal.Gate.SetReadiness(value); Render(); }, true, Journal.Gate.Readiness == value, 21); }
            Heading(692, L("BEFORE TRAINING", "ANTES DE ENTRENAR"));
            Text(page, new Rect(682, 220, 429, 89), L("Readiness is checked again each time. Rest is always an option.", "Revisamos tu estado en cada ocasión. Descansar siempre es una opción."), 23);
            if (Journal.IsTeen)
            {
                Text(page, new Rect(682, 325, 428, 39), L("Strength supervision", "Supervisión para fuerza"), 23);
                Button(page, "supervised", new Rect(679, 369, 449, 53), L("Appropriate supervision available", "Tengo supervisión adecuada"), () => { Journal.Gate.SetSupervision(true); Render(); }, true, Journal.Gate.Supervised == true, 20);
                Button(page, "unsupervised", new Rect(679, 432, 449, 53), L("Not available today", "Hoy no tengo supervisión"), () => { Journal.Gate.SetSupervision(false); Render(); }, true, Journal.Gate.Supervised == false, 20);
            }
            else Text(page, new Rect(682, 330, 429, 118), L("The plan adapts to your answer. Body appearance does not set training difficulty.", "La rutina se adapta a tu respuesta. La apariencia del personaje no define la dificultad."), 23);
            Button(page, "rest", new Rect(166, 515, 429, 49), L("REST / RETURN", "DESCANSAR / VOLVER"), () => { Journal.Gate.Rest(); Journal.ClearGate(); View = "hub"; Render(); });
            var review = Button(page, "review-readiness", new Rect(690, 512, 433, 52), L("REVIEW ROUTINE", "REVISAR RUTINA"), () => { if (Journal.ReviewReadiness()) { View = "review"; Render(); } }, true, true); review.interactable = Journal.Gate.CanReview;
        }
        void BuildReadinessReview()
        {
            var plan = Journal.PreparedPlan; bool ready = plan?.status == "draft_ready";
            Heading(170, ready ? L("REVIEW YOUR PLAN", "REVISA TU RUTINA") : L("TAKE CARE", "CUIDA TU SALUD"));
            string copy = Journal.Reviewed ? (Live ? L("Review saved. Enter the dungeon when you're ready to start.", "Revisión guardada. Entra a la mazmorra cuando quieras empezar.") : L("Review saved locally. No workout was started and no completion or reward was recorded.", "Revisión guardada localmente. No se inició un entrenamiento ni se registraron finalización o recompensas.")) : ready ? L("Review the full routine and any equipment limits before entering the dungeon. Training starts only when you choose ENTER.", "Revisa la rutina completa y las limitaciones de equipo antes de entrar a la mazmorra. Solo inicias al elegir ENTRAR.") : L("This result does not prescribe a training session. Rest or address the review requirements before trying again.", "Este resultado no indica una sesión de entrenamiento. Descansa o atiende los requisitos de revisión antes de volver a intentarlo.");
            Text(page, new Rect(166, 226, 430, 154), copy, 23);
            if (Journal.ReadinessAdjusted) Text(page, new Rect(166, 386, 430, 90), L("Your readiness changed the eligible exercises. Review this updated plan.", "Tu estado cambió los ejercicios disponibles. Revisa esta propuesta actualizada."), 21);
            if (ready && !Journal.Reviewed)
                Button(page, "acknowledge", new Rect(167, 456, 429, 62), Journal.Gate.Acknowledged ? L("[x] Plan reviewed", "[x] Rutina revisada") : L("[ ] I reviewed the plan and limits", "[ ] Revisé la rutina y sus límites"), () => { Journal.Gate.Acknowledge(!Journal.Gate.Acknowledged); Render(); }, true, Journal.Gate.Acknowledged, 20);
            Button(page, "back-readiness", new Rect(166, 512, 429, 52), Journal.Reviewed ? L("BACK TO WORKOUTS", "VOLVER A RUTINAS") : L("BACK", "VOLVER"), () => { if (Live) { Journal.ClearGate(); View = "briefing"; } else if (Journal.Reviewed) { Journal.ClearGate(); View = "hub"; } else { Journal.BeginPrepare(); View = "readiness"; } Render(); });
            Heading(692, ready ? L("YOUR SESSION", "TU SESIÓN") : L("NEXT STEP", "SIGUIENTE PASO"));
            var scroll = Scroll(page, new Rect(675, 225, 458, 284), PlanHeight(plan)); DrawPlan(scroll, plan, 426);
            if (ready && !Journal.Reviewed)
            {
                var save = Button(page, "save-review", new Rect(682, 512, 215, 52), L("SAVE REVIEW", "GUARDAR REVISIÓN"), () => { if (!Journal.SaveReview(out var error)) { Debug.LogWarning(error); Notice(L("Could not save. Please retry; no workout was started.", "No se pudo guardar. Reintenta; no se inició un entrenamiento.")); } else Render(); }, true, true); save.Label.fontSize = 19; save.interactable = Journal.CanSaveReview;
                var start = Button(page, "start-dungeon", new Rect(909, 512, 217, 52), L("ENTER DUNGEON", "ENTRAR"), OpenBoss, true, true, 20); start.interactable = Journal.CanSaveReview;
            }
        }
        public void OpenBoss()
        {
            bool restored = Journal.ActiveSession != null;
            try
            {
                if (!restored)
                {
                    Journal.SaveBoss(BossController.Create(Journal));
                }
                Journal.ClearGate(); Composition.gameObject.SetActive(false);
                BossWindow = new GameObject("Routine dungeon").AddComponent<PixelBossWindow>();
                BossWindow.Initialize(Journal, Language, character, () => {
                    var old = BossWindow; BossWindow = null; old.gameObject.SetActive(false); Destroy(old.gameObject);
                    Composition.gameObject.SetActive(true); View = "hub";
                    if (Live) { Exited?.Invoke(); return; } // straight back to the training hall
                    Render();
                }, restored);
            }
            catch (Exception e) { Debug.LogWarning(e.Message); Notice(L("The session could not be saved. Your review is kept; please retry.", "No se pudo guardar la sesión. Conservamos tu revisión; reintenta.")); }
        }
        // ---------- Live: one-tap briefing ----------
        bool supervisedToday;
        void BuildBriefing()
        {
            var entry = Journal.Selected;
            bool pending = Journal.CanPrepare && entry != null;
            Heading(170, L("TODAY'S QUEST", "MISIÓN DE HOY"));
            if (!pending)
            {
                bool doneToday = Journal.Entries.Any(e => e.date == WorkoutJournal.Date(Journal.Today) && e.status == "completed");
                Text(page, new Rect(166, 230, 430, 150), doneToday ? L("Boss defeated today. Rest is part of getting stronger.", "Jefe derrotado hoy. Descansar también te hace más fuerte.")
                    : L("Rest day. Want to train anyway?", "Día de descanso. ¿Quieres entrenar de todos modos?"), 26);
                Button(page, "brief-back", new Rect(166, 512, 430, 52), L("BACK", "VOLVER"), () => { View = "hub"; Render(); });
                Heading(692, L("EXTRA SESSION", "SESIÓN EXTRA"));
                Button(page, "train-anyway", new Rect(690, 260, 430, 96), L("TRAIN ANYWAY", "ENTRENAR"), TrainAnyway, true, true, 30);
                return;
            }
            var plan = Journal.Plan(entry);
            var mains = (plan.blocks ?? Array.Empty<TrainingBlock>()).Where(b => b.role == "main").ToArray();
            Text(page, new Rect(166, 214, 430, 46), Name(entry, plan).ToUpperInvariant(), 28, true, TextAnchor.MiddleCenter);
            Text(page, new Rect(166, 258, 430, 34), L("About ", "Aprox. ") + Mathf.CeilToInt(plan.estimated_seconds / 60f) + " min · " + mains.Length + L(" exercises", " ejercicios"), 21, true, TextAnchor.MiddleCenter);
            for (int i = 0; i < Math.Min(6, mains.Length); i++)
            {
                Text(page, new Rect(170, 300 + i * 38, 300, 36), ShortName(mains[i]), 20);
                Text(page, new Rect(470, 300 + i * 38, 126, 36), Dose(mains[i], true), 20, true, TextAnchor.MiddleRight);
            }
            Button(page, "brief-edit", new Rect(166, 516, 210, 48), L("Edit", "Editar"), () => { Journal.BeginEdit(); editReturn = "briefing"; OpenEditor(); }, true, false, 20);
            Button(page, "brief-back", new Rect(386, 516, 210, 48), L("Back", "Volver"), () => { View = "hub"; Render(); }, true, false, 20);
            Heading(692, L("READY?", "¿LISTO?"));
            float y = 222;
            if (Journal.IsTeen && plan.kind == "strength")
            {
                Button(page, "supervised", new Rect(690, y, 430, 52), (supervisedToday ? "[x] " : "[ ] ") + L("An adult supervisor is with me", "Tengo supervisión adulta"), () => { supervisedToday = !supervisedToday; Render(); }, true, supervisedToday, 19);
                y += 62;
            }
            var start = Button(page, "start-quest", new Rect(690, y, 430, 104), L("START", "¡COMENZAR!"), () => StartQuest("ready"), true, true, 36);
            y += 118;
            Button(page, "start-light", new Rect(690, y, 430, 54), L("Low energy: lighter session", "Poca energía: sesión ligera"), () => StartQuest("low_energy"), true, false, 19); y += 64;
            Button(page, "not-today", new Rect(690, y, 430, 54), L("Pain or feeling ill: rest", "Dolor o malestar: descansar"), () => StartQuest("pain"), true, false, 19);
        }
        void StartQuest(string readiness)
        {
            if (!Journal.CanPrepare) return;
            Journal.BeginPrepare(); Journal.Gate.SetReadiness(readiness);
            if (Journal.IsTeen) Journal.Gate.SetSupervision(supervisedToday);
            if (!Journal.ReviewReadiness()) { View = "briefing"; Render(); return; }
            if (Journal.PreparedPlan?.status != "draft_ready") { View = "review"; Render(); return; }
            Journal.Gate.Acknowledge(true);
            OpenBoss();
        }
        void TrainAnyway()
        {
            Journal.BeginCreate(Journal.Today);
            if (Journal.SaveEdit(out var error)) { View = "briefing"; Render(); }
            else { Debug.LogWarning(error); Notice(L("Couldn't create a session for today.", "No se pudo crear una sesión para hoy.")); }
        }

        // ---------- Live: routine editor and exercise picker ----------
        int pickerIndex = -1;
        string editReturn;
        string pickerRegion = "legs";
        static readonly string[] Regions = { "legs", "push", "pull", "core", "cardio", "mobility" };
        string RegionName(string r) => r == "legs" ? L("Legs", "Piernas") : r == "push" ? L("Push", "Empuje") : r == "pull" ? L("Pull", "Tracción")
            : r == "core" ? L("Core", "Abdomen") : r == "cardio" ? L("Cardio", "Cardio") : L("Mobility", "Movilidad");
        void Edit(Action change)
        {
            try { change(); editError = null; }
            catch (ArgumentException e) { editError = e.Message.Contains("at least one") ? L("Keep at least one exercise.", "Conserva al menos un ejercicio.") : L("That change isn't available for this routine.", "Ese cambio no está disponible para esta rutina."); }
            Render();
        }
        void BuildLiveEditor()
        {
            var plan = Journal.Plan(Journal.Draft);
            var exercises = Journal.DraftExercises;
            var mains = (plan.blocks ?? Array.Empty<TrainingBlock>()).Where(b => b.role == "main").ToArray();
            Heading(170, L("YOUR ROUTINE", "TU RUTINA"));
            var scroll = Scroll(page, new Rect(160, 214, 445, 288), Math.Max(1, exercises.Length) * 100 + 64);
            for (int i = 0; i < exercises.Length && i < mains.Length; i++)
            {
                int index = i; var b = mains[i]; float y = i * 100;
                Text(scroll, new Rect(4, y, 425, 38), b.name.Get(Language), 21);
                Text(scroll, new Rect(4, y + 40, 150, 44), Dose(b, true), 18);
                if (b.category != "cardio")
                {
                    Button(scroll, "sets-minus-" + i, new Rect(158, y + 40, 42, 44), "-", () => Edit(() => Journal.SetDraftSets(index, exercises[index].sets - 1)), true, false, 24);
                    Button(scroll, "sets-plus-" + i, new Rect(204, y + 40, 42, 44), "+", () => Edit(() => Journal.SetDraftSets(index, exercises[index].sets + 1)), true, false, 24);
                }
                Button(scroll, "change-" + i, new Rect(252, y + 40, 120, 44), L("Change", "Cambiar"), () => { pickerIndex = index; pickerRegion = RegionOf(exercises[index].exercise); View = "picker"; Render(); }, true, false, 18);
                Button(scroll, "remove-" + i, new Rect(378, y + 40, 52, 44), "X", () => Edit(() => Journal.RemoveDraftExercise(index)), true, false, 20).interactable = exercises.Length > 1;
                Rule(scroll, 4, y + 94, 425);
            }
            Button(scroll, "add-exercise", new Rect(4, exercises.Length * 100 + 6, 425, 50), L("+ ADD EXERCISE", "+ AÑADIR EJERCICIO"), () => { pickerIndex = -1; View = "picker"; Render(); }, true, true, 20)
                .interactable = exercises.Length < SoloGym.Training.TrainingEngine.MaxCustomExercises;
            Text(page, new Rect(162, 512, 440, 52), editError ?? L("About ", "Aprox. ") + Mathf.CeilToInt(plan.estimated_seconds / 60f) + " min", 18);
            Heading(692, L("SESSION", "SESIÓN"));
            var types = Journal.Plans.Profiles.Where(t => Fits(t.id) || t.id == Journal.EntryProfile(Journal.Draft)).ToArray();
            for (int i = 0; i < types.Length; i++)
            {
                string id = types[i].id;
                Button(page, "type-" + i, new Rect(675 + (i % 2) * 230, 214 + (i / 2) * 50, 222, 44), types[i].name.Get(Language), () => Edit(() => Journal.SetDraftProfile(id)), true, Journal.EntryProfile(Journal.Draft) == id, 17);
            }
            float row = 214 + ((types.Length + 1) / 2) * 50 + 6;
            var times = Journal.Durations; float width = (455f - (times.Length - 1) * 8) / times.Length;
            for (int i = 0; i < times.Length; i++) { int minutes = times[i]; Button(page, "duration-" + minutes, new Rect(675 + i * (width + 8), row, width, 44), minutes + " min", () => Edit(() => Journal.SetDuration(minutes)), true, minutes == Journal.Draft.minutes, 19); }
            row += 54;
            Button(page, "date-prev", new Rect(675, row, 48, 44), "<", () => ChangeDraftDate(-1));
            Text(page, new Rect(728, row, 349, 44), DayCaption(Journal.Draft.date), 21, true, TextAnchor.MiddleCenter);
            Button(page, "date-next", new Rect(1082, row, 48, 44), ">", () => ChangeDraftDate(1));
            Button(page, "cancel-edit", new Rect(675, 512, 220, 52), L("CANCEL", "CANCELAR"), () => TryLeave(() => { Journal.CancelEdit(); View = editReturn ?? "hub"; editReturn = null; Render(); }));
            Button(page, "save-edit", new Rect(910, 512, 220, 52), L("SAVE", "GUARDAR"), SaveEdit, true, true);
        }
        bool Fits(string profileId)
        {
            try { return Journal.Plans.Session(TrainingKeys.Make(profileId, Journal.Draft.minutes, "ready", false)) != null; }
            catch (ArgumentException) { return false; }
        }
                string RegionOf(string exercise)
        {
            var live = Journal.Plans as LiveTrainingPlans;
            return live != null && live.Engine.Exercises.TryGetValue(exercise ?? "", out var e) ? SoloGym.Training.TrainingEngine.Region(e) : "legs";
        }
        void BuildPicker()
        {
            var live = Journal.Plans as LiveTrainingPlans; if (live == null || Journal.Draft == null) { View = "hub"; Render(); return; }
            var current = Journal.DraftExercises;
            Heading(170, pickerIndex >= 0 ? L("CHANGE EXERCISE", "CAMBIAR EJERCICIO") : L("ADD EXERCISE", "AÑADIR EJERCICIO"));
            for (int i = 0; i < Regions.Length; i++) { string r = Regions[i]; Button(page, "region-" + r, new Rect(166, 220 + i * 50, 430, 44), RegionName(r), () => { pickerRegion = r; Render(); }, true, pickerRegion == r, 21); }
            Button(page, "picker-cancel", new Rect(166, 516, 430, 48), L("CANCEL", "CANCELAR"), () => { View = "edit"; Render(); });
            Heading(692, RegionName(pickerRegion).ToUpperInvariant());
            var options = live.Addable().Where(e => SoloGym.Training.TrainingEngine.Region(e) == pickerRegion
                && !current.Where((c, i) => i != pickerIndex).Any(c => c.exercise == e.id)).ToArray();
            var scroll = Scroll(page, new Rect(675, 214, 455, 340), Math.Max(1, options.Length) * 58);
            if (options.Length == 0) Text(scroll, new Rect(4, 0, 430, 90), L("Nothing else here fits your equipment.", "No hay más opciones con tu equipo."), 20);
            for (int i = 0; i < options.Length; i++)
            {
                var e = options[i];
                Button(scroll, "pick-" + e.id, new Rect(0, i * 58, 440, 52), e.name.Get(Language), () =>
                {
                    Edit(() => { if (pickerIndex >= 0) Journal.ReplaceDraftExercise(pickerIndex, e.id); else Journal.AddDraftExercise(e.id); });
                    View = "edit"; Render();
                }, true, current.Length > pickerIndex && pickerIndex >= 0 && current[pickerIndex].exercise == e.id, 19);
            }
        }

        void BuildSettings()
        {
            Heading(170, L("SETTINGS", "AJUSTES")); Text(page, new Rect(168, 222, 425, 91), L("Language", "Idioma"), 24);
            Button(page, "locale-es", new Rect(168, 321, 429, 60), "Español", () => SetLanguage("es"), true, Language == "es");
            Button(page, "locale-en", new Rect(168, 397, 429, 60), "English", () => SetLanguage("en"), true, Language == "en");
            if (Live)
            {
                var pr = Account.Profile;
                Heading(692, L("YOUR PLAN", "TU PLAN"));
                string days = string.Join(" · ", pr.weekdays.Select(d => PixelScheduleWindow.Day(d, Language)));
                Text(page, new Rect(681, 228, 427, 128), days + "\n" + pr.sessionMinutes + L(" min per session", " min por sesión"), 24);
                Text(page, new Rect(681, 365, 427, 125), L("Change goals, equipment or days from Home → Settings → Edit training plan. Your history is kept.", "Cambia objetivos, equipo o días desde Hogar → Ajustes → Editar plan. Tu historial se conserva."), 21);
            }
            else
            {
                Heading(692, L("LOCAL EXAMPLES", "EJEMPLOS LOCALES"));
                var p = catalog.profiles.First(x => x.id == profile);
                Text(page, new Rect(681, 228, 427, 128), p.name.Get(Language), 25); Text(page, new Rect(681, 365, 427, 125), L("Edits are saved on this device in an example journal, separately from personal training records.", "Los cambios se guardan en este dispositivo en un diario de ejemplo, separado de registros personales de entrenamiento."), 22);
            }
            Button(page, "done-settings", new Rect(682, 512, 443, 52), L("DONE", "LISTO"), () => { View = "hub"; Render(); }, true, true);
        }
        public void SetLanguage(string language) { Language = language == "es" ? "es" : "en"; PlayerPrefs.SetString("SoloGym.Home.Language.v1", Language); PlayerPrefs.Save(); Render(); }
        void ShowSettings() { View = "settings"; Render(); }
        public void LeaveToHub() { TryLeave(() => { Journal?.ClearGate(); Journal?.CancelEdit(); View = Journal?.Loaded == true ? "hub" : "error"; Render(); }); }
        public void TryLeave(Action action)
        {
            if (Journal?.Draft == null) { action(); return; }
            Modal(L("Discard these unsaved changes?", "¿Descartar estos cambios sin guardar?"), L("KEEP EDITING", "SEGUIR EDITANDO"), CloseModal,
                L("DISCARD", "DESCARTAR"), () => { Journal.CancelEdit(); CloseModal(); action(); });
        }
        public void Notice(string text) => Modal(text, L("BACK", "VOLVER"), CloseModal, null, null);
        void Modal(string message, string left, Action onLeft, string right, Action onRight)
        {
            CloseModal(); beforeModal = EventSystem.current.currentSelectedGameObject; pageGate.interactable = pageGate.blocksRaycasts = false; EventSystem.current.SetSelectedGameObject(null);
            modal = Rect("Journal modal", Composition, new Rect(0, 0, 1280, 720)); var blocker = modal.gameObject.AddComponent<Image>(); blocker.color = new Color(0, 0, 0, .6f);
            var frame = Frame(modal, new Rect(320, 218, 640, 284), "Confirmation"); Text(frame, new Rect(32, 26, 576, 142), message, 26, false, TextAnchor.MiddleCenter);
            var a = Action(frame, new Rect(right == null ? 170 : 32, 195, 278, 59), left, onLeft, true); a.name = "modal-keep";
            if (right != null) { var b = Action(frame, new Rect(330, 195, 278, 59), right, onRight, true); b.name = "modal-discard"; a.gameObject.AddComponent<PixelFieldTabNavigation>().Next = b; b.gameObject.AddComponent<PixelFieldTabNavigation>().Previous = a;
                a.navigation = new Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnRight = b }; b.navigation = new Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnLeft = a }; }
            a.Select();
        }
        public void CloseModal()
        { if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null; } if (pageGate != null) pageGate.interactable = pageGate.blocksRaycasts = true;
            if (beforeModal != null && beforeModal.activeInHierarchy && beforeModal.GetComponent<Selectable>()?.IsInteractable() == true) EventSystem.current.SetSelectedGameObject(beforeModal);
            beforeModal = null;
        }
        void Heading(float x, string value) => Text(page, new Rect(x - 7, 165, 444, 47), value, 29, true, TextAnchor.MiddleCenter);
        string Name(JournalEntry entry, TrainingPlan plan) => string.IsNullOrWhiteSpace(entry.title) ? plan.name.Get(Language) : entry.title;
        string WeekCaption() => Journal.WeekStart.ToString("d MMM", CultureInfo.GetCultureInfo(Language)) + " – " + Journal.WeekStart.AddDays(6).ToString("d MMM", CultureInfo.GetCultureInfo(Language));
        string DayCaption(string date) => WorkoutJournal.ParseDate(date).ToString("ddd d", CultureInfo.GetCultureInfo(Language)).ToUpperInvariant();
        string StatusLabel(string status) => status == "completed" ? L("Completed", "Completada") : status == "stopped" ? L("Stopped", "Interrumpida") : status == "missed" ? L("Not done", "No realizada") : L("Pending", "Pendiente");
        string ShortName(TrainingBlock b)
        {
            if (b.role == "warmup") return L("Warm-up", "Calentamiento");
            if (b.exercise_id == "dumbbell_goblet_squat") return L("Goblet squat", "Sentadilla con mancuerna");
            if (b.exercise_id == "dumbbell_romanian_deadlift") return L("Romanian deadlift", "Peso muerto rumano");
            if (b.exercise_id == "dumbbell_floor_press") return L("Floor chest press", "Press de pecho en el suelo");
            return b.name.Get(Language);
        }
        string Dose(TrainingBlock b, bool brief) => (b.unit == "minutes" ? b.quantity_min + " min" : b.sets + " × " + b.quantity_min + (b.quantity_max != b.quantity_min ? "–" + b.quantity_max : "") + (b.unit == "seconds" ? " s" : "")) + (b.per_side ? L(" / side", " / lado") : "") + (!brief && b.rest_seconds > 0 ? " · " + b.rest_seconds + L("s rest", "s descanso") : "");
        float MessageHeight(TrainingMessage message)
        {
            var settings = new TextGenerationSettings { font = Resources.Load<Font>("Fonts/PixelifySans"), fontSize = 20, lineSpacing = 1,
                scaleFactor = 1, generationExtents = new Vector2(412, 0), horizontalOverflow = HorizontalWrapMode.Wrap, verticalOverflow = VerticalWrapMode.Overflow };
            return Mathf.Max(54, new TextGenerator().GetPreferredHeight(message.text.Get(Language), settings) + 12);
        }
        float PlanHeight(TrainingPlan plan) => (plan?.messages ?? Array.Empty<TrainingMessage>()).Sum(m => MessageHeight(m) + 10) + (plan?.blocks?.Length ?? 0) * 108 + 68 + (SavedLogs.Length > 0 ? 60 + SavedLogs.Length * 112 : 0);
        BossLog[] SavedLogs => View == "full" ? Journal.Selected?.session?.logs ?? Array.Empty<BossLog>() : Array.Empty<BossLog>();
        void DrawPlan(Transform parent, TrainingPlan plan, float width)
        {
            if (plan == null) return;
            float y = 0;
            foreach (var message in plan.messages ?? Array.Empty<TrainingMessage>()) { float h = MessageHeight(message); Text(parent, new Rect(4, y, width - 8, h), message.text.Get(Language), 20); y += h + 10; }
            foreach (var block in plan.blocks ?? Array.Empty<TrainingBlock>())
            {
                Text(parent, new Rect(4, y, width - 8, 55), block.name.Get(Language), 23);
                Text(parent, new Rect(4, y + 58, width - 8, 43), Dose(block, false), 20); Rule(parent, 4, y + 105, width - 8); y += 108;
            }
            Text(parent, new Rect(4, y + 8, width - 8, 58), L("Manual records. Content pending professional review.", "Registro manual. Contenido pendiente de revisión profesional."), 18);
            if (SavedLogs.Length > 0)
            {
                y += 80; Text(parent, new Rect(4, y, width - 8, 38), L("SAVED RECORDS", "REGISTROS GUARDADOS"), 24); y += 40;
                foreach (var log in SavedLogs)
                {
                    Text(parent, new Rect(4, y, width - 8, 54), log.prescription.name.Get(Language) + " · " + (log.index + 1), 21);
                    string unit = log.prescription.unit == "minutes" ? "min" : log.prescription.unit == "seconds" ? "s" : "reps";
                    Text(parent, new Rect(4, y + 55, width - 8, 50), log.quantity.ToString("0.##", CultureInfo.InvariantCulture) + " " + unit
                        + (log.prescription.per_side ? L(" / side (lower count)", " / lado (menor cantidad)") : "")
                        + (log.hasLoad ? " · " + log.loadKg.ToString("0.##", CultureInfo.InvariantCulture) + " kg" : ""), 20);
                    Rule(parent, 4, y + 108, width - 8); y += 112;
                }
            }
        }
        PixelJournalAction Button(Transform parent, string id, Rect bounds, string label, Action action, bool frame = true, bool selected = false, int size = 22)
        { var b = PixelJournalUI.Action(parent, bounds, label, action, frame, selected, size); Register(id, b); return b; }
        void Register(string id, Selectable control) { control.name = id; Controls[id] = control; traversal.Add(control); }
        void WireTraversal()
        {
            for (int i = 0; i < traversal.Count; i++)
            {
                var previous = traversal[(i + traversal.Count - 1) % traversal.Count]; var next = traversal[(i + 1) % traversal.Count];
                var link = traversal[i].GetComponent<PixelFieldTabNavigation>() ?? traversal[i].gameObject.AddComponent<PixelFieldTabNavigation>(); link.Previous = previous; link.Next = next;
                traversal[i].navigation = new Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = previous, selectOnLeft = previous, selectOnDown = next, selectOnRight = next };
            }
        }
        public void Capture(string path)
        { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); var shot = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(path, shot.EncodeToPNG()); Destroy(shot); Debug.Log("WORKOUT_CAPTURE " + path); }
    }
}
