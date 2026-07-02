using System;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    /// <summary>
    /// Chỉ đọc input kéo màn hình, trả về screen delta.
    /// Không biết gì về camera hay world space.
    /// </summary>
    public class CameraDragInput
    {
        public bool IsDragging { get; private set; }

        /// <summary>Fired mỗi frame khi đang kéo, trả về screenDelta pixel.</summary>
        public event Action<Vector2> OnDrag;

        /// <summary>Fired khi bắt đầu kéo — để cancel align.</summary>
        public event Action OnDragBegan;

        public void Tick()
        {
#if UNITY_EDITOR
            TickMouse();
#else
            TickTouch();
#endif
        }

        private Vector2 _lastPos;

        private void TickMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                IsDragging = true;
                _lastPos = Input.mousePosition;
                OnDragBegan?.Invoke();
            }
            else if (Input.GetMouseButton(0) && IsDragging)
            {
                Vector2 current = Input.mousePosition;
                Vector2 delta = current - _lastPos;
                _lastPos = current;
                if (delta.sqrMagnitude > 0.01f)
                    OnDrag?.Invoke(delta);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                IsDragging = false;
            }
        }

        private void TickTouch()
        {
            if (Input.touchCount != 1)
            {
                IsDragging = false;
                return;
            }

            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                IsDragging = true;
                _lastPos = touch.position;
                OnDragBegan?.Invoke();
            }
            else if (touch.phase == TouchPhase.Moved && IsDragging)
            {
                Vector2 delta = touch.deltaPosition;
                if (delta.sqrMagnitude > 0.01f)
                    OnDrag?.Invoke(delta);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                IsDragging = false;
            }
        }
    }
}
