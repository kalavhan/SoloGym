using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoloGym.Source3D
{
    /// <summary>Export contract. All mesh positions are in the shared, metre-scaled rig bind space.</summary>
    [Serializable]
    public sealed class CharacterSourceData
    {
        public const string Schema = "sologym.character-source3d.v1";
        public string schema, sourceId, sourceRevision, skeletonId;
        public SourceBone[] bones;
        public SourceMaterial[] materials;
        public SourceMesh[] meshes;

        public static CharacterSourceData Read(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) throw new InvalidOperationException("Missing character source: " + resourcePath);
            var data = JsonUtility.FromJson<CharacterSourceData>(asset.text);
            if (data == null) throw new InvalidOperationException("Character source JSON is empty.");
            data.Validate();
            return data;
        }

        public void Validate()
        {
            Require(schema == Schema, "Unsupported source schema: " + schema);
            Require(!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(sourceRevision) &&
                !string.IsNullOrEmpty(skeletonId), "Source and skeleton provenance is missing.");
            Require(bones != null && bones.Length > 0, "Source has no bones.");
            Require(materials != null && materials.Length > 0, "Source has no materials.");
            Require(meshes != null && meshes.Length > 0, "Source has no meshes.");
            var names = new HashSet<string>();
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                Require(bone != null && !string.IsNullOrEmpty(bone.name) && names.Add(bone.name),
                    "Missing or duplicate bone at " + i);
                Require(bone.parent >= -1 && bone.parent < i, "Bone parents must precede children: " + bone.name);
                Require(Valid(bone.position, 3) && Valid(bone.rotation, 4), "Invalid bone rest transform: " + bone.name);
                float q = 0;
                foreach (float value in bone.rotation) q += value * value;
                Require(Mathf.Abs(q - 1) < .01f, "Rest quaternion is not normalized: " + bone.name);
            }
            for (int i = 0; i < materials.Length; i++)
                Require(materials[i] != null && Valid(materials[i].color, 4), "Invalid material at " + i);
            names.Clear();
            foreach (var mesh in meshes)
            {
                Require(mesh != null && !string.IsNullOrEmpty(mesh.id) && names.Add(mesh.id), "Duplicate/missing mesh ID.");
                Require(mesh.vertices != null && mesh.vertices.Length >= 9 && mesh.vertices.Length % 3 == 0,
                    "Invalid vertices: " + mesh.id);
                int count = mesh.vertices.Length / 3;
                Require(Valid(mesh.vertices, count * 3) && Valid(mesh.normals, count * 3), "Invalid geometry: " + mesh.id);
                Require(mesh.uv == null || mesh.uv.Length == 0 || Valid(mesh.uv, count * 2), "Invalid UVs: " + mesh.id);
                Require(mesh.boneIndices != null && mesh.boneIndices.Length == count * 4 &&
                    Valid(mesh.boneWeights, count * 4), "Invalid skinning array: " + mesh.id);
                for (int vertex = 0; vertex < count; vertex++)
                {
                    float total = 0;
                    for (int influence = 0; influence < 4; influence++)
                    {
                        int index = vertex * 4 + influence;
                        Require(mesh.boneIndices[index] >= 0 && mesh.boneIndices[index] < bones.Length &&
                            mesh.boneWeights[index] >= 0, "Invalid bone influence: " + mesh.id);
                        total += mesh.boneWeights[index];
                    }
                    Require(Mathf.Abs(total - 1) < .002f, "Weights do not sum to one: " + mesh.id + " vertex " + vertex);
                }
                Require(mesh.submeshes != null && mesh.submeshes.Length > 0, "No submeshes: " + mesh.id);
                foreach (var submesh in mesh.submeshes)
                {
                    Require(submesh != null && submesh.material >= 0 && submesh.material < materials.Length &&
                        submesh.triangles != null && submesh.triangles.Length % 3 == 0, "Invalid submesh: " + mesh.id);
                    foreach (int index in submesh.triangles)
                        Require(index >= 0 && index < count, "Triangle outside vertex array: " + mesh.id);
                }
                Require(mesh.shapeDelta == null || mesh.shapeDelta.Length == 0 || Valid(mesh.shapeDelta, count * 3),
                    "Invalid body shape: " + mesh.id);
                Require(mesh.shapeNormalDelta == null || mesh.shapeNormalDelta.Length == 0 ||
                    Valid(mesh.shapeNormalDelta, count * 3), "Invalid shape normals: " + mesh.id);
            }
        }

        static bool Valid(float[] values, int length)
        {
            if (values == null || values.Length != length) return false;
            foreach (float value in values)
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            return true;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }

    [Serializable] public sealed class SourceBone
    {
        public string name;
        public int parent = -1;
        public float[] position, rotation;
    }

    [Serializable] public sealed class SourceMaterial
    {
        public string id, region;
        public float[] color;
    }

    [Serializable] public sealed class SourceSubmesh
    {
        public int material;
        public int[] triangles;
    }

    [Serializable] public sealed class SourceMesh
    {
        public string id, slot, variant, hideWithSlot;
        public float[] vertices, normals, uv, boneWeights, shapeDelta, shapeNormalDelta;
        public int[] boneIndices;
        public SourceSubmesh[] submeshes;
    }
}
