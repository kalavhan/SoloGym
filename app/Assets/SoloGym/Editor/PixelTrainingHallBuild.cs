using SoloGym.UI;
using UnityEditor;
namespace SoloGym.Editor
{
    public static class PixelTrainingHallBuild
    {
        public const string ScenePath="Assets/SoloGym/Scenes/PixelTrainingHallHome.unity";
        [MenuItem("SoloGym/Pixel UI/Build Training Hall Home")]
        public static void BuildLinux()=>PixelReviewBuild.BuildLinux<HomeScreen>(ScenePath,"TrainingHall","SoloGymTrainingHall","SoloGym Training Hall Home");
    }
}
