using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>The approved consent and read-only reader layouts, backed by replaceable review documents.</summary>
    public sealed class PixelConsentWindow : MonoBehaviour
    {
        public ConsentReviewController Controller { get; private set; }
        public PixelSecondaryAction Back { get; private set; }
        public PixelSecondaryAction ReadPrivacy { get; private set; }
        public PixelSecondaryAction ReadTerms { get; private set; }
        public PixelCheckbox PrivacyChoice { get; private set; }
        public PixelCheckbox TermsChoice { get; private set; }
        public PixelPrimaryButton Continue { get; private set; }
        public PixelPrimaryButton ReaderReturn { get; private set; }
        public Text DocumentBody { get; private set; }
        public ScrollRect DecisionScroll { get; private set; }
        public ScrollRect DocumentScroll { get; private set; }
        public PixelDocumentScrollFocus ReaderFocus { get; private set; }
        public bool IsReading => documentId.Length > 0;
        public string DocumentId => documentId;
        public bool BeforeAccount { get; set; } = true;
        public Selectable LastControl => IsReading ? ReaderReturn : Continue.IsInteractable() ? (Selectable)Continue : TermsChoice;
        public event Action StateChanged;
        RectTransform decisions, decisionsViewport, reader, readerFrame;
        Text title, subtitle, provisional, helper, sample, privacyTitle, termsTitle;
        readonly Dictionary<string,float> scrollPositions = new Dictionary<string,float>();
        string documentId = "", returnLabel = "";
        Action back, forward, closeReader;
        Action<string> read;
        Selectable footer, locale;
        bool rendering;

        public void Initialize(Transform panel, Action backAction, Action continueAction, Action<string> readAction, Action closeAction,
            Selectable privacyFooter, Selectable languageControl, ReviewDocumentCatalog documents = null)
        {
            transform.SetParent(panel,false); PixelJournalUI.Stretch((RectTransform)transform);
            back=backAction; forward=continueAction; read=readAction; closeReader=closeAction; footer=privacyFooter; locale=languageControl;
            Controller=new ConsentReviewController(documents);
            Back=Action(transform,()=> { if(IsReading) closeReader?.Invoke(); else back?.Invoke(); }); Place(Back,new Rect(20,10,112,52));
            title=PixelJournalUI.Text(transform,new Rect(28,48,514,48),"",34,false,TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(transform,62,94,446);
            subtitle=PixelJournalUI.Text(transform,new Rect(28,99,514,38),"",24,false,TextAnchor.MiddleCenter);
            decisions=PixelJournalUI.Scroll(transform,new Rect(28,144,514,472),476);
            decisionsViewport=(RectTransform)decisions.parent; DecisionScroll=decisionsViewport.GetComponent<ScrollRect>(); StretchTrack(DecisionScroll);
            provisional=PixelJournalUI.Text(decisions,new Rect(0,0,502,38),"",21,false,TextAnchor.MiddleCenter); provisional.color=new Color32(183,185,175,255);
            ReadPrivacy=DocumentAction(decisions,"privacy",46,out privacyTitle);
            ReadTerms=DocumentAction(decisions,"terms",130,out termsTitle);
            PrivacyChoice=PixelCheckbox.Create(decisions,"Privacy review choice"); Place(PrivacyChoice,new Rect(0,222,502,52));
            TermsChoice=PixelCheckbox.Create(decisions,"Terms review choice"); Place(TermsChoice,new Rect(0,270,502,52));
            PrivacyChoice.onValueChanged.AddListener(Controller.ChoosePrivacy); TermsChoice.onValueChanged.AddListener(Controller.ChooseTerms);
            helper=PixelJournalUI.Text(decisions,new Rect(0,332,502,58),"",20,false,TextAnchor.MiddleCenter); helper.color=provisional.color;
            Continue=PixelPrimaryButton.Create(decisions,"",Advance); Continue.SetFontSize(30); Continue.Background.pixelsPerUnitMultiplier=2; Place(Continue,new Rect(0,398,502,64));
            reader=PixelJournalUI.Rect("Read-only review document",transform,new Rect()); PixelJournalUI.Stretch(reader);
            readerFrame=PixelJournalUI.Rect("Document field skin",reader,new Rect(24,144,522,342));
            var bg=readerFrame.gameObject.AddComponent<Image>(); bg.sprite=Resources.LoadAll<Sprite>("UI/Pixel/FormField")[0]; bg.type=Image.Type.Sliced; bg.pixelsPerUnitMultiplier=2; bg.raycastTarget=false;
            var textContent=PixelJournalUI.Scroll(readerFrame,new Rect(12,12,498,318),322);
            DocumentScroll=textContent.parent.GetComponent<ScrollRect>(); StretchTrack(DocumentScroll);
            ReaderFocus=DocumentScroll.gameObject.AddComponent<PixelDocumentScrollFocus>(); ReaderFocus.Scroll=DocumentScroll;
            ReaderFocus.transition=Selectable.Transition.None;
            ReaderFocus.FocusOutline=readerFrame.gameObject.AddComponent<Outline>();
            ReaderFocus.FocusOutline.effectColor=PixelJournalUI.Ivory; ReaderFocus.FocusOutline.effectDistance=new Vector2(1,-1); ReaderFocus.FocusOutline.enabled=false;
            DocumentScroll.verticalScrollbar.targetGraphic.color=new Color32(220,204,159,255);
            DocumentBody=PixelJournalUI.Text(textContent,new Rect(4,0,472,300),"",22,false,TextAnchor.UpperLeft);
            sample=PixelJournalUI.Text(reader,new Rect(28,494,502,42),"",20,false);
            ReaderReturn=PixelPrimaryButton.Create(reader,"",()=>closeReader?.Invoke()); ReaderReturn.SetFontSize(27); ReaderReturn.Background.pixelsPerUnitMultiplier=2; Place(ReaderReturn,new Rect(28,542,502,64));
            Controller.Changed+=Render; Render();
        }
        PixelSecondaryAction DocumentAction(Transform parent,string id,float y,out Text label)
        {
            var button=PixelSecondaryAction.Create(parent,PixelSecondaryAction.Appearance.Framed,"",()=>read?.Invoke(id));
            button.Label.fontSize=23; button.Background.pixelsPerUnitMultiplier=2; button.SetHorizontalPadding(16); Place(button,new Rect(0,y,502,64));
            // The whole row is one native target, with a separately aligned document title and action.
            button.Label.alignment=TextAnchor.MiddleRight;
            label=PixelJournalUI.Text(button.transform,new Rect(28,0,358,64),"",23,false);
            PixelJournalUI.Rule(button.transform,386,16,1).rectTransform.sizeDelta=new Vector2(1,32);
            return button;
        }
        public void OpenDecisions(string language)
        {
            SavePosition(); documentId=""; Controller.SetLanguage(language); Render();
            DecisionScroll.StopMovement(); decisions.anchoredPosition=Vector2.zero;
        }
        public void OpenDocument(string id,string language,string returnText)
        {
            if(id!="privacy" && id!="terms") throw new ArgumentException("Unknown review document.");
            SavePosition(); documentId=id; returnLabel=returnText; Controller.SetLanguage(language); Render(); RestorePosition();
        }
        public void SetLocale(string language,string returnText)
        {
            returnLabel=returnText;
            if(Controller.Language==language) return;
            SavePosition(); Controller.SetLanguage(language); RestorePosition();
        }
        public void Reset()
        {
            scrollPositions.Clear(); documentId=""; returnLabel=""; Controller.Reset();
            DocumentScroll.StopMovement(); DocumentScroll.verticalNormalizedPosition=1;
        }
        public void Advance()
        {
            if(!gameObject.activeInHierarchy || IsReading || !Controller.CanContinue) return;
            forward?.Invoke();
        }
        void SavePosition() { if(IsReading && DocumentScroll!=null) scrollPositions[documentId+":"+Controller.Language]=DocumentScroll.verticalNormalizedPosition; }
        void RestorePosition()
        {
            if(!IsReading) return;
            Canvas.ForceUpdateCanvases(); DocumentScroll.StopMovement(); DocumentScroll.verticalNormalizedPosition=scrollPositions.TryGetValue(documentId+":"+Controller.Language,out var p)?p:1;
        }
        void Render()
        {
            if(rendering) return; rendering=true;
            try
            {
                bool reading=IsReading;
                decisionsViewport.gameObject.SetActive(!reading); reader.gameObject.SetActive(reading);
                Back.SetLabel(L("Back","Volver"));
                title.text=reading ? Controller.Document(documentId)?.title.ToUpperInvariant() ?? (documentId=="privacy" ? L("PRIVACY POLICY","POLÍTICA DE PRIVACIDAD") : L("TERMS OF USE","TÉRMINOS DE USO")) : L("YOUR CHOICES","TUS DECISIONES");
                title.fontSize=reading?30:34;
                subtitle.text=reading ? L("Placeholder text · Lorem ipsum","Texto provisional · Lorem ipsum") : BeforeAccount ? L("Before creating your account","Antes de crear tu cuenta") : L("Before setting up your profile","Antes de configurar tu perfil");
                provisional.text=L("Provisional texts · Preview","Textos provisionales · Vista previa");
                privacyTitle.text=L("Privacy policy","Política de privacidad"); termsTitle.text=L("Terms of use","Términos de uso");
                ReadPrivacy.SetLabel(L("READ","LEER")); ReadTerms.SetLabel(L("READ","LEER"));
                PrivacyChoice.SetLabel(L("I have read the privacy policy.","He leído la política de privacidad."));
                TermsChoice.SetLabel(L("I accept the terms of use.","Acepto los términos de uso."));
                PrivacyChoice.interactable=Controller.Document("privacy")!=null; TermsChoice.interactable=Controller.Document("terms")!=null;
                PrivacyChoice.SetIsOnWithoutNotify(Controller.PrivacyChecked); TermsChoice.SetIsOnWithoutNotify(Controller.TermsChecked);
                helper.text=Controller.DocumentsAvailable ? L("This preview does not record acceptance.","Esta vista previa no registra aceptación.") : L("A preview document is unavailable. You can return or read the available text.","Falta un documento de muestra. Puedes volver o leer el texto disponible.");
                Continue.SetLabel(L("CONTINUE","CONTINUAR")); Continue.interactable=Controller.CanContinue;
                if(reading) DocumentBody.text=Controller.Document(documentId)?.body ?? L("This preview document is unavailable. No acceptance has been recorded.","Este documento de muestra no está disponible. No se registró ninguna aceptación.");
                sample.text=L("Sample document, not a legal policy.","Documento de muestra, sin validez legal.");
                ReaderReturn.SetLabel(returnLabel.Length>0?returnLabel:L("BACK TO MY CHOICES","VOLVER A MIS DECISIONES"));
                LayoutText(); SetTraversal(); StateChanged?.Invoke();
            }
            finally { rendering=false; }
        }
        void LayoutText()
        {
            DocumentBody.rectTransform.sizeDelta=new Vector2(472,Mathf.Max(40,DocumentBody.preferredHeight+12));
            DocumentScroll.content.sizeDelta=new Vector2(486,Mathf.Max(DocumentScroll.viewport.rect.height,DocumentBody.preferredHeight+12));
            decisions.sizeDelta=new Vector2(502,Mathf.Max(462,decisionsViewport.rect.height));
        }
        public void Relayout(float panelHeight)
        {
            decisionsViewport.sizeDelta=new Vector2(514,Mathf.Max(86,panelHeight-160));
            float height=Mathf.Max(86,panelHeight-290);
            readerFrame.sizeDelta=new Vector2(522,height);
            DocumentScroll.viewport.sizeDelta=new Vector2(498,Mathf.Max(62,height-24));
            Place(sample,new Rect(28,152+height,502,42)); Place(ReaderReturn,new Rect(28,200+height,502,64));
            LayoutText();
        }
        void SetTraversal()
        {
            if(IsReading) { Link(Back,locale,ReaderFocus); Link(ReaderFocus,Back,ReaderReturn); Link(ReaderReturn,ReaderFocus,footer); }
            else
            {
                Link(Back,locale,ReadPrivacy); Link(ReadPrivacy,Back,ReadTerms); Link(ReadTerms,ReadPrivacy,PrivacyChoice);
                Link(PrivacyChoice,ReadTerms,TermsChoice); Link(TermsChoice,PrivacyChoice,Continue); Link(Continue,TermsChoice,footer);
            }
        }
        void OnDisable() { SavePosition(); }
        void OnDestroy() { if(Controller!=null) Controller.Changed-=Render; }
        string L(string en,string es)=>Controller.Language=="es"?es:en;
        static PixelSecondaryAction Action(Transform parent,UnityEngine.Events.UnityAction callback)
        { var b=PixelSecondaryAction.Create(parent,PixelSecondaryAction.Appearance.Text,"",callback); b.Label.fontSize=22; b.SetHorizontalPadding(8); b.Background.pixelsPerUnitMultiplier=2; return b; }
        static void Place(Component c,Rect r)=>PixelJournalUI.Place((RectTransform)c.transform,r);
        static void StretchTrack(ScrollRect s)
        { s.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide; var r=(RectTransform)s.verticalScrollbar.transform; r.anchorMin=new Vector2(1,0); r.anchorMax=Vector2.one; r.pivot=new Vector2(1,.5f); r.anchoredPosition=Vector2.zero; r.sizeDelta=new Vector2(7,0); }
        static void Link(Selectable current,Selectable previous,Selectable next)
        {
            var tab=current.GetComponent<PixelFieldTabNavigation>()??current.gameObject.AddComponent<PixelFieldTabNavigation>();tab.Previous=previous;tab.Next=next;
            current.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=previous,selectOnDown=next,selectOnLeft=previous,selectOnRight=next};
        }
    }

    /// <summary>A focusable document viewport. Reading/scrolling never changes decisions.</summary>
    public sealed class PixelDocumentScrollFocus : Selectable
    {
        public ScrollRect Scroll;
        public Outline FocusOutline;
        public override void OnSelect(BaseEventData data) { base.OnSelect(data); if(FocusOutline!=null) FocusOutline.enabled=true; }
        public override void OnDeselect(BaseEventData data) { base.OnDeselect(data); if(FocusOutline!=null) FocusOutline.enabled=false; }
        protected override void OnDisable() { if(FocusOutline!=null) FocusOutline.enabled=false; base.OnDisable(); }
        public void ScrollBy(float pixels)
        {
            float max=Mathf.Max(0,Scroll.content.rect.height-Scroll.viewport.rect.height);
            Scroll.StopMovement(); var p=Scroll.content.anchoredPosition; p.y=Mathf.Clamp(p.y+pixels,0,max); Scroll.content.anchoredPosition=p;
        }
        public override void OnMove(AxisEventData data)
        {
            if(data.moveDir==MoveDirection.Down || data.moveDir==MoveDirection.Up) { ScrollBy(data.moveDir==MoveDirection.Down?44:-44); data.Use(); }
            else base.OnMove(data);
        }
        void Update()
        {
            if(EventSystem.current?.currentSelectedGameObject!=gameObject) return;
            if(Input.GetKeyDown(KeyCode.PageDown)) ScrollBy(Scroll.viewport.rect.height*.8f);
            if(Input.GetKeyDown(KeyCode.PageUp)) ScrollBy(-Scroll.viewport.rect.height*.8f);
            if(Input.GetKeyDown(KeyCode.Home)) ScrollBy(-float.MaxValue);
            if(Input.GetKeyDown(KeyCode.End)) ScrollBy(float.MaxValue);
        }
    }
}
