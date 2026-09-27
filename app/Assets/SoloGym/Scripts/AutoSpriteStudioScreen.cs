using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Static MVP customization in the existing game UI. Continue saves; Back discards.</summary>
    public sealed class AutoSpriteStudioScreen : MonoBehaviour
    {
        RectTransform root;
        Text title,note,hint,continueLabel,languageLabel,backLabel,footer;
        readonly Dictionary<string,Button> choices=new Dictionary<string,Button>();
        readonly Dictionary<string,Text> captions=new Dictionary<string,Text>();
        Action back,next;
        string capture;
        bool es;
        Vector2 size;
        Rect safe;
        ModularAppearance draft;
        public AutoSpriteAvatarView Avatar { get; private set; }
        public Button ContinueButton { get; private set; }
        public Button BackButton { get; private set; }
        public ModularAppearance Draft => draft.Clone();
        public Button Choice(string id) => choices[id];

        public void Initialize(string language,Action onBack,Action onContinue,string capturePath=null)
        {
            AutoSpriteSession.Initialize();
            es=language=="es" || (language!="en" && Application.systemLanguage==SystemLanguage.Spanish);
            AutoSpriteSession.Language=es?"es":"en";
            draft=AutoSpriteSession.Appearance;
            back=onBack;next=onContinue;capture=capturePath;
            var node=new GameObject("Character studio canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            node.transform.SetParent(transform,false);
            var canvas=node.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;canvas.pixelPerfect=true;
            root=SystemUI.Node("Studio 853x1844",node.transform,new Rect(0,0,853,1844));
            if(FindFirstObjectByType<Camera>()==null)
            {
                var camera=new GameObject("Studio camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color32(2,6,16,255);camera.cullingMask=0;
            }
            if(FindFirstObjectByType<EventSystem>()==null) new GameObject("Input",typeof(EventSystem),typeof(StandaloneInputModule));
            SystemUI.Art(root,new Rect(0,0,853,1844),"Art/PortalBackground-v1");
            SystemUI.Wordmark(root,new Rect(174,149,510,112));
            SystemUI.Icon(root,new Rect(43,47,28,34),"back");
            BackButton=SystemUI.Hit(root,new Rect(30,20,180,100),()=>back?.Invoke(),"Back without saving");
            backLabel=SystemUI.Caption(root,new Rect(85,43,120,44),"",29);
            languageLabel=SystemUI.Caption(root,new Rect(734,43,72,44),"",26,SystemUI.Theme.body);
            SystemUI.Hit(root,new Rect(690,20,130,100),ToggleLanguage);
            note=SystemUI.Caption(root,new Rect(80,275,693,28),"",20,SystemUI.Theme.body,SystemUI.Theme.accent);
            title=SystemUI.Caption(root,new Rect(90,313,673,64),"",58,SystemUI.Theme.headingBold);
            Avatar=gameObject.AddComponent<AutoSpriteAvatarView>();Avatar.Mount(root,new Rect(190,382,473,685));
            hint=SystemUI.Caption(root,new Rect(80,1066,693,30),"",19,SystemUI.Theme.body,SystemUI.Theme.accent);
            SystemUI.Panel(root,new Rect(62,1110,729,410),PanelStyle.Glass,true);
            Row("presentation",1124);AddChoice("male",200,1150,208,()=>SetGender("male"));AddChoice("female",425,1150,208,()=>SetGender("female"));
            Row("build",1212);
            for(int i=0;i<AutoSpriteSession.Builds.Length;i++)
            { string id=AutoSpriteSession.Builds[i];AddChoice(id,83+i*173,1239,166,()=>{draft.bodyId=Gender+"-"+id;Refresh();}); }
            Row("hair",1302);string[] hair={"none","close-crop","short-sweep"};
            for(int i=0;i<hair.Length;i++)
            { string id=hair[i];AddChoice(id,92+i*225,1329,217,()=>{draft.hairId=id;Refresh();}); }
            Row("outfit",1394);AddChoice("torso",131,1423,282,()=>{draft.torso=!draft.torso;Refresh();});
            AddChoice("legs",440,1423,282,()=>{draft.legs=!draft.legs;Refresh();});
            ContinueButton=SystemUI.Button(root,PortalFrameLayout.PrimaryRect,"",SaveAndContinue,PanelStyle.Primary);
            continueLabel=ContinueButton.GetComponentInChildren<Text>();
            footer=SystemUI.Caption(root,new Rect(95,1656,663,56),"",19,SystemUI.Theme.body,SystemUI.Theme.muted);
            SystemUI.Icon(root,new Rect(421,1740,10,25),"sigil");
            Refresh();Fit();
            if((AutoSpriteSession.HasArgument("-sologym-modular-smoke") || AutoSpriteSession.HasArgument("-sologym-autosprite-smoke")
                || (AutoSpriteSession.HasArgument("-sologym-smoke") && AutoSpriteSession.Argument("-sologym-window")=="character")) && !AutoSpriteSession.SmokeStarted)
            {
                AutoSpriteSession.SmokeStarted=true;
                new GameObject("Modular character checks").AddComponent<AutoSpriteReviewSmoke>().Run(capturePath);
            }
            else if(capture!=null && !AutoSpriteSession.CaptureTaken) StartCoroutine(Capture());
        }
        string Gender => draft.bodyId.StartsWith("female-")?"female":"male";
        string L(string en,string spanish) => es?spanish:en;
        void Row(string id,float y) => captions[id]=SystemUI.Caption(root,new Rect(90,y,673,24),"",18,SystemUI.Theme.body,SystemUI.Theme.accent);
        void AddChoice(string id,float x,float y,float width,Action action)
        { choices[id]=SystemUI.Button(root,new Rect(x,y,width,54),"",action);choices[id].gameObject.name="Appearance / "+id; }
        void SetGender(string gender)
        { string build=draft.bodyId.Substring(draft.bodyId.IndexOf('-')+1);draft.bodyId=gender+"-"+build;Refresh(); }
        void ToggleLanguage() { es=!es;AutoSpriteSession.Language=es?"es":"en";PlayerPrefs.SetString("SoloGym.Home.Language.v1",AutoSpriteSession.Language);PlayerPrefs.Save();Refresh(); }
        void SaveAndContinue() { if(!Avatar.IsLoaded)return;AutoSpriteSession.Save(draft);next?.Invoke(); }
        void Label(string id,string en,string spanish,bool selected)
        {
            choices[id].GetComponentInChildren<Text>().text=L(en,spanish);
            choices[id].GetComponent<SystemPanel>().style=selected?PanelStyle.Selected:PanelStyle.Outline;
            choices[id].GetComponent<SystemPanel>().SetAllDirty();
        }
        void Refresh()
        {
            Avatar.SetAppearance(draft);
            languageLabel.text=es?"ES":"EN";backLabel.text=L("Back","Volver");
            title.text=L("Your character","Tu personaje");note.text=L("MAKE IT YOURS","DALE TU ESTILO");
            hint.text=Avatar.IsLoaded?L("Your appearance does not change your training plan","Tu apariencia no cambia tu plan de entrenamiento"):Avatar.LastError;
            captions["presentation"].text=L("CHARACTER","PERSONAJE");captions["build"].text=L("BUILD","COMPLEXIÓN");
            captions["hair"].text=L("HAIRSTYLE","PEINADO");captions["outfit"].text=L("BASE OUTFIT","ATUENDO BASE");
            Label("male","Man","Hombre",Gender=="male");Label("female","Woman","Mujer",Gender=="female");
            Label("slim","Slim","Delgada",draft.bodyId.EndsWith("-slim"));Label("medium","Medium","Media",draft.bodyId.EndsWith("-medium"));
            Label("overweight","Overweight","Robusta",draft.bodyId.EndsWith("-overweight"));Label("muscular","Muscular","Musculosa",draft.bodyId.EndsWith("-muscular"));
            Label("none","No hair","Sin cabello",draft.hairId=="none");Label("close-crop","Short crop","Corto",draft.hairId=="close-crop");
            Label("short-sweep","Side sweep","De lado",draft.hairId=="short-sweep");
            Label("torso",draft.torso?"Top · on":"Top · off",draft.torso?"Camiseta · sí":"Camiseta · no",draft.torso);
            Label("legs",draft.legs?"Shorts · on":"Shorts · off",draft.legs?"Shorts · sí":"Shorts · no",draft.legs);
            continueLabel.text=L("SAVE & CONTINUE","GUARDAR Y CONTINUAR");ContinueButton.interactable=Avatar.IsLoaded;
            footer.text=L("Saved on this device when you continue. Edit later from Home.","Se guarda al continuar. Puedes editarla desde Inicio.");
        }
        void Update()
        {
            if(size.x!=Screen.width || size.y!=Screen.height || safe!=Screen.safeArea)Fit();
            if(Input.GetKeyDown(KeyCode.Escape))back?.Invoke();
        }
        void Fit() { SystemViewport.Fit(root,853,1844);size=new Vector2(Screen.width,Screen.height);safe=Screen.safeArea; }
        IEnumerator Capture()
        {
            for(int i=0;i<8;i++)yield return null;
            yield return new WaitForEndOfFrame();AutoSpriteSession.CaptureTaken=true;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(capture,image.EncodeToPNG());Destroy(image);
            Debug.Log("SOLOGYM_CAPTURE "+capture);
            if(AutoSpriteSession.HasArgument("-sologym-quit-after-capture"))Application.Quit();
        }
    }
}
