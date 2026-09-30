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
    public sealed class PixelNavigationSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public int screenWidth, screenHeight; public string locale; public Rect safeArea; public string[] checks; }
        PixelNavigationGallery gallery;
        readonly List<string> checks = new List<string>();
        bool passed = true;
        public void Run(PixelNavigationGallery target) { gallery = target; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) yield return KeyboardProbe();
            else if (PixelButtonGallery.HasArgument("-sologym-smoke")) yield return Interactions();
            gallery.ResetReview(); gallery.Navigation.Tab("workouts").Select(); yield return null; Canvas.ForceUpdateCanvases(); CheckLayout();
            Check(gallery.Navigation.Tab("workouts").ShowsFocus && gallery.Navigation.CurrentId == "home", "focus differs from the current destination");
            yield return new WaitForEndOfFrame(); Capture(CapturePath);
            string json = JsonUtility.ToJson(new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() }, true);
            File.WriteAllText(Path.ChangeExtension(CapturePath, ".smoke.json"), json + "\n"); Debug.Log("SOLOGYM_PIXEL_NAVIGATION_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe") || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(passed ? 0 : 1);
        }
        string CapturePath => Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture", "artifacts/visual/Navigation/gallery.png"));
        static void Capture(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); var image = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, image.EncodeToPNG()); } finally { Destroy(image); }
        }
        IEnumerator StateCapture(string name)
        {
            if (!PixelButtonGallery.HasArgument("-sologym-capture-states")) yield break;
            yield return new WaitForEndOfFrame(); Capture(Path.Combine(Path.GetDirectoryName(CapturePath), "states", name + ".png"));
        }
        IEnumerator Interactions()
        {
            var bar = gallery.Navigation; var home = bar.Tab("home"); var workouts = bar.Tab("workouts"); var dungeon = bar.Tab("dungeon"); var fasting = bar.Tab("fasting");
            Check(gallery.Profile.Value == "adult-off" && !fasting.gameObject.activeSelf, "adult fasting is off and omitted by default");
            Check(CurrentCount() == 1 && home.IsCurrent, "the initial confirmed route is the only current destination");
            Check(Click(workouts, true) && bar.CurrentId == "workouts" && gallery.ContentId == "workouts" && gallery.NavigationRequests == 1,
                "pointer activation including outer padding requests and confirms the destination");
            Click(workouts); Submit(workouts);
            Check(gallery.NavigationRequests == 1, "reselecting the current route does not repeat navigation");
            gallery.RejectRequests = true; Submit(dungeon);
            Check(bar.CurrentId == "workouts" && gallery.ContentId == "workouts", "rejected navigation leaves both current marker and content unchanged"); gallery.RejectRequests = false;
            var pointer = Pointer(dungeon); ExecuteEvents.Execute(dungeon.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(dungeon.VisualState == "pressed", "pointer down has a distinct pressed appearance"); ExecuteEvents.Execute(dungeon.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            int requests = gallery.NavigationRequests; pointer.button = PointerEventData.InputButton.Right; ExecuteEvents.Execute(dungeon.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(gallery.NavigationRequests == requests, "right click does not request navigation");
            Submit(dungeon); Check(bar.CurrentId == "dungeon" && gallery.ContentId == "dungeon", "native submit confirms a different route");
            requests = gallery.NavigationRequests; bar.SetCurrentWithoutNotify("home");
            Check(gallery.NavigationRequests == requests && home.IsCurrent && CurrentCount() == 1, "binding confirmed current state is silent and singular");
            gallery.ResetReview(); bar.SetTabInteractable("workouts", false); Click(workouts); Submit(workouts);
            Check(!workouts.TryNavigate() && bar.CurrentId == "home" && gallery.NavigationRequests == 0, "disabled destinations reject all activation paths");
            home.Select(); Move(home, MoveDirection.Right); Check(Selected(dungeon), "arrows skip a disabled destination");
            home.Select(); Check(home.GetComponent<PixelFieldTabNavigation>().Move(false) && Selected(dungeon), "Tab also skips disabled destinations");
            bar.SetTabInteractable("workouts", true);
            bar.SetInteractable(false); Submit(dungeon); Check(!dungeon.TryNavigate() && home.CurrentMarker.enabled && home.AccessibilityNode.state.HasFlag(AccessibilityState.Disabled), "the whole bar can wait for routing while preserving current state");
            yield return StateCapture("disabled"); bar.SetInteractable(true);
            var inherited = bar.transform.parent.gameObject.AddComponent<CanvasGroup>(); inherited.interactable = false; yield return null;
            Check(!dungeon.TryNavigate() && bar.CurrentId == "home", "inherited CanvasGroup gates also prevent navigation"); inherited.interactable = true; Destroy(inherited); yield return null;

            foreach (string profile in new[] { "unknown", "teen", "adult-off", "adult-on" })
            {
                gallery.SetReviewProfile(profile); bool allowed = profile == "adult-on";
                Check(fasting.gameObject.activeSelf == allowed && fasting.AccessibilityNode.isActive == allowed, profile + " visibility matches native accessibility availability");
                Check(bar.Columns == (allowed ? 4 : 3), profile + " reflows without an empty fasting slot");
                if (!allowed)
                {
                    requests = gallery.NavigationRequests; Submit(fasting); Check(!fasting.TryNavigate() && gallery.NavigationRequests == requests, profile + " rejects stale references to hidden fasting");
                    dungeon.Select(); Move(dungeon, MoveDirection.Right); Check(Selected(gallery.Locale), profile + " exits the bar without focusing hidden fasting");
                    Check(gallery.Locale.GetComponent<PixelFieldTabNavigation>().Move(true) && Selected(dungeon), profile + " reverse traversal returns to the last visible destination");
                }
                CheckLayout(); yield return StateCapture(profile);
            }
            Click(fasting); fasting.Select(); requests = gallery.NavigationRequests;
            gallery.SetReviewProfile("teen");
            Check(bar.CurrentId == "home" && gallery.ContentId == "home" && CurrentCount() == 1 && !fasting.IsCurrent,
                "losing fasting eligibility clears the old content and binds the caller's safe fallback");
            Check(Selected(home) && gallery.NavigationRequests == requests, "hiding a focused current tab rescues focus without issuing a second navigation");
            bool rejected = false; try { bar.SetCurrentWithoutNotify("fasting"); } catch (ArgumentException) { rejected = true; }
            Check(rejected && bar.CurrentId == "home", "hidden routes cannot become current via model binding");
            rejected = false; try { bar.BindVisibleRoutes(new[] { "home", "bogus" }, "home"); } catch (ArgumentException) { rejected = true; }
            Check(rejected && bar.Columns == 3 && !fasting.gameObject.activeSelf, "invalid visible-route updates are rejected before any mutation");
            rejected = false; try { bar.BindVisibleRoutes(new[] { "workouts", "dungeon" }, "home"); } catch (ArgumentException) { rejected = true; }
            Check(rejected && bar.CurrentId == "home" && home.gameObject.activeSelf, "removing the current route requires an explicit visible fallback");
            bool locale = gallery.Spanish; gallery.SetLocale(!locale); CheckLayout();
            Check(bar.CurrentId == "home" && !fasting.gameObject.activeSelf, "localization preserves current route and age/opt-in visibility"); gallery.SetLocale(locale);
            gallery.SetReviewProfile("adult-on"); var rect = (RectTransform)bar.transform; var oldSize = rect.sizeDelta;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 420); bar.RefreshLayout();
            Check(bar.Columns == 2 && bar.PreferredHeight >= 140, "narrow navigation wraps into two rows with a reported height");
            home.Select(); Move(home, MoveDirection.Down); Check(Selected(dungeon) && bar.CurrentId == "home", "vertical navigation follows the wrapped column without committing");
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200); bar.SetLabel("workouts", "Workout history and routine planning");
            Check(bar.Columns == 1 && bar.PreferredHeight > 292, "long localized labels request extra height without shrinking text");
            var measured = PixelPrimaryButton.MeasureWrappedLabel(workouts.Label);
            Check(measured.y <= workouts.Label.rectTransform.rect.height + 1.1f, "wrapped navigation caption is not clipped"); rect.sizeDelta = oldSize; gallery.SetLocale(locale); bar.RefreshLayout();
            bar.gameObject.SetActive(false); Check(!bar.AccessibilityNode.isActive && !home.AccessibilityNode.isActive && !home.TryNavigate(), "hidden bars disappear from accessibility and cannot navigate");
            bar.gameObject.SetActive(true); Check(bar.CurrentId == "home" && !dungeon.ShowsFocus, "reopening retains current state without stale focus");
            var temp = new AccessibilityHierarchy(); bar.BindAccessibility(temp, "Navigation"); temp.Clear(); bar.SetLabel("home", "Home");
            Check(home.AccessibilityNode == null, "cleared accessibility hierarchies detach safely"); bar.BindAccessibility(gallery.Hierarchy, "Navigation"); gallery.SetLocale(locale);
        }
        IEnumerator KeyboardProbe()
        {
            string marker = PixelButtonGallery.Argument("-sologym-keyboard-probe", ""); gallery.Profile.Option("adult-off").Select(); yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready"); float until = Time.realtimeSinceStartup + 25;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; ++i) yield return null;
            Check(File.Exists(marker + ".done"), "OS keyboard driver completed");
            Check(gallery.NavigationRequests == 3, "real arrows/Tab/Enter navigate Workouts, Dungeon and Fasting once each");
            Check(gallery.Profile.Value == "teen" && gallery.Navigation.CurrentId == "home" && gallery.ContentId == "home" && !gallery.Navigation.Tab("fasting").gameObject.activeSelf,
                "real Shift+Tab and profile selection remove fasting and clear its current content");
            Check(gallery.Spanish, "real keyboard traversal reaches the locale action without changing stable route IDs");
        }
        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases(); gallery.Hierarchy.RefreshNodeFrames(); int visible = 0;
            foreach (var tab in gallery.Navigation.Tabs)
            {
                if (!tab.gameObject.activeSelf) continue; ++visible; var bounds = PixelChoiceOption.ScreenBounds((RectTransform)tab.transform);
                Check(Contains(gallery.EffectiveSafeArea, bounds) && bounds.width >= 47 && bounds.height >= 47, tab.Id + " has a safe 48px-or-larger target");
                Check(Contains(PixelChoiceOption.ScreenBounds((RectTransform)gallery.Navigation.transform), bounds), tab.Id + " fits its allocated navigation region");
                Check(tab.CurrentMarker.enabled == tab.IsCurrent && tab.AccessibilityNode.role == AccessibilityRole.TabButton
                    && tab.AccessibilityNode.state.HasFlag(AccessibilityState.Selected) == tab.IsCurrent && tab.AccessibilityNode.label == tab.Label.text, tab.Id + " has a localized tab role and a distinct current marker");
            }
            Check(visible == (gallery.ReviewFastingEnabled ? 4 : 3) && CurrentCount() == 1 && gallery.Navigation.AccessibilityNode.role == AccessibilityRole.TabBar, "exactly one visible route is current in the native tab bar");
            foreach (var text in gallery.GetComponentsInChildren<Text>())
            {
                if (!text.enabled || string.IsNullOrEmpty(text.text)) continue; var size = PixelPrimaryButton.MeasureWrappedLabel(text);
                Check(size.x <= text.rectTransform.rect.width + 1.1f && size.y <= text.rectTransform.rect.height + 1.1f, text.name + " fits its localized caption");
            }
        }
        int CurrentCount() { int count = 0; foreach (var tab in gallery.Navigation.Tabs) if (tab.gameObject.activeSelf && tab.IsCurrent) ++count; return count; }
        static bool Selected(Selectable control) => EventSystem.current.currentSelectedGameObject == control.gameObject;
        static void Submit(Selectable control) => ExecuteEvents.Execute(control.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        static void Move(Selectable control, MoveDirection dir) => ExecuteEvents.Execute(control.gameObject, new AxisEventData(EventSystem.current) { moveDir = dir }, ExecuteEvents.moveHandler);
        static PointerEventData Pointer(Selectable control, bool edge = false)
        {
            var rect = (RectTransform)control.transform; var point = edge ? new Vector2(rect.rect.xMin + 3, rect.rect.yMin + 3) : rect.rect.center;
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(point)) };
        }
        static bool Click(Selectable control, bool edge = false)
        {
            Canvas.ForceUpdateCanvases(); var pointer = Pointer(control, edge); var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            foreach (var hit in hits)
            {
                var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (target == null) continue; if (target != control.gameObject) return false;
                ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler); return true;
            }
            return false;
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;
        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
