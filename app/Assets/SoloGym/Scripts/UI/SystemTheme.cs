using UnityEngine;

namespace SoloGym
{
    [CreateAssetMenu(menuName = "SoloGym/System theme")]
    public sealed class SystemTheme : ScriptableObject
    {
        public Color text = new Color32(227,233,246,255);
        public Color muted = new Color32(165,185,212,255);
        public Color accent = new Color32(83,224,255,255);
        public Color border = new Color32(147,197,240,255);
        public Color glass = new Color32(2,17,32,243);
        public Color primary = new Color32(0,102,174,255);
        public Color gold = new Color32(255,205,82,255);
        public Color warning = new Color32(255,207,161,255);
        public float corner = 18, borderWidth = 1.5f, glow = 1;
        public Font heading, headingBold, body;
        public int bodySize = 29;
        public float controlPadding = 20;
    }
}
