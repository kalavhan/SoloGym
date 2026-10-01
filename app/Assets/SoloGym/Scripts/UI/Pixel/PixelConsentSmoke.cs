using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Isolated consent UI and placeholder-boundary checks. No live identity or personal data.</summary>
    public sealed class PixelConsentSmoke : MonoBehaviour
    {
        sealed class CountingService : IAccountRegistrationService
        {
            public int Checks, Creates;
            public Task<RegistrationResult> CheckSetupAsync(CancellationToken token) { Checks++; return Task.FromResult(new RegistrationResult(RegistrationStatus.Ready,"fixture-permit")); }
            public Task<RegistrationResult> CreateAsync(string email,string password,string permit,CancellationToken token) { Creates++; return Task.FromResult(new RegistrationResult(RegistrationStatus.Created)); }
        }
        [Serializable] sealed class Report { public bool passed; public int checks,width,height; public string locale; public string[] failures; }
        readonly List<string> failures=new List<string>(); int checks;
        public bool Passed=>failures.Count==0;
        void Check(bool okay,string label) { checks++; if(!okay) { failures.Add(label); Debug.LogError("CONSENT_CHECK_FAILED "+label); } }
        void Enter(PixelLoginWindow w,bool identity=false)
        {
            w.OpenOnboarding(true,identity); w.Onboarding.Controller.SetAge("21"); w.Onboarding.Controller.SelectCountry("MX");
            w.Onboarding.Advance(); w.Onboarding.Controller.ChooseGender("female"); w.Onboarding.Controller.ChooseBody("fat"); w.Onboarding.Advance();
        }
        public IEnumerator Run(PixelLoginWindow w)
        {
            string language=w.Controller.Model.Language;
            var f=w.Consent; var c=f.Controller;
            Check(w.Page=="login","Default route remains login");
            Check(OnboardingStateChecks.Run().Contains("checks passed"),"Existing real policy and guardian gates preserved"); w.Controller.SetLanguage(language);
            string catalog=Resources.Load<TextAsset>("Onboarding/ReviewDocuments").text;
            foreach(string invalid in new[]{"", "{", catalog.Replace("review_placeholder_user_authorized","unknown"), catalog.Replace("\"production_acceptance_enabled\": false","\"production_acceptance_enabled\": true"), catalog.Replace("\"isFinal\": false","\"isFinal\": true")})
            {
                var missing=new ConsentReviewController(new ReviewDocumentCatalog(invalid)); missing.ChoosePrivacy(true); missing.ChooseTerms(true);
                Check(!missing.CanContinue && !missing.DocumentsAvailable,"Malformed, final or missing review documents cannot authorize preview");
            }
            var service=new CountingService();
            using(var registration=new AccountRegistrationController(service))
            {
                bool cleared=false; registration.SecretsCleared+=()=>cleared=true;
                var task=registration.RegisterAsync("hero@example.com","review secret","review secret",true); while(!task.IsCompleted) yield return null;
                Check(registration.ErrorKey=="provisional" && !registration.Created && cleared && service.Checks==0 && service.Creates==0,"Even a ready adapter receives no calls from placeholder registration");
            }
            Check(Click(w.Privacy),"Login privacy footer opens reader"); yield return Ready();
            Check(w.Page=="privacy" && f.IsReading && f.DocumentBody.text.StartsWith("Lorem ipsum") && w.LastNavigation.ReadOnly,"Login reader uses explicit Lorem ipsum resource");
            Click(f.ReaderReturn); yield return Ready(); Check(w.Page=="login","Reader returns to login");
            Enter(w); yield return Ready();
            Check(w.Page=="consent" && !c.PrivacyChecked && !c.TermsChecked && !f.Continue.IsInteractable(),"Consent replaces pending checkpoint with empty decisions");
            Check(!w.Onboarding.Hero.gameObject.activeInHierarchy,"Consent has no stale character overlay");
            f.Advance(); Check(w.Page=="consent","Direct Continue also requires both choices");
            TextFits(f,"empty"); yield return Shot(w,"empty");
            Check(Click(f.ReadPrivacy),"Full privacy row is pointer target"); yield return Ready();
            Check(w.Page=="privacy" && f.DocumentId=="privacy" && !c.PrivacyChecked,"Reading does not accept privacy");
            string body=f.DocumentBody.text;
            Check(body==c.Document("privacy").body && !c.Document("privacy").isFinal,"Displayed body matches non-final resource");
            TextFits(f,"privacy"); yield return Shot(w,"privacy");
            f.ReaderFocus.Select(); f.ReaderFocus.ScrollBy(100000); Check(!c.PrivacyChecked && !c.TermsChecked,"Scrolling never changes decisions");
            float position=f.DocumentScroll.verticalNormalizedPosition;
            Click(w.Terms); yield return Ready();
            Check(f.DocumentId=="terms" && w.Page=="terms","Switching document keeps decisions return route");
            Click(f.ReaderReturn); yield return Ready(); Check(w.Page=="consent" && !c.PrivacyChecked && !c.TermsChecked,"Document return keeps choices empty");
            Check(Click(f.PrivacyChoice),"Full privacy choice row toggles"); yield return null;
            Check(c.PrivacyChecked && !c.TermsChecked && !f.Continue.IsInteractable(),"One choice cannot continue");
            Check(Click(f.TermsChoice),"Independent terms choice toggles"); yield return null;
            Check(c.PrivacyChecked && c.TermsChecked && f.Continue.IsInteractable(),"Both choices enable preview");
            Click(f.TermsChoice); yield return null; Check(!f.Continue.IsInteractable() && c.PrivacyChecked,"Revoking terms disables Continue without revoking privacy");
            Click(f.TermsChoice); yield return null;
            f.ReadPrivacy.Select(); f.ReadPrivacy.GetComponent<PixelFieldTabNavigation>().Move(false);
            Check(EventSystem.current.currentSelectedGameObject==f.ReadTerms.gameObject,"Tab reaches document actions");
            f.PrivacyChoice.Select(); Check(c.PrivacyChecked,"Focus does not toggle");
            Click(w.Language); yield return null;
            Check(c.PrivacyChecked && c.TermsChecked,"Locale retains review decisions"); w.Controller.SetLanguage(language);
            Click(f.ReadPrivacy); yield return Ready();
            Check(Mathf.Abs(position-f.DocumentScroll.verticalNormalizedPosition)<.02f,"Reader position restored per document");
            Click(w.Language); yield return null;
            Check(f.DocumentBody.text==body && c.PrivacyChecked && c.TermsChecked,"Latin body is stable while reader UI localizes");
            w.Controller.SetLanguage(language); Click(f.ReaderReturn); yield return Ready();
            w.Backgrounded(); Check(c.CanContinue && w.Onboarding.Controller.Origin.AgeText=="21","Background preserves non-secret draft choices");
            yield return Shot(w,"checked");
            Check(Click(f.Continue),"Native Continue opens registration preview"); yield return Ready();
            Check(w.Page=="account" && !w.Account.Controller.Created,"Review handoff opens existing account form");
            var a=w.Account; a.Email.Input.text="hero@example.com"; a.Password.Input.text=a.Confirmation.Input.text="review secret"; a.SubmitAccount(); yield return null;
            Check(a.Controller.ErrorKey=="provisional" && !a.Controller.Created && a.Password.Input.text=="" && a.Confirmation.Input.text=="","Placeholder submission is honest and clears secrets");
            yield return Shot(w,"registration-preview");
            a.Password.Input.text=a.Confirmation.Input.text="review secret";
            Click(w.Privacy); yield return Ready(); Check(a.Password.Input.text=="","Account reader clears secrets");
            Click(f.ReaderReturn); yield return Ready(); Check(w.Page=="account" && a.Email.Input.text=="hero@example.com","Reader restores account form");
            Check(Click(a.Back),"Registration Back returns to decisions"); yield return Ready();
            Check(w.Page=="consent" && c.CanContinue && w.Onboarding.Controller.CharacterId=="female-fat","Back preserves review choices and character draft");
            Click(f.Back); yield return Ready(); Check(w.Page=="onboarding" && w.Onboarding.Controller.Step==GuildSetupStep.Character,"Consent Back restores character step");
            w.Onboarding.Controller.ChooseBody("medium"); w.Onboarding.Advance(); yield return Ready(); Check(c.CanContinue,"Cosmetic changes preserve decisions");
            Click(f.Back); yield return Ready(); w.Onboarding.GoBack(); w.Onboarding.Controller.SetAge("22");
            Check(!c.PrivacyChecked && !c.TermsChecked,"Age changes clear decisions");
            w.Onboarding.Advance(); w.Onboarding.Advance(); yield return Ready(); c.ChoosePrivacy(true); c.ChooseTerms(true);
            Click(f.Back); yield return Ready(); w.Onboarding.GoBack(); w.Onboarding.Controller.SelectCountry("CA");
            Check(!c.CanContinue && !c.PrivacyChecked && !c.TermsChecked,"Country changes clear decisions");
            w.Onboarding.GoBack(); yield return Ready(); Check(w.Page=="login" && w.Onboarding.Controller.Origin.AgeText=="" && !c.CanContinue,"Exit clears the full review draft");
            Enter(w,true); yield return Ready(); c.ChoosePrivacy(true); c.ChooseTerms(true); f.Advance(); yield return Ready();
            Check(w.Page=="profile-pending" && !w.Account.gameObject.activeInHierarchy,"Signed-in identity is never sent to duplicate registration");
            Check(FindFirstObjectByType<PixelTrainingHallHome>()==null && !w.Onboarding.Controller.Origin.PrivacyAcknowledged,"Review choices create no trusted consent or Home access");
            yield return Shot(w,"signed-in-checkpoint"); Click(w.Return); yield return Ready(); Check(w.Page=="consent" && c.CanContinue,"Signed-in checkpoint returns to decisions");
            // Exercise long future copy in the same reader without regenerating artwork.
            Click(f.ReadTerms); yield return Ready(); f.DocumentBody.text=body+"\n\n"+body+"\n\n"+body; f.Relayout(w.Panel.rect.height); yield return Ready();
            Check(f.DocumentScroll.content.rect.height>f.DocumentScroll.viewport.rect.height,"Long replacement text scrolls");
            f.ReaderFocus.Select(); f.ReaderFocus.ScrollBy(200); yield return null; Check(f.DocumentScroll.content.anchoredPosition.y>0,"Keyboard-scroll target moves document");
            yield return Shot(w,"long-document");
            Click(f.ReaderReturn); yield return Ready();
            f.gameObject.SetActive(false);
            var unavailable=new GameObject("Missing documents fixture",typeof(RectTransform)).AddComponent<PixelConsentWindow>();
            bool advanced=false;
            unavailable.Initialize(w.Panel,()=>{},()=>advanced=true,id=>{},()=>{},w.Privacy,w.Language,new ReviewDocumentCatalog("{}"));
            unavailable.OpenDecisions(language); unavailable.Relayout(w.Panel.rect.height); yield return Ready();
            Check(!unavailable.Continue.IsInteractable() && !unavailable.PrivacyChoice.IsInteractable() && !unavailable.TermsChoice.IsInteractable(),"Missing-document UI disables choices and continuation");
            unavailable.Advance(); Check(!advanced,"Missing-document direct action cannot advance");
            TextFits(unavailable,"missing documents"); yield return Shot(w,"missing-documents");
            unavailable.OpenDocument("privacy",language,"VOLVER"); yield return Ready();
            Check(!unavailable.DocumentBody.text.StartsWith("Lorem ipsum"),"Missing reader never invents a document body");
            TextFits(unavailable,"missing reader");
            Destroy(unavailable.gameObject); yield return Ready();
            Enter(w); EventSystem.current.SetSelectedGameObject(null); yield return Ready();
            TextFits(f,"normal"); Check(f.DecisionScroll.content.rect.height<=f.DecisionScroll.viewport.rect.height+1,"Normal consent fits without scrolling");
            var corners=new Vector3[4]; w.Panel.GetWorldCorners(corners);
            Check(corners[0].x>=Screen.safeArea.xMin-1 && corners[2].x<=Screen.safeArea.xMax+1 && corners[0].y>=Screen.safeArea.yMin-1,"Consent fits safe area");
            Save(w,Path.ChangeExtension(PixelWorkoutWindow.Arg("-sologym-capture"),".smoke.json"));
        }
        public IEnumerator KeyboardProbe(PixelLoginWindow w)
        {
            Enter(w); yield return Ready(); var f=w.Consent;
            bool reader=false,account=false; string marker=PixelWorkoutWindow.Arg("-sologym-keyboard-probe");
            f.Back.Select(); File.WriteAllText(marker+".ready","ready"); float deadline=Time.realtimeSinceStartup+40;
            while(!File.Exists(marker+".done") && Time.realtimeSinceStartup<deadline)
            { reader|=f.IsReading && f.gameObject.activeInHierarchy; account|=w.Page=="account"; yield return null; }
            Check(File.Exists(marker+".done"),"OS sequence completed"); Check(reader && account,"OS keyboard traverses reader, choices and registration");
            Check(w.Page=="consent" && f.Controller.CanContinue,"OS Back restores review decisions");
            Check(!w.Account.Controller.Created && !w.Onboarding.Controller.Origin.TermsAccepted,"OS preview creates no account or real acceptance");
            Save(w,marker+".report.json");
        }
        void TextFits(PixelConsentWindow f,string state)
        {
            Canvas.ForceUpdateCanvases(); foreach(var t in f.GetComponentsInChildren<Text>())
                if(t.text.Length>0 && t.gameObject.activeInHierarchy) Check(t.preferredHeight<=t.rectTransform.rect.height+.1f,state+": text fits "+t.text);
        }
        void Save(PixelLoginWindow w,string path)
        {
            var report=new Report{passed=Passed,checks=checks,width=Screen.width,height=Screen.height,locale=w.Controller.Model.Language,failures=failures.ToArray()};
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path,JsonUtility.ToJson(report,true)+"\n"); Debug.Log("CONSENT_SMOKE "+JsonUtility.ToJson(report));
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
