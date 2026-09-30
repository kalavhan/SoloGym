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
    public sealed class PixelCharacterSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string locale, character;
            public Rect safeArea;
            public string[] checks;
        }
        PixelCharacterGallery gallery;
        readonly List<string> checks = new List<string>();
        bool passed = true;
        public void Run(PixelCharacterGallery target) { gallery = target; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            for (int i = 0; i < 8; ++i) yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-keyboard-probe")) yield return KeyboardProbe();
            else if (PixelButtonGallery.HasArgument("-sologym-smoke")) yield return Interactions();
            gallery.ResetReview(); yield return null; Canvas.ForceUpdateCanvases(); CheckLayout();
            yield return new WaitForEndOfFrame();
            string path = CapturePath; Capture(path);
            var report = new Report { passed = passed, screenWidth = Screen.width, screenHeight = Screen.height,
                locale = gallery.Spanish ? "es" : "en", character = gallery.CharacterId, safeArea = gallery.EffectiveSafeArea, checks = checks.ToArray() };
            string json = JsonUtility.ToJson(report, true); File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), json + "\n");
            Debug.Log("SOLOGYM_PIXEL_CHARACTER_SMOKE " + json);
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-keyboard-probe")
                || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(passed ? 0 : 1);
        }
        string CapturePath => Path.GetFullPath(PixelButtonGallery.Argument("-sologym-capture", "artifacts/visual/CharacterViewport/gallery.png"));
        static void Capture(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); } finally { Destroy(capture); }
        }
        IEnumerator Interactions()
        {
            Check(gallery.MainView.HasCharacter && gallery.CompactView.HasCharacter, "initial appearance loads in both consumers");
            if (!gallery.MainView.HasCharacter || !gallery.CompactView.HasCharacter) yield break;
            float scale = gallery.MainView.SourcePixelScale; Vector3 feet = gallery.MainView.FeetWorld;
            float compactScale = gallery.CompactView.SourcePixelScale;
            Check(PixelCharacterCatalog.Entries.Count == 8, "catalog contains the eight user-authored appearances");
            foreach (var entry in PixelCharacterCatalog.Entries)
            {
                Check(Click(gallery.Presentation.Option(entry.presentation)) && Click(gallery.Body.Option(entry.body)), entry.id + " can be chosen through native pointer targets");
                yield return null; Canvas.ForceUpdateCanvases();
                Check(gallery.CharacterId == entry.id && gallery.MainView.HasCharacter && gallery.CompactView.HasCharacter && gallery.MainView.Character.id == entry.id && gallery.CompactView.Character.id == entry.id,
                    entry.id + " binds both consumers to the same stable ID");
                Check(Mathf.Abs(gallery.MainView.SourcePixelScale - scale) < .0001f && Vector3.Distance(gallery.MainView.FeetWorld, feet) < .01f,
                    entry.id + " preserves shared source scale and feet position");
                Check(Mathf.Abs(gallery.CompactView.SourcePixelScale - compactScale) < .0001f, entry.id + " also preserves scale in the compact consumer");
                CheckViewport(gallery.MainView, entry.id); CheckViewport(gallery.CompactView, entry.id + " compact");
                if (PixelButtonGallery.HasArgument("-sologym-capture-all"))
                { yield return new WaitForEndOfFrame(); Capture(Path.Combine(Path.GetDirectoryName(CapturePath), "roster", entry.id + ".png")); }
            }
            int changes = gallery.SelectionChanges;
            gallery.Body.Option(gallery.Body.Value).Select(); Submit(gallery.Body.Option(gallery.Body.Value));
            Check(gallery.SelectionChanges == changes, "reselecting the current body does not reissue a selection change");
            gallery.Presentation.Option("female").Select(); Submit(gallery.Presentation.Option("female"));
            gallery.Body.Option("muscular").Select(); Submit(gallery.Body.Option("muscular"));
            Check(gallery.MainView.Character.id == "female-muscular", "native keyboard submit switches the shown appearance");
            bool locale = gallery.Spanish; gallery.SetLocale(!locale); yield return null;
            Check(gallery.MainView.Character.id == "female-muscular" && gallery.MainView.AccessibilityNode.label.Contains(gallery.Body.Option("muscular").Label.text),
                "localization changes the description without changing the appearance");
            CheckLayout(); gallery.SetLocale(locale);
            var view = gallery.MainView; var rect = (RectTransform)view.transform; Vector2 originalSize = rect.sizeDelta;
            foreach (var size in new[] { new Vector2(110, 300), new Vector2(500, 160) })
            {
                rect.sizeDelta = size; view.RefreshLayout(); Canvas.ForceUpdateCanvases(); CheckViewport(view, "resized " + size);
                Check(view.SourcePixelScale > 0, "a narrow or short viewport contains the complete character with positive uniform scale");
            }
            rect.sizeDelta = originalSize; view.RefreshLayout();
            Check(!view.Show("unavailable-id", "Unknown appearance", "Character unavailable") && !view.HasCharacter && view.CharacterImage.sprite == null
                && view.EmptyLabel.enabled && view.AccessibilityNode.role == AccessibilityRole.StaticText, "unknown IDs clear stale artwork and expose a localized unavailable state");
            view.Clear("Choose a character"); Check(view.EmptyLabel.text == "Choose a character", "empty state uses caller-owned localized text");
            gallery.RefreshCharacter();
            var current = view.CharacterImage.sprite; var position = view.FeetWorld;
            for (int i = 0; i < 8; ++i) yield return null;
            Check(view.CharacterImage.sprite == current && view.FeetWorld == position && view.GetComponent<Animator>() == null,
                "static MVP display has no idle movement or frame animation");
            view.gameObject.SetActive(false); Check(!view.AccessibilityNode.isActive, "hidden preview is excluded from accessibility");
            view.gameObject.SetActive(true); Check(view.HasCharacter && view.AccessibilityNode.isActive, "reopening retains the chosen character");
            var hierarchy = new AccessibilityHierarchy(); view.BindAccessibility(hierarchy); hierarchy.Clear(); view.SetDescription("Updated description");
            Check(view.AccessibilityNode == null, "clearing an owning hierarchy safely detaches its character node");
            view.BindAccessibility(gallery.Hierarchy); gallery.RefreshCharacter();
        }
        IEnumerator KeyboardProbe()
        {
            string marker = PixelButtonGallery.Argument("-sologym-keyboard-probe", "");
            gallery.Presentation.Option("female").Select(); yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready"); float until = Time.realtimeSinceStartup + 25;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < until) yield return null;
            for (int i = 0; i < 5; ++i) yield return null;
            Check(File.Exists(marker + ".done"), "OS keyboard driver completed");
            Check(gallery.CharacterId == "male-muscular" && gallery.MainView.Character.id == "male-muscular" && gallery.CompactView.Character.id == "male-muscular",
                "real arrows, Tab and Enter choose the same appearance in both viewport sizes");
            Check(gallery.SelectionChanges == 3, "focus changes do not commit character choices");
            Check(gallery.Spanish, "real Tab navigation reaches the locale action and preserves the appearance");
        }
        void CheckViewport(PixelCharacterViewport view, string label)
        {
            var image = view.CharacterImage; var sprite = image.sprite; var entry = view.Character;
            Check(view.HasCharacter && sprite != null && image.color == Color.white, label + " uses source artwork with no recoloring");
            if (!view.HasCharacter) return;
            Check(Mathf.Abs(image.rectTransform.rect.width / image.rectTransform.rect.height - (float)entry.width / entry.height) < .001f,
                label + " preserves the authored width-to-height ratio");
            Check(Vector2.Distance(sprite.pivot, entry.feet) < .01f, label + " imports the authored feet pivot");
            Check(sprite.texture.filterMode == FilterMode.Point && sprite.texture.mipmapCount == 1,
                label + " uses point filtering without mipmaps");
            Check(Contains(PixelChoiceOption.ScreenBounds((RectTransform)view.transform), PixelChoiceOption.ScreenBounds(image.rectTransform)),
                label + " keeps all cropped pixels inside its viewport");
            Check(!image.raycastTarget && !view.EmptyLabel.raycastTarget && view.GetComponent<Selectable>() == null,
                label + " is decorative and cannot intercept selection or input");
        }
        void CheckLayout()
        {
            Canvas.ForceUpdateCanvases(); gallery.Hierarchy.RefreshNodeFrames();
            CheckViewport(gallery.MainView, "main preview"); CheckViewport(gallery.CompactView, "compact preview");
            foreach (var view in new[] { gallery.MainView, gallery.CompactView })
                Check(Contains(gallery.EffectiveSafeArea, PixelChoiceOption.ScreenBounds((RectTransform)view.transform)), "viewport fits the landscape safe area");
            foreach (var control in new[] { gallery.Presentation, gallery.Body })
                foreach (var option in control.Options)
                {
                    var bounds = PixelChoiceOption.ScreenBounds((RectTransform)option.transform);
                    Check(Contains(gallery.EffectiveSafeArea, bounds) && bounds.height >= 47 && bounds.width >= 47, option.Id + " is a safe 48px-or-larger target");
                }
            foreach (var text in gallery.GetComponentsInChildren<Text>())
            {
                if (!text.enabled || string.IsNullOrEmpty(text.text)) continue;
                var size = PixelPrimaryButton.MeasureWrappedLabel(text);
                Check(size.x <= text.rectTransform.rect.width + 1.1f && size.y <= text.rectTransform.rect.height + 1.1f, text.name + " fits its localized caption");
            }
            Check(gallery.MainView.AccessibilityNode != null && gallery.MainView.AccessibilityNode.label.Contains(gallery.Body.Option(gallery.Body.Value).Label.text),
                "one descriptive accessibility image node follows the visible character");
        }
        static bool Click(Selectable control)
        {
            Canvas.ForceUpdateCanvases(); var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, control.transform.position) };
            var rect = (RectTransform)control.transform; pointer.position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            foreach (var hit in hits)
            {
                var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject); if (target == null) continue;
                if (target != control.gameObject) return false;
                ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler); return true;
            }
            return false;
        }
        static void Submit(Selectable control) => ExecuteEvents.Execute(control.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1.1f && inner.xMax <= outer.xMax + 1.1f && inner.yMin >= outer.yMin - 1.1f && inner.yMax <= outer.yMax + 1.1f;
        void Check(bool result, string label) { passed &= result; checks.Add((result ? "PASS " : "FAIL ") + label); }
    }
}
