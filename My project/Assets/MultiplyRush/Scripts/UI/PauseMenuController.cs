using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("Flow")]
        public string mainMenuSceneName = "MainMenu";
        public bool allowKeyboardToggleInEditor = true;
        public float overlayFadeSpeed = 9f;
        public float panelHiddenScale = 0.9f;

        [Header("Visuals")]
        public Color accentColor = new Color(0.18f, 0.72f, 1f, 1f);
        public Color panelColor = new Color(0.05f, 0.09f, 0.18f, 0.96f);
        public Color dimColor = new Color(0f, 0f, 0f, 0.78f);
        public Color selectedQualityColor = new Color(0.23f, 0.77f, 1f, 1f);
        public Color unselectedQualityColor = new Color(0.17f, 0.24f, 0.34f, 1f);
        public bool animatePauseButton = false;
        public float pauseButtonPulseSpeed = 3.1f;
        public float pauseButtonPulseScale = 0.08f;

        private GameManager _gameManager;
        private LevelGenerator _levelGenerator;
        private CameraFollower _cameraFollower;
        private RectTransform _safeAreaRoot;
        private RectTransform _overlayRect;
        private RectTransform _panelRect;
        private CanvasGroup _overlayGroup;
        private Image _scanlineImage;
        private RectTransform _scanlineRect;
        private Button _pauseButton;
        private RectTransform _pauseButtonRect;
        private Image _pauseButtonImage;
        private Vector3 _pauseButtonBaseScale = Vector3.one;
        private Vector3 _panelBaseScale = Vector3.one;
        private Button _resumeButton;
        private Button _restartButton;
        private Button _mainMenuButton;
        private Slider _volumeSlider;
        private Text _volumeValueText;
        private Slider _cameraMotionSlider;
        private Text _cameraMotionValueText;
        private Text _qualityValueText;
        private Button _autoButton;
        private Button _lowButton;
        private Button _mediumButton;
        private Button _highButton;
        private bool _isPaused;
        private bool _canPause = true;
        private bool _isInitialized;
        private bool _suppressControlCallbacks;
        private float _overlayAlpha;
        private BackdropQuality _selectedQuality = BackdropQuality.Auto;

        public bool IsPaused => _isPaused;

        public void Initialize(
            GameManager gameManager,
            LevelGenerator levelGenerator,
            CameraFollower cameraFollower,
            RectTransform safeAreaRoot = null)
        {
            _gameManager = gameManager;
            _levelGenerator = levelGenerator;
            _cameraFollower = cameraFollower;

            if (safeAreaRoot != null)
            {
                _safeAreaRoot = safeAreaRoot;
            }
            else if (_safeAreaRoot == null)
            {
                _safeAreaRoot = ResolveSafeAreaRoot();
            }

            if (_safeAreaRoot == null)
            {
                return;
            }

            EnsureUiBuilt();
            HookListeners();
            ApplySavedSettings();
            ForceResume(true);
            _isInitialized = true;
        }

        public void SetPauseAvailable(bool canPause)
        {
            _canPause = canPause;
            if (!canPause)
            {
                ForceResume(true);
            }

            if (_pauseButton != null)
            {
                _pauseButton.gameObject.SetActive(canPause);
            }
        }

        public void ForceResume(bool instant = false)
        {
            SetPaused(false, instant);
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (allowKeyboardToggleInEditor && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePauseRequested();
            }
#endif

            AnimatePauseButton(Time.unscaledTime);
            AnimateOverlay(Time.unscaledDeltaTime, Time.unscaledTime);
        }

        private void OnDisable()
        {
            if (_isPaused)
            {
                ForceResume(true);
            }
        }

        private void OnDestroy()
        {
            if (_isPaused || Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        private void TogglePauseRequested()
        {
            if (!_canPause)
            {
                return;
            }

            SetPaused(!_isPaused, false);
        }

        private void SetPaused(bool paused, bool instant)
        {
            if (paused && !_canPause)
            {
                return;
            }

            if (_isPaused == paused && !instant)
            {
                return;
            }

            _isPaused = paused;

            if (_overlayRect != null && paused)
            {
                _overlayRect.gameObject.SetActive(true);
            }

            if (_overlayGroup != null)
            {
                _overlayGroup.interactable = paused;
                _overlayGroup.blocksRaycasts = paused;
            }

            if (_pauseButton != null)
            {
                _pauseButton.interactable = _canPause && !paused;
            }

            Time.timeScale = paused ? 0f : 1f;

            if (instant)
            {
                _overlayAlpha = paused ? 1f : 0f;
                ApplyOverlayVisuals(Time.unscaledTime);
                if (_overlayRect != null && !paused)
                {
                    _overlayRect.gameObject.SetActive(false);
                }
            }
        }

        private void AnimatePauseButton(float runTime)
        {
            if (_pauseButtonRect == null)
            {
                return;
            }

            if (!animatePauseButton)
            {
                _pauseButtonRect.localScale = _pauseButtonBaseScale;
                if (_pauseButtonImage != null)
                {
                    _pauseButtonImage.color = accentColor;
                }

                return;
            }

            if (!_canPause || _isPaused)
            {
                _pauseButtonRect.localScale = _pauseButtonBaseScale;
                if (_pauseButtonImage != null)
                {
                    _pauseButtonImage.color = accentColor;
                }
                return;
            }

            var pulse = 1f + Mathf.Sin(runTime * pauseButtonPulseSpeed) * pauseButtonPulseScale;
            _pauseButtonRect.localScale = _pauseButtonBaseScale * pulse;

            if (_pauseButtonImage != null)
            {
                var glow = 0.84f + Mathf.Abs(Mathf.Sin(runTime * 2.2f)) * 0.16f;
                _pauseButtonImage.color = accentColor * glow;
                _pauseButtonImage.color = new Color(_pauseButtonImage.color.r, _pauseButtonImage.color.g, _pauseButtonImage.color.b, 0.96f);
            }
        }

        private void AnimateOverlay(float deltaTime, float runTime)
        {
            if (_overlayGroup == null || _panelRect == null)
            {
                return;
            }

            if (deltaTime > 0f)
            {
                var target = _isPaused ? 1f : 0f;
                _overlayAlpha = Mathf.MoveTowards(_overlayAlpha, target, deltaTime * overlayFadeSpeed);
            }

            ApplyOverlayVisuals(runTime);

            if (!_isPaused && _overlayAlpha <= 0.001f && _overlayRect != null && _overlayRect.gameObject.activeSelf)
            {
                _overlayRect.gameObject.SetActive(false);
            }
        }

        private void ApplyOverlayVisuals(float runTime)
        {
            if (_overlayGroup != null)
            {
                _overlayGroup.alpha = _overlayAlpha;
            }

            if (_panelRect != null)
            {
                var eased = Mathf.SmoothStep(panelHiddenScale, 1f, _overlayAlpha);
                _panelRect.localScale = _panelBaseScale * eased;
            }

            if (_scanlineRect != null)
            {
                var y = Mathf.PingPong(runTime * 140f, 620f) - 310f;
                _scanlineRect.anchoredPosition = new Vector2(0f, y);
            }

            if (_scanlineImage != null)
            {
                var alpha = Mathf.Clamp(0.03f + Mathf.Sin(runTime * 2.4f) * 0.03f, 0.01f, 0.08f);
                _scanlineImage.color = new Color(0.62f, 0.94f, 1f, alpha * _overlayAlpha);
            }
        }

        private void HandleResumePressed()
        {
            SetPaused(false, false);
        }

        private void HandleRestartPressed()
        {
            SetPaused(false, true);
            if (_gameManager != null)
            {
                _gameManager.RetryCurrentLevelFromPauseMenu();
            }
        }

        private void HandleMainMenuPressed()
        {
            SetPaused(false, true);
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HandleVolumeChanged(float value)
        {
            if (_suppressControlCallbacks)
            {
                return;
            }

            var safeValue = Mathf.Clamp01(value);
            AudioListener.volume = safeValue;
            ProgressionStore.SetMasterVolume(safeValue);
            RefreshVolumeLabel(safeValue);
        }

        private void HandleCameraMotionChanged(float value)
        {
            if (_suppressControlCallbacks)
            {
                return;
            }

            var safeValue = Mathf.Clamp01(value);
            if (_cameraFollower != null)
            {
                _cameraFollower.SetMotionIntensity(safeValue);
            }

            ProgressionStore.SetCameraMotionIntensity(safeValue);
            RefreshCameraMotionLabel(safeValue);
        }

        private void SelectGraphicsQuality(BackdropQuality quality)
        {
            _selectedQuality = quality;
            ProgressionStore.SetGraphicsFidelity(_selectedQuality);
            if (_levelGenerator != null)
            {
                _levelGenerator.ApplyGraphicsFidelity(_selectedQuality, true);
            }

            RefreshQualityUi();
        }

        private void ApplySavedSettings()
        {
            var volume = ProgressionStore.GetMasterVolume(0.85f);
            var motion = ProgressionStore.GetCameraMotionIntensity(0.35f);
            _selectedQuality = ProgressionStore.GetGraphicsFidelity(BackdropQuality.Auto);

            AudioListener.volume = volume;

            if (_cameraFollower != null)
            {
                _cameraFollower.SetMotionIntensity(motion);
            }

            if (_levelGenerator != null)
            {
                _levelGenerator.ApplyGraphicsFidelity(_selectedQuality, false);
            }

            _suppressControlCallbacks = true;
            if (_volumeSlider != null)
            {
                _volumeSlider.value = volume;
            }

            if (_cameraMotionSlider != null)
            {
                _cameraMotionSlider.value = motion;
            }

            _suppressControlCallbacks = false;

            RefreshVolumeLabel(volume);
            RefreshCameraMotionLabel(motion);
            RefreshQualityUi();
        }

        private void RefreshVolumeLabel(float value)
        {
            if (_volumeValueText != null)
            {
                _volumeValueText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
            }
        }

        private void RefreshCameraMotionLabel(float value)
        {
            if (_cameraMotionValueText != null)
            {
                _cameraMotionValueText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
            }
        }

        private void RefreshQualityUi()
        {
            if (_qualityValueText != null)
            {
                _qualityValueText.text = "Graphics: " + _selectedQuality;
            }

            UpdateQualityButtonVisual(_autoButton, _selectedQuality == BackdropQuality.Auto);
            UpdateQualityButtonVisual(_lowButton, _selectedQuality == BackdropQuality.Low);
            UpdateQualityButtonVisual(_mediumButton, _selectedQuality == BackdropQuality.Medium);
            UpdateQualityButtonVisual(_highButton, _selectedQuality == BackdropQuality.High);
        }

        private void UpdateQualityButtonVisual(Button button, bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isSelected ? selectedQualityColor : unselectedQualityColor;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = isSelected ? Color.white : new Color(0.8f, 0.88f, 0.96f, 1f);
                label.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void HookListeners()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(TogglePauseRequested);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(HandleResumePressed);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(HandleRestartPressed);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveAllListeners();
                _mainMenuButton.onClick.AddListener(HandleMainMenuPressed);
            }

            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.RemoveAllListeners();
                _volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
            }

            if (_cameraMotionSlider != null)
            {
                _cameraMotionSlider.onValueChanged.RemoveAllListeners();
                _cameraMotionSlider.onValueChanged.AddListener(HandleCameraMotionChanged);
            }

            if (_autoButton != null)
            {
                _autoButton.onClick.RemoveAllListeners();
                _autoButton.onClick.AddListener(() => SelectGraphicsQuality(BackdropQuality.Auto));
            }

            if (_lowButton != null)
            {
                _lowButton.onClick.RemoveAllListeners();
                _lowButton.onClick.AddListener(() => SelectGraphicsQuality(BackdropQuality.Low));
            }

            if (_mediumButton != null)
            {
                _mediumButton.onClick.RemoveAllListeners();
                _mediumButton.onClick.AddListener(() => SelectGraphicsQuality(BackdropQuality.Medium));
            }

            if (_highButton != null)
            {
                _highButton.onClick.RemoveAllListeners();
                _highButton.onClick.AddListener(() => SelectGraphicsQuality(BackdropQuality.High));
            }
        }

        private RectTransform ResolveSafeAreaRoot()
        {
            var safeArea = GameObject.Find("SafeArea");
            if (safeArea != null)
            {
                return safeArea.GetComponent<RectTransform>();
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            return canvas.transform as RectTransform;
        }

        private void EnsureUiBuilt()
        {
            EnsurePauseButton();
            EnsureOverlay();
        }

        private void EnsurePauseButton()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var pauseTransform = _safeAreaRoot.Find("PauseButton");
            GameObject pauseObject;
            if (pauseTransform == null)
            {
                pauseObject = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                pauseObject.transform.SetParent(_safeAreaRoot, false);
            }
            else
            {
                pauseObject = pauseTransform.gameObject;
                if (pauseObject.GetComponent<Image>() == null)
                {
                    pauseObject.AddComponent<Image>();
                }

                if (pauseObject.GetComponent<Button>() == null)
                {
                    pauseObject.AddComponent<Button>();
                }
            }

            _pauseButtonRect = pauseObject.GetComponent<RectTransform>();
            _pauseButtonRect.anchorMin = new Vector2(1f, 1f);
            _pauseButtonRect.anchorMax = new Vector2(1f, 1f);
            _pauseButtonRect.pivot = new Vector2(1f, 1f);
            _pauseButtonRect.anchoredPosition = new Vector2(-28f, -30f);
            _pauseButtonRect.sizeDelta = new Vector2(108f, 108f);
            _pauseButtonBaseScale = Vector3.one;

            _pauseButtonImage = pauseObject.GetComponent<Image>();
            _pauseButtonImage.color = accentColor;
            var buttonOutline = _pauseButtonImage.GetComponent<Outline>();
            if (buttonOutline == null)
            {
                buttonOutline = _pauseButtonImage.gameObject.AddComponent<Outline>();
            }

            buttonOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            buttonOutline.effectDistance = new Vector2(2f, -2f);

            _pauseButton = pauseObject.GetComponent<Button>();

            var icon = EnsureText(
                _pauseButtonRect,
                "Icon",
                "II",
                58,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);
            icon.color = Color.white;
            icon.raycastTarget = false;
        }

        private void EnsureOverlay()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var overlayTransform = _safeAreaRoot.Find("PauseOverlay");
            GameObject overlayObject;
            if (overlayTransform == null)
            {
                overlayObject = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasGroup));
                overlayObject.transform.SetParent(_safeAreaRoot, false);
            }
            else
            {
                overlayObject = overlayTransform.gameObject;
                if (overlayObject.GetComponent<CanvasGroup>() == null)
                {
                    overlayObject.AddComponent<CanvasGroup>();
                }
            }

            _overlayRect = overlayObject.GetComponent<RectTransform>();
            Stretch(_overlayRect);
            _overlayGroup = overlayObject.GetComponent<CanvasGroup>();
            _overlayGroup.alpha = 0f;
            _overlayGroup.interactable = false;
            _overlayGroup.blocksRaycasts = false;

            var dim = EnsureImage(
                _overlayRect,
                "Dim",
                dimColor,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Stretch(dim.rectTransform);

            var panelImage = EnsureImage(
                _overlayRect,
                "Panel",
                panelColor,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(840f, 1260f));
            _panelRect = panelImage.rectTransform;
            _panelBaseScale = Vector3.one;

            var panelOutline = panelImage.GetComponent<Outline>();
            if (panelOutline == null)
            {
                panelOutline = panelImage.gameObject.AddComponent<Outline>();
            }

            panelOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            panelOutline.effectDistance = new Vector2(2.2f, -2.2f);

            var title = EnsureText(
                _panelRect,
                "Title",
                "PAUSED",
                98,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -100f),
                new Vector2(640f, 130f),
                true);
            title.color = new Color(0.82f, 0.93f, 1f, 1f);

            var subtitle = EnsureText(
                _panelRect,
                "Subtitle",
                "Tune your run before diving back in.",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -168f),
                new Vector2(680f, 64f),
                false);
            subtitle.color = new Color(0.76f, 0.86f, 0.96f, 0.94f);

            _resumeButton = EnsureActionButton(_panelRect, "ResumeButton", "RESUME", new Vector2(0f, 390f), accentColor);
            _restartButton = EnsureActionButton(_panelRect, "RestartButton", "RESTART LEVEL", new Vector2(0f, 270f), new Color(0.25f, 0.62f, 0.95f, 1f));
            _mainMenuButton = EnsureActionButton(_panelRect, "MainMenuButton", "MAIN MENU", new Vector2(0f, 150f), new Color(0.2f, 0.3f, 0.46f, 1f));

            var optionsCard = EnsureImage(
                _panelRect,
                "OptionsCard",
                new Color(0.09f, 0.14f, 0.24f, 0.96f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -178f),
                new Vector2(740f, 540f)).rectTransform;

            var optionsOutline = optionsCard.GetComponent<Outline>();
            if (optionsOutline == null)
            {
                optionsOutline = optionsCard.gameObject.AddComponent<Outline>();
            }

            optionsOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            optionsOutline.effectDistance = new Vector2(1.8f, -1.8f);

            EnsureText(
                optionsCard,
                "OptionsTitle",
                "OPTIONS",
                40,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -44f),
                new Vector2(340f, 56f),
                true).color = new Color(0.86f, 0.95f, 1f, 1f);

            EnsureText(
                optionsCard,
                "VolumeLabel",
                "Master Volume",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -110f),
                new Vector2(320f, 48f),
                false).color = new Color(0.82f, 0.9f, 0.98f, 1f);

            _volumeSlider = EnsureSlider(optionsCard, "VolumeSlider", new Vector2(0f, 140f), new Vector2(520f, 52f), new Color(0.22f, 0.76f, 1f, 1f));
            _volumeValueText = EnsureText(
                optionsCard,
                "VolumeValue",
                "100%",
                30,
                TextAnchor.MiddleRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -110f),
                new Vector2(140f, 48f),
                true);
            _volumeValueText.color = Color.white;

            EnsureText(
                optionsCard,
                "CameraMotionLabel",
                "Camera Motion",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -196f),
                new Vector2(320f, 48f),
                false).color = new Color(0.82f, 0.9f, 0.98f, 1f);

            _cameraMotionSlider = EnsureSlider(optionsCard, "CameraMotionSlider", new Vector2(0f, 54f), new Vector2(520f, 52f), new Color(0.38f, 0.88f, 0.78f, 1f));
            _cameraMotionValueText = EnsureText(
                optionsCard,
                "CameraMotionValue",
                "35%",
                30,
                TextAnchor.MiddleRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -196f),
                new Vector2(140f, 48f),
                true);
            _cameraMotionValueText.color = Color.white;

            _qualityValueText = EnsureText(
                optionsCard,
                "GraphicsLabel",
                "Graphics: Auto",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -282f),
                new Vector2(420f, 48f),
                false);
            _qualityValueText.color = new Color(0.82f, 0.9f, 0.98f, 1f);

            var row = EnsureRect(optionsCard, "GraphicsRow", new Vector2(0f, -126f), new Vector2(664f, 68f));
            _autoButton = EnsureQualityButton(row, "AutoQualityButton", "Auto", new Vector2(-252f, 0f));
            _lowButton = EnsureQualityButton(row, "LowQualityButton", "Low", new Vector2(-84f, 0f));
            _mediumButton = EnsureQualityButton(row, "MediumQualityButton", "Medium", new Vector2(84f, 0f));
            _highButton = EnsureQualityButton(row, "HighQualityButton", "High", new Vector2(252f, 0f));

            EnsureText(
                optionsCard,
                "Hint",
                "Lower graphics and camera motion for older phones.",
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 30f),
                new Vector2(660f, 42f),
                false).color = new Color(0.74f, 0.84f, 0.95f, 0.95f);

            _scanlineImage = EnsureImage(
                _panelRect,
                "Scanline",
                new Color(0.62f, 0.94f, 1f, 0.05f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(780f, 40f));
            _scanlineRect = _scanlineImage.rectTransform;
            _scanlineRect.SetAsLastSibling();

            _overlayRect.gameObject.SetActive(false);
        }

        private Button EnsureActionButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, Color color)
        {
            var buttonRect = EnsureRect(parent, name, anchoredPosition, new Vector2(560f, 92f));
            var image = buttonRect.GetComponent<Image>();
            if (image == null)
            {
                image = buttonRect.gameObject.AddComponent<Image>();
            }

            image.color = color;
            var outline = image.GetComponent<Outline>();
            if (outline == null)
            {
                outline = image.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.44f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            var button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
            button.colors = colors;

            var buttonLabel = EnsureText(
                buttonRect,
                "Label",
                label,
                38,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);
            buttonLabel.color = Color.white;

            return button;
        }

        private Slider EnsureSlider(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor)
        {
            var sliderRect = EnsureRect(parent, name, anchoredPosition, size);
            var track = EnsureImage(sliderRect, "Track", new Color(0.14f, 0.2f, 0.32f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(track.rectTransform);

            var fillArea = EnsureRect(sliderRect, "Fill Area", Vector2.zero, Vector2.zero);
            fillArea.anchorMin = new Vector2(0f, 0f);
            fillArea.anchorMax = new Vector2(1f, 1f);
            fillArea.offsetMin = new Vector2(12f, 12f);
            fillArea.offsetMax = new Vector2(-12f, -12f);

            var fill = EnsureImage(fillArea, "Fill", fillColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(fill.rectTransform);

            var handleArea = EnsureRect(sliderRect, "Handle Slide Area", Vector2.zero, Vector2.zero);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(12f, 2f);
            handleArea.offsetMax = new Vector2(-12f, -2f);

            var handle = EnsureImage(handleArea, "Handle", new Color(0.9f, 0.96f, 1f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 50f));

            var slider = sliderRect.GetComponent<Slider>();
            if (slider == null)
            {
                slider = sliderRect.gameObject.AddComponent<Slider>();
            }

            slider.transition = Selectable.Transition.ColorTint;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            var colors = slider.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.97f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.86f, 0.95f, 1f);
            slider.colors = colors;
            return slider;
        }

        private Button EnsureQualityButton(RectTransform parent, string name, string label, Vector2 anchoredPosition)
        {
            var buttonRect = EnsureRect(parent, name, anchoredPosition, new Vector2(152f, 56f));
            var image = buttonRect.GetComponent<Image>();
            if (image == null)
            {
                image = buttonRect.gameObject.AddComponent<Image>();
            }

            var button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
            button.colors = colors;

            var buttonLabel = EnsureText(
                buttonRect,
                "Label",
                label,
                26,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);
            buttonLabel.color = Color.white;

            return button;
        }

        private static RectTransform EnsureRect(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            GameObject rectObject;
            if (existing == null)
            {
                rectObject = new GameObject(name, typeof(RectTransform));
                rectObject.transform.SetParent(parent, false);
            }
            else
            {
                rectObject = existing.gameObject;
                if (rectObject.GetComponent<RectTransform>() == null)
                {
                    rectObject.AddComponent<RectTransform>();
                }
            }

            var rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image EnsureImage(
            RectTransform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
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
            rect.sizeDelta = size;

            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text EnsureText(
            RectTransform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            bool bold)
        {
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
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
