using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Opt-in verification of panel geometry, clipping and child input.</summary>
    public sealed class PixelPanelSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string locale;
            public Rect safeArea;
            public string[] checks;
        }
        readonly List<string> checks = new List<string>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        PixelPanelGallery gallery;
        bool passed = true;

        public void Run(PixelPanelGallery target) { gallery = target; StartCoroutine(Verify()); }

        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            Canvas.ForceUpdateCanvases();
            Check(Screen.width >= Screen.height, "fixture is landscape");
            if (PixelButtonGallery.HasArgument("-sologym-smoke"))
                yield return CheckInteractions();
            gallery.ResetReview();
            for (int i = 0; i < 3; ++i) yield return null;
            Canvas.ForceUpdateCanvases();
            CheckLayout();
            yield return new WaitForEndOfFrame();
            foreach (var panel in gallery.Panels) CheckBorder(panel.Background);
            string path = Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture",
                "artifacts/visual/ContentPanel/gallery.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); }
            finally { Destroy(capture); }
            var report = new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_PANEL_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-quit-after-capture"))
                Application.Quit(passed ? 0 : 1);
        }

        IEnumerator CheckInteractions()
        {
            bool initialSpanish = gallery.Spanish;
            Check(Click(gallery.Action) && gallery.ActionCount == 1, "contained button receives pointer input through the panel");
            gallery.Action.Select();
            ExecuteEvents.Execute(gallery.Action.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(gallery.ActionCount == 2, "contained button receives keyboard submit");
            var group = gallery.Tall.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            Click(gallery.Action);
            Check(gallery.ActionCount == 2, "panel CanvasGroup can gate child interaction");
            group.interactable = true;
            Destroy(group);
            yield return null;

            var actionRect = (RectTransform)gallery.Action.transform;
            var position = actionRect.anchoredPosition;
            actionRect.anchoredPosition += new Vector2(gallery.Tall.Content.rect.width + 96, 0);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Check(!Click(gallery.Action) && gallery.ActionCount == 2, "clipped child cannot receive pointer input outside the content area");
            Check(gallery.Action.Background.canvasRenderer.cull, "content mask visually culls an out-of-bounds child");
            actionRect.anchoredPosition = position;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Check(!gallery.Action.Background.canvasRenderer.cull && Click(gallery.Action) && gallery.ActionCount == 3,
                "restoring the child to the content area restores visibility and pointer input");

            float oldWidth = gallery.Wide.Background.rectTransform.rect.width;
            Vector2 before = CheckBorder(gallery.Wide.Background);
            Check(Click(gallery.Resize) && gallery.Narrow, "resize action changes the panel width");
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Vector2 after = CheckBorder(gallery.Wide.Background);
            Check(gallery.Wide.Background.rectTransform.rect.width < oldWidth - 1, "panel center resizes with its layout");
            Check(Vector2.Distance(before, after) < 1.5f, "rendered corner dimensions survive a width change");
            CheckLayout();
            Check(Click(gallery.Copy) && !gallery.LongCopy, "copy changes without regenerating the panel");
            Check(Click(gallery.Locale) && gallery.Spanish != initialSpanish, "locale button updates live panel content");
            CheckLayout();
            Click(gallery.Copy);
            Check(gallery.LongCopy, "long content can be restored in the narrow panel");
            CheckLayout();
            gallery.SetLocale(initialSpanish);
        }

        Vector2 CheckBorder(Image image)
        {
            var mesh = image.canvasRenderer.GetMesh();
            var xs = new List<float>(); var ys = new List<float>();
            foreach (var vertex in mesh.vertices) { Unique(xs, vertex.x); Unique(ys, vertex.y); }
            xs.Sort(); ys.Sort();
            bool sliced = xs.Count == 4 && ys.Count == 4;
            Check(sliced, image.name + " renders nine-slice geometry");
            if (!sliced) return Vector2.zero;
            float expected = image.sprite.border.x / image.pixelsPerUnit;
            var corner = new Vector2(xs[1] - xs[0], ys[1] - ys[0]);
            Check(Mathf.Abs(corner.x - expected) < 1.5f && Mathf.Abs(corner.y - expected) < 1.5f
                && Mathf.Abs(xs[3] - xs[2] - expected) < 1.5f && Mathf.Abs(ys[3] - ys[2] - expected) < 1.5f,
                image.name + " preserves all four authored corner regions");
            return corner;
        }

        static void Unique(List<float> values, float value)
        {
            foreach (float item in values) if (Mathf.Abs(item - value) < .05f) return;
            values.Add(value);
        }

        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var panel in gallery.Panels)
            {
                Check(Contains(gallery.EffectiveSafeArea, Bounds((RectTransform)panel.transform)), panel.name + " stays in the safe area");
                Check(Contains(Bounds((RectTransform)panel.transform), Bounds(panel.Content)), panel.name + " retains interior padding");
                Check(!panel.Background.raycastTarget, panel.name + " is a decorative frame without an input blocker");
                foreach (var text in panel.Content.GetComponentsInChildren<Text>())
                {
                    Vector2 measure = PixelPrimaryButton.MeasureWrappedLabel(text);
                    Check(measure.x <= text.rectTransform.rect.width + 1.1f && measure.y <= text.rectTransform.rect.height + 1.1f,
                        panel.name + "/" + text.name + " fits live text without truncation");
                    Check(Contains(Bounds(panel.Content), Bounds(text.rectTransform)), panel.name + "/" + text.name + " stays in the content slot");
                }
            }
            foreach (var button in gallery.Buttons)
            {
                Rect bounds = Bounds((RectTransform)button.transform);
                Check(Contains(gallery.EffectiveSafeArea, bounds) && bounds.height >= 47,
                    button.name + " stays visible with a minimum 48px target");
                var measured = PixelPrimaryButton.MeasureWrappedLabel(button.Label);
                Check(measured.x <= button.Label.rectTransform.rect.width + 1.1f && measured.y <= button.Label.rectTransform.rect.height + 1.1f,
                    button.name + " fits its localized label");
            }
        }

        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f
            && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;

        bool Click(PixelPrimaryButton button)
        {
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = null;
            foreach (var hit in hits)
            {
                target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
                if (target != null) break;
            }
            if (target != button.gameObject) return false;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerExitHandler);
            return true;
        }

        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
