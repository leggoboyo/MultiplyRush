using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplyRush
{
    public sealed class TouchDragInput : MonoBehaviour
    {
        private bool _isDraggingFromCurrentPress;
        private bool _wasPointerDownLastFrame;
        private float _lastX;

        public event Action DragStarted;
        public event Action DragEnded;

        public bool IsPointerDown => _wasPointerDownLastFrame;

        public bool TryGetPrimaryPointerScreenPosition(out Vector2 pointerPosition)
        {
            var hasPointer = TryGetPrimaryPointerX(out var pointerX);
            if (!hasPointer)
            {
                if (_wasPointerDownLastFrame)
                {
                    DragEnded?.Invoke();
                }

                _wasPointerDownLastFrame = false;
                _isDraggingFromCurrentPress = false;
                pointerPosition = Vector2.zero;
                return false;
            }

            if (!_wasPointerDownLastFrame)
            {
                _wasPointerDownLastFrame = true;
                _isDraggingFromCurrentPress = true;
                _lastX = pointerX;
                DragStarted?.Invoke();
            }
            else if (!_isDraggingFromCurrentPress)
            {
                _isDraggingFromCurrentPress = true;
                _lastX = pointerX;
            }

            pointerPosition = new Vector2(pointerX, GetPrimaryPointerY());
            return true;
        }

        public bool TryGetPrimaryPointerNormalizedX(out float normalizedX)
        {
            var hasPointer = TryGetPrimaryPointerX(out var pointerX);
            if (!hasPointer)
            {
                if (_wasPointerDownLastFrame)
                {
                    DragEnded?.Invoke();
                }

                _wasPointerDownLastFrame = false;
                _isDraggingFromCurrentPress = false;
                normalizedX = 0f;
                return false;
            }

            if (!_wasPointerDownLastFrame)
            {
                _wasPointerDownLastFrame = true;
                _isDraggingFromCurrentPress = true;
                _lastX = pointerX;
                DragStarted?.Invoke();
            }
            else if (!_isDraggingFromCurrentPress)
            {
                _isDraggingFromCurrentPress = true;
                _lastX = pointerX;
            }

            normalizedX = Mathf.Clamp01(pointerX / Mathf.Max(1f, Screen.width));
            return true;
        }

        public float GetHorizontalDeltaNormalized()
        {
            var hasPointer = TryGetPrimaryPointerX(out var pointerX);
            if (!hasPointer)
            {
                if (_wasPointerDownLastFrame)
                {
                    DragEnded?.Invoke();
                }

                _wasPointerDownLastFrame = false;
                _isDraggingFromCurrentPress = false;
                return 0f;
            }

            if (!_wasPointerDownLastFrame)
            {
                _wasPointerDownLastFrame = true;
                _isDraggingFromCurrentPress = true;
                _lastX = pointerX;
                DragStarted?.Invoke();
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

        private static float GetPrimaryPointerY()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.isPressed)
                {
                    return touch.position.ReadValue().y;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return mouse.position.ReadValue().y;
            }

            return 0f;
        }
    }
}
