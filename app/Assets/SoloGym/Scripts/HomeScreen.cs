using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>
    /// WIN-001. All geometry comes from the approved 853x1844 reference. Artwork is
    /// shared; every displayed label/value and all interactions are live UI objects.
    /// The illustrated Home portrait is static until the character milestone.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        const float Width = 853, Height = 1844;
        readonly Dictionary<string, MapElement> map = new Dictionary<string, MapElement>();
        readonly Dictionary<string, Text> labels = new Dictionary<string, Text>();
        readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        Font serif, bold, body, referenceBody;
        RectTransform root, modal, statePanel, banner, portraitCover;
        Canvas canvas;
        HomeController controller;
        RectTransform xpTransform;
        Text stateTitle, stateBody, secondary, bannerText, privateLabel;
        Button secondaryButton;
        Vector2 lastSize;
        Rect lastSafeArea;
        bool reviewMode;
        AutoSpriteStudioScreen autoSpriteStudio;
        public AutoSpriteAvatarView AutoSpriteAvatar { get; private set; }
        public Button CharacterButton => buttons["character"];
        public Button LanguageButton => buttons["language"];
        public string LanguagePreference => controller.Model.LanguagePreference;
        string requestedCapture;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        static Color Gold => SystemUI.Theme.gold;

        [Serializable] sealed class PixelMap { public MapElement[] elements; }
        [Serializable] sealed class MapElement
        {
            public string id, kind;
            public Box rect;
            public Box glyph_bounds;
            public Locales locale_overrides;
        }
        [Serializable] sealed class Box
        {
            public float x, y, width, height;
            public Rect Rect => new Rect(x, y, width, height);
        }
        [Serializable] sealed class Locales { public LocaleBox es; }
        [Serializable] sealed class LocaleBox { public Box rect, glyph_bounds; }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            reviewMode = Application.isEditor || HasArgument("-sologym-review");
            requestedCapture = Argument("-sologym-capture");
            if (AutoSpriteSession.Requested)
            {
                AutoSpriteSession.Initialize();
                if (AutoSpriteSession.CaptureTaken || AutoSpriteSession.SmokeStarted) requestedCapture = null;
            }
            serif = SystemUI.Theme.heading;
            bold = SystemUI.Theme.headingBold;
            body = SystemUI.Theme.body;
            referenceBody = Resources.Load<Font>("Fonts/NotoSerif-Regular");
            var data = JsonUtility.FromJson<PixelMap>(Resources.Load<TextAsset>("Home/PixelMap").text);
            foreach (var element in data.elements) map[element.id] = element;
            CreateCanvas();
            CreateArt();
            CreateText();
            controller = new HomeController(requestedCapture != null || AutoSpriteSession.SmokeStarted ? new HomeSnapshot() : null);
            controller.Changed += Render;
            controller.NavigationRequested += Navigate;
            controller.NoticeRequested += message => ShowNotice(message);
            controller.RetryRequested += () => ShowNotice(L("No live account service is connected in this local build.",
                "Esta versión local aún no está conectada a un servicio de cuentas."));
            CreateControls();
            CreateStateOverlays();
            var locale = Argument("-sologym-locale");
            if (locale != null) controller.SetLanguage(locale);
            if (AutoSpriteSession.Requested) controller.SetFemalePresentation(AutoSpriteSession.Appearance.bodyId.StartsWith("female-"));
            if (Enum.TryParse(Argument("-sologym-mode"), out HomeMode mode)) controller.SetMode(mode);
            Render(controller.Model);
            FitSafeArea();
            if (requestedCapture != null) StartCoroutine(Capture());
        }

        void CreateCanvas()
        {
            if (FindFirstObjectByType<Camera>() == null)
            {
                var cam = new GameObject("Home Camera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color32(3, 8, 17, 255);
                cam.cullingMask = 0;
            }
            var go = new GameObject("System UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            root = NewRect("Reference canvas 853x1844", go.transform, new Rect(0, 0, Width, Height));
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void CreateArt()
        {
            SystemUI.Art(root,new Rect(0,0,Width,Height),"Art/HomeBackground-v1");
            SystemUI.Wordmark(root,new Rect(42,13,366,83));
            SystemUI.Icon(root,new Rect(667,38,30,30),"globe",Silver);
            SystemUI.Icon(root,new Rect(780,28,46,48),"gear",Silver);
            SystemUI.Divider(root,149,117,614);
            var portraitPanel=SystemUI.Panel(root,new Rect(92,164,670,930),PanelStyle.Glass,true);
            portraitPanel.color=new Color(1,1,1,.7f);
            SystemUI.Panel(root,new Rect(591,198,145,48),PanelStyle.Slot);
            SystemUI.Icon(root,new Rect(594,200,43,42),"coin",Gold);
            SystemUI.Icon(root,new Rect(130,196,54,90),"sigil",Silver);
            SystemUI.Panel(root,new Rect(121,307,614,31),PanelStyle.Slot);
            SystemUI.Panel(root,new Rect(243,314,329,18),PanelStyle.Track);
            xpTransform=SystemUI.Panel(root,new Rect(246,317,200,12),PanelStyle.Fill).rectTransform;
            if (AutoSpriteSession.Requested)
            {
                AutoSpriteAvatar = gameObject.AddComponent<AutoSpriteAvatarView>();
                AutoSpriteAvatar.Mount(root, new Rect(253, 378, 337, 690));
            }
            else
            {
                var portrait=SystemUI.Art(root,new Rect(245,386,350,708),"Art/KaiPortrait-v1");
                portrait.uvRect=new Rect(.155f,.023f,.69f,.958f);
            }
            foreach(var slot in new[]{"head","torso","hands","legs","feet","back"})
            {
                var r=Bounds("gear."+slot+".control");SystemUI.Panel(root,r,slot=="hands"?PanelStyle.Selected:PanelStyle.Slot);
                SystemUI.Icon(root,new Rect(r.x+25,r.y+16,r.width-50,r.height-60),slot=="back"?"backpack":slot,slot=="hands"?Cyan:Silver);
            }
            SystemUI.Panel(root,new Rect(74,1118,704,97),PanelStyle.Glass);
            // Keep the frame edge behind the section heading, clear of both language labels.
            var statsHeadingSurface=SystemUI.Node("Stats heading surface",root,new Rect(275,1099,307,29)).gameObject.AddComponent<Image>();
            statsHeadingSurface.color=SystemUI.Theme.glass;statsHeadingSurface.raycastTarget=false;
            SystemUI.Icon(root,new Rect(127,1142,55,56),"power",Silver);
            SystemUI.Icon(root,new Rect(349,1142,58,59),"guard",Silver);
            SystemUI.Icon(root,new Rect(574,1142,60,59),"focus",Silver);
            SystemUI.Panel(root,new Rect(26,1229,800,426),PanelStyle.Glass,true);
            SystemUI.Icon(root,new Rect(70,1245,45,47),"sigil",Silver);
            SystemUI.Icon(root,new Rect(76,1320,63,42),"dumbbell",Silver);
            SystemUI.Icon(root,new Rect(161,1364,23,23),"clock",Silver);
            SystemUI.Icon(root,new Rect(78,1441,27,27),"warning",Gold);
            SystemUI.Panel(root,new Rect(52,1495,748,89),PanelStyle.Primary);
            SystemUI.Icon(root,new Rect(262,1596,32,50),"flame",Gold);
            SystemUI.Panel(root,new Rect(0,1675,853,169),PanelStyle.Glass);
            foreach(var nav in new[]{"system","train","tower","gear","gym"})
            {
                if(nav=="system")SystemUI.Panel(root,Bounds("navigation.system.control"),PanelStyle.Outline);
                SystemUI.Icon(root,Bounds("navigation."+nav+".icon"),nav=="system"?"sigil":nav=="gear"?"helmet":nav,nav=="system"?Cyan:Silver);
            }
        }

        void CreateText()
        {
            Label("header.language.label", 24, body);
            Label("header.title", 42, bold);
            Label("identity.username", 46, bold);
            Label("identity.class", 23, serif);
            Label("identity.level_label", 24, serif);
            Label("identity.level_value", 43, serif);
            Label("wallet.value", 38, bold, Gold);
            Label("fitness_xp.label", 22, body);
            Label("fitness_xp.value", 21, body);
            foreach (var slot in new[] {"head", "torso", "hands", "legs", "feet", "back"})
                Label("gear." + slot + ".label", 21, body, slot == "hands" ? Cyan : Silver);
            Label("combat_stats.heading", 27, body);
            foreach (var stat in new[] {"power", "guard", "focus"})
            {
                Label("combat_stats." + stat + ".label", 21, body);
                Label("combat_stats." + stat + ".value", 43, serif);
            }
            Label("today.heading", 35, bold);
            Label("today.name", 41, bold);
            Label("today.duration", 25, body);
            Label("today.difficulty", 25, body);
            Label("today.body", 24, body);
            Label("today.gap_text", 24, body, Gold);
            Label("today.cta_label", 47, bold);
            Label("consistency.value", 24, body);
            Label("consistency.body", 18, body, new Color32(167, 201, 227, 255));
            foreach (var nav in new[] {"system", "train", "tower", "gear", "gym"})
                Label("navigation." + nav + ".label", 26, serif, nav == "system" ? Cyan : Silver);
        }

        void CreateControls()
        {
            Hit("language", new Rect(650, 15, 98, 85), () => ShowLanguage());
            Hit("settings", new Rect(760, 10, 82, 85), () => ShowSettings());
            Hit("character", new Rect(250, 363, 345, 718), () => controller.OpenWindow("WIN-014"));
            foreach (var slot in new[] {"head", "torso", "hands", "legs", "feet", "back"})
            {
                string current = slot;
                Hit("gear_" + slot, Bounds("gear." + slot + ".control"), () => controller.OpenGearSlot(current));
            }
            Hit("primary", new Rect(54, 1495, 741, 90), () => controller.ActivatePrimary());
            Hit("consistency", new Rect(257, 1588, 435, 70), () => controller.OpenWindow("WIN-031"));
            string[] ids = { "WIN-001", "WIN-015", "WIN-037", "WIN-032", "WIN-043" };
            string[] names = { "system", "train", "tower", "gear", "gym" };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                Hit("nav_" + names[i], Bounds("navigation." + names[i] + ".control"), () => controller.OpenWindow(id));
            }
        }

        void CreateStateOverlays()
        {
            // Alternate Today states reuse the complete panel, not per-control renders.
            statePanel = NewRect("Alternate Today state", root, new Rect(52, 1300, 738, 180));
            var background = statePanel.gameObject.AddComponent<Image>();
            background.color = new Color32(5, 16, 30, 251);
            background.raycastTarget = false;
            stateTitle = TextAt("State title", statePanel, new Rect(28, 6, 672, 57), 35, bold);
            stateBody = TextAt("State message", statePanel, new Rect(28, 69, 667, 97), 25, body);
            stateBody.horizontalOverflow = HorizontalWrapMode.Wrap;

            banner = NewRect("Connection status", root, new Rect(260, 889, 330, 134));
            var shade = banner.gameObject.AddComponent<Image>();
            shade.color = new Color32(4, 14, 27, 245); shade.raycastTarget = false;
            bannerText = TextAt("Connection text", banner, new Rect(12, 9, 305, 112), 23, body);
            bannerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            privateLabel = TextAt("Profile privacy", root, new Rect(540, 264, 198, 30), 23, body, Cyan);
            portraitCover = NewRect("Unavailable profile illustration", root, new Rect(250, 350, 350, 738));
            var cover = portraitCover.gameObject.AddComponent<Image>();
            cover.color = new Color32(4, 13, 25, 255); cover.raycastTarget = false;
            var placeholder = TextAt("Profile placeholder", portraitCover, new Rect(20, 270, 310, 200), 29, serif);
            placeholder.alignment = TextAnchor.MiddleCenter;
            placeholder.horizontalOverflow = HorizontalWrapMode.Wrap;
            secondaryButton = DialogButton(root, new Rect(122, 1030, 608, 53), "", () => controller.OpenSavedRecord());
            secondary = secondaryButton.GetComponentInChildren<Text>();
            secondaryButton.gameObject.SetActive(false);
        }

        void Render(HomeViewModel model)
        {
            bool es = model.Language == "es";
            foreach (var pair in labels)
            {
                Rect rect = Bounds(pair.Key, es);
                if(es&&pair.Key=="fitness_xp.label")rect=new Rect(124,306,115,33);
                Place(pair.Value.rectTransform, rect);
                if (pair.Key != "header.language.label" &&
                    (pair.Key.StartsWith("fitness_xp.") || pair.Key.StartsWith("gear.") ||
                     pair.Key == "combat_stats.heading" || pair.Key.EndsWith(".label") && pair.Key.StartsWith("combat_stats.") ||
                     pair.Key == "today.duration" || pair.Key == "today.difficulty" || pair.Key == "today.body" ||
                     pair.Key == "today.gap_text" || pair.Key.StartsWith("consistency.")))
                    pair.Value.font = es ? body : referenceBody;
            }
            labels["wallet.value"].font = es ? bold : serif;
            foreach (var stat in new[] { "power", "guard", "focus" })
                labels["combat_stats." + stat + ".value"].font = es ? serif : bold;
            Set("header.language.label", es ? "ES" : "EN");
            Set("header.title", model.Title);
            Set("identity.username", model.HasConfirmedData ? model.UserName : "—");
            Set("identity.class", Track(model.ClassName.ToUpperInvariant()));
            Set("identity.level_label", model.HasConfirmedData ? (es ? "Nivel de\nentrenamiento" : "Fitness level") : "");
            Set("identity.level_value", model.FitnessLevelValue);
            Set("wallet.value", model.CoinsValue);
            Set("fitness_xp.label", es ? "XP de\nentrenamiento" : model.XpLabel);
            Set("fitness_xp.value", model.XpProgressLabel);
            var xpFit = labels["fitness_xp.label"].GetComponent<SystemTextFit>();
            xpFit.maximum = es ? 18 : 22;
            xpFit.minimum = 14;
            labels["fitness_xp.label"].lineSpacing = es ? .75f : 1;
            foreach (var slot in model.GearSlots) Set("gear." + slot.Id + ".label", slot.Label.ToUpperInvariant());
            Set("combat_stats.heading", model.CombatStatsLabel.ToUpperInvariant());
            Set("combat_stats.power.label", model.PowerLabel.ToUpperInvariant());
            Set("combat_stats.power.value", model.PowerValue);
            Set("combat_stats.guard.label", model.GuardLabel.ToUpperInvariant());
            Set("combat_stats.guard.value", model.GuardValue);
            Set("combat_stats.focus.label", model.FocusLabel.ToUpperInvariant());
            Set("combat_stats.focus.value", model.FocusValue);
            labels["combat_stats.focus.label"].GetComponent<SystemTextFit>().maximum = es ? 18 : 21;
            Set("today.heading", model.ShowPlanSummary ? model.PrimaryTitle.ToUpperInvariant() : model.TodayLabel.ToUpperInvariant());
            Set("today.name", model.PlanName);
            Set("today.duration", es ? "Aprox. 24 min" : "About 24 min");
            Set("today.difficulty", model.Readiness == HomeReadiness.LowEnergy ? (es ? "Ligero" : "Light") : (es ? "Medio" : "Medium"));
            Set("today.body", model.PrimaryBody);
            Set("today.gap_text", model.PlanWarning);
            Set("today.cta_label", model.PrimaryAction.ToUpperInvariant());
            Set("consistency.value", model.ConsistencyLabel);
            Set("consistency.body", model.ConsistencyNote);
            string[] names = { "system", "train", "tower", "gear", "gym" };
            for (int i = 0; i < names.Length; i++)
            {
                Set("navigation." + names[i] + ".label", model.Navigation[i].Label);
            }
            // Three pixels of decorative glow sit outside each end of the fill.
            Place(xpTransform, new Rect(246, 317, 323 * model.XpFraction, 12));
            xpTransform.gameObject.SetActive(model.XpFraction > 0);
            statePanel.gameObject.SetActive(!model.ShowPlanSummary);
            stateTitle.text = model.PrimaryTitle;
            stateBody.text = model.PrimaryBody;
            foreach (var key in new[] {"today.name", "today.duration", "today.difficulty", "today.body", "today.gap_text"})
                labels[key].gameObject.SetActive(model.ShowPlanSummary);
            buttons["primary"].interactable = model.PrimaryEnabled;
            buttons["primary"].gameObject.name = "Primary / " + model.PrimaryAction;
            banner.gameObject.SetActive(!string.IsNullOrEmpty(model.StatusBanner));
            bannerText.text = model.StatusBanner;
            privateLabel.gameObject.SetActive(model.IsPrivateProfile);
            privateLabel.text = model.PrivateProfileLabel;
            portraitCover.gameObject.SetActive(!model.HasConfirmedData || model.Mode == HomeMode.Setup);
            portraitCover.GetComponentInChildren<Text>().text = model.Mode == HomeMode.Setup
                ? L("Create your character", "Crea tu personaje") : model.LoadingLabel;
            secondaryButton.gameObject.SetActive(model.ShowSecondarySavedAction);
            secondary.text = model.SecondarySavedAction;
        }

        void Navigate(HomeNavigation destination)
        {
            if (reviewMode && (destination.WindowId == "WIN-015" || destination.WindowId == "WIN-016") &&
                string.IsNullOrEmpty(destination.SessionId))
            {
                CloseModal(); root.gameObject.SetActive(false);
                var training = new GameObject("Training flow").AddComponent<TrainingScreen>();
                training.Initialize(controller.Model.Language, () =>
                {
                    controller.SetLanguage(training.Language);
                    Destroy(training.gameObject); root.gameObject.SetActive(true); FitSafeArea();
                }, destination.WindowId == "WIN-016" ? "readiness" : "training");
                return;
            }
            if ((destination.WindowId == "WIN-014" || destination.WindowId == "WIN-032") && AutoSpriteSession.Requested)
            {
                OpenAutoSpriteStudio();
                return;
            }
            if (destination.WindowId == "WIN-001") { CloseModal(); return; }
            if (destination.WindowId == "WIN-055") { ShowSettings(); return; }
            string title = DestinationName(destination.WindowId);
            string bodyText = controller.FutureWindowNotice(destination.WindowId);
            if (destination.RequiresReadinessRecheck)
                bodyText += "\n\n" + L("Your next step is a readiness check, followed by plan review. No workout has started.",
                    "El siguiente paso es revisar tu estado y después tu rutina. No se ha iniciado un entrenamiento.");
            if (destination.ReadOnly)
                bodyText += "\n\n" + L("The saved record remains preserved. No exercise is resumed.",
                    "El registro guardado se conserva. No se reanuda ningún ejercicio.");
            ShowNotice(bodyText, title);
        }

        void OpenAutoSpriteStudio()
        {
            if (autoSpriteStudio != null) return;
            root.gameObject.SetActive(false);
            autoSpriteStudio = new GameObject("AutoSprite appearance").AddComponent<AutoSpriteStudioScreen>();
            autoSpriteStudio.Initialize(controller.Model.Language, CloseAutoSpriteStudio, CloseAutoSpriteStudio);
        }

        void CloseAutoSpriteStudio()
        {
            if (autoSpriteStudio != null) Destroy(autoSpriteStudio.gameObject);
            autoSpriteStudio = null;
            AutoSpriteAvatar.RefreshView();
            controller.SetFemalePresentation(AutoSpriteSession.Appearance.bodyId.StartsWith("female-"));
            root.gameObject.SetActive(true);
            if (AutoSpriteSession.Language != controller.Model.Language)
                controller.SetLanguage(AutoSpriteSession.Language);
            FitSafeArea();
        }

        void ShowLanguage()
        {
            OpenModal(L("Language", "Idioma"), 410);
            DialogButton(modal, new Rect(38, 105, 584, 66), "English", () => { controller.SetLanguage("en"); CloseModal(); });
            DialogButton(modal, new Rect(38, 188, 584, 66), "Español", () => { controller.SetLanguage("es"); CloseModal(); });
            DialogButton(modal, new Rect(38, 271, 584, 66), L("Use device language", "Usar idioma del dispositivo"),
                () => { controller.SetLanguage("auto"); CloseModal(); });
        }

        void ShowSettings()
        {
            OpenModal(L("Settings", "Ajustes"), reviewMode ? 510 : 410);
            DialogButton(modal, new Rect(38, 110, 584, 66), L("Language", "Idioma"), ShowLanguage);
            if (reviewMode)
                DialogButton(modal, new Rect(38, 195, 584, 66), L("Preview scenarios", "Escenarios de vista previa"), ShowScenarios);
            DialogButton(modal, new Rect(38, reviewMode ? 280 : 195, 584, 66),
                L("Back to Welcome", "Volver a Bienvenida"), () => SceneManager.LoadScene("Welcome"));
            var note = TextAt("Local build notice", modal, new Rect(38, reviewMode ? 371 : 290, 575, 74), 21, body);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.text = L("SoloGym · Home window\nLocal build with fictional profile data.",
                "SoloGym · Ventana de inicio\nVersión local con datos de perfil ficticios.");
        }

        void ShowScenarios()
        {
            OpenModal(L("Preview scenarios", "Escenarios de vista previa"), 980);
            var modes = new[] { HomeMode.Training, HomeMode.Recovery, HomeMode.Saved, HomeMode.Completed,
                HomeMode.Setup, HomeMode.Rest, HomeMode.Review, HomeMode.Loading, HomeMode.Error };
            string[] en = { "Training", "Recovery", "Saved session", "Completed", "Setup", "Rest today", "Review needed", "Loading", "Error" };
            string[] es = { "Entrenamiento", "Recuperación", "Sesión guardada", "Completado", "Configuración", "Descansar hoy", "Revisión necesaria", "Cargando", "Error" };
            for (int i = 0; i < modes.Length; i++)
            {
                HomeMode mode = modes[i];
                DialogButton(modal, new Rect(38, 105 + i * 69, 584, 58), L(en[i], es[i]), () =>
                {
                    controller.SetReadiness(HomeReadiness.Ready);
                    controller.SetTeenProfile(false);
                    controller.SetGuidanceRequired(false);
                    controller.SetMode(mode);
                    CloseModal();
                });
            }
            DialogButton(modal, new Rect(38, 745, 282, 58), L("Offline", "Sin conexión"), () =>
                { controller.SetConnectivity(HomeConnectivity.Offline); CloseModal(); });
            DialogButton(modal, new Rect(338, 745, 284, 58), L("Online", "En línea"), () =>
                { controller.SetConnectivity(HomeConnectivity.Online); CloseModal(); });
            DialogButton(modal, new Rect(38, 818, 584, 66), L("Teen · supervision needed", "Adolescente · requiere supervisión"), () =>
                { controller.SetMode(HomeMode.Training); controller.SetTeenProfile(true, false); CloseModal(); });
        }

        void ShowNotice(string message, string title = null)
        {
            OpenModal(title ?? L("System", "Sistema"), 540);
            var text = TextAt("Notice", modal, new Rect(38, 114, 584, 283), 27, body);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.alignment = TextAnchor.UpperLeft;
            text.text = message;
            DialogButton(modal, new Rect(38, 425, 584, 66), L("Return to System", "Volver al Sistema"), CloseModal);
        }

        void OpenModal(string title, float height)
        {
            CloseModal();
            var overlay = NewRect("Modal shade", root, new Rect(0, 0, Width, Height));
            var image = overlay.gameObject.AddComponent<Image>(); image.color = new Color(0, .015f, .035f, .8f);
            modal = NewRect("System panel", overlay, new Rect(96.5f, (Height - height) / 2, 660, height));
            var frame=modal.gameObject.AddComponent<SystemPanel>();frame.theme=SystemUI.Theme;frame.ornaments=true;
            var heading = TextAt("Dialog heading", modal, new Rect(38, 27, 525, 56), 35, bold);
            heading.text = title;
            DialogButton(modal, new Rect(574, 16, 66, 66), "×", CloseModal);
        }

        void CloseModal()
        {
            if (modal == null) return;
            GameObject overlay = modal.parent.gameObject;
            modal = null;
            overlay.SetActive(false);
            Destroy(overlay);
        }

        Button DialogButton(Transform parent, Rect rect, string caption, Action action)
        {return SystemUI.Button(parent,rect,caption,action);}

        void Hit(string key, Rect rect, Action action)
        {
            var node = NewRect("Control / " + key, root, rect);
            var img = node.gameObject.AddComponent<Image>(); img.color = Color.clear;
            var button = node.gameObject.AddComponent<Button>(); button.targetGraphic = img;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { StartCoroutine(Flash(img)); action(); });
            buttons[key] = button;
        }

        IEnumerator Flash(Image image)
        {
            image.color = new Color(.1f, .8f, 1, .12f);
            yield return new WaitForSecondsRealtime(.1f);
            if (image != null) image.color = Color.clear;
        }

        Text Label(string key, int size, Font font, Color? color = null)
        {
            Rect rect = Bounds(key);
            var result = TextAt(key, root, rect, size, font, color);
            var fit=result.gameObject.AddComponent<SystemTextFit>();fit.maximum=size;result.alignment=TextAnchor.MiddleCenter;
            labels[key] = result;
            return result;
        }

        Text TextAt(string name,Transform parent,Rect rect,int size,Font font,Color? color=null)
        {var text=SystemUI.Text(parent,rect,"",size,font,color);text.name=name;return text;}

        static RectTransform NewRect(string name,Transform parent,Rect rect)
        {return SystemUI.Node(name,parent,rect);}

        static void Place(RectTransform rt, Rect rect)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(rect.x, -rect.y); rt.sizeDelta = new Vector2(rect.width, rect.height);
        }

        Rect Bounds(string key, bool spanish = false)
        {
            if (!map.TryGetValue(key, out var entry)) throw new InvalidOperationException("Missing pixel mapping: " + key);
            var translated=entry.locale_overrides?.es?.rect;
            // JsonUtility can materialize omitted nested boxes as zero-sized objects.
            return spanish && translated!=null && translated.width>0 && translated.height>0 ? translated.Rect : entry.rect.Rect;
        }

        Rect TextBounds(string key, bool spanish)
        {
            var entry = map[key];
            if (spanish && entry.locale_overrides?.es?.glyph_bounds != null)
                return entry.locale_overrides.es.glyph_bounds.Rect;
            return entry.glyph_bounds != null ? entry.glyph_bounds.Rect : Bounds(key, spanish);
        }

        static Rect Expanded(Rect r, float pad)
        {
            float x = Mathf.Max(0, r.x - pad), y = Mathf.Max(0, r.y - pad);
            return new Rect(x, y, Mathf.Min(Width - x, r.width + pad * 2), Mathf.Min(Height - y, r.height + pad * 2));
        }

        void Set(string id, string value) { labels[id].text = value ?? ""; }
        static string Track(string value) { return string.Join(" ", value.ToCharArray()); }
        string L(string en, string es) { return controller != null && controller.Model.Language == "es" ? es : en; }

        string DestinationName(string id)
        {
            switch (id)
            {
                case "WIN-014": return L("Character", "Personaje");
                case "WIN-015": return L("Training", "Entrenamiento");
                case "WIN-016": return L("Readiness", "Estado de hoy");
                case "WIN-028": return L("Recovery", "Recuperación");
                case "WIN-030": return L("Session details", "Detalle de sesión");
                case "WIN-031": return L("Consistency", "Constancia");
                case "WIN-032": return L("Equipment", "Equipo");
                case "WIN-037": return L("Tower", "Torre");
                case "WIN-043": return L("Interdimensional Gym", "Gimnasio interdimensional");
                default: return L("Profile setup", "Configuración del perfil");
            }
        }

        void Update()
        {
            if (autoSpriteStudio != null) return;
            if (Screen.width != lastSize.x || Screen.height != lastSize.y || Screen.safeArea != lastSafeArea) FitSafeArea();
            if (Input.GetKeyDown(KeyCode.Escape)) CloseModal();
            if (Input.GetKeyDown(KeyCode.F8)) { reviewMode = true; ShowScenarios(); }
        }

        void FitSafeArea()
        {
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / Width, safe.height / Height);
            SystemViewport.Fit(root,Width,Height);
            lastSize = new Vector2(Screen.width, Screen.height); lastSafeArea = safe;
        }

        IEnumerator Capture()
        {
            for (int i = 0; i < 6; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(requestedCapture)));
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(requestedCapture, capture.EncodeToPNG());
            Destroy(capture);
            Debug.Log("SOLOGYM_CAPTURE " + requestedCapture);
            if (AutoSpriteSession.Requested) AutoSpriteSession.CaptureTaken = true;
            if (HasArgument("-sologym-smoke")) yield return SmokeControls();
            if (!AutoSpriteSession.Requested || HasArgument("-sologym-smoke") || HasArgument("-sologym-quit-after-capture")) Application.Quit();
        }

        IEnumerator SmokeControls()
        {
            bool ok = true;
            buttons["language"].onClick.Invoke(); yield return null;
            ok &= modal != null;
            var choices = modal.GetComponentsInChildren<Button>();
            foreach (var choice in choices)
                if (choice.GetComponentInChildren<Text>()?.text == "Español") { choice.onClick.Invoke(); break; }
            ok &= controller.Model.Language == "es";
            controller.SetMode(HomeMode.Saved);
            HomeNavigation captured = null; controller.NavigationRequested += r => captured = r;
            buttons["primary"].onClick.Invoke(); yield return null;
            ok &= captured != null && captured.WindowId == "WIN-016" && captured.RequiresReadinessRecheck;
            CloseModal();
            controller.SetReadiness(HomeReadiness.Ill);
            ok &= controller.Model.Mode == HomeMode.Rest;
            controller.SetReadiness(HomeReadiness.Ready);
            controller.SetMode(HomeMode.Training);
            controller.SetTeenProfile(true, false);
            ok &= controller.Model.Mode == HomeMode.Review;
            controller.SetConnectivity(HomeConnectivity.Offline);
            buttons["nav_gym"].onClick.Invoke(); yield return null;
            ok &= modal != null && controller.Model.IsOffline;
            CloseModal(); controller.SetTeenProfile(false); controller.SetConnectivity(HomeConnectivity.Online);
            controller.SetLanguage(Argument("-sologym-locale") ?? "en");
            controller.SetMode(HomeMode.Training);
            string result = "{\"passed\":" + (ok ? "true" : "false") + ",\"checks\":[\"language control\",\"saved-session readiness route\",\"illness recovery\",\"teen supervision\",\"offline gym notice\"]}";
            File.WriteAllText(Path.ChangeExtension(requestedCapture, ".smoke.json"), result);
            Debug.Log("SOLOGYM_SMOKE " + result);
            if (!ok) Application.Quit(2);
        }

        static bool HasArgument(string key) { return Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0; }
        static string Argument(string key)
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        void OnDestroy() { controller?.Dispose(); }
    }
}
