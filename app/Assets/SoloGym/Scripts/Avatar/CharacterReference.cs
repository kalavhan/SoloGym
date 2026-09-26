using System;
using UnityEngine;

namespace SoloGym
{
    /// <summary>Authoritative rig registration for a fit family (preview anchors, camera presets).</summary>
    public static class CharacterReference
    {
        public enum CameraPresetId { FullBody, FaceCloseUp }

        [Serializable] sealed class ManifestRoot
        {
            public string fit_family_id;
            public GoldenCapture golden_capture;
            public StageAnchor stage_anchor;
            public CameraPresetsBlock camera_presets;
            public HitRegion[] hit_regions;
        }

        [Serializable] sealed class CameraPresetsBlock
        {
            public CameraPresetData fullBody, faceCloseUp;
        }

        [Serializable] public sealed class GoldenCapture
        {
            public AvatarAppearance recipe;
            public string resource_png, expected_sha256;
        }

        [Serializable] public sealed class StageAnchor
        {
            public float rig_pivot_x, rig_pivot_y, clip_width, clip_height;
        }

        [Serializable] public sealed class CameraPresetData
        {
            public float rig_scale, rig_offset_x, rig_offset_y;
            public bool animate_limbs;
            public string anchor_socket;
        }

        [Serializable] public sealed class HitRegion
        {
            public string id, category;
            public float[] rect;
        }

        static ManifestRoot manifest;

        public static void Load(string fitFamilyId = "proof_male_athletic")
        {
            var asset = Resources.Load<TextAsset>("AvatarReference/" + fitFamilyId + "/ReferenceManifest");
            manifest = asset == null ? null : JsonUtility.FromJson<ManifestRoot>(asset.text);
        }

        static ManifestRoot M
        {
            get
            {
                if (manifest == null) Load();
                return manifest;
            }
        }

        public static StageAnchor Stage => M?.stage_anchor ?? new StageAnchor
            { rig_pivot_x = .5f, rig_pivot_y = .58f, clip_width = 665, clip_height = 720 };

        public static HitRegion[] HitRegions => M?.hit_regions ?? Array.Empty<HitRegion>();

        public static AvatarAppearance GoldenRecipe()
        {
            if (M?.golden_capture?.recipe == null)
                return new AvatarAppearance { torsoItemId = null, handItemId = null };
            var r = JsonUtility.FromJson<AvatarAppearance>(JsonUtility.ToJson(M.golden_capture.recipe));
            if (string.IsNullOrEmpty(r.torsoItemId)) r.torsoItemId = null;
            if (string.IsNullOrEmpty(r.handItemId)) r.handItemId = null;
            return r;
        }

        public static string GoldenResourcePath() =>
            M?.golden_capture?.resource_png ?? "AvatarReference/proof_male_athletic_front_golden";

        public static string ExpectedGoldenSha256() => M?.golden_capture?.expected_sha256 ?? "pending_first_capture";

        public static CameraPresetData Preset(CameraPresetId id)
        {
            var block = M?.camera_presets;
            if (block == null)
                return id == CameraPresetId.FaceCloseUp
                    ? new CameraPresetData { rig_scale = 2.6f, rig_offset_y = -95f }
                    : new CameraPresetData { rig_scale = 1f, animate_limbs = true };
            return id == CameraPresetId.FaceCloseUp ? block.faceCloseUp : block.fullBody;
        }

        public static LayeredAvatar.AvatarPreviewMode ToPreviewMode(CameraPresetId id) =>
            id == CameraPresetId.FaceCloseUp
                ? LayeredAvatar.AvatarPreviewMode.FaceCloseUp
                : LayeredAvatar.AvatarPreviewMode.FullBody;
    }
}
