using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    public sealed class PixelActionSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string locale;
            public Rect safeArea;
            public string[] checks;
        }
        PixelActionGallery gallery;
        readonly List<string> checks = new List<string>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        bool passed = true;
        public void Run(PixelActionGallery target) { gallery = target; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) yield return KeyboardProbe();
            else if (PixelButtonGallery.HasArgument("-sologym-smoke")) yield return Interactions();
            gallery.ResetReview(); gallery.Back.Select();
            yield return null; Canvas.ForceUpdateCanvases();
            Check(gallery.Back.ShowsFocus && !gallery.Recovery.ShowsFocus, "only the selected Back action has a focus frame");
            Check(!gallery.Recovery.Background.enabled && gallery.Recovery.Underline.enabled, "unfocused recovery stays a quiet underlined text action");
            Check(gallery.Disabled.VisualState == "disabled" && !gallery.Disabled.Underline.enabled, "disabled text drops its active underline");
            Check(gallery.Pending.VisualState == "pending" && !gallery.Pending.IsInteractable(), "pending state stays visible and cannot activate");
            CheckLayout();
            yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture", "artifacts/visual/SecondaryAction/gallery.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); } finally { Destroy(capture); }
            var report = new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_ACTION_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")
                || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(passed ? 0 : 1);
        }
        IEnumerator Interactions()
        {
            Check(Click(gallery.Recovery, true), "the padded corner of the text action is a clickable target");
            Check(gallery.Recovering && gallery.RecoveryVisits == 1, "recovery switches the offline example once");
            Check(Selected(gallery.Email.Input), "recovery transfers focus to its email input");
            Check(Click(gallery.Back), "Back receives a native pointer raycast");
            Check(!gallery.Recovering && gallery.BackVisits == 1 && Selected(gallery.Recovery), "Back restores the sign-in example and focuses its recovery entry");
            Check(gallery.Email.Input.text == "hero@example.com", "Back/recovery preserve the entered example address");
            Check(gallery.Recovery.ShowsFocus, "text action has a visible frame when selected");
            gallery.Back.Select();
            Check(!gallery.Recovery.ShowsFocus && !gallery.Recovery.Background.enabled, "moving focus clears the previous text action frame");
            var pointer = Pointer(gallery.Back);
            ExecuteEvents.Execute(gallery.Back.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(gallery.Back.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(gallery.Back.VisualState == "pressed" && gallery.Back.Label.rectTransform.anchoredPosition.y == -2,
                "held pointer shows the pressed state with a small label offset");
            ExecuteEvents.Execute(gallery.Back.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(gallery.Back.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            Check(gallery.Back.Label.rectTransform.anchoredPosition.y == 0, "release restores the label position without drift");
            pointer.button = PointerEventData.InputButton.Right;
            ExecuteEvents.Execute(gallery.Back.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(gallery.BackVisits == 1, "right click cannot invoke Back");
            Submit(gallery.Recovery);
            Check(gallery.RecoveryVisits == 2 && gallery.Recovering, "native keyboard submit invokes recovery");
            Submit(gallery.Back);
            Check(!gallery.Recovering && gallery.BackVisits == 2, "native keyboard submit invokes Back");

            int blocked = 0;
            gallery.Disabled.onClick.AddListener(() => ++blocked);
            gallery.Pending.onClick.AddListener(() => ++blocked);
            Click(gallery.Disabled); Submit(gallery.Disabled); Click(gallery.Pending); Submit(gallery.Pending);
            Check(blocked == 0, "disabled and pending actions block both pointer and submit callbacks");
            var group = gallery.Normal.gameObject.AddComponent<CanvasGroup>(); group.interactable = false;
            yield return null;
            Click(gallery.Normal); Submit(gallery.Normal);
            Check(gallery.NormalInvocations == 0 && gallery.Normal.VisualState == "disabled", "CanvasGroup disables appearance and activation together");
            group.interactable = true; Destroy(group); yield return null;
            gallery.Normal.gameObject.SetActive(false); Submit(gallery.Normal);
            Check(gallery.NormalInvocations == 0, "inactive actions ignore submit");
            gallery.Normal.gameObject.SetActive(true);
            Click(gallery.Normal); Click(gallery.Normal); Submit(gallery.Normal);
            Check(gallery.NormalInvocations == 1 && gallery.Normal.IsPending, "synchronous pending gate blocks repeat pointer/submit activation");
            gallery.Normal.SetLabel("Updated after pending"); gallery.Normal.SetPending(false);
            Check(gallery.Normal.Label.text == "Updated after pending" && gallery.Normal.IsInteractable(), "completion restores the latest caller label and interaction");
            gallery.Normal.SetPending(true, "Waiting"); gallery.Normal.interactable = false; gallery.Normal.SetPending(false);
            Check(!gallery.Normal.IsInteractable() && gallery.Normal.VisualState == "disabled", "ending pending does not override an explicit disabled gate");
            gallery.Normal.interactable = true;
            gallery.Normal.SetLabel("<b>literal</b>");
            Check(!gallery.Normal.Label.supportRichText && gallery.Normal.Label.text == "<b>literal</b>", "action labels keep markup literal");
            gallery.ResetReview();
            gallery.Normal.Select();
            Check(gallery.Normal.GetComponent<PixelFieldTabNavigation>().Move(false) && Selected(gallery.LongCaption), "Tab skips disabled and pending actions");
            Check(gallery.LongCaption.GetComponent<PixelFieldTabNavigation>().Move(true) && Selected(gallery.Normal), "Shift+Tab skips the same unavailable actions in reverse");
            gallery.Back.Select();
            ExecuteEvents.Execute(gallery.Back.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right }, ExecuteEvents.moveHandler);
            Check(Selected(gallery.Primary), "native arrow navigation can leave a secondary action");
            bool locale = gallery.Spanish; Click(gallery.LongCaption);
            Check(gallery.Spanish != locale, "long text action changes the live review locale");
            CheckLayout(); gallery.SetLocale(locale);
            gallery.Recovery.Select(); gallery.Recovery.gameObject.SetActive(false); gallery.Recovery.gameObject.SetActive(true);
            Check(!gallery.Recovery.ShowsFocus, "closing a selected text action does not leave a stale focus frame");
        }
        IEnumerator KeyboardProbe()
        {
            string marker = PixelButtonGallery.Argument("-sologym-keyboard-probe", "");
            bool locale = gallery.Spanish;
            gallery.Back.Select(); yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready");
            float until = Time.realtimeSinceStartup + 25;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; ++i) yield return null;
            Check(File.Exists(marker + ".done"), "OS keyboard driver completed");
            Check(gallery.RecoveryVisits == 1, "OS Tab and Enter invoke the text recovery action once");
            Check(gallery.BackVisits == 1 && !gallery.Recovering, "OS Tab and Enter return through the framed Back action");
            Check(gallery.Spanish != locale && Selected(gallery.LongCaption), "OS Shift+Tab/Tab skip unavailable states and activate the long text action");
            gallery.SetLocale(locale);
        }
        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var action in gallery.Actions)
            {
                Rect area = Bounds((RectTransform)action.transform);
                Check(Contains(gallery.EffectiveSafeArea, area), action.name + " stays inside the safe area");
                Check(area.height >= 47 && area.width >= 47, action.name + " keeps a 48px or larger captured hit area");
                var measured = PixelPrimaryButton.MeasureWrappedLabel(action.Label);
                Check(measured.x <= action.Label.rectTransform.rect.width + 1.1f && measured.y <= action.Label.rectTransform.rect.height + 1.1f,
                    action.name + " fits its localized caption without truncation");
                Check(action.Label.fontSize >= 24, action.name + " keeps a readable font size");
                var panel = action.GetComponentInParent<PixelContentPanel>();
                Check(panel != null && Contains(Bounds(panel.Content), area), action.name + " is not clipped by its panel content");
            }
            Check(gallery.Back.Background.sprite != gallery.Primary.Background.sprite, "secondary and primary have distinct sprite skins");
            Check(gallery.Back.Background.type == Image.Type.Sliced, "secondary corners retain nine-slice geometry");
            foreach (var text in gallery.GetComponentsInChildren<Text>())
            {
                if (text.GetComponentInParent<InputField>() != null || text.text.Length == 0) continue;
                var measured = PixelPrimaryButton.MeasureWrappedLabel(text);
                Check(measured.x <= text.rectTransform.rect.width + 1.1f && measured.y <= text.rectTransform.rect.height + 1.1f,
                    text.name + " has enough room in the review composition");
            }
        }
        static void Submit(Selectable action) => ExecuteEvents.Execute(action.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        static bool Selected(Selectable action) => EventSystem.current.currentSelectedGameObject == action.gameObject;
        static PointerEventData Pointer(Selectable action, bool corner = false)
        {
            var rect = (RectTransform)action.transform;
            var point = corner ? new Vector2(rect.rect.xMin + 3, rect.rect.yMin + 3) : rect.rect.center;
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(point)) };
        }
        bool Click(Selectable action, bool corner = false)
        {
            Canvas.ForceUpdateCanvases(); var pointer = Pointer(action, corner);
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = null;
            foreach (var hit in hits) { target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (target != null) break; }
            if (target != action.gameObject) return false;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerExitHandler); return true;
        }
        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f
            && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;
        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
