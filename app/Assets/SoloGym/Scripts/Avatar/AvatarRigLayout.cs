using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Hand-tuned proof rig rects (atlas UVs come from Parts.json; display size is art-tuned).</summary>
    public static class AvatarRigLayout
    {
        public static void Build(LayeredAvatar avatar, Transform root, Dictionary<string, LayeredAvatar.Part> parts,
            out LayeredAvatar.RigRefs refs)
        {
            refs = new LayeredAvatar.RigRefs();
            var rig = Joint("Body rig", root, 0, 0);
            refs.rig = rig;

            refs.leftArm = Joint("Far shoulder", rig, -74, 19);
            avatar.CreatePiece("upper_left", refs.leftArm, new Rect(-31, -10, 64, 142));
            refs.leftForearm = Joint("Far elbow", refs.leftArm, 0, 101);
            avatar.CreatePiece("forearm_left", refs.leftForearm, new Rect(-24, -8, 51, 134));
            var leftHand = Joint("Far wrist socket", refs.leftForearm, 0, 103);
            avatar.CreatePiece("hand_left", leftHand, new Rect(-27, -8, 58, 89));
            refs.leftGlove = avatar.CreatePiece("glove_left", leftHand, new Rect(-28, -10, 58, 86));

            refs.leftLeg = Joint("Far hip", rig, -40, 190);
            avatar.CreatePiece("thigh_left", refs.leftLeg, new Rect(-47, -3, 95, 154));
            refs.leftCalf = Joint("Far knee", refs.leftLeg, 1, 120);
            avatar.CreatePiece("calf_left", refs.leftCalf, new Rect(-40, -3, 84, 171));

            refs.rightLeg = Joint("Near hip", rig, 40, 190);
            avatar.CreatePiece("thigh_right", refs.rightLeg, new Rect(-45, -3, 92, 154));
            refs.rightCalf = Joint("Near knee", refs.rightLeg, -1, 120);
            avatar.CreatePiece("calf_right", refs.rightCalf, new Rect(-37, -3, 82, 171));

            avatar.CreatePiece("torso", rig, new Rect(-99, -11, 198, 225));
            refs.shirt = avatar.CreatePiece("shirt", rig, new Rect(-99, -12, 198, 228));

            refs.headSocket = Joint("Head socket", rig, 5, -112);
            avatar.CreatePiece("head", refs.headSocket, new Rect(-52, -12, 108, 158));
            avatar.CreatePiece("eyes", refs.headSocket, new Rect(-21, 48, 64, 16));
            avatar.CreatePiece("mouth", refs.headSocket, new Rect(5, 86, 27, 8));
            refs.hair = avatar.CreatePiece("hair_spiky", refs.headSocket, new Rect(-57, -27, 133, 90));

            refs.rightArm = Joint("Near shoulder", rig, 74, 19);
            avatar.CreatePiece("upper_right", refs.rightArm, new Rect(-33, -10, 64, 142));
            refs.rightForearm = Joint("Near elbow", refs.rightArm, 0, 101);
            avatar.CreatePiece("forearm_right", refs.rightForearm, new Rect(-25, -8, 52, 134));
            var rightHand = Joint("Near wrist socket", refs.rightForearm, 0, 103);
            avatar.CreatePiece("hand_right", rightHand, new Rect(-30, -8, 58, 89));
            refs.rightGlove = avatar.CreatePiece("glove_right", rightHand, new Rect(-29, -10, 58, 86));

            ApplyDrawOrder(rig, refs);
        }

        static void ApplyDrawOrder(RectTransform rig, LayeredAvatar.RigRefs refs)
        {
            int i = 0;
            SetIndex(refs.leftCalf, i++);
            SetIndex(refs.leftLeg, i++);
            SetIndex(refs.leftForearm, i++);
            SetIndex(refs.leftArm, i++);
            SetIndex(FindChild(rig, "torso"), i++);
            SetIndex(FindChild(rig, "head"), i++);
            SetIndex(FindChild(rig, "eyes"), i++);
            SetIndex(FindChild(rig, "mouth"), i++);
            SetIndex(refs.hair.transform as RectTransform, i++);
            SetIndex(refs.rightLeg, i++);
            SetIndex(refs.rightCalf, i++);
            SetIndex(refs.shirt.transform as RectTransform, i++);
            SetIndex(refs.rightArm, i++);
            SetIndex(refs.rightForearm, i++);
            SetIndex(FindChild(rightHandParent(refs), "hand_right"), i++);
            SetIndex(FindChild(leftHandParent(refs), "hand_left"), i++);
            SetIndex(refs.rightGlove.transform as RectTransform, i++);
            SetIndex(refs.leftGlove.transform as RectTransform, i++);
        }

        static Transform leftHandParent(LayeredAvatar.RigRefs refs) => refs.leftForearm;
        static Transform rightHandParent(LayeredAvatar.RigRefs refs) => refs.rightForearm;

        static void SetIndex(RectTransform t, int index)
        {
            if (t != null) t.SetSiblingIndex(index);
        }

        static RectTransform FindChild(Transform parent, string partId)
        {
            if (parent == null) return null;
            foreach (Transform c in parent)
                if (c.name == partId) return c as RectTransform;
            return null;
        }

        static RectTransform Joint(string name, Transform parent, float x, float y) =>
            SystemUI.Node(name, parent, new Rect(x, y, 0, 0));
    }
}
