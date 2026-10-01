using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoloGym
{
    /// <summary>Explicit placeholder content only. Never a source of production policy or permits.</summary>
    public sealed class ReviewDocumentCatalog
    {
        [Serializable] public sealed class Document
        {
            public string id, language, title, reviewRevision, body;
            public bool isFinal;
        }
        [Serializable] sealed class Data
        {
            public int schema;
            public string status;
            public bool production_acceptance_enabled;
            public Document[] documents;
        }
        readonly Dictionary<string, Document> documents = new Dictionary<string, Document>();
        public static ReviewDocumentCatalog Load()
        {
            var text = Resources.Load<TextAsset>("Onboarding/ReviewDocuments");
            return new ReviewDocumentCatalog(text == null ? null : text.text);
        }
        public ReviewDocumentCatalog(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                var data = JsonUtility.FromJson<Data>(json);
                if (data == null || data.schema != 1 || data.status != "review_placeholder_user_authorized" || data.production_acceptance_enabled || data.documents == null) return;
                var duplicates = new HashSet<string>();
                foreach (var d in data.documents)
                {
                    if (d == null || (d.id != "privacy" && d.id != "terms") || (d.language != "en" && d.language != "es")) continue;
                    string key = d.id + ":" + d.language;
                    if (documents.ContainsKey(key)) { duplicates.Add(key); continue; }
                    if (!d.isFinal && !string.IsNullOrWhiteSpace(d.title) && !string.IsNullOrWhiteSpace(d.body) && !string.IsNullOrWhiteSpace(d.reviewRevision)) documents.Add(key,d);
                }
                foreach (string key in duplicates) documents.Remove(key);
            }
            catch (ArgumentException) { /* Unavailable documents keep preview continuation disabled. */ }
        }
        public Document Find(string id, string language) => documents.TryGetValue(id+":"+language, out var d) ? d : null;
    }

    /// <summary>Memory-only demonstration choices. No persistence, legal receipts, eligibility or auth calls.</summary>
    public sealed class ConsentReviewController
    {
        readonly ReviewDocumentCatalog catalog;
        public string Language { get; private set; } = "es";
        public bool PrivacyChecked { get; private set; }
        public bool TermsChecked { get; private set; }
        public bool DocumentsAvailable => Document("privacy") != null && Document("terms") != null;
        public bool CanContinue => DocumentsAvailable && PrivacyChecked && TermsChecked;
        public event Action Changed;
        public ConsentReviewController(ReviewDocumentCatalog documents = null) { catalog = documents ?? ReviewDocumentCatalog.Load(); }
        public ReviewDocumentCatalog.Document Document(string id) => catalog.Find(id, Language);
        public void SetLanguage(string language)
        {
            if (language != "es" && language != "en") throw new ArgumentException("Unsupported document language.");
            if (Language == language) return;
            Language = language; Changed?.Invoke();
        }
        public void ChoosePrivacy(bool value) { PrivacyChecked = value && Document("privacy") != null; Changed?.Invoke(); }
        public void ChooseTerms(bool value) { TermsChecked = value && Document("terms") != null; Changed?.Invoke(); }
        public void Reset() { PrivacyChecked = TermsChecked = false; Changed?.Invoke(); }
    }
}
