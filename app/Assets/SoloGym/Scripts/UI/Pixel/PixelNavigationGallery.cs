using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Fictional route/audience fixture; no real profile, fasting session or workout is changed.</summary>
    public sealed class PixelNavigationGallery : MonoBehaviour
    {
        static readonly string[] StandardRoutes = { "home", "workouts", "dungeon" };
        static readonly string[] AdultRoutes = { "home", "workouts", "dungeon", "fasting" };
        public PixelNavigationBar Navigation { get; private set; }
        public PixelChoiceControl Profile { get; private set; }
        public PixelSecondaryAction Locale { get; private set; }
        public string ContentId { get; private set; }
        public bool Spanish { get; private set; }
        public bool RejectRequests { get; set; }
        public int NavigationRequests { get; private set; }
        public bool ReviewFastingEnabled => Profile.Value == "adult-on";
        public Rect EffectiveSafeArea { get; private set; }
        public AccessibilityHierarchy Hierarchy { get; private set; }
        AccessibilityHierarchy previousHierarchy;
        AccessibilityNode contentNode, localeNode;
        CanvasScaler scaler;
        RectTransform safeRoot, layout;
        PixelContentPanel panel;
        PixelCharacterViewport character;
        Text title, subtitle, heading, detail, audienceTitle, eligibility;
        int oldWidth, oldHeight;
        float inset;
        Rect oldSafeArea;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out inset); inset = Mathf.Max(0, inset);
            var canvasObject = new GameObject("Navigation review canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false); var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", canvasObject.transform); backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>(); fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", canvasObject.transform); layout = Rect("Navigation examples", safeRoot); layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null) new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = Label(layout, "Review title", 30); title.color = new Color32(251, 224, 166, 255); subtitle = Label(layout, "Review subtitle", 18);
            panel = PixelContentPanel.Create(layout); heading = Label(panel.Content, "Current destination", 28); heading.color = title.color;
            detail = Label(panel.Content, "Route description", 24); eligibility = Label(panel.Content, "Eligibility explanation", 20);
            character = PixelCharacterViewport.Create(panel.Content);
            audienceTitle = Label(layout, "Example profile heading", 22);
            Profile = PixelChoiceControl.Create(layout, new[] { "unknown", "teen", "adult-off", "adult-on" },
                new[] { "Age unknown", "Age 15–17", "Adult", "Adult + fasting" }, "adult-off");
            Navigation = PixelNavigationBar.Create(layout, AdultRoutes, new[] { "Home", "Workouts", "Dungeon", "Fasting" }, StandardRoutes, "home");
            Profile.onValueChanged.AddListener(_ => ApplyEligibility()); Navigation.onNavigate.AddListener(RequestRoute);
            Locale = PixelSecondaryAction.Create(layout, PixelSecondaryAction.Appearance.Text, "", () => SetLocale(!Spanish));
            Profile.SetTraversal(Locale, Navigation.Tab("home")); Navigation.SetTraversal(Profile.Option("adult-on"), Locale);
            Locale.SetTraversal(Navigation.Tab("fasting"), Profile.Option("unknown"));
            Hierarchy = new AccessibilityHierarchy(); Navigation.BindAccessibility(Hierarchy, "Main navigation"); Profile.BindAccessibility(Hierarchy, "Example profile");
            contentNode = Hierarchy.AddNode("Current destination"); contentNode.role = AccessibilityRole.StaticText;
            contentNode.frameGetter = () => PixelChoiceOption.ScreenBounds(panel.Content);
            localeNode = Hierarchy.AddNode("Language"); localeNode.role = AccessibilityRole.Button; localeNode.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)Locale.transform);
            localeNode.invoked += () => { if (!Locale.IsActive() || !Locale.IsInteractable()) return false; SetLocale(!Spanish); return true; };
            ResetReview(); Relayout(); previousHierarchy = AssistiveSupport.activeHierarchy; AssistiveSupport.activeHierarchy = Hierarchy;
        }
        void OnDestroy() { if (AssistiveSupport.activeHierarchy == Hierarchy) AssistiveSupport.activeHierarchy = previousHierarchy; }
        void RequestRoute(string id)
        {
            ++NavigationRequests;
            if (RejectRequests || (id == "fasting" && !ReviewFastingEnabled)) return;
            ContentId = id; Navigation.SetCurrentWithoutNotify(id); RefreshContent();
        }
        public void SetReviewProfile(string id) { Profile.SetValueWithoutNotify(id); ApplyEligibility(); }
        void ApplyEligibility()
        {
            // Fictional fixtures only: production screens must use trusted eligibility and adult opt-in.
            if (ContentId == "fasting" && !ReviewFastingEnabled) ContentId = "home";
            Navigation.BindVisibleRoutes(ReviewFastingEnabled ? AdultRoutes : StandardRoutes, ContentId ?? "home");
            Locale.SetTraversal(Navigation.Tab(ReviewFastingEnabled ? "fasting" : "dungeon"), Profile.Option("unknown"));
            RefreshContent();
        }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        public void ResetReview()
        {
            ContentId = "home"; RejectRequests = false; NavigationRequests = 0; Navigation.SetInteractable(true); Navigation.SetTabInteractable("dungeon", true);
            Profile.SetValueWithoutNotify("adult-off"); ApplyText(); ApplyEligibility(); EventSystem.current.SetSelectedGameObject(null);
        }
        void ApplyText()
        {
            title.text = Spanish ? "SOLOGYM / NAVEGACIÓN" : "SOLOGYM / NAVIGATION";
            subtitle.text = Spanish ? "Vista de prueba · Tab / flechas: foco · Entrar: abrir" : "Review preview · Tab / arrows: focus · Enter: open";
            audienceTitle.text = Spanish ? "PERFIL DE EJEMPLO" : "EXAMPLE PROFILE";
            string[] profiles = Spanish ? new[] { "Sin edad", "15–17 años", "Adulto", "Adulto + ayuno" } : new[] { "Age unknown", "Age 15–17", "Adult", "Adult + fasting" };
            for (int i = 0; i < profiles.Length; ++i) Profile.SetLabel(Profile.Options[i].Id, profiles[i]);
            string[] routes = Spanish ? new[] { "Hogar", "Rutinas", "Mazmorra", "Ayuno" } : new[] { "Home", "Workouts", "Dungeon", "Fasting" };
            for (int i = 0; i < routes.Length; ++i) Navigation.SetLabel(AdultRoutes[i], routes[i]);
            Profile.SetAccessibleGroupLabel(audienceTitle.text); Navigation.SetAccessibleLabel(Spanish ? "Navegación principal" : "Main navigation");
            Locale.SetLabel(Spanish ? "ES / EN" : "EN / ES"); localeNode.label = Spanish ? "Cambiar idioma: español o inglés" : "Change language: English or Spanish";
            RefreshContent();
        }
        void RefreshContent()
        {
            if (string.IsNullOrEmpty(ContentId)) return;
            heading.text = Navigation.Tab(ContentId).Label.text.ToUpperInvariant();
            string copy;
            switch (ContentId)
            {
                case "workouts": copy = Spanish ? "Tus rutinas, historial y ajustes." : "Your routines, history and adjustments."; break;
                case "dungeon": copy = Spanish ? "Tu rutina se convierte en una batalla." : "Your routine becomes a dungeon battle."; break;
                case "fasting": copy = Spanish ? "Registro opcional y privado. Sin recompensas." : "Optional private tracking. No rewards."; break;
                default: copy = Spanish ? "Tu personaje y gimnasio personal." : "Your character and personal gym."; break;
            }
            detail.text = copy;
            eligibility.text = ReviewFastingEnabled ? (Spanish ? "Ejemplo: adulto con ayuno activado." : "Example: adult with fasting enabled.")
                : Profile.Value == "adult-off" ? (Spanish ? "Ejemplo: adulto. Ayuno apagado por defecto." : "Example: adult. Fasting is off by default.")
                : Profile.Value == "teen" ? (Spanish ? "Ejemplo: 15–17 años. Ayuno no disponible." : "Example: age 15–17. Fasting unavailable.")
                : (Spanish ? "Ejemplo: edad sin confirmar. Ayuno oculto." : "Example: unconfirmed age. Fasting hidden.");
            character.Show("male-medium", Spanish ? "Bárbaro de complexión media" : "Medium-build Barbarian", Spanish ? "Personaje no disponible" : "Character unavailable");
            if (contentNode != null) contentNode.label = heading.text + ". " + detail.text + " " + eligibility.text;
        }
        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe"))
                gameObject.AddComponent<PixelNavigationSmoke>().Run(this);
        }
        void Update() { if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea) Relayout(); }
        void Relayout()
        {
            oldWidth = Screen.width; oldHeight = Screen.height; oldSafeArea = Screen.safeArea;
            scaler.scaleFactor = Mathf.Max(.75f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f)); var actual = Screen.safeArea;
            EffectiveSafeArea = UnityEngine.Rect.MinMaxRect(Mathf.Max(actual.xMin, inset), Mathf.Max(actual.yMin, inset),
                Mathf.Max(inset + 1, Mathf.Min(actual.xMax, Screen.width - inset)), Mathf.Max(inset + 1, Mathf.Min(actual.yMax, Screen.height - inset)));
            safeRoot.anchorMin = new Vector2(EffectiveSafeArea.xMin / Screen.width, EffectiveSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(EffectiveSafeArea.xMax / Screen.width, EffectiveSafeArea.yMax / Screen.height); safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            float width = Mathf.Min(1200, EffectiveSafeArea.width / scaler.scaleFactor - 32);
            layout.sizeDelta = new Vector2(width, 544);
            Place(title.rectTransform, 0, 0, width - 200, 36); Place(subtitle.rectTransform, 0, 42, width - 200, 28);
            Place((RectTransform)Locale.transform, width - 180, 0, 180, 64);
            Place((RectTransform)panel.transform, 0, 82, width, 250);
            Place((RectTransform)character.transform, 0, 0, 150, 170);
            Place(heading.rectTransform, 182, 6, width - 282, 38); Place(detail.rectTransform, 182, 52, width - 282, 64);
            Place(eligibility.rectTransform, 182, 126, width - 282, 44);
            Place(audienceTitle.rectTransform, 4, 350, width - 8, 28); Place((RectTransform)Profile.transform, 4, 380, width - 8, 64);
            Place((RectTransform)Navigation.transform, 4, 478, width - 8, 64);
            Profile.RefreshLayout(); Navigation.RefreshLayout(); Canvas.ForceUpdateCanvases(); character.RefreshLayout(); Hierarchy.RefreshNodeFrames();
        }
        static Text Label(Transform parent, string name, int size)
        { var t = Rect(name, parent).gameObject.AddComponent<Text>(); t.font = Resources.Load<Font>("Fonts/PixelifySans"); t.fontSize = size; t.color = new Color32(224, 219, 210, 255); t.alignment = TextAnchor.UpperLeft; t.raycastTarget = false; t.supportRichText = false; return t; }
        static RectTransform Rect(string name, Transform parent)
        { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); return r; }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
    }
}
