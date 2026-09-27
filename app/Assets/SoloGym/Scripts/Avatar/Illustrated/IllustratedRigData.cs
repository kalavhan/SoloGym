using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoloGym.Illustrated
{
    [Serializable] public sealed class IllustratedRigData
    {
        public string schema, sourceRevision, rigId;
        public IllustratedEntry[] entries;

        public void Validate()
        {
            Require(schema == "sologym.illustrated-rig.v1", "Unsupported illustrated rig schema.");
            Require(!string.IsNullOrEmpty(sourceRevision) && !string.IsNullOrEmpty(rigId), "Missing illustrated source provenance.");
            Require(entries != null && entries.Length > 0, "No illustrated entries.");
            var entryIds = new HashSet<string>();
            foreach (var entry in entries)
            {
                Require(entry != null && !string.IsNullOrEmpty(entry.id) && entryIds.Add(entry.id), "Invalid entry ID.");
                Require(!string.IsNullOrEmpty(entry.resource), "Missing illustrated texture resource.");
                Require(entry.cells != null && entry.cells.Length == 2, "Each proof needs base and equipped cells.");
                var cellIds = new HashSet<string>();
                foreach (var cell in entry.cells)
                {
                    Require(cell != null && cellIds.Add(cell.id) && (cell.id == "base" || cell.id == "equipped"), "Invalid cell ID.");
                    Require(Valid(cell.rect, 4) && cell.rect[0] >= 0 && cell.rect[1] >= 0 && cell.rect[2] > 0 && cell.rect[3] > 0, "Invalid cell rect.");
                    Require(Valid(cell.pivot, 2) && Valid(cell.ground, 2), "Invalid source pivot or ground.");
                    Require(Valid(cell.skinSource, 3) && Valid(cell.hairSource, 3), "Missing recolor source colors.");
                    Require(cell.joints != null && cell.joints.Length >= 12, "Incomplete illustrated skeleton.");
                    var names = new HashSet<string>();
                    foreach (var joint in cell.joints)
                    {
                        Require(joint != null && !string.IsNullOrEmpty(joint.id) && names.Add(joint.id), "Missing/duplicate joint.");
                        Require(string.IsNullOrEmpty(joint.parent) || (joint.parent != joint.id && names.Contains(joint.parent)), "Parents must precede children: " + joint.id);
                        Require(Valid(joint.point, 2) && Finite(joint.radius) && joint.radius > 0, "Invalid joint registration: " + joint.id);
                    }
                    foreach (string required in new[] { "pelvis", "chest", "neck", "head", "shoulder_l", "elbow_l", "hand_l", "shoulder_r", "elbow_r", "hand_r", "hip_l", "knee_l", "foot_l", "hip_r", "knee_r", "foot_r" })
                        Require(names.Contains(required), "Missing required joint: " + required);
                    Require(cell.deformRegions != null && cell.deformRegions.Length == 4, "Missing anatomical deformation regions.");
                    var regionIds = new HashSet<string>();
                    foreach (var region in cell.deformRegions)
                        Require(region != null && regionIds.Add(region.id) &&
                            (region.id == "arm_l" || region.id == "arm_r" || region.id == "leg_l" || region.id == "leg_r") &&
                            region.points != null && region.points.Length >= 6 && region.points.Length % 2 == 0 &&
                            Valid(region.points, region.points.Length), "Invalid anatomical deformation region.");
                    if (cell.underlaps != null)
                        foreach (var patch in cell.underlaps)
                            Require(patch != null && !string.IsNullOrEmpty(patch.id) && Valid(patch.uvOffset, 2) &&
                                patch.points != null && patch.points.Length >= 6 && patch.points.Length % 2 == 0 &&
                                Valid(patch.points, patch.points.Length), "Invalid registered cloth continuation.");
                    if (cell.maskPolygons != null)
                        foreach (var polygon in cell.maskPolygons)
                            Require(polygon != null && (polygon.region == "skin" || polygon.region == "hair") &&
                                polygon.points != null && polygon.points.Length >= 6 && polygon.points.Length % 2 == 0 &&
                                Valid(polygon.points, polygon.points.Length), "Invalid recolor polygon.");
                }
            }
        }

        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Valid(float[] values, int size)
        {
            if (values == null || values.Length != size) return false;
            foreach (float value in values) if (!Finite(value)) return false;
            return true;
        }
        static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }

    [Serializable] public sealed class IllustratedEntry
    {
        public string id, presentation, view, resource, maskResource;
        public IllustratedCell[] cells;
    }
    [Serializable] public sealed class IllustratedCell
    {
        public string id;
        public float[] rect, pivot, ground, solePoints, skinSource, hairSource;
        public IllustratedJoint[] joints;
        public IllustratedMaskPolygon[] maskPolygons;
        public IllustratedDeformRegion[] deformRegions;
        public IllustratedUnderlap[] underlaps;
    }
    [Serializable] public sealed class IllustratedJoint
    {
        public string id, parent;
        public float[] point;
        public float radius;
    }
    [Serializable] public sealed class IllustratedMaskPolygon
    {
        public string region;
        public float[] points;
    }
    [Serializable] public sealed class IllustratedDeformRegion
    {
        public string id;
        public float[] points;
    }
    [Serializable] public sealed class IllustratedUnderlap
    {
        public string id;
        public float[] points, uvOffset;
    }
}
