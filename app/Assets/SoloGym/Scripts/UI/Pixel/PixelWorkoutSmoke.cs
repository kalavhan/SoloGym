using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Native-player checks for journal persistence, training gates and complete UI flows.</summary>
    public sealed class PixelWorkoutSmoke : MonoBehaviour
    {
        readonly List<string> checks = new List<string>(), failures = new List<string>();
        sealed class Memory : IJournalStorage
        {
            public string Data; public bool Fail; public int Writes;
            public string Read() => Data;
            public void Write(string value) { if (Fail) throw new IOException("Simulated disk failure"); Data = value; Writes++; }
        }
        void Check(bool value, string name) { checks.Add(name); if (!value) { failures.Add(name); Debug.LogError("WORKOUT CHECK FAILED: " + name); } }
        void ModelChecks()
        {
            var catalog = JsonUtility.FromJson<TrainingCatalog>(Resources.Load<TextAsset>("Training/Preview").text);
            var options = JsonUtility.FromJson<JournalOptions>(Resources.Load<TextAsset>("Training/JournalOptions").text);
            TrainingSmoke.Verify(catalog); Check(true, "Existing 60 training fixtures and readiness gates");
            var memory = new Memory(); var day = new DateTime(2026, 9, 30);
            var journal = new WorkoutJournal(catalog, options, memory, "adult_gym_intermediate", day); journal.Load();
            Check(journal.Loaded && journal.Entries.Length == 3 && journal.CanPrepare, "Examples load with today's pending routine");
            Check(journal.Visible(true).Length == 2 && journal.Visible(true,"missed").Length == 1, "History separates completion and missed days");
            string original = journal.Export(); journal.BeginEdit(); journal.Draft.title = "Test journal"; journal.SetDuration(40);
            var choice = journal.Choices("squat").First(x => x.block.exercise_id != journal.Plan(journal.Draft).blocks.First(b => b.id == "squat").exercise_id);
            journal.SetSwap(choice.block.id, choice.block.exercise_id);
            Check(journal.Plan(journal.Draft).blocks.Any(b => b.exercise_id == choice.block.exercise_id), "Eligible exercise swap updates actual plan");
            journal.SetDuration(25); journal.SetSwap("squat", "leg_press");
            string budgetDraft = JsonUtility.ToJson(journal.Draft); bool budgetRejected = false;
            try { journal.SetSwap("hinge", "single_leg_glute_bridge"); } catch (ArgumentException) { budgetRejected = true; }
            Check(budgetRejected && JsonUtility.ToJson(journal.Draft) == budgetDraft, "Combined individually eligible swaps cannot exceed session budget");
            journal.SetDuration(40); journal.SetSwap(choice.block.id, choice.block.exercise_id);
            var draft = JsonUtility.ToJson(journal.Draft); bool rejected = false;
            try { journal.SetSwap("squat", "missing"); } catch (ArgumentException) { rejected = true; }
            Check(rejected && JsonUtility.ToJson(journal.Draft) == draft, "Invalid exercise swap is atomic");
            memory.Fail = true;
            Check(!journal.SaveEdit(out _) && journal.Draft != null && journal.Export() == original && memory.Data == null, "Failed save preserves stored state and edit draft");
            memory.Fail = false; Check(journal.SaveEdit(out _) && journal.Draft == null, "Retry commits draft after successful disk write");
            var restored = new WorkoutJournal(catalog, options, memory, "adult_gym_intermediate", day); restored.Load();
            Check(restored.Selected.title == "Test journal" && restored.Selected.minutes == 40 && restored.Selected.swaps.Length == 1, "Local edits survive restart");
            journal.BeginEdit(); journal.Draft.title = "Discard"; journal.CancelEdit(); Check(journal.Selected.title == "Test journal", "Cancel keeps saved plan unchanged");
            journal.Select("sample-completed"); string snapshot = JsonUtility.ToJson(journal.Selected);
            rejected = false; try { journal.BeginEdit(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Completed history is immutable");
            journal.BeginCreate(day, true); Check(journal.Draft.id != "sample-completed" && journal.Draft.status == "planned", "New draft never rewrites completed history"); journal.CancelEdit();
            Check(JsonUtility.ToJson(journal.Selected) == snapshot, "Completed prescription snapshot remains exact");
            journal.Select("sample-missed"); Check(!journal.CanEdit(journal.Selected) && !journal.CanPrepare, "Missed session creates no catch-up training");
            journal.BeginCreate(day, true); Check(journal.Draft.id != "sample-missed" && journal.Draft.date == WorkoutJournal.Date(day), "Missed routine can become a separate new draft"); journal.CancelEdit();
            journal.Select("sample-today"); journal.BeginPrepare(); Check(!journal.Gate.CanReview, "Readiness has no preselected answer");
            journal.Gate.SetReadiness("ready"); Check(journal.ReviewReadiness() && !journal.SaveReview(out _), "Readiness review requires acknowledgment");
            journal.Gate.Acknowledge(true); Check(journal.SaveReview(out _) && journal.LastReview != null && journal.Selected.status == "planned", "Saving a review never records exercise completion");
            journal.BeginPrepare(); Check(!journal.Gate.CanReview && !journal.CanSaveReview, "Reopening invalidates the previous readiness answer");
            journal.Gate.SetReadiness("ready"); journal.ReviewReadiness(); journal.Gate.SetReadiness("low_energy"); journal.Gate.Review(); journal.Gate.Acknowledge(true);
            Check(!journal.CanSaveReview && !journal.SaveReview(out _), "Changed gate context cannot save a stale prepared plan");
            journal.ReviewReadiness(); Check(journal.PreparedPlan.difficulty_effective == "light", "Low energy uses the existing lighter prescription");
            foreach (var state in new[] { "pain", "injury", "ill" })
            { journal.BeginPrepare(); journal.Gate.SetReadiness(state); journal.ReviewReadiness(); journal.Gate.Acknowledge(true); Check(journal.PreparedPlan.blocks.Length == 0 && !journal.SaveReview(out _), "Recovery blocks training: " + state); }
            var teen = new WorkoutJournal(catalog, options, new Memory(), "teen_home_supervised", day); teen.Load(); teen.BeginPrepare(); teen.Gate.SetReadiness("ready");
            Check(!teen.ReviewReadiness(), "Teen must answer supervision question"); teen.Gate.SetSupervision(false); teen.ReviewReadiness(); Check(!teen.CanSaveReview && teen.PreparedPlan.status == "needs_review", "Teen without supervision cannot accept strength routine");
            teen.BeginPrepare(); teen.Gate.SetReadiness("ready"); teen.Gate.SetSupervision(true); teen.ReviewReadiness(); teen.Gate.Acknowledge(true); Check(teen.SaveReview(out _), "Supervised teen example can be reviewed");
            var malformed = new Memory { Data = "{not json" }; var bad = new WorkoutJournal(catalog, options, malformed, "adult_gym_intermediate", day); bad.Load();
            Check(!bad.Loaded && malformed.Writes == 0 && malformed.Data == "{not json", "Corrupt file is preserved without silent reset");
            malformed.Data = original; bad.Load(); Check(bad.Loaded, "Retry loads recovered journal");
            var document = JsonUtility.FromJson<JournalDocument>(original); document.entries.First(e => e.status == "completed").completedPlan.blocks[0].name = null;
            malformed.Data = JsonUtility.ToJson(document); bad.Load(); Check(!bad.Loaded && malformed.Writes == 0, "Malformed history snapshot is rejected safely");
            document = JsonUtility.FromJson<JournalDocument>(original); document.profileId = "teen_home_supervised"; malformed.Data = JsonUtility.ToJson(document); bad.Load(); Check(!bad.Loaded, "Journal cannot cross adult/teen fixture profiles");
            var blank = new WorkoutJournal(catalog, options, new Memory(), "adult_gym_intermediate", day); blank.Load(true); Check(blank.Loaded && blank.Entries.Length == 0, "Empty journal is valid");
            blank.BeginCreate(day); blank.Draft.date = WorkoutJournal.Date(day.AddDays(-1)); Check(!blank.SaveEdit(out _), "Draft cannot be backdated");
            string path = Path.Combine(Application.temporaryCachePath, "journal-smoke-" + Guid.NewGuid().ToString("N") + ".json");
            var disk = new JournalFileStorage(path); disk.Write(original); disk.Write(journal.Export()); Check(disk.Read() == journal.Export() && !File.Exists(path + ".tmp"), "Atomic disk replacement roundtrip"); File.Delete(path);
            // Every exported individual substitution must remain loadable in its authored context.
            foreach (var p in catalog.profiles)
            {
                var j = new WorkoutJournal(catalog, options, new Memory(), p.id, day); j.Load(); j.BeginEdit();
                foreach (int minutes in new[] { 15, 25, 40 }) foreach (var o in options.options.Where(o => o.key == j.Key(minutes)))
                { j.SetDuration(minutes); j.SetSwap(o.block.id, o.block.exercise_id); var plan = j.Plan(j.Draft); Check(plan.estimated_seconds <= plan.budget_seconds, "Eligible substitution budget: " + o.key + "/" + o.block.exercise_id); }
            }
        }
        void Tap(PixelWorkoutWindow window, string id, bool pointer = false)
        {
            Check(window.Controls.ContainsKey(id), "Control exists: " + id); if (!window.Controls.TryGetValue(id, out var s)) return;
            Check(s.IsInteractable(), "Control enabled: " + id);
            if (pointer)
            {
                Canvas.ForceUpdateCanvases(); var rect = (RectTransform)s.transform;
                var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
                Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == s, "Pointer raycast reaches control: " + id);
                if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Selectable>() != s) Debug.LogWarning("Raycast " + id + " at " + data.position + ": " + string.Join(" | ", hits.Select(h => h.gameObject.name + " parent=" + h.gameObject.GetComponentInParent<Selectable>()?.name + " depth=" + h.depth)) + " root depth=" + s.GetComponent<Image>().depth + " cull=" + s.GetComponent<Image>().canvasRenderer.cull + " ray=" + s.GetComponent<Image>().raycastTarget);
                ExecuteEvents.Execute(s.gameObject, data, ExecuteEvents.pointerClickHandler);
            }
            else ExecuteEvents.Execute(s.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
        void TextFits(PixelWorkoutWindow window, string state)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var label in window.Composition.GetComponentsInChildren<Text>())
                if (label.gameObject.activeInHierarchy && !string.IsNullOrEmpty(label.text) && label.GetComponentInParent<InputField>() == null)
                    Check(label.preferredHeight <= label.rectTransform.rect.height + 2, state + " text fits: " + label.text.Replace("\n", " / "));
        }
        IEnumerator Shot(PixelWorkoutWindow window, string suffix)
        {
            yield return null; yield return new WaitForEndOfFrame();
            string path = PixelWorkoutWindow.Arg("-sologym-capture"); if (!string.IsNullOrEmpty(path)) window.Capture(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-" + suffix + ".png"));
        }
        public IEnumerator Run(PixelWorkoutWindow window)
        {
            try { ModelChecks(); } catch (Exception e) { Check(false, "Model checks exception: " + e); }
            Check(window.Journal?.Loaded == true, "Window loaded"); if (window.Journal?.Loaded != true) { Finish(); yield break; }
            var j = window.Journal; TextFits(window,"hub");
            Tap(window, "history", true); Tap(window, "filter-missed"); Check(window.Controls.ContainsKey("entry-sample-missed"), "Missed history visible"); TextFits(window,"history"); yield return Shot(window,"history");
            Tap(window, "week"); j.Select("sample-today"); window.Render();
            Tap(window,"full"); TextFits(window,"full"); yield return Shot(window,"full"); Tap(window,"back-preview"); yield return null; yield return new WaitForEndOfFrame();
            Tap(window,"edit",true); Check(window.View == "edit", "Pointer opens editor");
            ((InputField)window.Controls["edit-name"]).text = "Sesión personal"; Tap(window,"duration-40");
            Check(j.Draft.title == "Sesión personal" && j.Draft.minutes == 40, "Live text and time controls update draft");
            var swapId = window.Controls.FirstOrDefault(x => x.Key.StartsWith("swap-") && x.Value.IsInteractable()).Key;
            if (swapId != null) { Tap(window, swapId); Check(j.Draft.swaps.Length == 1, "Live swap control updates draft"); }
            TextFits(window,"editor"); yield return Shot(window,"editor");
            var scroll = window.Composition.GetComponentInChildren<ScrollRect>();
            Check(scroll.verticalScrollbar.handleRect.rect.height < scroll.viewport.rect.height, "Scrollbar thumb represents scrollable content");
            float beforeScroll = scroll.content.anchoredPosition.y;
            scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -3) });
            Check(scroll.content.anchoredPosition.y > beforeScroll, "Native wheel scroll reveals exercises");
            var lastSwap = window.Controls.LastOrDefault(x => x.Key.StartsWith("swap-")).Value;
            if (lastSwap != null) { lastSwap.Select(); yield return null; yield return new WaitForEndOfFrame(); var b = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, lastSwap.transform);
                Check(b.min.y >= scroll.viewport.rect.yMin - 1 && b.max.y <= scroll.viewport.rect.yMax + 1, "Keyboard focus reveals offscreen exercise action"); }
            scroll.verticalNormalizedPosition = 1; scroll.StopMovement();
            Tap(window,"cancel-edit"); Check(window.HasModal && !window.Controls["save-edit"].IsInteractable(), "Discard modal blocks underlying keyboard controls");
            ExecuteEvents.Execute(GameObject.Find("modal-keep"), new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(!window.HasModal && j.Draft != null, "Keep editing preserves draft");
            Tap(window,"save-edit"); Check(window.View == "hub" && j.Selected.title == "Sesión personal", "UI save returns to selected updated entry");
            Tap(window,"prepare"); Check(!window.Controls["review-readiness"].IsInteractable(), "UI readiness gate cannot be skipped");
            Tap(window,"ready-pain"); if (j.IsTeen) Tap(window,"unsupervised"); Tap(window,"review-readiness");
            Check(window.View == "review" && !window.Controls.ContainsKey("save-review"), "Pain shows recovery without accept action"); TextFits(window,"recovery"); yield return Shot(window,"recovery");
            Tap(window,"back-readiness"); Tap(window,"ready-ready"); if (j.IsTeen) Tap(window,"supervised"); TextFits(window,"readiness"); yield return Shot(window,"readiness");
            Tap(window,"review-readiness"); Tap(window,"acknowledge"); TextFits(window,"review"); yield return Shot(window,"review"); Tap(window,"save-review");
            Check(j.Reviewed && j.Selected.status == "planned", "UI saves only review and leaves routine pending"); Tap(window,"back-readiness");
            Tap(window,"settings"); Tap(window,window.Language == "es" ? "locale-en" : "locale-es"); TextFits(window,"settings"); Tap(window,"done-settings");
            // Restore the initial visual fixture without changing normal user data; smoke runs require a dedicated file.
            j.BeginEdit(); j.Draft.title = ""; j.SetDuration(25); j.SaveEdit(out _); window.SetLanguage(PixelWorkoutWindow.Arg("-sologym-locale","es")); window.Render();
            var corners = new Vector3[4]; window.Composition.GetWorldCorners(corners);
            Check(corners[0].x >= -.1f && corners[0].y >= -.1f && corners[2].x <= Screen.width + .1f && corners[2].y <= Screen.height + .1f, "Full composition fits landscape safe area");
            Check(window.Controls.Values.All(s => s.GetComponent<PixelFieldTabNavigation>() != null), "All native controls participate in keyboard traversal");
            foreach(var i in window.Composition.GetComponentsInChildren<Image>()) if(i.name.Contains("Rooms/WorkoutsR1")) Check(i.sprite != null, "Native window art loaded: " + i.name);
            TextFits(window,"final");
            window.Composition.gameObject.SetActive(false);
            yield return UnavailableChecks(window.Language);
            var home = new GameObject("Home integration check").AddComponent<PixelTrainingHallHome>(); home.AutomaticReview = false;
            home.SelectCharacter("female-fat", false); var child = home.OpenWorkouts(true);
            for(int i=0;i<6;i++) yield return null;
            Check(!home.Composition.gameObject.activeSelf && child.View == "readiness", "Home training action opens journal readiness");
            Check(child.Journal.IsTeen == home.Controller.Model.IsPrivateProfile, "Home preserves profile age gate");
            Check(child.Composition.GetComponentsInChildren<Image>().Any(x => x.sprite != null && x.sprite.name.Contains("female-fat")), "Home preserves selected appearance in journal portrait");
            child.LeaveToHub(); child.SetLanguage("en"); Tap(child,"nav-home"); yield return null;
            Check(home.Composition.gameObject.activeSelf && home.Controller.Model.Language == "en", "Return to Home restores room and chosen language");
            home.gameObject.SetActive(false); Destroy(home.gameObject);
            window.Composition.gameObject.SetActive(true); window.Render();
            Finish();
        }
        IEnumerator UnavailableChecks(string locale)
        {
            var memory = new Memory { Data = "invalid json" };
            var errorWindow = new GameObject("Journal recovery state test").AddComponent<PixelWorkoutWindow>(); errorWindow.AutomaticReview = false;
            errorWindow.Initialize(locale, "male-medium", false, () => { }, memory);
            Check(errorWindow.View == "loading" && !errorWindow.Controls["settings"].IsInteractable(), "Loading has no premature settings interaction");
            for(int i=0;i<4;i++) yield return null;
            Check(errorWindow.View == "error" && memory.Writes == 0, "Corrupt journal has a recoverable UI"); TextFits(errorWindow,"error"); yield return Shot(errorWindow,"error");
            memory.Data = null; Tap(errorWindow,"retry"); for(int i=0;i<4;i++) yield return null;
            Check(errorWindow.View == "hub", "Retry restores the same journal window");
            errorWindow.Journal.SelectDay(errorWindow.Journal.Today.AddDays(9)); errorWindow.Render(); TextFits(errorWindow,"empty");
            Check(errorWindow.Controls.ContainsKey("create"), "Empty calendar day offers planning"); yield return Shot(errorWindow,"empty");
            Tap(errorWindow,"create"); Check(errorWindow.Journal.Draft != null, "Create from empty day opens a real draft");
            Tap(errorWindow,"cancel-edit");
            ExecuteEvents.Execute(GameObject.Find("modal-discard"), new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(errorWindow.Journal.Draft == null && !errorWindow.HasModal, "Discard abandons only the unsaved draft");
            errorWindow.gameObject.SetActive(false); Destroy(errorWindow.gameObject); yield return null;
        }
        void Finish()
        {
            string path = PixelWorkoutWindow.Arg("-sologym-capture");
            var report = new Report { passed = failures.Count == 0, checkCount = checks.Count, width = Screen.width, height = Screen.height, checks = checks.ToArray(), failures = failures.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            if (!string.IsNullOrEmpty(path)) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(Path.ChangeExtension(path,".smoke.json"), json); }
            Debug.Log("WORKOUT_SMOKE " + json); if(failures.Count > 0) Application.Quit(2);
        }
        [Serializable] sealed class Report { public bool passed; public int checkCount,width,height; public string[] checks,failures; }
    }
}
