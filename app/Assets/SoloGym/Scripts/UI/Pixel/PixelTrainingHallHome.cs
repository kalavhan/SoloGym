using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Landscape Home: independent authored layers and live controls bound to existing Home state.</summary>
    public sealed class PixelTrainingHallHome : MonoBehaviour
    {
        [Serializable] public sealed class Appearance { public string id, resource, en, es; public Vector2 feet; }
        [Serializable] public sealed class Appearances { public Appearance[] characters; }
        [Serializable] public sealed class PropCatalog { public PropEntry[] objects; }
        [Serializable] public sealed class PropEntry { public string resource, en, es; }
        [Serializable] public sealed class LayoutSave { public int version = 1; public string roomId; public PixelRoomObject.State[] objects; }
        const string ArtRoot = "Rooms/TrainingHallR1/", CharacterRoot = "Characters/PixelLabR1/";
        const string CharacterKey = "SoloGym.PixelHall.Character.v1";
        public HomeController Controller { get; private set; }
        public PixelHomeRoom Room { get; private set; }
        public PixelNavigationBar Navigation { get; private set; }
        public PixelPrimaryButton TrainButton { get; private set; }
        public Image Hero { get; private set; }
        public string CharacterId { get; private set; }
        public RectTransform Composition { get; private set; }
        public readonly List<PixelRoomObject> Props = new List<PixelRoomObject>();
        public HomeNavigation LastRoute { get; private set; }
        public RectTransform Modal { get; private set; }
        public bool FastingVisible => Navigation.Tab("fasting").gameObject.activeSelf;
        public bool SmokeMode { get; private set; }
        RectTransform canvasRoot, safe, hud, modalShield;
        Image portrait;
        CanvasGroup homeGate;
        Text todayTitle, routine, userName, classLabel, title, fixture;
        PixelPrimaryButton decorate, settings;
        Appearance[] appearances;
        PropEntry[] propEntries;
        string language, layoutPath;
        string[] editSnapshot;
        Text editLabel;
        int editIndex, previousWidth, previousHeight;
        Rect previousSafe;
        bool fastingEnabled, review;
        float safeInset;
        Font font;
        string L(string en, string es) => language == "es" ? es : en;
        public static string Arg(string key, string fallback = "") => PixelButtonGallery.Argument(key, fallback);
        public static bool Has(string key) => PixelButtonGallery.HasArgument(key);

        void Awake()
        {
            Application.targetFrameRate = 60; Screen.orientation = ScreenOrientation.LandscapeLeft;
            SmokeMode = Has("-sologym-smoke"); review = Has("-sologym-review") || Has("-sologym-capture") || Application.isEditor;
            float.TryParse(Arg("-sologym-safe-inset", "0"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out safeInset);
            safeInset = Mathf.Clamp(safeInset, 0, 150);
            // Fasting is absent by default. This explicit review flag demonstrates the enabled adult state only.
            fastingEnabled = review && Has("-sologym-adult-fasting");
            font = Resources.Load<Font>("Fonts/PixelifySans");
            if (FindFirstObjectByType<Camera>() == null)
            { var cam = new GameObject("Home camera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(13,18,27,255); cam.cullingMask = 0; }
            if (EventSystem.current == null) new GameObject("Home input", typeof(EventSystem), typeof(StandaloneInputModule));
            canvasRoot = Rect("Fantasy Home canvas", transform);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            canvasRoot.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            safe = Rect("Safe area", canvasRoot); Composition = Rect("1280 x 720 composition", safe);
            Composition.anchorMin = Composition.anchorMax = Composition.pivot = new Vector2(.5f,.5f); Composition.sizeDelta = new Vector2(1280,720);
            var area = Rect("Room", Composition); Stretch(area);
            Room = PixelHomeRoom.Create(area, ArtRoot + "room");
            propEntries = JsonUtility.FromJson<PropCatalog>(Resources.Load<TextAsset>(ArtRoot + "objects").text).objects;
            foreach (var entry in propEntries)
            {
                var item = PixelRoomObject.Create(Room, entry.resource);
                if (item.ObjectId == "home.exercise-rug")
                {
                    item.Artwork.color = new Color(.58f, .45f, .55f, 1);
                    item.SetOverlay(Resources.Load<Sprite>(ArtRoot + "exercise-rug-trim"));
                }
                Props.Add(item);
            }
            appearances = JsonUtility.FromJson<Appearances>(Resources.Load<TextAsset>(CharacterRoot + "catalog").text).characters;
            Hero = Rect("Selected Barbarian — exact 256px export", Room.Objects).gameObject.AddComponent<Image>(); Hero.raycastTarget = false;
            hud = Rect("Live Home interface", Composition); Stretch(hud);
            BuildInterface();
            var seed = review ? new HomeSnapshot { UserName = "Aventurero" } : null;
            Controller = new HomeController(seed); Controller.Changed += Bind; Controller.NavigationRequested += Route;
            Controller.NoticeRequested += Notice; Controller.RetryRequested += RetryNotice;
            Controller.SetLanguage(Arg("-sologym-locale", Controller.Model.LanguagePreference));
            if (Has("-sologym-teen")) Controller.SetTeenProfile(true, false);
            SelectCharacter(Arg("-sologym-character", PlayerPrefs.GetString(CharacterKey, "male-medium")), false);
            layoutPath = Has("-sologym-layout-file") ? Arg("-sologym-layout-file") : Path.Combine(Application.persistentDataPath,"training-hall-layout-v1.json");
            if (!SmokeMode && !Has("-sologym-capture") && File.Exists(layoutPath))
            { try { ApplyLayout(File.ReadAllText(layoutPath)); } catch (Exception e) { Debug.LogWarning("Hall layout kept at defaults: " + e.Message); } }
            Bind(Controller.Model); Relayout();
        }
        void BuildInterface()
        {
            var controls = Rect("Home controls", hud); Stretch(controls);
            homeGate = controls.gameObject.AddComponent<CanvasGroup>();
            var titleFrame = Frame(controls, "Refuge plaque", new Rect(24,18,306,64));
            title = Text(titleFrame, new Rect(14,8,278,48), "MI REFUGIO", 32, TextAnchor.MiddleCenter);
            var profile = Frame(controls,"Profile",new Rect(976,16,280,78));
            portrait = Rect("Live portrait",profile).gameObject.AddComponent<Image>(); Place(portrait.rectTransform,new Rect(14,12,48,54)); portrait.preserveAspect = true; portrait.raycastTarget=false;
            userName = Text(profile,new Rect(72,8,142,34),"",22); classLabel = Text(profile,new Rect(72,40,142,30),"",20);
            settings = Action(profile,new Rect(218,11,52,56),"",ShowSettings,20);
            settings.SetIcon(Resources.LoadAll<Sprite>("UI/Pixel/IconSettings")[0]);
            settings.Content.offsetMin = new Vector2(10,8); settings.Content.offsetMax = new Vector2(-10,-8);
            settings.Icon.rectTransform.anchorMin=settings.Icon.rectTransform.anchorMax=new Vector2(.5f,.5f);settings.Icon.rectTransform.pivot=new Vector2(.5f,.5f);settings.Icon.rectTransform.anchoredPosition=Vector2.zero;
            settings.Icon.rectTransform.sizeDelta=new Vector2(28,28);
            var card = Frame(controls,"Today's routine",new Rect(963,480,297,180));
            todayTitle=Text(card,new Rect(18,12,260,30),"",25,TextAnchor.MiddleCenter);
            routine=Text(card,new Rect(20,45,258,56),"",20,TextAnchor.MiddleCenter);
            TrainButton=Action(card,new Rect(18,110,261,58),"",()=>Controller.ActivatePrimary(),27);
            decorate=Action(controls,new Rect(20,652,166,56),"",BeginDecorate,23);
            Frame(controls,"Navigation dock",new Rect(323,640,631,80));
            Navigation=PixelNavigationBar.Create(controls,new[]{"home","workouts","dungeon","fasting"},new[]{"Hogar","Rutinas","Mazmorra","Ayuno"},new[]{"home","workouts","dungeon"},"home");
            Place((RectTransform)Navigation.transform,new Rect(328,646,620,64));Navigation.SetLayoutMetrics(125,0);
            Navigation.onNavigate.AddListener(id=>{
                if(id=="fasting") { if(FastingVisible) Notice(L("The optional fasting area will be connected in its own iteration.","El área opcional de ayuno se conectará en su propia iteración.")); return; }
                Controller.OpenWindow(id=="workouts"?"WIN-015":"WIN-016");
            });
            fixture=Text(controls,new Rect(450,619,380,23),"",16,TextAnchor.MiddleCenter);
            foreach(var tab in Navigation.Tabs) {tab.Background.pixelsPerUnitMultiplier=2;tab.Label.fontSize=22;}
            Navigation.SetTraversal(decorate,TrainButton);
        }
        void Bind(HomeViewModel model)
        {
            language=model.Language;
            title.text=L("MY REFUGE","MI REFUGIO");userName.text=model.UserName;classLabel.text=L("Barbarian","Bárbaro");
            todayTitle.text=L("TODAY’S ROUTINE","RUTINA DE HOY");
            routine.text=model.ShowPlanSummary ? model.PlanName+"\n"+model.PlanDetail : model.PrimaryTitle;
            TrainButton.SetLabel(model.Mode==HomeMode.Training?L("TRAIN","ENTRENAR"):model.PrimaryAction);
            TrainButton.interactable=model.PrimaryEnabled; TrainButton.SetLoading(model.IsLoading,L("Loading…","Cargando…"));
            decorate.SetLabel(L("Decorate","Decorar"));
            Navigation.SetLabel("home",L("Home","Hogar"));Navigation.SetLabel("workouts",L("Workouts","Rutinas"));
            Navigation.SetLabel("dungeon",L("Dungeon","Mazmorra"));Navigation.SetLabel("fasting",L("Fasting","Ayuno"));
            Navigation.BindVisibleRoutes(fastingEnabled&&!model.IsPrivateProfile?new[]{"home","workouts","dungeon","fasting"}:new[]{"home","workouts","dungeon"},"home");
            fixture.text=model.FixtureNotice; // Existing data is fictional: do not present it as a live accepted plan.
            settings.gameObject.name=model.SettingsLabel;
        }
        public bool SelectCharacter(string id,bool persist=true)
        {
            var c=Array.Find(appearances,x=>x.id==id);if(c==null)return false;
            var sprite=Resources.Load<Sprite>(c.resource);if(sprite==null||sprite.rect.width>256||sprite.rect.height>256)return false;
            Hero.sprite=sprite;Hero.color=Color.white;CharacterId=id;
            var size=sprite.rect.size;float scale=192/(size.y-c.feet.y);
            PixelHomeRoom.Place(Hero.rectTransform,new Vector2(328,288),size*scale,new Vector2(c.feet.x/size.x,c.feet.y/size.y));
            // Crop metadata is applied to a separate portrait sprite; never regenerate the avatar.
            portrait.sprite=Resources.Load<Sprite>(CharacterRoot+id+"-portrait");
            if(persist&&!SmokeMode) {PlayerPrefs.SetString(CharacterKey,id);PlayerPrefs.Save();}
            return true;
        }
        void Route(HomeNavigation route)
        {
            LastRoute=route;if(SmokeMode)return;
            if((route.WindowId=="WIN-015"||route.WindowId=="WIN-016")&&string.IsNullOrEmpty(route.SessionId))
            {
                CloseModal();Composition.gameObject.SetActive(false);
                var training=new GameObject("Existing training flow").AddComponent<TrainingScreen>();
                training.Initialize(language,()=>{Controller.SetLanguage(training.Language);Destroy(training.gameObject);Composition.gameObject.SetActive(true);Screen.orientation=ScreenOrientation.LandscapeLeft;Relayout();},route.WindowId=="WIN-016"?"readiness":"training");return;
            }
            Notice(Controller.FutureWindowNotice(route.WindowId));
        }
        void RetryNotice()=>Notice(L("No live account service is connected in this local build.","Esta versión local aún no está conectada a un servicio de cuentas."));
        public void Notice(string message)
        {
            OpenModal(L("My refuge","Mi refugio"),580,300);
            Text(Modal,new Rect(30,66,520,142),message,24,TextAnchor.MiddleCenter);
            Action(Modal,new Rect(180,220,220,56),L("Back","Volver"),CloseModal,24);
        }
        public void ShowSettings()
        {
            OpenModal(L("Settings","Ajustes"),560,460);
            Action(Modal,new Rect(28,68,245,56),"Español",()=>{Controller.SetLanguage("es");ShowSettings();});
            Action(Modal,new Rect(286,68,245,56),"English",()=>{Controller.SetLanguage("en");ShowSettings();});
            Text(Modal,new Rect(30,135,500,30),L("Appearance · cosmetic only","Apariencia · solo cosmética"),23,TextAnchor.MiddleCenter);
            for(int i=0;i<appearances.Length;i++) {var c=appearances[i];Action(Modal,new Rect(28+(i%2)*258,178+(i/2)*52,245,46),language=="es"?c.es:c.en,()=>SelectCharacter(c.id),20);}
            Action(Modal,new Rect(165,394,230,52),L("Done","Listo"),CloseModal,24);
        }
        public void BeginDecorate()
        {
            CloseModal();editSnapshot=new string[Props.Count];for(int i=0;i<Props.Count;i++)editSnapshot[i]=Props[i].SaveState();editIndex=0;
            OpenModal(L("Arrange your gym","Organiza tu gimnasio"),470,274,false);
            // Keep most of the composition visible while inspecting changes.
            Modal.anchoredPosition=new Vector2(794,-346);
            editLabel=Text(Modal,new Rect(16,51,438,30),"",22,TextAnchor.MiddleCenter);
            Action(Modal,new Rect(20,86,66,46),"<",()=>CycleProp(-1),24);
            Action(Modal,new Rect(384,86,66,46),">",()=>CycleProp(1),24);
            Action(Modal,new Rect(94,86,112,46),L("Show/hide","Ver/ocultar"),()=>{Props[editIndex].SetVisible(!Props[editIndex].Visible);RefreshEditor();},20);
            Action(Modal,new Rect(214,86,76,46),"-",()=>AdjustProp(0,0,-.05f),24);
            Action(Modal,new Rect(298,86,76,46),"+",()=>AdjustProp(0,0,.05f),24);
            Action(Modal,new Rect(20,140,100,46),"X-",()=>AdjustProp(-4,0,0),24);Action(Modal,new Rect(130,140,100,46),"X+",()=>AdjustProp(4,0,0),24);
            Action(Modal,new Rect(240,140,100,46),"Y-",()=>AdjustProp(0,-4,0),24);Action(Modal,new Rect(350,140,100,46),"Y+",()=>AdjustProp(0,4,0),24);
            Action(Modal,new Rect(20,207,205,48),L("Cancel","Cancelar"),CancelDecorate,22);
            Action(Modal,new Rect(245,207,205,48),L("Save","Guardar"),SaveDecorate,22);RefreshEditor();
        }
        void CycleProp(int delta){editIndex=(editIndex+delta+Props.Count)%Props.Count;RefreshEditor();}
        void RefreshEditor(){if(editLabel!=null)editLabel.text=(language=="es"?propEntries[editIndex].es:propEntries[editIndex].en)+(Props[editIndex].Visible?"":" · "+L("hidden","oculto"));}
        public void AdjustProp(float x,float y,float scale)
        {
            var item=Props[editIndex];var next=item.PlacementState();next.position+=new Vector2(x,y);next.scale=Mathf.Clamp(next.scale+scale,.25f,3);
            try {item.RestoreState(JsonUtility.ToJson(next));}catch(ArgumentException){/* Keep last valid footprint. */}
        }
        public void CancelDecorate(){if(editSnapshot!=null)for(int i=0;i<Props.Count;i++)Props[i].RestoreState(editSnapshot[i]);editSnapshot=null;CloseModal();}
        public string ExportLayout(){var states=new PixelRoomObject.State[Props.Count];for(int i=0;i<Props.Count;i++)states[i]=Props[i].PlacementState();return JsonUtility.ToJson(new LayoutSave{roomId=Room.RoomId,objects=states},true);}
        public void ApplyLayout(string json)
        {
            if(string.IsNullOrEmpty(json)||json.Length>200000)throw new ArgumentException("Missing or oversized layout.");
            var data=JsonUtility.FromJson<LayoutSave>(json);if(data==null||data.version!=1||data.roomId!=Room.RoomId||data.objects==null||data.objects.Length!=Props.Count)throw new ArgumentException("Wrong layout version or room.");
            var seen=new HashSet<string>();var changes=new List<Action>();
            foreach(var state in data.objects){if(state==null||state.version!=2||!seen.Add(state.objectId))throw new ArgumentException("Duplicate or invalid object.");var item=Props.Find(x=>x.ObjectId==state.objectId);if(item==null)throw new ArgumentException("Unknown object.");changes.Add(item.PrepareRestore(JsonUtility.ToJson(state)));}
            foreach(var change in changes)change();
        }
        void SaveDecorate()
        {
            try {if(!SmokeMode){var dir=Path.GetDirectoryName(Path.GetFullPath(layoutPath));Directory.CreateDirectory(dir);File.WriteAllText(layoutPath+".tmp",ExportLayout());if(File.Exists(layoutPath))File.Replace(layoutPath+".tmp",layoutPath,null);else File.Move(layoutPath+".tmp",layoutPath);}editSnapshot=null;CloseModal();}
            catch(Exception e){Debug.LogError("Could not save hall layout: "+e.Message);editLabel.text=L("Could not save. Try again.","No se pudo guardar. Reintenta.");}
        }
        void OpenModal(string caption,float w,float h,bool dim=true)
        {
            CloseModal();homeGate.interactable=false;homeGate.blocksRaycasts=false;
            EventSystem.current.SetSelectedGameObject(null);
            Modal=Frame(hud,caption,new Rect((1280-w)/2,(720-h)/2,w,h));
            // A full-screen input shield prevents click-through while a dialog is open.
            {modalShield=Rect("Hall modal shield",hud);Stretch(modalShield);var image=modalShield.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,dim ? .55f : 0);image.raycastTarget=true;modalShield.SetSiblingIndex(Modal.GetSiblingIndex());}
            Text(Modal,new Rect(18,12,w-36,36),caption,27,TextAnchor.MiddleCenter);
            Canvas.ForceUpdateCanvases();
        }
        public void CloseModal()
        {
            if(Modal!=null){Modal.gameObject.SetActive(false);Destroy(Modal.gameObject);Modal=null;settings.Select();}
            if(homeGate!=null){homeGate.interactable=true;homeGate.blocksRaycasts=true;}
            if(modalShield!=null){modalShield.gameObject.SetActive(false);Destroy(modalShield.gameObject);modalShield=null;}
        }
        void Update()
        {
            if(Screen.width!=previousWidth||Screen.height!=previousHeight||Screen.safeArea!=previousSafe)Relayout();
            if(Input.GetKeyDown(KeyCode.Escape)&&Modal!=null){if(editSnapshot!=null)CancelDecorate();else CloseModal();}
        }
        public void Relayout()
        {
            previousWidth=Screen.width;previousHeight=Screen.height;previousSafe=Screen.safeArea;
            var s=Screen.safeArea;var min=new Vector2(Mathf.Max(s.xMin,safeInset),Mathf.Max(s.yMin,safeInset));var max=new Vector2(Mathf.Min(s.xMax,Screen.width-safeInset),Mathf.Min(s.yMax,Screen.height-safeInset));
            safe.anchorMin=new Vector2(min.x/Screen.width,min.y/Screen.height);safe.anchorMax=new Vector2(max.x/Screen.width,max.y/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            float scale=Mathf.Max(.01f,Mathf.Min((max.x-min.x)/1280,(max.y-min.y)/720));Composition.localScale=new Vector3(scale,scale,1);
            Canvas.ForceUpdateCanvases();Room.RefreshLayout();Navigation.RefreshLayout();Canvas.ForceUpdateCanvases();
        }
        IEnumerator Start()
        {
            for(int i=0;i<8;i++)yield return null;
            if(SmokeMode) yield return gameObject.AddComponent<PixelTrainingHallSmoke>().Run(this);
            string path=Arg("-sologym-capture");if(!string.IsNullOrEmpty(path))
            {CloseModal();yield return null;yield return new WaitForEndOfFrame();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,shot.EncodeToPNG());Destroy(shot);Debug.Log("TRAINING_HALL_CAPTURE "+path);if(!Has("-sologym-stay-open"))Application.Quit();}
        }
        RectTransform Frame(Transform parent,string name,Rect rect)
        {
            var r=Rect(name,parent);Place(r,rect);
            var fill=r.gameObject.AddComponent<Image>();fill.sprite=Resources.Load<Sprite>(ArtRoot+"ui-frame");fill.raycastTarget=false;
            var border=Rect("Reusable gold frame",r);Stretch(border);
            var art=border.gameObject.AddComponent<Image>();art.sprite=Resources.LoadAll<Sprite>("UI/Pixel/ContentPanel")[0];
            art.type=Image.Type.Sliced;art.fillCenter=false;art.pixelsPerUnitMultiplier=3;art.raycastTarget=false;return r;
        }
        PixelPrimaryButton Action(Transform parent,Rect rect,string label,UnityEngine.Events.UnityAction callback,int size=22)
        {var b=PixelPrimaryButton.Create(parent,label,callback);Place((RectTransform)b.transform,rect);b.Background.pixelsPerUnitMultiplier=2;b.SetFontSize(size);
            if(Modal!=null&&b.transform.IsChildOf(Modal)&&EventSystem.current.currentSelectedGameObject==null)b.Select();
            return b;}
        Text Text(Transform parent,Rect rect,string value,int size,TextAnchor align=TextAnchor.MiddleLeft)
        {var r=Rect(value,parent);Place(r,rect);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color32(255,233,191,255);t.alignment=align;t.text=value;t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,Rect b){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(b.x,-b.y);r.sizeDelta=b.size;}
        void OnDestroy(){Controller?.Dispose();}
    }
}
