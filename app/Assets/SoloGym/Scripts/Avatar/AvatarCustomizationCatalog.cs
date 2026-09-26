using System;
using UnityEngine;

namespace SoloGym
{
    /// <summary>Workshop options for appearance; shared validation for UI, LayeredAvatar, and smoke.</summary>
    public static class AvatarCustomizationCatalog
    {
        [Serializable] sealed class CatalogFile
        {
            public SkinEntry[] skinPalettes;
            public HairEntry[] hairStyles;
            public FaceEntry[] eyes;
            public FaceEntry[] mouths;
            public FitEntry[] fitFamilies;
            public SlotEntry[] equipmentSlots;
        }

        [Serializable] public sealed class SkinEntry
        {
            public string id;
            public bool enabled;
            public string displayColor;
            public float[] tint;
        }

        [Serializable] public sealed class HairEntry
        {
            public string id;
            public bool enabled;
            public string layerPartId;
            public string tileColor;
            public string labelEn, labelEs;
        }

        [Serializable] public sealed class FaceEntry
        {
            public string id;
            public bool enabled;
            public string layerPartId;
            public string tileColor;
            public string labelEn, labelEs;
        }

        [Serializable] public sealed class FitEntry
        {
            public string id;
            public bool enabled;
            public string labelEn, labelEs;
        }

        [Serializable] public sealed class EquipItemEntry
        {
            public string id;
            public bool enabled;
            public string tileColor;
            public string labelEn, labelEs;
            public string[] compatibleFitFamilies;
        }

        [Serializable] public sealed class SlotEntry
        {
            public string slotId;
            public EquipItemEntry[] items;
        }

        static CatalogFile data;

        static CatalogFile Data
        {
            get
            {
                if (data != null) return data;
                var asset = Resources.Load<TextAsset>("AvatarCustomization/Catalog");
                data = asset == null ? new CatalogFile() : JsonUtility.FromJson<CatalogFile>(asset.text);
                return data;
            }
        }

        public static SkinEntry[] SkinPalettes => Data.skinPalettes ?? Array.Empty<SkinEntry>();
        public static HairEntry[] HairStyles => Data.hairStyles ?? Array.Empty<HairEntry>();
        public static FaceEntry[] Eyes => Data.eyes ?? Array.Empty<FaceEntry>();
        public static FaceEntry[] Mouths => Data.mouths ?? Array.Empty<FaceEntry>();
        public static FitEntry[] FitFamilies => Data.fitFamilies ?? Array.Empty<FitEntry>();
        public static SlotEntry[] EquipmentSlots => Data.equipmentSlots ?? Array.Empty<SlotEntry>();

        public static Color ParseHex(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#') return fallback;
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return fallback;
        }

        public static Color SkinTint(string skinPaletteId)
        {
            foreach (var entry in SkinPalettes)
            {
                if (entry.id == skinPaletteId && entry.enabled && entry.tint != null && entry.tint.Length >= 3)
                    return new Color(entry.tint[0], entry.tint[1], entry.tint[2],
                        entry.tint.Length > 3 ? entry.tint[3] : 1f);
            }
            return new Color(1f, .77f, .57f);
        }

        public static bool ValidateRecipe(AvatarAppearance recipe)
        {
            if (recipe == null) return false;
            if (recipe.baseId != "proof_male_athletic_v1") return false;
            string fit = string.IsNullOrEmpty(recipe.fitFamilyId) ? recipe.fitId : recipe.fitFamilyId;
            if (!IsEnabledFit(fit)) return false;
            if (recipe.fitId != fit) return false;
            if (!IsEnabledSkin(recipe.skinPaletteId)) return false;
            if (!IsEnabledHair(recipe.hairId)) return false;
            if (!IsEnabledFace(recipe.eyesId, Eyes)) return false;
            if (!IsEnabledFace(recipe.mouthId, Mouths)) return false;
            if (recipe.headId != "proof_head" || recipe.eyebrowsId != "proof_brows") return false;
            if (recipe.hairPaletteId != "black" || recipe.eyePaletteId != "dark") return false;
            if (!ValidateSlotItem("torso", recipe.torsoItemId, fit)) return false;
            if (!ValidateSlotItem("hands", recipe.handItemId, fit)) return false;
            if (!ValidateSlotItem("head", recipe.headItemId, fit)) return false;
            if (!ValidateSlotItem("legs", recipe.legsItemId, fit)) return false;
            if (!ValidateSlotItem("feet", recipe.feetItemId, fit)) return false;
            return true;
        }

        static bool IsEnabledSkin(string id)
        {
            foreach (var e in SkinPalettes)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        static bool IsEnabledHair(string id)
        {
            foreach (var e in HairStyles)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        static bool IsEnabledFace(string id, FaceEntry[] list)
        {
            foreach (var e in list)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        public static bool IsEnabledFit(string id)
        {
            foreach (var e in FitFamilies)
                if (e.id == id && e.enabled) return true;
            return false;
        }

        static bool ValidateSlotItem(string slotId, string itemId, string fitId)
        {
            itemId ??= "";
            foreach (var slot in EquipmentSlots)
            {
                if (slot.slotId != slotId) continue;
                foreach (var item in slot.items ?? Array.Empty<EquipItemEntry>())
                {
                    if (item.id != itemId || !item.enabled) continue;
                    if (string.IsNullOrEmpty(item.id)) return true;
                    if (item.compatibleFitFamilies == null || item.compatibleFitFamilies.Length == 0) return true;
                    foreach (var fit in item.compatibleFitFamilies)
                        if (fit == fitId) return true;
                    return false;
                }
                return false;
            }
            return string.IsNullOrEmpty(itemId);
        }

        public static string HairLayerPartId(string hairId)
        {
            foreach (var e in HairStyles)
                if (e.id == hairId) return e.layerPartId;
            return "hair_spiky";
        }

        public static EquipItemEntry[] ItemsForSlot(string slotId)
        {
            foreach (var slot in EquipmentSlots)
                if (slot.slotId == slotId) return slot.items ?? Array.Empty<EquipItemEntry>();
            return Array.Empty<EquipItemEntry>();
        }
    }
}
