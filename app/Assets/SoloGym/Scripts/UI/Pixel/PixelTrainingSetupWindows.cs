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
    /// Shared shell for the guild-panel setup steps: header, one scrollable content list rebuilt on
    /// every render, and a primary action. Kept short on purpose: training time matters more than forms.
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
        protected RectTransform content, viewport, list;
        protected Text title, progress;
        protected Selectable footer, locale;
        protected Action back;
        protected readonly List<Selectable> path = new List<Selectable>();
        protected bool binding;
        protected static readonly Color Muted = new Color32(183, 185, 175, 255);

        protected void Shell(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            transform.SetParent(panel, false); PixelJournalUI.Stretch((RectTransform)transform);
            back = backAction; footer = privacy; locale = language;
            Back = SecondaryButton(transform, GoBack); Place(Back, new Rect(20, 10, 112, 52));
            title = PixelJournalUI.Text(transform, new Rect(28, 48, 514, 48), "", 32, false, TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(transform, 62, 94, 446);
            progress = PixelJournalUI.Text(transform, new Rect(28, 98, 514, 30), "", 22, false, TextAnchor.MiddleCenter);
            progress.color = Muted;
            content = PixelJournalUI.Scroll(transform, new Rect(28, 136, 514, 482), 486);
            viewport = (RectTransform)content.parent; Scroll = viewport.GetComponent<ScrollRect>();
            list = PixelJournalUI.Rect("Step controls", content, new Rect(0, 0, 502, 10));
            Status = PixelJournalUI.Text(content, new Rect(), "", 21, false); Status.color = new Color32(255, 194, 158, 255);
            Continue = PixelPrimaryButton.Create(content, "", Advance); Continue.SetFontSize(28); Continue.Background.pixelsPerUnitMultiplier = 2;
        }
        public virtual void SetLocale(string language) { if (Language == language) return; Language = language; Render(); }
        public abstract void GoBack();
        public abstract void Advance();
        public abstract void Render();
        public void Relayout(float panelHeight) { viewport.sizeDelta = new Vector2(514, Mathf.Max(70, panelHeight - 150)); Render(); }
        /// <summary>Clear every control from the previous render (labels included).</summary>
        protected void Begin()
        {
            foreach (Transform child in list) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            path.Clear(); Back.SetLabel(L("Back", "Volver"));
        }
        protected void Finish(float y)
        {
            list.sizeDelta = new Vector2(502, y);
            y += 8;
            if (Status.text.Length > 0) { Place(Status, new Rect(0, y, 502, 52)); y += 58; }
            Status.gameObject.SetActive(Status.text.Length > 0);
            Place(Continue, new Rect(0, y, 502, 64));
            content.sizeDelta = new Vector2(502, Mathf.Max(viewport.rect.height, y + 72));
            var chain = new List<Selectable> { Back }; chain.AddRange(path.Where(s => s != null));
            if (Continue.IsInteractable()) chain.Add(Continue);
            for (int i = 0; i < chain.Count; i++) Link(chain[i], i == 0 ? locale : chain[i - 1], i + 1 == chain.Count ? footer : chain[i + 1]);
            StateChanged?.Invoke();
        }
        protected void ResetScroll() { Scroll.StopMovement(); content.anchoredPosition = Vector2.zero; }
        protected string L(string en, string es) => Language == "es" ? es : en;
        protected Text Label(string text, float y, int size = 22, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, float height = 34)
        {
            var t = PixelJournalUI.Text(list, new Rect(0, y, 502, height), text, size, false, align);
            if (color.HasValue) t.color = color.Value;
            return t;
        }
        protected PixelChoiceOption Chip(string id, string label, Rect bounds, bool on, Action<bool> changed, int size = 21)
        {
            var option = PixelChoiceOption.Create(list, id, label, null, null);
            option.Background.pixelsPerUnitMultiplier = 2;
            option.graphic = null; option.Checkmark.gameObject.SetActive(false);
            option.Label.alignment = TextAnchor.MiddleCenter; option.SetLabelInsets(6, 6); option.Label.fontSize = size;
            option.SetIsOnWithoutNotify(on); option.RefreshVisual();
            option.onValueChanged.AddListener(value => { if (!binding) changed(value); });
            Place(option, bounds); path.Add(option); return option;
        }
        protected PixelPrimaryButton Stepper(string label, Rect bounds, Action step)
        {
            var b = PixelPrimaryButton.Create(list, label, () => { }); b.SetFontSize(30); b.Background.pixelsPerUnitMultiplier = 2;
            Place(b, bounds); var hold = b.gameObject.AddComponent<PixelHoldRepeat>(); hold.Step = step;
            b.onClick.AddListener(() => { if (!hold.ConsumePointer()) step(); });
            path.Add(b); return b;
        }
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
    }

    /// <summary>Press-and-hold repeat for steppers: one step on press, then faster steps while held.</summary>
    public sealed class PixelHoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action Step;
        bool down, usedPointer;
        float next, heldFor;
        public void OnPointerDown(PointerEventData e) { down = usedPointer = true; heldFor = 0; next = .4f; Step?.Invoke(); }
        public void OnPointerUp(PointerEventData e) => down = false;
        public void OnPointerExit(PointerEventData e) => down = false;
        /// <summary>True once after a pointer press, so the button's click does not step twice.</summary>
        public bool ConsumePointer() { bool used = usedPointer; usedPointer = false; return used; }
        void Update()
        {
            if (!down) return;
            heldFor += Time.unscaledDeltaTime;
            if (heldFor < next) return;
            Step?.Invoke(); next = heldFor + (heldFor > 1.6f ? .04f : .09f);
        }
        void OnDisable() => down = false;
    }

    /// <summary>Optional height and bodyweight with steppers (no typing). Private; never sets difficulty.</summary>
    public sealed class PixelBodyWindow : PixelSetupStep
    {
        public event Action Completed;
        public double HeightCm { get; private set; } = 170;
        public double WeightKg { get; private set; } = 70;
        public bool Provided { get; private set; }
        public bool Imperial { get; private set; }
        Text heightValue, weightValue;

        public void Initialize(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            Shell(panel, backAction, privacy, language);
        }
        public void Open(string language, bool hasHeight = false, double heightCm = 0, bool hasWeight = false, double weightKg = 0, string units = null)
        {
            if (hasHeight && heightCm >= 100 && heightCm <= 230) HeightCm = heightCm;
            if (hasWeight && weightKg >= 30 && weightKg <= 250) WeightKg = weightKg;
            if (units == "imperial") Imperial = true;
            SetLocale(language); Render(); ResetScroll();
        }
        public void ResetDraft() { HeightCm = 170; WeightKg = 70; Provided = false; }
        public override void GoBack() => back?.Invoke();
        public override void Advance() { if (!gameObject.activeInHierarchy) return; Provided = true; Completed?.Invoke(); }
        public void Skip() { Provided = false; Completed?.Invoke(); }
        string Height() { if (!Imperial) return Mathf.RoundToInt((float)HeightCm) + " cm"; int inches = Mathf.RoundToInt((float)(HeightCm / 2.54)); return inches / 12 + " ft " + inches % 12 + " in"; }
        string Weight() => Imperial ? Mathf.RoundToInt((float)(WeightKg * 2.20462)) + " lb" : Mathf.RoundToInt((float)WeightKg) + " kg";
        void StepHeight(int dir) { HeightCm = Math.Max(100, Math.Min(230, Imperial ? Math.Round(HeightCm / 2.54 + dir) * 2.54 : Math.Round(HeightCm) + dir)); heightValue.text = Height(); }
        void StepWeight(int dir) { WeightKg = Math.Max(30, Math.Min(250, Imperial ? Math.Round(WeightKg * 2.20462 + dir) / 2.20462 : Math.Round(WeightKg) + dir)); weightValue.text = Weight(); }
        public override void Render()
        {
            if (list == null || binding) return;
            binding = true;
            try
            {
                Begin();
                title.text = L("YOUR BODY", "TU CUERPO"); progress.text = L("Private · optional", "Privado · opcional");
                float y = 4;
                Chip("metric", "cm · kg", new Rect(0, y, 247, 52), !Imperial, on => { Imperial = !on; Render(); });
                Chip("imperial", "ft · lb", new Rect(255, y, 247, 52), Imperial, on => { Imperial = on; Render(); });
                y += 72;
                Label(L("Height", "Estatura"), y, 22, Muted); y += 34;
                Stepper("−", new Rect(0, y, 96, 72), () => StepHeight(-1));
                heightValue = Label(Height(), y, 40, null, TextAnchor.MiddleCenter, 72);
                Stepper("+", new Rect(406, y, 96, 72), () => StepHeight(1));
                y += 96;
                Label(L("Bodyweight", "Peso corporal"), y, 22, Muted); y += 34;
                Stepper("−", new Rect(0, y, 96, 72), () => StepWeight(-1));
                weightValue = Label(Weight(), y, 40, null, TextAnchor.MiddleCenter, 72);
                Stepper("+", new Rect(406, y, 96, 72), () => StepWeight(1));
                y += 92;
                var skip = SecondaryButton(list, Skip); skip.SetLabel(L("Skip for now", "Omitir por ahora")); Place(skip, new Rect(0, y, 502, 52)); path.Add(skip); y += 56;
                Status.text = "";
                Continue.SetLabel(L("CONTINUE", "CONTINUAR")); Continue.interactable = true;
                Finish(y);
            }
            finally { binding = false; }
        }
    }

    /// <summary>WIN-011 on one screen: where you train and the real equipment you have.</summary>
    public sealed class PixelEquipmentWindow : PixelSetupStep
    {
        public event Action Completed;
        EquipmentCatalogFile catalog;
        readonly Dictionary<string, EquipmentCatalogEntry> byId = new Dictionary<string, EquipmentCatalogEntry>();
        readonly HashSet<string> selected = new HashSet<string>();
        string environment = "";
        bool bodyweight, opened;
        public string EnvironmentId => environment;
        public bool BodyweightOnly => bodyweight || environment == "outdoor";
        public string[] EquipmentIds => BodyweightOnly ? Array.Empty<string>() : selected.Where(Visible).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        public void Initialize(Transform panel, Action backAction, Selectable privacy, Selectable language)
        {
            Shell(panel, backAction, privacy, language);
            var asset = Resources.Load<TextAsset>("Equipment/Catalog");
            catalog = asset == null ? new EquipmentCatalogFile() : JsonUtility.FromJson<EquipmentCatalogFile>(asset.text);
            foreach (var e in catalog.equipment ?? Array.Empty<EquipmentCatalogEntry>()) byId[e.id] = e;
        }
        public void Open(string language, string env = null, string[] equipment = null, bool bodyweightOnly = false)
        {
            if (!opened && !string.IsNullOrEmpty(env))
            {
                environment = env; bodyweight = bodyweightOnly; selected.Clear();
                foreach (var id in equipment ?? Array.Empty<string>()) selected.Add(id);
            }
            opened = true; SetLocale(language); Render(); ResetScroll();
        }
        public void ResetDraft() { environment = ""; bodyweight = false; selected.Clear(); opened = false; }
        string[] VisibleIds() => environment == "home" ? catalog.visible_home : environment == "gym" ? catalog.visible_gym : catalog.visible_outdoor ?? Array.Empty<string>();
        bool Visible(string id) => (VisibleIds() ?? Array.Empty<string>()).Contains(id);
        public bool Valid => environment.Length > 0 && (BodyweightOnly || selected.Any(Visible));
        public override void GoBack() => back?.Invoke();
        public override void Advance() { if (gameObject.activeInHierarchy && Valid) Completed?.Invoke(); }
        public override void Render()
        {
            if (list == null || binding) return;
            binding = true;
            try
            {
                Begin();
                title.text = L("WHERE DO YOU TRAIN?", "¿DÓNDE ENTRENAS?"); progress.text = L("Real equipment you can use", "Equipo real que puedes usar");
                float y = 4;
                var envs = catalog.environments ?? Array.Empty<EnvironmentCatalogEntry>();
                for (int i = 0; i < envs.Length; i++)
                {
                    string id = envs[i].id; float w = (502 - (envs.Length - 1) * 8f) / envs.Length;
                    Chip("env-" + id, envs[i].Label(Language), new Rect(i * (w + 8), y, w, 60), environment == id, on => { if (on && environment != id) { environment = id; bodyweight = false; } Render(); }, 22);
                }
                y += 76;
                if (environment == "outdoor")
                {
                    Label(L("Outdoor sessions use walking-based cardio, no equipment.", "Al aire libre: cardio caminando, sin equipo."), y, 20, Muted, TextAnchor.MiddleLeft, 56); y += 60;
                }
                else if (environment.Length > 0)
                {
                    Chip("bodyweight", L("Bodyweight only", "Solo peso corporal"), new Rect(0, y, 502, 56), bodyweight, on => { bodyweight = on; Render(); });
                    y += 68;
                    var ids = VisibleIds() ?? Array.Empty<string>();
                    for (int i = 0; i < ids.Length; i++)
                    {
                        string id = ids[i]; if (!byId.TryGetValue(id, out var item)) continue;
                        Chip("eq-" + id, item.Label(Language), new Rect((i % 2) * 255, y + (i / 2) * 60, 247, 52), !bodyweight && selected.Contains(id),
                            on => { bodyweight = false; if (on) selected.Add(id); else selected.Remove(id); Render(); }, 19);
                    }
                    y += ((ids.Length + 1) / 2) * 60 + 4;
                }
                Status.text = "";
                Continue.SetLabel(L("CONTINUE", "CONTINUAR")); Continue.interactable = Valid;
                Finish(y);
            }
            finally { binding = false; }
        }
    }

    /// <summary>WIN-012: training weekdays (2–5) and session length. Continuing starts training.</summary>
    public sealed class PixelScheduleWindow : PixelSetupStep
    {
        public event Action Completed;
        readonly HashSet<int> days = new HashSet<int>();
        public int Minutes { get; private set; } = 25;
        public int[] Weekdays => days.OrderBy(d => d).ToArray();
        public string Error { get; set; }
        bool opened;
        string notice;
        static readonly string[] En = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, Es = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
        public static string Day(int index, string language) => language == "es" ? Es[index] : En[index];

        public void Initialize(Transform panel, Action backAction, Selectable privacy, Selectable language) => Shell(panel, backAction, privacy, language);
        public void Open(string language, int[] weekdays = null, int minutes = 0)
        {
            if (!opened && weekdays != null && weekdays.Length > 0) { days.Clear(); foreach (var d in weekdays) if (d >= 0 && d <= 6) days.Add(d); }
            if (!opened && LiveTrainingPlans.SessionDurations.Contains(minutes)) Minutes = minutes;
            opened = true; Error = null; SetLocale(language); Render(); ResetScroll();
        }
        public void ResetDraft() { days.Clear(); Minutes = 25; opened = false; }
        public bool Valid => days.Count >= 2 && days.Count <= 5;
        public override void GoBack() => back?.Invoke();
        public override void Advance() { if (gameObject.activeInHierarchy && Valid) Completed?.Invoke(); }
        public void Toggle(int day, bool on)
        {
            notice = null; Error = null;
            if (on && days.Count >= 5 && !days.Contains(day)) notice = L("Up to 5 days. Rest days matter too.", "Hasta 5 días. El descanso también cuenta.");
            else if (on) days.Add(day); else days.Remove(day);
            Render();
        }
        public override void Render()
        {
            if (list == null || binding) return;
            binding = true;
            try
            {
                Begin();
                title.text = L("YOUR TRAINING WEEK", "TU SEMANA"); progress.text = L("Pick 2 to 5 days", "Elige de 2 a 5 días");
                float y = 4;
                for (int i = 0; i < 7; i++) { int day = i; Chip("day-" + i, Day(i, Language), new Rect(i * 72, y, 66, 64), days.Contains(i), on => Toggle(day, on)); }
                y += 84;
                Label(L("Minutes per session", "Minutos por sesión"), y, 22, Muted); y += 38;
                var durations = LiveTrainingPlans.SessionDurations;
                for (int i = 0; i < durations.Length; i++)
                {
                    int value = durations[i];
                    Chip("minutes-" + value, value.ToString(CultureInfo.InvariantCulture), new Rect(i * 127, y, 121, 64), Minutes == value, on => { notice = null; Error = null; if (on) Minutes = value; Render(); }, 26);
                }
                y += 82;
                if (days.Count > 0) { Label(string.Join(" · ", Weekdays.Select(d => Day(d, Language))) + "  —  " + Minutes + " min", y, 23, null, TextAnchor.MiddleCenter, 40); y += 44; }
                Status.text = Error ?? notice ?? (days.Count == 1 ? L("Choose at least 2 days.", "Elige al menos 2 días.") : "");
                Continue.SetLabel(L("START TRAINING", "¡A ENTRENAR!")); Continue.interactable = Valid;
                Finish(y);
            }
            finally { binding = false; }
        }
    }
}
