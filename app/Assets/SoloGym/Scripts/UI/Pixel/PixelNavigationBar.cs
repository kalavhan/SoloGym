using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Stable route IDs, caller-confirmed current state, and truly omitted unavailable destinations.</summary>
    public sealed class PixelNavigationBar : MonoBehaviour
    {
        public readonly UnityEvent<string> onNavigate = new UnityEvent<string>();
        public string CurrentId { get; private set; }
        public IReadOnlyList<PixelNavigationTab> Tabs => tabs.AsReadOnly();
        public float PreferredHeight { get; private set; }
        public int Columns { get; private set; }
        public AccessibilityNode AccessibilityNode { get; private set; }
        readonly List<PixelNavigationTab> tabs = new List<PixelNavigationTab>();
        CanvasGroup gate;
        LayoutElement layout;
        AccessibilityHierarchy hierarchy;
        Selectable previous, next;
        const float MinimumWidth = 180, Gap = 12;
        float minimumCellWidth = MinimumWidth, cellGap = Gap;

        public void SetLayoutMetrics(float minimumWidth, float gap)
        {
            if (float.IsNaN(minimumWidth) || float.IsInfinity(minimumWidth) || minimumWidth < 100
                || float.IsNaN(gap) || float.IsInfinity(gap) || gap < 0 || gap > 32)
                throw new ArgumentOutOfRangeException(nameof(minimumWidth));
            minimumCellWidth = minimumWidth; cellGap = gap;
            foreach (var tab in tabs) tab.SetCompactLabel(minimumWidth < MinimumWidth);
            RefreshLayout();
        }

        public static PixelNavigationBar Create(Transform parent, string[] ids, string[] labels, string[] visibleIds, string currentId)
        {
            if (ids == null || labels == null || ids.Length == 0 || ids.Length != labels.Length) throw new ArgumentException("Provide matching route IDs and localized labels.");
            var unique = new HashSet<string>();
            for (int i = 0; i < ids.Length; ++i)
                if (string.IsNullOrWhiteSpace(ids[i]) || string.IsNullOrWhiteSpace(labels[i]) || !unique.Add(ids[i])) throw new ArgumentException("Navigation IDs must be unique and labels nonempty.");
            ValidateVisible(unique, visibleIds, currentId);
            var root = new GameObject("Pixel navigation bar", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            root.transform.SetParent(parent, false); ((RectTransform)root.transform).sizeDelta = new Vector2(1000, 64);
            var bar = root.AddComponent<PixelNavigationBar>(); bar.gate = root.GetComponent<CanvasGroup>(); bar.layout = root.GetComponent<LayoutElement>(); bar.layout.minWidth = MinimumWidth;
            for (int i = 0; i < ids.Length; ++i) bar.tabs.Add(PixelNavigationTab.Create(root.transform, ids[i], labels[i], bar));
            bar.BindVisibleRoutes(visibleIds, currentId); return bar;
        }
        static HashSet<string> ValidateVisible(HashSet<string> known, string[] ids, string currentId)
        {
            if (ids == null || ids.Length == 0) throw new ArgumentException("At least one destination must remain visible.");
            var result = new HashSet<string>();
            foreach (var id in ids) if (id == null || !known.Contains(id) || !result.Add(id)) throw new ArgumentException("Visible routes must be unique known IDs.");
            if (currentId == null || !result.Contains(currentId)) throw new ArgumentException("The confirmed current route must be visible.");
            return result;
        }
        public PixelNavigationTab Tab(string id)
        {
            foreach (var tab in tabs) if (tab.Id == id) return tab;
            throw new ArgumentException("Unknown navigation ID: " + id);
        }
        /// <summary>Atomically bind eligibility plus the router's confirmed destination. Does not emit navigation.</summary>
        public void BindVisibleRoutes(string[] visibleIds, string confirmedCurrentId)
        {
            var known = new HashSet<string>(); foreach (var tab in tabs) known.Add(tab.Id);
            var visible = ValidateVisible(known, visibleIds, confirmedCurrentId);
            var events = EventSystem.current; var focused = events == null ? null : events.currentSelectedGameObject;
            bool rescue = false;
            foreach (var tab in tabs) if (tab.gameObject == focused && !visible.Contains(tab.Id)) rescue = true;
            CurrentId = confirmedCurrentId;
            foreach (var tab in tabs) { tab.SetCurrent(tab.Id == CurrentId); tab.gameObject.SetActive(visible.Contains(tab.Id)); }
            RefreshLayout(); SetTraversal(previous, next);
            if (rescue) RestoreFocus();
        }
        public void SetCurrentWithoutNotify(string id)
        {
            var tab = Tab(id);
            if (!tab.gameObject.activeSelf) throw new ArgumentException("A hidden route cannot become current.");
            CurrentId = id; foreach (var option in tabs) option.SetCurrent(option.Id == id);
        }
        internal bool Request(PixelNavigationTab tab)
        {
            if (!tab.IsActive() || !tab.IsInteractable() || tab.Id == CurrentId) return false;
            onNavigate.Invoke(tab.Id); return true;
        }
        public void SetLabel(string id, string label) { Tab(id).SetLabel(label); RefreshLayout(); }
        public void SetTabInteractable(string id, bool value)
        {
            var tab = Tab(id); bool focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == tab.gameObject;
            tab.interactable = value; tab.RefreshVisual(); if (!value && focused) RestoreFocus();
        }
        public void SetInteractable(bool value) { gate.interactable = value; foreach (var tab in tabs) tab.RefreshVisual(); }
        void RestoreFocus()
        {
            var current = Tab(CurrentId); if (current.IsActive() && current.IsInteractable()) { current.Select(); return; }
            foreach (var tab in tabs) if (tab.IsActive() && tab.IsInteractable()) { tab.Select(); return; }
            if (previous != null && previous.IsActive() && previous.IsInteractable()) previous.Select();
            else if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        List<PixelNavigationTab> Visible()
        { var result = new List<PixelNavigationTab>(); foreach (var tab in tabs) if (tab.gameObject.activeSelf) result.Add(tab); return result; }
        public void SetTraversal(Selectable before, Selectable after)
        {
            previous = before; next = after; var visible = Visible();
            for (int i = 0; i < visible.Count; ++i)
            {
                var tab = visible[i]; var link = tab.GetComponent<PixelFieldTabNavigation>() ?? tab.gameObject.AddComponent<PixelFieldTabNavigation>();
                link.Previous = i == 0 ? before : visible[i - 1]; link.Next = i + 1 == visible.Count ? after : visible[i + 1];
                tab.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = link.Previous, selectOnUp = link.Previous, selectOnRight = link.Next, selectOnDown = link.Next };
            }
        }
        internal bool MoveFocus(PixelNavigationTab current, MoveDirection direction)
        {
            if (direction == MoveDirection.None) return false;
            bool backwards = direction == MoveDirection.Left || direction == MoveDirection.Up;
            int step = direction == MoveDirection.Up || direction == MoveDirection.Down ? Mathf.Max(1, Columns) : 1; if (backwards) step = -step;
            var visible = Visible();
            for (int i = visible.IndexOf(current) + step; i >= 0 && i < visible.Count; i += step)
                if (visible[i].IsActive() && visible[i].IsInteractable()) { visible[i].Select(); return true; }
            var edge = backwards ? visible[0] : visible[visible.Count - 1]; return edge.GetComponent<PixelFieldTabNavigation>().Move(backwards);
        }
        public void RefreshLayout()
        {
            if (layout == null) return;
            var visible = Visible(); if (visible.Count == 0) return;
            float width = Mathf.Max(minimumCellWidth, ((RectTransform)transform).rect.width);
            Columns = Mathf.Clamp(Mathf.FloorToInt((width + cellGap) / (minimumCellWidth + cellGap)), 1, visible.Count);
            float cell = (width - cellGap * (Columns - 1)) / Columns, y = 0;
            for (int start = 0; start < visible.Count; start += Columns)
            {
                float height = 64;
                for (int i = start; i < Mathf.Min(visible.Count, start + Columns); ++i)
                {
                    var rect = (RectTransform)visible[i].transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cell);
                    height = Mathf.Max(height, PixelPrimaryButton.MeasureWrappedLabel(visible[i].Label).y + 24);
                }
                for (int i = start; i < Mathf.Min(visible.Count, start + Columns); ++i)
                {
                    var rect = (RectTransform)visible[i].transform; rect.anchoredPosition = new Vector2((i - start) * (cell + cellGap), -y);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                }
                y += height + cellGap;
            }
            PreferredHeight = y - cellGap; layout.minHeight = layout.preferredHeight = PreferredHeight;
        }
        public void BindAccessibility(AccessibilityHierarchy owner, string localizedLabel)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (string.IsNullOrWhiteSpace(localizedLabel)) throw new ArgumentException("A localized navigation name is required.");
            UnbindAccessibility(); hierarchy = owner; AccessibilityNode = owner.AddNode(localizedLabel); AccessibilityNode.role = AccessibilityRole.TabBar;
            AccessibilityNode.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)transform);
            foreach (var tab in tabs) tab.BindAccessibility(owner, AccessibilityNode); SyncAccessibility();
        }
        public void SetAccessibleLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A localized navigation name is required.");
            SyncAccessibility(); if (AccessibilityNode != null) AccessibilityNode.label = label;
        }
        void SyncAccessibility()
        {
            if (AccessibilityNode == null) return;
            if (!hierarchy.ContainsNode(AccessibilityNode)) { AccessibilityNode = null; hierarchy = null; return; }
            AccessibilityNode.isActive = isActiveAndEnabled;
        }
        public void UnbindAccessibility()
        {
            foreach (var tab in tabs) if (tab != null) tab.UnbindAccessibility();
            if (AccessibilityNode != null && hierarchy.ContainsNode(AccessibilityNode)) { AccessibilityNode.frameGetter = null; hierarchy.RemoveNode(AccessibilityNode); }
            AccessibilityNode = null; hierarchy = null;
        }
        void OnRectTransformDimensionsChange() => RefreshLayout();
        void OnEnable() { RefreshLayout(); SyncAccessibility(); }
        void OnDisable() => SyncAccessibility();
        void OnDestroy() => UnbindAccessibility();
    }
}
