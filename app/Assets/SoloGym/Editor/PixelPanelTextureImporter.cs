using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Crop/slice metadata only; keep the provider export bytes intact.</summary>
    public sealed class PixelPanelTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/SoloGym/Resources/UI/Pixel/ContentPanel.png") return;
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
            texture.spritesheet = new[] { new SpriteMetaData
            {
                name = "ContentPanel", rect = new Rect(32, 32, 960, 960),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                border = new Vector4(128, 128, 128, 128)
            } };
#pragma warning restore 618
        }
    }
}
