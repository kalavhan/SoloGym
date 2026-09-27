using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Plays complete AutoSprite frames using provider atlas coordinates and timing.</summary>
    public sealed class AutoSpriteAvatarView : MonoBehaviour
    {
        [Serializable] public sealed class Box { public int x, y, width, height; }
        [Serializable] public sealed class Frame
        {
            public int x, y, width, height;
            public float weight;
        }
        [Serializable] public sealed class Clip
        {
            public string id, resource;
            public int textureWidth, textureHeight;
            public float durationSeconds;
            public Box bounds;
            public Frame[] frames;
        }
        [Serializable] public sealed class Catalog { public string schema; public Clip[] clips; }

        const string Folder = "AvatarAutoSprite/female-studio-home-r1/";
        Catalog catalog;
        Clip clip;
        RawImage art;
        Rect stage;
        float totalWeight;
        public bool IsLoaded => clip != null && art != null && art.texture != null;
        public string LastError { get; private set; }
        public string ViewId => clip?.id;
        public int FrameIndex { get; private set; }
        public int FrameCount => clip?.frames.Length ?? 0;

        public void Mount(RectTransform parent, Rect rect)
        {
            AutoSpriteSession.Initialize();
            stage = rect;
            art = SystemUI.Node("AutoSprite whole character", parent, rect).gameObject.AddComponent<RawImage>();
            art.raycastTarget = false;
            art.enabled = false;
            try
            {
                var asset = Resources.Load<TextAsset>(Folder + "catalog");
                if (asset == null) throw new InvalidOperationException("AutoSprite catalog is missing.");
                catalog = JsonUtility.FromJson<Catalog>(asset.text);
                if (catalog == null || catalog.schema != "sologym.autosprite-atlas.v1" || catalog.clips == null)
                    throw new InvalidOperationException("Unsupported AutoSprite catalog.");
                RefreshView();
            }
            catch (Exception ex) { Fail(ex.Message); }
        }

        public void RefreshView()
        {
            if (catalog == null) return;
            try
            {
                var candidate = Array.Find(catalog.clips, c => c.id == AutoSpriteSession.View);
                if (candidate == null) throw new InvalidOperationException("Requested AutoSprite view is unavailable.");
                if (clip == candidate) { Draw(); return; }
                var texture = Resources.Load<Texture2D>(Folder + candidate.resource);
                Validate(candidate, texture);
                clip = candidate;
                art.texture = texture;
                totalWeight = 0;
                foreach (var frame in clip.frames) totalWeight += frame.weight;
                // One union of every frame's alpha bounds: fixed scale and baseline throughout the loop.
                float scale = Mathf.Min(stage.width / clip.bounds.width, stage.height / clip.bounds.height);
                float left = stage.x + (stage.width - clip.bounds.width * scale) / 2;
                float top = stage.y + stage.height - clip.bounds.height * scale;
                SystemUI.Place(art.rectTransform, new Rect(left, top, clip.bounds.width * scale, clip.bounds.height * scale));
                art.enabled = true;
                LastError = null;
                Draw();
            }
            catch (Exception ex) { Fail(ex.Message); }
        }

        static void Validate(Clip c, Texture2D texture)
        {
            if (texture == null || texture.width != c.textureWidth || texture.height != c.textureHeight)
                throw new InvalidOperationException("AutoSprite texture is missing or was resized on import.");
            if (c.frames == null || c.frames.Length < 2 || !float.IsFinite(c.durationSeconds) || c.durationSeconds <= 0
                || c.bounds == null || c.bounds.x < 0 || c.bounds.y < 0 || c.bounds.width <= 0 || c.bounds.height <= 0)
                throw new InvalidOperationException("Invalid AutoSprite frame metadata.");
            foreach (var f in c.frames)
                if (f.x < 0 || f.y < 0 || f.width <= 0 || f.height <= 0 || !float.IsFinite(f.weight) || f.weight <= 0
                    || f.x + f.width > c.textureWidth || f.y + f.height > c.textureHeight
                    || c.bounds.x + c.bounds.width > f.width || c.bounds.y + c.bounds.height > f.height)
                    throw new InvalidOperationException("AutoSprite frame lies outside the atlas.");
        }

        void Update()
        {
            if (catalog == null) return;
            if (ViewId != AutoSpriteSession.View) RefreshView();
            else if (IsLoaded) Draw();
        }

        void Draw()
        {
            float phase = Mathf.Repeat(AutoSpriteSession.PlaybackSeconds, clip.durationSeconds) / clip.durationSeconds * totalWeight;
            int index = 0;
            while (index < clip.frames.Length - 1 && phase >= clip.frames[index].weight)
                phase -= clip.frames[index++].weight;
            FrameIndex = index;
            var frame = clip.frames[index];
            var b = clip.bounds;
            // AutoSprite JSON is top-left origin; Unity texture UVs use bottom-left origin.
            art.uvRect = new Rect((frame.x + b.x) / (float)clip.textureWidth,
                1 - (frame.y + b.y + b.height) / (float)clip.textureHeight,
                b.width / (float)clip.textureWidth, b.height / (float)clip.textureHeight);
        }

        void Fail(string reason)
        {
            clip = null;
            LastError = reason;
            if (art != null) art.enabled = false;
            Debug.LogError("SOLOGYM_AUTOSPRITE_LOAD " + reason);
        }

        void OnDestroy() { if (art != null) Destroy(art.gameObject); }
    }
}
