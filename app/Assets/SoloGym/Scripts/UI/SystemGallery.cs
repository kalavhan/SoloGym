using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SoloGym
{
    /// <summary>Interactive engineering view for component reuse, resizing and localization.</summary>
    public sealed class SystemGallery : MonoBehaviour
    {
        RectTransform root,content;
        bool spanish,wide,alternate;
        float progress=.62f;
        string draft="Kai";
        SystemTheme previewTheme;
        public void Initialize()
        {
            previewTheme=Instantiate(SystemUI.Theme);
            var c=new GameObject("Component gallery",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));c.transform.SetParent(transform,false);
            c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            root=SystemUI.Node("Gallery",c.transform,new Rect(0,0,853,1844));
            if(FindFirstObjectByType<EventSystem>()==null)new GameObject("Input",typeof(EventSystem),typeof(StandaloneInputModule));
            Render();if(Arg("-sologym-capture")!=null)StartCoroutine(Capture());
        }
        void Update(){if(root!=null)SystemViewport.Fit(root,853,1844);}
        void Render()
        {
            if(content!=null){content.gameObject.SetActive(false);Destroy(content.gameObject);}
            content=SystemUI.Node("Components",root,new Rect(0,0,853,1844));
            SystemUI.Art(content,new Rect(0,0,853,1844),"Art/PortalBackground-v1");
            SystemUI.Panel(content,new Rect(48,130,757,1550),PanelStyle.Glass,true);
            SystemUI.Caption(content,new Rect(78,175,697,75),spanish?"COMPONENTES DEL SISTEMA":"SYSTEM COMPONENTS",47,SystemUI.Theme.headingBold);
            SystemUI.Caption(content,new Rect(85,260,683,80),spanish?"Vista técnica · paneles, texto y controles en código":"Engineering preview · panels, text and controls in code",26,SystemUI.Theme.body);
            SystemUI.Button(content,new Rect(83,370,215,76),spanish?"English":"Español",()=>{spanish=!spanish;Render();});
            SystemUI.Button(content,new Rect(318,370,215,76),spanish?"Cambiar tema":"Change theme",()=>{alternate=!alternate;Render();});
            SystemUI.Button(content,new Rect(553,370,215,76),spanish?"Redimensionar":"Resize",()=>{wide=!wide;Render();});
            float width=wide?650:480;
            var panel=SystemUI.Panel(content,new Rect(101,500,width,250),PanelStyle.Glass,true);
            SystemUI.Caption(panel.transform,new Rect(20,22,width-40,65),spanish?"UN PANEL REUTILIZABLE":"ONE REUSABLE PANEL",32,SystemUI.Theme.headingBold);
            SystemUI.Caption(panel.transform,new Rect(28,95,width-56,100),spanish?"Cambia el tamaño o el idioma. El marco y el texto se adaptan sin nuevas imágenes.":"Resize or switch language. The frame and text adapt without new images.",26,SystemUI.Theme.body);
            SystemUI.Button(content,new Rect(101,795,width,90),spanish?"ACCIÓN PRINCIPAL":"PRIMARY ACTION",()=>{progress=progress>.9f?.2f:progress+.1f;Render();},PanelStyle.Primary);
            SystemUI.Text(content,new Rect(103,930,550,45),spanish?"Nombre editable":"Editable name",30);
            var field=SystemUI.Panel(content,new Rect(102,985,650,82),PanelStyle.Input);field.raycastTarget=true;
            var input=field.gameObject.AddComponent<InputField>();input.targetGraphic=field;
            input.textComponent=SystemUI.Text(field.transform,new Rect(22,0,606,82),"",32);input.textComponent.horizontalOverflow=HorizontalWrapMode.Overflow;
            input.SetTextWithoutNotify(draft);input.onValueChanged.AddListener(v=>draft=v);
            SystemUI.Panel(content,new Rect(102,1130,650,25),PanelStyle.Track);
            SystemUI.Panel(content,new Rect(106,1134,642*progress,17),PanelStyle.Fill);
            SystemUI.Caption(content,new Rect(102,1175,650,48),Mathf.RoundToInt(progress*100)+"% · "+(spanish?"Valor real":"Live value"),28);
            string[] icons={"sigil","head","torso","hands","shield","train","gym"};
            for(int i=0;i<icons.Length;i++)SystemUI.Icon(content,new Rect(111+i*90,1310,60,70),icons[i]);
            SystemUI.Caption(content,new Rect(100,1470,650,100),spanish?"Fondo independiente · tipografía nativa · tema compartido":"Independent background · native typography · shared theme",29,SystemUI.Theme.body);
            previewTheme.accent=alternate?new Color32(211,152,255,255):SystemUI.Theme.accent;
            previewTheme.primary=alternate?new Color32(91,42,158,255):SystemUI.Theme.primary;
            foreach(var p in content.GetComponentsInChildren<SystemPanel>()){p.theme=previewTheme;p.SetVerticesDirty();}
        }
        IEnumerator Capture()
        {
            for(int i=0;i<8;i++)yield return null;
            if(Arg("-sologym-gallery-variant")=="alternate"){spanish=wide=alternate=true;Render();yield return null;}
            Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();string path=Arg("-sologym-capture");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());Destroy(image);Application.Quit();
        }
        static string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:null;}
        void OnDestroy(){if(previewTheme!=null)Destroy(previewTheme);}
    }
}
