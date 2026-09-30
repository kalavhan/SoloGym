using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    public sealed class PixelFieldSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string locale;
            public Rect safeArea;
            public string[] checks;
        }
        PixelFieldGallery gallery;
        readonly List<string> checks = new List<string>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        bool passed = true;
        public void Run(PixelFieldGallery target) { gallery = target; StartCoroutine(Verify()); }

        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-keyboard-probe"))
                yield return KeyboardProbe();
            else if (PixelButtonGallery.HasArgument("-sologym-smoke"))
                yield return Interactions();
            gallery.ResetReview();
            gallery.Password.Input.Select();
            yield return null; yield return null;
            gallery.Password.Input.caretPosition = gallery.Password.Input.text.Length;
            gallery.Password.Input.caretBlinkRate = 0;
            gallery.Password.Input.ForceLabelUpdate();
            Canvas.ForceUpdateCanvases();
            Check(gallery.Email.VisualState == "normal", "unfocused field has no focus outline");
            Check(gallery.Password.VisualState == "focused", "selected password presents focus");
            Check(gallery.Invalid.VisualState == "invalid", "invalid field presents a text error and border");
            Check(gallery.Disabled.VisualState == "disabled", "disabled field presents its unavailable state");
            CheckLayout();
            Check(gallery.Invalid.PreferredHeight > 130, "wrapped validation text expands the native layout height");
            Check(Bounds((RectTransform)gallery.Invalid.transform).yMin > Bounds((RectTransform)gallery.Disabled.transform).yMax,
                "expanded validation leaves space before the next field");
            yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture", "artifacts/visual/FormField/gallery.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); } finally { Destroy(capture); }
            var report = new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_FIELD_SMOKE " + json); // Reports contain no entered values.
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")
                || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(passed ? 0 : 1);
        }

        IEnumerator Interactions()
        {
            Check(Click(gallery.Email.Input), "pointer raycast reaches the native email input");
            yield return null; yield return null;
            Check(gallery.Email.Input.isFocused, "pointer activation opens native editing");
            Type(gallery.Email.Input, "hero+fit@example.com");
            Check(gallery.Email.Input.text == "hero+fit@example.com", "email keyboard hint preserves plus addressing during native typing");
            Key(gallery.Email.Input, KeyCode.A, EventModifiers.Control);
            Type(gallery.Email.Input, "niño");
            Key(gallery.Email.Input, KeyCode.Backspace);
            Check(gallery.Email.Input.text == "niñ", "native selection replacement and backspace preserve Unicode input");
            Key(gallery.Email.Input, KeyCode.A, EventModifiers.Control);
            Type(gallery.Email.Input, "hero@example.com");
            Check(gallery.Email.Input.text == "hero@example.com", "native select-all replaces the complete value");
            string longValue = new string('m', 80) + "@example.com";
            Key(gallery.Email.Input, KeyCode.A, EventModifiers.Control);
            Type(gallery.Email.Input, longValue);
            gallery.Email.Input.ForceLabelUpdate();
            Check(gallery.Email.Input.text == longValue && gallery.Email.Input.textComponent.text.Length < longValue.Length,
                "long input scrolls its visible substring without truncating the stored value");
            gallery.Email.SetValueWithoutNotify("hero@example.com");
            gallery.Email.SetError("Example validation");
            Check(gallery.Email.Message.text.StartsWith("! ") && gallery.Email.VisualState == "invalid", "validation feedback has a readable text marker");
            gallery.Email.SetError("");
            Check(gallery.Email.VisualState == "focused", "clearing validation restores the focused appearance");

            gallery.Password.Input.Select();
            yield return null; yield return null;
            Check(gallery.Email.VisualState == "normal", "focus moves off the previous input");
            Key(gallery.Password.Input, KeyCode.A, EventModifiers.Control);
            Type(gallery.Password.Input, "Trial9!pass");
            gallery.Password.Input.ForceLabelUpdate();
            Check(gallery.Password.Input.textComponent.text.IndexOf("Trial", StringComparison.Ordinal) < 0,
                "password text is masked in the rendered label");
            gallery.Password.Input.selectionAnchorPosition = 2;
            gallery.Password.Input.selectionFocusPosition = 5;
            string before = gallery.Password.Input.text;
            Check(Click(gallery.Password.Visibility), "password visibility control receives pointer input");
            Check(gallery.Password.PasswordVisible && gallery.Password.Input.inputType == InputField.InputType.Standard,
                "visibility toggle reveals the native value");
            Check(gallery.Password.Input.text == before && gallery.Password.Input.selectionAnchorPosition == 2
                && gallery.Password.Input.selectionFocusPosition == 5 && gallery.Password.Input.isFocused,
                "visibility toggle preserves value, selection and editing focus");
            Click(gallery.Password.Visibility);
            Check(!gallery.Password.PasswordVisible && gallery.Password.Input.text == before, "hide restores masking without changing the value");

            var passwordTab = gallery.Password.Input.GetComponent<PixelFieldTabNavigation>();
            Check(passwordTab.Move(false) && Selected(gallery.Password.Visibility), "Tab order reaches password visibility");
            var toggleTab = gallery.Password.Visibility.GetComponent<PixelFieldTabNavigation>();
            Check(toggleTab.Move(true) && Selected(gallery.Password.Input), "Shift+Tab returns to password editing");
            gallery.Password.SetInteractable(false);
            Check(!gallery.Password.Visibility.interactable, "disabling the password also disables its visibility control");
            gallery.Email.Input.Select();
            Check(gallery.Email.Input.GetComponent<PixelFieldTabNavigation>().Move(false) && Selected(gallery.Submit),
                "Tab skips a disabled field and its disabled visibility control");
            gallery.Password.SetInteractable(true);
            var group = gallery.Password.gameObject.AddComponent<CanvasGroup>(); group.interactable = false;
            Check(!gallery.Password.Input.IsInteractable() && !gallery.Password.Visibility.IsInteractable(), "CanvasGroup gates both password controls");
            group.interactable = true; Destroy(group);
            yield return null;

            gallery.Email.SetValueWithoutNotify(""); gallery.Password.SetValueWithoutNotify("");
            Click(gallery.Submit);
            Check(gallery.SuccessfulExamples == 0 && gallery.Email.Error.Length > 0 && gallery.Password.Error.Length > 0,
                "fixture validation blocks an empty example and presents both errors");
            gallery.Email.SetValueWithoutNotify("hero@example.com"); gallery.Password.SetValueWithoutNotify("Trial9!pass");
            Click(gallery.Submit);
            Check(gallery.SuccessfulExamples == 1 && gallery.Email.Error.Length == 0 && gallery.Password.Error.Length == 0,
                "corrected fictional values clear validation");
            bool spanish = gallery.Spanish;
            Click(gallery.Locale);
            Check(gallery.Spanish != spanish && gallery.Email.Input.text == "hero@example.com" && gallery.Password.Input.text == "Trial9!pass",
                "locale changes preserve input values");
            CheckLayout(); gallery.SetLocale(spanish);
            gallery.Password.TogglePasswordVisibility();
            gallery.Password.gameObject.SetActive(false); gallery.Password.gameObject.SetActive(true);
            Check(!gallery.Password.PasswordVisible, "closing the field restores password masking");
        }

        IEnumerator KeyboardProbe()
        {
            // The optional X11 harness sends real OS events to this isolated player.
            string marker = PixelButtonGallery.Argument("-sologym-keyboard-probe", "");
            gallery.Email.SetValueWithoutNotify(""); gallery.Password.SetValueWithoutNotify("");
            gallery.Email.Input.Select();
            yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready");
            float until = Time.realtimeSinceStartup + 25;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; ++i) yield return null;
            Check(File.Exists(marker + ".done"), "OS keyboard driver completed");
            Check(gallery.Email.Input.text == "hero+fit@example.com", "OS typing edits the email field");
            Check(gallery.Password.Input.text == "Trial9pass", "Tab moves OS typing to the password field");
            Check(gallery.Password.PasswordVisible, "keyboard submit activates the focused visibility control");
            Check(gallery.SuccessfulExamples == 1, "Shift+Tab and Enter submit the fictional example once");
        }

        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var field in gallery.Fields)
            {
                Rect area = Bounds((RectTransform)field.transform);
                Check(Contains(gallery.EffectiveSafeArea, area), field.name + " remains inside the safe area");
                Check(Bounds((RectTransform)field.Input.transform).height >= 47, field.name + " retains a 48px input target");
                Check(field.Background.sprite != null && field.Background.type == Image.Type.Sliced, field.name + " uses its sliced sprite skin");
                foreach (var text in new[] { field.Label, field.Message, field.Placeholder })
                {
                    var measured = PixelPrimaryButton.MeasureWrappedLabel(text);
                    Check(measured.x <= text.rectTransform.rect.width + 1.1f && measured.y <= text.rectTransform.rect.height + 1.1f,
                        field.name + "/" + text.name + " fits localized copy");
                }
                Check(!field.Input.textComponent.supportRichText, field.name + " treats typed markup as plain text");
            }
        }
        static void Type(InputField input, string text) { foreach (char c in text) input.ProcessEvent(new Event { type = EventType.KeyDown, character = c }); }
        static void Key(InputField input, KeyCode key, EventModifiers modifiers = EventModifiers.None) => input.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = key, modifiers = modifiers });
        static bool Selected(Selectable control) => EventSystem.current.currentSelectedGameObject == control.gameObject;
        bool Click(Selectable control)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)control.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = null;
            foreach (var hit in hits) { target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (target != null) break; }
            if (target != control.gameObject) return false;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerExitHandler); return true;
        }
        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]), max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f
            && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;
        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
