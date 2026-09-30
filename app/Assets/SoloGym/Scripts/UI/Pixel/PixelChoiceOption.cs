using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>One native Toggle in a single-choice control; selection and focus are distinct.</summary>
    public sealed class PixelChoiceOption : Toggle
    {
        public string Id { get; private set; }
        public Text Label { get; private set; }
        public Image Background { get; private set; }
        public Image Checkmark { get; private set; }
        public AccessibilityNode AccessibilityNode { get; private set; }
        public bool ShowsFocus => focus != null && focus.enabled;
        public string VisualState { get; private set; }
        Outline focus;
        Sprite selectedSkin, normalSkin;
        AccessibilityHierarchy hierarchy;
        bool readerFocus;
        PixelChoiceControl owner;

        internal static PixelChoiceOption Create(Transform parent, string id, string label, ToggleGroup group, PixelChoiceControl owner)
        {
            var root = Child("Choice: " + id, parent); root.sizeDelta = new Vector2(200, 64);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var option = root.gameObject.AddComponent<PixelChoiceOption>(); option.Id = id; option.owner = owner;
            option.SetIsOnWithoutNotify(false); option.group = group; option.transition = Transition.None;
            option.toggleTransition = ToggleTransition.None;
            option.selectedSkin = Resources.LoadAll<Sprite>("UI/Pixel/PrimaryButton")[0];
            option.normalSkin = Resources.LoadAll<Sprite>("UI/Pixel/SecondaryAction")[0];
            var face = Child("Shared choice skin", root); Stretch(face, Vector2.zero, Vector2.zero);
            option.Background = face.gameObject.AddComponent<Image>(); option.Background.type = Image.Type.Sliced; option.Background.raycastTarget = false;
            option.targetGraphic = option.Background;
            option.focus = face.gameObject.AddComponent<Outline>(); option.focus.effectColor = new Color32(250, 223, 145, 255);
            option.focus.effectDistance = new Vector2(2, -2); option.focus.enabled = false;
            var mark = Child("Selected marker", root); mark.anchorMin = mark.anchorMax = new Vector2(0, .5f);
            mark.pivot = new Vector2(.5f, .5f); mark.anchoredPosition = new Vector2(30, 0); mark.sizeDelta = new Vector2(24, 24);
            option.Checkmark = mark.gameObject.AddComponent<Image>(); option.Checkmark.sprite = Resources.LoadAll<Sprite>("UI/Pixel/ChoiceCheck")[0];
            option.Checkmark.preserveAspect = true; option.Checkmark.raycastTarget = false; option.graphic = option.Checkmark;
            var text = Child("Localized choice label", root); Stretch(text, new Vector2(46, 12), new Vector2(-12, -12));
            option.Label = text.gameObject.AddComponent<Text>(); option.Label.font = Resources.Load<Font>("Fonts/PixelifySans");
            option.Label.fontSize = 24; option.Label.alignment = TextAnchor.MiddleCenter; option.Label.supportRichText = false;
            option.Label.raycastTarget = false; option.Label.horizontalOverflow = HorizontalWrapMode.Wrap; option.Label.verticalOverflow = VerticalWrapMode.Truncate;
            option.SetLabel(label); return option;
        }
        public void SetLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A localized choice label is required.", nameof(value));
            Label.text = value; RefreshVisual();
        }
        public bool TryChoose()
        {
            if (!IsActive() || !IsInteractable()) return false;
            isOn = true; return true;
        }
        public override void OnSubmit(BaseEventData eventData) => TryChoose();
        public override void OnMove(AxisEventData eventData)
        {
            if (owner != null && owner.MoveFocus(this, eventData.moveDir))
                eventData.Use();
            else base.OnMove(eventData);
        }
        internal void RefreshVisual() => DoStateTransition(currentSelectionState, true);
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (Background == null || Label == null) return;
            bool unavailable = !IsInteractable(), pressed = !unavailable && state == SelectionState.Pressed;
            bool focused = !unavailable && (readerFocus || state == SelectionState.Selected || state == SelectionState.Highlighted);
            VisualState = unavailable ? "disabled" : pressed ? "pressed" : focused ? "focused" : isOn ? "selected" : "normal";
            Background.sprite = isOn ? selectedSkin : normalSkin;
            Background.color = unavailable ? new Color32(110, 116, 118, 255) : pressed ? new Color32(166, 168, 160, 255) : Color.white;
            Label.color = unavailable ? new Color32(151, 153, 151, 255) : new Color32(255, 240, 202, 255);
            Checkmark.enabled = isOn; Checkmark.color = Label.color;
            Checkmark.rectTransform.anchoredPosition = new Vector2(30, pressed ? -2 : 0);
            Stretch(Label.rectTransform, new Vector2(46, pressed ? 10 : 12), new Vector2(-12, pressed ? -14 : -12));
            focus.enabled = focused && !pressed; SyncAccessibility();
        }
        internal void BindAccessibility(AccessibilityHierarchy owner, AccessibilityNode parent)
        {
            UnbindAccessibility(); hierarchy = owner;
            AccessibilityNode = owner.AddNode(Label.text, parent); AccessibilityNode.role = AccessibilityRole.Toggle;
            AccessibilityNode.frameGetter = () => ScreenBounds((RectTransform)transform);
            AccessibilityNode.invoked += TryChoose; AccessibilityNode.focusChanged += ReaderFocus; SyncAccessibility();
        }
        internal void UnbindAccessibility()
        {
            if (AccessibilityNode != null && hierarchy.ContainsNode(AccessibilityNode))
            {
                AccessibilityNode.invoked -= TryChoose; AccessibilityNode.focusChanged -= ReaderFocus;
                AccessibilityNode.frameGetter = null; hierarchy.RemoveNode(AccessibilityNode);
            }
            AccessibilityNode = null; hierarchy = null; readerFocus = false; RefreshVisual();
        }
        void ReaderFocus(AccessibilityNode node, bool focused) { readerFocus = focused; RefreshVisual(); }
        void SyncAccessibility()
        {
            if (AccessibilityNode == null) return;
            if (!hierarchy.ContainsNode(AccessibilityNode)) { AccessibilityNode = null; hierarchy = null; readerFocus = false; return; }
            AccessibilityNode.label = Label.text; AccessibilityNode.isActive = IsActive();
            AccessibilityNode.state = (isOn ? AccessibilityState.Selected : AccessibilityState.None)
                | (IsInteractable() ? AccessibilityState.None : AccessibilityState.Disabled);
        }
        protected override void OnEnable() { base.OnEnable(); RefreshVisual(); }
        protected override void OnDisable()
        {
            var events = EventSystem.current;
            if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == gameObject) events.SetSelectedGameObject(null);
            readerFocus = false; base.OnDisable();
            if (focus != null) focus.enabled = false;
            SyncAccessibility();
        }
        protected override void OnDestroy() { UnbindAccessibility(); base.OnDestroy(); }
        internal static Rect ScreenBounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = -min;
            foreach (var corner in corners) { var p = RectTransformUtility.WorldToScreenPoint(camera, corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
        }
    }
}
