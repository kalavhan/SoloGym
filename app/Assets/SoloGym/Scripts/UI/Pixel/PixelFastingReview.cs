using System.Collections;
using UnityEngine;
namespace SoloGym.UI
{
    public sealed class PixelFastingReview : MonoBehaviour
    {
        IEnumerator Start()
        {
            var window=new GameObject("Optional local fasting").AddComponent<PixelFastingWindow>();
            bool smoke=PixelWorkoutWindow.Has("-sologym-smoke");
            window.Initialize(PixelWorkoutWindow.Arg("-sologym-locale","es"),!PixelWorkoutWindow.Has("-sologym-teen")&&!PixelWorkoutWindow.Has("-sologym-age-unknown"),route=>{
                window.gameObject.SetActive(false);Destroy(window.gameObject);
                if(route=="workouts"||route=="dungeon"){var journal=new GameObject("Workout journal").AddComponent<PixelWorkoutWindow>();journal.AutomaticReview=false;journal.Initialize(window.Language,PixelWorkoutWindow.Arg("-sologym-character","male-medium"),false,()=>{new GameObject("Home").AddComponent<PixelTrainingHallHome>();Destroy(journal.gameObject);},openReadiness:route=="dungeon");}
                else new GameObject("Home").AddComponent<PixelTrainingHallHome>();
            },smoke?new PixelFastingSmoke.Memory():null);
            for(int i=0;i<6;i++)yield return null;
            int exitCode=0;
            if(smoke){var checks=gameObject.AddComponent<PixelFastingSmoke>();yield return checks.Run(window);exitCode=checks.Passed?0:2;}
            string path=PixelWorkoutWindow.Arg("-sologym-capture");
            if(!string.IsNullOrEmpty(path)){yield return new WaitForEndOfFrame();window.Capture(path);}
            if((smoke||!string.IsNullOrEmpty(path))&&!PixelWorkoutWindow.Has("-sologym-stay-open"))Application.Quit(exitCode);
        }
    }
}
