using SoloGym.UI;
using UnityEditor;
using UnityEngine;
namespace SoloGym.Editor
{
    public static class PixelBossBuild
    {
        [MenuItem("SoloGym/Pixel UI/Build Boss Dungeon")]
        public static void BuildLinux()=>PixelReviewBuild.BuildLinux<PixelBossReview>("Assets/SoloGym/Scenes/PixelBossReview.unity","Boss","SoloGymBoss","SoloGym Boss Dungeon");
    }
    public sealed class PixelBossTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/SoloGym/Resources/Rooms/BossR1/")||!assetPath.EndsWith(".png"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
            t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;
            var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(s);
        }
    }
}
