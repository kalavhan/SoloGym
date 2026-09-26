using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SoloGym
{
    public enum WelcomeView { Welcome, EmailSignIn }
    public enum AuthStatus { Success, Cancelled, Unavailable, Offline, InvalidCredentials, ProviderError, RateLimited }

    /// <summary>
    /// A trusted service adapter supplies the server-authorized checkpoint after identity,
    /// session and account checks. UI input must never supply this destination.
    /// </summary>
    public sealed class AuthOutcome
    {
        public AuthStatus Status { get; }
        public string AuthorizedDestination { get; }
        public string Context { get; }
        public int RetryAfterSeconds { get; }

        public AuthOutcome(AuthStatus status, string authorizedDestination = null,
            string context = null, int retryAfterSeconds = 0)
        {
            Status = status;
            AuthorizedDestination = authorizedDestination;
            Context = context;
            RetryAfterSeconds = Math.Max(0, retryAfterSeconds);
        }
    }

    public interface IWelcomeAuthService
    {
        Task<AuthOutcome> SignInEmailAsync(string email, string password, CancellationToken token);
        Task<AuthOutcome> SignInGoogleAsync(CancellationToken token);
    }

    /// <summary>No credentials, tokens, network calls or simulated successful account.</summary>
    public sealed class UnconfiguredWelcomeAuthService : IWelcomeAuthService
    {
        public Task<AuthOutcome> SignInEmailAsync(string email, string password, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new AuthOutcome(AuthStatus.Unavailable));
        }

        public Task<AuthOutcome> SignInGoogleAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new AuthOutcome(AuthStatus.Unavailable));
        }
    }

    public sealed class WelcomeNavigation
    {
        public string WindowId { get; }
        public string Context { get; }
        public bool ReadOnly { get; }
        public bool AuthorizedByBackend { get; }

        public WelcomeNavigation(string windowId, string context = null, bool readOnly = false,
            bool authorizedByBackend = false)
        {
            WindowId = windowId;
            Context = context;
            ReadOnly = readOnly;
            AuthorizedByBackend = authorizedByBackend;
        }
    }

    public sealed class WelcomeViewModel
    {
        public string Language, LanguagePreference, Email, ErrorKey, ErrorText, StatusText;
        public WelcomeView View;
        public bool IsBusy, IsOffline, ShowPassword, CanSubmit;
        public int RetryAfterSeconds;
        public string Copy(string key) => WelcomeCopy.Get(key, Language);
    }

    /// <summary>
    /// Localized entry flow. Only language is persisted; passwords stay in the active
    /// input/request lifetime. An unconfigured build always reports unavailable auth.
    /// </summary>
    public sealed class WelcomeController : IDisposable
    {
        // Shared with HomeController so an explicit language survives navigation.
        const string LanguageKey = "SoloGym.Home.Language.v1";
        readonly IWelcomeAuthService service;
        string languagePreference, language, email = "", password = "", errorKey = "";
        WelcomeView view;
        bool online = true, showPassword, disposed;
        CancellationTokenSource inFlight;
        DateTime retryUntil;

        public WelcomeViewModel Model { get; private set; }
        public event Action<WelcomeViewModel> Changed;
        public event Action<WelcomeNavigation> NavigationRequested;
        public event Action PasswordCleared;

        public WelcomeController(IWelcomeAuthService service = null)
        {
            this.service = service ?? new UnconfiguredWelcomeAuthService();
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            Refresh();
        }

        public void SetLanguage(string choice)
        {
            choice = (choice ?? "auto").Trim().ToLowerInvariant();
            if (choice != "auto" && choice != "en" && choice != "es")
                throw new ArgumentException("Language must be en, es or auto.", nameof(choice));
            languagePreference = choice;
            language = ResolveLanguage(choice);
            PlayerPrefs.SetString(LanguageKey, choice);
            PlayerPrefs.Save();
            Refresh();
        }

        public void ToggleLanguage() => SetLanguage(language == "en" ? "es" : "en");

        public void OpenEmail()
        {
            if (disposed || inFlight != null) return;
            view = WelcomeView.EmailSignIn;
            errorKey = "";
            Refresh();
        }

        public void Back()
        {
            CancelRequest();
            ClearPassword();
            view = WelcomeView.Welcome;
            errorKey = "";
            Refresh();
        }

        public void SetEmail(string value)
        {
            if (disposed || inFlight != null) return;
            email = value ?? "";
            if (Model != null) Model.Email = email;
        }

        public void SetPassword(string value)
        {
            if (disposed || inFlight != null) return;
            password = value ?? "";
        }

        public void TogglePasswordVisibility()
        {
            if (disposed || inFlight != null) return;
            showPassword = !showPassword;
            Refresh();
        }

        public void SetOnline(bool isOnline)
        {
            online = isOnline;
            if (!online && inFlight != null)
            {
                CancelRequest();
                ClearPassword();
            }
            if (!online) errorKey = "offline";
            else if (errorKey == "offline") errorKey = "";
            Refresh();
        }

        public Task SignInEmailAsync()
        {
            if (!CanStart()) return Task.CompletedTask;
            email = email.Trim();
            if (string.IsNullOrWhiteSpace(email)) return ValidationFailure("required_email");
            if (!IsEmailAddress(email)) return ValidationFailure("invalid_email");
            if (string.IsNullOrEmpty(password)) return ValidationFailure("required_password");
            return AuthenticateAsync(false);
        }

        public Task SignInGoogleAsync()
        {
            if (!CanStart()) return Task.CompletedTask;
            return AuthenticateAsync(true);
        }

        public void CancelAuthentication()
        {
            CancelRequest();
            ClearPassword();
            view = WelcomeView.Welcome;
            errorKey = "";
            Refresh();
        }

        public void OnBackground()
        {
            // Native Google authentication may temporarily background the app. Keep
            // that provider operation alive; discard email/password requests instead.
            if (!providerRequest) CancelRequest();
            ClearPassword();
            Refresh();
        }

        public void ShowSessionExpired()
        {
            CancelRequest();
            ClearPassword();
            view = WelcomeView.Welcome;
            errorKey = "expired";
            Refresh();
        }

        public void CreateAccount() => Navigate("WIN-006", "create_email_account");
        public void ForgotPassword() => Navigate("WIN-005", "recover_password");
        public void OpenPrivacy() => Navigate("WIN-007", "privacy", true);
        public void OpenTerms() => Navigate("WIN-007", "terms", true);

        public void Refresh()
        {
            if (disposed) return;
            int remaining = Math.Max(0, (int)Math.Ceiling((retryUntil - DateTime.UtcNow).TotalSeconds));
            if (errorKey == "rate_limited" && remaining == 0) errorKey = "";
            Model = new WelcomeViewModel
            {
                Language = language, LanguagePreference = languagePreference,
                View = view, Email = email, IsBusy = inFlight != null,
                IsOffline = !online, ShowPassword = showPassword,
                CanSubmit = inFlight == null && online && remaining == 0,
                ErrorKey = errorKey,
                ErrorText = string.IsNullOrEmpty(errorKey) ? "" : WelcomeCopy.Get(errorKey, language),
                StatusText = inFlight != null ? WelcomeCopy.Get("signing_in", language) : "",
                RetryAfterSeconds = remaining
            };
            if (remaining > 0 && errorKey == "rate_limited")
                Model.ErrorText += " (" + remaining + " s)";
            Changed?.Invoke(Model);
        }

        bool providerRequest;

        async Task AuthenticateAsync(bool google)
        {
            var operation = new CancellationTokenSource();
            inFlight = operation;
            providerRequest = google;
            errorKey = "";
            Refresh();
            AuthOutcome outcome;
            try
            {
                outcome = google
                    ? await service.SignInGoogleAsync(operation.Token)
                    : await service.SignInEmailAsync(email, password, operation.Token);
            }
            catch (OperationCanceledException) { outcome = new AuthOutcome(AuthStatus.Cancelled); }
            catch (Exception)
            {
                // Provider exceptions can include sensitive request details; never log them.
                outcome = new AuthOutcome(AuthStatus.ProviderError);
            }

            bool ownsRequest = ReferenceEquals(inFlight, operation);
            if (ownsRequest) inFlight = null;
            bool cancelled = operation.IsCancellationRequested;
            operation.Dispose();
            if (disposed || !ownsRequest || cancelled) return;
            providerRequest = false;
            ClearPassword();
            if (outcome == null) outcome = new AuthOutcome(AuthStatus.ProviderError);
            switch (outcome.Status)
            {
                case AuthStatus.Success:
                    if (!IsAuthorizedCheckpoint(outcome.AuthorizedDestination))
                    {
                        errorKey = "provider_error";
                        break;
                    }
                    Refresh();
                    NavigationRequested?.Invoke(new WelcomeNavigation(outcome.AuthorizedDestination,
                        outcome.Context, false, true));
                    return;
                case AuthStatus.Cancelled:
                    view = WelcomeView.Welcome;
                    errorKey = "";
                    break;
                case AuthStatus.Unavailable: errorKey = "unavailable"; break;
                case AuthStatus.Offline: errorKey = "offline"; break;
                case AuthStatus.InvalidCredentials:
                    errorKey = "invalid";
                    view = WelcomeView.EmailSignIn;
                    break;
                case AuthStatus.RateLimited:
                    errorKey = "rate_limited";
                    retryUntil = DateTime.UtcNow.AddSeconds(Math.Max(1, outcome.RetryAfterSeconds));
                    break;
                default:
                    errorKey = "provider_error";
                    if (google) view = WelcomeView.Welcome;
                    break;
            }
            Refresh();
        }

        bool CanStart()
        {
            if (disposed || inFlight != null) return false;
            if (!online) { errorKey = "offline"; Refresh(); return false; }
            if (retryUntil > DateTime.UtcNow) { errorKey = "rate_limited"; Refresh(); return false; }
            return true;
        }

        Task ValidationFailure(string key)
        {
            errorKey = key;
            view = WelcomeView.EmailSignIn;
            Refresh();
            return Task.CompletedTask;
        }

        void Navigate(string target, string context, bool readOnly = false)
        {
            if (disposed || inFlight != null) return;
            ClearPassword();
            Refresh();
            NavigationRequested?.Invoke(new WelcomeNavigation(target, context, readOnly));
        }

        void CancelRequest()
        {
            var operation = inFlight;
            inFlight = null;
            providerRequest = false;
            if (operation != null) operation.Cancel();
            // The awaiting operation owns disposal and ignores any late completion.
        }

        void ClearPassword()
        {
            password = "";
            showPassword = false;
            PasswordCleared?.Invoke();
        }

        static bool IsEmailAddress(string value)
        {
            if (value.Length > 254) return false;
            try { return new MailAddress(value).Address == value; }
            catch (FormatException) { return false; }
        }

        static bool IsAuthorizedCheckpoint(string windowId)
        {
            switch (windowId)
            {
                case "WIN-001": case "WIN-006": case "WIN-007": case "WIN-008":
                case "WIN-009": case "WIN-010": case "WIN-011": case "WIN-012": case "WIN-013":
                    return true;
                default: return false;
            }
        }

        static string ResolveLanguage(string preference)
            => preference == "auto" ? (Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en") : preference;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            CancelRequest();
            ClearPassword();
            Changed = null;
            NavigationRequested = null;
            PasswordCleared = null;
        }
    }

    public static class WelcomeCopy
    {
        // Matches data/windows/WIN-002-welcome-sign-in.json; extra keys are native
        // controls and truthful availability messages for this unconfigured build.
        static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            { "title", new[] { "WELCOME", "BIENVENIDA" } },
            { "subtitle", new[] { "Choose how to enter", "Elige cómo entrar" } },
            { "email_entry", new[] { "SIGN IN WITH EMAIL", "ENTRAR CON CORREO" } },
            { "google", new[] { "Continue with Google", "Continuar con Google" } },
            { "apple", new[] { "Continue with Apple", "Continuar con Apple" } },
            { "new_here", new[] { "New here?", "¿Primera vez aquí?" } },
            { "create", new[] { "CREATE ACCOUNT", "CREAR CUENTA" } },
            { "privacy", new[] { "Privacy", "Privacidad" } },
            { "terms", new[] { "Terms", "Términos" } },
            { "back", new[] { "Back", "Volver" } },
            { "cancel", new[] { "Cancel", "Cancelar" } },
            { "close", new[] { "Close", "Cerrar" } },
            { "email_title", new[] { "SIGN IN", "INICIAR SESIÓN" } },
            { "email_label", new[] { "Email address", "Correo electrónico" } },
            { "password_label", new[] { "Password", "Contraseña" } },
            { "submit", new[] { "SIGN IN", "INICIAR SESIÓN" } },
            { "forgot", new[] { "Forgot password?", "¿Olvidaste tu contraseña?" } },
            { "show_password", new[] { "Show password", "Mostrar contraseña" } },
            { "hide_password", new[] { "Hide password", "Ocultar contraseña" } },
            { "signing_in", new[] { "Signing in…", "Iniciando sesión…" } },
            { "offline", new[] { "Connect to the internet to sign in.", "Conéctate a internet para iniciar sesión." } },
            { "invalid", new[] { "We couldn't sign you in. Check your details and try again.", "No pudimos iniciar sesión. Revisa tus datos e inténtalo de nuevo." } },
            { "provider_error", new[] { "Sign-in couldn't be completed. Try again.", "No se pudo completar el acceso. Inténtalo de nuevo." } },
            { "expired", new[] { "Your session ended. Sign in again.", "Tu sesión terminó. Vuelve a iniciar sesión." } },
            { "rate_limited", new[] { "Wait a moment before trying again.", "Espera un momento antes de volver a intentarlo." } },
            { "required_email", new[] { "Enter your email address.", "Introduce tu correo electrónico." } },
            { "invalid_email", new[] { "Enter a valid email address.", "Introduce un correo electrónico válido." } },
            { "required_password", new[] { "Enter your password.", "Introduce tu contraseña." } },
            { "retry", new[] { "Try again", "Reintentar" } },
            { "unavailable", new[] { "Sign-in is not available in this preview yet.", "El inicio de sesión aún no está disponible en esta vista previa." } },
            { "future_window", new[] { "This area is not available in this preview yet.", "Esta área aún no está disponible en esta vista previa." } },
            { "language", new[] { "Language", "Idioma" } },
            { "automatic", new[] { "Automatic", "Automático" } },
            { "preview", new[] { "Preview", "Vista previa" } }
        };

        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }
}
