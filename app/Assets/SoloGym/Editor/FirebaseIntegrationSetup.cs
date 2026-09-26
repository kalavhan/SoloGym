using System;
using System.IO;
using UnityEditor;

namespace SoloGym.Editor
{
    /// <summary>The adapter also compiles on clean clones before optional SDK installation.</summary>
    public static class FirebaseIntegrationSetup
    {
        const string Symbol = "SOLOGYM_FIREBASE_AUTH";
        public static string[] ScriptingDefinesForBuild
        {
            get
            {
                bool installed = File.Exists("Assets/Firebase/Plugins/Firebase.App.dll")
                    && File.Exists("Assets/Firebase/Plugins/Firebase.Auth.dll");
                return installed ? new[] { Symbol } : Array.Empty<string>();
            }
        }
    }
}
