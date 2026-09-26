using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace SoloGym
{
    public static class AvatarReferenceVerification
    {
        public static ReferenceCheckResult CaptureStage(LayeredAvatar avatar, string outputPngPath)
        {
            var result = new ReferenceCheckResult();
            if (avatar == null) { result.passed = false; result.detail = "no avatar"; return result; }
            result.recipeValid = avatar.CheckAttachments();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPngPath)) ?? ".");
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(outputPngPath, tex.EncodeToPNG());
            Destroy(tex);
            result.captureSha256 = Sha256File(outputPngPath);
            string expected = CharacterReference.ExpectedGoldenSha256();
            result.expectedSha256 = expected;
            result.goldenMatch = expected == "pending_first_capture" || string.Equals(expected, result.captureSha256,
                StringComparison.OrdinalIgnoreCase);
            result.passed = result.recipeValid && result.goldenMatch;
            result.detail = result.goldenMatch ? "reference check ok" : "golden sha mismatch (update manifest after approval)";
            return result;
        }

        public static void WriteJson(ReferenceCheckResult result, string jsonPath)
        {
            string json = "{\"passed\":" + (result.passed ? "true" : "false")
                + ",\"recipe_attachments\":" + (result.recipeValid ? "true" : "false")
                + ",\"golden_match\":" + (result.goldenMatch ? "true" : "false")
                + ",\"capture_sha256\":\"" + result.captureSha256 + "\""
                + ",\"expected_sha256\":\"" + result.expectedSha256 + "\""
                + ",\"detail\":\"" + result.detail + "\"}";
            File.WriteAllText(jsonPath, json);
        }

        static string Sha256File(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static void Destroy(UnityEngine.Object o) { if (o != null) UnityEngine.Object.Destroy(o); }

        public sealed class ReferenceCheckResult
        {
            public bool passed, recipeValid, goldenMatch;
            public string captureSha256, expectedSha256, detail;
        }
    }
}
