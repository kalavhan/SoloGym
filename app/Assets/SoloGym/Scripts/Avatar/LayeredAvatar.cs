using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Small registered 2D cutout proof. Mirrored views are proof-only, not eight-direction assets.</summary>
    public sealed class LayeredAvatar : MonoBehaviour
    {
        public enum AvatarPreviewMode { FullBody, FaceCloseUp }

        [Serializable] public sealed class Atlas { public Part[] parts; }
        [Serializable] public sealed class Part { public string id, resource; public float[] rect; public float[] skin; }
        readonly System.Collections.Generic.Dictionary<string, Part> parts =
            new System.Collections.Generic.Dictionary<string, Part>();
        readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();

        public sealed class RigRefs
        {
            public RectTransform rig, headSocket, leftArm, rightArm, leftForearm, rightForearm, leftLeg, rightLeg,
                leftCalf, rightCalf;
            public RawImage hair, shirt, leftGlove, rightGlove, leftHand, rightHand;
        }

        sealed class Attachment
        {
            public RawImage image;
            public RectTransform socket;
            public Vector3 offset;
        }
        readonly System.Collections.Generic.List<Attachment> attachments = new System.Collections.Generic.List<Attachment>();

        RectTransform rig, headSocket, leftArm, rightArm, leftForearm, rightForearm, leftLeg, rightLeg, leftCalf, rightCalf;
        RawImage hair, shirt, leftGlove, rightGlove, leftHand, rightHand;
        AvatarAppearance appearance;
        bool initialized;
        public bool IsInitialized => initialized;
        CharacterReference.CameraPresetData framing;
        public string action = "idle";
        public bool facingLeft;
        public bool freeze;
        public float previewTime = .6f;
        public AvatarPreviewMode previewMode = AvatarPreviewMode.FullBody;

        public void Initialize(AvatarAppearance recipe)
        {
            if (recipe == null || !recipe.IsSupportedProof)
                throw new ArgumentException("Unsupported character recipe.", nameof(recipe));
            var asset = Resources.Load<TextAsset>("AvatarProof/Parts");
            foreach (var p in JsonUtility.FromJson<Atlas>(asset.text).parts) parts[p.id] = p;
            AvatarRigLayout.Build(this, transform, parts, out var refs);
            rig = refs.rig;
            headSocket = refs.headSocket;
            leftArm = refs.leftArm;
            rightArm = refs.rightArm;
            leftForearm = refs.leftForearm;
            rightForearm = refs.rightForearm;
            leftLeg = refs.leftLeg;
            rightLeg = refs.rightLeg;
            leftCalf = refs.leftCalf;
            rightCalf = refs.rightCalf;
            hair = refs.hair;
            shirt = refs.shirt;
            leftGlove = refs.leftGlove;
            rightGlove = refs.rightGlove;
            leftHand = refs.leftHand;
            rightHand = refs.rightHand;
            initialized = true;
            Apply(recipe);
            ApplyFramingFromReference();
        }

        public void ApplyFramingFromReference()
        {
            CharacterReference.Load();
            framing = CharacterReference.Preset(previewMode == AvatarPreviewMode.FaceCloseUp
                ? CharacterReference.CameraPresetId.FaceCloseUp
                : CharacterReference.CameraPresetId.FullBody);
            ApplyPreviewFraming();
        }

        internal RawImage CreatePiece(string id, Transform renderRoot, RectTransform socket, Rect r)
        {
            var image = Piece(id, renderRoot, r);
            attachments.Add(new Attachment { image = image, socket = socket, offset = new Vector3(r.x, -r.y, 0) });
            return image;
        }

        void SyncAttachments()
        {
            foreach (var attachment in attachments)
            {
                // Every sprite is a sibling for reliable sorting; every attachment follows its skeletal socket.
                attachment.image.transform.localPosition = rig.InverseTransformPoint(attachment.socket.TransformPoint(attachment.offset));
                attachment.image.transform.localRotation = Quaternion.Inverse(rig.rotation) * attachment.socket.rotation;
            }
        }

        RawImage Piece(string id, Transform parent, Rect r)
        {
            Part part = parts[id];
            var texture = Resources.Load<Texture2D>(part.resource);
            var image = SystemUI.Node(id, parent, r).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
            SetPart(image, part);
            if (part.skin != null && part.skin.Length == 4)
            {
                var mat = new Material(Resources.Load<Shader>("AvatarProof/AvatarSkin"));
                mat.SetVector("_PartUV", new Vector4(image.uvRect.x, image.uvRect.y, image.uvRect.width, image.uvRect.height));
                mat.SetVector("_SkinBounds", new Vector4(part.skin[0], part.skin[1], part.skin[2], part.skin[3]));
                image.material = mat; materials.Add(mat);
            }
            return image;
        }

        void SetPart(RawImage image, Part p)
        {
            var t = (Texture2D)image.texture;
            image.uvRect = new Rect(p.rect[0] / t.width, 1 - (p.rect[1] + p.rect[3]) / t.height, p.rect[2] / t.width,
                p.rect[3] / t.height);
        }

        public bool Apply(AvatarAppearance recipe)
        {
            if (recipe == null || !recipe.IsSupportedProof) return false;
            appearance = recipe;
            Color skin = AvatarCustomizationCatalog.SkinTint(recipe.skinPaletteId);
            foreach (var mat in materials) mat.SetColor("_SkinTint", skin);
            string hairPart = AvatarCustomizationCatalog.HairLayerPartId(recipe.hairId);
            if (parts.ContainsKey(hairPart))
            {
                SetPart(hair, parts[hairPart]);
                foreach (var variant in CharacterReference.RigLayout.hair_variants)
                {
                    if (variant.id != hairPart) continue;
                    var part = parts[hairPart];
                    hair.rectTransform.sizeDelta = new Vector2(part.rect[2], part.rect[3]) * variant.scale;
                    attachments.Find(a => a.image == hair).offset = new Vector3(variant.x, -variant.y, 0);
                    break;
                }
            }
            shirt.gameObject.SetActive(!string.IsNullOrEmpty(recipe.torsoItemId));
            bool gloves = !string.IsNullOrEmpty(recipe.handItemId);
            leftGlove.gameObject.SetActive(gloves);
            rightGlove.gameObject.SetActive(gloves);
            leftHand.gameObject.SetActive(!gloves);
            rightHand.gameObject.SetActive(!gloves);
            ApplyPreviewFraming();
            SyncAttachments();
            return true;
        }

        void ApplyPreviewFraming()
        {
            if (rig == null) return;
            if (framing == null)
                framing = CharacterReference.Preset(previewMode == AvatarPreviewMode.FaceCloseUp
                    ? CharacterReference.CameraPresetId.FaceCloseUp
                    : CharacterReference.CameraPresetId.FullBody);
            float s = framing.rig_scale > 0 ? framing.rig_scale : 1f;
            rig.localScale = new Vector3(facingLeft ? -s : s, s, 1f);
            rig.anchoredPosition = new Vector2(framing.rig_offset_x, framing.rig_offset_y);
        }

        void LateUpdate()
        {
            if (rig == null) return;
            if (framing == null) ApplyFramingFromReference();
            float t = freeze ? previewTime : Time.unscaledTime;
            float walk = action == "walk" ? Mathf.Sin(t * 6) : 0;
            float punch = action == "jab" ? Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 3)), 4) : 0;
            Vector2 basePos = new Vector2(framing.rig_offset_x, framing.rig_offset_y);
            bool stableIdle = freeze && action == "idle";
            float bobY = stableIdle ? 0 : (action == "walk" ? Mathf.Abs(walk) * 5 : Mathf.Sin(t * 2) * 2);
            rig.anchoredPosition = basePos + new Vector2(0, bobY);
            float mirror = facingLeft ? -1f : 1f;
            float scale = framing.rig_scale > 0 ? framing.rig_scale : 1f;
            rig.localScale = new Vector3(mirror * scale, scale, 1f);
            if (stableIdle)
            {
                Rotate(leftLeg, 0); Rotate(rightLeg, 0);
                Rotate(leftCalf, 0); Rotate(rightCalf, 0);
                Rotate(leftArm, 0); Rotate(leftForearm, 0);
                Rotate(rightArm, 0); Rotate(rightForearm, 0);
                headSocket.localEulerAngles = Vector3.zero;
                SyncAttachments();
                return;
            }
            if (framing.animate_limbs)
            {
                headSocket.localEulerAngles = Vector3.zero;
                Rotate(leftLeg, walk * 15); Rotate(rightLeg, -walk * 15);
                Rotate(leftCalf, Mathf.Max(0, -walk) * 20); Rotate(rightCalf, Mathf.Max(0, walk) * 20);
                Rotate(leftArm, -walk * 12); Rotate(leftForearm, 0);
                Rotate(rightArm, walk * 12 + punch * 65);
                Rotate(rightForearm, action == "jab" ? 18 * (1 - punch) : 0);
            }
            else
            {
                Rotate(leftLeg, 0); Rotate(rightLeg, 0); Rotate(leftCalf, 0); Rotate(rightCalf, 0);
                Rotate(leftArm, 0); Rotate(leftForearm, 0);
                Rotate(rightArm, 0); Rotate(rightForearm, 0);
                headSocket.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 1.2f) * 2f);
            }
            SyncAttachments();
        }

        static void Rotate(RectTransform joint, float angle) { joint.localEulerAngles = new Vector3(0, 0, angle); }

        public bool CheckAttachments()
        {
            if (!initialized || appearance == null || !appearance.IsSupportedProof) return false;
            foreach (var a in attachments)
            {
                if (a.image.transform.parent != rig || a.socket == null) return false;
                if (Vector3.Distance(a.image.transform.position, a.socket.TransformPoint(a.offset)) > .01f) return false;
            }
            return attachments.Find(a => a.image == leftGlove).socket.parent == leftForearm
                && attachments.Find(a => a.image == rightGlove).socket.parent == rightForearm;
        }

        void OnDestroy() { foreach (var m in materials) if (m != null) Destroy(m); }
    }
}
