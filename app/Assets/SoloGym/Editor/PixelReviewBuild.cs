using System;
using System.IO;
using SoloGym.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Shared isolated fixture builder; restores project settings and editor scene setup.</summary>
    public static class PixelReviewBuild
    {
        public static void CreateScene<T>(string scenePath, string displayName) where T : MonoBehaviour
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Review scene creation was cancelled.");
            CreateSceneAsset<T>(scenePath, displayName);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        static void CreateSceneAsset<T>(string scenePath, string displayName) where T : MonoBehaviour
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SoloGym/Scenes"));
            // A fresh batch editor starts with an untitled scene; additive creation rejects it.
            // Interactive callers have already resolved unsaved edits, and BuildLinux restores
            // the prior scene setup after building the dedicated fixture.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(displayName);
            root.AddComponent<T>();
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new BuildFailedException("Could not save " + scenePath);
            AssetDatabase.SaveAssets();
        }

        public static void BuildLinux<T>(string scenePath, string folder, string binary, string displayName) where T : MonoBehaviour
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
                throw new BuildFailedException("The editor requires Linux Standalone support.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Review build was cancelled.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var fullScreen = PlayerSettings.fullScreenMode;
            bool resizable = PlayerSettings.resizableWindow;
            bool background = PlayerSettings.runInBackground;
            int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
            var orientation = PlayerSettings.defaultInterfaceOrientation;
            bool portrait = PlayerSettings.allowedAutorotateToPortrait;
            bool portraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool landscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            bool landscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
            try
            {
                CreateSceneAsset<T>(scenePath, displayName);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.runInBackground = true;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    "Builds", folder, binary + ".x86_64");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException(displayName + " build failed: " + report.summary.result);
                Debug.Log(displayName + " built: " + output);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                PlayerSettings.fullScreenMode = fullScreen;
                PlayerSettings.resizableWindow = resizable;
                PlayerSettings.runInBackground = background;
                PlayerSettings.defaultScreenWidth = width;
                PlayerSettings.defaultScreenHeight = height;
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = portraitUpsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
                AssetDatabase.SaveAssets();
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}
