using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelButtonBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelButtonReview.unity";

        [MenuItem("SoloGym/Pixel UI/Create Primary Button Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelButtonGallery>(ScenePath, "SoloGym Pixel Primary Button Review");

        [MenuItem("SoloGym/Pixel UI/Build Linux Primary Button Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelButtonGallery>(
            ScenePath, "FantasyButton", "SoloGymButton", "SoloGym Pixel Primary Button Review");
    }
}
