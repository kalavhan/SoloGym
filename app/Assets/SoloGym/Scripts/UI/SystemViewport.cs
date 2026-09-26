using UnityEngine;

namespace SoloGym
{
    public static class SystemViewport
    {
        public static float Scale(float width,float height)=>Mathf.Min(Screen.safeArea.width/width,Screen.safeArea.height/height);
        public static void Fit(RectTransform root,float width,float height,float keyboardLift=0)
        {
            Rect safe=Screen.safeArea;float scale=Scale(width,height);
            root.localScale=new Vector3(scale,scale,1);
            root.anchoredPosition=new Vector2(safe.x+(safe.width-width*scale)/2,-(Screen.height-safe.yMax+(safe.height-height*scale)/2)+keyboardLift);
        }
    }
}
