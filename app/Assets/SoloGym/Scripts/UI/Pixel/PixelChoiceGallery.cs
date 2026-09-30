using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Choice presentation in fictional battle phases; no prescription or rewards are generated.</summary>
    public sealed class PixelChoiceGallery : MonoBehaviour
    {
        public enum Phase { Warmup, Exercise, Rest, Pause }
        public PixelChoiceControl Difficulty { get; private set; }
        public PixelChoiceControl Appearance { get; private set; }
        public PixelPrimaryButton NextPhase { get; private set; }
        public PixelSecondaryAction BodyGate { get; private set; }
        public PixelSecondaryAction Locale { get; private set; }
        public bool Spanish { get; private set; }
        public Phase CurrentPhase { get; private set; }
        public int DifficultyChanges { get; private set; }
        public int AppearanceChanges { get; private set; }
        public int FictionalLoggedSets { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public AccessibilityHierarchy Hierarchy { get; private set; }
        AccessibilityHierarchy previousHierarchy;
        AccessibilityNode phaseNode, nextNode, gateNode, localeNode;
        CanvasScaler scaler;
        RectTransform safeRoot, layout;
        PixelContentPanel left, right;
        Text title, subtitle, leftTitle, rightTitle, phase, bodyHelp, difficultyStatus, bodyStatus;
        int oldWidth, oldHeight;
        float inset;
        Rect oldSafeArea;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out inset); inset = Mathf.Max(0, inset);
            var surface = new GameObject("Choice review canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            var canvas = surface.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", surface.transform);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>(); fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", surface.transform); layout = Rect("Choice examples", safeRoot);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null) new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = Label(layout, "Fixture title", 30); title.color = new Color32(251, 224, 166, 255);
            subtitle = Label(layout, "Fixture instructions", 18);
            left = PixelContentPanel.Create(layout); right = PixelContentPanel.Create(layout);
            leftTitle = Label(left.Content, "Difficulty heading", 24); rightTitle = Label(right.Content, "Appearance heading", 24);
            phase = Label(left.Content, "Current battle phase", 20); bodyHelp = Label(right.Content, "Appearance explanation", 20);
            difficultyStatus = Label(left.Content, "Selected difficulty", 20); bodyStatus = Label(right.Content, "Selected appearance", 20);
            Difficulty = PixelChoiceControl.Create(left.Content, new[] { "light", "medium", "hard" }, new[] { "Easy", "Medium", "Hard" }, "medium");
            Appearance = PixelChoiceControl.Create(right.Content, new[] { "skinny", "medium", "fat", "muscular" }, new[] { "Skinny", "Medium", "Fat", "Muscular" }, "medium");
            Difficulty.onValueChanged.AddListener(_ => { ++DifficultyChanges; RefreshStatus(); });
            Appearance.onValueChanged.AddListener(_ => { ++AppearanceChanges; RefreshStatus(); });
            NextPhase = PixelPrimaryButton.Create(left.Content, "", AdvancePhase); NextPhase.SetFontSize(24);
            BodyGate = PixelSecondaryAction.Create(right.Content, PixelSecondaryAction.Appearance.Framed, "", () =>
            { Appearance.SetInteractable(!Appearance.Interactable); RefreshStatus(); });
            Locale = PixelSecondaryAction.Create(layout, PixelSecondaryAction.Appearance.Text, "", () => SetLocale(!Spanish));
            Difficulty.SetTraversal(Locale, NextPhase); Appearance.SetTraversal(NextPhase, BodyGate);
            var tab = NextPhase.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous = Difficulty.Option("hard"); tab.Next = Appearance.Option("skinny");
            BodyGate.SetTraversal(Appearance.Option("muscular"), Locale); Locale.SetTraversal(BodyGate, Difficulty.Option("light"));
            Hierarchy = new AccessibilityHierarchy(); Difficulty.BindAccessibility(Hierarchy, "Training difficulty"); Appearance.BindAccessibility(Hierarchy, "Body appearance");
            phaseNode = Hierarchy.AddNode("Battle phase"); phaseNode.role = AccessibilityRole.StaticText;
            phaseNode.frameGetter = () => PixelChoiceOption.ScreenBounds(phase.rectTransform);
            nextNode = ActionNode(NextPhase, "Next phase"); gateNode = ActionNode(BodyGate, "Example availability"); localeNode = ActionNode(Locale, "Language");
            ResetReview(); Relayout(); previousHierarchy = AssistiveSupport.activeHierarchy; AssistiveSupport.activeHierarchy = Hierarchy;
        }
        AccessibilityNode ActionNode(Button button, string text)
        {
            var node = Hierarchy.AddNode(text); node.role = AccessibilityRole.Button;
            node.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)button.transform);
            node.invoked += () => { if (!button.IsActive() || !button.IsInteractable()) return false; button.onClick.Invoke(); return true; }; return node;
        }
        void OnDestroy() { if (AssistiveSupport.activeHierarchy == Hierarchy) AssistiveSupport.activeHierarchy = previousHierarchy; }
        public void AdvancePhase() { CurrentPhase = (Phase)(((int)CurrentPhase + 1) % 4); RefreshStatus(); }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        public void ResetReview()
        {
            CurrentPhase = Phase.Exercise; FictionalLoggedSets = 2; DifficultyChanges = AppearanceChanges = 0;
            Difficulty.SetInteractable(true); Difficulty.SetOptionInteractable("hard", true); Difficulty.SetValueWithoutNotify("medium");
            Appearance.SetInteractable(true); Appearance.SetValueWithoutNotify("medium");
            ApplyText(); EventSystem.current.SetSelectedGameObject(null);
        }
        void ApplyText()
        {
            title.text = Spanish ? "SOLOGYM / OPCIONES" : "SOLOGYM / CHOICE CONTROLS";
            subtitle.text = Spanish ? "Ejemplo sin conexión · Tab / flechas: foco · Entrar: elegir" : "Offline example · Tab / arrows: focus · Enter: choose";
            leftTitle.text = Spanish ? "DIFICULTAD EN LA BATALLA" : "BATTLE DIFFICULTY";
            rightTitle.text = Spanish ? "ASPECTO DEL PERSONAJE" : "CHARACTER APPEARANCE";
            bodyHelp.text = Spanish ? "Solo apariencia. No cambia tu entrenamiento." : "Appearance only. Your training stays the same.";
            string[] difficulty = Spanish ? new[] { "Fácil", "Medio", "Difícil" } : new[] { "Easy", "Medium", "Hard" };
            string[] bodies = Spanish ? new[] { "Delgado", "Medio", "Corpulento", "Musculoso" } : new[] { "Skinny", "Medium", "Fat", "Muscular" };
            for (int i = 0; i < difficulty.Length; ++i) Difficulty.SetLabel(Difficulty.Options[i].Id, difficulty[i]);
            for (int i = 0; i < bodies.Length; ++i) Appearance.SetLabel(Appearance.Options[i].Id, bodies[i]);
            Difficulty.SetAccessibleGroupLabel(leftTitle.text); Appearance.SetAccessibleGroupLabel(rightTitle.text);
            NextPhase.SetLabel(Spanish ? "Siguiente fase de ejemplo" : "Next example phase");
            Locale.SetLabel(Spanish ? "ES / EN" : "EN / ES");
            RefreshStatus();
        }
        void RefreshStatus()
        {
            string[] phases = Spanish ? new[] { "Calentamiento", "Serie en curso", "Descanso", "Pausa" } : new[] { "Warm-up", "Exercise", "Rest", "Paused" };
            phase.text = phases[(int)CurrentPhase] + (Spanish ? " · dificultad editable\n" : " · difficulty editable\n")
                + FictionalLoggedSets + (Spanish ? " series de ejemplo registradas" : " example sets recorded");
            difficultyStatus.text = (Spanish ? "Dificultad actual: " : "Current difficulty: ") + Difficulty.Option(Difficulty.Value).Label.text;
            bodyStatus.text = Appearance.Interactable ? (Spanish ? "Elige tu aspecto." : "Choose your appearance.") : (Spanish ? "Ejemplo desactivado; elección conservada." : "Disabled example; choice preserved.");
            BodyGate.SetLabel(Appearance.Interactable ? (Spanish ? "Desactivar ejemplo" : "Disable example") : (Spanish ? "Activar ejemplo" : "Enable example"));
            if (phaseNode != null) phaseNode.label = phase.text;
            if (nextNode != null) nextNode.label = NextPhase.Label.text;
            if (gateNode != null) gateNode.label = BodyGate.Label.text;
            if (localeNode != null) localeNode.label = Spanish ? "Cambiar idioma: español o inglés" : "Change language: English or Spanish";
        }
        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture")
                || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) gameObject.AddComponent<PixelChoiceSmoke>().Run(this);
        }
        void Update() { if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea) Relayout(); }
        void Relayout()
        {
            oldWidth = Screen.width; oldHeight = Screen.height; oldSafeArea = Screen.safeArea;
            scaler.scaleFactor = Mathf.Max(.75f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            var actual = Screen.safeArea;
            EffectiveSafeArea = UnityEngine.Rect.MinMaxRect(Mathf.Max(actual.xMin, inset), Mathf.Max(actual.yMin, inset),
                Mathf.Max(inset + 1, Mathf.Min(actual.xMax, Screen.width - inset)), Mathf.Max(inset + 1, Mathf.Min(actual.yMax, Screen.height - inset)));
            safeRoot.anchorMin = new Vector2(EffectiveSafeArea.xMin / Screen.width, EffectiveSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(EffectiveSafeArea.xMax / Screen.width, EffectiveSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            float width = Mathf.Min(1200, EffectiveSafeArea.width / scaler.scaleFactor - 32), column = (width - 24) / 2, inner = column - 80;
            layout.sizeDelta = new Vector2(width, 544);
            Place(title.rectTransform, 0, 0, width - 280, 36); Place(subtitle.rectTransform, 0, 42, width - 260, 28);
            Place((RectTransform)Locale.transform, width - 252, 0, 252, 64);
            Place((RectTransform)left.transform, 0, 82, column, 432); Place((RectTransform)right.transform, column + 24, 82, column, 432);
            Place(leftTitle.rectTransform, 4, 0, inner - 8, 32); Place(rightTitle.rectTransform, 4, 0, inner - 8, 32);
            Place(phase.rectTransform, 4, 40, inner - 8, 52); Place(bodyHelp.rectTransform, 4, 40, inner - 8, 52);
            Place((RectTransform)Difficulty.transform, 4, 102, inner - 8, 140); Place((RectTransform)Appearance.transform, 4, 102, inner - 8, 140);
            Difficulty.RefreshLayout(); Appearance.RefreshLayout();
            Place(difficultyStatus.rectTransform, 4, 252, inner - 8, 28); Place(bodyStatus.rectTransform, 4, 252, inner - 8, 28);
            Place((RectTransform)NextPhase.transform, 4, 284, inner - 8, 64); Place((RectTransform)BodyGate.transform, 4, 284, inner - 8, 64);
            Canvas.ForceUpdateCanvases(); Hierarchy?.RefreshNodeFrames();
        }
        static Text Label(Transform parent, string name, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/PixelifySans"); text.fontSize = size;
            text.color = new Color32(224, 219, 210, 255); text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false; text.supportRichText = false; return text;
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
