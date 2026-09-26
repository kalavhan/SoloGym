using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if SOLOGYM_FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
#endif

namespace SoloGym
{
    /// <summary>Firebase identity only. Profile, age and consent checkpoints remain separate.</summary>
    public sealed class FirebaseWelcomeAuthService : IWelcomeAuthService
    {
        [Serializable]
        public sealed class Settings
        {
            public bool enabled;
            public string projectId;
            public string androidAppId;
            public string iosAppId;
            public string apiKey;
            public string googleWebClientId;
        }

        readonly Settings settings;
        readonly SemaphoreSlim requests = new SemaphoreSlim(1, 1);
#if SOLOGYM_FIREBASE_AUTH
        Task<FirebaseAuth> initialization;
#endif
        public FirebaseWelcomeAuthService()
        {
            try
            {
                var asset = Resources.Load<TextAsset>("Auth/FirebaseConfig");
                settings = asset == null ? new Settings() : JsonUtility.FromJson<Settings>(asset.text);
            }
            catch (Exception) { settings = new Settings(); }
        }

        public async Task<AuthOutcome> SignInEmailAsync(string email, string password, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
#if SOLOGYM_FIREBASE_AUTH
            await requests.WaitAsync(token);
            FirebaseAuth auth = null;
            try
            {
                auth = await GetAuthAsync();
                token.ThrowIfCancellationRequested();
                if (auth == null) return new AuthOutcome(AuthStatus.Unavailable);
                // Firebase owns secure session persistence; never copy credentials into PlayerPrefs.
                AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                if (token.IsCancellationRequested) { auth.SignOut(); return new AuthOutcome(AuthStatus.Cancelled); }
                return Outcome(result.User);
            }
            catch (OperationCanceledException) { return new AuthOutcome(AuthStatus.Cancelled); }
            catch (Exception error) { return ErrorOutcome(error); }
            finally { requests.Release(); }
#else
            await Task.CompletedTask;
            return new AuthOutcome(AuthStatus.Unavailable);
#endif
        }

        public async Task<AuthOutcome> SignInGoogleAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
#if SOLOGYM_FIREBASE_AUTH && UNITY_ANDROID && !UNITY_EDITOR
            await requests.WaitAsync(token);
            FirebaseAuth auth = null;
            try
            {
                auth = await GetAuthAsync();
                token.ThrowIfCancellationRequested();
                if (auth == null || string.IsNullOrWhiteSpace(settings.googleWebClientId))
                    return new AuthOutcome(AuthStatus.Unavailable);
                var response = await AndroidGoogleIdentity.SignInAsync(settings.googleWebClientId, token);
                token.ThrowIfCancellationRequested();
                if (response.Status != AuthStatus.Success) return new AuthOutcome(response.Status);
                using (var credential = GoogleAuthProvider.GetCredential(response.IdToken, null))
                {
                    AuthResult result = await auth.SignInAndRetrieveDataWithCredentialAsync(credential);
                    if (token.IsCancellationRequested) { auth.SignOut(); return new AuthOutcome(AuthStatus.Cancelled); }
                    return Outcome(result.User);
                }
            }
            catch (OperationCanceledException) { return new AuthOutcome(AuthStatus.Cancelled); }
            catch (Exception error) { return ErrorOutcome(error); }
            finally { requests.Release(); }
#else
            await Task.CompletedTask;
            return new AuthOutcome(AuthStatus.Unavailable);
#endif
        }

#if SOLOGYM_FIREBASE_AUTH
        Task<FirebaseAuth> GetAuthAsync()
        {
            // No guessed project, anonymous account, emulator or production fallback.
            if (settings == null || !settings.enabled || string.IsNullOrWhiteSpace(settings.projectId)
                || string.IsNullOrWhiteSpace(AppId) || string.IsNullOrWhiteSpace(settings.apiKey))
                return Task.FromResult<FirebaseAuth>(null);
            if (initialization == null || initialization.IsFaulted || initialization.IsCanceled)
                initialization = InitializeAsync();
            return initialization;
        }

