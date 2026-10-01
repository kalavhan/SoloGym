using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Exactly one stable value, caller-owned availability, and responsive wrapped options.</summary>
    public sealed class PixelChoiceControl : MonoBehaviour
    {
        public readonly UnityEvent<string> onValueChanged = new UnityEvent<string>();
        public string Value { get; private set; }
        public float PreferredHeight { get; private set; }
        public int Columns { get; private set; }
        public IReadOnlyList<PixelChoiceOption> Options => options;
        readonly List<PixelChoiceOption> options = new List<PixelChoiceOption>();
        ToggleGroup toggles;
        CanvasGroup gate;
        LayoutElement layout;
        bool binding;
        AccessibilityHierarchy hierarchy;
        AccessibilityNode container;
        const float MinimumOptionWidth = 180, Gap = 12;
        float minimumWidth = MinimumOptionWidth, fixedHeight;
        public void UseSpriteCards()
        {
            minimumWidth = 112; fixedHeight = 208; layout.minWidth = minimumWidth;
            foreach (var option in options) option.UseSpriteCard();
            RefreshLayout();
        }

        public static PixelChoiceControl Create(Transform parent, string[] ids, string[] labels, string initialValue)
        {
            if (ids == null || labels == null || ids.Length == 0 || ids.Length != labels.Length)
                throw new ArgumentException("Choices need matching non-empty IDs and labels.");
            var unique = new HashSet<string>();
            for (int i = 0; i < ids.Length; ++i)
                if (string.IsNullOrWhiteSpace(ids[i]) || string.IsNullOrWhiteSpace(labels[i]) || !unique.Add(ids[i]))
                    throw new ArgumentException("Each choice needs a unique ID and a localized label.");
            if (!unique.Contains(initialValue ?? "")) throw new ArgumentException("Initial value must be one of the choice IDs.");
            var root = new GameObject("Single choice", typeof(RectTransform), typeof(ToggleGroup), typeof(CanvasGroup), typeof(LayoutElement));
            root.transform.SetParent(parent, false); ((RectTransform)root.transform).sizeDelta = new Vector2(480, 140);
            var control = root.AddComponent<PixelChoiceControl>();
            control.toggles = root.GetComponent<ToggleGroup>(); control.toggles.allowSwitchOff = false;
            control.gate = root.GetComponent<CanvasGroup>(); control.layout = root.GetComponent<LayoutElement>();
            control.layout.minWidth = MinimumOptionWidth;
            for (int i = 0; i < ids.Length; ++i)
            {
                var option = PixelChoiceOption.Create(root.transform, ids[i], labels[i], control.toggles, control);
                control.options.Add(option);
                option.onValueChanged.AddListener(on => control.Changed(option, on));
            }
            control.SetValueWithoutNotify(initialValue); control.SetTraversal(null, null); control.RefreshLayout(); return control;
        }
        public PixelChoiceOption Option(string id)
        {
            foreach (var option in options) if (option.Id == id) return option;
            throw new ArgumentException("Unknown choice ID: " + id, nameof(id));
        }
        public void SetValueWithoutNotify(string id)
        {
            var target = Option(id); binding = true;
            try { target.SetIsOnWithoutNotify(true); Value = id; foreach (var option in options) option.RefreshVisual(); }
            finally { binding = false; }
        }
        public void SetLabel(string id, string label) { Option(id).SetLabel(label); RefreshLayout(); }
        public void SetOptionInteractable(string id, bool enabled) { Option(id).interactable = enabled; RefreshVisuals(); }
        public void SetInteractable(bool enabled) { gate.interactable = enabled; RefreshVisuals(); }
        public bool Interactable => gate != null && gate.interactable;
        void Changed(PixelChoiceOption option, bool selected)
        {
            if (binding) return;
            RefreshVisuals();
            if (!selected || Value == option.Id) return;
            Value = option.Id; onValueChanged.Invoke(Value);
        }
        void RefreshVisuals() { foreach (var option in options) option.RefreshVisual(); }
        public void SetTraversal(Selectable before, Selectable after)
        {
            for (int i = 0; i < options.Count; ++i)
            {
                var tab = options[i].GetComponent<PixelFieldTabNavigation>() ?? options[i].gameObject.AddComponent<PixelFieldTabNavigation>();
                tab.Previous = i > 0 ? options[i - 1] : before; tab.Next = i + 1 < options.Count ? options[i + 1] : after;
                // Arrows move focus without changing the value; Enter/Space commits a choice.
                options[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = tab.Previous, selectOnUp = tab.Previous, selectOnRight = tab.Next, selectOnDown = tab.Next };
            }
        }
        internal bool MoveFocus(PixelChoiceOption current, MoveDirection direction)
        {
            if (direction == MoveDirection.None) return false;
            bool backwards = direction == MoveDirection.Left || direction == MoveDirection.Up;
            int step = (direction == MoveDirection.Up || direction == MoveDirection.Down) ? Mathf.Max(1, Columns) : 1;
            if (backwards) step = -step;
            for (int i = options.IndexOf(current) + step; i >= 0 && i < options.Count; i += step)
            {
                if (!options[i].IsActive() || !options[i].IsInteractable()) continue;
                options[i].Select(); return true;
            }
            var edge = backwards ? options[0] : options[options.Count - 1];
            return edge.GetComponent<PixelFieldTabNavigation>().Move(backwards);
        }
        public void RefreshLayout()
        {
            if (options.Count == 0 || layout == null) return;
            float width = Mathf.Max(minimumWidth, ((RectTransform)transform).rect.width);
            Columns = Mathf.Clamp(Mathf.FloorToInt((width + Gap) / (minimumWidth + Gap)), 1, options.Count);
            float cell = (width - Gap * (Columns - 1)) / Columns, y = 0;
            for (int start = 0; start < options.Count; start += Columns)
            {
                float rowHeight = Mathf.Max(64, fixedHeight);
                for (int i = start; i < Mathf.Min(options.Count, start + Columns); ++i)
                {
                    var rect = (RectTransform)options[i].transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cell);
                    if (fixedHeight == 0) rowHeight = Mathf.Max(rowHeight, PixelPrimaryButton.MeasureWrappedLabel(options[i].Label).y + 24);
                }
                for (int i = start; i < Mathf.Min(options.Count, start + Columns); ++i)
                {
                    var rect = (RectTransform)options[i].transform;
                    rect.anchoredPosition = new Vector2((i - start) * (cell + Gap), -y); rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
                }
                y += rowHeight + Gap;
            }
            PreferredHeight = y - Gap; layout.minHeight = layout.preferredHeight = PreferredHeight;
        }
        public void BindAccessibility(AccessibilityHierarchy owner, string groupLabel)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (string.IsNullOrWhiteSpace(groupLabel)) throw new ArgumentException("A localized group name is required.");
            UnbindAccessibility(); hierarchy = owner; container = owner.AddNode(groupLabel); container.role = AccessibilityRole.Container;
            container.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)transform);
            foreach (var option in options) option.BindAccessibility(owner, container);
        }
        public void SetAccessibleGroupLabel(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("A localized group name is required.");
            if (container != null && hierarchy.ContainsNode(container)) container.label = text;
        }
        public void UnbindAccessibility()
        {
            foreach (var option in options) if (option != null) option.UnbindAccessibility();
            if (container != null && hierarchy.ContainsNode(container)) hierarchy.RemoveNode(container);
            container = null; hierarchy = null;
        }
        void OnRectTransformDimensionsChange() => RefreshLayout();
        void OnEnable() { RefreshVisuals(); RefreshLayout(); }
        void OnDestroy() => UnbindAccessibility();
    }
}
