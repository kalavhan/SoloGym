using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Isolated landscape fixture. It does not load app scenes or write player data.</summary>
    public sealed class PixelButtonGallery : MonoBehaviour
    {
        public PixelPrimaryButton Normal { get; private set; }
        public PixelPrimaryButton Highlighted { get; private set; }
        public PixelPrimaryButton Pressed { get; private set; }
        public PixelPrimaryButton Selected { get; private set; }
        public PixelPrimaryButton Disabled { get; private set; }
        public PixelPrimaryButton Loading { get; private set; }
        public PixelPrimaryButton Small { get; private set; }
        public PixelPrimaryButton Wide { get; private set; }
        public PixelPrimaryButton Demo { get; private set; }
        public PixelPrimaryButton Complete { get; private set; }
        public PixelPrimaryButton Locale { get; private set; }
        public int DemoCount { get; private set; }
        public bool Spanish { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public IReadOnlyList<PixelPrimaryButton> Buttons => buttons;

        readonly List<PixelPrimaryButton> buttons = new List<PixelPrimaryButton>();
        readonly List<Text> captions = new List<Text>();
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform safeRoot, content;
        Text title, subtitle, variantCaption, demoCaption, status;
        Font font;
        int oldWidth, oldHeight;
        Rect oldSafeArea;
        float requestedInset;

        void Awake()
        {
            Spanish = Argument("-sologym-locale", "es") != "en";
            float.TryParse(Argument("-sologym-safe-inset", "0"),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out requestedInset);
            requestedInset = Mathf.Max(0, requestedInset);
            font = Resources.Load<Font>("Fonts/PixelifySans") ?? Resources.Load<Font>("Fonts/NotoSans-Regular");

            var surface = new GameObject("Pixel primary button review", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            surface.transform.SetParent(transform, false);
            canvas = surface.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            scaler = surface.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = NewRect("Plain review backdrop", surface.transform);
            Stretch(backdrop);
            var fill = backdrop.gameObject.AddComponent<Image>();
            fill.color = new Color32(23, 21, 29, 255);
            fill.raycastTarget = false;
            safeRoot = NewRect("Safe area", surface.transform);
            content = NewRect("Landscape fixture", safeRoot);
            content.anchorMin = content.anchorMax = new Vector2(.5f, .5f);
            content.pivot = new Vector2(.5f, .5f);

            if (EventSystem.current == null)
                new GameObject("Review EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            title = NewText("Title", 30, new Color32(251, 224, 166, 255));
            subtitle = NewText("Subtitle", 18, new Color32(177, 173, 181, 255));
            for (int i = 0; i < 6; ++i)
                captions.Add(NewText("State caption " + i, 18, new Color32(208, 204, 211, 255)));

            Normal = NewButton("Normal");
            Highlighted = NewButton("Highlighted");
            Pressed = NewButton("Pressed");
            Selected = NewButton("Selected");
            Disabled = NewButton("Disabled");
            Disabled.interactable = false;
            Loading = NewButton("Loading");
            variantCaption = NewText("Size caption", 18, new Color32(208, 204, 211, 255));
            Small = NewButton("Compact");
            Wide = NewButton("Long localized label");
            demoCaption = NewText("Interaction caption", 18, new Color32(208, 204, 211, 255));
            Demo = NewButton("Interactive example", () =>
            {
                ++DemoCount;
                Demo.SetLoading(true, Spanish ? "Guardando..." : "Saving...");
                UpdateStatus();
            });
            Complete = NewButton("Complete example", () =>
            {
                Demo.SetLoading(false);
                UpdateStatus();
            });
            Locale = NewButton("Language", () => SetLocale(!Spanish));
            status = NewText("Interaction result", 18, new Color32(177, 173, 181, 255));
            ApplyLabels();
            Relayout();
        }

        IEnumerator Start()
        {
            yield return null;
            RestoreSampleStates();
            if (HasArgument("-sologym-smoke") || HasArgument("-sologym-capture"))
                gameObject.AddComponent<PixelButtonSmoke>().Run(this);
        }

        void Update()
        {
            if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea)
                Relayout();
        }

        PixelPrimaryButton NewButton(string name, UnityEngine.Events.UnityAction action = null)
        {
            var button = PixelPrimaryButton.Create(content, name, action);
            button.name = name;
            button.SetFontSize(26);
            buttons.Add(button);
            return button;
        }

        Text NewText(string name, int size, Color color)
        {
            var rect = NewRect(name, content);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public void SetLocale(bool spanish)
        {
            Spanish = spanish;
            ApplyLabels();
        }

        void ApplyLabels()
        {
            title.text = Spanish ? "SOLOGYM / BOTÓN PRINCIPAL" : "SOLOGYM / PRIMARY BUTTON";
            subtitle.text = Spanish
                ? "Componente de revisión · ratón, toque y teclado · flechas + Enter"
                : "Component review · mouse, touch and keyboard · arrows + Enter";
            string[] stateNames = Spanish
                ? new[] { "NORMAL", "CURSOR ENCIMA", "PRESIONADO", "FOCO DE TECLADO", "DESACTIVADO", "CARGANDO" }
                : new[] { "NORMAL", "HOVERED", "PRESSED", "KEYBOARD FOCUS", "DISABLED", "LOADING" };
            for (int i = 0; i < captions.Count; ++i) captions[i].text = stateNames[i];
            Normal.SetLabel(Spanish ? "Entrar" : "Enter");
            Highlighted.SetLabel(Spanish ? "Explorar" : "Explore");
            Pressed.SetLabel(Spanish ? "Continuar" : "Continue");
            Selected.SetLabel(Spanish ? "Comenzar" : "Begin");
            Disabled.SetLabel(Spanish ? "No disponible" : "Unavailable");
            Loading.SetLabel(Spanish ? "Guardar" : "Save");
            Loading.SetLoading(true, Spanish ? "Guardando..." : "Saving...");
            variantCaption.text = Spanish ? "TAMAÑO COMPACTO / TEXTO LARGO" : "COMPACT SIZE / LONG LABEL";
            Small.SetLabel(Spanish ? "Volver" : "Back");
            Wide.SetLabel(Spanish ? "Preparar mi próxima aventura" : "Prepare my next adventure");
            demoCaption.text = Spanish ? "PRUEBA INTERACTIVA · SIN DATOS REALES" : "INTERACTIVE EXAMPLE · NO REAL DATA";
            Demo.SetLabel(Spanish ? "Registrar serie" : "Log a set");
            if (Demo.IsLoading) Demo.SetLoading(true, Spanish ? "Guardando..." : "Saving...");
            Complete.SetLabel(Spanish ? "Completar prueba" : "Finish example");
            Locale.SetLabel(Spanish ? "Idioma: ES" : "Language: EN");
            UpdateStatus();
        }

        void UpdateStatus()
        {
            if (status == null) return;
            status.text = Spanish
                ? $"Series de prueba: {DemoCount} · {(Demo.IsLoading ? "Pulsa Completar prueba para liberar el botón." : "Registrar serie activa el bloqueo de carga.") }"
                : $"Example sets: {DemoCount} · {(Demo.IsLoading ? "Choose Finish example to release the button." : "Log a set activates the loading lock.") }";
        }

        public void ResetDemo()
        {
            DemoCount = 0;
            Demo.SetLoading(false);
            UpdateStatus();
        }

        public void RestoreSampleStates()
        {
            var events = EventSystem.current;
            if (events == null) return;
            events.SetSelectedGameObject(null);
            foreach (var button in buttons)
            {
                var data = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            }
            Selected.Select();
            var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(Highlighted.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(Pressed.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(Pressed.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Selected.Select();
        }

        void Relayout()
        {
            oldWidth = Screen.width;
            oldHeight = Screen.height;
            oldSafeArea = Screen.safeArea;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            // Keep 64-unit buttons at least 48 screen pixels in the small review layout.
            scaler.scaleFactor = Mathf.Max(.75f, scale);
            var actual = Screen.safeArea;
            var simulated = new Rect(requestedInset, requestedInset,
                Mathf.Max(1, Screen.width - requestedInset * 2), Mathf.Max(1, Screen.height - requestedInset * 2));
            float left = Mathf.Max(actual.xMin, simulated.xMin), bottom = Mathf.Max(actual.yMin, simulated.yMin);
            float right = Mathf.Min(actual.xMax, simulated.xMax), top = Mathf.Min(actual.yMax, simulated.yMax);
            EffectiveSafeArea = Rect.MinMaxRect(left, bottom, Mathf.Max(left + 1, right), Mathf.Max(bottom + 1, top));
            safeRoot.anchorMin = new Vector2(EffectiveSafeArea.xMin / Screen.width, EffectiveSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(EffectiveSafeArea.xMax / Screen.width, EffectiveSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            float width = Mathf.Min(1280, EffectiveSafeArea.width / scaler.scaleFactor);
            content.sizeDelta = new Vector2(width, 544);
            float gap = 24, margin = 28;
            float column = (width - margin * 2 - gap * 2) / 3;
            Place(title.rectTransform, margin, 0, width - margin * 2, 40);
            Place(subtitle.rectTransform, margin, 42, width - margin * 2, 28);
            for (int i = 0; i < 6; ++i)
            {
                float x = margin + (i % 3) * (column + gap), y = 84 + (i / 3) * 104;
                Place(captions[i].rectTransform, x, y, column, 22);
                Place((RectTransform)buttons[i].transform, x, y + 26, column, 64);
            }
            Place(variantCaption.rectTransform, margin, 302, width - margin * 2, 22);
            Place((RectTransform)Small.transform, margin, 328, 176, 64);
            Place((RectTransform)Wide.transform, margin + 200, 328, width - margin * 2 - 200, 64);
            Place(demoCaption.rectTransform, margin, 414, width - margin * 2, 22);
            Place((RectTransform)Demo.transform, margin, 440, column, 64);
            Place((RectTransform)Complete.transform, margin + column + gap, 440, column, 64);
            Place((RectTransform)Locale.transform, margin + (column + gap) * 2, 440, column, 64);
            Place(status.rectTransform, margin, 516, width - margin * 2, 26);
            Canvas.ForceUpdateCanvases();
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static bool HasArgument(string flag) => Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0;

        public static string Argument(string flag, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, flag);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
    }
}
