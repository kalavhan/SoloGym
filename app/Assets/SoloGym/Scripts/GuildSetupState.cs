using System;

namespace SoloGym
{
    public enum GuildSetupStep { AgeCountry, Character, Consent }

    /// <summary>A private, memory-only preparation draft. It never issues eligibility or registration permits.</summary>
    public sealed class GuildSetupController : IDisposable
    {
        readonly OnboardingController origin = new OnboardingController();
        public OnboardingViewModel Origin => origin.Model;
        public OnboardingOption[] Countries => origin.Countries;
        public GuildSetupStep Step { get; private set; }
        public string Gender { get; private set; } = "male";
        public string Body { get; private set; } = "medium";
        public string CharacterId => Gender + "-" + Body;
        public string ErrorKey { get; private set; } = "";
        public event Action Changed;
        public event Action ExitRequested;
        public event Action EligibilityChanged;
        public GuildSetupController() { origin.Changed += _ => Changed?.Invoke(); }
        public void SetLanguage(string language) => origin.SetLanguage(language);
        public void SetAge(string value)
        {
            if (Step != GuildSetupStep.AgeCountry) return;
            if (Origin.AgeText != (value ?? "")) EligibilityChanged?.Invoke();
            ErrorKey = ""; origin.SetAge(value);
        }
        public void SelectCountry(string code)
        {
            if (Step != GuildSetupStep.AgeCountry) return;
            string before = Origin.CountryCode;
            ErrorKey = ""; origin.SelectCountry(code);
            if (before != Origin.CountryCode) EligibilityChanged?.Invoke();
        }
        public bool ChooseGender(string value)
        {
            if (Step != GuildSetupStep.Character || (value != "male" && value != "female")) return false;
            Gender = value; ErrorKey = ""; Changed?.Invoke(); return true;
        }
        public bool ChooseBody(string value)
        {
            if (Step != GuildSetupStep.Character || (value != "skinny" && value != "medium" && value != "fat" && value != "muscular")) return false;
            Body = value; ErrorKey = ""; Changed?.Invoke(); return true;
        }
        public bool TryGetReviewAudience(out bool teen)
        {
            teen = false;
            if (origin.ValidateDraft().Length != 0) return false;
            teen = origin.IsTeenAudience();
            return true;
        }
        public void Continue(bool appearanceAvailable)
        {
            if (Step == GuildSetupStep.Consent) return;
            ErrorKey = origin.ValidateDraft();
            if (ErrorKey.Length != 0) { Step = GuildSetupStep.AgeCountry; Changed?.Invoke(); return; }
            if (Step == GuildSetupStep.AgeCountry) Step = GuildSetupStep.Character;
            else if (!appearanceAvailable) ErrorKey = "appearance_unavailable";
            else Step = GuildSetupStep.Consent;
            Changed?.Invoke();
        }
        public void Back()
        {
            ErrorKey = "";
            if (Step == GuildSetupStep.AgeCountry) { Discard(); ExitRequested?.Invoke(); return; }
            Step = Step == GuildSetupStep.Character ? GuildSetupStep.AgeCountry : GuildSetupStep.Character;
            Changed?.Invoke();
        }
        public void Discard()
        {
            Step = GuildSetupStep.AgeCountry; Gender = "male"; Body = "medium"; ErrorKey = "";
            origin.Decline(); EligibilityChanged?.Invoke(); Changed?.Invoke();
        }
        public void Dispose() { origin.Dispose(); Changed = null; ExitRequested = null; EligibilityChanged = null; }
    }
}
