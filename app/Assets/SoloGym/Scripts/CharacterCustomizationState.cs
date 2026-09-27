using System;
using UnityEngine;

namespace SoloGym
{
    public sealed class CharacterCustomizationViewModel
    {
        public string Language, Error, ErrorKey;
        public bool ReviewMode, CanContinue;
        public AvatarAppearance Appearance;
        public string Copy(string key) => CharacterCustomizationCopy.Get(key, Language);
    }

    public sealed class CharacterCustomizationController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        readonly bool reviewMode;
        readonly AvatarAppearance appearance = new AvatarAppearance();
        string language, languagePreference, errorKey = "";
        bool disposed;

        public CharacterCustomizationViewModel Model { get; private set; }
        public event Action<CharacterCustomizationViewModel> Changed;
        public event Action ExitRequested;
        public event Action<string> CheckpointRequested;

        public CharacterCustomizationController(bool reviewMode = false)
        {
            this.reviewMode = reviewMode;
            languagePreference = PlayerPrefs.GetString(LanguageKey, "auto");
            if (languagePreference != "en" && languagePreference != "es") languagePreference = "auto";
            language = ResolveLanguage(languagePreference);
            SyncFitFields();
            Refresh();
        }

        public void SetLanguage(string value)
        {
            if (disposed) return;
            if (value != "es" && value != "en" && value != "auto")
                throw new ArgumentException("Language must be en, es or auto.", nameof(value));
            languagePreference = value;
            language = ResolveLanguage(value);
            PlayerPrefs.SetString(LanguageKey, value);
            PlayerPrefs.Save();
            Refresh();
        }

        public void SelectSkin(string id)
        {
            if (disposed) return;
            if (!CatalogSkinEnabled(id)) { Fail("option_invalid"); return; }
            appearance.skinPaletteId = id;
            errorKey = "";
            Refresh();
        }

        public void SelectHair(string id)
        {
            if (disposed) return;
            if (!CatalogHairEnabled(id)) { Fail("option_invalid"); return; }
            appearance.hairId = id;
            errorKey = "";
            Refresh();
        }

        public void SelectFacePart(string category, string id)
        {
            if (disposed) return;
            if (category == "eyes")
            {
                if (!CatalogFaceEnabled(id, AvatarCustomizationCatalog.Eyes)) { Fail("option_invalid"); return; }
                appearance.eyesId = id;
            }
            else if (category == "mouth")
            {
                if (!CatalogFaceEnabled(id, AvatarCustomizationCatalog.Mouths)) { Fail("option_invalid"); return; }
                appearance.mouthId = id;
            }
            else { Fail("option_invalid"); return; }
            errorKey = "";
            Refresh();
        }

        public void SelectFitFamily(string id)
        {
            if (disposed) return;
            if (!AvatarCustomizationCatalog.IsEnabledFit(id)) { Fail("option_locked"); return; }
            appearance.fitFamilyId = id;
            appearance.fitId = id;
            errorKey = "";
            Refresh();
        }

        public void Equip(string slotId, string itemId)
        {
            if (disposed) return;
            itemId ??= "";
            string fit = appearance.fitFamilyId;
            if (!AppearanceWithSlot(slotId, itemId).IsSupportedProof)
            {
                Fail("option_invalid");
                return;
            }
            ApplySlot(slotId, itemId);
            errorKey = "";
            Refresh();
        }

        AvatarAppearance AppearanceWithSlot(string slotId, string itemId)
        {
            var clone = CloneAppearance(appearance);
            ApplySlotTo(clone, slotId, itemId ?? "");
            return clone;
        }

        void ApplySlot(string slotId, string itemId)
        {
            ApplySlotTo(appearance, slotId, itemId ?? "");
        }

        static void ApplySlotTo(AvatarAppearance target, string slotId, string itemId)
        {
            switch (slotId)
            {
                case "torso": target.torsoItemId = itemId; break;
                case "hands": target.handItemId = itemId; break;
                case "head": target.headItemId = itemId; break;
                case "legs": target.legsItemId = itemId; break;
                case "feet": target.feetItemId = itemId; break;
            }
        }

        public void Continue()
        {
            if (disposed) return;
            if (!appearance.IsSupportedProof) { Fail("appearance_invalid"); return; }
            errorKey = "";
            if (!reviewMode) { Fail("training_setup_pending"); return; }
            CheckpointRequested?.Invoke("REVIEW:SETUP_COMPLETE");
            Refresh();
        }

        public void Decline()
        {
            if (disposed) return;
            ResetDraft();
            Refresh();
            ExitRequested?.Invoke();
        }

        void ResetDraft()
        {
            appearance.baseId = "proof_male_athletic_v1";
            appearance.fitId = appearance.fitFamilyId = "proof_male_athletic";
            appearance.skinPaletteId = "warm";
            appearance.hairId = "spiky";
            appearance.eyesId = "proof_eyes";
            appearance.mouthId = "proof_mouth";
            appearance.headId = "proof_head";
            appearance.eyebrowsId = "proof_brows";
            appearance.hairPaletteId = "black";
            appearance.eyePaletteId = "dark";
            appearance.torsoItemId = "proof_training_top";
            appearance.handItemId = "proof_cyan_gloves";
            appearance.headItemId = appearance.legsItemId = appearance.feetItemId = "";
            errorKey = "";
        }

        void SyncFitFields()
        {
            if (string.IsNullOrEmpty(appearance.fitFamilyId)) appearance.fitFamilyId = appearance.fitId;
            if (string.IsNullOrEmpty(appearance.fitId)) appearance.fitId = appearance.fitFamilyId;
        }

