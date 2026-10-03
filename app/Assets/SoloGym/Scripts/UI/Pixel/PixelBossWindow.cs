using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoloGym.Rendering;
using static SoloGym.UI.PixelJournalUI;

namespace SoloGym.UI
{
    /// <summary>Boss stand-off: the 3D summoning hall (or the painted fallback) with live, local manual workout controls.</summary>
    public sealed class PixelBossWindow : MonoBehaviour
    {
        public BossController Controller { get; private set; }
        public RectTransform Composition { get; private set; }
        public readonly Dictionary<string,Selectable> Controls=new Dictionary<string,Selectable>();
        public string Language {get;private set;}
        public string View {get;private set;}="session";
        public Action Exited;
        WorkoutJournal journal;
        RectTransform safe,page;
        string character,error,notice,editingId,quantity="",load="",inputSet="";
        Text restLabel;
        readonly List<Selectable> traversal=new List<Selectable>();
        int lastWidth,lastHeight,lastRest=-1;
        Rect lastSafe;
        float inset;
        bool supervised;
        BossContent content;
        BossEntry boss;
        PixelSpriteLoop heroLoop,bossLoop;
        BossStage3D stage;
        public BossStage3D Stage=>stage;
        string BossName=>boss==null?L("BOSS","JEFE"):boss.name.Get(Language);
        string Line(string key)=>content==null?"":content.Line(key,Language);
        string L(string en,string es)=>Language=="es"?es:en;
        public void Initialize(WorkoutJournal source,string locale,string appearance,Action exited,bool restored,Func<double> clock=null)
        {
            journal=source;Language=locale;character=appearance;Exited=exited;
            try { Controller=new BossController(journal.ActiveSession,journal.Plans,journal.SaveBoss,restored,clock); }
            catch(Exception e) { error=e.Message; View="error"; }
            var root=Rect("Dungeon canvas",transform,new Rect());var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;canvas.pixelPerfect=true;
            root.gameObject.AddComponent<GraphicRaycaster>();root.gameObject.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            safe=Rect("Dungeon safe area",root,new Rect());Composition=Rect("1280x720 boss composition",safe,new Rect(0,0,1280,720));
            Composition.anchorMin=Composition.anchorMax=Composition.pivot=new Vector2(.5f,.5f);Composition.anchoredPosition=Vector2.zero;
            float.TryParse(PixelWorkoutWindow.Arg("-sologym-safe-inset","0"),NumberStyles.Float,CultureInfo.InvariantCulture,out inset);inset=Mathf.Clamp(inset,0,150);
            try{content=BossContent.Load();boss=content?.Pick(Controller?.Data.id);}catch(ArgumentException e){Debug.LogWarning(e.Message);}
            var bossStill=boss==null?null:Resources.Load<Sprite>("Game/Bosses/"+boss.id+"/idle");
            // The 3D summoning hall renders behind this overlay; the painted room is the fallback when it is missing.
            stage=PixelWorkoutWindow.Has("-sologym-flat-boss")?null:BossStage3D.Spawn();
            if(stage!=null)
            {
                stage.SetHero(Resources.Load<Sprite>("Characters/PixelLabR1/"+character),PixelSpriteLoop.Load("Game/Animations/"+character+"/idle"));
                stage.SetBoss(bossStill!=null?bossStill:Resources.Load<Sprite>("Rooms/BossR1/guardian"),null);
            }
            else
            {
                CoverBackdrop(root,"Rooms/BossR1/background",.45f);
                Art(Composition,new Rect(0,0,1280,720),"Rooms/BossR1/background");
                // Three depth rows: the boss stands behind and above, your hero in front. Full source canvases, no stretching.
                var bossImage=Art(Composition,new Rect(470,185,290,290),bossStill!=null?"Game/Bosses/"+boss.id+"/idle":"Rooms/BossR1/guardian");bossImage.preserveAspect=true;
                var heroImage=Art(Composition,new Rect(100,262,360,360),"Characters/PixelLabR1/"+character);heroImage.preserveAspect=true;
                bossLoop=PixelSpriteLoop.Attach(bossImage,bossImage.sprite);heroLoop=PixelSpriteLoop.Attach(heroImage,heroImage.sprite);
            }
            Render();Relayout();
            if(!restored)StartCoroutine(FlashIn(root));
        }
        /// <summary>Summoning flash: a white screen that fades to reveal the stand-off room.</summary>
        System.Collections.IEnumerator FlashIn(Transform canvasRoot)
        {
            var layer=Rect("Summon flash",canvasRoot,new Rect());Stretch(layer);var white=layer.gameObject.AddComponent<Image>();white.color=Color.white;white.raycastTarget=false;layer.SetAsLastSibling();
            for(float t=0;t<.8f;t+=Time.unscaledDeltaTime){white.color=new Color(1,1,1,1-t/.8f);yield return null;}
            Destroy(layer.gameObject);
        }
        /// <summary>The hero performs the current exercise and the boss mimics it, until the boss gives out on the last working set.</summary>
        void UpdateStage()
        {
            if(stage==null&&(heroLoop==null||bossLoop==null))return;
            bool active=Controller!=null&&View=="session"&&Controller.CanLog;
            string key=active?content?.Animation(Controller.Current.exercise_id):null;
            var hero=key==null?null:PixelSpriteLoop.Load("Game/Animations/"+character+"/"+key);
            var rival=key==null||boss==null||Controller.IsFinalMainSet?null:PixelSpriteLoop.Load("Game/Animations/"+boss.id+"/"+key);
            if(stage!=null){stage.PlayHero(hero);stage.PlayBoss(rival);}
            else{heroLoop.Play(hero);bossLoop.Play(rival);}
        }
        /// <summary>The line the boss speaks now, or empty. Lines come from the boss content data.</summary>
        /// <summary>Top-left of the speech frame in composition space: centred over the boss's head, kept on screen.</summary>
        Vector2 SpeechAnchor()
        {
            var head=stage!=null?stage.BossHeadScreen():null;
            if(head==null||Composition==null)return new Vector2(560,112);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Composition,head.Value,null,out var local);
            float x=Mathf.Clamp(local.x+640-210,290,850),y=Mathf.Clamp(360-local.y-80-14,104,300);
            return new Vector2(x,y);
        }
        string BossSpeech()
        {
            if(Controller==null||Controller.NeedsReadiness)return "";
            if(Controller.Closed)return Line(Controller.Data.state=="completed"?BossContent.DefeatedLine:BossContent.WonLine);
            if(Controller.CanLog&&Controller.IsFinalMainSet)return Line(BossContent.LastRepLine);
            return Controller.Data.logs.Length==0&&!Controller.Data.paused?Line(BossContent.ChallengeLine):"";
        }
        public void Relayout()
        {
            lastWidth=Screen.width;lastHeight=Screen.height;lastSafe=Screen.safeArea;
            var r=Screen.safeArea;float x=Mathf.Max(inset,r.xMin),y=Mathf.Max(inset,r.yMin),right=Mathf.Min(Screen.width-inset,r.xMax),top=Mathf.Min(Screen.height-inset,r.yMax);
            safe.anchorMin=new Vector2(x/Screen.width,y/Screen.height);safe.anchorMax=new Vector2(right/Screen.width,top/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            float scale=Mathf.Min((right-x)/1280,(top-y)/720);Composition.localScale=new Vector3(scale,scale,1);Canvas.ForceUpdateCanvases();
        }
        void Update()
        {
            if(Composition==null)return;
            if(lastWidth!=Screen.width||lastHeight!=Screen.height||lastSafe!=Screen.safeArea)Relayout();
            int seconds=Controller==null?0:(int)Math.Ceiling(Controller.RestLeft);
            if(seconds!=lastRest){lastRest=seconds;if(restLabel!=null)restLabel.text=Clock(seconds);if(seconds==0&&View=="session"&&Controller!=null&&!Controller.Data.paused)Render();}
            if(Input.GetKeyDown(KeyCode.Tab)&&EventSystem.current.currentSelectedGameObject==null)traversal.FirstOrDefault(s=>s.IsInteractable())?.Select();
            if(Input.GetKeyDown(KeyCode.Escape)){if(View!="session"){View="session";Render();}else if(Controller?.NeedsReadiness==true)Exited?.Invoke();else if(Controller!=null&&!Controller.Closed)Perform(()=>Controller.Pause());}
        }
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);}
        void OnApplicationPause(bool paused){if(paused&&Controller!=null&&!Controller.Closed&&!Controller.Data.paused&&!Controller.NeedsReadiness)Perform(()=>Controller.Pause());}
        public void Render()
        {
            var selected=EventSystem.current.currentSelectedGameObject;string focus=selected!=null?selected.name:null;EventSystem.current.SetSelectedGameObject(null);
            if(page!=null){page.gameObject.SetActive(false);Destroy(page.gameObject);}Controls.Clear();traversal.Clear();restLabel=null;
            page=Rect("Dungeon live controls",Composition,new Rect(0,0,1280,720));
            UpdateStage();UpdateBoard();
            // The room carries the routine (quest board) and the fighters; the overlay keeps to the edges and one action area.
            bool modal=Controller==null||View=="overview"||View=="correction"||View=="stop"||View=="finish"||View=="adjust"||Controller.NeedsReadiness||Controller.Closed||Controller.Data.paused;
            string speech=BossSpeech();
            if(speech!=""&&!modal){var at=SpeechAnchor();Frame(page,new Rect(at.x,at.y,420,80),"Boss speech");Text(page,new Rect(at.x+14,at.y+4,392,72),speech,21,false,TextAnchor.MiddleCenter);}
            if(modal)
            {
                var dim=Rect("Modal dim",page,new Rect(0,0,1280,720)).gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.45f);
                if(View=="overview")BuildOverview();
                else if(View=="correction")BuildCorrection();
                else
                {
                    var paper=Paper(new Rect(452,118,376,485));
                    if(Controller==null) {Title(paper,L("SAVE UNAVAILABLE","ARCHIVO NO DISPONIBLE"));Text(paper,new Rect(22,100,332,223),L("This saved session cannot be resumed safely. The original file has been kept. Return to the journal and recover the file before trying again.","No se puede reanudar esta sesión. El archivo original se conservó. Vuelve al diario y recupera el archivo antes de reintentar."),23);Button(paper,"exit-error",new Rect(22,398,332,62),L("BACK","VOLVER"),()=>Exited?.Invoke());}
                    else if(View=="stop")BuildStop(paper);
                    else if(View=="finish")BuildFinish(paper);
                    else if(Controller.NeedsReadiness)BuildRecheck(paper);
                    else if(Controller.Closed)BuildSummary(paper);
                    else if(Controller.Data.paused)BuildPaused(paper);
                    else BuildManual(paper);
                }
            }
            else if(Controller.RestLeft>0)BuildRest();
            else BuildDuel();
            if(View!="overview")Button(page,"overview",new Rect(1080,650,176,52),L("View routine","Ver rutina"),()=>{View="overview";Render();},false,18).interactable=Controller!=null;
            else Button(page,"overview",new Rect(1080,650,176,52),L("BACK","VOLVER"),()=>{View="session";Render();},false,18);
            // Edges: who you face (top left), difficulty and session controls (top right).
            var title=Frame(page,new Rect(24,18,372,74),"Stand-off title");
            Text(title,new Rect(14,6,344,36),L("DUEL","DUELO")+" · "+BossName.ToUpperInvariant(),27,false);
            Text(title,new Rect(14,42,344,26),(boss!=null?boss.stands_for.Get(Language)+" · ":"")+(Controller?.Data.plan.name.Get(Language)??"")+" · "+StateName(),17,false);
            var diff=Frame(page,new Rect(668,18,336,74),"Persistent difficulty");Text(diff,new Rect(10,2,316,22),L("DIFFICULTY","DIFICULTAD"),15,false,TextAnchor.MiddleCenter);
            var ids=new[]{"light","medium","hard"};var labels=new[]{L("Easy","Fácil"),L("Medium","Media"),L("Hard","Difícil")};
            for(int i=0;i<3;i++){string value=ids[i];Button(diff,"difficulty-"+value,new Rect(10+i*106,26,100,42),labels[i],()=>{if(Perform(()=>Controller.SetDifficulty(value))) {notice=Controller.Data.difficulty!=value?L("Readiness and experience limit this choice.","Tu estado y experiencia limitan esta opción."):L("Remaining work updated; records kept.","Trabajo pendiente ajustado; registros conservados.");Render();}},Controller?.Data.difficulty==value,18).interactable=Controller!=null&&!Controller.Closed&&!Controller.NeedsReadiness;}
            bool returning=Controller?.NeedsReadiness==true;
            Button(page,"pause",new Rect(1012,26,110,58),returning?L("BACK","VOLVER"):L("PAUSE","PAUSA"),()=>{if(returning)Exited?.Invoke();else Perform(()=>Controller.Pause());},false,19).interactable=Controller!=null&&!Controller.Closed&&(returning||!Controller.Data.paused);
            Button(page,"stop",new Rect(1130,26,126,58),L("STOP","TERMINAR"),()=>{View="stop";Render();},true,19).interactable=Controller!=null&&!Controller.Closed;
            string message=error!=null?L("Could not save or apply this change. Records kept; retry or lower the difficulty.","No se pudo guardar o aplicar el cambio. Registros conservados; reintenta o reduce la dificultad."):notice;
            if(!string.IsNullOrEmpty(message)&&!modal){var banner=Frame(page,new Rect(270,470,740,40),"Notice");Text(banner,new Rect(12,2,716,36),message,17,false,TextAnchor.MiddleCenter);}
            Text(page,new Rect(14,690,300,26),journal.IsLive?L("Your session","Tu sesión"):L("Sample data · manual log","Datos de ejemplo · registro manual"),15,false);
            for(int i=0;i<traversal.Count;i++){var prev=traversal[(i+traversal.Count-1)%traversal.Count];var next=traversal[(i+1)%traversal.Count];var tab=traversal[i].GetComponent<PixelFieldTabNavigation>()??traversal[i].gameObject.AddComponent<PixelFieldTabNavigation>();tab.Previous=prev;tab.Next=next;traversal[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=prev,selectOnDown=next,selectOnLeft=prev,selectOnRight=next};}
            if(focus!=null&&Controls.TryGetValue(focus,out var control)&&control.IsInteractable())control.Select();
        }
        /// <summary>The routine lives on the stone board in the room; the current exercise burns gold.</summary>
        void UpdateBoard()
        {
            var board=stage!=null?stage.board:null;if(board==null)return;
            if(Controller==null){board.SetHeader(L("TODAY'S SESSION","SESIÓN DE HOY"),"");board.SetRows(null,-1);return;}
            var blocks=Controller.Data.plan.blocks;var rows=new List<BossQuestBoard.Row>();int current=-1;var now=Controller.Closed?null:Controller.Current;
            for(int i=0;i<blocks.Length;i++){var b=blocks[i];rows.Add(new BossQuestBoard.Row{name=b.name.Get(Language),dose=(b.sets>1?b.sets+" × ":"")+Dose(b),done=BossSession.Count(Controller.Data,b.id)>=b.sets});if(now!=null&&b.id==now.id)current=i;}
            board.SetHeader(L("TODAY'S SESSION","SESIÓN DE HOY"),now==null?(Controller.Closed?StateName():""):RoleName(now.role));
            board.SetRows(rows,current);
        }
        string RoleName(string role)=>role=="warmup"?L("Warm up","Calentamiento"):role=="cooldown"?L("Recover","Recuperación"):L("Train","Entrenamiento");
        /// <summary>One line that shrinks to fit its box instead of being cut off.</summary>
        static Text Fit(Text t,int min,int max){t.resizeTextForBestFit=true;t.resizeTextMinSize=min;t.resizeTextMaxSize=max;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        /// <summary>Compact dose for the boss card: 3x10, 2x30 s, 3 min.</summary>
        string ShortDose(TrainingBlock b)
        {
            string q=b.quantity_max.ToString(CultureInfo.InvariantCulture);string unit=b.unit=="seconds"?" s":b.unit=="minutes"?" min":"";
            return b.sets>1?b.sets+"x"+q+unit:(b.unit=="reps"?q+" reps":q+unit);
        }
        /// <summary>Default view: you and the boss side by side on the same exercise, and one big COMPLETE.</summary>
        void BuildDuel()
        {
            var b=Controller.Current;if(b==null)return;
            var you=Frame(page,new Rect(250,518,330,104),"Your card");
            Text(you,new Rect(16,8,298,24),(b.role=="warmup"?L("YOU · WARM-UP","TÚ · CALENTAMIENTO"):b.role=="cooldown"?L("YOU · RECOVERY","TÚ · RECUPERACIÓN"):L("YOU","TÚ")),17,false).color=new Color32(240,190,90,255);
            string exercise=b.name.Get(Language);Fit(Text(you,new Rect(16,32,298,34),exercise,21,false),13,21);
            Fit(Text(you,new Rect(16,68,298,28),(b.sets>1?L("Set ","Serie ")+(BossSession.Count(Controller.Data,b.id)+1)+"/"+b.sets+" · ":"")+Dose(b),18,false),12,18);
            var vs=Frame(page,new Rect(592,528,96,84),"VS badge");Text(vs,new Rect(0,0,96,84),"VS",32,false,TextAnchor.MiddleCenter).color=new Color32(255,214,120,255);
            var rival=Frame(page,new Rect(700,518,330,104),"Boss card");
            Text(rival,new Rect(16,8,298,24),BossName.ToUpperInvariant(),17,false,TextAnchor.MiddleRight).color=new Color32(240,190,90,255);
            string mock=Controller.IsFinalMainSet?L("Gave up. All yours.","Se rindió. Todo tuyo."):content!=null?content.Mockery(b.exercise_id,ShortDose(b),b.name.Get(Language),Language):"";
            Text(rival,new Rect(16,32,298,64),mock,20,false,TextAnchor.MiddleRight);
            Button(page,"adjust",new Rect(250,640,190,56),L("Other amount","Otra cantidad"),()=>{View="adjust";Render();},false,18);
            Button(page,"complete",new Rect(450,630,380,74),L("COMPLETE","COMPLETAR"),()=>{notice=null;Perform(()=>Controller.CompleteSet());},true,32);
            Button(page,"finish",new Rect(840,640,190,56),L("Finish all","Terminar todo"),()=>{View="finish";Render();},false,18);
        }
        void BuildRest()
        {
            var card=Frame(page,new Rect(360,502,560,122),"Rest card");
            Text(card,new Rect(16,6,528,24),L("REST","DESCANSO"),17,false,TextAnchor.MiddleCenter).color=new Color32(240,190,90,255);
            restLabel=Text(card,new Rect(16,26,528,58),Clock((int)Math.Ceiling(Controller.RestLeft)),38,false,TextAnchor.MiddleCenter);
            var next=Controller.Current;Text(card,new Rect(16,88,528,28),next==null?"":L("Next: ","Sigue: ")+next.name.Get(Language)+" · "+ShortDose(next),17,false,TextAnchor.MiddleCenter);
            Button(page,"skip-rest",new Rect(450,630,380,74),L("I'M READY","ESTOY LISTO"),()=>Perform(()=>Controller.SkipRest()),true,30);
        }
        RectTransform Paper(Rect bounds)
        {
            var r=Frame(page,bounds,"Parchment workout card");
            var paper=Rect("Reusable journal parchment",r,new Rect(7,7,bounds.width-14,bounds.height-14)).gameObject.AddComponent<RawImage>();paper.texture=Resources.Load<Texture2D>("Rooms/WorkoutsR1/journal");paper.uvRect=new Rect(0,0,.46f,1);paper.raycastTarget=false;
            // Frame border remains above the shared parchment texture.
            var edge=Art(r,new Rect(0,0,bounds.width,bounds.height),"UI/Pixel/ContentPanel");edge.sprite=Resources.LoadAll<Sprite>("UI/Pixel/ContentPanel")[0];edge.type=Image.Type.Sliced;edge.fillCenter=false;edge.pixelsPerUnitMultiplier=3;return r;
        }
        void Title(Transform p,string value)=>Text(p,new Rect(18,20,340,40),value,30,true,TextAnchor.MiddleCenter);
        void BuildFinish(Transform p)
        {
            Title(p,L("FINISH ROUTINE?","¿TERMINAR RUTINA?"));
            Text(p,new Rect(26,80,324,150),L("Records every remaining set as done and wins the stand-off. Use it when you trained without tapping each set.","Registra como hechas todas las series restantes y ganas el duelo. Úsalo si entrenaste sin marcar cada serie."),22);
            Button(p,"confirm-finish",new Rect(24,250,328,72),L("YES, ALL DONE","SÍ, TODO HECHO"),()=>{View="session";Perform(()=>Controller.CompleteAll());},true,24);
            Button(p,"cancel-finish",new Rect(24,340,328,58),L("BACK","VOLVER"),()=>{View="session";Render();});
        }
        /// <summary>Optional: record a different quantity or a load.</summary>
        void BuildManual(Transform p)
        {
            var b=Controller.Current;if(b==null){View="session";return;}string id=Controller.NextId;
            if(inputSet!=id){inputSet=id;quantity=b.quantity_max.ToString(CultureInfo.InvariantCulture);load="";}
            Title(p,b.role=="warmup"?L("WARM-UP","CALENTAMIENTO"):b.role=="cooldown"?L("RECOVERY","RECUPERACIÓN"):L("YOUR SET","TU SERIE"));
            Text(p,new Rect(23,69,330,74),b.name.Get(Language),25,true,TextAnchor.MiddleCenter);
            Text(p,new Rect(22,148,332,50),L("Set ","Serie ")+(BossSession.Count(Controller.Data,b.id)+1)+" / "+b.sets+" · "+Dose(b),21,true,TextAnchor.MiddleCenter);
            Rule(p,23,207,330);
            Text(p,new Rect(24,214,328,28),b.unit=="reps"?L("Reps completed","Repeticiones realizadas"):b.unit=="seconds"?L("Seconds completed","Segundos realizados"):L("Minutes completed","Minutos realizados"),21);
            var field=Field(p,"quantity",new Rect(79,212,218,96),"",quantity,v=>quantity=v);field.Label.gameObject.SetActive(false);
            Button(p,"minus",new Rect(24,244,50,64),"−",()=>AdjustQuantity(-1));Button(p,"plus",new Rect(302,244,50,64),"+",()=>AdjustQuantity(1));
            if(b.role=="main")
            {
                Text(p,new Rect(24,324,170,62),L("Optional load\n(kg)","Peso opcional\n(kg)"),21);
                var weight=Field(p,"load",new Rect(196,292,156,96),"",load,v=>load=v);weight.Label.gameObject.SetActive(false);
                Text(p,new Rect(24,389,328,28),L("Rest between sets: ","Descanso entre series: ")+Clock(b.rest_seconds),19,true,TextAnchor.MiddleCenter);
            }
            else Text(p,new Rect(24,325,328,83),L("Move gently. This illustration is not an exercise demonstration.","Muévete suavemente. Esta ilustración no demuestra el ejercicio."),22);
            Button(p,"log",new Rect(23,418,212,53),L("RECORD","REGISTRAR"),()=>{if(ParseInputs(out var q,out var kg)){View="session";Perform(()=>Controller.Log(id,q,kg));}},true,22);
            Button(p,"cancel-adjust",new Rect(243,418,110,53),L("BACK","VOLVER"),()=>{View="session";Render();},false,19);
        }
        void BuildPaused(Transform p)
        {
            Title(p,L("PAUSED","EN PAUSA"));Text(p,new Rect(26,88,324,129),L("Your recorded work is saved. Difficulty can still be adjusted below.","Tu trabajo registrado está guardado. Puedes ajustar la dificultad abajo."),24);
            if(Controller.RestLeft>0)Text(p,new Rect(26,225,324,56),L("Rest remaining: ","Descanso restante: ")+Clock((int)Math.Ceiling(Controller.RestLeft)),24);
            Button(p,"resume",new Rect(24,290,328,59),L("RESUME","CONTINUAR"),()=>Perform(()=>Controller.Resume()),true);
            Button(p,"save-exit",new Rect(24,367,328,59),L("SAVE & RETURN","GUARDAR Y VOLVER"),()=>Exited?.Invoke());
        }
        void BuildStop(Transform p)
        {
            Title(p,L("STOP SESSION?","¿TERMINAR SESIÓN?"));Text(p,new Rect(26,81,324,168),L("Your records will be kept as an incomplete session. There is no catch-up debt. Stop if you have pain, injury or illness.","Tus registros se conservarán como sesión incompleta. No genera una deuda de ejercicio. Detente si hay dolor, lesión o enfermedad."),23);
            Button(p,"confirm-stop",new Rect(24,257,328,58),L("STOP & KEEP RECORDS","TERMINAR Y GUARDAR"),()=>{if(Perform(()=>Controller.Stop("user"))){View="session";Render();}},true,20);
            Button(p,"pain-stop",new Rect(24,330,328,58),L("PAIN / FEELING UNWELL","DOLOR / MALESTAR"),()=>{if(Perform(()=>Controller.Stop("pain_or_unwell"))){View="session";Render();}},false,20);
            Button(p,"cancel-stop",new Rect(24,405,328,58),L("BACK","VOLVER"),()=>{View="session";Render();});
        }
        void BuildRecheck(Transform p)
        {
            Title(p,L("WELCOME BACK","ANTES DE CONTINUAR"));Text(p,new Rect(24,78,328,77),L("How do you feel now? Saved sessions reopen paused.","¿Cómo te sientes ahora? Tu sesión se recuperó en pausa."),23);
            bool teen=Controller.IsTeen;
            if(teen)Button(p,"resume-supervision",new Rect(24,160,328,57),(supervised?"[x] ":"[ ] ")+L("Supervision available","Tengo supervisión"),()=>{supervised=!supervised;Render();},supervised,20);
            Button(p,"resume-ready",new Rect(24,230,328,55),L("Ready","Me siento bien"),()=>Perform(()=>Controller.Recheck("ready",!teen||supervised)),true).interactable=!teen||supervised;
            Button(p,"resume-light",new Rect(24,297,328,55),L("Low energy","Poca energía / fatiga"),()=>Perform(()=>Controller.Recheck("low_energy",!teen||supervised))).interactable=!teen||supervised;
            Button(p,"resume-stop",new Rect(24,368,328,73),teen?L("Unwell / no supervision: stop","Malestar / sin supervisión: terminar"):L("Pain, injury or illness: stop","Dolor, lesión o enfermedad: terminar"),()=>Perform(()=>Controller.Recheck("pain",false)),false,21);
        }
        void BuildSummary(Transform p)
        {
            bool completed=Controller.Data.state=="completed";Title(p,completed?L("SESSION FINISHED","SESIÓN FINALIZADA"):L("SESSION STOPPED","SESIÓN INTERRUMPIDA"));
            Text(p,new Rect(25,85,326,72),Controller.Data.logs.Length+L(" sets recorded"," series registradas"),25,true,TextAnchor.MiddleCenter);
            Text(p,new Rect(25,182,326,159),completed?(journal.IsLive?L("Warm-up, working sets and recovery recorded in your journal. No extra exercise is ever required.","Calentamiento, series y recuperación registrados en tu diario. Nunca se requiere ejercicio extra."):L("Warm-up, working sets and recovery recorded. No extra exercise is required. These local examples do not issue rewards.","Calentamiento, series y recuperación registrados. No necesitas ejercicio extra. Estos ejemplos locales no otorgan recompensas.")):L("Your work is saved, even when you stop early. Rest and take care of yourself. No extra exercise is owed.","Tu trabajo está guardado, aunque hayas terminado antes. Descansa y cuídate. No debes recuperar el ejercicio."),22);
            Button(p,"archive",new Rect(24,397,328,65),journal.IsLive?L("CONTINUE","CONTINUAR"):L("BACK TO WORKOUTS","VOLVER A RUTINAS"),Archive,true,21);
        }
        void BuildOverview()
        {
            var p=Paper(new Rect(194,160,880,440));Text(p,new Rect(27,18,825,42),L("ROUTINE & RECORDS","RUTINA Y REGISTROS"),30,true,TextAnchor.MiddleCenter);
            var s=Controller.Data;var text=s.plan.messages.Select(m=>m.text.Get(Language)).ToArray();float messages=text.Sum(t=>Mathf.Max(58,Mathf.Ceil(t.Length/83f)*25+12));
            var scroll=Scroll(p,new Rect(26,76,828,337),messages+s.plan.blocks.Length*90+s.logs.Length*70+86);float y=0;
            foreach(string message in text){float h=Mathf.Max(58,Mathf.Ceil(message.Length/83f)*25+12);Text(scroll,new Rect(4,y,795,h),message,21);y+=h;}
            foreach(var b in s.plan.blocks){Text(scroll,new Rect(4,y,780,38),b.name.Get(Language),23);Text(scroll,new Rect(4,y+38,780,40),b.sets+" × "+Dose(b)+" · "+L("Rest ","Descanso ")+Clock(b.rest_seconds),21);Rule(scroll,4,y+84,790);y+=90;}
            Text(scroll,new Rect(4,y,790,65),L("Recorded quantities can be corrected. Body appearance never sets your training.","Puedes corregir cantidades registradas. La apariencia nunca determina tu entrenamiento."),21);y+=76;
            foreach(var log in s.logs){string id=log.id;Text(scroll,new Rect(4,y,612,63),log.prescription.name.Get(Language)+" · "+(log.index+1)+"\n"+log.quantity.ToString("0.##",CultureInfo.InvariantCulture)+" "+Unit(log.prescription)+(log.hasLoad?" · "+log.loadKg.ToString("0.##",CultureInfo.InvariantCulture)+" kg":""),20);
                Button(scroll,"correct-"+id,new Rect(635,y+4,152,52),L("Correct","Corregir"),()=>{editingId=id;quantity=log.quantity.ToString("0.##",CultureInfo.InvariantCulture);load=log.hasLoad?log.loadKg.ToString("0.##",CultureInfo.InvariantCulture):"";View="correction";Render();},false,20);y+=70;}
        }
        void BuildCorrection()
        {
            var p=Paper(new Rect(448,164,376,431));Title(p,L("CORRECT RECORD","CORREGIR REGISTRO"));
            var l=Controller.Data.logs.First(x=>x.id==editingId);Text(p,new Rect(25,75,326,76),l.prescription.name.Get(Language)+" · "+(l.index+1),23);
            Field(p,"quantity",new Rect(24,154,328,96),Unit(l.prescription),quantity,v=>quantity=v);Field(p,"load",new Rect(24,262,328,96),L("Optional load (kg)","Peso opcional (kg)"),load,v=>load=v);
            Button(p,"save-correction",new Rect(24,364,328,54),L("SAVE CORRECTION","GUARDAR CORRECCIÓN"),()=>{if(ParseInputs(out var q,out var kg)&&Perform(()=>Controller.Correct(editingId,q,kg))){View="overview";inputSet="";Render();}},true,21);
        }
        void Archive(){try{journal.ArchiveBoss();Exited?.Invoke();}catch(Exception e){error=e.Message;Render();}}
        public bool Perform(Func<bool> operation){bool ok=operation();error=ok?null:Controller.Error;Render();return ok;}
        bool ParseInputs(out double q,out double? kg)
        {
            q=0;kg=null;double parsed;
            bool valid=double.TryParse(quantity.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out q)&&BossSession.Finite(q)&&q>=0&&q<=10000;
            if(!string.IsNullOrWhiteSpace(load)){valid&=double.TryParse(load.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out parsed);kg=parsed;valid&=BossSession.Finite(parsed)&&parsed>=0&&parsed<=1000;}
            if(!valid){notice=L("Enter a valid completed quantity (zero is allowed). Load is optional.","Indica una cantidad válida realizada (puede ser cero). El peso es opcional.");Render();}return valid;
        }
        void AdjustQuantity(int delta){double.TryParse(quantity,NumberStyles.Float,CultureInfo.InvariantCulture,out double v);quantity=Math.Max(0,Math.Min(10000,v+delta)).ToString("0.##",CultureInfo.InvariantCulture);Render();}
        PixelFormField Field(Transform p,string id,Rect bounds,string label,string value,Action<string> change)
        {var f=PixelFormField.Create(p,PixelFormField.Kind.Text,label,"—");Place((RectTransform)f.transform,bounds);f.SetLabelColor(Ink);f.Background.pixelsPerUnitMultiplier=2;f.Input.characterLimit=9;f.Input.keyboardType=TouchScreenKeyboardType.DecimalPad;f.SetValueWithoutNotify(value);f.Input.onValueChanged.AddListener(v=>change(v));f.Message.gameObject.SetActive(false);Register(id,f.Input);return f;}
        PixelJournalAction Button(Transform p,string id,Rect bounds,string label,Action action,bool selected=false,int size=22){var b=PixelJournalUI.Action(p,bounds,label,action,true,selected,size);Register(id,b);return b;}
        void Register(string id,Selectable s){s.name=id;Controls[id]=s;traversal.Add(s);}
        string StateName()=>Controller==null?L("Unavailable","No disponible"):Controller.Closed?L("Saved","Guardada"):Controller.Data.paused?L("Paused","En pausa"):L("In progress","En curso");
        string Unit(TrainingBlock b)=>(b.unit=="minutes"?"min":b.unit=="seconds"?"s":"reps")+(b.per_side?L(" / side (lower count)"," / lado (menor cantidad)"):"");
        string Dose(TrainingBlock b)=>b.quantity_min+(b.quantity_min==b.quantity_max?"":"–"+b.quantity_max)+" "+Unit(b);
        public static string Clock(int seconds)=>TimeSpan.FromSeconds(Math.Max(0,seconds)).ToString(@"m\:ss");
        public void Capture(string path){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,shot.EncodeToPNG());Destroy(shot);Debug.Log("BOSS_CAPTURE "+path);}
    }
}
