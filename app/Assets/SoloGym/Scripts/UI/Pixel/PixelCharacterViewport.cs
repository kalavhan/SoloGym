using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>A noninteractive static appearance with one uniform scale and a stable feet anchor.</summary>
    public sealed class PixelCharacterViewport : MonoBehaviour
    {
        public Image CharacterImage { get; private set; }
        public Text EmptyLabel { get; private set; }
        public PixelCharacterCatalog.Entry Character { get; private set; }
        public bool HasCharacter => Character != null && CharacterImage != null && CharacterImage.enabled;
        public float SourcePixelScale { get; private set; }
        public AccessibilityNode AccessibilityNode { get; private set; }
        public Vector3 FeetWorld => CharacterImage.rectTransform.position;
        AccessibilityHierarchy hierarchy;
        string description;
        const float Padding = 8;

        public static PixelCharacterViewport Create(Transform parent)
        {
            var root = new GameObject("Static character viewport", typeof(RectTransform), typeof(RectMask2D));
            root.transform.SetParent(parent, false); ((RectTransform)root.transform).sizeDelta = new Vector2(360, 420);
            var viewport = root.AddComponent<PixelCharacterViewport>();
            var image = new GameObject("Exact authored appearance", typeof(RectTransform), typeof(Image)); image.transform.SetParent(root.transform, false);
            viewport.CharacterImage = image.GetComponent<Image>(); viewport.CharacterImage.raycastTarget = false;
            viewport.CharacterImage.type = Image.Type.Simple; viewport.CharacterImage.color = Color.white;
            viewport.CharacterImage.enabled = false;
            var label = new GameObject("Unavailable appearance", typeof(RectTransform), typeof(Text)); label.transform.SetParent(root.transform, false);
            viewport.EmptyLabel = label.GetComponent<Text>(); viewport.EmptyLabel.font = Resources.Load<Font>("Fonts/PixelifySans");
            viewport.EmptyLabel.fontSize = 24; viewport.EmptyLabel.alignment = TextAnchor.MiddleCenter; viewport.EmptyLabel.supportRichText = false;
            viewport.EmptyLabel.color = new Color32(224, 219, 210, 255); viewport.EmptyLabel.raycastTarget = false;
            var textRect = viewport.EmptyLabel.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(Padding, Padding); textRect.offsetMax = new Vector2(-Padding, -Padding);
            return viewport;
        }
        /// <summary>False clears stale artwork and shows the caller's localized fallback.</summary>
        public bool Show(string id, string localizedDescription, string unavailableLabel)
        {
            RequireLabel(localizedDescription); RequireLabel(unavailableLabel);
            if (!PixelCharacterCatalog.TryFind(id, out var entry)) { Clear(unavailableLabel); return false; }
            var sprites = Resources.LoadAll<Sprite>(entry.resource);
            if (sprites.Length != 1 || !Mathf.Approximately(sprites[0].rect.width, entry.width)
                || !Mathf.Approximately(sprites[0].rect.height, entry.height)) { Clear(unavailableLabel); return false; }
            Character = entry; CharacterImage.sprite = sprites[0]; CharacterImage.enabled = true;
            EmptyLabel.enabled = false; description = localizedDescription; RefreshLayout(); SyncAccessibility(); return true;
        }
        public void Clear(string localizedLabel)
        {
            RequireLabel(localizedLabel); Character = null; SourcePixelScale = 0;
            CharacterImage.enabled = false; CharacterImage.sprite = null;
            EmptyLabel.text = localizedLabel; EmptyLabel.enabled = true; description = localizedLabel; SyncAccessibility();
        }
        public void SetDescription(string localizedDescription) { RequireLabel(localizedDescription); description = localizedDescription; SyncAccessibility(); }
        static void RequireLabel(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Provide a localized character description or fallback."); }
        public void RefreshLayout()
        {
            if (!HasCharacter) return;
            var rect = (RectTransform)transform; Vector2 envelope = PixelCharacterCatalog.Envelope;
            SourcePixelScale = Mathf.Max(0, Mathf.Min((rect.rect.width - 2 * Padding) / envelope.x, (rect.rect.height - 2 * Padding) / envelope.y));
            var image = CharacterImage.rectTransform;
            image.anchorMin = image.anchorMax = new Vector2(.5f, 0);
            image.pivot = new Vector2(Character.feet.x / Character.width, Character.feet.y / Character.height);
            image.anchoredPosition = new Vector2(0, Padding); image.sizeDelta = new Vector2(Character.width, Character.height) * SourcePixelScale;
        }
        public void BindAccessibility(AccessibilityHierarchy owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            UnbindAccessibility(); hierarchy = owner; AccessibilityNode = owner.AddNode(description ?? "");
            AccessibilityNode.role = AccessibilityRole.Image;
            AccessibilityNode.frameGetter = () => PixelChoiceOption.ScreenBounds((RectTransform)transform); SyncAccessibility();
        }
        void SyncAccessibility()
        {
            if (AccessibilityNode == null) return;
            if (!hierarchy.ContainsNode(AccessibilityNode)) { AccessibilityNode = null; hierarchy = null; return; }
            AccessibilityNode.label = description; AccessibilityNode.isActive = isActiveAndEnabled;
            AccessibilityNode.role = HasCharacter ? AccessibilityRole.Image : AccessibilityRole.StaticText;
        }
        public void UnbindAccessibility()
        {
            if (AccessibilityNode != null && hierarchy.ContainsNode(AccessibilityNode))
            { AccessibilityNode.frameGetter = null; hierarchy.RemoveNode(AccessibilityNode); }
            AccessibilityNode = null; hierarchy = null;
        }
        void OnRectTransformDimensionsChange() => RefreshLayout();
        void OnEnable() { RefreshLayout(); SyncAccessibility(); }
        void OnDisable() => SyncAccessibility();
        void OnDestroy() => UnbindAccessibility();
    }
}
