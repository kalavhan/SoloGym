using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SoloGym
{
    public enum OnboardingStep { AgeRegion, Consent, Document }

    [Serializable]
    public sealed class OnboardingOption
    {
        public string code, countryCode, en, es;
        public string Code => code;
        public string Label(string language) => language == "es" ? es : en;
    }

    [Serializable]
    sealed class OnboardingRegionData
    {
        public OnboardingOption[] countries;
        public OnboardingOption[] subdivisions;
    }

    [Serializable]
    sealed class OnboardingDocumentData
    {
        public OnboardingDocument[] documents;
    }

    [Serializable]
    sealed class OnboardingDocument
    {
        public string id, language, version, title, body;
        public bool available;
    }

    public sealed class OnboardingViewModel
    {
        public string Language, AgeText, CountryCode, SubdivisionCode, CountryLabel, SubdivisionLabel;
        public string Status, Error, ErrorKey, DocumentId, DocumentTitle, DocumentBody;
        public OnboardingStep Step;
        public bool PrivacyAcknowledged, TermsAccepted, ReviewMode, CanContinue, DocumentAvailable;
        public bool HasSubdivisions;
        public string Copy(string key) => OnboardingCopy.Get(key, Language);
    }

    /// <summary>
    /// Memory-only age/residence and consent draft. Catalog entries are display data,
    /// never eligibility policy. No server policy/receipt service is configured yet,
    /// so normal mode cannot claim eligibility or persist acceptance. Review mode is
    /// explicitly fictional and can emit only a prefixed visual-review checkpoint.
    /// </summary>
    public sealed class OnboardingController : IDisposable
    {
        const string LanguageKey = "SoloGym.Home.Language.v1";
        readonly bool reviewMode;
        readonly OnboardingOption[] countries, subdivisions;
        readonly OnboardingDocument[] documents;
        string language, ageText = "", countryCode = "", subdivisionCode = "", errorKey = "", documentId = "";
        bool privacyAcknowledged, termsAccepted, disposed;
        OnboardingStep step, documentReturnStep;
        public OnboardingViewModel Model { get; private set; }
        public event Action<OnboardingViewModel> Changed;
        public event Action ExitRequested;
        public event Action<string> CheckpointRequested;
        public OnboardingOption[] Countries => (OnboardingOption[])countries.Clone();
        public OnboardingOption[] Subdivisions
        {
            get
            {
                var result = new List<OnboardingOption>();
                foreach (var item in subdivisions)
                    if (item != null && item.countryCode == countryCode) result.Add(item);
                return result.ToArray();
            }
        }

        public OnboardingController(bool reviewMode = false)
        {
            this.reviewMode = reviewMode;
            language = ResolveLanguage(PlayerPrefs.GetString(LanguageKey, "auto"));
            var regionAsset = Resources.Load<TextAsset>("Onboarding/RegionCatalog");
            var documentAsset = Resources.Load<TextAsset>("Onboarding/Documents");
            OnboardingRegionData regions = null;
            OnboardingDocumentData docs = null;
            try { if (regionAsset != null) regions = JsonUtility.FromJson<OnboardingRegionData>(regionAsset.text); }
            catch (ArgumentException) { /* Missing/malformed catalog fails closed. */ }
            try { if (documentAsset != null) docs = JsonUtility.FromJson<OnboardingDocumentData>(documentAsset.text); }
            catch (ArgumentException) { /* No substitute legal documents. */ }
            countries = regions?.countries ?? new OnboardingOption[0];
            subdivisions = regions?.subdivisions ?? new OnboardingOption[0];
            documents = docs?.documents ?? new OnboardingDocument[0];
            if (reviewMode)
            {
                ageText = "21";
                if (Find(countries, "MX") != null) countryCode = "MX";
            }
            Refresh();
        }

        public void SetLanguage(string value)
        {
            if (disposed) return;
            if (value != "es" && value != "en" && value != "auto")
                throw new ArgumentException("Language must be en, es or auto.", nameof(value));
            language = ResolveLanguage(value);
            // This stores only the shared language preference; no onboarding data.
            PlayerPrefs.SetString(LanguageKey, value);
            PlayerPrefs.Save();
            Refresh();
        }

        public void SetAge(string value)
        {
            if (disposed || step != OnboardingStep.AgeRegion) return;
            ageText = value ?? "";
            ResetDecisions();
            errorKey = "";
            Refresh();
        }

        public void SelectCountry(string code)
        {
            if (disposed || step != OnboardingStep.AgeRegion) return;
            code = NormalizeCode(code);
            if (code.Length != 0 && Find(countries, code) == null)
            {
                // Never retain an earlier valid country when a new selection fails.
                countryCode = "";
                subdivisionCode = "";
                ResetDecisions();
                Fail("country_unavailable");
                return;
            }
            if (code != countryCode)
            {
                countryCode = code;
                subdivisionCode = "";
                ResetDecisions();
            }
            errorKey = "";
            Refresh();
        }

        public void SelectSubdivision(string code)
        {
            if (disposed || step != OnboardingStep.AgeRegion) return;
            code = NormalizeCode(code);
            if (code.Length != 0 && Find(Subdivisions, code) == null)
            {
                subdivisionCode = "";
                Fail("subdivision_unavailable");
                return;
            }
            subdivisionCode = code;
            errorKey = "";
            Refresh();
        }

        public void ContinueAge()
        {
            if (disposed || step != OnboardingStep.AgeRegion) return;
            string validation = ValidateAgeAndCountry();
            if (validation.Length != 0) { Fail(validation); return; }
            // No country is silently enabled by a visual catalog or client flag.
            if (!reviewMode) { Fail("policy_review_pending"); return; }
            step = OnboardingStep.Consent;
            errorKey = "";
            Refresh();
        }

        public void SetPrivacyAcknowledged(bool value)
        {
            if (disposed || step != OnboardingStep.Consent) return;
            privacyAcknowledged = value;
            errorKey = "";
            Refresh();
        }

        public void SetTermsAccepted(bool value)
        {
            if (disposed || step != OnboardingStep.Consent) return;
            termsAccepted = value;
            errorKey = "";
            Refresh();
        }

        public void ContinueConsent()
        {
            if (disposed || step != OnboardingStep.Consent) return;
            if (!reviewMode) { Fail("policy_review_pending"); return; }
            if (!privacyAcknowledged || !termsAccepted) { Fail("required_decisions"); return; }
            // Checking review controls never creates a consent receipt, account,
            // saved flag or an authorized profile destination.
            errorKey = "review_checkpoint";
            Refresh();
            CheckpointRequested?.Invoke("REVIEW:WIN-009");
        }

        public void ReadDocument(string id)
        {
            if (disposed) return;
            if (id != "privacy" && id != "terms") throw new ArgumentException("Unknown document.", nameof(id));
            if (step != OnboardingStep.Document) documentReturnStep = step;
            documentId = id;
            step = OnboardingStep.Document;
            errorKey = "";
            Refresh();
        }

        public void Back()
        {
            if (disposed) return;
            errorKey = "";
            if (step == OnboardingStep.Document)
            {
                step = documentReturnStep;
                documentId = "";
                Refresh();
            }
            else if (step == OnboardingStep.Consent)
            {
                step = OnboardingStep.AgeRegion;
                Refresh();
            }
            else Decline();
        }

        public void Decline()
        {
            if (disposed) return;
            ResetDraft();
            Refresh();
            ExitRequested?.Invoke();
        }

        void ResetDecisions() { privacyAcknowledged = false; termsAccepted = false; }
        void ResetDraft()
        {
            ageText = countryCode = subdivisionCode = errorKey = documentId = "";
            ResetDecisions();
            step = documentReturnStep = OnboardingStep.AgeRegion;
        }
        void Fail(string key) { errorKey = key; Refresh(); }

        string ValidateAgeAndCountry()
        {
            if (ageText.Length == 0) return "required_age";
            // Reject decimals, signs, exponent notation and Unicode digit lookalikes.
            foreach (char c in ageText) if (c < '0' || c > '9') return "invalid_age";
            if (!int.TryParse(ageText, NumberStyles.None, CultureInfo.InvariantCulture, out int age)
                || age < 1 || age > 120) return "invalid_age";
            if (age < 15) return "under_15";
            if (countryCode.Length == 0) return "required_country";
            if (Find(countries, countryCode) == null) return "country_unavailable";
            if (subdivisionCode.Length != 0 && Find(Subdivisions, subdivisionCode) == null)
                return "subdivision_unavailable";
            return "";
        }

        void Refresh()
        {
            if (disposed) return;
            var country = Find(countries, countryCode);
            var subdivision = Find(Subdivisions, subdivisionCode);
            OnboardingDocument document = null;
            foreach (var entry in documents)
                if (entry != null && entry.id == documentId && entry.language == language) { document = entry; break; }
            bool available = document != null && document.available
                && !string.IsNullOrWhiteSpace(document.version) && !string.IsNullOrWhiteSpace(document.body);
            Model = new OnboardingViewModel
            {
                Language = language, AgeText = ageText, CountryCode = countryCode,
                SubdivisionCode = subdivisionCode, CountryLabel = country?.Label(language) ?? "",
                SubdivisionLabel = subdivision?.Label(language) ?? "", Step = step,
                PrivacyAcknowledged = privacyAcknowledged, TermsAccepted = termsAccepted,
                ReviewMode = reviewMode, ErrorKey = errorKey, Error = OnboardingCopy.Get(errorKey, language),
                Status = reviewMode ? OnboardingCopy.Get("review_mode", language) : "",
                CanContinue = step == OnboardingStep.AgeRegion ? ValidateAgeAndCountry().Length == 0
                    : step == OnboardingStep.Consent && reviewMode && privacyAcknowledged && termsAccepted,
                HasSubdivisions = Subdivisions.Length > 0, DocumentId = documentId,
                DocumentTitle = OnboardingCopy.Get(documentId, language), DocumentAvailable = available,
                DocumentBody = available ? document.body : OnboardingCopy.Get("documents_unavailable", language)
            };
            Changed?.Invoke(Model);
        }

        static OnboardingOption Find(OnboardingOption[] options, string code)
        {
            foreach (var item in options) if (item != null && item.code == code) return item;
            return null;
        }
        static string NormalizeCode(string value) => (value ?? "").Trim().ToUpperInvariant();
        static string ResolveLanguage(string value) => value == "en" || value == "es" ? value
            : Application.systemLanguage == SystemLanguage.Spanish ? "es" : "en";
        public void Dispose()
        {
            if (disposed) return;
            ResetDraft();
            disposed = true;
            Model = null;
            Changed = null;
            ExitRequested = null;
            CheckpointRequested = null;
        }
    }

    public static class OnboardingCopy
    {
        static readonly Dictionary<string, string[]> Values = new Dictionary<string, string[]>
        {
            { "back", new[] { "Back", "Volver" } },
            { "continue", new[] { "CONTINUE", "CONTINUAR" } },
            { "step_age", new[] { "STEP 1 · PROFILE", "PASO 1 · PERFIL" } },
            { "age_title", new[] { "YOUR ORIGIN", "TU ORIGEN" } },
            { "age_subtitle", new[] { "Age and country of residence", "Edad y país de residencia" } },
            { "age", new[] { "Age", "Edad" } },
            { "years", new[] { "years", "años" } },
            { "age_helper", new[] { "SoloGym is for people aged 15 and over.", "SoloGym es para personas de 15 años o más." } },
            { "country", new[] { "Country of residence", "País de residencia" } },
            { "subdivision", new[] { "State / province (optional)", "Estado / provincia (opcional)" } },
            { "select", new[] { "Select", "Seleccionar" } },
            { "none", new[] { "Not selected", "Sin seleccionar" } },
            { "age_private", new[] { "Your age is private. We do not use GPS.", "Tu edad es privada. No usamos GPS." } },
            { "privacy", new[] { "Privacy", "Privacidad" } },
            { "terms", new[] { "Terms", "Términos" } },
            { "step_consent", new[] { "STEP 2 · PRIVACY", "PASO 2 · PRIVACIDAD" } },
            { "consent_title", new[] { "YOUR PRIVACY\nYOUR CHOICES", "TU PRIVACIDAD\nTUS DECISIONES" } },
            { "consent_subtitle", new[] { "You choose what to share", "Tú decides qué compartir" } },
            { "private_data", new[] { "Your age and workout data are private.", "Tu edad y tus datos de entrenamiento son privados." } },
            { "social_later", new[] { "Rankings and social features are set up later.", "El ranking y las funciones sociales se configuran después." } },
            { "read_privacy", new[] { "Read privacy policy", "Leer política de privacidad" } },
            { "read_terms", new[] { "Read terms of use", "Leer términos de uso" } },
            { "ack_privacy", new[] { "I have read the privacy policy.", "He leído la política de privacidad." } },
            { "accept_terms", new[] { "I accept the terms of use.", "Acepto los términos de uso." } },
            { "no_optional", new[] { "No optional permissions enabled.", "Sin permisos opcionales activados." } },
            { "not_now", new[] { "Not now", "Ahora no" } },
            { "settings_later", new[] { "You can review your choices in Settings.", "Puedes revisar tus decisiones en Ajustes." } },
            { "review_mode", new[] { "REVIEW · fictional data; nothing saved", "REVISIÓN · datos ficticios; no se guardan" } },
            { "review_checkpoint", new[] { "Review complete. No consent was recorded. The profile step is coming next.", "Revisión terminada. No se registró consentimiento. El perfil será el próximo paso." } },
            { "required_age", new[] { "Enter your age.", "Introduce tu edad." } },
            { "invalid_age", new[] { "Enter a whole age from 1 to 120.", "Introduce una edad entera entre 1 y 120." } },
            { "under_15", new[] { "SoloGym is available from age 15. Return to the welcome screen.", "SoloGym está disponible a partir de los 15 años. Vuelve a la bienvenida." } },
            { "required_country", new[] { "Choose your country of residence.", "Selecciona tu país de residencia." } },
            { "country_unavailable", new[] { "This country is not available in this catalog.", "Este país no está disponible en este catálogo." } },
            { "subdivision_unavailable", new[] { "Choose a state/province from this country, or leave it blank.", "Selecciona un estado/provincia de este país o déjalo vacío." } },
            { "no_subdivisions", new[] { "State/province selection is not available for this country yet. This field is optional.", "La selección de estado/provincia aún no está disponible para este país. Este campo es opcional." } },
            { "policy_review_pending", new[] { "Enrollment is not available for this country yet. You can return to the welcome screen.", "El registro aún no está disponible para este país. Puedes volver a la bienvenida." } },
            { "required_decisions", new[] { "Both choices are required to continue this review.", "Marca ambas decisiones para continuar esta revisión." } },
            { "documents_unavailable", new[] { "This document is not available yet. You cannot provide real acceptance until the final document is published. No decision has been recorded.", "Este documento aún no está disponible. No puedes aceptarlo de forma real hasta que se publique el documento final. No se ha registrado ninguna decisión." } },
            { "document_close", new[] { "Return", "Regresar" } },
            { "search", new[] { "Search", "Buscar" } }
        };
        public static string Get(string key, string language)
            => Values.TryGetValue(key ?? "", out string[] value) ? value[language == "es" ? 1 : 0] : key ?? "";
    }

    /// <summary>Focused behavioral checks; callable from the native full-window smoke pass.</summary>
    public static class OnboardingStateChecks
    {
        public static string Run()
        {
            string originalLanguage = PlayerPrefs.GetString("SoloGym.Home.Language.v1", "auto");
            int passed = 0;
            try
            {
                using (var normal = new OnboardingController())
                {
                    int checkpoints = 0;
                    normal.CheckpointRequested += _ => checkpoints++;
                    Require(normal.Model.AgeText == "" && normal.Model.CountryCode == "", "real flow starts empty");
                    normal.SetAge("14"); normal.SelectCountry("MX"); normal.ContinueAge();
                    Require(normal.Model.ErrorKey == "under_15" && normal.Model.Step == OnboardingStep.AgeRegion, "age minimum"); passed++;
                    normal.SetAge("15.0"); normal.ContinueAge();
                    Require(normal.Model.ErrorKey == "invalid_age", "integer age only"); passed++;
                    normal.SetAge("15"); normal.ContinueAge();
                    Require(normal.Model.ErrorKey == "policy_review_pending" && checkpoints == 0, "teen policy fails closed");
                    normal.SetAge("21"); normal.ContinueAge();
                    Require(normal.Model.ErrorKey == "policy_review_pending" && checkpoints == 0, "adult policy fails closed"); passed++;
                    normal.SelectSubdivision("MX-NLE"); normal.SelectCountry("CA");
                    Require(normal.Model.SubdivisionCode == "", "country change clears subdivision");
                    normal.SelectSubdivision("MX-NLE");
                    Require(normal.Model.ErrorKey == "subdivision_unavailable" && normal.Model.SubdivisionCode == "", "reject foreign subdivision"); passed++;
                    normal.SelectCountry("ZZ"); normal.ContinueAge();
                    Require(normal.Model.CountryCode == "" && !normal.Model.CanContinue, "unknown country cannot keep prior selection"); passed++;
                    normal.ReadDocument("privacy");
                    Require(!normal.Model.DocumentAvailable && normal.Model.DocumentBody.Length > 0, "unavailable final document");
                    normal.Back();
                    Require(normal.Model.Step == OnboardingStep.AgeRegion && normal.Model.AgeText == "21", "reader preserves draft"); passed++;
                    normal.ContinueConsent();
                    Require(checkpoints == 0, "real flow never authorizes profile");
                }
                using (var review = new OnboardingController(true))
                {
                    string checkpoint = null;
                    int exits = 0;
                    review.CheckpointRequested += value => checkpoint = value;
                    review.ExitRequested += () => exits++;
                    review.ContinueAge();
                    Require(review.Model.Step == OnboardingStep.Consent && !review.Model.PrivacyAcknowledged && !review.Model.TermsAccepted, "unchecked initial consent");
                    review.ContinueConsent();
                    Require(checkpoint == null && review.Model.ErrorKey == "required_decisions", "unchecked cannot continue"); passed++;
                    review.SetPrivacyAcknowledged(true); review.SetTermsAccepted(true); review.SetLanguage("es");
                    review.ReadDocument("terms"); review.SetLanguage("en"); review.Back();
                    Require(review.Model.PrivacyAcknowledged && review.Model.TermsAccepted && review.Model.Language == "en", "language and reader preserve choices"); passed++;
                    review.ContinueConsent();
                    Require(checkpoint == "REVIEW:WIN-009", "review checkpoint is explicitly unauthorised"); passed++;
                    review.Back(); review.SetAge("22"); review.ContinueAge();
                    Require(!review.Model.PrivacyAcknowledged && !review.Model.TermsAccepted, "changed eligibility resets decisions"); passed++;
                    review.Decline();
                    Require(exits == 1 && review.Model.AgeText == "" && review.Model.CountryCode == "" && !review.Model.PrivacyAcknowledged && !review.Model.TermsAccepted, "decline clears unsaved draft"); passed++;
                }
            }
            finally
            {
                PlayerPrefs.SetString("SoloGym.Home.Language.v1", originalLanguage);
                PlayerPrefs.Save();
            }
            return passed + " onboarding state checks passed";
        }
        static void Require(bool condition, string context)
        {
            if (!condition) throw new InvalidOperationException("Onboarding check failed: " + context);
        }
    }
}
