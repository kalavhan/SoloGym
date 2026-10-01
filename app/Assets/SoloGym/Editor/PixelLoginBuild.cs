using SoloGym.UI;
using UnityEditor;
using UnityEngine;
namespace SoloGym.Editor
{
    public static class PixelLoginBuild
    {
        [MenuItem("SoloGym/Pixel UI/Build Guild Login")]
        public static void BuildLinux() => PixelReviewBuild.BuildLinux<WelcomeScreen>("Assets/SoloGym/Scenes/PixelLoginReview.unity", "Login", "SoloGymLogin", "SoloGym Guild Login");
    }
    public sealed class PixelLoginTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/SoloGym/Resources/Rooms/LoginR1/") || !assetPath.EndsWith(".png")) return;
            var t = (TextureImporter)assetImporter; t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single; t.spritePixelsPerUnit = 100;
            t.mipmapEnabled = false; t.npotScale = TextureImporterNPOTScale.None; t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            t.textureCompression = TextureImporterCompression.Uncompressed; t.maxTextureSize = 2048;
        }
    }
}