        static bool CatalogSkinEnabled(string id)
        {
            foreach (var e in AvatarCustomizationCatalog.SkinPalettes)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        static bool CatalogHairEnabled(string id)
        {
            foreach (var e in AvatarCustomizationCatalog.HairStyles)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        static bool CatalogFaceEnabled(string id, AvatarCustomizationCatalog.FaceEntry[] list)
        {
            foreach (var e in list)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        static string ResolveLanguage(string preference)
        {
            if (preference == "en" || preference == "es") return preference;
            return Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
        }

        void Fail(string key)
        {
            errorKey = key;
            Refresh();
        }

        void Refresh()
        {
            if (disposed) return;
            SyncFitFields();
            Model = new CharacterCustomizationViewModel
            {
                Language = language,
                ErrorKey = errorKey,
                Error = CharacterCustomizationCopy.Get(errorKey, language),
                ReviewMode = reviewMode,
                Appearance = CloneAppearance(appearance),
                CanContinue = appearance.IsSupportedProof
            };
            Changed?.Invoke(Model);
        }

        static AvatarAppearance CloneAppearance(AvatarAppearance source)
        {
            return JsonUtility.FromJson<AvatarAppearance>(JsonUtility.ToJson(source));
        }

        public void Dispose()
        {
            if (disposed) return;
            ResetDraft();
            disposed = true;
            Model = null;
            Changed = null;
            ExitRequested = null;
            CheckpointRequested = null;
        }
    }

    public static class CharacterCustomizationCopy
    {
        static readonly System.Collections.Generic.Dictionary<string, string[]> Values =
            new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "window_title", new[] { "YOUR CHARACTER", "TU PERSONAJE" } },
                { "subtitle", new[] { "Shape your look before training", "Define tu aspecto antes de entrenar" } },
                { "skin_section", new[] { "Skin tone", "Tono de piel" } },
                { "face_section", new[] { "Face", "Cara" } },
                { "body_section", new[] { "Body build", "Complexión" } },
                { "equip_section", new[] { "Equipment", "Equipo" } },
                { "hair_row", new[] { "Hairstyle", "Peinado" } },
                { "eyes_row", new[] { "Eyes", "Ojos" } },
                { "mouth_row", new[] { "Mouth", "Boca" } },
                { "slot_torso", new[] { "Torso", "Torso" } },
                { "slot_hands", new[] { "Hands", "Manos" } },
                { "slot_head", new[] { "Head", "Cabeza" } },
                { "slot_legs", new[] { "Legs", "Piernas" } },
                { "slot_feet", new[] { "Feet", "Pies" } },
                { "cat_skin", new[] { "Skin", "Piel" } },
                { "cat_face", new[] { "Face", "Cara" } },
                { "cat_body", new[] { "Body", "Cuerpo" } },
                { "cat_gear", new[] { "Gear", "Equipo" } },
                { "cat_rotate", new[] { "Turn", "Girar" } },
                { "face_picker", new[] { "Hairstyle · Eyes · Mouth", "Peinado · Ojos · Boca" } },
                { "locked_soon", new[] { "Coming soon", "Próximamente" } },
                { "helper", new[] {
                    "Height and weight are private training data. They do not define your appearance.",
                    "Altura y peso son datos privados de entrenamiento. No definen tu apariencia." } },
                { "continue", new[] { "CONTINUE", "CONTINUAR" } },
                { "not_now", new[] { "Not now", "Ahora no" } },
                { "option_invalid", new[] { "That option is not available.", "Esa opción no está disponible." } },
                { "option_locked", new[] { "That build is not available yet.", "Esa complexión aún no está disponible." } },
                { "appearance_invalid", new[] { "Choose a supported appearance.", "Elige una apariencia compatible." } },
                { "training_setup_pending", new[] {
                    "Training setup is not available yet. The private profile service and production gates must be ready before saving choices.",
                    "La configuración del entrenamiento aún no está disponible. El servicio de perfil privado y las condiciones de producción deben estar listos antes de guardar las elecciones." } },
                { "setup_complete_title", new[] { "Setup complete", "Configuración lista" } },
                { "setup_complete_body", new[] {
                    "Your initial training setup is complete in this preview. Nothing was saved to your account.",
                    "Tu configuración inicial de entrenamiento está lista en esta vista previa. No se guardó nada en tu cuenta." } }
            };

        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }

    public static class CharacterCustomizationStateChecks
    {
        public static string Run()
        {
            const string languageKey = "SoloGym.Home.Language.v1";
            string originalLanguage = PlayerPrefs.GetString(languageKey, "auto");
            int passed = 0;
            try
            {
                using (var normal = new CharacterCustomizationController())
                {
                    normal.Continue();
                    Require(normal.Model.ErrorKey == "training_setup_pending", "production cannot checkpoint"); passed++;
                }
                using (var review = new CharacterCustomizationController(true))
                {
                    string checkpoint = null;
                    review.CheckpointRequested += id => checkpoint = id;
                    review.SelectSkin("deep");
                    review.SelectHair("swept");
                    review.Equip("torso", "");
                    review.Equip("hands", "");
                    review.Continue();
                    Require(checkpoint == "REVIEW:SETUP_COMPLETE", "setup complete checkpoint"); passed++;
                    Require(review.Model.Appearance.skinPaletteId == "deep", "skin persisted"); passed++;
                    Require(string.IsNullOrEmpty(review.Model.Appearance.torsoItemId), "unequip torso"); passed++;
                    review.Decline();
                    Require(review.Model.Appearance.skinPaletteId == "warm", "decline resets"); passed++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(languageKey, originalLanguage);
                PlayerPrefs.Save();
            }
            return passed + " character customization state checks passed";
        }

        static void Require(bool condition, string context)
        {
            if (!condition) throw new InvalidOperationException("Character check failed: " + context);
        }
    }
}
