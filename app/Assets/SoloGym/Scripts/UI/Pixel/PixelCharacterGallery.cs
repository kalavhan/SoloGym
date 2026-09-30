using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Offline character selection plus a compact reuse sample. No account or training writes.</summary>
    public sealed class PixelCharacterGallery : MonoBehaviour
    {
        public PixelCharacterViewport MainView { get; private set; }
        public PixelCharacterViewport CompactView { get; private set; }
        public PixelChoiceControl Presentation { get; private set; }
        public PixelChoiceControl Body { get; private set; }
        public PixelSecondaryAction Locale { get; private set; }
        public bool Spanish { get; private set; }
        public int SelectionChanges { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public AccessibilityHierarchy Hierarchy { get; private set; }
        public string CharacterId => Presentation.Value + "-" + Body.Value;
        AccessibilityHierarchy previousHierarchy;
        AccessibilityNode localeNode;
        CanvasScaler scaler;
        RectTransform safeRoot, layout;
        PixelContentPanel stage, controls;
        Text title, subtitle, characterName, presentationTitle, bodyTitle, compactTitle, help;
        int oldWidth, oldHeight;
        Rect oldSafeArea;
        float inset;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out inset); inset = Mathf.Max(0, inset);
            var surface = new GameObject("Character review canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            var canvas = surface.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", surface.transform); backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>(); fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", surface.transform); layout = Rect("Character examples", safeRoot);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null) new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = Label(layout, "Review title", 30); title.color = new Color32(251, 224, 166, 255);
            subtitle = Label(layout, "Review subtitle", 18);
            stage = PixelContentPanel.Create(layout); controls = PixelContentPanel.Create(layout);
            characterName = Label(stage.Content, "Character name", 24); characterName.alignment = TextAnchor.MiddleCenter;
            MainView = PixelCharacterViewport.Create(stage.Content); CompactView = PixelCharacterViewport.Create(controls.Content);
            presentationTitle = Label(controls.Content, "Presentation heading", 22); bodyTitle = Label(controls.Content, "Body heading", 22);
            compactTitle = Label(controls.Content, "Compact heading", 22); help = Label(controls.Content, "Cosmetic explanation", 20);
            Presentation = PixelChoiceControl.Create(controls.Content, new[] { "female", "male" }, new[] { "Female", "Male" }, "female");
            Body = PixelChoiceControl.Create(controls.Content, new[] { "skinny", "medium", "fat", "muscular" }, new[] { "Skinny", "Medium", "Fat", "Muscular" }, "medium");
            Presentation.onValueChanged.AddListener(_ => { ++SelectionChanges; RefreshCharacter(); });
            Body.onValueChanged.AddListener(_ => { ++SelectionChanges; RefreshCharacter(); });
            Locale = PixelSecondaryAction.Create(layout, PixelSecondaryAction.Appearance.Text, "", () => SetLocale(!Spanish));
            Presentation.SetTraversal(Locale, Body.Option("skinny")); Body.SetTraversal(Presentation.Option("male"), Locale);
            Locale.SetTraversal(Body.Option("muscular"), Presentation.Option("female"));
            Hierarchy = new AccessibilityHierarchy(); Presentation.BindAccessibility(Hierarchy, "Presentation"); Body.BindAccessibility(Hierarchy, "Body appearance");
            MainView.BindAccessibility(Hierarchy);
            // The second view duplicates the same character; expose only one image description.
            localeNode = Hierarchy.AddNode("Language"); localeNode.role = AccessibilityRole.Button;
            localeNode.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)Locale.transform);
            localeNode.invoked += () => { if (!Locale.IsActive() || !Locale.IsInteractable()) return false; SetLocale(!Spanish); return true; };
            ResetReview(); Relayout(); previousHierarchy = AssistiveSupport.activeHierarchy; AssistiveSupport.activeHierarchy = Hierarchy;
        }
        void OnDestroy() { if (AssistiveSupport.activeHierarchy == Hierarchy) AssistiveSupport.activeHierarchy = previousHierarchy; }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        public void ResetReview()
        {
            string id = PixelButtonGallery.Argument("-sologym-character", "female-medium");
            if (!PixelCharacterCatalog.TryFind(id, out var entry)) PixelCharacterCatalog.TryFind("female-medium", out entry);
            Presentation.SetValueWithoutNotify(entry.presentation); Body.SetValueWithoutNotify(entry.body);
            SelectionChanges = 0; ApplyText(); EventSystem.current.SetSelectedGameObject(null);
        }
        void ApplyText()
        {
            title.text = Spanish ? "SOLOGYM / PERSONAJE" : "SOLOGYM / CHARACTER";
            subtitle.text = Spanish ? "Vista de prueba · Tab / flechas: foco · Entrar: elegir" : "Review preview · Tab / arrows: focus · Enter: choose";
            presentationTitle.text = Spanish ? "PERSONAJE" : "CHARACTER"; bodyTitle.text = Spanish ? "COMPLEXIÓN" : "BODY APPEARANCE";
            compactTitle.text = Spanish ? "VISTA COMPACTA" : "COMPACT VIEW";
            help.text = Spanish ? "Aspecto libre.\nMismo entrenamiento." : "Your look.\nSame training.";
            Presentation.SetLabel("female", Spanish ? "Mujer" : "Female"); Presentation.SetLabel("male", Spanish ? "Hombre" : "Male");
            string[] names = Spanish ? new[] { "Delgado", "Medio", "Corpulento", "Musculoso" } : new[] { "Skinny", "Medium", "Fat", "Muscular" };
            for (int i = 0; i < names.Length; ++i) Body.SetLabel(Body.Options[i].Id, names[i]);
            Presentation.SetAccessibleGroupLabel(presentationTitle.text); Body.SetAccessibleGroupLabel(bodyTitle.text);
            Locale.SetLabel(Spanish ? "ES / EN" : "EN / ES"); localeNode.label = Spanish ? "Cambiar idioma: español o inglés" : "Change language: English or Spanish";
            RefreshCharacter();
        }
        public void RefreshCharacter()
        {
            string className = Spanish ? (Presentation.Value == "female" ? "BÁRBARA" : "BÁRBARO") : "BARBARIAN";
            characterName.text = className + " · " + Body.Option(Body.Value).Label.text;
            string description = className + ", " + Presentation.Option(Presentation.Value).Label.text + ", " + Body.Option(Body.Value).Label.text;
            string fallback = Spanish ? "Personaje no disponible" : "Character unavailable";
            MainView.Show(CharacterId, description, fallback); CompactView.Show(CharacterId, description, fallback);
        }
        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture")
                || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) gameObject.AddComponent<PixelCharacterSmoke>().Run(this);
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
            safeRoot.anchorMax = new Vector2(EffectiveSafeArea.xMax / Screen.width, EffectiveSafeArea.yMax / Screen.height); safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            float width = Mathf.Min(1200, EffectiveSafeArea.width / scaler.scaleFactor - 32);
            float right = Mathf.Max(484, width * .48f), left = width - right - 24, inner = right - 80;
            layout.sizeDelta = new Vector2(width, 544);
            Place(title.rectTransform, 0, 0, width - 200, 36); Place(subtitle.rectTransform, 0, 42, width - 200, 28);
            Place((RectTransform)Locale.transform, width - 180, 0, 180, 64);
            Place((RectTransform)stage.transform, 0, 82, left, 462); Place((RectTransform)controls.transform, left + 24, 82, right, 462);
            Place(characterName.rectTransform, 0, 0, left - 80, 32);
            Place((RectTransform)MainView.transform, 0, 38, left - 80, 344);
            Place(presentationTitle.rectTransform, 0, 0, inner, 28); Place((RectTransform)Presentation.transform, 0, 30, inner, 64);
            Place(bodyTitle.rectTransform, 0, 108, inner, 28); Place((RectTransform)Body.transform, 0, 138, inner, 140);
            Presentation.RefreshLayout(); Body.RefreshLayout();
            Place(compactTitle.rectTransform, 0, 294, inner - 122, 28); Place(help.rectTransform, 0, 324, inner - 126, 58);
            Place((RectTransform)CompactView.transform, inner - 110, 286, 110, 96);
            help.fontSize = 18;
            Canvas.ForceUpdateCanvases(); MainView.RefreshLayout(); CompactView.RefreshLayout(); Hierarchy.RefreshNodeFrames();
        }
        static Text Label(Transform parent, string name, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/PixelifySans"); text.fontSize = size;
            text.color = new Color32(224, 219, 210, 255); text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false; text.supportRichText = false; return text;
        }
        static RectTransform Rect(string name, Transform parent) { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
    }
}
