using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplyRush
{
    public sealed class TouchDragInput : MonoBehaviour
    {
        private bool _isDraggingFromCurrentPress;
        private float _lastX;

        public float GetHorizontalDeltaNormalized()
        {
            var hasPointer = TryGetPrimaryPointerX(out var pointerX);
            if (!hasPointer)
            {
                _isDraggingFromCurrentPress = false;
                return 0f;
            }

            if (!_isDraggingFromCurrentPress)
            {
                _isDraggingFromCurrentPress = true;
                _lastX = pointerX;
                return 0f;
            }

            var deltaPixels = pointerX - _lastX;
            _lastX = pointerX;
            return deltaPixels / Mathf.Max(1f, Screen.width);
        }

        private static bool TryGetPrimaryPointerX(out float pointerX)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.isPressed)
                {
                    pointerX = touch.position.ReadValue().x;
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                pointerX = mouse.position.ReadValue().x;
                return true;
            }

            pointerX = 0f;
            return false;
        }
    }
}
