using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SoloGym.Training;
using UnityEngine;

namespace SoloGym
{
    /// <summary>Loads the bundled training content once (Resources/Training/Rules).</summary>
    public static class TrainingContent
    {
        static TrainingEngine engine;
        public static TrainingEngine Engine
        {
            get
            {
                if (engine != null) return engine;
                string Read(string name)
                {
                    var asset = Resources.Load<TextAsset>("Training/Rules/" + name);
                    if (asset == null) throw new InvalidOperationException("Missing training content: " + name);
                    return asset.text;
                }
                engine = new TrainingEngine(
                    JsonUtility.FromJson<ExerciseCatalogData>(Read("exercises")),
                    JsonUtility.FromJson<TemplateData>(Read("templates")),
                    JsonUtility.FromJson<TrainingRulesData>(Read("rules")));
                return engine;
            }
        }
    }

    /// <summary>
    /// The signed-in account for this app process. Profile, journal, fasting and room layout
    /// are kept in a per-account folder on this device (test build: no cloud sync yet).
    /// </summary>
    public sealed class AccountSession
    {
        public static AccountSession Current { get; private set; }
        public static event Action Changed;
        public IdentityUser User { get; }
        public UserProfile Profile { get; private set; }
        public string Folder { get; }
        public string LoadError { get; private set; }
        public string ProfilePath => Path.Combine(Folder, "profile.json");
        public string JournalPath => Path.Combine(Folder, "journal-v1.json");
        public string FastingPath => Path.Combine(Folder, "fasting-v1.json");
        public string LayoutPath => Path.Combine(Folder, "training-hall-layout-v1.json");
        public bool SetupComplete => Profile != null && Profile.setupComplete && Profile.MissingSetup().Length == 0;

        AccountSession(IdentityUser user, string root)
        {
            User = user;
            Folder = Path.Combine(root, "accounts", Hash(user.UserId));
            Load();
        }

