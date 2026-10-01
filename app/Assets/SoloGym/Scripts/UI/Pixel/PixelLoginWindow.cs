using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Guild entrance with live sign-in controls. Identity never enters fictional Home data.</summary>
    public sealed class PixelLoginWindow : MonoBehaviour
    {
        public WelcomeController Controller { get; private set; }
        public PixelFormField Email { get; private set; }
        public PixelFormField Password { get; private set; }
        public PixelPrimaryButton Submit { get; private set; }
        public Button Google { get; private set; }
        public PixelSecondaryAction Recovery { get; private set; }
        public PixelSecondaryAction CreateAccount { get; private set; }
        public PixelSecondaryAction Privacy { get; private set; }
        public PixelSecondaryAction Terms { get; private set; }
        public PixelSecondaryAction Language { get; private set; }
        public PixelSecondaryAction Cancel { get; private set; }
        public PixelSecondaryAction Return { get; private set; }
        public ScrollRect FormScroll { get; private set; }
        public Text Status { get; private set; }
        public Text NoticeBody { get; private set; }
        public string Page { get; private set; } = "login";
        public WelcomeNavigation LastNavigation { get; private set; }
        public RectTransform Composition { get; private set; }
        public RectTransform Panel { get; private set; }
        public bool AutomaticReview = true;
        public PixelAccountForm Account { get; private set; }
        public PixelOnboardingFlow Onboarding { get; private set; }
        public PixelConsentWindow Consent { get; private set; }
        public PixelPrivateProfileWindow Profile { get; private set; }
        public PixelGoalsWindow Goals { get; private set; }
        string profileReturnPage = "login";
        bool accountFromConsent, identitySetup;
        IAccountRegistrationService registrationService;
        string documentReturnPage = "login";
        RectTransform loginRule;

        RectTransform safe, form, viewport, notice, noticeContent, background;
        Text title, subtitle, googleLabel, accountHint, divider;
        Image panelEdge;
        bool smoke, online = true, initialized, wasBusy;
        string renderedLanguage, renderedError;
        float safeInset, refreshAt, keyboardOverride, previousKeyboard = -1;
        int previousWidth, previousHeight;
        Rect previousSafe;
        const string LanguageKey = "SoloGym.Home.Language.v1";
        string originalLanguage; bool hadLanguage;

        public void Initialize(IWelcomeAuthService service = null, string locale = null, IAccountRegistrationService registration = null)
        {
            if (initialized) return;
            initialized = true;
            registrationService = registration;
            smoke = PixelWorkoutWindow.Has("-sologym-smoke") || PixelWorkoutWindow.Has("-sologym-account-smoke") || PixelWorkoutWindow.Has("-sologym-onboarding-smoke") || PixelWorkoutWindow.Has("-sologym-consent-smoke") || PixelWorkoutWindow.Has("-sologym-profile-smoke") || PixelWorkoutWindow.Has("-sologym-goals-smoke");
            hadLanguage = PlayerPrefs.HasKey(LanguageKey); originalLanguage = PlayerPrefs.GetString(LanguageKey);
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            float.TryParse(PixelWorkoutWindow.Arg("-sologym-safe-inset", "0"), out safeInset);
            safeInset = Mathf.Clamp(safeInset, 0, 150);
            bool isolated = smoke || !string.IsNullOrEmpty(PixelWorkoutWindow.Arg("-sologym-capture")) || !string.IsNullOrEmpty(PixelWorkoutWindow.Arg("-sologym-keyboard-probe"));
            Controller = new WelcomeController(service ?? (isolated ? (IWelcomeAuthService)new UnconfiguredWelcomeAuthService() : new FirebaseWelcomeAuthService()));
            Build();
            Controller.Changed += Render;
            Controller.PasswordCleared += ClearPassword;
            Controller.NavigationRequested += Navigate;
            if (locale == "en" || locale == "es" || locale == "auto") Controller.SetLanguage(locale);
            Controller.OpenEmail();
            if (!smoke) Controller.SetOnline(Application.internetReachability != NetworkReachability.NotReachable);
            Render(Controller.Model); Relayout();
        }

        void Build()
        {
            if (FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Guild entrance camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(15, 24, 32, 255); camera.cullingMask = 0;
            }
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
            var canvasRoot = PixelJournalUI.Rect("Login canvas", transform, new Rect());
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            // Full-bleed architecture and safe-area controls are distinct siblings.
            background = PixelJournalUI.Art(canvasRoot, new Rect(), "Rooms/LoginR1/entrance").rectTransform;
            background.anchorMin = background.anchorMax = background.pivot = new Vector2(.5f, .5f);
            safe = PixelJournalUI.Rect("Device safe area", canvasRoot, new Rect());
            Composition = PixelJournalUI.Rect("1280 x 720 login composition", safe, new Rect(0, 0, 1280, 720));
            Composition.anchorMin = Composition.anchorMax = Composition.pivot = new Vector2(.5f, .5f); Composition.anchoredPosition = Vector2.zero;
            var wordmark = PixelJournalUI.Text(Composition, new Rect(64, 12, 560, 122), "SOLOGYM", 94, false);
            wordmark.fontStyle = FontStyle.Bold;
            wordmark.horizontalOverflow = HorizontalWrapMode.Overflow;
            var shadow = wordmark.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(3, -4); shadow.effectColor = new Color32(13, 20, 28, 255);
            subtitle = PixelJournalUI.Text(Composition, new Rect(66, 117, 540, 36), "", 27, false);
            Panel = PixelJournalUI.Frame(Composition, new Rect(674, 44, 570, 632), "Separate sign-in panel");
            panelEdge = Panel.GetComponentsInChildren<Image>()[1]; PixelJournalUI.Stretch(panelEdge.rectTransform);
            title = PixelJournalUI.Text(Panel, new Rect(26, 20, 518, 60), "", 38, false, TextAnchor.MiddleCenter);
            loginRule = PixelJournalUI.Rule(Panel, 62, 84, 446).rectTransform;
            form = PixelJournalUI.Scroll(Panel, new Rect(28, 100, 514, 516), 520);
            viewport = (RectTransform)form.parent; FormScroll = viewport.GetComponent<ScrollRect>();
            FormScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Status = PixelJournalUI.Text(form, new Rect(0, 0, 502, 64), "", 20, false); Status.color = new Color32(255, 194, 158, 255);
            Email = PixelFormField.Create(form, PixelFormField.Kind.Email, "", "");
            Password = PixelFormField.Create(form, PixelFormField.Kind.Password, "", "********");
            foreach (var field in new[] { Email, Password })
            {
                field.Background.pixelsPerUnitMultiplier = 2;
                field.Label.fontSize = 23; field.SetLabelColor(PixelJournalUI.Ivory);
                field.Input.textComponent.fontSize = 24;
                field.Input.characterLimit = field == Email ? 254 : 4096;
            }
            Password.Visibility.Background.pixelsPerUnitMultiplier = 2;
            Password.Visibility.SetHorizontalPadding(8);
            Email.Input.onValueChanged.AddListener(value => Controller.SetEmail(value));
            Password.Input.onValueChanged.AddListener(value => Controller.SetPassword(value));
            // Submit only from a keyboard Return/Done event, never on ordinary focus loss.
            Email.Input.onSubmit.AddListener(_ => SelectPassword());
            Password.Input.onSubmit.AddListener(_ => SubmitEmail());
            Recovery = TextAction(form, "", () => Controller.ForgotPassword());
            Recovery.name = "Password recovery";
            Submit = PixelPrimaryButton.Create(form, "", SubmitEmail); Submit.SetFontSize(32); Submit.Background.pixelsPerUnitMultiplier = 2;
            divider = PixelJournalUI.Text(form, new Rect(), "o", 20, false, TextAnchor.MiddleCenter);
            Google = ProviderButton(form);
            accountHint = PixelJournalUI.Text(form, new Rect(), "", 22, false, TextAnchor.MiddleRight);
            CreateAccount = TextAction(form, "", () => Controller.CreateAccount());
            Cancel = TextAction(form, "", () => { Controller.CancelAuthentication(); Submit.Select(); });
            Cancel.gameObject.SetActive(false);
            Privacy = TextAction(Composition, "", () => Controller.OpenPrivacy());
            Terms = TextAction(Composition, "", () => Controller.OpenTerms());
            Language = TextAction(Composition, "ES / EN", () => Controller.ToggleLanguage());
            Place(Privacy, new Rect(28, 668, 148, 52)); Place(Terms, new Rect(182, 668, 136, 52)); Place(Language, new Rect(1098, 668, 152, 52));
            Link(Email.Input, Language, Password.Input);
            Email.SetTraversal(Language, Password.Input); Password.SetTraversal(Email.Input, Recovery);
            Link(Password.Visibility, Password.Input, Recovery); Link(Recovery, Password.Visibility, Submit);
            Link(Submit, Recovery, Google); Link(Google, Submit, CreateAccount);
            Link(CreateAccount, Google, Privacy); Link(Privacy, CreateAccount, Terms); Link(Terms, Privacy, Language); Link(Language, Terms, Email.Input);
            Link(Cancel, Language, Language);
            notice = PixelJournalUI.Rect("Account destination", Panel, new Rect(28, 100, 514, 516));
            noticeContent = PixelJournalUI.Scroll(notice, new Rect(0, 0, 514, 430), 430);
            NoticeBody = PixelJournalUI.Text(noticeContent, new Rect(0, 0, 494, 420), "", 24, false, TextAnchor.UpperLeft);
            Return = TextAction(notice, "", BackToLogin); Place(Return, new Rect(0, 446, 502, 64));
            Link(Return, Language, Privacy); notice.gameObject.SetActive(false);
            Account = new GameObject("Email account form", typeof(RectTransform)).AddComponent<PixelAccountForm>();
            Account.Initialize(Panel, registrationService, BackToLogin, Privacy, Language, AccountBack);
            Account.StateChanged += AccountStateChanged;
            Account.gameObject.SetActive(false);
            Onboarding = new GameObject("Guild onboarding", typeof(RectTransform)).AddComponent<PixelOnboardingFlow>();
            Onboarding.Initialize(Panel, Composition, BackToLogin, Privacy, Language);
            Onboarding.StateChanged += OnboardingStateChanged;
            Onboarding.gameObject.SetActive(false);
            Consent = new GameObject("Guild consent and documents", typeof(RectTransform)).AddComponent<PixelConsentWindow>();
            Consent.Initialize(Panel, BackFromConsent, ContinueFromConsent,
                id => { if (id == "privacy") Controller.OpenPrivacy(); else Controller.OpenTerms(); }, BackToLogin, Privacy, Language);
            Consent.StateChanged += ConsentStateChanged;
            Onboarding.ConsentRequested += OpenConsent;
            Onboarding.Controller.EligibilityChanged += Consent.Reset;
            Consent.gameObject.SetActive(false);
            Profile = new GameObject("Private profile review",typeof(RectTransform)).AddComponent<PixelPrivateProfileWindow>();
            Profile.Initialize(Panel,BackFromProfile,ExitProfile,Privacy,Language,true);
            Profile.StateChanged += ProfileStateChanged;
            Onboarding.Controller.EligibilityChanged += Profile.Reset;
            Account.ProfilePreviewRequested += OpenProfileReview;
            Profile.gameObject.SetActive(false);
            Goals = new GameObject("Goals and experience review",typeof(RectTransform)).AddComponent<PixelGoalsWindow>();
            Goals.Initialize(Panel,BackFromGoals,ExitProfile,CanReviewGoals,Privacy,Language);
            Goals.StateChanged += GoalsStateChanged;
            Profile.GoalsRequested += OpenGoalsReview;
            Onboarding.Controller.EligibilityChanged += Goals.Reset;
            Goals.gameObject.SetActive(false);
        }

        Button ProviderButton(Transform parent)
        {
            var rect = PixelJournalUI.Rect("Google sign-in", parent, new Rect(0, 0, 502, 64));
            var surface = rect.gameObject.AddComponent<WelcomeProviderBackground>(); surface.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = surface;
            var colors = button.colors; colors.highlightedColor = new Color32(230, 241, 247, 255); colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(203, 222, 234, 255); colors.disabledColor = new Color32(143, 151, 158, 255); button.colors = colors;
            var logo = PixelJournalUI.Rect("Original Google G", rect, new Rect(42, 16, 32, 32)).gameObject.AddComponent<RawImage>();
            logo.texture = Resources.Load<Texture2D>("Welcome/GoogleG"); logo.raycastTarget = false;
            googleLabel = PixelJournalUI.Text(rect, new Rect(84, 0, 398, 64), "", 24, true, TextAnchor.MiddleCenter);
            googleLabel.font = Resources.Load<Font>("Fonts/GoogleSans-Medium"); googleLabel.color = new Color32(31, 31, 31, 255);
            button.onClick.AddListener(SubmitGoogle); return button;
        }

        void AccountStateChanged()
        {
            if (Account == null || Page != "account") return;
            Privacy.interactable = Terms.interactable = !Account.Controller.Busy;
            Link(Privacy, Account.PreviewProfile.gameObject.activeSelf ? Account.PreviewProfile : Account.SignIn, Terms);
            Link(Language, Account.Controller.Busy ? Account.Cancel : Terms,
                Account.Controller.Busy ? Account.Cancel : Account.Back);
        }

        void OnboardingStateChanged()
        {
            if (Page != "onboarding" || Onboarding == null) return;
            Privacy.interactable = Terms.interactable = Language.interactable = !Onboarding.CountryOpen;
            Link(Privacy, Onboarding.LastControl, Terms); Link(Language, Terms, Onboarding.Back);
        }

        void ConsentStateChanged()
        {
            if (Consent == null || (Page != "consent" && Page != "privacy" && Page != "terms")) return;
            if(Page == "consent" && Profile != null && !Consent.Controller.CanContinue) { Profile.Reset(); Goals?.Reset(); }
            Link(Privacy, Consent.LastControl, Terms); Link(Language, Terms, Consent.Back);
        }
        void ProfileStateChanged()
        {
            if(Page != "profile" || Profile == null) return;
            Link(Privacy,Profile.LastControl,Terms); Link(Language,Terms,Profile.Back);
        }
        void GoalsStateChanged()
        {
            if(Page != "goals" || Goals == null) return;
            Link(Privacy,Goals.LastControl,Terms); Link(Language,Terms,Goals.Back);
        }
        string DocumentReturnLabel => documentReturnPage == "goals" ? L("BACK TO MY CHOICES","VOLVER A MIS ELECCIONES") : documentReturnPage == "profile" ? L("BACK TO MY PROFILE", "VOLVER A MI PERFIL")
            : documentReturnPage == "consent" ? L("BACK TO MY CHOICES", "VOLVER A MIS DECISIONES")
            : documentReturnPage == "account" ? L("BACK TO REGISTRATION", "VOLVER AL REGISTRO")
            : documentReturnPage == "onboarding" ? L("BACK TO SETUP", "VOLVER A LA CONFIGURACIÓN")
            : L("BACK TO SIGN IN", "VOLVER AL INICIO");

        void Render(WelcomeViewModel model)
        {
            bool languageChanged = renderedLanguage != model.Language; renderedLanguage = model.Language;
            subtitle.text = L("TRAINING GUILD", "GREMIO DE ENTRENAMIENTO");
            Email.SetLocalizedText(model.Copy("email_label"), L("you@email.com", "tu@correo.com"), "", "", "");
            Password.SetLocalizedText(model.Copy("password_label"), "********", "", L("Show", "Mostrar"), L("Hide", "Ocultar"));
            if (Email.Input.text != model.Email) Email.SetValueWithoutNotify(model.Email);
            Email.SetInteractable(!model.IsBusy); Password.SetInteractable(!model.IsBusy);
            Submit.SetLabel(L("SIGN IN", "ENTRAR")); Submit.SetLoading(model.IsBusy, model.Copy("signing_in")); Submit.interactable = model.CanSubmit || model.IsBusy;
            Google.interactable = model.CanSubmit; googleLabel.text = model.Copy("google");
            Recovery.SetLabel(model.Copy("forgot")); Recovery.interactable = !model.IsBusy;
            CreateAccount.SetLabel(L("Create account", "Crear cuenta")); CreateAccount.interactable = !model.IsBusy;
            accountHint.text = L("No account yet?", "¿No tienes cuenta?");
            Privacy.SetLabel(model.Copy("privacy")); Terms.SetLabel(model.Copy("terms"));
            Privacy.interactable = Terms.interactable = !model.IsBusy;
            Language.SetLabel(model.Language == "es" ? "ES / EN" : "EN / ES");
            Cancel.SetLabel(model.Copy("cancel")); Return.SetLabel(documentReturnPage == "onboarding" ? L("Back to setup", "Volver a la configuración") : documentReturnPage == "account" ? L("Back to registration", "Volver al registro") : L("Back to sign in", "Volver al inicio"));
            Cancel.gameObject.SetActive(model.IsBusy && Page == "login");
            Recovery.gameObject.SetActive(!model.IsBusy);
            Status.text = model.ErrorText;
            // Field messages remain inline; provider errors are generic and do not expose account existence.
            Email.SetError(model.ErrorKey == "required_email" || model.ErrorKey == "invalid_email" ? model.ErrorText : "");
            Password.SetError(model.ErrorKey == "required_password" ? model.ErrorText : "");
            if (Email.Error.Length > 0 || Password.Error.Length > 0) Status.text = "";
            Status.gameObject.SetActive(Status.text.Length > 0);
            if (model.IsBusy && !wasBusy) { ReleaseKeyboard(); Cancel.Select(); }
            if (!model.IsBusy && wasBusy && Page == "login") Submit.Select();
            wasBusy = model.IsBusy;
            if (Page == "login") title.text = model.Copy("email_title");
            else if (Page != "account" && Page != "onboarding" && Page != "consent" && Page != "privacy" && Page != "terms" && Page != "profile" && Page != "goals" && languageChanged) PopulateNotice();
            if (Account != null)
            {
                Account.SetLocale(model.Language); Account.Controller.SetOnline(!model.IsOffline);
                AccountStateChanged();
            }
            if (Onboarding != null) { Onboarding.SetLocale(model.Language); OnboardingStateChanged(); }
            if (Consent != null) { Consent.SetLocale(model.Language, DocumentReturnLabel); ConsentStateChanged(); }
            if (Profile != null) { Profile.SetLocale(model.Language); ProfileStateChanged(); }
            if (Goals != null) { Goals.SetLocale(model.Language); GoalsStateChanged(); }
            LayoutForm();
            if (renderedError != model.ErrorKey) { renderedError = model.ErrorKey; FormScroll.verticalNormalizedPosition = 1; }
        }

        void LayoutForm()
        {
            float y = 0;
            if (Status.text.Length > 0)
            {
                Status.rectTransform.sizeDelta = new Vector2(502, 64);
                float height = Mathf.Max(48, Status.preferredHeight + 12);
                PixelJournalUI.Place(Status.rectTransform, new Rect(0, y, 502, height)); y += height + 8;
            }
            Place(Email, new Rect(0, y, 502, Email.PreferredHeight)); y += Email.PreferredHeight + 8;
            Place(Password, new Rect(0, y, 502, Password.PreferredHeight)); y += Password.PreferredHeight;
            Place(Recovery, new Rect(0, y, 502, 52)); Place(Cancel, new Rect(0, y, 502, 52)); y += 54;
            Place(Submit, new Rect(0, y, 502, 64)); y += 66;
            PixelJournalUI.Place(divider.rectTransform, new Rect(0, y, 502, 24)); y += 26;
            Place(Google, new Rect(0, y, 502, 64)); y += 68;
            PixelJournalUI.Place(accountHint.rectTransform, new Rect(0, y, 248, 64));
            Place(CreateAccount, new Rect(250, y, 252, 64)); y += 64;
            form.sizeDelta = new Vector2(502, Mathf.Max(viewport.rect.height, y));
        }

        public void SubmitEmail()
        {
            if (Page != "login" || !Controller.Model.CanSubmit) return;
            Controller.SetEmail(Email.Input.text); Controller.SetPassword(Password.Input.text);
            ReleaseKeyboard(); _ = Controller.SignInEmailAsync();
        }
        public void SubmitGoogle()
        {
            if (Page != "login" || !Controller.Model.CanSubmit) return;
            ReleaseKeyboard(); _ = Controller.SignInGoogleAsync();
        }
        void SelectPassword() { if (Page == "login" && !Controller.Model.IsBusy) { Password.Input.Select(); Password.Input.ActivateInputField(); } }
        void ClearPassword() { if (Password == null) return; Password.SetValueWithoutNotify(""); Password.HidePassword(); }
        void ReleaseKeyboard()
        {
            Email.Input.DeactivateInputField(); Password.Input.DeactivateInputField();
            EventSystem.current?.SetSelectedGameObject(null);
        }
        public void OpenAccount(bool fromConsent = false)
        {
            if (fromConsent && (Onboarding.Controller.Step != GuildSetupStep.Consent || !Consent.Controller.CanContinue)) return;
            accountFromConsent = fromConsent; Goals.gameObject.SetActive(false); Profile.gameObject.SetActive(false);
            Consent.gameObject.SetActive(false);
            Controller.CancelAuthentication(); ReleaseKeyboard();
            Onboarding.gameObject.SetActive(false);
            Page = "account"; documentReturnPage = "login";
            viewport.gameObject.SetActive(false); notice.gameObject.SetActive(false);
            title.gameObject.SetActive(false); loginRule.gameObject.SetActive(false);
            Account.gameObject.SetActive(true); Account.Open(Controller.Model.Email, Controller.Model.Language, fromConsent);
            Link(Privacy, Account.PreviewProfile.gameObject.activeSelf ? Account.PreviewProfile : Account.SignIn, Terms); Link(Language, Terms, Account.Back);
            Relayout(); Account.Back.Select();
        }
        public void OpenOnboarding(bool reset = true, bool verifiedIdentity = false)
        {
            if (reset) { identitySetup = verifiedIdentity; accountFromConsent = false; Consent.Reset(); Profile.Reset(); Goals.Reset(); }
            Goals.gameObject.SetActive(false); Profile.gameObject.SetActive(false);
            Consent.gameObject.SetActive(false);
            Controller.CancelAuthentication(); ReleaseKeyboard();
            Page = "onboarding"; documentReturnPage = "login";
            Account.gameObject.SetActive(false); viewport.gameObject.SetActive(false); notice.gameObject.SetActive(false);
            title.gameObject.SetActive(false); loginRule.gameObject.SetActive(false);
            Onboarding.gameObject.SetActive(true); Onboarding.Open(Controller.Model.Language, reset);
            OnboardingStateChanged(); Relayout(); Onboarding.Back.Select();
        }
        public void OpenConsent()
        {
            if (Onboarding.Controller.Step != GuildSetupStep.Consent) return;
            Goals.gameObject.SetActive(false); Profile.gameObject.SetActive(false); ReleaseKeyboard(); Page = "consent";
            viewport.gameObject.SetActive(false); notice.gameObject.SetActive(false); title.gameObject.SetActive(false); loginRule.gameObject.SetActive(false);
            Account.gameObject.SetActive(false); Onboarding.gameObject.SetActive(false);
            Consent.BeforeAccount = !identitySetup; Consent.gameObject.SetActive(true); Consent.OpenDecisions(Controller.Model.Language);
            Privacy.interactable = Terms.interactable = Language.interactable = true;
            ConsentStateChanged(); Relayout(); Consent.Back.Select();
        }
        void BackFromConsent()
        {
            Onboarding.Controller.Back(); OpenOnboarding(false);
        }
        void AccountBack()
        {
            if (accountFromConsent) { Controller.SetEmail(Account.Email.Input.text); OpenConsent(); }
            else BackToLogin();
        }
        void ContinueFromConsent()
        {
            if (Page != "consent" || !Consent.Controller.CanContinue) return;
            if (identitySetup) OpenProfileReview(); else OpenAccount(true);
        }
        public void OpenProfileReview()
        {
            if(!Consent.Controller.CanContinue || Onboarding.Controller.Step!=GuildSetupStep.Consent) return;
            if(!((Page=="consent" && identitySetup) || (Page=="account" && accountFromConsent))) return;
            profileReturnPage=Page; ShowProfile();
        }
        void ShowProfile()
        {
            ReleaseKeyboard(); Account.ClearSecrets(); Goals.gameObject.SetActive(false);
            Profile.SetGoalsAvailable(HasReviewedSetup());
            Account.gameObject.SetActive(false); Onboarding.gameObject.SetActive(false); Consent.gameObject.SetActive(false);
            viewport.gameObject.SetActive(false); notice.gameObject.SetActive(false); title.gameObject.SetActive(false); loginRule.gameObject.SetActive(false);
            Page="profile"; Profile.gameObject.SetActive(true); Profile.Open(Controller.Model.Language);
            Privacy.interactable=Terms.interactable=Language.interactable=true;
            ProfileStateChanged(); Relayout(); Profile.Back.Select();
        }
        bool HasReviewedSetup() => Onboarding.Controller.Step==GuildSetupStep.Consent && Onboarding.Controller.TryGetReviewAudience(out _)
            && Consent.Controller.CanContinue && (identitySetup || accountFromConsent);
        bool CanReviewGoals() => HasReviewedSetup() && Profile.Controller.Model.ReviewMode
            && Profile.Controller.Model.Step==ProfileStep.Checkpoint
            && (Profile.Controller.Model.Readiness=="ready" || Profile.Controller.Model.Readiness=="low_energy");
        public void OpenGoalsReview() { if(Page=="profile") ShowGoals(); }
        void ShowGoals()
        {
            if(!CanReviewGoals() || !Onboarding.Controller.TryGetReviewAudience(out bool teen)) return;
            ReleaseKeyboard(); Profile.ReleaseKeyboard(); Account.ClearSecrets();
            Account.gameObject.SetActive(false); Onboarding.gameObject.SetActive(false); Consent.gameObject.SetActive(false); Profile.gameObject.SetActive(false);
            viewport.gameObject.SetActive(false); notice.gameObject.SetActive(false); title.gameObject.SetActive(false); loginRule.gameObject.SetActive(false);
            Page="goals"; Goals.gameObject.SetActive(true); Goals.Open(Controller.Model.Language,teen);
            Privacy.interactable=Terms.interactable=Language.interactable=true;
            GoalsStateChanged(); Relayout(); Goals.Back.Select();
        }
        void BackFromGoals()
        {
            Profile.Controller.Back(); ShowProfile();
        }
        void BackFromProfile()
        {
            if(profileReturnPage=="consent") OpenConsent();
            else if(profileReturnPage=="account") RestoreAccount();
            else ExitProfile();
        }
        void ExitProfile() { Page="profile"; BackToLogin(); }
        void RestoreAccount()
        {
            Goals.gameObject.SetActive(false); Profile.gameObject.SetActive(false); Consent.gameObject.SetActive(false);
            Page="account"; notice.gameObject.SetActive(false); title.gameObject.SetActive(false); loginRule.gameObject.SetActive(false);
            Account.gameObject.SetActive(true); Account.SetLocale(Controller.Model.Language);
            Link(Privacy,Account.PreviewProfile.gameObject.activeSelf ? Account.PreviewProfile : Account.SignIn,Terms); AccountStateChanged();
            Relayout(); Account.Back.Select();
        }
        void Navigate(WelcomeNavigation destination)
        {
            if (destination.Context == "create_email_account" || (destination.AuthorizedByBackend && destination.WindowId == "WIN-006"))
            { LastNavigation = destination; OpenOnboarding(true, destination.AuthorizedByBackend); return; }
            if (Page != "privacy" && Page != "terms") documentReturnPage = Page;
            Onboarding.gameObject.SetActive(false); Account.gameObject.SetActive(false); Profile.gameObject.SetActive(false); Goals.gameObject.SetActive(false);
            LastNavigation = destination; ReleaseKeyboard();
            Page = destination.AuthorizedByBackend ? "checkpoint" : destination.Context == "privacy" || destination.Context == "terms" ? destination.Context : destination.WindowId == "WIN-005" ? "recovery" : "create";
            viewport.gameObject.SetActive(false);
            bool reading = Page == "privacy" || Page == "terms";
            notice.gameObject.SetActive(!reading); title.gameObject.SetActive(!reading); loginRule.gameObject.SetActive(!reading);
            Consent.gameObject.SetActive(reading);
            if (reading)
            {
                Consent.OpenDocument(Page, Controller.Model.Language, DocumentReturnLabel);
                ConsentStateChanged(); Consent.Back.Select();
            }
            else { Link(Privacy, Return, Terms); Link(Language, Terms, Return); PopulateNotice(); Return.Select(); }
            Relayout();
        }
        void PopulateNotice()
        {
            Return.SetLabel(documentReturnPage == "onboarding" ? L("Back to setup", "Volver a la configuración") : documentReturnPage == "account" ? L("Back to registration", "Volver al registro") : L("Back to sign in", "Volver al inicio"));
            if (Page == "checkpoint")
            {
                title.text = L("IDENTITY VERIFIED", "IDENTIDAD VERIFICADA");
                NoticeBody.text = L("You signed in. Age, consent and profile setup are still required.\n\nThe connected setup flow is not available in this build yet. No workout or sample profile has been opened.", "Iniciaste sesión. Aún falta completar edad, consentimiento y perfil.\n\nLa configuración conectada aún no está disponible en esta versión. No se abrió ninguna rutina ni perfil de ejemplo.");
            }
            else
            {
                bool recovery = Page == "recovery";
                title.text = recovery ? L("RECOVER ACCESS", "RECUPERAR ACCESO") : L("CREATE ACCOUNT", "CREAR CUENTA");
                NoticeBody.text = recovery
                    ? L("Password recovery is not connected in this build yet.\n\nNo email has been sent. Return to sign in to try again.", "La recuperación de contraseña aún no está conectada en esta versión.\n\nNo se ha enviado ningún correo. Vuelve al inicio para intentarlo de nuevo.")
                    : L("Account creation is the next step being built.\n\nNo account has been created. Age, consent and fitness setup will be required before entering your personal gym.", "La creación de cuenta es el siguiente paso en desarrollo.\n\nNo se ha creado ninguna cuenta. Antes de entrar a tu gimnasio será necesario completar edad, consentimiento y perfil físico.");
            }
            float h = Mathf.Max(420, NoticeBody.preferredHeight + 12);
            NoticeBody.rectTransform.sizeDelta = new Vector2(494, h); noticeContent.sizeDelta = new Vector2(502, h);
        }
        public void BackToLogin()
        {
            bool document = Page == "privacy" || Page == "terms";
            if (document && documentReturnPage == "consent") { OpenConsent(); return; }
            if (document && documentReturnPage == "goals" && CanReviewGoals()) { ShowGoals(); return; }
            if (document && documentReturnPage == "profile") { ShowProfile(); return; }
            Consent.gameObject.SetActive(false);
            if ((Page == "privacy" || Page == "terms") && documentReturnPage == "onboarding")
            { OpenOnboarding(false); return; }
            if ((Page == "privacy" || Page == "terms") && documentReturnPage == "account")
            {
                RestoreAccount(); return;
            }
            if (Page == "account") Controller.SetEmail(Account.Email.Input.text);
            Goals.gameObject.SetActive(false); Goals.Reset();
            Profile.gameObject.SetActive(false); Profile.Reset(); profileReturnPage="login";
            Page = "login"; accountFromConsent = identitySetup = false; Consent.Reset(); Onboarding.Controller.Discard(); Onboarding.gameObject.SetActive(false);
            Privacy.interactable = Terms.interactable = Language.interactable = true; Account.gameObject.SetActive(false);
            title.gameObject.SetActive(true); loginRule.gameObject.SetActive(true);
            Link(Privacy, CreateAccount, Terms); Link(Language, Terms, Email.Input);
            documentReturnPage = "login";
            notice.gameObject.SetActive(false); viewport.gameObject.SetActive(true);
            Controller.OpenEmail(); if (Controller.Model.IsOffline) Controller.SetOnline(false);
            Relayout(); FormScroll.StopMovement(); FormScroll.verticalNormalizedPosition = 1; form.anchoredPosition = Vector2.zero; Email.Input.Select();
        }

        void Update()
        {
            if (Controller == null) return;
            float keyboard = KeyboardPixels;
            if (previousWidth != Screen.width || previousHeight != Screen.height || previousSafe != Screen.safeArea || Math.Abs(previousKeyboard - keyboard) > .5f) Relayout();
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 1;
                bool nowOnline = Application.internetReachability != NetworkReachability.NotReachable;
                if (!smoke && online != nowOnline) { online = nowOnline; Controller.SetOnline(online); }
                else Controller.Refresh();
                Account.Controller.Refresh();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Page == "account" && Account.Controller.Busy) Account.Controller.Cancel();
                else if (Page == "account") AccountBack();
                else if (Page == "goals") Goals.GoBack();
                else if (Page == "profile") Profile.GoBack();
                else if (Page == "consent") BackFromConsent();
                else if (Page == "onboarding") Onboarding.GoBack();
                else if (Page != "login") BackToLogin(); else if (Controller.Model.IsBusy) Controller.CancelAuthentication(); else { ReleaseKeyboard(); Password.HidePassword(); }
            }
        }
        float KeyboardPixels => keyboardOverride > 0 ? keyboardOverride : TouchScreenKeyboard.visible ? Mathf.Max(TouchScreenKeyboard.area.height, Screen.height * .4f) : 0;
        internal void ReviewKeyboard(float height) { keyboardOverride = height; Relayout(); }
        public void Relayout()
        {
            if (Composition == null) return;
            previousWidth = Screen.width; previousHeight = Screen.height; previousSafe = Screen.safeArea; previousKeyboard = KeyboardPixels;
            var s = Screen.safeArea;
            var min = new Vector2(Mathf.Max(s.xMin, safeInset), Mathf.Max(s.yMin, safeInset));
            var max = new Vector2(Mathf.Min(s.xMax, Screen.width - safeInset), Mathf.Min(s.yMax, Screen.height - safeInset));
            safe.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height); safe.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
            float scale = Mathf.Max(.01f, Mathf.Min((max.x-min.x) / 1280, (max.y-min.y) / 720));
            Composition.localScale = new Vector3(scale, scale, 1);
            float cover = Mathf.Max(Screen.width / 640f, Screen.height / 360f); background.sizeDelta = new Vector2(640 * cover, 360 * cover);
            Canvas.ForceUpdateCanvases();
            // Keep readable type at its normal scale; shorten/scroll the form above the keyboard instead of shrinking the whole screen.
            float logicalBottom = 676;
            if (previousKeyboard > 0)
            {
                var corners = new Vector3[4]; Composition.GetWorldCorners(corners);
                logicalBottom = Mathf.Clamp((corners[1].y - previousKeyboard - 12) / scale, 290, 676);
            }
            Panel.sizeDelta = new Vector2(570, logicalBottom - 44);
            viewport.sizeDelta = new Vector2(514, logicalBottom - 160);
            notice.sizeDelta = viewport.sizeDelta;
            ((RectTransform)noticeContent.parent).sizeDelta = new Vector2(514, Mathf.Max(70, notice.rect.height - 82));
            Place(Return, new Rect(0, notice.rect.height - 70, 502, 64));
            Privacy.gameObject.SetActive(previousKeyboard <= 0); Terms.gameObject.SetActive(previousKeyboard <= 0); Language.gameObject.SetActive(previousKeyboard <= 0);
            LayoutForm(); Account.Relayout(Panel.rect.height, previousKeyboard > 0); Onboarding.Relayout(Panel.rect.height, previousKeyboard > 0); Consent.Relayout(Panel.rect.height); Profile.Relayout(Panel.rect.height,previousKeyboard>0); Goals.Relayout(Panel.rect.height); Canvas.ForceUpdateCanvases();
            if (previousKeyboard > 0)
            {
                var selected = EventSystem.current?.currentSelectedGameObject;
                if (selected != null && selected.transform.IsChildOf(form))
                {
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
                    float shift = bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y : bounds.max.y > viewport.rect.yMax ? viewport.rect.yMax - bounds.max.y : 0;
                    var pos = form.anchoredPosition; pos.y = Mathf.Clamp(pos.y + shift, 0, Mathf.Max(0, form.rect.height - viewport.rect.height)); form.anchoredPosition = pos;
                }
            }
        }

        public void Backgrounded() { if (Controller == null) return; Controller.OnBackground(); Account?.Backgrounded(); Onboarding?.Backgrounded(); Profile?.ReleaseKeyboard(); ReleaseKeyboard(); }
        void OnApplicationPause(bool paused) { if (paused) Backgrounded(); }
        void OnApplicationFocus(bool focused) { if (!focused) Backgrounded(); }
        void OnDisable() { if (Controller != null) Controller.CancelAuthentication(); Account?.Backgrounded(); Onboarding?.Backgrounded(); }
        void OnDestroy()
        {
            Controller?.Dispose();
            if (smoke) { if (hadLanguage) PlayerPrefs.SetString(LanguageKey, originalLanguage); else PlayerPrefs.DeleteKey(LanguageKey); PlayerPrefs.Save(); }
        }
        IEnumerator Start()
        {
            var testService = PixelWorkoutWindow.Has("-sologym-smoke") ? new PixelLoginSmoke.Service() : null;
            bool goalsReview = PixelWorkoutWindow.Has("-sologym-goals-smoke");
            bool profileReview = PixelWorkoutWindow.Has("-sologym-profile-smoke");
            bool consentReview = PixelWorkoutWindow.Has("-sologym-consent-smoke");
            bool onboardingReview = PixelWorkoutWindow.Has("-sologym-onboarding-smoke");
            bool accountReview = PixelWorkoutWindow.Has("-sologym-account-smoke");
            var accountService = accountReview ? new PixelAccountSmoke.Service() : null;
            if (!initialized) Initialize(testService, PixelWorkoutWindow.Arg("-sologym-locale"), accountService);
            if (PixelWorkoutWindow.Arg("-sologym-window") == "account") OpenAccount();
            if (PixelWorkoutWindow.Arg("-sologym-window") == "onboarding") OpenOnboarding();
            if (PixelWorkoutWindow.Arg("-sologym-window") == "profile" && PixelWorkoutWindow.Has("-sologym-review")) { profileReturnPage="login"; ShowProfile(); }
            if (PixelWorkoutWindow.Arg("-sologym-window")=="goals" && PixelWorkoutWindow.Has("-sologym-review")) PixelGoalsSmoke.Enter(this,PixelWorkoutWindow.Arg("-sologym-review-age"),true);
            if (!AutomaticReview) yield break;
            for (int i=0; i<8; i++) yield return null;
            int exitCode = 0;
            if (goalsReview) { var checks=gameObject.AddComponent<PixelGoalsSmoke>(); yield return checks.Run(this); exitCode=checks.Passed?0:2; }
            else if (profileReview) { var checks = gameObject.AddComponent<PixelProfileSmoke>(); yield return checks.Run(this); exitCode=checks.Passed?0:2; }
            else if (consentReview) { var checks = gameObject.AddComponent<PixelConsentSmoke>(); yield return checks.Run(this); exitCode = checks.Passed ? 0 : 2; }
            else if (onboardingReview) { var checks = gameObject.AddComponent<PixelOnboardingSmoke>(); yield return checks.Run(this); exitCode = checks.Passed ? 0 : 2; }
            else if (accountReview) { var checks = gameObject.AddComponent<PixelAccountSmoke>(); yield return checks.Run(this, accountService); exitCode = checks.Passed ? 0 : 2; }
            else if (smoke) { var checks = gameObject.AddComponent<PixelLoginSmoke>(); yield return checks.Run(this, testService); exitCode = checks.Passed ? 0 : 2; }
            else if (!string.IsNullOrEmpty(PixelWorkoutWindow.Arg("-sologym-keyboard-probe")))
            {
                if (PixelWorkoutWindow.Has("-sologym-goals-keyboard")) { var checks=gameObject.AddComponent<PixelGoalsSmoke>(); yield return checks.KeyboardProbe(this); exitCode=checks.Passed?0:2; }
                else if (PixelWorkoutWindow.Has("-sologym-profile-keyboard")) { var checks=gameObject.AddComponent<PixelProfileSmoke>(); yield return checks.KeyboardProbe(this); exitCode=checks.Passed?0:2; }
                else if (PixelWorkoutWindow.Has("-sologym-consent-keyboard")) { var checks = gameObject.AddComponent<PixelConsentSmoke>(); yield return checks.KeyboardProbe(this); exitCode = checks.Passed ? 0 : 2; }
                else if (Page == "onboarding") { var checks = gameObject.AddComponent<PixelOnboardingSmoke>(); yield return checks.KeyboardProbe(this); exitCode = checks.Passed ? 0 : 2; }
                else if (Page == "account") { var checks = gameObject.AddComponent<PixelAccountSmoke>(); yield return checks.KeyboardProbe(this); exitCode = checks.Passed ? 0 : 2; }
                else { var checks = gameObject.AddComponent<PixelLoginSmoke>(); yield return checks.KeyboardProbe(this); exitCode = checks.Passed ? 0 : 2; }
            }
            string path = PixelWorkoutWindow.Arg("-sologym-capture");
            if (!string.IsNullOrEmpty(path)) { yield return new WaitForEndOfFrame(); Capture(path); }
            if ((smoke || !string.IsNullOrEmpty(path)) && !PixelWorkoutWindow.Has("-sologym-stay-open")) Application.Quit(exitCode);
        }
        public void Capture(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(path, texture.EncodeToPNG()); Destroy(texture);
        }
        string L(string en, string es) => Controller.Model.Language == "es" ? es : en;
        static void Place(Component c, Rect rect) => PixelJournalUI.Place((RectTransform)c.transform, rect);
        static PixelSecondaryAction TextAction(Transform p, string label, UnityEngine.Events.UnityAction action)
        {
            var b = PixelSecondaryAction.Create(p, PixelSecondaryAction.Appearance.Text, label, action);
            b.Label.fontSize = 22; b.SetHorizontalPadding(8);
            b.Background.pixelsPerUnitMultiplier = 2; return b;
        }
        static void Link(Selectable current, Selectable previous, Selectable next)
        {
            var tab = current.GetComponent<PixelFieldTabNavigation>() ?? current.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous = previous; tab.Next = next;
            if (!(current is InputField)) current.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous, selectOnDown = next };
        }
    }
}
