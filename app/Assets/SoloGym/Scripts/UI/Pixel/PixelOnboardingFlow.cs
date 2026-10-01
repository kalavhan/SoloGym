using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Two reviewed preparation steps. All draft choices are private and memory-only.</summary>
    public sealed class PixelOnboardingFlow : MonoBehaviour
    {
        public GuildSetupController Controller { get; private set; }
        public PixelFormField Age { get; private set; }
        public PixelSecondaryAction Country { get; private set; }
        public PixelSecondaryAction Back { get; private set; }
        public PixelPrimaryButton Continue { get; private set; }
        public PixelChoiceControl Gender { get; private set; }
        public PixelChoiceControl Bodies { get; private set; }
        public PixelFormField Search { get; private set; }
        public PixelSecondaryAction CloseCountries { get; private set; }
        public Image Hero { get; private set; }
        public ScrollRect Scroll { get; private set; }
        public Text Status { get; private set; }
        public bool CountryOpen => picker != null && picker.gameObject.activeSelf;
        public IReadOnlyList<PixelSecondaryAction> CountryResults => visibleCountries;
        public event Action StateChanged;
        readonly List<PixelSecondaryAction> visibleCountries = new List<PixelSecondaryAction>();
        readonly Dictionary<string, PixelSecondaryAction> countryButtons = new Dictionary<string, PixelSecondaryAction>();
        readonly string[] bodyIds = { "skinny", "medium", "fat", "muscular" };
        RectTransform body, viewport, ageGroup, characterGroup, pendingGroup, picker, results;
        Text title, subtitle, countryLabel, countryError, privateNote, guidance, genderLabel, bodyLabel, cosmeticNote, pendingText, pickerTitle, noResults;
        PixelSecondaryAction leave;
        Selectable privacy, locale;
        Action exit;
        string language = "";
        GuildSetupStep renderedStep = (GuildSetupStep)(-1);

        public void Initialize(Transform panel, Transform composition, Action exitAction, Selectable privacyControl, Selectable languageControl)
        {
            transform.SetParent(panel, false); PixelJournalUI.Stretch((RectTransform)transform);
            privacy = privacyControl; locale = languageControl; exit = exitAction;
            Controller = new GuildSetupController();
            Back = Action(transform, "", GoBack); Place(Back, new Rect(20, 10, 112, 52));
            title = PixelJournalUI.Text(transform, new Rect(28, 48, 514, 48), "", 34, false, TextAnchor.MiddleCenter);
            PixelJournalUI.Rule(transform, 62, 94, 446);
            subtitle = PixelJournalUI.Text(transform, new Rect(28, 99, 514, 38), "", 24, false, TextAnchor.MiddleCenter);
            body = PixelJournalUI.Scroll(transform, new Rect(28, 144, 514, 472), 476);
            viewport = (RectTransform)body.parent; Scroll = viewport.GetComponent<ScrollRect>(); StretchTrack(Scroll);
            ageGroup = PixelJournalUI.Rect("Private age and country draft", body, new Rect(0, 0, 502, 380));
            Age = PixelFormField.Create(ageGroup, PixelFormField.Kind.Text, "", "");
            Age.SetLabelColor(PixelJournalUI.Ivory); Age.Label.fontSize = 23; Age.Input.textComponent.fontSize = 24;
            Age.Background.pixelsPerUnitMultiplier = 2; Age.Input.keyboardType = TouchScreenKeyboardType.NumberPad; Age.Input.characterLimit = 16;
            Age.Input.onValueChanged.AddListener(value => Controller.SetAge(value));
            Age.Input.onSubmit.AddListener(_ => { ReleaseKeyboard(); Country.Select(); });
            countryLabel = PixelJournalUI.Text(ageGroup, new Rect(), "", 23, false);
            Country = Action(ageGroup, "", OpenCountries, true); Country.Label.fontSize = 24;
            countryError = PixelJournalUI.Text(ageGroup, new Rect(), "", 20, false); countryError.color = new Color32(255, 194, 158, 255);
            privateNote = PixelJournalUI.Text(ageGroup, new Rect(), "", 21, false);
            guidance = PixelJournalUI.Text(ageGroup, new Rect(), "", 23, false);
            characterGroup = PixelJournalUI.Rect("Choose a whole Barbarian", body, new Rect(0, 0, 502, 400));
            genderLabel = PixelJournalUI.Text(characterGroup, new Rect(0, 0, 502, 28), "", 23, false);
            Gender = PixelChoiceControl.Create(characterGroup, new[] { "male", "female" }, new[] { "Male", "Female" }, "male");
            Place(Gender, new Rect(0, 32, 502, 64)); Gender.onValueChanged.AddListener(value => Controller.ChooseGender(value));
            bodyLabel = PixelJournalUI.Text(characterGroup, new Rect(0, 108, 502, 30), "", 23, false);
            Bodies = PixelChoiceControl.Create(characterGroup, bodyIds, new[] { "Slim", "Medium", "Heavy", "Muscular" }, "medium");
            Bodies.UseSpriteCards(); Place(Bodies, new Rect(0, 148, 502, 208));
            Bodies.onValueChanged.AddListener(value => Controller.ChooseBody(value));
            cosmeticNote = PixelJournalUI.Text(characterGroup, new Rect(0, 366, 502, 32), "", 21, false, TextAnchor.MiddleCenter);
            pendingGroup = PixelJournalUI.Rect("Privacy setup checkpoint", body, new Rect(0, 0, 502, 370));
            pendingText = PixelJournalUI.Text(pendingGroup, new Rect(0, 16, 502, 270), "", 25, false, TextAnchor.UpperLeft);
            leave = Action(pendingGroup, "", Leave); Place(leave, new Rect(0, 314, 502, 64));
            Status = PixelJournalUI.Text(body, new Rect(), "", 21, false); Status.color = countryError.color;
            Continue = PixelPrimaryButton.Create(body, "", Advance); Continue.SetFontSize(30); Continue.Background.pixelsPerUnitMultiplier = 2;
            Hero = PixelJournalUI.Rect("Original PixelLab hero", composition, new Rect()).gameObject.AddComponent<Image>(); Hero.raycastTarget = false;
            Hero.transform.SetSiblingIndex(Mathf.Max(0, panel.GetSiblingIndex()));
            BuildCountries();
            Controller.Changed += Render; Controller.ExitRequested += () => exit?.Invoke(); Render();
        }
        void BuildCountries()
        {
            picker = PixelJournalUI.Frame(transform, new Rect(0, 0, 570, 632), "Country picker");
            PixelJournalUI.Stretch(picker);
            PixelJournalUI.Stretch(picker.GetComponentsInChildren<Image>()[1].rectTransform);
            CloseCountries = Action(picker, "", ClosePicker); Place(CloseCountries, new Rect(20, 10, 112, 52));
            pickerTitle = PixelJournalUI.Text(picker, new Rect(28, 60, 514, 48), "", 30, false, TextAnchor.MiddleCenter);
            Search = PixelFormField.Create(picker, PixelFormField.Kind.Text, "", "");
            Search.SetLabelColor(PixelJournalUI.Ivory); Search.Background.pixelsPerUnitMultiplier = 2; Search.Input.characterLimit = 80;
            Place(Search, new Rect(28, 114, 502, 100));
            results = PixelJournalUI.Scroll(picker, new Rect(28, 226, 514, 390), 394); StretchTrack(results.parent.GetComponent<ScrollRect>());
            noResults = PixelJournalUI.Text(results, new Rect(0, 0, 502, 60), "", 22, false);
            foreach (var country in Controller.Countries)
            {
                string code = country.code;
                countryButtons.Add(code, Action(results, code, () => { Controller.SelectCountry(code); ClosePicker(); }, true));
            }
            Search.Input.onValueChanged.AddListener(_ => FilterCountries());
            Search.Input.onSubmit.AddListener(_ => { Search.Input.DeactivateInputField(); if (visibleCountries.Count > 0) visibleCountries[0].Select(); else CloseCountries.Select(); });
            picker.gameObject.SetActive(false);
        }
        public void SetLocale(string value)
        {
            if (language == value) return;
            language = value; Controller.SetLanguage(value);
        }
        public void Open(string value, bool reset = true)
        {
            if (reset) Controller.Discard();
            SetLocale(value); Render(); ResetScroll();
        }
        public void Advance()
        {
            if (!gameObject.activeInHierarchy || CountryOpen) return;
            ReleaseKeyboard(); Controller.Continue(PixelLabRoster.TryFind(Controller.CharacterId, out _));
            ResetScroll();
            if (Controller.ErrorKey.Length > 0) { if (Age.Error.Length > 0) Age.Input.Select(); else Country.Select(); }
            else Back.Select();
        }
        public void GoBack()
        {
            if (CountryOpen) { ClosePicker(); return; }
            ReleaseKeyboard(); Controller.Back(); ResetScroll(); if (gameObject.activeInHierarchy) Back.Select();
        }
        public void Leave() { ReleaseKeyboard(); Controller.Discard(); exit?.Invoke(); }
        public void OpenCountries()
        {
            if (!gameObject.activeInHierarchy || Controller.Step != GuildSetupStep.AgeCountry) return;
            ReleaseKeyboard(); picker.gameObject.SetActive(true); Search.SetValueWithoutNotify(""); FilterCountries();
            Gate(); Search.Input.Select(); Search.Input.ActivateInputField(); StateChanged?.Invoke();
        }
        public void ClosePicker()
        {
            Search.Input.DeactivateInputField(); picker.gameObject.SetActive(false); Gate();
            if (gameObject.activeInHierarchy) Country.Select(); StateChanged?.Invoke();
        }
        void Gate()
        {
            Back.interactable = Continue.interactable = !CountryOpen;
            Age.SetInteractable(!CountryOpen); Country.interactable = !CountryOpen;
        }
        public void FilterCountries()
        {
            visibleCountries.Clear();
            var countries = new List<OnboardingOption>(Controller.Countries);
            var culture = CultureInfo.GetCultureInfo(language == "es" ? "es" : "en");
            countries.Sort((a,b) => culture.CompareInfo.Compare(a.Label(language), b.Label(language), CompareOptions.IgnoreCase));
            string query = Search.Input.text.Trim();
            foreach (var country in countries)
            {
                string label = country.Label(language); var button = countryButtons[country.code]; button.SetLabel(label);
                bool show = culture.CompareInfo.IndexOf(label, query, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0 || country.code.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                button.gameObject.SetActive(show);
                if (!show) continue;
                Place(button, new Rect(0, visibleCountries.Count * 64, 502, 60)); visibleCountries.Add(button);
            }
            noResults.gameObject.SetActive(visibleCountries.Count == 0); noResults.text = L("No countries found.", "No se encontraron países.");
            results.sizeDelta = new Vector2(502, Mathf.Max(((RectTransform)results.parent).rect.height, visibleCountries.Count * 64));
            results.anchoredPosition = Vector2.zero;
            Link(CloseCountries, visibleCountries.Count > 0 ? visibleCountries[visibleCountries.Count-1] : Search.Input, Search.Input);
            Search.SetTraversal(CloseCountries, visibleCountries.Count > 0 ? visibleCountries[0] : CloseCountries);
            for (int i=0;i<visibleCountries.Count;i++) Link(visibleCountries[i], i==0 ? Search.Input : visibleCountries[i-1], i==visibleCountries.Count-1 ? CloseCountries : visibleCountries[i+1]);
        }
        void Render()
        {
            var step = Controller.Step; bool age = step == GuildSetupStep.AgeCountry, character = step == GuildSetupStep.Character;
            ageGroup.gameObject.SetActive(age); characterGroup.gameObject.SetActive(character); pendingGroup.gameObject.SetActive(!age && !character);
            Continue.gameObject.SetActive(age || character);
            title.text = age ? L("YOUR ORIGIN", "TU ORIGEN") : character ? L("CHOOSE YOUR CHARACTER", "ELIGE TU PERSONAJE") : L("PRIVACY & CONSENT", "PRIVACIDAD Y CONSENTIMIENTO");
            title.fontSize = !age && !character ? 28 : 34;
            subtitle.text = age ? L("Basic details", "Datos básicos") : character ? L("Barbarian", "Bárbaro") : L("Next step", "Siguiente paso");
            Back.SetLabel(L("Back", "Volver")); Continue.SetLabel(L("CONTINUE", "CONTINUAR"));
            Age.SetLocalizedText(L("Age", "Edad"), L("Your age", "Tu edad"), "", "", "");
            Age.SetValueWithoutNotify(Controller.Origin.AgeText);
            string key = Controller.ErrorKey;
            string error = key == "appearance_unavailable" ? L("This appearance is unavailable.", "Este personaje no está disponible.") : Controller.Origin.Copy(key);
            bool ageError = key == "required_age" || key == "invalid_age" || key == "under_15";
            Age.SetError(ageError ? error : "");
            countryLabel.text = L("Country of residence", "País de residencia");
            Country.SetLabel(Controller.Origin.CountryCode.Length == 0 ? L("Choose your country", "Elige tu país") : Controller.Origin.CountryLabel);
            countryError.text = key.Contains("country") ? error : "";
            privateNote.text = L("These details are private.", "Estos datos son privados.");
            guidance.text = L("Next, choose your character.\nAppearance does not change your training.", "Después elegirás tu personaje.\nSu aspecto no cambia tu entrenamiento.");
            genderLabel.text = L("Character gender", "Género del personaje");
            Gender.SetLabel("male", L("Male", "Hombre")); Gender.SetLabel("female", L("Female", "Mujer")); Gender.SetValueWithoutNotify(Controller.Gender);
            bodyLabel.text = L("Choose a character", "Elige un personaje");
            string[] labels = language == "es" ? Controller.Gender == "female" ? new[] { "Delgada", "Media", "Robusta", "Muscular" } : new[] { "Delgado", "Medio", "Robusto", "Muscular" } : new[] { "Slim", "Medium", "Heavy", "Muscular" };
            for (int i=0;i<bodyIds.Length;i++)
            {
                string id = bodyIds[i]; Bodies.SetLabel(id, labels[i]);
                var option = Bodies.Option(id);
                bool available = PixelLabRoster.Place(option.Preview, Controller.Gender+"-"+id, new Vector2(((RectTransform)option.transform).rect.width*.5f, 170), .6f);
                Bodies.SetOptionInteractable(id, available);
            }
            Bodies.SetValueWithoutNotify(Controller.Body);
            PixelLabRoster.Place(Hero, Controller.CharacterId, new Vector2(336, 653), 1.95f);
            Hero.gameObject.SetActive(character && gameObject.activeInHierarchy);
            cosmeticNote.text = L("Only changes your appearance.", "Solo cambia la apariencia.");
            pendingText.text = L("Your choices are ready for the next step.\n\nPrivacy, consent and eligibility setup are not available yet in this version.\n\nNo account or profile has been saved.", "Tus elecciones están listas para el siguiente paso.\n\nLa configuración de privacidad, consentimiento y elegibilidad aún no está disponible en esta versión.\n\nNo se guardó ninguna cuenta ni perfil.");
            leave.SetLabel(L("Return to sign in", "Volver al inicio"));
            Status.text = key.Length > 0 && !ageError && countryError.text.Length == 0 ? error : "";
            Status.gameObject.SetActive(Status.text.Length > 0);
            pickerTitle.text = countryLabel.text; CloseCountries.SetLabel(L("Close", "Cerrar"));
            Search.SetLocalizedText(L("Search", "Buscar"), L("Country or code", "País o código"), "", "", "");
            if (CountryOpen) FilterCountries();
            Layout(); SetTraversal();
            if (renderedStep != step) { renderedStep = step; ResetScroll(); }
            StateChanged?.Invoke();
        }
        public void SetTraversal()
        {
            if (Controller.Step == GuildSetupStep.AgeCountry)
            { Link(Back, locale, Age.Input); Age.SetTraversal(Back, Country); Link(Country, Age.Input, Continue); Link(Continue, Country, privacy); }
            else if (Controller.Step == GuildSetupStep.Character)
            { Link(Back, locale, Gender.Options[0]); Gender.SetTraversal(Back, Bodies.Options[0]); Bodies.SetTraversal(Gender.Options[1], Continue); Link(Continue, Bodies.Options[3], privacy); }
            else { Link(Back, locale, leave); Link(leave, Back, privacy); }
        }
        public Selectable LastControl => Controller.Step == GuildSetupStep.PrivacyPending ? (Selectable)leave : Continue;
        void Layout()
        {
            float y = 0;
            if (Controller.Step == GuildSetupStep.AgeCountry)
            {
                Place(Age, new Rect(0, 0, 502, Age.PreferredHeight)); y = Age.PreferredHeight + 22;
                Place(countryLabel, new Rect(0,y,502,30)); y += 34;
                Place(Country, new Rect(0,y,502,64)); y += 66;
                Place(countryError, new Rect(0,y,502,54)); if (countryError.text.Length > 0) y += 54;
                Place(privateNote, new Rect(0,y+20,502,32)); y += 78;
                Place(guidance, new Rect(0,y,502,78)); y += 94;
            }
            else y = Controller.Step == GuildSetupStep.Character ? 406 : 390;
            if (Status.text.Length > 0) { Place(Status,new Rect(0,y,502,64)); y += 68; }
            Place(Continue, new Rect(0,y,502,64));
            body.sizeDelta = new Vector2(502, Mathf.Max(viewport.rect.height, y + (Continue.gameObject.activeSelf ? 64 : 0)));
        }
        public void Relayout(float panelHeight, bool keyboard)
        {
            viewport.sizeDelta = new Vector2(514, Mathf.Max(86,panelHeight-160));
            ((RectTransform)results.parent).sizeDelta = new Vector2(514, Mathf.Max(60,panelHeight-242));
            Layout(); Canvas.ForceUpdateCanvases();
            if (keyboard && !CountryOpen) RevealSelection();
        }
        public void RevealSelection()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(body)) return;
            var b = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
            float delta = b.min.y < viewport.rect.yMin ? viewport.rect.yMin-b.min.y : b.max.y > viewport.rect.yMax ? viewport.rect.yMax-b.max.y : 0;
            var p=body.anchoredPosition; p.y=Mathf.Clamp(p.y+delta,0,Mathf.Max(0,body.rect.height-viewport.rect.height)); body.anchoredPosition=p;
        }
        void ResetScroll() { Scroll.StopMovement(); body.anchoredPosition = Vector2.zero; }
        public void ReleaseKeyboard() { Age?.Input.DeactivateInputField(); Search?.Input.DeactivateInputField(); }
        public void Backgrounded() { ReleaseKeyboard(); if(CountryOpen) ClosePicker(); }
        void OnEnable() { if (Controller != null) Render(); }
        void OnDisable() { Backgrounded(); if(Hero != null) Hero.gameObject.SetActive(false); }
        void OnDestroy() { Controller?.Dispose(); if(Hero != null) Destroy(Hero.gameObject); }
        string L(string en,string es) => language == "es" ? es : en;
        static void StretchTrack(ScrollRect scroll)
        {
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var r=(RectTransform)scroll.verticalScrollbar.transform; r.anchorMin=new Vector2(1,0); r.anchorMax=Vector2.one; r.pivot=new Vector2(1,.5f); r.anchoredPosition=Vector2.zero; r.sizeDelta=new Vector2(7,0);
        }
        static void Place(Component c,Rect r) => PixelJournalUI.Place((RectTransform)c.transform,r);
        static PixelSecondaryAction Action(Transform parent,string text,UnityEngine.Events.UnityAction action,bool framed=false)
        {
            var b=PixelSecondaryAction.Create(parent,framed ? PixelSecondaryAction.Appearance.Framed : PixelSecondaryAction.Appearance.Text,text,action);
            b.Label.fontSize=22; b.SetHorizontalPadding(8); b.Background.pixelsPerUnitMultiplier=2; return b;
        }
        static void Link(Selectable current,Selectable previous,Selectable next)
        {
            var tab=current.GetComponent<PixelFieldTabNavigation>() ?? current.gameObject.AddComponent<PixelFieldTabNavigation>(); tab.Previous=previous; tab.Next=next;
            if (!(current is InputField)) current.navigation=new Navigation { mode=Navigation.Mode.Explicit,selectOnUp=previous,selectOnDown=next };
        }
    }
}
