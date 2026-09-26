using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Optional measurements and readiness with live fields on the approved portal artwork.</summary>
    public sealed class PrivateProfileScreen : MonoBehaviour
    {
        const float W=853,H=1844;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        RectTransform root,page,modal;
        Font serif,bold,body;
        PrivateProfileController controller;
        Action back,leave;
        string capture;
        bool rendering;
        Text status;
        InputField heightInput,feetInput,inchesInput,weightInput,lastFocused;
        readonly Dictionary<string,Button> controls=new Dictionary<string,Button>();
        Vector2 lastSize;
        Rect lastSafe;
        float lastKeyboardHeight;
        bool lastKeyboard;

        public void Initialize(string language,bool review,Action onBack,Action onExit,string capturePath=null)
        {
            back=onBack;leave=onExit;capture=capturePath;
            serif=SystemUI.Theme.heading;bold=SystemUI.Theme.headingBold;body=SystemUI.Theme.body;
            controller=new PrivateProfileController(review);controller.SetLanguage(language);
            var node=new GameObject("Private profile canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));node.transform.SetParent(transform,false);
            var canvas=node.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;canvas.pixelPerfect=true;
            root=RectNode("Profile reference 853x1844",node.transform,new Rect(0,0,W,H));
            controller.Changed+=Render;
            controller.ExitRequested+=Exit;
            // The controller exposes only a prefixed review destination; it cannot authorize a live profile.
            controller.CheckpointRequested+=id=>Notice(L("Next step","Siguiente paso"),L("Experience and goals is the next window. Nothing was saved to your account.","Experiencia y objetivos es la siguiente ventana. No se guardaron datos en tu cuenta."));
            if(review&&capture!=null)
            {
                string view=Argument("-sologym-profile-view")??"measurements";
                if(view!="notice")controller.ContinueNotice();
                if(view=="imperial")controller.SetUnits("imperial");
                if(view=="readiness"||view=="paused")controller.ContinueMeasurements();
                if(view=="paused"){controller.SelectReadiness("pain");controller.ContinueReadiness();}
            }
            Render(controller.Model);Fit();if(capture!=null)StartCoroutine(Capture());
        }
        public void Resume(string language)
        {
            gameObject.SetActive(true);
            controller.SetLanguage(language);
            Fit();
        }


        void Render(PrivateProfileViewModel model)
        {
            // Keep the focused native input and current pointer targets alive on each keystroke.
            if(!rendering&&page!=null&&model.Step==ProfileStep.Measurements&&HasFocus())
            { if(status!=null)status.text=model.Error;return; }
            rendering=true;CloseModal();
            if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}
            heightInput=feetInput=inchesInput=weightInput=null;controls.Clear();
            page=RectNode("Private profile page",root,new Rect(0,0,W,H));
            SystemUI.PortalPage(page,718,965);
            SystemUI.Divider(page,807);
            SystemUI.Icon(page,new Rect(43,47,28,34),"back",Silver);
            SystemUI.Icon(page,new Rect(421,1728,10,25),"sigil",Silver);
            Live("Back",new Rect(92,53,L(61,83),25),L("Back","Volver"),31);
            Live("Language",new Rect(765,56,29,20),model.Language.ToUpperInvariant(),27);
            Hit("back",new Rect(30,20,180,100),Back);
            Hit("language",new Rect(680,20,130,100),Language);
            Live("Title",new Rect(L(128,139),836,L(596,575),53),L("PRIVATE PROFILE","PERFIL PRIVADO"),72,Silver,bold);
            if(model.Step==ProfileStep.Measurements)Measurements();else StepPage();
            Live("Privacy",new Rect(L(294,275),1735,L(89,116),24),L("Privacy","Privacidad"),29);
            Live("Terms",new Rect(L(491,472),1735,L(68,101),24),L("Terms","Términos"),29);
            Hit("privacy",new Rect(250,1706,179,85),()=>Document("privacy"));
            Hit("terms",new Rect(450,1706,172,85),()=>Document("terms"));
            if(model.ReviewMode)TextAt(page,new Rect(80,280,693,35),L("PREVIEW · FICTIONAL DATA · NOT SAVED","VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"),20,body,Cyan,TextAnchor.MiddleCenter);
            rendering=false;
        }
        void Measurements()
        {
            var m=controller.Model;bool metric=m.UnitSystem=="metric";
            Live("Subtitle",new Rect(L(174,233),918,L(508,386),31),L("Your measurements, your pace","Tus medidas, a tu ritmo"),38);
            Live("Units",new Rect(111,991,L(68,116),26),L("Units","Unidades"),32);
            SystemUI.Panel(page,new Rect(104,1026,325,77),metric?PanelStyle.Selected:PanelStyle.Outline);
            SystemUI.Panel(page,new Rect(429,1026,323,77),metric?PanelStyle.Outline:PanelStyle.Selected);
            SystemUI.Icon(page,new Rect(151,1046,38,38),"sigil",metric?Cyan:Silver);
            SystemUI.Icon(page,new Rect(500,1046,38,38),"sigil",metric?Silver:Cyan);
            SystemUI.InputFrame(page,new Rect(102,1171,651,80),511);
            SystemUI.InputFrame(page,new Rect(102,1319,651,80),511);
            SystemUI.Icon(page,new Rect(181,1449,33,37),"lock",Silver);
            Live("Metric",new Rect(225,1053,120,29),"cm / kg",37,metric?Silver:new Color32(179,193,221,255),metric?bold:serif);
            Live("Imperial",new Rect(582,1053,83,29),"ft / lb",37,metric?new Color32(179,193,221,255):Silver,metric?serif:bold);
            Hit("metric",new Rect(104,1023,325,82),()=>ChangeUnits("metric"));
            Hit("imperial",new Rect(429,1023,323,82),()=>ChangeUnits("imperial"));
            Live("Height label",new Rect(106,1136,500,26),L("Height (optional)","Altura (opcional)"),32);
            if(metric)
            {
                heightInput=Field("height",new Rect(111,1176,492,68),m.HeightText,controller.SetHeight);
                Live("Height unit",new Rect(666,1202,44,22),"cm",32);
            }
            else
            {
                feetInput=Field("feet",new Rect(111,1176,191,68),m.HeightFeetText,controller.SetHeightFeet,true);
                TextAt(page,new Rect(305,1176,63,68),"ft",31,serif,Silver);
                inchesInput=Field("inches",new Rect(376,1176,216,68),m.HeightInchesText,controller.SetHeightInches);
                Live("Height unit",new Rect(672,1202,32,22),"in",32);
            }
            Live("Weight label",new Rect(106,1283,530,26),L("Bodyweight (optional)","Peso corporal (opcional)"),32);
            weightInput=Field("weight",new Rect(111,1324,492,68),m.WeightText,controller.SetWeight);
            Live("Weight unit",new Rect(672,1350,35,27),metric?"kg":"lb",33);
            Live("Private cue",new Rect(L(231,231),1437,L(459,495),L(76,76)),L("You can continue without adding\nmeasurements.\nNot shown on your public profile.","Puedes continuar sin añadir tus medidas.\nNo se muestran en tu perfil público."),29);
            Primary(()=>{KeyboardOff();controller.ContinueMeasurements();},L("CONTINUE","CONTINUAR"));
            status=TextAt(page,new Rect(105,1652,645,27),m.Error,20,body,new Color32(255,207,161,255),TextAnchor.MiddleCenter);
            status.horizontalOverflow=HorizontalWrapMode.Wrap;
        }
        void StepPage()
        {
            var m=controller.Model;
            switch(m.Step)
            {
                case ProfileStep.Notice:
                    Heading(L("Before your measurements","Antes de tus medidas"));
                    Paragraph(1010,L("Height and bodyweight are optional. They stay out of your public profile and rankings. You can leave both fields empty.","La altura y el peso son opcionales. No se muestran en tu perfil público ni en el ranking. Puedes dejar ambos campos vacíos."));
                    Paragraph(1210,m.ReviewMode?L("This preview uses fictional values. It does not save measurements or collect health-data consent.","Esta vista previa usa valores ficticios. No guarda medidas ni recoge consentimiento para datos de salud."):L("Profile setup is not available yet. The applicable data-use policy and secure storage must be ready before measurements can be entered.","El perfil aún no está disponible. La política aplicable de uso de datos y su almacenamiento deben estar listos antes de introducir medidas."));
                    Primary(()=>controller.ContinueNotice(),m.ReviewMode?L("CONTINUE","CONTINUAR"):L("NOT AVAILABLE YET","AÚN NO DISPONIBLE"),m.ReviewMode);
                    Secondary("leave",new Rect(245,1645,363,37),L("Not now","Ahora no"),controller.Decline);
                    break;
                case ProfileStep.Readiness:
                    Heading(L("How are you feeling today?","¿Cómo te sientes hoy?"));
                    string[] codes={"ready","low_energy","ill","pain","injury","unsure"};
                    string[] en={"Ready to train","Low energy","Feeling unwell","Pain","Injury","Not sure / pause"};
                    string[] es={"Listo para entrenar","Poca energía","Me siento enfermo/a","Dolor","Lesión","No estoy seguro/a / pausar"};
                    for(int i=0;i<codes.Length;i++){string code=codes[i];Choice(code,new Rect(107,1001+i*81,641,74),L(en[i],es[i]),m.Readiness==code,()=>controller.SelectReadiness(code));}
                    Primary(()=>controller.ContinueReadiness(),L("CONTINUE","CONTINUAR"),!string.IsNullOrEmpty(m.Readiness));
                    break;
                case ProfileStep.Paused:
                    Heading(L("Pause for today","Pausa por hoy"));
                    Paragraph(1030,L("We will pause training setup based on your answer. You can return when you feel ready.","Pausaremos la preparación del entrenamiento según tu respuesta. Puedes volver cuando te sientas listo/a."));
                    Paragraph(1240,L("This is not a diagnosis. If you need guidance about your symptoms, ask a health professional.","Esto no es un diagnóstico. Si necesitas orientación sobre tus síntomas, consulta a un profesional de salud."));
                    Primary(controller.Decline,L("CLOSE FOR NOW","CERRAR POR AHORA"));
                    Secondary("revise",new Rect(245,1645,363,37),L("Review my answer","Revisar mi respuesta"),()=>controller.Back());
                    break;
                default:
                    Heading(L("Next: experience and goals","Sigue: experiencia y objetivos"));
                    Paragraph(1070,L("Your preview reaches the next window here. Nothing has been saved to your account.","Tu vista previa llega aquí a la siguiente ventana. No se guardaron datos en tu cuenta."));
                    Primary(controller.Decline,L("CLOSE PREVIEW","CERRAR VISTA PREVIA"));
                    Secondary("revise",new Rect(245,1645,363,37),L("Back","Volver"),()=>controller.Back());
                    break;
            }
            status=TextAt(page,new Rect(105,1496,645,35),m.Error,20,body,new Color32(255,207,161,255),TextAnchor.MiddleCenter);
            status.horizontalOverflow=HorizontalWrapMode.Wrap;
        }
        void Heading(string title)=>TextAt(page,new Rect(105,918,643,65),title,35,serif,Silver,TextAnchor.MiddleCenter);
        void Paragraph(float y,string content)
        {var text=TextAt(page,new Rect(112,y,629,176),content,29,body,Silver,TextAnchor.UpperLeft);text.horizontalOverflow=HorizontalWrapMode.Wrap;}
        void Primary(Action action,string text,bool active=true)
        {
            var surface=SystemUI.Panel(page,new Rect(91,1545,671,96),PanelStyle.Primary);
            surface.color=active?Color.white:new Color(.5f,.6f,.7f,.7f);
            SystemUI.Caption(page,new Rect(115,1547,623,92),text,45,bold,active?Silver:SystemUI.Theme.muted);
            var button=Hit("continue",new Rect(91,1545,671,96),action);button.interactable=active;
        }
        void Secondary(string key,Rect rect,string label,Action action)
        {TextAt(page,rect,label,26,serif,Cyan,TextAnchor.MiddleCenter);Hit(key,rect,action);}
        void Choice(string key,Rect rect,string label,bool selected,Action action)
        {controls[key]=SystemUI.Button(page,rect,(selected?"◆  ":"◇  ")+label,action,selected?PanelStyle.Selected:PanelStyle.Outline);}

        InputField Field(string key,Rect rect,string value,Action<string> changed,bool integer=false)
        {
            var node=RectNode(key,page,rect);var image=node.gameObject.AddComponent<Image>();image.color=Color.clear;
            var input=node.gameObject.AddComponent<InputField>();input.targetGraphic=image;
            input.textComponent=TextAt(node,new Rect(27,0,rect.width-50,rect.height),"",40,serif,Silver);
            // Locale-specific decimal separators are validated by the controller, never silently stripped.
            input.contentType=InputField.ContentType.Standard;input.keyboardType=integer?TouchScreenKeyboardType.NumberPad:TouchScreenKeyboardType.DecimalPad;
            input.characterLimit=18;input.lineType=InputField.LineType.SingleLine;input.customCaretColor=true;input.caretColor=Cyan;
            var placeholder=TextAt(node,new Rect(27,0,rect.width-50,rect.height),"—",32,serif,new Color32(135,157,182,255));input.placeholder=placeholder;
            input.SetTextWithoutNotify(value??"");input.ForceLabelUpdate();input.onValueChanged.AddListener(v=>changed(v));return input;
        }
        void ChangeUnits(string system){KeyboardOff();controller.SetUnits(system);}
        void Back(){KeyboardOff();if(controller.Model.Step==ProfileStep.Notice){back?.Invoke();gameObject.SetActive(false);}else controller.Back();}
        void Exit(){KeyboardOff();leave?.Invoke();Destroy(gameObject);}
        void Language()
        {
            KeyboardOff();OpenModal(L("Language","Idioma"));
            ModalButton(new Rect(35,140,640,86),"English",()=>controller.SetLanguage("en"));
            ModalButton(new Rect(35,240,640,86),"Español",()=>controller.SetLanguage("es"));
            ModalButton(new Rect(35,340,640,86),L("Use device language","Usar idioma del dispositivo"),()=>controller.SetLanguage("auto"));
        }
        void Document(string id)
        {
            KeyboardOff();Notice(id=="privacy"?L("Privacy","Privacidad"):L("Terms","Términos"),L("The final document is not available yet. This preview cannot record acceptance. Your measurements remain in memory only.","El documento final aún no está disponible. Esta vista previa no puede registrar aceptación. Tus medidas permanecen solo en memoria."));
        }
        void Notice(string title,string message)
        {
            OpenModal(title);var text=TextAt(modal,new Rect(35,135,640,445),message,29,body,Silver,TextAnchor.UpperLeft);text.horizontalOverflow=HorizontalWrapMode.Wrap;
            ModalButton(new Rect(35,620,640,83),L("Close","Cerrar"),CloseModal);
        }
        void OpenModal(string title)
        {
            KeyboardOff();CloseModal();var shade=RectNode("Modal shade",root,new Rect(0,0,W,H));shade.gameObject.AddComponent<Image>().color=new Color(0,.01f,.04f,.87f);
            modal=RectNode("System dialog",shade,new Rect(71,500,711,745));var frame=modal.gameObject.AddComponent<SystemPanel>();frame.theme=SystemUI.Theme;frame.ornaments=true;
            TextAt(modal,new Rect(30,24,560,70),title,34,bold,Silver);ModalButton(new Rect(615,12,70,80),"×",CloseModal);
        }
        void ModalButton(Rect rect,string label,Action action)
        {SystemUI.Button(modal,rect,label,action);}

        void CloseModal(){if(modal==null)return;var old=modal.parent.gameObject;modal=null;old.SetActive(false);Destroy(old);}
        bool HasFocus()=>heightInput!=null&&heightInput.isFocused||feetInput!=null&&feetInput.isFocused||inchesInput!=null&&inchesInput.isFocused||weightInput!=null&&weightInput.isFocused;
        void KeyboardOff(){foreach(var input in new[]{heightInput,feetInput,inchesInput,weightInput})if(input!=null)input.DeactivateInputField();EventSystem.current?.SetSelectedGameObject(null);}
        string L(string en,string es)=>controller.Model.Language=="es"?es:en;
        float L(float en,float es)=>controller.Model.Language=="es"?es:en;
        Text Live(string name,Rect rect,string value,int size,Color? color=null,Font font=null)
        {
            var box=new Rect(rect.x,rect.y-8,rect.width,rect.height+16);
            var t=SystemUI.Caption(page,box,value,size,font??serif,color??Silver);t.name=name;
            if(name.EndsWith("label")||name=="Units")t.alignment=TextAnchor.MiddleLeft;
            if(name=="Title")t.gameObject.AddComponent<OnboardingSilverText>();return t;
        }

        static RectTransform RectNode(string name,Transform parent,Rect rect)
        {return SystemUI.Node(name,parent,rect);}
        Text TextAt(Transform parent,Rect rect,string value,int size,Font font,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {return SystemUI.Text(parent,rect,value,size,font,color,align);}
        Button Hit(string key,Rect rect,Action action)
        {var node=RectNode(key,page,rect);var image=node.gameObject.AddComponent<Image>();image.color=Color.clear;var b=node.gameObject.AddComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.None;b.onClick.AddListener(()=>action());controls[key]=b;return b;}
        void Update()
        {
            if(root==null)return;
            if(lastSize.x!=Screen.width||lastSize.y!=Screen.height||lastSafe!=Screen.safeArea||lastKeyboard!=TouchScreenKeyboard.visible||lastKeyboardHeight!=TouchScreenKeyboard.area.height||lastFocused!=FocusedInput())Fit();
            if(Input.GetKeyDown(KeyCode.Escape)){if(modal!=null)CloseModal();else Back();}
        }
        InputField FocusedInput(){foreach(var input in new[]{heightInput,feetInput,inchesInput,weightInput})if(input!=null&&input.isFocused)return input;return null;}
        void Fit()
        {
            var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/W,safe.height/H);float top=Screen.height-safe.yMax+(safe.height-H*scale)/2;float lift=0;
            if(TouchScreenKeyboard.visible){float bottom=weightInput!=null&&weightInput.isFocused?1400:1250;float occlusion=TouchScreenKeyboard.area.height>0?TouchScreenKeyboard.area.yMax:Screen.height*.42f;lift=Mathf.Max(0,occlusion+20*scale-(Screen.height-top-bottom*scale));}
            SystemViewport.Fit(root,W,H,lift);lastSize=new Vector2(Screen.width,Screen.height);lastSafe=safe;lastKeyboard=TouchScreenKeyboard.visible;lastKeyboardHeight=TouchScreenKeyboard.area.height;lastFocused=FocusedInput();
        }
        IEnumerator Capture()
        {
            for(int i=0;i<8;i++)yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(capture,image.EncodeToPNG());Destroy(image);Debug.Log("SOLOGYM_CAPTURE "+capture);
            if(HasArgument("-sologym-smoke"))yield return Smoke();Application.Quit();
        }
        IEnumerator Smoke()
        {
            bool passed=true;
            try{Debug.Log("SOLOGYM_PROFILE_STATE "+PrivateProfileStateChecks.Run());}catch(Exception e){Debug.LogError(e.Message);passed=false;}
            controls["imperial"].onClick.Invoke();yield return null;passed &= feetInput!=null&&inchesInput!=null&&controller.Model.UnitSystem=="imperial";
            controls["metric"].onClick.Invoke();yield return null;passed &= controller.Model.HeightCm==170m&&controller.Model.BodyweightKg==70m;
            heightInput.text="";weightInput.text="";yield return null;passed &= controller.Model.HeightCm==null&&controller.Model.BodyweightKg==null;
            controls["continue"].onClick.Invoke();yield return null;passed &= controller.Model.Step==ProfileStep.Readiness;
            controls["pain"].onClick.Invoke();controls["continue"].onClick.Invoke();yield return null;passed &= controller.Model.Step==ProfileStep.Paused;
            controller.Back();controls["ready"].onClick.Invoke();controls["continue"].onClick.Invoke();yield return null;passed &= controller.Model.Step==ProfileStep.Checkpoint;
            string json="{\"passed\":"+(passed?"true":"false")+",\"checks\":[\"profile state checks\",\"native feet/inches controls\",\"unit round trip\",\"blank optional measurements\",\"readiness navigation\",\"pain pauses\",\"ready review checkpoint\"]}";
            File.WriteAllText(Path.ChangeExtension(capture,".smoke.json"),json);Debug.Log("SOLOGYM_PROFILE_SMOKE "+json);if(!passed)Application.Quit(2);
        }
        // Exercises native bindings plus the parent callback/reentry, not only the state model.
        internal bool CheckBackRetentionForReview(Action reenter)
        {
            if(!controller.Model.ReviewMode||controller.Model.Step!=ProfileStep.Notice)return false;
            controls["continue"].onClick.Invoke();
            heightInput.text="181.25";weightInput.text="82.375";
            controls["imperial"].onClick.Invoke();
            string feet=feetInput.text,inches=inchesInput.text,weight=weightInput.text;
            controls["back"].onClick.Invoke();
            controls["back"].onClick.Invoke();
            bool hidden=!gameObject.activeSelf;
            reenter();
            bool retained=hidden&&gameObject.activeSelf&&controller.Model.Step==ProfileStep.Notice
                &&controller.Model.HeightCm==181.25m&&controller.Model.BodyweightKg==82.375m
                &&controller.Model.UnitSystem=="imperial";
            controls["continue"].onClick.Invoke();
            return retained&&feetInput!=null&&inchesInput!=null
                &&feetInput.text==feet&&inchesInput.text==inches&&weightInput.text==weight;
        }
        void OnDestroy(){controller?.Dispose();}
        static bool HasArgument(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0;
        static string Argument(string key){var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,key);return index>=0&&index+1<args.Length?args[index+1]:null;}
    }
}
