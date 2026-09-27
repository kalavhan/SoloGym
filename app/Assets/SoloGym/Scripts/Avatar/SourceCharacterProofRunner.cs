using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SoloGym.Source3D
{
    /// <summary>Repeatable structural evidence and review images from the same source used by the studio.</summary>
    public static class SourceCharacterProofRunner
    {
        const string SourceResource = "AvatarSource3D/Character";

        [Serializable] sealed class Frame
        {
            public string id, png, camera, pose, skin, hair, torso, hands, error, validation_detail;
            public float pose_seconds, body_shape, ground_contact_error_metres;
            public int width, height, active_meshes;
            public bool structural_passed;
        }

        [Serializable] sealed class Report
        {
            public string schema = "sologym.character-source-proof.v1";
            public string renderer = "source3d";
            public string source_resource = SourceResource;
            public string source_sha256;
            public bool structural_passed;
            public string visual_review_status = "pending";
            public string visual_review_scope = "Inspect anatomy, joint deformation, clothing intersections, hair, facial features, silhouette and ground contact in every PNG. Structural checks do not approve appearance.";
            public string[] checks, failures;
            public Frame[] frames;
        }

        sealed class Shot
        {
            public string id, camera, pose;
            public float shape, seconds;
            public bool bare, alternate;
            public Shot(string id, string camera, bool bare = false, string pose = "idle",
                float shape = .5f, bool alternate = false, float seconds = .55f)
            {
                this.id = id; this.camera = camera; this.bare = bare; this.pose = pose;
                this.shape = shape; this.alternate = alternate; this.seconds = seconds;
            }
        }

        public static IEnumerator Run(AvatarViewHost host, string outputDirectory, Action<bool> completed)
        {
            var checks = new List<string>();
            var failures = new List<string>();
            var frames = new List<Frame>();
            var report = new Report();
            string directory = null;
            try { directory = Path.GetFullPath(outputDirectory); Directory.CreateDirectory(directory); }
            catch (Exception e) { failures.Add("Cannot create proof directory: " + e.Message); }
            var source = host != null ? host.Source : null;
            checks.Add("source3d renderer selected");
            if (source == null) failures.Add("Source proof requires the source3d renderer. Remove -sologym-avatar-renderer legacy.");
            checks.Add("source character loaded");
            if (source != null && !source.IsLoaded) failures.Add("Source load failed: " + source.LastError);
            var sourceAsset = Resources.Load<TextAsset>(SourceResource);
            checks.Add("source resource recorded");
            if (sourceAsset == null) failures.Add("Missing Resources/" + SourceResource);
            else
            {
                using (var hash = SHA256.Create())
                    report.source_sha256 = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(sourceAsset.text))).Replace("-", "").ToLowerInvariant();
            }
            if (source != null && source.IsLoaded && directory != null)
            {
                var shots = new List<Shot>();
                foreach (string camera in new[] { "front", "side", "back", "three-quarter", "gameplay" })
                    shots.Add(new Shot("bare-" + camera, camera, true));
                foreach (string camera in new[] { "front", "side", "back", "three-quarter", "gameplay" })
                    shots.Add(new Shot("equipped-" + camera, camera));
                shots.Add(new Shot("equipped-face", "face"));
                shots.Add(new Shot("alternate-face", "face", alternate: true));
                shots.Add(new Shot("alternate-gameplay", "gameplay", alternate: true));
                shots.Add(new Shot("walk-gameplay", "gameplay", pose: "walk", seconds: .37f));
                shots.Add(new Shot("jab-gameplay", "gameplay", pose: "jab", seconds: .25f));
                shots.Add(new Shot("bare-walk-gameplay", "gameplay", bare: true, pose: "walk", seconds: .37f));
                shots.Add(new Shot("bare-jab-gameplay", "gameplay", bare: true, pose: "jab", seconds: .25f));
                shots.Add(new Shot("walk-side", "side", pose: "walk", seconds: .37f));
                shots.Add(new Shot("jab-side", "side", pose: "jab", seconds: .25f));
                shots.Add(new Shot("shape-min-equipped", "three-quarter", shape: 0));
                shots.Add(new Shot("shape-mid-equipped", "three-quarter", shape: .5f));
                shots.Add(new Shot("shape-max-equipped", "three-quarter", shape: 1));
                foreach (var shot in shots)
                {
                    var recipe = new AvatarAppearance();
                    if (shot.bare) { recipe.torsoItemId = ""; recipe.handItemId = ""; }
                    if (shot.alternate) { recipe.skinPaletteId = "deep"; recipe.hairId = "swept"; }
                    var frame = new Frame
                    {
                        id = shot.id, png = shot.id + ".png", camera = shot.camera, pose = shot.pose,
                        pose_seconds = shot.seconds, body_shape = shot.shape, skin = recipe.skinPaletteId,
                        hair = recipe.hairId, torso = recipe.torsoItemId, hands = recipe.handItemId
                    };
                    try
                    {
                        if (!host.ApplyRecipe(recipe)) throw new InvalidOperationException("Source rejected the proof recipe.");
                        host.SetBodyShape(shot.shape);
                        host.ResetTurntable();
                        host.SetSourceCameraPreset(shot.camera);
                        host.SetPose(shot.pose, shot.seconds, true);
                    }
                    catch (Exception e) { frame.error = e.Message; }
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    yield return new WaitForEndOfFrame();
                    if (string.IsNullOrEmpty(frame.error))
                    {
                        try
                        {
                            if (!source.TryValidate(out string reason)) throw new InvalidOperationException(reason);
                            frame.validation_detail = reason;
                            frame.ground_contact_error_metres = source.GroundContactError;
                            if (source.IsPartVisible("torso") == shot.bare || source.IsPartVisible("hands") == shot.bare)
                                throw new InvalidOperationException("Equipment visibility does not match the requested recipe.");
                            string path = Path.Combine(directory, frame.png);
                            source.CapturePng(path);
                            if (!File.Exists(path) || new FileInfo(path).Length < 128)
                                throw new InvalidOperationException("Source capture is missing or empty.");
                            frame.width = source.RenderTexture.width;
                            frame.height = source.RenderTexture.height;
                            frame.active_meshes = source.ActiveMeshCount;
                            frame.structural_passed = true;
                        }
                        catch (Exception e) { frame.error = e.Message; }
                    }
                    checks.Add(shot.id + ": valid source rig, recipe visibility and PNG written");
                    if (!frame.structural_passed) failures.Add(shot.id + ": " + frame.error);
                    frames.Add(frame);
                }
            }
            report.structural_passed = failures.Count == 0 && frames.Count == 22;
            report.frames = frames.ToArray();
            report.checks = checks.ToArray();
            report.failures = failures.ToArray();
            string json = JsonUtility.ToJson(report, true);
            if (directory != null)
            {
                try { File.WriteAllText(Path.Combine(directory, "source-proof.json"), json + "\n"); }
                catch (Exception e) { Debug.LogError("SOLOGYM_SOURCE_PROOF_WRITE " + e.Message); report.structural_passed = false; }
            }
            Debug.Log("SOLOGYM_SOURCE_PROOF structural_passed=" + report.structural_passed + " visual_review_status=pending output=" + directory);
            foreach (string failure in failures) Debug.LogError("SOLOGYM_SOURCE_PROOF_FAILED " + failure);
            completed(report.structural_passed);
        }
    }
}
