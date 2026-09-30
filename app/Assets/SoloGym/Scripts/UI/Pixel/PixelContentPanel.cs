using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>A decorative nine-sliced frame with a padded, clipped live-content slot.</summary>
    [AddComponentMenu("SoloGym/UI/Pixel Content Panel")]
    public sealed class PixelContentPanel : MonoBehaviour
    {
        public const float MinimumWidth = 240, MinimumHeight = 144;
        [SerializeField] Image background;
        [SerializeField] RectTransform content;

        public Image Background => background;
        public RectTransform Content => content;

        public static PixelContentPanel Create(Transform parent)
        {
            var sprites = Resources.LoadAll<Sprite>("UI/Pixel/ContentPanel");
            if (sprites.Length == 0)
                throw new InvalidOperationException("Import the ContentPanel sprite before creating panels.");
            var root = new GameObject("Framed content panel", typeof(RectTransform), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 360);
            var layout = root.GetComponent<LayoutElement>();
            layout.minWidth = MinimumWidth; layout.minHeight = MinimumHeight;
            layout.preferredWidth = 480; layout.preferredHeight = 360;
            var panel = root.AddComponent<PixelContentPanel>();
            panel.background = root.AddComponent<Image>();
            panel.background.sprite = sprites[0];
            panel.background.type = Image.Type.Sliced;
            panel.background.raycastTarget = false;
            panel.content = new GameObject("Live content", typeof(RectTransform), typeof(RectMask2D))
                .GetComponent<RectTransform>();
            panel.content.SetParent(root.transform, false);
            panel.content.anchorMin = Vector2.zero;
            panel.content.anchorMax = Vector2.one;
            panel.SetPadding(40, 40, 40, 40);
            return panel;
        }

        /// <summary>Insets are logical UI units; preserve at least the 32-unit corner region.</summary>
        public void SetPadding(float left, float right, float top, float bottom)
        {
            if (!ValidInset(left) || !ValidInset(right) || !ValidInset(top) || !ValidInset(bottom))
                throw new ArgumentOutOfRangeException(nameof(left), "Panel insets must be finite and at least 32 units.");
            if (content == null) return;
            content.offsetMin = new Vector2(left, bottom);
            content.offsetMax = new Vector2(-right, -top);
        }

        static bool ValidInset(float value) => value >= 32 && !float.IsInfinity(value) && !float.IsNaN(value);
    }
}
