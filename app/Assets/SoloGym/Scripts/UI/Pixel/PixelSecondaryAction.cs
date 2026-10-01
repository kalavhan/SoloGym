using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Quieter framed and text actions; callbacks and localized copy belong to the caller.</summary>
    [AddComponentMenu("SoloGym/UI/Pixel Secondary Action")]
    public sealed class PixelSecondaryAction : Button
    {
        public enum Appearance { Framed, Text }

        [SerializeField] Appearance appearance;
        [SerializeField] Image background;
        [SerializeField] Image underline;
        [SerializeField] Text label;
        [SerializeField] Outline focusOutline;
        [SerializeField] string normalLabel = "";
        [SerializeField] string pendingLabel = "";
        [SerializeField] bool pending;
        float horizontalPadding = 20;
        public Text Label => label;
        public Image Background => background;
        public Image Underline => underline;
        public Appearance Style => appearance;
        public bool IsPending => pending;
        public bool ShowsFocus => focusOutline != null && focusOutline.enabled;
        public string VisualState { get; private set; } = "normal";

        public static PixelSecondaryAction Create(Transform parent, Appearance style, string text, UnityAction onClick = null)
        {
            var root = Child(style + " action", parent);
            root.sizeDelta = new Vector2(320, 64);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 160; layout.minHeight = 64;
            layout.preferredWidth = 320; layout.preferredHeight = 64;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear; hit.raycastTarget = true;
            var action = root.gameObject.AddComponent<PixelSecondaryAction>();
            action.appearance = style;
            var face = Child("Secondary sprite skin", root); Stretch(face, 0, 0);
            action.background = face.gameObject.AddComponent<Image>();
            var sprites = Resources.LoadAll<Sprite>("UI/Pixel/SecondaryAction");
            if (sprites.Length == 0) throw new InvalidOperationException("SecondaryAction sprite is missing.");
            action.background.sprite = sprites[0]; action.background.type = Image.Type.Sliced;
            action.background.raycastTarget = false;
            action.focusOutline = face.gameObject.AddComponent<Outline>();
            action.focusOutline.effectColor = new Color32(250, 223, 145, 255);
            action.focusOutline.effectDistance = new Vector2(2, -2);
            action.focusOutline.enabled = false;
            action.targetGraphic = action.background; action.transition = Transition.None;
            var caption = Child("Localized action label", root); Stretch(caption, 20, 12);
            action.label = caption.gameObject.AddComponent<Text>();
            action.label.font = Resources.Load<Font>("Fonts/PixelifySans");
            action.label.fontSize = 24; action.label.alignment = TextAnchor.MiddleCenter;
            action.label.supportRichText = false; action.label.raycastTarget = false;
            action.label.horizontalOverflow = HorizontalWrapMode.Wrap;
            action.label.verticalOverflow = VerticalWrapMode.Truncate;
            var line = Child("Text action underline", root);
            line.anchorMin = line.anchorMax = line.pivot = new Vector2(.5f, .5f);
            action.underline = line.gameObject.AddComponent<Image>(); action.underline.raycastTarget = false;
            action.SetLabel(text);
            if (onClick != null) action.onClick.AddListener(onClick);
            return action;
        }

        public void SetLabel(string text) { normalLabel = text ?? ""; RefreshContent(); }

        public void SetHorizontalPadding(float value)
        { horizontalPadding = Mathf.Clamp(value, 8, 48); RefreshContent(); }

        /// <summary>Set synchronously before an async operation to block repeat pointer/keyboard activation.</summary>
        public void SetPending(bool value, string localizedCaption = "")
        {
            pending = value; pendingLabel = localizedCaption ?? ""; RefreshContent();
        }

        public void SetTraversal(Selectable previous, Selectable next)
        {
            var tab = GetComponent<PixelFieldTabNavigation>() ?? gameObject.AddComponent<PixelFieldTabNavigation>();
            tab.Previous = previous; tab.Next = next;
            navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous,
                selectOnLeft = previous, selectOnDown = next, selectOnRight = next };
        }

        public override bool IsInteractable() => !pending && base.IsInteractable();
        public override void OnSubmit(BaseEventData eventData)
        {
            if (IsActive() && IsInteractable()) onClick.Invoke();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (background == null || label == null) return;
            bool disabled = !base.IsInteractable();
            bool pressed = !disabled && !pending && state == SelectionState.Pressed;
            bool focused = !disabled && !pending && (state == SelectionState.Selected || state == SelectionState.Highlighted);
            VisualState = disabled ? "disabled" : pending ? "pending" : state.ToString().ToLowerInvariant();
            // Text actions acquire the same quiet frame on focus/press; their full hit area is always present.
            background.enabled = appearance == Appearance.Framed || focused || pressed;
            background.color = disabled ? new Color32(110, 116, 118, 255)
                : pending ? new Color32(158, 158, 151, 255)
                : pressed ? new Color32(166, 168, 160, 255) : Color.white;
            label.color = disabled ? new Color32(151, 153, 151, 255)
                : pending ? new Color32(210, 204, 185, 255) : new Color32(244, 223, 177, 255);
            focusOutline.enabled = focused;
            Stretch(label.rectTransform, horizontalPadding, 12);
            if (pressed) label.rectTransform.anchoredPosition = new Vector2(0, -2);
            UpdateUnderline(pressed, disabled);
        }

        protected override void OnEnable() { base.OnEnable(); RefreshContent(); }
        protected override void OnDisable()
        {
            var events = EventSystem.current;
            if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == gameObject)
                events.SetSelectedGameObject(null);
            base.OnDisable();
            if (focusOutline != null) focusOutline.enabled = false;
        }
        protected override void OnRectTransformDimensionsChange() { base.OnRectTransformDimensionsChange(); RefreshContent(); }
        void RefreshContent()
        {
            if (label == null) return;
            label.text = pending && pendingLabel.Length > 0 ? pendingLabel : normalLabel;
            // Keep a readable fixed font. A layout group can grow the action for wrapped captions.
            var measured = PixelPrimaryButton.MeasureWrappedLabel(label);
            var layout = GetComponent<LayoutElement>();
            if (layout != null && !float.IsInfinity(measured.y)) layout.preferredHeight = Mathf.Max(64, measured.y + 24);
            DoStateTransition(currentSelectionState, true);
        }
        void UpdateUnderline(bool pressed, bool disabled)
        {
            if (underline == null) return;
            underline.enabled = appearance == Appearance.Text && !pending && !disabled;
            underline.color = label.color;
            var measured = PixelPrimaryButton.MeasureWrappedLabel(label);
            underline.rectTransform.sizeDelta = new Vector2(Mathf.Min(label.preferredWidth, label.rectTransform.rect.width), 2);
            underline.rectTransform.anchoredPosition = new Vector2(0, -Mathf.Min(measured.y, label.rectTransform.rect.height) / 2 - (pressed ? 3 : 1));
        }
        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        static void Stretch(RectTransform rect, float x, float y)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y);
        }
    }
}
