using UnityEngine;

namespace MultiplyRush
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        public RectTransform targetRect;
        public bool continuousRefresh = true;

        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;

        private void Awake()
        {
            if (targetRect == null)
            {
                targetRect = GetComponent<RectTransform>();
            }
        }

        private void OnEnable()
        {
            ApplySafeArea(force: true);
        }

        private void Update()
        {
            if (!continuousRefresh)
            {
                return;
            }

            ApplySafeArea(force: false);
        }

        private void ApplySafeArea(bool force)
        {
            if (targetRect == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var resolution = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == _lastSafeArea && resolution == _lastResolution)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastResolution = resolution;

            if (resolution.x <= 0 || resolution.y <= 0)
            {
                ApplyFullscreen();
                return;
            }

            var invalidSafeArea = safeArea.width < 1f ||
                                  safeArea.height < 1f ||
                                  safeArea.xMin < 0f ||
                                  safeArea.yMin < 0f ||
                                  safeArea.xMax > resolution.x ||
                                  safeArea.yMax > resolution.y;

            if (invalidSafeArea)
            {
                ApplyFullscreen();
                return;
            }

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= resolution.x;
            anchorMin.y /= resolution.y;
            anchorMax.x /= resolution.x;
            anchorMax.y /= resolution.y;

            targetRect.anchorMin = anchorMin;
            targetRect.anchorMax = anchorMax;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
        }

        private void ApplyFullscreen()
        {
            targetRect.anchorMin = Vector2.zero;
            targetRect.anchorMax = Vector2.one;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
        }
    }
}
