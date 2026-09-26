using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Pulsing edge chevrons and a slim Track/Fill rail when a ScrollRect overflows.</summary>
    public sealed class SystemScrollAffordance : MonoBehaviour
    {
        const float ChevronW = 40, ChevronH = 28, HitPad = 44;
        const float RailW = 10, RailInset = 6, PulsePeriod = 2f;
        const float TopChevronAboveViewport = 40;

        ScrollRect scroll;
        float rowStride;
        float trackHeight;
        float chevronCenterX;
        Rect viewportPageRect;
        RectTransform host;
        Transform chevronParent;
        GameObject railRoot, topRoot, bottomRoot;
        RectTransform topIcon, bottomIcon;
        RectTransform thumb;
        Vector3 topIconBase, bottomIconBase;
        bool built;

        public void Initialize(ScrollRect target, Rect pageViewport, float stride, Transform chevronRoot = null)
        {
            scroll = target;
            viewportPageRect = pageViewport;
            rowStride = stride > 0 ? stride : 74f;
            trackHeight = pageViewport.height - 16;
            chevronCenterX = pageViewport.x + (pageViewport.width - HitPad) * 0.5f;
            chevronParent = chevronRoot != null ? chevronRoot : scroll.viewport.parent;
            host = GetComponent<RectTransform>();
            if (host == null) host = gameObject.AddComponent<RectTransform>();
            SystemUI.Place(host, pageViewport);
            BuildIfNeeded(pageViewport.width, pageViewport.height);
            scroll.onValueChanged.AddListener(OnScroll);
            Refresh();
        }

        void OnEnable()
        {
            if (scroll != null) Refresh();
        }

        void OnDestroy()
        {
            if (scroll != null) scroll.onValueChanged.RemoveListener(OnScroll);
        }

        void Update() => PulseChevrons();

        void OnScroll(Vector2 _) => Refresh();

        void BuildIfNeeded(float w, float h)
        {
            if (built) return;
            built = true;
            railRoot = SystemUI.Node("Scroll rail", host, new Rect(w - RailW - RailInset, 8, RailW, h - 16)).gameObject;
            SystemUI.Panel(railRoot.transform, new Rect(0, 0, RailW, h - 16), PanelStyle.Track);
            thumb = SystemUI.Node("Scroll thumb", railRoot.transform, new Rect(0, 0, RailW, 32));
            var thumbPanel = thumb.gameObject.AddComponent<SystemPanel>();
            thumbPanel.theme = SystemUI.Theme;
            thumbPanel.style = PanelStyle.Fill;
            thumbPanel.raycastTarget = false;

            float topY = viewportPageRect.y - TopChevronAboveViewport;
            topRoot = BuildChevron("Scroll hint top", new Rect(chevronCenterX, topY, HitPad, HitPad), true, out topIcon);
            bottomRoot = BuildChevron("Scroll hint bottom",
                new Rect(chevronCenterX, viewportPageRect.y + viewportPageRect.height + 6, HitPad, HitPad), false, out bottomIcon);
            topIconBase = topIcon.localPosition;
            bottomIconBase = bottomIcon.localPosition;
        }

        GameObject BuildChevron(string name, Rect pageHitRect, bool pointUp, out RectTransform iconRoot)
        {
            var hit = SystemUI.Node(name, chevronParent, pageHitRect);
            var image = hit.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            var button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Nudge(pointUp ? -1f : 1f));
            iconRoot = SystemUI.Node("Chevron", hit,
                new Rect((HitPad - ChevronW) / 2, (HitPad - ChevronH) / 2, ChevronW, ChevronH));
            iconRoot.pivot = new Vector2(0.5f, 0.5f);
            iconRoot.anchorMin = iconRoot.anchorMax = new Vector2(0.5f, 0.5f);
            iconRoot.anchoredPosition = Vector2.zero;
            if (pointUp) iconRoot.localEulerAngles = new Vector3(0, 0, 180);
            var icon = iconRoot.gameObject.AddComponent<SystemIcon>();
            icon.kind = "down";
            icon.color = SystemUI.Theme.accent;
            icon.raycastTarget = false;
            return hit.gameObject;
        }

        void Nudge(float direction)
        {
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            float max = scroll.content.rect.height - scroll.viewport.rect.height;
            if (max <= 0) return;
            float delta = Mathf.Max(rowStride, scroll.viewport.rect.height * 0.25f) * direction;
            var pos = scroll.content.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y + delta, 0, max);
            scroll.content.anchoredPosition = pos;
            scroll.velocity = Vector2.zero;
            Refresh();
        }

        void Refresh()
        {
            if (scroll == null || scroll.content == null || scroll.viewport == null || !built) return;
            Canvas.ForceUpdateCanvases();
            float viewH = scroll.viewport.rect.height;
            float contentH = scroll.content.rect.height;
            bool overflow = contentH > viewH + 1f;
            if (railRoot != null) railRoot.SetActive(overflow);
            if (topRoot != null) topRoot.SetActive(false);
            if (bottomRoot != null) bottomRoot.SetActive(false);
            if (!overflow) return;

            float norm = scroll.verticalNormalizedPosition;
            if (topRoot != null) topRoot.SetActive(norm < 0.98f);
            if (bottomRoot != null) bottomRoot.SetActive(norm > 0.02f);

            float thumbH = Mathf.Max(24f, trackHeight * (viewH / contentH));
            float travel = Mathf.Max(0, trackHeight - thumbH);
            float thumbY = (1f - norm) * travel;
            SystemUI.Place(thumb, new Rect(0, thumbY, RailW, thumbH));
        }

        void PulseChevrons()
        {
            float t = Time.unscaledTime;
            float wave = (Mathf.Sin(t * Mathf.PI * 2f / PulsePeriod) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.88f, 1f, wave);
            float bob = Mathf.Lerp(-1.5f, 1.5f, wave);
            if (topIcon != null && topRoot != null && topRoot.activeSelf)
            {
                topIcon.localScale = Vector3.one * scale;
                topIcon.localPosition = topIconBase + new Vector3(0, bob, 0);
            }
            if (bottomIcon != null && bottomRoot != null && bottomRoot.activeSelf)
            {
                bottomIcon.localScale = Vector3.one * scale;
                bottomIcon.localPosition = bottomIconBase + new Vector3(0, -bob, 0);
            }
        }
    }
}
