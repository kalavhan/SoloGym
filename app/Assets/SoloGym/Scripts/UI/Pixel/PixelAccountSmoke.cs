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
    /// <summary>Isolated registration fixtures; no real accounts, secrets in reports, or network calls.</summary>
    public sealed class PixelAccountSmoke : MonoBehaviour
    {
        public sealed class Service : IAccountRegistrationService
        {
            public readonly List<TaskCompletionSource<RegistrationResult>> Setup = new List<TaskCompletionSource<RegistrationResult>>();
            public readonly List<TaskCompletionSource<RegistrationResult>> Creation = new List<TaskCompletionSource<RegistrationResult>>();
            public bool CorrectPayload;
            public Task<RegistrationResult> CheckSetupAsync(CancellationToken token)
            { var t = new TaskCompletionSource<RegistrationResult>(); Setup.Add(t); return t.Task; }
            public Task<RegistrationResult> CreateAsync(string email, string password, string permit, CancellationToken token)
            {
                CorrectPayload = email == "hero+review@example.com" && password == " Trial9 " && permit == "fixture-permit";
                var t = new TaskCompletionSource<RegistrationResult>(); Creation.Add(t); return t.Task;
            }
            public void Permit(int index) => Setup[index].SetResult(new RegistrationResult(RegistrationStatus.Ready, "fixture-permit"));
            public void Finish(RegistrationStatus status, int retry = 0) => Creation[Creation.Count - 1].SetResult(new RegistrationResult(status, retryAfterSeconds: retry));
        }
        [Serializable] sealed class Report { public bool passed; public int checks; public string locale; public int width,height; public string[] failures; }
        int checks; readonly List<string> failures = new List<string>();
        public bool Passed => failures.Count == 0;
        void Check(bool okay, string label) { checks++; if (!okay) { failures.Add(label); Debug.LogError("ACCOUNT_CHECK_FAILED " + label); } }
        void Fill(PixelAccountForm f) { f.Email.Input.text = "hero+review@example.com"; f.Password.Input.text = f.Confirmation.Input.text = " Trial9 "; }
        public IEnumerator Run(PixelLoginWindow w, Service service)
        {
            string locale = w.Controller.Model.Language;
            var f = w.Account;
            Check(service != null, "Explicit fixture service"); if (service == null) yield break;
            Check(w.Page == "login", "Normal startup remains login");
            Check(Click(w.CreateAccount), "Native login entry opens registration"); yield return new WaitForEndOfFrame(); yield return null;
            Check(w.Page == "account" && f.gameObject.activeInHierarchy, "Complete account form replaces login panel contents");
            Check(f.Email.Input.text == "" && f.Password.Input.text == "" && f.Confirmation.Input.text == "", "No seeded credentials");
            Check(Click(f.Submit), "Native account action is reachable"); yield return null;
            Check(f.Controller.ErrorKey == "required_email" && service.Setup.Count == 0, "Required email stays local");
            f.Email.Input.text = "not an email"; f.SubmitAccount();
            Check(f.Controller.ErrorKey == "invalid_email" && service.Setup.Count == 0, "Malformed email stays local");
            f.Email.Input.text = "hero+review@example.com"; f.SubmitAccount();
            Check(f.Controller.ErrorKey == "required_password", "Required password");
            f.Password.Input.text = " Trial9 "; f.SubmitAccount();
            Check(f.Controller.ErrorKey == "required_confirmation", "Required confirmation");
            f.Confirmation.Input.text = "Trial9"; f.SubmitAccount();
            Check(f.Controller.ErrorKey == "mismatch" && service.Setup.Count == 0, "Password comparison preserves whitespace");
            yield return Shot(w, "mismatch"); TextFits(f, "mismatch");
            f.Controller.Reset(); Fill(f);
            Check(Click(f.Email.Input), "Pointer focuses email"); yield return null; yield return null;
            f.Email.Input.onSubmit.Invoke(f.Email.Input.text); yield return null;
            Check(EventSystem.current.currentSelectedGameObject == f.Password.Input.gameObject, "Email Return goes to password");
            f.Password.Input.onSubmit.Invoke(f.Password.Input.text); yield return null;
            Check(EventSystem.current.currentSelectedGameObject == f.Confirmation.Input.gameObject, "Password Return goes to confirmation");
            f.Confirmation.Input.DeactivateInputField(); yield return null;
            Check(service.Setup.Count == 0, "Blur never submits");
            Click(f.Password.Visibility); yield return null;
            Check(f.Password.PasswordVisible && !f.Confirmation.PasswordVisible, "Password visibility is independent");
            Click(f.Confirmation.Visibility); yield return null;
            Check(f.Confirmation.PasswordVisible, "Confirmation visibility is available");
            Click(w.Language); yield return null;
            Check(f.Password.Input.text == " Trial9 " && f.Confirmation.Input.text == " Trial9 ", "Locale keeps draft values");
            w.Controller.SetLanguage(locale);
            Check(f.Confirmation.Input.GetComponent<PixelFieldTabNavigation>().Move(false), "Confirmation Tab reaches visibility");
            Check(EventSystem.current.currentSelectedGameObject == f.Confirmation.Visibility.gameObject, "Confirmation toggle is keyboard reachable");
            f.Confirmation.Input.onSubmit.Invoke(f.Confirmation.Input.text); yield return null;
            Check(service.Setup.Count == 1 && service.Creation.Count == 0 && f.Controller.Busy, "Preflight never receives or submits credentials");
            f.SubmitAccount(); w.SubmitGoogle(); w.SubmitEmail();
            Check(service.Setup.Count == 1 && !f.Email.Input.interactable && !w.Privacy.interactable && !f.Back.interactable, "Pending blocks duplicates, other provider and navigation");
            Check(f.Cancel.GetComponent<PixelFieldTabNavigation>().Move(false), "Pending Tab reaches locale");
            Check(w.Language.GetComponent<PixelFieldTabNavigation>().Move(false) && EventSystem.current.currentSelectedGameObject == f.Cancel.gameObject, "Locale Tab returns to pending Cancel");
            Check(Click(f.Cancel), "Pending cancellation reachable"); yield return null;
            Check(!f.Controller.Busy && f.Password.Input.text == "" && f.Confirmation.Input.text == "" && !f.Password.PasswordVisible && !f.Confirmation.PasswordVisible, "Cancellation clears and masks both passwords");
            Fill(f); f.SubmitAccount(); yield return null;
            service.Permit(0); yield return null;
            Check(f.Controller.Busy && service.Creation.Count == 0, "Late cancelled preflight cannot create an identity");
            service.Setup[1].SetResult(new RegistrationResult(RegistrationStatus.Ready)); yield return null;
            Check(f.Controller.ErrorKey == "unavailable" && service.Creation.Count == 0, "Ready without a permit fails closed");
            Fill(f); f.SubmitAccount(); yield return null;
            service.Setup[2].SetResult(new RegistrationResult(RegistrationStatus.Created)); yield return null;
            Check(!f.Controller.Created && service.Creation.Count == 0, "Preflight cannot claim account creation");
            Fill(f); f.SubmitAccount(); yield return null;
            service.Setup[3].SetResult(new RegistrationResult(RegistrationStatus.SetupRequired)); yield return null;
            Check(f.Controller.ErrorKey == "setup" && service.Creation.Count == 0, "Missing age and consent prevent account creation");
            yield return Shot(w, "setup-required"); TextFits(f, "setup");
            using (var actual = new AccountRegistrationController())
            {
                var pending = actual.RegisterAsync("hero+review@example.com", " Trial9 ", " Trial9 ");
                while (!pending.IsCompleted) yield return null;
                Check(actual.ErrorKey == "setup" && !actual.Created, "Actual default adapter never creates an identity");
            }
            f.Controller.SetOnline(false); Fill(f); f.SubmitAccount();
            Check(!f.Submit.IsInteractable() && service.Setup.Count == 4, "Offline prevents preflight");
            yield return Shot(w, "offline"); f.Controller.SetOnline(true);
            Fill(f); f.SubmitAccount(); yield return null; service.Permit(4); yield return null;
            Check(service.Creation.Count == 1 && service.CorrectPayload, "Only permitted creation receives unchanged password and trimmed email");
            yield return Shot(w, "pending");
            w.Backgrounded(); yield return null;
            Check(f.Controller.ErrorKey == "unknown" && !f.Controller.Busy && f.Password.Input.text == "", "Background during creation reports uncertain outcome and clears secrets");
            service.Creation[0].SetResult(new RegistrationResult(RegistrationStatus.Created)); yield return null;
            Check(!f.Controller.Created && w.Page == "account", "Late creation completion cannot navigate");
            yield return Shot(w, "interrupted");
            Fill(f); f.SubmitAccount(); yield return null; service.Permit(5); yield return null;
            service.Finish(RegistrationStatus.Rejected); yield return null;
            Check(f.Controller.ErrorKey == "rejected" && !f.Status.text.Contains("exists"), "Rejection does not enumerate emails");
            Fill(f); f.SubmitAccount(); yield return null; service.Permit(6); yield return null;
            service.Finish(RegistrationStatus.PasswordPolicy); yield return null;
            Check(f.Password.Error.Length > 0 && f.Confirmation.Input.text == "", "Provider password policy is inline with secrets cleared");
            Fill(f); f.SubmitAccount(); yield return null; service.Permit(7); yield return null;
            service.Finish(RegistrationStatus.RateLimited, 1); yield return null;
            Check(!f.Submit.IsInteractable() && f.Controller.RetrySeconds > 0, "Rate limit disables retry");
            yield return new WaitForSecondsRealtime(1.1f); f.Controller.Refresh();
            Check(f.Submit.IsInteractable(), "Cooldown expiry permits retry");
            Fill(f); f.SubmitAccount(); yield return null; service.Permit(8); yield return null;
            service.Creation[service.Creation.Count-1].SetException(new Exception("Injected private provider payload"));
            yield return null;
            Check(f.Controller.ErrorKey == "unknown" && !f.Status.text.Contains("Injected"), "Provider exception after submission is redacted and uncertain");
            Fill(f); Click(w.Privacy); yield return new WaitForEndOfFrame(); yield return null;
            Check(w.Page == "privacy" && w.LastNavigation.ReadOnly && f.Password.Input.text == "", "Privacy is read-only and clears secrets");
            Check(w.Language.GetComponent<PixelFieldTabNavigation>().Move(false) && EventSystem.current.currentSelectedGameObject == w.Return.gameObject, "Document locale Tab reaches return action");
            Click(w.Return); yield return new WaitForEndOfFrame(); yield return null;
            Check(w.Page == "account" && f.Email.Input.text == "hero+review@example.com", "Document return preserves email and account route");
            Click(w.Terms); yield return new WaitForEndOfFrame(); yield return null;
            Check(w.Page == "terms" && w.LastNavigation.ReadOnly, "Terms remain read-only");
            Click(w.Return); yield return new WaitForEndOfFrame(); yield return null;
            Fill(f); f.SubmitAccount(); yield return null; service.Permit(9); yield return null;
            service.Finish(RegistrationStatus.Created); yield return null; yield return null;
            Check(f.Controller.Created && !f.Submit.gameObject.activeSelf && f.Password.Input.text == "", "Trusted completion shows receipt and clears secrets");
            Check(FindFirstObjectByType<PixelTrainingHallHome>() == null, "Account creation never opens fictional Home");
            yield return Shot(w, "created-fixture"); TextFits(f, "receipt");
            Click(f.SignIn); yield return new WaitForEndOfFrame(); yield return null;
            Check(w.Page == "login" && w.Email.Input.text == "hero+review@example.com" && w.Password.Input.text == "", "Sign-in return transfers only email");
            Click(w.CreateAccount); yield return new WaitForEndOfFrame(); yield return null;
            Fill(f); f.Confirmation.Input.Select(); w.ReviewKeyboard(Screen.height * .45f); yield return null; yield return null;
            Check(f.Scroll.viewport.rect.height < 472, "Keyboard reduces scroll region");
            var corners = new Vector3[4]; ((RectTransform)f.Confirmation.Input.transform).GetWorldCorners(corners);
            Check(corners[0].y >= Screen.height * .45f - 1, "Selected confirmation stays above keyboard");
            yield return Shot(w, "keyboard");
            w.ReviewKeyboard(0); f.Controller.Reset(); f.Email.SetValueWithoutNotify("");
            w.Controller.SetLanguage(locale); EventSystem.current.SetSelectedGameObject(null); w.Relayout(); yield return null; yield return null;
            TextFits(f, "normal");
            w.Panel.GetWorldCorners(corners);
            Check(corners[0].x >= Screen.safeArea.xMin-1 && corners[2].x <= Screen.safeArea.xMax+1 && corners[0].y >= Screen.safeArea.yMin-1, "Panel fits safe area");
            Check(f.Scroll.content.rect.height <= f.Scroll.viewport.rect.height+1, "Entire normal form fits without scrolling");
            Save(w, Path.ChangeExtension(PixelWorkoutWindow.Arg("-sologym-capture"), ".smoke.json"));
        }

        public IEnumerator KeyboardProbe(PixelLoginWindow w)
        {
            var f = w.Account; string marker = PixelWorkoutWindow.Arg("-sologym-keyboard-probe");
            bool passwordFocus = false, confirmationFocus = false, visible = false; int checksStarted = 0;
            RegistrationPhase previous = RegistrationPhase.Idle;
            Action changed = () => { if (f.Controller.Phase == RegistrationPhase.CheckingSetup && previous != RegistrationPhase.CheckingSetup) checksStarted++; previous = f.Controller.Phase; };
            f.Controller.Changed += changed;
            f.Email.Input.Select(); f.Email.Input.ActivateInputField(); yield return null;
            File.WriteAllText(marker + ".ready", "ready"); float deadline = Time.realtimeSinceStartup + 30;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < deadline)
            {
                passwordFocus |= f.Password.Input.isFocused; confirmationFocus |= f.Confirmation.Input.isFocused;
                visible |= f.Confirmation.PasswordVisible; yield return null;
            }
            f.Controller.Changed -= changed;
            Check(File.Exists(marker + ".done"), "OS keyboard sequence completed");
            Check(f.Email.Input.text == "hero+fit@example.com", "OS keyboard preserves plus-address");
            Check(passwordFocus && confirmationFocus, "Return advances through both passwords");
            Check(visible, "Tab and Return reach confirmation visibility");
            Check(checksStarted == 1 && f.Controller.ErrorKey == "setup", "OS Return submits once to no-network prerequisites");
            Check(f.Password.Input.text == "" && f.Confirmation.Input.text == "" && !f.Confirmation.PasswordVisible, "OS submission clears and masks secrets");
            Save(w, marker + ".report.json");
        }

        void Save(PixelLoginWindow w, string path)
        {
            var report = new Report { passed = Passed, checks = checks, locale = w.Controller.Model.Language, width = Screen.width, height = Screen.height, failures = failures.ToArray() };
            if (!string.IsNullOrEmpty(path)) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path, JsonUtility.ToJson(report, true)+"\n"); }
            Debug.Log("ACCOUNT_SMOKE " + JsonUtility.ToJson(report));
        }
        void TextFits(PixelAccountForm f, string state)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var t in f.GetComponentsInChildren<Text>())
            {
                if (t.text.Length == 0 || !t.gameObject.activeInHierarchy || t.name.Contains("Editable") || t.name.Contains("Placeholder")) continue;
                Check(t.preferredHeight <= t.rectTransform.rect.height + .1f, state + ": label fits " + t.transform.parent.name);
            }
        }
        static bool Click(Selectable target)
        {
            Canvas.ForceUpdateCanvases(); var r = (RectTransform)target.transform;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != target.gameObject) return false;
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler); return true;
        }
        static IEnumerator Shot(PixelLoginWindow w, string name)
        {
            string path = PixelWorkoutWindow.Arg("-sologym-capture"); if (string.IsNullOrEmpty(path)) yield break;
            yield return new WaitForEndOfFrame();
            w.Capture(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-" + name + ".png"));
        }
    }
}
