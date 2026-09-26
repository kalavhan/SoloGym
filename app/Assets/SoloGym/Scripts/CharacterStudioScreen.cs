using System;
using System.Collections;
using System.Collections.Generic;
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
        static readonly Rect StageRect = new Rect(94, 380, 665, 780);
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;

        RectTransform root, page, modal, overlaySheet, carouselHost, hitLayer;
        Font serif, bold, body;
        CharacterCustomizationController controller;
        AvatarViewHost viewHost;
        StudioCategory category = StudioCategory.Skin;
        FaceRow faceRow = FaceRow.Hair;
        string gearSlot = "torso";
        Action back, leave;
        Action<string> onCheckpoint;
        string capture;
        Text status, categoryLabel;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
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
            controller = new CharacterCustomizationController(review);
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
            if (capture != null) StartCoroutine(Capture());
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
            Live("Back", new Rect(92, 53, L(61, 83), 25), L("Back", "Volver"), 31);
            Live("Language", new Rect(765, 56, 29, 20), controller.Model.Language.ToUpperInvariant(), 27);
            Hit("back", new Rect(30, 20, 180, 100), Back);
            Hit("language", new Rect(680, 20, 130, 100), Language);
            Live("Title", new Rect(L(168, 200), 490, L(517, 453), 53),
                controller.Model.Copy("window_title"), 72, Silver, bold);
            var shade = SystemUI.Node("Stage shade", page, StageRect);
            shade.gameObject.AddComponent<Image>().color = new Color(0, .02f, .06f, .35f);
            viewHost = shade.gameObject.AddComponent<AvatarViewHost>();
            viewHost.Mount(page, StageRect);
            BuildHitRegions();
            overlaySheet = SystemUI.Node("Option overlay", page, new Rect(62, 1220, 729, 300));
            var sheetPanel = overlaySheet.gameObject.AddComponent<SystemPanel>();
            sheetPanel.theme = SystemUI.Theme;
            sheetPanel.style = PanelStyle.Glass;
            sheetPanel.ornaments = false;
            categoryLabel = SystemUI.Text(overlaySheet, new Rect(24, 12, 680, 28), "", 22, serif, Silver);
            carouselHost = SystemUI.Node("Carousel host", overlaySheet, new Rect(12, 48, 705, 230));
            BuildCategoryDock();
            var primaryRect = PortalFrameLayout.PrimaryRect;
            var secondaryRect = PortalFrameLayout.SecondaryRect;
            Primary(() => controller.Continue(), controller.Model.Copy("continue"), true, primaryRect,
                PortalFrameLayout.PrimaryCaptionRect);
            Secondary("leave", secondaryRect, controller.Model.Copy("not_now"), controller.Decline);
            status = SystemUI.Text(page, new Rect(105, primaryRect.y - 36, 642, 28), "", 18, body,
                new Color32(255, 207, 161, 255), TextAnchor.MiddleCenter);
            if (controller.Model.ReviewMode)
                SystemUI.Text(page, new Rect(80, 280, 693, 35),
                    L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"),
                    20, body, Cyan, TextAnchor.MiddleCenter);
        }

        void BuildHitRegions()
        {
            hitLayer = SystemUI.Node("Avatar hit regions", page, StageRect);
            foreach (var region in CharacterReference.HitRegions)
            {
                if (region.rect == null || region.rect.Length < 4) continue;
                float x = region.rect[0] * StageRect.width;
                float y = region.rect[1] * StageRect.height;
                float w = region.rect[2] * StageRect.width;
                float h = region.rect[3] * StageRect.height;
                string cat = region.category;
                Hit("hit_" + region.id, new Rect(StageRect.x + x, StageRect.y + y, w, h), () => SelectCategory(ParseCategory(cat)));
            }
        }

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
            const float y = 1188, h = 52, w = 130, gap = 8, x0 = 62;
            AddDockButton("dock_skin", x0, y, w, h, "cat_skin", StudioCategory.Skin);
            AddDockButton("dock_face", x0 + (w + gap), y, w, h, "cat_face", StudioCategory.Face);
            AddDockButton("dock_body", x0 + 2 * (w + gap), y, w, h, "cat_body", StudioCategory.Body);
            AddDockButton("dock_gear", x0 + 3 * (w + gap), y, w, h, "cat_gear", StudioCategory.Gear);
            Hit("dock_rotate", new Rect(x0 + 4 * (w + gap), y, w, h), () =>
            {
                if (viewHost?.Avatar != null) viewHost.Avatar.facingLeft = !viewHost.Avatar.facingLeft;
            });
            SystemUI.Caption(page, new Rect(x0 + 4 * (w + gap), y, w, h), controller.Model.Copy("cat_rotate"), 18, body, Cyan);
        }

        void AddDockButton(string key, float x, float y, float w, float h, string copyKey, StudioCategory cat)
        {
            var panel = SystemUI.Panel(page, new Rect(x, y, w, h),
                category == cat ? PanelStyle.Selected : PanelStyle.Outline);
            panel.raycastTarget = true;
            var b = panel.gameObject.AddComponent<Button>();
            b.targetGraphic = panel;
            b.onClick.AddListener(() => SelectCategory(cat));
            SystemUI.Caption(page, new Rect(x, y, w, h), controller.Model.Copy(copyKey), 17, body, Silver);
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
            viewHost.SetCameraPreset(category == StudioCategory.Face
                ? CharacterReference.CameraPresetId.FaceCloseUp
                : CharacterReference.CameraPresetId.FullBody);
            viewHost.ApplyRecipe(controller.Model.Appearance);
        }

        void OnModelChanged(CharacterCustomizationViewModel model)
        {
            if (!built) BuildShell();
            viewHost?.ApplyRecipe(model.Appearance);
            ApplyCameraForCategory();
            if (status != null) status.text = model.Error;
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
            }
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
                Destroy(carouselHost.GetChild(i).gameObject);
            var model = controller.Model;
            bool es = model.Language == "es";
            categoryLabel.text = CategoryTitle(model);
            float y = 8;
            switch (category)
            {
                case StudioCategory.Skin:
                    BuildSkinCarousel(y, model);
                    break;
                case StudioCategory.Face:
                    y = SystemUI.OptionThumbnailRow(carouselHost, y, 680, model.Copy("face_picker"), 64, 8,
                        FaceTiles(model, es), serif, controls);
                    break;
                case StudioCategory.Body:
                    y = BuildFitTiles(y, model, es);
                    break;
                case StudioCategory.Gear:
                    y = BuildGearSlotRow(y, model);
                    y = BuildGearItems(y, model, es);
                    break;
            }
        }

        string CategoryTitle(CharacterCustomizationViewModel model)
        {
            switch (category)
            {
                case StudioCategory.Face: return model.Copy("cat_face");
                case StudioCategory.Body: return model.Copy("cat_body");
                case StudioCategory.Gear: return model.Copy("cat_gear");
                default: return model.Copy("cat_skin");
            }
        }

        void BuildSkinCarousel(float y, CharacterCustomizationViewModel model)
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
            SystemUI.SkinSwatchRow(carouselHost, y, 680, 52, 10, tiles.ToArray(), controls);
        }

        SystemUI.WorkshopTileSpec[] FaceTiles(CharacterCustomizationViewModel model, bool es)
        {
            var list = new List<SystemUI.WorkshopTileSpec>();
            foreach (var entry in AvatarCustomizationCatalog.HairStyles)
            {
                string id = entry.id;
                list.Add(Tile("hair_" + id, model.Appearance.hairId == id, entry.enabled,
                    entry.tileColor, es ? entry.labelEs : entry.labelEn, () => controller.SelectHair(id)));
            }
            foreach (var entry in AvatarCustomizationCatalog.Eyes)
            {
                string id = entry.id;
                list.Add(Tile("eyes_" + id, model.Appearance.eyesId == id, entry.enabled,
                    entry.tileColor, es ? entry.labelEs : entry.labelEn, () => controller.SelectFacePart("eyes", id)));
            }
            foreach (var entry in AvatarCustomizationCatalog.Mouths)
            {
                string id = entry.id;
                list.Add(Tile("mouth_" + id, model.Appearance.mouthId == id, entry.enabled,
                    entry.tileColor, es ? entry.labelEs : entry.labelEn, () => controller.SelectFacePart("mouth", id)));
            }
            return list.ToArray();
        }

        static SystemUI.WorkshopTileSpec Tile(string key, bool selected, bool enabled, string hex, string caption, Action onSelect)
        {
            return new SystemUI.WorkshopTileSpec
            {
                key = key,
                selected = selected,
                enabled = enabled,
                tileColor = AvatarCustomizationCatalog.ParseHex(hex, Color.gray),
                caption = caption,
                onSelect = onSelect
            };
        }

        float BuildFitTiles(float y, CharacterCustomizationViewModel model, bool es)
        {
            string fit = model.Appearance.fitFamilyId;
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var entry in AvatarCustomizationCatalog.FitFamilies)
            {
                string id = entry.id;
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "fit_" + id,
                    selected = fit == id,
                    enabled = entry.enabled,
                    tileColor = entry.enabled ? new Color32(48, 72, 98, 255) : new Color32(28, 36, 48, 255),
                    caption = entry.enabled ? (es ? entry.labelEs : entry.labelEn) : model.Copy("locked_soon"),
                    onSelect = () => controller.SelectFitFamily(id)
                });
            }
            return SystemUI.OptionThumbnailRow(carouselHost, y, 680, "", 64, 8, tiles.ToArray(), serif, controls);
        }

        float BuildGearSlotRow(float y, CharacterCustomizationViewModel model)
        {
            string[] slots = { "torso", "hands", "head", "legs", "feet" };
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var slot in slots)
            {
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "gslot_" + slot,
                    selected = gearSlot == slot,
                    enabled = true,
                    tileColor = new Color32(30, 50, 70, 255),
                    caption = model.Copy("slot_" + slot),
                    onSelect = () => { gearSlot = slot; RebuildCarousel(); }
                });
            }
            return SystemUI.OptionThumbnailRow(carouselHost, y, 680, model.Copy("equip_section"), 56, 6,
                tiles.ToArray(), serif, controls);
        }

        float BuildGearItems(float y, CharacterCustomizationViewModel model, bool es)
        {
            string current = GearSlotValue(model.Appearance, gearSlot);
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var item in AvatarCustomizationCatalog.ItemsForSlot(gearSlot))
            {
                string id = item.id ?? "";
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "equip_" + gearSlot + "_" + (string.IsNullOrEmpty(id) ? "none" : id),
                    selected = current == id,
                    enabled = item.enabled,
                    tileColor = AvatarCustomizationCatalog.ParseHex(item.tileColor, Color.gray),
                    caption = es ? item.labelEs : item.labelEn,
                    onSelect = () => controller.Equip(gearSlot, id)
                });
            }
            return SystemUI.OptionThumbnailRow(carouselHost, y, 680, "", 64, 8, tiles.ToArray(), serif, controls);
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
            SystemUI.Text(page, rect, label, 26, serif, Cyan, TextAnchor.MiddleCenter);
            Hit(key, rect, action);
        }

        void Back() { back?.Invoke(); gameObject.SetActive(false); }
        void Exit() { leave?.Invoke(); Destroy(gameObject); }

        void Language()
        {
            OpenModal(L("Language", "Idioma"));
            ModalButton(new Rect(35, 140, 640, 86), "English", () => controller.SetLanguage("en"));
            ModalButton(new Rect(35, 240, 640, 86), "Español", () => controller.SetLanguage("es"));
            ModalButton(new Rect(35, 340, 640, 86), L("Use device language", "Usar idioma del dispositivo"),
                () => controller.SetLanguage("auto"));
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
            if (HasArgument("-sologym-smoke")) yield return Smoke();
            Application.Quit();
        }

        IEnumerator ReferenceCheck()
        {
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

        IEnumerator Smoke()
        {
            bool passed = true;
            try { Debug.Log("SOLOGYM_CHARACTER_STATE " + CharacterCustomizationStateChecks.Run()); }
            catch (Exception e) { Debug.LogError(e.Message); passed = false; }
            if (controls.TryGetValue("skin_deep", out Button skinDeep))
            {
                skinDeep.onClick.Invoke();
                yield return null;
                passed &= controller.Model.Appearance.skinPaletteId == "deep";
            }
            string checkpoint = null;
            controller.CheckpointRequested += id => checkpoint = id;
            if (controls.TryGetValue("continue", out Button cont))
            {
                cont.onClick.Invoke();
                yield return null;
                passed &= checkpoint == "REVIEW:SETUP_COMPLETE";
            }
            string json = "{\"passed\":" + (passed ? "true" : "false")
                + ",\"checks\":[\"character state checks\",\"studio carousel\",\"setup complete checkpoint\"]}";
            File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), json);
            Debug.Log("SOLOGYM_CHARACTER_SMOKE " + json);
            if (!passed) Application.Quit(2);
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
        float L(float en, float es) => controller.Model.Language == "es" ? es : en;
        static bool HasArgument(string key) => Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0;
    }
}
