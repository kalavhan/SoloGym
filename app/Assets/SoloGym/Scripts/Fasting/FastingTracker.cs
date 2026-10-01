using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SoloGym
{
    [Serializable] public sealed class FastingEntry
    {
        public string id;
        public long startUtc, endUtc;
        public int planMinutes;
    }
    [Serializable] public sealed class FastingDocument
    {
        public int version;
        public string context;
        public bool enabled, reducedMotion;
        public int planMinutes;
        public FastingEntry active;
        public FastingEntry[] records;
    }

    /// <summary>Optional local adult log. Wall-clock records, no training/reward dependencies.</summary>
    public sealed class FastingTracker
    {
        readonly IJournalStorage storage;
        readonly Func<DateTimeOffset> clock;
        FastingDocument data;
        public bool Eligible { get; }
        public bool Loaded => data != null;
        public string Error { get; private set; }
        public FastingDocument Data => Clone(data);
        public bool ReducedMotion => data?.reducedMotion ?? true;
        public FastingEntry Active => data?.active == null ? null : new FastingEntry { id=data.active.id,startUtc=data.active.startUtc,endUtc=data.active.endUtc,planMinutes=data.active.planMinutes };
        public long Now => clock().ToUnixTimeSeconds();
        public long Elapsed => data?.active == null ? 0 : Math.Max(0, Now - data.active.startUtc);
        public bool ClockBeforeStart => data?.active != null && Now < data.active.startUtc;
        public bool PastChosenEnd => data?.active != null && Now >= data.active.startUtc + data.active.planMinutes * 60L;
        public int Band => TimeBand(Elapsed);
        public static int TimeBand(long seconds) => seconds < 4*3600 ? 0 : seconds < 16*3600 ? 1 : seconds < 24*3600 ? 2 : seconds < 48*3600 ? 3 : 4;
        public static string Duration(long seconds)
        { seconds = Math.Max(0, seconds); return (seconds/3600).ToString("00") + ":" + (seconds/60%60).ToString("00") + ":" + (seconds%60).ToString("00"); }
        public FastingTracker(IJournalStorage target, bool eligible, Func<DateTimeOffset> now = null)
        { storage = target ?? throw new ArgumentNullException(nameof(target)); Eligible = eligible; clock = now ?? (() => DateTimeOffset.UtcNow); }
        static FastingDocument Clone(FastingDocument value)
        {
            if (value == null) return null;
            var copy = JsonUtility.FromJson<FastingDocument>(JsonUtility.ToJson(value));
            if (string.IsNullOrEmpty(copy.active?.id)) copy.active = null;
            return copy;
        }
        public void Load()
        {
            data = null; Error = null;
            if (!Eligible) { Error = "ineligible"; return; } // Do not even read private records for teens/unknown age.
            try
            {
                string json = storage.Read();
                var next = json == null ? new FastingDocument { version=1,context="local-fasting-v1",planMinutes=720,records=Array.Empty<FastingEntry>() } : JsonUtility.FromJson<FastingDocument>(json);
                if (next?.active != null && string.IsNullOrEmpty(next.active.id))
                {
                    if(next.active.startUtc!=0||next.active.endUtc!=0||next.active.planMinutes!=0)throw new ArgumentException("Invalid active record");
                    next.active=null; // Unity materializes an absent inline class as an empty object.
                }
                Validate(next); data = next;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException)
            { Error = "load"; } // Preserve corrupt/unsupported originals; no automatic reset.
        }
        static bool Plan(int minutes) => minutes >= 60 && minutes <= 1200;
        static bool Timestamp(long value) => value >= 946684800 && value <= 4133980800;
        static void Validate(FastingDocument d)
        {
            if (d == null || d.version != 1 || d.context != "local-fasting-v1" || !Plan(d.planMinutes) || d.records == null || d.records.Length > 5000)
                throw new ArgumentException("Invalid fasting document");
            if (d.active != null && (!d.enabled || string.IsNullOrEmpty(d.active.id) || !Timestamp(d.active.startUtc) || d.active.endUtc != 0 || !Plan(d.active.planMinutes)))
                throw new ArgumentException("Invalid active record");
            var ids = new System.Collections.Generic.HashSet<string>();
            long previousEnd = 0;
            foreach (var e in d.records.OrderBy(x => x?.startUtc ?? 0))
            {
                if (e == null || string.IsNullOrEmpty(e.id) || !ids.Add(e.id) || !Timestamp(e.startUtc) || !Timestamp(e.endUtc) || e.endUtc < e.startUtc || !Plan(e.planMinutes) || e.startUtc < previousEnd)
                    throw new ArgumentException("Invalid history");
                previousEnd = e.endUtc;
            }
            if (d.active != null && (ids.Contains(d.active.id) || d.records.Any(x => x.endUtc > d.active.startUtc))) throw new ArgumentException("Overlapping active record");
        }
        bool Change(Action<FastingDocument> mutation)
        {
            Error = null;
            if (!Eligible || data == null) { Error = "unavailable"; return false; }
            var next = Clone(data);
            try { mutation(next); Validate(next); storage.Write(JsonUtility.ToJson(next, true)); data = next; return true; }
            catch (ArgumentException e) { Error = e.Message; return false; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Error = "save"; return false; }
        }
        static void Require(bool yes, string error) { if (!yes) throw new ArgumentException(error); }
        public bool Enable(int minutes, bool acknowledged) => Change(d =>
        { Require(acknowledged,"ack"); Require(Plan(minutes),"plan"); Require(d.active == null,"active"); d.enabled = true; d.planMinutes = minutes; });
        public bool Disable() => Change(d => { Require(d.active == null,"active"); d.enabled = false; });
        public bool SetReducedMotion(bool value) => Change(d => d.reducedMotion = value);
        public bool Start(long start, int minutes) => Change(d =>
        {
            Require(d.enabled && d.active == null,"active"); Require(Plan(minutes),"plan"); Require(Timestamp(start) && start <= Now,"future");
            Require(d.records.All(x=>x.endUtc<=start),"overlap"); d.planMinutes=minutes;
            d.active=new FastingEntry{id=Guid.NewGuid().ToString("N"),startUtc=start,planMinutes=minutes};
        });
        public bool CorrectStart(long start) => Change(d =>
        { Require(d.active != null,"active"); Require(Timestamp(start) && start <= Now,"future"); Require(d.records.All(x=>x.endUtc<=start),"overlap"); d.active.startUtc=start; });
        public bool EndNow() => Change(d =>
        {
            Require(d.active != null,"active"); long end=Now; Require(end>=d.active.startUtc && Timestamp(end),"clock");
            d.active.endUtc=end; d.records=d.records.Concat(new[]{d.active}).ToArray(); d.active=null;
        });
        public bool Correct(string id,long start,long end) => Change(d =>
        {
            var e=d.records.FirstOrDefault(x=>x.id==id); Require(e!=null,"missing");
            Require(Timestamp(start)&&Timestamp(end)&&start<=end,"order"); Require(end<=Now,"future");
            Require(d.records.Where(x=>x.id!=id).All(x=>end<=x.startUtc||start>=x.endUtc)&&(d.active==null||end<=d.active.startUtc),"overlap");
            e.startUtc=start;e.endUtc=end;
        });
        public bool Delete(string id) => Change(d => { Require(d.records.Any(x=>x.id==id),"missing"); d.records=d.records.Where(x=>x.id!=id).ToArray(); });
        public bool DiscardActive() => Change(d => { Require(d.active!=null,"active");d.active=null; });
        public bool ClearHistory() => Change(d => d.records=Array.Empty<FastingEntry>());
        public static string EditTime(long utc) => DateTimeOffset.FromUnixTimeSeconds(utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz",CultureInfo.InvariantCulture);
        public static bool ParseTime(string input,out long utc)
        {
            utc=0;
            if(DateTimeOffset.TryParseExact(input,new[]{"yyyy-MM-dd HH:mm:ss zzz","yyyy-MM-dd HH:mm zzz"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var withOffset))
            {utc=withOffset.ToUnixTimeSeconds();return true;}
            if(!DateTime.TryParseExact(input,new[]{"yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var local))return false;
            if(TimeZoneInfo.Local.IsInvalidTime(local)||TimeZoneInfo.Local.IsAmbiguousTime(local))return false;
            utc=new DateTimeOffset(local,TimeZoneInfo.Local.GetUtcOffset(local)).ToUnixTimeSeconds();return true;
        }
    }
}
