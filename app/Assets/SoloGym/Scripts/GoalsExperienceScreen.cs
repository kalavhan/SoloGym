using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Goals and experience on the shared portal frame with live choice rows.</summary>
    public sealed class GoalsExperienceScreen : MonoBehaviour
    {
        const float W = 853, H = 1844;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        RectTransform root, page, modal;
        Font serif, bold, body;
        GoalsExperienceController controller;
        Action back, leave;
        Action<string> onCheckpoint;
        string capture;
        Text status;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
        Vector2 lastSize;
        Rect lastSafe;

        public void Initialize(string language, bool review, bool teenAudience, Action onBack, Action onExit,
            Action<string> checkpointHandler = null, string capturePath = null, string initialStep = null)
        {
            back = onBack;
            leave = onExit;
            onCheckpoint = checkpointHandler;
            capture = capturePath;
            serif = SystemUI.Theme.heading;
            bold = SystemUI.Theme.headingBold;
            body = SystemUI.Theme.body;
            controller = new GoalsExperienceController(review, teenAudience);
            controller.SetLanguage(language);
            var node = new GameObject("Goals experience canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;
            canvas.pixelPerfect = true;
            root = RectNode("Goals reference 853x1844", node.transform, new Rect(0, 0, W, H));
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
            if (initialStep == "experience")
            {
                controller.SelectGoal("general_fitness");
                controller.ContinueGoal();
            }
            else if (initialStep == "review")
            {
                controller.SelectGoal("general_fitness");
                controller.ContinueGoal();
                controller.SelectExperience("beginner");
                controller.ContinueExperience();
            }
        }

        public void Resume(string language)
        {
            gameObject.SetActive(true);
            controller.SetLanguage(language);
            Fit();
        }

        void Render(GoalsExperienceViewModel model)
        {
            CloseModal();
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            controls.Clear();
            page = RectNode("Goals page", root, new Rect(0, 0, W, H));
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
                case GoalsExperienceStep.Goal:
                    GoalStep(model);
                    break;
                case GoalsExperienceStep.Experience:
                    ExperienceStep(model);
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
            if (model.UncertaintyDialogOpen) UncertaintyDialog(model);
            status = TextAt(page, new Rect(105, 1496, 645, 35), model.Error, 20, body, new Color32(255, 207, 161, 255),
                TextAnchor.MiddleCenter);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void GoalStep(GoalsExperienceViewModel model)
        {
            Heading(model.Copy("goal_title"));
            TextAt(page, new Rect(112, 990, 629, 40), model.Copy("goal_subtitle"), 29, body, Silver, TextAnchor.MiddleCenter);
            if (model.IsTeenAudience)
            {
                var teen = TextAt(page, new Rect(112, 1035, 629, 50), model.Copy("teen_helper"), 24, body, Silver,
                    TextAnchor.MiddleCenter);
                teen.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            float startY = model.IsTeenAudience ? 1095 : 1045;
            for (int i = 0; i < model.VisibleGoals.Length; i++)
            {
                var goal = model.VisibleGoals[i];
                Choice(goal.id, new Rect(107, startY + i * 81, 641, 74), goal.Label(model.Language),
                    model.GoalId == goal.id, () => controller.SelectGoal(goal.id));
            }
            float footerY = startY + model.VisibleGoals.Length * 81 + 12;
            var footer = TextAt(page, new Rect(112, footerY, 629, 40), model.Copy("goal_footer"), 26, body, Silver,
                TextAnchor.MiddleCenter);
            footer.horizontalOverflow = HorizontalWrapMode.Wrap;
            Primary(() => controller.ContinueGoal(), model.Copy("continue"), model.CanContinue);
            Secondary("leave", new Rect(245, 1645, 363, 37), model.Copy("not_now"), controller.Decline);
        }

        void ExperienceStep(GoalsExperienceViewModel model)
        {
            Heading(model.Copy("experience_title"));
            TextAt(page, new Rect(112, 990, 629, 40), model.Copy("experience_subtitle"), 29, body, Silver,
                TextAnchor.MiddleCenter);
            for (int i = 0; i < model.ExperienceChoices.Length; i++)
            {
                var entry = model.ExperienceChoices[i];
                Choice(entry.id, new Rect(107, 1045 + i * 81, 641, 74), entry.Label(model.Language),
                    model.ExperienceId == entry.id, () => controller.SelectExperience(entry.id));
            }
            Secondary("unsure", new Rect(245, 1280, 363, 37), model.Copy("uncertainty_link"), controller.OpenUncertainty);
            Primary(() => controller.ContinueExperience(), model.Copy("continue"), model.CanContinue);
            Secondary("leave", new Rect(245, 1645, 363, 37), model.Copy("not_now"), controller.Decline);
        }

        void ReviewStep(GoalsExperienceViewModel model)
        {
            Heading(model.Copy("review_title"));
            Paragraph(1000, model.Copy("review_focus") + ": " + model.GoalLabel);
            Hit("change_goal", new Rect(112, 1065, 280, 40), controller.EditGoal);
            TextAt(page, new Rect(112, 1065, 280, 40), model.Copy("change_goal"), 26, serif, Cyan, TextAnchor.MiddleLeft);
            Paragraph(1120, model.Copy("review_experience") + ": " + model.ExperienceLabel);
            Hit("change_experience", new Rect(112, 1185, 320, 40), controller.EditExperience);
            TextAt(page, new Rect(112, 1185, 320, 40), model.Copy("change_experience"), 26, serif, Cyan,
                TextAnchor.MiddleLeft);
            Paragraph(1260, model.Copy("review_body"));
            Paragraph(1420, model.Copy("review_progress"));
            Primary(() => controller.ContinueReview(), model.Copy("continue"), model.CanContinue && model.ReviewMode);
            Secondary("back_review", new Rect(245, 1645, 363, 37), model.Copy("back"), () => controller.Back());
        }

        void UncertaintyDialog(GoalsExperienceViewModel model)
        {
            OpenModal(model.Copy("uncertainty_title"));
            var text = TextAt(modal, new Rect(35, 135, 640, 320), model.Copy("uncertainty_body"), 29, body, Silver,
                TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            ModalButton(new Rect(35, 480, 640, 83), model.Copy("uncertainty_confirm"), controller.ConfirmUncertaintyBeginner);
            ModalButton(new Rect(35, 580, 640, 83), model.Copy("uncertainty_cancel"), controller.CancelUncertainty);
        }

        void Heading(string title) => TextAt(page, new Rect(105, 918, 643, 65), title, 35, serif, Silver, TextAnchor.MiddleCenter);
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
        void Choice(string key, Rect rect, string label, bool selected, Action action)
        {
            controls[key] = SystemUI.Button(page, rect, (selected ? "◆  " : "◇  ") + label, action,
                selected ? PanelStyle.Selected : PanelStyle.Outline);
        }

        void Back()
        {
            if (controller.Model.Step == GoalsExperienceStep.Goal)
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
                else if (controller.Model.UncertaintyDialogOpen) controller.CancelUncertainty();
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
            try { Debug.Log("SOLOGYM_GOALS_STATE " + GoalsExperienceStateChecks.Run()); }
            catch (Exception e) { Debug.LogError(e.Message); passed = false; }
            controls["strength"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.GoalId == "strength";
            controls["continue"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.Step == GoalsExperienceStep.Experience;
            controls["intermediate"].onClick.Invoke();
            controls["continue"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.Step == GoalsExperienceStep.Review;
            controls["change_goal"].onClick.Invoke();
            yield return null;
            passed &= controller.Model.Step == GoalsExperienceStep.Goal;
            controller.SetLanguage("es");
            yield return null;
            passed &= controller.Model.Language == "es";
            string json = "{\"passed\":" + (passed ? "true" : "false")
                + ",\"checks\":[\"goals state checks\",\"goal selection\",\"experience navigation\",\"review edit\",\"language\"]}" ;
            File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), json);
            Debug.Log("SOLOGYM_GOALS_SMOKE " + json);
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
        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
