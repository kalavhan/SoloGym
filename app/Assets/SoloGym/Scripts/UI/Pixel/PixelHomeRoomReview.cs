using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Home composition: separate environment, slot-placed props and exact existing character.</summary>
    public sealed class PixelHomeRoomReview : MonoBehaviour
    {
        public PixelHomeRoom Room { get; private set; }
        public PixelCharacterViewport Character { get; private set; }
        public PixelRoomObject Ring { get; private set; }
        public PixelRoomObject Bag { get; private set; }
        public RectTransform InterfaceLayer { get; private set; }
        public bool Spanish { get; private set; }
        public Rect EffectiveSafeArea { get; private set; }
        public bool ShowCharacter { get; private set; } = true;
        RectTransform roomArea;
        Rect oldSafeArea;
        int oldWidth, oldHeight;
        float inset;

        void Awake()
        {
            Spanish = PixelButtonGallery.Argument("-sologym-locale", "es") != "en";
            float.TryParse(PixelButtonGallery.Argument("-sologym-safe-inset", "0"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out inset);
            inset = Mathf.Max(0, inset);
            var canvasRoot = new GameObject("Home composition canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            canvasRoot.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var matte = Child("Landscape matte", canvasRoot.transform); Stretch(matte);
            var fill = matte.gameObject.AddComponent<Image>(); fill.color = new Color32(18, 17, 24, 255); fill.raycastTarget = false;
            roomArea = Child("Safe room composition", canvasRoot.transform);
            Room = PixelHomeRoom.Create(roomArea);
            Ring = PixelRoomObject.Create(Room, "Rooms/Props/TrainingRingR1/item");
            Bag = PixelRoomObject.Create(Room, "Rooms/Props/HangingBagR1/item");
            Character = PixelCharacterViewport.Create(Room.Objects);
            // Match the approved hero's height and feet position, while retaining the shared eight-body envelope.
            var feet = Room.AnchorPoint("hero.feet");
            PixelHomeRoom.Place((RectTransform)Character.transform, feet + new Vector2(0, 8), new Vector2(316, 516), new Vector2(.5f, 0));
            string id = PixelButtonGallery.Argument("-sologym-character", "male-medium");
            Character.Show(id, Spanish ? "Bárbaro de ejemplo" : "Example Barbarian", Spanish ? "Personaje no disponible" : "Character unavailable");
            InterfaceLayer = Child("Independent live interface — next Home iteration", canvasRoot.transform);
            SetCharacterVisible(!PixelButtonGallery.HasArgument("-sologym-room-only"));
            Ring.SetVisible(!PixelButtonGallery.HasArgument("-sologym-hide-ring"));
            Bag.SetVisible(!PixelButtonGallery.HasArgument("-sologym-hide-bag"));
            Relayout();
        }
        static RectTransform Child(string name, Transform parent)
        { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); return r; }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        public void SetCharacterVisible(bool visible) { ShowCharacter = visible; Character.gameObject.SetActive(visible); }
        void Update()
        {
            if (oldWidth != Screen.width || oldHeight != Screen.height || oldSafeArea != Screen.safeArea) Relayout();
            // Review-only layer inspection. These are not product navigation or decoration commands.
            if (Input.GetKeyDown(KeyCode.F2)) SetCharacterVisible(!ShowCharacter);
            if (Input.GetKeyDown(KeyCode.F3)) Room.Exterior.enabled = !Room.Exterior.enabled;
            if (Input.GetKeyDown(KeyCode.F4)) Room.Architecture.enabled = !Room.Architecture.enabled;
            if (Input.GetKeyDown(KeyCode.F5)) Ring.SetVisible(!Ring.Visible);
            if (Input.GetKeyDown(KeyCode.F6)) Bag.SetVisible(!Bag.Visible);
        }
        public void Relayout()
        {
            oldWidth = Screen.width; oldHeight = Screen.height; oldSafeArea = Screen.safeArea;
            var s = Screen.safeArea;
            float x = Mathf.Min(Mathf.Max(s.xMin, inset), Screen.width - 1);
            float y = Mathf.Min(Mathf.Max(s.yMin, inset), Screen.height - 1);
            EffectiveSafeArea = Rect.MinMaxRect(x, y, Mathf.Max(x + 1, Mathf.Min(s.xMax, Screen.width - inset)),
                Mathf.Max(y + 1, Mathf.Min(s.yMax, Screen.height - inset)));
            foreach (var rect in new[] { roomArea, InterfaceLayer })
            {
                rect.anchorMin = new Vector2(EffectiveSafeArea.xMin / Screen.width, EffectiveSafeArea.yMin / Screen.height);
                rect.anchorMax = new Vector2(EffectiveSafeArea.xMax / Screen.width, EffectiveSafeArea.yMax / Screen.height);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            Canvas.ForceUpdateCanvases(); Room.RefreshLayout(); Character.RefreshLayout();
            // Apply pixel-adjusted meshes at the final scale, not the intermediate parent size.
            Canvas.ForceUpdateCanvases();
        }
        IEnumerator Start()
        {
            yield return null;
            if (PixelButtonGallery.HasArgument("-sologym-smoke") || PixelButtonGallery.HasArgument("-sologym-capture"))
                gameObject.AddComponent<PixelHomeRoomSmoke>().Run(this);
        }
    }
}
