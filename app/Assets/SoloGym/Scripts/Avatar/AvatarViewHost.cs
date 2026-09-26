using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Single compositor instance with manifest-driven camera presets; recipe updates in place.</summary>
    public sealed class AvatarViewHost : MonoBehaviour
    {
        RectTransform clip;
        LayeredAvatar avatar;
        CharacterReference.CameraPresetId presetId = CharacterReference.CameraPresetId.FullBody;
        AvatarAppearance lastRecipe;

        public LayeredAvatar Avatar => avatar;

        public void Mount(RectTransform parent, Rect stageRect)
        {
            CharacterReference.Load();
            var anchor = CharacterReference.Stage;
            float w = anchor.clip_width > 0 ? anchor.clip_width : stageRect.width;
            float h = anchor.clip_height > 0 ? anchor.clip_height : stageRect.height;
            float px = stageRect.x + stageRect.width * anchor.rig_pivot_x;
            float py = stageRect.y + stageRect.height * anchor.rig_pivot_y;
            clip = SystemUI.Node("Avatar stage clip", parent, new Rect(px - w * 0.5f, py - h * 0.55f, w, h));
            clip.gameObject.AddComponent<RectMask2D>();
            var rigNode = SystemUI.Node("Avatar rig root", clip, new Rect(w * 0.5f, h * 0.55f, 0, 0));
            avatar = rigNode.gameObject.AddComponent<LayeredAvatar>();
            avatar.freeze = true;
            avatar.previewTime = 0.55f;
            avatar.action = "idle";
        }

        public void SetCameraPreset(CharacterReference.CameraPresetId id)
        {
            presetId = id;
            if (avatar == null) return;
            avatar.previewMode = CharacterReference.ToPreviewMode(id);
            avatar.ApplyFramingFromReference();
        }

        public bool ApplyRecipe(AvatarAppearance recipe)
        {
            if (avatar == null || recipe == null) return false;
            lastRecipe = recipe;
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
            if (avatar != null) avatar.previewTime = t;
        }
    }
}
