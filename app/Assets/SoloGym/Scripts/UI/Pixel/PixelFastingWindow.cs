using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SoloGym.UI.PixelJournalUI;

namespace SoloGym.UI
{
    /// <summary>One optional adult window: setup, clock, corrections and private local history.</summary>
    public sealed class PixelFastingWindow : MonoBehaviour
    {
        public FastingTracker Tracker { get; private set; }
        public RectTransform Composition { get; private set; }
        public PixelFastingClock ClockArt { get; private set; }
        public string View { get; private set; }
        public string Language { get; private set; }
        public readonly Dictionary<string,Selectable> Controls = new Dictionary<string,Selectable>();
        public Action<string> Exited;
        RectTransform safe,page,clockRoot;
        readonly List<Selectable> traversal=new List<Selectable>();
        Text elapsed,stageLabel,bandLabel,timerNotice,chosenEnd,startLabel;
        string error,editId,editStart,editEnd,confirm;
        int planMinutes=720,lastWidth,lastHeight,historyPage;
        bool acknowledged,initialized;
        Rect lastSafe;float inset;
        string L(string en,string es)=>Language=="es"?es:en;
        public static string DefaultPath=>PixelWorkoutWindow.Arg("-sologym-fasting-file",Path.Combine(Application.persistentDataPath,"fasting-local-v1.json"));
        public void Initialize(string locale,bool eligible,Action<string> onExit,IJournalStorage storage=null,Func<DateTimeOffset> now=null)
        {
            if(initialized)return;initialized=true;Language=locale=="es"?"es":"en";Exited=onExit;
            Application.targetFrameRate=60;Screen.orientation=ScreenOrientation.LandscapeLeft;
            if(EventSystem.current==null)new GameObject("Fasting input",typeof(EventSystem),typeof(StandaloneInputModule));
            if(FindFirstObjectByType<Camera>()==null){var cam=new GameObject("Fasting camera").AddComponent<Camera>();cam.cullingMask=0;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color32(13,18,27,255);}
            Tracker=new FastingTracker(storage??new JournalFileStorage(DefaultPath),eligible,now);Tracker.Load();
            View=!eligible?"unavailable":!Tracker.Loaded?"error":Tracker.Data.enabled?"timer":"setup";
            if(Tracker.Loaded)planMinutes=Tracker.Data.planMinutes;
            float.TryParse(PixelWorkoutWindow.Arg("-sologym-safe-inset","0"),NumberStyles.Float,CultureInfo.InvariantCulture,out inset);inset=Mathf.Clamp(inset,0,150);
            var canvasRoot=Rect("Fasting canvas",transform,new Rect());var canvas=canvasRoot.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=70;canvas.pixelPerfect=true;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();canvasRoot.gameObject.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            safe=Rect("Safe area",canvasRoot,new Rect());Composition=Rect("1280x720 fasting composition",safe,new Rect(0,0,1280,720));Composition.anchorMin=Composition.anchorMax=Composition.pivot=new Vector2(.5f,.5f);Composition.anchoredPosition=Vector2.zero;
            // An ineligible route never constructs or loads the fasting artwork or private data.
            if(eligible)
            {
                Art(Composition,new Rect(0,0,1280,720),"Rooms/FastingR1/background");
                clockRoot=Rect("Independent arcane clock",Composition,new Rect(190,40,560,560));ClockArt=clockRoot.gameObject.AddComponent<PixelFastingClock>();ClockArt.Initialize();
            }
            Render();Relayout();
        }
        public void Relayout()
        {
            lastWidth=Screen.width;lastHeight=Screen.height;lastSafe=Screen.safeArea;var r=Screen.safeArea;
            float x=Mathf.Max(inset,r.xMin),y=Mathf.Max(inset,r.yMin),right=Mathf.Min(Screen.width-inset,r.xMax),top=Mathf.Min(Screen.height-inset,r.yMax);
            safe.anchorMin=new Vector2(x/Screen.width,y/Screen.height);safe.anchorMax=new Vector2(right/Screen.width,top/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            float scale=Mathf.Max(.01f,Mathf.Min((right-x)/1280,(top-y)/720));Composition.localScale=new Vector3(scale,scale,1);Canvas.ForceUpdateCanvases();
        }
        void Update()
        {
            if(!initialized||!Composition.gameObject.activeInHierarchy)return;
            if(lastWidth!=Screen.width||lastHeight!=Screen.height||lastSafe!=Screen.safeArea)Relayout();
            RefreshTimer();
            if(Input.GetKeyDown(KeyCode.Tab)&&EventSystem.current.currentSelectedGameObject==null)traversal.FirstOrDefault(x=>x.IsInteractable())?.Select();
            if(Input.GetKeyDown(KeyCode.Escape)){if(View=="timer"||View=="setup"||View=="error"||View=="unavailable")Exited?.Invoke("home");else Show(Tracker.Data.enabled?"timer":"setup");}
        }
        public void Show(string view){error=null;View=view;Render();}
        public bool Perform(Func<bool> change,string after=null)
        {bool ok=change();error=ok?null:Tracker.Error;if(ok&&after!=null){View=after;if(after=="setup")acknowledged=false;}Render();return ok;}
        public void Render()
        {
            if(Composition==null)return;
            var selected=EventSystem.current.currentSelectedGameObject;string focus=selected!=null?selected.name:null;EventSystem.current.SetSelectedGameObject(null);
            if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}Controls.Clear();traversal.Clear();elapsed=stageLabel=bandLabel=timerNotice=chosenEnd=startLabel=null;
            page=Rect("Fasting live controls",Composition,new Rect(0,0,1280,720));
            if(clockRoot!=null)clockRoot.gameObject.SetActive(View=="timer");
            if(!Tracker.Eligible)
            {var unavailable=Frame(page,new Rect(300,250,680,220),"Unavailable route");Text(unavailable,new Rect(30,30,620,78),L("This area is unavailable for this profile.","Esta área no está disponible para este perfil."),27,false,TextAnchor.MiddleCenter);Button(unavailable,"exit",new Rect(200,133,280,60),L("RETURN HOME","VOLVER AL HOGAR"),()=>Exited?.Invoke("home"));FinishFocus(focus);return;}
            var plaque=Frame(page,new Rect(24,18,338,80),"Time chamber title");Text(plaque,new Rect(10,7,318,38),L("CHAMBER OF TIME","CÁMARA DEL TIEMPO"),28,false,TextAnchor.MiddleCenter);Text(plaque,new Rect(10,46,318,27),L("OPTIONAL FASTING","AYUNO OPCIONAL"),20,false,TextAnchor.MiddleCenter);
            if(Tracker.Loaded)
            {
                Button(page,"history",new Rect(1014,24,178,58),L("HISTORY","HISTORIAL"),()=>Show("history"));
                var settings=Button(page,"settings",new Rect(1206,24,50,58),"",()=>Show("settings"),true);
                Art(settings.transform,new Rect(11,14,28,28),"UI/Pixel/IconSettings").sprite=Resources.LoadAll<Sprite>("UI/Pixel/IconSettings")[0];
            }
            if(Tracker.Loaded&&Tracker.Active!=null&&View!="timer"&&View!="settings")Button(page,"end",new Rect(733,24,261,58),L("END NOW","FINALIZAR AHORA"),()=>Perform(()=>Tracker.EndNow(),"history"),true,23);
            switch(View)
            {
                case "timer":BuildTimer();break;
                case "setup":BuildSetup();break;
                case "start":case "edit-active":case "edit-record":BuildEdit();break;
                case "history":BuildHistory();break;
                case "settings":BuildSettings();break;
                case "guide":BuildGuide();break;
                case "confirm":BuildConfirm();break;
                default:BuildError();break;
            }
            if(Tracker.Loaded&&Tracker.Data.enabled)
            {
                string[] ids={"home","workouts","dungeon","fasting"};string[] labels={L("Home","Hogar"),L("Workouts","Rutinas"),L("Dungeon","Mazmorra"),L("Fasting","Ayuno")};
                for(int i=0;i<4;i++){string route=ids[i];Button(page,"nav-"+route,new Rect(314+i*164,647,164,58),labels[i],()=>{if(route=="fasting")Show("timer");else Exited?.Invoke(route);},i==3,23);}
            }
            else Button(page,"nav-home",new Rect(490,647,300,58),L("RETURN HOME","VOLVER AL HOGAR"),()=>Exited?.Invoke("home"));
            Text(page,new Rect(12,690,298,24),PixelWorkoutWindow.Has("-sologym-smoke")?L("Sample data · isolated review","Datos de ejemplo · revisión aislada"):L("Local log · no cloud sync","Registro local · sin sincronización"),15,false);
            if(error!=null)Text(page,new Rect(180,604,1040,36),ErrorMessage(error),19,false,TextAnchor.MiddleCenter);
            RefreshTimer();FinishFocus(focus);
        }
        void FinishFocus(string focus)
        {
            var available=traversal.Where(x=>x.IsInteractable()).ToArray();
            for(int i=0;i<available.Length;i++){var prev=available[(i+available.Length-1)%available.Length];var next=available[(i+1)%available.Length];var tab=available[i].GetComponent<PixelFieldTabNavigation>()??available[i].gameObject.AddComponent<PixelFieldTabNavigation>();tab.Previous=prev;tab.Next=next;available[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=prev,selectOnDown=next,selectOnLeft=prev,selectOnRight=next};}
            if(focus!=null&&Controls.TryGetValue(focus,out var current)&&current.IsInteractable())current.Select();
        }
        RectTransform Paper(Rect b)
        {
            var p=Frame(page,b,"Parchment information");var paper=Rect("Reusable parchment",p,new Rect(7,7,b.width-14,b.height-14)).gameObject.AddComponent<RawImage>();paper.texture=Resources.Load<Texture2D>("Rooms/WorkoutsR1/journal");paper.uvRect=new Rect(0,0,.46f,1);paper.raycastTarget=false;
            var edge=Art(p,new Rect(0,0,b.width,b.height),"UI/Pixel/ContentPanel");edge.sprite=Resources.LoadAll<Sprite>("UI/Pixel/ContentPanel")[0];edge.type=Image.Type.Sliced;edge.fillCenter=false;edge.pixelsPerUnitMultiplier=3;return p;
        }
        RectTransform Wide(string title)
        {var p=Paper(new Rect(190,116,900,480));Text(p,new Rect(25,18,850,40),title,29,true,TextAnchor.MiddleCenter);return p;}
        void BuildTimer()
        {
            bool active=Tracker.Data.active!=null;
            Text(page,new Rect(381,259,178,25),L("TIME","TIEMPO"),17,false,TextAnchor.MiddleCenter);
            elapsed=Text(page,new Rect(368,287,205,52),"",42,false,TextAnchor.MiddleCenter);elapsed.resizeTextForBestFit=true;elapsed.resizeTextMinSize=28;elapsed.resizeTextMaxSize=42;
            Text(page,new Rect(378,343,186,25),L("ELAPSED","TRANSCURRIDO"),17,false,TextAnchor.MiddleCenter);
            Button(page,"guide",new Rect(326,590,286,47),L("View time guide","Ver guía de etapas"),()=>Show("guide"),false,21);
            var p=Paper(new Rect(735,106,473,492));
            Text(p,new Rect(24,26,425,30),active?L("APPROXIMATE STAGE","ETAPA ORIENTATIVA"):L("YOUR PRIVATE CLOCK","TU RELOJ PRIVADO"),24,true,TextAnchor.MiddleCenter);
            stageLabel=Text(p,new Rect(25,74,423,50),"",34,true,TextAnchor.MiddleCenter);Rule(p,57,139,359);
            bandLabel=Text(p,new Rect(30,155,413,46),"",23,true,TextAnchor.MiddleCenter);
            Text(p,new Rect(37,211,399,64),L("Body changes vary.\nThis clock does not measure them.","Los cambios varían entre personas.\nEl reloj no mide tu metabolismo."),21,true,TextAnchor.MiddleCenter);Rule(p,31,285,411);
            if(active)
            {
                Text(p,new Rect(36,299,120,30),L("Start","Inicio"),22);startLabel=Text(p,new Rect(157,295,280,42),"",20,true,TextAnchor.MiddleRight);
                Rule(p,36,342,401);Text(p,new Rect(36,351,126,30),L("Chosen end","Fin elegido"),22);chosenEnd=Text(p,new Rect(157,346,280,42),"",20,true,TextAnchor.MiddleRight);
                Button(p,"edit-start",new Rect(115,387,245,42),L("Edit start","Editar inicio"),()=>OpenEdit("edit-active"),false,20,false);
                Button(p,"end",new Rect(31,435,411,49),L("END NOW","FINALIZAR AHORA"),()=>{if(Perform(()=>Tracker.EndNow(),"history"))error=null;},true,25);
            }
            else
            {
                Text(p,new Rect(40,300,393,87),L("Choose your times.\nYou can end at any time.","Elige el inicio y el fin. Puedes finalizar cuando quieras."),25,true,TextAnchor.MiddleCenter);
                Button(p,"start",new Rect(31,421,411,57),L("SET UP A LOG","PREPARAR REGISTRO"),()=>OpenEdit("start"),true,24);
            }
            timerNotice=Text(page,new Rect(710,607,543,29),"",18,false,TextAnchor.MiddleCenter);
        }
        void RefreshTimer()
        {
            if(!Tracker.Loaded)return;var active=Tracker.Active;ClockArt?.Bind(active!=null&&View=="timer",Tracker.ReducedMotion||PixelWorkoutWindow.Has("-sologym-reduced-motion"),Tracker.Band);
            if(elapsed==null)return;
            elapsed.text=active==null?"--:--:--":Tracker.ClockBeforeStart?"--:--:--":FastingTracker.Duration(Tracker.Elapsed);
            stageLabel.text=active==null?L("No active log","Sin registro activo"):Tracker.ClockBeforeStart?L("Check the start","Revisa el inicio"):Stage(Tracker.Band);
            bandLabel.text=active==null?L("Optional · no rewards","Opcional · sin recompensas"):Band(Tracker.Band);
            if(startLabel!=null)startLabel.text=DisplayTime(active.startUtc);if(chosenEnd!=null)chosenEnd.text=DisplayTime(active.startUtc+active.planMinutes*60L);
            if(timerNotice!=null)timerNotice.gameObject.SetActive(error==null);
            if(timerNotice!=null)timerNotice.text=Tracker.ClockBeforeStart?L("The device clock is before the recorded start.","La hora del dispositivo es anterior al inicio."):Tracker.PastChosenEnd?L("Chosen end passed. Finish or correct your log.","El fin elegido pasó. Finaliza o corrige el registro."):L("You can finish whenever you want.","Puedes finalizar cuando quieras.");
        }
        string Stage(int band)
        {string[] en={"After eating","Between meals","16–24 hour interval","24–48 hour interval","Over 48 hours"};string[] es={"Tras la comida","Entre comidas","Intervalo de 16–24 h","Intervalo de 24–48 h","Más de 48 horas"};return (Language=="es"?es:en)[band];}
        string Band(int band)=>(new[]{"0–4 h","4–16 h","16–24 h","24–48 h","48+ h"})[band]+L(" since last meal"," desde la última comida");
        string DisplayTime(long utc)=>DateTimeOffset.FromUnixTimeSeconds(utc).ToLocalTime().ToString("dd MMM · HH:mm",CultureInfo.GetCultureInfo(Language=="es"?"es-MX":"en-US"));
        static string HistoryTime(long utc)=>DateTimeOffset.FromUnixTimeSeconds(utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm",CultureInfo.InvariantCulture);
        void BuildSetup()
        {
            var p=Wide(L("OPTIONAL ADULT LOG","REGISTRO OPCIONAL PARA ADULTOS"));
            Text(p,new Rect(40,76,820,60),L("Off by default. This local log has no rewards or effect on training.","Apagado por defecto. Este registro local no da recompensas ni cambia tu entrenamiento."),25,true,TextAnchor.MiddleCenter);
            Text(p,new Rect(45,148,810,146),L("Do not use during pregnancy or breastfeeding, with an eating-disorder history, or with type 1 diabetes treated with insulin. If you have a medical condition or take medication, consult a qualified clinician first. Longer is not better; the 20-hour plan limit is not medical clearance.","No usar durante embarazo o lactancia, con antecedentes de trastornos alimentarios ni con diabetes tipo 1 tratada con insulina. Si tienes una condición médica o tomas medicamentos, consulta antes a un profesional. Más tiempo no es mejor; el límite de 20 horas no garantiza seguridad."),24);
            Button(p,"ack",new Rect(44,315,812,66),(acknowledged?"[x] ":"[ ] ")+L("I am 18+; I have read the information and no exclusion applies.","Tengo 18+; leí la información y no se aplica ninguna exclusión."),()=>{acknowledged=!acknowledged;Render();},acknowledged,22);
            Button(p,"enable",new Rect(244,403,412,57),L("ENABLE OPTIONAL LOG","ACTIVAR REGISTRO OPCIONAL"),()=>Perform(()=>Tracker.Enable(planMinutes,acknowledged),"timer"),true,23).interactable=acknowledged;
        }
        public void OpenEdit(string mode,string id=null)
        {
            View=mode;error=null;editId=id;var e=mode=="edit-active"?Tracker.Data.active:mode=="edit-record"?Tracker.Data.records.First(x=>x.id==id):null;
            editStart=FastingTracker.EditTime(e?.startUtc??Tracker.Now);editEnd=FastingTracker.EditTime(e?.endUtc??Tracker.Now);planMinutes=e?.planMinutes??Tracker.Data.planMinutes;Render();
        }
        void BuildEdit()
        {
            bool starting=View=="start",history=View=="edit-record";var p=Wide(starting?L("CHOOSE YOUR TIMES","ELIGE LOS HORARIOS"):L("CORRECT YOUR RECORD","CORRIGE TU REGISTRO"));
            Text(p,new Rect(42,70,816,48),L("Date and time with UTC offset · example: 2026-10-01 08:30:00 -06:00","Fecha y hora con zona UTC · ejemplo: 2026-10-01 08:30:00 -06:00"),21,true,TextAnchor.MiddleCenter);
            Field(p,"start-time",new Rect(88,124,724,102),L("Actual start","Inicio real"),editStart,v=>editStart=v);
            if(history)Field(p,"end-time",new Rect(88,248,724,102),L("Actual end","Fin real"),editEnd,v=>editEnd=v);
            else if(starting)
            {
                Text(p,new Rect(88,234,724,35),L("Chosen duration · hours (not a target)","Duración elegida · horas (no es una meta)"),23);
                int[] plans={12,14,16,18,20};for(int i=0;i<plans.Length;i++){int minutes=plans[i]*60;Button(p,"plan-"+plans[i],new Rect(88+i*146,282,137,51),plans[i]+" h",()=>{planMinutes=minutes;Render();},planMinutes==minutes);}
                Text(p,new Rect(88,346,724,44),L("Logs keep actual time; plans never extend automatically.","Se conserva el tiempo real si se pasa el fin; no se amplía el plan."),21);
            }
            else Text(p,new Rect(88,251,724,100),L("The chosen duration stays at ","La duración elegida se mantiene en ")+planMinutes/60+L(" h. The displayed end follows the corrected start. Past records may exceed the plan limit; they are not goals."," h. El fin mostrado sigue al inicio corregido. Los registros reales pueden superar el plan; no son metas."),24);
            Button(p,"cancel-edit",new Rect(89,401,290,58),L("CANCEL","CANCELAR"),()=>Show(history?"history":"timer"));
            Button(p,"save-edit",new Rect(400,401,413,58),starting?L("START LOG","INICIAR REGISTRO"):L("SAVE CORRECTION","GUARDAR CORRECCIÓN"),SaveEdit,true,24);
        }
        void SaveEdit()
        {
            if(!FastingTracker.ParseTime(editStart,out long start)||(View=="edit-record"&&!FastingTracker.ParseTime(editEnd,out _))){error="time";Render();return;}
            if(View=="start")Perform(()=>Tracker.Start(start,planMinutes),"timer");
            else if(View=="edit-active")Perform(()=>Tracker.CorrectStart(start),"timer");
            else {FastingTracker.ParseTime(editEnd,out long end);Perform(()=>Tracker.Correct(editId,start,end),"history");}
        }
        void BuildHistory()
        {
            var p=Wide(L("PRIVATE HISTORY","HISTORIAL PRIVADO"));var all=Tracker.Data.records.OrderByDescending(x=>x.startUtc).ToArray();
            int pages=Math.Max(1,(all.Length+19)/20);historyPage=Mathf.Clamp(historyPage,0,pages-1);var records=all.Skip(historyPage*20).Take(20).ToArray();
            Text(p,new Rect(35,66,830,48),L("Only on this device. Correct or delete your records.","Solo en este dispositivo. Corrige o borra tus registros."),23,true,TextAnchor.MiddleCenter);
            var list=Scroll(p,new Rect(36,124,828,282),Math.Max(282,records.Length*84));
            if(records.Length==0)Text(list,new Rect(25,50,755,110),L("No saved records. You can leave this feature off.","Todavía no hay registros. Puedes dejar esta función apagada."),28,true,TextAnchor.MiddleCenter);
            for(int i=0;i<records.Length;i++)
            {
                var e=records[i];float y=i*84;Text(list,new Rect(8,y+5,465,69),HistoryTime(e.startUtc)+" → "+HistoryTime(e.endUtc)+"\n"+FastingTracker.Duration(e.endUtc-e.startUtc),21);
                Button(list,"edit-"+e.id,new Rect(490,y+12,156,53),L("Correct","Corregir"),()=>OpenEdit("edit-record",e.id),false,21);
                Button(list,"delete-"+e.id,new Rect(659,y+12,137,53),L("Delete","Borrar"),()=>AskDelete(e.id),false,21);Rule(list,8,y+80,788);
            }
            if(pages>1){Button(p,"history-previous",new Rect(38,417,150,48),"<",()=>{historyPage--;Render();}).interactable=historyPage>0;Button(p,"history-next",new Rect(712,417,150,48),">",()=>{historyPage++;Render();}).interactable=historyPage<pages-1;Text(p,new Rect(195,426,85,28),(historyPage+1)+" / "+pages,18);}
            Button(p,"history-back",new Rect(290,417,320,48),L("BACK TO CLOCK","VOLVER AL RELOJ"),()=>Show(Tracker.Data.enabled?"timer":"setup"));
        }
        void BuildSettings()
        {
            var p=Wide(L("CLOCK SETTINGS","AJUSTES DEL RELOJ"));var d=Tracker.Data;
            Text(p,new Rect(38,77,824,60),L("Disabling keeps your history. Deleting history is a separate action.","Desactivar conserva el historial. Borrar el historial es una acción distinta."),25,true,TextAnchor.MiddleCenter);
            Button(p,"motion",new Rect(80,156,740,58),(d.reducedMotion?"[x] ":"[ ] ")+L("Reduced motion · static clock","Movimiento reducido · reloj estático"),()=>Perform(()=>Tracker.SetReducedMotion(!d.reducedMotion)),d.reducedMotion,25);
            if(d.active!=null)
            {
                Text(p,new Rect(82,226,736,66),L("End or discard the active log before disabling.","Finaliza o descarta el registro activo antes de desactivar."),24,true,TextAnchor.MiddleCenter);
                Button(p,"discard",new Rect(80,313,358,58),L("DISCARD ACTIVE LOG","DESCARTAR REGISTRO ACTIVO"),()=>AskDelete("active"),false,20);
                Button(p,"end",new Rect(458,313,362,58),L("END NOW","FINALIZAR AHORA"),()=>Perform(()=>Tracker.EndNow(),"history"),true,24);
            }
            else Button(p,"disable",new Rect(80,261,740,60),d.enabled?L("DISABLE · KEEP HISTORY","DESACTIVAR · CONSERVAR HISTORIAL"):L("ENABLEMENT INFORMATION","INFORMACIÓN PARA ACTIVAR"),()=>{if(d.enabled)Perform(()=>Tracker.Disable(),"setup");else {acknowledged=false;Show("setup");}},false,24);
            Button(p,"clear-history",new Rect(80,398,358,58),L("DELETE HISTORY","BORRAR HISTORIAL"),()=>AskDelete("all"),false,23).interactable=d.records.Length>0;
            Button(p,"settings-back",new Rect(458,398,362,58),L("DONE","LISTO"),()=>Show(d.enabled?"timer":"setup"),true,24);
        }
        void BuildGuide()
        {
            var p=Wide(L("TIME GUIDE","GUÍA POR TIEMPO"));
            Text(p,new Rect(38,71,824,66),L("These are time ranges, not measured biological stages. They do not confirm ketosis, autophagy or immune regeneration.","Son intervalos de tiempo, no etapas biológicas medidas. No confirman cetosis, autofagia ni regeneración inmune."),23,true,TextAnchor.MiddleCenter);
            for(int i=0;i<5;i++){Text(p,new Rect(70,155+i*40,310,35),(new[]{"0–4 h","4–16 h","16–24 h","24–48 h","48+ h"})[i],23);Text(p,new Rect(390,155+i*40,435,35),Stage(i),23);}
            Text(p,new Rect(50,365,800,51),L("Longer is not better. Ranges beyond supported plans only describe actual or corrected records.","Más tiempo no es mejor. Los intervalos fuera del plan solo describen registros reales o corregidos."),21,true,TextAnchor.MiddleCenter);
            Button(p,"guide-back",new Rect(294,422,312,47),L("BACK","VOLVER"),()=>Show("timer"));
        }
        void AskDelete(string id){confirm=id;Show("confirm");}
        void BuildConfirm()
        {
            var p=Wide(L("DELETE THIS DATA?","¿BORRAR ESTOS DATOS?"));
            Text(p,new Rect(70,126,760,122),confirm=="all"?L("All saved fasting history will be deleted from this local log. Your active timer and enablement setting stay unchanged.","Se borrará todo el historial local de ayuno. Tu registro activo y la activación no cambiarán."):confirm=="active"?L("Discard the active log without adding it to history? This cannot be undone.","¿Descartar el registro activo sin añadirlo al historial? No se puede deshacer."):L("This saved entry will be deleted. This cannot be undone.","Se borrará este registro guardado. No se puede deshacer."),27,true,TextAnchor.MiddleCenter);
            Button(p,"cancel-delete",new Rect(80,337,350,61),L("KEEP IT","CONSERVAR"),()=>Show(confirm=="all"||confirm=="active"?"settings":"history"),true,25);
            Button(p,"confirm-delete",new Rect(468,337,350,61),L("DELETE","BORRAR"),()=>Perform(()=>confirm=="all"?Tracker.ClearHistory():confirm=="active"?Tracker.DiscardActive():Tracker.Delete(confirm),confirm=="active"?"timer":"history"),false,25);
        }
        void BuildError()
        {
            var p=Wide(L("LOCAL FILE UNAVAILABLE","ARCHIVO LOCAL NO DISPONIBLE"));Text(p,new Rect(65,122,770,167),L("The original file has been preserved. No records were reset or replaced. Check device storage or restore a valid copy, then retry.","Se conservó el archivo original. No se reiniciaron ni reemplazaron registros. Revisa el almacenamiento o recupera una copia válida y reintenta."),28,true,TextAnchor.MiddleCenter);
            Button(p,"retry",new Rect(280,356,340,65),L("RETRY","REINTENTAR"),()=>{Tracker.Load();Show(Tracker.Loaded?(Tracker.Data.enabled?"timer":"setup"):"error");},true,25);
        }
        PixelFormField Field(Transform p,string id,Rect bounds,string label,string value,Action<string> changed)
        {var f=PixelFormField.Create(p,PixelFormField.Kind.Text,label,"yyyy-MM-dd HH:mm:ss ±HH:mm");Place((RectTransform)f.transform,bounds);f.SetLabelColor(Ink);f.Background.pixelsPerUnitMultiplier=2;f.Input.characterLimit=32;f.SetValueWithoutNotify(value);f.Input.onValueChanged.AddListener(v=>changed(v));f.Message.gameObject.SetActive(false);Register(id,f.Input);return f;}
        PixelJournalAction Button(Transform p,string id,Rect b,string label,Action click,bool primary=false,int size=22,bool framed=true)
        {var a=Action(p,b,label,click,framed,primary,size);Register(id,a);return a;}
        void Register(string id,Selectable s){s.name=id;Controls[id]=s;traversal.Add(s);}
        string ErrorMessage(string id)
        {
            switch(id){
                case "time":return L("Use YYYY-MM-DD HH:mm:ss ±HH:mm, for example 2026-10-01 08:30:00 -06:00.","Usa AAAA-MM-DD HH:mm:ss ±HH:mm, por ejemplo 2026-10-01 08:30:00 -06:00.");
                case "future":case "order":return L("Check the dates: start must not follow end, and actual times cannot be in the future.","Revisa las fechas: inicio anterior al fin y horas reales que no estén en el futuro.");
                case "overlap":return L("This time overlaps another record. Correct the times before saving.","Este horario se cruza con otro registro. Corrige las horas antes de guardar.");
                case "clock":return L("The device time is before the start. Correct the start or device clock.","La hora del dispositivo es anterior al inicio. Corrige el inicio o el reloj.");
                case "active":return L("End or discard the active log first.","Primero finaliza o descarta el registro activo.");
                default:return L("Could not save. Your previous records and input are preserved; retry.","No se pudo guardar. Se conservaron tus registros y datos; reintenta.");
            }
        }
        public void Capture(string path){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());Destroy(image);Debug.Log("FASTING_CAPTURE "+path);}
    }
}
