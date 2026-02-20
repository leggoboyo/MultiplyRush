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

        private void Awake()
        {
            if (hintText != null && string.IsNullOrWhiteSpace(hintText.text))
            {
                hintText.text = "Drag left/right to steer";
            }

            var wasDismissed = PlayerPrefs.GetInt(DragHintDismissedKey, 0) == 1;
            var shouldShow = !showOnlyUntilFirstDrag || !wasDismissed;
            if (rootPanel != null)
            {
                rootPanel.SetActive(shouldShow);
            }
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
