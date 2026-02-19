using System;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class ResultOverlayController : MonoBehaviour
    {
        public GameObject rootPanel;
        public Text titleText;
        public Text detailText;
        public Button retryButton;
        public Button nextButton;

        public event Action OnRetryRequested;
        public event Action OnNextRequested;

        private void Awake()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() => OnRetryRequested?.Invoke());
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => OnNextRequested?.Invoke());
            }
        }

        public void ShowResult(bool didWin, int levelIndex, int playerCount, int enemyCount)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = didWin ? "WIN" : "LOSE";
                titleText.color = didWin ? new Color(0.2f, 0.95f, 0.35f) : new Color(1f, 0.3f, 0.3f);
            }

            if (detailText != null)
            {
                detailText.text = "Level " + levelIndex + "\nYou: " + playerCount + "  Enemy: " + enemyCount;
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(!didWin);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(didWin);
            }
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }
    }
}
