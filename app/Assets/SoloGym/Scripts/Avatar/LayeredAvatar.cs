using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Small registered 2D cutout proof. Mirrored views are proof-only, not eight-direction assets.</summary>
    public sealed class LayeredAvatar : MonoBehaviour
    {
        [Serializable] public sealed class Atlas {public Part[] parts;}
        [Serializable] public sealed class Part {public string id,resource;public float[] rect;public float[] skin;}
        readonly Dictionary<string,Part> parts=new Dictionary<string,Part>();
        readonly List<Material> materials=new List<Material>();
        RectTransform rig,leftArm,rightArm,leftForearm,rightForearm,leftLeg,rightLeg,leftCalf,rightCalf;
        RawImage hair,shirt,leftGlove,rightGlove;
        AvatarAppearance appearance;
        public string action="idle";
        public bool facingLeft;
        public bool freeze;
        public float previewTime=.6f;
        public void Initialize(AvatarAppearance recipe)
        {
            var asset=Resources.Load<TextAsset>("AvatarProof/Parts");
            foreach(var p in JsonUtility.FromJson<Atlas>(asset.text).parts)parts[p.id]=p;
            rig=Joint("Body rig",transform,0,0);
            leftArm=Joint("Far shoulder",rig,-78,17);
            Piece("upper_left",leftArm,new Rect(-29,-11,64,142));
            leftForearm=Joint("Far elbow",leftArm,0,104);
            Piece("forearm_left",leftForearm,new Rect(-24,-9,51,134));
            var leftHand=Joint("Far wrist socket",leftForearm,1,105);
            Piece("hand_left",leftHand,new Rect(-27,-9,58,89));
            leftGlove=Piece("glove_left",leftHand,new Rect(-37,-19,77,112));
            leftLeg=Joint("Far hip",rig,-40,190);Piece("thigh_left",leftLeg,new Rect(-47,-3,95,154));
            leftCalf=Joint("Far knee",leftLeg,1,120);Piece("calf_left",leftCalf,new Rect(-40,-3,84,171));
            rightLeg=Joint("Near hip",rig,40,190);Piece("thigh_right",rightLeg,new Rect(-45,-3,92,154));
            rightCalf=Joint("Near knee",rightLeg,-1,120);Piece("calf_right",rightCalf,new Rect(-37,-3,82,171));
            Piece("torso",rig,new Rect(-99,-11,198,225));
            shirt=Piece("shirt",rig,new Rect(-100,-13,200,230));
            var head=Joint("Head socket",rig,5,-112);
            Piece("head",head,new Rect(-52,-12,108,158));
            Piece("eyes",head,new Rect(-21,48,64,16));
            Piece("mouth",head,new Rect(5,86,27,8));
            hair=Piece("hair_spiky",head,new Rect(-57,-27,133,90));
            rightArm=Joint("Near shoulder",rig,77,18);
            Piece("upper_right",rightArm,new Rect(-32,-11,64,142));
            rightForearm=Joint("Near elbow",rightArm,0,104);
            Piece("forearm_right",rightForearm,new Rect(-25,-9,52,134));
            var rightHand=Joint("Near wrist socket",rightForearm,-1,105);
            Piece("hand_right",rightHand,new Rect(-30,-9,58,89));
            rightGlove=Piece("glove_right",rightHand,new Rect(-38,-19,77,112));
            leftCalf.SetAsFirstSibling();rightCalf.SetAsFirstSibling();
            leftForearm.SetAsFirstSibling();rightForearm.SetAsFirstSibling();
            Apply(recipe);
        }
        static RectTransform Joint(string name,Transform parent,float x,float y)
        {
            var n=SystemUI.Node(name,parent,new Rect(x,y,0,0));return n;
        }
        RawImage Piece(string id,Transform parent,Rect r)
        {
            Part part=parts[id];var texture=Resources.Load<Texture2D>(part.resource);
            var image=SystemUI.Node(id,parent,r).gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            SetPart(image,part);
            if(part.skin!=null&&part.skin.Length==4)
            {
                var mat=new Material(Resources.Load<Shader>("AvatarProof/AvatarSkin"));
                mat.SetVector("_PartUV",new Vector4(image.uvRect.x,image.uvRect.y,image.uvRect.width,image.uvRect.height));
                mat.SetVector("_SkinBounds",new Vector4(part.skin[0],part.skin[1],part.skin[2],part.skin[3]));
                image.material=mat;materials.Add(mat);
            }
            return image;
        }
        void SetPart(RawImage image,Part p)
        {var t=(Texture2D)image.texture;image.uvRect=new Rect(p.rect[0]/t.width,1-(p.rect[1]+p.rect[3])/t.height,p.rect[2]/t.width,p.rect[3]/t.height);}
        public bool Apply(AvatarAppearance recipe)
        {
            if(recipe==null||!recipe.IsSupportedProof)return false;
            appearance=recipe;
            Color skin=recipe.skinPaletteId=="deep"?new Color(.49f,.28f,.17f):new Color(1,.77f,.57f);
            foreach(var mat in materials)mat.SetColor("_SkinTint",skin);
            SetPart(hair,parts[recipe.hairId=="swept"?"hair_swept":"hair_spiky"]);
            shirt.gameObject.SetActive(!string.IsNullOrEmpty(recipe.torsoItemId));
            leftGlove.gameObject.SetActive(!string.IsNullOrEmpty(recipe.handItemId));rightGlove.gameObject.SetActive(leftGlove.gameObject.activeSelf);
            return true;
        }
        void LateUpdate()
        {
            if(rig==null)return;
            float t=freeze?previewTime:Time.unscaledTime;
            float walk=action=="walk"?Mathf.Sin(t*6):0;
            float punch=action=="jab"?Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*3)),4):0;
            rig.anchoredPosition=new Vector2(0,action=="walk"?Mathf.Abs(walk)*5:Mathf.Sin(t*2)*2);
            rig.localScale=new Vector3(facingLeft?-1:1,1,1);
            Rotate(leftLeg,walk*15);Rotate(rightLeg,-walk*15);Rotate(leftCalf,Mathf.Max(0,-walk)*20);Rotate(rightCalf,Mathf.Max(0,walk)*20);
            Rotate(leftArm,-7-walk*12);Rotate(leftForearm,-10);
            Rotate(rightArm,7+walk*12+punch*80);Rotate(rightForearm,action=="jab"?25*(1-punch):10);
            // Limb and attached equipment move as one group and can cross the torso's depth.
            if(action=="jab"&&punch>.45f)leftArm.SetAsFirstSibling();
            rightArm.SetAsLastSibling();
        }
        static void Rotate(RectTransform joint,float angle){joint.localEulerAngles=new Vector3(0,0,angle);}
        public bool CheckAttachments()=>leftGlove.transform.parent.parent==leftForearm&&rightGlove.transform.parent.parent==rightForearm;
        void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);}
    }
}
