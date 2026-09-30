using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Crop/slice metadata only; keep the provider export bytes intact.</summary>
    public sealed class PixelChoiceTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/SoloGym/Resources/UI/Pixel/ChoiceCheck.png") return;
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
                name = "ChoiceCheck", rect = new Rect(169, 232, 699, 612),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                border = Vector4.zero
            } };
#pragma warning restore 618
        }
    }
}
