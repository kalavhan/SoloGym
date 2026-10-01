using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Memory-only focus/experience review; uses catalog choices, never prescribes difficulty.</summary>
    public sealed class PixelGoalsWindow : MonoBehaviour
    {
        public GoalsExperienceController Controller { get; private set; }
        public PixelChoiceOption[] Goals { get; private set; }
        public PixelChoiceOption[] Experiences { get; private set; }
        public PixelSecondaryAction Back { get; private set; }
        public PixelSecondaryAction Unsure { get; private set; }
        public PixelSecondaryAction Cancel { get; private set; }
        public PixelSecondaryAction ChangeGoal { get; private set; }
        public PixelSecondaryAction ChangeExperience { get; private set; }
        public PixelPrimaryButton Continue { get; private set; }
        public ScrollRect Scroll { get; private set; }
        public Text Status { get; private set; }
        public bool EquipmentPending { get; private set; }
        public event Action StateChanged;
        /// <summary>Raised when the reviewed choices continue to the connected equipment window.</summary>
        public event Action EquipmentRequested;
        public Selectable LastControl => Continue.IsInteractable() ? (Selectable)Continue : Controller.Model.Step==GoalsExperienceStep.Goal ? Goals[Goals.Length-1] : Unsure;
        RectTransform content, viewport, goalGroup, experienceGroup, summaryGroup;
        Text title, progress, review, helper, uncertaintyHint, detail, goalValue, experienceValue, goalLabel, experienceLabel;
        Selectable footer, locale;
        Action back, exit;
        Func<bool> canProceed;
        bool binding;
        string lastView;

        public void Initialize(Transform panel, Action backAction, Action exitAction, Func<bool> entryAllowed, Selectable privacy, Selectable language)
        {
            transform.SetParent(panel,false); PixelJournalUI.Stretch((RectTransform)transform);
            back=backAction; exit=exitAction; canProceed=entryAllowed; footer=privacy; locale=language;
            Back=ActionButton(transform,GoBack); Place(Back,new Rect(20,10,112,52));
            title=PixelJournalUI.Text(transform,new Rect(28,48,514,48),"",32,false,TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(transform,62,94,446);
            progress=PixelJournalUI.Text(transform,new Rect(28,98,514,30),"",22,false,TextAnchor.MiddleCenter);
            review=PixelJournalUI.Text(transform,new Rect(28,128,514,26),"",20,false,TextAnchor.MiddleCenter);
            review.color=new Color32(183,185,175,255);
            content=PixelJournalUI.Scroll(transform,new Rect(28,154,514,464),468);
            viewport=(RectTransform)content.parent; Scroll=viewport.GetComponent<ScrollRect>();
            Scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            var track=(RectTransform)Scroll.verticalScrollbar.transform;
            track.anchorMin=new Vector2(1,0); track.anchorMax=Vector2.one; track.pivot=new Vector2(1,.5f); track.anchoredPosition=Vector2.zero; track.sizeDelta=new Vector2(7,0);
            goalGroup=PixelJournalUI.Rect("Focus choices",content,new Rect(0,0,502,340));
            experienceGroup=PixelJournalUI.Rect("Experience choices",content,new Rect(0,12,502,198));
            summaryGroup=PixelJournalUI.Rect("Review choices",content,new Rect(0,18,502,258));
            PixelJournalUI.Rule(summaryGroup,4,0,494); PixelJournalUI.Rule(summaryGroup,4,94,494); PixelJournalUI.Rule(summaryGroup,4,248,494);
            goalLabel=PixelJournalUI.Text(summaryGroup,new Rect(8,16,370,30),"",21,false);
            goalValue=PixelJournalUI.Text(summaryGroup,new Rect(8,44,374,44),"",26,false);
            experienceLabel=PixelJournalUI.Text(summaryGroup,new Rect(8,110,370,30),"",21,false);
            experienceValue=PixelJournalUI.Text(summaryGroup,new Rect(8,140,374,100),"",24,false);
            ChangeGoal=ActionButton(summaryGroup,()=>Controller.EditGoal()); Place(ChangeGoal,new Rect(390,30,112,52));
            ChangeExperience=ActionButton(summaryGroup,()=>Controller.EditExperience()); Place(ChangeExperience,new Rect(390,136,112,52));
            Unsure=ActionButton(content,()=>Controller.OpenUncertainty()); Place(Unsure,new Rect(0,220,502,52));
            uncertaintyHint=PixelJournalUI.Text(content,new Rect(0,274,502,54),"",21,false,TextAnchor.MiddleCenter); uncertaintyHint.color=review.color;
            detail=PixelJournalUI.Text(content,new Rect(),"",23,false,TextAnchor.UpperLeft);
            helper=PixelJournalUI.Text(content,new Rect(),"",20,false,TextAnchor.MiddleCenter); helper.color=review.color;
            Status=PixelJournalUI.Text(content,new Rect(),"",21,false); Status.color=new Color32(255,194,158,255);
            Cancel=ActionButton(content,()=> { if(EquipmentPending) exit?.Invoke(); else Controller.CancelUncertainty(); });
            Continue=PixelPrimaryButton.Create(content,"",Advance); Continue.SetFontSize(30); Continue.Background.pixelsPerUnitMultiplier=2;
            CreateController(true); // No adult audience is assumed before the parent validates age/country.
        }
        void CreateController(bool teen)
        {
            Controller?.Dispose();
            Controller=new GoalsExperienceController(true,teen);
            RebuildOptions();
            Controller.Changed+=Render;
            Controller.CheckpointRequested+=id=> {
                if(id!="REVIEW:WIN-011" || !canProceed()) return;
                if(EquipmentRequested!=null) EquipmentRequested.Invoke();
                else { EquipmentPending=true; Render(Controller.Model); }
            };
            Render(Controller.Model);
        }
        void RebuildOptions()
        {
            foreach(Transform child in goalGroup) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach(Transform child in experienceGroup) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var model=Controller.Model;
            Goals=new PixelChoiceOption[model.VisibleGoals.Length]; Experiences=new PixelChoiceOption[model.ExperienceChoices.Length];
            for(int i=0;i<Goals.Length;i++)
            {
                string id=model.VisibleGoals[i].id;
                Goals[i]=Option(goalGroup,id,new Rect(0,i*68,502,60),()=>Controller.SelectGoal(id));
            }
            for(int i=0;i<Experiences.Length;i++)
            {
                string id=model.ExperienceChoices[i].id;
                Experiences[i]=Option(experienceGroup,id,new Rect(0,i*106,502,92),()=>Controller.SelectExperience(id));
                Experiences[i].Label.fontSize=23;
            }
        }
        PixelChoiceOption Option(Transform parent,string id,Rect bounds,Action action)
        {
            var option=PixelChoiceOption.Create(parent,id,id,null,null); option.Background.pixelsPerUnitMultiplier=2;
            option.Label.alignment=TextAnchor.MiddleLeft; option.SetLabelInsets(78,16);
            option.onValueChanged.AddListener(_=> { if(!binding) action(); }); Place(option,bounds); return option;
        }
        public void Open(string language,bool teen)
        {
            if(Controller.Model.IsTeenAudience!=teen) { EquipmentPending=false; lastView=null; CreateController(teen); }
            SetLocale(language); Render(Controller.Model);
        }
        public void SetLocale(string language) { if(Controller.Model.Language!=language) Controller.SetLanguage(language); }
        /// <summary>Editing a saved setup: start at the review step with the person's current choices.</summary>
        public void Prefill(string goal,string experience)
        {
            if(string.IsNullOrEmpty(goal)||string.IsNullOrEmpty(experience)||Controller.Model.Step!=GoalsExperienceStep.Goal||!string.IsNullOrEmpty(Controller.Model.GoalId)) return;
            Controller.SelectGoal(goal); Controller.ContinueGoal();
            if(Controller.Model.Step==GoalsExperienceStep.Experience) { Controller.SelectExperience(experience); Controller.ContinueExperience(); }
        }
        public void Reset() { EquipmentPending=false; lastView=null; Controller.Reset(); }
        public void GoBack()
        {
            if(EquipmentPending) { EquipmentPending=false; Render(Controller.Model); }
            else if(Controller.Model.UncertaintyDialogOpen) Controller.CancelUncertainty();
            else if(Controller.Model.Step==GoalsExperienceStep.Goal) back?.Invoke();
            else Controller.Back();
        }
        public void Advance()
        {
            if(!gameObject.activeInHierarchy || !canProceed()) return;
            if(EquipmentPending) { EquipmentPending=false; Render(Controller.Model); return; }
            if(Controller.Model.UncertaintyDialogOpen) { Controller.ConfirmUncertaintyBeginner(); return; }
            switch(Controller.Model.Step)
            {
                case GoalsExperienceStep.Goal: Controller.ContinueGoal(); break;
                case GoalsExperienceStep.Experience: Controller.ContinueExperience(); break;
                case GoalsExperienceStep.Review: Controller.ContinueReview(); break;
            }
        }
        void Render(GoalsExperienceViewModel m)
        {
            if(binding) return;
            binding=true;
            try
            {
                bool modal=m.UncertaintyDialogOpen, goal=m.Step==GoalsExperienceStep.Goal && !modal && !EquipmentPending;
                bool experience=m.Step==GoalsExperienceStep.Experience && !modal && !EquipmentPending, summary=m.Step==GoalsExperienceStep.Review && !EquipmentPending;
                title.text=EquipmentPending ? L("NEXT: YOUR EQUIPMENT","SIGUE: TU EQUIPO") : goal ? L("CHOOSE YOUR FOCUS","ELIGE TU ENFOQUE") : summary ? L("REVIEW YOUR PATH","REVISA TU RUTA") : L("YOUR STARTING POINT","TU PUNTO DE PARTIDA");
                progress.text=EquipmentPending ? L("TRAINING SETUP","CONFIGURACIÓN DE ENTRENAMIENTO") : goal ? L("1 OF 3 · GOAL","1 DE 3 · OBJETIVO") : summary ? L("3 OF 3 · REVIEW","3 DE 3 · RESUMEN") : L("2 OF 3 · EXPERIENCE","2 DE 3 · EXPERIENCIA");
                review.text=EquipmentRequested!=null ? L("Training setup","Configuración de entrenamiento") : L("Preview","Vista previa"); Back.SetLabel(L("Back","Volver"));
                goalGroup.gameObject.SetActive(goal); experienceGroup.gameObject.SetActive(experience); summaryGroup.gameObject.SetActive(summary);
                for(int i=0;i<Goals.Length;i++) { Goals[i].SetLabel(m.VisibleGoals[i].Label(m.Language)); Goals[i].SetIsOnWithoutNotify(m.GoalId==Goals[i].Id); Goals[i].RefreshVisual(); }
                for(int i=0;i<Experiences.Length;i++) { Experiences[i].SetLabel(ExperienceText(Experiences[i].Id)); Experiences[i].SetIsOnWithoutNotify(m.ExperienceId==Experiences[i].Id); Experiences[i].RefreshVisual(); }
                Unsure.gameObject.SetActive(experience); Unsure.SetLabel(L("I'm not sure","No estoy seguro/a"));
                uncertaintyHint.gameObject.SetActive(experience); uncertaintyHint.text=L("We'll suggest a starting point\nfor you to confirm.","Te propondremos una base\npara que la confirmes.");
                goalLabel.text=m.Copy("review_focus"); experienceLabel.text=m.Copy("review_experience"); goalValue.text=m.GoalLabel; experienceValue.text=ExperienceText(m.ExperienceId);
                ChangeGoal.SetLabel(L("Change","Cambiar")); ChangeExperience.SetLabel(L("Change","Cambiar"));
                detail.gameObject.SetActive(modal||EquipmentPending||summary);
                detail.text=modal ? m.Copy("uncertainty_title")+"\n\n"+m.Copy("uncertainty_body") : EquipmentPending ? L("Available equipment is the next window. Schedule and session length will follow.\n\nThis preview has not saved a profile or prepared a workout.","El equipo disponible es la siguiente ventana. Después elegirás horario y duración.\n\nEsta vista previa no guardó un perfil ni preparó una rutina.") : L("Next: equipment and schedule.\nDifficulty can change during battle.","Después: equipo y horario.\nLa dificultad podrá cambiar en la batalla.");
                detail.alignment=summary ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
                helper.text=goal ? (m.IsTeenAudience ? m.Copy("teen_helper")+"\n" : "")+L("You can change it later.","Podrás cambiarlo más adelante.") : experience ? L("Difficulty adjusts during your routine.","La dificultad se ajusta durante la rutina.") : summary ? (EquipmentRequested!=null ? L("Next you'll choose equipment and days.","Después elegirás equipo y días.") : L("This preview does not save a plan.","Esta vista previa no guarda un plan.")) : "";
                Status.text=m.Error; Status.gameObject.SetActive(Status.text.Length>0);
                Cancel.gameObject.SetActive(modal||EquipmentPending); Cancel.SetLabel(EquipmentPending ? L("Leave preview","Salir de vista previa") : L("Keep my choice","Conservar mi elección"));
                Continue.SetLabel(EquipmentPending ? L("REVIEW MY CHOICES","REVISAR MIS ELECCIONES") : modal ? L("USE BEGINNER BASE","USAR BASE PRINCIPIANTE") : L("CONTINUE","CONTINUAR"));
                Continue.SetFontSize(modal||EquipmentPending?26:30); Continue.interactable=canProceed()&&(m.CanContinue||modal||EquipmentPending);
                Layout(); ConfigureNavigation();
                string view=m.Step+"/"+modal+"/"+EquipmentPending;
                if(lastView!=view) { lastView=view; Scroll.StopMovement(); content.anchoredPosition=Vector2.zero; if(gameObject.activeInHierarchy) Back.Select(); }
            }
            finally { binding=false; }
            StateChanged?.Invoke();
        }
        string ExperienceText(string id) => id=="beginner" ? L("I'm starting\nor returning","Estoy empezando\no retomando") : id=="intermediate" ? L("I train regularly and know\nthe basic movements","Entreno con regularidad\ny conozco los movimientos básicos") : "";
        void Layout()
        {
            var m=Controller.Model; float y=386;
            if(m.UncertaintyDialogOpen||EquipmentPending)
            {
                Place(detail,new Rect(8,20,486,220));
                float h=Mathf.Max(220,detail.preferredHeight+4); Place(detail,new Rect(8,20,486,h));
                y=Mathf.Max(328,h+40); Place(Cancel,new Rect(0,y,502,52)); y+=60; Place(helper,new Rect());
            }
            else
            {
                if(m.Step==GoalsExperienceStep.Review) Place(detail,new Rect(0,280,502,64));
                Place(helper,new Rect(0,356,502,30));
                float h=Mathf.Max(30,helper.preferredHeight+4);
                Place(helper,new Rect(0,386-h,502,h));
            }
            if(Status.text.Length>0) { float h=Status.preferredHeight+8; Place(Status,new Rect(0,y,502,h)); y+=h+8; }
            Place(Continue,new Rect(0,y+4,502,64)); content.sizeDelta=new Vector2(502,Mathf.Max(viewport.rect.height,y+72));
        }
        void ConfigureNavigation()
        {
            var path=new List<Selectable>{Back};
            if(goalGroup.gameObject.activeSelf) path.AddRange(Goals);
            if(experienceGroup.gameObject.activeSelf) { path.AddRange(Experiences); path.Add(Unsure); }
            if(summaryGroup.gameObject.activeSelf) { path.Add(ChangeGoal); path.Add(ChangeExperience); }
            if(Cancel.gameObject.activeSelf) path.Add(Cancel);
            if(Continue.IsInteractable()) path.Add(Continue);
            for(int i=0;i<path.Count;i++) Link(path[i],i==0?locale:path[i-1],i+1==path.Count?footer:path[i+1]);
        }
        public void Relayout(float panelHeight) { viewport.sizeDelta=new Vector2(514,Mathf.Max(70,panelHeight-168)); Layout(); }
        void Update()
        {
            if(!Input.GetKeyDown(KeyCode.Tab)) return;
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected==null||!selected.transform.IsChildOf(content)) return;
            Canvas.ForceUpdateCanvases(); var b=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,selected.transform);
            float delta=b.min.y<viewport.rect.yMin?viewport.rect.yMin-b.min.y:b.max.y>viewport.rect.yMax?viewport.rect.yMax-b.max.y:0;
            var p=content.anchoredPosition; p.y=Mathf.Clamp(p.y+delta,0,Mathf.Max(0,content.rect.height-viewport.rect.height)); content.anchoredPosition=p;
        }
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
            current.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=previous,selectOnDown=next};
        }
    }
}
