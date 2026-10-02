using System;
using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Frames under Resources/Game/ (boss art and exercise animations) are exact PixelLab exports: crisp, uncompressed sprites.</summary>
    public sealed class PixelGameSpriteImporter : AssetPostprocessor
    {
        const string Root = "Assets/SoloGym/Resources/Game/";
        public override uint GetVersion() => 1;
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) || !assetPath.EndsWith(".png", StringComparison.Ordinal)) return;
            var texture = (TextureImporter)assetImporter; texture.textureType = TextureImporterType.Sprite; texture.spriteImportMode = SpriteImportMode.Single;
            texture.spritePixelsPerUnit = 100; texture.alphaSource = TextureImporterAlphaSource.FromInput; texture.alphaIsTransparency = true;
            texture.mipmapEnabled = false; texture.npotScale = TextureImporterNPOTScale.None; texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp; texture.textureCompression = TextureImporterCompression.Uncompressed; texture.maxTextureSize = 256;
            var settings = new TextureImporterSettings(); texture.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter; texture.SetTextureSettings(settings);
        }
    }
}
