using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SoloGym.Editor
{
    /// <summary>Reproducible entry/Home scenes and local development builds.</summary>
    public static class SoloGymBuild
    {
        public const string ScenePath = "Assets/SoloGym/Scenes/SystemHome.unity";
        public const string WelcomeScenePath = "Assets/SoloGym/Scenes/Welcome.unity";

        [MenuItem("SoloGym/Create or Open System Home")]
        public static void CreateScene()
        {
            ConfigureCommon();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Scene creation was cancelled.");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SoloGym/Scenes"));
            AssetDatabase.Refresh();
            Scene scene = File.Exists(Path.Combine(ProjectRoot, ScenePath))
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            HomeScreen screen = UnityEngine.Object.FindFirstObjectByType<HomeScreen>();
            if (screen == null)
            {
                var root = new GameObject("SoloGym System Home");
                screen = root.AddComponent<HomeScreen>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new BuildFailedException("Could not save " + ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("SoloGym: System Home scene is ready at " + ScenePath);
        }

        [MenuItem("SoloGym/Create or Open Welcome")]
        public static void CreateWelcomeScene()
        {
            ConfigureCommon();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Scene creation was cancelled.");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SoloGym/Scenes"));
            AssetDatabase.Refresh();
            Scene scene = File.Exists(Path.Combine(ProjectRoot, WelcomeScenePath))
                ? EditorSceneManager.OpenScene(WelcomeScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            WelcomeScreen screen = UnityEngine.Object.FindFirstObjectByType<WelcomeScreen>();
            if (screen == null)
            {
                var root = new GameObject("SoloGym Welcome");
                root.AddComponent<WelcomeScreen>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, WelcomeScenePath))
                throw new BuildFailedException("Could not save " + WelcomeScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("SoloGym: Welcome scene is ready at " + WelcomeScenePath);
        }

        [MenuItem("SoloGym/Configure Entry and Home Scenes")]
        public static void ConfigureScenes()
        {
            CreateScene();
            CreateWelcomeScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(WelcomeScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("SoloGym/Build/Linux Development")]
        public static void BuildLinux()
        {
            ConfigureScenes();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
                throw new BuildFailedException("This Unity editor has no Linux Standalone support module.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            Build(BuildTarget.StandaloneLinux64, "Builds/Linux/SoloGym.x86_64");
        }

        [MenuItem("SoloGym/Configure Android")]
        public static void ConfigureAndroid()
        {
            ConfigureCommon();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("This Unity editor has no Android Build Support module.");

            string androidPlayer = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer");
            string sdk = Path.Combine(androidPlayer, "SDK");
            string jdk = Path.Combine(androidPlayer, "OpenJDK");
            string ndk = Path.Combine(androidPlayer, "NDK");
            if (!Directory.Exists(sdk) || !Directory.Exists(jdk) || !Directory.Exists(ndk))
                throw new BuildFailedException("Android requires this editor's bundled SDK, OpenJDK and NDK modules.");

            AndroidExternalToolsSettings.sdkRootPath = sdk;
            AndroidExternalToolsSettings.jdkRootPath = jdk;
            AndroidExternalToolsSettings.ndkRootPath = ndk;
            EditorPrefs.SetBool("SdkUseEmbedded", true);
            EditorPrefs.SetBool("JdkUseEmbedded", true);
            EditorPrefs.SetBool("NdkUseEmbedded", true);
            EditorPrefs.SetBool("GradleUseEmbedded", true);

            bool hasIl2Cpp = Directory.Exists(Path.Combine(androidPlayer, "Variations/il2cpp"));
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,
                hasIl2Cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = hasIl2Cpp ? AndroidArchitecture.ARM64 : AndroidArchitecture.ARMv7;
            if (!hasIl2Cpp)
                Debug.LogWarning("SoloGym: IL2CPP Android support is absent; building a local Mono ARMv7 development APK.");

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kalavhan.sologym");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 5;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = true;
            AssetDatabase.SaveAssets();
            Debug.Log("SoloGym: Android uses bundled tools, API auto, " + (hasIl2Cpp ? "IL2CPP ARM64" : "Mono ARMv7") + ".");
        }

        [MenuItem("SoloGym/Build/Android Development APK")]
        public static void BuildAndroid()
        {
            ConfigureScenes();
            ConfigureAndroid();
            Build(BuildTarget.Android, "Builds/Android/SoloGym-debug.apk");
        }

        /// <summary>
        /// MVP test APK: the connected fitness flow without review-only sample routes
        /// (no SOLOGYM_REVIEW define). Output path can be overridden with SOLOGYM_APK_PATH.
        /// </summary>
        [MenuItem("SoloGym/Build/Android Test APK (MVP)")]
        public static void BuildAndroidTest()
        {
            ConfigureScenes();
            ConfigureAndroid();
            string output = Environment.GetEnvironmentVariable("SOLOGYM_APK_PATH");
            Build(BuildTarget.Android, string.IsNullOrWhiteSpace(output) ? "Builds/Android/SoloGym-mvp-test.apk" : output, false);
        }

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        static void EnsureTheme()
        {
            const string folder="Assets/SoloGym/Resources/UI";
            const string path=folder+"/DefaultTheme.asset";
            if(AssetDatabase.LoadAssetAtPath<SystemTheme>(path)!=null)return;
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var theme=ScriptableObject.CreateInstance<SystemTheme>();
            theme.heading=Resources.Load<Font>("Fonts/LiberationSerif-Regular");
            theme.headingBold=Resources.Load<Font>("Fonts/LiberationSerif-Bold");
            theme.body=Resources.Load<Font>("Fonts/NotoSans-Regular");
            AssetDatabase.CreateAsset(theme,path);AssetDatabase.SaveAssets();
        }

        static void ConfigureCommon()
        {
            PlayerSettings.companyName = "kalavhan";
            PlayerSettings.productName = "SoloGym";
            PlayerSettings.bundleVersion = "0.6.1";
            EnsureTheme();
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
        }

        static void Build(BuildTarget target, string relativeOutput, bool review = true)
        {
            string output = Path.IsPathRooted(relativeOutput) ? relativeOutput : Path.Combine(ProjectRoot, relativeOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string[] authDefines = FirebaseIntegrationSetup.ScriptingDefinesForBuild;
            if (Array.IndexOf(authDefines, "SOLOGYM_FIREBASE_AUTH") < 0)
                Debug.LogWarning("SoloGym: Firebase SDK is not installed; sign-in and account creation will report unavailable in this build.");
            var localBuildDefines = new string[authDefines.Length + (review ? 1 : 0)];
            Array.Copy(authDefines, localBuildDefines, authDefines.Length);
            // Review builds keep the explicitly labelled sample Home accessible
            // independently of authentication; the MVP test APK omits them.
            if (review) localBuildDefines[authDefines.Length] = "SOLOGYM_REVIEW";
            var options = new BuildPlayerOptions
            {
                scenes = new[] { WelcomeScenePath, ScenePath },
                locationPathName = output,
                target = target,
                extraScriptingDefines = localBuildDefines,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("SoloGym " + target + " build " + report.summary.result +
                    " with " + report.summary.totalErrors + " error(s). Check the editor log.");
            Debug.Log("SoloGym: built " + output + " (" + report.summary.totalSize + " bytes).");
        }
    }

    /// <summary>Preserve reference geometry and color instead of resizing/compressing UI art.</summary>
    public sealed class HomeTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            bool referenceAsset = assetPath.StartsWith("Assets/SoloGym/Resources/Home/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/SoloGym/Resources/Welcome/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/SoloGym/Resources/Provider/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/SoloGym/Resources/Onboarding/", StringComparison.Ordinal)
                || assetPath.StartsWith("Assets/SoloGym/Resources/Profile/", StringComparison.Ordinal);
            if (!referenceAsset || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Default;
            texture.sRGBTexture = true;
            texture.alphaSource = TextureImporterAlphaSource.FromInput;
            texture.alphaIsTransparency = true;
            texture.isReadable = true;
            texture.mipmapEnabled = false;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.maxTextureSize = 4096;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.crunchedCompression = false;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            ConfigurePlatform(texture, "Standalone");
            ConfigurePlatform(texture, "Android");
            ConfigurePlatform(texture, "iPhone");
        }

        static void ConfigurePlatform(TextureImporter texture, string name)
        {
            var platform = texture.GetPlatformTextureSettings(name);
            platform.name = name;
            platform.overridden = true;
            platform.maxTextureSize = 4096;
            platform.format = TextureImporterFormat.RGBA32;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.crunchedCompression = false;
            texture.SetPlatformTextureSettings(platform);
        }
    }
}
