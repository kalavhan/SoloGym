using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace SoloGym
{
    public enum RegistrationStatus { Ready, Created, SetupRequired, Unavailable, Offline, Rejected, PasswordPolicy, RateLimited, Cancelled, Unknown }
    public enum RegistrationPhase { Idle, CheckingSetup, Creating }

    public sealed class RegistrationResult
    {
        public RegistrationStatus Status { get; }
        public string Permit { get; }
        public int RetryAfterSeconds { get; }
        public RegistrationResult(RegistrationStatus status, string permit = null, int retryAfterSeconds = 0)
        { Status = status; Permit = permit; RetryAfterSeconds = Math.Max(0, retryAfterSeconds); }
    }

    /// <summary>
    /// The future trusted adapter must authorize age/region/consent before issuing a permit,
    /// then validate that permit again during creation. A UI checkbox is never authorization.
    /// Neither call may log credentials or persist them in client preferences.
    /// </summary>
    public interface IAccountRegistrationService
    {
        Task<RegistrationResult> CheckSetupAsync(CancellationToken token);
        Task<RegistrationResult> CreateAsync(string email, string password, string permit, CancellationToken token);
    }

    /// <summary>No configured policy/receipt service: never send credentials or create an identity.</summary>
    public sealed class UnconfiguredAccountRegistrationService : IAccountRegistrationService
    {
        public Task<RegistrationResult> CheckSetupAsync(CancellationToken token)
        { token.ThrowIfCancellationRequested(); return Task.FromResult(new RegistrationResult(RegistrationStatus.SetupRequired)); }
        public Task<RegistrationResult> CreateAsync(string email, string password, string permit, CancellationToken token)
        { token.ThrowIfCancellationRequested(); return Task.FromResult(new RegistrationResult(RegistrationStatus.Unavailable)); }
    }

    /// <summary>Memory-only form state; independent of sign-in, with no Home/profile authorization.</summary>
    public sealed class AccountRegistrationController : IDisposable
    {
        readonly IAccountRegistrationService service;
        CancellationTokenSource operation;
        DateTime retryUntil;
        bool disposed, online = true;
        public RegistrationPhase Phase { get; private set; }
        public string ErrorKey { get; private set; } = "";
        public bool Created { get; private set; }
        public bool Busy => operation != null;
        public int RetrySeconds => Math.Max(0, (int)Math.Ceiling((retryUntil - DateTime.UtcNow).TotalSeconds));
        public bool CanSubmit => !disposed && online && !Busy && !Created && RetrySeconds == 0;
        public event Action Changed;
        public event Action SecretsCleared;

        public AccountRegistrationController(IAccountRegistrationService service = null)
        { this.service = service ?? new UnconfiguredAccountRegistrationService(); }

        public Task RegisterAsync(string email, string password, string confirmation)
        {
            if (!CanSubmit) return Task.CompletedTask;
            email = (email ?? "").Trim();
            if (email.Length == 0) return Invalid("required_email");
            try
            {
                var parsed = new MailAddress(email);
                if (parsed.Address != email || !email.Contains("@")) return Invalid("invalid_email");
            }
            catch (FormatException) { return Invalid("invalid_email"); }
            if (string.IsNullOrEmpty(password)) return Invalid("required_password");
            if (string.IsNullOrEmpty(confirmation)) return Invalid("required_confirmation");
            if (!string.Equals(password, confirmation, StringComparison.Ordinal)) return Invalid("mismatch");
            return RegisterValidatedAsync(email, password);
        }

        Task Invalid(string key) { ErrorKey = key; Changed?.Invoke(); return Task.CompletedTask; }
        async Task RegisterValidatedAsync(string email, string password)
        {
            var current = new CancellationTokenSource();
            operation = current; ErrorKey = ""; Phase = RegistrationPhase.CheckingSetup; Changed?.Invoke();
            RegistrationResult result = null;
            bool submitted = false;
            try
            {
                result = await service.CheckSetupAsync(current.Token);
                if (!Owns(current)) return;
                if (result?.Status == RegistrationStatus.Ready)
                {
                    if (string.IsNullOrWhiteSpace(result.Permit)) result = new RegistrationResult(RegistrationStatus.Unavailable);
                    else
                    {
                        Phase = RegistrationPhase.Creating; Changed?.Invoke();
                        if (!Owns(current)) return;
                        submitted = true;
                        result = await service.CreateAsync(email, password, result.Permit, current.Token);
                    }
                }
                else if (result?.Status == RegistrationStatus.Created)
                    result = new RegistrationResult(RegistrationStatus.Unavailable); // preflight cannot create an account
            }
            catch (OperationCanceledException) { result = new RegistrationResult(submitted ? RegistrationStatus.Unknown : RegistrationStatus.Cancelled); }
            catch (Exception) { result = new RegistrationResult(submitted ? RegistrationStatus.Unknown : RegistrationStatus.Unavailable); }
            finally
            {
                password = null;
                bool owned = Owns(current);
                if (ReferenceEquals(operation, current)) operation = null;
                current.Dispose();
                if (owned)
                {
                    Phase = RegistrationPhase.Idle;
                    SecretsCleared?.Invoke();
                    Apply(result, submitted);
                    Changed?.Invoke();
                }
            }
        }

        bool Owns(CancellationTokenSource current) => !disposed && ReferenceEquals(operation, current) && !current.IsCancellationRequested;
        void Apply(RegistrationResult result, bool submitted)
        {
            switch (result?.Status)
            {
                case RegistrationStatus.Created: Created = submitted; ErrorKey = submitted ? "" : "unavailable"; break;
                case RegistrationStatus.SetupRequired: ErrorKey = "setup"; break;
                case RegistrationStatus.PasswordPolicy: ErrorKey = "policy"; break;
                case RegistrationStatus.Rejected: ErrorKey = "rejected"; break;
                case RegistrationStatus.Offline: ErrorKey = submitted ? "unknown" : "offline"; break;
                case RegistrationStatus.RateLimited:
                    retryUntil = DateTime.UtcNow.AddSeconds(Math.Max(1, result.RetryAfterSeconds)); ErrorKey = "rate"; break;
                case RegistrationStatus.Cancelled: ErrorKey = submitted ? "unknown" : ""; break;
                case RegistrationStatus.Unknown: ErrorKey = "unknown"; break;
                default: ErrorKey = submitted ? "unknown" : "unavailable"; break;
            }
        }

        public void Cancel()
        {
            bool mayHaveSubmitted = Busy && Phase == RegistrationPhase.Creating;
            var previous = operation; operation = null;
            // The continuation owns disposal, even if the service ignores cancellation.
            previous?.Cancel();
            Phase = RegistrationPhase.Idle;
            if (mayHaveSubmitted) ErrorKey = "unknown";
            SecretsCleared?.Invoke(); Changed?.Invoke();
        }
        public void SetOnline(bool value)
        {
            if (online == value) return;
            online = value;
            if (!online) { Cancel(); if (ErrorKey != "unknown") ErrorKey = "offline"; }
            else if (ErrorKey == "offline") ErrorKey = "";
            Changed?.Invoke();
        }
        public void Refresh()
        {
            if (disposed) return;
            if (ErrorKey == "rate" && RetrySeconds == 0) ErrorKey = "";
            Changed?.Invoke();
        }
        public void Reset()
        {
            Cancel(); Created = false;
            if (ErrorKey != "unknown" && RetrySeconds == 0) ErrorKey = online ? "" : "offline";
            Changed?.Invoke();
        }
        public void Dispose()
        {
            if (disposed) return;
            Cancel(); disposed = true; Changed = null; SecretsCleared = null;
        }
    }
}
