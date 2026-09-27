using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace SoloGym.Illustrated
{
    /// <summary>Bounded captures of the live illustrated renderer; structural evidence is not art approval.</summary>
    public static class IllustratedCharacterProofRunner
    {
        const string Resource = "AvatarIllustrated/Registration";
        const int Fps = 12, FrameCount = 24;

        [Serializable] sealed class Frame
        {
            public string id, png, presentation, camera, outfit, skin, hair, action;
            public string entry_id, source_revision, sequence_id, validation_detail, error;
            public int frame_index = -1, width, height, invalid_vertex_count, mesh_vertex_count;
            public float pose_seconds, ground_error, registration_ground_error, weight_sum_error, max_vertex_displacement;
            public bool structural_passed;
        }

        [Serializable] sealed class MotionSequence
        {
            public string id, presentation, camera, outfit = "equipped", skin = "source", hair = "black", action, png_pattern;
            public int fps = Fps, frame_count = FrameCount;
        }

        [Serializable] sealed class StudioScreenshot
        {
            public string png, presentation, camera = "studio", outfit = "equipped", action = "bind";
            public int width, height;
        }

        [Serializable] sealed class Report
        {
            public string schema = "sologym.illustrated-character-proof.v1";
            public string renderer = "illustrated";
            public string capture_scope = "runtime_character";
            public string source_manifest_resource = Resource;
            public string source_manifest_path = "app/Assets/SoloGym/Resources/AvatarIllustrated/Registration.json";
            public string source_manifest_sha256;
            public string ground_error_units = "source_cell_pixels";
            public float ground_error_tolerance = .05f;
            public bool structural_passed;
            public string visual_review_status = "pending";
            public string ground_error_scope = "Pose drift of the source alpha sole contour through the rendered mesh; registration_ground_error separately records source metadata-to-alpha registration offset.";
            public string visual_review_scope = "Inspect silhouette, skin/hair recoloring, clothing, joint deformation, action readability and foot contact. Source alpha sole-contour checks do not establish visual acceptance. This bounded proof swaps coherent base/equipped surfaces; it does not prove modular garments.";
            public string[] checks, failures;
            public Frame[] frames;
            public MotionSequence[] motion_sequences;
            public StudioScreenshot[] studio_screenshots;
        }

        public static IEnumerator Run(AvatarViewHost host, string outputDirectory, Action refreshUI, Action<bool> completed, string setupError = null)
        {
            var report = new Report();
            var frames = new List<Frame>();
            var sequences = new List<MotionSequence>();
            var studios = new List<StudioScreenshot>();
            var checks = new List<string>();
            var failures = new List<string>();
            checks.Add("illustrated command-line options valid");
            if (!string.IsNullOrEmpty(setupError)) failures.Add(setupError);
            string directory = null;
            try { directory = Path.GetFullPath(outputDirectory); Directory.CreateDirectory(directory); }
            catch (Exception ex) { failures.Add("Cannot create proof directory: " + ex.Message); }
            var avatar = host != null ? host.Illustrated : null;
            checks.Add("illustrated renderer selected and source loaded");
            if (avatar == null || !avatar.IsLoaded) failures.Add("Illustrated renderer not loaded: " + (avatar != null ? avatar.LastError : "renderer not selected"));
            var asset = Resources.Load<TextAsset>(Resource);
            checks.Add("loaded source manifest hash recorded");
            if (asset == null) failures.Add("Missing Resources/" + Resource);
            else
                using (var sha = SHA256.Create())
                    report.source_manifest_sha256 = BitConverter.ToString(sha.ComputeHash(asset.bytes)).Replace("-", "").ToLowerInvariant();

            if (avatar != null && avatar.IsLoaded && directory != null && asset != null && string.IsNullOrEmpty(setupError))
            {
                foreach (string presentation in new[] { "male", "female" })
                foreach (string camera in new[] { "studio", "gameplay" })
                {
                    foreach (string outfit in new[] { "base", "equipped" })
                    {
                        string id = presentation + "-" + camera + "-" + outfit + "-bind";
                        var frame = NewFrame(id, "static/" + id + ".png", presentation, camera, outfit, "source", "black", "bind", 0);
                        Configure(host, frame, refreshUI);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        yield return new WaitForEndOfFrame();
                        CaptureFrame(avatar, directory, frame, checks, failures);
                        frames.Add(frame);
                        if (camera == "studio" && outfit == "equipped")
                        {
                            try
                            {
                                var studio = new StudioScreenshot { png = presentation + "-studio-menu.png", presentation = presentation,
                                    width = Screen.width, height = Screen.height };
                                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                                try { File.WriteAllBytes(Path.Combine(directory, studio.png), screenshot.EncodeToPNG()); }
                                finally { UnityEngine.Object.Destroy(screenshot); }
                                studios.Add(studio);
                            }
                            catch (Exception ex) { failures.Add("Studio context screenshot: " + ex.Message); }
                        }
                    }
                    foreach (bool hairOnly in new[] { false, true })
                    {
                        string id = presentation + "-" + camera + (hairOnly ? "-hair-silver" : "-skin-deep");
                        var frame = NewFrame(id, "static/" + id + ".png", presentation, camera, "equipped",
                            hairOnly ? "source" : "deep", hairOnly ? "silver" : "black", "bind", 0);
                        Configure(host, frame, refreshUI);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        yield return new WaitForEndOfFrame();
                        CaptureFrame(avatar, directory, frame, checks, failures);
                        frames.Add(frame);
                    }
                    foreach (string outfit in new[] { "equipped", "base" })
                    foreach (string action in new[] { "idle", "walk", "jab" })
                    {
                        // Retain equipped IDs so existing review pages and videos remain valid.
                        string id = presentation + "-" + camera + (outfit == "base" ? "-base-" : "-") + action;
                        sequences.Add(new MotionSequence { id = id, presentation = presentation, camera = camera, action = action,
                            outfit = outfit, png_pattern = "motion/" + id + "/frame-%03d.png" });
                        for (int index = 0; index < FrameCount; index++)
                        {
                            var frame = NewFrame(id + "-" + index.ToString("D3"), "motion/" + id + "/frame-" + index.ToString("D3") + ".png",
                                presentation, camera, outfit, "source", "black", action, index / (float)Fps);
                            frame.sequence_id = id;
                            frame.frame_index = index;
                            if (index == 0) Configure(host, frame, refreshUI);
                            else host.SetPose(action, frame.pose_seconds, true);
                            yield return null;
                            Canvas.ForceUpdateCanvases();
                            yield return new WaitForEndOfFrame();
                            CaptureFrame(avatar, directory, frame, checks, failures);
                            frames.Add(frame);
                        }
                        Debug.Log("SOLOGYM_ILLUSTRATED_SEQUENCE " + id + " frames=" + FrameCount);
                    }
                }
            }
            report.structural_passed = failures.Count == 0 && frames.Count == 592 && sequences.Count == 24 && studios.Count == 2;
            report.frames = frames.ToArray();
            report.motion_sequences = sequences.ToArray();
            report.studio_screenshots = studios.ToArray();
            report.checks = checks.ToArray();
            report.failures = failures.ToArray();
            if (directory != null)
            {
                try { File.WriteAllText(Path.Combine(directory, "illustrated-proof.json"), JsonUtility.ToJson(report, true) + "\n"); }
                catch (Exception ex) { Debug.LogError("SOLOGYM_ILLUSTRATED_PROOF_WRITE " + ex.Message); report.structural_passed = false; }
            }
            Debug.Log("SOLOGYM_ILLUSTRATED_PROOF structural_passed=" + report.structural_passed + " visual_review_status=pending output=" + directory);
            foreach (string failure in failures) Debug.LogError("SOLOGYM_ILLUSTRATED_PROOF_FAILED " + failure);
            completed(report.structural_passed);
        }

        static Frame NewFrame(string id, string png, string presentation, string camera, string outfit, string skin, string hair, string action, float seconds)
            => new Frame { id = id, png = png, presentation = presentation, camera = camera, outfit = outfit,
                skin = skin, hair = hair, action = action, pose_seconds = seconds, sequence_id = "" };

        static void Configure(AvatarViewHost host, Frame frame, Action refreshUI)
        {
            try
            {
                if (!host.SetIllustratedCharacter(frame.presentation, frame.camera)) throw new InvalidOperationException(host.Illustrated.LastError);
                host.SetIllustratedEquipped(frame.outfit == "equipped");
                host.SetIllustratedColors(frame.skin, frame.hair);
                host.SetCameraPreset(CharacterReference.CameraPresetId.FullBody);
                host.SetPose(frame.action, frame.pose_seconds, true);
                refreshUI?.Invoke();
            }
            catch (Exception ex) { frame.error = ex.Message; }
        }

        static void CaptureFrame(IllustratedAvatar2D avatar, string directory, Frame frame, List<string> checks, List<string> failures)
        {
            try
            {
                if (!string.IsNullOrEmpty(frame.error)) throw new InvalidOperationException(frame.error);
                if (!avatar.TryValidate(out string reason)) throw new InvalidOperationException(reason);
                frame.validation_detail = reason;
                frame.entry_id = avatar.CurrentEntryId;
                frame.source_revision = avatar.SourceRevision;
                frame.ground_error = avatar.GroundError;
                frame.registration_ground_error = avatar.RegistrationGroundError;
                frame.weight_sum_error = avatar.WeightSumError;
                frame.invalid_vertex_count = avatar.InvalidVertexCount;
                frame.mesh_vertex_count = avatar.MeshVertexCount;
                frame.max_vertex_displacement = avatar.MaxVertexDisplacement;
                if (avatar.Presentation != frame.presentation || avatar.View != frame.camera || avatar.IsEquipped != (frame.outfit == "equipped")
                    || avatar.SkinPaletteId != frame.skin || avatar.HairPaletteId != frame.hair || avatar.CurrentAction != frame.action)
                    throw new InvalidOperationException("Live illustrated state does not match the requested frame.");
                string path = Path.Combine(directory, frame.png);
                avatar.CapturePng(path);
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < 128 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71)
                    throw new InvalidOperationException("Capture is not a nonempty PNG.");
                frame.width = ReadInt32(bytes, 16);
                frame.height = ReadInt32(bytes, 20);
                if (frame.width <= 0 || frame.height <= 0) throw new InvalidOperationException("Capture dimensions are invalid.");
                frame.structural_passed = true;
            }
            catch (Exception ex) { frame.error = ex.Message; }
            checks.Add(frame.id + ": live state, registered mesh and renderer PNG");
            if (!frame.structural_passed) failures.Add(frame.id + ": " + frame.error);
        }

        static int ReadInt32(byte[] bytes, int offset)
            => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
    }
}
