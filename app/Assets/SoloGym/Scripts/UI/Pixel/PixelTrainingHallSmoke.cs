using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    public sealed class PixelTrainingHallSmoke : MonoBehaviour
    {
        readonly List<string> checks=new List<string>();readonly List<string> failures=new List<string>();
        void Check(bool value,string name){checks.Add(name);if(!value){failures.Add(name);Debug.LogError("HALL CHECK FAILED: "+name);}}
        public IEnumerator Run(PixelTrainingHallHome home)
        {
            yield return null;
            Check(home.GetComponent<HomeScreen>()!=null&&!home.GetComponent<HomeScreen>().enabled,"Existing Home entry hands off to landscape hall");
            Check(home.Room.RoomId=="home.training-hall.r1","Correct room identity");
            Check(home.Props.Count>=14,"Separate placeable equipment and decor");
            string original=home.CharacterId;
            foreach(var gender in new[]{"male","female"})foreach(var body in new[]{"skinny","medium","fat","muscular"})
            {Check(home.SelectCharacter(gender+"-"+body,false),"Load "+gender+"-"+body);Check(home.Hero.sprite.rect.width<=256&&home.Hero.sprite.rect.height<=256,"Character canvas limit "+gender+"-"+body);}
            Check(!home.SelectCharacter("missing",false)&&home.CharacterId=="female-muscular","Invalid appearance keeps last valid sprite");home.SelectCharacter(original,false);
            home.Controller.SetReadiness(HomeReadiness.Ready);home.Controller.SetTeenProfile(false);home.Controller.SetMode(HomeMode.Training);
            home.TrainButton.OnSubmit(new BaseEventData(EventSystem.current));
            Check(home.LastRoute!=null&&home.LastRoute.WindowId=="WIN-016"&&home.LastRoute.RequiresReadinessRecheck,"Primary action preserves readiness check");
            home.Controller.SetMode(HomeMode.Saved);home.TrainButton.OnSubmit(new BaseEventData(EventSystem.current));
            Check(home.LastRoute!=null&&home.LastRoute.SessionId!=null&&home.LastRoute.RequiresReadinessRecheck,"Saved session cannot bypass new readiness check");
            home.Controller.SetReadiness(HomeReadiness.Ill);home.TrainButton.OnSubmit(new BaseEventData(EventSystem.current));Check(home.LastRoute.WindowId=="WIN-028","Illness routes to recovery");
            home.Controller.SetReadiness(HomeReadiness.Ready);home.Controller.SetTeenProfile(true,false);Check(!home.FastingVisible,"Teen fasting route is absent");
            home.Controller.SetMode(HomeMode.Training);Check(home.Controller.Model.Mode==HomeMode.Review,"Teen strength supervision preserved");
            home.Controller.SetTeenProfile(false);home.Controller.SetConnectivity(HomeConnectivity.Loading,false);Check(!home.TrainButton.IsInteractable(),"Loading data disables training");
            home.Controller.SetConnectivity(HomeConnectivity.Online,true);home.Controller.SetMode(HomeMode.Training);
            home.Navigation.Tab("workouts").TryNavigate();Check(home.LastRoute.WindowId=="WIN-015"&&home.Navigation.CurrentId=="home","Navigation emits route without false current destination");
            var rug=home.Props.Find(x=>x.ObjectId=="home.exercise-rug");
            Check(rug.Overlay!=null&&rug.Overlay.transform.parent==rug.transform,"Rug trim shares the placed object");
            rug.SetVisible(false);Check(!rug.Artwork.enabled&&!rug.Overlay.enabled,"Hidden rug hides fabric and trim");
            rug.SetVisible(true);Check(rug.Artwork.enabled&&rug.Overlay.enabled,"Shown rug restores fabric and trim");
            string layout=home.ExportLayout();home.BeginDecorate();home.AdjustProp(2,0,0);home.CancelDecorate();Check(home.ExportLayout()==layout,"Cancel restores decoration draft");
            home.ApplyLayout(layout);Check(home.ExportLayout()==layout,"Layout roundtrip preserves every object");
            var invalid=JsonUtility.FromJson<PixelTrainingHallHome.LayoutSave>(layout);invalid.objects[0].visible=!invalid.objects[0].visible;invalid.objects[invalid.objects.Length-1].objectId="missing";
            bool rejected=false;try{home.ApplyLayout(JsonUtility.ToJson(invalid));}catch(ArgumentException){rejected=true;}
            Check(rejected&&home.ExportLayout()==layout,"Invalid layout is rejected atomically");
            home.ShowSettings();Check(home.Modal!=null,"Settings and appearance controls open");
            Check(!home.TrainButton.IsInteractable()&&!home.Navigation.Tab("workouts").TryNavigate(),"Modal blocks underlying keyboard and pointer controls");
            home.CloseModal();Check(home.TrainButton.IsInteractable(),"Closing modal restores Home controls");
            home.Controller.SetLanguage(PixelTrainingHallHome.Arg("-sologym-locale","es"));
            if(PixelTrainingHallHome.Has("-sologym-teen"))home.Controller.SetTeenProfile(true,false);
            home.Relayout();yield return null;
            var corners=new Vector3[4];home.Composition.GetWorldCorners(corners);Check(corners[0].x>=-.1f&&corners[0].y>=-.1f&&corners[2].x<=Screen.width+.1f&&corners[2].y<=Screen.height+.1f,"Composition fits landscape viewport");
            foreach(var t in home.Navigation.Tabs)if(t.gameObject.activeSelf)Check(t.Label.preferredWidth<=t.Label.rectTransform.rect.width+1,"Navigation label fits one line: "+t.Id);
            foreach(var label in home.Composition.GetComponentsInChildren<Text>())
                if(label.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(label.text))
                    Check(label.preferredHeight<=label.rectTransform.rect.height+1,"Visible text fits: "+label.text.Replace("\n"," / "));
            string capture=PixelTrainingHallHome.Arg("-sologym-capture");
            var report=new Report{passed=failures.Count==0,checkCount=checks.Count,checks=checks.ToArray(),failures=failures.ToArray(),width=Screen.width,height=Screen.height};
            string json=JsonUtility.ToJson(report,true);if(!string.IsNullOrEmpty(capture)){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));File.WriteAllText(Path.ChangeExtension(capture,".smoke.json"),json);}
            Debug.Log("TRAINING_HALL_SMOKE "+json);if(failures.Count>0){Application.Quit(2);yield break;}
        }
        [Serializable] sealed class Report {public bool passed;public int checkCount,width,height;public string[] checks,failures;}
    }
}
