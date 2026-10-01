using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace SoloGym.Editor
{
    public sealed class PixelTrainingHallImporter : AssetPostprocessor
    {
        [Serializable] sealed class Entry {public string id;public Vector2 feet;public Box portraitRect;}
        [Serializable] sealed class Data {public Entry[] characters;}
        [Serializable] sealed class Box {public float x,y,width,height;}
        public override uint GetVersion()=>1;
        void OnPreprocessTexture()
        {
            bool room=assetPath.StartsWith("Assets/SoloGym/Resources/Rooms/TrainingHallR1/",StringComparison.Ordinal);
            const string characters="Assets/SoloGym/Resources/Characters/PixelLabR1/";
            bool character=assetPath.StartsWith(characters,StringComparison.Ordinal);
            if((!room&&!character)||!assetPath.EndsWith(".png",StringComparison.Ordinal))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
            t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,0);t.SetTextureSettings(settings);
            if(character&&assetPath.EndsWith("-portrait.png",StringComparison.Ordinal))
            {
                string id=Path.GetFileNameWithoutExtension(assetPath).Replace("-portrait","");var c=Array.Find(JsonUtility.FromJson<Data>(File.ReadAllText(characters+"catalog.json")).characters,x=>x.id==id);var r=c.portraitRect;
                t.spriteImportMode=SpriteImportMode.Multiple;
#pragma warning disable 618
                t.spritesheet=new[]{new SpriteMetaData{name=id+"-portrait",rect=new Rect(r.x,r.y,r.width,r.height),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)}};
#pragma warning restore 618
            }
        }
    }
}
