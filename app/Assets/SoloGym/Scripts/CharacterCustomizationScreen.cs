using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    public sealed class CharacterCustomizationScreen : MonoBehaviour
    {
        const float W = PortalFrameLayout.PageWidth, H = PortalFrameLayout.PageHeight;
        const float WindowTitleY = 490;
        const float SwatchSize = 56f, SwatchGap = 10f, TileSize = 72f, TileGap = 8f;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        RectTransform root, page, modal;
        Font serif, bold, body;
        CharacterCustomizationController controller;
        LayeredAvatar bodyAvatar, faceAvatar;
        Action back, leave;
        Action<string> onCheckpoint;
        string capture;
        Text status;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();
        Vector2 lastSize;
        Rect lastSafe;
        float choicesScrollY;
        PortalWindowFrame.ScheduleSetupLayout layout;

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
            var node = new GameObject("Character canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            node.transform.SetParent(transform, false);
            var canvas = node.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvas.pixelPerfect = true;
            root = RectNode("Character reference 853x1844", node.transform, new Rect(0, 0, W, H));
            controller.Changed += Render;
            controller.ExitRequested += Exit;
            controller.CheckpointRequested += id =>
            {
                if (onCheckpoint != null) onCheckpoint(id);
                else Notice(controller.Model.Copy("setup_complete_title"), controller.Model.Copy("setup_complete_body"));
            };
            Render(controller.Model);
            Fit();
            if (capture != null) StartCoroutine(Capture());
        }

        public void Resume(string language)
        {
            gameObject.SetActive(true);
            controller.SetLanguage(language);
            Fit();
        }

        void Render(CharacterCustomizationViewModel model)
        {
            CloseModal();
            if (page != null)
            {
                var existingScroll = page.GetComponentInChildren<ScrollRect>();
                if (existingScroll != null && existingScroll.content != null)
                    choicesScrollY = existingScroll.content.anchoredPosition.y;
                page.gameObject.SetActive(false);
                Destroy(page.gameObject);
            }
            controls.Clear();
            bodyAvatar = faceAvatar = null;
            page = RectNode("Character page", root, new Rect(0, 0, W, H));
            float choicesHeight = MeasureChoicesContentHeight(model);
            layout = PortalWindowFrame.SolveCharacterSetup(choicesHeight);
            SystemUI.PortalPage(page, layout.PanelY, layout.PanelHeight);
            SystemUI.Divider(page, 807);
            SystemUI.Icon(page, new Rect(43, 47, 28, 34), "back", Silver);
            SystemUI.Icon(page, new Rect(421, 1728, 10, 25), "sigil", Silver);
            Live("Back", new Rect(92, 53, L(61, 83), 25), L("Back", "Volver"), 31);
            Live("Language", new Rect(765, 56, 29, 20), model.Language.ToUpperInvariant(), 27);
            Hit("back", new Rect(30, 20, 180, 100), Back);
            Hit("language", new Rect(680, 20, 130, 100), Language);
            Live("Title", new Rect(L(168, 200), WindowTitleY, L(517, 453), 53),
                model.Copy("window_title"), 72, Silver, bold);
            TextAt(page, new Rect(112, WindowTitleY + 62, 629, 52), model.Copy("subtitle"), 24, body, Silver,
                TextAnchor.MiddleCenter).horizontalOverflow = HorizontalWrapMode.Wrap;
            BuildBodyPreview(model);
            BuildChoicesScroll(model);
            Primary(() => controller.Continue(), model.Copy("continue"), model.CanContinue, layout.PrimaryRect,
                layout.PrimaryCaptionRect);
            Secondary("leave", layout.SecondaryRect, model.Copy("not_now"), controller.Decline);
            Live("Privacy", new Rect(L(294, 275), 1735, L(89, 116), 24), L("Privacy", "Privacidad"), 29);
            Live("Terms", new Rect(L(491, 472), 1735, L(68, 101), 24), L("Terms", "Términos"), 29);
            Hit("privacy", new Rect(250, 1706, 179, 85), () => Document("privacy"));
            Hit("terms", new Rect(450, 1706, 172, 85), () => Document("terms"));
            if (model.ReviewMode)
                TextAt(page, new Rect(80, 280, 693, 35),
                    L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"),
                    20, body, Cyan, TextAnchor.MiddleCenter);
            status = TextAt(page, layout.StatusRect, model.Error, 20, body, new Color32(255, 207, 161, 255),
                TextAnchor.MiddleCenter);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void BuildBodyPreview(CharacterCustomizationViewModel model)
        {
            float top = PortalWindowFrame.CharacterPreviewTop;
            float height = PortalWindowFrame.CharacterPreviewHeight;
            var clip = RectNode("Avatar preview clip", page, new Rect(95, top, 665, height));
            clip.gameObject.AddComponent<RectMask2D>();
            var node = RectNode("Layered avatar body", clip, new Rect(332, height * 0.55f, 0, 0));
            bodyAvatar = node.gameObject.AddComponent<LayeredAvatar>();
            bodyAvatar.freeze = true;
            bodyAvatar.previewTime = 0.55f;
            bodyAvatar.action = "idle";
            bodyAvatar.previewMode = LayeredAvatar.AvatarPreviewMode.FullBody;
            bodyAvatar.Initialize(model.Appearance);
        }

        void BuildFacePreview(RectTransform parent, CharacterCustomizationViewModel model, float y)
        {
            var clip = RectNode("Face close-up", parent, new Rect(12, y, 617, 140));
            clip.gameObject.AddComponent<RectMask2D>();
            SystemUI.Panel(clip, new Rect(0, 0, 617, 140), PanelStyle.Outline);
            var node = RectNode("Face avatar", clip, new Rect(308, 95, 0, 0));
            faceAvatar = node.gameObject.AddComponent<LayeredAvatar>();
            faceAvatar.freeze = true;
            faceAvatar.previewTime = 0.4f;
            faceAvatar.action = "idle";
            faceAvatar.previewMode = LayeredAvatar.AvatarPreviewMode.FaceCloseUp;
            faceAvatar.Initialize(model.Appearance);
        }

        float MeasureChoicesContentHeight(CharacterCustomizationViewModel model)
        {
            var measureHost = RectNode("Choices measure", page,
                new Rect(PortalFrameLayout.ContentX, -8000, PortalFrameLayout.ContentWidth, 8));
            BuildChoicesContent(model, measureHost, out float height);
            Destroy(measureHost.gameObject);
            return height;
        }

        void BuildChoicesScroll(CharacterCustomizationViewModel model)
        {
            var viewportRect = layout.ScrollViewport;
            var viewport = RectNode("Choices viewport", page, viewportRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            BuildChoicesContent(model, viewport, out _);
            var content = viewport.GetChild(0) as RectTransform;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            Canvas.ForceUpdateCanvases();
            float maxScroll = Mathf.Max(0, content.rect.height - viewport.rect.height);
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(choicesScrollY, 0, maxScroll));
            scroll.onValueChanged.AddListener(_ =>
            {
                if (scroll.content != null) choicesScrollY = scroll.content.anchoredPosition.y;
            });
            SystemUI.AttachScrollAffordance(scroll, viewportRect, 74, page);
        }

        void BuildChoicesContent(CharacterCustomizationViewModel model, RectTransform viewport, out float totalHeight)
        {
            var content = RectNode("Choices content", viewport,
                new Rect(0, 0, PortalFrameLayout.ScrollContentWidth, 1200));
            content.pivot = new Vector2(0, 1);
            content.anchorMin = content.anchorMax = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            float y = 8;
            var app = model.Appearance;
            bool es = model.Language == "es";

            TextAt(content, new Rect(12, y, 617, 30), model.Copy("skin_section"), 24, serif, Silver, TextAnchor.MiddleLeft);
            y += 34;
            var skinTiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var entry in AvatarCustomizationCatalog.SkinPalettes)
            {
                if (!entry.enabled) continue;
                string id = entry.id;
                skinTiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "skin_" + id,
                    selected = app.skinPaletteId == id,
                    enabled = true,
                    tileColor = AvatarCustomizationCatalog.ParseHex(entry.displayColor, Color.gray),
                    onSelect = () => controller.SelectSkin(id)
                });
            }
            y = SystemUI.SkinSwatchRow(content, y, 617, SwatchSize, SwatchGap, skinTiles.ToArray(), controls);

            TextAt(content, new Rect(12, y, 617, 30), model.Copy("face_section"), 24, serif, Silver, TextAnchor.MiddleLeft);
            y += 34;
            BuildFacePreview(content, model, y);
            y += 152;

            y = BuildHairRow(content, y, model, app, es);
            y = BuildFacePartRow(content, y, model.Copy("eyes_row"), AvatarCustomizationCatalog.Eyes, app.eyesId, "eyes", es);
            y = BuildFacePartRow(content, y, model.Copy("mouth_row"), AvatarCustomizationCatalog.Mouths, app.mouthId, "mouth", es);

            TextAt(content, new Rect(12, y, 617, 30), model.Copy("body_section"), 24, serif, Silver, TextAnchor.MiddleLeft);
            y += 34;
            y = BuildFitRow(content, y, model, app, es);

            TextAt(content, new Rect(12, y, 617, 30), model.Copy("equip_section"), 24, serif, Silver, TextAnchor.MiddleLeft);
            y += 34;
            foreach (var slot in AvatarCustomizationCatalog.EquipmentSlots)
                y = BuildEquipSlotRow(content, y, model, slot.slotId, app, es);

            var helper = TextAt(content, new Rect(12, y, 617, 100), model.Copy("helper"), 21, body, Silver,
                TextAnchor.UpperLeft);
            helper.horizontalOverflow = HorizontalWrapMode.Wrap;
            Canvas.ForceUpdateCanvases();
            y += helper.preferredHeight + PortalFrameLayout.SectionGapMd;
            content.sizeDelta = new Vector2(PortalFrameLayout.ScrollContentWidth, y);
            totalHeight = y;
        }

        float BuildHairRow(RectTransform content, float y, CharacterCustomizationViewModel model, AvatarAppearance app, bool es)
        {
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var entry in AvatarCustomizationCatalog.HairStyles)
            {
                string id = entry.id;
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "hair_" + id,
                    selected = app.hairId == id,
                    enabled = entry.enabled,
                    tileColor = AvatarCustomizationCatalog.ParseHex(entry.tileColor, Color.gray),
                    caption = es ? entry.labelEs : entry.labelEn,
                    onSelect = () => controller.SelectHair(id)
                });
            }
            return SystemUI.OptionThumbnailRow(content, y, 617, model.Copy("hair_row"), TileSize, TileGap,
                tiles.ToArray(), serif, controls);
        }

        float BuildFacePartRow(RectTransform content, float y, string rowLabel,
            AvatarCustomizationCatalog.FaceEntry[] entries, string selectedId, string category, bool es)
        {
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var entry in entries)
            {
                string id = entry.id;
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = category + "_" + id,
                    selected = selectedId == id,
                    enabled = entry.enabled,
                    tileColor = AvatarCustomizationCatalog.ParseHex(entry.tileColor, Color.gray),
                    caption = es ? entry.labelEs : entry.labelEn,
                    onSelect = () => controller.SelectFacePart(category, id)
                });
            }
            return SystemUI.OptionThumbnailRow(content, y, 617, rowLabel, TileSize, TileGap, tiles.ToArray(), serif,
                controls);
        }

        float BuildFitRow(RectTransform content, float y, CharacterCustomizationViewModel model, AvatarAppearance app,
            bool es)
        {
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            string fit = string.IsNullOrEmpty(app.fitFamilyId) ? app.fitId : app.fitFamilyId;
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
            return SystemUI.OptionThumbnailRow(content, y, 617, "", TileSize, TileGap, tiles.ToArray(), serif, controls);
        }

        float BuildEquipSlotRow(RectTransform content, float y, CharacterCustomizationViewModel model, string slotId,
            AvatarAppearance app, bool es)
        {
            string rowLabel = model.Copy("slot_" + slotId);
            string current = SlotValue(app, slotId);
            var tiles = new List<SystemUI.WorkshopTileSpec>();
            foreach (var item in AvatarCustomizationCatalog.ItemsForSlot(slotId))
            {
                string id = item.id ?? "";
                tiles.Add(new SystemUI.WorkshopTileSpec
                {
                    key = "equip_" + slotId + "_" + (string.IsNullOrEmpty(id) ? "none" : id),
                    selected = current == id,
                    enabled = item.enabled,
                    tileColor = AvatarCustomizationCatalog.ParseHex(item.tileColor, Color.gray),
                    caption = es ? item.labelEs : item.labelEn,
                    onSelect = () => controller.Equip(slotId, id)
                });
            }
            return SystemUI.OptionThumbnailRow(content, y, 617, rowLabel, TileSize, TileGap, tiles.ToArray(), serif,
                controls);
        }

        static string SlotValue(AvatarAppearance app, string slotId)
        {
            switch (slotId)
            {
                case "torso": return app.torsoItemId ?? "";
                case "hands": return app.handItemId ?? "";
                case "head": return app.headItemId ?? "";
                case "legs": return app.legsItemId ?? "";
                case "feet": return app.feetItemId ?? "";
                default: return "";
            }
        }

        void Primary(Action action, string text, bool active, Rect buttonRect, Rect captionRect)
        {
            var surface = SystemUI.Panel(page, buttonRect, PanelStyle.Primary);
            surface.color = active ? Color.white : new Color(.5f, .6f, .7f, .7f);
            SystemUI.Caption(page, captionRect, text, 45, bold, active ? Silver : SystemUI.Theme.muted);
            var button = Hit("continue", buttonRect, action);
            button.interactable = active;
        }

        void Secondary(string key, Rect rect, string label, Action action)
        {
            TextAt(page, rect, label, 26, serif, Cyan, TextAnchor.MiddleCenter);
            Hit(key, rect, action);
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
            try { Debug.Log("SOLOGYM_CHARACTER_STATE " + CharacterCustomizationStateChecks.Run()); }
            catch (Exception e) { Debug.LogError(e.Message); passed = false; }
            if (controls.TryGetValue("skin_deep", out Button skinDeep))
            {
                skinDeep.onClick.Invoke();
                yield return null;
                passed &= controller.Model.Appearance.skinPaletteId == "deep";
            }
            if (controls.TryGetValue("hair_swept", out Button hair))
            {
                hair.onClick.Invoke();
                yield return null;
            }
            if (controls.TryGetValue("equip_torso_none", out Button unequip))
            {
                unequip.onClick.Invoke();
                yield return null;
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
                + ",\"checks\":[\"character state checks\",\"workshop selections\",\"setup complete checkpoint\"]}";
            File.WriteAllText(Path.ChangeExtension(capture, ".smoke.json"), json);
            Debug.Log("SOLOGYM_CHARACTER_SMOKE " + json);
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
