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
        public float fadeInDuration = 0.2f;
        public float fadeOutDuration = 0.12f;
        public float startScale = 0.86f;
        public float overshootScale = 1.05f;
        public float settleSpeed = 14f;

        public event Action OnRetryRequested;
        public event Action OnNextRequested;

        private CanvasGroup _canvasGroup;
        private RectTransform _panelRect;
        private Vector3 _panelBaseScale = Vector3.one;
        private bool _isVisible;
        private bool _isAnimatingIn;
        private bool _isAnimatingOut;
        private float _animTimer;

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

            if (rootPanel == null)
            {
                rootPanel = gameObject;
            }

            _canvasGroup = rootPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = rootPanel.AddComponent<CanvasGroup>();
            }

            var panel = rootPanel.transform.Find("Panel");
            if (panel != null)
            {
                _panelRect = panel.GetComponent<RectTransform>();
                if (_panelRect != null)
                {
                    _panelBaseScale = _panelRect.localScale;
                }
            }

        }

        private void Update()
        {
            if (!_isAnimatingIn && !_isAnimatingOut)
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_isAnimatingIn)
            {
                AnimateIn(deltaTime);
                return;
            }

            AnimateOut(deltaTime);
        }

        public void ShowResult(bool didWin, int levelIndex, int playerCount, int enemyCount)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            _isVisible = true;
            _isAnimatingIn = true;
            _isAnimatingOut = false;
            _animTimer = 0f;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (_panelRect != null)
            {
                _panelRect.localScale = _panelBaseScale * startScale;
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
            if (!_isVisible)
            {
                if (rootPanel != null)
                {
                    rootPanel.SetActive(false);
                }
                return;
            }

            _isAnimatingOut = true;
            _isAnimatingIn = false;
            _animTimer = 0f;
            _isVisible = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private void AnimateIn(float deltaTime)
        {
            _animTimer += deltaTime;
            var duration = Mathf.Max(0.03f, fadeInDuration);
            var progress = Mathf.Clamp01(_animTimer / duration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = eased;
            }

            if (_panelRect != null)
            {
                var overshoot = Mathf.Lerp(startScale, overshootScale, eased);
                _panelRect.localScale = _panelBaseScale * overshoot;
                _panelRect.localScale = Vector3.Lerp(_panelRect.localScale, _panelBaseScale, 1f - Mathf.Exp(-settleSpeed * deltaTime));
            }

            if (progress >= 1f)
            {
                _isAnimatingIn = false;
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f;
                }

                if (_panelRect != null)
                {
                    _panelRect.localScale = _panelBaseScale;
                }
            }
        }

        private void AnimateOut(float deltaTime)
        {
            _animTimer += deltaTime;
            var duration = Mathf.Max(0.02f, fadeOutDuration);
            var progress = Mathf.Clamp01(_animTimer / duration);
            var eased = Mathf.Pow(1f - progress, 2f);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = eased;
            }

            if (_panelRect != null)
            {
                var scale = Mathf.Lerp(1f, startScale, progress);
                _panelRect.localScale = _panelBaseScale * scale;
            }

            if (progress >= 1f)
            {
                _isAnimatingOut = false;
                if (rootPanel != null)
                {
                    rootPanel.SetActive(false);
                }

                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }
            }
        }
    }
}
