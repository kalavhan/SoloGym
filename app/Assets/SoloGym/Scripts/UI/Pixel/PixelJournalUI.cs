using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Small native controls for the parchment journal, using the existing sprite skins.</summary>
    public static class PixelJournalUI
    {
        public static readonly Color Ink = new Color32(35, 29, 24, 255);
        public static readonly Color Ivory = new Color32(255, 235, 194, 255);
        public static RectTransform Rect(string name, Transform parent, Rect bounds)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); Place(r, bounds); return r;
        }
        public static void Place(RectTransform r, Rect b)
        { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(b.x, -b.y); r.sizeDelta = b.size; }
        public static void Stretch(RectTransform r)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        public static Image Art(Transform p, Rect b, string resource)
        {
            var i = Rect(resource, p, b).gameObject.AddComponent<Image>(); i.sprite = Resources.Load<Sprite>(resource); i.raycastTarget = false; return i;
        }
        public static RectTransform Frame(Transform p, Rect b, string name)
        {
            var r = Rect(name, p, b); var fill = r.gameObject.AddComponent<Image>();
            fill.sprite = Resources.Load<Sprite>("Rooms/TrainingHallR1/ui-frame"); fill.raycastTarget = false;
            var edge = Art(r, new Rect(0, 0, b.width, b.height), "UI/Pixel/ContentPanel");
            edge.sprite = Resources.LoadAll<Sprite>("UI/Pixel/ContentPanel")[0]; edge.type = Image.Type.Sliced; edge.fillCenter = false; edge.pixelsPerUnitMultiplier = 3;
            return r;
        }
        public static Text Text(Transform p, Rect b, string text, int size = 22, bool paper = true, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var t = Rect("Text: " + text, p, b).gameObject.AddComponent<Text>();
            t.font = Resources.Load<Font>("Fonts/PixelifySans"); t.fontSize = size; t.text = text; t.supportRichText = false;
            t.color = paper ? Ink : Ivory; t.alignment = align; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.raycastTarget = false; return t;
        }
        public static PixelJournalAction Action(Transform parent, Rect b, string label, Action callback, bool framed = true, bool selected = false, int size = 22)
        {
            var r = Rect(label, parent, b); var hit = r.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.canvasRenderer.cullTransparentMesh = false;
            var action = r.gameObject.AddComponent<PixelJournalAction>(); action.Initialize(label, callback, framed, selected, size); return action;
        }
        /// <summary>
        /// Fills the whole screen behind the 1280x720 composition (no black side bars on wide phones).
        /// dim &lt; 1 darkens the fill when the composition draws its own copy of the art.
        /// </summary>
        public static Image CoverBackdrop(Transform canvasRoot, string resource, float dim = 1f)
        {
            var r = new GameObject("Full-bleed backdrop: " + resource, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(canvasRoot, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.SetAsFirstSibling();
            var image = r.gameObject.AddComponent<Image>(); image.sprite = Resources.Load<Sprite>(resource); image.raycastTarget = false; image.color = new Color(dim, dim, dim, 1);
            var fit = r.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = image.sprite != null ? image.sprite.rect.width / image.sprite.rect.height : 16f / 9f;
            return image;
        }
        public static Image Rule(Transform p, float x, float y, float width)
        { var i = Rect("Live separator", p, new Rect(x, y, width, 1)).gameObject.AddComponent<Image>(); i.color = new Color32(129, 91, 50, 125); i.raycastTarget = false; return i; }
        public static RectTransform Scroll(Transform p, Rect bounds, float contentHeight)
        {
            var outer = Rect("Journal scroll", p, bounds); var hit = outer.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.canvasRenderer.cullTransparentMesh = false;
            outer.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Scrollable live text", outer, new Rect(0, 0, bounds.width - 12, Mathf.Max(bounds.height, contentHeight)));
            var scroll = outer.gameObject.AddComponent<ScrollRect>(); scroll.viewport = outer; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            if (contentHeight > bounds.height)
            {
                var track = Rect("Scroll indicator", outer, new Rect(bounds.width - 7, 0, 7, bounds.height));
                track.gameObject.AddComponent<Image>().color = new Color32(107, 75, 47, 70);
                var thumb = Rect("Scroll thumb", track, new Rect(0, 0, 7, bounds.height)); thumb.gameObject.AddComponent<Image>().color = new Color32(102, 65, 35, 230);
                Stretch(thumb);
                var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = thumb; bar.targetGraphic = thumb.GetComponent<Image>(); bar.direction = Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar = bar; bar.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            outer.gameObject.AddComponent<PixelJournalScrollFocus>().Scroll = scroll; return content;
        }
    }
    public sealed class PixelJournalAction : Button
    {
        public Text Label { get; private set; }
        Image skin;
        readonly List<Text> inkTexts = new List<Text>();
        public void AddInkText(Text text) { inkTexts.Add(text); Refresh(); }
        bool framed, selected;
        public void Initialize(string text, Action callback, bool frame, bool current, int size)
        {
            framed = frame; selected = current;
            skin = PixelJournalUI.Art(transform, new Rect(0, 0, ((RectTransform)transform).rect.width, ((RectTransform)transform).rect.height), "UI/Pixel/PrimaryButton");
            PixelJournalUI.Stretch(skin.rectTransform); skin.sprite = Resources.LoadAll<Sprite>("UI/Pixel/" + (selected ? "PrimaryButton" : "SecondaryAction"))[0];
            skin.type = Image.Type.Sliced; skin.pixelsPerUnitMultiplier = 2;
            Label = PixelJournalUI.Text(transform, new Rect(), text, size, !framed && !selected, TextAnchor.MiddleCenter);
            PixelJournalUI.Stretch(Label.rectTransform); Label.rectTransform.offsetMin = new Vector2(7, 4); Label.rectTransform.offsetMax = new Vector2(-7, -4);
            targetGraphic = skin; transition = Transition.None; onClick.AddListener(() => callback?.Invoke()); Refresh();
        }
        public override void OnSubmit(BaseEventData data) { if (IsActive() && IsInteractable()) onClick.Invoke(); }
        public void Refresh() => DoStateTransition(currentSelectionState, true);
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (skin == null || Label == null) return;
            bool focus = IsInteractable() && ((!PixelTouch.HideFocus && (state == SelectionState.Selected || state == SelectionState.Highlighted)) || state == SelectionState.Pressed);
            skin.enabled = framed || selected || focus;
            skin.color = !IsInteractable() ? new Color32(145, 143, 132, 255) : state == SelectionState.Pressed ? new Color32(178, 182, 164, 255) : Color.white;
            Label.color = !IsInteractable() ? new Color32(133, 122, 99, 255) : skin.enabled ? PixelJournalUI.Ivory : PixelJournalUI.Ink;
            foreach (var text in inkTexts) text.color = Label.color;
            var border = skin.GetComponent<Outline>();
            if (border == null) { border = skin.gameObject.AddComponent<Outline>(); border.effectDistance = new Vector2(2, -2); border.effectColor = PixelJournalUI.Ivory; }
            border.enabled = focus;
        }
    }
    public sealed class PixelJournalScrollFocus : MonoBehaviour
    {
        public ScrollRect Scroll;
        GameObject last;
        void LateUpdate()
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current == last) return; last = current;
            if (current == null || Scroll == null || !current.transform.IsChildOf(Scroll.content)) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(Scroll.viewport, current.transform);
            float shift = bounds.min.y < Scroll.viewport.rect.yMin ? Scroll.viewport.rect.yMin - bounds.min.y : bounds.max.y > Scroll.viewport.rect.yMax ? Scroll.viewport.rect.yMax - bounds.max.y : 0;
            var pos = Scroll.content.anchoredPosition; pos.y = Mathf.Clamp(pos.y + shift, 0, Mathf.Max(0, Scroll.content.rect.height - Scroll.viewport.rect.height));
            Scroll.content.anchoredPosition = pos;
        }
    }
}
