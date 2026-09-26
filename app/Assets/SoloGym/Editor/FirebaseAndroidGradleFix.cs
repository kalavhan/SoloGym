using System.IO;
using UnityEditor.Android;

namespace SoloGym.Editor
{
    /// <summary>
    /// EDM 1.2.189 prefixes absolute Linux paths with file:/// and produces file:////.
    /// Give Gradle the absolute directory instead; its URI conversion also handles spaces.
    /// </summary>
    public sealed class FirebaseAndroidGradleFix : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string parent = Directory.GetParent(path)?.FullName;
            foreach (string candidate in new[] { Path.Combine(path, "settings.gradle"),
                parent == null ? "" : Path.Combine(parent, "settings.gradle") })
            {
                if (!File.Exists(candidate)) continue;
                string source = File.ReadAllText(candidate);
                const string malformed = "def unityProjectPath = $/file:////";
                string fixedSource = source.Replace(malformed, "def unityProjectPath = $//");
                if (source != fixedSource) File.WriteAllText(candidate, fixedSource);
            }
        }
    }
}
