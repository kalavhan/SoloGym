using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    public sealed class PixelHomeRoomSmoke : MonoBehaviour
    {
        [Serializable] sealed class CheckResult { public string name; public bool passed; }
        [Serializable] sealed class Report { public int width, height, passed, failed; public List<CheckResult> checks; }
        readonly List<CheckResult> checks = new List<CheckResult>();
        PixelHomeRoomReview review;
        string capture;
        public void Run(PixelHomeRoomReview value) { review = value; StartCoroutine(Verify()); }
        IEnumerator Verify()
        {
            yield return null;
            capture = PixelButtonGallery.Argument("-sologym-capture", "");
            if (PixelButtonGallery.HasArgument("-sologym-smoke"))
            {
                var room = review.Room;
                CheckLayout("initial");
                Check(room.Exterior.transform.GetSiblingIndex() < room.Architecture.transform.GetSiblingIndex()
                    && room.Architecture.transform.GetSiblingIndex() < room.Objects.GetSiblingIndex(), "exterior, architecture and occupants use separate ordered layers");
                foreach (var image in room.GetComponentsInChildren<Image>()) Check(!image.raycastTarget, image.name + " does not intercept future interface input");
                var shell = room.Architecture.sprite;
                var exterior = room.Exterior.sprite;
                Check(shell.texture.filterMode == FilterMode.Point && exterior.texture.filterMode == FilterMode.Point, "environment artwork keeps nearest-neighbor sampling");
                Check(!review.InterfaceLayer.IsChildOf(room.Plane), "live interface is independent of the room's art scaling");
                var id = review.Character.Character.id;
                var scale = review.Character.SourcePixelScale;
                var feet = review.Character.FeetWorld;
                foreach (string body in new[] { "female-skinny", "female-medium", "female-fat", "female-muscular", "male-skinny", "male-medium", "male-fat", "male-muscular" })
                {
                    Check(review.Character.Show(body, body, "Unavailable"), body + " reuses an existing source");
                    Check(Mathf.Approximately(review.Character.SourcePixelScale, scale) && Vector3.Distance(feet, review.Character.FeetWorld) < .01f,
                        body + " preserves body proportions and feet position");
                }
                review.Character.Show(id, id, "Unavailable");
                bool shown = review.ShowCharacter;
                review.SetCharacterVisible(false);
                Check(room.Architecture.isActiveAndEnabled && room.Exterior.isActiveAndEnabled, "hiding the character leaves the environment intact");
                yield return Capture("shell-only");
                room.Exterior.enabled = false; yield return Capture("exterior-hidden");
                Check(room.Architecture.isActiveAndEnabled && room.Architecture.sprite == shell, "hiding the exterior does not replace the room");
                room.Exterior.enabled = true;
                room.Architecture.enabled = false; yield return Capture("architecture-hidden");
                Check(room.Exterior.isActiveAndEnabled && room.Exterior.sprite == exterior, "exterior remains separately renderable");
                room.Architecture.enabled = true; review.SetCharacterVisible(shown);
                bool rejected = false;
                try { room.SetExterior(shell); } catch (ArgumentException) { rejected = true; }
                Check(rejected && room.Exterior.sprite == exterior, "a mismatched exterior aspect ratio is rejected without mutation");
                room.SetExterior(exterior);
                rejected = false; try { room.AnchorPoint("missing"); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "missing placement IDs fail explicitly instead of silently moving objects");

                // Resize the parent within the same player session to catch stale anchored geometry.
                var area = (RectTransform)room.transform.parent;
                var oldMax = area.offsetMax;
                area.offsetMax = oldMax - new Vector2(123, 57);
                Canvas.ForceUpdateCanvases(); room.RefreshLayout(); CheckLayout("after parent resize");
                area.offsetMax = oldMax; review.Relayout();
            }
            yield return Capture("");
            int failed = checks.FindAll(c => !c.passed).Count;
            var report = new Report { width = Screen.width, height = Screen.height, passed = checks.Count - failed, failed = failed, checks = checks };
            if (!string.IsNullOrEmpty(capture)) File.WriteAllText(Path.ChangeExtension(capture, ".json"), JsonUtility.ToJson(report, true));
            Debug.Log("HOME ROOM: " + report.passed + "/" + checks.Count + " passed");
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-quit-after-capture")) Application.Quit(failed == 0 ? 0 : 1);
        }
        void CheckLayout(string state)
        {
            Canvas.ForceUpdateCanvases();
            var room = review.Room;
            var bounds = PixelChoiceOption.ScreenBounds(room.Plane);
            var safe = review.EffectiveSafeArea;
            Check(bounds.xMin >= safe.xMin - 1 && bounds.yMin >= safe.yMin - 1 && bounds.xMax <= safe.xMax + 1 && bounds.yMax <= safe.yMax + 1,
                state + ": full room stays inside the safe landscape viewport");
            Check(Mathf.Abs(bounds.width / bounds.height - room.ReferenceSize.x / room.ReferenceSize.y) < .01f,
                state + ": architecture keeps its aspect ratio");
            var point = room.AnchorPoint("hero.feet");
            var expected = room.Plane.TransformPoint(new Vector3(point.x - room.ReferenceSize.x / 2, room.ReferenceSize.y / 2 - point.y, 0));
            Check(Vector3.Distance(expected, review.Character.FeetWorld) < 1, state + ": character's actual boots stay on the room anchor");
            var hero = review.Character.CharacterImage.rectTransform;
            Check(Mathf.Abs(hero.rect.width / hero.rect.height - (float)review.Character.Character.width / review.Character.Character.height) < .001f,
                state + ": character is never stretched to fill the room");
        }
        void Check(bool passed, string name)
        { checks.Add(new CheckResult { name = name, passed = passed }); if (!passed) Debug.LogError("HOME ROOM: " + name); }
        IEnumerator Capture(string suffix)
        {
            if (string.IsNullOrEmpty(capture)) yield break;
            yield return new WaitForEndOfFrame();
            string path = suffix == "" ? capture : Path.Combine(Path.GetDirectoryName(capture), Path.GetFileNameWithoutExtension(capture) + "-" + suffix + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(path, texture.EncodeToPNG()); Destroy(texture);
        }
    }
}
