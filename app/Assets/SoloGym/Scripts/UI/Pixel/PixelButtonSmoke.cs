using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Opt-in player verification through real uGUI event handlers and raycasts.</summary>
    public sealed class PixelButtonSmoke : MonoBehaviour
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
        readonly List<RaycastResult> raycasts = new List<RaycastResult>();
        PixelButtonGallery gallery;
        bool passed = true;
        string path;

        public void Run(PixelButtonGallery target)
        {
            gallery = target;
            path = Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture",
                "artifacts/visual/pixel-primary-button/gallery.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            StartCoroutine(VerifyAndCapture());
        }

        IEnumerator VerifyAndCapture()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            Check(Screen.width >= Screen.height, "fixture is landscape");
            Check(EventSystem.current != null, "uGUI EventSystem is available");
            if (PixelButtonGallery.HasArgument("-sologym-smoke"))
                yield return VerifyInteractions();

            gallery.ResetDemo();
            gallery.RestoreSampleStates();
            for (int i = 0; i < 4; ++i) yield return null;
            Canvas.ForceUpdateCanvases();
            CheckState(gallery.Normal, "normal");
            CheckState(gallery.Highlighted, "highlighted");
            CheckState(gallery.Pressed, "pressed");
            CheckState(gallery.Selected, "selected");
            CheckState(gallery.Disabled, "disabled");
            CheckState(gallery.Loading, "loading");
            foreach (var button in gallery.Buttons) CheckGeometry(button);

            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { Destroy(image); }
            var report = new Report
            {
                passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea,
                checks = checks.ToArray()
            };
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_BUTTON_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-quit-after-capture"))
                Application.Quit(passed ? 0 : 1);
        }

        IEnumerator VerifyInteractions()
        {
            int clicks = 0;
            UnityEngine.Events.UnityAction listener = () => ++clicks;
            gallery.Normal.onClick.AddListener(listener);
            Check(Click(gallery.Normal), "raycast finds the button through its live label");
            Check(clicks == 1, "left pointer click activates once");
            Click(gallery.Normal, PointerEventData.InputButton.Right);
            Check(clicks == 1, "right pointer click does not activate");

            gallery.Normal.Select();
            ExecuteEvents.Execute(gallery.Normal.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(clicks == 2, "keyboard submit activates once");
            yield return null;
            yield return null;

            ExecuteEvents.Execute(gallery.Normal.gameObject,
                new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right, moveVector = Vector2.right },
                ExecuteEvents.moveHandler);
            Check(EventSystem.current.currentSelectedGameObject == gallery.Highlighted.gameObject,
                "native right-arrow navigation moves keyboard focus to the adjacent button");

            var pointer = Pointer(gallery.Normal);
            ExecuteEvents.Execute(gallery.Normal.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(gallery.Normal.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            CheckState(gallery.Normal, "pressed");
            ExecuteEvents.Execute(gallery.Normal.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(gallery.Normal.gameObject, pointer, ExecuteEvents.pointerExitHandler);

            int disabledClicks = 0;
            UnityEngine.Events.UnityAction disabledListener = () => ++disabledClicks;
            gallery.Disabled.onClick.AddListener(disabledListener);
            Click(gallery.Disabled);
            ExecuteEvents.Execute(gallery.Disabled.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(disabledClicks == 0, "disabled button blocks pointer and keyboard submission");
            gallery.Disabled.onClick.RemoveListener(disabledListener);

            string label = gallery.Normal.Label.text;
            gallery.Normal.SetLoading(true, "Loading...");
            Click(gallery.Normal);
            ExecuteEvents.Execute(gallery.Normal.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(clicks == 2 && gallery.Normal.IsLoading, "loading button blocks pointer and keyboard submission");
            gallery.Normal.SetLoading(false);
            Check(gallery.Normal.Label.text == label && gallery.Normal.interactable,
                "ending loading restores the original label and enabled state");
            gallery.Normal.interactable = false;
            gallery.Normal.SetLoading(true, "Loading...");
            gallery.Normal.SetLoading(false);
            Check(!gallery.Normal.interactable, "loading completion preserves a previously disabled button");
            gallery.Normal.interactable = true;

            var group = gallery.Normal.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            Click(gallery.Normal);
            Check(clicks == 2, "parent UI interaction gate blocks activation");
            group.interactable = true;
            group.blocksRaycasts = false;
            Check(!Click(gallery.Normal) && clicks == 2, "raycast gate blocks pointer activation");
            group.blocksRaycasts = true;
            Destroy(group);
            yield return null;

            gallery.ResetDemo();
            Click(gallery.Demo);
            Click(gallery.Demo);
            ExecuteEvents.Execute(gallery.Demo.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(gallery.DemoCount == 1 && gallery.Demo.IsLoading,
                "synchronous loading prevents a second pointer or submit activation");
            Click(gallery.Complete);
            Check(!gallery.Demo.IsLoading, "completion releases the loading gate");
            Click(gallery.Demo);
            Check(gallery.DemoCount == 2, "button can activate again after completion");

            bool initialLocale = gallery.Spanish;
            gallery.SetLocale(!initialLocale);
            Check(gallery.Demo.IsLoading, "locale changes preserve the current loading gate");
            Click(gallery.Complete);
            Check(gallery.Demo.Label.text == (gallery.Spanish ? "Registrar serie" : "Log a set"),
                "loading restores the current localized label");
            foreach (var button in gallery.Buttons) CheckGeometry(button);
            gallery.SetLocale(initialLocale);
            gallery.Normal.onClick.RemoveListener(listener);

            // A taller button must keep its requested type size when a caption fits by wrapping.
            var wrapped = PixelPrimaryButton.Create(gallery.Normal.transform.parent, "");
            ((RectTransform)wrapped.transform).sizeDelta = new Vector2(320, 112);
            wrapped.SetFontSize(28);
            foreach (string caption in new[] { "Preparar mi próxima aventura", "Prepare my next adventure" })
            {
                wrapped.SetLabel(caption);
                Canvas.ForceUpdateCanvases();
                bool needsWrap = wrapped.Label.preferredWidth > wrapped.Label.rectTransform.rect.width;
                var measured = PixelPrimaryButton.MeasureWrappedLabel(wrapped.Label);
                Check(needsWrap && wrapped.Label.cachedTextGeneratorForLayout.lineCount >= 2,
                    "long localized caption uses multiple lines: " + caption);
                Check(wrapped.Label.fontSize == 28 && measured.x <= wrapped.Label.rectTransform.rect.width + .1f
                    && measured.y <= wrapped.Label.rectTransform.rect.height + .1f,
                    "wrapped caption retains its largest requested font size: " + caption);
            }
            wrapped.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            wrapped.SetLabel(new string('W', 64));
            Check(PixelPrimaryButton.MeasureWrappedLabel(wrapped.Label).x > wrapped.Label.rectTransform.rect.width,
                "label measurement detects horizontal overflow when text cannot wrap");
            Destroy(wrapped.gameObject);
            yield return null;
        }

        PointerEventData Pointer(PixelPrimaryButton button, PointerEventData.InputButton mouse = PointerEventData.InputButton.Left)
        {
            var rect = (RectTransform)button.transform;
            return new PointerEventData(EventSystem.current)
            {
                button = mouse,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                pointerId = mouse == PointerEventData.InputButton.Left ? -1 : -2,
                clickCount = 1,
                eligibleForClick = true
            };
        }

        bool Click(PixelPrimaryButton button, PointerEventData.InputButton mouse = PointerEventData.InputButton.Left)
        {
            Canvas.ForceUpdateCanvases();
            var data = Pointer(button, mouse);
            raycasts.Clear();
            EventSystem.current.RaycastAll(data, raycasts);
            GameObject target = null;
            foreach (var hit in raycasts)
            {
                var candidate = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
                if (candidate == null) continue;
                target = candidate;
                break;
            }
            if (target != button.gameObject) return false;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerExitHandler);
            return true;
        }

        void CheckGeometry(PixelPrimaryButton button)
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Rect bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Rect safe = gallery.EffectiveSafeArea;
            const float tolerance = 1.1f;
            Check(bounds.xMin >= safe.xMin - tolerance && bounds.yMin >= safe.yMin - tolerance
                && bounds.xMax <= safe.xMax + tolerance && bounds.yMax <= safe.yMax + tolerance,
                button.name + " stays inside the safe area");
            Check(bounds.width >= 48 - tolerance && bounds.height >= 48 - tolerance,
                button.name + " retains a 48px minimum pointer target");
            Check(button.Background.sprite != null && button.Background.type == Image.Type.Sliced,
                button.name + " uses a sliced sprite background");
            var measured = PixelPrimaryButton.MeasureWrappedLabel(button.Label);
            Check(measured.x <= button.Label.rectTransform.rect.width + tolerance
                && measured.y <= button.Label.rectTransform.rect.height + tolerance,
                button.name + " fits its live localized label");
        }

        void CheckState(PixelPrimaryButton button, string expected)
        {
            Check(string.Equals(button.VisualState, expected, StringComparison.OrdinalIgnoreCase),
                button.name + " presents " + expected + " (actual: " + button.VisualState + ")");
        }

        void Check(bool result, string label)
        {
            passed &= result;
            checks.Add((result ? "PASS " : "FAIL ") + label);
        }
    }
}
