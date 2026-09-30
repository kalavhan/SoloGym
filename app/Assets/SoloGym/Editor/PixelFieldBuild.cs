using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelFieldBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelFieldReview.unity";

        [MenuItem("SoloGym/Pixel UI/Create Form Field Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelFieldGallery>(ScenePath, "SoloGym Form Field Review");

        [MenuItem("SoloGym/Pixel UI/Build Linux Form Field Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelFieldGallery>(
            ScenePath, "FantasyField", "SoloGymField", "SoloGym Form Field Review");
    }
}
