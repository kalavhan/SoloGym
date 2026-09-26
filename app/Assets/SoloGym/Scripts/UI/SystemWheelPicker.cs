using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Compact vertical snap wheel for numeric lists (mobile-style picker).</summary>
    public sealed class SystemWheelPicker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        const int PadRows = 2;
        ScrollRect scroll;
        ScrollRect parentScroll;
        RectTransform content;
        float rowHeight;
        float viewHeight;
        int[] values;
        int lastIndex = -1;
        Action<int> onValueChanged;
        Func<int, string> format;
        bool suppressCallback;
        bool snapping;
        bool dragging;

        public static SystemWheelPicker Attach(
            Transform parent,
            Rect rect,
            int[] values,
            int selectedValue,
            Func<int, string> format,
            Action<int> onValueChanged)
        {
            var host = SystemUI.Node("Wheel picker", parent, rect);
            var picker = host.gameObject.AddComponent<SystemWheelPicker>();
            picker.Build(rect.width, rect.height, values, selectedValue, format, onValueChanged);
            return picker;
        }

        void Build(float width, float viewHeight, int[] valueList, int selectedValue, Func<int, string> labelFormat,
            Action<int> changed)
        {
            values = valueList ?? Array.Empty<int>();
            format = labelFormat ?? (v => v.ToString());
            onValueChanged = changed;
            this.viewHeight = viewHeight;
            rowHeight = 44f;
            parentScroll = GetComponentInParent<ScrollRect>();
            var viewport = SystemUI.Node("Wheel viewport", transform, new Rect(0, 0, width, viewHeight));
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.01f);
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var band = SystemUI.Panel(viewport, new Rect(2, viewHeight * 0.5f - rowHeight * 0.5f, width - 4, rowHeight),
                PanelStyle.Outline);
            band.raycastTarget = false;
            band.color = new Color(1, 1, 1, 0.08f);
            int rows = values.Length + PadRows * 2;
            float contentH = rowHeight * rows;
            content = SystemUI.Node("Wheel content", viewport, new Rect(0, 0, width, contentH));
            content.pivot = new Vector2(0, 1);
            content.anchorMin = content.anchorMax = new Vector2(0, 1);
            var font = SystemUI.Theme.body;
            for (int i = 0; i < values.Length; i++)
            {
                float y = (PadRows + i) * rowHeight;
                var row = SystemUI.Text(content, new Rect(0, y, width, rowHeight), format(values[i]), 22, font,
                    SystemUI.Theme.text, TextAnchor.MiddleCenter);
                row.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            int index = IndexOfValue(selectedValue);
            suppressCallback = true;
            ScrollToIndex(index, false);
            lastIndex = index;
            suppressCallback = false;
        }

        int IndexOfValue(int value)
        {
            for (int i = 0; i < values.Length; i++)
                if (values[i] == value) return i;
            return 0;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            if (parentScroll != null) parentScroll.enabled = false;
            scroll.OnBeginDrag(eventData);
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            scroll.OnDrag(eventData);
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            scroll.OnEndDrag(eventData);
            if (parentScroll != null) parentScroll.enabled = true;
            SnapToNearest();
            eventData.Use();
        }

        public void OnScroll(PointerEventData eventData)
        {
            scroll.OnScroll(eventData);
            eventData.Use();
        }

        void SnapToNearest()
        {
            if (scroll == null || values.Length == 0) return;
            int index = NearestIndex();
            ScrollToIndex(index, true);
            if (index != lastIndex)
            {
                lastIndex = index;
                if (!suppressCallback) onValueChanged?.Invoke(values[index]);
            }
        }

        int NearestIndex()
        {
            float centerOffset = content.anchoredPosition.y + (viewHeight - rowHeight) * 0.5f;
            return Mathf.Clamp(Mathf.RoundToInt(centerOffset / rowHeight - PadRows), 0, values.Length - 1);
        }

        void ScrollToIndex(int index, bool animated)
        {
            index = Mathf.Clamp(index, 0, values.Length - 1);
            float targetY = (PadRows + index) * rowHeight - (viewHeight - rowHeight) * 0.5f;
            targetY = Mathf.Max(0, targetY);
            if (!animated)
            {
                content.anchoredPosition = new Vector2(0, targetY);
                return;
            }
            snapping = true;
            StopAllCoroutines();
            StartCoroutine(SmoothScroll(targetY));
        }

        System.Collections.IEnumerator SmoothScroll(float targetY)
        {
            float start = content.anchoredPosition.y;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 10f)
            {
                content.anchoredPosition = new Vector2(0, Mathf.Lerp(start, targetY, t));
                yield return null;
            }
            content.anchoredPosition = new Vector2(0, targetY);
            snapping = false;
        }

        void LateUpdate()
        {
            if (scroll == null || dragging || snapping) return;
            if (scroll.velocity.magnitude > 80f) return;
            if (scroll.velocity.magnitude > 0.5f)
            {
                scroll.velocity *= 0.9f;
                if (scroll.velocity.magnitude < 30f)
                {
                    scroll.velocity = Vector2.zero;
                    SnapToNearest();
                }
            }
        }
    }
}
