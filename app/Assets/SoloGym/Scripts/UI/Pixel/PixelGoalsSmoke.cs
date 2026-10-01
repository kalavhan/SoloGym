using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Fictional connected-flow verification; never registers, persists a profile or generates training.</summary>
    public sealed class PixelGoalsSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public int checks,width,height; public string locale; public string[] failures; }
        readonly List<string> failures=new List<string>(); int checks;
        public bool Passed=>failures.Count==0;
        void Check(bool okay,string label) { checks++; if(!okay) { failures.Add(label); Debug.LogError("GOALS_CHECK_FAILED "+label); } }
        internal static void Enter(PixelLoginWindow w,string age,bool signedIn,string readiness="ready")
        {
            w.OpenOnboarding(true,signedIn); w.Onboarding.Controller.SetAge(age); w.Onboarding.Controller.SelectCountry("MX");
            w.Onboarding.Advance();
            if(w.Onboarding.Controller.Step!=GuildSetupStep.Character) return;
            w.Onboarding.Controller.ChooseGender("female"); w.Onboarding.Controller.ChooseBody("fat"); w.Onboarding.Advance();
            w.Consent.Controller.ChoosePrivacy(true); w.Consent.Controller.ChooseTerms(true); w.Consent.Advance();
            if(!signedIn) w.OpenProfileReview();
            if(w.Page!="profile") return;
            w.Profile.Advance(); w.Profile.Height.Input.text=""; w.Profile.Weight.Input.text=""; w.Profile.Advance();
            w.Profile.Controller.SelectReadiness(readiness); w.Profile.Advance();
            if(w.Profile.Controller.Model.Step==ProfileStep.Checkpoint) w.Profile.Advance();
        }
        public IEnumerator Run(PixelLoginWindow w)
        {
            var f=w.Goals; string language=w.Controller.Model.Language;
            Check(w.Page=="login","Startup stays on login"); w.OpenGoalsReview(); Check(w.Page=="login","Direct goals call cannot bypass setup");
            Check(GoalsExperienceStateChecks.Run().Contains("checks passed"),"Existing goal/experience domain and production gates pass"); w.Controller.SetLanguage(language);
            foreach(string age in new[]{"","14","15.0","121"})
            { Enter(w,age,true); w.OpenGoalsReview(); Check(w.Page=="onboarding" && !f.gameObject.activeInHierarchy,"Missing/invalid/underage draft cannot become adult: "+age); }
            foreach(string readiness in new[]{"ill","pain","injury","unsure"})
            {
                Enter(w,"21",true,readiness); w.OpenGoalsReview();
                Check(w.Page=="profile" && !f.gameObject.activeInHierarchy,"Readiness pause prevents goals: "+readiness);
            }
            Enter(w,"21",false); yield return Ready();
            Check(w.Page=="goals" && !w.Account.Controller.Created,"Provisional registration reaches review without creating an account");
            Check(f.Controller.Model.GoalId=="" && f.Controller.Model.ExperienceId=="" && !f.Continue.IsInteractable(),"Both initial choices empty; Continue disabled");
            Check(f.Goals.Length==5 && !f.Controller.Model.IsTeenAudience,"Validated adult receives five catalog goals");
            TextFits(f,"empty"); yield return Shot(w,"empty");
            f.Goals[0].Select(); Check(f.Controller.Model.GoalId=="" && f.Goals[0].ShowsFocus,"Focus does not select a goal");
            f.Advance(); yield return Ready(); Check(f.Controller.Model.ErrorKey=="goal_required","Empty goal cannot advance by direct action"); TextFits(f,"required");
            foreach(var row in f.Goals)
            {
                f.Scroll.verticalNormalizedPosition=1; yield return Ready(); Check(Click(row),"Pointer hits full goal row: "+row.Id); yield return Ready();
                Check(f.Controller.Model.GoalId==row.Id && Selected(f.Goals)==1,"Exclusive catalog selection: "+row.Id);
            }
            Click(f.Goals[0]); yield return Ready(); EventSystem.current.SetSelectedGameObject(null); yield return Shot(w,"goal");
            Check(Click(f.Continue),"Goal Continue accepts pointer"); yield return Ready();
            Check(f.Controller.Model.Step==GoalsExperienceStep.Experience && !f.Continue.IsInteractable(),"Experience starts empty");
            TextFits(f,"experience-empty");
            f.Advance(); Check(f.Controller.Model.ErrorKey=="experience_required","Empty experience is required");
            Click(f.Experiences[1]); yield return Ready(); Check(f.Controller.Model.ExperienceId=="intermediate","Regular training is explicit");
            Check(Click(f.Unsure),"Uncertainty link clickable"); yield return Ready();
            Check(f.Controller.Model.UncertaintyDialogOpen && f.Controller.Model.ExperienceId=="intermediate" && !f.Experiences[1].gameObject.activeInHierarchy,"Uncertainty hides underlying choices without selecting beginner");
            TextFits(f,"uncertainty"); Visible(f,f.Cancel,"Confirmation cancel visible on first open"); Visible(f,f.Continue,"Confirmation accept visible on first open"); yield return Shot(w,"uncertainty");
            Click(w.Privacy); yield return Ready(); Click(w.Terms); yield return Ready(); Click(w.Consent.ReaderReturn); yield return Ready();
            Check(w.Page=="goals" && f.Controller.Model.UncertaintyDialogOpen && f.Controller.Model.ExperienceId=="intermediate","Document-to-document return preserves confirmation and draft");
            Click(w.Language); yield return Ready(); Check(f.Controller.Model.UncertaintyDialogOpen,"Locale keeps confirmation open"); w.Controller.SetLanguage(language);
            Check(Click(f.Cancel),"Cancel confirmation is reachable"); yield return Ready();
            Check(!f.Controller.Model.UncertaintyDialogOpen && f.Controller.Model.ExperienceId=="intermediate","Cancel preserves previous choice");
            Click(f.Unsure); yield return Ready(); f.GoBack(); yield return Ready(); Check(f.Controller.Model.Step==GoalsExperienceStep.Experience && f.Controller.Model.ExperienceId=="intermediate","Back cancels uncertainty before leaving experience");
            Click(f.Unsure); yield return Ready(); Check(Click(f.Continue),"Beginner confirmation is an explicit action"); yield return Ready();
            Check(f.Controller.Model.ExperienceId=="beginner" && f.Controller.Model.Step==GoalsExperienceStep.Review,"Confirmed beginner reaches summary");
            TextFits(f,"summary"); EventSystem.current.SetSelectedGameObject(null); yield return Shot(w,"summary");
            Check(Click(f.ChangeGoal),"Summary focus edit works"); yield return Ready();
            Check(f.Controller.Model.Step==GoalsExperienceStep.Goal && f.Controller.Model.ExperienceId=="beginner","Changing focus keeps experience");
            Click(f.Goals[2]); f.Advance(); f.Advance(); yield return Ready();
            Check(f.Controller.Model.GoalId=="muscle_growth" && f.Controller.Model.Step==GoalsExperienceStep.Review,"Edited focus returns through existing experience");
            Check(Click(f.ChangeExperience),"Summary experience edit works"); yield return Ready(); Click(f.Experiences[1]); f.Advance(); yield return Ready();
            TextFits(f,"long-summary"); yield return Shot(w,"intermediate-summary");
            Check(f.Controller.Model.GoalId=="muscle_growth" && f.Controller.Model.ExperienceId=="intermediate","Both revised choices shown");
            Check(Click(f.Continue),"Summary Continue reaches equipment checkpoint"); yield return Ready();
            Check(f.EquipmentPending && w.Page=="goals" && FindFirstObjectByType<PixelTrainingHallHome>()==null,"Checkpoint stays in review; no Home or training");
            TextFits(f,"equipment"); Visible(f,f.Cancel,"Checkpoint exit visible"); Visible(f,f.Continue,"Checkpoint back visible"); yield return Shot(w,"equipment-pending");
            Click(w.Privacy); yield return Ready(); Click(w.Consent.ReaderReturn); yield return Ready(); Check(f.EquipmentPending,"Reader preserves equipment checkpoint");
            f.GoBack(); f.GoBack(); f.GoBack(); f.GoBack(); yield return Ready();
            Check(w.Page=="profile" && w.Profile.Controller.Model.Step==ProfileStep.Readiness,"Back returns to readiness without resetting selections");
            w.Profile.Controller.SelectReadiness("low_energy"); w.Profile.Advance(); yield return Ready(); Check(Click(w.Profile.Continue),"Profile checkpoint connects to goals by real pointer"); yield return Ready();
            Check(w.Page=="goals" && f.Controller.Model.GoalId=="muscle_growth" && f.Controller.Model.ExperienceId=="intermediate","Low-energy entry preserves existing goal draft");
            Check(w.Onboarding.Controller.CharacterId=="female-fat" && w.Profile.Controller.Model.HeightCm==null,"Goal choices do not change appearance or optional measurements");
            f.GoBack(); w.Profile.GoBack(); w.Profile.GoBack(); w.Profile.GoBack(); w.Account.Back.onClick.Invoke();
            w.Consent.Controller.ChoosePrivacy(false); Check(f.Controller.Model.GoalId=="" && w.Profile.Controller.Model.Step==ProfileStep.Notice,"Revoking consent clears dependent drafts");
            Enter(w,"17",true); yield return Ready();
            Check(f.Controller.Model.IsTeenAudience && f.Goals.Length==2 && f.Goals[0].Id=="general_fitness" && f.Goals[1].Id=="mobility","Teen has only two allowed goals");
            f.Controller.SelectGoal("strength"); Check(f.Controller.Model.GoalId=="" && !f.Continue.IsInteractable(),"Hidden adult goal cannot be selected programmatically for teen");
            Click(f.Goals[1]); yield return Ready(); TextFits(f,"teen"); yield return Shot(w,"teen");
            f.Advance(); yield return Ready(); Check(Click(f.Experiences[0]),"Teen beginner row reachable"); yield return Ready(); f.Advance(); yield return Ready();
            Click(w.Language); yield return Ready(); Check(f.Controller.Model.GoalId=="mobility" && f.Controller.Model.ExperienceId=="beginner","Language preserves teen selection IDs"); w.Controller.SetLanguage(language);
            f.GoBack(); f.GoBack(); f.GoBack(); w.Profile.GoBack(); w.Profile.GoBack(); w.Profile.GoBack(); w.Consent.Back.onClick.Invoke(); w.Onboarding.GoBack(); w.Onboarding.Controller.SetAge("22");
            Check(f.Controller.Model.GoalId=="" && f.Controller.Model.ExperienceId=="" && !w.Consent.Controller.CanContinue,"Age changes discard goal and consent drafts");
            Enter(w,"21",true); yield return Ready(); Check(Click(f.Goals[0]),"Exit setup goal selected"); f.Advance(); yield return Ready(); Check(Click(f.Experiences[0]),"Exit setup experience selected"); f.Advance(); yield return Ready(); f.Advance(); yield return Ready();
            Check(Click(f.Cancel),"Equipment checkpoint provides explicit exit"); yield return Ready();
            Check(w.Page=="login" && f.Controller.Model.GoalId=="" && f.Controller.Model.ExperienceId=="" && !f.EquipmentPending && w.Onboarding.Controller.Origin.AgeText=="","Exit clears all setup drafts");
            Enter(w,"21",true); yield return Ready(); Check(Click(f.Goals[0]),"Final goal selected"); f.Advance(); yield return Ready(); Check(Click(f.Experiences[0]),"Final experience selected"); yield return Ready();
            TextFits(f,"experience-selected"); EventSystem.current.SetSelectedGameObject(null); yield return Shot(w,"experience");
            f.GoBack(); yield return Ready(); EventSystem.current.SetSelectedGameObject(null); TextFits(f,"final");
            Check(f.Scroll.content.rect.height<=f.Scroll.viewport.rect.height+1,"Normal goal choices fit without scrolling");
            var corners=new Vector3[4]; w.Panel.GetWorldCorners(corners); Check(corners[0].x>=Screen.safeArea.xMin-1 && corners[2].x<=Screen.safeArea.xMax+1 && corners[0].y>=Screen.safeArea.yMin-1,"Window stays within landscape safe area");
            Save(w,Path.ChangeExtension(PixelWorkoutWindow.Arg("-sologym-capture"),".smoke.json"));
        }
        public IEnumerator KeyboardProbe(PixelLoginWindow w)
        {
            Enter(w,"21",true); yield return Ready(); w.Goals.Back.Select();
            string marker=PixelWorkoutWindow.Arg("-sologym-keyboard-probe"); File.WriteAllText(marker+".ready","ready");
            bool intermediate=false,uncertainty=false,equipment=false; float deadline=Time.realtimeSinceStartup+50;
            while(!File.Exists(marker+".done") && Time.realtimeSinceStartup<deadline)
            {
                var current=w.Goals.Controller.Model; intermediate|=current.ExperienceId=="intermediate"; uncertainty|=current.UncertaintyDialogOpen; equipment|=w.Goals.EquipmentPending; yield return null;
            }
            var m=w.Goals.Controller.Model;
            Check(File.Exists(marker+".done"),"OS key sequence completed");
            Check(intermediate && uncertainty && equipment,"Keyboard traversed selection, confirmation, summary and checkpoint");
            Check(m.GoalId=="strength" && m.ExperienceId=="beginner" && m.Step==GoalsExperienceStep.Review && !w.Goals.EquipmentPending,"Keyboard edits and checkpoint Back retain choices");
            Check(w.Page=="goals" && !w.Account.Controller.Created,"Keyboard review creates no account"); Save(w,marker+".report.json");
        }
        static int Selected(PixelChoiceOption[] options) { int count=0; foreach(var o in options) if(o.isOn) count++; return count; }
        void TextFits(PixelGoalsWindow f,string state)
        {
            Canvas.ForceUpdateCanvases(); foreach(var t in f.GetComponentsInChildren<Text>())
                if(t.text.Length>0 && t.gameObject.activeInHierarchy)
                {
                    Check(t.preferredHeight<=t.rectTransform.rect.height+.1f,state+": text fits "+t.text);
                    if(t.transform.IsChildOf(f.Scroll.content))
                    {
                        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(f.Scroll.content,t.transform);
                        Check(bounds.max.y<=f.Scroll.content.rect.yMax+1 && bounds.min.y>=f.Scroll.content.rect.yMin-1,state+": text stays inside content "+t.text);
                    }
                }
        }
        void Visible(PixelGoalsWindow f,Selectable control,string label)
        {
            Canvas.ForceUpdateCanvases(); var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(f.Scroll.viewport,control.transform);
            Check(control.gameObject.activeInHierarchy && bounds.min.y>=f.Scroll.viewport.rect.yMin-1 && bounds.max.y<=f.Scroll.viewport.rect.yMax+1,label);
        }
        void Save(PixelLoginWindow w,string path)
        {
            var report=new Report{passed=Passed,checks=checks,width=Screen.width,height=Screen.height,locale=w.Controller.Model.Language,failures=failures.ToArray()};
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path,JsonUtility.ToJson(report,true)+"\n"); Debug.Log("GOALS_SMOKE "+JsonUtility.ToJson(report));
        }
        static IEnumerator Ready() { yield return new WaitForEndOfFrame(); yield return null; }
        static IEnumerator Shot(PixelLoginWindow w,string name)
        {
            string path=PixelWorkoutWindow.Arg("-sologym-capture"); if(string.IsNullOrEmpty(path)) yield break;
            yield return new WaitForEndOfFrame(); w.Capture(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"-"+name+".png"));
        }
        static bool Click(Selectable target)
        {
            Canvas.ForceUpdateCanvases(); var r=(RectTransform)target.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=target.gameObject) return false;
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerClickHandler); return true;
        }
    }
}
