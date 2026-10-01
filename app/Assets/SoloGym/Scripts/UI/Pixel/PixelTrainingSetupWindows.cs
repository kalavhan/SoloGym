using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SoloGym.Training;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>
    /// Shared shell for the guild-panel setup steps after Goals: header, scrollable content and a
    /// primary action, using the same native controls and metrics as <see cref="PixelGoalsWindow"/>.
    /// </summary>
    public abstract class PixelSetupStep : MonoBehaviour
    {
        public PixelSecondaryAction Back { get; private set; }
        public PixelPrimaryButton Continue { get; private set; }
        public ScrollRect Scroll { get; private set; }
        public Text Status { get; private set; }
        public event Action StateChanged;
        public string Language { get; private set; } = "es";
        public Selectable LastControl => Continue.IsInteractable() ? (Selectable)Continue : Back;
        protected RectTransform content, viewport;
        protected Text title, progress;
        protected Selectable footer, locale;
        protected Action back;
        protected readonly List<Selectable> path = new List<Selectable>();
        protected bool binding;

        protected void Shell(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            transform.SetParent(panel, false); PixelJournalUI.Stretch((RectTransform)transform);
            back = backAction; footer = privacy; locale = language;
            Back = SecondaryButton(transform, GoBack); Place(Back, new Rect(20, 10, 112, 52));
            title = PixelJournalUI.Text(transform, new Rect(28, 48, 514, 48), "", 32, false, TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(transform, 62, 94, 446);
            progress = PixelJournalUI.Text(transform, new Rect(28, 98, 514, 30), "", 22, false, TextAnchor.MiddleCenter);
            content = PixelJournalUI.Scroll(transform, new Rect(28, 136, 514, 482), 486);
            viewport = (RectTransform)content.parent; Scroll = viewport.GetComponent<ScrollRect>();
            Status = PixelJournalUI.Text(content, new Rect(), "", 21, false); Status.color = new Color32(255, 194, 158, 255);
            Continue = PixelPrimaryButton.Create(content, "", Advance); Continue.SetFontSize(28); Continue.Background.pixelsPerUnitMultiplier = 2;
        }
        public virtual void SetLocale(string language) { if (Language == language) return; Language = language; Render(); }
        public abstract void GoBack();
        public abstract void Advance();
        public abstract void Render();
        public void Relayout(float panelHeight) { viewport.sizeDelta = new Vector2(514, Mathf.Max(70, panelHeight - 150)); Render(); }
        protected void Finish(float y)
        {
            if (Status.text.Length > 0) { Place(Status, new Rect(0, y, 502, 40)); float h = Mathf.Max(30, Status.preferredHeight + 8); Place(Status, new Rect(0, y, 502, h)); y += h + 6; }
            Status.gameObject.SetActive(Status.text.Length > 0);
            Place(Continue, new Rect(0, y + 4, 502, 64));
            content.sizeDelta = new Vector2(502, Mathf.Max(viewport.rect.height, y + 74));
            var chain = new List<Selectable> { Back }; chain.AddRange(path.Where(s => s != null && s.gameObject.activeInHierarchy));
            if (Continue.IsInteractable()) chain.Add(Continue);
            for (int i = 0; i < chain.Count; i++) Link(chain[i], i == 0 ? locale : chain[i - 1], i + 1 == chain.Count ? footer : chain[i + 1]);
            StateChanged?.Invoke();
        }
        protected void ResetScroll() { Scroll.StopMovement(); content.anchoredPosition = Vector2.zero; if (gameObject.activeInHierarchy) Back.Select(); }
        protected string L(string en, string es) => Language == "es" ? es : en;
        protected Text Paragraph(string text, ref float y, int size = 21, Color? color = null)
        {
            var t = PixelJournalUI.Text(content, new Rect(0, y, 502, 30), text, size, false, TextAnchor.UpperLeft);
            if (color.HasValue) t.color = color.Value;
            float h = Mathf.Max(30, t.preferredHeight + 6); Place(t, new Rect(0, y, 502, h)); y += h + 6; return t;
        }
        protected PixelChoiceOption Option(Transform parent, string id, string label, Rect bounds, Action<bool> changed, bool chip = false)
        {
            var option = PixelChoiceOption.Create(parent, id, label, null, null);
            option.Background.pixelsPerUnitMultiplier = 2;
            if (chip)
            {
                option.graphic = null; option.Checkmark.gameObject.SetActive(false);
                option.Label.alignment = TextAnchor.MiddleCenter; option.SetLabelInsets(4, 4); option.Label.fontSize = 21;
            }
            else { option.Label.alignment = TextAnchor.MiddleLeft; option.SetLabelInsets(66, 14); option.Label.fontSize = 22; }
            option.onValueChanged.AddListener(value => { if (!binding) changed(value); });
            Place(option, bounds); path.Add(option); return option;
        }
        protected void Set(PixelChoiceOption option, bool on) { option.SetIsOnWithoutNotify(on); option.RefreshVisual(); }
        protected static void Clear(Transform parent) { foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
        protected static void Place(Component c, Rect r) => PixelJournalUI.Place((RectTransform)c.transform, r);
        protected static PixelSecondaryAction SecondaryButton(Transform parent, UnityEngine.Events.UnityAction action)
        {
            var b = PixelSecondaryAction.Create(parent, PixelSecondaryAction.Appearance.Text, "", action); b.Label.fontSize = 22; b.SetHorizontalPadding(8); b.Background.pixelsPerUnitMultiplier = 2; return b;
        }
        static void Link(Selectable current, Selectable previous, Selectable next)
        {
            var tab = current.GetComponent<PixelFieldTabNavigation>() ?? current.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous = previous; tab.Next = next;
            current.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous, selectOnDown = next };
        }
        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(content)) return;
            Canvas.ForceUpdateCanvases(); var b = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
            float delta = b.min.y < viewport.rect.yMin ? viewport.rect.yMin - b.min.y : b.max.y > viewport.rect.yMax ? viewport.rect.yMax - b.max.y : 0;
            var p = content.anchoredPosition; p.y = Mathf.Clamp(p.y + delta, 0, Mathf.Max(0, content.rect.height - viewport.rect.height)); content.anchoredPosition = p;
        }
    }

    /// <summary>WIN-011: where you train and which real equipment is available (not avatar gear).</summary>
    public sealed class PixelEquipmentWindow : PixelSetupStep
    {
        public EquipmentController Controller { get; private set; }
        public event Action Completed;
        RectTransform list;
        readonly Dictionary<string, PixelChoiceOption> options = new Dictionary<string, PixelChoiceOption>();
        PixelChoiceOption bodyweight;
        string lastStep;

        public void Initialize(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            Shell(panel, backAction, privacy, language);
            list = PixelJournalUI.Rect("Equipment choices", content, new Rect(0, 0, 502, 10));
            Controller = new EquipmentController(true);
            Controller.Changed += _ => Render();
            Controller.CheckpointRequested += _ => Completed?.Invoke();
        }
        public void Open(string language, string environment = null, string[] equipment = null, bool bodyweightOnly = false)
        {
            SetLocale(language);
            if (!string.IsNullOrEmpty(environment) && string.IsNullOrEmpty(Controller.Model.EnvironmentId))
            {
                Controller.SelectEnvironment(environment); Controller.ContinueEnvironment();
                if (bodyweightOnly || environment == "outdoor") Controller.SetBodyweightOnly(true);
                else foreach (var id in equipment ?? Array.Empty<string>()) if (Controller.Model.VisibleEquipment.Any(e => e.id == id)) Controller.ToggleEquipment(id);
            }
            lastStep = null; Render();
        }
        public void ResetDraft() { Controller.Decline(); lastStep = null; }
        public string EnvironmentId => Controller.Model.EnvironmentId;
        public string[] EquipmentIds => Controller.Model.BodyweightOnly ? Array.Empty<string>() : Controller.Model.SelectedEquipmentIds;
        public bool BodyweightOnly => Controller.Model.BodyweightOnly;
        public override void SetLocale(string language) { if (Controller != null && Controller.Model.Language != language) Controller.SetLanguage(language); base.SetLocale(language); }
        public override void GoBack()
        {
            var step = Controller.Model.Step;
            if (step == EquipmentStep.Environment) back?.Invoke(); else Controller.Back();
        }
        public override void Advance()
        {
            if (!gameObject.activeInHierarchy) return;
            switch (Controller.Model.Step)
            {
                case EquipmentStep.Environment: Controller.ContinueEnvironment(); break;
                case EquipmentStep.Equipment:
                    // Outdoor training uses bodyweight-only explicitly.
                    if (Controller.Model.VisibleEquipment.Length == 0 && !Controller.Model.BodyweightOnly) Controller.SetBodyweightOnly(true);
                    Controller.ContinueEquipment(); break;
                case EquipmentStep.Review: Controller.ContinueReview(); break;
            }
        }
        public override void Render()
        {
            if (Controller == null || binding) return;
            binding = true;
            try
            {
                var m = Controller.Model; Clear(list); options.Clear(); path.Clear(); bodyweight = null;
                Back.SetLabel(L("Back", "Volver"));
                float y = 0;
                if (m.Step == EquipmentStep.Environment)
                {
                    title.text = L("WHERE DO YOU TRAIN?", "¿DÓNDE ENTRENAS?"); progress.text = L("1 OF 3 · PLACE", "1 DE 3 · LUGAR");
                    foreach (var env in m.Environments)
                    {
                        string id = env.id;
                        var o = Option(list, id, env.Label(m.Language), new Rect(0, y, 502, 60), on => { if (on) Controller.SelectEnvironment(id); else Render(); });
                        Set(o, m.EnvironmentId == id); options[id] = o; y += 68;
                    }
                    y += 4;
                    Paragraph(L("This is the real equipment you can use for training, not your character's gear.", "Es el equipo real que puedes usar para entrenar, no el equipo de tu personaje."), ref y, 20, new Color32(183, 185, 175, 255));
                }
                else if (m.Step == EquipmentStep.Equipment)
                {
                    title.text = L("YOUR EQUIPMENT", "TU EQUIPO"); progress.text = L("2 OF 3 · ", "2 DE 3 · ") + m.EnvironmentLabel.ToUpperInvariant();
                    bodyweight = Option(list, "bodyweight", L("Bodyweight only", "Solo peso corporal"), new Rect(0, y, 502, 60), on => Controller.SetBodyweightOnly(on));
                    Set(bodyweight, m.BodyweightOnly); y += 72;
                    if (m.VisibleEquipment.Length == 0)
                        Paragraph(L("Outdoor sessions use walking-based cardio with no equipment.", "Las sesiones al aire libre usan cardio caminando, sin equipo."), ref y);
                    else
                    {
                        Paragraph(L("Or choose everything you have:", "O elige todo lo que tienes:"), ref y, 21);
                        foreach (var item in m.VisibleEquipment)
                        {
                            string id = item.id;
                            var o = Option(list, id, item.Label(m.Language), new Rect(0, y, 502, 56), _ => Controller.ToggleEquipment(id));
                            Set(o, !m.BodyweightOnly && m.SelectedEquipmentIds.Contains(id)); options[id] = o; y += 62;
                        }
                    }
                }
                else
                {
                    title.text = L("REVIEW YOUR EQUIPMENT", "REVISA TU EQUIPO"); progress.text = L("3 OF 3 · REVIEW", "3 DE 3 · RESUMEN");
                    Paragraph(L("Place", "Lugar"), ref y, 21, new Color32(183, 185, 175, 255));
                    Paragraph(m.EnvironmentLabel, ref y, 26);
                    var edit = SecondaryButton(list, () => Controller.EditEnvironment()); edit.SetLabel(L("Change place", "Cambiar lugar")); Place(edit, new Rect(0, y, 502, 52)); path.Add(edit); y += 62;
                    Paragraph(L("Equipment", "Equipo"), ref y, 21, new Color32(183, 185, 175, 255));
                    Paragraph(m.EquipmentSummary, ref y, 23);
                    var editGear = SecondaryButton(list, () => Controller.EditEquipment()); editGear.SetLabel(L("Change equipment", "Cambiar equipo")); Place(editGear, new Rect(0, y, 502, 52)); path.Add(editGear); y += 62;
                    Paragraph(L("Exercises are chosen only from what you selected. You can update this later in Settings.", "Los ejercicios se eligen solo con lo que seleccionaste. Puedes cambiarlo después en Ajustes."), ref y, 20, new Color32(183, 185, 175, 255));
                }
                list.sizeDelta = new Vector2(502, y);
                Status.text = m.Error ?? "";
                Continue.SetLabel(m.Step == EquipmentStep.Review ? L("NEXT: SCHEDULE", "SIGUE: HORARIO") : L("CONTINUE", "CONTINUAR"));
                Continue.interactable = m.CanContinue || (m.Step == EquipmentStep.Equipment && m.VisibleEquipment.Length == 0);
                Finish(y + 8);
                string view = m.Step.ToString(); if (lastStep != view) { lastStep = view; ResetScroll(); }
            }
            finally { binding = false; }
        }
        void OnDestroy() { Controller?.Dispose(); }
    }

    /// <summary>WIN-012: training weekdays (2–5) and session length.</summary>
    public sealed class PixelScheduleWindow : PixelSetupStep
    {
        public event Action Completed;
        readonly HashSet<int> days = new HashSet<int>();
        public int Minutes { get; private set; } = 25;
        public int[] Weekdays => days.OrderBy(d => d).ToArray();
        RectTransform list;
        bool opened;
        static readonly string[] En = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, Es = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
        static readonly string[] LongEn = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }, LongEs = { "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };
        public static string Day(int index, string language, bool longName = false) => language == "es" ? (longName ? LongEs : Es)[index] : (longName ? LongEn : En)[index];

        public void Initialize(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            Shell(panel, backAction, privacy, language);
            list = PixelJournalUI.Rect("Schedule choices", content, new Rect(0, 0, 502, 10));
        }
        public void Open(string language, int[] weekdays = null, int minutes = 0)
        {
            SetLocale(language);
            if (!opened && weekdays != null && weekdays.Length > 0) { days.Clear(); foreach (var d in weekdays) if (d >= 0 && d <= 6) days.Add(d); }
            if (!opened && LiveTrainingPlans.SessionDurations.Contains(minutes)) Minutes = minutes;
            opened = true; Render(); ResetScroll();
        }
        public void ResetDraft() { days.Clear(); Minutes = 25; opened = false; }
        public bool Valid => days.Count >= 2 && days.Count <= 5;
        public override void GoBack() => back?.Invoke();
        public override void Advance() { if (gameObject.activeInHierarchy && Valid) Completed?.Invoke(); }
        string notice;
        public void Toggle(int day, bool on)
        {
            notice = null;
            if (on && days.Count >= 5 && !days.Contains(day)) notice = L("Choose up to 5 days. Rest days are part of the plan.", "Elige hasta 5 días. Los días de descanso son parte del plan.");
            else if (on) days.Add(day); else days.Remove(day);
            Render();
        }
        public void SetMinutes(int value) { notice = null; if (LiveTrainingPlans.SessionDurations.Contains(value)) Minutes = value; Render(); }
        public override void Render()
        {
            if (list == null || binding) return;
            binding = true;
            try
            {
                Clear(list); path.Clear();
                Back.SetLabel(L("Back", "Volver"));
                title.text = L("YOUR TRAINING WEEK", "TU SEMANA DE ENTRENAMIENTO"); progress.text = L("SCHEDULE · 2 TO 5 DAYS", "HORARIO · 2 A 5 DÍAS");
                float y = 0;
                var head = PixelJournalUI.Text(list, new Rect(0, y, 502, 30), L("Training days", "Días de entrenamiento"), 22, false); y += 36;
                for (int i = 0; i < 7; i++)
                {
                    int day = i;
                    var o = Option(list, "day-" + i, Day(i, Language), new Rect(i * 72, y, 66, 60), on => Toggle(day, on), true);
                    Set(o, days.Contains(i));
                }
                y += 72;
                PixelJournalUI.Text(list, new Rect(0, y, 502, 30), L("Session length", "Duración de cada sesión"), 22, false); y += 36;
                var durations = LiveTrainingPlans.SessionDurations;
                for (int i = 0; i < durations.Length; i++)
                {
                    int value = durations[i];
                    var o = Option(list, "minutes-" + value, value + " min", new Rect(i * 127, y, 121, 60), on => { if (on) SetMinutes(value); else Render(); }, true);
                    Set(o, Minutes == value);
                }
                y += 76;
                string summary = days.Count == 0 ? L("No days selected yet.", "Aún no eliges días.")
                    : string.Join(" · ", Weekdays.Select(d => Day(d, Language))) + "  —  " + Minutes + L(" min each", " min cada una");
                Paragraph(summary, ref y, 23);
                Paragraph(L("Rest days are part of the plan. Missed days never add extra work.", "Los días de descanso son parte del plan. Los días sin entrenar nunca suman trabajo extra."), ref y, 20, new Color32(183, 185, 175, 255));
                list.sizeDelta = new Vector2(502, y);
                Status.text = notice ?? (days.Count == 1 ? L("Choose at least 2 days.", "Elige al menos 2 días.") : "");
                Continue.SetLabel(L("SEE MY PLAN", "VER MI PLAN")); Continue.interactable = Valid;
                Finish(y + 8);
            }
            finally { binding = false; }
        }
    }

    /// <summary>Plan review and acceptance: the generated week from the person's own setup.</summary>
    public sealed class PixelPlanReviewWindow : PixelSetupStep
    {
        public event Action Accepted;
        RectTransform list;
        PixelChoiceOption acknowledge;
        bool acknowledged, blocked;
        TrainingInput input;
        int[] weekdays = Array.Empty<int>();
        public bool Acknowledged => acknowledged;
        public bool Blocked => blocked;
        public string Error { get; set; }

        public void Initialize(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            Shell(panel, backAction, privacy, language);
            list = PixelJournalUI.Rect("Plan summary", content, new Rect(0, 0, 502, 10));
        }
        public void Open(string language, TrainingInput setup, int[] days)
        {
            input = setup?.Copy(); weekdays = (days ?? Array.Empty<int>()).OrderBy(d => d).ToArray(); acknowledged = false; Error = null;
            SetLocale(language); Render(); ResetScroll();
        }
        public override void GoBack() => back?.Invoke();
        public override void Advance() { if (gameObject.activeInHierarchy && acknowledged && !blocked && input != null) Accepted?.Invoke(); }
        string Goal(string id) => id == "strength" ? L("Strength", "Fuerza") : id == "muscle_growth" ? L("Muscle growth", "Masa muscular") : id == "endurance" ? L("Endurance", "Resistencia")
            : id == "mobility" ? L("Mobility", "Movilidad") : L("General fitness", "Condición general");
        public override void Render()
        {
            if (list == null || binding) return;
            binding = true;
            try
            {
                Clear(list); path.Clear(); acknowledge = null; blocked = false;
                Back.SetLabel(L("Back", "Volver"));
                title.text = L("YOUR TRAINING PLAN", "TU PLAN DE ENTRENAMIENTO"); progress.text = L("REVIEW AND START", "REVISA Y COMIENZA");
                float y = 0; var muted = new Color32(183, 185, 175, 255);
                if (input == null) { Paragraph(L("Setup is incomplete.", "La configuración está incompleta."), ref y); list.sizeDelta = new Vector2(502, y); Continue.interactable = false; Finish(y); return; }
                var engine = TrainingContent.Engine;
                var effectiveGoal = TrainingEngine.EffectiveGoal(input);
                Paragraph(Goal(effectiveGoal) + " · " + (input.experience == "intermediate" ? L("Intermediate", "Intermedio") : L("Beginner", "Principiante"))
                    + " · " + (input.environment == "gym" ? L("Gym", "Gimnasio") : input.environment == "outdoor" ? L("Outdoor", "Aire libre") : L("Home", "Casa")), ref y, 22);
                if (effectiveGoal != input.goal) Paragraph(L("Under 18: we start with general fitness foundations.", "Menores de 18: comenzamos con bases de condición general."), ref y, 19, muted);
                var templates = engine.WeekTemplates(input, weekdays);
                var seen = new HashSet<string>(); var messages = new List<string>();
                TrainingPlan first = null;
                for (int i = 0; i < weekdays.Length; i++)
                {
                    var p = input.Copy(); p.teen_supervision_available = input.IsTeen; p.readiness = "ready";
                    var plan = engine.GenerateSession(p, templates[i]);
                    if (plan.status != "draft_ready") blocked |= plan.status == "needs_changes";
                    if (first == null && plan.status == "draft_ready") first = plan;
                    string minutes = plan.status == "draft_ready" ? Mathf.CeilToInt(plan.estimated_seconds / 60f) + " min" : L("needs changes", "requiere cambios");
                    var row = PixelJournalUI.Text(list, new Rect(0, y, 502, 34), PixelScheduleWindow.Day(weekdays[i], Language) + "  ·  " + plan.name.Get(Language) + "  ·  " + minutes, 22, false); y += 38;
                    foreach (var m in plan.messages ?? Array.Empty<TrainingMessage>())
                        if (seen.Add(m.code) && m.code != "difficulty_adjusted") messages.Add(m.text.Get(Language));
                }
                PixelJournalUI.Rule(list, 4, y + 2, 494); y += 12;
                if (first != null)
                {
                    Paragraph(L("First session: ", "Primera sesión: ") + first.name.Get(Language), ref y, 22);
                    foreach (var b in first.blocks)
                    {
                        string dose = b.unit == "minutes" ? b.quantity_min + " min" : b.sets + " × " + b.quantity_min + (b.quantity_max != b.quantity_min ? "–" + b.quantity_max : "") + (b.unit == "seconds" ? " s" : "") + (b.per_side ? L(" / side", " / lado") : "");
                        Paragraph("• " + b.name.Get(Language) + "  " + dose, ref y, 20);
                    }
                }
                if (input.IsTeen && templates.Any(t => engine.Template(t).kind == "strength"))
                    messages.Add(L("Strength days need appropriate supervision; you'll confirm it before each session.", "Los días de fuerza requieren supervisión adecuada; la confirmarás antes de cada sesión."));
                foreach (var m in messages) Paragraph(m, ref y, 19, new Color32(255, 214, 160, 255));
                Paragraph(L("Readiness is checked before every session. Difficulty can change during the dungeon. Exercise content is a draft pending professional review.",
                    "Antes de cada sesión revisamos cómo estás. La dificultad puede cambiar en la mazmorra. El contenido de ejercicios es un borrador pendiente de revisión profesional."), ref y, 19, muted);
                if (blocked) Paragraph(L("Some sessions don't fit the time you chose. Go back and choose more time.", "Algunas sesiones no caben en el tiempo elegido. Vuelve y elige más tiempo."), ref y, 21, new Color32(255, 194, 158, 255));
                else
                {
                    acknowledge = Option(list, "acknowledge", L("I reviewed my plan", "Revisé mi plan"), new Rect(0, y, 502, 60), on => { acknowledged = on; Render(); });
                    Set(acknowledge, acknowledged); y += 70;
                }
                list.sizeDelta = new Vector2(502, y);
                Status.text = Error ?? "";
                Continue.SetLabel(L("START TRAINING", "COMENZAR")); Continue.interactable = acknowledged && !blocked;
                Finish(y + 8);
            }
            finally { binding = false; }
        }
    }
}
