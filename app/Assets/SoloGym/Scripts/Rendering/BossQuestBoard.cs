using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.Rendering
{
    /// <summary>
    /// The stone quest board in the summoning hall. Lists today's exercises on its slate in a world-space canvas;
    /// the current one glows gold inside a ring of fire.
    /// </summary>
    public sealed class BossQuestBoard : MonoBehaviour
    {
        public struct Row
        {
            public string name, dose;
            public bool done;
        }

        [Tooltip("Slate area in the board's local space (world units, origin at the board's feet).")]
        public Rect slate = new Rect(-1.52f, 1.35f, 3.04f, 4.6f);
        public int maxRows = 5;
        public Material glowMaterial, fireMaterial;
        public int sortingOrder = -5;
        [Tooltip("Warm point light that follows the current row and lights the 3D frame.")]
        public bool rowLight = true;

        static readonly Color Done = new Color32(120, 140, 160, 255);
        static readonly Color Pending = new Color32(226, 220, 205, 255);
        static readonly Color Current = new Color32(255, 214, 120, 255);
        static readonly Color Gold = new Color32(240, 190, 90, 255);

        const float Scale = 0.01f; // canvas units per world unit = 100
        Canvas canvas;
        RectTransform rows;
        Text title, footer;
        Transform highlight;
        ParticleSystem fire;
        readonly List<Text[]> lines = new List<Text[]>();
        Font font;

        void Awake() => Build();

        void Build()
        {
            if (canvas != null) return;
            font = Resources.Load<Font>("Fonts/PixelifySans");
            var go = new GameObject("Slate text", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = sortingOrder + 2;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            var r = (RectTransform)go.transform;
            r.sizeDelta = new Vector2(slate.width / Scale, slate.height / Scale);
            r.localScale = Vector3.one * Scale;
            r.localPosition = new Vector3(slate.center.x, slate.center.y, -0.02f);

            float w = r.sizeDelta.x, h = r.sizeDelta.y;
            title = Label(r, new Rect(10, 10, w - 20, 52), 34, Gold, TextAnchor.MiddleCenter, true);
            footer = Label(r, new Rect(10, h - 50, w - 20, 40), 22, Gold, TextAnchor.MiddleCenter, false);
            rows = (RectTransform)new GameObject("Rows", typeof(RectTransform)).transform; rows.SetParent(r, false); Place(rows, new Rect(0, 70, w, h - 130));

            // Glow behind the current row, and a ring of fire around it.
            highlight = new GameObject("Current exercise").transform; highlight.SetParent(transform, false);
            if (glowMaterial != null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Glow"; DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(highlight, false); quad.transform.localPosition = new Vector3(0, 0, -0.01f);
                quad.transform.localScale = new Vector3(slate.width * 1.15f, 1.1f, 1);
                var mr = quad.GetComponent<Renderer>(); mr.sharedMaterial = glowMaterial; mr.sortingOrder = sortingOrder + 1;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
            if (fireMaterial != null) fire = Fire(highlight);
            if (rowLight)
            {
                var lamp = new GameObject("Row light").AddComponent<Light>(); lamp.transform.SetParent(highlight, false);
                lamp.transform.localPosition = new Vector3(0, 0, -0.6f);
                lamp.type = LightType.Point; lamp.color = new Color(1f, 0.62f, 0.25f); lamp.range = 3.2f; lamp.intensity = 1.8f; lamp.shadows = LightShadows.None;
            }
        }

        ParticleSystem Fire(Transform parent)
        {
            var go = new GameObject("Fire border"); go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.loop = true; main.playOnAwake = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f); main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.08f), new Color(1f, 0.9f, 0.35f));
            main.gravityModifier = -0.15f; main.maxParticles = 400; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.prewarm = true;
            var em = p.emission; em.rateOverTime = 140f;
            var sh = p.shape; sh.shapeType = ParticleSystemShapeType.BoxEdge; sh.scale = new Vector3(slate.width * 0.98f, 0.66f, 0f);
            var col = p.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var noise = p.noise; noise.enabled = true; noise.strength = 0.12f; noise.frequency = 2f; noise.scrollSpeed = 1f;
            var pr = go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = fireMaterial; pr.sortingOrder = sortingOrder + 3;
            p.Play();
            return p;
        }

        Text Label(Transform parent, Rect b, int size, Color color, TextAnchor align, bool glow)
        {
            var t = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false); Place(t.rectTransform, b);
            t.font = font; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false; t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            var shadow = t.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, 0.8f); shadow.effectDistance = new Vector2(2, -2);
            if (glow) { var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(1f, 0.55f, 0.1f, 0.55f); o.effectDistance = new Vector2(2, 2); }
            return t;
        }

        static void Place(RectTransform r, Rect b)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(b.x, -b.y); r.sizeDelta = b.size;
        }

        public void SetHeader(string heading, string stage)
        {
            Build();
            title.text = heading ?? "";
            footer.text = stage ?? "";
        }

        /// <summary>Shows a window of rows around the current exercise. current = -1 hides the highlight.</summary>
        public void SetRows(IList<Row> all, int current)
        {
            Build();
            int count = all == null ? 0 : all.Count;
            int first = 0;
            if (count > maxRows) first = Mathf.Clamp(current - 1, 0, count - maxRows);
            int shown = Mathf.Min(maxRows, count);
            float rowHeight = rows.sizeDelta.y / maxRows;
            while (lines.Count < shown)
            {
                int i = lines.Count;
                var name = Label(rows, new Rect(18, i * rowHeight + 4, rows.sizeDelta.x - 36, rowHeight * 0.55f), 28, Pending, TextAnchor.LowerLeft, false);
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 18; name.resizeTextMaxSize = 28; name.horizontalOverflow = HorizontalWrapMode.Wrap;
                var dose = Label(rows, new Rect(18, i * rowHeight + rowHeight * 0.55f + 2, rows.sizeDelta.x - 36, rowHeight * 0.42f), 19, Pending, TextAnchor.UpperLeft, false);
                dose.verticalOverflow = VerticalWrapMode.Overflow;
                lines.Add(new[] { name, dose });
            }
            for (int i = 0; i < lines.Count; i++)
            {
                bool on = i < shown;
                lines[i][0].gameObject.SetActive(on); lines[i][1].gameObject.SetActive(on);
                if (!on) continue;
                var row = all[first + i];
                bool isCurrent = first + i == current;
                lines[i][0].text = row.name; lines[i][1].text = row.dose;
                var c = isCurrent ? Current : row.done ? Done : Pending;
                lines[i][0].color = c; lines[i][1].color = isCurrent ? Gold : c;
                var outline = lines[i][0].GetComponent<Outline>();
                if (isCurrent && outline == null) { outline = lines[i][0].gameObject.AddComponent<Outline>(); outline.effectColor = new Color(1f, 0.55f, 0.1f, 0.6f); outline.effectDistance = new Vector2(2, 2); }
                if (outline != null) outline.enabled = isCurrent;
            }
            bool highlighted = current >= first && current < first + shown;
            highlight.gameObject.SetActive(highlighted);
            if (highlighted)
            {
                // Row centre in board space: canvas top, minus the rows offset, minus the row's middle.
                float canvasTop = slate.yMax;
                float y = canvasTop - (70f + (current - first + 0.5f) * rowHeight) * Scale;
                highlight.localPosition = new Vector3(slate.center.x, y, -0.03f);
                float rowWorld = rowHeight * Scale;
                var glow = highlight.Find("Glow"); if (glow != null) glow.localScale = new Vector3(slate.width * 1.15f, rowWorld * 1.7f, 1f);
                if (fire != null) { var shape = fire.shape; shape.scale = new Vector3(slate.width * 0.96f, rowWorld * 0.95f, 0f); }
            }
        }
    }
}
