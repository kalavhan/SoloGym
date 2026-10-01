using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using static SoloGym.UI.PixelJournalUI;
namespace SoloGym.UI
{
    public sealed class PixelFastingClock : MonoBehaviour
    {
        [Serializable] sealed class FrameData { public string file; public int durationMs; }
        [Serializable] sealed class Atlas { public FrameData[] frames; }
        Image overlay; Material material; Sprite[] frames; float[] ends; float duration;
        public bool ReducedMotion { get; private set; }
        public int FrameIndex { get; private set; }
        public int Band { get; private set; }
        public bool Running { get; private set; }
        public void Initialize()
        {
            Art(transform,new Rect(0,0,560,560),"Rooms/FastingR1/clock");
            overlay=Art(transform,new Rect(0,0,560,560),"Rooms/FastingR1/clock");
            var atlas=JsonUtility.FromJson<Atlas>(Resources.Load<TextAsset>("Rooms/FastingR1/motion-atlas").text);
            frames=new Sprite[atlas.frames.Length];ends=new float[frames.Length];
            for(int i=0;i<frames.Length;i++)
            {frames[i]=Resources.Load<Sprite>("Rooms/FastingR1/Motion/"+Path.GetFileNameWithoutExtension(atlas.frames[i].file));duration+=atlas.frames[i].durationMs/1000f;ends[i]=duration;}
            material=new Material(Resources.Load<Shader>("Shaders/FastingRunes"));overlay.material=material;
            Bind(false,true,0);
        }
        public void Bind(bool running,bool reducedMotion,int band)
        {
            Running=running;ReducedMotion=reducedMotion;Band=band;overlay.enabled=running&&!reducedMotion;
            // Equal luminosity; subtle hue differences never intensify with duration.
            var hues=new[]{new Color(1,.92f,.75f),new Color(.85f,1,.87f),new Color(.8f,1,1),new Color(.87f,.93f,1),new Color(.94f,.94f,.94f)};
            var tint=hues[Mathf.Clamp(band,0,4)];tint.a=.32f;overlay.color=tint;
        }
        void Update()
        {
            if(overlay==null||!overlay.enabled||duration<=0)return;
            float phase=(Time.unscaledTime+Band*.65f)%duration;int i=0;while(i<ends.Length-1&&phase>=ends[i])i++;
            FrameIndex=i;overlay.sprite=frames[i];
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