        public static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? "")).Take(12).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        public static AccountSession Begin(IdentityUser user, string root = null)
        {
            if (user == null || string.IsNullOrEmpty(user.UserId)) throw new ArgumentException("A signed-in identity is required.");
            if (Current != null && Current.User.UserId == user.UserId) return Current;
            Current = new AccountSession(user, root ?? Application.persistentDataPath);
            Changed?.Invoke();
            return Current;
        }

        public static void End(bool signOut = true)
        {
            Current = null;
            if (signOut) FirebaseWelcomeAuthService.Shared.SignOut();
            Changed?.Invoke();
        }

        void Load()
        {
            LoadError = null;
            try
            {
                if (File.Exists(ProfilePath))
                {
                    if (new FileInfo(ProfilePath).Length > 200000) throw new IOException("Profile file is oversized.");
                    var loaded = JsonUtility.FromJson<UserProfile>(File.ReadAllText(ProfilePath));
                    if (loaded == null || loaded.version != UserProfile.CurrentVersion || loaded.accountId != User.UserId)
                        throw new ArgumentException("Saved profile belongs to another account or version.");
                    loaded.Normalize(); Profile = loaded;
                }
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                // Keep the unreadable file untouched; setup starts from a fresh in-memory profile.
                LoadError = e.Message; Profile = null;
            }
            if (Profile == null) Profile = new UserProfile { accountId = User.UserId, createdUtc = UserProfile.NowUtc() };
            if (!string.IsNullOrEmpty(User.Email)) Profile.email = User.Email;
            if (!string.IsNullOrEmpty(User.DisplayName)) Profile.displayName = User.DisplayName;
        }

        public bool SaveProfile(out string error)
        {
            error = null;
            try
            {
                Profile.Normalize(); Profile.updatedUtc = UserProfile.NowUtc();
                Directory.CreateDirectory(Folder);
                new JournalFileStorage(ProfilePath).Write(JsonUtility.ToJson(Profile, true));
                LoadError = null; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { error = e.Message; return false; }
        }

        public LiveTrainingPlans CreatePlans() => new LiveTrainingPlans(TrainingContent.Engine, Profile.ToTrainingInput());

        /// <summary>Opens this account's journal and fills scheduled days (today through next week).</summary>
        public WorkoutJournal OpenJournal(DateTime today, IJournalStorage storage = null)
        {
            var plans = CreatePlans();
            var journal = new WorkoutJournal(plans, storage ?? new JournalFileStorage(JournalPath), LiveTrainingPlans.AccountProfile, today);
            var days = Profile.weekdays;
            journal.DefaultProfile = day => plans.DefaultProfileFor(day, days);
            journal.DefaultMinutes = Profile.sessionMinutes;
            journal.Load(true);
            if (journal.Loaded)
            {
                DateTime start = today;
                if (!string.IsNullOrEmpty(Profile.planStartDate)) DateTime.TryParseExact(Profile.planStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
                try { journal.EnsureScheduled(plans.Schedule(days, today, WorkoutJournal.Monday(today).AddDays(13)), Profile.sessionMinutes, start); }
                catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException) { Debug.LogWarning("Schedule not extended: " + e.Message); }
            }
            return journal;
        }

        /// <summary>Accept a (re)generated plan: future untouched planned days follow the new setup.</summary>
        public bool AcceptPlan(DateTime today, out string error)
        {
            Profile.setupComplete = true; Profile.planAcceptedUtc = UserProfile.NowUtc();
            if (string.IsNullOrEmpty(Profile.planStartDate)) Profile.planStartDate = WorkoutJournal.Date(today);
            if (!SaveProfile(out error)) return false;
            try
            {
                var plans = CreatePlans();
                var journal = new WorkoutJournal(plans, new JournalFileStorage(JournalPath), LiveTrainingPlans.AccountProfile, today);
                journal.Load(true);
                if (!journal.Loaded)
                {
                    // An unreadable or older journal is preserved beside the new one.
                    if (File.Exists(JournalPath)) File.Copy(JournalPath, JournalPath + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), true);
                    File.Delete(JournalPath);
                    journal = new WorkoutJournal(plans, new JournalFileStorage(JournalPath), LiveTrainingPlans.AccountProfile, today);
                    journal.Load(true);
                }
                journal.ReplaceFuturePlan(plans.Schedule(Profile.weekdays, today, WorkoutJournal.Monday(today).AddDays(13)), Profile.sessionMinutes);
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException || e is InvalidOperationException)
            { error = e.Message; return false; }
        }

        /// <summary>Removes this account's profile, journal, fasting records and room layout from this device.</summary>
        public bool DeleteLocalData(out string error)
        {
            error = null;
            try { if (Directory.Exists(Folder)) Directory.Delete(Folder, true); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { error = e.Message; return false; }
        }

        public void ApplyCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            Profile.characterId = characterId; SaveProfile(out _);
        }
    }

    /// <summary>
    /// Registration through Firebase after the on-device setup is complete. Test build: age,
    /// country and consent are validated on the device; a trusted server check is still required
    /// before a public release (see docs/product/sologym-product-blueprint.md).
    /// </summary>
    public sealed class FirebaseAccountRegistrationService : IAccountRegistrationService
    {
        readonly Func<bool> setupReady;
        readonly FirebaseWelcomeAuthService identity;
        public FirebaseAccountRegistrationService(Func<bool> onDeviceSetupReady, FirebaseWelcomeAuthService service = null)
        { setupReady = onDeviceSetupReady ?? (() => false); identity = service ?? FirebaseWelcomeAuthService.Shared; }
        public Task<RegistrationResult> CheckSetupAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!identity.Configured) return Task.FromResult(new RegistrationResult(RegistrationStatus.Unavailable));
            return Task.FromResult(setupReady() ? new RegistrationResult(RegistrationStatus.Ready, "on-device-setup") : new RegistrationResult(RegistrationStatus.SetupRequired));
        }
        public Task<RegistrationResult> CreateAsync(string email, string password, string permit, CancellationToken token)
        {
            if (permit != "on-device-setup" || !setupReady()) return Task.FromResult(new RegistrationResult(RegistrationStatus.SetupRequired));
            return identity.CreateUserAsync(email, password, token);
        }
    }
}
