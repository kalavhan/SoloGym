using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>A navigation request, not a model selection. The router confirms the current destination.</summary>
    public sealed class PixelNavigationTab : Button
    {
        public string Id { get; private set; }
        public bool IsCurrent { get; private set; }
        public Text Label { get; private set; }
        public Image Background { get; private set; }
        public Image CurrentMarker { get; private set; }
        public bool ShowsFocus => focus != null && focus.enabled;
        public string VisualState { get; private set; }
        public AccessibilityNode AccessibilityNode { get; private set; }
        PixelNavigationBar owner;
        Sprite normalSkin, currentSkin;
        Outline focus;
        AccessibilityHierarchy hierarchy;
        bool readerFocus;

        internal static PixelNavigationTab Create(Transform parent, string id, string label, PixelNavigationBar owner)
        {
            var root = Child("Navigation: " + id, parent); root.sizeDelta = new Vector2(220, 64);
            var target = root.gameObject.AddComponent<Image>(); target.color = Color.clear; target.raycastTarget = true;
            var tab = root.gameObject.AddComponent<PixelNavigationTab>(); tab.Id = id; tab.owner = owner; tab.transition = Transition.None;
            tab.normalSkin = Resources.LoadAll<Sprite>("UI/Pixel/SecondaryAction")[0];
            tab.currentSkin = Resources.LoadAll<Sprite>("UI/Pixel/PrimaryButton")[0];
            var face = Child("Shared navigation skin", root); Stretch(face, Vector2.zero, Vector2.zero);
            tab.Background = face.gameObject.AddComponent<Image>(); tab.Background.raycastTarget = false; tab.Background.type = Image.Type.Sliced;
            tab.targetGraphic = tab.Background; tab.focus = face.gameObject.AddComponent<Outline>(); tab.focus.effectColor = new Color32(250, 223, 145, 255);
            tab.focus.effectDistance = new Vector2(2, -2); tab.focus.enabled = false;
            var marker = Child("Current destination marker", root); marker.anchorMin = marker.anchorMax = new Vector2(0, .5f);
            marker.anchoredPosition = new Vector2(30, 0); marker.sizeDelta = new Vector2(24, 24);
            tab.CurrentMarker = marker.gameObject.AddComponent<Image>(); tab.CurrentMarker.sprite = Resources.LoadAll<Sprite>("UI/Pixel/ChoiceCheck")[0];
            tab.CurrentMarker.preserveAspect = true; tab.CurrentMarker.raycastTarget = false;
            var caption = Child("Localized destination", root); Stretch(caption, new Vector2(46, 12), new Vector2(-12, -12));
            tab.Label = caption.gameObject.AddComponent<Text>(); tab.Label.font = Resources.Load<Font>("Fonts/PixelifySans"); tab.Label.fontSize = 24;
            tab.Label.alignment = TextAnchor.MiddleCenter; tab.Label.supportRichText = false; tab.Label.raycastTarget = false;
            tab.onClick.AddListener(() => tab.TryNavigate()); tab.SetLabel(label); return tab;
        }
        public bool TryNavigate() => IsActive() && IsInteractable() && owner.Request(this);
        public override void OnSubmit(BaseEventData eventData) => TryNavigate();
        public override void OnMove(AxisEventData eventData)
        { if (owner != null && owner.MoveFocus(this, eventData.moveDir)) eventData.Use(); else base.OnMove(eventData); }
        public void SetLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A localized destination label is required.");
            Label.text = value; RefreshVisual();
        }
        internal void SetCurrent(bool value) { IsCurrent = value; RefreshVisual(); }
        internal void RefreshVisual() => DoStateTransition(currentSelectionState, true);
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (Background == null || Label == null) return;
            bool disabled = !IsInteractable(), pressed = !disabled && state == SelectionState.Pressed;
            bool focused = !disabled && (readerFocus || state == SelectionState.Selected || state == SelectionState.Highlighted);
            VisualState = disabled ? "disabled" : pressed ? "pressed" : focused ? "focused" : IsCurrent ? "current" : "normal";
            Background.sprite = IsCurrent ? currentSkin : normalSkin;
            Background.color = disabled ? new Color32(110, 116, 118, 255) : pressed ? new Color32(166, 168, 160, 255) : Color.white;
            Label.color = disabled ? new Color32(151, 153, 151, 255) : new Color32(255, 240, 202, 255);
            CurrentMarker.enabled = IsCurrent; CurrentMarker.color = Label.color;
            CurrentMarker.rectTransform.anchoredPosition = new Vector2(30, pressed ? -2 : 0);
            Stretch(Label.rectTransform, new Vector2(46, pressed ? 10 : 12), new Vector2(-12, pressed ? -14 : -12));
            focus.enabled = focused && !pressed; SyncAccessibility();
        }
        internal void BindAccessibility(AccessibilityHierarchy ownerHierarchy, AccessibilityNode parent)
        {
            UnbindAccessibility(); hierarchy = ownerHierarchy; AccessibilityNode = hierarchy.AddNode(Label.text, parent);
            AccessibilityNode.role = AccessibilityRole.TabButton; AccessibilityNode.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)transform);
            AccessibilityNode.invoked += TryNavigate; AccessibilityNode.focusChanged += ReaderFocus; SyncAccessibility();
        }
        internal void UnbindAccessibility()
        {
            if (AccessibilityNode != null && hierarchy.ContainsNode(AccessibilityNode))
            { AccessibilityNode.invoked -= TryNavigate; AccessibilityNode.focusChanged -= ReaderFocus; AccessibilityNode.frameGetter = null; hierarchy.RemoveNode(AccessibilityNode); }
            AccessibilityNode = null; hierarchy = null; readerFocus = false; RefreshVisual();
        }
        void ReaderFocus(AccessibilityNode node, bool value) { readerFocus = value; RefreshVisual(); }
        void SyncAccessibility()
        {
            if (AccessibilityNode == null) return;
            if (!hierarchy.ContainsNode(AccessibilityNode)) { AccessibilityNode = null; hierarchy = null; readerFocus = false; return; }
            AccessibilityNode.label = Label.text; AccessibilityNode.isActive = IsActive();
            AccessibilityNode.state = (IsCurrent ? AccessibilityState.Selected : AccessibilityState.None) | (IsInteractable() ? AccessibilityState.None : AccessibilityState.Disabled);
        }
        protected override void OnEnable() { base.OnEnable(); RefreshVisual(); }
        protected override void OnDisable()
        {
            var events = EventSystem.current;
            if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == gameObject) events.SetSelectedGameObject(null);
            readerFocus = false; base.OnDisable(); if (focus != null) focus.enabled = false; SyncAccessibility();
        }
        protected override void OnDestroy() { UnbindAccessibility(); base.OnDestroy(); }
        static RectTransform Child(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
    }
}
