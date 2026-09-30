using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Offline account-navigation example plus reusable action states.</summary>
    public sealed class PixelActionGallery : MonoBehaviour
    {
        public PixelSecondaryAction Back { get; private set; }
        public PixelSecondaryAction Recovery { get; private set; }
        public PixelSecondaryAction Normal { get; private set; }
        public PixelSecondaryAction Disabled { get; private set; }
        public PixelSecondaryAction Pending { get; private set; }
        public PixelSecondaryAction LongCaption { get; private set; }
        public PixelFormField Email { get; private set; }
        public PixelPrimaryButton Primary { get; private set; }
        public bool Spanish { get; private set; }
        public bool Recovering { get; private set; }
        public int RecoveryVisits { get; private set; }
        public int BackVisits { get; private set; }
        public int NormalInvocations { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public PixelSecondaryAction[] Actions => new[] { Back, Recovery, Normal, Disabled, Pending, LongCaption };
        PixelContentPanel left, right;
        Text title, subtitle, heading, helper, states;
        RectTransform safeRoot, layout;
        CanvasScaler scaler;
        int oldWidth, oldHeight;
        Rect oldSafeArea;
        float inset;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out inset);
            inset = Mathf.Max(0, inset);
            var surface = new GameObject("Action review canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            var canvas = surface.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", surface.transform);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>(); fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", surface.transform);
            layout = Rect("Secondary action examples", safeRoot); layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null) new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = Label(layout, "Fixture title", 30); title.color = new Color32(251, 224, 166, 255);
            subtitle = Label(layout, "Offline instructions", 18);
            left = PixelContentPanel.Create(layout); right = PixelContentPanel.Create(layout);
            heading = Label(left.Content, "Example heading", 26);
            helper = Label(left.Content, "Example explanation", 20);
            states = Label(right.Content, "State examples heading", 24);
            Email = PixelFormField.Create(left.Content, PixelFormField.Kind.Email, "", "");
            Back = PixelSecondaryAction.Create(left.Content, PixelSecondaryAction.Appearance.Framed, "", GoBack);
            Back.name = "Back action";
            Primary = PixelPrimaryButton.Create(left.Content, "", () =>
                helper.text = Spanish ? "Ejemplo completado. No se envió ningún correo." : "Example completed. No email was sent.");
            Primary.SetFontSize(24);
            Recovery = PixelSecondaryAction.Create(left.Content, PixelSecondaryAction.Appearance.Text, "", () =>
            {
                Recovering = !Recovering;
                if (Recovering) ++RecoveryVisits;
                ApplyText(); Email.Input.Select();
            });
            Recovery.name = "Recovery text action";
            Normal = PixelSecondaryAction.Create(right.Content, PixelSecondaryAction.Appearance.Framed, "", () =>
            {
                ++NormalInvocations;
                Normal.SetPending(true, Spanish ? "Preparando…" : "Preparing…");
                StartCoroutine(CompleteExample());
            });
            Normal.name = "Available secondary action";
            Disabled = PixelSecondaryAction.Create(right.Content, PixelSecondaryAction.Appearance.Text, "");
            Disabled.name = "Disabled text action";
            Pending = PixelSecondaryAction.Create(right.Content, PixelSecondaryAction.Appearance.Framed, "");
            Pending.name = "Pending secondary action";
            LongCaption = PixelSecondaryAction.Create(right.Content, PixelSecondaryAction.Appearance.Text, "", () => SetLocale(!Spanish));
            LongCaption.name = "Long localized text action";
            Email.SetTraversal(LongCaption, Back);
            Back.SetTraversal(Email.Input, Primary);
            var tab = Primary.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous = Back; tab.Next = Recovery;
            Recovery.SetTraversal(Primary, Normal);
            Normal.SetTraversal(Recovery, Disabled);
            Disabled.SetTraversal(Normal, Pending);
            Pending.SetTraversal(Disabled, LongCaption);
            LongCaption.SetTraversal(Pending, Email.Input);
            ResetReview(); Relayout();
        }
        IEnumerator CompleteExample()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Normal.SetPending(false);
        }
        void GoBack()
        {
            ++BackVisits; Recovering = false; ApplyText(); Recovery.Select();
        }
        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture")
                || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) gameObject.AddComponent<PixelActionSmoke>().Run(this);
        }
        void Update()
        {
            if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea) Relayout();
        }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        void ApplyText()
        {
            title.text = Spanish ? "SOLOGYM / ACCIONES SECUNDARIAS" : "SOLOGYM / SECONDARY ACTIONS";
            subtitle.text = Spanish ? "Revisión sin conexión · Tab / Shift+Tab · Entrar para activar" : "Offline review · Tab / Shift+Tab · Enter to activate";
            heading.text = Recovering ? (Spanish ? "RECUPERA TU ACCESO" : "RECOVER YOUR ACCESS") : (Spanish ? "ACCESO A TU AVENTURA" : "ACCESS YOUR ADVENTURE");
            helper.text = Spanish ? "Prueba los controles con un correo inventado." : "Try these controls with a fictional address.";
            Email.SetLocalizedText(Spanish ? "Correo electrónico" : "Email address", Spanish ? "tu@ejemplo.com" : "you@example.com", "", "", "");
            Back.SetLabel(Spanish ? "Volver" : "Back");
            Primary.SetLabel(Recovering ? (Spanish ? "Probar envío" : "Test recovery") : (Spanish ? "Continuar" : "Continue"));
            Recovery.SetLabel(Recovering ? (Spanish ? "Volver al acceso" : "Return to sign in") : (Spanish ? "¿Olvidaste tu contraseña?" : "Forgot your password?"));
            states.text = Spanish ? "ESTADOS DE APOYO" : "SUPPORTING ACTION STATES";
            Normal.SetLabel(Spanish ? "Probar acción secundaria" : "Try secondary action");
            Disabled.SetLabel(Spanish ? "Acción no disponible" : "Action unavailable");
            Pending.SetLabel(Spanish ? "Preparar ejemplo" : "Prepare example");
            Pending.SetPending(true, Spanish ? "Preparando ejemplo…" : "Preparing example…");
            LongCaption.SetLabel(Spanish ? "Cambiar al idioma inglés para revisar los textos" : "Switch to Spanish to review localized captions");
        }
        public void ResetReview()
        {
            StopAllCoroutines();
            Recovering = false; RecoveryVisits = BackVisits = NormalInvocations = 0;
            Normal.SetPending(false); Normal.interactable = true; Disabled.interactable = false;
            Email.SetValueWithoutNotify("hero@example.com"); Email.SetError("");
            ApplyText(); EventSystem.current.SetSelectedGameObject(null);
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
            float width = Mathf.Min(1200, EffectiveSafeArea.width / scaler.scaleFactor - 32), column = (width - 24) / 2, inner = column - 80;
            layout.sizeDelta = new Vector2(width, 544);
            Place(title.rectTransform, 0, 0, width, 36); Place(subtitle.rectTransform, 0, 42, width, 28);
            Place((RectTransform)left.transform, 0, 82, column, 432);
            Place((RectTransform)right.transform, column + 24, 82, column, 432);
            Place(heading.rectTransform, 4, 0, inner - 8, 34); Place(helper.rectTransform, 4, 38, inner - 8, 48);
            Place((RectTransform)Email.transform, 4, 92, inner - 8, 100);
            Place((RectTransform)Back.transform, 4, 210, 160, 64);
            Place((RectTransform)Primary.transform, 176, 210, inner - 180, 64);
            Place((RectTransform)Recovery.transform, 4, 284, inner - 8, 64);
            Place(states.rectTransform, 4, 0, inner - 8, 32);
            Place((RectTransform)Normal.transform, 4, 42, inner - 8, 64);
            Place((RectTransform)Disabled.transform, 4, 114, inner - 8, 64);
            Place((RectTransform)Pending.transform, 4, 186, inner - 8, 64);
            Place((RectTransform)LongCaption.transform, 4, 258, inner - 8, 90);
            Canvas.ForceUpdateCanvases();
        }
        static Text Label(Transform parent, string name, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/PixelifySans");
            text.fontSize = size; text.color = new Color32(224, 219, 210, 255); text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false; text.raycastTarget = false; return text;
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
