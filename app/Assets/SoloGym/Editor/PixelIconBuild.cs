using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelIconBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelIconReview.unity";
        [MenuItem("SoloGym/Pixel UI/Create Icon Button Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelIconGallery>(ScenePath, "SoloGym Icon Button Review");
        [MenuItem("SoloGym/Pixel UI/Build Linux Icon Button Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelIconGallery>(
            ScenePath, "FantasyIcon", "SoloGymIcon", "SoloGym Icon Button Review");
    }
}
