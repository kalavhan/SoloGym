using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Builds the shared skeleton and its independently sorted sprite attachments.</summary>
    public static class AvatarRigLayout
    {
        public static void Build(LayeredAvatar avatar, Transform root, Dictionary<string, LayeredAvatar.Part> parts,
            out LayeredAvatar.RigRefs refs)
        {
            var layout = CharacterReference.RigLayout;
            if (layout?.joints == null || layout.pieces == null)
                throw new InvalidOperationException("Character reference has no rig registration.");
            var rig = SystemUI.Node("Body rig", root, new Rect(0, 0, 0, 0));
            var bones = SystemUI.Node("Skeleton", rig, new Rect(0, 0, 0, 0));
            var joints = new Dictionary<string, RectTransform> { { "root", bones } };
            foreach (var joint in layout.joints)
                joints.Add(joint.id, SystemUI.Node(joint.id, joints[joint.parent], new Rect(joint.x, joint.y, 0, 0)));
            var images = new Dictionary<string, RawImage>();
            foreach (var piece in layout.pieces)
            {
                var part = parts[piece.id];
                // Preserve source proportions instead of stretching each atlas cutout into unrelated dimensions.
                var rect = new Rect(piece.x, piece.y, part.rect[2] * piece.scale, part.rect[3] * piece.scale);
                images.Add(piece.id, avatar.CreatePiece(piece.id, rig, joints[piece.socket], rect));
            }
            refs = new LayeredAvatar.RigRefs
            {
                rig = rig, headSocket = joints["head"],
                leftArm = joints["shoulder_far"], rightArm = joints["shoulder_near"],
                leftForearm = joints["elbow_far"], rightForearm = joints["elbow_near"],
                leftLeg = joints["hip_far"], rightLeg = joints["hip_near"],
                leftCalf = joints["knee_far"], rightCalf = joints["knee_near"],
                hair = images["hair_spiky"], shirt = images["shirt"],
                leftGlove = images["glove_left"], rightGlove = images["glove_right"],
                leftHand = images["hand_left"], rightHand = images["hand_right"]
            };
        }
    }
}
