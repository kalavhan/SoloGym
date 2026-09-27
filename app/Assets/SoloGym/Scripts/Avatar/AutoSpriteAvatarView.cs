using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Static composition of unchanged body, garment and hair textures.</summary>
    public sealed class AutoSpriteAvatarView : MonoBehaviour
    {
        [Serializable] public sealed class Box { public float x, y, width, height; public Rect Rect => new Rect(x,y,width,height); }
        [Serializable] public sealed class Layer { public string id, resource; public Box sourceRect, destinationRect; }
        [Serializable] public sealed class Polygon { public Vector2[] points; public int[] triangles; }
        [Serializable] public sealed class Body
        {
            public string id, gender, bodyType, resource;
            public Layer torso, legs;
            public Layer[] hair;
            public Polygon[] neck, hands;
        }
        [Serializable] public sealed class Catalog { public string schema; public int canvasSize; public Box viewBounds; public Body[] bodies; }
        const string Folder = "AvatarAutoSprite/modular-front-v1/";
        static Catalog catalog;
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        RectTransform layers;
        public bool IsLoaded { get; private set; }
        public string LastError { get; private set; }
        public string BodyId { get; private set; }
        public string HairId { get; private set; }
        public int VisibleLayers => layers == null ? 0 : layers.childCount;

        public void Mount(RectTransform parent, Rect stage)
        {
            try
            {
                if (catalog == null)
                {
                    var json = Resources.Load<TextAsset>(Folder + "catalog");
                    if (json == null) throw new InvalidOperationException("Modular character catalog is missing.");
                    catalog = JsonUtility.FromJson<Catalog>(json.text);
                    if (catalog.schema != "sologym.modular-avatar.v1" || catalog.canvasSize != 1024 || catalog.bodies.Length != 8)
                        throw new InvalidOperationException("Unsupported modular character catalog.");
                }
                var b = catalog.viewBounds;
                float scale = Mathf.Min(stage.width / b.width, stage.height / b.height);
                // The same view bounds and scale for every body. No per-preset zoom.
                layers = SystemUI.Node("Static modular character", parent, new Rect(
                    stage.x + (stage.width - b.width * scale)/2 - b.x * scale,
                    stage.y + stage.height - (b.y+b.height)*scale, 1024,1024));
                layers.localScale = Vector3.one * scale;
                RefreshView();
            }
            catch (Exception e) { Fail(e); }
        }

        public void RefreshView() => SetAppearance(AutoSpriteSession.Appearance);
        public void SetAppearance(ModularAppearance appearance)
        {
            if (layers == null) return;
            foreach (Transform child in layers) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            try
            {
                var selection = appearance.Clone(); selection.Normalize();
                var body = Array.Find(catalog.bodies, b => b.id == selection.bodyId);
                if (body == null) throw new InvalidOperationException("Body preset is missing.");
                var texture = Load(body.resource);
                Add(texture, new Rect(0,0,1024,1024), new Rect(0,0,1024,1024), "Body");
                if (selection.legs) Add(body.legs, "Shorts");
                if (selection.torso) { Add(body.torso,"Top"); Restore(texture,body.neck,"Neck"); }
                if (selection.legs) Restore(texture,body.hands,"Hands");
                if (selection.hairId != "none") Add(Array.Find(body.hair,h => h.id == selection.hairId),"Hair");
                BodyId = body.id; HairId = selection.hairId; IsLoaded = true; LastError = null;
            }
            catch (Exception e) { Fail(e); }
        }

        static Texture2D Load(string path)
        {
            if (textures.TryGetValue(path,out var existing)) return existing;
            var texture = Resources.Load<Texture2D>(Folder + path);
            if (texture == null || texture.width != 1024 || texture.height != 1024)
                throw new InvalidOperationException("Missing or resized modular sprite: " + path);
            textures[path]=texture; return texture;
        }
        void Add(Layer layer,string name)
        {
            if (layer == null) throw new InvalidOperationException("Missing fitted layer: " + name);
            Add(Load(layer.resource),layer.sourceRect.Rect,layer.destinationRect.Rect,name);
        }
        void Add(Texture2D texture,Rect source,Rect destination,string name)
        {
            var art=SystemUI.Node(name,layers,destination).gameObject.AddComponent<RawImage>();
            art.raycastTarget=false;art.texture=texture;
            art.uvRect=new Rect(source.x/texture.width,1-source.yMax/texture.height,source.width/texture.width,source.height/texture.height);
        }
        void Restore(Texture2D texture,Polygon[] polygons,string name)
        {
            foreach(var polygon in polygons)
            {
                var patch=SystemUI.Node(name,layers,new Rect(0,0,1024,1024)).gameObject.AddComponent<ModularBodyPatch>();
                patch.Configure(texture,polygon);
            }
        }
        void Fail(Exception e)
        {
            IsLoaded=false;LastError=e.Message;
            if(layers!=null) foreach(Transform child in layers) child.gameObject.SetActive(false);
            Debug.LogError("SOLOGYM_MODULAR_LOAD " + e.Message);
        }
        void OnDestroy() { if(layers!=null) Destroy(layers.gameObject); }
    }

    /// <summary>Draw the original body's neck/hands above clothes, using the reviewed masks.</summary>
    public sealed class ModularBodyPatch : MaskableGraphic
    {
        Texture2D texture;
        AutoSpriteAvatarView.Polygon polygon;
        public override Texture mainTexture => texture;
        public void Configure(Texture2D source,AutoSpriteAvatarView.Polygon shape)
        { texture=source;polygon=shape;raycastTarget=false;SetAllDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(polygon==null) return;
            foreach(var p in polygon.points) vh.AddVert(new Vector3(p.x,-p.y,0),Color.white,new Vector2(p.x/1024,1-p.y/1024));
            for(int i=0;i<polygon.triangles.Length;i+=3) vh.AddTriangle(polygon.triangles[i],polygon.triangles[i+1],polygon.triangles[i+2]);
        }
    }
}
