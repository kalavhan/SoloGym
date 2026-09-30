using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Native InputField with notifications for its surrounding presentation.</summary>
    public sealed class PixelFieldInput : InputField
    {
        public Action RefreshPresentation;
        public bool HasKeyboardFocus { get; private set; }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            RefreshPresentation?.Invoke();
        }

        protected override void OnDisable()
        {
            HasKeyboardFocus = false;
            base.OnDisable();
        }

        public override void OnSelect(BaseEventData data)
        {
            HasKeyboardFocus = true;
            base.OnSelect(data);
            RefreshPresentation?.Invoke();
        }

        public override void OnDeselect(BaseEventData data)
        {
            HasKeyboardFocus = false;
            base.OnDeselect(data);
            RefreshPresentation?.Invoke();
        }
    }
}
