using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelNavigationBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelNavigationReview.unity";
        [MenuItem("SoloGym/Pixel UI/Create Navigation Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelNavigationGallery>(ScenePath, "SoloGym Navigation Review");
        [MenuItem("SoloGym/Pixel UI/Build Linux Navigation Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelNavigationGallery>(ScenePath, "FantasyNavigation", "SoloGymNavigation", "SoloGym Navigation Review");
    }
}
