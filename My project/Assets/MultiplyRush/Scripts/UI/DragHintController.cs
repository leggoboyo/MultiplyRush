using UnityEngine;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class DragHintController : MonoBehaviour
    {
        private const string DragHintDismissedKey = "mr_drag_hint_dismissed";

        public GameObject rootPanel;
        public Text hintText;
        public TouchDragInput dragInput;
        public bool showOnlyUntilFirstDrag = true;
        public float floatDistance = 8f;
        public float floatSpeed = 2.2f;
        public float pulseScale = 0.05f;

        private RectTransform _rootRect;
        private Vector2 _baseAnchoredPosition;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (hintText != null && string.IsNullOrWhiteSpace(hintText.text))
            {
                hintText.text = "Drag to steer • Green gates help • Red gates hurt";
            }

            var wasDismissed = PlayerPrefs.GetInt(DragHintDismissedKey, 0) == 1;
            var shouldShow = !showOnlyUntilFirstDrag || !wasDismissed;
            if (rootPanel != null)
            {
                rootPanel.SetActive(shouldShow);
                _rootRect = rootPanel.GetComponent<RectTransform>();
                if (_rootRect != null)
                {
                    _baseAnchoredPosition = _rootRect.anchoredPosition;
                    _baseScale = _rootRect.localScale;
                }
            }
        }

        private void Update()
        {
            if (rootPanel == null || !rootPanel.activeSelf || _rootRect == null)
            {
                return;
            }

            var wave = Mathf.Sin(Time.unscaledTime * floatSpeed);
            _rootRect.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, wave * floatDistance);
            _rootRect.localScale = _baseScale * (1f + Mathf.Abs(wave) * pulseScale);
        }

        private void OnEnable()
        {
            if (dragInput != null)
            {
                dragInput.DragStarted += HandleDragStarted;
            }
        }

        private void OnDisable()
        {
            if (dragInput != null)
            {
                dragInput.DragStarted -= HandleDragStarted;
            }
        }

        private void HandleDragStarted()
        {
            if (rootPanel != null && rootPanel.activeSelf)
            {
                rootPanel.SetActive(false);
            }

            if (showOnlyUntilFirstDrag)
            {
                PlayerPrefs.SetInt(DragHintDismissedKey, 1);
                PlayerPrefs.Save();
            }
        }
    }
}
