using System;
using UnityEngine;

namespace SoloGym
{
    [Serializable]
    public sealed class ModularAppearance
    {
        public string bodyId = "male-medium", hairId = "close-crop";
        public bool torso = true, legs = true;
        public ModularAppearance Clone() => JsonUtility.FromJson<ModularAppearance>(JsonUtility.ToJson(this));
        public void Normalize()
        {
            // A retired selection keeps its nearest available build.
            if (bodyId == "male-obese") bodyId = "male-overweight";
            if (bodyId == "female-obese") bodyId = "female-overweight";
            if (Array.IndexOf(AutoSpriteSession.BodyIds, bodyId) < 0) bodyId = "male-medium";
            if (hairId != "none" && hairId != "close-crop" && hairId != "short-sweep") hairId = "close-crop";
        }
    }

    /// <summary>Static MVP appearance, saved locally only when the player confirms.</summary>
    public static class AutoSpriteSession
    {
        public const string AppearanceKey = "SoloGym.Appearance.Modular.v1";
        public static readonly string[] Builds = { "slim", "medium", "overweight", "muscular" };
        public static readonly string[] BodyIds = { "male-slim", "male-medium", "male-overweight", "male-muscular",
            "female-slim", "female-medium", "female-overweight", "female-muscular" };
        public static string Language = "es";
        public static bool CaptureTaken, SmokeStarted;
        static bool initialized;
        static ModularAppearance saved;
        public static ModularAppearance Appearance { get { Initialize(); return saved.Clone(); } }
        // The modular character is the default; explicit old renderers remain historical proofs.
        public static bool Requested => Argument("-sologym-avatar-renderer") == null
            || Argument("-sologym-avatar-renderer") == "autosprite"
            || Argument("-sologym-avatar-renderer") == "modular";

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            string preference = Argument("-sologym-locale") ?? PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto");
            Language = preference == "en" || preference == "es" ? preference
                : Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
            ReloadAppearance();
        }

        public static void ReloadAppearance()
        {
            try { saved = JsonUtility.FromJson<ModularAppearance>(PlayerPrefs.GetString(AppearanceKey, "")); }
            catch (ArgumentException) { saved = null; }
            saved ??= new ModularAppearance();
            saved.Normalize();
        }

        public static void Save(ModularAppearance appearance)
        {
            saved = appearance.Clone(); saved.Normalize();
            PlayerPrefs.SetString(AppearanceKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
        }

        public static bool HasArgument(string key) => Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0;
        public static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
