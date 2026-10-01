using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Labeled single-line native input with a sprite skin and caller-owned validation.</summary>
    public sealed class PixelFormField : MonoBehaviour
    {
        public enum Kind { Text, Email, Password }
        public PixelFieldInput Input { get; private set; }
        public Text Label { get; private set; }
        public Text Placeholder { get; private set; }
        public Text Message { get; private set; }
        public Image Background { get; private set; }
        public PixelPrimaryButton Visibility { get; private set; }
        public bool PasswordVisible { get; private set; }
        public string Error { get; private set; } = "";
        public string VisualState { get; private set; }
        public Kind FieldKind { get; private set; }
        Color labelColor = new Color32(251, 224, 166, 255);
        public void SetLabelColor(Color value) { labelColor = value; Refresh(); }
        Outline outline;
        LayoutElement layoutElement;
        public float PreferredHeight => layoutElement == null ? 130 : layoutElement.preferredHeight;
        string helper = "", showLabel = "Show", hideLabel = "Hide";
        PixelFieldTabNavigation traversal;

        public static PixelFormField Create(Transform parent, Kind kind, string label, string placeholder, string helper = "")
        {
            var sprites = Resources.LoadAll<Sprite>("UI/Pixel/FormField");
            if (sprites.Length == 0) throw new InvalidOperationException("Import the FormField sprite before creating inputs.");
            var root = Rect("Labeled form field", parent);
            root.sizeDelta = new Vector2(480, 130);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = kind == Kind.Password ? 400 : 240;
            layout.minHeight = 100; layout.preferredHeight = 130; layout.preferredWidth = 480;
            var field = root.gameObject.AddComponent<PixelFormField>();
            field.layoutElement = layout;
            field.FieldKind = kind;
            field.Label = Text(root, "Label", 22);
            Place(field.Label.rectTransform, 0, 0, 0, 28);
            var inputRect = Rect("Native input", root);
            Place(inputRect, 0, kind == Kind.Password ? 140 : 0, 32, 64);
            field.Background = inputRect.gameObject.AddComponent<Image>();
            field.Background.sprite = sprites[0]; field.Background.type = Image.Type.Sliced;
            field.outline = inputRect.gameObject.AddComponent<Outline>();
            field.outline.effectDistance = new Vector2(2, -2);
            field.outline.useGraphicAlpha = true;
            field.Input = inputRect.gameObject.AddComponent<PixelFieldInput>();
            field.Input.transition = Selectable.Transition.None;
            field.Input.targetGraphic = field.Background;
            var viewport = Rect("Text viewport", inputRect);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20, 8); viewport.offsetMax = new Vector2(-20, -8);
            viewport.gameObject.AddComponent<RectMask2D>();
            var value = Text(viewport, "Editable value", 24);
            Stretch(value.rectTransform);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.alignment = TextAnchor.MiddleLeft;
            field.Placeholder = Text(viewport, "Placeholder", 22);
            Stretch(field.Placeholder.rectTransform);
            field.Placeholder.alignment = TextAnchor.MiddleLeft;
            field.Placeholder.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.Placeholder.color = new Color32(161, 154, 143, 255);
            field.Input.textComponent = value;
            field.Input.placeholder = field.Placeholder;
            field.Input.contentType = kind == Kind.Password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            field.Input.lineType = InputField.LineType.SingleLine;
            field.Input.characterValidation = InputField.CharacterValidation.None;
            if (kind == Kind.Email) field.Input.keyboardType = TouchScreenKeyboardType.EmailAddress;
            field.Input.asteriskChar = '*';
            field.Input.customCaretColor = true;
            field.Input.caretColor = new Color32(251, 224, 166, 255);
            field.Input.selectionColor = new Color32(69, 107, 115, 210);
            field.Input.caretWidth = 2;
            field.Message = Text(root, "Helper or error", 18);
            Place(field.Message.rectTransform, 0, 0, 102, 28);
            field.traversal = inputRect.gameObject.AddComponent<PixelFieldTabNavigation>();
            if (kind == Kind.Password)
            {
                field.Visibility = PixelPrimaryButton.Create(root, "Show", field.TogglePasswordVisibility);
                field.Visibility.name = "Password visibility";
                var toggle = (RectTransform)field.Visibility.transform;
                toggle.anchorMin = toggle.anchorMax = toggle.pivot = new Vector2(1, 1);
                toggle.anchoredPosition = new Vector2(0, -32); toggle.sizeDelta = new Vector2(128, 64);
                field.Visibility.SetFontSize(20);
                // Pointer taps retain the input's focus/selection; explicit Tab links still reach this control.
                field.Visibility.navigation = new Navigation { mode = Navigation.Mode.None };
                field.Visibility.gameObject.AddComponent<PixelFieldTabNavigation>().Previous = field.Input;
                field.traversal.Next = field.Visibility;
            }
            field.Input.RefreshPresentation = field.Refresh;
            field.SetLocalizedText(label, placeholder, helper, "Show", "Hide");
            return field;
        }

        public void SetLocalizedText(string label, string placeholder, string helperText, string show, string hide)
        {
            Label.text = label ?? ""; Placeholder.text = placeholder ?? ""; helper = helperText ?? "";
            showLabel = show ?? ""; hideLabel = hide ?? "";
            Refresh(); // Changing UI copy never replaces entered values or clears caller validation.
        }

        public void SetError(string error) { Error = error ?? ""; Refresh(); }
        public void SetInteractable(bool value) { Input.interactable = value; Refresh(); }
        public void SetValueWithoutNotify(string value) { Input.SetTextWithoutNotify(value ?? ""); }
        public void SetTraversal(Selectable previous, Selectable next)
        {
            traversal.Previous = previous;
            if (Visibility == null) traversal.Next = next;
            else Visibility.GetComponent<PixelFieldTabNavigation>().Next = next;
        }

        public void TogglePasswordVisibility()
        {
            if (FieldKind != Kind.Password || !Input.IsInteractable()) return;
            PasswordVisible = !PasswordVisible;
            Input.inputType = PasswordVisible ? InputField.InputType.Standard : InputField.InputType.Password;
            Input.ForceLabelUpdate();
            Refresh();
        }

        public void HidePassword()
        {
            if (FieldKind != Kind.Password || Input == null) return;
            PasswordVisible = false; Input.inputType = InputField.InputType.Password; Input.ForceLabelUpdate(); Refresh();
        }

        void OnDisable() { HidePassword(); }
        void OnRectTransformDimensionsChange() { RefreshLayout(); }

        void RefreshLayout()
        {
            if (Input == null || Message == null || layoutElement == null) return;
            float labelHeight = Mathf.Max(28, Mathf.Ceil(Label.preferredHeight));
            float inputTop = labelHeight + 4;
            float messageTop = inputTop + 70;
            float messageHeight = string.IsNullOrEmpty(Message.text) ? 0 : Mathf.Max(28, Mathf.Ceil(Message.preferredHeight));
            Place(Label.rectTransform, 0, 0, 0, labelHeight);
            Place((RectTransform)Input.transform, 0, FieldKind == Kind.Password ? 140 : 0, inputTop, 64);
            Place(Message.rectTransform, 0, 0, messageTop, messageHeight);
            if (Visibility != null) ((RectTransform)Visibility.transform).anchoredPosition = new Vector2(0, -inputTop);
            layoutElement.preferredHeight = messageHeight == 0 ? inputTop + 68 : messageTop + messageHeight;
        }

        void Refresh()
        {
            if (Input == null || Message == null) return;
            bool enabled = Input.IsInteractable();
            bool focused = enabled && Input.HasKeyboardFocus;
            bool invalid = Error.Length != 0;
            VisualState = !enabled ? "disabled" : invalid ? "invalid" : focused ? "focused" : "normal";
            Background.color = enabled ? Color.white : new Color32(132, 130, 129, 255);
            outline.enabled = enabled && (focused || invalid);
            outline.effectColor = invalid ? new Color32(235, 139, 121, 255) : new Color32(251, 224, 166, 255);
            Label.color = enabled ? labelColor : new Color32(170, 164, 155, 255);
            Input.textComponent.color = enabled ? new Color32(255, 240, 202, 255) : new Color32(170, 164, 155, 255);
            Message.text = invalid ? "! " + Error : helper;
            RefreshLayout();
            Message.color = invalid ? new Color32(255, 174, 155, 255) : new Color32(181, 174, 163, 255);
            if (Visibility != null)
            {
                Visibility.interactable = enabled;
                Visibility.SetLabel(PasswordVisible ? hideLabel : showLabel);
            }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        static Text Text(Transform parent, string name, int size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = Resources.Load<Font>("Fonts/PixelifySans"); text.fontSize = size;
            text.supportRichText = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.UpperLeft;
            return text;
        }
        static void Place(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 1); rect.offsetMin = new Vector2(left, -y - height); rect.offsetMax = new Vector2(-right, -y);
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
