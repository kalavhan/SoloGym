using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Offline component fixture, not an authentication screen.</summary>
    public sealed class PixelFieldGallery : MonoBehaviour
    {
        public PixelFormField Email { get; private set; }
        public PixelFormField Password { get; private set; }
        public PixelFormField Invalid { get; private set; }
        public PixelFormField Disabled { get; private set; }
        public PixelPrimaryButton Submit { get; private set; }
        public PixelPrimaryButton Locale { get; private set; }
        public bool Spanish { get; private set; }
        public int SuccessfulExamples { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public PixelFormField[] Fields => new[] { Email, Password, Invalid, Disabled };
        PixelContentPanel left, right;
        CanvasScaler scaler;
        RectTransform safeRoot, layout;
        Text title, subtitle;
        int oldWidth, oldHeight;
        float inset;
        Rect oldSafeArea;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out inset);
            inset = Mathf.Max(0, inset);
            var surface = new GameObject("Field review canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            var canvas = surface.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", surface.transform);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>(); fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", surface.transform);
            layout = Rect("Field examples", safeRoot); layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null) new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = Label("Fixture title", 30); title.color = new Color32(251, 224, 166, 255);
            subtitle = Label("Fixture instructions", 18);
            left = PixelContentPanel.Create(layout); right = PixelContentPanel.Create(layout);
            foreach (var panel in new[] { left, right })
            {
                var group = panel.Content.gameObject.AddComponent<VerticalLayoutGroup>();
                group.padding = new RectOffset(4, 4, 0, 4); group.spacing = 12;
                group.childControlWidth = group.childControlHeight = true;
                group.childForceExpandWidth = true; group.childForceExpandHeight = false;
            }
            Email = PixelFormField.Create(left.Content, PixelFormField.Kind.Email, "", ""); Email.name = "Editable email";
            Password = PixelFormField.Create(left.Content, PixelFormField.Kind.Password, "", ""); Password.name = "Editable password";
            Invalid = PixelFormField.Create(right.Content, PixelFormField.Kind.Email, "", ""); Invalid.name = "Invalid example";
            Disabled = PixelFormField.Create(right.Content, PixelFormField.Kind.Text, "", ""); Disabled.name = "Disabled example";
            Submit = PixelPrimaryButton.Create(left.Content, "", ValidateExample); Submit.SetFontSize(24);
            Locale = PixelPrimaryButton.Create(right.Content, "", () => SetLocale(!Spanish)); Locale.SetFontSize(24);
            Submit.name = "Validate example"; Locale.name = "Change locale";
            Email.SetTraversal(Locale, Password.Input);
            Password.SetTraversal(Email.Input, Submit);
            Invalid.SetTraversal(Submit, Locale);
            Disabled.SetTraversal(Invalid.Input, Locale);
            var submitTab = Submit.gameObject.AddComponent<PixelFieldTabNavigation>(); submitTab.Previous = Password.Visibility; submitTab.Next = Invalid.Input;
            var localeTab = Locale.gameObject.AddComponent<PixelFieldTabNavigation>(); localeTab.Previous = Invalid.Input; localeTab.Next = Email.Input;
            Email.Input.onSubmit.AddListener(_ => Password.Input.Select());
            Password.Input.onSubmit.AddListener(_ => ValidateExample());
            Email.Input.onValueChanged.AddListener(_ => Email.SetError(""));
            Password.Input.onValueChanged.AddListener(_ => Password.SetError(""));
            Invalid.Input.onValueChanged.AddListener(_ => Invalid.SetError(""));
            ApplyText(); ResetReview(); Relayout();
        }

        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture")
                || PixelButtonGallery.HasArgument("-sologym-keyboard-probe"))
                gameObject.AddComponent<PixelFieldSmoke>().Run(this);
        }
        void Update()
        {
            if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea) Relayout();
        }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        void ApplyText()
        {
            title.text = Spanish ? "SOLOGYM / CAMPOS DE FORMULARIO" : "SOLOGYM / FORM FIELDS";
            subtitle.text = Spanish ? "Revisión sin conexión · usa datos inventados · Tab / Shift+Tab" : "Offline review · use fictional data · Tab / Shift+Tab";
            string show = Spanish ? "Ver" : "Show", hide = Spanish ? "Ocultar" : "Hide";
            Email.SetLocalizedText(Spanish ? "Correo electrónico" : "Email address", Spanish ? "tu@ejemplo.com" : "you@example.com",
                Spanish ? "Correo de ejemplo, sin conexión." : "Example address, no connection.", show, hide);
            Password.SetLocalizedText(Spanish ? "Contraseña" : "Password", Spanish ? "Contraseña" : "Password",
                Spanish ? "No uses tu contraseña real." : "Do not use your real password.", show, hide);
            Invalid.SetLocalizedText(Spanish ? "Ejemplo con error" : "Validation example", "", "", show, hide);
            Invalid.SetError(Spanish ? "Este correo está incompleto. Agrega @ y un dominio como ejemplo.com." : "Enter a complete address, including @ and a domain such as example.com.");
            Disabled.SetLocalizedText(Spanish ? "Campo desactivado" : "Disabled field", "", "", show, hide);
            Submit.SetLabel(Spanish ? "Probar formulario" : "Test form");
            Locale.SetLabel(Spanish ? "Idioma: ES" : "Language: EN");
        }
        public void ValidateExample()
        {
            // This demonstrates caller-owned feedback, not production address/password policy.
            bool emailReady = !string.IsNullOrWhiteSpace(Email.Input.text) && Email.Input.text.IndexOf('@') > 0;
            bool passwordReady = Password.Input.text.Length != 0;
            Email.SetError(emailReady ? "" : Spanish ? "Escribe un correo con @." : "Enter an address containing @.");
            Password.SetError(passwordReady ? "" : Spanish ? "Escribe una contraseña." : "Enter a password.");
            if (!emailReady) Email.Input.Select();
            else if (!passwordReady) Password.Input.Select();
            else
            {
                ++SuccessfulExamples;
                Submit.SetLabel(Spanish ? "Prueba completada" : "Example completed");
            }
        }
        public void ResetReview()
        {
            SuccessfulExamples = 0;
            Email.SetValueWithoutNotify(""); Email.SetError("");
            Password.SetValueWithoutNotify("Demo-only9!"); Password.SetError(""); Password.HidePassword();
            Invalid.SetValueWithoutNotify("correo-sin-arroba");
            Disabled.SetValueWithoutNotify("explorador@example.com"); Disabled.SetInteractable(false);
            ApplyText();
            EventSystem.current.SetSelectedGameObject(null);
        }

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
            float width = Mathf.Min(1200, EffectiveSafeArea.width / scaler.scaleFactor - 32), column = (width - 24) / 2;
            layout.sizeDelta = new Vector2(width, 544);
            Place(title.rectTransform, 0, 0, width, 36); Place(subtitle.rectTransform, 0, 42, width, 28);
            Place((RectTransform)left.transform, 0, 82, column, 432);
            Place((RectTransform)right.transform, column + 24, 82, column, 432);
            Canvas.ForceUpdateCanvases();
        }
        Text Label(string name, int size)
        {
            var text = Rect(name, layout).gameObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/PixelifySans");
            text.fontSize = size; text.color = new Color32(224, 219, 210, 255); text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false; return text;
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
