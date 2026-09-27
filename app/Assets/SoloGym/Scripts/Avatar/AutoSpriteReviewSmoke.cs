using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Opt-in player checks of real Studio/Home navigation and atlas playback.</summary>
    public sealed class AutoSpriteReviewSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public string[] checks; }
        readonly List<string> checks = new List<string>();
        bool passed = true;
        string folder;

        public void Run(string capturePath)
        {
            DontDestroyOnLoad(gameObject);
            AutoSpriteSession.CaptureTaken = true;
            folder = Path.GetDirectoryName(Path.GetFullPath(capturePath ?? "artifacts/local/autosprite/review.png"));
            Directory.CreateDirectory(folder);
            StartCoroutine(CheckFlow());
        }

        void Check(bool ok, string label)
        {
            passed &= ok;
            checks.Add((ok ? "PASS " : "FAIL ") + label);
        }

        IEnumerator CheckFlow()
        {
            for (int i = 0; i < 8; i++) yield return null;
            var studio = FindFirstObjectByType<AutoSpriteStudioScreen>();
            Check(studio != null && studio.Avatar.IsLoaded, "configuration loads transparent atlas");
            if (studio == null || !studio.Avatar.IsLoaded) { Finish(); yield break; }
            Check(studio.Avatar.ViewId == "front" && studio.Avatar.FrameCount == 25, "configuration uses front atlas");
            if (AutoSpriteSession.Paused) studio.TogglePause();
            int before = studio.Avatar.FrameIndex;
            yield return new WaitForSecondsRealtime(.35f);
            Check(studio.Avatar.FrameIndex != before, "idle advances through provider frames");
            studio.TogglePause();
            yield return null;
            before = studio.Avatar.FrameIndex;
            yield return new WaitForSecondsRealtime(.2f);
            Check(studio.Avatar.FrameIndex == before, "pause freezes frame without changing pose");
            yield return Save("studio-front.png");
            studio.ContinueButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 15;
            HomeScreen home = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                home = FindFirstObjectByType<HomeScreen>();
                if (home != null && home.AutoSpriteAvatar != null) break;
            }
            Check(home != null && home.AutoSpriteAvatar != null && home.AutoSpriteAvatar.IsLoaded,
                "Continue opens real Home screen");
            if (home == null || home.AutoSpriteAvatar == null) { Finish(); yield break; }
            Check(home.AutoSpriteAvatar.ViewId == "front" && AutoSpriteSession.Paused,
                "Home uses front view and retains pause state");
            yield return Save("home-front.png");
            home.LanguageButton.onClick.Invoke();
            yield return null;
            Button automatic = null;
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                string label = button.GetComponentInChildren<Text>()?.text;
                if (label == "Use device language" || label == "Usar idioma del dispositivo") automatic = button;
            }
            Check(automatic != null, "Home exposes automatic language option");
            if (automatic != null) automatic.onClick.Invoke();
            yield return null;
            home.CharacterButton.onClick.Invoke();
            yield return null;
            studio = FindFirstObjectByType<AutoSpriteStudioScreen>();
            Check(studio != null && studio.Avatar.ViewId == "front", "Home character reopens front-facing configuration");
            if (studio != null)
            {
                studio.BackButton.onClick.Invoke();
                yield return null;
                Check(home.AutoSpriteAvatar.ViewId == "front", "return keeps Home front-facing");
                Check(home.LanguagePreference == "auto", "unchanged studio language preserves automatic Home preference");
            }
            Finish();
        }

        IEnumerator Save(string name)
        {
            for (int i = 0; i < 4; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
            Destroy(image);
        }

        void Finish()
        {
            string json = JsonUtility.ToJson(new Report { passed = passed, checks = checks.ToArray() }, true);
            File.WriteAllText(Path.Combine(folder, "navigation.smoke.json"), json);
            Debug.Log("SOLOGYM_AUTOSPRITE_SMOKE " + json);
            Application.Quit(passed ? 0 : 2);
        }
    }
}
