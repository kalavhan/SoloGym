using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Independent native Toggle with the shared pixel border/check art and a full-row hit target.</summary>
    public sealed class PixelCheckbox : Toggle
    {
        public Text Label { get; private set; }
        Image box, mark;
        Outline focus;
        public static PixelCheckbox Create(Transform parent, string name)
        {
            var root = PixelJournalUI.Rect(name, parent, new Rect(0,0,502,52));
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.canvasRenderer.cullTransparentMesh = false;
            var control = root.gameObject.AddComponent<PixelCheckbox>(); control.transition = Transition.None; control.toggleTransition = ToggleTransition.None;
            control.box = PixelJournalUI.Art(root,new Rect(8,9,34,34),"UI/Pixel/PrimaryButton");
            control.box.sprite = Resources.LoadAll<Sprite>("UI/Pixel/PrimaryButton")[0]; control.box.type = Image.Type.Sliced; control.box.fillCenter = false; control.box.pixelsPerUnitMultiplier = 2;
            control.targetGraphic = control.box;
            control.focus = control.box.gameObject.AddComponent<Outline>(); control.focus.effectColor = PixelJournalUI.Ivory; control.focus.effectDistance = new Vector2(2,-2);
            control.mark = PixelJournalUI.Art(root,new Rect(14,15,22,22),"UI/Pixel/ChoiceCheck"); control.mark.preserveAspect = true; control.graphic = control.mark;
            control.Label = PixelJournalUI.Text(root,new Rect(62,0,440,52),"",22,false);
            control.SetIsOnWithoutNotify(false); control.Refresh(); return control;
        }
        public void SetLabel(string text) { Label.text=text; Refresh(); }
        public void Refresh() => DoStateTransition(currentSelectionState,true);
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            if (box == null || Label == null) return;
            bool enabled = IsInteractable();
            box.color = enabled ? Color.white : new Color32(130,132,126,255);
            Label.color = enabled ? PixelJournalUI.Ivory : new Color32(151,153,151,255);
            mark.color = Label.color;
            focus.enabled = enabled && (!PixelTouch.HideFocus && (state == SelectionState.Selected || state == SelectionState.Highlighted));
        }
    }
}
