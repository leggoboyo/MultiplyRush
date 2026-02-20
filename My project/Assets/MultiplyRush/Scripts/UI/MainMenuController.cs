using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public string gameSceneName = "Game";
        public Text bestLevelText;
        public float titlePulseSpeed = 2.2f;
        public float titlePulseScale = 0.035f;
        public float buttonFloatSpeed = 2.8f;
        public float buttonFloatDistance = 10f;
        public float buttonPulseScale = 0.06f;

        private RectTransform _titleRect;
        private RectTransform _playButtonRect;
        private Vector3 _titleBaseScale = Vector3.one;
        private Vector3 _buttonBaseScale = Vector3.one;
        private Vector2 _buttonBasePosition;
        private Image _playButtonImage;
        private Color _buttonBaseColor = Color.white;

        private void Start()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            SceneVisualTuning.ApplyMenuLook();
            EnsureBackgroundBehindContent();
            CacheMenuElements();

            if (bestLevelText != null)
            {
                bestLevelText.text = "Best Level: " + ProgressionStore.GetBestLevel();
            }
        }

        private void Update()
        {
            AnimateMenu(Time.unscaledTime);
        }

        private void EnsureBackgroundBehindContent()
        {
            var background = GameObject.Find("Background");
            if (background == null)
            {
                return;
            }

            var backgroundRect = background.GetComponent<RectTransform>();
            if (backgroundRect == null)
            {
                return;
            }

            backgroundRect.SetAsFirstSibling();
        }

        private void CacheMenuElements()
        {
            var title = GameObject.Find("Title");
            if (title != null)
            {
                _titleRect = title.GetComponent<RectTransform>();
                if (_titleRect != null)
                {
                    _titleBaseScale = _titleRect.localScale;
                }
            }

            var playButton = GameObject.Find("PlayButton");
            if (playButton != null)
            {
                _playButtonRect = playButton.GetComponent<RectTransform>();
                if (_playButtonRect != null)
                {
                    _buttonBaseScale = _playButtonRect.localScale;
                    _buttonBasePosition = _playButtonRect.anchoredPosition;
                }

                _playButtonImage = playButton.GetComponent<Image>();
                if (_playButtonImage != null)
                {
                    _buttonBaseColor = _playButtonImage.color;
                }
            }
        }

        private void AnimateMenu(float runTime)
        {
            if (_titleRect != null)
            {
                var titlePulse = 1f + Mathf.Sin(runTime * titlePulseSpeed) * titlePulseScale;
                _titleRect.localScale = _titleBaseScale * titlePulse;
            }

            if (_playButtonRect != null)
            {
                var floatWave = Mathf.Sin(runTime * buttonFloatSpeed);
                _playButtonRect.anchoredPosition = _buttonBasePosition + new Vector2(0f, floatWave * buttonFloatDistance);
                var pulse = 1f + Mathf.Abs(floatWave) * buttonPulseScale;
                _playButtonRect.localScale = _buttonBaseScale * pulse;
            }

            if (_playButtonImage != null)
            {
                var glow = 0.84f + Mathf.Abs(Mathf.Sin(runTime * buttonFloatSpeed)) * 0.16f;
                _playButtonImage.color = new Color(
                    _buttonBaseColor.r * glow,
                    _buttonBaseColor.g * glow,
                    _buttonBaseColor.b * glow,
                    _buttonBaseColor.a);
            }
        }

        public void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
