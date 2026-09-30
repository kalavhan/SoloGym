using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Metadata crops the provider canvas; source and runtime PNG bytes stay unchanged.</summary>
    public sealed class PixelButtonTextureImporter : AssetPostprocessor
    {
        const string Path = "Assets/SoloGym/Resources/UI/Pixel/PrimaryButton.png";

        void OnPreprocessTexture()
        {
            if (assetPath != Path) return;
            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Sprite;
            texture.spriteImportMode = SpriteImportMode.Multiple;
            texture.spritePixelsPerUnit = 400;
            var settings = new TextureImporterSettings();
            texture.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            texture.SetTextureSettings(settings);
            texture.alphaSource = TextureImporterAlphaSource.FromInput;
            texture.alphaIsTransparency = true;
            texture.mipmapEnabled = false;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.maxTextureSize = 1024;
#pragma warning disable 618
            texture.spritesheet = new[]
            {
                new SpriteMetaData
                {
                    name = "PrimaryButton", rect = new Rect(32, 372, 960, 278),
                    alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                    border = new Vector4(72, 72, 72, 72)
                }
            };
#pragma warning restore 618
        }
    }
}
