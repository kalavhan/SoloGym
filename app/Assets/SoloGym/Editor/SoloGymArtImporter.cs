using UnityEditor;
using UnityEngine;
namespace SoloGym.Editor
{
    public sealed class SoloGymArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/SoloGym/Resources/Art/")&&!assetPath.StartsWith("Assets/SoloGym/Resources/AvatarProof/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        }
        public override uint GetVersion()=>1;
    }
}
