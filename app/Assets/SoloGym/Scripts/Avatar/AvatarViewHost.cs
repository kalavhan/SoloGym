using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>The illustrated studio renderer, with an explicitly requested 3D engineering experiment.</summary>
    public sealed class AvatarViewHost : MonoBehaviour
    {
        RectTransform clip;
        LayeredAvatar avatar;
        Source3D.SourceCharacterView source;
        SoloGym.Illustrated.IllustratedAvatar2D illustrated;
        CharacterReference.CameraPresetId presetId = CharacterReference.CameraPresetId.FullBody;
        AvatarAppearance lastRecipe;
        string sourcePose = "idle";
        bool poseFrozen = true;
        float poseSeconds = .55f;

        public LayeredAvatar Avatar => avatar;
        public Source3D.SourceCharacterView Source => source;
        public SoloGym.Illustrated.IllustratedAvatar2D Illustrated => illustrated;
        public bool UsesSource3D => source != null;
        public bool UsesIllustrated => illustrated != null;
        public string CurrentPose => sourcePose;
        public bool PoseFrozen => poseFrozen;
        public float PoseTime => illustrated != null ? illustrated.PreviewTime : poseSeconds;
        public AvatarAppearance Recipe => lastRecipe;
        public string SourceCameraPreset { get; private set; } = "front";
        public float BodyShape { get; private set; } = .5f;
        public float TurntableYaw { get; private set; }

        public static bool IllustratedRequested
        {
            get
            {
                var args = System.Environment.GetCommandLineArgs();
                int index = System.Array.IndexOf(args, "-sologym-avatar-renderer");
                string renderer = index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
                return renderer == "illustrated" || (string.IsNullOrEmpty(renderer)
                    && System.Array.IndexOf(args, "-sologym-illustrated-proof") >= 0);
            }
        }

        public void Mount(RectTransform parent, Rect stageRect)
        {
            CharacterReference.Load();
            var args = System.Environment.GetCommandLineArgs();
            int rendererIndex = System.Array.IndexOf(args, "-sologym-avatar-renderer");
            string renderer = rendererIndex >= 0 && rendererIndex + 1 < args.Length ? args[rendererIndex + 1] : null;
            if (IllustratedRequested)
            {
                illustrated = gameObject.AddComponent<SoloGym.Illustrated.IllustratedAvatar2D>();
                illustrated.Mount(parent, stageRect);
                if (!illustrated.Load()) Debug.LogError("SOLOGYM_ILLUSTRATED_CHARACTER_LOAD " + illustrated.LastError);
                else illustrated.SetCharacter("male", "studio");
                illustrated.SetPose("idle", .55f, true);
                return;
            }
            bool sourceProof = System.Array.IndexOf(args, "-sologym-character-proof") >= 0;
            bool useSource3D = renderer == "source3d" || (string.IsNullOrEmpty(renderer) && sourceProof);
            if (useSource3D)
            {
                source = gameObject.AddComponent<Source3D.SourceCharacterView>();
                bool proof = sourceProof
                    || System.Array.IndexOf(args, "-sologym-avatar-reference-check") >= 0;
                source.Mount(parent, stageRect, proof);
                if (!source.Load()) Debug.LogError("SOLOGYM_SOURCE_CHARACTER_LOAD " + source.LastError);
                source.SetBodyShape(BodyShape);
                source.SetPose("idle", .55f, true);
                return;
            }
            var anchor = CharacterReference.Stage;
            float w = stageRect.width, h = stageRect.height;
            clip = SystemUI.Node("Avatar stage clip", parent, stageRect);
            clip.gameObject.AddComponent<RectMask2D>();
            var rigNode = SystemUI.Node("Avatar rig root", clip,
                new Rect(w * anchor.rig_pivot_x, h * anchor.rig_pivot_y, 0, 0));
            avatar = rigNode.gameObject.AddComponent<LayeredAvatar>();
            avatar.freeze = true;
            avatar.previewTime = 0.55f;
            avatar.action = "idle";
        }

        public void SetCameraPreset(CharacterReference.CameraPresetId id)
        {
            presetId = id;
            if (illustrated != null)
            {
                illustrated.SetCameraPreset(id == CharacterReference.CameraPresetId.FaceCloseUp ? "face" : "full");
                return;
            }
            if (source != null)
            {
                SetSourceCameraPreset(id == CharacterReference.CameraPresetId.FaceCloseUp ? "face" : "front");
                return;
            }
            if (avatar == null) return;
            avatar.previewMode = CharacterReference.ToPreviewMode(id);
            avatar.ApplyFramingFromReference();
        }

        public bool ApplyRecipe(AvatarAppearance recipe)
        {
            // The illustrated proof has its own explicit state and must never reuse a legacy recipe.
            if (illustrated != null) return false;
            if (recipe == null || !recipe.IsSupportedProof) return false;
            lastRecipe = recipe;
            if (source != null)
            {
                return source.ApplyRecipe(recipe);
            }
            if (avatar == null) return false;
            if (!avatar.IsInitialized)
            {
                avatar.Initialize(recipe);
                SetCameraPreset(presetId);
                return true;
            }
            avatar.previewMode = CharacterReference.ToPreviewMode(presetId);
            avatar.ApplyFramingFromReference();
            return avatar.Apply(recipe);
        }

        public void SetPreviewTime(float t)
        {
            poseSeconds = t;
            poseFrozen = true;
            if (illustrated != null) illustrated.SetPose(sourcePose, t, true);
            if (source != null) source.SetPose(sourcePose, t, true);
            if (avatar != null) avatar.previewTime = t;
        }

        public void SetSourceCameraPreset(string camera)
        {
            if (source == null) return;
            source.SetCameraPreset(camera);
            SourceCameraPreset = camera;
        }

        public void SetBodyShape(float shape)
        {
            BodyShape = Mathf.Clamp01(shape);
            if (source != null) source.SetBodyShape(BodyShape);
        }

        public void SetPose(string pose, float seconds, bool freeze = true)
        {
            sourcePose = pose;
            poseSeconds = seconds;
            poseFrozen = freeze;
            if (illustrated != null) illustrated.SetPose(pose, seconds, freeze);
            if (source != null) source.SetPose(pose, seconds, freeze);
            if (avatar == null) return;
            avatar.action = pose;
            avatar.previewTime = seconds;
            avatar.freeze = freeze;
        }

        public void Rotate()
        {
            if (illustrated != null)
                illustrated.SetCharacter(illustrated.Presentation, illustrated.View == "studio" ? "gameplay" : "studio");
            else if (source != null)
            {
                TurntableYaw = Mathf.Repeat(TurntableYaw + 45, 360);
                source.SetTurntable(TurntableYaw);
            }
            else if (avatar != null) avatar.facingLeft = !avatar.facingLeft;
        }

        public void ResetTurntable()
        {
            TurntableYaw = 0;
            if (source != null) source.SetTurntable(0);
        }

        public bool SetIllustratedCharacter(string presentation, string view)
            => illustrated != null && illustrated.SetCharacter(presentation, view);

        public void SetIllustratedColors(string skin, string hair)
        {
            if (illustrated != null) illustrated.SetColors(skin, hair);
        }

        public void SetIllustratedEquipped(bool equipped)
        {
            if (illustrated != null) illustrated.SetEquipped(equipped);
        }
    }
}
