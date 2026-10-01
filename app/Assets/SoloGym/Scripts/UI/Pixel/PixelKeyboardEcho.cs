using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>
    /// On phones the landscape keyboard covers most of the screen, often including the field being
    /// edited. This top banner always shows which field is active and what is being typed.
    /// </summary>
    public sealed class PixelKeyboardEcho : MonoBehaviour
    {
        RectTransform bar;
        Text label, value;
        float blink;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Application.isMobilePlatform || FindFirstObjectByType<PixelKeyboardEcho>() != null) return;
            var go = new GameObject("Keyboard echo"); DontDestroyOnLoad(go); go.AddComponent<PixelKeyboardEcho>();
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
            bar = new GameObject("Typing banner", typeof(RectTransform)).GetComponent<RectTransform>(); bar.SetParent(transform, false);
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(.5f, 1); bar.sizeDelta = new Vector2(0, 118); bar.anchoredPosition = Vector2.zero;
            var bg = bar.gameObject.AddComponent<Image>(); bg.color = new Color32(14, 20, 28, 245); bg.raycastTarget = false;
            var edge = new GameObject("Gold edge", typeof(RectTransform)).GetComponent<RectTransform>(); edge.SetParent(bar, false);
            edge.anchorMin = Vector2.zero; edge.anchorMax = new Vector2(1, 0); edge.pivot = new Vector2(.5f, 0); edge.sizeDelta = new Vector2(0, 4);
            edge.gameObject.AddComponent<Image>().color = new Color32(214, 170, 92, 255);
            var font = Resources.Load<Font>("Fonts/PixelifySans");
            label = Make("Field", new Vector2(40, -10), new Vector2(-40, 36), 24, new Color32(183, 185, 175, 255), font);
            value = Make("Typed text", new Vector2(40, -44), new Vector2(-40, 64), 40, new Color32(255, 240, 202, 255), font);
            bar.gameObject.SetActive(false);
        }

        Text Make(string name, Vector2 position, Vector2 size, int fontSize, Color color, Font font)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(bar, false);
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = position; r.sizeDelta = new Vector2(size.x - position.x, size.y);
            var t = r.gameObject.AddComponent<Text>(); t.font = font; t.fontSize = fontSize; t.color = color; t.raycastTarget = false;
            t.supportRichText = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Truncate; t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        void LateUpdate()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var input = selected != null ? selected.GetComponent<InputField>() : null;
            bool show = input != null && input.isFocused && input.gameObject.activeInHierarchy;
            if (bar.gameObject.activeSelf != show) bar.gameObject.SetActive(show);
            if (!show) return;
            var field = input.GetComponentInParent<PixelFormField>();
            string name = field != null && field.Label != null && !string.IsNullOrWhiteSpace(field.Label.text) ? field.Label.text
                : input.placeholder is Text p ? p.text : "";
            label.text = name;
            string text = input.text ?? "";
            if (input.contentType == InputField.ContentType.Password) text = new string('•', text.Length);
            // Keep the end of long entries visible.
            if (text.Length > 48) text = "…" + text.Substring(text.Length - 47);
            blink += Time.unscaledDeltaTime;
            value.text = text + ((int)(blink * 2) % 2 == 0 ? "▌" : " ");
        }
    }
}
