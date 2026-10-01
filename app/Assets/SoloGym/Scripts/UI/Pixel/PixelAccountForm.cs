using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>The account form shares the login room/panel, with live controls and no baked text.</summary>
    public sealed class PixelAccountForm : MonoBehaviour
    {
        public AccountRegistrationController Controller { get; private set; }
        public PixelFormField Email { get; private set; }
        public PixelFormField Password { get; private set; }
        public PixelFormField Confirmation { get; private set; }
        public PixelPrimaryButton Submit { get; private set; }
        public PixelSecondaryAction Back { get; private set; }
        public PixelSecondaryAction SignIn { get; private set; }
        public PixelSecondaryAction Cancel { get; private set; }
        public ScrollRect Scroll { get; private set; }
        public Text Status { get; private set; }
        public event Action StateChanged;
        RectTransform content, viewport;
        Text title, subtitle, hint;
        string language = "es", lastError;
        bool wasBusy;
        Action exit, back;
        bool previewOnly;

        public void Initialize(Transform panel, IAccountRegistrationService service, Action exitAction,
            Selectable privacy, Selectable languageControl, Action backAction = null)
        {
            transform.SetParent(panel, false);
            var root = gameObject.GetComponent<RectTransform>();
            PixelJournalUI.Stretch(root);
            exit = exitAction; back = backAction ?? exitAction;
            Controller = new AccountRegistrationController(service);
            Back = LinkButton(root, "", GoBack); Place(Back, new Rect(20, 10, 112, 52));
            title = PixelJournalUI.Text(root, new Rect(28, 48, 514, 48), "", 38, false, TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(root, 62, 94, 446);
            subtitle = PixelJournalUI.Text(root, new Rect(28, 99, 514, 38), "", 24, false, TextAnchor.MiddleCenter);
            content = PixelJournalUI.Scroll(root, new Rect(28, 144, 514, 472), 476);
            viewport = (RectTransform)content.parent; Scroll = viewport.GetComponent<ScrollRect>();
            Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var track = (RectTransform)Scroll.verticalScrollbar.transform;
            track.anchorMin = new Vector2(1, 0); track.anchorMax = new Vector2(1, 1); track.pivot = new Vector2(1, .5f);
            track.anchoredPosition = Vector2.zero; track.sizeDelta = new Vector2(7, 0);
            Status = PixelJournalUI.Text(content, new Rect(0, 0, 502, 60), "", 22, false);
            Status.color = new Color32(255, 194, 158, 255);
            Email = Field(PixelFormField.Kind.Email);
            Password = Field(PixelFormField.Kind.Password);
            Confirmation = Field(PixelFormField.Kind.Password);
            Email.Input.onSubmit.AddListener(_ => Focus(Password));
            Password.Input.onSubmit.AddListener(_ => Focus(Confirmation));
            Confirmation.Input.onSubmit.AddListener(_ => SubmitAccount());
            Submit = PixelPrimaryButton.Create(content, "", SubmitAccount);
            Submit.SetFontSize(30); Submit.Background.pixelsPerUnitMultiplier = 2;
            hint = PixelJournalUI.Text(content, new Rect(), "", 23, false, TextAnchor.MiddleCenter);
            SignIn = LinkButton(content, "", Leave);
            Cancel = LinkButton(content, "", () => Controller.Cancel());
            Navigate(Back, languageControl, Email.Input);
            Email.SetTraversal(Back, Password.Input);
            Password.SetTraversal(Email.Input, Confirmation.Input);
            Navigate(Password.Visibility, Password.Input, Confirmation.Input);
            Confirmation.SetTraversal(Password.Visibility, Submit);
            Navigate(Confirmation.Visibility, Confirmation.Input, Submit);
            Navigate(Submit, Confirmation.Visibility, SignIn);
            Navigate(SignIn, Submit, privacy); Navigate(Cancel, languageControl, languageControl);
            Controller.SecretsCleared += ClearSecrets;
            Controller.Changed += Render;
            Render();
        }

        PixelFormField Field(PixelFormField.Kind kind)
        {
            var f = PixelFormField.Create(content, kind, "", "");
            f.Background.pixelsPerUnitMultiplier = 2;
            f.Label.fontSize = 23; f.SetLabelColor(PixelJournalUI.Ivory);
            f.Input.textComponent.fontSize = 24; f.Input.characterLimit = kind == PixelFormField.Kind.Email ? 254 : 4096;
            if (f.Visibility != null) { f.Visibility.Background.pixelsPerUnitMultiplier = 2; f.Visibility.SetHorizontalPadding(8); }
            return f;
        }

        public void SetLocale(string locale)
        {
            if (language == locale) return;
            language = locale; Render();
        }
        public void Open(string email, string locale, bool review = false)
        {
            language = locale; previewOnly = review;
            Email.SetValueWithoutNotify(email);
            Controller.Reset(); Render();
            Scroll.StopMovement(); content.anchoredPosition = Vector2.zero;
        }
        public void SubmitAccount()
        {
            if (!gameObject.activeInHierarchy || !Controller.CanSubmit) return;
            string email = Email.Input.text.Trim(); Email.SetValueWithoutNotify(email);
            ReleaseKeyboard();
            _ = Controller.RegisterAsync(email, Password.Input.text, Confirmation.Input.text, previewOnly);
        }
        public void ClearSecrets()
        {
            foreach (var f in new[] { Password, Confirmation })
            { f?.SetValueWithoutNotify(""); f?.HidePassword(); }
        }
        public void ReleaseKeyboard()
        {
            foreach (var f in new[] { Email, Password, Confirmation }) f?.Input.DeactivateInputField();
            if (EventSystem.current?.currentSelectedGameObject?.transform.IsChildOf(transform) == true)
                EventSystem.current.SetSelectedGameObject(null);
        }
        void Focus(PixelFormField f)
        {
            if (Controller.Busy || !gameObject.activeInHierarchy) return;
            f.Input.Select(); f.Input.ActivateInputField();
        }
        void GoBack() { Controller.Cancel(); ReleaseKeyboard(); back?.Invoke(); }
        void Leave() { Controller.Cancel(); ReleaseKeyboard(); exit?.Invoke(); }
        public void Backgrounded() { Controller.Cancel(); ReleaseKeyboard(); }

        void Render()
        {
            bool busy = Controller.Busy, created = Controller.Created;
            title.text = created ? L("ACCOUNT CREATED", "CUENTA CREADA") : L("CREATE ACCOUNT", "CREAR CUENTA");
            subtitle.text = created ? "" : previewOnly ? L("Preview · Email registration", "Vista previa · Registro por correo") : L("Email registration", "Registro por correo");
            Back.SetLabel(L("Back", "Volver")); Back.interactable = !busy;
            Email.SetLocalizedText(L("Email address", "Correo electrónico"), L("you@email.com", "tu@correo.com"), "", "", "");
            Password.SetLocalizedText(L("Password", "Contraseña"), "********", "", L("Show", "Mostrar"), L("Hide", "Ocultar"));
            Confirmation.SetLocalizedText(L("Repeat password", "Repetir contraseña"), "********", "", L("Show", "Mostrar"), L("Hide", "Ocultar"));
            string key = Controller.ErrorKey, error = Error(key);
            Email.SetError(key == "required_email" || key == "invalid_email" ? error : "");
            Password.SetError(key == "required_password" || key == "policy" ? error : "");
            Confirmation.SetError(key == "required_confirmation" || key == "mismatch" ? error : "");
            foreach (var f in new[] { Email, Password, Confirmation })
            { f.gameObject.SetActive(!created); f.SetInteractable(!busy); }
            Status.text = created
                ? L("Your account was created. Profile setup is still required before opening your gym.\n\nConnected profile setup is not available in this build yet.",
                    "Tu cuenta fue creada. Antes de abrir tu gimnasio aún debes configurar tu perfil.\n\nLa configuración de perfil conectada aún no está disponible en esta versión.")
                : Email.Error.Length + Password.Error.Length + Confirmation.Error.Length > 0 ? "" : error;
            Status.gameObject.SetActive(Status.text.Length > 0);
            Submit.gameObject.SetActive(!created);
            Submit.SetLabel(L("CREATE ACCOUNT", "CREAR CUENTA"));
            Submit.SetLoading(busy, Controller.Phase == RegistrationPhase.CheckingSetup ? L("CHECKING…", "COMPROBANDO…") : L("CREATING…", "CREANDO…"));
            Submit.interactable = Controller.CanSubmit || busy;
            hint.text = L("Already have an account?", "¿Ya tienes cuenta?");
            hint.gameObject.SetActive(!created && !busy);
            SignIn.SetLabel(L("Sign in", "Iniciar sesión")); SignIn.gameObject.SetActive(!busy);
            Cancel.SetLabel(L("Cancel", "Cancelar")); Cancel.gameObject.SetActive(busy);
            if (busy && !wasBusy) { ReleaseKeyboard(); Cancel.Select(); }
            if (!busy && wasBusy && gameObject.activeInHierarchy) { if (created) SignIn.Select(); else Submit.Select(); }
            wasBusy = busy;
            Layout();
            if (lastError != key) { lastError = key; Scroll.verticalNormalizedPosition = 1; }
            StateChanged?.Invoke();
        }

        string Error(string key)
        {
            switch (key)
            {
                case "required_email": return L("Enter your email.", "Escribe tu correo.");
                case "invalid_email": return L("Enter a valid email.", "Escribe un correo válido.");
                case "required_password": return L("Enter a password.", "Escribe una contraseña.");
                case "required_confirmation": return L("Repeat your password.", "Repite tu contraseña.");
                case "mismatch": return L("The passwords do not match.", "Las contraseñas no coinciden.");
                case "policy": return L("This password does not meet the account policy.", "Esta contraseña no cumple la política de la cuenta.");
                case "provisional": return L("These documents are placeholders. Live registration is not available yet. No account was created.", "Los textos son provisionales. El registro real aún no está disponible. No se creó ninguna cuenta.");
                case "setup": return L("Age and consent setup is not available yet. No account was created.", "La configuración de edad y consentimiento aún no está disponible. No se creó ninguna cuenta.");
                case "offline": return L("No connection. Reconnect to try again.", "Sin conexión. Reconéctate para volver a intentar.");
                case "rate": return L("Please wait before trying again.", "Espera antes de volver a intentar.") + " (" + Controller.RetrySeconds + " s)";
                case "rejected": return L("Unable to create an account with these details. Try signing in or try again.", "No se pudo crear una cuenta con estos datos. Intenta iniciar sesión o vuelve a intentar.");
                case "unknown": return L("The request was interrupted and may have completed. Try signing in before creating an account again.", "La solicitud se interrumpió y pudo completarse. Intenta iniciar sesión antes de volver a crear una cuenta.");
                case "unavailable": return L("Account creation is unavailable. Please try again later.", "La creación de cuenta no está disponible. Inténtalo más tarde.");
                default: return "";
            }
        }

        void Layout()
        {
            float y = 0;
            if (Status.text.Length > 0)
            {
                Status.rectTransform.sizeDelta = new Vector2(502, 60);
                float height = Mathf.Max(48, Status.preferredHeight + 12);
                Place(Status, new Rect(0, y, 502, height)); y += height + 12;
            }
            if (!Controller.Created)
            {
                foreach (var f in new[] { Email, Password, Confirmation })
                { Place(f, new Rect(0, y, 502, f.PreferredHeight)); y += f.PreferredHeight + 4; }
                y += 4; Place(Submit, new Rect(0, y, 502, 64)); y += 76;
                Place(hint, new Rect(0, y, 502, 30)); y += 28;
            }
            Place(SignIn, new Rect(0, y, 502, 52)); Place(Cancel, new Rect(0, y, 502, 52)); y += 52;
            content.sizeDelta = new Vector2(502, Mathf.Max(viewport.rect.height, y));
        }

        public void Relayout(float panelHeight, bool keyboard)
        {
            viewport.sizeDelta = new Vector2(514, Mathf.Max(86, panelHeight - 160));
            Layout(); Canvas.ForceUpdateCanvases();
            if (keyboard) RevealSelection();
        }
        public void RevealSelection()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(content)) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
            float shift = bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y : bounds.max.y > viewport.rect.yMax ? viewport.rect.yMax - bounds.max.y : 0;
            var pos = content.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y + shift, 0, Mathf.Max(0, content.rect.height - viewport.rect.height)); content.anchoredPosition = pos;
        }
        void OnDisable() { if (Controller != null) { Controller.Cancel(); ReleaseKeyboard(); } }
        void OnDestroy() { Controller?.Dispose(); }
        string L(string en, string es) => language == "es" ? es : en;
        static void Place(Component c, Rect r) => PixelJournalUI.Place((RectTransform)c.transform, r);
        static PixelSecondaryAction LinkButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var b = PixelSecondaryAction.Create(parent, PixelSecondaryAction.Appearance.Text, label, action);
            b.Label.fontSize = 22; b.SetHorizontalPadding(8); b.Background.pixelsPerUnitMultiplier = 2; return b;
        }
        static void Navigate(Selectable current, Selectable previous, Selectable next)
        {
            var tab = current.GetComponent<PixelFieldTabNavigation>() ?? current.gameObject.AddComponent<PixelFieldTabNavigation>();
            tab.Previous = previous; tab.Next = next;
            if (!(current is InputField)) current.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous, selectOnDown = next };
        }
    }
}
