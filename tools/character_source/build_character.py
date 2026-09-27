"""Build the editable SoloGym character and registered runtime data in Blender.

Run with the pinned portable Blender, not the system Python interpreter:
  blender --background --python tools/character_source/build_character.py
All geometry is derived from one source body. No generated limb images are used.
"""
import hashlib
import json
import math
import sys
from collections import Counter
from pathlib import Path

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
from prepare_base import load_base

SOURCE = ROOT / "design/character-source"
OUTPUT = ROOT / "app/Assets/SoloGym/Resources/AvatarSource3D"
PROOF = ROOT / "artifacts/visual/CharacterSource3D"
for directory in (SOURCE, OUTPUT, PROOF):
    directory.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
base = load_base(SOURCE / "vendor/makehuman", muscle=0.25)
bone_names = [b["name"] for b in base["bones"]]
bone_lookup = {name: i for i, name in enumerate(bone_names)}


def v3(v):
    return Vector(v)


def blender(v):
    return Vector((v[0], -v[2], v[1]))


def unity(v):
    return [v[0], v[2], -v[1]]


def smooth(a, b, value):
    t = max(0, min(1, (value - a) / (b - a)))
    return t * t * (3 - 2 * t)


def style(v):
    """One continuous, mild proportion adjustment; also applied to joint helpers."""
    x, y, z = v
    head = smooth(1.50, 1.59, y)
    x *= 1 + .07 * head
    y += (y - 1.59) * .055 * head
    z = .05 + (z - .05) * (1 + .045 * head)
    return Vector((x, y, z))


# Lower the source A-pose once, baking precisely the same transforms into the
# source vertices, shape target, eye helpers and joint locations.
original_bones = {b["name"]: style(b["position"]) for b in base["bones"]}
rest_transforms = {}
for bone in base["bones"]:
    parent = bone["parent"]
    if isinstance(parent, int):
        parent = bone_names[parent] if parent >= 0 else None
    inherited = rest_transforms.get(parent, Matrix.Identity(4))
    delta = Matrix.Identity(4)
    if bone["name"].startswith("upper_arm."):
        angle = math.radians(-24 if bone["name"].endswith(".L") else 24)
        origin = original_bones[bone["name"]]
        delta = Matrix.Translation(origin) @ Matrix.Rotation(angle, 4, "Z") @ Matrix.Translation(-origin)
    rest_transforms[bone["name"]] = inherited @ delta


def transform(position, influences):
    p = style(position)
    return sum((rest_transforms[name] @ p * weight for name, weight in influences), Vector())


weights = base["weights"]
positions = [transform(p, w) for p, w in zip(base["positions"], weights)]
high_source = base["shapes"]["muscle100"]
if isinstance(high_source, dict):
    high_source = high_source["positions"]
strong = [transform(p, w) for p, w in zip(high_source, weights)]
bone_positions = {name: rest_transforms[name] @ original_bones[name] for name in bone_names}

MATERIALS = [
    ("skin", "skin", (.63, .43, .32, 1)),
    ("training_fabric", "cloth", (.045, .080, .115, 1)),
    ("shorts", "cloth", (.075, .092, .13, 1)),
    ("cyan_trim", "trim", (.10, .66, .74, 1)),
    ("hair_black", "hair", (.025, .032, .055, 1)),
    ("hair_highlight", "hair", (.055, .075, .115, 1)),
    ("sclera", "eye", (.84, .85, .83, 1)),
    ("iris", "eye", (.10, .19, .21, 1)),
    ("pupil", "eye", (.008, .010, .018, 1)),
    ("lips", "face", (.34, .16, .13, 1)),
    ("boots", "cloth", (.035, .049, .069, 1)),
    ("glove", "cloth", (.055, .25, .34, 1)),
]
materials = []
for name, region, color in MATERIALS:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = .72
    materials.append(mat)

arm = bpy.data.armatures.new("SoloGym shared humanoid v1")
rig = bpy.data.objects.new("SoloGym source rig", arm)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
for source_bone in base["bones"]:
    name = source_bone["name"]
    bone = arm.edit_bones.new(name)
    bone.head = blender(bone_positions[name])
    tail = rest_transforms[name] @ style(source_bone["tail"])
    bone.tail = blender(tail)
    if (bone.tail - bone.head).length < .015:
        bone.tail = bone.head + Vector((0, 0, .04))
    parent = source_bone["parent"]
    if isinstance(parent, int):
        parent = bone_names[parent] if parent >= 0 else None
    if parent:
        bone.parent = arm.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")
