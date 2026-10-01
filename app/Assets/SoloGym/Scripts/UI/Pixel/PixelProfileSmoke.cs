using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Fictional profile review checks; no live identity, stored measurements or training.</summary>
    public sealed class PixelProfileSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public int checks,width,height; public string locale; public string[] failures; }
        readonly List<string> failures=new List<string>(); int checks;
        public bool Passed=>failures.Count==0;
        void Check(bool okay,string label) { checks++; if(!okay) { failures.Add(label); Debug.LogError("PROFILE_CHECK_FAILED "+label); } }
        void Enter(PixelLoginWindow w,bool signedIn)
        {
            w.OpenOnboarding(true,signedIn); w.Onboarding.Controller.SetAge("21"); w.Onboarding.Controller.SelectCountry("MX");
            w.Onboarding.Advance(); w.Onboarding.Controller.ChooseGender("female"); w.Onboarding.Controller.ChooseBody("fat"); w.Onboarding.Advance();
            w.Consent.Controller.ChoosePrivacy(true); w.Consent.Controller.ChooseTerms(true); w.Consent.Advance();
        }
        public IEnumerator Run(PixelLoginWindow w)
        {
            var f=w.Profile; var c=f.Controller; string language=w.Controller.Model.Language;
            Check(w.Page=="login","Default startup remains login"); w.OpenProfileReview(); Check(w.Page=="login","Profile shortcut cannot bypass reviewed entry");
            Check(PrivateProfileStateChecks.Run().Contains("checks passed"),"Legacy optional-data, decimal, conversion and production-gate checks");
            w.Controller.SetLanguage(language);
            using(var real=new PrivateProfileController(false))
            {
                real.ContinueNotice(); real.SetHeight("170"); real.Pause(); real.Reset(); real.ContinueMeasurements();
                Check(real.Model.Step==ProfileStep.Notice && real.Model.HeightCm==null && !real.Model.CanContinue,"Reset and pause never grant real profile authorization");
            }
            Enter(w,false); yield return Ready();
            Check(w.Page=="account" && w.Account.PreviewProfile.gameObject.activeInHierarchy,"Provisional account offers explicit profile preview");
            w.Account.Email.Input.text="hero@example.com"; w.Account.Password.Input.text=w.Account.Confirmation.Input.text="fictional secret";
            w.Account.Scroll.verticalNormalizedPosition=0; yield return Ready();
            Check(Click(w.Account.PreviewProfile),"Full account preview link is clickable after scroll"); yield return Ready();
            Check(w.Page=="profile" && c.Model.Step==ProfileStep.Notice && c.Model.HeightCm==null,"Notice precedes any fictional measurements");
            Check(w.Account.Password.Input.text=="" && w.Account.Confirmation.Input.text=="" && !w.Account.Controller.Created,"Profile preview clears secrets without creating an account");
            TextFits(f,"notice"); yield return Shot(w,"notice");
            f.GoBack(); yield return Ready(); Check(w.Page=="account" && w.Account.Email.Input.text=="hero@example.com","Notice Back returns to the same email draft");
            w.OpenProfileReview(); yield return Ready();
            Check(Click(f.Continue),"Notice Continue is a real control"); yield return Ready();
            Check(c.Model.Step==ProfileStep.Measurements && c.Model.HeightCm==170m && c.Model.BodyweightKg==70m,"Clearly marked review fixture loads once");
            TextFits(f,"metric"); yield return Shot(w,"measurements");
            f.Height.Input.text="170,125"; f.Weight.Input.text="70.375"; yield return Ready();
            var cm=c.Model.HeightCm; var kg=c.Model.BodyweightKg;
            for(int i=0;i<10;i++) { c.SetUnits("imperial"); c.SetUnits("metric"); }
            Check(c.Model.HeightCm==cm && c.Model.BodyweightKg==kg,"Native input bindings preserve exact values across repeated conversion");
            Check(Click(f.Units.Option("imperial")),"Imperial choice hit target works"); yield return Ready();
            Check(f.Feet.gameObject.activeInHierarchy && f.Inches.gameObject.activeInHierarchy && !f.Height.gameObject.activeInHierarchy,"Imperial uses separate feet/inches inputs");
            f.Feet.Input.text="5"; f.Inches.Input.text="8.5"; f.Weight.Input.text="150"; yield return Ready();
            Check(c.Model.HeightCm==173.99m && c.Model.BodyweightKg==68.03885550m,"Imperial entry normalizes correct canonical units");
            TextFits(f,"imperial"); yield return Shot(w,"imperial");
            f.Inches.Input.text="12"; f.Advance(); yield return Ready();
            Check(!f.Continue.IsInteractable() && c.Model.HeightInchesText=="12" && c.Model.HeightCm==null,"Invalid inches stay editable and cannot continue");
            c.SetUnits("metric"); Check(c.Model.UnitSystem=="imperial","Invalid raw input prevents lossy unit change");
            Click(w.Privacy); yield return Ready(); Check(w.Page=="privacy","Profile footer opens document reader");
            Click(w.Terms); yield return Ready(); Click(w.Consent.ReaderReturn); yield return Ready();
            Check(w.Page=="profile" && c.Model.HeightInchesText=="12" && c.Model.ErrorKey=="invalid_height_imperial","Reader returns with invalid draft and validation intact");
            Click(w.Language); yield return Ready(); Check(c.Model.HeightInchesText=="12","Locale preserves invalid draft"); w.Controller.SetLanguage(language);
            TextFits(f,"validation"); yield return Shot(w,"invalid");
            f.Feet.Input.text=""; f.Inches.Input.text=""; f.Weight.Input.text=""; c.SetUnits("metric"); yield return Ready();
            Check(c.Model.HeightCm==null && c.Model.BodyweightKg==null && f.Height.Input.text=="" && f.Weight.Input.text=="" && f.Continue.IsInteractable(),"Both optional values may be absent");
            w.Backgrounded(); Check(c.Model.HeightCm==null && c.Model.Step==ProfileStep.Measurements,"Background preserves non-secret profile draft");
            f.Weight.Input.Select(); w.ReviewKeyboard(260); yield return Ready(); f.RevealSelection(); yield return Ready();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(f.Scroll.viewport,f.Weight.Input.transform);
            Check(bounds.min.y>=f.Scroll.viewport.rect.yMin-1 && bounds.max.y<=f.Scroll.viewport.rect.yMax+1,"Focused weight field stays above simulated keyboard");
            Check(!w.Privacy.gameObject.activeInHierarchy,"Footer yields space to keyboard"); w.ReviewKeyboard(0); yield return Ready();
            f.Scroll.verticalNormalizedPosition=0; yield return Ready(); Check(Click(f.Continue),"Empty measurements can continue by pointer"); yield return Ready();
            Check(c.Model.Step==ProfileStep.Readiness && c.Model.Readiness=="" && !f.Continue.IsInteractable(),"Readiness starts unselected");
            TextFits(f,"readiness"); yield return Shot(w,"readiness");
            f.Choices[0].Select(); Check(c.Model.Readiness=="","Focus never chooses readiness");
            for(int i=0;i<f.Choices.Length;i++)
            {
                Check(Click(f.Choices[i]),"Readiness full row clickable "+i); yield return Ready();
                int selected=0; foreach(var choice in f.Choices) if(choice.isOn) selected++;
                Check(selected==1 && f.Choices[i].isOn,"Readiness remains mutually exclusive "+i);
                f.Advance(); yield return Ready();
                Check(c.Model.Step==(i<2?ProfileStep.Checkpoint:ProfileStep.Paused),"Readiness route respects existing domain "+i);
                if(i==0) { TextFits(f,"checkpoint"); yield return Shot(w,"checkpoint"); }
                if(i==3) { TextFits(f,"paused"); yield return Shot(w,"paused"); }
                f.GoBack(); yield return Ready(); Check(c.Model.Step==ProfileStep.Readiness,"Can change the response after checkpoint/pause "+i);
            }
            Check(Click(f.Secondary),"Explicit pause does not require a selected option"); yield return Ready();
            Check(c.Model.Step==ProfileStep.Paused && c.Model.Readiness=="unsure","Pause action uses the uncertainty route");
            f.GoBack(); f.GoBack(); yield return Ready(); Check(c.Model.Step==ProfileStep.Measurements && c.Model.HeightCm==null,"Back retains absent measurements");
            f.Height.Input.text="1e3"; f.Advance(); yield return Ready(); Check(c.Model.ErrorKey=="invalid_height" && f.Height.Input.text=="1e3","Exponent strings cannot become measurements");
            f.Height.Input.text="175.5"; f.Weight.Input.text="82";
            f.GoBack(); f.GoBack(); yield return Ready(); Check(w.Page=="account","Back reaches original form");
            w.OpenProfileReview(); f.Advance(); yield return Ready(); Check(c.Model.HeightCm==175.5m && c.Model.BodyweightKg==82m,"Re-entering does not reload the fixture over edits");
            Check(w.Onboarding.Controller.CharacterId=="female-fat","Measurements do not alter appearance");
            f.GoBack(); f.GoBack(); w.Account.Back.onClick.Invoke(); w.Consent.Back.onClick.Invoke(); w.Onboarding.GoBack(); w.Onboarding.Controller.SetAge("22");
            Check(c.Model.Step==ProfileStep.Notice && c.Model.HeightCm==null && c.Model.Readiness=="","Age change discards old private review draft");
            Enter(w,true); yield return Ready(); Check(w.Page=="profile" && !w.Account.gameObject.activeInHierarchy,"Signed-in review reaches profile without duplicate registration");
            f.Advance(); f.Height.Input.text="160"; f.GoBack(); Check(Click(f.Secondary),"Not now exits explicitly"); yield return Ready();
            Check(w.Page=="login" && c.Model.HeightCm==null && c.Model.BodyweightKg==null && c.Model.Readiness=="" && !w.Consent.Controller.CanContinue,"Exit discards profile and setup drafts");
            Check(FindFirstObjectByType<PixelTrainingHallHome>()==null && !w.Onboarding.Controller.Origin.TermsAccepted,"No real acceptance, Home or exercise is created");
            Enter(w,true); f.Advance(); EventSystem.current.SetSelectedGameObject(null); yield return Ready();
            TextFits(f,"final");
            Check(f.Scroll.content.rect.height<=f.Scroll.viewport.rect.height+1,"Default measurements fit without scrolling");
            var corners=new Vector3[4]; w.Panel.GetWorldCorners(corners);
            Check(corners[0].x>=Screen.safeArea.xMin-1 && corners[2].x<=Screen.safeArea.xMax+1 && corners[0].y>=Screen.safeArea.yMin-1,"Profile remains in landscape safe area");
            Save(w,Path.ChangeExtension(PixelWorkoutWindow.Arg("-sologym-capture"),".smoke.json"));
        }
        public IEnumerator KeyboardProbe(PixelLoginWindow w)
        {
            Enter(w,true); w.Profile.Advance(); yield return Ready();
            w.Profile.Height.Input.Select(); w.Profile.Height.Input.ActivateInputField();
            var marker=PixelWorkoutWindow.Arg("-sologym-keyboard-probe"); File.WriteAllText(marker+".ready","ready");
            bool readiness=false,paused=false; float deadline=Time.realtimeSinceStartup+40;
            while(!File.Exists(marker+".done") && Time.realtimeSinceStartup<deadline)
            { readiness|=w.Profile.Controller.Model.Step==ProfileStep.Readiness; paused|=w.Profile.Controller.Model.Step==ProfileStep.Paused; yield return null; }
            var m=w.Profile.Controller.Model;
            Check(File.Exists(marker+".done"),"OS key sequence completed");
            Check(m.HeightCm==175.5m && m.BodyweightKg==82m,"OS keyboard edits decimal measurements");
            Check(readiness && paused && m.Step==ProfileStep.Readiness && m.Readiness=="pain","OS key selection, pause and Back preserve state");
            Check(w.Page=="profile" && !w.Account.Controller.Created,"OS flow stays isolated");
            Save(w,marker+".report.json");
        }
        void TextFits(PixelPrivateProfileWindow f,string state)
        {
            Canvas.ForceUpdateCanvases(); foreach(var t in f.GetComponentsInChildren<Text>())
                if(t.text.Length>0 && t.gameObject.activeInHierarchy) Check(t.preferredHeight<=t.rectTransform.rect.height+.1f,state+": text fits "+t.text);
        }
        void Save(PixelLoginWindow w,string path)
        {
            var report=new Report{passed=Passed,checks=checks,width=Screen.width,height=Screen.height,locale=w.Controller.Model.Language,failures=failures.ToArray()};
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path,JsonUtility.ToJson(report,true)+"\n"); Debug.Log("PROFILE_SMOKE "+JsonUtility.ToJson(report));
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
