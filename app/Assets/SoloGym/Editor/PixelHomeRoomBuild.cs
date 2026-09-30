using SoloGym.UI;
using UnityEditor;

namespace SoloGym.Editor
{
    public static class PixelHomeRoomBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelHomeRoomReview.unity";
        [MenuItem("SoloGym/Pixel UI/Create Home Room Review")]
        public static void CreateScene() => PixelReviewBuild.CreateScene<PixelHomeRoomReview>(ScenePath, "SoloGym Home Room Review");
        [MenuItem("SoloGym/Pixel UI/Build Linux Home Room Review")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelHomeRoomReview>(ScenePath, "FantasyHomeRoom", "SoloGymHomeRoom", "SoloGym Home Room Review");
    }
}
