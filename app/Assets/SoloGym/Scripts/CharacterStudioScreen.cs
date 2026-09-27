using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Full-screen CAS-style character studio (single avatar host, overlay carousel).</summary>
    public sealed class CharacterStudioScreen : MonoBehaviour
    {
        enum StudioCategory { None, Skin, Face, Body, Gear }
        enum FaceRow { Hair, Eyes, Mouth }

        const float W = PortalFrameLayout.PageWidth, H = PortalFrameLayout.PageHeight;
        static readonly Rect StageRect = new Rect(94, 400, 665, 740);
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;

        RectTransform root, page, modal, overlaySheet, carouselHost, hitLayer;
        Font serif, bold, body;
        CharacterCustomizationController controller;
        AvatarViewHost viewHost;
        StudioCategory category = StudioCategory.Skin;
        FaceRow faceRow = FaceRow.Hair;
        string gearSlot = "torso";
        string sourceReviewCamera = "front";
        string illustratedInputError;
        Action back, leave;
        Action<string> onCheckpoint;
        string capture;
        Text status, categoryLabel, backLabel, languageLabel, titleLabel, reviewLabel, leaveLabel, stageHint, mirrorLabel;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
        readonly Dictionary<string, Text> dockLabels = new Dictionary<string, Text>();
        readonly Dictionary<string, LayeredAvatar.Part> thumbnailParts = new Dictionary<string, LayeredAvatar.Part>();
        readonly Dictionary<string, Text> motionLabels = new Dictionary<string, Text>();
        Vector2 lastSize;
        Rect lastSafe;
        bool built;

        public void Initialize(string language, bool review, Action onBack, Action onExit,
            Action<string> checkpointHandler = null, string capturePath = null)
        {
            back = onBack;
            leave = onExit;
            onCheckpoint = checkpointHandler;
            capture = capturePath;
            serif = SystemUI.Theme.heading;
            bold = SystemUI.Theme.headingBold;
            body = SystemUI.Theme.body;
            // Illustrated choices belong to the proof host, never the legacy appearance draft.
            controller = new CharacterCustomizationController(review || AvatarViewHost.IllustratedRequested);
            controller.SetLanguage(language);
            var node = new GameObject("Character studio canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvas.pixelPerfect = true;
            root = SystemUI.Node("Studio 853x1844", node.transform, new Rect(0, 0, W, H));
            controller.Changed += OnModelChanged;
            controller.ExitRequested += Exit;
            controller.CheckpointRequested += id =>
            {
                if (onCheckpoint != null) onCheckpoint(id);
                else Notice(controller.Model.Copy("setup_complete_title"), controller.Model.Copy("setup_complete_body"));
            };
            BuildShell();
            OnModelChanged(controller.Model);
            Fit();
            if (viewHost.UsesIllustrated) ConfigureIllustratedArguments();
            else if (review)
            {
                if (Argument("-sologym-avatar-variant") == "alternate")
                {
                    controller.SelectSkin("deep");
                    controller.SelectHair("swept");
                    controller.Equip("torso", "");
                    controller.Equip("hands", "");
                }
                string initialView = Argument("-sologym-character-view");
                if (initialView == "face" || initialView == "gear" || initialView == "body")
                    SelectCategory(ParseCategory(initialView));
                string camera = Argument("-sologym-character-camera");
                if (!string.IsNullOrEmpty(camera) && viewHost.UsesSource3D)
                {
                    if (camera == "face") SelectCategory(StudioCategory.Face);
                    else
                    {
                        sourceReviewCamera = camera;
                        viewHost.SetSourceCameraPreset(camera);
                    }
                }
                if (float.TryParse(Argument("-sologym-character-shape"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float shape)) viewHost.SetBodyShape(shape);
                string pose = Argument("-sologym-character-action");
                if (!string.IsNullOrEmpty(pose)) viewHost.SetPose(pose, .37f);
            }
            if (HasArgument("-sologym-illustrated-proof")) StartCoroutine(IllustratedProof());
            else if (HasArgument("-sologym-character-proof")) StartCoroutine(SourceProof());
            else if (capture != null) StartCoroutine(Capture());
            else if (HasArgument("-sologym-avatar-reference-check")) StartCoroutine(ReferenceCheck());
        }

        public void Resume(string language)
        {
            gameObject.SetActive(true);
            controller.SetLanguage(language);
            Fit();
        }

        void BuildShell()
        {
            if (built) return;
            built = true;
            page = SystemUI.Node("Studio page", root, new Rect(0, 0, W, H));
            SystemUI.Art(page, new Rect(0, 0, W, H), "Art/PortalBackground-v1");
            SystemUI.Wordmark(page, new Rect(174, 149, 510, 112));
            SystemUI.Icon(page, new Rect(43, 47, 28, 34), "back", Silver);
            SystemUI.Icon(page, new Rect(421, 1728, 10, 25), "sigil", Silver);
            backLabel = Live("Back", new Rect(92, 53, 90, 25), L("Back", "Volver"), 31);
            languageLabel = Live("Language", new Rect(760, 56, 38, 20), controller.Model.Language.ToUpperInvariant(), 27);
            Hit("back", new Rect(30, 20, 180, 100), Back);
            Hit("language", new Rect(680, 20, 130, 100), Language);
            titleLabel = Live("Title", new Rect(103, 326, 647, 55),
                controller.Model.Copy("window_title"), 62, Silver, bold);
            var shade = SystemUI.Node("Stage shade", page, StageRect);
            viewHost = shade.gameObject.AddComponent<AvatarViewHost>();
            viewHost.Mount(page, StageRect);
            BuildHitRegions();
            stageHint = SystemUI.Caption(page, new Rect(120, 1144, 613, 28), "", 20, body, Cyan);
            overlaySheet = SystemUI.Node("Option overlay", page, new Rect(62, 1254, 729, 242));
            var sheetPanel = overlaySheet.gameObject.AddComponent<SystemPanel>();
            sheetPanel.theme = SystemUI.Theme;
            sheetPanel.style = PanelStyle.Glass;
            sheetPanel.ornaments = false;
            sheetPanel.raycastTarget = false;
            categoryLabel = SystemUI.Text(overlaySheet, new Rect(24, 12, 680, 30), "", 24, serif, Silver);
            carouselHost = SystemUI.Node("Carousel host", overlaySheet, new Rect(12, 52, 705, 176));
            BuildCategoryDock();
            var primaryRect = PortalFrameLayout.PrimaryRect;
            var secondaryRect = PortalFrameLayout.SecondaryRect;
            if (viewHost.UsesIllustrated)
            {
                BuildIllustratedMotionControls(primaryRect);
                Secondary("leave", secondaryRect, L("Close preview", "Cerrar vista previa"), Exit);
            }
            else
            {
                Primary(() => controller.Continue(), controller.Model.Copy("continue"), true, primaryRect,
                    PortalFrameLayout.PrimaryCaptionRect);
                Secondary("leave", secondaryRect, controller.Model.Copy("not_now"), controller.Decline);
            }
            status = SystemUI.Text(page, new Rect(105, primaryRect.y - 36, 642, 28), "", 18, body,
                new Color32(255, 207, 161, 255), TextAnchor.MiddleCenter);
            if (controller.Model.ReviewMode)
                reviewLabel = SystemUI.Text(page, new Rect(80, 280, 693, 28),
                    L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"),
                    20, body, Cyan, TextAnchor.MiddleCenter);
        }

        void BuildHitRegions()
        {
            hitLayer = SystemUI.Node("Avatar hit regions", page, StageRect);
            // Unity raycasts the last sibling first: put broad regions behind precise ones.
            var regions = new List<CharacterReference.HitRegion>(CharacterReference.HitRegions);
            regions.Sort((a, b) => RegionArea(b).CompareTo(RegionArea(a)));
            foreach (var region in regions)
            {
                if (region.rect == null || region.rect.Length < 4) continue;
                float x = region.rect[0] * StageRect.width;
                float y = region.rect[1] * StageRect.height;
                float w = region.rect[2] * StageRect.width;
                float h = region.rect[3] * StageRect.height;
                string cat = region.category;
                var button = SystemUI.Hit(hitLayer, new Rect(x, y, w, h), () => SelectCategory(ParseCategory(cat)), "hit_" + region.id);
                controls["hit_" + region.id] = button;
            }
        }

        static float RegionArea(CharacterReference.HitRegion region) =>
            region.rect != null && region.rect.Length >= 4 ? region.rect[2] * region.rect[3] : 0;

        static StudioCategory ParseCategory(string id)
        {
            switch (id)
            {
                case "face": return StudioCategory.Face;
                case "body": return StudioCategory.Body;
                case "gear": return StudioCategory.Gear;
                default: return StudioCategory.Skin;
            }
        }

        void BuildCategoryDock()
        {
            const float y = 1188, h = 52, w = 139.4f, gap = 8, x0 = 62;
            AddDockButton("dock_skin", x0, y, w, h, "cat_skin", StudioCategory.Skin);
            AddDockButton("dock_face", x0 + (w + gap), y, w, h, "cat_face", StudioCategory.Face);
            AddDockButton("dock_body", x0 + 2 * (w + gap), y, w, h, "cat_body", StudioCategory.Body);
            AddDockButton("dock_gear", x0 + 3 * (w + gap), y, w, h, "cat_gear", StudioCategory.Gear);
            var mirrorPanel = SystemUI.Panel(page, new Rect(x0 + 4 * (w + gap), y, w, h), PanelStyle.Outline);
            mirrorPanel.rimWidth = 1.5f;
            Hit("dock_rotate", new Rect(x0 + 4 * (w + gap), y, w, h), () =>
            {
                viewHost?.Rotate();
                if (viewHost != null && viewHost.UsesIllustrated) RefreshIllustratedUI();
            });
            mirrorLabel = SystemUI.Caption(page, new Rect(x0 + 4 * (w + gap), y, w, h), RotationLabel(), 19, body, Cyan);
        }

        void AddDockButton(string key, float x, float y, float w, float h, string copyKey, StudioCategory cat)
        {
            var panel = SystemUI.Panel(page, new Rect(x, y, w, h),
                category == cat ? PanelStyle.Selected : PanelStyle.Outline);
            panel.raycastTarget = true;
            var b = panel.gameObject.AddComponent<Button>();
            b.targetGraphic = panel;
            b.onClick.AddListener(() => SelectCategory(cat));
            dockLabels[copyKey] = SystemUI.Caption(page, new Rect(x, y, w, h), controller.Model.Copy(copyKey), 20, body, Silver);
            controls[key] = b;
        }

        void SelectCategory(StudioCategory cat)
        {
            category = cat;
            ApplyCameraForCategory();
            RefreshDock();
            RebuildCarousel();
        }

        void ApplyCameraForCategory()
        {
            if (viewHost == null) return;
            if (viewHost.UsesIllustrated && !viewHost.Illustrated.IsLoaded) return;
            // The full-body regions do not describe the enlarged face camera.
            if (hitLayer != null) hitLayer.gameObject.SetActive(category != StudioCategory.Face);
            viewHost.SetCameraPreset(category == StudioCategory.Face
                ? CharacterReference.CameraPresetId.FaceCloseUp
                : CharacterReference.CameraPresetId.FullBody);
            if (viewHost.UsesIllustrated) return;
            if (viewHost.UsesSource3D && category != StudioCategory.Face)
                viewHost.SetSourceCameraPreset(sourceReviewCamera);
            viewHost.ApplyRecipe(controller.Model.Appearance);
        }

        void OnModelChanged(CharacterCustomizationViewModel model)
        {
            if (!built) BuildShell();
            if (!viewHost.UsesIllustrated) viewHost.ApplyRecipe(model.Appearance);
            ApplyCameraForCategory();
            RefreshCopy(model);
            if (status != null) status.text = viewHost.UsesIllustrated ? IllustratedError() : model.Error;
            if (continueButton != null)
            {
                continueButton.interactable = model.CanContinue;
                continueText.color = model.CanContinue ? Silver : SystemUI.Theme.muted;
            }
            RebuildCarousel();
            RefreshDock();
        }

        Button continueButton;
        Text continueText;

        void RefreshCopy(CharacterCustomizationViewModel model)
        {
            backLabel.text = L("Back", "Volver");
            languageLabel.text = model.Language.ToUpperInvariant();
            titleLabel.text = model.Copy("window_title");
            if (continueText != null) continueText.text = model.Copy("continue");
            leaveLabel.text = viewHost.UsesIllustrated ? L("Close preview", "Cerrar vista previa") : model.Copy("not_now");
            mirrorLabel.text = RotationLabel();
            stageHint.text = StageHintText();
            if (reviewLabel != null) reviewLabel.text = viewHost.UsesIllustrated
                ? L("ILLUSTRATED PREVIEW · NOT SAVED", "VISTA PREVIA ILUSTRADA · SIN GUARDAR")
                : L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR");
            foreach (var label in dockLabels) label.Value.text = viewHost.UsesIllustrated && label.Key == "cat_face"
                ? L("Hair", "Cabello") : model.Copy(label.Key);
            RefreshIllustratedMotionControls();
        }

        void Primary(Action action, string text, bool active, Rect buttonRect, Rect captionRect)
        {
            var surface = SystemUI.Panel(page, buttonRect, PanelStyle.Primary);
            surface.color = active ? Color.white : new Color(.5f, .6f, .7f, .7f);
            continueText = SystemUI.Caption(page, captionRect, text, 45, bold, active ? Silver : SystemUI.Theme.muted);
            continueButton = Hit("continue", buttonRect, action);
            continueButton.interactable = active;
        }

        void RefreshDock()
        {
            foreach (var kv in controls)
            {
                if (!kv.Key.StartsWith("dock_", StringComparison.Ordinal)) continue;
                var panel = kv.Value.GetComponent<SystemPanel>();
                if (panel == null) continue;
                panel.style = kv.Key == DockKey(category) ? PanelStyle.Selected : PanelStyle.Outline;
                panel.SetVerticesDirty();
            }
            if (stageHint != null) stageHint.text = StageHintText();
        }

        static string DockKey(StudioCategory cat)
        {
            switch (cat)
            {
                case StudioCategory.Face: return "dock_face";
                case StudioCategory.Body: return "dock_body";
                case StudioCategory.Gear: return "dock_gear";
                default: return "dock_skin";
            }
        }

        void RebuildCarousel()
        {
            if (carouselHost == null) return;
            for (int i = carouselHost.childCount - 1; i >= 0; i--)
            {
                var old = carouselHost.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            // The dictionary must not retain destroyed controls from another category.
            var stale = new List<string>();
            foreach (var entry in controls)
                if (entry.Key.StartsWith("skin_") || entry.Key.StartsWith("hair_") || entry.Key.StartsWith("eyes_")
                    || entry.Key.StartsWith("mouth_") || entry.Key.StartsWith("fit_") || entry.Key.StartsWith("gslot_")
                    || entry.Key.StartsWith("equip_") || entry.Key.StartsWith("frow_")
                    || entry.Key.StartsWith("illustrated_")) stale.Add(entry.Key);
            foreach (var key in stale) controls.Remove(key);
            var model = controller.Model;
            bool es = model.Language == "es";
            categoryLabel.text = CategoryTitle(model);
            if (viewHost.UsesIllustrated)
            {
                BuildIllustratedCarousel();
                return;
            }
            switch (category)
            {
                case StudioCategory.Skin: BuildSkinCarousel(model); break;
                case StudioCategory.Face: BuildFaceCarousel(model, es); break;
                case StudioCategory.Body: BuildFitTiles(model, es); break;
                case StudioCategory.Gear:
                    BuildGearSlotRow(model);
                    BuildGearItems(model, es);
                    break;
            }
        }

        string CategoryTitle(CharacterCustomizationViewModel model)
        {
            if (viewHost.UsesIllustrated)
            {
                switch (category)
                {
                    case StudioCategory.Face: return L("Hair color", "Color de cabello");
                    case StudioCategory.Body: return L("Body · Normal", "Cuerpo · Normal");
                    case StudioCategory.Gear: return L("Outfit", "Atuendo");
                    default: return L("Skin", "Piel") + " · " + SkinLabel(viewHost.Illustrated.SkinPaletteId);
                }
            }
            switch (category)
            {
                case StudioCategory.Face: return model.Copy("cat_face") + " · " + FaceRowLabel(faceRow);
                case StudioCategory.Body: return model.Copy("cat_body");
                case StudioCategory.Gear: return model.Copy("cat_gear") + " · " + model.Copy("slot_" + gearSlot);
                default: return model.Copy("cat_skin") + " · " + SkinLabel(model.Appearance.skinPaletteId);
            }
        }

        void ConfigureIllustratedArguments()
        {
            try
            {
                string presentation = ChoiceArgument("-sologym-character-presentation", "male", "male", "female");
                string camera = ChoiceArgument("-sologym-character-camera", "studio", "studio", "gameplay", "front", "full");
                if (camera == "front" || camera == "full") camera = "studio";
                var skins = new List<string> { "source" };
                foreach (var palette in AvatarCustomizationCatalog.SkinPalettes)
                    if (palette.enabled) skins.Add(palette.id);
                string skin = ChoiceArgument("-sologym-character-skin", "source", skins.ToArray());
                string hair = ChoiceArgument("-sologym-character-hair", "black", "black", "brown", "silver");
                string outfit = ChoiceArgument("-sologym-character-outfit", "equipped", "base", "equipped");
                string action = ChoiceArgument("-sologym-character-action", "idle", "bind", "idle", "walk", "jab");
                string initialView = ChoiceArgument("-sologym-character-view", "skin", "skin", "face", "body", "gear");
                float seconds = .55f;
                if (HasArgument("-sologym-character-time") && (!float.TryParse(Argument("-sologym-character-time"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out seconds) || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0))
                    throw new ArgumentException("-sologym-character-time requires finite, non-negative seconds.");
                bool freeze = capture != null || HasArgument("-sologym-illustrated-proof");
                if (HasArgument("-sologym-character-freeze"))
                {
                    string value = Argument("-sologym-character-freeze");
                    if (string.IsNullOrEmpty(value) || value.StartsWith("-", StringComparison.Ordinal)) value = "1";
                    if (value != "0" && value != "1" && value != "false" && value != "true")
                        throw new ArgumentException("-sologym-character-freeze requires 0, 1, false or true.");
                    freeze = value == "1" || value == "true";
                }
                // Validate every option before touching the registered character.
                if (!viewHost.SetIllustratedCharacter(presentation, camera))
                    throw new InvalidOperationException(viewHost.Illustrated.LastError ?? "The requested illustrated character could not be loaded.");
                viewHost.SetIllustratedColors(skin, hair);
                viewHost.SetIllustratedEquipped(outfit == "equipped");
                viewHost.SetPose(action, seconds, freeze);
                category = ParseCategory(initialView);
            }
            catch (Exception ex)
            {
                illustratedInputError = ex.Message;
                Debug.LogError("SOLOGYM_ILLUSTRATED_ARGUMENT " + illustratedInputError);
            }
            RefreshIllustratedUI();
        }

        static string ChoiceArgument(string key, string fallback, params string[] allowed)
        {
            if (!HasArgument(key)) return fallback;
            string value = Argument(key);
            foreach (string option in allowed) if (value == option) return value;
            throw new ArgumentException(key + " requires " + string.Join(", ", allowed) + ".");
        }

        string IllustratedError() => !string.IsNullOrEmpty(illustratedInputError) ? illustratedInputError : viewHost.Illustrated.LastError;

        void RefreshIllustratedUI()
        {
            if (viewHost == null || !viewHost.UsesIllustrated) return;
            ApplyCameraForCategory();
            RefreshCopy(controller.Model);
            RefreshDock();
            RebuildCarousel();
            if (status != null) status.text = IllustratedError();
        }

        string StageHintText()
        {
            if (viewHost != null && viewHost.UsesIllustrated)
            {
                string person = viewHost.Illustrated.Presentation == "female" ? L("WOMAN", "MUJER") : L("MAN", "HOMBRE");
                string camera = viewHost.Illustrated.View == "gameplay" ? L("GAMEPLAY VIEW", "VISTA DE JUEGO") : L("STUDIO VIEW", "VISTA DE ESTUDIO");
                return person + " · " + L("NORMAL", "NORMAL") + " · " + (category == StudioCategory.Face ? L("HAIR DETAIL", "DETALLE DEL CABELLO") : camera);
            }
            return category == StudioCategory.Face ? L("FACE DETAIL", "DETALLE DEL ROSTRO")
                : L("TOUCH YOUR CHARACTER TO CUSTOMIZE", "TOCA TU PERSONAJE PARA PERSONALIZAR");
        }

        void BuildIllustratedCarousel()
        {
            var illustration = viewHost.Illustrated;
            if (category == StudioCategory.Skin)
            {
                var swatches = new List<SystemUI.WorkshopTileSpec>();
                swatches.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "illustrated_skin_source", selected = illustration.SkinPaletteId == "source", enabled = true,
                    tileColor = new Color32(194, 139, 101, 255),
                    onSelect = () => { viewHost.SetIllustratedColors("source", illustration.HairPaletteId); RefreshIllustratedUI(); }
                });
                foreach (var palette in AvatarCustomizationCatalog.SkinPalettes)
                {
                    if (!palette.enabled) continue;
                    string id = palette.id;
                    swatches.Add(new SystemUI.WorkshopTileSpec
                    {
                        key = "illustrated_skin_" + id, selected = illustration.SkinPaletteId == id, enabled = true,
                        tileColor = AvatarCustomizationCatalog.ParseHex(palette.displayColor, Color.gray),
                        onSelect = () => { viewHost.SetIllustratedColors(id, illustration.HairPaletteId); RefreshIllustratedUI(); }
                    });
                }
                SystemUI.SkinSwatchRow(carouselHost, 24, 680, 52, 16, swatches.ToArray(), controls);
                SystemUI.Caption(carouselHost, new Rect(16, 110, 673, 28),
                    L("Choose your skin tone", "Elige tu tono de piel"), 23, serif, Silver);
            }
            else if (category == StudioCategory.Face)
            {
                string[] colors = { "black", "brown", "silver" };
                Color[] fills = { new Color32(28, 32, 42, 255), new Color32(111, 70, 43, 255), new Color32(187, 195, 208, 255) };
                var swatches = new List<SystemUI.WorkshopTileSpec>();
                for (int i = 0; i < colors.Length; i++)
                {
                    string id = colors[i];
                    swatches.Add(new SystemUI.WorkshopTileSpec
                    {
                        key = "illustrated_hair_" + id, selected = illustration.HairPaletteId == id, enabled = true,
                        tileColor = fills[i], onSelect = () => { viewHost.SetIllustratedColors(illustration.SkinPaletteId, id); RefreshIllustratedUI(); }
                    });
                }
                SystemUI.SkinSwatchRow(carouselHost, 20, 680, 66, 24, swatches.ToArray(), controls);
                SystemUI.Caption(carouselHost, new Rect(16, 112, 673, 28),
                    L("Hair color", "Color de cabello"), 23, serif, Silver);
            }
            else if (category == StudioCategory.Body)
            {
                SmallTab("illustrated_fit_male", new Rect(44, 18, 296, 80), L("Man · Normal", "Hombre · Normal"),
                    illustration.Presentation == "male", () => { viewHost.SetIllustratedCharacter("male", illustration.View); RefreshIllustratedUI(); });
                SmallTab("illustrated_fit_female", new Rect(360, 18, 296, 80), L("Woman · Normal", "Mujer · Normal"),
                    illustration.Presentation == "female", () => { viewHost.SetIllustratedCharacter("female", illustration.View); RefreshIllustratedUI(); });
                SystemUI.Caption(carouselHost, new Rect(16, 116, 673, 28),
                    L("Choose your character", "Elige tu personaje"), 23, serif, Silver);
            }
            else if (category == StudioCategory.Gear)
            {
                SmallTab("illustrated_outfit_base", new Rect(44, 18, 296, 80), L("Training basics", "Ropa de entrenamiento"),
                    !illustration.IsEquipped, () => { viewHost.SetIllustratedEquipped(false); RefreshIllustratedUI(); });
                SmallTab("illustrated_outfit_equipped", new Rect(360, 18, 296, 80), L("Full outfit", "Atuendo completo"),
                    illustration.IsEquipped, () => { viewHost.SetIllustratedEquipped(true); RefreshIllustratedUI(); });
                SystemUI.Caption(carouselHost, new Rect(16, 116, 673, 28),
                    L("Choose your outfit", "Elige tu atuendo"), 23, serif, Silver);
            }
        }

        void BuildIllustratedMotionControls(Rect rect)
        {
            string[] actions = { "idle", "walk", "jab", "pause" };
            float width = (rect.width - 24) / 4;
            for (int i = 0; i < actions.Length; i++)
            {
                string action = actions[i];
                var area = new Rect(rect.x + i * (width + 8), rect.y + 14, width, 64);
                var panel = SystemUI.Panel(page, area, PanelStyle.Outline);
                panel.raycastTarget = true;
                var button = panel.gameObject.AddComponent<Button>();
                button.targetGraphic = panel;
                button.onClick.AddListener(() =>
                {
                    if (action == "pause") viewHost.SetPose(viewHost.CurrentPose, viewHost.PoseTime, !viewHost.PoseFrozen);
                    else viewHost.SetPose(action, 0, false);
                    RefreshIllustratedMotionControls();
                });
                controls["motion_" + action] = button;
                motionLabels[action] = SystemUI.Caption(page, area, "", 22, body, Silver);
            }
            RefreshIllustratedMotionControls();
        }

        void RefreshIllustratedMotionControls()
        {
            if (viewHost == null || !viewHost.UsesIllustrated) return;
            foreach (var entry in motionLabels)
            {
                string action = entry.Key;
                entry.Value.text = action == "idle" ? L("Idle", "Reposo") : action == "walk" ? L("Walk", "Caminar")
                    : action == "jab" ? L("Jab", "Golpe") : viewHost.PoseFrozen ? L("Play", "Reproducir") : L("Pause", "Pausar");
                var panel = controls["motion_" + action].GetComponent<SystemPanel>();
                panel.style = (action == "pause" ? viewHost.PoseFrozen : viewHost.CurrentPose == action) ? PanelStyle.Selected : PanelStyle.Outline;
                panel.SetVerticesDirty();
            }
        }

        string SkinLabel(string id)
        {
            switch (id)
            {
                case "source": return L("Original", "Original");
                case "sand": return L("Sand", "Arena");
                case "honey": return L("Honey", "Miel");
                case "amber": return L("Amber", "Ámbar");
                case "deep": return L("Deep", "Moreno");
                case "espresso": return L("Espresso", "Café");
                case "cocoa": return L("Cocoa", "Cacao");
                case "umber": return L("Umber", "Ébano");
                default: return L("Warm", "Cálido");
            }
        }

        void BuildSkinCarousel(CharacterCustomizationViewModel model)
        {
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var entry in AvatarCustomizationCatalog.SkinPalettes)
            {
                if (!entry.enabled) continue;
                string id = entry.id;
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "skin_" + id,
                    selected = model.Appearance.skinPaletteId == id,
                    enabled = true,
                    tileColor = AvatarCustomizationCatalog.ParseHex(entry.displayColor, Color.gray),
                    onSelect = () => controller.SelectSkin(id)
                });
            }
            SystemUI.SkinSwatchRow(carouselHost, 20, 680, 64, 16, tiles.ToArray(), controls);
            SystemUI.Caption(carouselHost, new Rect(16, 110, 673, 28),
                L("Choose your skin tone", "Elige tu tono de piel"), 23, serif, Silver);
        }

        string FaceRowLabel(FaceRow row) => row == FaceRow.Hair ? L("Hair", "Cabello")
            : row == FaceRow.Eyes ? L("Eyes", "Ojos") : L("Mouth", "Boca");

        void BuildFaceCarousel(CharacterCustomizationViewModel model, bool es)
        {
            for (int i = 0; i < 3; i++)
            {
                var row = (FaceRow)i;
                SmallTab("frow_" + row, new Rect(12 + i * 230, 0, 218, 36), FaceRowLabel(row), faceRow == row,
                    () => { faceRow = row; RebuildCarousel(); });
            }
            if (faceRow == FaceRow.Hair)
            {
                var entries = AvatarCustomizationCatalog.HairStyles;
                float x = CenteredStart(entries.Length, 146, 16);
                foreach (var entry in entries)
                {
                    string id = entry.id;
                    ArtOption("hair_" + id, new Rect(x, 48, 146, 122), entry.layerPartId,
                        es ? entry.labelEs : entry.labelEn, model.Appearance.hairId == id, entry.enabled,
                        () => controller.SelectHair(id));
                    x += 162;
                }
                return;
            }
            var faces = faceRow == FaceRow.Eyes ? AvatarCustomizationCatalog.Eyes : AvatarCustomizationCatalog.Mouths;
            string slot = faceRow == FaceRow.Eyes ? "eyes" : "mouth";
            string current = faceRow == FaceRow.Eyes ? model.Appearance.eyesId : model.Appearance.mouthId;
            float fx = CenteredStart(faces.Length, 170, 16);
            foreach (var entry in faces)
            {
                string id = entry.id;
                ArtOption(slot + "_" + id, new Rect(fx, 48, 170, 122), entry.layerPartId,
                    es ? entry.labelEs : entry.labelEn, current == id, entry.enabled,
                    () => controller.SelectFacePart(slot, id));
                fx += 186;
            }
        }

        void BuildFitTiles(CharacterCustomizationViewModel model, bool es)
        {
            var entries = AvatarCustomizationCatalog.FitFamilies;
            float x = CenteredStart(entries.Length, 124, 12);
            foreach (var entry in entries)
            {
                string id = entry.id;
                string caption = entry.enabled ? L("Athletic", "Atlético") : (es ? entry.labelEs : entry.labelEn);
                ArtOption("fit_" + id, new Rect(x, 4, 124, 133), entry.enabled ? "torso" : null,
                    caption, model.Appearance.fitFamilyId == id, entry.enabled, () => controller.SelectFitFamily(id));
                if (!entry.enabled) SystemUI.Caption(carouselHost, new Rect(x, 140, 124, 22),
                    L("Soon", "Pronto"), 17, body, SystemUI.Theme.muted);
                x += 136;
            }
        }

        void BuildGearSlotRow(CharacterCustomizationViewModel model)
        {
            string[] slots = { "torso", "hands", "head", "legs", "feet" };
            for (int i = 0; i < slots.Length; i++)
            {
                string slot = slots[i];
                SmallTab("gslot_" + slot, new Rect(12 + i * 138, 0, 130, 36), model.Copy("slot_" + slot),
                    gearSlot == slot, () => { gearSlot = slot; RebuildCarousel(); });
            }
        }

        void BuildGearItems(CharacterCustomizationViewModel model, bool es)
        {
            string slot = gearSlot;
            string current = GearSlotValue(model.Appearance, slot);
            var items = AvatarCustomizationCatalog.ItemsForSlot(slot);
            float x = CenteredStart(items.Length, 162, 16);
            foreach (var item in items)
            {
                string id = item.id ?? "";
                string part = id == "proof_training_top" ? "shirt" : id == "proof_cyan_gloves" ? "glove_left" : null;
                ArtOption("equip_" + slot + "_" + (string.IsNullOrEmpty(id) ? "none" : id),
                    new Rect(x, 48, 162, 122), part, es ? item.labelEs : item.labelEn,
                    current == id, item.enabled, () => controller.Equip(slot, id));
                x += 178;
            }
        }

        static float CenteredStart(int count, float width, float gap) => (705 - count * width - (count - 1) * gap) * .5f;

        void SmallTab(string key, Rect rect, string caption, bool selected, Action select)
        {
            var panel = SystemUI.Panel(carouselHost, rect, selected ? PanelStyle.Selected : PanelStyle.Outline);
            panel.rimWidth = selected ? 2 : 1;
            panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.onClick.AddListener(() => select());
            SystemUI.Caption(panel.transform, new Rect(4, 0, rect.width - 8, rect.height), caption, 18, body,
                selected ? Silver : SystemUI.Theme.muted);
            controls[key] = button;
        }

        void ArtOption(string key, Rect rect, string partId, string caption, bool selected, bool enabled, Action select)
        {
            var panel = SystemUI.Panel(carouselHost, rect, selected ? PanelStyle.Selected : PanelStyle.Slot);
            panel.rimWidth = selected ? 2.5f : 1;
            panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.interactable = enabled;
            button.onClick.AddListener(() => { if (enabled) select(); });
            if (!enabled)
                SystemUI.Icon(panel.transform, new Rect(rect.width * .5f - 20, 23, 40, 48), "lock", SystemUI.Theme.muted);
            else if (!string.IsNullOrEmpty(partId))
                AtlasThumbnail(panel.transform, new Rect(20, 10, rect.width - 40, rect.height - 49), partId);
            else
                SystemUI.Caption(panel.transform, new Rect(20, 10, rect.width - 40, rect.height - 49), "—", 36, serif, Cyan);
            var label = SystemUI.Caption(panel.transform, new Rect(6, rect.height - 34, rect.width - 12, 30),
                caption, 18, body, enabled ? Silver : SystemUI.Theme.muted);
            label.GetComponent<SystemTextFit>().multiline = true;
            if (selected) SystemUI.Caption(panel.transform, new Rect(rect.width - 24, 3, 20, 22), "◆", 15, body, Cyan);
            controls[key] = button;
        }

        void AtlasThumbnail(Transform parent, Rect rect, string partId)
        {
            if (thumbnailParts.Count == 0)
            {
                var asset = Resources.Load<TextAsset>("AvatarProof/Parts");
                if (asset != null)
                    foreach (var part in JsonUtility.FromJson<LayeredAvatar.Atlas>(asset.text).parts) thumbnailParts[part.id] = part;
            }
            if (!thumbnailParts.TryGetValue(partId, out var entry) || entry.rect == null || entry.rect.Length < 4) return;
            var texture = Resources.Load<Texture2D>(entry.resource);
            if (texture == null) return;
            var r = entry.rect;
            float scale = Mathf.Min(rect.width / r[2], rect.height / r[3]);
            var art = SystemUI.Art(parent, new Rect(rect.x + (rect.width - r[2] * scale) * .5f,
                rect.y + (rect.height - r[3] * scale) * .5f, r[2] * scale, r[3] * scale), entry.resource);
            art.uvRect = new Rect(r[0] / texture.width, 1 - (r[1] + r[3]) / texture.height, r[2] / texture.width, r[3] / texture.height);
        }

        static string GearSlotValue(AvatarAppearance app, string slot)
        {
            switch (slot)
            {
                case "torso": return app.torsoItemId ?? "";
                case "hands": return app.handItemId ?? "";
                case "head": return app.headItemId ?? "";
                case "legs": return app.legsItemId ?? "";
                case "feet": return app.feetItemId ?? "";
                default: return "";
            }
        }

        void Secondary(string key, Rect rect, string label, Action action)
        {
            leaveLabel = SystemUI.Text(page, rect, label, 26, serif, Cyan, TextAnchor.MiddleCenter);
            Hit(key, rect, action);
        }

        void Back() { back?.Invoke(); gameObject.SetActive(false); }
        void Exit() { leave?.Invoke(); Destroy(gameObject); }

        void Language()
        {
            OpenModal(L("Language", "Idioma"));
            ModalButton(new Rect(35, 140, 640, 86), "English", () => { CloseModal(); controller.SetLanguage("en"); });
            ModalButton(new Rect(35, 240, 640, 86), "Español", () => { CloseModal(); controller.SetLanguage("es"); });
            ModalButton(new Rect(35, 340, 640, 86), L("Use device language", "Usar idioma del dispositivo"),
                () => { CloseModal(); controller.SetLanguage("auto"); });
        }

        void Notice(string title, string message)
        {
            OpenModal(title);
            var text = SystemUI.Text(modal, new Rect(35, 135, 640, 445), message, 29, body, Silver, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            ModalButton(new Rect(35, 620, 640, 83), L("Close", "Cerrar"), CloseModal);
        }

        void Document(string id) { }

        void OpenModal(string title)
        {
            CloseModal();
            var shade = SystemUI.Node("Modal shade", root, new Rect(0, 0, W, H));
            shade.gameObject.AddComponent<Image>().color = new Color(0, .01f, .04f, .87f);
            modal = SystemUI.Node("System dialog", shade, new Rect(71, 500, 711, 745));
            var frame = modal.gameObject.AddComponent<SystemPanel>();
            frame.theme = SystemUI.Theme;
            frame.ornaments = true;
            SystemUI.Text(modal, new Rect(30, 24, 560, 70), title, 34, bold, Silver);
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
            bool smoke = HasArgument("-sologym-smoke");
            int exitCode = string.IsNullOrEmpty(illustratedInputError) ? 0 : 2;
            if (smoke) yield return Smoke(passed => exitCode = passed && string.IsNullOrEmpty(illustratedInputError) ? 0 : 2);
            // A capture is also useful during interactive review. Only automated runs exit.
            if (smoke || HasArgument("-sologym-quit-after-capture")) Application.Quit(exitCode);
        }

        IEnumerator SourceProof()
        {
            for (int i = 0; i < 8; i++) yield return null;
            string directory = Argument("-sologym-character-proof");
            if (string.IsNullOrEmpty(directory) || directory.StartsWith("-", StringComparison.Ordinal))
                directory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts/visual/CharacterSource3D");
            int exitCode = 2;
            yield return Source3D.SourceCharacterProofRunner.Run(viewHost, directory, passed => exitCode = passed ? 0 : 2);
            // Requesting a bounded proof run is explicit automation; ordinary studio sessions stay open.
            Application.Quit(exitCode);
        }

        IEnumerator IllustratedProof()
        {
            for (int i = 0; i < 8; i++) yield return null;
            string directory = Argument("-sologym-illustrated-proof");
            if (string.IsNullOrEmpty(directory) || directory.StartsWith("-", StringComparison.Ordinal))
                directory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts/visual/CharacterIllustrated");
            category = StudioCategory.Skin;
            int exitCode = 2;
            yield return Illustrated.IllustratedCharacterProofRunner.Run(viewHost, directory,
                RefreshIllustratedUI, passed => exitCode = passed ? 0 : 2, illustratedInputError);
            Application.Quit(exitCode);
        }

        IEnumerator ReferenceCheck()
        {
            if (viewHost.UsesIllustrated)
            {
                yield return IllustratedProof();
                yield break;
            }
            if (viewHost.UsesSource3D)
            {
                // Source geometry has its own evidence. Never compare it to the cutout golden image.
                yield return SourceProof();
                yield break;
            }
            for (int i = 0; i < 6; i++) yield return null;
            yield return new WaitForEndOfFrame();
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts/visual/AvatarReference");
            string png = Path.Combine(dir, "proof_male_athletic_runtime.png");
            var golden = CharacterReference.GoldenRecipe();
            viewHost.ApplyRecipe(golden);
            viewHost.SetCameraPreset(CharacterReference.CameraPresetId.FullBody);
            yield return null;
            yield return new WaitForEndOfFrame();
            var result = AvatarReferenceVerification.CaptureStage(viewHost.Avatar, png);
            AvatarReferenceVerification.WriteJson(result, Path.Combine(dir, "reference-check.json"));
            Debug.Log("SOLOGYM_AVATAR_REFERENCE " + result.detail);
            Application.Quit(result.passed ? 0 : 2);
        }

        IEnumerator Smoke(Action<bool> completed)
        {
            if (viewHost.UsesIllustrated)
            {
                yield return IllustratedSmoke(completed);
                yield break;
            }
            if (viewHost.UsesSource3D)
            {
                yield return SourceSmoke(completed);
                yield break;
            }
            var checks = new List<string>();
            var failures = new List<string>();
            void Check(string name, bool condition)
            {
                checks.Add(name);
                if (condition) return;
                failures.Add(name);
                Debug.LogError("SOLOGYM_CHARACTER_CHECK_FAILED " + name);
            }
            try
            {
                Debug.Log("SOLOGYM_CHARACTER_STATE " + CharacterCustomizationStateChecks.Run());
                Check("character state checks", true);
            }
            catch (Exception e) { Debug.LogError(e.Message); Check("character state checks", false); }

            var avatar = viewHost.Avatar;
            Check("avatar initialized", avatar != null && avatar.IsInitialized);
            if (avatar == null)
            {
                WriteSmokeResult(checks, failures);
                completed(false);
                yield break;
            }
            var pieces = new Dictionary<string, RawImage>();
            foreach (var image in avatar.GetComponentsInChildren<RawImage>(true)) pieces[image.name] = image;
            bool Visible(string id, bool expected) => pieces.TryGetValue(id, out var image)
                && image.gameObject.activeInHierarchy == expected;

            Check("skin dock opens", ClickSmokeControl("dock_skin"));
            Check("skin option available", ClickSmokeControl("skin_deep"));
            yield return new WaitForEndOfFrame();
            Check("skin choice applied", controller.Model.Appearance.skinPaletteId == "deep" && avatar.CheckAttachments());

            // Exercise the real raycast order: broad skin/torso regions must not intercept the head.
            bool headReceivesPointer = false;
            if (EventSystem.current != null && controls.TryGetValue("hit_head", out var headButton))
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)headButton.transform;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                headReceivesPointer = hits.Count > 0 && hits[0].gameObject == headButton.gameObject;
                if (headReceivesPointer)
                    ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                else if (hits.Count > 0) Debug.LogError("SOLOGYM_HEAD_RAYCAST_BLOCKER " + hits[0].gameObject.name);
            }
            Check("head pointer hit priority", headReceivesPointer);
            if (!headReceivesPointer) ClickSmokeControl("dock_face");
            yield return new WaitForEndOfFrame();
            Check("head opens face camera", category == StudioCategory.Face
                && avatar.previewMode == LayeredAvatar.AvatarPreviewMode.FaceCloseUp
                && !hitLayer.gameObject.activeSelf && avatar.CheckAttachments());
            Check("hair tab opens", ClickSmokeControl("frow_Hair"));
            Check("spiky hair available", ClickSmokeControl("hair_spiky"));
            yield return new WaitForEndOfFrame();
            Rect spikyUV = pieces.TryGetValue("hair_spiky", out var hairImage) ? hairImage.uvRect : default;
            Check("spiky hair applied", controller.Model.Appearance.hairId == "spiky" && hairImage != null);
            Check("swept hair available", ClickSmokeControl("hair_swept"));
            yield return new WaitForEndOfFrame();
            Check("hair sprite changes", controller.Model.Appearance.hairId == "swept"
                && hairImage != null && hairImage.uvRect != spikyUV && avatar.CheckAttachments());

            Check("body dock opens", ClickSmokeControl("dock_body"));
            yield return new WaitForEndOfFrame();
            Check("full body camera restored", category == StudioCategory.Body
                && avatar.previewMode == LayeredAvatar.AvatarPreviewMode.FullBody
                && hitLayer.gameObject.activeSelf && avatar.CheckAttachments());

            Check("gear dock opens", ClickSmokeControl("dock_gear"));
            Check("torso slot opens", ClickSmokeControl("gslot_torso"));
            Check("unequip torso available", ClickSmokeControl("equip_torso_none"));
            yield return new WaitForEndOfFrame();
            Check("torso removed visually", string.IsNullOrEmpty(controller.Model.Appearance.torsoItemId) && Visible("shirt", false));
            Check("equip torso available", ClickSmokeControl("equip_torso_proof_training_top"));
            yield return new WaitForEndOfFrame();
            Check("torso restored visually", controller.Model.Appearance.torsoItemId == "proof_training_top" && Visible("shirt", true));
            Check("hands slot opens", ClickSmokeControl("gslot_hands"));
            Check("unequip gloves available", ClickSmokeControl("equip_hands_none"));
            yield return new WaitForEndOfFrame();
            Check("bare hands replace gloves", string.IsNullOrEmpty(controller.Model.Appearance.handItemId)
                && Visible("glove_left", false) && Visible("glove_right", false)
                && Visible("hand_left", true) && Visible("hand_right", true));
            Check("equip gloves available", ClickSmokeControl("equip_hands_proof_cyan_gloves"));
            yield return new WaitForEndOfFrame();
            Check("gloves replace bare hands", controller.Model.Appearance.handItemId == "proof_cyan_gloves"
                && Visible("glove_left", true) && Visible("glove_right", true)
                && Visible("hand_left", false) && Visible("hand_right", false));

            bool wasFacingLeft = avatar.facingLeft;
            Check("mirror control available", ClickSmokeControl("dock_rotate"));
            yield return new WaitForEndOfFrame();
            var rig = avatar.transform.Find("Body rig");
            Check("mirror applied to rig", avatar.facingLeft != wasFacingLeft && rig != null
                && (rig.localScale.x < 0) == avatar.facingLeft && avatar.CheckAttachments());
            avatar.freeze = true;
            foreach (string pose in new[] { "idle", "walk", "jab" })
            {
                avatar.action = pose;
                avatar.previewTime = .37f;
                yield return new WaitForEndOfFrame();
                Check(pose + " attachments stay registered", avatar.CheckAttachments());
            }
            avatar.action = "idle";
            avatar.previewTime = .55f;

            // Keep navigation last so it cannot remove the studio before the interaction checks.
            string checkpoint = null;
            controller.CheckpointRequested += id => checkpoint = id;
            Check("continue control available", ClickSmokeControl("continue"));
            yield return null;
            Check("setup complete checkpoint", checkpoint == "REVIEW:SETUP_COMPLETE");
            WriteSmokeResult(checks, failures);
            completed(failures.Count == 0);
        }

        IEnumerator SourceSmoke(Action<bool> completed)
        {
            var checks = new List<string>();
            var failures = new List<string>();
            void Check(string name, bool condition)
            {
                checks.Add(name);
                if (condition) return;
                failures.Add(name);
                Debug.LogError("SOLOGYM_CHARACTER_CHECK_FAILED " + name);
            }
            var source = viewHost.Source;
            bool Valid()
            {
                if (source == null) return false;
                bool valid = source.TryValidate(out string reason);
                if (!valid) Debug.LogError("SOLOGYM_SOURCE_CHARACTER_VALIDATION " + reason);
                return valid;
            }
            try
            {
                Debug.Log("SOLOGYM_CHARACTER_STATE " + CharacterCustomizationStateChecks.Run());
                Check("character state checks", true);
            }
            catch (Exception e) { Debug.LogError(e.Message); Check("character state checks", false); }
            Check("source character loaded", source != null && source.IsLoaded);
            Check("source geometry and rig valid", Valid());
            if (source == null || !source.IsLoaded)
            {
                WriteSmokeResult(checks, failures);
                completed(false);
                yield break;
            }

            Check("skin dock opens", ClickSmokeControl("dock_skin"));
            Check("skin option available", ClickSmokeControl("skin_deep"));
            yield return new WaitForEndOfFrame();
            Check("skin recipe applied to source", source.CurrentRecipe.skinPaletteId == "deep" && Valid());
            bool headReceivesPointer = false;
            if (EventSystem.current != null && controls.TryGetValue("hit_head", out var headButton))
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)headButton.transform;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                headReceivesPointer = hits.Count > 0 && hits[0].gameObject == headButton.gameObject;
                if (headReceivesPointer) ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            }
            Check("head pointer hit priority", headReceivesPointer);
            if (!headReceivesPointer) ClickSmokeControl("dock_face");
            yield return new WaitForEndOfFrame();
            Check("head opens source face camera", category == StudioCategory.Face
                && source.CurrentCameraPreset == "face" && !hitLayer.gameObject.activeSelf);
            Check("hair tab opens", ClickSmokeControl("frow_Hair"));
            Check("spiky hair available", ClickSmokeControl("hair_spiky"));
            yield return new WaitForEndOfFrame();
            Check("spiky hair geometry applied", source.CurrentRecipe.hairId == "spiky"
                && source.IsPartVisible("spiky") && !source.IsPartVisible("swept") && Valid());
            Check("swept hair available", ClickSmokeControl("hair_swept"));
            yield return new WaitForEndOfFrame();
            Check("swept hair geometry applied", source.CurrentRecipe.hairId == "swept"
                && source.IsPartVisible("swept") && !source.IsPartVisible("spiky") && Valid());
            Check("body dock opens", ClickSmokeControl("dock_body"));
            yield return new WaitForEndOfFrame();
            Check("full source camera restored", category == StudioCategory.Body
                && source.CurrentCameraPreset == sourceReviewCamera && hitLayer.gameObject.activeSelf);
            Check("gear dock opens", ClickSmokeControl("dock_gear"));
            Check("torso slot opens", ClickSmokeControl("gslot_torso"));
            Check("unequip torso available", ClickSmokeControl("equip_torso_none"));
            yield return new WaitForEndOfFrame();
            Check("fitted torso removed", !source.IsPartVisible("torso") && string.IsNullOrEmpty(source.CurrentRecipe.torsoItemId));
            Check("equip torso available", ClickSmokeControl("equip_torso_proof_training_top"));
            yield return new WaitForEndOfFrame();
            Check("fitted torso restored", source.IsPartVisible("torso") && Valid());
            Check("hands slot opens", ClickSmokeControl("gslot_hands"));
            Check("unequip gloves available", ClickSmokeControl("equip_hands_none"));
            yield return new WaitForEndOfFrame();
            Check("fitted gloves removed", !source.IsPartVisible("hands") && string.IsNullOrEmpty(source.CurrentRecipe.handItemId));
            Check("equip gloves available", ClickSmokeControl("equip_hands_proof_cyan_gloves"));
            yield return new WaitForEndOfFrame();
            Check("fitted gloves restored", source.IsPartVisible("hands") && Valid());
            float beforeYaw = source.TurntableYaw;
            Check("rotate control available", ClickSmokeControl("dock_rotate"));
            yield return new WaitForEndOfFrame();
            Check("rotate changes source yaw", Mathf.Abs(Mathf.DeltaAngle(beforeYaw, source.TurntableYaw)) > 1 && Valid());
            foreach (string pose in new[] { "idle", "walk", "jab" })
            {
                viewHost.SetPose(pose, .37f);
                yield return new WaitForEndOfFrame();
                Check(pose + " source rig valid", Valid());
            }
            viewHost.SetPose("idle", .55f);
            viewHost.ResetTurntable();
            string checkpoint = null;
            controller.CheckpointRequested += id => checkpoint = id;
            Check("continue control available", ClickSmokeControl("continue"));
            yield return null;
            Check("setup complete checkpoint", checkpoint == "REVIEW:SETUP_COMPLETE");
            WriteSmokeResult(checks, failures);
            completed(failures.Count == 0);
        }

        IEnumerator IllustratedSmoke(Action<bool> completed)
        {
            var checks = new List<string>();
            var failures = new List<string>();
            void Check(string name, bool condition)
            {
                checks.Add(name);
                if (condition) return;
                failures.Add(name);
                Debug.LogError("SOLOGYM_CHARACTER_CHECK_FAILED " + name);
            }
            var illustration = viewHost.Illustrated;
            Check("illustrated command-line options valid", string.IsNullOrEmpty(illustratedInputError));
            Check("illustrated source loaded", illustration != null && illustration.IsLoaded);
            if (illustration == null || !illustration.IsLoaded)
            {
                WriteSmokeResult(checks, failures);
                completed(false);
                yield break;
            }
            string legacyRecipe = JsonUtility.ToJson(controller.Model.Appearance);
            Check("body dock opens", ClickSmokeControl("dock_body"));
            Check("female normal selectable", ClickSmokeControl("illustrated_fit_female"));
            yield return new WaitForEndOfFrame();
            Check("female normal applied", illustration.Presentation == "female");
            Check("male normal selectable", ClickSmokeControl("illustrated_fit_male"));
            yield return new WaitForEndOfFrame();
            Check("male normal applied", illustration.Presentation == "male");
            Check("skin dock opens", ClickSmokeControl("dock_skin"));
            Check("skin color selectable", ClickSmokeControl("illustrated_skin_deep"));
            Check("skin color applied", illustration.SkinPaletteId == "deep");
            Check("hair dock opens", ClickSmokeControl("dock_face"));
            Check("hair color selectable", ClickSmokeControl("illustrated_hair_brown"));
            Check("hair color applied", illustration.HairPaletteId == "brown");
            Check("gear dock opens", ClickSmokeControl("dock_gear"));
            Check("base outfit selectable", ClickSmokeControl("illustrated_outfit_base"));
            Check("base outfit applied", !illustration.IsEquipped);
            Check("equipped outfit selectable", ClickSmokeControl("illustrated_outfit_equipped"));
            Check("equipped outfit applied", illustration.IsEquipped);
            string oldView = illustration.View;
            Check("view toggle available", ClickSmokeControl("dock_rotate"));
            Check("view toggle applied", illustration.View != oldView);
            foreach (string action in new[] { "idle", "walk", "jab" })
            {
                Check(action + " playback control", ClickSmokeControl("motion_" + action));
                Check(action + " plays", illustration.CurrentAction == action && !viewHost.PoseFrozen);
                viewHost.SetPose(action, .25f, true);
                yield return new WaitForEndOfFrame();
                Check(action + " registered mesh valid", illustration.TryValidate(out _));
            }
            Check("proof does not change legacy recipe", JsonUtility.ToJson(controller.Model.Appearance) == legacyRecipe);
            Check("proof has no continue checkpoint", continueButton == null && !controls.ContainsKey("continue"));
            WriteSmokeResult(checks, failures);
            completed(failures.Count == 0);
        }

        bool ClickSmokeControl(string key)
        {
            if (!controls.TryGetValue(key, out var button) || button == null
                || !button.gameObject.activeInHierarchy || !button.IsInteractable()) return false;
            try { button.onClick.Invoke(); return true; }
            catch (Exception e)
            {
                Debug.LogError("SOLOGYM_CHARACTER_CONTROL " + key + ": " + e.Message);
                return false;
            }
        }

        [Serializable] sealed class StudioSmokeResult
        {
            public bool passed;
            public string renderer, visual_review_status;
            public string[] checks, failures;
        }

        void WriteSmokeResult(List<string> checks, List<string> failures)
        {
            string json = JsonUtility.ToJson(new StudioSmokeResult
            {
                passed = failures.Count == 0, checks = checks.ToArray(), failures = failures.ToArray(),
                renderer = viewHost.UsesIllustrated ? "illustrated" : viewHost.UsesSource3D ? "source3d" : "legacy", visual_review_status = "pending"
            });
            File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), json);
            Debug.Log("SOLOGYM_CHARACTER_SMOKE " + json);
        }

        void OnDestroy() => controller?.Dispose();

        Text Live(string name, Rect rect, string value, int size, Color? color = null, Font font = null)
        {
            var box = new Rect(rect.x, rect.y - 8, rect.width, rect.height + 16);
            var t = SystemUI.Caption(page, box, value, size, font ?? serif, color ?? Silver);
            t.name = name;
            if (name == "Title") t.gameObject.AddComponent<OnboardingSilverText>();
            return t;
        }

        Button Hit(string key, Rect rect, Action action)
        {
            var node = SystemUI.Node(key, page, rect);
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
        string RotationLabel() => viewHost != null && viewHost.UsesIllustrated
            ? (viewHost.Illustrated.View == "studio" ? L("Gameplay", "Juego") : L("Studio", "Estudio"))
            : viewHost != null && viewHost.UsesSource3D ? L("Rotate", "Girar") : L("Mirror", "Reflejar");
        float L(float en, float es) => controller.Model.Language == "es" ? es : en;
        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        static bool HasArgument(string key) => Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0;
    }
}
