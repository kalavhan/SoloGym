using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SoloGym.Source3D
{
    /// <summary>A depth-rendered view of one source rig. Cameras never alter part registration.</summary>
    public sealed class SourceCharacterView : MonoBehaviour
    {
        const int ProofLayer = 31;
        sealed class Piece
        {
            public SourceMesh source;
            public SkinnedMeshRenderer renderer;
            public Mesh mesh;
        }
        sealed class GroundVertex
        {
            public Piece piece;
            public Vector3 position, shapeDelta;
            public BoneWeight weights;
        }

        readonly List<Piece> pieces = new List<Piece>();
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Dictionary<string, int> boneLookup = new Dictionary<string, int>();
        readonly List<GroundVertex> soleVertices = new List<GroundVertex>();
        Transform sceneRoot, rigRoot;
        Transform[] bones;
        Vector3[] restPositions;
        Quaternion[] restRotations;
        Matrix4x4[] skinMatrices, bindposes;
        Camera viewCamera;
        RawImage image;
        RenderTexture target;
        Bounds restBounds;
        string action = "idle";
        float previewTime = .55f, clockStarted;
        bool freeze = true, plainBackground;

        public bool IsLoaded { get; private set; }
        public string LastError { get; private set; }
        public CharacterSourceData Source { get; private set; }
        public RenderTexture RenderTexture => target;
        public string CurrentCameraPreset { get; private set; } = "front";
        public float TurntableYaw { get; private set; }
        public float BodyShape { get; private set; }
        public float GroundContactError { get; private set; }
        public AvatarAppearance CurrentRecipe { get; private set; }
        public string CurrentAction => action;
        public float PreviewTime => freeze ? previewTime : previewTime + Time.unscaledTime - clockStarted;
        public int ActiveMeshCount
        {
            get
            {
                int count = 0;
                foreach (var piece in pieces) if (piece.renderer.enabled) count++;
                return count;
            }
        }

        public void Mount(RectTransform parent, Rect stageRect, bool plainBackground = false)
        {
            this.plainBackground = plainBackground;
            if (image != null) Destroy(image.gameObject);
            var node = SystemUI.Node("Shared source character", parent, stageRect);
            image = node.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            // Match the actual stage aspect; allocating a square texture stretches anatomy.
            float widthPixels = Mathf.Max(1, stageRect.width) * 1.5f;
            float heightPixels = Mathf.Max(1, stageRect.height) * 1.5f;
            float resolutionScale = Mathf.Min(1, Mathf.Min(1536 / widthPixels, 2048 / heightPixels));
            int width = Mathf.Max(1, Mathf.RoundToInt(widthPixels * resolutionScale));
            int height = Mathf.Max(1, Mathf.RoundToInt(heightPixels * resolutionScale));
            CreateTarget(width, height);
            if (viewCamera != null) ConfigureCamera();
        }

        public bool Load(string resourcePath = "AvatarSource3D/Character")
        {
            ReleaseSource();
            try
            {
                Source = CharacterSourceData.Read(resourcePath);
                var shader = Resources.Load<Shader>("AvatarSource3D/CharacterToon");
                if (shader == null) throw new InvalidOperationException("Missing shared character shader.");
                sceneRoot = new GameObject("Character source proof scene").transform;
                sceneRoot.gameObject.layer = ProofLayer;
                // Keep scene content away from the main game while the dedicated camera uses layer 31.
                sceneRoot.position = new Vector3(1000 + GetInstanceID() % 100 * 5, -1000, 0);
                rigRoot = Child("Shared character rig", sceneRoot);
                BuildSkeleton();
                BuildMaterials(shader);
                foreach (var sourceMesh in Source.meshes) BuildMesh(sourceMesh);
                RegisterSoleVertices();
                if (target == null) CreateTarget(768, 1024);
                var cameraNode = Child("Character source camera", sceneRoot);
                viewCamera = cameraNode.gameObject.AddComponent<Camera>();
                viewCamera.cullingMask = 1 << ProofLayer;
                viewCamera.clearFlags = CameraClearFlags.SolidColor;
                viewCamera.orthographic = true;
                viewCamera.nearClipPlane = .02f;
                viewCamera.farClipPlane = 30;
                viewCamera.allowHDR = false;
                viewCamera.allowMSAA = true;
                viewCamera.targetTexture = target;
                viewCamera.depth = -20;
                IsLoaded = true;
                LastError = null;
                ApplyRecipe(CurrentRecipe ?? new AvatarAppearance());
                SetBodyShape(BodyShape);
                SetTurntable(TurntableYaw);
                ConfigureCamera();
                ApplyPose();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Debug.LogError("Character source: " + LastError);
                ReleaseSource();
                return false;
            }
        }

        void BuildSkeleton()
        {
            int count = Source.bones.Length;
            bones = new Transform[count];
            restPositions = new Vector3[count];
            restRotations = new Quaternion[count];
            skinMatrices = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
            {
                var source = Source.bones[i];
                var bone = Child(source.name, source.parent < 0 ? rigRoot : bones[source.parent]);
                bone.localPosition = V3(source.position, 0);
                bone.localRotation = new Quaternion(source.rotation[0], source.rotation[1], source.rotation[2], source.rotation[3]);
                bones[i] = bone;
                restPositions[i] = bone.localPosition;
                restRotations[i] = bone.localRotation;
                boneLookup[Normalize(source.name)] = i;
            }
        }

        void BuildMaterials(Shader shader)
        {
            foreach (var source in Source.materials)
            {
                var material = new Material(shader) { name = source.id + " (source proof)" };
                material.SetColor("_Color", new Color(source.color[0], source.color[1], source.color[2], source.color[3]));
                material.SetFloat("_Contour", source.region == "eye" ? 0 : source.region == "skin" ? .4f : .7f);
                ownedMaterials.Add(material);
            }
        }

        void BuildMesh(SourceMesh source)
        {
            var node = Child(source.id, rigRoot);
            var renderer = node.gameObject.AddComponent<SkinnedMeshRenderer>();
            int count = source.vertices.Length / 3;
            var vertices = Vectors(source.vertices);
            var mesh = new Mesh { name = source.id + " (registered mesh)", indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = vertices;
            mesh.normals = Vectors(source.normals);
            if (source.uv != null && source.uv.Length == count * 2)
            {
                var uv = new Vector2[count];
                for (int i = 0; i < count; i++) uv[i] = new Vector2(source.uv[i * 2], source.uv[i * 2 + 1]);
                mesh.uv = uv;
            }
            var weights = new BoneWeight[count];
            for (int i = 0; i < count; i++)
            {
                int offset = i * 4;
                weights[i] = new BoneWeight
                {
                    boneIndex0 = source.boneIndices[offset], boneIndex1 = source.boneIndices[offset + 1],
                    boneIndex2 = source.boneIndices[offset + 2], boneIndex3 = source.boneIndices[offset + 3],
                    weight0 = source.boneWeights[offset], weight1 = source.boneWeights[offset + 1],
                    weight2 = source.boneWeights[offset + 2], weight3 = source.boneWeights[offset + 3]
                };
            }
            mesh.boneWeights = weights;
            var bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bindposes[i] = bones[i].worldToLocalMatrix * node.localToWorldMatrix;
            mesh.bindposes = bindposes;
            this.bindposes = bindposes;
            mesh.subMeshCount = source.submeshes.Length;
            var materialSet = new Material[source.submeshes.Length];
            for (int i = 0; i < source.submeshes.Length; i++)
            {
                mesh.SetTriangles(source.submeshes[i].triangles, i, false);
                materialSet[i] = ownedMaterials[source.submeshes[i].material];
            }
            if (source.shapeDelta != null && source.shapeDelta.Length == source.vertices.Length)
                mesh.AddBlendShapeFrame("body_shape", 100, Vectors(source.shapeDelta),
                    source.shapeNormalDelta != null && source.shapeNormalDelta.Length == source.normals.Length
                        ? Vectors(source.shapeNormalDelta) : null, null);
            mesh.RecalculateBounds();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = materialSet;
            renderer.bones = bones;
            renderer.rootBone = bones[0];
            renderer.quality = SkinQuality.Bone4;
            renderer.updateWhenOffscreen = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Animation bounds must cover the jab without changing camera framing every frame.
            var bounds = mesh.bounds;
            bounds.Expand(1.2f);
            renderer.localBounds = bounds;
            if (pieces.Count == 0) restBounds = mesh.bounds;
            else restBounds.Encapsulate(mesh.bounds);
            pieces.Add(new Piece { source = source, renderer = renderer, mesh = mesh });
        }

        public bool ApplyRecipe(AvatarAppearance recipe)
        {
            if (recipe == null) return false;
            CurrentRecipe = recipe;
            if (!IsLoaded) return false;
            for (int i = 0; i < ownedMaterials.Count; i++)
            {
                var source = Source.materials[i];
                if (source.region == "skin")
                    ownedMaterials[i].SetColor("_Color", Retint(source, AvatarCustomizationCatalog.SkinTint(recipe.skinPaletteId)));
                else if (source.region == "hair")
                    ownedMaterials[i].SetColor("_Color", recipe.hairPaletteId == "black" || string.IsNullOrEmpty(recipe.hairPaletteId)
                        ? SourceColor(source) : Retint(source, HairTint(recipe.hairPaletteId)));
            }
            foreach (var piece in pieces)
            {
                var source = piece.source;
                bool visible = SlotEnabled(source.slot);
                if (source.slot == "hair" && !string.IsNullOrEmpty(source.variant))
                    visible &= source.variant == recipe.hairId;
                if (!string.IsNullOrEmpty(source.hideWithSlot)) visible &= !SlotEnabled(source.hideWithSlot);
                piece.renderer.enabled = visible;
            }
            return true;
        }

        bool SlotEnabled(string slot)
        {
            if (CurrentRecipe == null) return false;
            switch (slot)
            {
                case "hair": return !string.IsNullOrEmpty(CurrentRecipe.hairId) && CurrentRecipe.hairId != "bald";
                case "top": case "torso": return !string.IsNullOrEmpty(CurrentRecipe.torsoItemId);
                case "hands": case "gloves": return !string.IsNullOrEmpty(CurrentRecipe.handItemId);
                case "headgear": return !string.IsNullOrEmpty(CurrentRecipe.headItemId);
                default: return true;
            }
        }

        public bool IsPartVisible(string idOrSlot)
        {
            string slot = idOrSlot == "torso" ? "top" : idOrSlot;
            foreach (var piece in pieces)
                if ((piece.source.id == idOrSlot || piece.source.slot == slot || piece.source.slot == idOrSlot ||
                    piece.source.variant == idOrSlot) && piece.renderer.enabled) return true;
            return false;
        }

        public void SetBodyShape(float value)
        {
            BodyShape = Mathf.Clamp01(value);
            foreach (var piece in pieces)
                if (piece.mesh.blendShapeCount > 0) piece.renderer.SetBlendShapeWeight(0, BodyShape * 100);
        }

        public void SetCameraPreset(string id)
        {
            switch (id)
            {
                case "front": case "side": case "back": case "gameplay": case "face": case "three-quarter":
                    CurrentCameraPreset = id;
                    ConfigureCamera();
                    break;
                default: throw new ArgumentException("Unsupported character camera: " + id);
            }
        }

        public void SetTurntable(float yawDegrees)
        {
            TurntableYaw = yawDegrees;
            if (rigRoot != null) rigRoot.localRotation = Quaternion.Euler(0, yawDegrees, 0);
        }

        public void SetPose(string id, float seconds, bool freeze = true)
        {
            if (id != "idle" && id != "walk" && id != "jab") throw new ArgumentException("Unsupported character pose: " + id);
            action = id;
            previewTime = seconds;
            this.freeze = freeze;
            clockStarted = Time.unscaledTime;
            ApplyPose();
        }

        void LateUpdate()
        {
            if (IsLoaded) ApplyPose();
        }

        void ApplyPose()
        {
            if (!IsLoaded) return;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = restPositions[i];
                bones[i].localRotation = restRotations[i];
            }
            float time = PreviewTime;
            if (action == "walk")
            {
                float wave = Mathf.Sin(time * Mathf.PI * 2);
                Rotate(new[] { "thigh.L", "upperleg.L", "leftupperleg", "LeftUpLeg" }, -wave * 25, Vector3.right);
                Rotate(new[] { "thigh.R", "upperleg.R", "rightupperleg", "RightUpLeg" }, wave * 25, Vector3.right);
                Rotate(new[] { "shin.L", "lowerleg.L", "leftlowerleg", "LeftLeg" }, Mathf.Max(0, -wave) * 35, Vector3.right);
                Rotate(new[] { "shin.R", "lowerleg.R", "rightlowerleg", "RightLeg" }, Mathf.Max(0, wave) * 35, Vector3.right);
                Rotate(new[] { "upper_arm.L", "upperarm.L", "leftupperarm", "LeftArm" }, wave * 18, Vector3.right);
                Rotate(new[] { "upper_arm.R", "upperarm.R", "rightupperarm", "RightArm" }, -wave * 18, Vector3.right);
                // The source already bends its elbows about 48 degrees. Unbend for a relaxed walk.
                Rotate(new[] { "forearm.L", "lowerarm.L", "leftlowerarm", "LeftForeArm" }, 28, Vector3.right);
                Rotate(new[] { "forearm.R", "lowerarm.R", "rightlowerarm", "RightForeArm" }, 28, Vector3.right);
            }
            else if (action == "jab")
            {
                float punch = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * Mathf.PI * 2)), 2);
                Rotate(new[] { "chest", "spine2", "spine.002" }, -punch * 12, Vector3.up);
                Rotate(new[] { "upper_arm.L", "upperarm.L", "leftupperarm", "LeftArm" }, -40 - punch * 43, Vector3.right);
                Rotate(new[] { "forearm.L", "lowerarm.L", "leftlowerarm", "LeftForeArm" }, Mathf.Lerp(-43, 48, punch), Vector3.right);
                Rotate(new[] { "upper_arm.R", "upperarm.R", "rightupperarm", "RightArm" }, -40, Vector3.right);
                Rotate(new[] { "forearm.R", "lowerarm.R", "rightlowerarm", "RightForeArm" }, -43, Vector3.right);
            }
            else
            {
                Rotate(new[] { "chest", "spine2", "spine.002" }, Mathf.Sin(time * 2) * .65f, Vector3.right);
                Rotate(new[] { "head" }, Mathf.Sin(time * .8f) * .8f, Vector3.up);
            }
            AnchorSoles();
        }

        void RegisterSoleVertices()
        {
            soleVertices.Clear();
            float soleCeiling = restBounds.min.y + .09f;
            foreach (var piece in pieces)
            {
                if (piece.mesh.bounds.min.y > soleCeiling) continue;
                Vector3[] vertices = piece.mesh.vertices;
                BoneWeight[] weights = piece.mesh.boneWeights;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (vertices[i].y > soleCeiling) continue;
                    soleVertices.Add(new GroundVertex
                    {
                        piece = piece, position = vertices[i], weights = weights[i],
                        shapeDelta = piece.source.shapeDelta != null && piece.source.shapeDelta.Length > i * 3 + 2
                            ? V3(piece.source.shapeDelta, i * 3) : Vector3.zero
                    });
                }
            }
        }

        void AnchorSoles()
        {
            if (soleVertices.Count == 0) return;
            for (int i = 0; i < bones.Length; i++)
                skinMatrices[i] = rigRoot.worldToLocalMatrix * bones[i].localToWorldMatrix * bindposes[i];
            float lowest = float.PositiveInfinity;
            foreach (var vertex in soleVertices)
            {
                if (!vertex.piece.renderer.enabled) continue;
                Vector3 point = vertex.position + vertex.shapeDelta * BodyShape;
                BoneWeight weight = vertex.weights;
                Vector3 posed = skinMatrices[weight.boneIndex0].MultiplyPoint3x4(point) * weight.weight0
                    + skinMatrices[weight.boneIndex1].MultiplyPoint3x4(point) * weight.weight1
                    + skinMatrices[weight.boneIndex2].MultiplyPoint3x4(point) * weight.weight2
                    + skinMatrices[weight.boneIndex3].MultiplyPoint3x4(point) * weight.weight3;
                lowest = Mathf.Min(lowest, posed.y);
            }
            if (!Finite(lowest)) return;
            // A shared root correction preserves all part registration. This is floor contact for
            // a stationary pose probe, not a foot-plant/stride IK system for gameplay locomotion.
            float correction = restBounds.min.y - lowest;
            bones[0].localPosition += Vector3.up * correction;
        }

        void Rotate(string[] aliases, float degrees, Vector3 rigAxis)
        {
            foreach (string alias in aliases)
            {
                if (!boneLookup.TryGetValue(Normalize(alias), out int index)) continue;
                Transform bone = bones[index];
                Vector3 axis = bone.parent.InverseTransformDirection(rigRoot.TransformDirection(rigAxis));
                bone.localRotation = Quaternion.AngleAxis(degrees, axis) * bone.localRotation;
                return;
            }
        }

        void ConfigureCamera()
        {
            if (viewCamera == null) return;
            float height = Mathf.Max(1, restBounds.size.y);
            Vector3 focus = new Vector3(restBounds.center.x, restBounds.min.y + height * .51f, restBounds.center.z);
            Vector3 direction = new Vector3(0, .02f, 1);
            float size = height * .565f;
            switch (CurrentCameraPreset)
            {
                case "side": direction = Vector3.right; break;
                case "back": direction = Vector3.back; break;
                case "three-quarter": direction = new Vector3(.65f, .08f, 1); break;
                case "gameplay": direction = new Vector3(.7f, 1.15f, 1); size = height * .59f; break;
                case "face":
                    focus.y = restBounds.min.y + height * .895f;
                    focus.z += height * .012f;
                    direction = new Vector3(.16f, .025f, 1);
                    size = height * .17f;
                    break;
            }
            // This camera includes the entire registered source, including a broad A-pose.
            if (CurrentCameraPreset != "face" && CurrentCameraPreset != "side")
                size = Mathf.Max(size, restBounds.size.x / Mathf.Max(.2f, target.width / (float)target.height) * .56f);
            viewCamera.aspect = target.width / (float)target.height;
            viewCamera.orthographicSize = size;
            viewCamera.backgroundColor = plainBackground ? new Color(.095f, .115f, .15f, 1) : Color.clear;
            viewCamera.transform.localPosition = focus + direction.normalized * height * 3;
            viewCamera.transform.LookAt(sceneRoot.TransformPoint(focus), Vector3.up);
        }

        public bool TryValidate(out string reason)
        {
            if (!IsLoaded) { reason = LastError ?? "Source not loaded."; return false; }
            Mesh baked = null;
            try
            {
                Source.Validate();
                if (ActiveMeshCount == 0) throw new InvalidOperationException("No character meshes are visible.");
                ApplyPose();
                baked = new Mesh();
                float lowest = float.PositiveInfinity;
                foreach (var piece in pieces)
                {
                    if (!piece.renderer.enabled) continue;
                    piece.renderer.BakeMesh(baked);
                    foreach (Vector3 point in baked.vertices)
                    {
                        if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z))
                            throw new InvalidOperationException("Non-finite posed vertex: " + piece.source.id);
                        lowest = Mathf.Min(lowest, point.y);
                    }
                    if (baked.bounds.size.magnitude > 20)
                        throw new InvalidOperationException("Unbounded posed mesh: " + piece.source.id);
                    baked.Clear();
                }
                GroundContactError = Mathf.Abs(lowest - restBounds.min.y);
                if (GroundContactError > .002f)
                    throw new InvalidOperationException("Posed soles departed source ground by " + GroundContactError.ToString("F4") + " metres.");
                reason = "Source schema, hierarchy, weights, indices, posed vertices and sole contact valid. Visual fit remains a separate review.";
                return true;
            }
            catch (Exception ex) { reason = ex.Message; return false; }
            finally { if (baked != null) Destroy(baked); }
        }

        public void CapturePng(string path)
        {
            if (!IsLoaded) throw new InvalidOperationException("Cannot capture unloaded character source.");
            ApplyPose();
            viewCamera.Render();
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Destroy(texture); }
        }

        void CreateTarget(int width, int height)
        {
            if (target != null) { target.Release(); Destroy(target); }
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Shared character view", antiAliasing = 4, filterMode = FilterMode.Bilinear
            };
            target.Create();
            if (image != null) image.texture = target;
            if (viewCamera != null) viewCamera.targetTexture = target;
        }

        void OnEnable() { if (sceneRoot != null) sceneRoot.gameObject.SetActive(true); }
        void OnDisable() { if (sceneRoot != null) sceneRoot.gameObject.SetActive(false); }
        void OnDestroy()
        {
            ReleaseSource();
            if (target != null) { target.Release(); Destroy(target); }
            if (image != null) Destroy(image.gameObject);
        }

        void ReleaseSource()
        {
            IsLoaded = false;
            if (sceneRoot != null) { sceneRoot.gameObject.SetActive(false); Destroy(sceneRoot.gameObject); }
            sceneRoot = rigRoot = null;
            viewCamera = null;
            foreach (var piece in pieces) if (piece.mesh != null) Destroy(piece.mesh);
            pieces.Clear();
            soleVertices.Clear();
            foreach (var material in ownedMaterials) if (material != null) Destroy(material);
            ownedMaterials.Clear();
            boneLookup.Clear();
            bones = null;
            Source = null;
        }

        static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.gameObject.layer = ProofLayer;
            return child;
        }
        static Vector3 V3(float[] values, int offset) => new Vector3(values[offset], values[offset + 1], values[offset + 2]);
        static Vector3[] Vectors(float[] values)
        {
            var vectors = new Vector3[values.Length / 3];
            for (int i = 0; i < vectors.Length; i++) vectors[i] = V3(values, i * 3);
            return vectors;
        }
        static string Normalize(string name) => name.ToLowerInvariant().Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
        static bool Finite(float value) => !float.IsInfinity(value) && !float.IsNaN(value);
        static Color HairTint(string palette) => palette == "brown" ? new Color(.14f, .07f, .04f) : new Color(.035f, .045f, .065f);
        static Color SourceColor(SourceMaterial source) => new Color(source.color[0], source.color[1], source.color[2], source.color[3]);
        Color Retint(SourceMaterial source, Color palette)
        {
            // Palette changes preserve authored highlights and shade patches inside the region.
            Color basis = SourceColor(source);
            foreach (var candidate in Source.materials)
                if (candidate.region == source.region) { basis = SourceColor(candidate); break; }
            Color authored = SourceColor(source);
            return new Color(Mathf.Clamp01(authored.r * palette.r / Mathf.Max(.001f, basis.r)),
                Mathf.Clamp01(authored.g * palette.g / Mathf.Max(.001f, basis.g)),
                Mathf.Clamp01(authored.b * palette.b / Mathf.Max(.001f, basis.b)), authored.a);
        }
    }
}
