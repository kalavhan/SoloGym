using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Fits native font size uniformly; never rescales glyph vertices.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class SystemTextFit : MonoBehaviour
    {
        public int maximum=32,minimum=16;
        public bool multiline;
        Text label;
        string previous;
        Vector2 previousSize;
        Font previousFont;
        int previousMaximum,previousMinimum;
        float previousLineSpacing;
        bool previousMultiline;
        void LateUpdate()
        {
            if(label==null)label=GetComponent<Text>();
            var rect=label.rectTransform.rect;
            if(previous==label.text&&previousSize==rect.size&&previousFont==label.font&&
                previousMaximum==maximum&&previousMinimum==minimum&&previousLineSpacing==label.lineSpacing&&previousMultiline==multiline)return;
            previous=label.text;previousSize=rect.size;previousFont=label.font;
            previousMaximum=maximum;previousMinimum=minimum;previousLineSpacing=label.lineSpacing;previousMultiline=multiline;
            label.resizeTextForBestFit=false;label.fontSize=maximum;
            bool wrap=multiline||(!string.IsNullOrEmpty(label.text)&&label.text.Contains("\n"));
            label.horizontalOverflow=wrap?HorizontalWrapMode.Wrap:HorizontalWrapMode.Overflow;
            if(string.IsNullOrEmpty(label.text)||label.font==null)return;
            float factor=Mathf.Min(1,rect.height/(maximum*1.08f));
            if(!wrap)
            {
                float width=label.preferredWidth;
                if(width>0)factor=Mathf.Min(factor,rect.width/width);
            }
            label.fontSize=Mathf.Clamp(Mathf.FloorToInt(maximum*factor),minimum,maximum);
            if(wrap)
                while(label.fontSize>minimum&&label.preferredHeight>rect.height)label.fontSize--;
        }
    }
}
