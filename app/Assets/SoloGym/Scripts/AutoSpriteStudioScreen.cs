using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Focused AutoSprite art review in the existing game studio composition.</summary>
    public sealed class AutoSpriteStudioScreen : MonoBehaviour
    {
        RectTransform root;
        Text title, note, viewLabel, hint, pauseLabel, continueLabel, languageLabel, backLabel, outfitLabel, footer;
        Button pause;
        Action back, next;
        string capture;
        bool es;
        Vector2 size;
        Rect safe;
        public AutoSpriteAvatarView Avatar { get; private set; }
        public Button ContinueButton { get; private set; }
        public Button BackButton { get; private set; }

        public void Initialize(string language, Action onBack, Action onContinue, string capturePath = null)
        {
            AutoSpriteSession.Initialize();
            AutoSpriteSession.Language = language == "en" ? "en" : "es";
            es = AutoSpriteSession.Language == "es";
            back = onBack; next = onContinue; capture = capturePath;
            var node = new GameObject("AutoSprite studio canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvas.pixelPerfect = true;
            root = SystemUI.Node("Studio 853x1844", node.transform, new Rect(0, 0, 853, 1844));
            if (FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Studio camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(2, 6, 16, 255);
                camera.cullingMask = 0;
            }
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
            SystemUI.Art(root, new Rect(0, 0, 853, 1844), "Art/PortalBackground-v1");
            SystemUI.Wordmark(root, new Rect(174, 149, 510, 112));
            SystemUI.Icon(root, new Rect(43, 47, 28, 34), "back");
            BackButton = SystemUI.Hit(root, new Rect(30, 20, 180, 100), () => back?.Invoke(), "Back to Home");
            backLabel = SystemUI.Caption(root, new Rect(85, 43, 120, 44), "", 29);
            languageLabel = SystemUI.Caption(root, new Rect(734, 43, 72, 44), "", 26, SystemUI.Theme.body);
            SystemUI.Hit(root, new Rect(690, 20, 130, 100), ToggleLanguage);
            note = SystemUI.Caption(root, new Rect(80, 280, 693, 28), "", 20, SystemUI.Theme.body, SystemUI.Theme.accent);
            title = SystemUI.Caption(root, new Rect(90, 326, 673, 64), "", 58, SystemUI.Theme.headingBold);
            Avatar = gameObject.AddComponent<AutoSpriteAvatarView>();
            Avatar.Mount(root, new Rect(164, 408, 525, 728));
            hint = SystemUI.Caption(root, new Rect(90, 1150, 673, 32), "", 20, SystemUI.Theme.body, SystemUI.Theme.accent);
            SystemUI.Panel(root, new Rect(91, 1220, 671, 254), PanelStyle.Glass);
            viewLabel = SystemUI.Caption(root, new Rect(115, 1244, 623, 38), "", 28, SystemUI.Theme.headingBold);
            outfitLabel = SystemUI.Caption(root, new Rect(117, 1295, 619, 40), "", 23, SystemUI.Theme.body);
            pause = SystemUI.Button(root, new Rect(255, 1386, 343, 58), "", TogglePause);
            pauseLabel = pause.GetComponentInChildren<Text>();
            ContinueButton = SystemUI.Button(root, PortalFrameLayout.PrimaryRect, "", () => next?.Invoke(), PanelStyle.Primary);
            continueLabel = ContinueButton.GetComponentInChildren<Text>();
            footer = SystemUI.Caption(root, new Rect(105, 1654, 643, 46), "", 18, SystemUI.Theme.body, SystemUI.Theme.muted);
            SystemUI.Icon(root, new Rect(421, 1728, 10, 25), "sigil");
            Refresh(); Fit();
            if (AutoSpriteSession.HasArgument("-sologym-autosprite-smoke") && !AutoSpriteSession.SmokeStarted)
            {
                AutoSpriteSession.SmokeStarted = true;
                new GameObject("AutoSprite review checks").AddComponent<AutoSpriteReviewSmoke>().Run(capturePath);
            }
            else if (capture != null && !AutoSpriteSession.CaptureTaken) StartCoroutine(Capture());
        }

        string L(string en, string spanish) => es ? spanish : en;
        void ToggleLanguage()
        {
            es = !es;
            AutoSpriteSession.Language = es ? "es" : "en";
            Refresh();
        }

        public void TogglePause() { AutoSpriteSession.TogglePause(); Refresh(); }

        void Refresh()
        {
            languageLabel.text = es ? "ES" : "EN";
            backLabel.text = L("Back", "Volver");
            outfitLabel.text = L("Training outfit", "Atuendo de entrenamiento");
            footer.text = L("Appearance preview · changes stay in this session", "Vista previa · cambios solo en esta sesión");
            title.text = L("Your character", "Tu personaje");
            note.text = L("APPEARANCE PREVIEW", "VISTA PREVIA DE APARIENCIA");
            hint.text = Avatar.IsLoaded ? "" : Avatar.LastError;
            viewLabel.text = L("FRONT VIEW", "VISTA FRONTAL");
            pauseLabel.text = AutoSpriteSession.Paused ? L("Play idle", "Animar reposo") : L("Pause idle", "Pausar reposo");
            continueLabel.text = L("CONTINUE TO HOME", "CONTINUAR AL INICIO");
            ContinueButton.interactable = Avatar.IsLoaded;
            pause.interactable = Avatar.IsLoaded;
        }

        void Update()
        {
            if (size.x != Screen.width || size.y != Screen.height || safe != Screen.safeArea) Fit();
            if (Input.GetKeyDown(KeyCode.Escape)) back?.Invoke();
        }
        void Fit()
        {
            SystemViewport.Fit(root, 853, 1844);
            size = new Vector2(Screen.width, Screen.height); safe = Screen.safeArea;
        }

        IEnumerator Capture()
        {
            for (int i = 0; i < 8; i++) yield return null;
            yield return new WaitForEndOfFrame();
            AutoSpriteSession.CaptureTaken = true;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(capture, image.EncodeToPNG()); Destroy(image);
            Debug.Log("SOLOGYM_CAPTURE " + capture);
            if (AutoSpriteSession.HasArgument("-sologym-quit-after-capture")) Application.Quit();
        }
    }
}
