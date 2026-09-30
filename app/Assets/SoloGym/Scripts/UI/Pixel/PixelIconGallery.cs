using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Component-only navigation fixture; no profile settings or player data are saved.</summary>
    public sealed class PixelIconGallery : MonoBehaviour
    {
        public PixelIconButton Back { get; private set; }
        public PixelIconButton Settings { get; private set; }
        public PixelIconButton Normal { get; private set; }
        public PixelIconButton Disabled { get; private set; }
        public PixelIconButton Large { get; private set; }
        public PixelPrimaryButton Locale { get; private set; }
        public PixelIconButton[] Buttons => new[] { Back, Settings, Normal, Disabled, Large };
        public bool Spanish { get; private set; }
        public bool SettingsOpen { get; private set; }
        public int BackVisits { get; private set; }
        public int SettingsVisits { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public AccessibilityHierarchy Hierarchy { get; private set; }
        AccessibilityHierarchy previousHierarchy;
        AccessibilityNode localeNode, contextNode;
        PixelContentPanel left, right;
        CanvasScaler scaler;
        RectTransform safeRoot, layout;
        Text title, subtitle, leftTitle, rightTitle, context, description;
        Text backCaption, settingsCaption, normalCaption, disabledCaption, largeCaption;
        Text normalState, disabledState, largeState;
        int oldWidth, oldHeight;
        float inset;
        Rect oldSafeArea;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out inset);
            inset = Mathf.Max(0, inset);
            var surface = new GameObject("Icon review canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            var canvas = surface.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", surface.transform);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>(); fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", surface.transform);
            layout = Rect("Icon examples", safeRoot); layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null) new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = Label(layout, "Fixture title", 30); title.color = new Color32(251, 224, 166, 255);
            subtitle = Label(layout, "Fixture instructions", 18);
            left = PixelContentPanel.Create(layout); right = PixelContentPanel.Create(layout);
            leftTitle = Label(left.Content, "Navigation heading", 24); rightTitle = Label(right.Content, "Examples heading", 24);
            context = Label(left.Content, "Navigation destination", 26);
            description = Label(left.Content, "Navigation explanation", 22);
            backCaption = Label(left.Content, "Back caption", 22); settingsCaption = Label(left.Content, "Settings caption", 22);
            settingsCaption.alignment = TextAnchor.UpperRight;
            normalCaption = Label(right.Content, "Normal caption", 24); disabledCaption = Label(right.Content, "Disabled caption", 24);
            largeCaption = Label(right.Content, "Large caption", 24);
            normalState = Label(right.Content, "Normal state", 18); disabledState = Label(right.Content, "Disabled state", 18);
            largeState = Label(right.Content, "Large state", 18);
            var back = Resources.LoadAll<Sprite>("UI/Pixel/IconBack")[0];
            var settings = Resources.LoadAll<Sprite>("UI/Pixel/IconSettings")[0];
            Back = PixelIconButton.Create(left.Content, back, "Back", GoBack);
            Settings = PixelIconButton.Create(left.Content, settings, "Settings", OpenSettings);
            Normal = PixelIconButton.Create(right.Content, back, "Return home", GoBack);
            Disabled = PixelIconButton.Create(right.Content, settings, "Settings unavailable"); Disabled.interactable = false;
            Large = PixelIconButton.Create(right.Content, settings, "Open home settings", OpenSettings);
            Large.Icon.rectTransform.sizeDelta = new Vector2(48, 48);
            Back.BindCaption(backCaption); Settings.BindCaption(settingsCaption); Normal.BindCaption(normalCaption);
            Disabled.BindCaption(disabledCaption); Large.BindCaption(largeCaption);
            Locale = PixelPrimaryButton.Create(left.Content, "", () => SetLocale(!Spanish)); Locale.SetFontSize(24);
            Back.SetTraversal(Locale, Settings); Settings.SetTraversal(Back, Normal);
            Normal.SetTraversal(Settings, Disabled); Disabled.SetTraversal(Normal, Large); Large.SetTraversal(Disabled, Locale);
            var tab = Locale.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous = Large; tab.Next = Back;
            Hierarchy = new AccessibilityHierarchy();
            foreach (var button in Buttons) button.BindAccessibility(Hierarchy);
            localeNode = Hierarchy.AddNode("Language"); localeNode.role = AccessibilityRole.Button;
            localeNode.frameGetter = () => Bounds((RectTransform)Locale.transform);
            localeNode.invoked += () => { if (!Locale.IsActive() || !Locale.IsInteractable()) return false; Locale.onClick.Invoke(); return true; };
            contextNode = Hierarchy.AddNode("Example"); contextNode.role = AccessibilityRole.StaticText;
            contextNode.frameGetter = () => Bounds(context.rectTransform);
            ResetReview(); Relayout();
            previousHierarchy = AssistiveSupport.activeHierarchy; AssistiveSupport.activeHierarchy = Hierarchy;
        }
        void OnDestroy()
        {
            if (AssistiveSupport.activeHierarchy == Hierarchy) AssistiveSupport.activeHierarchy = previousHierarchy;
        }
        public void GoBack() { ++BackVisits; SettingsOpen = false; ApplyText(); Back.Select(); }
        public void OpenSettings() { ++SettingsVisits; SettingsOpen = true; ApplyText(); Settings.Select(); }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        public void ResetReview()
        {
            BackVisits = SettingsVisits = 0; SettingsOpen = false;
            Disabled.interactable = false; ApplyText(); EventSystem.current.SetSelectedGameObject(null);
        }
        void ApplyText()
        {
            title.text = Spanish ? "SOLOGYM / BOTONES CON ICONO" : "SOLOGYM / ICON BUTTONS";
            subtitle.text = Spanish ? "Revisión sin conexión · Tab / Shift+Tab · Entrar para activar" : "Offline review · Tab / Shift+Tab · Enter to activate";
            leftTitle.text = Spanish ? "NAVEGACIÓN DEL REFUGIO" : "HOME NAVIGATION";
            rightTitle.text = Spanish ? "ESTADOS Y TAMAÑOS" : "STATES AND SIZES";
            Back.SetLabel(Spanish ? "Volver" : "Back"); Settings.SetLabel(Spanish ? "Ajustes" : "Settings");
            Normal.SetLabel(Spanish ? "Volver al refugio" : "Return to your home");
            Disabled.SetLabel(Spanish ? "Ajustes no disponibles" : "Settings unavailable");
            Large.SetLabel(Spanish ? "Abrir ajustes del refugio" : "Open your home settings");
            normalState.text = Spanish ? "NORMAL · 64 × 64" : "NORMAL · 64 × 64";
            disabledState.text = Spanish ? "DESACTIVADO · 64 × 64" : "DISABLED · 64 × 64";
            largeState.text = Spanish ? "GRANDE · 96 × 96" : "LARGE · 96 × 96";
            context.text = SettingsOpen ? (Spanish ? "AJUSTES DEL REFUGIO" : "HOME SETTINGS") : (Spanish ? "TU REFUGIO" : "YOUR HOME");
            description.text = SettingsOpen ? (Spanish ? "Vista de ejemplo. Usa Volver para regresar." : "Example view. Use Back to return.")
                : (Spanish ? "Abre los ajustes con el engranaje. Tu aventura empieza aquí." : "Open settings with the cog. Your adventure starts here.");
            Locale.SetLabel(Spanish ? "Idioma: ES / EN" : "Language: EN / ES");
            if (localeNode != null) localeNode.label = Locale.Label.text;
            if (contextNode != null) contextNode.label = context.text + ". " + description.text;
        }
        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture")
                || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) gameObject.AddComponent<PixelIconSmoke>().Run(this);
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
            Place(title.rectTransform, 0, 0, width, 36); Place(subtitle.rectTransform, 0, 42, width, 28);
            Place((RectTransform)left.transform, 0, 82, column, 432); Place((RectTransform)right.transform, column + 24, 82, column, 432);
            Place(leftTitle.rectTransform, 4, 0, inner - 8, 32); Place(rightTitle.rectTransform, 4, 0, inner - 8, 32);
            Place((RectTransform)Back.transform, 4, 52, 64, 64); Place((RectTransform)Settings.transform, inner - 68, 52, 64, 64);
            Place(backCaption.rectTransform, 4, 126, 180, 28); Place(settingsCaption.rectTransform, inner - 184, 126, 180, 28);
            Place(context.rectTransform, 4, 178, inner - 8, 34); Place(description.rectTransform, 4, 216, inner - 8, 58);
            Place((RectTransform)Locale.transform, 4, 284, inner - 8, 64);
            Place((RectTransform)Normal.transform, 4, 52, 64, 64); Place(normalState.rectTransform, 96, 48, inner - 100, 24);
            Place(normalCaption.rectTransform, 96, 76, inner - 100, 56);
            Place((RectTransform)Disabled.transform, 4, 148, 64, 64); Place(disabledState.rectTransform, 96, 144, inner - 100, 24);
            Place(disabledCaption.rectTransform, 96, 172, inner - 100, 56);
            Place((RectTransform)Large.transform, 4, 244, 96, 96); Place(largeState.rectTransform, 124, 244, inner - 128, 24);
            Place(largeCaption.rectTransform, 124, 274, inner - 128, 62);
            Canvas.ForceUpdateCanvases(); Hierarchy?.RefreshNodeFrames();
        }
        static Text Label(Transform parent, string name, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/PixelifySans");
            text.fontSize = size; text.color = new Color32(224, 219, 210, 255); text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false; text.raycastTarget = false; return text;
        }
        public static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return UnityEngine.Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
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
