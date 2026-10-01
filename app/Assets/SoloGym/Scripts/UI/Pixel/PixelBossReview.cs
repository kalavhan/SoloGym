using System.Collections;
using UnityEngine;
namespace SoloGym.UI
{
    /// <summary>Standalone fixture enters through the same journal readiness flow as Home.</summary>
    public sealed class PixelBossReview : MonoBehaviour
    {
        IEnumerator Start()
        {
            var window=new GameObject("Workout entry for dungeon").AddComponent<PixelWorkoutWindow>();window.AutomaticReview=false;
            window.Initialize(PixelWorkoutWindow.Arg("-sologym-locale","es"),PixelWorkoutWindow.Arg("-sologym-character","male-medium"),PixelWorkoutWindow.Has("-sologym-teen"),()=>{window.gameObject.SetActive(false);new GameObject("Home after dungeon journal").AddComponent<PixelTrainingHallHome>();Destroy(window.gameObject);},null,true);
            while(window.View=="loading")yield return null;
            for(int i=0;i<4;i++)yield return null;
            if(PixelWorkoutWindow.Has("-sologym-smoke"))yield return gameObject.AddComponent<PixelBossSmoke>().Run(window);
        }
    }
}
