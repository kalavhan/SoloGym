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
    public sealed class PixelBossSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public string[] checks, failures; }
        sealed class Memory : IJournalStorage
        {
            public string Data; public bool Fail; public int Writes;
            public string Read()=>Data;
            public void Write(string value){if(Fail)throw new IOException("Simulated full disk");Data=value;Writes++;}
        }
        readonly List<string> checks=new List<string>(),failures=new List<string>();
        void Check(bool value,string name){checks.Add(name);if(!value){failures.Add(name);Debug.LogError("BOSS CHECK FAILED: "+name);}}
        TrainingCatalog training;JournalOptions options;BossCatalog bosses;
        WorkoutJournal Journal(Memory store,string profile="adult_gym_intermediate",int minutes=25,string ready="ready")
        {
            var j=new WorkoutJournal(training,options,store,profile,new DateTime(2026,10,1));j.Load();
            j.BeginEdit();j.SetDuration(minutes);if(!j.SaveEdit(out var error))throw new Exception(error);
            j.BeginPrepare();j.Gate.SetReadiness(ready);if(j.IsTeen)j.Gate.SetSupervision(true);j.ReviewReadiness();j.Gate.Acknowledge(true);return j;
        }
        BossController Begin(WorkoutJournal j,Func<double> clock)
        {j.SaveBoss(BossController.Create(j,bosses));return new BossController(j.ActiveSession,bosses,j.SaveBoss,false,clock);}
        void Models()
        {
            training=JsonUtility.FromJson<TrainingCatalog>(Resources.Load<TextAsset>("Training/Preview").text);
            options=JsonUtility.FromJson<JournalOptions>(Resources.Load<TextAsset>("Training/JournalOptions").text);
            bosses=JsonUtility.FromJson<BossCatalog>(Resources.Load<TextAsset>("Training/BossOptions").text);
            TrainingSmoke.Verify(training);Check(true,"Existing readiness fixtures remain valid");
            double time=10000;var memory=new Memory();var j=Journal(memory);var c=Begin(j,()=>time);
            Check(c.Current.role=="warmup"&&c.Damage==0,"Explicit start preserves warm-up and awards no damage");
            string id=c.NextId;string before=memory.Data;memory.Fail=true;
            Check(!c.Log(id,3,null)&&c.Data.logs.Length==0&&memory.Data==before,"Failed save preserves active set and file");memory.Fail=false;
            Check(c.Log(id,3,null)&&c.Damage==0&&c.Current.role=="main","Warm-up recorded without combat progress");
            Check(c.Log(id,999,900)&&c.Data.logs.Length==1,"Retried old operation cannot record next set");
            id=c.NextId;Check(!c.Log(id,double.NaN,null)&&!c.Log(id,5,double.PositiveInfinity),"Invalid numeric inputs rejected");
            Check(c.Log(id,3,12.5)&&c.Damage==50&&c.RestLeft==120,"Manual partial set matches reference fraction; load has no effect");
            string originalLog=JsonUtility.ToJson(c.Data.logs.Last());int damage=c.Damage;
            Check(c.SetDifficulty("light")&&c.Damage==damage&&JsonUtility.ToJson(c.Data.logs.Last())==originalLog,"Difficulty during rest preserves record and never awards damage");
            Check(c.RestLeft==120,"Difficulty preserves the active rest timer");
            Check(c.Pause(),"Pause saves");double rest=c.RestLeft;time+=500;
            Check(c.RestLeft==rest&&!c.CanLog,"Pause freezes remaining rest");
            Check(c.SetDifficulty("medium")&&c.Damage==damage&&c.Data.plan.blocks.First(b=>b.id=="squat").sets==2,"Difficulty during pause preserves recorded work");
            Check(c.Resume()&&c.RestLeft==rest,"Resume restores remaining rest");time+=rest;
            Check(c.CanLog,"Clock expiry enables set without auto-recording");
            Check(c.Correct(id,100,999)&&c.Damage==100&&c.Data.logs.Length==2,"Correction replaces original slot and respects its fixed cap");
            Check(c.Correct(id,0,null)&&c.Damage==0&&c.Data.logs.Last().prescription.quantity_min==6,"Correction can reduce progress, keeps original dose");
            var reloaded=new WorkoutJournal(training,options,memory,j.ProfileId,j.Today);reloaded.Load();
            var restored=new BossController(reloaded.ActiveSession,bosses,reloaded.SaveBoss,true,()=>time);
            Check(restored.NeedsReadiness&&restored.Data.paused&&!restored.CanLog,"Restart requires fresh readiness and stays paused");
            Check(restored.Recheck("low_energy",true)&&restored.Data.difficulty=="light"&&restored.Data.paused,"Low-energy recovery adapts only remaining work");
            Check(restored.Resume(),"Resume after readiness is explicit");
            while(!restored.Closed){time+=600;var b=restored.Current;if(!restored.Log(restored.NextId,b.quantity_min,null))throw new Exception(restored.Error);}
            Check(restored.Data.state=="completed"&&restored.Data.logs.Last().prescription.role=="cooldown"&&restored.Damage<1000,"Completion includes recovery; no extra work forced to fill missing HP");
            Check(!restored.Log("unknown",100,null),"Closed session cannot add records");
            memory.Fail=true;bool failed=false;try{reloaded.ArchiveBoss();}catch(IOException){failed=true;}Check(failed&&reloaded.ActiveSession!=null&&reloaded.Selected.status=="planned","Failed journal return keeps saved summary for retry");memory.Fail=false;
            reloaded.ArchiveBoss();reloaded.ArchiveBoss();Check(reloaded.Selected.status=="completed"&&reloaded.Selected.session.logs.Length==restored.Data.logs.Length&&reloaded.ActiveSession==null,"Return archives actual logs atomically and idempotently");
            reloaded.Load();Check(reloaded.Loaded&&reloaded.Selected.completedPlan!=null,"Completed history survives restart");
            foreach(string ready in new[]{"pain","injury","ill"})
            {var m=new Memory();var a=Journal(m);var b=Begin(a,()=>time);b=new BossController(a.ActiveSession,bosses,a.SaveBoss,true,()=>time);Check(b.Recheck(ready,true)&&b.Data.state=="stopped"&&!b.CanLog,"Recovery stops saved prescription: "+ready);a.ArchiveBoss();Check(a.Selected.status=="stopped"&&!a.CanPrepare&&!a.CanEdit(a.Selected),"Stopped history is not a missed-workout debt: "+ready);}
            var retirement=Begin(Journal(new Memory(),minutes:40),()=>time);
            retirement.Log(retirement.NextId,3,null);retirement.Log(retirement.NextId,6,null);time+=600;retirement.Log(retirement.NextId,6,null);
            int retiredDamage=retirement.Damage;string retiredLogs=JsonUtility.ToJson(retirement.Data.logs[2]);
            Check(retirement.SetDifficulty("light")&&retirement.Current.id!="squat"&&retirement.RestLeft==120&&retirement.Damage==retiredDamage,"Reducing 3 to 2 sets retires unlogged work without damage or cancelled rest");
            retirement.Pause();Check(retirement.SetDifficulty("medium")&&retirement.Data.plan.blocks.First(b=>b.id=="squat").sets==2&&JsonUtility.ToJson(retirement.Data.logs[2])==retiredLogs,"Raising difficulty never reopens a retired block or modifies logs");
            var teen=Journal(new Memory(),"teen_home_supervised");var t=Begin(teen,()=>time);Check(t.SetDifficulty("hard")&&t.Data.difficulty=="medium","Teen hard selection retains existing cap");
            t=new BossController(teen.ActiveSession,bosses,teen.SaveBoss,true,()=>time);Check(t.Recheck("ready",false)&&t.Closed,"Teen resume without supervision stops strength session");
            var novice=Begin(Journal(new Memory(),"adult_home_beginner"),()=>time);Check(novice.SetDifficulty("hard")&&novice.Data.difficulty=="medium","Beginner hard choice retains experience cap");
            Check(novice.Data.plan.messages.Any(m=>m.code=="pull_coverage_gap"),"Home pulling gap remains disclosed");
            // Every authored context starts safely; changes stay within original budget and preserve current damage.
            foreach(var variant in bosses.entries.Where(v=>v.key.EndsWith(":medium")))
            {
                var parts=variant.key.Split(':');var a=Journal(new Memory(),parts[0],int.Parse(parts[1]),parts[2]);var b=Begin(a,()=>time);
                foreach(string level in new[]{"light","hard","medium"}){int old=b.Damage;b.SetDifficulty(level);Check(b.Data.plan.estimated_seconds<=b.Data.plan.budget_seconds&&b.Damage==old,"Difficulty budget/gates: "+variant.key+"/"+level);}
                b.Log(b.NextId,b.Current.quantity_min,null);b.Stop("user");a.ArchiveBoss();Check(a.Selected.session.logs.Length==1&&a.Selected.status=="stopped","Early stop keeps warm-up: "+variant.key);
            }
            // Edited journal exercise identities survive difficulty changes, never silently replace user selections.
            var swapped=Journal(new Memory(),minutes:40);swapped.ClearGate();swapped.BeginEdit();swapped.SetSwap("squat","leg_press");swapped.SaveEdit(out _);swapped.BeginPrepare();swapped.Gate.SetReadiness("ready");swapped.ReviewReadiness();swapped.Gate.Acknowledge(true);
            var sc=Begin(swapped,()=>time);Check(sc.SetDifficulty("light")&&sc.Data.plan.blocks.Any(b=>b.exercise_id=="leg_press"),"Difficulty retains eligible selected exercise");
            var malformed=new Memory{Data="{bad json"};var bad=new WorkoutJournal(training,options,malformed,j.ProfileId,j.Today);bad.Load();Check(!bad.Loaded&&malformed.Writes==0,"Corrupt save preserved with error");
            var invalid=WorkoutJournal.Clone(c.Data);invalid.logs[0].quantity=double.NaN;bool rejected=false;try{BossSession.Validate(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"Corrupt nonfinite log rejected");
            invalid=WorkoutJournal.Clone(c.Data);invalid.logs[0].index=4;rejected=false;try{BossSession.Validate(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"Corrupt set order rejected");
        }
        void Tap(IDictionary<string,Selectable> controls,string id,bool pointer=false)
        {
            Check(controls.ContainsKey(id),"Control exists: "+id);if(!controls.TryGetValue(id,out var s))return;Check(s.IsInteractable(),"Control enabled: "+id);
            if(pointer){Canvas.ForceUpdateCanvases();var r=(RectTransform)s.transform;var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Selectable>()==s,"Pointer reaches "+id);ExecuteEvents.Execute(s.gameObject,e,ExecuteEvents.pointerClickHandler);}
            else ExecuteEvents.Execute(s.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        void Fits(PixelBossWindow w,string state)
        {
            Canvas.ForceUpdateCanvases();foreach(var t in w.Composition.GetComponentsInChildren<Text>())if(t.gameObject.activeInHierarchy&&t.GetComponentInParent<InputField>()==null&&!string.IsNullOrEmpty(t.text))Check(t.preferredHeight<=t.rectTransform.rect.height+2,state+" text fits: "+t.text.Replace("\n"," / "));
        }
        IEnumerator Shot(PixelBossWindow w,string name)
        {yield return null;yield return new WaitForEndOfFrame();string p=PixelWorkoutWindow.Arg("-sologym-capture");if(p!="")w.Capture(Path.Combine(Path.GetDirectoryName(p),Path.GetFileNameWithoutExtension(p)+"-"+name+".png"));}
        public IEnumerator Run(PixelWorkoutWindow w)
        {
            try{Models();}catch(Exception e){Check(false,"Model exception: "+e);}
            if(w.View!="readiness")w.Prepare();Tap(w.Controls,"ready-ready");if(w.Journal.IsTeen)Tap(w.Controls,"supervised");Tap(w.Controls,"review-readiness");Check(!w.Controls["start-dungeon"].IsInteractable(),"Start blocked until acknowledgment");Tap(w.Controls,"acknowledge");
            yield return null;yield return new WaitForEndOfFrame();Tap(w.Controls,"start-dungeon",true);yield return null;
            var b=w.BossWindow;if(b==null){Check(false,"Dungeon opened");Finish();yield break;}
            Fits(b,"warm-up");yield return Shot(b,"warmup");
            ((InputField)b.Controls["quantity"]).text=b.Controller.Current.quantity_min.ToString();Tap(b.Controls,"log");Fits(b,"exercise");yield return Shot(b,"exercise");
            ((InputField)b.Controls["quantity"]).text="7";Tap(b.Controls,"difficulty-light");Check(((InputField)b.Controls["quantity"]).text=="7","Difficulty preserves unsubmitted quantity on the same active set");Tap(b.Controls,"difficulty-medium");
            ((InputField)b.Controls["quantity"]).text="8";if(b.Controls.TryGetValue("load",out var load))((InputField)load).text="12.5";Tap(b.Controls,"log");
            Check(b.Controller.Data.logs.Length==2,"UI manually records set");Fits(b,"rest");yield return Shot(b,"rest");
            Tap(b.Controls,"difficulty-light");Tap(b.Controls,"pause");Fits(b,"paused");yield return Shot(b,"paused");Tap(b.Controls,"difficulty-medium");Check(b.Controller.Data.paused,"Difficulty available while paused");
            Tap(b.Controls,"overview");Fits(b,"overview");yield return Shot(b,"overview");
            string correct=b.Controls.Keys.First(k=>k.StartsWith("correct-"));Tap(b.Controls,correct);((InputField)b.Controls["quantity"]).text="1";Tap(b.Controls,"save-correction");Check(b.Controller.Data.logs[0].quantity==1,"UI corrects existing record");Tap(b.Controls,"overview");
            Tap(b.Controls,"save-exit");yield return null;Check(w.Composition.gameObject.activeSelf&&w.Journal.ActiveSession!=null,"Save and return restores journal, preserves active session");
            w.Prepare();yield return null;b=w.BossWindow;Check(b.Controller.NeedsReadiness,"Journal resumes through fresh readiness");
            string savedId=b.Controller.Data.id;Tap(b.Controls,"pause");Check(w.Journal.ActiveSession.id==savedId&&w.Journal.ActiveSession.state=="active","Back from resume readiness preserves saved workout without answering or stopping");w.Prepare();yield return null;b=w.BossWindow;
            Fits(b,"resume-readiness");yield return Shot(b,"resume-readiness");if(w.Journal.IsTeen)Tap(b.Controls,"resume-supervision");Tap(b.Controls,"resume-ready");Tap(b.Controls,"resume");
            Tap(b.Controls,"stop");Fits(b,"stop-confirmation");yield return Shot(b,"stop");Tap(b.Controls,"confirm-stop");Fits(b,"stopped-summary");yield return Shot(b,"summary");
            Tap(b.Controls,"archive");yield return null;Check(w.Journal.ActiveSession==null&&w.Journal.Selected.status=="stopped","UI archives stopped session with records");
            Tap(w.Controls,"full");Check(w.Composition.GetComponentsInChildren<Text>().Any(t=>t.text=="SAVED RECORDS"||t.text=="REGISTROS GUARDADOS"),"Journal history exposes saved quantities after leaving dungeon");
            yield return null;yield return new WaitForEndOfFrame();string historyPath=PixelWorkoutWindow.Arg("-sologym-capture");
            if(historyPath!="")w.Capture(Path.Combine(Path.GetDirectoryName(historyPath),Path.GetFileNameWithoutExtension(historyPath)+"-journal-records.png"));
            // A controllable clock exercises actual completion controls without waiting out every rest in real time.
            double clock=20000;var diskFailure=new Memory();var completionJournal=Journal(diskFailure);
            completionJournal.SaveBoss(BossController.Create(completionJournal,bosses));
            var completeWindow=new GameObject("Completion and save-failure review").AddComponent<PixelBossWindow>();bool returned=false;
            completeWindow.Initialize(completionJournal,w.Language,"male-medium",()=>returned=true,false,()=>clock);
            ((InputField)completeWindow.Controls["quantity"]).text="3";diskFailure.Fail=true;Tap(completeWindow.Controls,"log");
            Check(completeWindow.Controller.Data.logs.Length==0&&((InputField)completeWindow.Controls["quantity"]).text=="3","UI save failure retains input and active set for retry");Fits(completeWindow,"save-error");yield return Shot(completeWindow,"save-error");diskFailure.Fail=false;Tap(completeWindow.Controls,"log");
            while(!completeWindow.Controller.Closed)
            {
                clock+=600;completeWindow.Render();var block=completeWindow.Controller.Current;
                ((InputField)completeWindow.Controls["quantity"]).text=block.quantity_min.ToString();Tap(completeWindow.Controls,"log");
            }
            Check(completeWindow.Controller.Damage==1000&&completeWindow.Controller.Data.logs.Last().prescription.role=="cooldown","UI full routine reaches bounded progress and includes recovery");Fits(completeWindow,"completed-summary");yield return Shot(completeWindow,"completed");
            diskFailure.Fail=true;Tap(completeWindow.Controls,"archive");Check(!returned&&completionJournal.ActiveSession!=null,"UI failed archive leaves summary available");diskFailure.Fail=false;Tap(completeWindow.Controls,"archive");Check(returned&&completionJournal.Selected.status=="completed","UI successful retry archives completion once");completeWindow.gameObject.SetActive(false);Destroy(completeWindow.gameObject);
            // New separate example for a stable composition capture, never replaces completed history.
            w.Journal.BeginCreate(w.Journal.Today);w.Journal.SaveEdit(out _);w.Prepare();Tap(w.Controls,"ready-ready");if(w.Journal.IsTeen)Tap(w.Controls,"supervised");Tap(w.Controls,"review-readiness");Tap(w.Controls,"acknowledge");Tap(w.Controls,"start-dungeon");yield return null;b=w.BossWindow;
            ((InputField)b.Controls["quantity"]).text=b.Controller.Current.quantity_min.ToString();Tap(b.Controls,"log");((InputField)b.Controls["quantity"]).text="8";
            yield return null;yield return new WaitForEndOfFrame();Fits(b,"final");string capture=PixelWorkoutWindow.Arg("-sologym-capture");if(capture!="")b.Capture(capture);Finish();
            if(!PixelWorkoutWindow.Has("-sologym-stay-open"))Application.Quit(failures.Count==0?0:1);
        }
        void Finish()
        {
            string p=PixelWorkoutWindow.Arg("-sologym-capture");if(p!="")File.WriteAllText(Path.ChangeExtension(p,"json"),JsonUtility.ToJson(new Report{checks=checks.ToArray(),failures=failures.ToArray()},true));
            Debug.Log("BOSS_SMOKE "+(failures.Count==0?"PASS":"FAIL")+" checks="+checks.Count+" failures="+failures.Count);
        }
    }
}
