using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Combined weekday and availability setup, then badge review on the shared portal frame.</summary>
    public sealed class ScheduleScreen : MonoBehaviour
    {
        const float W = PortalFrameLayout.PageWidth, H = PortalFrameLayout.PageHeight;
        const float WindowTitleY = 490;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        RectTransform root, page, modal;
        Font serif, bold, body;
        ScheduleController controller;
        Action back, leave;
        Action<string> onCheckpoint;
        string capture;
        Text status;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
        Vector2 lastSize;
        Rect lastSafe;
        float setupScrollY;
        PortalWindowFrame.ScheduleSetupLayout setupLayout;

        public void Initialize(string language, bool review, Action onBack, Action onExit,
            Action<string> checkpointHandler = null, string capturePath = null, string initialStep = null)
        {
            back = onBack;
            leave = onExit;
            onCheckpoint = checkpointHandler;
            capture = capturePath;
            serif = SystemUI.Theme.heading;
            bold = SystemUI.Theme.headingBold;
            body = SystemUI.Theme.body;
            controller = new ScheduleController(review);
            controller.SetLanguage(language);
            var node = new GameObject("Schedule canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvas.pixelPerfect = true;
            root = RectNode("Schedule reference 853x1844", node.transform, new Rect(0, 0, W, H));
            controller.Changed += Render;
            controller.ExitRequested += Exit;
            controller.CheckpointRequested += id =>
            {
                if (onCheckpoint != null) onCheckpoint(id);
                else Notice(controller.Model.Copy("next_window_notice_title"), controller.Model.Copy("next_window_notice_body"));
            };
            ApplyInitialStep(initialStep);
            Render(controller.Model);
            Fit();
            if (capture != null) StartCoroutine(Capture());
        }

        void ApplyInitialStep(string initialStep)
        {
            if (!controller.Model.ReviewMode || string.IsNullOrEmpty(initialStep)) return;
            if (initialStep == "review" || initialStep == "duration")
            {
                controller.ToggleDay(0);
                controller.ToggleDay(2);
                controller.ToggleDay(4);
                controller.SetAvailabilityHours(1);
                controller.SetAvailabilityMinutes(30);
            }
        }

        public void Resume(string language)
        {
            gameObject.SetActive(true);
            controller.SetLanguage(language);
            Fit();
        }

        void Render(ScheduleViewModel model)
        {
            CloseModal();
            if (page != null)
            {
                if (model.Step == ScheduleStep.Setup)
                {
                    var existingScroll = page.GetComponentInChildren<ScrollRect>();
                    if (existingScroll != null && existingScroll.content != null)
                        setupScrollY = existingScroll.content.anchoredPosition.y;
                }
                page.gameObject.SetActive(false);
                Destroy(page.gameObject);
            }
            controls.Clear();
            page = RectNode("Schedule page", root, new Rect(0, 0, W, H));
            float scrollContentHeight = MeasureSetupScrollContentHeight(model);
            setupLayout = PortalWindowFrame.SolveScheduleSetup(scrollContentHeight);
            SystemUI.PortalPage(page, setupLayout.PanelY, setupLayout.PanelHeight);
            SystemUI.Divider(page, 807);
            SystemUI.Icon(page, new Rect(43, 47, 28, 34), "back", Silver);
            SystemUI.Icon(page, new Rect(421, 1728, 10, 25), "sigil", Silver);
            Live("Back", new Rect(92, 53, L(61, 83), 25), L("Back", "Volver"), 31);
            Live("Language", new Rect(765, 56, 29, 20), model.Language.ToUpperInvariant(), 27);
            Hit("back", new Rect(30, 20, 180, 100), Back);
            Hit("language", new Rect(680, 20, 130, 100), Language);
            Live("Title", new Rect(L(168, 200), WindowTitleY, L(517, 453), 53),
                model.Copy("window_title"), 72, Silver, bold);
            SetupStep(model);
            Live("Privacy", new Rect(L(294, 275), 1735, L(89, 116), 24), L("Privacy", "Privacidad"), 29);
            Live("Terms", new Rect(L(491, 472), 1735, L(68, 101), 24), L("Terms", "Términos"), 29);
            Hit("privacy", new Rect(250, 1706, 179, 85), () => Document("privacy"));
            Hit("terms", new Rect(450, 1706, 172, 85), () => Document("terms"));
            if (model.ReviewMode)
                TextAt(page, new Rect(80, 280, 693, 35),
                    L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"),
                    20, body, Cyan, TextAnchor.MiddleCenter);
            var statusRect = setupLayout.StatusRect;
            status = TextAt(page, statusRect, model.Error, 20, body, new Color32(255, 207, 161, 255),
                TextAnchor.MiddleCenter);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        float MeasureSetupScrollContentHeight(ScheduleViewModel model)
        {
            var measureHost = RectNode("Setup scroll measure", page,
                new Rect(PortalFrameLayout.ContentX, -4000, PortalFrameLayout.ContentWidth, 8));
            BuildSetupContent(model, measureHost);
            var content = measureHost.GetChild(0) as RectTransform;
            Canvas.ForceUpdateCanvases();
            float height = content != null ? content.rect.height : 0;
            Destroy(measureHost.gameObject);
            return height;
        }

        void SetupStep(ScheduleViewModel model)
        {
            var viewportRect = setupLayout.ScrollViewport;
            var viewport = RectNode("Setup scroll viewport", page, viewportRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            BuildSetupContent(model, viewport);
            var content = viewport.GetChild(0) as RectTransform;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            Canvas.ForceUpdateCanvases();
            float maxScroll = Mathf.Max(0, content.rect.height - viewport.rect.height);
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(setupScrollY, 0, maxScroll));
            scroll.onValueChanged.AddListener(_ =>
            {
                if (scroll.content != null) setupScrollY = scroll.content.anchoredPosition.y;
            });
            SystemUI.AttachScrollAffordance(scroll, viewportRect, 72, page);
            BuildTimeWheels(model, setupLayout.WheelBandTop, setupLayout.TimeHeaderTop);
            Primary(() => controller.ContinueSetup(), model.Copy("continue"), model.CanContinue, setupLayout.PrimaryRect,
                setupLayout.PrimaryCaptionRect);
            Secondary("leave", setupLayout.SecondaryRect, model.Copy("not_now"), controller.Decline);
        }

        void BuildTimeWheels(ScheduleViewModel model, float wheelBandTop, float timeHeaderTop)
        {
            float wheelBandHeight = PortalFrameLayout.WheelBandHeight;
            TextAt(page, new Rect(105, timeHeaderTop, 643, 34), model.Copy("time_title"),
                28, serif, Silver, TextAnchor.MiddleCenter);
            var sub = TextAt(page, new Rect(112, wheelBandTop - 46, 629, 36), model.Copy("time_subtitle"), 22, body,
                Silver, TextAnchor.MiddleCenter);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            const float wheelW = 76f, labelW = 108f, pairGap = 32f;
            float pairW = wheelW + labelW;
            float rowW = pairW * 2 + pairGap;
            float rowX = PortalFrameLayout.ContentX + (PortalFrameLayout.ContentWidth - rowW) * 0.5f;
            float wheelY = wheelBandTop;
            SystemWheelPicker.Attach(page, new Rect(rowX, wheelY, wheelW, wheelBandHeight), model.HourOptions,
                model.AvailabilityHours, h => h.ToString(), controller.SetAvailabilityHours);
            WheelLabel(rowX + wheelW + 6, wheelY, wheelBandHeight, labelW, model.Copy("hours_label"));
            float minX = rowX + pairW + pairGap;
            SystemWheelPicker.Attach(page, new Rect(minX, wheelY, wheelW, wheelBandHeight), model.MinuteOptions,
                model.AvailabilityMinutes, m => m.ToString("00"), controller.SetAvailabilityMinutes);
            WheelLabel(minX + wheelW + 6, wheelY, wheelBandHeight, labelW, model.Copy("minutes_label"));
        }

        void WheelLabel(float x, float y, float wheelBandHeight, float width, string label)
        {
            var text = TextAt(page, new Rect(x, y + wheelBandHeight * 0.5f - 18, width, 36), label, 21, body, Silver,
                TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        void BuildSetupContent(ScheduleViewModel model, RectTransform viewport)
        {
            var content = RectNode("Setup scroll content", viewport,
                new Rect(0, 0, PortalFrameLayout.ScrollContentWidth, 900));
            content.pivot = new Vector2(0, 1);
            content.anchorMin = content.anchorMax = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            float y = 24;
            var daysTitle = TextAt(content, new Rect(12, y, 617, 36), model.Copy("days_title"), 28, serif, Silver,
                TextAnchor.MiddleCenter);
            Canvas.ForceUpdateCanvases();
            y += daysTitle.preferredHeight + 14;
            var subtitle = TextAt(content, new Rect(12, y, 617, 52), model.Copy("days_subtitle"), 25, body, Silver,
                TextAnchor.MiddleCenter);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            Canvas.ForceUpdateCanvases();
            y += Mathf.Max(52, subtitle.preferredHeight) + 16;
            const float chipW = 148f, chipH = 58f, gap = 8f, startX = 12f;
            for (int i = 0; i < model.Weekdays.Length; i++)
            {
                var entry = model.Weekdays[i];
                int row = i < 4 ? 0 : 1;
                int col = i < 4 ? i : i - 4;
                float rowWidth = row == 0 ? 4 * chipW + 3 * gap : 3 * chipW + 2 * gap;
                float rowStart = startX + (617f - rowWidth) * 0.5f;
                float rowY = y + row * (chipH + gap);
                var rect = new Rect(rowStart + col * (chipW + gap), rowY, chipW, chipH);
                bool selected = Array.IndexOf(model.SelectedDayIndices, entry.index) >= 0;
                Choice("day_" + entry.index, content, rect, entry.Label(model.Language), selected,
                    () => controller.ToggleDay(entry.index));
            }
            y += 2 * (chipH + gap) + 16;
            var helper = TextAt(content, new Rect(12, y, 617, 200), model.Copy("setup_helper"), 23, body, Silver,
                TextAnchor.UpperLeft);
            helper.horizontalOverflow = HorizontalWrapMode.Wrap;
            Canvas.ForceUpdateCanvases();
            y += helper.preferredHeight + PortalFrameLayout.SectionGapMd;
            content.sizeDelta = new Vector2(PortalFrameLayout.ScrollContentWidth, y);
        }

        void Primary(Action action, string text, bool active = true, Rect? buttonRect = null, Rect? captionRect = null)
        {
            var rect = buttonRect ?? PortalFrameLayout.PrimaryRect;
            var caption = captionRect ?? PortalFrameLayout.PrimaryCaptionRect;
            var surface = SystemUI.Panel(page, rect, PanelStyle.Primary);
            surface.color = active ? Color.white : new Color(.5f, .6f, .7f, .7f);
            SystemUI.Caption(page, caption, text, 45, bold, active ? Silver : SystemUI.Theme.muted);
            var button = Hit("continue", rect, action);
            button.interactable = active;
        }

        void Secondary(string key, Rect rect, string label, Action action)
        {
            TextAt(page, rect, label, 26, serif, Cyan, TextAnchor.MiddleCenter);
            Hit(key, rect, action);
        }

        void Choice(string key, Transform parent, Rect rect, string label, bool selected, Action action)
        {
            controls[key] = SystemUI.ChoiceButton(parent, rect, label, selected, action);
        }

        void Back()
        {
            back?.Invoke();
            gameObject.SetActive(false);
        }

        void Exit() { leave?.Invoke(); Destroy(gameObject); }

        void Language()
        {
            OpenModal(L("Language", "Idioma"));
            ModalButton(new Rect(35, 140, 640, 86), "English", () => controller.SetLanguage("en"));
            ModalButton(new Rect(35, 240, 640, 86), "Español", () => controller.SetLanguage("es"));
            ModalButton(new Rect(35, 340, 640, 86), L("Use device language", "Usar idioma del dispositivo"),
                () => controller.SetLanguage("auto"));
        }

        void Document(string id)
        {
            Notice(id == "privacy" ? L("Privacy", "Privacidad") : L("Terms", "Términos"),
                L("The final document is not available yet. This preview cannot record acceptance. Your choices remain in memory only.",
                    "El documento final aún no está disponible. Esta vista previa no puede registrar aceptación. Tus elecciones permanecen solo en memoria."));
        }

        void Notice(string title, string message)
        {
            OpenModal(title);
            var text = TextAt(modal, new Rect(35, 135, 640, 445), message, 29, body, Silver, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            ModalButton(new Rect(35, 620, 640, 83), L("Close", "Cerrar"), CloseModal);
        }

        void OpenModal(string title)
        {
            CloseModal();
            var shade = RectNode("Modal shade", root, new Rect(0, 0, W, H));
            shade.gameObject.AddComponent<Image>().color = new Color(0, .01f, .04f, .87f);
            modal = RectNode("System dialog", shade, new Rect(71, 500, 711, 745));
            var frame = modal.gameObject.AddComponent<SystemPanel>();
            frame.theme = SystemUI.Theme;
            frame.ornaments = true;
            TextAt(modal, new Rect(30, 24, 560, 70), title, 34, bold, Silver);
            ModalButton(new Rect(615, 12, 70, 80), "×", CloseModal);
        }

        void ModalButton(Rect rect, string label, Action action) => SystemUI.Button(modal, rect, label, action);

        void CloseModal()
        {
            if (modal == null) return;
            var old = modal.parent.gameObject;
            modal = null;
            old.SetActive(false);
            Destroy(old);
        }

        void Update()
        {
            if (root == null) return;
            if (lastSize.x != Screen.width || lastSize.y != Screen.height || lastSafe != Screen.safeArea) Fit();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (modal != null) CloseModal();
                else Back();
            }
        }

        void Fit()
        {
            SystemViewport.Fit(root, W, H, 0);
            lastSize = new Vector2(Screen.width, Screen.height);
            lastSafe = Screen.safeArea;
        }

        IEnumerator Capture()
        {
            for (int i = 0; i < 8; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(capture, image.EncodeToPNG());
            Destroy(image);
            Debug.Log("SOLOGYM_CAPTURE " + capture);
            if (HasArgument("-sologym-smoke")) yield return Smoke();
            Application.Quit();
        }

        IEnumerator Smoke()
        {
            bool passed = true;
            try { Debug.Log("SOLOGYM_SCHEDULE_STATE " + ScheduleStateChecks.Run()); }
            catch (Exception e) { Debug.LogError(e.Message); passed = false; }
            if (controls.TryGetValue("day_0", out Button day0))
            {
                day0.onClick.Invoke();
                yield return null;
                controls["day_2"].onClick.Invoke();
                controls["day_4"].onClick.Invoke();
                yield return null;
                passed &= controller.Model.SelectedDayIndices.Length == 3;
                string checkpoint = null;
                controller.CheckpointRequested += id => checkpoint = id;
                controls["continue"].onClick.Invoke();
                yield return null;
                passed &= checkpoint == "REVIEW:WIN-013" && controller.Model.Step == ScheduleStep.Setup;
            }
            string json = "{\"passed\":" + (passed ? "true" : "false")
                + ",\"checks\":[\"schedule state checks\",\"day selection\",\"setup to next window\"]}";
            File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), json);
            Debug.Log("SOLOGYM_SCHEDULE_SMOKE " + json);
            if (!passed) Application.Quit(2);
        }

        void OnDestroy() => controller?.Dispose();

        static RectTransform RectNode(string name, Transform parent, Rect rect) => SystemUI.Node(name, parent, rect);

        Text Live(string name, Rect rect, string value, int size, Color? color = null, Font font = null)
        {
            var box = new Rect(rect.x, rect.y - 8, rect.width, rect.height + 16);
            var t = SystemUI.Caption(page, box, value, size, font ?? serif, color ?? Silver);
            t.name = name;
            if (name == "Title") t.gameObject.AddComponent<OnboardingSilverText>();
            return t;
        }

        Text TextAt(Transform parent, Rect rect, string value, int size, Font font, Color color, TextAnchor align = TextAnchor.MiddleLeft)
            => SystemUI.Text(parent, rect, value, size, font, color, align);

        Button Hit(string key, Rect rect, Action action)
        {
            var node = RectNode(key, page, rect);
            var image = node.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            var b = node.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => action());
            controls[key] = b;
            return b;
        }

        string L(string en, string es) => controller.Model.Language == "es" ? es : en;
        float L(float en, float es) => controller.Model.Language == "es" ? es : en;
        static bool HasArgument(string key) => Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0;
    }
}
