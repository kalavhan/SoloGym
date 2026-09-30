using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelCharacterBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelCharacterReview.unity";
        [MenuItem("SoloGym/Pixel UI/Create Character Viewport Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelCharacterGallery>(ScenePath, "SoloGym Character Viewport Review");
        [MenuItem("SoloGym/Pixel UI/Build Linux Character Viewport Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelCharacterGallery>(ScenePath, "FantasyCharacter", "SoloGymCharacter", "SoloGym Character Viewport Review");
    }
}