        async Task<FirebaseAuth> InitializeAsync()
        {
            var availability = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (availability != DependencyStatus.Available) return null;
            // Explicit options let an unconfigured preview launch without google-services.json.
            var app = FirebaseApp.GetInstance("SoloGymIdentity") ?? FirebaseApp.Create(new AppOptions
            {
                ApiKey = settings.apiKey,
                AppId = AppId,
                ProjectId = settings.projectId
            }, "SoloGymIdentity");
            return FirebaseAuth.GetAuth(app);
        }

        string AppId
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return settings.iosAppId;
#else
                return settings.androidAppId;
#endif
            }
        }

        static AuthOutcome Outcome(FirebaseUser user)
        {
            if (user == null || user.IsAnonymous) return new AuthOutcome(AuthStatus.ProviderError);
            // A Firebase identity is not proof that SoloGym consent/profile setup is complete.
            return new AuthOutcome(AuthStatus.Success, "WIN-006", "firebase_identity_requires_onboarding");
        }

        static AuthOutcome ErrorOutcome(Exception error)
        {
            var firebase = error.GetBaseException() as FirebaseException;
            if (firebase == null) return new AuthOutcome(AuthStatus.ProviderError);
            var code = (AuthError)firebase.ErrorCode;
            if (code == AuthError.NetworkRequestFailed) return new AuthOutcome(AuthStatus.Offline);
            if (code == AuthError.TooManyRequests) return new AuthOutcome(AuthStatus.RateLimited, retryAfterSeconds: 60);
            // Preserve email-enumeration protection: never reveal whether an email exists.
            if (code == AuthError.WrongPassword || code == AuthError.UserNotFound || code == AuthError.InvalidEmail
                || code == AuthError.InvalidCredential || code == AuthError.UserDisabled)
                return new AuthOutcome(AuthStatus.InvalidCredentials);
            if (code == AuthError.OperationNotAllowed || code == AuthError.InvalidApiKey)
                return new AuthOutcome(AuthStatus.Unavailable);
            return new AuthOutcome(AuthStatus.ProviderError);
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    internal static class AndroidGoogleIdentity
    {
        internal sealed class Result
        {
            public AuthStatus Status;
            public string IdToken;
        }

        sealed class Callback : AndroidJavaProxy
        {
            readonly TaskCompletionSource<Result> completion;
            readonly SynchronizationContext context;
            public Callback(TaskCompletionSource<Result> completion, SynchronizationContext context)
                : base("com.kalavhan.sologym.auth.GoogleIdentityBridge$Callback")
            { this.completion = completion; this.context = context; }
            [UnityEngine.Scripting.Preserve]
            public void onResult(string status, string idToken)
            {
                context.Post(_ => completion.TrySetResult(new Result
                {
                    Status = status == "success" ? AuthStatus.Success : status == "cancelled" ? AuthStatus.Cancelled
                        : status == "unavailable" ? AuthStatus.Unavailable : AuthStatus.ProviderError,
                    IdToken = idToken
                }), null);
            }
        }

        internal static async Task<Result> SignInAsync(string webClientId, CancellationToken token)
        {
            var completion = new TaskCompletionSource<Result>();
            var context = SynchronizationContext.Current;
            if (context == null) return new Result { Status = AuthStatus.Unavailable };
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var bridge = new AndroidJavaObject("com.kalavhan.sologym.auth.GoogleIdentityBridge"))
            {
                var callback = new Callback(completion, context);
                bool active = true;
                using (token.Register(() => context.Post(_ =>
                {
                    if (!active) return;
                    bridge.Call("cancel");
                    completion.TrySetResult(new Result { Status = AuthStatus.Cancelled });
                }, null)))
                {
                    bridge.Call("signIn", activity, webClientId, callback);
                    var result = await completion.Task;
                    active = false;
                    GC.KeepAlive(callback);
                    return result;
                }
            }
        }
    }
#endif
}
