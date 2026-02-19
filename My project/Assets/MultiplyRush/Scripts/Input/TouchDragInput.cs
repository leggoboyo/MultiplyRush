using UnityEngine;

namespace MultiplyRush
{
    public sealed class TouchDragInput : MonoBehaviour
    {
        private bool _isDragging;
        private float _lastX;

        public float GetHorizontalDeltaNormalized()
        {
            var deltaPixels = 0f;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _isDragging = true;
                        _lastX = touch.position.x;
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (_isDragging)
                        {
                            deltaPixels = touch.position.x - _lastX;
                            _lastX = touch.position.x;
                        }
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        _isDragging = false;
                        break;
                }
            }
            else
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                if (Input.GetMouseButtonDown(0))
                {
                    _isDragging = true;
                    _lastX = Input.mousePosition.x;
                }
                else if (Input.GetMouseButton(0) && _isDragging)
                {
                    var mouseX = Input.mousePosition.x;
                    deltaPixels = mouseX - _lastX;
                    _lastX = mouseX;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _isDragging = false;
                }
#else
                _isDragging = false;
#endif
            }

            return deltaPixels / Mathf.Max(1f, Screen.width);
        }
    }
}
