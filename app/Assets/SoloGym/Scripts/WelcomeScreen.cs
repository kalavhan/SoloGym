using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Approved Welcome composition with native localized controls and credential input.</summary>
    public sealed class WelcomeScreen : MonoBehaviour
    {
        const float Width = 853, Height = 1844;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        readonly Dictionary<string, Element> map = new Dictionary<string, Element>();
        readonly Dictionary<string, Text> labels = new Dictionary<string, Text>();
        readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        Texture2D googleLogo;
        Font serif, bold, body, googleFont;
        RectTransform root, welcomeGroup, emailGroup, modal;
        WelcomeController controller;
        InputField emailInput, passwordInput;
        Text mainStatus, emailStatus, emailTitle, emailLabel, passwordLabel, submitLabel,
            forgotLabel, showPasswordLabel, backLabel, cancelLabel;
        Button submitButton, showPasswordButton, forgotButton, backButton, cancelButton;
        Rect lastSafe;
        Vector2 lastSize;
        bool reviewMode, allowPreview, lastKeyboard, lastOnline = true, switching;
        OnboardingScreen onboarding;
        float lastKeyboardHeight, nextRefresh;
        string requestedCapture;

        [Serializable] sealed class PixelMap { public Element[] elements; }
        [Serializable] sealed class Element
        {
            public string id, kind;
            public Box rect, glyph_bounds;
            public Locales locale_overrides;
        }
        [Serializable] sealed class Box
        {
            public float x, y, width, height;
            public Rect Rect => new Rect(x, y, width, height);
        }
        [Serializable] sealed class Locales { public LocaleBox en; }
        [Serializable] sealed class LocaleBox { public Box rect, glyph_bounds; }
        [Serializable] sealed class PreviewConfig { public bool enabled; public string projectId, apiKey; }

        void Awake()
        {
            if (Argument("-sologym-window") == "home")
            {
                SceneManager.LoadScene("SystemHome");
                return;
            }
            if(Argument("-sologym-window")=="avatar")
            {gameObject.AddComponent<AvatarProofScreen>().Initialize();enabled=false;return;}
            if(Argument("-sologym-window")=="components")
            {gameObject.AddComponent<SystemGallery>().Initialize();enabled=false;return;}
            if (Argument("-sologym-window") == "goals")
            {
                Application.targetFrameRate = 60;
                Screen.orientation = ScreenOrientation.Portrait;
                requestedCapture = Argument("-sologym-capture");
                reviewMode = Application.isEditor || HasArgument("-sologym-review");
#if SOLOGYM_REVIEW
                reviewMode = true;
#endif
                if (!reviewMode) { Debug.LogError("WIN-010 preview requires -sologym-review."); enabled = false; return; }
                if (FindFirstObjectByType<EventSystem>() == null)
                    new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
                var preview = new GameObject("SoloGym goals preview").AddComponent<OnboardingScreen>();
                string goalsLocale = Argument("-sologym-locale");
                preview.Initialize(goalsLocale ?? "auto", true, () => Application.Quit(), null, requestedCapture);
                enabled = false;
                return;
            }
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            requestedCapture = Argument("-sologym-capture");
            reviewMode = Application.isEditor || HasArgument("-sologym-review");
#if SOLOGYM_REVIEW
            // Local review APKs keep the explicitly labeled fictional Home accessible
            // even after real Firebase is configured. This never authorizes an account.
            reviewMode = true;
#endif
            googleLogo = Resources.Load<Texture2D>("Welcome/GoogleG");
            serif = SystemUI.Theme.heading;
            bold = SystemUI.Theme.headingBold;
            body = SystemUI.Theme.body;
            googleFont = Resources.Load<Font>("Fonts/GoogleSans-Medium");
            var data = JsonUtility.FromJson<PixelMap>(Resources.Load<TextAsset>("Welcome/PixelMap").text);
            foreach (var element in data.elements) map[element.id] = element;
            var configAsset = Resources.Load<TextAsset>("Auth/FirebaseConfig");
            var config = configAsset == null ? null : JsonUtility.FromJson<PreviewConfig>(configAsset.text);
            allowPreview = reviewMode || config == null || !config.enabled ||
                string.IsNullOrWhiteSpace(config.projectId) || string.IsNullOrWhiteSpace(config.apiKey);
            // Captures and their focused smoke checks never contact an identity provider.
            IWelcomeAuthService auth = requestedCapture != null || HasArgument("-sologym-smoke")
                ? (IWelcomeAuthService)new UnconfiguredWelcomeAuthService() : new FirebaseWelcomeAuthService();
            controller = new WelcomeController(auth);
            CreateCanvas();
            CreateArtAndText();
            CreateControls();
            CreateEmailView();
            controller.Changed += Render;
            controller.NavigationRequested += Navigate;
            controller.PasswordCleared += ClearPasswordInput;
            string locale = Argument("-sologym-locale");
            if (locale != null) controller.SetLanguage(locale);
            if (Argument("-sologym-view") == "email") controller.OpenEmail();
            Render(controller.Model);
            FitSafeArea();
            string initialWindow = Argument("-sologym-window");
            if ((initialWindow == "age" || initialWindow == "consent" || initialWindow == "profile") && reviewMode)
                OpenOnboarding(true, null, requestedCapture);
            else if (initialWindow == "goals" && reviewMode)
                OpenOnboarding(true, null, requestedCapture);
            else if (requestedCapture != null) StartCoroutine(Capture());
        }

        void CreateCanvas()
        {
            if (FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Welcome Camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(2, 6, 16, 255);
                camera.cullingMask = 0;
            }
            var canvasNode = new GameObject("Welcome UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasNode.transform.SetParent(transform, false);
            var canvas = canvasNode.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            root = NewRect("Reference canvas 853x1844", canvasNode.transform, new Rect(0, 0, Width, Height));
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void CreateArtAndText()
        {
            SystemUI.PortalPage(root,1019,632);
            SystemUI.Divider(root,1147,163,525);
            SystemUI.Icon(root,new Rect(421,1705,10,25),"sigil",Silver);
            welcomeGroup = NewRect("Welcome controls", root, new Rect(0, 0, Width, Height));
            SystemUI.Panel(welcomeGroup,Bounds("email.button"),PanelStyle.Primary);
            SystemUI.Panel(welcomeGroup,Bounds("register.button"),PanelStyle.Outline);
            // One native rounded surface replaces every AI provider interior pixel.
            // Its smooth contour preserves the approved silhouette without corner seams.
            var provider = NewRect("Official Google rounded surface", welcomeGroup, Bounds("google.button"))
                .gameObject.AddComponent<WelcomeProviderBackground>();
            provider.color = Color.white;
            provider.raycastTarget = false;
            var logo = NewRect("Official Google G", welcomeGroup, Bounds("google.logo")).gameObject.AddComponent<RawImage>();
            logo.texture = googleLogo;
            logo.color = Color.white;
            logo.raycastTarget = false;
            Label("language.label", root, 25, body);
            Label("entry.title", welcomeGroup, 80, bold);
            Label("entry.subtitle", welcomeGroup, 35, serif);
            Label("email.label", welcomeGroup, 43, bold);
            Label("google.label", welcomeGroup, 36, googleFont, new Color32(31, 31, 31, 255));
            Label("register.hint", welcomeGroup, 33, serif);
            Label("register.label", welcomeGroup, 34, serif, Cyan);
            Label("privacy.label", root, 27, serif);
            Label("terms.label", root, 27, serif);
            mainStatus = TextAt("Authentication status", welcomeGroup, new Rect(195, 1168, 463, 57), 23, body);
            mainStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            mainStatus.alignment = TextAnchor.MiddleCenter;
        }

        void CreateControls()
        {
            Hit("language", root, Touch(Bounds("language")), ShowLanguage);
            Hit("email", welcomeGroup, Touch(Bounds("email.button")), () => controller.OpenEmail());
            Hit("google", welcomeGroup, Touch(Bounds("google.button")), () => StartGoogle());
            Hit("register", welcomeGroup, Touch(Bounds("register.button")), () => controller.CreateAccount());
            Hit("privacy", root, Touch(Bounds("privacy.link")), () => controller.OpenPrivacy());
            Hit("terms", root, Touch(Bounds("terms.link")), () => controller.OpenTerms());
            cancelButton = NativeButton(root, new Rect(277, 945, 300, 70), "", () => controller.CancelAuthentication());
            cancelLabel = cancelButton.GetComponentInChildren<Text>();
            cancelButton.gameObject.SetActive(false);
        }

        void CreateEmailView()
        {
            emailGroup = NewRect("Email sign-in", root, new Rect(0, 0, Width, Height));
            Fill("Email panel interior", emailGroup, new Rect(70, 1036, 716, 602), new Color32(3, 16, 31, 255));
            emailTitle = TextAt("Email title", emailGroup, new Rect(110, 1064, 633, 72), 52, bold);
            emailTitle.alignment = TextAnchor.MiddleCenter;
            emailStatus = TextAt("Email status", emailGroup, new Rect(115, 1141, 623, 44), 22, body);
            emailStatus.alignment = TextAnchor.MiddleCenter;
            emailStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            emailLabel = TextAt("Email label", emailGroup, new Rect(107, 1187, 640, 37), 28, body);
            emailInput = InputAt("Email", new Rect(105, 1228, 643, 82), false);
            emailInput.onValueChanged.AddListener(value => controller.SetEmail(value));
            passwordLabel = TextAt("Password label", emailGroup, new Rect(107, 1327, 640, 37), 28, body);
            passwordInput = InputAt("Password", new Rect(105, 1368, 643, 82), true);
            passwordInput.onValueChanged.AddListener(value => controller.SetPassword(value));
            showPasswordButton = NativeButton(emailGroup, new Rect(595, 1368, 151, 82), "", () => controller.TogglePasswordVisibility());
            showPasswordLabel = showPasswordButton.GetComponentInChildren<Text>();
            showPasswordLabel.fontSize = 22;
            submitButton = NativeButton(emailGroup, new Rect(104, 1480, 645, 92), "", () => StartEmail());
            ((SystemPanel)submitButton.targetGraphic).style=PanelStyle.Primary;
            submitLabel = submitButton.GetComponentInChildren<Text>();
            submitLabel.font = bold; submitLabel.fontSize = 38;
            forgotButton = NativeButton(emailGroup, new Rect(157, 1580, 538, 52), "", () => controller.ForgotPassword());
            forgotButton.targetGraphic.color = Color.clear;
            forgotLabel = forgotButton.GetComponentInChildren<Text>();
            forgotLabel.fontSize = 25;
            backButton = NativeButton(emailGroup, new Rect(40, 40, 150, 80), "", Back);
            backLabel = backButton.GetComponentInChildren<Text>();
            emailGroup.gameObject.SetActive(false);
        }

        InputField InputAt(string name, Rect rect, bool password)
        {
            var node = NewRect(name + " input", emailGroup, rect);
            var background=node.gameObject.AddComponent<SystemPanel>();background.theme=SystemUI.Theme;background.style=PanelStyle.Input;
            var input = node.gameObject.AddComponent<InputField>();
            var value = TextAt(name + " value", node, new Rect(19, 1, rect.width - (password ? 181 : 38), rect.height - 2), 30, body);
            value.supportRichText = false;
            input.textComponent = value;
            input.targetGraphic = background;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.EmailAddress;
            input.keyboardType = password ? TouchScreenKeyboardType.Default : TouchScreenKeyboardType.EmailAddress;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = password ? 4096 : 254;
            input.asteriskChar = '•';
            input.selectionColor = new Color(.13f, .69f, 1, .45f);
            input.caretColor = Cyan; input.customCaretColor = true;
            return input;
        }

        void Render(WelcomeViewModel model)
        {
            bool english = model.Language == "en";
            foreach (var pair in labels) Place(pair.Value.rectTransform, Bounds(pair.Key));
            Set("language.label", english ? "EN" : "ES");
            Set("entry.title", model.Copy("title"));
            Set("entry.subtitle", model.Copy("subtitle"));
            Set("email.label", model.Copy("email_entry"));
            Set("google.label", model.Copy("google"));
            Set("register.hint", model.Copy("new_here"));
            Set("register.label", model.Copy("create"));
            Set("privacy.label", model.Copy("privacy"));
            Set("terms.label", model.Copy("terms"));
            bool email = model.View == WelcomeView.EmailSignIn;
            welcomeGroup.gameObject.SetActive(!email);
            emailGroup.gameObject.SetActive(email);
            string status = model.IsBusy ? model.StatusText : model.ErrorText;
            mainStatus.text = status;
            mainStatus.gameObject.SetActive(!string.IsNullOrEmpty(status));
            labels["entry.subtitle"].gameObject.SetActive(string.IsNullOrEmpty(status));
            emailStatus.text = status;
            emailTitle.text = model.Copy("email_title");
            emailLabel.text = model.Copy("email_label");
            passwordLabel.text = model.Copy("password_label");
            submitLabel.text = model.IsBusy ? model.Copy("signing_in") : model.Copy("submit");
            forgotLabel.text = model.Copy("forgot");
            showPasswordLabel.text = model.ShowPassword ? L("Hide", "Ocultar") : L("Show", "Mostrar");
            backLabel.text = "‹ " + model.Copy("back");
            cancelLabel.text = model.Copy("cancel");
            cancelButton.gameObject.SetActive(model.IsBusy);
            cancelButton.transform.SetAsLastSibling();
            foreach (var key in new[] { "email", "google", "register", "privacy", "terms" })
                buttons[key].interactable = !model.IsBusy;
            buttons["google"].interactable = model.CanSubmit;
            submitButton.interactable = model.CanSubmit;
            forgotButton.interactable = !model.IsBusy;
            showPasswordButton.interactable = !model.IsBusy;
            emailInput.interactable = passwordInput.interactable = !model.IsBusy;
            if (emailInput.text != model.Email) emailInput.SetTextWithoutNotify(model.Email ?? "");
            passwordInput.contentType = model.ShowPassword ? InputField.ContentType.Standard : InputField.ContentType.Password;
            passwordInput.lineType = InputField.LineType.SingleLine;
            passwordInput.ForceLabelUpdate();
            FitSafeArea();
        }

        async void StartEmail() { await controller.SignInEmailAsync(); }
        async void StartGoogle() { await controller.SignInGoogleAsync(); }
        void ClearPasswordInput()
        {
            if (passwordInput == null) return;
            passwordInput.SetTextWithoutNotify("");
            passwordInput.contentType = InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        void Back()
        {
            EventSystem.current?.SetSelectedGameObject(null);
            emailInput.DeactivateInputField(); passwordInput.DeactivateInputField();
            controller.Back();
        }

        void Navigate(WelcomeNavigation navigation)
        {
            if (navigation.WindowId == "WIN-006") { OpenOnboarding(false); return; }
            if (navigation.WindowId == "WIN-007" && (navigation.Context == "privacy" || navigation.Context == "terms"))
            { OpenOnboarding(false, navigation.Context); return; }
            string heading = navigation.Context == "privacy" ? controller.Model.Copy("privacy") :
                navigation.Context == "terms" ? controller.Model.Copy("terms") :
                navigation.WindowId == "WIN-005" ? controller.Model.Copy("forgot") : controller.Model.Copy("create");
            string message = navigation.AuthorizedByBackend
                ? L("Your identity was verified. Profile setup will be available in an upcoming version.",
                    "Tu identidad se verificó. La configuración del perfil estará disponible en una próxima versión.")
                : controller.Model.Copy("future_window");
            // Identity success cannot silently enter the fictional Home fixture.
            ShowNotice(message, heading);
        }

        void ShowLanguage()
        {
            OpenModal(controller.Model.Copy("language"), allowPreview || reviewMode ? 725 : 430);
            NativeButton(modal, new Rect(34, 108, 592, 82), "English", () => { controller.SetLanguage("en"); CloseModal(); });
            NativeButton(modal, new Rect(34, 207, 592, 82), "Español", () => { controller.SetLanguage("es"); CloseModal(); });
            NativeButton(modal, new Rect(34, 306, 592, 82), L("Use device language", "Usar idioma del dispositivo"),
                () => { controller.SetLanguage("auto"); CloseModal(); });
            if (allowPreview || reviewMode)
            {
                var note = TextAt("Sample notice", modal, new Rect(34, 410, 592, 75), 23, body);
                note.horizontalOverflow = HorizontalWrapMode.Wrap;
                note.text = L("Sample Home uses fictional profile data. It does not sign you in.",
                    "El Inicio de muestra usa datos ficticios. No inicia una sesión de cuenta.");
                NativeButton(modal, new Rect(34, 505, 592, 82), L("View sample Home", "Ver Inicio de muestra"), OpenSampleHome);
                NativeButton(modal, new Rect(34, 608, 592, 82), L("Preview profile setup", "Vista previa de configuración"), () => OpenOnboarding(true));
            }
        }

        void OpenOnboarding(bool review, string document = null, string capturePath = null)
        {
            if (onboarding != null || (review && !reviewMode && !allowPreview)) return;
            CloseModal();
            controller.CancelAuthentication();
            ClearPasswordInput();
            root.gameObject.SetActive(false);
            onboarding = new GameObject("SoloGym onboarding").AddComponent<OnboardingScreen>();
            onboarding.Initialize(controller.Model.LanguagePreference, review, () =>
            {
                onboarding = null;
                root.gameObject.SetActive(true);
                controller.SetLanguage(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"));
                Render(controller.Model);
            }, document, capturePath);
        }

        void OpenSampleHome()
        {
            if (!allowPreview && !reviewMode || switching) return;
            switching = true;
            controller.CancelAuthentication();
            ClearPasswordInput();
            SceneManager.LoadScene("SystemHome");
        }

        void ShowNotice(string message, string title)
        {
            OpenModal(title, 520);
            var notice = TextAt("Notice", modal, new Rect(34, 120, 592, 230), 27, body);
            notice.alignment = TextAnchor.UpperLeft;
            notice.horizontalOverflow = HorizontalWrapMode.Wrap;
            notice.text = message;
            NativeButton(modal, new Rect(34, 396, 592, 82), controller.Model.Copy("close"), CloseModal);
        }

        void OpenModal(string title, float height)
        {
            CloseModal();
            var shade = NewRect("Dialog shade", root, new Rect(0, 0, Width, Height));
            shade.gameObject.AddComponent<Image>().color = new Color(0, .01f, .04f, .85f);
            modal = NewRect("System dialog", shade, new Rect(96.5f, (Height - height) / 2, 660, height));
            var frame=modal.gameObject.AddComponent<SystemPanel>();frame.theme=SystemUI.Theme;frame.ornaments=true;
            var heading = TextAt("Dialog heading", modal, new Rect(34, 22, 500, 70), 34, bold);
            heading.text = title;
            NativeButton(modal, new Rect(559, 8, 92, 92), "×", CloseModal);
        }

        void CloseModal()
        {
            if (modal == null) return;
            var shade = modal.parent.gameObject;
            modal = null;
            shade.SetActive(false);
            Destroy(shade);
        }

        Text Label(string key, Transform parent, int size, Font font, Color? color = null)
        {
            var label = TextAt(key, parent, Bounds(key), size, font, color);
            var fit=label.gameObject.AddComponent<SystemTextFit>();fit.maximum=size;label.alignment=TextAnchor.MiddleCenter;
            if(key=="entry.title")label.gameObject.AddComponent<OnboardingSilverText>();
            labels[key] = label;
            return label;
        }

        Text TextAt(string name,Transform parent,Rect rect,int size,Font font,Color? color=null)
        {var text=SystemUI.Text(parent,rect,"",size,font,color);text.name=name;return text;}

        Button NativeButton(Transform parent, Rect rect, string caption, Action action)
        {return SystemUI.Button(parent,rect,caption,action);}

        void Hit(string key, Transform parent, Rect rect, Action action)
        {
            var node = NewRect("Control / " + key, parent, rect);
            var image = node.gameObject.AddComponent<Image>(); image.color = Color.clear;
            var button = node.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => action());
            buttons[key] = button;
        }



        void Fill(string name, Transform parent, Rect rect, Color color)
        {
            var image = NewRect(name, parent, rect).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
        }

        static RectTransform NewRect(string name,Transform parent,Rect rect)
        {return SystemUI.Node(name,parent,rect);}

        static void Place(RectTransform transform, Rect rect)
        {
            transform.anchorMin = transform.anchorMax = new Vector2(0, 1);
            transform.pivot = new Vector2(0, 1);
            transform.anchoredPosition = new Vector2(rect.x, -rect.y);
            transform.sizeDelta = new Vector2(rect.width, rect.height);
        }

        Rect Bounds(string key) => map[key].rect.Rect;
        Rect TextBounds(string key, bool english)
        {
            var entry = map[key];
            if (english && entry.locale_overrides?.en?.glyph_bounds != null)
                return entry.locale_overrides.en.glyph_bounds.Rect;
            return entry.glyph_bounds != null ? entry.glyph_bounds.Rect : entry.rect.Rect;
        }

        static Rect Expanded(Rect rect, float padding) => new Rect(rect.x - padding, rect.y - padding,
            rect.width + padding * 2, rect.height + padding * 2);
        static Rect Touch(Rect rect)
        {
            // 108 reference pixels is at least 48 logical pixels on a typical 390dp
            // portrait device. The visual geometry is never enlarged with the target.
            float width = Mathf.Max(108, rect.width), height = Mathf.Max(108, rect.height);
            return new Rect(rect.center.x - width / 2, rect.center.y - height / 2, width, height);
        }
        void Set(string key, string text) => labels[key].text = text ?? "";
        string L(string en, string es) => controller.Model.Language == "es" ? es : en;

        void Update()
        {
            if (controller == null || onboarding != null) return;
            bool keyboard = TouchScreenKeyboard.visible && controller.Model.View == WelcomeView.EmailSignIn;
            float keyboardHeight = keyboard ? TouchScreenKeyboard.area.height : 0;
            if (lastSize.x != Screen.width || lastSize.y != Screen.height || lastSafe != Screen.safeArea ||
                lastKeyboard != keyboard || !Mathf.Approximately(lastKeyboardHeight, keyboardHeight)) FitSafeArea();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (modal != null) CloseModal();
                else if (controller.Model.View == WelcomeView.EmailSignIn) Back();
                else if (controller.Model.IsBusy) controller.CancelAuthentication();
            }
            if (Input.GetKeyDown(KeyCode.F8)) { reviewMode = true; ShowLanguage(); }
            if (requestedCapture == null && Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1;
                bool online = Application.internetReachability != NetworkReachability.NotReachable;
                if (online != lastOnline) { lastOnline = online; controller.SetOnline(online); }
                else if (controller.Model.RetryAfterSeconds > 0) controller.Refresh();
            }
        }

        void FitSafeArea()
        {
            if (root == null) return;
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / Width, safe.height / Height);
            float topInset = Screen.height - safe.yMax + (safe.height - Height * scale) / 2;
            float upward = 0;
            bool keyboard = TouchScreenKeyboard.visible && controller.Model.View == WelcomeView.EmailSignIn;
            float keyboardHeight = keyboard ? TouchScreenKeyboard.area.height : 0;
            if (keyboard)
            {
                float occlusion = keyboardHeight > 0 ? TouchScreenKeyboard.area.yMax : Screen.height * .42f;
                // Lift the complete form so its submit action and both focused fields
                // remain above the native keyboard; restore the exact fit when hidden.
                float submitBottom = Screen.height - topInset - 1572 * scale;
                upward = Mathf.Max(0, occlusion + 18 * scale - submitBottom);
            }
            SystemViewport.Fit(root,Width,Height,upward);
            lastSize = new Vector2(Screen.width, Screen.height); lastSafe = safe;
            lastKeyboard = keyboard; lastKeyboardHeight = keyboardHeight;
        }

        IEnumerator Capture()
        {
            for (int i = 0; i < 6; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(requestedCapture)));
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(requestedCapture, screenshot.EncodeToPNG());
            Destroy(screenshot);
            Debug.Log("SOLOGYM_CAPTURE " + requestedCapture);
            if (HasArgument("-sologym-smoke")) yield return SmokeControls();
            Application.Quit();
        }

        IEnumerator SmokeControls()
        {
            bool ok = true;
            controller.Back();
            buttons["language"].onClick.Invoke(); yield return null;
            ok &= modal != null;
            foreach (var button in modal.GetComponentsInChildren<Button>())
                if (button.GetComponentInChildren<Text>()?.text == "Español") { button.onClick.Invoke(); break; }
            ok &= controller.Model.Language == "es";
            buttons["email"].onClick.Invoke(); yield return null;
            ok &= controller.Model.View == WelcomeView.EmailSignIn && emailInput.keyboardType == TouchScreenKeyboardType.EmailAddress;
            submitButton.onClick.Invoke(); yield return null;
            ok &= controller.Model.ErrorKey == "required_email";
            emailInput.text = "invalid";
            submitButton.onClick.Invoke(); yield return null;
            ok &= controller.Model.ErrorKey == "invalid_email";
            emailInput.text = "fixture@example.invalid";
            submitButton.onClick.Invoke(); yield return null;
            ok &= controller.Model.ErrorKey == "required_password";
            // Synthetic local field content only; never contacts Firebase in capture mode.
            passwordInput.text = "local-smoke-only";
            ok &= passwordInput.contentType == InputField.ContentType.Password;
            showPasswordButton.onClick.Invoke();
            ok &= passwordInput.contentType == InputField.ContentType.Standard;
            backButton.onClick.Invoke(); yield return null;
            ok &= controller.Model.View == WelcomeView.Welcome && passwordInput.text == "" &&
                passwordInput.contentType == InputField.ContentType.Password;
            bool navigated = false;
            controller.NavigationRequested += _ => navigated = true;
            buttons["google"].onClick.Invoke(); yield return null;
            ok &= controller.Model.ErrorKey == "unavailable" && !navigated && !controller.Model.IsBusy;
            controller.Back(); controller.SetEmail("");
            controller.SetLanguage(Argument("-sologym-locale") ?? "en");
            string result = "{\"passed\":" + (ok ? "true" : "false") +
                ",\"checks\":[\"language control\",\"email input and validation\",\"password masking and reveal\",\"back clears password\",\"unconfigured provider never authorizes Home\"]}";
            File.WriteAllText(Path.ChangeExtension(requestedCapture, ".smoke.json"), result);
            Debug.Log("SOLOGYM_SMOKE " + result);
            if (!ok) Application.Quit(2);
        }

        static bool HasArgument(string key) => Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0;
        static string Argument(string key)
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        void OnApplicationPause(bool paused) { if (paused) controller?.OnBackground(); }
        void OnDestroy() { controller?.Dispose(); }
    }

    /// <summary>Code-native rounded provider surface; no generated button bitmap.</summary>
    public sealed class WelcomeProviderBackground : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            const int steps = 12;
            const float feather = .6f;
            float radius = Mathf.Min(13, Mathf.Min(rect.width, rect.height) / 2);
            var centers = new[]
            {
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius)
            };
            Color32 solid = color;
            Color32 transparent = color; transparent.a = 0;
            mesh.AddVert(rect.center, solid, Vector2.zero);
            int count = 4 * (steps + 1);
            for (int corner = 0; corner < 4; corner++)
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner * 90 + step * 90f / steps) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    mesh.AddVert(centers[corner] + direction * radius, solid, Vector2.zero);
                }
            for (int corner = 0; corner < 4; corner++)
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner * 90 + step * 90f / steps) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    mesh.AddVert(centers[corner] + direction * (radius + feather), transparent, Vector2.zero);
                }
            for (int point = 0; point < count; point++)
            {
                int a = 1 + point, b = 1 + (point + 1) % count;
                mesh.AddTriangle(0, a, b);
                mesh.AddTriangle(a, a + count, b + count);
                mesh.AddTriangle(a, b + count, b);
            }
        }
    }
}
