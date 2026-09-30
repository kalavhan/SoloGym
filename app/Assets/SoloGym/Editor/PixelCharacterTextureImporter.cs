using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoloGym.Editor
{
    /// <summary>Only crop/pivot/import metadata changes; runtime PNGs are exact provider exports.</summary>
    public sealed class PixelCharacterTextureImporter : AssetPostprocessor
    {
        const string Root = "Assets/SoloGym/Resources/Characters/BarbarianR1/";
        [Serializable] sealed class Crop { public float x, y, width, height; }
        [Serializable] sealed class Entry { public string id; public Crop sourceRect; public Vector2 feet; public int width, height; }
        [Serializable] sealed class Data { public Entry[] characters; }
        public override uint GetVersion() => 1;
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) || !assetPath.EndsWith(".png", StringComparison.Ordinal)) return;
            var catalog = JsonUtility.FromJson<Data>(File.ReadAllText(Root + "catalog.json"));
            var entry = Array.Find(catalog.characters, x => x.id == Path.GetFileNameWithoutExtension(assetPath));
            if (entry == null) throw new InvalidOperationException("Missing character framing metadata: " + assetPath);
            var texture = (TextureImporter)assetImporter; texture.textureType = TextureImporterType.Sprite;
            texture.spriteImportMode = SpriteImportMode.Multiple; texture.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings(); texture.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; texture.SetTextureSettings(settings);
            texture.alphaSource = TextureImporterAlphaSource.FromInput; texture.alphaIsTransparency = true;
            texture.mipmapEnabled = false; texture.npotScale = TextureImporterNPOTScale.None; texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp; texture.textureCompression = TextureImporterCompression.Uncompressed; texture.maxTextureSize = 2048;
#pragma warning disable 618
            texture.spritesheet = new[] { new SpriteMetaData { name = entry.id, rect = new Rect(entry.sourceRect.x, entry.sourceRect.y, entry.sourceRect.width, entry.sourceRect.height), alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(entry.feet.x / entry.width, entry.feet.y / entry.height), border = Vector4.zero } };
#pragma warning restore 618
        }
    }
}
