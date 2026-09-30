using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>One sprite skin with live content and native uGUI selection/input semantics.</summary>
    [AddComponentMenu("SoloGym/UI/Pixel Primary Button")]
    public sealed class PixelPrimaryButton : Button
    {
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] Text label;
        [SerializeField] RectTransform content;
        [SerializeField] Outline focusOutline;
        [SerializeField] string normalLabel = "";
        [SerializeField] string busyLabel = "Loading…";
        [SerializeField] bool loading;
        int requestedFontSize = 28;
        SelectionState lastState;

        public bool IsLoading => loading;
        public Text Label => label;
        public Image Background => background;
        public Image Icon => icon;
        public RectTransform Content => content;
        public string VisualState { get; private set; } = "normal";

        public static PixelPrimaryButton Create(Transform parent, string text, UnityAction onClick = null)
        {
            var root = new GameObject("Primary action", typeof(RectTransform), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 64);
            var layout = root.GetComponent<LayoutElement>();
            layout.minWidth = 160; layout.minHeight = 64;
            layout.preferredWidth = 320; layout.preferredHeight = 64;

            // A transparent root catches the full rectangular touch target, including cut corners.
            var hit = root.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var button = root.AddComponent<PixelPrimaryButton>();
            var face = Child("Sprite border and fill", root.transform);
            Stretch(face, 0, 0);
            button.background = face.gameObject.AddComponent<Image>();
            var sprites = Resources.LoadAll<Sprite>("UI/Pixel/PrimaryButton");
            if (sprites.Length == 0)
                throw new InvalidOperationException("PrimaryButton sprite is missing. Import the pixel UI asset before creating buttons.");
            button.background.sprite = sprites[0];
            button.background.type = Image.Type.Sliced;
            button.background.raycastTarget = false;
            button.focusOutline = face.gameObject.AddComponent<Outline>();
            button.focusOutline.effectColor = new Color32(250, 223, 145, 255);
            button.focusOutline.effectDistance = new Vector2(2, -2);
            button.focusOutline.useGraphicAlpha = true;
            button.focusOutline.enabled = false;
            button.targetGraphic = button.background;
            button.transition = Transition.None; // State rendering is immediate; there is no idle animation.

            button.content = Child("Live content", root.transform);
            Stretch(button.content, 24, 8);
            var iconRect = Child("Optional icon", button.content);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f);
            iconRect.pivot = new Vector2(0, .5f);
            iconRect.sizeDelta = new Vector2(24, 24);
            button.icon = iconRect.gameObject.AddComponent<Image>();
            button.icon.preserveAspect = true;
            button.icon.raycastTarget = false;
            button.icon.gameObject.SetActive(false);
            var labelRect = Child("Localized label", button.content);
            Stretch(labelRect, 0, 0);
            button.label = labelRect.gameObject.AddComponent<Text>();
            button.label.font = Resources.Load<Font>("Fonts/PixelifySans") ?? Resources.Load<Font>("Fonts/NotoSans-Regular");
            button.label.fontSize = button.requestedFontSize;
            button.label.alignment = TextAnchor.MiddleCenter;
            button.label.supportRichText = false;
            button.label.horizontalOverflow = HorizontalWrapMode.Wrap;
            button.label.verticalOverflow = VerticalWrapMode.Truncate;
            button.label.raycastTarget = false;
            button.SetLabel(text);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        public void SetLabel(string text)
        {
            normalLabel = text ?? "";
            RefreshContent();
        }

        public void SetFontSize(int size)
        {
            requestedFontSize = Mathf.Clamp(size, 20, 48);
            RefreshContent();
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            if (label != null)
                label.rectTransform.offsetMin = new Vector2(sprite != null ? 32 : 0, 0);
            RefreshContent();
        }

        /// <summary>Call synchronously from the handler before starting an async request.</summary>
        public void SetLoading(bool value, string loadingLabel = "Loading…")
        {
            loading = value;
            busyLabel = loadingLabel ?? "";
            RefreshContent();
            DoStateTransition(currentSelectionState, true);
        }

        public override bool IsInteractable() => !loading && base.IsInteractable();

        // Keyboard submission uses the same guarded event as pointer input. No delayed coroutine
        // is needed for a static press state, and a callback may safely disable/destroy the view.
        public override void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable()) return;
            onClick.Invoke();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            lastState = state;
            if (background == null || label == null) return;
            bool unavailable = !base.IsInteractable();
            bool pressed = !loading && !unavailable && state == SelectionState.Pressed;
            bool focused = !loading && !unavailable &&
                (state == SelectionState.Selected || state == SelectionState.Highlighted);
            VisualState = loading ? "loading" : unavailable ? "disabled" : state.ToString().ToLowerInvariant();
            background.color = unavailable ? new Color32(119, 130, 130, 255)
                : loading ? new Color32(151, 185, 182, 255)
                : pressed ? new Color32(162, 189, 181, 255) : Color.white;
            label.color = unavailable ? new Color32(194, 198, 188, 255) : new Color32(255, 240, 202, 255);
            if (icon != null) icon.color = label.color;
            if (focusOutline != null) focusOutline.enabled = focused;
            if (content != null)
            {
                content.offsetMin = new Vector2(24, pressed ? 6 : 8);
                content.offsetMax = new Vector2(-24, pressed ? -10 : -8);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshContent();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (focusOutline != null) focusOutline.enabled = false;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            RefreshContent();
        }

        void RefreshContent()
        {
            if (label == null) return;
            label.text = loading ? busyLabel : normalLabel;
            label.fontSize = requestedFontSize;
            // Localized captions can wrap to two lines. Fit only as far as the readable floor;
            // callers must allocate a wider/taller rect if content still does not fit.
            for (int size = requestedFontSize; size >= 20; size--)
            {
                label.fontSize = size;
                var measured = MeasureWrappedLabel(label);
                var available = label.rectTransform.rect.size;
                if (measured.x <= available.x + .1f && measured.y <= available.y + .1f) break;
            }
            DoStateTransition(lastState, true);
        }

        internal static Vector2 MeasureWrappedLabel(Text text)
        {
            if (string.IsNullOrEmpty(text.text)) return Vector2.zero;
            if (text.font == null) return new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            // preferredWidth deliberately measures a single unwrapped line. Generate with the
            // real width instead, but permit vertical overflow so truncation cannot hide text.
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = true;
            var generator = text.cachedTextGeneratorForLayout;
            if (!generator.Populate(text.text, settings))
                return new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            return generator.rectExtents.size / text.pixelsPerUnit;
        }

        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
