using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    public static class SystemUI
    {
        static SystemTheme theme;
        public static SystemTheme Theme
        {
            get
            {
                if(theme==null)
                {
                    theme=Resources.Load<SystemTheme>("UI/DefaultTheme");
                    if(theme==null)theme=ScriptableObject.CreateInstance<SystemTheme>();
                    if(theme.heading==null)theme.heading=Resources.Load<Font>("Fonts/LiberationSerif-Regular");
                    if(theme.headingBold==null)theme.headingBold=Resources.Load<Font>("Fonts/LiberationSerif-Bold");
                    if(theme.body==null)theme.body=Resources.Load<Font>("Fonts/NotoSans-Regular");
                }
                return theme;
            }
        }
        public static RectTransform Node(string name,Transform parent,Rect r)
        {
            var n=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();n.SetParent(parent,false);Place(n,r);return n;
        }
        public static void Place(RectTransform n,Rect r)
        {n.anchorMin=n.anchorMax=n.pivot=new Vector2(0,1);n.anchoredPosition=new Vector2(r.x,-r.y);n.sizeDelta=r.size;}
        public static Text Text(Transform parent,Rect rect,string value,int size=29,Font font=null,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var text=Node("Label",parent,rect).gameObject.AddComponent<Text>();text.text=value;text.font=font??Theme.heading;
            text.fontSize=size;text.color=color??Theme.text;text.alignment=align;text.raycastTarget=false;text.supportRichText=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }
        public static Text Caption(Transform parent,Rect rect,string value,int size,Font font=null,Color? color=null)
        {
            // Uniform native font fitting preserves glyph proportions; no vertex stretching.
            var t=Text(parent,rect,value,size,font,color,TextAnchor.MiddleCenter);
            var fit=t.gameObject.AddComponent<SystemTextFit>();fit.maximum=size;fit.minimum=16;fit.multiline=(value!=null&&value.Contains("\n"))||rect.height>size*2.2f;return t;
        }
        public static SystemPanel Panel(Transform parent,Rect r,PanelStyle style=PanelStyle.Glass,bool ornaments=false,string name="System panel")
        {var p=Node(name,parent,r).gameObject.AddComponent<SystemPanel>();p.theme=Theme;p.style=style;p.ornaments=ornaments;p.raycastTarget=false;return p;}
        public static Button Button(Transform parent,Rect r,string label,Action action,PanelStyle style=PanelStyle.Outline)
        {
            var p=Panel(parent,r,style);p.raycastTarget=true;var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;
            var colors=b.colors;colors.highlightedColor=new Color(.85f,1,1);colors.pressedColor=new Color(.45f,.75f,1);colors.disabledColor=new Color(.4f,.5f,.6f,.65f);b.colors=colors;
            b.onClick.AddListener(()=>action?.Invoke());
            var caption=Caption(p.transform,new Rect(Theme.controlPadding,0,r.width-2*Theme.controlPadding,r.height),label,Theme.bodySize,Theme.body);
            caption.GetComponent<SystemTextFit>().multiline=false;return b;
        }
        public static Button ChoiceButton(Transform parent,Rect r,string label,bool selected,Action action,PanelStyle? unselectedStyle=null)
        {
            var style=selected?PanelStyle.Selected:unselectedStyle??PanelStyle.Outline;
            var p=Panel(parent,r,style);p.raycastTarget=true;
            if(!selected)p.rimWidth=2.5f;
            var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;
            var colors=b.colors;colors.highlightedColor=new Color(.85f,1,1);colors.pressedColor=new Color(.45f,.75f,1);colors.disabledColor=new Color(.4f,.5f,.6f,.65f);b.colors=colors;
            b.onClick.AddListener(()=>action?.Invoke());
            const float glyphX=20,glyphW=36,labelX=56;
            Text(p.transform,new Rect(glyphX,0,glyphW,r.height),selected?"◆":"◇",Theme.bodySize,Theme.body,Theme.text,TextAnchor.MiddleCenter);
            var caption=Text(p.transform,new Rect(labelX,0,r.width-labelX-Theme.controlPadding,r.height),label,Theme.bodySize,Theme.body,Theme.text,TextAnchor.MiddleLeft);
            var fit=caption.gameObject.AddComponent<SystemTextFit>();fit.maximum=Theme.bodySize;fit.minimum=16;fit.multiline=false;
            return b;
        }
        public static Button Hit(Transform parent,Rect r,Action action,string name="Control")
        {
            var n=Node(name,parent,r);var image=n.gameObject.AddComponent<Image>();image.color=Color.clear;
            var b=n.gameObject.AddComponent<Button>();b.targetGraphic=image;
            b.onClick.AddListener(()=>action?.Invoke());return b;
        }
        public static RawImage Art(Transform parent,Rect r,string resource,bool preserveAspect=false)
        {
            var texture=Resources.Load<Texture2D>(resource);var n=Node(resource,parent,r);
            var image=n.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            if(preserveAspect&&texture!=null)
            {float s=Mathf.Min(r.width/texture.width,r.height/texture.height);Place(n,new Rect(r.x+(r.width-texture.width*s)/2,r.y+(r.height-texture.height*s)/2,texture.width*s,texture.height*s));}
            return image;
        }
        public static void Wordmark(Transform parent,Rect r)
        {
            var mark=Art(parent,r,"Art/Wordmark-v1");
            // Tight alpha content rectangle; metadata only, the original PNG is retained unchanged.
            mark.uvRect=new Rect(.015f,.16f,.97f,.68f);

        }
        public static void PortalPage(Transform parent,float panelY,float panelHeight)
        {
            // Panel Y/height: prefer PortalWindowFrame solvers for content-driven windows (WIN-012+).
            Art(parent,new Rect(0,0,853,1844),"Art/PortalBackground-v1");
            Wordmark(parent,new Rect(174,149,510,112));
            Icon(parent,new Rect(694,48,32,32),"globe",Theme.text);
            Panel(parent,new Rect(62,panelY,729,panelHeight),PanelStyle.Glass,true);
        }
        public static void Divider(Transform parent,float y,float x=245,float width=365)
        {
            var left=Node("Divider",parent,new Rect(x,y,width/2-20,1.5f)).gameObject.AddComponent<Image>();left.color=Theme.border;left.raycastTarget=false;
            var right=Node("Divider",parent,new Rect(x+width/2+20,y,width/2-20,1.5f)).gameObject.AddComponent<Image>();right.color=Theme.border;right.raycastTarget=false;
            Icon(parent,new Rect(x+width/2-12,y-13,24,26),"sigil",Theme.accent);
        }
        [Serializable] sealed class IconEntry { public string id; public float[] rect; }
        [Serializable] sealed class IconMap { public IconEntry[] entries; }
        static IconMap icons;
        public static Graphic Icon(Transform parent,Rect r,string kind,Color? color=null)
        {
            if(icons==null)icons=JsonUtility.FromJson<IconMap>(Resources.Load<TextAsset>("Art/IconAtlas").text);
            foreach(var entry in icons.entries)
                if(entry.id==kind)
                {
                    var texture=Resources.Load<Texture2D>("Art/IllustratedIcons-v1");var b=entry.rect;
                    float scale=Mathf.Min(r.width/b[2],r.height/b[3]);
                    var image=Art(parent,new Rect(r.x+(r.width-b[2]*scale)/2,r.y+(r.height-b[3]*scale)/2,b[2]*scale,b[3]*scale),"Art/IllustratedIcons-v1");
                    image.uvRect=new Rect(b[0]/texture.width,1-(b[1]+b[3])/texture.height,b[2]/texture.width,b[3]/texture.height);return image;
                }
            var i=Node("Icon / "+kind,parent,r).gameObject.AddComponent<SystemIcon>();i.kind=kind;i.color=color??Theme.accent;i.raycastTarget=false;return i;
        }
        public static void InputFrame(Transform parent,Rect r,float unitDivider=0)
        {
            Panel(parent,r,PanelStyle.Input);
            if(unitDivider>0){var line=Node("Unit divider",parent,new Rect(r.x+unitDivider,r.y+16,1.5f,r.height-32)).gameObject.AddComponent<Image>();line.color=Theme.border*.6f;line.raycastTarget=false;}
        }
        public static SystemScrollAffordance AttachScrollAffordance(ScrollRect scroll,Rect viewportPageRect,float rowStride=74f,Transform chevronParent=null)
        {
            var host=Node("Scroll affordance",scroll.viewport.parent,viewportPageRect);
            var affordance=host.gameObject.AddComponent<SystemScrollAffordance>();
            affordance.Initialize(scroll,viewportPageRect,rowStride,chevronParent??scroll.viewport.parent);
            return affordance;
        }
    }
}