rig.select_set(False)
objects = []


def create_mesh(name, verts, faces, influence, mat=0, slot="body", variant="", hide="", target=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([blender(p) for p in verts], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(materials[mat])
    for poly in mesh.polygons:
        poly.use_smooth = True
    obj["slot"], obj["variant"], obj["hideWithSlot"], obj["material_index"] = slot, variant, hide, mat
    for bone_name in bone_names:
        obj.vertex_groups.new(name=bone_name)
    if isinstance(influence, str):
        influence = [[(influence, 1.)] for _ in verts]
    for index, items in enumerate(influence):
        for bone_name, value in items:
            if value > 0:
                obj.vertex_groups[bone_name].add([index], value, "REPLACE")
    if target is not None:
        obj.shape_key_add(name="Basis")
        key = obj.shape_key_add(name="Athletic")
        for vertex, coordinate in zip(key.data, target):
            vertex.co = blender(coordinate)
        key.value = .5
    modifier = obj.modifiers.new("Shared skinning", "ARMATURE")
    modifier.object = rig
    obj.parent = rig
    objects.append(obj)
    return obj


body_faces = base["faces"]
# Use normals of the complete body so seams between coverage patches stay smooth.
normal_mesh = bpy.data.meshes.new("body normals helper")
normal_mesh.from_pydata([blender(p) for p in positions], [], body_faces)
normal_mesh.update()
normals = [Vector(unity(v.normal)) for v in normal_mesh.vertices]
strong_normal_mesh = bpy.data.meshes.new("strong normals helper")
strong_normal_mesh.from_pydata([blender(p) for p in strong], [], body_faces)
strong_normal_mesh.update()
strong_normals = [Vector(unity(v.normal)) for v in strong_normal_mesh.vertices]
body_surface = BVHTree.FromPolygons(positions[:13380], body_faces)
strong_surface = BVHTree.FromPolygons(strong[:13380], body_faces)


def face_center(face):
    return sum((positions[i] for i in face), Vector()) / len(face)


def category(face):
    x, y, z = face_center(face)
    influences = Counter()
    for i in face:
        for name, value in weights[i]:
            influences[name] += value / len(face)
    if y < .17:
        return "boots"
    if .67 < y < 1.005 and influences["pelvis"] + influences["spine"] + influences["thigh.L"] + influences["thigh.R"] > .70:
        return "shorts"
    if influences["hand.L"] + influences["hand.R"] > .80:
        return "hands"
    torso = sum(influences[b] for b in ("pelvis", "spine", "chest", "neck", "clavicle.L", "clavicle.R"))
    neckline = 1.42 + .11 * min(1, abs(x) / .13)
    if .995 <= y < neckline and torso > .74:
        return "top"
    return "body"


groups = {key: [] for key in ("body", "top", "hands", "shorts", "boots")}
for face in body_faces:
    groups[category(face)].append(face)


def patch(name, faces, mat, offset=0, slot="body", hide=""):
    indices = sorted({i for face in faces for i in face})
    index = {old: new for new, old in enumerate(indices)}
    obj = create_mesh(name, [positions[i] + normals[i] * offset for i in indices],
                      [[index[i] for i in face] for face in faces], [weights[i] for i in indices],
                      mat, slot, hide=hide,
                      target=[strong[i] + strong_normals[i] * offset for i in indices])
    obj["original_indices"] = indices
    if offset and name in ("training_top", "training_shorts", "training_boots"):
        # Smooth the actual garment edge into a fitted curve instead of following
        # the staircase produced by selecting vertices from a body grid.
        edges = Counter(tuple(sorted((face[i], face[(i+1)%len(face)]))) for face in faces for i in range(len(face)))
        neighbors = {}; all_neighbors={i:set() for i in range(len(indices))}
        for a,b in edges:
            all_neighbors[index[a]].add(index[b]);all_neighbors[index[b]].add(index[a])
        for (a,b), count in edges.items():
            if count == 1:
                neighbors.setdefault(index[a], []).append(index[b])
                neighbors.setdefault(index[b], []).append(index[a])
        for key, surface in (("Basis",body_surface),("Athletic",strong_surface)):
            vertices = obj.data.shape_keys.key_blocks[key].data
            points = [Vector(unity(v.co)) for v in vertices]
            for _ in range(8):
                next_points = points[:]
                for i, adjacent in neighbors.items():
                    average = sum((points[j] for j in adjacent), Vector()) / len(adjacent)
                    location, normal, _, _ = surface.find_nearest(points[i].lerp(average,.48))
                    next_points[i] = location + normal * offset
                for i in set(j for i in neighbors for j in all_neighbors[i]) - set(neighbors):
                    adjacent=all_neighbors[i]
                    average=sum((points[j] for j in adjacent),Vector())/len(adjacent)
                    location,normal,_,_=surface.find_nearest(points[i].lerp(average,.18))
                    next_points[i]=location+normal*offset
                points = next_points
            for v, p in zip(vertices,points):v.co=blender(p)
        for vertex,basis in zip(obj.data.vertices,obj.data.shape_keys.key_blocks["Basis"].data):vertex.co=basis.co
        obj.data.update()
    return obj


# Keep a two-face border of skin below the garment openings. The interior is
# masked, while a smoothed garment boundary cannot expose an empty body seam.
top_vertices={i for face in groups["top"] for i in face}
outside_vertices={i for face in body_faces if category(face)!="top" for i in face}
border=top_vertices & outside_vertices
border_faces=[]; interior=[]
for face in groups["top"]:
    (border_faces if any(i in border for i in face) else interior).append(face)
border={i for face in border_faces for i in face}
interior2=[]
for face in interior:
    (border_faces if any(i in border for i in face) else interior2).append(face)
patch("body", groups["body"]+border_faces, 0)
patch("covered_torso", interior2, 0, hide="top")
patch("bare_hands", groups["hands"], 0, hide="hands")
# Retain skin just under permanent garment openings as well.
for label in ("shorts", "boots"):
    garment_vertices={i for face in groups[label] for i in face}
    other_vertices={i for face in body_faces if category(face)!=label for i in face}
    seam=garment_vertices & other_vertices
    border=[face for face in groups[label] if any(i in seam for i in face)]
    patch(label+"_opening_skin",border,0)
top_mesh=patch("training_top", groups["top"], 1, .0045, "top")
shorts_mesh=patch("training_shorts", groups["shorts"], 2, .014)

# A real horizontal waistband bridges the body's rear anatomical crease.
band_positions=[];band_strong=[];band_faces=[];band_weights=[];sectors=64
for y in (.972, .997, 1.026):
    for j in range(sectors):
        phi=math.tau*j/sectors
        direction=Vector((math.cos(phi),0,math.sin(phi)))
        origin=Vector((0,y,.025))
        for surface,points in ((body_surface,band_positions),(strong_surface,band_strong)):
            point,normal,face,_=surface.ray_cast(origin,direction,.5)
            points.append(point+normal*.015)
        nearest=min(body_faces[face],key=lambda i:(positions[i]-band_positions[-1]).length_squared)
        band_weights.append(weights[nearest])
for ring in range(2):
    for j in range(sectors):band_faces.append((ring*sectors+j,ring*sectors+(j+1)%sectors,(ring+1)*sectors+(j+1)%sectors,(ring+1)*sectors+j))
create_mesh("shorts_waistband",band_positions,band_faces,band_weights,2,target=band_strong)

# Shoes have a continuous upper and sole, not individual toe-shaped shells.
for side in ("L","R"):
    ankle=bone_positions["foot."+side]
    ring_specs=[(.002,.067,.145,.075),(.025,.067,.145,.075),(.050,.062,.137,.070),
                (.092,.053,.101,.043),(.145,.042,.057,.023),(.205,.041,.048,.021)]
    verts=[]; targets=[]; faces=[]; influences=[]; sectors=32
    for y,rx,rz,cz in ring_specs:
        for j in range(sectors):
            phi=math.tau*j/sectors
            direction=Vector((math.cos(phi),0,math.sin(phi)))
            fallback=Vector((ankle.x+rx*math.cos(phi),y,cz+rz*math.sin(phi)))
            for surface,points in ((body_surface,verts),(strong_surface,targets)):
                hit=None
                if y>.12:hit,_,_,_=surface.ray_cast(Vector((ankle.x,y,.02)),direction,.2)
                points.append(hit+direction*.009 if hit is not None else fallback)
            shin_weight=smooth(.095,.185,y)*.7
            influences.append([("foot."+side,1-shin_weight),("shin."+side,shin_weight)])
    for ring in range(len(ring_specs)-1):
        for j in range(sectors):
            faces.append((ring*sectors+j,ring*sectors+(j+1)%sectors,
                          (ring+1)*sectors+(j+1)%sectors,(ring+1)*sectors+j))
    faces.append(tuple(reversed(range(sectors))))
    create_mesh("training_boots_"+side,verts,faces,influences,10,target=targets)


def tube(name, points, radii, material, influence="head", slot="body", variant="", target=None):
    """Small real geometry for seams, eyebrows and sculpted hair strands."""
    verts, faces = [], []
    sides = 8
    for i, point in enumerate(points):
        tangent = (points[min(i+1, len(points)-1)] - points[max(i-1, 0)]).normalized()
        normal = tangent.cross(Vector((0, 0, 1)))
        if normal.length < .01:
            normal = tangent.cross(Vector((0, 1, 0)))
        normal.normalize()
        binormal = tangent.cross(normal).normalized()
        for j in range(sides):
            angle = j * math.tau / sides
            verts.append(point + radii[i] * (normal * math.cos(angle) + binormal * math.sin(angle)))
    for i in range(len(points)-1):
        for j in range(sides):
            faces.append((i*sides+j, i*sides+(j+1)%sides, (i+1)*sides+(j+1)%sides, (i+1)*sides+j))
    faces += [tuple(reversed(range(sides))), tuple((len(points)-1)*sides+j for j in range(sides))]
    if not isinstance(influence, str) and len(influence) == len(points):
        influence = [entry for entry in influence for _ in range(sides)]
    target_vertices = None
    if target is not None:
        target_vertices = [vertex + target[i//sides] - points[i//sides] for i, vertex in enumerate(verts)]
    return create_mesh(name, verts, faces, influence, material, slot, variant, target=target_vertices)


def add_seams(obj, label, mat, slot, radius):
    faces=[list(face.vertices) for face in obj.data.polygons]
    edges = Counter(tuple(sorted((face[i], face[(i+1)%len(face)]))) for face in faces for i in range(len(face)))
    # A seam uses the exact body edge and its weights/shape deltas; it is never
    # positioned independently in the studio renderer.
    remaining = {edge for edge, count in edges.items() if count == 1}
    n=0
    while remaining:
        a,b=min(remaining);remaining.remove((a,b));loop=[a,b]
        while loop[-1]!=loop[0]:
            matches=[edge for edge in remaining if loop[-1] in edge]
            if not matches:break
            edge=min(matches);remaining.remove(edge)
            loop.append(edge[1] if edge[0]==loop[-1] else edge[0])
        tube(f"{label}_{n:03}", [Vector(unity(obj.data.vertices[i].co)) for i in loop],
             [radius]*len(loop), mat, [weights[obj["original_indices"][i]] for i in loop], slot,
             target=[Vector(unity(obj.data.shape_keys.key_blocks["Athletic"].data[i].co)) for i in loop])
        n+=1


add_seams(top_mesh, "top_binding", 3, "top", .0012)
add_seams(shorts_mesh, "shorts_binding", 1, "body", .0012)

# Hair is a fitted scalp plus authored tapered locks. The two variants share
# one skull, material family and attachment bone.
for variant in ("spiky", "swept"):
    verts=[]; faces=[]; sectors=64; rings=18
    center=Vector((0,1.657,.045))
    for ring in range(rings+1):
        for sector in range(sectors):
            phi=math.tau*sector/sectors
            front=(math.sin(phi)+1)/2
            # A low side/back hairline and a small asymmetrical forehead fringe.
            limit=1.96-.65*front
            limit+=.095*front*math.sin(phi*3+.5)
            theta=max(.001,ring/rings*limit)
            direction=Vector((math.sin(theta)*math.cos(phi), math.cos(theta), math.sin(theta)*math.sin(phi)))
            hit,normal,_,_=body_surface.ray_cast(center,direction,.30)
            if hit is None:hit=center+direction*.09
            crown=(1-ring/rings)**1.6
            volume=.005+.003*crown
            p=hit+direction*volume
            if variant=="spiky":
                p.y+=.004*crown*(.5+.5*math.sin(phi*5+theta*7))
                p.x-=.004*crown
            else:
                p.x+=.023*crown
                p.y+=.018*crown
            verts.append(p)
    for ring in range(rings):
        for sector in range(sectors):
            faces.append((ring*sectors+sector,ring*sectors+(sector+1)%sectors,
                          (ring+1)*sectors+(sector+1)%sectors,(ring+1)*sectors+sector))
    create_mesh("hair_cap_"+variant,verts,faces,"head",4,"hair",variant)
    # Attached tapered locks give the hair a deliberate silhouette instead of
    # treating a raised spherical scalp as a finished hairstyle.
    for row in range(2):
        for col in range(7):
            x=(col-3)*.019
            root=Vector((x,1.714-.009*abs(col-3),.015+row*.043))
            if variant=="spiky":
                tip=root+Vector((-.020+x*.15,.068-.009*abs(col-3),.025))
                middle=root.lerp(tip,.53)+Vector((0,.009,0))
            else:
                tip=root+Vector((.045,.044-.006*abs(col-3),.058))
                middle=root.lerp(tip,.47)+Vector((-.008,.028,0))
            tube(f"hair_lock_{variant}_{row}_{col}",[root,middle,tip],[.019,.016,.0007],
                 4,"head","hair",variant)
    # Low raised strand ridges follow the same fitted scalp surface.
    for stripe in range(7):
        sector=int((stripe+2)*sectors/12)%sectors
        points=[verts[ring*sectors+sector]+Vector((0,.0015,0)) for ring in range(3,16)]
        tube(f"hair_ridge_{variant}_{stripe}",points,[.0004]+[.0008]*11+[.0003],5,"head","hair",variant)

# Real eyeball meshes from the same source helpers, with geometric irises.
eyes = base["eyes"]
eye_positions = [transform(p, [("head",1.)]) for p in eyes["positions"]]
create_mesh("eyeballs", eye_positions, eyes["faces"], "head", 6)
for sign in (-1,1):
    half = [p for p in eye_positions if p.x*sign > 0]
    center = sum(half, Vector()) / len(half)
    radius = max((p-center).length for p in half)
    front = max(p.z for p in half)
    for label, r, z, material in (("iris", .0068, front+.0007,7), ("pupil", .0031,front+.0010,8)):
        verts=[Vector((center.x, center.y,z))]
        for j in range(24):
            a=j*math.tau/24
            verts.append(Vector((center.x+math.cos(a)*r,center.y+math.sin(a)*r,z-.001)))
        faces=[(0,j+1,(j+1)%24+1) for j in range(24)]
        create_mesh(f"{label}_{sign}",verts,faces,"head",material)
    # Slightly angular, restrained brows sit on the actual facial surface.
    points=[]
    for dx,dy in ((-.020,.023),(-.010,.026),(.006,.024),(.019,.019)):
        x=center.x+dx*sign; y=center.y+dy
        candidates=[p for p in positions[:13380] if abs(p.x-x)<.01 and abs(p.y-y)<.012]
        z=max((p.z for p in candidates),default=front)+.0015
        points.append(Vector((x,y,z)))
    tube(f"eyebrow_{sign}", points, [.0013,.0031,.003,.0008],4)

# Boxing gloves are fitted volumes around each hand; the covered source hands
# are switched off with the same slot. Wrists retain their skin/forearm weights.
for side in ("L","R"):
    name="hand."+side
    hand_points=[p for p,w in zip(positions,weights) if any(n==name and value>.88 for n,value in w)]
    center=sum(hand_points,Vector())/len(hand_points)
    wrist=bone_positions[name]
    axis=(center-wrist).normalized()
    across=axis.cross(Vector((0,0,1))).normalized()
    forward=across.cross(axis).normalized()
    hand_length=.109
    center=wrist+axis*.063
    vertices=[]; faces=[]
    rings=12; sections=20
    for ring in range(rings+1):
        lat=math.pi*ring/rings
        for j in range(sections):
            a=math.tau*j/sections
            vertices.append(center+axis*math.cos(lat)*.067+
                            (across*math.cos(a)*.053+forward*math.sin(a)*.061)*math.sin(lat))
    for ring in range(rings):
        for j in range(sections):
            faces.append((ring*sections+j, ring*sections+(j+1)%sections,
                          (ring+1)*sections+(j+1)%sections,(ring+1)*sections+j))
    create_mesh("boxing_glove_"+side,vertices,faces,name,11,"hands")
    tube("glove_cuff_"+side,[wrist-axis*.013,wrist+axis*.022],[.038,.043],3,name,"hands")
    inward=Vector((-1 if side=="L" else 1,0,0))
    thumb=wrist+axis*.043+inward*.045+forward*.020
    tube("glove_thumb_"+side,[thumb-axis*.025,thumb,thumb+axis*.034],[.018,.026,.012],11,name,"hands")


def merge_by_settings():
    """Batch decorative seams/locks into a small number of skinned meshes."""
    groups={}
    for obj in objects:
        key=tuple(obj.get(k,"") for k in ("slot","variant","hideWithSlot","material_index"))
        groups.setdefault(key,[]).append(obj)
    # Join is unnecessary for authoring. Runtime export batches meshes below,
    # retaining editable seam and lock objects in the source .blend.
    return groups


def export():
    records=[]
    for key, group in merge_by_settings().items():
        slot, variant, hide, material_index=key
        record={"id":group[0].name,"slot":slot,"variant":variant,"hideWithSlot":hide,
                "vertices":[],"normals":[],"boneIndices":[],"boneWeights":[],
                "shapeDelta":[],"shapeNormalDelta":[],"sourceVertexIndices":[],
                "fitMode":"body-derived" if all(obj.get("original_indices") for obj in group) else "authored-weighted",
                "submeshes":[{"material":material_index,"triangles":[]}]}
        for obj in group:
            mesh=obj.data
            mesh.calc_loop_triangles()
            start=len(record["vertices"])//3
            target=mesh.shape_keys.key_blocks.get("Athletic") if mesh.shape_keys else None
            original=obj.get("original_indices")
            for i,vertex in enumerate(mesh.vertices):
                record["sourceVertexIndices"].append(original[i] if original else -1)
                record["vertices"].extend(unity(vertex.co))
                normal=Vector(unity(vertex.normal))
                if original and material_index==0:
                    normal=normals[original[i]]
                record["normals"].extend(normal)
                record["shapeDelta"].extend(unity(target.data[i].co-vertex.co) if target else [0,0,0])
                record["shapeNormalDelta"].extend(strong_normals[original[i]]-normals[original[i]] if original and material_index==0 else [0,0,0])
                influences=sorted([(bone_lookup[obj.vertex_groups[g.group].name],g.weight) for g in vertex.groups],key=lambda x:-x[1])[:4]
                total=sum(w for _,w in influences)
                if total <= 0: raise ValueError("Unweighted vertex: "+obj.name)
                influences=[(b,w/total) for b,w in influences]
                influences += [(0,0)]*(4-len(influences))
                record["boneIndices"].extend(b for b,_ in influences)
                record["boneWeights"].extend(w for _,w in influences)
            for triangle in mesh.loop_triangles:
                record["submeshes"][0]["triangles"].extend(start+i for i in reversed(triangle.vertices))
        for field in ("vertices","normals","boneWeights","shapeDelta","shapeNormalDelta"):
            record[field]=[round(float(v),6) for v in record[field]]
        records.append(record)
    bones=[]
    for bone in base["bones"]:
        name,parent=bone["name"],bone["parent"]
        if isinstance(parent,int):parent=bone_names[parent] if parent>=0 else None
        local=bone_positions[name]-bone_positions[parent] if parent else bone_positions[name]
        bones.append({"name":name,"parent":bone_lookup[parent] if parent else -1,
                      "position":[round(v,6) for v in local],"rotation":[0,0,0,1]})
    revision=hashlib.sha256(Path(__file__).read_bytes()+Path(__file__).with_name("prepare_base.py").read_bytes()+
                            (SOURCE/"vendor/makehuman/SOURCE.json").read_bytes()+bpy.app.version_string.encode()).hexdigest()
    payload={"schema":"sologym.character-source3d.v1","sourceId":"sologym_athlete_master_v1",
             "sourceRevision":revision,"skeletonId":"sologym_humanoid_22_v1","bones":bones,
             "materials":[{"id":name,"region":region,"color":color} for name,region,color in MATERIALS],"meshes":records}
    (OUTPUT/"Character.json").write_text(json.dumps(payload,separators=(",",":"))+"\n")
    (SOURCE/"export-manifest.json").write_text(json.dumps({
        "schema_version":1,"source_revision":revision,"blender_version":bpy.app.version_string,
        "source_file":"design/character-source/SoloGymAthlete-v1.blend",
        "runtime_file":"app/Assets/SoloGym/Resources/AvatarSource3D/Character.json",
        "mesh_count":len(records),"bone_count":len(bones),"vertices":sum(len(m["vertices"])//3 for m in records),
        "triangles":sum(len(s["triangles"])//3 for m in records for s in m["submeshes"]),
        "shape_control":{"name":"athletic","min_target":.25,"max_target":1.0},
        "coordinates":"Unity X left, Y up, Z front; metres; ground Y=0",
        "status":"engineering_proof_pending_visual_review"},indent=2)+"\n")


# The source root and runtime root share the actual lowest point of the shoes.
sole=min(vertex.co.z for obj in objects if obj.name.startswith("training_boots") for vertex in obj.data.vertices)
ground_shift=Vector((0,0,-sole))
for obj in objects:
    if obj.data.shape_keys:
        for key in obj.data.shape_keys.key_blocks:
            for vertex in key.data:vertex.co+=ground_shift
    for vertex in obj.data.vertices:vertex.co+=ground_shift
for name in bone_positions:bone_positions[name].y-=sole
bpy.context.view_layer.objects.active=rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
for bone in arm.edit_bones:
    bone.head+=ground_shift;bone.tail+=ground_shift
bpy.ops.object.mode_set(mode="OBJECT")
rig.select_set(False)
export()

# Source camera/lights are reproducible authoring views. Runtime captures remain
# the acceptance evidence for Unity materials and actual customization.
scene=bpy.context.scene
scene.render.engine="CYCLES"
scene.cycles.samples=16
scene.cycles.use_denoising=True
scene.render.resolution_x=720
scene.render.resolution_y=1080
scene.render.resolution_percentage=100
scene.world.color=(.16,.18,.22)
scene.view_settings.view_transform="AgX"
camera_data=bpy.data.cameras.new("StudioCamera")
camera=bpy.data.objects.new("StudioCamera",camera_data)
bpy.context.collection.objects.link(camera)
camera.location=blender(Vector((0,1.00,4)))
target=blender(Vector((0,.94,0)))
camera.rotation_euler=(target-camera.location).to_track_quat("-Z","Y").to_euler()
camera_data.type="ORTHO";camera_data.ortho_scale=2.10
scene.camera=camera
for name,position,power,size in (("Key",(-2,3,3),400,3),("Fill",(2,2,2),200,3),("Rim",(0,2,-2),450,2)):
    data=bpy.data.lights.new(name,"AREA");data.energy=power;data.shape="DISK";data.size=size
    light=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(light)
    light.location=blender(position);light.rotation_euler=(blender((0,1,0))-light.location).to_track_quat("-Z","Y").to_euler()
for obj in objects:
    obj.hide_render=bool(obj.get("hideWithSlot")) or obj.get("variant")=="swept"
    obj.hide_set(obj.hide_render)
scene.render.image_settings.file_format="PNG"
scene.render.filepath=str(PROOF/"blender-source-front.png")
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/"SoloGymAthlete-v1.blend"))
if "--render" in sys.argv:
    bpy.ops.render.render(write_still=True)
print("SOLOGYM_SOURCE_EXPORTED", OUTPUT/"Character.json")
