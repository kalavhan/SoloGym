using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SoloGym.UI
{
    public sealed class PixelHomeLayoutSmoke : MonoBehaviour
    {
        [Serializable] sealed class Result { public string name; public bool passed; }
        [Serializable] sealed class Report { public int passed, failed; public List<Result> checks; }
        readonly List<Result> checks = new List<Result>();
        void Check(bool passed, string name) { checks.Add(new Result { name = name, passed = passed }); if (!passed) Debug.LogError(name); }
        public void Run(PixelHomeRoomReview review) => StartCoroutine(Verify(review));
        IEnumerator Verify(PixelHomeRoomReview review)
        {
            yield return null;
            var props = review.Room.GetComponentsInChildren<PixelRoomObject>(true);
            string originalRing = review.Ring.SaveState();
            string baseline = PixelHomeLayout.Export(review);
            PixelHomeLayout.Apply(review, baseline);
            Check(PixelHomeLayout.Export(review) == baseline, "default layout round-trips into explicit placements");
            string fixture = File.ReadAllText(PixelButtonGallery.Argument("-sologym-room-layout", ""));
            var data = JsonUtility.FromJson<PixelHomeLayout.Document>(fixture);
            PixelHomeLayout.Apply(review, fixture);
            string edited = PixelHomeLayout.Export(review);
            foreach (var state in data.objects)
            {
                var prop = Array.Find(props, p => p.ObjectId == state.objectId);
                Check(prop != null && prop.Position == state.position && Mathf.Approximately(prop.PlacementScale, state.scale)
                    && prop.Visible == state.visible && prop.Layer == state.layer && prop.DrawOrder == state.drawOrder,
                    state.objectId + " imported placement/visibility/depth");
            }
            Check(review.CharacterPosition == data.character.position && Mathf.Approximately(review.CharacterScale, data.character.scale),
                "shared character framing imports without changing appearance");
            string appearance = review.Character.Character.id;
            foreach (string body in new[] { "female-fat", "male-skinny" })
            {
                review.Character.Show(body, body, body);
                Check(Vector3.Distance(review.Character.FeetWorld, review.Room.Plane.TransformPoint(new Vector3(
                    data.character.position.x - review.Room.ReferenceSize.x / 2,
                    review.Room.ReferenceSize.y / 2 - data.character.position.y, 0))) < 1,
                    body + " keeps imported feet position");
            }
            review.Character.Show(appearance, appearance, appearance);
            PixelHomeLayout.Apply(review, edited);
            Check(PixelHomeLayout.Export(review) == edited, "edited export round-trips exactly");
            foreach (Action<PixelHomeLayout.Document> corrupt in new Action<PixelHomeLayout.Document>[] {
                d => d.version = 99, d => d.roomId = "other", d => d.referenceSize.x = 99,
                d => d.objects[0] = d.objects[1], d => d.objects[0].objectId = "unknown",
                d => d.objects[0].variantId = "unknown", d => d.objects[0].slotId = "hero.feet",
                d => d.objects[d.objects.Length - 1].position = new Vector2(-9999, 0),
                d => d.objects[0].scale = 0, d => d.objects[0].scale = float.NaN,
                d => d.objects[0].layer = "wall", d => d.objects[0].drawOrder = 10001,
                d => d.character.scale = 3, d => d.character = null,
                d => Array.Resize(ref d.objects, d.objects.Length - 1) })
            {
                var bad = JsonUtility.FromJson<PixelHomeLayout.Document>(edited); corrupt(bad); bool rejected = false;
                try { PixelHomeLayout.Apply(review, JsonUtility.ToJson(bad)); } catch (ArgumentException) { rejected = true; }
                Check(rejected && PixelHomeLayout.Export(review) == edited, "invalid layout rejected without partial mutation");
            }
            var ring = review.Ring; var source = ring.Artwork.sprite;
            ring.RestoreState(originalRing);
            Check(ring.Position == review.Room.AnchorPoint(ring.SlotId) && ring.PlacementScale == 1, "legacy v1 save restores slot defaults");
            PixelHomeLayout.Apply(review, edited); ring.SetVariant("base");
            Check(ring.Artwork.sprite == source && PixelHomeLayout.Export(review) == edited, "variant rebinding preserves edited placement");
            var swap = JsonUtility.FromJson<PixelHomeLayout.Document>(edited);
            var entry = Array.Find(swap.objects, s => s.objectId == ring.ObjectId);
            entry.layer = "foreground"; entry.drawOrder = 999;
            PixelHomeLayout.Apply(review, JsonUtility.ToJson(swap));
            Check(ring.transform.parent == review.Room.Foreground && ring.transform.GetSiblingIndex() == review.Room.Foreground.childCount - 1,
                "layer/order edits change actual Unity hierarchy");
            PixelHomeLayout.Apply(review, edited);
            var area = (RectTransform)review.Room.transform.parent; var oldMax = area.offsetMax;
            area.offsetMax = oldMax - new Vector2(123, 57);
            Canvas.ForceUpdateCanvases(); review.Room.RefreshLayout();
            foreach (var prop in props)
            {
                var p = prop.Position;
                var expected = review.Room.Plane.TransformPoint(new Vector3(p.x - review.Room.ReferenceSize.x / 2, review.Room.ReferenceSize.y / 2 - p.y, 0));
                Check(Vector3.Distance(expected, prop.transform.position) < 1, prop.ObjectId + " keeps imported anchor after resize");
            }
            area.offsetMax = oldMax; review.Relayout();
            yield return null; yield return new WaitForEndOfFrame();
            string capture = PixelButtonGallery.Argument("-sologym-capture", "");
            int failed = checks.FindAll(c => !c.passed).Count;
            if (!string.IsNullOrEmpty(capture))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(capture));
                var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(capture, texture.EncodeToPNG()); Destroy(texture);
                File.WriteAllText(Path.ChangeExtension(capture, ".json"), JsonUtility.ToJson(new Report { passed = checks.Count - failed, failed = failed, checks = checks }, true));
            }
            Debug.Log("HOME LAYOUT: " + (checks.Count - failed) + "/" + checks.Count + " passed");
            Application.Quit(failed == 0 ? 0 : 1);
        }
    }
}
