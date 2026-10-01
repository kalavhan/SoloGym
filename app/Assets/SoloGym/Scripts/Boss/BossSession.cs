using System;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    [Serializable] public sealed class BossVariant { public string key; public TrainingPlan plan; public JournalOption[] options; }
    [Serializable] public sealed class BossCatalog { public int version; public string context; public BossVariant[] entries; }
    [Serializable] public sealed class BossLog
    {
        public string id, blockId, recordedUtc, correctedUtc;
        public int index;
        public double quantity, loadKg, share;
        public bool hasLoad;
        public TrainingBlock prescription;
    }
    [Serializable] public sealed class BossSession
    {
        public int version = 1;
        public string id, entryId, profile, date, context = "review-fixtures-only", readiness, difficulty, state = "active", stopReason = "";
        public TrainingPlan original, plan;
        public BossLog[] logs = Array.Empty<BossLog>();
        public int cursor;
        public double restUntil, pausedRest;
        public bool paused;
        public static void Validate(BossSession s)
        {
            if (s == null || s.version != 1 || s.context != "review-fixtures-only" || string.IsNullOrEmpty(s.id) || string.IsNullOrEmpty(s.entryId)
                || !new[] { "active", "completed", "stopped" }.Contains(s.state) || !new[] { "ready", "low_energy" }.Contains(s.readiness)
                || !new[] { "light", "medium", "hard" }.Contains(s.difficulty) || s.plan?.blocks == null || s.original?.blocks == null
                || s.plan.blocks.Length != s.original.blocks.Length || s.plan.blocks.Length < 2 || s.plan.blocks.Length > 30
                || s.logs == null || s.logs.Length > 90 || s.cursor < 0 || s.cursor > s.plan.blocks.Length
                || !Finite(s.restUntil) || !Finite(s.pausedRest) || s.restUntil < 0 || s.pausedRest < 0 || s.pausedRest > 3600)
                throw new ArgumentException("Invalid saved dungeon session.");
            var ids = s.plan.blocks.Select(b => b?.id).ToArray();
            if (ids.Any(string.IsNullOrEmpty) || ids.Distinct().Count()!=ids.Length || s.original.blocks.Sum(b=>b.boss_share)!=1000
                || s.plan.blocks.Any(b => b.name == null || string.IsNullOrEmpty(b.name.en) || string.IsNullOrEmpty(b.name.es) || b.quantity_min<1 || b.quantity_max<b.quantity_min || b.sets<1 || b.sets>3 || b.rest_seconds<0 || b.rest_seconds>600
                    || !new[]{"reps","seconds","minutes"}.Contains(b.unit) || !new[]{"warmup","main","cooldown"}.Contains(b.role))
                || s.logs.Select(l=>l?.id).Distinct().Count()!=s.logs.Length)
                throw new ArgumentException("Invalid dungeon prescription.");
            foreach (var b in s.plan.blocks)
            {
                var original = s.original.blocks.FirstOrDefault(x=>x.id==b.id);
                var logs = s.logs.Where(l=>l?.blockId==b.id).OrderBy(l=>l.index).ToArray();
                if (original == null || original.exercise_id!=b.exercise_id || logs.Length>b.sets || logs.Sum(l=>l.share)>original.boss_share+.00001
                    || logs.Where((l,i)=>l.index!=i).Any()) throw new ArgumentException("Invalid dungeon ledger.");
            }
            foreach (var l in s.logs)
                if (l==null || string.IsNullOrEmpty(l.id) || !ids.Contains(l.blockId) || !Finite(l.quantity) || l.quantity<0 || l.quantity>10000
                    || !Finite(l.loadKg) || l.loadKg<0 || l.loadKg>1000 || !Finite(l.share) || l.share<0 || l.prescription?.quantity_min<1
                    || l.prescription==null || l.prescription.id!=l.blockId || l.prescription.exercise_id!=s.plan.blocks.First(b=>b.id==l.blockId).exercise_id)
                    throw new ArgumentException("Invalid manual log.");
            for (int i=0;i<s.cursor;i++) if (Count(s,s.plan.blocks[i].id)!=s.plan.blocks[i].sets) throw new ArgumentException("Skipped prescription.");
            if (s.logs.Any(l=>Array.IndexOf(ids,l.blockId)>s.cursor) || (s.state=="completed" && s.cursor!=s.plan.blocks.Length)) throw new ArgumentException("Invalid session position.");
        }
        public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
        public static int Count(BossSession s, string id) => s.logs.Count(l=>l.blockId==id);
    }

    /// <summary>Local manual exercise ledger. No trusted rewards or automatic exercise detection.</summary>
    public sealed class BossController
    {
        readonly BossCatalog catalog;
        readonly Action<BossSession> persist;
        readonly Func<double> now;
        public BossSession Data { get; private set; }
        public string Error { get; private set; }
        public bool NeedsReadiness { get; private set; }
        public TrainingBlock Current => Data.cursor<Data.plan.blocks.Length ? Data.plan.blocks[Data.cursor] : null;
        public bool Closed => Data.state!="active";
        public int Damage => Math.Min(1000,(int)Math.Floor(Data.logs.Sum(l=>l.share*Math.Min(1,l.quantity/l.prescription.quantity_min))+.000001));
        public double RestLeft => Data.paused ? Data.pausedRest : Math.Max(0, Data.restUntil-now());
        public bool CanLog => !Closed && !NeedsReadiness && !Data.paused && RestLeft<=0 && Current!=null;
        public string NextId => Current==null ? "" : Data.id+":"+Current.id+":"+BossSession.Count(Data,Current.id);
        public static double UtcNow() => (DateTime.UtcNow-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalSeconds;
        public BossController(BossSession data, BossCatalog source, Action<BossSession> save, bool restored, Func<double> clock=null)
        {
            BossSession.Validate(data); catalog=source; persist=save; now=clock??UtcNow; Data=WorkoutJournal.Clone(data);
            NeedsReadiness=restored&&!Closed;
            if (NeedsReadiness) { Data.pausedRest=Data.paused ? Data.pausedRest : Math.Max(0,Data.restUntil-now()); Data.paused=true; }
            // Reject stale/unsupported exercise identities before a saved prescription can be resumed.
            Resolve(Data,Data.difficulty,Data.readiness);
        }
        static string Key(BossSession s,string difficulty,string readiness) => s.profile+":"+(s.original.budget_seconds/60)+":"+readiness+":"+(s.profile=="teen_home_supervised"?"1":"0")+":"+difficulty;
        TrainingPlan Resolve(BossSession s,string difficulty,string readiness)
        {
            var variant=catalog.entries.FirstOrDefault(v=>v.key==Key(s,difficulty,readiness));
            if (variant?.plan.status!="draft_ready") throw new ArgumentException("Unsupported training context.");
            var result=WorkoutJournal.Clone(variant.plan);
            foreach (var original in s.original.blocks.Where(b=>b.role=="main"))
            {
                int index=Array.FindIndex(result.blocks,b=>b.id==original.id);
                var option=variant.options.FirstOrDefault(o=>o.block.id==original.id&&o.block.exercise_id==original.exercise_id);
                if(index<0||option==null) throw new ArgumentException("Exercise needs a new readiness review.");
                result.estimated_seconds+=option.secondsDelta; result.blocks[index]=WorkoutJournal.Clone(option.block);
            }
            if(result.blocks.Select(b=>b.exercise_id).Distinct().Count()!=result.blocks.Length) throw new ArgumentException("Duplicate exercise.");
            return result;
        }
        public static BossSession Create(WorkoutJournal journal, BossCatalog catalog)
        {
            if(!journal.CanSaveReview) throw new InvalidOperationException("Review and acknowledge today's routine first.");
            var s=new BossSession {id=Guid.NewGuid().ToString("N"),entryId=journal.SelectedId,profile=journal.ProfileId,date=WorkoutJournal.Date(journal.Today),readiness=journal.Gate.Readiness,
                difficulty=journal.PreparedPlan.difficulty_effective,original=WorkoutJournal.Clone(journal.PreparedPlan),plan=WorkoutJournal.Clone(journal.PreparedPlan)};
            var controller=new BossController(s,catalog,_=>{},false);
            s.plan=controller.Resolve(s,s.difficulty,s.readiness); s.original=WorkoutJournal.Clone(s.plan);
            if(s.plan.estimated_seconds>s.plan.budget_seconds) throw new ArgumentException("Plan exceeds the reviewed time budget.");
            BossSession.Validate(s); return s;
        }
        bool Change(Action<BossSession> change)
        {
            try { var candidate=WorkoutJournal.Clone(Data); change(candidate); BossSession.Validate(candidate); persist(candidate); Data=candidate; Error=null; return true; }
            catch(Exception e) when(e is ArgumentException||e is InvalidOperationException||e is System.IO.IOException||e is UnauthorizedAccessException)
            { Error=e.Message; return false; }
        }
        public bool Log(string expectedId,double quantity,double? load)
        {
            // A retry refers to the original set, never the next one.
            if(Data.logs.Any(l=>l.id==expectedId)) return true;
            if(!CanLog||expectedId!=NextId) { Error="This set is no longer active."; return false; }
            return Change(s=> {
                var b=s.plan.blocks[s.cursor]; ValidateQuantity(quantity,load);
                int count=BossSession.Count(s,b.id);
                double used=s.logs.Where(l=>l.blockId==b.id).Sum(l=>l.share);
                double budget=s.original.blocks.First(x=>x.id==b.id).boss_share;
                var log=new BossLog {id=expectedId,blockId=b.id,index=count,quantity=quantity,hasLoad=load.HasValue,loadKg=load??0,
                    share=Math.Max(0,budget-used)/(b.sets-count),prescription=WorkoutJournal.Clone(b),recordedUtc=DateTime.UtcNow.ToString("O")};
                s.logs=s.logs.Concat(new[]{log}).ToArray();
                bool anotherSet=count+1<b.sets;
                s.restUntil=anotherSet ? now()+b.rest_seconds : 0;
                if(!anotherSet) s.cursor++;
                if(s.cursor==s.plan.blocks.Length) s.state="completed";
            });
        }
        static void ValidateQuantity(double quantity,double? load)
        { if(!BossSession.Finite(quantity)||quantity<0||quantity>10000||(load.HasValue&&(!BossSession.Finite(load.Value)||load.Value<0||load.Value>1000))) throw new ArgumentException("Invalid quantity or optional load."); }
        public bool Correct(string id,double quantity,double? load) => Change(s=> {
            ValidateQuantity(quantity,load); var l=s.logs.FirstOrDefault(x=>x.id==id)??throw new ArgumentException("Unknown record.");
            l.quantity=quantity;l.hasLoad=load.HasValue;l.loadKg=load??0;l.correctedUtc=DateTime.UtcNow.ToString("O");
        });
        public bool Pause() => Closed||NeedsReadiness ? false : Change(s=> { if(!s.paused){s.pausedRest=RestLeft;s.paused=true;} });
        public bool Resume() => Closed||NeedsReadiness ? false : Change(s=> { if(s.paused){s.restUntil=now()+s.pausedRest;s.pausedRest=0;s.paused=false;} });
        public bool Stop(string reason) => Closed ? false : Change(s=> {s.state="stopped";s.stopReason=reason;s.paused=false;s.restUntil=s.pausedRest=0;});
        void Adjust(BossSession s,string difficulty,string readiness)
        {
            if(Closed) throw new InvalidOperationException("Session is closed.");
            var next=Resolve(s,difficulty,readiness);
            for(int i=0;i<next.blocks.Length;i++)
            {
                var b=next.blocks[i];var before=s.plan.blocks[i];int done=BossSession.Count(s,b.id);
                if(i<s.cursor||b.role!="main") next.blocks[i]=WorkoutJournal.Clone(before);
                else if(done>b.sets) { b.estimated_seconds+=(done-b.sets)*b.additional_set_seconds;b.sets=done; }
            }
            next.estimated_seconds=next.blocks.Sum(b=>b.estimated_seconds);
            if(next.estimated_seconds>next.budget_seconds) throw new ArgumentException("Remaining work would exceed the reviewed time budget.");
            s.plan=next;s.difficulty=next.difficulty_effective;s.readiness=readiness;
            // Retiring a remaining set never awards its unused share or reopens a completed block.
            while(s.cursor<next.blocks.Length&&BossSession.Count(s,next.blocks[s.cursor].id)>=next.blocks[s.cursor].sets) s.cursor++;
        }
        public bool SetDifficulty(string value) => NeedsReadiness ? false : Change(s=>Adjust(s,value,s.readiness));
        public bool Recheck(string readiness,bool supervised)
        {
            if(!NeedsReadiness) return false;
            if(new[]{"pain","injury","ill"}.Contains(readiness)|| (Data.profile=="teen_home_supervised"&&!supervised))
            { bool stopped=Stop(readiness=="ready"?"supervision":readiness);if(stopped)NeedsReadiness=false;return stopped; }
            bool saved=Change(s=> {Adjust(s,s.difficulty,readiness);s.paused=true;});
            if(saved) NeedsReadiness=false; return saved;
        }
    }
}
