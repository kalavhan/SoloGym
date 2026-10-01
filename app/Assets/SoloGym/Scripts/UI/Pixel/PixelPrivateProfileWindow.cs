using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Private, memory-only profile review over the shared guild entrance.</summary>
    public sealed class PixelPrivateProfileWindow : MonoBehaviour
    {
        public PrivateProfileController Controller { get; private set; }
        public PixelSecondaryAction Back { get; private set; }
        public PixelSecondaryAction Secondary { get; private set; }
        public PixelPrimaryButton Continue { get; private set; }
        public PixelChoiceControl Units { get; private set; }
        public PixelFormField Height { get; private set; }
        public PixelFormField Feet { get; private set; }
        public PixelFormField Inches { get; private set; }
        public PixelFormField Weight { get; private set; }
        public PixelChoiceOption[] Choices { get; private set; }
        public ScrollRect Scroll { get; private set; }
        public Text Status { get; private set; }
        public event Action StateChanged;
        public Selectable LastControl => Continue.gameObject.activeSelf && Continue.IsInteractable() ? (Selectable)Continue : Secondary;
        RectTransform content, viewport, measurementGroup, readinessGroup;
        Text title, progress, review, heading, noticeCopy, helper, detail, readinessHint;
        Text heightUnit, feetUnit, inchesUnit, weightUnit;
        Selectable footer, locale;
        Action back;
        bool binding;
        ProfileStep lastStep = (ProfileStep)(-1);
        static readonly string[] ReadinessIds = { "ready", "low_energy", "ill", "pain", "injury", "unsure" };

        public void Initialize(Transform panel, Action backAction, Action exitAction, Selectable privacy, Selectable language, bool reviewMode)
        {
            transform.SetParent(panel, false); PixelJournalUI.Stretch((RectTransform)transform);
            back = backAction; footer = privacy; locale = language;
            Controller = new PrivateProfileController(reviewMode);
            Controller.ExitRequested += exitAction;
            Back = ActionButton(transform, GoBack); Place(Back, new Rect(20,10,112,52));
            title = PixelJournalUI.Text(transform, new Rect(28,48,514,48), "", 34, false, TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(transform,62,94,446);
            progress = PixelJournalUI.Text(transform,new Rect(28,98,514,30),"",22,false,TextAnchor.MiddleCenter);
            review = PixelJournalUI.Text(transform,new Rect(28,128,514,26),"",20,false,TextAnchor.MiddleCenter);
            review.color = new Color32(183,185,175,255);
            content = PixelJournalUI.Scroll(transform,new Rect(28,154,514,464),468);
            viewport = (RectTransform)content.parent; Scroll = viewport.GetComponent<ScrollRect>();
            Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var track = (RectTransform)Scroll.verticalScrollbar.transform;
            track.anchorMin = new Vector2(1,0); track.anchorMax = Vector2.one; track.pivot = new Vector2(1,.5f); track.anchoredPosition = Vector2.zero; track.sizeDelta = new Vector2(7,0);
            heading = PixelJournalUI.Text(content,new Rect(0,0,502,40),"",27,false,TextAnchor.MiddleCenter);
            noticeCopy = PixelJournalUI.Text(content,new Rect(8,58,486,180),"",23,false,TextAnchor.UpperLeft);
            measurementGroup = PixelJournalUI.Rect("Optional measurements",content,new Rect(0,42,502,292));
            Units = PixelChoiceControl.Create(measurementGroup,new[]{"metric","imperial"},new[]{"Metric","Imperial"},"metric");
            Place(Units,new Rect(0,0,502,64));
            foreach(var option in Units.Options) { option.Label.fontSize = 24; option.Background.pixelsPerUnitMultiplier = 2; }
            Units.onValueChanged.AddListener(value => { if(!binding) { ReleaseKeyboard(); Controller.SetUnits(value); } });
            Height = Field(measurementGroup,Controller.SetHeight,out heightUnit);
            Feet = Field(measurementGroup,Controller.SetHeightFeet,out feetUnit);
            Inches = Field(measurementGroup,Controller.SetHeightInches,out inchesUnit);
            Weight = Field(measurementGroup,Controller.SetWeight,out weightUnit);
            Height.Input.onSubmit.AddListener(_=>Focus(Weight)); Feet.Input.onSubmit.AddListener(_=>Focus(Inches)); Inches.Input.onSubmit.AddListener(_=>Focus(Weight));
            Weight.Input.onSubmit.AddListener(_=>Advance());
            readinessHint = PixelJournalUI.Text(content,new Rect(0,38,502,26),"",20,false,TextAnchor.MiddleCenter);
            readinessGroup = PixelJournalUI.Rect("Current readiness",content,new Rect(0,68,502,220));
            Choices = new PixelChoiceOption[ReadinessIds.Length];
            for(int i=0;i<Choices.Length;i++)
            {
                string id=ReadinessIds[i];
                var option=PixelChoiceOption.Create(readinessGroup,id,id,null,null);
                option.Label.fontSize=21; option.Background.pixelsPerUnitMultiplier=2;
                option.onValueChanged.AddListener(_=> { if(!binding) Controller.SelectReadiness(id); });
                Place(option,new Rect((i%2)*257,(i/2)*72,245,64)); Choices[i]=option;
            }
            detail = PixelJournalUI.Text(content,new Rect(),"",22,false,TextAnchor.UpperLeft);
            helper = PixelJournalUI.Text(content,new Rect(),"",20,false,TextAnchor.MiddleCenter); helper.color=review.color;
            Status = PixelJournalUI.Text(content,new Rect(),"",21,false); Status.color=new Color32(255,194,158,255);
            Secondary = ActionButton(content,SecondaryAction);
            Continue = PixelPrimaryButton.Create(content,"",Advance); Continue.SetFontSize(30); Continue.Background.pixelsPerUnitMultiplier=2;
            Controller.Changed += Render;
            Render(Controller.Model);
        }

        PixelFormField Field(Transform parent, Action<string> update, out Text unit)
        {
            var f=PixelFormField.Create(parent,PixelFormField.Kind.Text,"","");
            f.Background.pixelsPerUnitMultiplier=2; f.Label.fontSize=23; f.SetLabelColor(PixelJournalUI.Ivory);
            f.Input.textComponent.fontSize=24; f.Input.keyboardType=TouchScreenKeyboardType.DecimalPad; f.Input.characterLimit=18;
            f.Input.onValueChanged.AddListener(value=> { if(!binding) update(value); });
            var valueViewport=(RectTransform)f.Input.textComponent.transform.parent;
            valueViewport.offsetMax=new Vector2(-76,-8);
            unit=PixelJournalUI.Text(f.Input.transform,new Rect(0,0,60,64),"",23,false,TextAnchor.MiddleCenter);
            unit.rectTransform.anchorMin=unit.rectTransform.anchorMax=new Vector2(1,1); unit.rectTransform.pivot=Vector2.one; unit.rectTransform.anchoredPosition=new Vector2(-6,0);
            return f;
        }
        public void Open(string language) { SetLocale(language); Render(Controller.Model); }
        public void SetLocale(string language) { if(Controller.Model.Language!=language) Controller.SetLanguage(language); }
        public void Reset() { ReleaseKeyboard(); Controller.Reset(); Scroll.StopMovement(); content.anchoredPosition=Vector2.zero; }
        public void GoBack()
        {
            ReleaseKeyboard();
            if(Controller.Model.Step==ProfileStep.Notice) back?.Invoke(); else Controller.Back();
        }
        public void Advance()
        {
            if(!gameObject.activeInHierarchy) return;
            ReleaseKeyboard();
            switch(Controller.Model.Step)
            {
                case ProfileStep.Notice: Controller.ContinueNotice(); break;
                case ProfileStep.Measurements: Controller.ContinueMeasurements(); break;
                case ProfileStep.Readiness: Controller.ContinueReadiness(); break;
                case ProfileStep.Paused: case ProfileStep.Checkpoint: Controller.Back(); break;
            }
        }
        void SecondaryAction()
        {
            ReleaseKeyboard();
            if(Controller.Model.Step==ProfileStep.Readiness) Controller.Pause(); else Controller.Decline();
        }
        void Render(PrivateProfileViewModel m)
        {
            if(binding) return;
            binding=true;
            try
            {
                bool notice=m.Step==ProfileStep.Notice, measurements=m.Step==ProfileStep.Measurements, readiness=m.Step==ProfileStep.Readiness;
                bool paused=m.Step==ProfileStep.Paused, checkpoint=m.Step==ProfileStep.Checkpoint, metric=m.UnitSystem=="metric";
                title.text=L("PRIVATE PROFILE","PERFIL PRIVADO");
                progress.text=notice ? L("1 OF 3 · BEFORE YOUR DATA","1 DE 3 · ANTES DE TUS DATOS") : measurements ? L("2 OF 3 · OPTIONAL MEASUREMENTS","2 DE 3 · MEDIDAS OPCIONALES") : L("3 OF 3 · HOW YOU FEEL","3 DE 3 · CÓMO TE SIENTES");
                review.text=m.ReviewMode ? L("Preview","Vista previa") : L("Private-data setup unavailable","Datos privados no disponibles");
                Back.SetLabel(L("Back","Volver"));
                heading.text=notice ? L("Your training, your pace","Tu entrenamiento, a tu ritmo") : measurements ? L("Your measurements, your pace","Tus medidas, a tu ritmo") : readiness ? L("How do you feel today?","¿Cómo llegas hoy?") : paused ? L("LET'S PAUSE","HAGAMOS UNA PAUSA") : L("NEXT: YOUR TRAINING","SIGUE: TU ENTRENAMIENTO");
                noticeCopy.text=L("Height and bodyweight are optional.\n\nThey do not change your character or appear in rankings.\n\nYou can continue without measurements.","Altura y peso son opcionales.\n\nNo cambian tu personaje ni aparecen en rankings.\n\nPodrás continuar sin añadir medidas.");
                noticeCopy.gameObject.SetActive(notice);
                measurementGroup.gameObject.SetActive(measurements); readinessGroup.gameObject.SetActive(readiness);
                readinessHint.gameObject.SetActive(readiness); readinessHint.text=L("Choose one; you can change it.","Elige una opción; podrás cambiarla.");
                Height.gameObject.SetActive(metric); Feet.gameObject.SetActive(!metric); Inches.gameObject.SetActive(!metric);
                Units.SetLabel("metric",L("Metric","Métrico")); Units.SetLabel("imperial",L("Imperial","Imperial")); Units.SetValueWithoutNotify(m.UnitSystem);
                Height.SetLocalizedText(L("Height (optional)","Altura (opcional)"),"—","","","");
                Feet.SetLocalizedText(L("Height · feet","Altura · pies"),"—","","",""); Inches.SetLocalizedText(L("Inches","Pulgadas"),"—","","","");
                Weight.SetLocalizedText(L("Bodyweight (optional)","Peso corporal (opcional)"),"—","","","");
                Height.SetValueWithoutNotify(m.HeightText); Feet.SetValueWithoutNotify(m.HeightFeetText); Inches.SetValueWithoutNotify(m.HeightInchesText); Weight.SetValueWithoutNotify(m.WeightText);
                heightUnit.text="cm"; feetUnit.text="ft"; inchesUnit.text="in"; weightUnit.text=metric ? "kg" : "lb";
                for(int i=0;i<Choices.Length;i++)
                {
                    var label=ReadinessLabel(i); Choices[i].SetLabel(label); Choices[i].SetIsOnWithoutNotify(m.Readiness==ReadinessIds[i]); Choices[i].RefreshVisual();
                }
                detail.gameObject.SetActive(paused||checkpoint);
                detail.text=paused ? L("Training stays paused for now. You can change your response or leave.\n\nThis screen does not assess or diagnose your condition.","Por ahora dejamos el entrenamiento en pausa. Puedes cambiar tu respuesta o salir.\n\nEsta pantalla no evalúa ni diagnostica tu condición.")
                    : L("Next: goals and training experience.\n\nThis preview has not saved a profile or generated a workout. Connected setup is still pending.","Siguiente: objetivos y experiencia.\n\nEsta vista previa no guardó un perfil ni generó una rutina. La configuración conectada sigue pendiente.");
                helper.text=notice ? L("Review with example data.\nNo profile is saved.","Revisión con datos de ejemplo.\nNo se guarda un perfil.") : measurements ? L("You can leave both fields empty.\nExample values · Not saved.","Puedes dejar ambos campos vacíos.\nValores de ejemplo · No se guardan.") : readiness ? L("If you feel unwell or unsure,\nyou can pause here.","Si tienes molestias o dudas,\npuedes pausar aquí.") : "";
                Status.text=m.Error.Length>0 ? m.Error : measurements && !m.CanContinue ? L("Check the numbers or leave the fields empty.","Revisa los números o deja los campos vacíos.") : "";
                Status.gameObject.SetActive(Status.text.Length>0);
                Secondary.SetLabel(readiness ? L("Pause for now","Pausar por ahora") : notice ? L("Not now","Ahora no") : L("Leave preview","Salir de vista previa"));
                Secondary.gameObject.SetActive(!measurements);
                Continue.SetLabel(paused ? L("CHANGE RESPONSE","CAMBIAR RESPUESTA") : checkpoint ? L("REVIEW MY ANSWERS","REVISAR MIS RESPUESTAS") : L("CONTINUE","CONTINUAR"));
                Continue.interactable=m.CanContinue||paused||checkpoint;
                Layout(); ConfigureNavigation();
                if(lastStep!=m.Step) { lastStep=m.Step; Scroll.StopMovement(); content.anchoredPosition=Vector2.zero; if(gameObject.activeInHierarchy) Back.Select(); }
            }
            finally { binding=false; }
            StateChanged?.Invoke();
        }
        string ReadinessLabel(int i)
        {
            string[] en={"Ready to train","Low energy","Feeling unwell","In pain","An injury","Not sure"};
            string[] es={"Con ganas","Poca energía","Me siento mal","Tengo dolor","Tengo una lesión","Tengo dudas"};
            return Controller.Model.Language=="es" ? es[i] : en[i];
        }
        void Layout()
        {
            var step=Controller.Model.Step; float y;
            if(step==ProfileStep.Measurements)
            {
                Place(Height,new Rect(0,76,502,Height.PreferredHeight));
                Place(Feet,new Rect(0,76,245,Feet.PreferredHeight)); Place(Inches,new Rect(257,76,245,Inches.PreferredHeight));
                float h=Controller.Model.UnitSystem=="metric" ? Height.PreferredHeight : Mathf.Max(Feet.PreferredHeight,Inches.PreferredHeight);
                Place(Weight,new Rect(0,84+h,502,Weight.PreferredHeight));
                y=42+84+h+Weight.PreferredHeight+6; Place(helper,new Rect(0,y,502,48)); y+=56;
            }
            else if(step==ProfileStep.Notice)
            {
                float h=Mathf.Max(180,noticeCopy.preferredHeight+4); Place(noticeCopy,new Rect(8,54,486,h));
                y=54+h+14; Place(helper,new Rect(0,y,502,56)); y=Mathf.Max(338,y+64);
            }
            else if(step==ProfileStep.Readiness)
            {
                y=284; Place(helper,new Rect(0,y,502,48)); y+=52;
            }
            else
            {
                float h=Mathf.Max(180,detail.preferredHeight+4); Place(detail,new Rect(8,60,486,h));
                Place(helper,new Rect(0,0,502,0)); y=Mathf.Max(338,60+h+12);
            }
            if(Status.text.Length>0) { float h=Status.preferredHeight+12; Place(Status,new Rect(0,y,502,h)); y+=h+8; }
            if(Secondary.gameObject.activeSelf) { Place(Secondary,new Rect(0,y,502,52)); y+=56; }
            Place(Continue,new Rect(0,y,502,64)); y+=68;
            content.sizeDelta=new Vector2(502,Mathf.Max(viewport.rect.height,y));
        }
        void ConfigureNavigation()
        {
            var path=new List<Selectable>{Back};
            if(Controller.Model.Step==ProfileStep.Measurements)
            {
                foreach(var option in Units.Options) path.Add(option);
                if(Controller.Model.UnitSystem=="metric") path.Add(Height.Input); else { path.Add(Feet.Input); path.Add(Inches.Input); }
                path.Add(Weight.Input);
            }
            if(Controller.Model.Step==ProfileStep.Readiness) path.AddRange(Choices);
            if(Secondary.gameObject.activeSelf) path.Add(Secondary);
            if(Continue.IsInteractable()) path.Add(Continue);
            for(int i=0;i<path.Count;i++) Link(path[i],i==0?locale:path[i-1],i+1==path.Count?footer:path[i+1]);
        }
        public void Relayout(float panelHeight,bool keyboard)
        {
            viewport.sizeDelta=new Vector2(514,Mathf.Max(70,panelHeight-168)); Layout(); Canvas.ForceUpdateCanvases();
            if(keyboard) RevealSelection();
        }
        public void RevealSelection()
        {
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected==null||!selected.transform.IsChildOf(content)) return;
            var b=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,selected.transform);
            float delta=b.min.y<viewport.rect.yMin ? viewport.rect.yMin-b.min.y : b.max.y>viewport.rect.yMax ? viewport.rect.yMax-b.max.y : 0;
            var p=content.anchoredPosition; p.y=Mathf.Clamp(p.y+delta,0,Mathf.Max(0,content.rect.height-viewport.rect.height)); content.anchoredPosition=p;
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Tab)) RevealSelection();
        }
        public void ReleaseKeyboard() { foreach(var f in new[]{Height,Feet,Inches,Weight}) f?.Input.DeactivateInputField(); }
        void Focus(PixelFormField f) { f.Input.Select(); f.Input.ActivateInputField(); RevealSelection(); }
        void OnDisable() { ReleaseKeyboard(); }
        void OnDestroy() { Controller?.Dispose(); }
        string L(string en,string es)=>Controller.Model.Language=="es"?es:en;
        static void Place(Component c,Rect r)=>PixelJournalUI.Place((RectTransform)c.transform,r);
        static PixelSecondaryAction ActionButton(Transform parent,UnityEngine.Events.UnityAction action)
        {
            var b=PixelSecondaryAction.Create(parent,PixelSecondaryAction.Appearance.Text,"",action); b.Label.fontSize=22; b.SetHorizontalPadding(8); b.Background.pixelsPerUnitMultiplier=2; return b;
        }
        static void Link(Selectable current,Selectable previous,Selectable next)
        {
            var tab=current.GetComponent<PixelFieldTabNavigation>()??current.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous=previous; tab.Next=next;
            if(!(current is InputField)) current.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=previous,selectOnDown=next,selectOnLeft=previous,selectOnRight=next};
        }
    }
}
