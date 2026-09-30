using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelActionBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelActionReview.unity";
        [MenuItem("SoloGym/Pixel UI/Create Secondary Action Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelActionGallery>(ScenePath, "SoloGym Secondary Action Review");
        [MenuItem("SoloGym/Pixel UI/Build Linux Secondary Action Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelActionGallery>(
            ScenePath, "FantasyAction", "SoloGymAction", "SoloGym Secondary Action Review");
    }
}
