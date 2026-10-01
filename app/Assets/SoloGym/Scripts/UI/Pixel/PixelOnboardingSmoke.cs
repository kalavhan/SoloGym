using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Fictional local drafts only. No account, profile, consent or provider writes.</summary>
    public sealed class PixelOnboardingSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public int checks,width,height; public string locale; public string[] failures; }
        readonly List<string> failures = new List<string>(); int checks;
        public bool Passed => failures.Count == 0;
        void Check(bool okay,string text) { checks++; if (!okay) { failures.Add(text); Debug.LogError("ONBOARDING_CHECK_FAILED "+text); } }
        public IEnumerator Run(PixelLoginWindow w)
        {
            string language=w.Controller.Model.Language;
            string homeKey="SoloGym.PixelHall.Character.v1", original=PlayerPrefs.GetString(homeKey); bool hadHome=PlayerPrefs.HasKey(homeKey);
            Check(w.Page=="login","Default entry stays login");
            Check(Click(w.CreateAccount),"Native create-account entry"); yield return Ready();
            var f=w.Onboarding; var c=f.Controller;
            Check(w.Page=="onboarding" && c.Step==GuildSetupStep.AgeCountry,"Create account enters origin");
            Check(c.Origin.AgeText=="" && c.Origin.CountryCode=="","Private draft starts empty");
            Check(OnboardingStateChecks.Run().Contains("checks passed"),"Existing policy, consent and guardian gates preserved");
            w.Controller.SetLanguage(language); f.SetLocale(language);
            f.Advance(); Check(c.ErrorKey=="required_age","Age required");
            foreach(var invalid in new[]{"0","121","15.5","-1","+21","٢١"," 21 "})
            { f.Age.Input.text=invalid; f.Advance(); Check(c.ErrorKey=="invalid_age","Reject age syntax or range: "+invalid); }
            f.Age.Input.text="14"; f.Advance(); Check(c.ErrorKey=="under_15","Under-15 stays at age gate");
            f.Age.Input.text="15"; f.Advance(); Check(c.ErrorKey=="required_country","15 still needs country");
            f.Age.Input.text="18"; c.SelectCountry("ZZ"); f.Advance(); Check(c.ErrorKey=="required_country","Unknown country is not accepted");
            f.Age.Input.text="21"; f.Age.Input.Select(); yield return Ready(); f.Age.Input.onSubmit.Invoke("21"); yield return null;
            Check(EventSystem.current.currentSelectedGameObject==f.Country.gameObject,"Age Return focuses country without opening it");
            f.Age.Input.DeactivateInputField(); Check(c.Step==GuildSetupStep.AgeCountry,"Blur never advances");
            Check(Click(f.Country),"Country picker pointer entry"); yield return Ready();
            Check(f.CountryOpen && !w.Privacy.interactable && !w.Language.interactable && !f.Continue.interactable,"Picker gates underlying navigation");
            f.Search.Input.text="mex"; Check(f.CountryResults.Count==1,"Accent-insensitive country search");
            Check(Click(f.CountryResults[0]),"Native country selection"); yield return Ready();
            Check(!f.CountryOpen && c.Origin.CountryCode=="MX","Select stable MX code");
            f.OpenCountries(); f.Search.Input.text="zzzzz"; Check(f.CountryResults.Count==0,"No-results search");
            f.Search.Input.onSubmit.Invoke("zzzzz"); Check(EventSystem.current.currentSelectedGameObject==f.CloseCountries.gameObject,"Empty search Return reaches Close");
            f.GoBack(); Check(!f.CountryOpen && c.Origin.CountryCode=="MX","Escape path closes picker without losing selection");
            f.OpenCountries(); f.Search.Input.text="CA"; Check(f.CountryResults.Count>0,"Country code search");
            f.Search.Input.text=""; Check(f.CountryResults.Count==c.Countries.Length,"Empty query restores countries");
            yield return Shot(w,"countries"); f.ClosePicker();
            yield return Shot(w,"origin"); TextFits(f,"origin");
            Check(f.Scroll.content.rect.height<=f.Scroll.viewport.rect.height+1,"Origin fits without scroll");
            Check(Click(f.Continue),"Native Continue to character"); yield return Ready();
            Check(c.Step==GuildSetupStep.Character && f.Hero.gameObject.activeInHierarchy,"Character step shows hero");
            Check(f.Bodies.Columns==4,"Four character cards fit in one row");
            Check(!c.ChooseBody("obese") && !c.ChooseGender("invalid"),"Only approved whole appearances allowed");
            foreach(var gender in new[]{"male","female"})
            {
                Check(Click(f.Gender.Option(gender)),"Native gender: "+gender); yield return null;
                foreach(var body in new[]{"skinny","medium","fat","muscular"})
                {
                    Check(Click(f.Bodies.Option(body)),"Native card: "+gender+"-"+body); yield return null;
                    Check(c.CharacterId==gender+"-"+body && PixelLabRoster.TryFind(c.CharacterId,out var entry) && f.Hero.sprite==entry.Sprite && entry.Sprite.rect.size==new Vector2(256,256),"Unchanged 256 canvas: "+c.CharacterId);
                    Check(f.Hero.rectTransform.anchoredPosition==new Vector2(336,-653) && f.Hero.rectTransform.sizeDelta==Vector2.one*499.2f,"Uniform feet and scale: "+c.CharacterId);
                    TextFits(f,c.CharacterId); yield return Shot(w,c.CharacterId);
                }
            }
            f.Bodies.Option("skinny").Select(); f.Bodies.Option("skinny").OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right});
            Check(c.Body=="muscular" && EventSystem.current.currentSelectedGameObject==f.Bodies.Option("medium").gameObject,"Arrow focus does not commit appearance");
            f.Bodies.Option("medium").OnSubmit(new BaseEventData(EventSystem.current)); Check(c.Body=="medium","Return commits focused appearance");
            Click(w.Language); yield return null;
            Check(c.Origin.CountryCode=="MX" && c.CharacterId=="female-medium" && c.Origin.AgeText=="21","Locale preserves private draft and appearance");
            w.Controller.SetLanguage(language); yield return null;
            Click(w.Privacy); yield return Ready();
            Check(w.Page=="privacy" && w.LastNavigation.ReadOnly && !f.Hero.gameObject.activeInHierarchy,"Read-only document hides hero");
            Click(w.Terms); yield return Ready();
            Check(w.Page=="terms" && w.LastNavigation.ReadOnly,"Switch documents while preserving return route");
            Check(Click(w.Consent.ReaderReturn),"Native return from document"); yield return Ready();
            Check(w.Page=="onboarding" && c.Step==GuildSetupStep.Character && c.CharacterId=="female-medium" && f.Hero.gameObject.activeInHierarchy,"Document returns to same draft step");
            Click(w.Terms); yield return Ready(); Click(w.Consent.ReaderReturn); yield return Ready();
            f.GoBack(); Check(c.Step==GuildSetupStep.AgeCountry && !f.Hero.gameObject.activeSelf && c.Origin.AgeText=="21","Back retains origin and hides hero");
            f.Advance(); Check(c.CharacterId=="female-medium","Forward retains selected character");
            f.Advance(); yield return Ready();
            Check(c.Step==GuildSetupStep.Consent && !c.Origin.PrivacyAcknowledged && !c.Origin.TermsAccepted && !w.Account.Controller.Created,"Final Continue cannot accept consent or create account");
            Check(FindFirstObjectByType<PixelTrainingHallHome>()==null && PlayerPrefs.GetString(homeKey)==original && PlayerPrefs.HasKey(homeKey)==hadHome,"No Home entry or committed appearance");
            yield return Shot(w,"consent");
            Click(w.Consent.Back); yield return Ready(); f.GoBack(); f.Age.Input.Select(); w.ReviewKeyboard(Screen.height*.45f); yield return Ready();
            Check(f.Scroll.viewport.rect.height<472,"Keyboard reduces origin viewport");
            var corners=new Vector3[4]; ((RectTransform)f.Age.Input.transform).GetWorldCorners(corners);
            Check(corners[0].y>=Screen.height*.45f-1,"Age stays above keyboard"); yield return Shot(w,"keyboard"); w.ReviewKeyboard(0);
            f.OpenCountries(); w.Backgrounded(); Check(!f.CountryOpen && c.Origin.AgeText=="21","Background closes keyboard/picker and retains memory draft");
            f.GoBack(); yield return Ready();
            Check(w.Page=="login" && c.Origin.AgeText=="" && c.Origin.CountryCode=="" && c.CharacterId=="male-medium","Exit clears draft");
            Check(!f.Hero.gameObject.activeInHierarchy,"Login has no stale hero");
            w.OpenOnboarding(); c.SetAge("120"); c.SelectCountry("MX"); f.Advance(); Check(c.Step==GuildSetupStep.Character,"Upper age boundary accepted only as draft");
            f.GoBack(); f.Age.Input.text="21"; f.Advance(); EventSystem.current.SetSelectedGameObject(null); yield return Ready();
            TextFits(f,"normal character"); Check(f.Scroll.content.rect.height<=f.Scroll.viewport.rect.height+1,"Character fits without scroll");
            w.Panel.GetWorldCorners(corners); Check(corners[0].x>=Screen.safeArea.xMin-1 && corners[2].x<=Screen.safeArea.xMax+1 && corners[0].y>=Screen.safeArea.yMin-1,"Panel fits safe area");
            Save(w,Path.ChangeExtension(PixelWorkoutWindow.Arg("-sologym-capture"),".smoke.json"));
        }
        public IEnumerator KeyboardProbe(PixelLoginWindow w)
        {
            var f=w.Onboarding; string marker=PixelWorkoutWindow.Arg("-sologym-keyboard-probe"); bool countryFocus=false, picker=false, character=false;
            f.Age.Input.Select(); f.Age.Input.ActivateInputField(); yield return null; File.WriteAllText(marker+".ready","ready");
            float deadline=Time.realtimeSinceStartup+40;
            while(!File.Exists(marker+".done") && Time.realtimeSinceStartup<deadline)
            { countryFocus|=EventSystem.current.currentSelectedGameObject==f.Country.gameObject; picker|=f.CountryOpen; character|=f.Controller.Step==GuildSetupStep.Character; yield return null; }
            Check(File.Exists(marker+".done"),"OS input completed"); Check(f.Controller.Origin.AgeText=="21" && f.Controller.Origin.CountryCode=="MX","OS input selects age and country");
            Check(countryFocus && picker && character,"OS Return traverses both screens"); Check(f.Controller.CharacterId=="female-muscular","OS keyboard selects gender and final card");
            Check(f.Controller.Step==GuildSetupStep.Consent && !f.Controller.Origin.TermsAccepted,"OS final Continue stops at consent boundary");
            Save(w,marker+".report.json");
        }
        void TextFits(PixelOnboardingFlow f,string state)
        {
            Canvas.ForceUpdateCanvases();
            foreach(var t in f.GetComponentsInChildren<Text>())
            { if(t.text.Length==0 || !t.gameObject.activeInHierarchy || t.name.Contains("Editable") || t.name.Contains("Placeholder")) continue;
              Check(t.preferredHeight<=t.rectTransform.rect.height+.1f,state+": text fits "+t.text); }
        }
        void Save(PixelLoginWindow w,string path)
        {
            var report=new Report{passed=Passed,checks=checks,width=Screen.width,height=Screen.height,locale=w.Controller.Model.Language,failures=failures.ToArray()};
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path,JsonUtility.ToJson(report,true)+"\n"); Debug.Log("ONBOARDING_SMOKE "+JsonUtility.ToJson(report));
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
            var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
            if(hits.Count==0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=target.gameObject) return false;
            ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.pointerClickHandler); return true;
        }
    }
}
