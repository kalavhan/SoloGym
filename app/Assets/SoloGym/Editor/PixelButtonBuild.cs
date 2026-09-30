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
    /// <summary>Builds only the primary-button fixture without modifying app scene registration.</summary>
    public static class PixelButtonBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/PixelButtonReview.unity";

        [MenuItem("SoloGym/Pixel UI/Create Primary Button Review")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Button review scene creation was cancelled.");
            CreateSceneAsset();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void CreateSceneAsset()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SoloGym/Scenes"));
            // A fresh batch editor starts with an untitled scene; additive creation rejects it.
            // Interactive callers have already resolved unsaved edits, and BuildLinux restores
            // the prior scene setup after building the dedicated fixture.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SoloGym Pixel Primary Button Review");
            root.AddComponent<PixelButtonGallery>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new BuildFailedException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("SoloGym/Pixel UI/Build Linux Primary Button Review")]
        public static void BuildLinux()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
                throw new BuildFailedException("The editor requires Linux Standalone support.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Button review build was cancelled.");
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
                CreateSceneAsset();
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
                    "Builds/FantasyButton/SoloGymButton.x86_64");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException("Primary button review build failed: " + report.summary.result);
                Debug.Log("SoloGym primary button review built: " + output);
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
