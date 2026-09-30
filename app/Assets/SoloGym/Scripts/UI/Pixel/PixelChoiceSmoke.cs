using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    public sealed class PixelChoiceSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string locale;
            public Rect safeArea;
            public string[] checks;
        }
        PixelChoiceGallery gallery;
        readonly List<string> checks = new List<string>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        bool passed = true;
        public void Run(PixelChoiceGallery target) { gallery = target; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) yield return KeyboardProbe();
            else if (PixelButtonGallery.HasArgument("-sologym-smoke")) yield return Interactions();
            gallery.ResetReview(); gallery.Difficulty.Option("light").Select();
            yield return null; Canvas.ForceUpdateCanvases(); CheckLayout();
            Check(gallery.Difficulty.Option("light").ShowsFocus && gallery.Difficulty.Value == "medium", "focus and selected value are visually and behaviorally distinct");
            yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture", "artifacts/visual/ChoiceControl/gallery.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); } finally { Destroy(capture); }
            var report = new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true); File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_CHOICE_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")
                || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(passed ? 0 : 1);
        }
        IEnumerator Interactions()
        {
            var control = gallery.Difficulty; var easy = control.Option("light"); var medium = control.Option("medium"); var hard = control.Option("hard");
            Check(OneSelected(control) && control.Value == "medium", "initial binding selects exactly the requested value");
            Check(Click(easy, true) && control.Value == "light" && gallery.DifficultyChanges == 1, "the full padded target selects Easy and emits one change");
            Click(easy); Submit(easy);
            Check(OneSelected(control) && control.Value == "light" && gallery.DifficultyChanges == 1, "clicking/submitting the selected option cannot clear it or duplicate a change");
            medium.Select();
            Check(control.Value == "light" && medium.ShowsFocus && easy.Checkmark.enabled, "focus can move without silently changing difficulty");
            var pointer = Pointer(medium); ExecuteEvents.Execute(medium.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(medium.VisualState == "pressed", "held pointer has a distinct press state");
            ExecuteEvents.Execute(medium.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            pointer.button = PointerEventData.InputButton.Right; ExecuteEvents.Execute(medium.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(control.Value == "light", "right click cannot change a choice");
            Submit(medium); Check(control.Value == "medium" && gallery.DifficultyChanges == 2, "native keyboard submit selects a new value once");
            control.SetOptionInteractable("hard", false); Click(hard); Submit(hard);
            Check(!hard.TryChoose() && control.Value == "medium", "unavailable options block pointer, submit and accessible activation");
            medium.Select(); Move(medium, MoveDirection.Right);
            Check(Selected(gallery.NextPhase), "arrow navigation skips an unavailable option");
            medium.Select();
            Check(medium.GetComponent<PixelFieldTabNavigation>().Move(false) && Selected(gallery.NextPhase), "Tab uses the same availability-aware order");
            control.SetOptionInteractable("hard", true);
            easy.Select(); Move(easy, MoveDirection.Down);
            Check(Selected(hard) && control.Value == "medium", "Down follows the wrapped row while leaving selection unchanged");
            Move(hard, MoveDirection.Up);
            Check(Selected(easy), "Up returns to the same column in the previous row");
            int before = gallery.DifficultyChanges; control.SetValueWithoutNotify("hard");
            Check(control.Value == "hard" && OneSelected(control) && gallery.DifficultyChanges == before, "binding existing data updates the whole group without issuing a user event");
            control.SetInteractable(false); Click(easy); Submit(easy);
            Check(control.Value == "hard" && hard.Checkmark.enabled && hard.AccessibilityNode.state.HasFlag(AccessibilityState.Selected)
                && hard.AccessibilityNode.state.HasFlag(AccessibilityState.Disabled), "disabled selection retains its marker and accessible selected/disabled state");
            control.SetInteractable(true);
            var gate = control.transform.parent.gameObject.AddComponent<CanvasGroup>(); gate.interactable = false; yield return null;
            Check(!easy.TryChoose() && control.Value == "hard", "an inherited CanvasGroup also gates selection");
            gate.interactable = true; Destroy(gate); yield return null;
            easy.Select(); control.gameObject.SetActive(false);
            Check(!easy.TryChoose() && !easy.AccessibilityNode.isActive, "hidden choices cannot activate and are hidden from accessibility");
            control.gameObject.SetActive(true); yield return null;
            Check(control.Value == "hard" && OneSelected(control) && !easy.ShowsFocus, "reopening keeps the choice without stale keyboard focus");
            bool rejected = false; try { control.SetValueWithoutNotify("unknown"); } catch (ArgumentException) { rejected = true; }
            Check(rejected && control.Value == "hard", "unknown stable IDs are rejected without losing the current selection");
            before = gallery.DifficultyChanges;
            gallery.Appearance.Option("fat").TryChoose();
            Check(gallery.Appearance.Value == "fat" && control.Value == "hard" && gallery.DifficultyChanges == before, "body appearance cannot prescribe or change difficulty");
            bool locale = gallery.Spanish; gallery.SetLocale(!locale);
            Check(control.Value == "hard" && gallery.Appearance.Value == "fat" && gallery.DifficultyChanges == before, "localization preserves stable values and emits no selection event");
            CheckLayout(); gallery.SetLocale(locale);

            // These are UI fixtures, not live training prescriptions or reward issuance.
            for (int i = 0; i < 4; ++i)
            {
                var phase = gallery.CurrentPhase; int logged = gallery.FictionalLoggedSets;
                string id = control.Value == "light" ? "medium" : "light";
                Click(control.Option(id));
                Check(control.Value == id && gallery.CurrentPhase == phase && gallery.FictionalLoggedSets == logged,
                    "difficulty can change during " + phase + " without restarting the example or changing its recorded work");
                gallery.AdvancePhase();
            }
            var rect = (RectTransform)gallery.Appearance.transform; Vector2 old = rect.sizeDelta;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 960); gallery.Appearance.RefreshLayout();
            Check(gallery.Appearance.Columns == 4 && gallery.Appearance.PreferredHeight == 64, "wide choice controls place all four options in one row");
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200); gallery.Appearance.RefreshLayout();
            gallery.Appearance.SetLabel("muscular", "Long localized appearance caption");
            Check(gallery.Appearance.Columns == 1 && gallery.Appearance.PreferredHeight > 292, "narrow choices wrap and request extra height for a long label");
            var tall = gallery.Appearance.Option("muscular"); var measured = PixelPrimaryButton.MeasureWrappedLabel(tall.Label);
            Check(measured.y <= tall.Label.rectTransform.rect.height + 1.1f, "wrapped labels retain their readable font and full text");
            rect.sizeDelta = old; gallery.SetLocale(locale); gallery.Appearance.RefreshLayout();
            var temp = new AccessibilityHierarchy(); control.BindAccessibility(temp, "Difficulty"); temp.Clear();
            control.SetLabel("light", "Easy");
            Check(easy.AccessibilityNode == null, "a cleared screen hierarchy safely detaches surviving options");
            control.BindAccessibility(gallery.Hierarchy, "Difficulty"); gallery.SetLocale(locale);
        }
        IEnumerator KeyboardProbe()
        {
            string marker = PixelButtonGallery.Argument("-sologym-keyboard-probe", "");
            gallery.Difficulty.Option("medium").Select(); yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready"); float until = Time.realtimeSinceStartup + 25;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; ++i) yield return null;
            Check(File.Exists(marker + ".done"), "OS keyboard driver completed");
            Check(gallery.Difficulty.Value == "light" && gallery.DifficultyChanges == 3, "real arrows/Enter choose difficulty without duplicate selection events");
            Check(gallery.Appearance.Value == "fat" && gallery.AppearanceChanges == 2, "real keyboard navigation independently selects body appearance");
            Check(gallery.CurrentPhase == PixelChoiceGallery.Phase.Pause && gallery.FictionalLoggedSets == 2,
                "Tab/Shift+Tab cross the fixture while difficulty remains editable in an active and paused routine");
        }
        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases(); gallery.Hierarchy.RefreshNodeFrames();
            foreach (var control in new[] { gallery.Difficulty, gallery.Appearance })
            {
                Check(OneSelected(control), "single-choice group has exactly one selected option");
                foreach (var option in control.Options)
                {
                    var area = PixelChoiceOption.ScreenBounds((RectTransform)option.transform);
                    Check(Contains(gallery.EffectiveSafeArea, area), option.Id + " is inside the safe area");
                    Check(area.height >= 47 && area.width >= 47, option.Id + " retains a 48px captured target");
                    Check(Contains(PixelChoiceOption.ScreenBounds((RectTransform)control.transform), area), option.Id + " fits inside its allocated group");
                    Check(option.Checkmark.enabled == option.isOn && option.Background.sprite != null, option.Id + " communicates selection with art and a checkmark");
                    Check(option.AccessibilityNode != null && option.AccessibilityNode.label == option.Label.text
                        && option.AccessibilityNode.state.HasFlag(AccessibilityState.Selected) == option.isOn, option.Id + " exposes its localized native accessibility selection");
                }
            }
            foreach (var text in gallery.GetComponentsInChildren<Text>())
            {
                var measured = PixelPrimaryButton.MeasureWrappedLabel(text);
                Check(measured.x <= text.rectTransform.rect.width + 1.1f && measured.y <= text.rectTransform.rect.height + 1.1f,
                    text.name + " fits localized text without truncation");
            }
        }
        static bool OneSelected(PixelChoiceControl control) { int count = 0; foreach (var option in control.Options) if (option.isOn) ++count; return count == 1 && control.Option(control.Value).isOn; }
        static bool Selected(Selectable option) => EventSystem.current.currentSelectedGameObject == option.gameObject;
        static void Submit(Selectable option) => ExecuteEvents.Execute(option.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        static void Move(Selectable option, MoveDirection dir) => ExecuteEvents.Execute(option.gameObject, new AxisEventData(EventSystem.current) { moveDir = dir }, ExecuteEvents.moveHandler);
        static PointerEventData Pointer(Selectable option, bool corner = false)
        {
            var rect = (RectTransform)option.transform; var point = corner ? new Vector2(rect.rect.xMin + 3, rect.rect.yMin + 3) : rect.rect.center;
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(point)) };
        }
        bool Click(Selectable option, bool corner = false)
        {
            Canvas.ForceUpdateCanvases(); var pointer = Pointer(option, corner); hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = null;
            foreach (var hit in hits) { target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (target != null) break; }
            if (target != option.gameObject) return false;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler); ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerExitHandler); return true;
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f
            && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;
        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
