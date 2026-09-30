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
                var ring = review.Ring;
                var bag = review.Bag;
                string savedRing = ring.SaveState();
                string savedBag = bag.SaveState();
                var bagSprite = bag.Artwork.sprite;
                Check(bag.transform.parent == room.BackObjects && bag.transform.GetSiblingIndex() > ring.transform.GetSiblingIndex(),
                    "hanging bag shares the prop layer with independent ordered artwork");
                Check(bag.Artwork.sprite.texture.filterMode == FilterMode.Point, "bag keeps point-filtered artwork");
                bag.SetVisible(false);
                string hiddenBag = bag.SaveState();
                Check(!bag.Artwork.enabled && ring.SaveState() == savedRing && room.Architecture.enabled && review.Character.HasCharacter,
                    "hiding the bag leaves the ring, architecture and character intact");
                bag.SetVisible(true); bag.RestoreState(hiddenBag);
                Check(!bag.Visible && !bag.Artwork.enabled && bag.Artwork.sprite == bagSprite,
                    "bag visibility round-trips without replacing artwork");
                bag.RestoreState(savedBag);
                bool bagRejected = false;
                try { bag.RestoreState(savedBag.Replace("bag.hook", "ring.floor")); } catch (ArgumentException) { bagRejected = true; }
                Check(bagRejected && bag.SaveState() == savedBag && ring.SaveState() == savedRing,
                    "a hanging bag cannot claim the ring floor slot or mutate either prop on failure");
                var ringPosition = ((RectTransform)ring.transform).anchoredPosition;
                var footprint = ring.FootprintInRoom();
                Check(ring.transform.parent == room.BackObjects && room.BackObjects.GetSiblingIndex() < room.Objects.GetSiblingIndex(),
                    "ring is an independent prop behind the character");
                Check(ring.Artwork.sprite.texture.filterMode == FilterMode.Point, "ring keeps point-filtered artwork");
                ring.SetVisible(false);
                Check(!ring.Artwork.enabled && room.Architecture.enabled && review.Character.HasCharacter,
                    "removing the ring leaves the room and character intact");
                string hiddenState = ring.SaveState();
                ring.SetVisible(true); ring.RestoreState(hiddenState);
                Check(!ring.Visible && !ring.Artwork.enabled, "saved visibility round-trips without removing the placement");
                ring.RestoreState(savedRing); ring.SetVariant("base");
                Check(ring.SaveState() == savedRing && ((RectTransform)ring.transform).anchoredPosition == ringPosition,
                    "restoring a save and binding a variant preserve identity and anchor");
                var copiedFootprint = ring.FootprintInRoom(); copiedFootprint[0] = Vector2.zero;
                Check(ring.FootprintInRoom()[0] == footprint[0], "callers cannot mutate the saved footprint through the returned array");
                foreach (string invalid in new[] { savedRing.Replace("\"version\":1", "\"version\":2"),
                    savedRing.Replace("home.refuge.r1", "other-room"), savedRing.Replace("home.training-ring", "other-item"),
                    savedRing.Replace("ring.floor", "hero.feet"), savedRing.Replace("\"base\"", "\"unapproved-winter\"") })
                {
                    bool invalidRejected = false;
                    try { ring.RestoreState(invalid); } catch (ArgumentException) { invalidRejected = true; }
                    Check(invalidRejected && ring.SaveState() == savedRing, "invalid save is rejected atomically: " + invalid);
                }
                bool variantRejected = false;
                try { ring.SetVariant("unapproved-winter"); } catch (ArgumentException) { variantRejected = true; }
                Check(variantRejected && ring.SaveState() == savedRing, "unknown seasonal art is not silently substituted or regenerated");
                int oldCount = room.GetComponentsInChildren<PixelRoomObject>(true).Length;
                bool duplicateRejected = false;
                try { PixelRoomObject.Create(room, "Rooms/Props/TrainingRingR1/item", savedRing); } catch (ArgumentException) { duplicateRejected = true; }
                Check(duplicateRejected && room.GetComponentsInChildren<PixelRoomObject>(true).Length == oldCount,
                    "duplicate room identities and occupied slots fail before creating a second object");
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
            // Capture the pair at one settled viewport state, after the resize exercise.
            // Otherwise nearest-neighbor rounding between layouts can obscure the visibility comparison.
            bool finalVisibility = review.Ring.Visible;
            Canvas.ForceUpdateCanvases();
            var roomVertices = review.Room.Architecture.canvasRenderer.GetMesh().vertices;
            review.Ring.SetVisible(false); yield return Capture("ring-hidden");
            review.Ring.SetVisible(finalVisibility);
            bool finalBagVisibility = review.Bag.Visible;
            review.Bag.SetVisible(false); yield return Capture("bag-hidden");
            review.Bag.SetVisible(finalBagVisibility); yield return Capture("");
            var finalVertices = review.Room.Architecture.canvasRenderer.GetMesh().vertices;
            bool stableVertices = roomVertices.Length == finalVertices.Length;
            for (int i = 0; stableVertices && i < roomVertices.Length; i++) stableVertices &= Vector3.Distance(roomVertices[i], finalVertices[i]) < .0001f;
            Check(stableVertices, "prop visibility does not change the architecture pixel grid after resizing");
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
            var ring = review.Ring;
            var ringRect = (RectTransform)ring.transform;
            var ringAnchor = room.AnchorPoint(ring.SlotId);
            var ringExpected = room.Plane.TransformPoint(new Vector3(ringAnchor.x - room.ReferenceSize.x / 2, room.ReferenceSize.y / 2 - ringAnchor.y, 0));
            Check(Vector3.Distance(ringExpected, ring.transform.position) < 1 && ringRect.sizeDelta == ring.DisplaySize,
                state + ": ring retains its floor pivot and uniform authored size after layout changes");
            Check(Mathf.Abs(ringRect.rect.width / ringRect.rect.height - ring.Artwork.sprite.rect.width / ring.Artwork.sprite.rect.height) < .001f,
                state + ": ring does not stretch independently along either axis");
            var bag = review.Bag;
            var bagRect = (RectTransform)bag.transform;
            var hook = room.AnchorPoint(bag.SlotId);
            var hookExpected = room.Plane.TransformPoint(new Vector3(hook.x - room.ReferenceSize.x / 2, room.ReferenceSize.y / 2 - hook.y, 0));
            Check(Vector3.Distance(hookExpected, bag.transform.position) < 1 && bagRect.sizeDelta == bag.DisplaySize,
                state + ": bag keeps its top attachment pivot and uniform size after layout changes");
            Check(Mathf.Abs(bagRect.rect.width / bagRect.rect.height - bag.Artwork.sprite.rect.width / bag.Artwork.sprite.rect.height) < .001f,
                state + ": hanging bag preserves its source aspect ratio");
        }
        void Check(bool passed, string name)
        { checks.Add(new CheckResult { name = name, passed = passed }); if (!passed) Debug.LogError("HOME ROOM: " + name); }
        IEnumerator Capture(string suffix)
        {
            if (string.IsNullOrEmpty(capture)) yield break;
            // A prior capture can resume this coroutine at end-of-frame; allow a complete
            // layout/render cycle after subsequent visibility or parent-size changes.
            yield return null;
            yield return new WaitForEndOfFrame();
            string path = suffix == "" ? capture : Path.Combine(Path.GetDirectoryName(capture), Path.GetFileNameWithoutExtension(capture) + "-" + suffix + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(path, texture.EncodeToPNG()); Destroy(texture);
        }
    }
}
