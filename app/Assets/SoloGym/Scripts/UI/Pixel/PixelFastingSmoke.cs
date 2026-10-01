using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SoloGym.UI
{
    public sealed class PixelFastingSmoke : MonoBehaviour
    {
        public sealed class Memory : IJournalStorage
        {
            public string Data; public int Reads,Writes; public bool Fail;
            public string Read(){Reads++;return Data;}
            public void Write(string value){if(Fail)throw new IOException("Injected failure");Data=value;Writes++;}
        }
        [Serializable] sealed class Result { public string scenario; public int checks,failures; public string[] problems; }
        int checks;readonly List<string> failures=new List<string>();
        public bool Passed => failures.Count == 0;
        void Check(bool yes,string label){checks++;if(!yes){failures.Add(label);Debug.LogError("FASTING_SMOKE_FAIL "+label);}}
        void Models()
        {
            var now=new DateTimeOffset(2026,10,1,12,0,0,TimeSpan.Zero);var memory=new Memory();var tracker=new FastingTracker(memory,true,()=>now);tracker.Load();
            Check(tracker.Loaded&&!tracker.Data.enabled&&memory.Writes==0,"Off by default, empty and no implicit persistence");
            Check(!tracker.Start(tracker.Now,720)&&!tracker.Enable(720,false)&&!tracker.Enable(1260,true),"Cannot start disabled, bypass acknowledgement, or select >20 hours");
            Check(tracker.Enable(720,true),"Adult explicitly enables");
            Check(!tracker.Start(tracker.Now+1,720),"Reject future start");
            long initial=tracker.Now-8*3600;memory.Fail=true;Check(!tracker.Start(initial,720)&&tracker.Data.active==null,"Failed start preserves inactive state");memory.Fail=false;
            Check(tracker.Start(initial,720)&&!tracker.Start(initial,720),"Single active session");
            Check(!tracker.Disable()&&tracker.Data.enabled,"Disabling cannot silently discard active data");
            now=now.AddHours(22);Check(tracker.Elapsed==30*3600&&tracker.PastChosenEnd&&tracker.Data.active!=null&&tracker.Data.active.planMinutes==720,"Elapsed records actual 30h without auto-end, extension or cap");
            tracker=new FastingTracker(memory,true,()=>now);tracker.Load();Check(tracker.Elapsed==30*3600,"Restart restores wall-clock elapsed");
            memory.Fail=true;long before=tracker.Data.active.startUtc;Check(!tracker.CorrectStart(before-3600)&&tracker.Data.active.startUtc==before,"Failed correction rolls back");Check(!tracker.EndNow()&&tracker.Data.active!=null&&tracker.Data.records.Length==0,"Failed end preserves timer and retry");memory.Fail=false;
            Check(tracker.EndNow()&&tracker.Data.active==null&&tracker.Data.records.Length==1,"End after limit records actual time once");Check(!tracker.EndNow()&&tracker.Data.records.Length==1,"Repeated end cannot duplicate history");
            var record=tracker.Data.records[0];Check(record.endUtc-record.startUtc==30*3600,"Actual duration not rewritten to target");
            Check(!tracker.Correct(record.id,record.endUtc+1,record.endUtc),"Reject reversed interval");Check(!tracker.Correct(record.id,record.startUtc,tracker.Now+1),"Reject future end");
            Check(tracker.Correct(record.id,record.startUtc-3600,record.endUtc),"Historical correction may exceed twenty hours");
            Check(!tracker.Start(record.startUtc,720),"Reject overlapping new session");
            now=now.AddHours(1);Check(tracker.Start(tracker.Now,720),"New nonoverlapping session");
            var activeStart=tracker.Data.active.startUtc;now=now.AddHours(-2);Check(tracker.ClockBeforeStart&&tracker.Elapsed==0&&!tracker.EndNow(),"Clock rollback never creates negative/invalid history");now=now.AddHours(3);
            Check(!tracker.Correct(record.id,record.startUtc,tracker.Now),"Correction cannot overlap active session");Check(tracker.DiscardActive()&&tracker.Data.records.Length==1,"Discard active preserves history");
            Check(tracker.SetReducedMotion(true)&&tracker.Disable()&&tracker.Data.records.Length==1,"Disable preserves history and reduced motion preference");
            memory.Fail=true;Check(!tracker.Delete(record.id)&&tracker.Data.records.Length==1,"Failed delete preserves original history");memory.Fail=false;
            Check(tracker.Delete(record.id)&&tracker.Data.records.Length==0,"Explicit record deletion");
            Check(tracker.Enable(1200,true)&&tracker.Start(tracker.Now,1200)&&tracker.EndNow(),"20h plan allowed, immediate zero-duration end allowed");
            Check(tracker.ClearHistory()&&tracker.Data.enabled,"Deleting history is distinct from disabling");
            foreach(bool eligibility in new[]{false,false}){var denied=new Memory{Data=memory.Data};var teen=new FastingTracker(denied,eligibility,()=>now);teen.Load();Check(!teen.Loaded&&denied.Reads==0&&!teen.Enable(720,true)&&denied.Writes==0,"Teen/unknown route cannot read or write adult data");}
            var corrupt=new Memory{Data="{ broken"};var bad=new FastingTracker(corrupt,true);bad.Load();Check(!bad.Loaded&&corrupt.Writes==0&&corrupt.Data=="{ broken","Malformed JSON preserved");
            corrupt.Data="{\"version\":99,\"context\":\"local-fasting-v1\",\"records\":[]}";bad.Load();Check(!bad.Loaded&&corrupt.Writes==0,"Unsupported version preserved");
            corrupt.Data="{}";bad.Load();Check(!bad.Loaded&&corrupt.Writes==0,"Missing schema is not accepted as a fresh log");
            corrupt.Data="{\"version\":1,\"context\":\"local-fasting-v1\",\"planMinutes\":720,\"enabled\":true,\"records\":[],\"active\":{\"startUtc\":1790856000,\"planMinutes\":720}}";bad.Load();Check(!bad.Loaded&&corrupt.Writes==0,"Damaged active record is preserved, never silently discarded");
            Check(FastingTracker.ParseTime("2026-10-01 08:30 -06:00",out var a)&&FastingTracker.ParseTime("2026-10-01 14:30 +00:00",out var b)&&a==b,"Explicit UTC offsets round-trip to same instant");
            Check(FastingTracker.ParseTime(FastingTracker.EditTime(tracker.Now+37),out var precise)&&precise==tracker.Now+37,"Editing timestamps preserves seconds");
            Check(!FastingTracker.ParseTime("not a date",out _),"Invalid text rejected");
            long[] thresholds={0,4*3600-1,4*3600,16*3600-1,16*3600,24*3600,48*3600};int[] bands={0,0,1,1,2,3,4};
            for(int i=0;i<thresholds.Length;i++)Check(FastingTracker.TimeBand(thresholds[i])==bands[i],"Neutral stage boundary "+thresholds[i]);
            Check(FastingTracker.Duration(101*3600+61)=="101:01:01","Duration does not wrap at 24h or 100h");
            string path=Path.Combine(Application.temporaryCachePath,"fasting-atomic-"+Guid.NewGuid().ToString("N")+".json");var disk=new JournalFileStorage(path);disk.Write(memory.Data);disk.Write(memory.Data);Check(disk.Read()==memory.Data&&!File.Exists(path+".tmp"),"Atomic create and replacement");File.Delete(path);
        }
        void Tap(PixelFastingWindow w,string id,bool pointer=false)
        {
            Check(w.Controls.ContainsKey(id),"Control exists "+id);if(!w.Controls.TryGetValue(id,out var s))return;Check(s.IsInteractable(),"Control enabled "+id);
            if(pointer){Canvas.ForceUpdateCanvases();var r=(RectTransform)s.transform;var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Selectable>()==s,"Pointer reaches "+id);ExecuteEvents.Execute(s.gameObject,e,ExecuteEvents.pointerClickHandler);}
            else ExecuteEvents.Execute(s.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        void Fits(PixelFastingWindow w,string state)
        {
            Canvas.ForceUpdateCanvases();foreach(var t in w.Composition.GetComponentsInChildren<Text>())if(t.gameObject.activeInHierarchy&&t.GetComponentInParent<InputField>()==null&&!string.IsNullOrEmpty(t.text))Check(t.preferredHeight<=t.rectTransform.rect.height+2,state+" text fits: "+t.text.Replace("\n"," / "));
        }
        IEnumerator Shot(PixelFastingWindow w,string name)
        {yield return null;yield return new WaitForEndOfFrame();var capture=PixelWorkoutWindow.Arg("-sologym-capture");if(capture!="")w.Capture(Path.Combine(Path.GetDirectoryName(capture),Path.GetFileNameWithoutExtension(capture)+"-"+name+".png"));}
        public IEnumerator Run(PixelFastingWindow w)
        {
            try{Models();}catch(Exception e){Check(false,"Model exception "+e);}
            if(!w.Tracker.Eligible){Check(w.ClockArt==null&&w.Controls.Count==1&&w.Controls.ContainsKey("exit"),"Ineligible route contains no fasting controls or art");Fits(w,"ineligible");yield return Shot(w,"ineligible");Finish();yield break;}
            Fits(w,"setup");Check(!w.Tracker.Data.enabled&&!w.Controls["enable"].IsInteractable(),"Setup requires explicit acknowledgment");yield return Shot(w,"setup");Tap(w,"ack");yield return null;yield return new WaitForEndOfFrame();Tap(w,"enable",true);Fits(w,"empty-clock");
            Tap(w,"start");Fits(w,"start-editor");yield return Shot(w,"start-editor");((InputField)w.Controls["start-time"]).text="invalid";Tap(w,"save-edit");Check(w.Tracker.Data.active==null&&((InputField)w.Controls["start-time"]).text=="invalid","Bad date preserves editor input");
            ((InputField)w.Controls["start-time"]).text=FastingTracker.EditTime(w.Tracker.Now-8*3600-24*60);Tap(w,"save-edit");Check(w.Tracker.Data.active!=null&&w.ClockArt.Running,"Start enables real clock and decorative animation");Fits(w,"active");yield return Shot(w,"active");
            int frame=w.ClockArt.FrameIndex;yield return new WaitForSecondsRealtime(.7f);Check(w.ClockArt.FrameIndex!=frame||w.ClockArt.ReducedMotion,"Motion plays using actual frame metadata");
            Tap(w,"guide");Fits(w,"guide");yield return Shot(w,"guide");Tap(w,"guide-back");Tap(w,"edit-start");Fits(w,"correct-start");((InputField)w.Controls["start-time"]).text=FastingTracker.EditTime(w.Tracker.Now-18*3600);Tap(w,"save-edit");Check(w.Tracker.Band==2,"Active correction changes time band");Fits(w,"stage-16-24");
            Tap(w,"settings");Fits(w,"active-settings");Tap(w,"motion");Check(w.ClockArt.ReducedMotion,"Reduced motion saved immediately");Tap(w,"settings-back");Check(w.Tracker.Data.reducedMotion,"Reduced motion retained");yield return null;yield return new WaitForEndOfFrame();Tap(w,"end",true);Check(w.Tracker.Data.active==null&&w.Tracker.Data.records.Length==1,"End stores actual history");Fits(w,"history");yield return Shot(w,"history");
            string id=w.Tracker.Data.records[0].id;Tap(w,"edit-"+id);Fits(w,"correct-history");yield return Shot(w,"correct-history");long start=w.Tracker.Data.records[0].startUtc;((InputField)w.Controls["start-time"]).text=FastingTracker.EditTime(start-3600);Tap(w,"save-edit");Check(w.Tracker.Data.records[0].startUtc==start-3600,"History correction saves actual start");
            Tap(w,"delete-"+id);Fits(w,"delete-confirm");Tap(w,"cancel-delete");Check(w.Tracker.Data.records.Length==1,"Cancel delete preserves history");
            Tap(w,"settings");Fits(w,"idle-settings");Tap(w,"disable");Check(!w.Tracker.Data.enabled&&w.Tracker.Data.records.Length==1,"Disable preserves records and hides route");Check(!w.Controls.ContainsKey("nav-fasting"),"Disabled fasting absent from navigation");
            Tap(w,"history");Tap(w,"delete-"+id);Tap(w,"confirm-delete");Check(w.Tracker.Data.records.Length==0,"History deletion works while disabled");Tap(w,"history-back");Tap(w,"ack"); // A fresh acknowledgement is required after disable.
            if(!w.Controls["enable"].IsInteractable())Tap(w,"ack");Tap(w,"enable");Tap(w,"start");Tap(w,"save-edit");Tap(w,"settings");Tap(w,"discard");Tap(w,"confirm-delete");Check(w.Tracker.Data.active==null&&w.Tracker.Data.records.Length==0,"Discard active never adds history");
            // Separate injected save failure exercises actual UI retry without touching personal files.
            var fail=new Memory();var time=DateTimeOffset.UtcNow;var other=new GameObject("Fasting save failure fixture").AddComponent<PixelFastingWindow>();other.Initialize(w.Language,true,_=>{},fail,()=>time);Tap(other,"ack");Tap(other,"enable");Tap(other,"start");string input=((InputField)other.Controls["start-time"]).text;fail.Fail=true;Tap(other,"save-edit");Check(other.Tracker.Data.active==null&&((InputField)other.Controls["start-time"]).text==input,"Failed UI write preserves input");Fits(other,"save-error");yield return Shot(other,"save-error");fail.Fail=false;Tap(other,"save-edit");Check(other.Tracker.Data.active!=null,"UI save retry succeeds once");other.gameObject.SetActive(false);Destroy(other.gameObject);
            if(w.Language=="es")
            {
                w.gameObject.SetActive(false);
                var homeEntry=new GameObject("Fasting Home integration");homeEntry.AddComponent<HomeScreen>();
                var home=homeEntry.GetComponent<PixelTrainingHallHome>();home.AutomaticReview=false;
                yield return null;
                yield return gameObject.AddComponent<PixelTrainingHallSmoke>().Run(home);
                Check(!home.FastingVisible,"Home keeps optional route off before opt-in");
                home.Controller.SetTeenProfile(true,false);home.ShowSettings();
                Check(!home.Modal.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("ayuno")||x.text.Contains("fasting"))&&home.OpenFasting()==null,"Teen Home has no fasting setting or callable route");home.CloseModal();home.Controller.SetTeenProfile(false);
                var opened=home.OpenFasting();yield return null;
                Check(opened!=null&&!home.Composition.gameObject.activeSelf&&opened.View=="setup","Adult Home opens suitability setup");
                Tap(opened,"ack");Tap(opened,"enable");Tap(opened,"start");Tap(opened,"save-edit");string activeId=opened.Tracker.Active.id;Tap(opened,"nav-home");yield return null;
                Check(home.Composition.gameObject.activeSelf&&home.FastingVisible,"Return refreshes newly enabled navigation");
                opened=home.OpenFasting();yield return null;Check(opened.Tracker.Active.id==activeId,"Home reopening restores the same persisted timer");
                Tap(opened,"end");Tap(opened,"settings");Tap(opened,"disable");Tap(opened,"nav-home");yield return null;
                Check(!home.FastingVisible,"Disable removes Home fasting tab");home.gameObject.SetActive(false);Destroy(home.gameObject);w.gameObject.SetActive(true);
            }
            // Stable final screenshot represents a separate isolated example.
            w.Tracker.SetReducedMotion(false);w.Tracker.Start(w.Tracker.Now-8*3600-24*60-16,720);w.Show("timer");Fits(w,"final");yield return null;Finish();
        }
        void Finish()
        {
            var path=PixelWorkoutWindow.Arg("-sologym-capture");if(path!=""){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(Path.ChangeExtension(path,"json"),JsonUtility.ToJson(new Result{scenario=PixelWorkoutWindow.Arg("-sologym-locale","es")+(PixelWorkoutWindow.Has("-sologym-teen")?"-teen":"-adult"),checks=checks,failures=failures.Count,problems=failures.ToArray()},true));}
            Debug.Log("FASTING_SMOKE_RESULT checks="+checks+" failures="+failures.Count);
        }
    }
}
