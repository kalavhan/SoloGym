using UnityEngine;

namespace SoloGym.UI
{
    /// <summary>Touch phones do not draw keyboard focus rings; pressed and selected states stay visible.</summary>
    public static class PixelTouch
    {
        public static bool HideFocus => Application.isMobilePlatform;
    }
}
