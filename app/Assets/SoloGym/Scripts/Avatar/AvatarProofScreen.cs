using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SoloGym
{
    public sealed class AvatarProofScreen : MonoBehaviour
    {
        RectTransform root;
        LayeredAvatar avatar;
        AvatarAppearance recipe=new AvatarAppearance();
        Text state;
        public void Initialize()
        {
            var canvas=new GameObject("Avatar engineering proof",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            root=SystemUI.Node("Avatar proof",canvas.transform,new Rect(0,0,853,1844));
            if(FindFirstObjectByType<EventSystem>()==null)new GameObject("Input",typeof(EventSystem),typeof(StandaloneInputModule));
            SystemUI.Art(root,new Rect(0,0,853,1844),"Art/HomeBackground-v1");
            SystemUI.Panel(root,new Rect(48,100,757,1590),PanelStyle.Glass,true);
            SystemUI.Caption(root,new Rect(75,135,703,80),"MODULAR AVATAR",53,SystemUI.Theme.headingBold);
            SystemUI.Caption(root,new Rect(85,220,683,65),"ENGINEERING PROOF · NOT APPROVED CHARACTER ART",22,SystemUI.Theme.body);
            var node=SystemUI.Node("Registered avatar",root,new Rect(426,555,0,0));avatar=node.gameObject.AddComponent<LayeredAvatar>();avatar.Initialize(recipe);
            state=SystemUI.Caption(root,new Rect(90,1100,673,55),"",25,SystemUI.Theme.body);
            SystemUI.Button(root,new Rect(85,1190,211,78),"Skin tone",()=>{recipe.skinPaletteId=recipe.skinPaletteId=="warm"?"deep":"warm";Apply();});
            SystemUI.Button(root,new Rect(321,1190,211,78),"Hairstyle",()=>{recipe.hairId=recipe.hairId=="spiky"?"swept":"spiky";Apply();});
            SystemUI.Button(root,new Rect(557,1190,211,78),"Equipment",()=>{bool off=!string.IsNullOrEmpty(recipe.torsoItemId);recipe.torsoItemId=off?null:"proof_training_top";recipe.handItemId=off?null:"proof_cyan_gloves";Apply();});
            SystemUI.Button(root,new Rect(85,1300,211,78),"Idle",()=>{avatar.action="idle";Apply();});
            SystemUI.Button(root,new Rect(321,1300,211,78),"Walk",()=>{avatar.action="walk";Apply();});
            SystemUI.Button(root,new Rect(557,1300,211,78),"Jab",()=>{avatar.action="jab";Apply();});
            SystemUI.Button(root,new Rect(85,1410,330,78),"Turn proof view",()=>{avatar.facingLeft=!avatar.facingLeft;Apply();});
            SystemUI.Button(root,new Rect(438,1410,330,78),"Pause / play",()=>avatar.freeze=!avatar.freeze);
            var note=SystemUI.Text(root,new Rect(94,1530,665,120),"One shared rig. Skin regions tint separately from clothing.\nTwo mirrored proof views; eight-direction art, final masks and body fits still need authoring.",24,SystemUI.Theme.body,SystemUI.Theme.muted,TextAnchor.MiddleCenter);note.horizontalOverflow=HorizontalWrapMode.Wrap;
            if(Arg("-sologym-avatar-variant")=="alternate")
            {recipe.skinPaletteId="deep";recipe.hairId="swept";recipe.torsoItemId=recipe.handItemId=null;avatar.facingLeft=true;avatar.action="walk";}
            if(Arg("-sologym-avatar-action")!=null)avatar.action=Arg("-sologym-avatar-action");
            Apply();if(Arg("-sologym-capture")!=null){avatar.freeze=true;avatar.previewTime=.52f;StartCoroutine(Capture());}
        }
        void Apply(){avatar.Apply(recipe);state.text=recipe.skinPaletteId+" · "+recipe.hairId+" · "+avatar.action+" · "+(avatar.facingLeft?"left":"right");}
        void Update(){if(root!=null)SystemViewport.Fit(root,853,1844);}
        IEnumerator Capture()
        {
            for(int i=0;i<8;i++)yield return null;
            Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            string path=Arg("-sologym-capture");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());Destroy(image);
            bool ok=avatar.CheckAttachments()&&recipe.IsSupportedProof;
            string before=JsonUtility.ToJson(recipe);var invalid=JsonUtility.FromJson<AvatarAppearance>(before);invalid.fitId="unsupported";
            ok &= !avatar.Apply(invalid);
            File.WriteAllText(Path.ChangeExtension(path,".proof.json"),"{\"attachments_and_fit_validation_passed\":"+(ok?"true":"false")+",\"production_ready\":false}");
            if(Arg("-sologym-avatar-frames")=="yes")
            {
                var folder=Path.Combine(Path.GetDirectoryName(path),"motion");Directory.CreateDirectory(folder);
                for(int i=0;i<24;i++)
                {
                    avatar.previewTime=i/12f;yield return null;yield return new WaitForEndOfFrame();
                    var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,i.ToString("D3")+".png"),frame.EncodeToPNG());Destroy(frame);
                }
            }
            Application.Quit(ok?0:2);
        }
        static string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:null;}
    }
}
