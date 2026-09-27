using System;
using UnityEngine;

namespace SoloGym
{
    /// <summary>Local art-review choices shared by Studio and Home; never an appearance save.</summary>
    public static class AutoSpriteSession
    {
        static bool initialized;
        // First version uses one front-facing presentation on both screens.
        public const string View = "front";
        public static string Language = "es";
        public static bool Paused;
        public static bool CaptureTaken;
        public static bool SmokeStarted;
        public static float Seconds { get; private set; }
        static float clockStart;

        public static bool Requested => Argument("-sologym-avatar-renderer") == "autosprite"
            && (Application.isEditor || HasArgument("-sologym-review"));

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            Language = Argument("-sologym-locale") ?? "es";
            Paused = HasArgument("-sologym-autosprite-still");
            clockStart = Time.unscaledTime;
        }

        public static float PlaybackSeconds => Seconds + (Paused ? 0 : Time.unscaledTime - clockStart);

        public static void TogglePause()
        {
            Seconds = PlaybackSeconds;
            Paused = !Paused;
            clockStart = Time.unscaledTime;
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
