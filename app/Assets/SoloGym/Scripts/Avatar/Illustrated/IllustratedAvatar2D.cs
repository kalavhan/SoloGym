using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.Illustrated
{
    /// <summary>
    /// Bounded illustrated proof: one coherent surface, continuous weighted deformation and
    /// registered base/equipped texture cells. It does not claim modular clothing or new views.
    /// </summary>
    public sealed class IllustratedAvatar2D : MonoBehaviour
    {
        const int GridColumns = 60, GridRows = 90;
        const int ArmProfileSamples = 64;
        struct Influence
        {
            public int a, b, c, d;
            public float wa, wb, wc, wd;
        }
        sealed class Region
        {
            public string id;
            public int[] allowedBones;
            public float attachmentStart;
            public Vector2 armAlong, armAcross;
            public float armLength, armWidth, armStartCenter, armEndCenter;
            public float[] armMin, armMax;
            public Rect bindRect;
            public int columns, rows;
            public bool[] visibleTriangles;
            public Vector2[] validationVertices;
            public IllustratedDeformRegion source;
            public IllustratedUnderlap patch;
            public readonly Dictionary<Vector2, Influence> weights = new Dictionary<Vector2, Influence>();
            public AvatarRegisteredMesh graphic;
            public Texture2D ownership;
            public Texture2D underlap;
            public Material material;
            public Mesh captureMesh;
            public MeshRenderer captureRenderer;
            public Vector3[] captureVertices;
            public Vector2[] captureUVs;
        }
        IllustratedRigData data;
        IllustratedEntry entry;
        IllustratedCell cell;
        Texture2D atlas, generatedMask;
        Material material;
        Camera captureCamera;
        Transform captureScene;
        AvatarRegisteredMesh graphic;
        RectTransform stage, rig;
        Rect stageRect;
        readonly Dictionary<string, int> indices = new Dictionary<string, int>();
        readonly List<Region> regions = new List<Region>();
        readonly List<Vector2> soleContour = new List<Vector2>();
        readonly List<Vector2> leftSole = new List<Vector2>(), rightSole = new List<Vector2>();
        float leftGround, rightGround;
        Vector2[] bind, posed, segmentEnds;
        float[] angles, localAngles, cosAngles, sinAngles;
        int[] parents;
        float groundOffset, groundY, poseTime, started;
        Rect sourceBindRect;
        bool freeze = true;
        string cameraPreset = "full";

        public bool IsLoaded { get; private set; }
        public string LastError { get; private set; }
        public string CurrentEntryId => entry != null ? entry.id : null;
        public string SourceRevision => data != null ? data.sourceRevision : null;
        public string Presentation { get; private set; } = "male";
        public string View { get; private set; } = "studio";
        public bool IsEquipped { get; private set; } = true;
        public string SkinPaletteId { get; private set; } = "source";
        public string HairPaletteId { get; private set; } = "black";
        public string CurrentAction { get; private set; } = "bind";
        public float PreviewTime => freeze ? poseTime : poseTime + Time.unscaledTime - started;
        public float GroundError { get; private set; }
        public float RegistrationGroundError { get; private set; }
        public float WeightSumError { get; private set; }
        public float MaxVertexDisplacement { get; private set; }
        public int FoldedArmTriangleCount { get; private set; }
        public int ReassignedArmPixelCount { get; private set; }
        public int InvalidVertexCount
        {
            get { int count = 0; foreach (var region in regions) count += region.graphic.InvalidVertexCount; return count; }
        }
        public int MeshVertexCount
        {
            get { int count = 0; foreach (var region in regions) count += (region.columns + 1) * (region.rows + 1); return count; }
        }

        public void Mount(RectTransform parent, Rect rect)
        {
            stageRect = rect;
            stage = SystemUI.Node("Illustrated character stage", parent, rect);
            stage.gameObject.AddComponent<RectMask2D>();
            rig = SystemUI.Node("Illustrated source registration", stage, new Rect(rect.width * .5f, rect.height * .92f, 0, 0));
            var surface = SystemUI.Node("Continuous illustrated surface", rig, new Rect(0, 0, 1, 1));
            graphic = surface.gameObject.AddComponent<AvatarRegisteredMesh>();
            graphic.raycastTarget = false;
        }

        public bool Load(string resource = "AvatarIllustrated/Registration")
        {
            try
            {
                if (graphic == null) throw new InvalidOperationException("Mount the illustrated viewer before loading it.");
                var source = Resources.Load<TextAsset>(resource);
                if (source == null) throw new InvalidOperationException("Missing illustrated registration: " + resource);
                data = JsonUtility.FromJson<IllustratedRigData>(source.text);
                if (data == null) throw new InvalidOperationException("Empty illustrated registration.");
                data.Validate();
                var shader = Resources.Load<Shader>("AvatarIllustrated/IllustratedTint");
                if (shader == null) throw new InvalidOperationException("Missing illustrated material shader.");
                if (material != null) Destroy(material);
                material = new Material(shader) { name = "Illustrated source masks" };
                graphic.material = material;
                IsLoaded = true;
                if (!SetCharacter(Presentation, View)) throw new InvalidOperationException(LastError);
                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                IsLoaded = false;
                LastError = ex.Message;
                Debug.LogError("SOLOGYM_ILLUSTRATED_LOAD " + LastError);
                return false;
            }
        }

        public bool SetCharacter(string presentation, string view)
        {
            if (!IsLoaded) return false;
            foreach (var candidate in data.entries)
            {
                if (candidate.presentation != presentation || candidate.view != view) continue;
                var previousEntry = entry;
                var previousAtlas = atlas;
                string previousPresentation = Presentation, previousView = View;
                try
                {
                    var candidateAtlas = Resources.Load<Texture2D>(candidate.resource);
                    if (candidateAtlas == null) throw new InvalidOperationException("Missing illustrated atlas: " + candidate.resource);
                    if (!candidateAtlas.isReadable) throw new InvalidOperationException("Illustrated atlas is not readable: " + candidate.resource);
                    entry = candidate;
                    Presentation = presentation;
                    View = view;
                    atlas = candidateAtlas;
                    RegisterCell();
                    graphic.enabled = true;
                    LastError = null;
                    return true;
                }
                catch (Exception ex)
                {
                    entry = previousEntry;
                    atlas = previousAtlas;
                    Presentation = previousPresentation;
                    View = previousView;
                    LastError = ex.Message;
                    if (entry != null && atlas != null)
                    {
                        try { RegisterCell(); }
                        catch (Exception restoreError)
                        {
                            IsLoaded = false;
                            graphic.enabled = false;
                            LastError += "; prior view recovery failed: " + restoreError.Message;
                        }
                    }
                    else { cell = null; graphic.enabled = false; }
                    return false;
                }
            }
            LastError = "No authored illustrated view for " + presentation + "/" + view;
            return false;
        }

        public void SetEquipped(bool equipped)
        {
            IsEquipped = equipped;
            if (IsLoaded && entry != null) RegisterCell();
        }

        void RegisterCell()
        {
            cell = null;
            foreach (var candidate in entry.cells)
                if (candidate.id == (IsEquipped ? "equipped" : "base")) { cell = candidate; break; }
            if (cell == null) throw new InvalidOperationException("Missing outfit state.");
            if (cell.rect[0] + cell.rect[2] > atlas.width + .01f || cell.rect[1] + cell.rect[3] > atlas.height + .01f)
                throw new InvalidOperationException("Registered cell extends outside its source atlas.");
            int count = cell.joints.Length;
            bind = new Vector2[count];
            posed = new Vector2[count];
            segmentEnds = new Vector2[count];
            angles = new float[count];
            localAngles = new float[count];
            cosAngles = new float[count];
            sinAngles = new float[count];
            parents = new int[count];
            indices.Clear();
            WeightSumError = 0;
            for (int i = 0; i < count; i++)
            {
                var joint = cell.joints[i];
                indices[joint.id] = i;
                bind[i] = SourcePoint(joint.point[0], joint.point[1]);
                parents[i] = string.IsNullOrEmpty(joint.parent) ? -1 : indices[joint.parent];
                segmentEnds[i] = bind[i];
            }
            // A bone influences the anatomical segment from its named pivot to its main child.
            for (int i = 0; i < count; i++)
            {
                string child = MainChild(cell.joints[i].id);
                if (child != null && indices.TryGetValue(child, out int childIndex)) segmentEnds[i] = bind[childIndex];
            }
            groundY = SourcePoint(cell.ground[0], cell.ground[1]).y;
            sourceBindRect = new Rect(-cell.pivot[0] * cell.rect[2], (cell.pivot[1] - 1) * cell.rect[3], cell.rect[2], cell.rect[3]);
            BuildRegions();
            RegisterSoleContour();
            var uv = new Rect(cell.rect[0] / atlas.width, 1 - (cell.rect[1] + cell.rect[3]) / atlas.height,
                cell.rect[2] / atlas.width, cell.rect[3] / atlas.height);
            material.mainTexture = atlas;
            material.SetVector("_CellUV", new Vector4(uv.x, uv.y, uv.width, uv.height));
            BuildMask();
            SetColors(SkinPaletteId, HairPaletteId);
            EvaluateBones();
            foreach (var region in regions)
            {
                ApplyRegionMaterial(region);
                region.graphic.material = region.material;
                region.graphic.Configure(atlas, RegionMeshUV(region), region.bindRect, point => PoseVertex(point, region), region.columns, region.rows);
            }
            ApplyFraming();
            UpdateSurface();
        }

        public void SetColors(string skinPaletteId, string hairPaletteId)
        {
            SkinPaletteId = skinPaletteId;
            HairPaletteId = hairPaletteId;
            if (material == null || cell == null) return;
            var skinSource = C(cell.skinSource);
            var hairSource = C(cell.hairSource);
            material.SetColor("_SkinSource", skinSource);
            material.SetColor("_HairSource", hairSource);
            material.SetFloat("_SkinEnabled", skinPaletteId == "source" || skinPaletteId == "warm" ? 0 : 1);
            material.SetFloat("_HairEnabled", hairPaletteId == "black" ? 0 : 1);
            material.SetColor("_SkinTint", skinPaletteId == "source" ? skinSource : AvatarCustomizationCatalog.SkinTint(skinPaletteId));
            Color hair = hairSource;
            if (hairPaletteId == "brown") hair = new Color(.22f, .105f, .052f);
            else if (hairPaletteId == "silver") hair = new Color(.45f, .49f, .54f);
            material.SetColor("_HairTint", hair);
            foreach (var region in regions) ApplyRegionMaterial(region);
        }

        Rect RegionUV(Region region)
        {
            var uv = new Rect(cell.rect[0] / atlas.width, 1 - (cell.rect[1] + cell.rect[3]) / atlas.height,
                cell.rect[2] / atlas.width, cell.rect[3] / atlas.height);
            if (region.patch != null)
            {
                uv.x += region.patch.uvOffset[0] * uv.width;
                uv.y -= region.patch.uvOffset[1] * uv.height;
            }
            return uv;
        }

        Rect RegionMeshUV(Region region)
        {
            Rect uv = RegionUV(region);
            return new Rect(uv.x + (region.bindRect.xMin - sourceBindRect.xMin) / sourceBindRect.width * uv.width,
                uv.y + (region.bindRect.yMin - sourceBindRect.yMin) / sourceBindRect.height * uv.height,
                region.bindRect.width / sourceBindRect.width * uv.width, region.bindRect.height / sourceBindRect.height * uv.height);
        }

        void ApplyRegionMaterial(Region region)
        {
            region.material.CopyPropertiesFromMaterial(material);
            Rect uv = RegionUV(region);
            region.material.SetVector("_CellUV", new Vector4(uv.x, uv.y, uv.width, uv.height));
            region.material.SetTexture("_RegionTex", region.ownership);
            region.material.SetTexture("_UnderlapTex", region.underlap != null ? region.underlap : Texture2D.blackTexture);
            region.material.SetFloat("_UseRegionMask", 1);
            region.material.SetFloat("_UnderlapEnabled", region.id == "body" && (CurrentAction == "walk" || CurrentAction == "jab") ? 1 : 0);
            if (region.patch != null)
            {
                // The only supported continuations in this proof are explicitly authored cloth.
                region.material.SetFloat("_SkinEnabled", 0);
                region.material.SetFloat("_HairEnabled", 0);
            }
        }

        public void SetCameraPreset(string preset)
        {
            if (preset != "full" && preset != "face") throw new ArgumentException("Unsupported illustrated camera: " + preset);
            cameraPreset = preset;
            ApplyFraming();
        }

        void ApplyFraming()
        {
            if (cell == null || rig == null) return;
            float scale = Mathf.Min(stageRect.width * .94f / (cell.rect[2] * 1.3f), stageRect.height * .94f / cell.rect[3]);
            if (cameraPreset == "face")
            {
                scale *= 2.8f;
                Vector2 head = bind[indices["head"]];
                rig.anchoredPosition = new Vector2(stageRect.width * .5f - head.x * scale, -stageRect.height * .48f - head.y * scale);
            }
            else rig.anchoredPosition = new Vector2(stageRect.width * .5f, -stageRect.height * .94f);
            rig.localScale = Vector3.one * scale;
        }

        public void SetPose(string action, float seconds, bool freeze = true)
        {
            if (action != "bind" && action != "idle" && action != "walk" && action != "jab")
                throw new ArgumentException("Unsupported illustrated action: " + action);
            if (!IllustratedRigData.Finite(seconds)) throw new ArgumentException("Non-finite pose time.");
            CurrentAction = action;
            poseTime = seconds;
            this.freeze = freeze;
            started = Time.unscaledTime;
            if (IsLoaded && cell != null) UpdateSurface();
        }

        void LateUpdate() { if (IsLoaded && cell != null && !freeze) UpdateSurface(); }

        void EvaluateBones()
        {
            Array.Clear(localAngles, 0, localAngles.Length);
            float time = PreviewTime;
            if (CurrentAction == "idle")
            {
                // Keep the authored face and ground fixed; a small chest breath is enough here.
                Turn("chest", Mathf.Sin(time * 2) * .35f);
                Turn("neck", -Mathf.Sin(time * 2) * .35f);
            }
            else if (CurrentAction == "walk")
            {
                float wave = Mathf.Sin(time * Mathf.PI * 2);
                WalkLeg("_l", time);
                WalkLeg("_r", time + .5f);
                Turn("shoulder_l", -wave * 5); Turn("shoulder_r", wave * 5);
                Turn("elbow_l", -2); Turn("elbow_r", 2);
            }
            else if (CurrentAction == "jab")
            {
                float punch = Mathf.Pow(Mathf.Max(0, Mathf.Sin(time * Mathf.PI * 2)), 2);
                // This surface has one authored projection: keep the punch in that plane and
                // limit shoulder twist rather than fabricating unseen arm/torso surfaces.
                float side = Mathf.Sign(bind[indices["shoulder_r"]].x - bind[indices["chest"]].x);
                if (side == 0) side = 1;
                Turn("shoulder_r", side * punch * 78);
                Turn("elbow_r", side * punch * 8);
                Turn("chest", -side * punch * 1.5f);
                Turn("neck", side * punch * 1.5f);
            }
            for (int i = 0; i < bind.Length; i++)
            {
                int parent = parents[i];
                if (parent < 0) { angles[i] = localAngles[i]; posed[i] = bind[i]; }
                else
                {
                    angles[i] = angles[parent] + localAngles[i];
                    posed[i] = posed[parent] + Rotate(bind[i] - bind[parent], angles[parent]);
                }
                cosAngles[i] = Mathf.Cos(angles[i] * Mathf.Deg2Rad);
                sinAngles[i] = Mathf.Sin(angles[i] * Mathf.Deg2Rad);
            }
            groundOffset = 0;
            if (CurrentAction == "walk")
            {
                bool leftStance = Mathf.Repeat(time, 1) < .5f;
                float baseline = leftStance ? leftGround : rightGround;
                float contact = LowestPosedSole(leftStance ? leftSole : rightSole);
                groundOffset = baseline - contact;
                float swingBaseline = leftStance ? rightGround : leftGround;
                float swing = LowestPosedSole(leftStance ? rightSole : leftSole) + groundOffset;
                GroundError = Mathf.Max(Mathf.Abs(contact + groundOffset - baseline), Mathf.Max(0, swingBaseline - swing));
            }
            else
            {
                float lowest = LowestPosedSole(soleContour);
                if (CurrentAction != "bind") groundOffset = groundY - lowest;
                GroundError = Mathf.Abs(lowest + groundOffset - groundY);
            }
        }

        void WalkLeg(string side, float time)
        {
            float phase = Mathf.Repeat(time, 1);
            if (phase < .5f) return; // The stance ankle remains exactly at its registered bind point.
            float swing = (phase - .5f) * 2;
            float lift = Mathf.Sin(swing * Mathf.PI) * cell.rect[3] * .038f;
            if (lift < .001f) return;
            Vector2 hip = bind[indices["hip" + side]], knee = bind[indices["knee" + side]], ankle = bind[indices["foot" + side]];
            float travel = Mathf.Sin(swing * Mathf.PI * 2) * cell.rect[2] * .01f;
            Vector2 target = ankle + new Vector2(travel, lift);
            Vector2 upper = knee - hip, lower = ankle - knee, direction = target - hip;
            float firstLength = upper.magnitude, secondLength = lower.magnitude;
            float length = Mathf.Clamp(direction.magnitude, Mathf.Abs(firstLength - secondLength) + .001f, firstLength + secondLength - .001f);
            Vector2 oldDirection = ankle - hip;
            float bend = Mathf.Sign(oldDirection.x * upper.y - oldDirection.y * upper.x);
            if (bend == 0) bend = side == "_l" ? -1 : 1;
            float angle = Mathf.Atan2(direction.y, direction.x) + bend * Mathf.Acos(Mathf.Clamp(
                (firstLength * firstLength + length * length - secondLength * secondLength) / (2 * firstLength * length), -1, 1));
            float hipAngle = Mathf.DeltaAngle(Mathf.Atan2(upper.y, upper.x) * Mathf.Rad2Deg, angle * Mathf.Rad2Deg);
            Vector2 solvedKnee = hip + Rotate(upper, hipAngle);
            Vector2 solvedLower = target - solvedKnee;
            float kneeAngle = Mathf.DeltaAngle(Mathf.Atan2(lower.y, lower.x) * Mathf.Rad2Deg,
                Mathf.Atan2(solvedLower.y, solvedLower.x) * Mathf.Rad2Deg) - hipAngle;
            Turn("hip" + side, hipAngle);
            Turn("knee" + side, kneeAngle);
            Turn("foot" + side, -hipAngle - kneeAngle);
        }

        void UpdateSurface()
        {
            EvaluateBones();
            MaxVertexDisplacement = 0;
            foreach (var region in regions)
            {
                region.material.SetFloat("_UnderlapEnabled", region.id == "body" && (CurrentAction == "walk" || CurrentAction == "jab") ? 1 : 0);
                region.graphic.UpdatePose();
            }
        }

        Vector3 PoseVertex(Vector2 point, Region region)
        {
            // Exact identity at bind avoids accumulated floating-point drift during reference checks.
            if (CurrentAction == "bind") return point;
            Vector2 skinned = RegionSkin(point, region) + Vector2.up * groundOffset;
            MaxVertexDisplacement = Mathf.Max(MaxVertexDisplacement, Vector2.Distance(point, skinned));
            return skinned;
        }

        Vector2 Skin(Vector2 point, Influence influence) =>
            BonePoint(point, influence.a) * influence.wa + BonePoint(point, influence.b) * influence.wb
            + BonePoint(point, influence.c) * influence.wc + BonePoint(point, influence.d) * influence.wd;
        Vector2 RegionSkin(Vector2 point, Region region)
        {
            Influence weights = GetWeights(point, region);
            if (CurrentAction == "jab" && region.id == "arm_r" && region.armWidth > 0)
            {
                int shoulder = region.allowedBones[0];
                Vector2 offset = point - bind[shoulder];
                float along = Vector2.Dot(offset, region.armAlong) / region.armLength;
                if (along > -.2f && along < 1)
                {
                    // The far upper arm is partly hidden in the authored resting projection.
                    // A full extension must restore its projected volume, not rotate the narrow
                    // visible strip into a cord. Derive that volume from this same arm's bicep;
                    // the source UVs, bind silhouette and punch angle remain unchanged.
                    float profile = Mathf.Clamp(along * ArmProfileSamples - .5f, 0, ArmProfileSamples - 1);
                    int first = Mathf.FloorToInt(profile), next = Mathf.Min(first + 1, ArmProfileSamples - 1);
                    float lower = Mathf.Lerp(region.armMin[first], region.armMin[next], profile - first);
                    float upper = Mathf.Lerp(region.armMax[first], region.armMax[next], profile - first);
                    float width = Mathf.Max(1, upper - lower);
                    float expansion = Mathf.Clamp(region.armWidth / width - 1, 0, 4);
                    float extension = Mathf.SmoothStep(0, 1, Mathf.Abs(localAngles[shoulder]) / 60);
                    float cap = Mathf.SmoothStep(0, 1, (along + .2f) / .3f);
                    float elbow = 1 - Mathf.SmoothStep(0, 1, (along - .65f) / .35f);
                    float across = Vector2.Dot(offset, region.armAcross);
                    // Include one neighboring grid cell so a narrow source strip still samples
                    // the linear corrective at both triangle edges. Outside it only translate;
                    // do not magnify distant, transparent vertices.
                    float band = width * .5f + Mathf.Max(region.bindRect.width / region.columns, region.bindRect.height / region.rows);
                    float center = (lower + upper) * .5f;
                    float targetCenter = Mathf.Lerp(region.armStartCenter, region.armEndCenter, Mathf.Clamp01(along));
                    float fromCenter = Mathf.Clamp(across - center, -band, band);
                    point += region.armAcross * ((fromCenter * expansion + targetCenter - center) * extension * cap * elbow);
                }
            }
            return Skin(point, weights);
        }
        Vector2 BonePoint(Vector2 point, int bone)
        {
            Vector2 offset = point - bind[bone];
            return posed[bone] + new Vector2(offset.x * cosAngles[bone] - offset.y * sinAngles[bone],
                offset.x * sinAngles[bone] + offset.y * cosAngles[bone]);
        }

        void RegisterSoleContour()
        {
            soleContour.Clear();
            leftSole.Clear(); rightSole.Clear();
            leftGround = rightGround = float.PositiveInfinity;
            if (!atlas.isReadable) throw new InvalidOperationException("Illustrated atlas must remain readable for optical ground registration.");
            Color32[] pixels = atlas.GetPixels32();
            int width = Mathf.RoundToInt(cell.rect[2]), height = Mathf.RoundToInt(cell.rect[3]);
            int startX = Mathf.RoundToInt(cell.rect[0]), startY = Mathf.RoundToInt(cell.rect[1]);
            int firstRow = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(cell.joints[indices["foot_l"]].point[1],
                cell.joints[indices["foot_r"]].point[1]) * height - height * .04f), 0, height - 1);
            float lowest = float.PositiveInfinity;
            // Sample the opaque foot silhouette (not empty cell corners or ankle pivots).
            // Neighbor tests retain side/toe edges that become lowest during a rotated step.
            for (int y = firstRow; y < height; y++)
                for (int x = 1; x < width - 1; x++)
                {
                    int index = (atlas.height - 1 - (startY + y)) * atlas.width + startX + x;
                    if (pixels[index].a < 192) continue;
                    bool boundary = y == height - 1 || pixels[index - 1].a < 192 || pixels[index + 1].a < 192
                        || (index >= atlas.width && pixels[index - atlas.width].a < 192);
                    if (!boundary) continue;
                    Vector2 point = SourcePoint((x + .5f) / width, (y + .5f) / height);
                    soleContour.Add(point);
                    if ((point - bind[indices["foot_l"]]).sqrMagnitude < (point - bind[indices["foot_r"]]).sqrMagnitude)
                    { leftSole.Add(point); leftGround = Mathf.Min(leftGround, point.y); }
                    else { rightSole.Add(point); rightGround = Mathf.Min(rightGround, point.y); }
                    lowest = Mathf.Min(lowest, point.y);
                }
            if (soleContour.Count == 0) throw new InvalidOperationException("No opaque sole contours in registered foot region.");
            if (leftSole.Count == 0 || rightSole.Count == 0) throw new InvalidOperationException("Both registered feet need opaque sole contours.");
            RegistrationGroundError = Mathf.Abs(lowest - groundY);
            // Retain the actual source floor as the animation baseline; report approximate
            // metadata separately rather than silently moving the bind illustration.
            groundY = lowest;
        }

        float LowestPosedSole(List<Vector2> contour)
        {
            float lowest = float.PositiveInfinity;
            foreach (Vector2 point in contour)
                lowest = Mathf.Min(lowest, CurrentAction == "bind" ? point.y : GridSkin(point).y);
            return lowest;
        }

        Vector2 GridSkin(Vector2 point)
        {
            Region region = RegionAt(point.x / cell.rect[2] + cell.pivot[0], cell.pivot[1] - point.y / cell.rect[3]);
            float gx = Mathf.Clamp01((point.x - sourceBindRect.xMin) / sourceBindRect.width) * GridColumns;
            float gy = Mathf.Clamp01((point.y - sourceBindRect.yMin) / sourceBindRect.height) * GridRows;
            int x = Mathf.Min(GridColumns - 1, Mathf.FloorToInt(gx)), y = Mathf.Min(GridRows - 1, Mathf.FloorToInt(gy));
            float fx = gx - x, fy = gy - y;
            Vector2 bottomLeft = GridVertex(x, y, region), topRight = GridVertex(x + 1, y + 1, region);
            // Exactly the same diagonal and interpolation as AvatarRegisteredMesh triangles.
            return fx <= fy
                ? bottomLeft * (1 - fy) + GridVertex(x, y + 1, region) * (fy - fx) + topRight * fx
                : bottomLeft * (1 - fx) + topRight * fy + GridVertex(x + 1, y, region) * (fx - fy);
        }

        Vector2 GridVertex(int x, int y, Region region)
        {
            Vector2 point = new Vector2(sourceBindRect.xMin + sourceBindRect.width * ((float)x / GridColumns),
                sourceBindRect.yMin + sourceBindRect.height * ((float)y / GridRows));
            return RegionSkin(point, region);
        }

        Influence GetWeights(Vector2 point, Region region)
        {
            if (region.weights.TryGetValue(point, out var cached)) return cached;
            float topY = cell.pivot[1] - point.y / cell.rect[3];
            float topX = point.x / cell.rect[2] + cell.pivot[0];
            int head = indices["head"], neck = indices["neck"];
            // A registered head remains one rigid painted surface. Hair can extend below the neck.
            if ((region.id == "body" || region.patch != null) && (point.y > bind[neck].y + cell.rect[3] * .01f || InRegion(topX, topY, "hair")))
            {
                cached = new Influence { a = head, wa = 1 };
                region.weights[point] = cached;
                return cached;
            }
            if (region.id == "body" || region.patch != null)
            {
                int left = indices["foot_l"], right = indices["foot_r"];
                if (point.y < Mathf.Max(bind[left].y, bind[right].y) + cell.rect[3] * .005f)
                {
                    int foot = (point - bind[left]).sqrMagnitude < (point - bind[right]).sqrMagnitude ? left : right;
                    cached = new Influence { a = foot, wa = 1 };
                    region.weights[point] = cached;
                    return cached;
                }
            }
            float a = 0, b = 0, c = 0, d = 0;
            int ai = 0, bi = 0, ci = 0, di = 0;
            foreach (int i in region.allowedBones)
            {
                float radius = cell.joints[i].radius * cell.rect[3];
                float distance = SegmentDistance(point, bind[i], segmentEnds[i]);
                float score = Mathf.Pow(radius / (distance + radius * .18f), 4);
                if (score > a) { d = c; di = ci; c = b; ci = bi; b = a; bi = ai; a = score; ai = i; }
                else if (score > b) { d = c; di = ci; c = b; ci = bi; b = score; bi = i; }
                else if (score > c) { d = c; di = ci; c = score; ci = i; }
                else if (score > d) { d = score; di = i; }
            }
            float total = a + b + c + d;
            cached = new Influence { a = ai, b = bi, c = ci, d = di, wa = a / total, wb = b / total, wc = c / total, wd = d / total };
            WeightSumError = Mathf.Max(WeightSumError, Mathf.Abs(cached.wa + cached.wb + cached.wc + cached.wd - 1));
            region.weights[point] = cached;
            return cached;
        }

        void BuildRegions()
        {
            ReleaseRegions();
            if (cell.deformRegions == null || cell.deformRegions.Length != 4)
                throw new InvalidOperationException("Authored arm/leg ownership regions are required for this illustrated proof.");
            regions.Add(new Region { id = "body", graphic = graphic,
                allowedBones = new[] { indices["pelvis"], indices["chest"], indices["neck"], indices["head"],
                    indices["hip_l"], indices["knee_l"], indices["foot_l"], indices["hip_r"], indices["knee_r"], indices["foot_r"] },
                material = new Material(material) });
            foreach (var source in cell.deformRegions)
            {
                if (!source.id.StartsWith("arm_")) continue; // Pelvis and both legs retain continuous topology.
                bool arm = source.id.StartsWith("arm_");
                string side = source.id.EndsWith("_l") ? "_l" : "_r";
                var node = SystemUI.Node("Weighted " + source.id, rig, new Rect(0, 0, 1, 1));
                var regionGraphic = node.gameObject.AddComponent<AvatarRegisteredMesh>();
                regionGraphic.raycastTarget = false;
                var region = new Region { id = source.id, source = source, graphic = regionGraphic,
                    allowedBones = new[] { indices[(arm ? "shoulder" : "hip") + side],
                        indices[(arm ? "elbow" : "knee") + side], indices[(arm ? "hand" : "foot") + side] },
                    material = new Material(material) };
                Vector2 start = bind[region.allowedBones[0]], along = (bind[region.allowedBones[1]] - start).normalized;
                float first = float.PositiveInfinity;
                for (int i = 0; i < source.points.Length; i += 2)
                    first = Mathf.Min(first, Vector2.Dot(SourcePoint(source.points[i], source.points[i + 1]) - start, along));
                region.attachmentStart = Mathf.Max(0, first);
                regions.Add(region);
            }
            int width = Mathf.RoundToInt(cell.rect[2]), height = Mathf.RoundToInt(cell.rect[3]);
            var masks = new byte[regions.Count][];
            var underlap = new byte[width * height];
            for (int i = 0; i < masks.Length; i++) masks[i] = new byte[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int owner = RegionIndexAt((x + .5f) / width, 1 - (y + .5f) / height);
                    masks[owner][y * width + x] = 255;
                    if (owner > 0)
                    {
                        var region = regions[owner];
                        Vector2 start = bind[region.allowedBones[0]], along = (bind[region.allowedBones[1]] - start).normalized;
                        Vector2 point = SourcePoint((x + .5f) / width, 1 - (y + .5f) / height);
                        float distance = Vector2.Dot(point - start, along);
                        if (distance <= region.attachmentStart + cell.rect[3] * .020f) underlap[y * width + x] = 255;
                    }
                }
            ReassignDetachedArmIslands(masks, width, height);
            RegisterArmProfiles(masks, width, height);
            for (int i = 0; i < regions.Count; i++)
            {
                var mask = new Texture2D(width, height, TextureFormat.Alpha8, false, true)
                { name = "Authored " + regions[i].id + " ownership", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                mask.LoadRawTextureData(masks[i]);
                mask.Apply(false, true);
                regions[i].ownership = mask;
            }
            regions[0].underlap = new Texture2D(width, height, TextureFormat.Alpha8, false, true)
            { name = "Registered shoulder underlap", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            regions[0].underlap.LoadRawTextureData(underlap);
            regions[0].underlap.Apply(false, true);
            if (cell.underlaps != null)
                foreach (var patch in cell.underlaps)
                {
                    var node = SystemUI.Node("Registered cloth continuation " + patch.id, rig, new Rect(0, 0, 1, 1));
                    var surface = node.gameObject.AddComponent<AvatarRegisteredMesh>();
                    surface.raycastTarget = false;
                    var region = new Region { id = "underlap_" + patch.id, patch = patch, graphic = surface,
                        allowedBones = regions[0].allowedBones, material = new Material(material) };
                    var pixels = new byte[width * height];
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                            if (PolygonContains((x + .5f) / width, 1 - (y + .5f) / height, patch.points))
                                pixels[y * width + x] = 255;
                    region.ownership = new Texture2D(width, height, TextureFormat.Alpha8, false, true)
                    { name = "Authored cloth continuation " + patch.id, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    region.ownership.LoadRawTextureData(pixels);
                    region.ownership.Apply(false, true);
                    regions.Add(region);
                }
            Color32[] sourcePixels = atlas.GetPixels32();
            foreach (var region in regions)
            {
                region.bindRect = sourceBindRect;
                region.columns = GridColumns;
                region.rows = GridRows;
                if (region.source == null) continue;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (int i = 0; i < region.source.points.Length; i += 2)
                {
                    Vector2 point = SourcePoint(region.source.points[i], region.source.points[i + 1]);
                    minimum = Vector2.Min(minimum, point); maximum = Vector2.Max(maximum, point);
                }
                minimum = Vector2.Max(minimum - Vector2.one * 2, sourceBindRect.min);
                maximum = Vector2.Min(maximum + Vector2.one * 2, sourceBindRect.max);
                region.bindRect = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
                // Only the cropped arm needs fine geometry. A 2px spacing resolves the narrow
                // far-arm source strip without multiplying the full-body mesh's vertex count.
                region.columns = Mathf.Clamp(Mathf.CeilToInt(region.bindRect.width / 2), 1, 250);
                region.rows = Mathf.Clamp(Mathf.CeilToInt(region.bindRect.height / 2), 1, 250);
                region.visibleTriangles = new bool[region.columns * region.rows * 2];
                region.validationVertices = new Vector2[(region.columns + 1) * (region.rows + 1)];
                for (int y = 0; y < region.rows; y++)
                    for (int x = 0; x < region.columns; x++)
                        for (int triangle = 0; triangle < 2; triangle++)
                        {
                            float fx = (x + (triangle == 0 ? 1f / 3 : 2f / 3)) / region.columns;
                            float fy = (y + (triangle == 0 ? 2f / 3 : 1f / 3)) / region.rows;
                            Vector2 point = region.bindRect.min + Vector2.Scale(region.bindRect.size, new Vector2(fx, fy));
                            float u = point.x / cell.rect[2] + cell.pivot[0], v = cell.pivot[1] - point.y / cell.rect[3];
                            int localX = Mathf.Clamp(Mathf.FloorToInt(u * width), 0, width - 1);
                            int localY = Mathf.Clamp(Mathf.FloorToInt((1 - v) * height), 0, height - 1);
                            if (masks[regions.IndexOf(region)][localY * width + localX] == 0) continue;
                            int px = Mathf.Clamp(Mathf.FloorToInt(cell.rect[0] + u * cell.rect[2]), 0, atlas.width - 1);
                            int py = Mathf.Clamp(Mathf.FloorToInt(cell.rect[1] + v * cell.rect[3]), 0, atlas.height - 1);
                            region.visibleTriangles[(y * region.columns + x) * 2 + triangle] = sourcePixels[(atlas.height - 1 - py) * atlas.width + px].a >= 192;
                        }
            }
            // In both authored projections screen-right is the rear arm. Its shoulder cap must
            // disappear behind the torso. Source continuations stay behind every original pixel.
            for (int order = 0; order <= 3; order++)
                foreach (var region in regions)
                    if (DrawOrder(region) == order) region.graphic.transform.SetAsLastSibling();
        }

        void ReassignDetachedArmIslands(byte[][] masks, int width, int height)
        {
            ReassignedArmPixelCount = 0;
            Color32[] pixels = atlas.GetPixels32();
            int startX = Mathf.RoundToInt(cell.rect[0]), bottomY = atlas.height - Mathf.RoundToInt(cell.rect[1]) - height;
            var opaque = new bool[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    opaque[y * width + x] = pixels[(bottomY + y) * atlas.width + startX + x].a >= 32;
            var labels = new int[width * height];
            var queue = new int[width * height];
            for (int r = 1; r < regions.Count; r++)
            {
                Array.Clear(labels, 0, labels.Length);
                int nextLabel = 0, largest = 0, largestCount = 0;
                for (int pixel = 0; pixel < labels.Length; pixel++)
                {
                    if (!opaque[pixel] || masks[r][pixel] == 0 || labels[pixel] != 0) continue;
                    int label = ++nextLabel, head = 0, tail = 1;
                    queue[0] = pixel; labels[pixel] = label;
                    while (head < tail)
                    {
                        int current = queue[head++], x = current % width;
                        for (int side = 0; side < 4; side++)
                        {
                            if ((side == 0 && x == 0) || (side == 1 && x == width - 1)) continue;
                            int neighbor = current + (side == 0 ? -1 : side == 1 ? 1 : side == 2 ? -width : width);
                            if (neighbor < 0 || neighbor >= labels.Length || labels[neighbor] != 0 || !opaque[neighbor] || masks[r][neighbor] == 0) continue;
                            labels[neighbor] = label; queue[tail++] = neighbor;
                        }
                    }
                    if (tail > largestCount) { largestCount = tail; largest = label; }
                }
                if (nextLabel <= 1) continue;
                // Source fragments outside the connected arm stay on the body. This changes
                // ownership only: every original texel still has exactly one owner at bind.
                for (int pixel = 0; pixel < labels.Length; pixel++)
                {
                    if (labels[pixel] == 0 || labels[pixel] == largest) continue;
                    int x = pixel % width, y = pixel / width;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int px = x + dx, py = y + dy;
                            if (px < 0 || px >= width || py < 0 || py >= height) continue;
                            int neighbor = py * width + px;
                            if (masks[r][neighbor] == 0 || labels[neighbor] == largest) continue;
                            masks[r][neighbor] = 0; masks[0][neighbor] = 255; ReassignedArmPixelCount++;
                        }
                }
            }
        }

        void RegisterArmProfiles(byte[][] masks, int width, int height)
        {
            Color32[] pixels = atlas.GetPixels32();
            int startX = Mathf.RoundToInt(cell.rect[0]), startY = Mathf.RoundToInt(cell.rect[1]);
            for (int r = 1; r < regions.Count; r++)
            {
                Region region = regions[r];
                Vector2 shoulder = bind[region.allowedBones[0]], upperArm = bind[region.allowedBones[1]] - shoulder;
                region.armLength = upperArm.magnitude;
                region.armAlong = upperArm / region.armLength;
                region.armAcross = new Vector2(-region.armAlong.y, region.armAlong.x);
                region.armMin = new float[ArmProfileSamples];
                region.armMax = new float[ArmProfileSamples];
                for (int i = 0; i < ArmProfileSamples; i++)
                { region.armMin[i] = float.PositiveInfinity; region.armMax[i] = float.NegativeInfinity; }
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        if (masks[r][y * width + x] == 0) continue;
                        int index = (atlas.height - startY - height + y) * atlas.width + startX + x;
                        if (pixels[index].a < 192) continue;
                        Vector2 offset = SourcePoint((x + .5f) / width, 1 - (y + .5f) / height) - shoulder;
                        float along = Vector2.Dot(offset, region.armAlong) / region.armLength;
                        if (along < 0 || along >= 1) continue;
                        int bin = Mathf.FloorToInt(along * ArmProfileSamples);
                        float across = Vector2.Dot(offset, region.armAcross);
                        region.armMin[bin] = Mathf.Min(region.armMin[bin], across);
                        region.armMax[bin] = Mathf.Max(region.armMax[bin], across);
                    }
                // A missing arm section cannot be reconstructed by this corrective.
                bool complete = true;
                for (int i = 0; i < ArmProfileSamples; i++)
                    if (!IllustratedRigData.Finite(region.armMin[i]) || !IllustratedRigData.Finite(region.armMax[i])) complete = false;
                if (!complete) continue;
                // Smooth the sampled section boundaries before inverting their widths. The
                // previous coarse bins magnified both raster steps and centerline kinks.
                var smoothMin = new float[ArmProfileSamples];
                var smoothMax = new float[ArmProfileSamples];
                for (int i = 0; i < ArmProfileSamples; i++)
                {
                    float total = 0;
                    for (int offset = -3; offset <= 3; offset++)
                    {
                        float weight = 4 - Mathf.Abs(offset);
                        int sample = Mathf.Clamp(i + offset, 0, ArmProfileSamples - 1);
                        smoothMin[i] += region.armMin[sample] * weight;
                        smoothMax[i] += region.armMax[sample] * weight;
                        total += weight;
                    }
                    smoothMin[i] /= total; smoothMax[i] /= total;
                }
                region.armMin = smoothMin; region.armMax = smoothMax;
                region.armStartCenter = (smoothMin[0] + smoothMax[0]) * .5f;
                region.armEndCenter = (smoothMin[ArmProfileSamples - 1] + smoothMax[ArmProfileSamples - 1]) * .5f;
                float widthSum = 0;
                int firstBicep = ArmProfileSamples * 2 / 3;
                for (int i = firstBicep; i < ArmProfileSamples; i++) widthSum += smoothMax[i] - smoothMin[i];
                region.armWidth = widthSum / (ArmProfileSamples - firstBicep) * .88f;
            }
        }

        Region RegionAt(float x, float y) => regions[RegionIndexAt(x, y)];
        int RegionIndexAt(float x, float y)
        {
            for (int i = 1; i < regions.Count; i++)
                if (regions[i].source != null && PolygonContains(x, y, regions[i].source.points)) return i;
            return 0;
        }

        void ReleaseRegions()
        {
            foreach (var region in regions)
            {
                if (region.graphic != null && region.graphic != graphic)
                { region.graphic.gameObject.SetActive(false); Destroy(region.graphic.gameObject); }
                if (region.ownership != null) Destroy(region.ownership);
                if (region.underlap != null) Destroy(region.underlap);
                if (region.material != null) Destroy(region.material);
                if (region.captureRenderer != null)
                { region.captureRenderer.enabled = false; Destroy(region.captureRenderer.gameObject); }
                if (region.captureMesh != null) Destroy(region.captureMesh);
            }
            regions.Clear();
        }

        void BuildMask()
        {
            if (generatedMask != null) { Destroy(generatedMask); generatedMask = null; }
            var supplied = string.IsNullOrEmpty(entry.maskResource) ? null : Resources.Load<Texture2D>(entry.maskResource);
            if (supplied != null)
            {
                material.SetTexture("_MaskTex", supplied);
                material.SetFloat("_AtlasMask", 1);
                return;
            }
            const int width = 256, height = 384;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x + .5f) / width, v = 1 - (y + .5f) / height;
                    pixels[y * width + x] = new Color32(InRegion(u, v, "skin") ? (byte)255 : (byte)0,
                        InRegion(u, v, "hair") ? (byte)255 : (byte)0, 0, 255);
                }
            generatedMask = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            { name = "Registered skin and hair regions", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            generatedMask.SetPixels32(pixels);
            generatedMask.Apply(false, true);
            material.SetTexture("_MaskTex", generatedMask);
            material.SetFloat("_AtlasMask", 0);
        }

        bool InRegion(float x, float y, string region)
        {
            if (cell.maskPolygons == null) return false;
            foreach (var polygon in cell.maskPolygons)
            {
                if (polygon.region != region) continue;
                if (PolygonContains(x, y, polygon.points)) return true;
            }
            return false;
        }

        static bool PolygonContains(float x, float y, float[] points)
        {
            bool inside = false;
            int count = points.Length / 2;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                float ix = points[i * 2], iy = points[i * 2 + 1], jx = points[j * 2], jy = points[j * 2 + 1];
                if ((iy > y) != (jy > y) && x < (jx - ix) * (y - iy) / (jy - iy) + ix) inside = !inside;
            }
            return inside;
        }

        public bool TryValidate(out string reason)
        {
            if (!IsLoaded || cell == null) { reason = LastError ?? "Illustrated proof not loaded."; return false; }
            UpdateSurface();
            foreach (var region in regions)
                if (!region.graphic.CheckMesh(out var bounds, out reason)) return false;
            if (!IllustratedRigData.Finite(WeightSumError) || WeightSumError > .0001f)
            { reason = "Illustrated skin weights are not normalized."; return false; }
            if (!IllustratedRigData.Finite(GroundError) || GroundError > .05f)
            { reason = "Registered sole anchors left the source ground."; return false; }
            if (CurrentAction == "bind" && MaxVertexDisplacement != 0)
            { reason = "Bind pose changed registered artwork geometry."; return false; }
            CheckArmTriangles();
            if (FoldedArmTriangleCount != 0)
            { reason = FoldedArmTriangleCount + " visible arm triangles inverted during deformation."; return false; }
            reason = "Continuous mesh, finite vertices, normalized weights, uninverted arm triangles and registered ground anchors valid; optical fit and motion require visual review.";
            return true;
        }

        void CheckArmTriangles()
        {
            FoldedArmTriangleCount = 0;
            foreach (var region in regions)
            {
                if (region.source == null) continue;
                int columns = region.columns, rows = region.rows, stride = columns + 1;
                for (int y = 0; y <= rows; y++)
                    for (int x = 0; x <= columns; x++)
                    {
                        Vector2 point = region.bindRect.min + Vector2.Scale(region.bindRect.size, new Vector2((float)x / columns, (float)y / rows));
                        region.validationVertices[y * stride + x] = CurrentAction == "bind" ? point : RegionSkin(point, region);
                    }
                for (int y = 0; y < rows; y++)
                    for (int x = 0; x < columns; x++)
                    {
                        int a = y * stride + x, b = a + stride, triangle = (y * columns + x) * 2;
                        if (region.visibleTriangles[triangle] && SignedArea(region.validationVertices[a], region.validationVertices[b], region.validationVertices[b + 1]) >= 0)
                            FoldedArmTriangleCount++;
                        if (region.visibleTriangles[triangle + 1] && SignedArea(region.validationVertices[b + 1], region.validationVertices[a + 1], region.validationVertices[a]) >= 0)
                            FoldedArmTriangleCount++;
                    }
            }
        }

        static float SignedArea(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

        /// <summary>Exports only this surface, without the menu, using the same mesh and shader.</summary>
        public void CapturePng(string path)
        {
            if (!IsLoaded) throw new InvalidOperationException("Illustrated proof not loaded.");
            UpdateSurface();
            PrepareCaptureMesh();
            int width = Mathf.RoundToInt(cell.rect[2] * 1.4f), height = Mathf.RoundToInt(cell.rect[3] * 1.08f);
            var render = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                var frame = graphic.BindRect;
                captureCamera.transform.localPosition = new Vector3(frame.center.x, frame.center.y, -10);
                captureCamera.orthographicSize = frame.height * .54f;
                captureCamera.aspect = width / (float)height;
                captureCamera.targetTexture = render;
                captureCamera.Render();
                RenderTexture.active = render;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                captureCamera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                if (texture != null) Destroy(texture);
            }
        }

        void PrepareCaptureMesh()
        {
            if (captureScene == null)
            {
                captureScene = new GameObject("Illustrated proof capture scene").transform;
                captureScene.position = new Vector3(10000 + GetInstanceID() % 100 * 2000, -10000, 0);
                var cameraObject = new GameObject("Illustrated proof capture camera");
                cameraObject.transform.SetParent(captureScene, false);
                captureCamera = cameraObject.AddComponent<Camera>();
                captureCamera.enabled = false;
                captureCamera.orthographic = true;
                captureCamera.clearFlags = CameraClearFlags.SolidColor;
                captureCamera.backgroundColor = new Color(.07f, .09f, .12f, 1);
                captureCamera.cullingMask = 1 << 31;
                captureCamera.nearClipPlane = .1f;
                captureCamera.farClipPlane = 20;
                captureCamera.allowHDR = false;
                captureCamera.allowMSAA = false;
            }
            for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
            {
                var region = regions[regionIndex];
                int columns = region.columns, rows = region.rows;
                if (region.captureMesh == null)
                {
                var surface = new GameObject("Registered " + region.id, typeof(MeshFilter), typeof(MeshRenderer));
                surface.transform.SetParent(captureScene, false);
                surface.layer = 31;
                region.captureRenderer = surface.GetComponent<MeshRenderer>();
                region.captureRenderer.sortingOrder = DrawOrder(region);
                region.captureRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                region.captureRenderer.receiveShadows = false;
                region.captureMesh = new Mesh { name = "Illustrated " + region.id + " capture mesh" };
                surface.GetComponent<MeshFilter>().sharedMesh = region.captureMesh;
                int count = (columns + 1) * (rows + 1);
                region.captureVertices = new Vector3[count];
                region.captureUVs = new Vector2[count];
                var colors = new Color32[count];
                for (int i = 0; i < count; i++) colors[i] = new Color32(255, 255, 255, 255);
                region.captureMesh.vertices = region.captureVertices;
                region.captureMesh.colors32 = colors;
                var triangles = new int[columns * rows * 6];
                int cursor = 0, stride = columns + 1;
                for (int y = 0; y < rows; y++)
                    for (int x = 0; x < columns; x++)
                    {
                        int bottomLeft = y * stride + x, topLeft = bottomLeft + stride;
                        triangles[cursor++] = bottomLeft; triangles[cursor++] = topLeft; triangles[cursor++] = topLeft + 1;
                        triangles[cursor++] = topLeft + 1; triangles[cursor++] = bottomLeft + 1; triangles[cursor++] = bottomLeft;
                    }
                region.captureMesh.triangles = triangles;
                }
            Rect uv = region.graphic.uvRect;
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    float fx = (float)x / columns, fy = (float)y / rows;
                    int index = y * (columns + 1) + x;
                    Vector2 bindPoint = new Vector2(region.bindRect.xMin + region.bindRect.width * fx,
                        region.bindRect.yMin + region.bindRect.height * fy);
                    region.captureVertices[index] = PoseVertex(bindPoint, region);
                    region.captureUVs[index] = new Vector2(uv.x + uv.width * fx, uv.y + uv.height * fy);
                }
            region.captureMesh.vertices = region.captureVertices;
            region.captureMesh.uv = region.captureUVs;
            region.captureMesh.RecalculateBounds();
            region.captureRenderer.sharedMaterial = region.material;
            }
        }

        void OnDestroy()
        {
            if (generatedMask != null) Destroy(generatedMask);
            if (material != null) Destroy(material);
            ReleaseRegions();
            if (captureScene != null) Destroy(captureScene.gameObject);
            if (stage != null) Destroy(stage.gameObject);
        }
        Vector2 SourcePoint(float x, float y) => new Vector2((x - cell.pivot[0]) * cell.rect[2], (cell.pivot[1] - y) * cell.rect[3]);
        void Turn(string bone, float degrees) { if (indices.TryGetValue(bone, out int index)) localAngles[index] = degrees; }
        static Color C(float[] values) => new Color(values[0], values[1], values[2], 1);
        static int DrawOrder(Region region) => region.patch != null ? 0 : region.id == "arm_r" ? 1 : region.id == "body" ? 2 : 3;
        static Vector2 Rotate(Vector2 point, float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad, sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }
        static float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            float t = delta.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude) : 0;
            return Vector2.Distance(point, start + delta * t);
        }
        static string MainChild(string id)
        {
            switch (id)
            {
                case "pelvis": return "chest";
                case "chest": return "neck";
                case "neck": return "head";
                case "shoulder_l": return "elbow_l";
                case "elbow_l": return "hand_l";
                case "shoulder_r": return "elbow_r";
                case "elbow_r": return "hand_r";
                case "hip_l": return "knee_l";
                case "knee_l": return "foot_l";
                case "hip_r": return "knee_r";
                case "knee_r": return "foot_r";
                default: return null;
            }
        }
    }
}
