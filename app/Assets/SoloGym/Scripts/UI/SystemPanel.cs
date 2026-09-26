using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    public enum PanelStyle { Glass, Primary, Outline, Input, Selected, Slot, Track, Fill }

    /// <summary>Resolution-independent System chrome. No screen textures or sampled UVs.</summary>
    public sealed class SystemPanel : MaskableGraphic
    {
        public PanelStyle style;
        public SystemTheme theme;
        public bool ornaments;
        public float corner = -1;
        public float rimWidth = -1;
        public override Texture mainTexture => Texture2D.whiteTexture;
        Color Tint(Color c) => c * color;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear(); var t=theme!=null?theme:SystemUI.Theme; var r=rectTransform.rect;
            bool active=style==PanelStyle.Primary||style==PanelStyle.Selected||style==PanelStyle.Fill;
            float cut=corner>=0?corner:t.corner;
            if(style==PanelStyle.Track||style==PanelStyle.Fill)cut=7;
            if(style==PanelStyle.Glass)cut=0;
            Color fill=active?t.primary:t.glass;
            if(style==PanelStyle.Track)fill=new Color32(1,9,20,230);
            if(style==PanelStyle.Slot)fill=new Color32(1,12,23,210);
            if(style==PanelStyle.Fill)fill=t.accent;
            Color rim=active||style==PanelStyle.Input?t.accent:t.border;
            if(style==PanelStyle.Outline)rim=t.accent;
            if(style==PanelStyle.Glass)rim=t.border;
            var p=Points(r,cut);
            if(active)
                for(int k=8;k>=1;k--){var glow=r;glow.xMin-=k;glow.yMin-=k;glow.xMax+=k;glow.yMax+=k;Stroke(v,Points(glow,cut+k),new Color(rim.r,rim.g,rim.b,(.025f+.13f*(1-k/9f))*t.glow),2);}
            Fan(v,p,Tint(fill),Tint(active?Color.Lerp(fill,Color.black,.5f):fill));
            if(style!=PanelStyle.Fill)
            {
                float stroke=rimWidth>=0?rimWidth:active?2.5f:t.borderWidth;
                Stroke(v,p,Tint(rim),stroke);
                var inner=r;inner.xMin+=5;inner.xMax-=5;inner.yMin+=5;inner.yMax-=5;
                if(style==PanelStyle.Primary||style==PanelStyle.Glass)
                    Stroke(v,Points(inner,Mathf.Max(0,cut-2)),Tint(new Color(rim.r,rim.g,rim.b,active?.55f:.18f)),1);
            }
            if(style==PanelStyle.Primary)
            {
                var inner=r;inner.xMin+=8;inner.xMax-=8;inner.yMin+=7;inner.yMax-=7;
                Stroke(v,Points(inner,cut-5),Tint(new Color(.7f,1,1,1)),2.8f);
                // Fine crystalline facets are deterministic mesh decoration, independent of artwork.
                for(int i=0;i<22;i++)
                {
                    float x=r.xMin+15+(r.width-30)*i/22f;
                    float y=r.yMin+r.height*(.2f+.6f*((i*7)%11)/11f);
                    Line(v,new Vector2(x,y),new Vector2(x+16,y+12),Tint(new Color(.2f,.75f,1,.15f)),.8f);
                    Line(v,new Vector2(x+16,y+12),new Vector2(x+32,y-9),Tint(new Color(.2f,.75f,1,.1f)),.8f);
                }
            }
            if(ornaments)
            {
                float a=32;
                foreach(var pos in new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax)})
                {
                    float dx=pos.x==r.xMin?1:-1,dy=pos.y==r.yMin?1:-1;
                    Line(v,pos+new Vector2(dx*3,dy*a),pos+new Vector2(dx*a,dy*3),Tint(rim),2);
                    Line(v,pos+new Vector2(dx*8,dy*a),pos+new Vector2(dx*a,dy*8),Tint(t.accent),1);
                }
                Vector2 c=new Vector2(r.center.x,r.yMax);
                Line(v,c+new Vector2(-15,0),c+new Vector2(0,14),Tint(t.accent),2);
                Line(v,c+new Vector2(0,14),c+new Vector2(15,0),Tint(t.accent),2);
            }
        }
        static Vector2[] Points(Rect r,float c)
        {
            c=Mathf.Clamp(c,0,Mathf.Min(r.width,r.height)*.45f);
            return new[]{new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin,r.yMin+c)};
        }
        static void Fan(VertexHelper v,Vector2[] p,Color top,Color bottom)
        {
            int start=v.currentVertCount;float min=p[0].y,max=p[4].y;Vector2 center=(p[0]+p[4])*.5f;
            v.AddVert(center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            foreach(var q in p)v.AddVert(q,Color.Lerp(bottom,top,Mathf.InverseLerp(min,max,q.y)),Vector2.zero);
            for(int i=0;i<p.Length;i++)v.AddTriangle(start,start+1+i,start+1+(i+1)%p.Length);
        }
        static void Stroke(VertexHelper v,Vector2[] p,Color color,float width)
        {for(int i=0;i<p.Length;i++)Line(v,p[i],p[(i+1)%p.Length],color,width);}
        public static void Line(VertexHelper v,Vector2 a,Vector2 b,Color color,float width)
        {
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=v.currentVertCount;
            v.AddVert(a-n,color,Vector2.zero);v.AddVert(a+n,color,Vector2.zero);v.AddVert(b+n,color,Vector2.zero);v.AddVert(b-n,color,Vector2.zero);
            v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);
        }
    }
}
