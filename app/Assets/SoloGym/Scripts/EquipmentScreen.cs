using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Environment and equipment selection on the shared portal frame.</summary>
    public sealed class EquipmentScreen : MonoBehaviour
    {
        const float W = 853, H = 1844;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        RectTransform root, page, modal;
        Font serif, bold, body;
        EquipmentController controller;
        Action back, leave;
        Action<string> onCheckpoint;
        string capture;
        Text status;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
        Vector2 lastSize;
        Rect lastSafe;
        float equipmentScrollY;

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
            controller = new EquipmentController(review);
            controller.SetLanguage(language);
            var node = new GameObject("Equipment canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvas.pixelPerfect = true;
            root = RectNode("Equipment reference 853x1844", node.transform, new Rect(0, 0, W, H));
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
            if (initialStep == "equipment")
            {
                controller.SelectEnvironment("home");
                controller.ContinueEnvironment();
            }
            else if (initialStep == "review")
            {
                controller.SelectEnvironment("home");
                controller.ContinueEnvironment();
                controller.SetBodyweightOnly(true);
                controller.ContinueEquipment();
            }
        }

        public void Resume(string language)
        {
            gameObject.SetActive(true);
            controller.SetLanguage(language);
            Fit();
        }

        void Render(EquipmentViewModel model)
        {
            CloseModal();
            if (page != null)
            {
                if (model.Step == EquipmentStep.Equipment)
                {
                    var existingScroll = page.GetComponentInChildren<ScrollRect>();
                    if (existingScroll != null && existingScroll.content != null)
                        equipmentScrollY = existingScroll.content.anchoredPosition.y;
                }
                page.gameObject.SetActive(false);
                Destroy(page.gameObject);
            }
            controls.Clear();
            page = RectNode("Equipment page", root, new Rect(0, 0, W, H));
            SystemUI.PortalPage(page, 718, 965);
            SystemUI.Divider(page, 807);
            SystemUI.Icon(page, new Rect(43, 47, 28, 34), "back", Silver);
            SystemUI.Icon(page, new Rect(421, 1728, 10, 25), "sigil", Silver);
            Live("Back", new Rect(92, 53, L(61, 83), 25), L("Back", "Volver"), 31);
            Live("Language", new Rect(765, 56, 29, 20), model.Language.ToUpperInvariant(), 27);
            Hit("back", new Rect(30, 20, 180, 100), Back);
            Hit("language", new Rect(680, 20, 130, 100), Language);
            Live("Title", new Rect(L(128, 139), 836, L(596, 575), 53),
                model.Copy("window_title"), 72, Silver, bold);
            switch (model.Step)
            {
                case EquipmentStep.Environment:
                    EnvironmentStep(model);
                    break;
                case EquipmentStep.Equipment:
                    EquipmentStepView(model);
                    break;
                default:
                    ReviewStep(model);
                    break;
            }
            Live("Privacy", new Rect(L(294, 275), 1735, L(89, 116), 24), L("Privacy", "Privacidad"), 29);
            Live("Terms", new Rect(L(491, 472), 1735, L(68, 101), 24), L("Terms", "Términos"), 29);
            Hit("privacy", new Rect(250, 1706, 179, 85), () => Document("privacy"));
            Hit("terms", new Rect(450, 1706, 172, 85), () => Document("terms"));
            if (model.ReviewMode)
                TextAt(page, new Rect(80, 280, 693, 35),
                    L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"),
                    20, body, Cyan, TextAnchor.MiddleCenter);
            status = TextAt(page, new Rect(105, 1496, 645, 35), model.Error, 20, body, new Color32(255, 207, 161, 255),
                TextAnchor.MiddleCenter);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void EnvironmentStep(EquipmentViewModel model)
        {
            Heading(model.Copy("environment_title"));
            TextAt(page, new Rect(112, 978, 629, 36), model.Copy("environment_subtitle"), 29, body, Silver, TextAnchor.MiddleCenter);
            const float firstRowY = 1072;
            for (int i = 0; i < model.Environments.Length; i++)
            {
                var entry = model.Environments[i];
                Choice(entry.id, page, new Rect(107, firstRowY + i * 81, 641, 74), entry.Label(model.Language),
                    model.EnvironmentId == entry.id, () => controller.SelectEnvironment(entry.id));
            }
            var footer = TextAt(page, new Rect(112, firstRowY + model.Environments.Length * 81 + 8, 629, 40), model.Copy("environment_footer"), 26, body, Silver,
                TextAnchor.MiddleCenter);
            footer.horizontalOverflow = HorizontalWrapMode.Wrap;
            Primary(() => controller.ContinueEnvironment(), model.Copy("continue"), model.CanContinue);
            Secondary("leave", new Rect(245, 1645, 363, 37), model.Copy("not_now"), controller.Decline);
        }

        void EquipmentStepView(EquipmentViewModel model)
        {
            TextAt(page, new Rect(105, 900, 643, 60), model.Copy("equipment_title"), 35, serif, Silver, TextAnchor.MiddleCenter);
            const float subtitleY = 968;
            const float subtitleH = 72;
            var equipmentSubtitle = TextAt(page, new Rect(112, subtitleY, 629, subtitleH), model.Copy("equipment_subtitle"), 27, body,
                Silver, TextAnchor.MiddleCenter);
            equipmentSubtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            const float listTop = subtitleY + subtitleH + 48;
            const float continueTop = 1545;
            const float belowListGap = 52;
            float viewportH = continueTop - listTop - belowListGap;
            var viewportRect = new Rect(95, listTop, 665, viewportH);
            var viewport = RectNode("Equipment scroll viewport", page, viewportRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            const float scrollPadTop = 8;
            const float rowH = 74;
            float contentHeight = scrollPadTop + rowH;
            if (model.VisibleEquipment.Length == 0) contentHeight += 120;
            else contentHeight += rowH * model.VisibleEquipment.Length;
            var content = RectNode("Equipment scroll content", viewport, new Rect(0, 0, 641, contentHeight));
            content.pivot = new Vector2(0, 1);
            content.anchorMin = content.anchorMax = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            Choice("bodyweight", content, new Rect(12, scrollPadTop, 617, rowH), model.Copy("bodyweight_only"), model.BodyweightOnly,
                () => controller.SetBodyweightOnly(!model.BodyweightOnly));
            float rowY = scrollPadTop + rowH;
            if (model.VisibleEquipment.Length == 0)
            {
                var outdoor = TextAt(content, new Rect(12, rowY + 8, 617, 120), model.Copy("outdoor_helper"), 24, body, Silver,
                    TextAnchor.UpperLeft);
                outdoor.horizontalOverflow = HorizontalWrapMode.Wrap;
                Canvas.ForceUpdateCanvases();
                float helperH = outdoor.preferredHeight + 8;
                content.sizeDelta = new Vector2(641, scrollPadTop + rowH + helperH);
            }
            else
            {
                for (int i = 0; i < model.VisibleEquipment.Length; i++)
                {
                    var entry = model.VisibleEquipment[i];
                    bool selected = Array.IndexOf(model.SelectedEquipmentIds, entry.id) >= 0;
                    string key = entry.id;
                    controls[key] = SystemUI.ChoiceButton(content, new Rect(12, rowY + i * rowH, 617, rowH),
                        entry.Label(model.Language), selected, () => controller.ToggleEquipment(key));
                }
            }
            Canvas.ForceUpdateCanvases();
            float maxScroll = Mathf.Max(0, content.rect.height - viewport.rect.height);
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(equipmentScrollY, 0, maxScroll));
            scroll.onValueChanged.AddListener(_ =>
            {
                if (scroll.content != null) equipmentScrollY = scroll.content.anchoredPosition.y;
            });
            SystemUI.AttachScrollAffordance(scroll, viewportRect, rowH, page);
            Primary(() => controller.ContinueEquipment(), model.Copy("continue"), model.CanContinue);
            Secondary("leave", new Rect(245, 1645, 363, 37), model.Copy("not_now"), controller.Decline);
        }

        void ReviewStep(EquipmentViewModel model)
        {
            Heading(model.Copy("review_title"));
            float y = 1000;
            y = PlaceWrappedBlock(y, model.Copy("review_environment") + ": " + model.EnvironmentLabel, 80) + 8;
            PlaceReviewLink("change_environment", y, model.Copy("change_environment"), controller.EditEnvironment);
            y += 44;
            y = PlaceWrappedBlock(y, model.Copy("review_equipment") + ": " + model.EquipmentSummary, 280) + 8;
            PlaceReviewLink("change_equipment", y, model.Copy("change_equipment"), controller.EditEquipment);
            y += 44;
            float bodyMax = Mathf.Max(80, 1535 - y);
            PlaceWrappedBlock(y, model.Copy("review_body"), bodyMax);
            Primary(() => controller.ContinueReview(), model.Copy("continue"), model.CanContinue && model.ReviewMode);
            Secondary("back_review", new Rect(245, 1645, 363, 37), model.Copy("back"), () => controller.Back());
        }

        void Heading(string title) => TextAt(page, new Rect(105, 918, 643, 65), title, 35, serif, Silver, TextAnchor.MiddleCenter);

        float PlaceWrappedBlock(float y, string content, float maxHeight)
        {
            var text = TextAt(page, new Rect(112, y, 629, maxHeight), content, 29, body, Silver, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Canvas.ForceUpdateCanvases();
            float h = Mathf.Min(text.preferredHeight, maxHeight);
            text.rectTransform.sizeDelta = new Vector2(629, h);
            return y + h;
        }

        void PlaceReviewLink(string key, float y, string label, Action action)
        {
            var rect = new Rect(112, y, 320, 40);
            Hit(key, rect, action);
            TextAt(page, rect, label, 26, serif, Cyan, TextAnchor.MiddleLeft);
        }

        void Paragraph(float y, string content)
        {
            var text = TextAt(page, new Rect(112, y, 629, 100), content, 29, body, Silver, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
        void Primary(Action action, string text, bool active = true)
        {
            var surface = SystemUI.Panel(page, new Rect(91, 1545, 671, 96), PanelStyle.Primary);
            surface.color = active ? Color.white : new Color(.5f, .6f, .7f, .7f);
            SystemUI.Caption(page, new Rect(115, 1547, 623, 92), text, 45, bold, active ? Silver : SystemUI.Theme.muted);
            var button = Hit("continue", new Rect(91, 1545, 671, 96), action);
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
            if (controller.Model.Step == EquipmentStep.Environment)
            {
                back?.Invoke();
                gameObject.SetActive(false);
            }
            else controller.Back();
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
            try { Debug.Log("SOLOGYM_EQUIPMENT_STATE " + EquipmentStateChecks.Run()); }
            catch (Exception e) { Debug.LogError(e.Message); passed = false; }
            controls["gym"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.EnvironmentId == "gym";
            controls["continue"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.Step == EquipmentStep.Equipment;
            controls["bodyweight"].onClick.Invoke();
            controls["continue"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.Step == EquipmentStep.Review && controller.Model.BodyweightOnly;
            string json = "{\"passed\":" + (passed ? "true" : "false")
                + ",\"checks\":[\"equipment state checks\",\"environment selection\",\"bodyweight path\",\"review step\"]}";
            File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), json);
            Debug.Log("SOLOGYM_EQUIPMENT_SMOKE " + json);
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
