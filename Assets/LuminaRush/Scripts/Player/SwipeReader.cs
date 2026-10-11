using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LuminaRush
{
    public struct SwipeFrame
    {
        public bool Left;
        public bool Right;
        public bool Up;
        public bool Down;
    }

    public sealed class SwipeReader
    {
        const float MinDistance = 48f;
        readonly List<RaycastResult> hits = new List<RaycastResult>(8);
        Vector2 origin;
        bool tracking;
        bool blocked;
        int finger = -1;

        public SwipeFrame Read()
        {
            SwipeFrame frame = default;
            ReadKeys(ref frame);
            if (Input.touchCount > 0)
                ReadTouch(ref frame);
            else
                ReadMouse(ref frame);
            return frame;
        }

        static void ReadKeys(ref SwipeFrame frame)
        {
            if (Pressed(KeyCode.LeftArrow) || Pressed(KeyCode.A)) frame.Left = true;
            if (Pressed(KeyCode.RightArrow) || Pressed(KeyCode.D)) frame.Right = true;
            if (Pressed(KeyCode.UpArrow) || Pressed(KeyCode.W) || Pressed(KeyCode.Space)) frame.Up = true;
            if (Pressed(KeyCode.DownArrow) || Pressed(KeyCode.S)) frame.Down = true;
        }

        static bool Pressed(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        void ReadTouch(ref SwipeFrame frame)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && !tracking)
                {
                    tracking = true;
                    blocked = OverUi(touch.position);
                    finger = touch.fingerId;
                    origin = touch.position;
                }
                else if (tracking && touch.fingerId == finger && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    if (!blocked)
                        Classify(touch.position - origin, ref frame);
                    tracking = false;
                    blocked = false;
                    finger = -1;
                }
            }
        }

        void ReadMouse(ref SwipeFrame frame)
        {
            if (Input.GetMouseButtonDown(0))
            {
                tracking = true;
                blocked = OverUi(Input.mousePosition);
                origin = Input.mousePosition;
            }
            else if (tracking && Input.GetMouseButtonUp(0))
            {
                if (!blocked)
                    Classify((Vector2)Input.mousePosition - origin, ref frame);
                tracking = false;
                blocked = false;
            }
        }

        bool OverUi(Vector2 position)
        {
            if (EventSystem.current == null)
                return false;
            var data = new PointerEventData(EventSystem.current);
            data.position = position;
            hits.Clear();
            EventSystem.current.RaycastAll(data, hits);
            return hits.Count > 0;
        }

        static void Classify(Vector2 delta, ref SwipeFrame frame)
        {
            if (delta.magnitude < MinDistance)
                return;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x < 0f) frame.Left = true;
                else frame.Right = true;
            }
            else
            {
                if (delta.y > 0f) frame.Up = true;
                else frame.Down = true;
            }
        }
    }
}
