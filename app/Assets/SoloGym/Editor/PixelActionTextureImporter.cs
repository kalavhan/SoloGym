using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Crop/slice metadata only; keep the provider export bytes intact.</summary>
    public sealed class PixelActionTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/SoloGym/Resources/UI/Pixel/SecondaryAction.png") return;
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
                name = "SecondaryAction", rect = new Rect(24, 384, 976, 258),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                border = new Vector4(64, 64, 64, 64)
            } };
#pragma warning restore 618
        }
    }
}
