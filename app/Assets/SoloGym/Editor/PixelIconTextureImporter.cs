using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Crop/slice metadata only; keep the provider export bytes intact.</summary>
    public sealed class PixelIconTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            bool back = assetPath == "Assets/SoloGym/Resources/UI/Pixel/IconBack.png";
            bool settingsIcon = assetPath == "Assets/SoloGym/Resources/UI/Pixel/IconSettings.png";
            if (!back && !settingsIcon) return;
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
                name = back ? "IconBack" : "IconSettings",
                rect = back ? new Rect(198, 245, 638, 539) : new Rect(63, 63, 898, 908),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                border = Vector4.zero
            } };
#pragma warning restore 618
        }
    }
}
