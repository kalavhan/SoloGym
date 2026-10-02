using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>A native button with independent icon art and a required localized name.</summary>
    [AddComponentMenu("SoloGym/UI/Pixel Icon Button")]
    public sealed class PixelIconButton : Button
    {
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] Outline focusOutline;
        [SerializeField] string accessibleLabel = "";
        [SerializeField] Text caption;
        AccessibilityHierarchy accessibilityHierarchy;
        AccessibilityNode accessibilityNode;
        bool accessibilityFocused;
        readonly Vector3[] corners = new Vector3[4];
        public Image Background => background;
        public Image Icon => icon;
        public string AccessibleLabel => accessibleLabel;
        public AccessibilityNode AccessibilityNode => accessibilityNode;
        public bool ShowsFocus => focusOutline != null && focusOutline.enabled;
        public string VisualState { get; private set; } = "normal";

        public static PixelIconButton Create(Transform parent, Sprite sprite, string localizedLabel, UnityAction onClick = null)
        {
            RequireLabel(localizedLabel);
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            var skins = Resources.LoadAll<Sprite>("UI/Pixel/SecondaryAction");
            if (skins.Length == 0) throw new InvalidOperationException("SecondaryAction skin is missing.");
            var root = Child("Icon action", parent); root.sizeDelta = new Vector2(64, 64);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.minHeight = layout.preferredWidth = layout.preferredHeight = 64;
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var button = root.gameObject.AddComponent<PixelIconButton>();
            var face = Child("Shared secondary skin", root);
            face.anchorMin = Vector2.zero; face.anchorMax = Vector2.one; face.offsetMin = face.offsetMax = Vector2.zero;
            button.background = face.gameObject.AddComponent<Image>();
            button.background.sprite = skins[0]; button.background.type = Image.Type.Sliced; button.background.raycastTarget = false;
            button.focusOutline = face.gameObject.AddComponent<Outline>();
            button.focusOutline.effectColor = new Color32(250, 223, 145, 255);
            button.focusOutline.effectDistance = new Vector2(2, -2); button.focusOutline.enabled = false;
            var iconRect = Child("Independent icon", root);
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.sizeDelta = new Vector2(36, 36);
            button.icon = iconRect.gameObject.AddComponent<Image>();
            button.icon.type = Image.Type.Simple; button.icon.preserveAspect = true; button.icon.raycastTarget = false;
            button.targetGraphic = button.background; button.transition = Transition.None;
            button.SetIcon(sprite); button.SetLabel(localizedLabel);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }
        public void SetIcon(Sprite sprite)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            icon.sprite = sprite;
        }
        public void SetLabel(string localizedLabel)
        {
            RequireLabel(localizedLabel); accessibleLabel = localizedLabel;
            if (caption != null) caption.text = accessibleLabel;
            SyncAccessibility();
        }
        /// <summary>Optional always-visible companion text, laid out and owned by the screen.</summary>
        public void BindCaption(Text liveCaption)
        {
            caption = liveCaption;
            if (caption != null) { caption.supportRichText = false; caption.raycastTarget = false; caption.text = accessibleLabel; }
            DoStateTransition(currentSelectionState, true);
        }
        public void SetTraversal(Selectable previous, Selectable next)
        {
            var tab = GetComponent<PixelFieldTabNavigation>() ?? gameObject.AddComponent<PixelFieldTabNavigation>();
            tab.Previous = previous; tab.Next = next;
            navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = previous,
                selectOnUp = previous, selectOnRight = next, selectOnDown = next };
        }
        public bool TryActivate()
        {
            if (!IsActive() || !IsInteractable()) return false;
            onClick.Invoke(); return true;
        }
        public override void OnSubmit(BaseEventData eventData) => TryActivate();

        /// <summary>The screen owns and activates the hierarchy; this control never replaces it globally.</summary>
        public void BindAccessibility(AccessibilityHierarchy hierarchy, AccessibilityNode parent = null)
        {
            if (hierarchy == null) throw new ArgumentNullException(nameof(hierarchy));
            UnbindAccessibility();
            accessibilityHierarchy = hierarchy;
            accessibilityNode = hierarchy.AddNode(accessibleLabel, parent);
            accessibilityNode.role = AccessibilityRole.Button;
            accessibilityNode.frameGetter = ScreenBounds;
            accessibilityNode.invoked += TryActivate;
            accessibilityNode.focusChanged += OnAccessibilityFocus;
            SyncAccessibility();
        }
        public void UnbindAccessibility()
        {
            if (accessibilityNode != null && accessibilityHierarchy.ContainsNode(accessibilityNode))
            {
                accessibilityNode.invoked -= TryActivate;
                accessibilityNode.focusChanged -= OnAccessibilityFocus;
                accessibilityNode.frameGetter = null;
                accessibilityHierarchy.RemoveNode(accessibilityNode);
            }
            accessibilityNode = null; accessibilityHierarchy = null; accessibilityFocused = false;
            DoStateTransition(currentSelectionState, true);
        }
        void OnAccessibilityFocus(AccessibilityNode node, bool focused)
        {
            accessibilityFocused = focused; DoStateTransition(currentSelectionState, true);
        }
        void SyncAccessibility()
        {
            if (accessibilityNode == null) return;
            // A screen can clear its hierarchy before destroying its controls.
            if (!accessibilityHierarchy.ContainsNode(accessibilityNode))
            {
                accessibilityNode = null; accessibilityHierarchy = null; accessibilityFocused = false; return;
            }
            accessibilityNode.label = accessibleLabel;
            accessibilityNode.isActive = IsActive();
            accessibilityNode.state = IsInteractable() ? AccessibilityState.None : AccessibilityState.Disabled;
        }
        public Rect ScreenBounds()
        {
            ((RectTransform)transform).GetWorldCorners(corners);
            var canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = -min;
            foreach (var corner in corners)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (background == null || icon == null) return;
            bool disabled = !IsInteractable();
            bool pressed = !disabled && state == SelectionState.Pressed;
            bool focused = !disabled && (accessibilityFocused || (!PixelTouch.HideFocus && (state == SelectionState.Selected || state == SelectionState.Highlighted)));
            VisualState = disabled ? "disabled" : pressed ? "pressed" : focused ? "focused" : "normal";
            background.color = disabled ? new Color32(110, 116, 118, 255) : pressed ? new Color32(166, 168, 160, 255) : Color.white;
            icon.color = disabled ? new Color32(132, 138, 138, 255) : Color.white;
            icon.rectTransform.anchoredPosition = new Vector2(0, pressed ? -2 : 0);
            focusOutline.enabled = focused && !pressed;
            if (caption != null) caption.color = disabled ? new Color32(151, 153, 151, 255) : new Color32(244, 223, 177, 255);
            SyncAccessibility();
        }
        protected override void OnEnable() { base.OnEnable(); DoStateTransition(currentSelectionState, true); }
        protected override void OnDisable()
        {
            var events = EventSystem.current;
            if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == gameObject) events.SetSelectedGameObject(null);
            accessibilityFocused = false;
            base.OnDisable();
            if (focusOutline != null) focusOutline.enabled = false;
            SyncAccessibility();
        }
        protected override void OnDestroy() { UnbindAccessibility(); base.OnDestroy(); }
        static void RequireLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("An icon action requires a localized accessible label.", nameof(value));
        }
        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
    }
}
