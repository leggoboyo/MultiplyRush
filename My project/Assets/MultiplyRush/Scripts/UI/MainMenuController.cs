using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Flow")]
        public string gameSceneName = "Game";
        public Text bestLevelText;
        public DifficultyMode defaultDifficulty = DifficultyMode.Normal;

        [Header("Difficulty")]
        public Color selectedDifficultyColor = new Color(0.26f, 0.72f, 0.98f, 1f);
        public Color unselectedDifficultyColor = new Color(0.16f, 0.23f, 0.34f, 1f);

        [Header("Motion")]
        public float titlePulseSpeed = 2.2f;
        public float titlePulseScale = 0.035f;
        public float buttonFloatSpeed = 2.8f;
        public float buttonFloatDistance = 10f;
        public float buttonPulseScale = 0.08f;
        public float glowDriftSpeed = 0.22f;
        public float scanlineSpeed = 54f;
        public float playTransitionDuration = 0.45f;
        public float playZoomScale = 1.12f;
        public Color transitionFlashColor = new Color(0.3f, 0.84f, 1f, 1f);

        [Header("Palette")]
        public Color backgroundTopColor = new Color(0.03f, 0.09f, 0.2f, 1f);
        public Color backgroundBottomColor = new Color(0.01f, 0.02f, 0.08f, 1f);
        public Color playButtonColor = new Color(0.1f, 0.68f, 1f, 1f);
        public Color neonAccentColor = new Color(0.3f, 0.94f, 1f, 1f);

        private RectTransform _safeAreaRoot;
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
        private RectTransform _musicRow;
        private Text _musicLabel;
        private Text _musicTrackLabel;
        private Button _musicPrevButton;
        private Button _musicNextButton;
        private RectTransform _leftGlowRect;
        private RectTransform _rightGlowRect;
        private RectTransform _scanlineRect;
        private RectTransform _playShineRect;
        private RectTransform _badgeRect;
        private RectTransform _taglineRect;
        private Text _titleText;
        private Text _taglineText;
        private Text _badgeText;
        private float _scanlineBaseY;
        private Image _transitionFlashImage;
        private bool _isStartingGame;

        private void Start()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            SceneVisualTuning.ApplyMenuLook();
            CacheMenuElements();
            EnsureBackgroundBehindContent();
            EnsureRuntimePolish();

            _selectedDifficulty = ProgressionStore.GetDifficultyMode(defaultDifficulty);
            EnsureDifficultySelector();
            EnsureMusicSelector();
            ApplyDifficultySelectionVisuals();
            HapticsDirector.EnsureInstance();
            AppLifecycleController.EnsureInstance().SetPauseOnFocusLoss(true);

            if (bestLevelText == null)
            {
                var bestLevelObj = GameObject.Find("BestLevel");
                if (bestLevelObj != null)
                {
                    bestLevelText = bestLevelObj.GetComponent<Text>();
                }
            }

            if (bestLevelText != null)
            {
                bestLevelText.text = "Best Level: " + ProgressionStore.GetBestLevel();
                StyleBodyText(bestLevelText, 48, true);
            }

            var audio = AudioDirector.EnsureInstance();
            audio.SetGameplayTrackIndex(ProgressionStore.GetGameplayMusicTrack(0, audio.GetGameplayTrackCount()), false);
            audio.RefreshMasterVolume();
            audio.SetMusicCue(AudioMusicCue.MainMenu, true);
            RefreshMusicTrackLabel();
        }

        private void Update()
        {
            if (_isStartingGame)
            {
                return;
            }

            AnimateMenu(Time.unscaledTime);
        }

        public void Play()
        {
            if (_isStartingGame)
            {
                return;
            }

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.78f, 1.04f);
            HapticsDirector.Instance?.Play(HapticCue.LightTap);
            ProgressionStore.SetDifficultyMode(_selectedDifficulty);
            StartCoroutine(PlayTransitionAndLoad());
        }

        private void CacheMenuElements()
        {
            if (bestLevelText != null)
            {
                _safeAreaRoot = bestLevelText.transform.parent as RectTransform;
            }

            if (_safeAreaRoot == null)
            {
                var safeArea = GameObject.Find("SafeArea");
                if (safeArea != null)
                {
                    _safeAreaRoot = safeArea.GetComponent<RectTransform>();
                }
            }

            var title = GameObject.Find("Title");
            if (title != null)
            {
                _titleRect = title.GetComponent<RectTransform>();
                _titleText = title.GetComponent<Text>();
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

            _safeAreaRoot = backgroundRect.parent as RectTransform;
            backgroundRect.SetAsFirstSibling();

            var image = background.GetComponent<Image>();
            if (image != null)
            {
                image.color = backgroundBottomColor;
            }
        }

        private void EnsureRuntimePolish()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = "MULTIPLY RUSH";
                StyleHeadline(_titleText, 108);
            }

            if (_playButtonRect != null)
            {
                _playButtonRect.sizeDelta = new Vector2(520f, 150f);
            }

            if (_playButtonImage != null)
            {
                _playButtonImage.color = playButtonColor;
                var outline = _playButtonImage.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = _playButtonImage.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
                outline.effectDistance = new Vector2(2.2f, -2.2f);
                _buttonBaseColor = _playButtonImage.color;
            }

            if (_playButtonRect != null)
            {
                var playLabel = FindOrCreateText(
                    _playButtonRect,
                    "Label",
                    "PLAY NOW",
                    58,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                StyleBodyText(playLabel, 58, true);
                if (playLabel != null)
                {
                    playLabel.color = Color.white;
                }

                var shine = FindOrCreateImage(
                    _playButtonRect,
                    "Shine",
                    new Color(1f, 1f, 1f, 0.22f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(100f, 0f));
                if (shine != null)
                {
                    _playShineRect = shine.rectTransform;
                    _playShineRect.pivot = new Vector2(0.5f, 0.5f);
                    _playShineRect.localRotation = Quaternion.Euler(0f, 0f, 14f);
                    _playShineRect.SetAsLastSibling();
                }
            }

            _leftGlowRect = FindOrCreateImage(
                _safeAreaRoot,
                "MenuGlowLeft",
                new Color(0.13f, 0.45f, 0.95f, 0.22f),
                new Vector2(0.2f, 0.35f),
                new Vector2(0.2f, 0.35f),
                Vector2.zero,
                new Vector2(560f, 560f)).rectTransform;

            _rightGlowRect = FindOrCreateImage(
                _safeAreaRoot,
                "MenuGlowRight",
                new Color(0.07f, 0.93f, 0.98f, 0.2f),
                new Vector2(0.82f, 0.73f),
                new Vector2(0.82f, 0.73f),
                Vector2.zero,
                new Vector2(420f, 420f)).rectTransform;

            _scanlineRect = FindOrCreateImage(
                _safeAreaRoot,
                "Scanline",
                new Color(0.5f, 0.95f, 1f, 0.14f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1200f, 58f)).rectTransform;
            _scanlineRect.SetAsLastSibling();
            _scanlineBaseY = 0f;

            _badgeRect = FindOrCreateImage(
                _safeAreaRoot,
                "NoAdsBadge",
                new Color(0.1f, 0.23f, 0.43f, 0.9f),
                new Vector2(0.5f, 0.93f),
                new Vector2(0.5f, 0.93f),
                Vector2.zero,
                new Vector2(560f, 72f)).rectTransform;
            _badgeText = FindOrCreateText(
                _badgeRect,
                "Label",
                "100% OFFLINE  |  ZERO ADS  |  INFINITE LEVELS",
                24,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            StyleBodyText(_badgeText, 24, true);
            _badgeText.color = new Color(0.84f, 0.97f, 1f, 1f);

            _taglineRect = FindOrCreateText(
                _safeAreaRoot,
                "Tagline",
                "Swipe, multiply, and overwhelm the enemy crowd.",
                34,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.69f),
                new Vector2(0.5f, 0.69f),
                Vector2.zero,
                new Vector2(960f, 78f)).rectTransform;
            _taglineText = _taglineRect.GetComponent<Text>();
            StyleBodyText(_taglineText, 34, false);
            _taglineText.color = new Color(0.86f, 0.93f, 1f, 0.96f);

            var footerText = FindOrCreateText(
                _safeAreaRoot,
                "FooterText",
                "No boosts. No timers. Just pure crowd-run chaos.",
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.12f),
                new Vector2(0.5f, 0.12f),
                Vector2.zero,
                new Vector2(900f, 56f));
            StyleBodyText(footerText, 24, false);
            footerText.color = new Color(0.72f, 0.83f, 0.95f, 0.86f);

            _leftGlowRect.SetAsFirstSibling();
            _rightGlowRect.SetAsFirstSibling();
            _scanlineRect.SetAsFirstSibling();
            EnsureTransitionFlash();
        }

        private IEnumerator PlayTransitionAndLoad()
        {
            _isStartingGame = true;
            AudioDirector.Instance?.PlaySfx(AudioSfxCue.PlayTransition, 0.9f, 1f);

            var playButton = _playButtonRect != null ? _playButtonRect.GetComponent<Button>() : null;
            if (playButton != null)
            {
                playButton.interactable = false;
            }

            EnsureTransitionFlash();
            var flashRect = _transitionFlashImage != null ? _transitionFlashImage.rectTransform : null;
            var startScale = _buttonBaseScale;
            var startButtonPosition = _buttonBasePosition;
            var startTitlePosition = _titleRect != null ? _titleRect.anchoredPosition : Vector2.zero;
            var duration = Mathf.Max(0.12f, playTransitionDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);

                if (_transitionFlashImage != null)
                {
                    _transitionFlashImage.color = new Color(
                        transitionFlashColor.r,
                        transitionFlashColor.g,
                        transitionFlashColor.b,
                        Mathf.Lerp(0f, 0.94f, eased));
                }

                if (flashRect != null)
                {
                    var scale = Mathf.Lerp(1.02f, 1.2f, eased);
                    flashRect.localScale = new Vector3(scale, scale, 1f);
                }

                if (_playButtonRect != null)
                {
                    _playButtonRect.localScale = Vector3.Lerp(startScale, startScale * playZoomScale, eased);
                    _playButtonRect.anchoredPosition = Vector2.Lerp(
                        startButtonPosition,
                        startButtonPosition + new Vector2(0f, 26f),
                        eased);
                }

                if (_titleRect != null)
                {
                    _titleRect.anchoredPosition = Vector2.Lerp(startTitlePosition, startTitlePosition + new Vector2(0f, 28f), eased);
                }

                yield return null;
            }

            AudioDirector.Instance?.SetMusicCue(AudioMusicCue.Gameplay, true);
            SceneManager.LoadScene(gameSceneName);
        }

        private void AnimateMenu(float runTime)
        {
            var colorWave = 0.5f + Mathf.Sin(runTime * 0.45f) * 0.5f;

            if (_titleRect != null)
            {
                var titlePulse = 1f + Mathf.Sin(runTime * titlePulseSpeed) * titlePulseScale;
                _titleRect.localScale = _titleBaseScale * titlePulse;
                _titleRect.anchoredPosition = new Vector2(0f, Mathf.Sin(runTime * 0.85f) * 6f);
            }

            if (_playButtonRect != null)
            {
                var floatWave = Mathf.Sin(runTime * buttonFloatSpeed);
                _playButtonRect.anchoredPosition = _buttonBasePosition + new Vector2(0f, floatWave * buttonFloatDistance);
                var pulse = 1f + Mathf.Abs(floatWave) * buttonPulseScale;
                _playButtonRect.localScale = _buttonBaseScale * pulse;

                if (_playShineRect != null)
                {
                    var width = _playButtonRect.sizeDelta.x + 140f;
                    var x = Mathf.PingPong(runTime * 320f, width) - width * 0.5f;
                    _playShineRect.anchoredPosition = new Vector2(x, 0f);
                }

                if (_difficultyRow != null)
                {
                    var rowPosition = _buttonBasePosition + new Vector2(0f, 140f + Mathf.Sin(runTime * 1.7f) * 5f);
                    _difficultyRow.anchoredPosition = rowPosition;
                }

                if (_musicRow != null)
                {
                    var rowPosition = _buttonBasePosition + new Vector2(0f, 218f + Mathf.Sin(runTime * 1.5f + 0.4f) * 5f);
                    _musicRow.anchoredPosition = rowPosition;
                }
            }

            if (_playButtonImage != null)
            {
                var glow = 0.86f + Mathf.Abs(Mathf.Sin(runTime * buttonFloatSpeed)) * 0.14f;
                _playButtonImage.color = new Color(
                    _buttonBaseColor.r * glow,
                    _buttonBaseColor.g * glow,
                    _buttonBaseColor.b * glow,
                    _buttonBaseColor.a);
            }

            if (_leftGlowRect != null)
            {
                _leftGlowRect.anchoredPosition = new Vector2(
                    Mathf.Sin(runTime * glowDriftSpeed) * 170f,
                    -60f + Mathf.Cos(runTime * glowDriftSpeed * 0.7f) * 72f);
                var scale = 1f + Mathf.Sin(runTime * 0.9f) * 0.06f;
                _leftGlowRect.localScale = new Vector3(scale, scale, 1f);
            }

            if (_rightGlowRect != null)
            {
                _rightGlowRect.anchoredPosition = new Vector2(
                    Mathf.Cos(runTime * glowDriftSpeed * 1.2f) * 150f,
                    Mathf.Sin(runTime * glowDriftSpeed * 0.8f) * 90f);
                var scale = 1f + Mathf.Cos(runTime * 1.1f) * 0.08f;
                _rightGlowRect.localScale = new Vector3(scale, scale, 1f);
            }

            if (_scanlineRect != null)
            {
                var y = Mathf.PingPong(runTime * scanlineSpeed, 1040f) - 520f;
                _scanlineRect.anchoredPosition = new Vector2(0f, _scanlineBaseY + y);
                var alpha = 0.08f + Mathf.Sin(runTime * 2.4f) * 0.05f;
                var image = _scanlineRect.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.52f, 0.95f, 1f, Mathf.Clamp(alpha, 0.03f, 0.16f));
                }
            }

            if (_badgeRect != null)
            {
                _badgeRect.localScale = Vector3.one * (1f + Mathf.Sin(runTime * 2f) * 0.025f);
            }

            if (_taglineRect != null)
            {
                _taglineRect.anchoredPosition = new Vector2(0f, Mathf.Sin(runTime * 1.35f) * 5f);
            }

            if (_titleText != null)
            {
                var titleColor = Color.Lerp(new Color(0.86f, 0.95f, 1f, 1f), neonAccentColor, colorWave * 0.24f);
                _titleText.color = titleColor;
            }
        }

        private void EnsureDifficultySelector()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var rowTransform = _safeAreaRoot.Find("DifficultyRow");
            if (rowTransform == null)
            {
                var rowObject = new GameObject("DifficultyRow");
                rowObject.transform.SetParent(_safeAreaRoot, false);
                _difficultyRow = rowObject.AddComponent<RectTransform>();
                _difficultyRow.anchorMin = new Vector2(0.5f, 0.5f);
                _difficultyRow.anchorMax = new Vector2(0.5f, 0.5f);
                _difficultyRow.pivot = new Vector2(0.5f, 0.5f);
                _difficultyRow.sizeDelta = new Vector2(560f, 140f);
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
                _difficultyRow.anchoredPosition = _buttonBasePosition + new Vector2(0f, 140f);
            }

            _difficultyLabel = EnsureDifficultyText(
                _difficultyRow,
                "DifficultyLabel",
                "Difficulty",
                new Vector2(0f, 48f),
                28);

            _easyButton = EnsureDifficultyButton(_difficultyRow, "EasyButton", "Easy", new Vector2(-186f, -2f), out _easyLabel);
            _normalButton = EnsureDifficultyButton(_difficultyRow, "NormalButton", "Normal", new Vector2(0f, -2f), out _normalLabel);
            _hardButton = EnsureDifficultyButton(_difficultyRow, "HardButton", "Hard", new Vector2(186f, -2f), out _hardLabel);

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

        private void EnsureMusicSelector()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var rowTransform = _safeAreaRoot.Find("MusicRow");
            if (rowTransform == null)
            {
                var rowObject = new GameObject("MusicRow");
                rowObject.transform.SetParent(_safeAreaRoot, false);
                _musicRow = rowObject.AddComponent<RectTransform>();
                _musicRow.anchorMin = new Vector2(0.5f, 0.5f);
                _musicRow.anchorMax = new Vector2(0.5f, 0.5f);
                _musicRow.pivot = new Vector2(0.5f, 0.5f);
                _musicRow.sizeDelta = new Vector2(640f, 128f);
            }
            else
            {
                _musicRow = rowTransform.GetComponent<RectTransform>();
            }

            if (_musicRow == null)
            {
                return;
            }

            if (_playButtonRect != null)
            {
                _musicRow.anchoredPosition = _buttonBasePosition + new Vector2(0f, 218f);
            }

            _musicLabel = EnsureDifficultyText(
                _musicRow,
                "MusicLabel",
                "Gameplay Track",
                new Vector2(0f, 44f),
                26);

            _musicTrackLabel = EnsureDifficultyText(
                _musicRow,
                "MusicTrackLabel",
                "Hyper Neon",
                new Vector2(0f, -2f),
                30);
            if (_musicTrackLabel != null)
            {
                _musicTrackLabel.color = new Color(0.88f, 0.98f, 1f, 1f);
            }

            _musicPrevButton = EnsureMusicNavButton(_musicRow, "MusicPrevButton", "<", new Vector2(-258f, -2f));
            _musicNextButton = EnsureMusicNavButton(_musicRow, "MusicNextButton", ">", new Vector2(258f, -2f));

            if (_musicPrevButton != null)
            {
                _musicPrevButton.onClick.RemoveAllListeners();
                _musicPrevButton.onClick.AddListener(() => CycleMusicTrack(-1));
            }

            if (_musicNextButton != null)
            {
                _musicNextButton.onClick.RemoveAllListeners();
                _musicNextButton.onClick.AddListener(() => CycleMusicTrack(1));
            }
        }

        private void CycleMusicTrack(int delta)
        {
            var audio = AudioDirector.Instance ?? AudioDirector.EnsureInstance();
            var trackCount = Mathf.Max(1, audio.GetGameplayTrackCount());
            var current = ProgressionStore.GetGameplayMusicTrack(0, trackCount);
            var next = (current + delta) % trackCount;
            if (next < 0)
            {
                next += trackCount;
            }

            audio.SetGameplayTrackIndex(next, false);
            AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.64f, 1.06f);
            HapticsDirector.Instance?.Play(HapticCue.LightTap);
            RefreshMusicTrackLabel();
        }

        private void RefreshMusicTrackLabel()
        {
            if (_musicTrackLabel == null)
            {
                return;
            }

            var audio = AudioDirector.Instance;
            if (audio == null)
            {
                _musicTrackLabel.text = "Track";
                return;
            }

            var index = ProgressionStore.GetGameplayMusicTrack(0, audio.GetGameplayTrackCount());
            var trackName = audio.GetGameplayTrackName(index);
            _musicTrackLabel.text = "#" + (index + 1) + "  " + trackName;
        }

        private void SelectDifficulty(DifficultyMode mode)
        {
            if (_selectedDifficulty == mode)
            {
                return;
            }

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.66f, 1.08f);
            HapticsDirector.Instance?.Play(HapticCue.LightTap);
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
            rect.sizeDelta = new Vector2(400f, 36f);

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

            var outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
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
            rect.sizeDelta = new Vector2(164f, 52f);

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
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = labelText;
            label.color = Color.white;

            var outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return button;
        }

        private static Button EnsureMusicNavButton(RectTransform parent, string objectName, string labelText, Vector2 anchoredPosition)
        {
            Text label;
            var button = EnsureDifficultyButton(parent, objectName, labelText, anchoredPosition, out label);
            var rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(92f, 52f);
            }

            if (label != null)
            {
                label.fontSize = 34;
                label.fontStyle = FontStyle.Bold;
            }

            return button;
        }

        private static Image FindOrCreateImage(
            RectTransform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find(name);
            GameObject imageObject;
            if (existing == null)
            {
                imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(parent, false);
            }
            else
            {
                imageObject = existing.gameObject;
                if (imageObject.GetComponent<Image>() == null)
                {
                    imageObject.AddComponent<Image>();
                }
            }

            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private void EnsureTransitionFlash()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var flash = FindOrCreateImage(
                _safeAreaRoot,
                "TransitionFlash",
                new Color(transitionFlashColor.r, transitionFlashColor.g, transitionFlashColor.b, 0f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            if (flash == null)
            {
                return;
            }

            var rect = flash.rectTransform;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            flash.raycastTarget = false;
            flash.transform.SetAsLastSibling();
            _transitionFlashImage = flash;
        }

        private static Text FindOrCreateText(
            RectTransform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find(name);
            GameObject textObject;
            if (existing == null)
            {
                textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
            }
            else
            {
                textObject = existing.gameObject;
                if (textObject.GetComponent<Text>() == null)
                {
                    textObject.AddComponent<Text>();
                }
            }

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = value;
            return label;
        }

        private static void StyleHeadline(Text label, int fontSize)
        {
            if (label == null)
            {
                return;
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.86f);
            outline.effectDistance = new Vector2(2.2f, -2.2f);

            var shadow = label.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = label.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0.15f, 0.74f, 1f, 0.38f);
            shadow.effectDistance = new Vector2(0f, -6f);
        }

        private static void StyleBodyText(Text label, int fontSize, bool bold)
        {
            if (label == null)
            {
                return;
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

            var outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
