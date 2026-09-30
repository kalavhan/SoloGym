using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloGym.UI
{
    /// <summary>Explicit Tab order without stealing arrow keys from native text editing.</summary>
    public sealed class PixelFieldTabNavigation : MonoBehaviour
    {
        public Selectable Previous;
        public Selectable Next;
        static int handledFrame = -1;

        void Update()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject
                || handledFrame == Time.frameCount || !Input.GetKeyDown(KeyCode.Tab)) return;
            handledFrame = Time.frameCount;
            Move(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }

        public bool Move(bool backwards)
        {
            var target = backwards ? Previous : Next;
            var visited = new HashSet<Selectable>();
            while (target != null && visited.Add(target))
            {
                if (target.IsActive() && target.IsInteractable())
                {
                    target.Select();
                    return true;
                }
                var link = target.GetComponent<PixelFieldTabNavigation>();
                if (link == null) break;
                target = backwards ? link.Previous : link.Next;
            }
            return false;
        }
    }
}
