using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>WIN-015/016/017, native static review flow using the existing UI art.</summary>
    public sealed class TrainingScreen : MonoBehaviour
    {
        const float W = 853, H = 1844;
        const string SavedKey = "SoloGym.Training.ReviewedPreview.v1";
        RectTransform root, page, content;
        TrainingController controller;
        TrainingCatalog catalog;
        Action exit;
        string language, capture, savedSummary;
        bool smoke;
        float y;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
        public string Language => language;
        string L(string en, string es) => language == "es" ? es : en;

        public void Initialize(string locale, Action onExit, string initial = "training", string capturePath = null, bool runSmoke = false)
        {
            language = locale == "es" || locale == "en" ? locale : Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
            exit = onExit; capture = capturePath; smoke = runSmoke;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
            if (FindFirstObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Training camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(3, 8, 17, 255); camera.cullingMask = 0;
            }
            var node = new GameObject("Training canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 45;
            root = SystemUI.Node("Training safe area", node.transform, new Rect(0, 0, W, H));
            try
            {
                var asset = Resources.Load<TextAsset>("Training/Preview");
                if (asset == null) throw new InvalidOperationException("Missing training preview catalog.");
                catalog = JsonUtility.FromJson<TrainingCatalog>(asset.text);
                controller = new TrainingController(catalog);
                if (Argument("-sologym-training-profile") == "teen") controller.SelectProfile(2);
                if (initial == "readiness" || initial == "plan") controller.BeginReadiness();
                if (initial == "plan")
                {
                    controller.SetReadiness(Argument("-sologym-readiness") ?? "ready");
                    if (controller.Profile.teen) controller.SetSupervision(Argument("-sologym-supervision") == "yes");
                    controller.Review();
                }
                if (PlayerPrefs.HasKey(SavedKey)) savedSummary = L("A reviewed sample plan is saved on this device.", "Hay un plan de ejemplo revisado en este dispositivo.");
                Render();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                SystemUI.PortalPage(root, 400, 800);
                SystemUI.Caption(root, new Rect(100, 500, 653, 140), L("Training could not load.", "No se pudo cargar el entrenamiento."), 34);
                SystemUI.Button(root, new Rect(130, 740, 593, 90), L("Back", "Volver"), () => exit?.Invoke());
            }
            SystemViewport.Fit(root, W, H);
            if (capture != null || smoke) StartCoroutine(CaptureAndVerify());
        }

        void Update() { if (root != null) SystemViewport.Fit(root, W, H); }
        void OnDestroy() { if (root != null) Destroy(root.parent.gameObject); }
        void Change(Action action) { action(); Render(); }
        void Render()
        {
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            controls.Clear();
            page = SystemUI.Node("Training page", root, new Rect(0, 0, W, H));
            SystemUI.Art(page, new Rect(0, 0, W, H), "Art/PortalBackground-v1");
            SystemUI.Wordmark(page, new Rect(224, 42, 405, 100));
            controls["back"] = SystemUI.Button(page, new Rect(35, 45, 145, 85), L("Back", "Volver"), Back);
            controls["language"] = SystemUI.Button(page, new Rect(684, 45, 134, 85), language.ToUpperInvariant(), () =>
            { language = language == "es" ? "en" : "es"; Render(); });
            SystemUI.Caption(page, new Rect(90, 163, 673, 40), L("TRAINING QUEST", "MISIÓN DE ENTRENAMIENTO"), 25, SystemUI.Theme.body, SystemUI.Theme.accent);
            string title = controller.Step == TrainingStep.Hub ? L("Training", "Entrenamiento") :
                controller.Step == TrainingStep.Readiness ? L("How are you today?", "¿Cómo estás hoy?") :
                controller.Step == TrainingStep.Rest ? L("Time to recover", "Tiempo de recuperar") : L("Your session", "Tu sesión");
            SystemUI.Caption(page, new Rect(65, 212, 723, 80), title, 58, SystemUI.Theme.headingBold);
            SystemUI.Caption(page, new Rect(70, 303, 713, 47), L("LOCAL PREVIEW · SAMPLE DATA", "VISTA LOCAL · DATOS DE EJEMPLO"), 23, SystemUI.Theme.body, SystemUI.Theme.gold);
            SystemUI.Panel(page, new Rect(42, 373, 769, 1140), PanelStyle.Glass, true);
            var viewport = SystemUI.Node("Training scroll viewport", page, new Rect(74, 398, 705, 1085));
            viewport.gameObject.AddComponent<RectMask2D>();
            var background = viewport.gameObject.AddComponent<Image>(); background.color = Color.clear;
            content = SystemUI.Node("Training content", viewport, new Rect(0, 0, 705, 1085));
            y = 12;
            if (controller.Step == TrainingStep.Hub) Hub();
            else if (controller.Step == TrainingStep.Readiness) Readiness();
            else if (controller.Step == TrainingStep.Plan) Plan();
            else Rest();
            content.sizeDelta = new Vector2(705, Mathf.Max(1085, y + 20));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            SystemUI.AttachScrollAffordance(scroll, new Rect(74, 398, 705, 1085), 70, page);
            Footer();
            SystemUI.Caption(page, new Rect(65, 1760, 723, 55), L("Manual logs · No automatic rep counting", "Registro manual · Sin conteo automático"), 23, SystemUI.Theme.body);
        }

        void Label(string value, int size = 29, float height = 64, Color? color = null)
        {
            SystemUI.Text(content, new Rect(22, y, 661, height), value, size, SystemUI.Theme.body, color);
            y += height + 12;
        }
        void Heading(string text) { Label(text, 34, 49, SystemUI.Theme.accent); }
        void Choice(string id, string label, bool selected, Action action)
        {
            controls[id] = SystemUI.ChoiceButton(content, new Rect(12, y, 681, 86), label, selected, () => Change(action)); y += 99;
        }
        void Hub()
        {
            SystemUI.Icon(content, new Rect(291, y, 123, 105), "dumbbell", SystemUI.Theme.accent); y += 123;
            Heading(L("Build your next session", "Prepara tu próxima sesión"));
            Label(L("Check today’s readiness, then review the plan before you exercise.", "Revisa tu estado de hoy y después la rutina antes de entrenar."), 30, 100);
            if (savedSummary != null) Label(savedSummary, 26, 90, SystemUI.Theme.gold);
            Heading(L("Sample profile", "Perfil de ejemplo"));
            for (int i = 0; i < controller.Profiles.Length; i++)
            {
                int index = i;
                Choice("profile-" + i, controller.Profiles[i].name.Get(language), i == controller.ProfileIndex, () => controller.SelectProfile(index));
            }
            Label(L("These examples are separate from your account and character. No personal plan or workout is started.",
                "Estos ejemplos son independientes de tu cuenta y personaje. No se inicia un plan personal ni un entrenamiento."), 26, 135);
            Label(L("Draft exercise content still needs professional and youth review.", "El contenido de ejercicio aún necesita revisión profesional y para adolescentes."), 25, 100, SystemUI.Theme.gold);
        }
        void Readiness()
        {
            Heading(L("Choose what fits today", "Elige cómo te sientes hoy"));
            Label(L("Rest is always an option. This check does not diagnose a condition.", "Descansar siempre es una opción. Esta revisión no es un diagnóstico."), 28, 96);
            var values = new[] { "ready", "low_energy", "pain", "injury", "ill" };
            var en = new[] { "Ready for my usual session", "Low energy / sore — take it lighter", "I have pain", "I have an injury", "I feel ill" };
            var es = new[] { "Listo para mi sesión habitual", "Poca energía / fatiga — algo ligero", "Tengo dolor", "Tengo una lesión", "Me siento enfermo" };
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                Choice(value, L(en[i], es[i]), controller.Readiness == value, () => controller.SetReadiness(value));
            }
            if (controller.Profile.teen)
            {
                Heading(L("Strength supervision", "Supervisión para fuerza"));
                Choice("supervised", L("Appropriate supervision available", "Tengo supervisión adecuada"), controller.Supervised == true, () => controller.SetSupervision(true));
                Choice("unsupervised", L("Not available today", "Hoy no tengo supervisión"), controller.Supervised == false, () => controller.SetSupervision(false));
            }
            Heading(L("Available time", "Tiempo disponible"));
            foreach (int minutes in new[] { 15, 25, 40 })
            {
                int m = minutes;
                controls["minutes-" + m] = SystemUI.ChoiceButton(content, new Rect(12 + (m == 15 ? 0 : m == 25 ? 230 : 460), y, 221, 86),
                    m + " min", controller.Minutes == m, () => Change(() => controller.SetMinutes(m)));
            }
            y += 108;
        }
        void Plan()
        {
            var plan = controller.Plan;
            Heading(plan.name.Get(language));
            string difficulty = plan.difficulty_effective == "light" ? L("Light", "Ligero") : L("Medium", "Medio");
            Label(plan.status == "draft_ready" ? Mathf.CeilToInt(plan.estimated_seconds / 60f) + " min · " + difficulty + L(" · Manual log", " · Registro manual") :
                L("This plan needs changes before continuing.", "Este plan necesita cambios para continuar."), 30, 90);
            Label(controller.Profile.name.Get(language), 26, 70);
            Label(L("Available equipment: ", "Equipo disponible: ") + string.Join(", ", controller.Profile.equipment.Select(e => e.Get(language))), 25, 160);
            foreach (var message in plan.messages ?? Array.Empty<TrainingMessage>()) Label(message.text.Get(language), 27, 165, SystemUI.Theme.gold);
            foreach (var block in plan.blocks ?? Array.Empty<TrainingBlock>())
            {
                SystemUI.Panel(content, new Rect(8, y, 689, 177), PanelStyle.Slot);
                var role = block.role == "warmup" ? L("WARM-UP", "CALENTAMIENTO") : block.role == "cooldown" ? L("COOL-DOWN", "VUELTA A LA CALMA") : L("MAIN WORK", "TRABAJO PRINCIPAL");
                SystemUI.Text(content, new Rect(26, y + 13, 640, 29), role, 20, SystemUI.Theme.body, SystemUI.Theme.accent);
                SystemUI.Text(content, new Rect(26, y + 49, 640, 50), block.name.Get(language), 30, SystemUI.Theme.headingBold);
                string dose = block.sets + " × " + block.quantity_min + (block.quantity_max != block.quantity_min ? "–" + block.quantity_max : "") +
                    (block.unit == "minutes" ? " min" : L(" reps", " rep.")) + (block.per_side ? L(" / side", " / lado") : "");
                SystemUI.Text(content, new Rect(26, y + 109, 640, 50), dose + (block.rest_seconds > 0 ? " · " + block.rest_seconds + L("s rest", "s descanso") : ""), 27, SystemUI.Theme.body);
                y += 195;
            }
            if (plan.status == "draft_ready")
            {
                Label(L("Review every exercise and any equipment gaps above. Saving this draft does not start a session.",
                    "Revisa todos los ejercicios y las limitaciones de equipo. Guardar este borrador no inicia una sesión."), 27, 140);
                Choice("acknowledge", L("I reviewed the plan and its limits", "Revisé el plan y sus limitaciones"), controller.Acknowledged,
                    () => controller.Acknowledge(!controller.Acknowledged));
            }
        }
        void Rest()
        {
            SystemUI.Icon(content, new Rect(290, 60, 125, 140), "guard", SystemUI.Theme.accent); y = 240;
            Heading(L("Rest belongs in the plan", "Descansar es parte del plan"));
            foreach (var message in controller.Plan?.messages ?? Array.Empty<TrainingMessage>()) Label(message.text.Get(language), 30, 210);
            Label(L("No session has started. There is no missed-workout debt and no penalty for choosing rest.",
                "No se inició una sesión. No hay entrenamientos que debas recuperar ni penalización por descansar."), 31, 210);
        }
        void Footer()
        {
            string primary; Action action; bool enabled = true;
            if (controller.Step == TrainingStep.Hub) { primary = L("Check today’s readiness", "Revisar mi estado de hoy"); action = () => Change(controller.BeginReadiness); }
            else if (controller.Step == TrainingStep.Readiness) { primary = L("Review today’s plan", "Revisar el plan de hoy"); enabled = controller.CanReview; action = () => Change(() => controller.Review()); }
            else if (controller.Step == TrainingStep.Rest) { primary = L("Return to training", "Volver a entrenamiento"); action = () => Change(controller.Back); }
            else
            {
                primary = controller.Plan.status == "draft_ready" ? L("Save reviewed draft", "Guardar borrador revisado") : L("Adjust today’s choices", "Ajustar las opciones de hoy");
                enabled = controller.Plan.status != "draft_ready" || controller.CanAccept;
                action = () =>
                {
                    if (controller.Plan.status != "draft_ready") { Change(controller.Back); return; }
                    if (!controller.Accept()) return;
                    PlayerPrefs.SetString(SavedKey, controller.AcceptedKey); PlayerPrefs.Save();
                    savedSummary = L("Draft saved locally. No workout has started. Session logging is the next window.",
                        "Borrador guardado aquí. No se inició un entrenamiento. El registro de sesión es la siguiente ventana.");
                    Render();
                };
            }
            controls["primary"] = SystemUI.Button(page, new Rect(65, 1545, 723, 96), primary, action, PanelStyle.Primary);
            controls["primary"].interactable = enabled;
            if (controller.Step != TrainingStep.Rest)
                controls["rest"] = SystemUI.Button(page, new Rect(65, 1661, 723, 80), L("Choose rest today", "Elegir descanso hoy"), () => Change(controller.Rest));
        }
        void Back()
        {
            if (controller.Step == TrainingStep.Hub) exit?.Invoke();
            else Change(controller.Back);
        }
        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        IEnumerator CaptureAndVerify()
        {
            yield return null; yield return new WaitForEndOfFrame();
            if (capture != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));
                var image = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(capture, image.EncodeToPNG()); Destroy(image);
            }
            if (smoke)
            {
                bool passed = false;
                bool hadSave = PlayerPrefs.HasKey(SavedKey);
                string oldSave = PlayerPrefs.GetString(SavedKey, "");
                try
                {
                    TrainingSmoke.Verify(catalog);
                    controller = new TrainingController(catalog); Render();
                    controls["primary"].onClick.Invoke();
                    AssertUI(!controls["primary"].interactable, "readiness button starts disabled");
                    controls["low_energy"].onClick.Invoke(); controls["primary"].onClick.Invoke();
                    AssertUI(controller.Plan.difficulty_effective == "light", "UI chooses light plan");
                    AssertUI(!controls["primary"].interactable, "plan needs acknowledgment");
                    controls["acknowledge"].onClick.Invoke();
                    AssertUI(controls["primary"].interactable, "acknowledgment enables save");
                    controls["primary"].onClick.Invoke();
                    AssertUI(PlayerPrefs.GetString(SavedKey) == controller.AcceptedKey, "UI saves reviewed draft");
                    controls["rest"].onClick.Invoke();
                    AssertUI(controller.Step == TrainingStep.Rest, "UI voluntary rest");
                    controls["primary"].onClick.Invoke();
                    controls["profile-2"].onClick.Invoke(); controls["primary"].onClick.Invoke();
                    controls["ready"].onClick.Invoke();
                    AssertUI(!controls["primary"].interactable, "teen requires supervision answer");
                    controls["unsupervised"].onClick.Invoke(); controls["primary"].onClick.Invoke();
                    AssertUI(controller.Plan.status == "needs_review" && !controls.ContainsKey("acknowledge"), "UI cannot approve unsupervised teen plan");
                    string previous = language; controls["language"].onClick.Invoke();
                    AssertUI(language != previous && controller.Plan.status == "needs_review", "language preserves blocked state");
                    passed = true; Debug.Log("SOLOGYM_TRAINING_SMOKE passed: state matrix and 9 UI checks");
                }
                catch (Exception e) { Debug.LogException(e); }
                finally
                {
                    if (hadSave) PlayerPrefs.SetString(SavedKey, oldSave); else PlayerPrefs.DeleteKey(SavedKey);
                    PlayerPrefs.Save();
                }
                if (capture != null) File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), "{\"passed\":" + (passed ? "true" : "false") + "}");
                Application.Quit(passed ? 0 : 1);
            }
            else if (Environment.GetCommandLineArgs().Contains("-sologym-quit-after-capture")) Application.Quit();
        }
        static void AssertUI(bool result, string message)
        { if (!result) throw new InvalidOperationException("Training UI smoke: " + message); }
    }
}
