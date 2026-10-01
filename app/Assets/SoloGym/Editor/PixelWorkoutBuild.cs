using SoloGym.UI;
using UnityEditor;
namespace SoloGym.Editor
{
    public static class PixelWorkoutBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelWorkoutReview.unity";
        [MenuItem("SoloGym/Pixel UI/Build Workout Journal")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<PixelWorkoutWindow>(ScenePath, "Workouts", "SoloGymWorkouts", "SoloGym Workout Journal");
    }
}
