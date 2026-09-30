using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelChoiceBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelChoiceReview.unity";
        [MenuItem("SoloGym/Pixel UI/Create Choice Control Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelChoiceGallery>(ScenePath, "SoloGym Choice Control Review");
        [MenuItem("SoloGym/Pixel UI/Build Linux Choice Control Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelChoiceGallery>(ScenePath, "FantasyChoice", "SoloGymChoice", "SoloGym Choice Control Review");
    }
}
