using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Two approved windows assembled from shared artwork and live controls.</summary>
    public sealed class OnboardingScreen : MonoBehaviour
    {
        const float W = 853, H = 1844;
        static Color Silver => SystemUI.Theme.text;
        static Color Cyan => SystemUI.Theme.accent;
        RectTransform root, page, modal;
        Font serif, bold, body;
        OnboardingController controller;
        PrivateProfileScreen profile;
        GoalsExperienceScreen goals;
        EquipmentScreen equipment;
        int profileReturnFrame = -1;
        InputField ageInput;
        Text status, countryValue, regionValue, privacyCheck, termsCheck, continueText;
        Button continueButton;
        Action closed;
        bool rebuilding, documentOnly;
        string capture;
        Vector2 lastSize;
        Rect lastSafe;
        bool lastKeyboard;
        float lastKeyboardHeight;
        readonly Dictionary<string, Button> controls = new Dictionary<string, Button>();

        public void Initialize(string language, bool review, Action onClose, string document = null, string capturePath = null)
        {
            closed = onClose; documentOnly = document != null; capture = capturePath;
            serif = SystemUI.Theme.heading;
            bold = SystemUI.Theme.headingBold;
            body = SystemUI.Theme.body;
            controller = new OnboardingController(review);
            controller.SetLanguage(language);
            controller.ExitRequested += Exit;
            controller.CheckpointRequested += id =>
            {
                if (id == "REVIEW:WIN-009" && controller.Model.ReviewMode) OpenProfile();
            };
            var canvasNode = new GameObject("Onboarding UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasNode.transform.SetParent(transform, false);
            var canvas = canvasNode.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; canvas.pixelPerfect = true;
            root = RectNode("Reference 853x1844", canvasNode.transform, new Rect(0, 0, W, H));
            controller.Changed += Render;
            if (document != null) controller.ReadDocument(document);
            else if (Argument("-sologym-window") == "consent" && review) controller.ContinueAge();
            Render(controller.Model); Fit();
            if (Argument("-sologym-window") == "profile" && review)
                OpenProfile(capture);
            else if (Argument("-sologym-window") == "goals" && review)
                OpenGoals(capture);
            else if (Argument("-sologym-window") == "equipment" && review)
                OpenEquipment(capture);
            else if (capture != null) StartCoroutine(Capture());
        }

        void OpenProfile(string capturePath = null)
        {
            if (!controller.Model.ReviewMode) return;
            KeyboardOff(); CloseModal(); root.gameObject.SetActive(false);
            if (profile != null)
            {
                profile.Resume(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"));
                return;
            }
            var node = new GameObject("Private fitness profile");
            node.transform.SetParent(transform, false);
            profile = node.AddComponent<PrivateProfileScreen>();
            profile.Initialize(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"), true, () =>
            {
                root.gameObject.SetActive(true);
                // The same Escape press must not also back out of this parent screen.
                profileReturnFrame = Time.frameCount;
                controller.SetLanguage(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"));
            }, () => controller.Decline(), capturePath, id =>
            {
                if (id == "REVIEW:WIN-010") OpenGoals();
            });
        }

        void OpenGoals(string capturePath = null)
        {
            if (!controller.Model.ReviewMode) return;
            KeyboardOff();
            CloseModal();
            if (profile != null) profile.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            bool teen = Argument("-sologym-audience") == "teen" || controller.IsTeenAudience();
            string step = Argument("-sologym-goals-view");
            if (goals != null)
            {
                goals.Resume(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"));
                return;
            }
            var node = new GameObject("Goals and experience");
            node.transform.SetParent(transform, false);
            goals = node.AddComponent<GoalsExperienceScreen>();
            goals.Initialize(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"), true, teen, () =>
            {
                goals.gameObject.SetActive(false);
                if (profile != null) profile.gameObject.SetActive(true);
                else root.gameObject.SetActive(true);
            }, () => controller.Decline(), id =>
            {
                if (id == "REVIEW:WIN-011") OpenEquipment();
            }, capturePath, step);
        }

        void OpenEquipment(string capturePath = null)
        {
            if (!controller.Model.ReviewMode) return;
            KeyboardOff();
            CloseModal();
            if (goals != null) goals.gameObject.SetActive(false);
            if (profile != null) profile.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            string step = Argument("-sologym-equipment-view");
            if (equipment != null)
            {
                equipment.Resume(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"));
                return;
            }
            var node = new GameObject("Available equipment");
            node.transform.SetParent(transform, false);
            equipment = node.AddComponent<EquipmentScreen>();
            equipment.Initialize(PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto"), true, () =>
            {
                equipment.gameObject.SetActive(false);
                if (goals != null) goals.gameObject.SetActive(true);
                else root.gameObject.SetActive(true);
            }, () => controller.Decline(), id =>
            {
                if (id != "REVIEW:WIN-012") return;
                KeyboardOff();
                Notice(L("Next step", "Siguiente paso"),
                    L("Schedule and session time is the next window. Nothing was saved to your account.",
                        "El horario y la duración de sesión es la siguiente ventana. No se guardaron datos en tu cuenta."));
            }, capturePath ?? (Argument("-sologym-window") == "equipment" ? capture : null), step);
        }

        void Render(OnboardingViewModel model)
        {
            // Editing a numeric field must not recreate its native keyboard or selection.
            if (!rebuilding && page != null && model.Step == OnboardingStep.AgeRegion && ageInput != null &&
                ageInput.isFocused)
            {
                if (status != null) status.text = model.Error;
                return;
            }
            rebuilding = true;
            CloseModal();
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = RectNode("Onboarding page", root, new Rect(0, 0, W, H));
            controls.Clear(); ageInput = null;
            bool consent = model.Step == OnboardingStep.Consent;
            bool document = model.Step == OnboardingStep.Document;
            SystemUI.PortalPage(page,718,965);
            SystemUI.Icon(page,new Rect(43,47,28,34),"back",Silver);
            SystemUI.Divider(page,807);
            Live("Back", new Rect(92, 53, L(61, 83), 25), L("Back", "Volver"), 31);
            Live("Language", new Rect(765, 56, 29, 20), model.Language.ToUpperInvariant(), 27);
            Hit("back", new Rect(30, 20, 180, 100), () => { if (documentOnly) Exit(); else controller.Back(); });
            Hit("language", new Rect(680, 20, 130, 100), Language);
            if (consent) ConsentPage(); else AgePage();
            // The review marker is always visible in review mode, including captures.
            if (model.ReviewMode)
                TextAt(page, new Rect(90, 280, 673, 35), L("PREVIEW · FICTIONAL DATA · NOT SAVED", "VISTA PREVIA · DATOS FICTICIOS · SIN GUARDAR"), 20, body, Cyan, TextAnchor.MiddleCenter);
            if (document) Document();
            rebuilding = false;
        }



        void AgePage()
        {
            var m = controller.Model;
            SystemUI.InputFrame(page,new Rect(102,1025,651,80),511);
            SystemUI.Panel(page,new Rect(102,1204,651,93),PanelStyle.Input);
            SystemUI.Panel(page,new Rect(102,1357,651,93),PanelStyle.Input);
            SystemUI.Icon(page,new Rect(706,1236,24,24),"down",Silver);
            SystemUI.Icon(page,new Rect(706,1389,24,24),"down",Silver);
            SystemUI.Panel(page,new Rect(91,1543,671,100),PanelStyle.Primary);
            SystemUI.Icon(page,new Rect(421,1728,10,25),"sigil",Silver);
            Live("Step", new Rect(323,771,210,15), L("S T E P  1  ·  P R O F I L E", "P A S O  1  ·  P E R F I L"),24,Cyan);
            Live("Title", new Rect(L(183,199),836,L(487,455),61),L("YOUR ORIGIN","TU ORIGEN"),78,Silver,bold);
            Live("Subtitle", new Rect(L(179,228),923,L(495,397),33),L("Age and country of residence","Edad y país de residencia"),39);
            Live("Age label",new Rect(105,984,L(51,63),26),L("Age","Edad"),31);
            var field = RectNode("Age numeric input", page, new Rect(103,1025,508,75));
            var target=field.gameObject.AddComponent<Image>();target.color=Color.clear;
            ageInput=field.gameObject.AddComponent<InputField>();ageInput.targetGraphic=target;
            var value=TextAt(field,new Rect(34,0,455,75),"",40,serif,Silver);
            ageInput.textComponent=value;ageInput.contentType=InputField.ContentType.IntegerNumber;
            ageInput.keyboardType=TouchScreenKeyboardType.NumberPad;ageInput.characterLimit=3;
            ageInput.customCaretColor=true;ageInput.caretColor=Cyan;ageInput.selectionColor=new Color(.1f,.6f,1,.4f);

            var placeholder=TextAt(field,new Rect(34,0,455,75),L("Enter age","Tu edad"),32,serif,new Color(.6f,.7f,.8f));
            ageInput.placeholder=placeholder;
            ageInput.SetTextWithoutNotify(m.AgeText);ageInput.ForceLabelUpdate();
            ageInput.onValueChanged.AddListener(controller.SetAge);
            // Keep the page alive when tapping another control. Rebuilding on end
            // edit destroys that control between pointer down and pointer up.
            Live("Years",new Rect(652,1053,65,26),L("years","años"),34);
            Live("Minimum",new Rect(106,1117,L(325,469),22),L("SoloGym is for ages 15 and up.","SoloGym es para personas de 15 años o más."),26);
            Live("Country label",new Rect(105,1174,L(266,218),27),L("Country of residence","País de residencia"),31);
            countryValue=TextAt(page,new Rect(138,1222,540,62),string.IsNullOrEmpty(m.CountryLabel)?L("Select","Seleccionar"):m.CountryLabel,40,serif,Silver);
            Live("Region label",new Rect(105,1326,L(320,346),26),L("State / province (optional)","Estado / provincia (opcional)"),31);
            regionValue=TextAt(page,new Rect(138,1375,540,62),string.IsNullOrEmpty(m.SubdivisionLabel)?L("Select","Seleccionar"):m.SubdivisionLabel,37,serif,Silver);
            Live("Private cue",new Rect(L(260,265),1491,L(372,366),22),L("Your age is private. We do not use GPS.","Tu edad es privada. No usamos GPS."),26);
            continueText=Live("Continue",new Rect(L(312,294),1576,L(228,264),34),L("CONTINUE","CONTINUAR"),45,Silver,bold);
            Live("Privacy",new Rect(L(276,270),1735,L(99,118),24),L("Privacy","Privacidad"),29);
            Live("Terms",new Rect(L(482,469),1735,L(77,102),24),L("Terms","Términos"),29);
            Hit("country",new Rect(99,1203,656,97),()=>Picker(false));
            Hit("region",new Rect(99,1357,656,94),()=>Picker(true));
            continueButton=Hit("continue",new Rect(91,1543,671,100),()=>{KeyboardOff();controller.ContinueAge();if(!string.IsNullOrEmpty(controller.Model.Error)) Notice(L("Before continuing","Antes de continuar"),controller.Model.Error);});
            Hit("privacy",new Rect(235,1705,177,82),()=>{KeyboardOff();controller.ReadDocument("privacy");});
            Hit("terms",new Rect(450,1705,165,82),()=>{KeyboardOff();controller.ReadDocument("terms");});
            status=TextAt(page,new Rect(105,1651,642,25),m.Error,18,body,new Color32(255,204,163,255),TextAnchor.MiddleCenter);
        }

        void ConsentPage()
        {
            var m=controller.Model;
            SystemUI.Panel(page,new Rect(102,L(1122,1164),651,78),PanelStyle.Outline);
            SystemUI.Panel(page,new Rect(102,L(1207,1249),651,80),PanelStyle.Outline);
            SystemUI.Panel(page,new Rect(108,L(1313,1355),36,36),PanelStyle.Outline);
            SystemUI.Panel(page,new Rect(108,L(1380,1422),36,36),PanelStyle.Outline);
            var primary=SystemUI.Panel(page,new Rect(91,1539,671,94),PanelStyle.Primary);
            primary.color=m.CanContinue?Color.white:new Color(.5f,.6f,.7f,.7f);

            Live("Step",new Rect(289,771,276,15),L("S T E P  2  ·  P R I V A C Y","P A S O  2  ·  P R I V A C I D A D"),23,Cyan);
            Live("Title 1",new Rect(L(205,165),833,L(446,529),51),L("YOUR PRIVACY","TU PRIVACIDAD"),66,Silver,bold);
            Live("Title 2",new Rect(L(174,160),901,L(505,535),51),L("YOUR CHOICES","TUS DECISIONES"),66,Silver,bold);
            Live("Subtitle",new Rect(L(218,228),973,L(418,398),32),L("You choose what to share","Tú decides qué compartir"),37);
            var paragraph=TextAt(page,new Rect(108,1023,645,127),L("Your age and workout data are private.\nRankings and social features are set up later.","Tu edad y tus datos de entrenamiento son\nprivados.\nEl ranking y las funciones sociales se configuran\ndespués."),31,serif,Silver,TextAnchor.UpperLeft);
            paragraph.horizontalOverflow=HorizontalWrapMode.Wrap;paragraph.lineSpacing=.9f;
            Live("Privacy document",new Rect(135,L(1149,1191),L(288,433),30),L("Read privacy policy","Leer política de privacidad"),36);
            Live("Terms document",new Rect(135,L(1235,1277),L(275,351),30),L("Read terms of use","Leer términos de uso"),36);
            Live("Privacy acknowledgement",new Rect(175,L(1322,1364),L(387,402),23),L("I have read the privacy policy.","He leído la política de privacidad."),29);
            Live("Terms acceptance",new Rect(175,L(1390,1432),L(300,335),23),L("I accept the terms of use.","Acepto los términos de uso."),29);
            Live("Optional",new Rect(107,L(1458,1488),L(366,394),22),L("No optional permissions enabled.","Sin permisos opcionales activados."),26,new Color32(190,203,227,255));
            continueText=Live("Continue",new Rect(L(312,294),1572,L(228,264),34),L("CONTINUE","CONTINUAR"),45,m.CanContinue?Silver:new Color32(153,188,225,255),bold);
            Live("Decline",new Rect(L(376,367),1650,L(100,120),27),L("Not now","Ahora no"),33,Cyan);
            Live("Footer",new Rect(L(184,215),1756,L(485,418),22),L("You can review your choices in Settings.","Puedes revisar tus decisiones en Ajustes."),26);
            privacyCheck=TextAt(page,new Rect(108,L(1305,1347),37,50),m.PrivacyAcknowledged?"✓":"",40,body,Cyan,TextAnchor.MiddleCenter);
            termsCheck=TextAt(page,new Rect(108,L(1372,1414),37,50),m.TermsAccepted?"✓":"",40,body,Cyan,TextAnchor.MiddleCenter);
            Hit("privacy_document",new Rect(101,L(1122,1164),652,78),()=>controller.ReadDocument("privacy"));
            Hit("terms_document",new Rect(101,L(1207,1249),652,80),()=>controller.ReadDocument("terms"));
            Hit("privacy_check",new Rect(101,L(1294,1336),652,66),()=>controller.SetPrivacyAcknowledged(!controller.Model.PrivacyAcknowledged));
            Hit("terms_check",new Rect(101,L(1362,1404),652,66),()=>controller.SetTermsAccepted(!controller.Model.TermsAccepted));
            continueButton=Hit("continue",new Rect(91,1539,671,94),()=>controller.ContinueConsent());
            continueButton.interactable=m.CanContinue;
            Hit("decline",new Rect(285,1636,283,63),controller.Decline);
            status=TextAt(page,new Rect(110,1511,635,22),m.Error,17,body,new Color32(255,204,163,255),TextAnchor.MiddleCenter);
        }

        void KeyboardOff(){ if(ageInput!=null){ ageInput.DeactivateInputField(); EventSystem.current?.SetSelectedGameObject(null); } }
        void Picker(bool region)
        {
            KeyboardOff();
            var options=region?controller.Subdivisions:controller.Countries;
            if(options.Length==0){Notice(L("Region","Región"),L("Choose a country first. Regional selection is optional and may not be available yet.","Elige un país primero. La región es opcional y puede no estar disponible aún."));return;}
            OpenModal(region?L("State / province","Estado / provincia"):L("Country of residence","País de residencia"));
            var viewport=RectNode("Scrollable choices",modal,new Rect(25,118,660,820));
            viewport.gameObject.AddComponent<Image>().color=new Color32(4,18,34,255);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;
            int count=options.Length+(region?1:0);
            var content=RectNode("Choices",viewport,new Rect(0,0,650,count*82));scroll.content=content;scroll.viewport=viewport;
            int index=0;
            if(region){ModalButton(content,new Rect(6,0,636,78),L("Skip region","Omitir región"),()=>{controller.SelectSubdivision("");CloseModal();});index++;}
            foreach(var option in options){var opt=option;ModalButton(content,new Rect(6,index++*82,636,78),opt.Label(controller.Model.Language),()=>{if(region)controller.SelectSubdivision(opt.Code);else controller.SelectCountry(opt.Code);CloseModal();});}
        }
        void Language()
        {
            KeyboardOff();OpenModal(L("Language","Idioma"));
            ModalButton(modal,new Rect(30,140,650,90),"English",()=>controller.SetLanguage("en"));
            ModalButton(modal,new Rect(30,245,650,90),"Español",()=>controller.SetLanguage("es"));
            ModalButton(modal,new Rect(30,350,650,90),L("Use device language","Usar idioma del dispositivo"),()=>controller.SetLanguage("auto"));
        }
        void Document()
        {
            OpenModal(controller.Model.DocumentTitle);
            var view=RectNode("Document viewport",modal,new Rect(35,120,640,750));
            view.gameObject.AddComponent<Image>().color=new Color32(4,18,34,255);view.gameObject.AddComponent<RectMask2D>();
            var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;
            var content=RectNode("Document content",view,new Rect(0,0,630,750));
            var text=TextAt(content,new Rect(0,0,622,750),controller.Model.DocumentBody,28,body,Silver,TextAnchor.UpperLeft);
            text.horizontalOverflow=HorizontalWrapMode.Wrap;
            Canvas.ForceUpdateCanvases();float height=Mathf.Max(750,text.preferredHeight+30);content.sizeDelta=new Vector2(630,height);text.rectTransform.sizeDelta=new Vector2(622,height);
            scroll.viewport=view;scroll.content=content;
            ModalButton(modal,new Rect(35,890,640,80),L("Back","Volver"),()=>{if(documentOnly)Exit();else controller.Back();});
        }
        void Notice(string title,string message)
        {
            OpenModal(title);var text=TextAt(modal,new Rect(35,140,640,610),message,30,body,Silver,TextAnchor.UpperLeft);text.horizontalOverflow=HorizontalWrapMode.Wrap;
            ModalButton(modal,new Rect(35,890,640,80),L("Close","Cerrar"),CloseModal);
        }
        void OpenModal(string title)
        {
            CloseModal();KeyboardOff();
            var shade=RectNode("Modal shade",root,new Rect(0,0,W,H));shade.gameObject.AddComponent<Image>().color=new Color(0,.01f,.04f,.86f);
            modal=RectNode("System dialog",shade,new Rect(71,420,711,1000));var frame=modal.gameObject.AddComponent<SystemPanel>();frame.theme=SystemUI.Theme;frame.ornaments=true;
            TextAt(modal,new Rect(32,24,565,70),title,33,bold,Silver);
            ModalButton(modal,new Rect(611,12,80,90),"×",()=>{if(controller.Model.Step==OnboardingStep.Document){if(documentOnly)Exit();else controller.Back();}else CloseModal();});
        }
        void CloseModal(){if(modal==null)return;var old=modal.parent.gameObject;modal=null;old.SetActive(false);Destroy(old);}
        void Exit(){KeyboardOff();closed?.Invoke();Destroy(gameObject);}
        string L(string en,string es)=>controller.Model.Language=="es"?es:en;
        float L(float en,float es)=>controller.Model.Language=="es"?es:en;

        Text Live(string name,Rect rect,string content,int size,Color? color=null,Font font=null)
        {
            var text=SystemUI.Caption(page,new Rect(rect.x,rect.y-8,rect.width,rect.height+16),content,size,font??serif,color??Silver);text.name=name;
            if(name.StartsWith("Title",StringComparison.Ordinal))text.gameObject.AddComponent<OnboardingSilverText>();
            return text;
        }

        static RectTransform RectNode(string name,Transform parent,Rect rect)
        {return SystemUI.Node(name,parent,rect);}
        Text TextAt(Transform parent,Rect rect,string value,int size,Font font,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {return SystemUI.Text(parent,rect,value,size,font,color,align);}
        Button Hit(string key,Rect rect,Action action)
        {
            var node=RectNode(key,page,rect);var image=node.gameObject.AddComponent<Image>();image.color=Color.clear;
            var button=node.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>action());controls[key]=button;return button;
        }
        void ModalButton(Transform parent,Rect rect,string label,Action action)
        {SystemUI.Button(parent,rect,label,action);}

        void Update()
        {
            if(root==null||(profile!=null&&profile.gameObject.activeSelf)||Time.frameCount==profileReturnFrame)return;
            if(lastSize.x!=Screen.width||lastSize.y!=Screen.height||lastSafe!=Screen.safeArea||lastKeyboard!=TouchScreenKeyboard.visible||lastKeyboardHeight!=TouchScreenKeyboard.area.height)Fit();
            if(Input.GetKeyDown(KeyCode.Escape)){if(controller.Model.Step==OnboardingStep.Document){if(documentOnly)Exit();else controller.Back();}else if(modal!=null)CloseModal();else controller.Back();}
        }
        void Fit()
        {
            var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/W,safe.height/H);float top=Screen.height-safe.yMax+(safe.height-H*scale)/2;float lift=0;
            if(TouchScreenKeyboard.visible&&ageInput!=null){float occlusion=TouchScreenKeyboard.area.height>0?TouchScreenKeyboard.area.yMax:Screen.height*.42f;lift=Mathf.Max(0,occlusion+20*scale-(Screen.height-top-1110*scale));}
            SystemViewport.Fit(root,W,H,lift);
            lastSize=new Vector2(Screen.width,Screen.height);lastSafe=safe;lastKeyboard=TouchScreenKeyboard.visible;lastKeyboardHeight=TouchScreenKeyboard.area.height;
        }
        IEnumerator Capture()
        {
            for(int i=0;i<8;i++)yield return null;
            Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture)));
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(capture,image.EncodeToPNG());Destroy(image);
            Debug.Log("SOLOGYM_CAPTURE "+capture);
            if(HasArgument("-sologym-smoke"))yield return Smoke();
            Application.Quit();
        }
        IEnumerator Smoke()
        {
            bool passed=true;
            try { Debug.Log("SOLOGYM_ONBOARDING_STATE " + OnboardingStateChecks.Run()); }
            catch (Exception exception) { passed=false; Debug.LogError(exception.Message); }
            if(controller.Model.Step==OnboardingStep.Consent)controller.Back();
            controller.SetAge("14");controls["continue"].onClick.Invoke();yield return null;
            passed &= controller.Model.Step==OnboardingStep.AgeRegion&&!string.IsNullOrEmpty(controller.Model.Error);
            CloseModal();controller.SetAge("21");controller.SelectCountry("MX");controls["country"].onClick.Invoke();yield return null;passed &= modal!=null;CloseModal();
            controls["continue"].onClick.Invoke();yield return null;passed &= controller.Model.Step==OnboardingStep.Consent&&!continueButton.interactable;
            controls["privacy_document"].onClick.Invoke();yield return null;passed &= controller.Model.Step==OnboardingStep.Document&&!controller.Model.DocumentAvailable;
            controller.Back();controls["privacy_check"].onClick.Invoke();controls["terms_check"].onClick.Invoke();yield return null;passed &= controller.Model.PrivacyAcknowledged&&controller.Model.TermsAccepted;
            controller.SetLanguage("en");yield return null;passed &= controller.Model.PrivacyAcknowledged&&controller.Model.Language=="en";
            controller.Back();yield return null;passed &= controller.Model.AgeText=="21"&&controller.Model.CountryCode=="MX";
            using(var real=new OnboardingController(false)){real.SetAge("21");real.SelectCountry("MX");real.ContinueAge();passed &= real.Model.Step==OnboardingStep.AgeRegion&&!string.IsNullOrEmpty(real.Model.Error);}
            controller.ContinueAge();controls["continue"].onClick.Invoke();yield return null;
            var retainedProfile=profile;
            passed &= retainedProfile!=null&&retainedProfile.CheckBackRetentionForReview(()=>controls["continue"].onClick.Invoke());
            passed &= profile==retainedProfile;
            string result="{\"passed\":"+(passed?"true":"false")+",\"checks\":[\"11 onboarding state checks\",\"underage blocked\",\"native country picker\",\"consent initially disabled\",\"document reader unavailable\",\"checkbox interactions\",\"locale preserves choices\",\"back preserves draft\",\"real policy fails closed\",\"profile draft survives Back to consent and reentry through native controls\"]}";
            File.WriteAllText(Path.ChangeExtension(capture,".smoke.json"),result);Debug.Log("SOLOGYM_ONBOARDING_SMOKE "+result);if(!passed)Application.Quit(2);
        }
        void OnDestroy(){controller?.Dispose();}
        static bool HasArgument(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0;
        static string Argument(string key){var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,key);return index>=0&&index+1<args.Length?args[index+1]:null;}
    }
}

namespace SoloGym
{
    public sealed class OnboardingSilverText : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            Rect r = ((RectTransform)transform).rect;
            UIVertex v = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref v, i);
                float t = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y);
                v.color = Color.Lerp(new Color32(154,170,195,255), new Color32(255,255,255,255),t);
                mesh.SetUIVertex(v, i);
            }
        }
    }
}
