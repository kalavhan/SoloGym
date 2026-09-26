using System;
namespace SoloGym
{
    /// <summary>Appearance choices only. No fitness measurements or combat rewards.</summary>
    [Serializable]
    public sealed class AvatarAppearance
    {
        public string baseId = "proof_male_athletic_v1", fitId = "proof_male_athletic", fitFamilyId = "proof_male_athletic";
        public string skinPaletteId = "warm", headId = "proof_head", hairId = "spiky", hairPaletteId = "black";
        public string eyesId = "proof_eyes", eyePaletteId = "dark", eyebrowsId = "proof_brows", mouthId = "proof_mouth";
        public string torsoItemId = "proof_training_top", handItemId = "proof_cyan_gloves";
        public string headItemId = "", legsItemId = "", feetItemId = "";

        public bool IsSupportedProof => AvatarCustomizationCatalog.ValidateRecipe(this);
    }
}
