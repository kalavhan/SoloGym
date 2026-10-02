using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace UnityEngine
{
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = false, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
        public static string ToJson(object value, bool pretty = false) => JsonSerializer.Serialize(value, value.GetType(), Options);
    }
    public static class Debug { public static void Log(object m) => Console.WriteLine(m); public static void LogWarning(object m) => Console.WriteLine("WARN " + m); public static void LogError(object m) => Console.WriteLine("ERR " + m); }
    public static class PlayerPrefs
    {
        static readonly Dictionary<string, string> s = new Dictionary<string, string>();
        public static string GetString(string k, string d = "") => s.TryGetValue(k, out var v) ? v : d;
        public static void SetString(string k, string v) => s[k] = v;
        public static bool HasKey(string k) => s.ContainsKey(k);
        public static void DeleteKey(string k) => s.Remove(k);
        public static void Save() { }
    }
    public enum SystemLanguage { English, Spanish }
    public static class Application { public static SystemLanguage systemLanguage = SystemLanguage.English; public static string persistentDataPath = System.IO.Path.GetTempPath(); }
    public static class Mathf { public static float Clamp01(float v) => Math.Max(0, Math.Min(1, v)); }
    public class TextAsset { public string text; }
    public static class Resources { public static Func<string, string> Loader; public static T Load<T>(string path) where T : class { var t = Loader?.Invoke(path); return t == null ? null : new TextAsset { text = t } as T; } }
}
