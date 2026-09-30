using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelPanelBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelPanelReview.unity";

        [MenuItem("SoloGym/Pixel UI/Create Content Panel Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelPanelGallery>(ScenePath, "SoloGym Content Panel Review");

        [MenuItem("SoloGym/Pixel UI/Build Linux Content Panel Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelPanelGallery>(
            ScenePath, "FantasyPanel", "SoloGymPanel", "SoloGym Content Panel Review");
    }
}
