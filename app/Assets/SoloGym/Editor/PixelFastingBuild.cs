using SoloGym.UI;
using UnityEditor;
using UnityEngine;
namespace SoloGym.Editor
{
    public static class PixelFastingBuild
    {
        [MenuItem("SoloGym/Pixel UI/Build Fasting Clock")]
        public static void BuildLinux()=>PixelReviewBuild.BuildLinux<PixelFastingReview>("Assets/SoloGym/Scenes/PixelFastingReview.unity","Fasting","SoloGymFasting","SoloGym Fasting Clock");
    }
    public sealed class PixelFastingTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/SoloGym/Resources/Rooms/FastingR1/")||!assetPath.EndsWith(".png"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
            t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;
            var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(s);
        }
    }
}
