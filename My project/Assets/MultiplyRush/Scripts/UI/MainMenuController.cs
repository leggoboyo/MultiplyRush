using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public string gameSceneName = "Game";
        public Text bestLevelText;
        public DifficultyMode defaultDifficulty = DifficultyMode.Normal;
        public Color selectedDifficultyColor = new Color(0.26f, 0.72f, 0.98f, 1f);
        public Color unselectedDifficultyColor = new Color(0.2f, 0.28f, 0.38f, 1f);
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
        private DifficultyMode _selectedDifficulty;
        private RectTransform _difficultyRow;
        private Button _easyButton;
        private Button _normalButton;
        private Button _hardButton;
        private Text _easyLabel;
        private Text _normalLabel;
        private Text _hardLabel;
        private Text _difficultyLabel;

        private void Start()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            SceneVisualTuning.ApplyMenuLook();
            EnsureBackgroundBehindContent();
            CacheMenuElements();
            _selectedDifficulty = ProgressionStore.GetDifficultyMode(defaultDifficulty);
            EnsureDifficultySelector();
            ApplyDifficultySelectionVisuals();

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

                if (_difficultyRow != null)
                {
                    var rowPosition = _buttonBasePosition + new Vector2(0f, 108f + Mathf.Sin(runTime * 1.7f) * 5f);
                    _difficultyRow.anchoredPosition = rowPosition;
                }
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
            ProgressionStore.SetDifficultyMode(_selectedDifficulty);
            SceneManager.LoadScene(gameSceneName);
        }

        private void EnsureDifficultySelector()
        {
            var rowTransform = GameObject.Find("DifficultyRow");
            if (rowTransform == null)
            {
                var parent = _playButtonRect != null
                    ? _playButtonRect.parent as RectTransform
                    : transform as RectTransform;
                if (parent == null)
                {
                    return;
                }

                var rowObject = new GameObject("DifficultyRow");
                rowObject.transform.SetParent(parent, false);
                _difficultyRow = rowObject.AddComponent<RectTransform>();
                _difficultyRow.anchorMin = new Vector2(0.5f, 0.5f);
                _difficultyRow.anchorMax = new Vector2(0.5f, 0.5f);
                _difficultyRow.pivot = new Vector2(0.5f, 0.5f);
                _difficultyRow.sizeDelta = new Vector2(470f, 130f);
            }
            else
            {
                _difficultyRow = rowTransform.GetComponent<RectTransform>();
            }

            if (_difficultyRow == null)
            {
                return;
            }

            if (_playButtonRect != null)
            {
                _difficultyRow.anchoredPosition = _buttonBasePosition + new Vector2(0f, 108f);
            }

            _difficultyLabel = EnsureDifficultyText(
                _difficultyRow,
                "DifficultyLabel",
                "Difficulty",
                new Vector2(0f, 42f),
                24);

            _easyButton = EnsureDifficultyButton(_difficultyRow, "EasyButton", "Easy", new Vector2(-150f, -4f), out _easyLabel);
            _normalButton = EnsureDifficultyButton(_difficultyRow, "NormalButton", "Normal", new Vector2(0f, -4f), out _normalLabel);
            _hardButton = EnsureDifficultyButton(_difficultyRow, "HardButton", "Hard", new Vector2(150f, -4f), out _hardLabel);

            if (_easyButton != null)
            {
                _easyButton.onClick.RemoveAllListeners();
                _easyButton.onClick.AddListener(() => SelectDifficulty(DifficultyMode.Easy));
            }

            if (_normalButton != null)
            {
                _normalButton.onClick.RemoveAllListeners();
                _normalButton.onClick.AddListener(() => SelectDifficulty(DifficultyMode.Normal));
            }

            if (_hardButton != null)
            {
                _hardButton.onClick.RemoveAllListeners();
                _hardButton.onClick.AddListener(() => SelectDifficulty(DifficultyMode.Hard));
            }
        }

        private void SelectDifficulty(DifficultyMode mode)
        {
            _selectedDifficulty = mode;
            ApplyDifficultySelectionVisuals();
        }

        private void ApplyDifficultySelectionVisuals()
        {
            UpdateDifficultyButtonVisual(_easyButton, _easyLabel, _selectedDifficulty == DifficultyMode.Easy);
            UpdateDifficultyButtonVisual(_normalButton, _normalLabel, _selectedDifficulty == DifficultyMode.Normal);
            UpdateDifficultyButtonVisual(_hardButton, _hardLabel, _selectedDifficulty == DifficultyMode.Hard);
            if (_difficultyLabel != null)
            {
                _difficultyLabel.text = "Difficulty: " + _selectedDifficulty;
            }
        }

        private void UpdateDifficultyButtonVisual(Button button, Text label, bool isSelected)
        {
            if (button != null)
            {
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isSelected ? selectedDifficultyColor : unselectedDifficultyColor;
                }
            }

            if (label != null)
            {
                label.color = isSelected ? Color.white : new Color(0.8f, 0.88f, 0.96f, 1f);
                label.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static Text EnsureDifficultyText(
            RectTransform parent,
            string objectName,
            string textValue,
            Vector2 anchoredPosition,
            int fontSize)
        {
            var existing = parent.Find(objectName);
            GameObject textObject;
            if (existing == null)
            {
                textObject = new GameObject(objectName);
                textObject.transform.SetParent(parent, false);
            }
            else
            {
                textObject = existing.gameObject;
            }

            var rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(360f, 32f);

            var label = textObject.GetComponent<Text>();
            if (label == null)
            {
                label = textObject.AddComponent<Text>();
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.9f, 0.96f, 1f, 1f);
            label.text = textValue;
            return label;
        }

        private static Button EnsureDifficultyButton(
            RectTransform parent,
            string objectName,
            string labelText,
            Vector2 anchoredPosition,
            out Text label)
        {
            var existing = parent.Find(objectName);
            GameObject buttonObject;
            if (existing == null)
            {
                buttonObject = new GameObject(objectName);
                buttonObject.transform.SetParent(parent, false);
            }
            else
            {
                buttonObject = existing.gameObject;
            }

            var rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(130f, 44f);

            var image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
            button.colors = colors;

            var labelTransform = buttonObject.transform.Find("Label");
            GameObject labelObject;
            if (labelTransform == null)
            {
                labelObject = new GameObject("Label");
                labelObject.transform.SetParent(buttonObject.transform, false);
            }
            else
            {
                labelObject = labelTransform.gameObject;
            }

            var labelRect = labelObject.GetComponent<RectTransform>();
            if (labelRect == null)
            {
                labelRect = labelObject.AddComponent<RectTransform>();
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label = labelObject.GetComponent<Text>();
            if (label == null)
            {
                label = labelObject.AddComponent<Text>();
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = labelText;
            label.color = Color.white;

            return button;
        }
    }
}
