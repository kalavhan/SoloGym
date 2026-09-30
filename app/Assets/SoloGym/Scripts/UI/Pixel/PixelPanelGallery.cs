using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Panel composition fixture; all content is local review data.</summary>
    public sealed class PixelPanelGallery : MonoBehaviour
    {
        public PixelContentPanel Tall { get; private set; }
        public PixelContentPanel Wide { get; private set; }
        public PixelContentPanel Compact { get; private set; }
        public PixelPrimaryButton Action { get; private set; }
        public PixelPrimaryButton Resize { get; private set; }
        public PixelPrimaryButton Copy { get; private set; }
        public PixelPrimaryButton Locale { get; private set; }
        public Text WideBody { get; private set; }
        public bool Spanish { get; private set; }
        public bool Narrow { get; private set; }
        public bool LongCopy { get; private set; } = true;
        public int ActionCount { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public PixelContentPanel[] Panels => new[] { Tall, Wide, Compact };
        public PixelPrimaryButton[] Buttons => new[] { Action, Resize, Copy, Locale };

        CanvasScaler scaler;
        RectTransform safeRoot, layout;
        Text heading, subtitle, tallHeading, tallBody, wideHeading, compactHeading, compactValue, note;
        Font font;
        int oldWidth, oldHeight;
        Rect oldSafeArea;
        float inset;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out inset);
            inset = Mathf.Max(0, inset);
            font = Resources.Load<Font>("Fonts/PixelifySans");
            var surface = new GameObject("Panel review canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            var canvas = surface.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Rect("Review backdrop", surface.transform);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var fill = backdrop.gameObject.AddComponent<Image>();
            fill.color = new Color32(23, 21, 29, 255); fill.raycastTarget = false;
            safeRoot = Rect("Safe area", surface.transform);
            layout = Rect("Panel examples", safeRoot);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            if (EventSystem.current == null)
                new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            heading = Label(layout, "Fixture title", 30, true);
            subtitle = Label(layout, "Fixture instructions", 18);
            Tall = PixelContentPanel.Create(layout); Tall.name = "Tall panel";
            Wide = PixelContentPanel.Create(layout); Wide.name = "Wide panel";
            Compact = PixelContentPanel.Create(layout); Compact.name = "Compact panel";
            tallHeading = Label(Tall.Content, "Live title", 28, true);
            tallBody = Label(Tall.Content, "Live body", 20);
            Action = PixelPrimaryButton.Create(Tall.Content, "", () => { ++ActionCount; ApplyText(); });
            Action.name = "Contained primary button"; Action.SetFontSize(24);
            wideHeading = Label(Wide.Content, "Live title", 26, true);
            WideBody = Label(Wide.Content, "Localized paragraph", 20);
            compactHeading = Label(Compact.Content, "Live title", 20, true);
            compactValue = Label(Compact.Content, "Live value", 24);
            note = Label(layout, "Sizing notes", 20);
            Resize = PixelPrimaryButton.Create(layout, "", ToggleWidth);
            Copy = PixelPrimaryButton.Create(layout, "", ToggleCopy);
            Locale = PixelPrimaryButton.Create(layout, "", () => SetLocale(!Spanish));
            Resize.name = "Resize panel"; Copy.name = "Change copy"; Locale.name = "Change locale";
            Resize.SetFontSize(24); Copy.SetFontSize(24); Locale.SetFontSize(24);
            ApplyText(); Relayout();
        }

        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture"))
                gameObject.AddComponent<PixelPanelSmoke>().Run(this);
        }

        void Update()
        {
            if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea) Relayout();
        }

        public void ToggleWidth() { Narrow = !Narrow; Relayout(); }
        public void ToggleCopy() { LongCopy = !LongCopy; ApplyText(); }
        public void SetLocale(bool spanish) { Spanish = spanish; ApplyText(); }
        public void ResetReview()
        {
            ActionCount = 0; Narrow = false; LongCopy = true;
            ApplyText(); Relayout();
            EventSystem.current.SetSelectedGameObject(null);
        }

        void ApplyText()
        {
            heading.text = Spanish ? "SOLOGYM / PANEL DE CONTENIDO" : "SOLOGYM / CONTENT PANEL";
            subtitle.text = Spanish ? "Un mismo marco · contenido editable · tres proporciones" : "One reusable frame · live content · three proportions";
            tallHeading.text = Spanish ? "Tu refugio" : "Your refuge";
            tallBody.text = Spanish
                ? $"El lugar donde comienza tu próxima aventura.\n\nPrueba de interfaz.\nAcciones: {ActionCount}"
                : $"Where your next adventure begins.\n\nInterface example.\nActions: {ActionCount}";
            Action.SetLabel(Spanish ? "Probar acción" : "Try action");
            wideHeading.text = Spanish ? "Un espacio para tu historia" : "Room for your story";
            WideBody.text = Spanish
                ? (LongCopy ? "Este texto sigue siendo editable. El marco conserva sus esquinas cuando cambia el ancho del panel." : "Texto breve, mismo marco.")
                : (LongCopy ? "This text remains editable. The frame keeps its corners when the panel changes width." : "Short copy, same frame.");
            compactHeading.text = Spanish ? "COMPACTO" : "COMPACT";
            compactValue.text = "240 × 144";
            note.text = Spanish ? "Contenido recortado\nal área interior.\n\nTexto y botones vivos." : "Content clipped\nto the inner area.\n\nLive text and buttons.";
            Resize.SetLabel(Spanish ? "Cambiar ancho" : "Change width");
            Copy.SetLabel(Spanish ? "Cambiar texto" : "Change copy");
            Locale.SetLabel(Spanish ? "Idioma: ES" : "Language: EN");
        }

        void Relayout()
        {
            oldWidth = Screen.width; oldHeight = Screen.height; oldSafeArea = Screen.safeArea;
            scaler.scaleFactor = Mathf.Max(.75f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            var actual = Screen.safeArea;
            EffectiveSafeArea = UnityEngine.Rect.MinMaxRect(Mathf.Max(actual.xMin, inset), Mathf.Max(actual.yMin, inset),
                Mathf.Max(inset + 1, Mathf.Min(actual.xMax, Screen.width - inset)),
                Mathf.Max(inset + 1, Mathf.Min(actual.yMax, Screen.height - inset)));
            safeRoot.anchorMin = new Vector2(EffectiveSafeArea.xMin / Screen.width, EffectiveSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(EffectiveSafeArea.xMax / Screen.width, EffectiveSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            float width = Mathf.Min(1200, EffectiveSafeArea.width / scaler.scaleFactor - 32);
            layout.sizeDelta = new Vector2(width, 544);
            Place(heading.rectTransform, 0, 0, width, 36);
            Place(subtitle.rectTransform, 0, 42, width, 28);
            Place((RectTransform)Tall.transform, 0, 82, 344, 356);
            float right = width - 368;
            Place((RectTransform)Wide.transform, 368, 82, Narrow ? Mathf.Min(480, right) : right, 198);
            Place((RectTransform)Compact.transform, 368, 294, 240, 144);
            Place(note.rectTransform, 636, 302, width - 636, 128);
            Place(tallHeading.rectTransform, 0, 0, 264, 38);
            Place(tallBody.rectTransform, 0, 48, 264, 124);
            Place((RectTransform)Action.transform, 4, 206, 256, 64);
            float inside = ((RectTransform)Wide.transform).rect.width - 80;
            Place(wideHeading.rectTransform, 0, 0, inside, 36);
            Place(WideBody.rectTransform, 0, 42, inside, 76);
            Place(compactHeading.rectTransform, 0, 0, 160, 28);
            Place(compactValue.rectTransform, 0, 32, 160, 32);
            float column = (width - 48) / 3;
            Place((RectTransform)Resize.transform, 0, 464, column, 64);
            Place((RectTransform)Copy.transform, column + 24, 464, column, 64);
            Place((RectTransform)Locale.transform, (column + 24) * 2, 464, column, 64);
            Canvas.ForceUpdateCanvases();
        }

        Text Label(Transform parent, string name, int size, bool accent = false)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size;
            text.color = accent ? new Color32(251, 224, 166, 255) : new Color32(224, 219, 210, 255);
            text.alignment = TextAnchor.UpperLeft; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
