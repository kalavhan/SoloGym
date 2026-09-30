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
    public sealed class PixelIconSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string locale;
            public Rect safeArea;
            public string[] checks;
        }
        PixelIconGallery gallery;
        readonly List<string> checks = new List<string>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        bool passed = true;
        public void Run(PixelIconGallery target) { gallery = target; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) yield return KeyboardProbe();
            else if (PixelButtonGallery.HasArgument("-sologym-smoke")) yield return Interactions();
            gallery.ResetReview(); gallery.Settings.Select();
            yield return null; Canvas.ForceUpdateCanvases();
            Check(gallery.Settings.ShowsFocus && !gallery.Back.ShowsFocus, "only the selected icon shows keyboard focus");
            Check(gallery.Disabled.VisualState == "disabled", "unavailable icon renders disabled");
            CheckLayout();
            yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture", "artifacts/visual/IconButton/gallery.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); } finally { Destroy(capture); }
            var report = new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_ICON_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")
                || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(passed ? 0 : 1);
        }
        IEnumerator Interactions()
        {
            Check(Click(gallery.Settings, true), "the icon's padded corner belongs to its full button hit area");
            Check(gallery.SettingsVisits == 1 && gallery.SettingsOpen, "pointer opens the settings example once");
            Check(Click(gallery.Back) && gallery.BackVisits == 1 && !gallery.SettingsOpen, "Back returns through native raycast activation");
            var pointer = Pointer(gallery.Settings);
            ExecuteEvents.Execute(gallery.Settings.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            Check(gallery.Settings.ShowsFocus, "pointer hover visibly highlights an icon");
            ExecuteEvents.Execute(gallery.Settings.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(gallery.Settings.VisualState == "pressed" && gallery.Settings.Icon.rectTransform.anchoredPosition.y == -2, "pointer hold gives a distinct pressed presentation");
            ExecuteEvents.Execute(gallery.Settings.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(gallery.Settings.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            Check(gallery.Settings.Icon.rectTransform.anchoredPosition.y == 0, "release restores the icon without position drift");
            pointer.button = PointerEventData.InputButton.Right;
            ExecuteEvents.Execute(gallery.Settings.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(gallery.SettingsVisits == 1, "right click cannot activate an icon");
            Submit(gallery.Settings); Submit(gallery.Back);
            Check(gallery.SettingsVisits == 2 && gallery.BackVisits == 2 && !gallery.SettingsOpen, "native submit activates both navigation examples");
            int blocked = 0; gallery.Disabled.onClick.AddListener(() => ++blocked);
            Click(gallery.Disabled); Submit(gallery.Disabled);
            Check(!gallery.Disabled.TryActivate() && blocked == 0, "disabled icon blocks pointer, keyboard and accessibility activation");
            Check(gallery.Disabled.AccessibilityNode.state == AccessibilityState.Disabled, "accessibility reports disabled status");
            var group = gallery.Settings.gameObject.AddComponent<CanvasGroup>(); group.interactable = false;
            yield return null;
            Click(gallery.Settings); Submit(gallery.Settings);
            Check(!gallery.Settings.TryActivate() && gallery.SettingsVisits == 2 && gallery.Settings.AccessibilityNode.state == AccessibilityState.Disabled,
                "CanvasGroup gates every activation path and accessibility state");
            group.interactable = true; Destroy(group); yield return null;
            gallery.Settings.Select(); gallery.Settings.gameObject.SetActive(false);
            Check(!gallery.Settings.AccessibilityNode.isActive && !gallery.Settings.TryActivate(), "hidden icon is absent from accessibility and cannot activate");
            gallery.Settings.gameObject.SetActive(true);
            Check(!gallery.Settings.ShowsFocus && gallery.Settings.AccessibilityNode.isActive, "reopening restores availability without stale focus");
            Check(gallery.Settings.TryActivate() && gallery.SettingsVisits == 3, "the accessibility callback shares the guarded activation path");
            gallery.Back.Select();
            Check(gallery.Back.GetComponent<PixelFieldTabNavigation>().Move(false) && Selected(gallery.Settings), "Tab reaches Settings from Back");
            gallery.Normal.Select();
            Check(gallery.Normal.GetComponent<PixelFieldTabNavigation>().Move(false) && Selected(gallery.Large), "Tab skips the unavailable icon");
            Check(gallery.Large.GetComponent<PixelFieldTabNavigation>().Move(true) && Selected(gallery.Normal), "Shift+Tab skips the unavailable icon in reverse");
            gallery.Back.Select();
            ExecuteEvents.Execute(gallery.Back.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right }, ExecuteEvents.moveHandler);
            Check(Selected(gallery.Settings), "native directional navigation reaches the next icon");
            var icon = gallery.Back.Icon.sprite; var skin = gallery.Back.Background.sprite;
            gallery.Back.SetIcon(gallery.Settings.Icon.sprite);
            Check(gallery.Back.Background.sprite == skin && gallery.Back.Icon.sprite == gallery.Settings.Icon.sprite, "swapping icon art preserves the shared skin");
            gallery.Back.SetIcon(icon);
            bool rejected = false; try { gallery.Back.SetLabel(" "); } catch (ArgumentException) { rejected = true; }
            Check(rejected && !string.IsNullOrWhiteSpace(gallery.Back.AccessibleLabel), "empty accessible names are rejected without losing the existing name");
            rejected = false; try { gallery.Back.SetIcon(null); } catch (ArgumentNullException) { rejected = true; }
            Check(rejected && gallery.Back.Icon.sprite == icon, "missing icon art cannot erase the current symbol");
            bool locale = gallery.Spanish; var oldLabel = gallery.Settings.AccessibleLabel;
            gallery.SetLocale(!locale);
            Check(gallery.Settings.AccessibleLabel != oldLabel && gallery.Settings.AccessibilityNode.label == gallery.Settings.AccessibleLabel,
                "locale changes update the visible and native accessible name together");
            CheckLayout(); gallery.SetLocale(locale);
            var oldNode = gallery.Back.AccessibilityNode;
            gallery.Back.UnbindAccessibility();
            Check(!gallery.Hierarchy.ContainsNode(oldNode), "unbinding removes the native node from its hierarchy");
            gallery.Back.BindAccessibility(gallery.Hierarchy);
            Check(gallery.Hierarchy.ContainsNode(gallery.Back.AccessibilityNode), "rebinding creates a live native button node");
            var temporaryHierarchy = new AccessibilityHierarchy();
            gallery.Back.BindAccessibility(temporaryHierarchy);
            temporaryHierarchy.Clear();
            gallery.Back.SetLabel("Return");
            Check(gallery.Back.AccessibilityNode == null, "screen-owned hierarchy teardown safely detaches a surviving control");
            gallery.Back.BindAccessibility(gallery.Hierarchy);
            gallery.SetLocale(locale);
        }
        IEnumerator KeyboardProbe()
        {
            string marker = PixelButtonGallery.Argument("-sologym-keyboard-probe", "");
            bool locale = gallery.Spanish; gallery.Back.Select(); yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready");
            float until = Time.realtimeSinceStartup + 25;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; ++i) yield return null;
            Check(File.Exists(marker + ".done"), "OS keyboard driver completed");
            Check(gallery.BackVisits == 1, "OS Tab/Enter activate a Back icon once");
            Check(gallery.SettingsVisits == 2 && gallery.SettingsOpen, "OS Tab/Enter activate both Settings icon sizes");
            Check(gallery.Spanish != locale && Selected(gallery.Settings), "OS Shift+Tab changes locale and Tab skips the disabled icon");
            gallery.SetLocale(locale);
        }
        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases(); gallery.Hierarchy.RefreshNodeFrames();
            foreach (var button in gallery.Buttons)
            {
                var bounds = button.ScreenBounds();
                Check(Contains(gallery.EffectiveSafeArea, bounds), button.AccessibleLabel + " stays in the safe area");
                Check(bounds.width >= 47 && bounds.height >= 47, button.AccessibleLabel + " retains at least a 48px captured target");
                Check(button.Icon.preserveAspect && button.Icon.type == Image.Type.Simple && button.Icon.sprite != button.Background.sprite,
                    button.AccessibleLabel + " preserves its independent icon aspect ratio");
                Check(Contains(bounds, PixelIconGallery.Bounds(button.Icon.rectTransform)), button.AccessibleLabel + " keeps icon artwork inside its hit area");
                var panel = button.GetComponentInParent<PixelContentPanel>();
                Check(Contains(PixelIconGallery.Bounds(panel.Content), bounds), button.AccessibleLabel + " avoids panel clipping");
                var node = button.AccessibilityNode;
                Check(node != null && node.label == button.AccessibleLabel && node.role == AccessibilityRole.Button,
                    button.AccessibleLabel + " exposes a labeled native accessibility button");
                Check(node != null && Vector2.Distance(node.frame.position, bounds.position) < 1 && Vector2.Distance(node.frame.size, bounds.size) < 1,
                    button.AccessibleLabel + " exposes the actual on-screen target bounds");
            }
            foreach (var text in gallery.GetComponentsInChildren<Text>())
            {
                var measured = PixelPrimaryButton.MeasureWrappedLabel(text);
                Check(measured.x <= text.rectTransform.rect.width + 1.1f && measured.y <= text.rectTransform.rect.height + 1.1f,
                    text.name + " fits without truncated localized text");
            }
        }
        static bool Selected(Selectable button) => EventSystem.current.currentSelectedGameObject == button.gameObject;
        static void Submit(Selectable button) => ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        static PointerEventData Pointer(Selectable button, bool corner = false)
        {
            var rect = (RectTransform)button.transform;
            var point = corner ? new Vector2(rect.rect.xMin + 3, rect.rect.yMin + 3) : rect.rect.center;
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(point)) };
        }
        bool Click(Selectable button, bool corner = false)
        {
            Canvas.ForceUpdateCanvases(); var pointer = Pointer(button, corner);
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = null;
            foreach (var hit in hits) { target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (target != null) break; }
            if (target != button.gameObject) return false;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerExitHandler); return true;
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f
            && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;
        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
