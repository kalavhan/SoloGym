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
    /// <summary>Explicit isolated review only. No provider traffic, credentials in reports or successful sample Home.</summary>
    public sealed class PixelLoginSmoke : MonoBehaviour
    {
        public sealed class Service : IWelcomeAuthService
        {
            public int EmailCalls, GoogleCalls;
            readonly List<TaskCompletionSource<AuthOutcome>> operations = new List<TaskCompletionSource<AuthOutcome>>();
            public int Count => operations.Count;
            public Task<AuthOutcome> SignInEmailAsync(string email, string password, CancellationToken token) { EmailCalls++; return Begin(); }
            public Task<AuthOutcome> SignInGoogleAsync(CancellationToken token) { GoogleCalls++; return Begin(); }
            Task<AuthOutcome> Begin() { var task = new TaskCompletionSource<AuthOutcome>(); operations.Add(task); return task.Task; }
            public void Complete(int index, AuthStatus status, string destination = null, int retry = 0) => operations[index].TrySetResult(new AuthOutcome(status, destination, retryAfterSeconds: retry));
            public void Fail(int index) => operations[index].TrySetException(new Exception("Injected provider exception; must not reach UI or logs"));
        }
        [Serializable] sealed class Report { public bool passed; public int checks; public string locale; public int width,height; public string[] failures; }
        int checks; readonly List<string> failures = new List<string>();
        public bool Passed => failures.Count == 0;
        void Check(bool okay, string label) { checks++; if (!okay) { failures.Add(label); Debug.LogError("LOGIN_CHECK_FAILED " + label); } }
        public IEnumerator Run(PixelLoginWindow w, Service service)
        {
            string locale = w.Controller.Model.Language;
            Check(service != null, "Review uses only an injected service"); if (service == null) yield break;
            Check(w.Controller.Model.Email == "" && w.Password.Input.text == "" && w.Page == "login", "No seeded account or secret");
            Check(FindFirstObjectByType<WelcomeScreen>() == null, "Default Welcome entry hands off to landscape login");
            Check(Screen.width > Screen.height && !Screen.autorotateToPortrait, "Landscape entry");
            Check(Click(w.Submit), "Native raycast reaches sign-in"); yield return null;
            Check(w.Controller.Model.ErrorKey == "required_email" && service.Count == 0, "Empty email stays local");
            w.Email.Input.text = "invalid"; w.SubmitEmail();
            Check(w.Controller.Model.ErrorKey == "invalid_email" && service.Count == 0, "Malformed email never reaches provider");
            w.Email.Input.text = "hero+review@example.com"; w.SubmitEmail();
            Check(w.Controller.Model.ErrorKey == "required_password" && service.Count == 0, "Empty password stays local");
            w.Controller.OpenEmail();
            Check(Click(w.Email.Input), "Native pointer focuses email"); yield return null; yield return null;
            Check(w.Email.Input.isFocused, "Email enters native text editing");
            w.Email.Input.onSubmit.Invoke(w.Email.Input.text); yield return null; yield return null;
            Check(EventSystem.current.currentSelectedGameObject == w.Password.Input.gameObject, "Email keyboard submit selects password");
            w.Password.Input.text = "review-only-value";
            w.Password.Input.DeactivateInputField(); yield return null;
            Check(service.Count == 0, "Ordinary blur does not submit credentials");
            Click(w.Password.Visibility); yield return null;
            Check(w.Password.PasswordVisible && w.Password.Input.inputType == InputField.InputType.Standard, "Visibility control reveals existing value");
            string email = w.Email.Input.text; string value = w.Password.Input.text;
            Click(w.Language); yield return null;
            Check(w.Email.Input.text == email && w.Password.Input.text == value, "Locale change preserves typed values");
            w.Controller.SetLanguage(locale);
            Check(w.Email.Input.GetComponent<PixelFieldTabNavigation>().Move(false), "Tab advances from email");
            Check(EventSystem.current.currentSelectedGameObject == w.Password.Input.gameObject, "Tab target is password");
            Check(w.Password.Input.GetComponent<PixelFieldTabNavigation>().Move(false), "Tab reaches password visibility");
            Check(EventSystem.current.currentSelectedGameObject == w.Password.Visibility.gameObject, "Visibility is keyboard reachable");
            // Native keyboard Return follows the same onSubmit event as platform Done.
            w.Password.Input.onSubmit.Invoke(w.Password.Input.text); yield return null;
            Check(service.Count == 1 && w.Controller.Model.IsBusy, "Keyboard submit begins one provider request");
            w.Submit.OnSubmit(new BaseEventData(EventSystem.current)); w.SubmitEmail(); w.SubmitGoogle();
            Check(service.Count == 1 && !w.Google.interactable && !w.Password.Input.interactable && w.Cancel.gameObject.activeInHierarchy, "Pending state blocks duplicate and alternate submissions, exposes Cancel");
            yield return Shot(w, "pending");
            Click(w.Cancel); yield return null;
            Check(!w.Controller.Model.IsBusy && w.Password.Input.text == "" && !w.Password.PasswordVisible, "Cancel clears and masks password");
            w.Password.Input.text = "review-only-value"; w.SubmitEmail(); yield return null;
            service.Complete(0, AuthStatus.Success, "WIN-006"); yield return null; yield return null;
            Check(w.Controller.Model.IsBusy && w.Page == "login", "Late cancelled completion cannot replace a newer request");
            service.Complete(1, AuthStatus.InvalidCredentials); yield return null; yield return null;
            Check(!w.Controller.Model.IsBusy && w.Controller.Model.ErrorKey == "invalid" && w.Email.Input.text == email && w.Password.Input.text == "", "Generic invalid credentials preserve email and clear secret");
            yield return Shot(w, "invalid-credentials");
            TextFits(w, "invalid");
            w.Controller.SetOnline(false);
            Check(!w.Submit.IsInteractable() && !w.Google.interactable && w.Controller.Model.ErrorKey == "offline", "Offline state disables provider actions");
            yield return Shot(w, "offline"); w.Controller.SetOnline(true);
            w.Password.Input.text = "review-only-value"; w.SubmitEmail(); yield return null; int backgroundRequest = service.Count - 1;
            w.Backgrounded();
            Check(!w.Controller.Model.IsBusy && w.Password.Input.text == "", "Background cancels email and clears input");
            service.Complete(backgroundRequest, AuthStatus.Success, "WIN-006"); yield return null; yield return null;
            Check(w.Page == "login", "Backgrounded email cannot navigate late");
            w.SubmitGoogle(); yield return null; int googleRequest = service.Count - 1;
            w.Backgrounded(); Check(w.Controller.Model.IsBusy, "Native provider can background without cancelling Google flow");
            service.Complete(googleRequest, AuthStatus.Cancelled); yield return null; yield return null;
            Check(!w.Controller.Model.IsBusy && w.Page == "login" && w.Controller.Model.ErrorKey == "", "Provider cancellation returns to idle sign-in");
            w.SubmitGoogle(); yield return null; service.Fail(service.Count - 1); yield return null; yield return null;
            Check(w.Controller.Model.ErrorKey == "provider_error" && !w.Status.text.Contains("Injected"), "Provider exception is generic and redacted");
            w.SubmitGoogle(); yield return null; service.Complete(service.Count - 1, AuthStatus.RateLimited, retry: 1); yield return null; yield return null;
            Check(!w.Google.IsInteractable() && w.Controller.Model.RetryAfterSeconds > 0, "Rate limit disables retry and shows remaining time");
            yield return new WaitForSecondsRealtime(1.1f); w.Controller.Refresh();
            Check(w.Google.IsInteractable(), "Rate limit permits retry after expiry");
            w.SubmitGoogle(); yield return null; service.Complete(service.Count - 1, AuthStatus.Unavailable); yield return null; yield return null;
            Check(w.Controller.Model.ErrorKey == "unavailable" && w.Page == "login", "Unavailable provider is honest");
            w.SubmitGoogle(); yield return null; service.Complete(service.Count - 1, AuthStatus.Success, "WIN-014"); yield return null; yield return null;
            Check(w.Controller.Model.ErrorKey == "provider_error" && w.Page == "login", "Untrusted or unsupported route cannot open Home");
            w.SubmitGoogle(); yield return null; service.Complete(service.Count - 1, AuthStatus.Success, "WIN-006"); yield return null; yield return null;
            Check(w.Page == "checkpoint" && w.LastNavigation.AuthorizedByBackend && FindFirstObjectByType<PixelTrainingHallHome>() == null, "Valid identity preserves pending onboarding, never enters fictional Home");
            yield return Shot(w, "identity-checkpoint"); TextFits(w, "checkpoint");
            Click(w.Return); yield return new WaitForEndOfFrame(); yield return null;
            Check(w.Page == "login" && w.Password.Input.text == "", "Return restores only email");
            int before = service.Count; Check(Click(w.Recovery), "Native recovery raycast after returning from checkpoint"); yield return null;
            Check(w.Page == "recovery" && service.Count == before, "Recovery entry makes no false sent-email claim");
            TextFits(w, "recovery"); yield return Shot(w, "recovery"); Click(w.Return); yield return new WaitForEndOfFrame(); yield return null;
            Click(w.CreateAccount); yield return null;
            Check(w.Page == "account" && service.Count == before, "Create-account entry makes no fake account");
            TextFits(w, "create"); yield return Shot(w, "create-account"); Click(w.Account.Back); yield return new WaitForEndOfFrame(); yield return null;
            Click(w.Privacy); yield return null;
            Check(w.Page == "privacy" && w.LastNavigation.ReadOnly && w.NoticeBody.text.Length > 0, "Privacy opens actual read-only document availability");
            string document = w.NoticeBody.text; Click(w.Language); yield return null;
            Check(w.NoticeBody.text != document && w.Page == "privacy", "Document availability localizes without accepting anything");
            TextFits(w, "privacy"); w.Controller.SetLanguage(locale); Click(w.Return); yield return new WaitForEndOfFrame(); yield return null;
            Click(w.Terms); yield return null;
            Check(w.Page == "terms" && w.LastNavigation.ReadOnly, "Terms are read-only"); Click(w.Return); yield return new WaitForEndOfFrame(); yield return null;
            // Simulate platform keyboard geometry at the same input/layout boundary without a real mobile IME.
            w.Password.Input.Select(); w.ReviewKeyboard(Screen.height * .45f); yield return null; yield return null;
            Check(w.FormScroll.viewport.rect.height < 516 && w.Composition.localScale.x > 0, "Keyboard shortens scroll viewport, not font scale");
            yield return Shot(w, "keyboard-layout"); w.ReviewKeyboard(0); w.Controller.CancelAuthentication();
            w.Controller.SetEmail(""); w.Controller.OpenEmail(); w.Controller.SetLanguage(locale); EventSystem.current.SetSelectedGameObject(null);
            w.Relayout(); yield return null; yield return null; TextFits(w, "normal");
            var rect = w.Panel; var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Check(corners[0].x >= Screen.safeArea.xMin - 1 && corners[2].x <= Screen.safeArea.xMax + 1 && corners[0].y >= Screen.safeArea.yMin - 1 && corners[2].y <= Screen.safeArea.yMax + 1, "Panel fits device safe area");
            Check(w.FormScroll.content.rect.height <= w.FormScroll.viewport.rect.height + 1, "Normal state exposes the whole form without scrolling");
            var result = new Report { passed = Passed, checks = checks, locale = locale, width = Screen.width, height = Screen.height, failures = failures.ToArray() };
            string path = PixelWorkoutWindow.Arg("-sologym-capture");
            if (!string.IsNullOrEmpty(path)) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(Path.ChangeExtension(path, ".smoke.json"), JsonUtility.ToJson(result, true) + "\n"); }
            Debug.Log("LOGIN_SMOKE " + JsonUtility.ToJson(result));
        }
        void TextFits(PixelLoginWindow w, string state)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in w.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(text.text) || !text.gameObject.activeInHierarchy || text.transform.parent.GetComponent<InputField>() != null) continue;
                if (text.name.Contains("Editable") || text.name.Contains("Placeholder")) continue;
                Check(text.preferredHeight <= text.rectTransform.rect.height + 2, state + ": live label fits " + text.name + " on " + text.transform.parent.name);
            }
        }
        public IEnumerator KeyboardProbe(PixelLoginWindow w)
        {
            string marker = PixelWorkoutWindow.Arg("-sologym-keyboard-probe");
            int submissions = 0; bool busy = false, passwordSelected = false, shown = false;
            Action<WelcomeViewModel> changed = model => { if (model.IsBusy && !busy) submissions++; busy = model.IsBusy; };
            w.Controller.Changed += changed; w.Email.Input.Select(); w.Email.Input.ActivateInputField();
            yield return null; yield return null;
            File.WriteAllText(marker + ".ready", "ready");
            float deadline = Time.realtimeSinceStartup + 30;
            while (!File.Exists(marker + ".done") && Time.realtimeSinceStartup < deadline)
            {
                passwordSelected |= EventSystem.current.currentSelectedGameObject == w.Password.Input.gameObject;
                shown |= w.Password.PasswordVisible; yield return null;
            }
            w.Controller.Changed -= changed;
            Check(File.Exists(marker + ".done"), "OS probe completed before timeout");
            Check(w.Email.Input.text == "hero+fit@example.com", "Real keyboard preserves plus-address email");
            Check(passwordSelected, "Real Return from email selects password");
            Check(shown, "Real Tab and Return reach visibility toggle");
            Check(submissions == 1, "Real password Return submits exactly once");
            Check(w.Controller.Model.ErrorKey == "unavailable", "Keyboard probe stays on no-network auth adapter");
            Check(w.Password.Input.text == "" && !w.Password.PasswordVisible, "Password is cleared and masked after keyboard submission");
            var report = new Report { passed = Passed, checks = checks, locale = w.Controller.Model.Language, width = Screen.width, height = Screen.height, failures = failures.ToArray() };
            File.WriteAllText(marker + ".report.json", JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("LOGIN_KEYBOARD " + JsonUtility.ToJson(report));
        }
        static bool Click(Selectable target)
        {
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)target.transform;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != target.gameObject)
            {
                Debug.Log("LOGIN_RAYCAST_MISS target=" + target.name + " active=" + target.gameObject.activeInHierarchy + " enabled=" + target.IsInteractable() + " pos=" + pointer.position + " rect=" + rect.rect + " hit=" + (hits.Count == 0 ? "none" : hits[0].gameObject.name));
                return false;
            }
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler); return true;
        }
        static IEnumerator Shot(PixelLoginWindow w, string state)
        {
            string path = PixelWorkoutWindow.Arg("-sologym-capture");
            if (string.IsNullOrEmpty(path)) yield break;
            yield return new WaitForEndOfFrame();
            w.Capture(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-" + state + ".png"));
        }
    }
}
