using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

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
        public float backdropStripDriftSpeed = 90f;
        public float backdropStarDriftSpeed = 46f;
        public float backdropCometDriftSpeed = 170f;
        public float backdropRingPulseSpeed = 0.72f;
        public float backdropWaveSweepSpeed = 0.54f;
        public float backdropParticleOrbitSpeed = 0.68f;

        [Header("Palette")]
        public Color backgroundTopColor = new Color(0.03f, 0.09f, 0.2f, 1f);
        public Color backgroundBottomColor = new Color(0.01f, 0.02f, 0.08f, 1f);
        public Color playButtonColor = new Color(0.1f, 0.68f, 1f, 1f);
        public Color neonAccentColor = new Color(0.3f, 0.94f, 1f, 1f);

        [Header("Background Video")]
        public bool enableVideoBackground = true;
        public VideoClip menuBackgroundClip;
        [Range(0.15f, 1f)] public float menuVideoBrightness = 0.68f;
        [Range(0.5f, 1.2f)] public float menuVideoPlaybackSpeed = 1f;
        public bool muteMenuVideo = true;

        private RectTransform _safeAreaRoot;
        private RectTransform _titleRect;
        private RectTransform _playButtonRect;
        private RectTransform _bestLevelRect;
        private RectTransform _metaCardRect;
        private RectTransform _footerRect;
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
        private RectTransform _musicTrackPlateRect;
        private Text _musicLabel;
        private Text _musicTrackLabel;
        private Button _musicPrevButton;
        private Button _musicNextButton;
        private RectTransform _leftGlowRect;
        private RectTransform _rightGlowRect;
        private RectTransform _scanlineRect;
        private RectTransform _playShineRect;
        private RectTransform _badgeRect;
        private RectTransform _studioRect;
        private RectTransform _taglineRect;
        private Button _progressButton;
        private RectTransform _progressOverlayRect;
        private CanvasGroup _progressOverlayGroup;
        private RectTransform _progressPanelRect;
        private ScrollRect _progressScrollRect;
        private RectTransform _progressViewportRect;
        private RectTransform _progressContentRect;
        private Button _progressCloseButton;
        private Text _progressSummaryText;
        private readonly List<RectTransform> _progressRowRects = new List<RectTransform>(48);
        private readonly List<Text> _progressLevelTexts = new List<Text>(48);
        private readonly List<Text> _progressScoreTexts = new List<Text>(48);
        private readonly List<Button> _progressReplayButtons = new List<Button>(48);
        private int _requestedReplayLevel = -1;
        private RectTransform _backdropLayerRect;
        private RectTransform _backdropNebulaARect;
        private RectTransform _backdropNebulaBRect;
        private RectTransform _backdropHorizonRect;
        private RectTransform _backdropAuroraARect;
        private RectTransform _backdropAuroraBRect;
        private readonly List<RectTransform> _backdropStripRects = new List<RectTransform>(16);
        private readonly List<float> _backdropStripSpeeds = new List<float>(16);
        private readonly List<float> _backdropStripPhases = new List<float>(16);
        private readonly List<float> _backdropStripBaseX = new List<float>(16);
        private readonly List<RectTransform> _backdropStarRects = new List<RectTransform>(40);
        private readonly List<float> _backdropStarSpeeds = new List<float>(40);
        private readonly List<float> _backdropStarPhases = new List<float>(40);
        private readonly List<float> _backdropStarBaseX = new List<float>(40);
        private readonly List<RectTransform> _backdropCometRects = new List<RectTransform>(8);
        private readonly List<float> _backdropCometSpeeds = new List<float>(8);
        private readonly List<float> _backdropCometPhases = new List<float>(8);
        private readonly List<float> _backdropCometAmplitudes = new List<float>(8);
        private readonly List<RectTransform> _backdropPulseRingRects = new List<RectTransform>(6);
        private readonly List<float> _backdropPulseRingSpeeds = new List<float>(6);
        private readonly List<float> _backdropPulseRingPhases = new List<float>(6);
        private readonly List<Vector2> _backdropPulseRingBasePositions = new List<Vector2>(6);
        private readonly List<RectTransform> _backdropWaveRects = new List<RectTransform>(6);
        private readonly List<float> _backdropWaveSpeeds = new List<float>(6);
        private readonly List<float> _backdropWavePhases = new List<float>(6);
        private readonly List<float> _backdropWaveBaseY = new List<float>(6);
        private readonly List<RectTransform> _backdropOrbitParticleRects = new List<RectTransform>(12);
        private readonly List<Vector2> _backdropOrbitCenters = new List<Vector2>(12);
        private readonly List<float> _backdropOrbitRadii = new List<float>(12);
        private readonly List<float> _backdropOrbitSpeeds = new List<float>(12);
        private readonly List<float> _backdropOrbitPhases = new List<float>(12);
        private Sprite _softOrbSprite;
        private Sprite _starOrbSprite;
        private Texture2D _softOrbTexture;
        private Texture2D _starOrbTexture;
        private Image _metaCardImage;
        private Text _titleText;
        private Text _taglineText;
        private Text _studioText;
        private Text _footerText;
        private Text _playButtonLabel;
        private float _scanlineBaseY;
        private Image _transitionFlashImage;
        private RawImage _videoBackground;
        private VideoPlayer _menuVideoPlayer;
        private RenderTexture _menuVideoRenderTexture;
        private bool _isStartingGame;
        private Vector2 _lastSafeAreaSize = new Vector2(-1f, -1f);
        private Vector2 _titleBasePosition;
        private Vector2 _taglineBasePosition;
        private float _nextMusicLabelRefreshTime;

        private void Start()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            SceneVisualTuning.ApplyMenuLook();
            CacheMenuElements();
            EnsureBackgroundBehindContent();
            EnsureSafeAreaFitter();
            EnsureRuntimePolish();

            _selectedDifficulty = ProgressionStore.GetDifficultyMode(defaultDifficulty);
            EnsureDifficultySelector();
            EnsureMusicSelector();
            RefreshResponsiveLayout(true);
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

            UpdateVideoBackgroundState();
            if (Time.unscaledTime >= _nextMusicLabelRefreshTime)
            {
                RefreshMusicTrackLabel();
                _nextMusicLabelRefreshTime = Time.unscaledTime + 0.4f;
            }
            RefreshResponsiveLayout();
            AnimateMenu(Time.unscaledTime);
        }

        public void Play()
        {
            if (_isStartingGame)
            {
                return;
            }

            if (_requestedReplayLevel > 0)
            {
                ProgressionStore.SetRequestedStartLevel(_requestedReplayLevel);
                _requestedReplayLevel = -1;
            }
            else
            {
                ProgressionStore.ClearRequestedStartLevel();
            }

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.78f, 1.04f);
            AudioDirector.Instance?.StopGameplayPreview();
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
                    _titleBasePosition = _titleRect.anchoredPosition;
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
            EnsureVideoBackground(_safeAreaRoot != null ? _safeAreaRoot.parent as RectTransform : null);
            backgroundRect.SetAsFirstSibling();

            var image = background.GetComponent<Image>();
            if (image != null)
            {
                var hasVideo = enableVideoBackground && menuBackgroundClip != null;
                var tint = Color.Lerp(backgroundBottomColor, backgroundTopColor, 0.45f);
                tint.a = hasVideo ? 0.56f : 0.78f;
                image.color = tint;
            }
        }

        private void EnsureSafeAreaFitter()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var fitter = _safeAreaRoot.GetComponent<SafeAreaFitter>();
            if (fitter == null)
            {
                fitter = _safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();
            }

            fitter.targetRect = _safeAreaRoot;
            fitter.continuousRefresh = true;
        }

        private void EnsureVideoBackground(RectTransform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return;
            }

            var existing = canvasRoot.Find("MenuVideoBackground");
            GameObject videoObject;
            if (existing == null)
            {
                videoObject = new GameObject("MenuVideoBackground", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer));
                videoObject.transform.SetParent(canvasRoot, false);
            }
            else
            {
                videoObject = existing.gameObject;
                if (videoObject.GetComponent<RawImage>() == null)
                {
                    videoObject.AddComponent<RawImage>();
                }

                if (videoObject.GetComponent<VideoPlayer>() == null)
                {
                    videoObject.AddComponent<VideoPlayer>();
                }
            }

            var rect = videoObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            _videoBackground = videoObject.GetComponent<RawImage>();
            _menuVideoPlayer = videoObject.GetComponent<VideoPlayer>();
            if (_videoBackground != null)
            {
                _videoBackground.raycastTarget = false;
                _videoBackground.transform.SetAsFirstSibling();
            }

            if (_menuVideoPlayer != null)
            {
                _menuVideoPlayer.playOnAwake = false;
                _menuVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                _menuVideoPlayer.waitForFirstFrame = false;
                _menuVideoPlayer.isLooping = true;
                _menuVideoPlayer.skipOnDrop = true;
            }

            UpdateVideoBackgroundState();
        }

        private void UpdateVideoBackgroundState()
        {
            var hasVideo = enableVideoBackground &&
                           menuBackgroundClip != null &&
                           _videoBackground != null &&
                           _menuVideoPlayer != null;

            if (!hasVideo)
            {
                if (_menuVideoPlayer != null && _menuVideoPlayer.isPlaying)
                {
                    _menuVideoPlayer.Stop();
                }

                if (_videoBackground != null)
                {
                    _videoBackground.texture = null;
                    _videoBackground.gameObject.SetActive(false);
                }

                ReleaseVideoRenderTexture();
                return;
            }

            _menuVideoPlayer.clip = menuBackgroundClip;
            _menuVideoPlayer.audioOutputMode = muteMenuVideo ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;
            _menuVideoPlayer.playbackSpeed = Mathf.Clamp(menuVideoPlaybackSpeed, 0.5f, 1.2f);

            _videoBackground.gameObject.SetActive(true);
            _videoBackground.color = new Color(menuVideoBrightness, menuVideoBrightness, menuVideoBrightness, 1f);

            EnsureVideoRenderTexture();
            if (_menuVideoRenderTexture == null)
            {
                return;
            }

            _menuVideoPlayer.targetTexture = _menuVideoRenderTexture;
            _videoBackground.texture = _menuVideoRenderTexture;
            if (!_menuVideoPlayer.isPlaying)
            {
                _menuVideoPlayer.Play();
            }
        }

        private void EnsureVideoRenderTexture()
        {
            var targetWidth = 1280;
            var targetHeight = 720;
            if (menuBackgroundClip != null)
            {
                targetWidth = Mathf.Clamp((int)menuBackgroundClip.width, 640, 2560);
                targetHeight = Mathf.Clamp((int)menuBackgroundClip.height, 360, 1440);
            }

            if (_menuVideoRenderTexture != null &&
                _menuVideoRenderTexture.width == targetWidth &&
                _menuVideoRenderTexture.height == targetHeight)
            {
                if (!_menuVideoRenderTexture.IsCreated())
                {
                    _menuVideoRenderTexture.Create();
                }

                return;
            }

            ReleaseVideoRenderTexture();
            _menuVideoRenderTexture = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = "MenuVideoRT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _menuVideoRenderTexture.Create();
        }

        private void ReleaseVideoRenderTexture()
        {
            if (_menuVideoRenderTexture == null)
            {
                return;
            }

            if (_menuVideoRenderTexture.IsCreated())
            {
                _menuVideoRenderTexture.Release();
            }

            Destroy(_menuVideoRenderTexture);
            _menuVideoRenderTexture = null;
        }

        private void EnsureRuntimePolish()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            EnsureProceduralBackdrop();

            if (_titleText != null)
            {
                _titleText.text = "MULTIPLY RUSH";
                StyleHeadline(_titleText, 108);
            }

            if (_playButtonRect != null)
            {
                _playButtonRect.sizeDelta = new Vector2(520f, 150f);
                _buttonBasePosition = _playButtonRect.anchoredPosition;
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
                _playButtonLabel = FindOrCreateText(
                    _playButtonRect,
                    "Label",
                    "PLAY NOW",
                    58,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                StyleBodyText(_playButtonLabel, 58, true);
                if (_playButtonLabel != null)
                {
                    _playButtonLabel.color = Color.white;
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
            var leftGlowImage = _leftGlowRect != null ? _leftGlowRect.GetComponent<Image>() : null;
            if (leftGlowImage != null)
            {
                EnsureBackdropSprites();
                leftGlowImage.sprite = _softOrbSprite;
                leftGlowImage.type = Image.Type.Simple;
                leftGlowImage.preserveAspect = true;
            }

            _rightGlowRect = FindOrCreateImage(
                _safeAreaRoot,
                "MenuGlowRight",
                new Color(0.07f, 0.93f, 0.98f, 0.2f),
                new Vector2(0.82f, 0.73f),
                new Vector2(0.82f, 0.73f),
                Vector2.zero,
                new Vector2(420f, 420f)).rectTransform;
            var rightGlowImage = _rightGlowRect != null ? _rightGlowRect.GetComponent<Image>() : null;
            if (rightGlowImage != null)
            {
                EnsureBackdropSprites();
                rightGlowImage.sprite = _softOrbSprite;
                rightGlowImage.type = Image.Type.Simple;
                rightGlowImage.preserveAspect = true;
            }

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
                "TopDecorStrip",
                new Color(0.18f, 0.52f, 0.86f, 0.34f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(640f, 44f)).rectTransform;
            if (_badgeRect != null)
            {
                var badgeImage = _badgeRect.GetComponent<Image>();
                if (badgeImage != null)
                {
                    badgeImage.color = new Color(0.18f, 0.52f, 0.86f, 0.2f);
                }

                var leftAccent = FindOrCreateImage(
                    _badgeRect,
                    "LeftAccent",
                    new Color(0.62f, 0.9f, 1f, 0.48f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(28f, 0f),
                    new Vector2(42f, 6f));
                if (leftAccent != null)
                {
                    leftAccent.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
                }

                var rightAccent = FindOrCreateImage(
                    _badgeRect,
                    "RightAccent",
                    new Color(0.62f, 0.9f, 1f, 0.48f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-28f, 0f),
                    new Vector2(42f, 6f));
                if (rightAccent != null)
                {
                    rightAccent.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
                }

                for (var i = 0; i < _badgeRect.childCount; i++)
                {
                    var child = _badgeRect.GetChild(i);
                    if (child == null)
                    {
                        continue;
                    }

                    if (child.name == "LeftAccent" || child.name == "RightAccent")
                    {
                        continue;
                    }

                    var staleText = child.GetComponent<Text>();
                    if (staleText != null)
                    {
                        staleText.gameObject.SetActive(false);
                    }
                }
            }

            _studioText = FindOrCreateText(
                _safeAreaRoot,
                "StudioLabel",
                "by ZoKorp Games",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.62f),
                new Vector2(0.5f, 0.62f),
                Vector2.zero,
                new Vector2(720f, 56f));
            StyleBodyText(_studioText, 30, true);
            _studioText.color = new Color(0.75f, 0.91f, 1f, 0.94f);
            _studioRect = _studioText.rectTransform;

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
            _taglineBasePosition = _taglineRect.anchoredPosition;

            _metaCardImage = FindOrCreateImage(
                _safeAreaRoot,
                "MetaCard",
                new Color(0.08f, 0.17f, 0.31f, 0.88f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(792f, 362f));
            _metaCardRect = _metaCardImage != null ? _metaCardImage.rectTransform : null;
            if (_metaCardImage != null)
            {
                var outline = _metaCardImage.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = _metaCardImage.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
                outline.effectDistance = new Vector2(1.8f, -1.8f);
                _metaCardRect.SetAsLastSibling();
            }

            _footerText = FindOrCreateText(
                _safeAreaRoot,
                "FooterText",
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.12f),
                new Vector2(0.5f, 0.12f),
                Vector2.zero,
                new Vector2(900f, 56f));
            StyleBodyText(_footerText, 24, false);
            _footerText.color = new Color(0.72f, 0.83f, 0.95f, 0f);
            _footerText.gameObject.SetActive(false);

            EnsureProgressOverlay();
            Text progressLabel;
            _progressButton = EnsureDifficultyButton(
                _safeAreaRoot,
                "ProgressButton",
                "PROGRESS",
                new Vector2(0f, -208f),
                out progressLabel);
            if (_progressButton != null)
            {
                var progressRect = _progressButton.GetComponent<RectTransform>();
                if (progressRect != null)
                {
                    progressRect.sizeDelta = new Vector2(260f, 58f);
                }

                _progressButton.onClick.RemoveAllListeners();
                _progressButton.onClick.AddListener(ToggleProgressOverlay);
            }

            if (progressLabel != null)
            {
                progressLabel.fontSize = 30;
                progressLabel.fontStyle = FontStyle.Bold;
            }

            if (_backdropLayerRect != null)
            {
                var backdropIndex = _backdropLayerRect.GetSiblingIndex();
                var maxIndex = Mathf.Max(0, _safeAreaRoot.childCount - 1);
                _leftGlowRect.SetSiblingIndex(Mathf.Clamp(backdropIndex + 1, 0, maxIndex));
                _rightGlowRect.SetSiblingIndex(Mathf.Clamp(backdropIndex + 2, 0, maxIndex));
                _scanlineRect.SetSiblingIndex(Mathf.Clamp(backdropIndex + 3, 0, maxIndex));
            }
            else
            {
                _leftGlowRect.SetAsFirstSibling();
                _rightGlowRect.SetAsFirstSibling();
                _scanlineRect.SetAsFirstSibling();
            }
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
            AnimateProceduralBackdrop(runTime);

            if (_titleRect != null)
            {
                var titlePulse = 1f + Mathf.Sin(runTime * titlePulseSpeed) * titlePulseScale;
                _titleRect.localScale = _titleBaseScale * titlePulse;
                _titleRect.anchoredPosition = _titleBasePosition + new Vector2(0f, Mathf.Sin(runTime * 0.85f) * 6f);
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
            }

            if (_metaCardRect != null)
            {
                _metaCardRect.localScale = Vector3.one * (1f + Mathf.Sin(runTime * 1.5f) * 0.016f);
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
                _badgeRect.localScale = Vector3.one * (1f + Mathf.Sin(runTime * 2.2f) * 0.015f);
                var stripImage = _badgeRect.GetComponent<Image>();
                if (stripImage != null)
                {
                    var alpha = 0.24f + Mathf.Abs(Mathf.Sin(runTime * 1.8f)) * 0.16f;
                    stripImage.color = new Color(0.18f, 0.52f, 0.86f, alpha);
                }
            }

            if (_studioText != null)
            {
                var glow = 0.78f + Mathf.Abs(Mathf.Sin(runTime * 1.25f)) * 0.22f;
                _studioText.color = new Color(0.75f * glow, 0.91f * glow, 1f * glow, 0.95f);
            }

            if (_taglineRect != null)
            {
                _taglineRect.anchoredPosition = _taglineBasePosition + new Vector2(0f, Mathf.Sin(runTime * 1.35f) * 5f);
            }

            if (_titleText != null)
            {
                var titleColor = Color.Lerp(new Color(0.86f, 0.95f, 1f, 1f), neonAccentColor, colorWave * 0.24f);
                _titleText.color = titleColor;
            }
        }

        private void EnsureDifficultySelector()
        {
            if (_metaCardRect == null)
            {
                return;
            }

            var rowTransform = _metaCardRect.Find("DifficultyRow");
            if (rowTransform == null)
            {
                var rowObject = new GameObject("DifficultyRow");
                rowObject.transform.SetParent(_metaCardRect, false);
                _difficultyRow = rowObject.AddComponent<RectTransform>();
                _difficultyRow.anchorMin = new Vector2(0.5f, 0.5f);
                _difficultyRow.anchorMax = new Vector2(0.5f, 0.5f);
                _difficultyRow.pivot = new Vector2(0.5f, 0.5f);
                _difficultyRow.sizeDelta = new Vector2(560f, 126f);
            }
            else
            {
                _difficultyRow = rowTransform.GetComponent<RectTransform>();
            }

            if (_difficultyRow == null)
            {
                return;
            }

            _difficultyRow.anchoredPosition = new Vector2(0f, -82f);

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
            if (_metaCardRect == null)
            {
                return;
            }

            var rowTransform = _metaCardRect.Find("MusicRow");
            if (rowTransform == null)
            {
                var rowObject = new GameObject("MusicRow");
                rowObject.transform.SetParent(_metaCardRect, false);
                _musicRow = rowObject.AddComponent<RectTransform>();
                _musicRow.anchorMin = new Vector2(0.5f, 0.5f);
                _musicRow.anchorMax = new Vector2(0.5f, 0.5f);
                _musicRow.pivot = new Vector2(0.5f, 0.5f);
                _musicRow.sizeDelta = new Vector2(640f, 126f);
            }
            else
            {
                _musicRow = rowTransform.GetComponent<RectTransform>();
            }

            if (_musicRow == null)
            {
                return;
            }

            _musicRow.anchoredPosition = new Vector2(0f, 8f);

            _musicLabel = EnsureDifficultyText(
                _musicRow,
                "MusicLabel",
                "Music Tracks",
                new Vector2(0f, 44f),
                28);

            var musicTrackPlate = FindOrCreateImage(
                _musicRow,
                "MusicTrackPlate",
                new Color(0.14f, 0.22f, 0.34f, 0.9f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 4f),
                new Vector2(450f, 62f));
            if (musicTrackPlate != null)
            {
                _musicTrackPlateRect = musicTrackPlate.rectTransform;
                _musicTrackPlateRect.SetAsFirstSibling();
                var plateOutline = musicTrackPlate.GetComponent<Outline>();
                if (plateOutline == null)
                {
                    plateOutline = musicTrackPlate.gameObject.AddComponent<Outline>();
                }

                plateOutline.effectColor = new Color(0f, 0f, 0f, 0.56f);
                plateOutline.effectDistance = new Vector2(1.4f, -1.4f);
            }

            _musicTrackLabel = EnsureDifficultyText(
                _musicRow,
                "MusicTrackLabel",
                "Hyper Neon",
                new Vector2(0f, 4f),
                33);
            if (_musicTrackLabel != null)
            {
                _musicTrackLabel.color = new Color(0.88f, 0.98f, 1f, 1f);
                _musicTrackLabel.fontStyle = FontStyle.Bold;
                _musicTrackLabel.rectTransform.sizeDelta = new Vector2(640f, 64f);
            }

            _musicPrevButton = EnsureMusicNavButton(_musicRow, "MusicPrevButton", "<", new Vector2(-258f, 4f));
            _musicNextButton = EnsureMusicNavButton(_musicRow, "MusicNextButton", ">", new Vector2(258f, 4f));

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
            var current = Mathf.Clamp(audio.GetSelectedGameplayTrackIndex(), 0, trackCount - 1);
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

        private void RefreshResponsiveLayout(bool force = false)
        {
            if (_safeAreaRoot == null || _playButtonRect == null || _titleRect == null)
            {
                return;
            }

            var safeSize = _safeAreaRoot.rect.size;
            if (!force && (safeSize - _lastSafeAreaSize).sqrMagnitude < 1f)
            {
                return;
            }

            _lastSafeAreaSize = safeSize;
            var width = Mathf.Max(320f, safeSize.x);
            var height = Mathf.Max(560f, safeSize.y);
            var layoutProfile = IPhoneLayoutCatalog.ResolveCurrent();
            var compact = layoutProfile.compact || height < 1280f;
            var ultraCompact = layoutProfile.ultraCompact || height < 980f;
            var layoutScale = Mathf.Clamp(layoutProfile.menuScale, 0.88f, 1.08f);
            var topInsetPadding = Mathf.Lerp(0f, 22f, Mathf.Clamp01((layoutProfile.topInsetRatio - 0.03f) * 45f));

            if (_titleRect != null)
            {
                _titleRect.anchoredPosition = new Vector2(0f, ((compact ? 296f : 332f) - topInsetPadding) * layoutScale);
                _titleBasePosition = _titleRect.anchoredPosition;
                if (_titleText != null)
                {
                    _titleText.fontSize = Mathf.RoundToInt((compact ? 88f : 104f) * layoutScale);
                }
            }

            if (_taglineRect != null)
            {
                _taglineRect.anchoredPosition = new Vector2(0f, ((compact ? 214f : 244f) - topInsetPadding) * layoutScale);
                _taglineRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 104f : 120f)) * layoutScale, 400f, 980f),
                    (compact ? 66f : 76f) * layoutScale);
                if (_taglineText != null)
                {
                    _taglineText.fontSize = Mathf.RoundToInt((compact ? 31f : 36f) * layoutScale);
                }

                _taglineBasePosition = _taglineRect.anchoredPosition;
            }

            if (_metaCardRect != null)
            {
                _metaCardRect.anchoredPosition = new Vector2(0f, (compact ? -4f : 10f) * layoutScale);
                _metaCardRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 74f : 90f)) * layoutScale, 520f, 860f),
                    (compact ? 346f : 380f) * layoutScale);
            }

            if (bestLevelText != null)
            {
                _bestLevelRect = bestLevelText.rectTransform;
            }

            if (_bestLevelRect != null)
            {
                _bestLevelRect.SetParent(_metaCardRect != null ? _metaCardRect : _safeAreaRoot, false);
                _bestLevelRect.anchorMin = new Vector2(0.5f, 0.5f);
                _bestLevelRect.anchorMax = new Vector2(0.5f, 0.5f);
                _bestLevelRect.pivot = new Vector2(0.5f, 0.5f);
                _bestLevelRect.anchoredPosition = new Vector2(0f, (compact ? 126f : 136f) * layoutScale);
                _bestLevelRect.sizeDelta = new Vector2(460f * layoutScale, 58f * layoutScale);
                if (bestLevelText != null)
                {
                    bestLevelText.fontSize = Mathf.RoundToInt((compact ? 52f : 56f) * layoutScale);
                }
            }

            if (_musicRow != null)
            {
                _musicRow.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 164f : 190f)) * layoutScale, 500f, 700f),
                    (compact ? 122f : 134f) * layoutScale);
                _musicRow.anchoredPosition = new Vector2(0f, (compact ? 28f : 34f) * layoutScale);

                var rowWidth = _musicRow.sizeDelta.x;
                var navOffset = Mathf.Clamp((rowWidth * 0.39f) - 12f, 176f, 286f);
                var navSize = new Vector2(Mathf.Clamp(rowWidth * 0.17f, 88f, 118f), 52f * layoutScale);
                var trackWidth = Mathf.Max(220f, rowWidth - (navOffset * 2f) - 54f);

                if (_musicPrevButton != null)
                {
                    var prevRect = _musicPrevButton.GetComponent<RectTransform>();
                    if (prevRect != null)
                    {
                        prevRect.anchoredPosition = new Vector2(-navOffset, 4f * layoutScale);
                        prevRect.sizeDelta = navSize;
                    }
                }

                if (_musicNextButton != null)
                {
                    var nextRect = _musicNextButton.GetComponent<RectTransform>();
                    if (nextRect != null)
                    {
                        nextRect.anchoredPosition = new Vector2(navOffset, 4f * layoutScale);
                        nextRect.sizeDelta = navSize;
                    }
                }

                if (_musicLabel != null)
                {
                    _musicLabel.rectTransform.anchoredPosition = new Vector2(0f, 46f * layoutScale);
                    _musicLabel.fontSize = Mathf.RoundToInt((compact ? 30f : 32f) * layoutScale);
                }

                if (_musicTrackLabel != null)
                {
                    _musicTrackLabel.rectTransform.anchoredPosition = new Vector2(0f, 4f * layoutScale);
                    _musicTrackLabel.rectTransform.sizeDelta = new Vector2(trackWidth, 62f * layoutScale);
                    _musicTrackLabel.fontSize = Mathf.RoundToInt((compact ? 32f : 35f) * layoutScale);
                }
            }

            if (_musicTrackPlateRect != null)
            {
                _musicTrackPlateRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 318f : 350f)) * layoutScale, 330f, 470f),
                    58f * layoutScale);
            }

            if (_difficultyRow != null)
            {
                _difficultyRow.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 186f : 210f)) * layoutScale, 450f, 610f),
                    (compact ? 130f : 144f) * layoutScale);
                _difficultyRow.anchoredPosition = new Vector2(0f, (compact ? -96f : -108f) * layoutScale);
            }

            if (_playButtonRect != null)
            {
                _playButtonRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 210f : 240f)) * layoutScale, 340f, 540f),
                    (compact ? 132f : 150f) * layoutScale);
                _buttonBasePosition = new Vector2(0f, (compact ? -262f : -314f) * layoutScale);
            }

            if (_playButtonLabel != null)
            {
                _playButtonLabel.fontSize = Mathf.RoundToInt((compact ? 50f : 58f) * layoutScale);
            }

            if (_badgeRect != null)
            {
                _badgeRect.anchorMin = new Vector2(0.5f, 1f);
                _badgeRect.anchorMax = new Vector2(0.5f, 1f);
                _badgeRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 120f : 140f)) * layoutScale, 400f, 780f),
                    (compact ? 34f : 40f) * layoutScale);
                _badgeRect.anchoredPosition = new Vector2(0f, ((compact ? -68f : -78f) - topInsetPadding) * layoutScale);
            }

            if (_studioRect != null)
            {
                _studioRect.anchoredPosition = new Vector2(0f, ((compact ? 256f : 286f) - topInsetPadding) * layoutScale);
                _studioRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - (ultraCompact ? 196f : 220f)) * layoutScale, 410f, 820f),
                    (compact ? 52f : 56f) * layoutScale);
                if (_studioText != null)
                {
                    _studioText.fontSize = Mathf.RoundToInt((compact ? 26f : 30f) * layoutScale);
                }
            }

            if (_footerText != null)
            {
                _footerRect = _footerText.rectTransform;
            }

            if (_footerRect != null)
            {
                _footerRect.anchorMin = new Vector2(0.5f, 0f);
                _footerRect.anchorMax = new Vector2(0.5f, 0f);
                _footerRect.sizeDelta = new Vector2(Mathf.Clamp((width - 140f) * layoutScale, 460f, 940f), 56f * layoutScale);
                _footerRect.anchoredPosition = new Vector2(0f, 34f * layoutScale);
                _footerRect.gameObject.SetActive(false);
            }

            if (_progressButton != null)
            {
                var progressRect = _progressButton.GetComponent<RectTransform>();
                if (progressRect != null)
                {
                    progressRect.anchoredPosition = new Vector2(0f, (compact ? -206f : -236f) * layoutScale);
                    progressRect.sizeDelta = new Vector2(
                        Mathf.Clamp((width - (ultraCompact ? 392f : 430f)) * layoutScale, 220f, 320f),
                        (compact ? 54f : 58f) * layoutScale);
                }
            }

            if (_progressPanelRect != null)
            {
                _progressPanelRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - 110f) * layoutScale, 520f, 900f),
                    Mathf.Clamp((height - 240f) * layoutScale, 740f, 1360f));
            }

            if (_progressSummaryText != null)
            {
                _progressSummaryText.rectTransform.sizeDelta = new Vector2(
                    Mathf.Clamp((width - 180f) * layoutScale, 450f, 780f),
                    64f * layoutScale);
            }

            if (_progressViewportRect != null)
            {
                _progressViewportRect.sizeDelta = new Vector2(
                    Mathf.Clamp((width - 170f) * layoutScale, 460f, 760f),
                    Mathf.Clamp((height - 420f) * layoutScale, 420f, 920f));
            }

            if (_progressCloseButton != null)
            {
                var closeRect = _progressCloseButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.anchoredPosition = new Vector2(0f, Mathf.Clamp((-height * 0.36f) * layoutScale, -568f, -420f));
                    closeRect.sizeDelta = new Vector2(
                        Mathf.Clamp((width - 420f) * layoutScale, 220f, 280f),
                        62f * layoutScale);
                }
            }
        }

        private void EnsureProceduralBackdrop()
        {
            if (_safeAreaRoot == null)
            {
                return;
            }

            var existing = _safeAreaRoot.Find("MenuBackdropLayer");
            GameObject backdropObject;
            if (existing == null)
            {
                backdropObject = new GameObject("MenuBackdropLayer", typeof(RectTransform));
                backdropObject.transform.SetParent(_safeAreaRoot, false);
            }
            else
            {
                backdropObject = existing.gameObject;
            }

            _backdropLayerRect = backdropObject.GetComponent<RectTransform>();
            if (_backdropLayerRect == null)
            {
                _backdropLayerRect = backdropObject.AddComponent<RectTransform>();
            }

            _backdropLayerRect.anchorMin = Vector2.zero;
            _backdropLayerRect.anchorMax = Vector2.one;
            _backdropLayerRect.offsetMin = Vector2.zero;
            _backdropLayerRect.offsetMax = Vector2.zero;
            var background = _safeAreaRoot.Find("Background");
            if (background != null)
            {
                var targetIndex = Mathf.Clamp(background.GetSiblingIndex() + 1, 0, Mathf.Max(0, _safeAreaRoot.childCount - 1));
                _backdropLayerRect.SetSiblingIndex(targetIndex);
            }
            else
            {
                _backdropLayerRect.SetAsFirstSibling();
            }

            EnsureBackdropSprites();

            var nebulaAImage = FindOrCreateImage(
                _backdropLayerRect,
                "NebulaA",
                new Color(0.24f, 0.55f, 1f, 0.2f),
                new Vector2(0.16f, 0.77f),
                new Vector2(0.16f, 0.77f),
                Vector2.zero,
                new Vector2(820f, 820f));
            if (nebulaAImage != null)
            {
                if (_softOrbSprite != null)
                {
                    nebulaAImage.sprite = _softOrbSprite;
                    nebulaAImage.type = Image.Type.Simple;
                    nebulaAImage.preserveAspect = true;
                }

                _backdropNebulaARect = nebulaAImage.rectTransform;
            }

            var nebulaBImage = FindOrCreateImage(
                _backdropLayerRect,
                "NebulaB",
                new Color(0.04f, 0.82f, 0.96f, 0.14f),
                new Vector2(0.82f, 0.28f),
                new Vector2(0.82f, 0.28f),
                Vector2.zero,
                new Vector2(670f, 670f));
            if (nebulaBImage != null)
            {
                if (_softOrbSprite != null)
                {
                    nebulaBImage.sprite = _softOrbSprite;
                    nebulaBImage.type = Image.Type.Simple;
                    nebulaBImage.preserveAspect = true;
                }

                _backdropNebulaBRect = nebulaBImage.rectTransform;
            }

            var horizonImage = FindOrCreateImage(
                _backdropLayerRect,
                "HorizonGlow",
                new Color(0.1f, 0.46f, 0.9f, 0.16f),
                new Vector2(0.5f, 0.56f),
                new Vector2(0.5f, 0.56f),
                Vector2.zero,
                new Vector2(1560f, 210f));
            if (horizonImage != null)
            {
                if (_softOrbSprite != null)
                {
                    horizonImage.sprite = _softOrbSprite;
                    horizonImage.type = Image.Type.Simple;
                    horizonImage.preserveAspect = false;
                }

                _backdropHorizonRect = horizonImage.rectTransform;
            }

            var auroraA = FindOrCreateImage(
                _backdropLayerRect,
                "AuroraA",
                new Color(0.26f, 0.86f, 1f, 0.12f),
                new Vector2(0.5f, 0.64f),
                new Vector2(0.5f, 0.64f),
                Vector2.zero,
                new Vector2(1660f, 390f));
            if (auroraA != null)
            {
                if (_softOrbSprite != null)
                {
                    auroraA.sprite = _softOrbSprite;
                    auroraA.type = Image.Type.Simple;
                    auroraA.preserveAspect = false;
                }

                _backdropAuroraARect = auroraA.rectTransform;
                _backdropAuroraARect.localRotation = Quaternion.Euler(0f, 0f, -6f);
            }

            var auroraB = FindOrCreateImage(
                _backdropLayerRect,
                "AuroraB",
                new Color(0.18f, 0.68f, 1f, 0.11f),
                new Vector2(0.5f, 0.48f),
                new Vector2(0.5f, 0.48f),
                Vector2.zero,
                new Vector2(1540f, 340f));
            if (auroraB != null)
            {
                if (_softOrbSprite != null)
                {
                    auroraB.sprite = _softOrbSprite;
                    auroraB.type = Image.Type.Simple;
                    auroraB.preserveAspect = false;
                }

                _backdropAuroraBRect = auroraB.rectTransform;
                _backdropAuroraBRect.localRotation = Quaternion.Euler(0f, 0f, 7f);
            }

            const int stripCount = 14;
            _backdropStripRects.Clear();
            _backdropStripSpeeds.Clear();
            _backdropStripPhases.Clear();
            _backdropStripBaseX.Clear();
            for (var i = 0; i < stripCount; i++)
            {
                var xHash = Hash01(i + 71);
                var yHash = Hash01(i + 103);
                var widthHash = Hash01(i + 149);
                var speedHash = Hash01(i + 211);
                var phaseHash = Hash01(i + 251);
                var baseX = Mathf.Lerp(-460f, 460f, xHash);
                var baseY = Mathf.Lerp(-620f, 620f, yHash);
                var size = Mathf.Lerp(120f, 360f, widthHash);

                var strip = FindOrCreateImage(
                    _backdropLayerRect,
                    "DriftStrip_" + i,
                    new Color(0.4f, 0.78f, 1f, Mathf.Lerp(0.05f, 0.15f, Hash01(i + 317))),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(baseX, baseY),
                    new Vector2(size, size));
                if (strip == null)
                {
                    continue;
                }

                if (_softOrbSprite != null)
                {
                    strip.sprite = _softOrbSprite;
                    strip.type = Image.Type.Simple;
                    strip.preserveAspect = true;
                }

                strip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-22f, 22f, Hash01(i + 347)));
                strip.raycastTarget = false;
                _backdropStripRects.Add(strip.rectTransform);
                _backdropStripSpeeds.Add(Mathf.Lerp(0.68f, 1.28f, speedHash));
                _backdropStripPhases.Add(phaseHash * 1000f);
                _backdropStripBaseX.Add(baseX);
            }

            const int starCount = 84;
            _backdropStarRects.Clear();
            _backdropStarSpeeds.Clear();
            _backdropStarPhases.Clear();
            _backdropStarBaseX.Clear();
            for (var i = 0; i < starCount; i++)
            {
                var xHash = Hash01(i + 401);
                var yHash = Hash01(i + 463);
                var sizeHash = Hash01(i + 509);
                var speedHash = Hash01(i + 557);
                var phaseHash = Hash01(i + 601);
                var baseX = Mathf.Lerp(-520f, 520f, xHash);
                var baseY = Mathf.Lerp(-660f, 660f, yHash);
                var size = Mathf.Lerp(4f, 11f, sizeHash);

                var star = FindOrCreateImage(
                    _backdropLayerRect,
                    "Star_" + i,
                    new Color(0.9f, 0.98f, 1f, Mathf.Lerp(0.35f, 0.9f, sizeHash)),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(baseX, baseY),
                    new Vector2(size, size));
                if (star == null)
                {
                    continue;
                }

                if (_starOrbSprite != null)
                {
                    star.sprite = _starOrbSprite;
                    star.type = Image.Type.Simple;
                    star.preserveAspect = true;
                }

                star.raycastTarget = false;
                _backdropStarRects.Add(star.rectTransform);
                _backdropStarSpeeds.Add(Mathf.Lerp(0.74f, 1.32f, speedHash));
                _backdropStarPhases.Add(phaseHash * 1400f);
                _backdropStarBaseX.Add(baseX);
            }

            const int cometCount = 6;
            _backdropCometRects.Clear();
            _backdropCometSpeeds.Clear();
            _backdropCometPhases.Clear();
            _backdropCometAmplitudes.Clear();
            for (var i = 0; i < cometCount; i++)
            {
                var speedHash = Hash01(i + 1701);
                var phaseHash = Hash01(i + 1723);
                var widthHash = Hash01(i + 1747);
                var alphaHash = Hash01(i + 1783);
                var comet = FindOrCreateImage(
                    _backdropLayerRect,
                    "Comet_" + i,
                    new Color(0.76f, 0.95f, 1f, Mathf.Lerp(0.15f, 0.34f, alphaHash)),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(Mathf.Lerp(120f, 250f, widthHash), Mathf.Lerp(8f, 16f, widthHash)));
                if (comet == null)
                {
                    continue;
                }

                if (_softOrbSprite != null)
                {
                    comet.sprite = _softOrbSprite;
                    comet.type = Image.Type.Simple;
                    comet.preserveAspect = false;
                }

                var cometRect = comet.rectTransform;
                cometRect.localRotation = Quaternion.Euler(0f, 0f, -28f);
                cometRect.SetSiblingIndex(Mathf.Clamp(_backdropLayerRect.childCount - 1, 0, _backdropLayerRect.childCount - 1));
                _backdropCometRects.Add(cometRect);
                _backdropCometSpeeds.Add(Mathf.Lerp(0.58f, 1.18f, speedHash));
                _backdropCometPhases.Add(phaseHash * 2200f);
                _backdropCometAmplitudes.Add(Mathf.Lerp(18f, 68f, Hash01(i + 1823)));
            }

            const int ringCount = 4;
            _backdropPulseRingRects.Clear();
            _backdropPulseRingSpeeds.Clear();
            _backdropPulseRingPhases.Clear();
            _backdropPulseRingBasePositions.Clear();
            for (var i = 0; i < ringCount; i++)
            {
                var xHash = Hash01(i + 1901);
                var yHash = Hash01(i + 1933);
                var speedHash = Hash01(i + 1973);
                var phaseHash = Hash01(i + 2017);
                var basePosition = new Vector2(
                    Mathf.Lerp(-420f, 420f, xHash),
                    Mathf.Lerp(-480f, 480f, yHash));
                var ring = FindOrCreateImage(
                    _backdropLayerRect,
                    "PulseRing_" + i,
                    new Color(0.42f, 0.88f, 1f, 0.18f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    basePosition,
                    new Vector2(280f, 280f));
                if (ring == null)
                {
                    continue;
                }

                if (_softOrbSprite != null)
                {
                    ring.sprite = _softOrbSprite;
                    ring.type = Image.Type.Simple;
                    ring.preserveAspect = true;
                }

                ring.raycastTarget = false;
                _backdropPulseRingRects.Add(ring.rectTransform);
                _backdropPulseRingSpeeds.Add(Mathf.Lerp(0.62f, 1.06f, speedHash));
                _backdropPulseRingPhases.Add(phaseHash);
                _backdropPulseRingBasePositions.Add(basePosition);
            }

            const int waveCount = 5;
            _backdropWaveRects.Clear();
            _backdropWaveSpeeds.Clear();
            _backdropWavePhases.Clear();
            _backdropWaveBaseY.Clear();
            for (var i = 0; i < waveCount; i++)
            {
                var widthHash = Hash01(i + 2111);
                var speedHash = Hash01(i + 2137);
                var phaseHash = Hash01(i + 2179);
                var yHash = Hash01(i + 2213);
                var width = Mathf.Lerp(1180f, 1740f, widthHash);
                var height = Mathf.Lerp(62f, 102f, Hash01(i + 2239));
                var baseY = Mathf.Lerp(-420f, 210f, yHash);
                var wave = FindOrCreateImage(
                    _backdropLayerRect,
                    "WaveRibbon_" + i,
                    new Color(0.34f, 0.82f, 1f, Mathf.Lerp(0.05f, 0.13f, Hash01(i + 2267))),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, baseY),
                    new Vector2(width, height));
                if (wave == null)
                {
                    continue;
                }

                if (_softOrbSprite != null)
                {
                    wave.sprite = _softOrbSprite;
                    wave.type = Image.Type.Simple;
                    wave.preserveAspect = false;
                }

                wave.raycastTarget = false;
                wave.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 8f, Hash01(i + 2299)));
                _backdropWaveRects.Add(wave.rectTransform);
                _backdropWaveSpeeds.Add(Mathf.Lerp(0.56f, 1.12f, speedHash));
                _backdropWavePhases.Add(phaseHash * 100f);
                _backdropWaveBaseY.Add(baseY);
            }

            const int orbitParticleCount = 10;
            _backdropOrbitParticleRects.Clear();
            _backdropOrbitCenters.Clear();
            _backdropOrbitRadii.Clear();
            _backdropOrbitSpeeds.Clear();
            _backdropOrbitPhases.Clear();
            for (var i = 0; i < orbitParticleCount; i++)
            {
                var center = new Vector2(
                    Mathf.Lerp(-300f, 320f, Hash01(i + 2309)),
                    Mathf.Lerp(-340f, 420f, Hash01(i + 2347)));
                var size = Mathf.Lerp(42f, 94f, Hash01(i + 2381));
                var radius = Mathf.Lerp(26f, 86f, Hash01(i + 2423));
                var speed = Mathf.Lerp(0.46f, 1.22f, Hash01(i + 2473));
                var phase = Hash01(i + 2519) * Mathf.PI * 2f;
                var particle = FindOrCreateImage(
                    _backdropLayerRect,
                    "OrbitParticle_" + i,
                    new Color(0.76f, 0.97f, 1f, Mathf.Lerp(0.14f, 0.3f, Hash01(i + 2551))),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    center,
                    new Vector2(size, size));
                if (particle == null)
                {
                    continue;
                }

                if (_softOrbSprite != null)
                {
                    particle.sprite = _softOrbSprite;
                    particle.type = Image.Type.Simple;
                    particle.preserveAspect = true;
                }

                particle.raycastTarget = false;
                _backdropOrbitParticleRects.Add(particle.rectTransform);
                _backdropOrbitCenters.Add(center);
                _backdropOrbitRadii.Add(radius);
                _backdropOrbitSpeeds.Add(speed);
                _backdropOrbitPhases.Add(phase);
            }
        }

        private void AnimateProceduralBackdrop(float runTime)
        {
            if (_backdropLayerRect == null)
            {
                return;
            }

            if (_backdropNebulaARect != null)
            {
                _backdropNebulaARect.anchoredPosition = new Vector2(
                    Mathf.Sin(runTime * 0.22f) * 160f,
                    Mathf.Cos(runTime * 0.16f) * 72f);
                var scale = 1f + Mathf.Sin(runTime * 0.64f) * 0.08f;
                _backdropNebulaARect.localScale = new Vector3(scale, scale, 1f);
            }

            if (_backdropNebulaBRect != null)
            {
                _backdropNebulaBRect.anchoredPosition = new Vector2(
                    Mathf.Cos(runTime * 0.19f) * 120f,
                    Mathf.Sin(runTime * 0.24f) * 84f);
                var scale = 1f + Mathf.Cos(runTime * 0.74f) * 0.1f;
                _backdropNebulaBRect.localScale = new Vector3(scale, scale, 1f);
            }

            if (_backdropHorizonRect != null)
            {
                _backdropHorizonRect.anchoredPosition = new Vector2(0f, Mathf.Sin(runTime * 0.82f) * 30f);
                var horizonImage = _backdropHorizonRect.GetComponent<Image>();
                if (horizonImage != null)
                {
                    var alpha = 0.12f + Mathf.Abs(Mathf.Sin(runTime * 1.5f)) * 0.12f;
                    horizonImage.color = new Color(0.12f, 0.54f, 0.98f, alpha);
                }
            }

            if (_backdropAuroraARect != null)
            {
                _backdropAuroraARect.anchoredPosition = new Vector2(
                    Mathf.Sin(runTime * 0.18f) * 110f,
                    20f + (Mathf.Cos(runTime * 0.13f) * 34f));
                _backdropAuroraARect.localScale = new Vector3(
                    1.04f + (Mathf.Sin(runTime * 0.42f) * 0.12f),
                    1f + (Mathf.Cos(runTime * 0.36f) * 0.07f),
                    1f);
                var image = _backdropAuroraARect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = 0.1f + (Mathf.Abs(Mathf.Sin(runTime * 0.6f)) * 0.16f);
                    image.color = new Color(0.26f, 0.86f, 1f, alpha);
                }
            }

            if (_backdropAuroraBRect != null)
            {
                _backdropAuroraBRect.anchoredPosition = new Vector2(
                    Mathf.Cos(runTime * 0.17f) * 130f,
                    -40f + (Mathf.Sin(runTime * 0.11f) * 28f));
                _backdropAuroraBRect.localScale = new Vector3(
                    1f + (Mathf.Cos(runTime * 0.38f) * 0.1f),
                    1f + (Mathf.Sin(runTime * 0.44f) * 0.08f),
                    1f);
                var image = _backdropAuroraBRect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = 0.08f + (Mathf.Abs(Mathf.Sin(runTime * 0.52f)) * 0.14f);
                    image.color = new Color(0.18f, 0.68f, 1f, alpha);
                }
            }

            var stripCount = Mathf.Min(_backdropStripRects.Count, Mathf.Min(_backdropStripSpeeds.Count, _backdropStripBaseX.Count));
            for (var i = 0; i < stripCount; i++)
            {
                var rect = _backdropStripRects[i];
                if (rect == null)
                {
                    continue;
                }

                var phase = i < _backdropStripPhases.Count ? _backdropStripPhases[i] : 0f;
                var y = Mathf.Repeat(phase + (runTime * backdropStripDriftSpeed * _backdropStripSpeeds[i]), 1560f) - 780f;
                var x = _backdropStripBaseX[i] + Mathf.Sin(runTime * (0.55f + i * 0.06f)) * 34f;
                rect.anchoredPosition = new Vector2(x, y);
                var alphaPulse = 0.04f + Mathf.Abs(Mathf.Sin(runTime * (1.2f + i * 0.11f))) * 0.12f;
                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.42f, 0.8f, 1f, alphaPulse);
                }
            }

            var starCount = Mathf.Min(_backdropStarRects.Count, Mathf.Min(_backdropStarSpeeds.Count, _backdropStarBaseX.Count));
            for (var i = 0; i < starCount; i++)
            {
                var rect = _backdropStarRects[i];
                if (rect == null)
                {
                    continue;
                }

                var phase = i < _backdropStarPhases.Count ? _backdropStarPhases[i] : 0f;
                var y = Mathf.Repeat(phase + (runTime * backdropStarDriftSpeed * _backdropStarSpeeds[i]), 1540f) - 770f;
                var x = _backdropStarBaseX[i] + Mathf.Sin(runTime * (0.9f + i * 0.2f)) * 8f;
                rect.anchoredPosition = new Vector2(x, y);
                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = 0.35f + Mathf.Abs(Mathf.Sin(runTime * (2f + i * 0.09f))) * 0.45f;
                    image.color = new Color(0.88f, 0.96f, 1f, alpha);
                }
            }

            var cometCount = Mathf.Min(_backdropCometRects.Count, Mathf.Min(_backdropCometSpeeds.Count, _backdropCometPhases.Count));
            for (var i = 0; i < cometCount; i++)
            {
                var rect = _backdropCometRects[i];
                if (rect == null)
                {
                    continue;
                }

                var speed = _backdropCometSpeeds[i];
                var phase = _backdropCometPhases[i];
                var travel = Mathf.Repeat(phase + (runTime * backdropCometDriftSpeed * speed), 2260f) - 1130f;
                var baseY = 560f - (travel * 0.58f);
                var amplitude = i < _backdropCometAmplitudes.Count ? _backdropCometAmplitudes[i] : 24f;
                var y = baseY + Mathf.Sin(runTime * (0.9f + i * 0.17f)) * amplitude;
                var x = -980f + travel;
                rect.anchoredPosition = new Vector2(x, y);
                rect.localRotation = Quaternion.Euler(0f, 0f, -30f + Mathf.Sin(runTime * (0.6f + i * 0.08f)) * 8f);

                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = 0.06f + Mathf.Abs(Mathf.Sin(runTime * (1.24f + i * 0.12f))) * 0.28f;
                    image.color = new Color(0.78f, 0.95f, 1f, alpha);
                }
            }

            var ringCount = Mathf.Min(_backdropPulseRingRects.Count, Mathf.Min(_backdropPulseRingSpeeds.Count, _backdropPulseRingPhases.Count));
            for (var i = 0; i < ringCount; i++)
            {
                var rect = _backdropPulseRingRects[i];
                if (rect == null)
                {
                    continue;
                }

                var phase = Mathf.Repeat((runTime * backdropRingPulseSpeed * _backdropPulseRingSpeeds[i]) + _backdropPulseRingPhases[i], 1f);
                var expanded = Mathf.SmoothStep(0f, 1f, phase);
                var basePosition = i < _backdropPulseRingBasePositions.Count ? _backdropPulseRingBasePositions[i] : Vector2.zero;
                rect.anchoredPosition = basePosition + new Vector2(
                    Mathf.Sin(runTime * (0.22f + i * 0.08f)) * 28f,
                    Mathf.Cos(runTime * (0.18f + i * 0.05f)) * 22f);
                var ringScale = Mathf.Lerp(0.42f, 1.62f, expanded);
                rect.localScale = new Vector3(ringScale, ringScale, 1f);

                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = (1f - expanded) * 0.2f;
                    image.color = new Color(
                        Mathf.Lerp(0.22f, 0.56f, expanded),
                        Mathf.Lerp(0.68f, 0.94f, expanded),
                        1f,
                        alpha);
                }
            }

            var waveCount = Mathf.Min(_backdropWaveRects.Count, Mathf.Min(_backdropWaveSpeeds.Count, _backdropWavePhases.Count));
            for (var i = 0; i < waveCount; i++)
            {
                var rect = _backdropWaveRects[i];
                if (rect == null)
                {
                    continue;
                }

                var speed = _backdropWaveSpeeds[i];
                var phase = _backdropWavePhases[i];
                var baseY = i < _backdropWaveBaseY.Count ? _backdropWaveBaseY[i] : 0f;
                var sweep = Mathf.Sin((runTime * backdropWaveSweepSpeed * speed) + phase);
                var driftX = sweep * Mathf.Lerp(120f, 220f, Hash01(i + 2633));
                var driftY = Mathf.Cos((runTime * 0.26f * speed) + phase) * 28f;
                rect.anchoredPosition = new Vector2(driftX, baseY + driftY);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(runTime * (0.2f + i * 0.08f)) * 8f);
                rect.localScale = new Vector3(1f + Mathf.Abs(sweep) * 0.22f, 1f + Mathf.Sin(runTime * (0.42f + i * 0.05f)) * 0.16f, 1f);
                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = 0.05f + Mathf.Abs(Mathf.Sin(runTime * (0.58f + i * 0.06f))) * 0.12f;
                    image.color = new Color(0.34f, 0.84f, 1f, alpha);
                }
            }

            var orbitCount = Mathf.Min(_backdropOrbitParticleRects.Count, Mathf.Min(_backdropOrbitCenters.Count, _backdropOrbitRadii.Count));
            for (var i = 0; i < orbitCount; i++)
            {
                var rect = _backdropOrbitParticleRects[i];
                if (rect == null)
                {
                    continue;
                }

                var center = _backdropOrbitCenters[i];
                var radius = _backdropOrbitRadii[i];
                var speed = i < _backdropOrbitSpeeds.Count ? _backdropOrbitSpeeds[i] : 1f;
                var phase = i < _backdropOrbitPhases.Count ? _backdropOrbitPhases[i] : 0f;
                var angle = phase + (runTime * backdropParticleOrbitSpeed * speed);
                rect.anchoredPosition = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var pulse = 0.8f + (Mathf.Sin(runTime * (1.4f + i * 0.09f)) * 0.24f);
                rect.localScale = new Vector3(pulse, pulse, 1f);
                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    var alpha = 0.1f + Mathf.Abs(Mathf.Sin(runTime * (1.26f + i * 0.14f))) * 0.3f;
                    image.color = new Color(0.76f, 0.97f, 1f, alpha);
                }
            }
        }

        private static float Hash01(int seed)
        {
            var value = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private void EnsureBackdropSprites()
        {
            if (_softOrbSprite != null && _starOrbSprite != null)
            {
                return;
            }

            if (_softOrbTexture == null)
            {
                _softOrbTexture = BuildRadialTexture(160, 0.92f, 3.4f);
            }

            if (_starOrbTexture == null)
            {
                _starOrbTexture = BuildRadialTexture(72, 1f, 6.4f);
            }

            if (_softOrbTexture != null && _softOrbSprite == null)
            {
                _softOrbSprite = Sprite.Create(
                    _softOrbTexture,
                    new Rect(0f, 0f, _softOrbTexture.width, _softOrbTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            if (_starOrbTexture != null && _starOrbSprite == null)
            {
                _starOrbSprite = Sprite.Create(
                    _starOrbTexture,
                    new Rect(0f, 0f, _starOrbTexture.width, _starOrbTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        private static Texture2D BuildRadialTexture(int size, float edgeAlpha, float falloffPower)
        {
            var safeSize = Mathf.Clamp(size, 32, 256);
            var texture = new Texture2D(safeSize, safeSize, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var colors = new Color[safeSize * safeSize];
            var center = (safeSize - 1f) * 0.5f;
            var invRadius = 1f / Mathf.Max(1f, center);
            var index = 0;
            for (var y = 0; y < safeSize; y++)
            {
                for (var x = 0; x < safeSize; x++)
                {
                    var dx = (x - center) * invRadius;
                    var dy = (y - center) * invRadius;
                    var dist = Mathf.Clamp01(Mathf.Sqrt((dx * dx) + (dy * dy)));
                    var alpha = Mathf.Pow(1f - dist, Mathf.Max(1f, falloffPower));
                    alpha = Mathf.Clamp01(alpha * edgeAlpha);
                    colors[index++] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }

        private void RefreshMusicTrackLabel()
        {
            if (_musicTrackLabel == null)
            {
                return;
            }

            var audio = AudioDirector.Instance ?? AudioDirector.EnsureInstance();
            if (audio == null)
            {
                _musicTrackLabel.text = "#1  Hyper Neon";
                return;
            }

            var index = Mathf.Clamp(audio.GetSelectedGameplayTrackIndex(), 0, Mathf.Max(0, audio.GetGameplayTrackCount() - 1));
            var trackName = audio.GetGameplayTrackName(index);
            if (string.IsNullOrWhiteSpace(trackName))
            {
                trackName = "Track " + (index + 1);
            }

            _musicTrackLabel.text = "#" + (index + 1) + "  " + trackName;
            if (string.IsNullOrWhiteSpace(_musicTrackLabel.text))
            {
                _musicTrackLabel.text = "#1  Hyper Neon";
            }

            _musicTrackLabel.color = new Color(0.92f, 0.98f, 1f, 1f);
            _musicTrackLabel.fontStyle = FontStyle.Bold;
            _musicTrackLabel.enabled = true;
            _musicTrackLabel.gameObject.SetActive(true);
        }

        private void ToggleProgressOverlay()
        {
            if (_progressOverlayRect == null || _progressOverlayGroup == null)
            {
                return;
            }

            var show = !_progressOverlayRect.gameObject.activeSelf;
            _progressOverlayRect.gameObject.SetActive(show);
            _progressOverlayGroup.alpha = show ? 1f : 0f;
            _progressOverlayGroup.interactable = show;
            _progressOverlayGroup.blocksRaycasts = show;
            if (_progressButton != null)
            {
                _progressButton.gameObject.SetActive(!show);
            }
            if (show)
            {
                RefreshProgressOverlay();
                if (_progressScrollRect != null)
                {
                    _progressScrollRect.verticalNormalizedPosition = 1f;
                }

                AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.6f, 1.04f);
            }
        }

        private void EnsureProgressOverlay()
        {
            if (_safeAreaRoot == null || _progressOverlayRect != null)
            {
                return;
            }

            var overlayObject = new GameObject("ProgressOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayObject.transform.SetParent(_safeAreaRoot, false);
            _progressOverlayRect = overlayObject.GetComponent<RectTransform>();
            _progressOverlayRect.anchorMin = Vector2.zero;
            _progressOverlayRect.anchorMax = Vector2.one;
            _progressOverlayRect.offsetMin = Vector2.zero;
            _progressOverlayRect.offsetMax = Vector2.zero;
            _progressOverlayGroup = overlayObject.GetComponent<CanvasGroup>();

            var dim = FindOrCreateImage(
                _progressOverlayRect,
                "Dim",
                new Color(0f, 0f, 0f, 0.76f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            if (dim != null)
            {
                dim.raycastTarget = true;
            }

            var panel = FindOrCreateImage(
                _progressOverlayRect,
                "Panel",
                new Color(0.05f, 0.12f, 0.24f, 0.96f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 1120f));
            _progressPanelRect = panel != null ? panel.rectTransform : null;
            if (_progressPanelRect == null)
            {
                return;
            }

            var outline = panel.GetComponent<Outline>();
            if (outline == null)
            {
                outline = panel.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);

            var title = FindOrCreateText(
                _progressPanelRect,
                "Title",
                "RUN PROGRESSION",
                56,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -64f),
                new Vector2(680f, 88f));
            StyleHeadline(title, 56);

            _progressSummaryText = FindOrCreateText(
                _progressPanelRect,
                "Summary",
                string.Empty,
                28,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -126f),
                new Vector2(700f, 64f));
            StyleBodyText(_progressSummaryText, 28, true);

            var viewportObject = new GameObject("ScrollViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(_progressPanelRect, false);
            _progressViewportRect = viewportObject.GetComponent<RectTransform>();
            _progressViewportRect.anchorMin = new Vector2(0.5f, 0.5f);
            _progressViewportRect.anchorMax = new Vector2(0.5f, 0.5f);
            _progressViewportRect.pivot = new Vector2(0.5f, 0.5f);
            _progressViewportRect.anchoredPosition = new Vector2(0f, -30f);
            _progressViewportRect.sizeDelta = new Vector2(700f, 792f);

            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;

            var contentObject = new GameObject("ScrollContent", typeof(RectTransform));
            contentObject.transform.SetParent(_progressViewportRect, false);
            _progressContentRect = contentObject.GetComponent<RectTransform>();
            _progressContentRect.anchorMin = new Vector2(0.5f, 1f);
            _progressContentRect.anchorMax = new Vector2(0.5f, 1f);
            _progressContentRect.pivot = new Vector2(0.5f, 1f);
            _progressContentRect.anchoredPosition = Vector2.zero;
            _progressContentRect.sizeDelta = new Vector2(674f, 40f);

            _progressScrollRect = viewportObject.GetComponent<ScrollRect>();
            _progressScrollRect.horizontal = false;
            _progressScrollRect.vertical = true;
            _progressScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _progressScrollRect.inertia = true;
            _progressScrollRect.decelerationRate = 0.12f;
            _progressScrollRect.scrollSensitivity = 52f;
            _progressScrollRect.viewport = _progressViewportRect;
            _progressScrollRect.content = _progressContentRect;

            _progressRowRects.Clear();
            _progressLevelTexts.Clear();
            _progressScoreTexts.Clear();
            _progressReplayButtons.Clear();
            EnsureProgressRows(24);

            _progressCloseButton = EnsureDifficultyButton(_progressPanelRect, "CloseButton", "CLOSE", new Vector2(0f, -526f), out var closeLabel);
            if (_progressCloseButton != null)
            {
                var closeRect = _progressCloseButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.sizeDelta = new Vector2(248f, 62f);
                }

                _progressCloseButton.onClick.RemoveAllListeners();
                _progressCloseButton.onClick.AddListener(ToggleProgressOverlay);
            }

            if (closeLabel != null)
            {
                closeLabel.fontSize = 28;
            }

            _progressOverlayRect.gameObject.SetActive(false);
            _progressOverlayGroup.alpha = 0f;
            _progressOverlayGroup.interactable = false;
            _progressOverlayGroup.blocksRaycasts = false;
        }

        private void RefreshProgressOverlay()
        {
            var unlocked = ProgressionStore.GetUnlockedLevel();
            var best = ProgressionStore.GetBestLevel();
            if (_progressSummaryText != null)
            {
                _progressSummaryText.text = "Unlocked " + unlocked + "   •   Best " + best + "   •   Replay Any Level";
            }

            EnsureProgressRows(Mathf.Max(1, unlocked));
            var visibleRows = Mathf.Max(1, unlocked);
            const float rowHeight = 72f;
            const float rowGap = 12f;
            if (_progressContentRect != null)
            {
                var contentHeight = 16f + visibleRows * (rowHeight + rowGap);
                _progressContentRect.sizeDelta = new Vector2(674f, contentHeight);
                _progressContentRect.anchoredPosition = new Vector2(0f, 0f);
            }

            for (var i = 0; i < _progressRowRects.Count; i++)
            {
                var level = unlocked - i;
                var visible = i < visibleRows;
                var rowRect = _progressRowRects[i];
                if (rowRect != null)
                {
                    rowRect.gameObject.SetActive(visible);
                    if (visible)
                    {
                        rowRect.anchoredPosition = new Vector2(0f, -(8f + (i * (rowHeight + rowGap))));
                    }

                    var rowImage = rowRect.GetComponent<Image>();
                    if (rowImage != null)
                    {
                        var baseColor = i % 2 == 0
                            ? new Color(0.12f, 0.26f, 0.42f, 0.84f)
                            : new Color(0.09f, 0.2f, 0.35f, 0.82f);
                        rowImage.color = baseColor;
                    }
                }

                if (!visible)
                {
                    continue;
                }

                if (i < _progressLevelTexts.Count && _progressLevelTexts[i] != null)
                {
                    _progressLevelTexts[i].text = "LEVEL " + level;
                }

                var bestSurvivors = ProgressionStore.GetBestSurvivorsForLevel(level);
                if (i < _progressScoreTexts.Count && _progressScoreTexts[i] != null)
                {
                    _progressScoreTexts[i].text = bestSurvivors > 0
                        ? "Best Survivors " + NumberFormatter.ToCompact(bestSurvivors)
                        : "No clear yet";
                }

                if (i < _progressReplayButtons.Count && _progressReplayButtons[i] != null)
                {
                    var replayButton = _progressReplayButtons[i];
                    var canReplay = level <= unlocked;
                    replayButton.interactable = canReplay;
                    replayButton.onClick.RemoveAllListeners();
                    var levelCopy = Mathf.Max(1, level);
                    replayButton.onClick.AddListener(() =>
                    {
                        _requestedReplayLevel = levelCopy;
                        ToggleProgressOverlay();
                        Play();
                    });
                    var replayImage = replayButton.GetComponent<Image>();
                    if (replayImage != null)
                    {
                        replayImage.color = canReplay
                            ? new Color(0.22f, 0.74f, 1f, 0.96f)
                            : new Color(0.22f, 0.32f, 0.45f, 0.65f);
                    }
                }
            }
        }

        private void EnsureProgressRows(int requiredRows)
        {
            if (_progressContentRect == null)
            {
                return;
            }

            var safeRequiredRows = Mathf.Clamp(requiredRows, 1, 5000);
            EnsureBackdropSprites();
            for (var i = _progressRowRects.Count; i < safeRequiredRows; i++)
            {
                var row = FindOrCreateImage(
                    _progressContentRect,
                    "Row_" + i,
                    i % 2 == 0
                        ? new Color(0.12f, 0.26f, 0.42f, 0.84f)
                        : new Color(0.09f, 0.2f, 0.35f, 0.82f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(674f, 72f));
                if (row == null)
                {
                    continue;
                }

                var rowRect = row.rectTransform;
                rowRect.pivot = new Vector2(0.5f, 1f);
                _progressRowRects.Add(rowRect);
                var rowOutline = row.GetComponent<Outline>();
                if (rowOutline == null)
                {
                    rowOutline = row.gameObject.AddComponent<Outline>();
                }

                rowOutline.effectColor = new Color(0f, 0f, 0f, 0.48f);
                rowOutline.effectDistance = new Vector2(1.2f, -1.2f);

                var nodeGlow = FindOrCreateImage(
                    rowRect,
                    "NodeGlow",
                    new Color(0.36f, 0.84f, 1f, 0.62f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(12f, 0f),
                    new Vector2(36f, 36f));
                if (nodeGlow != null && _softOrbSprite != null)
                {
                    nodeGlow.sprite = _softOrbSprite;
                    nodeGlow.type = Image.Type.Simple;
                    nodeGlow.preserveAspect = true;
                }

                var levelLabel = FindOrCreateText(
                    rowRect,
                    "LevelLabel",
                    "LEVEL",
                    30,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(22f, 0f),
                    new Vector2(232f, 56f));
                StyleBodyText(levelLabel, 30, true);
                _progressLevelTexts.Add(levelLabel);

                var scoreLabel = FindOrCreateText(
                    rowRect,
                    "ScoreLabel",
                    "Best 0",
                    23,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-20f, 0f),
                    new Vector2(260f, 54f));
                StyleBodyText(scoreLabel, 23, false);
                _progressScoreTexts.Add(scoreLabel);

                var replayButton = EnsureDifficultyButton(rowRect, "ReplayButton", "REPLAY", new Vector2(244f, 0f), out var replayLabel);
                if (replayButton != null)
                {
                    var replayRect = replayButton.GetComponent<RectTransform>();
                    if (replayRect != null)
                    {
                        replayRect.sizeDelta = new Vector2(156f, 50f);
                    }

                    if (replayLabel != null)
                    {
                        replayLabel.fontSize = 22;
                    }

                    _progressReplayButtons.Add(replayButton);
                }
            }
        }

        private void OnDisable()
        {
            if (_menuVideoPlayer != null && _menuVideoPlayer.isPlaying)
            {
                _menuVideoPlayer.Stop();
            }
        }

        private void OnDestroy()
        {
            ReleaseVideoRenderTexture();
            if (_softOrbSprite != null)
            {
                Destroy(_softOrbSprite);
                _softOrbSprite = null;
            }

            if (_starOrbSprite != null)
            {
                Destroy(_starOrbSprite);
                _starOrbSprite = null;
            }

            if (_softOrbTexture != null)
            {
                Destroy(_softOrbTexture);
                _softOrbTexture = null;
            }

            if (_starOrbTexture != null)
            {
                Destroy(_starOrbTexture);
                _starOrbTexture = null;
            }
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
            rect.sizeDelta = new Vector2(520f, 56f);

            var label = textObject.GetComponent<Text>();
            if (label == null)
            {
                label = textObject.AddComponent<Text>();
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = new Color(0.9f, 0.96f, 1f, 1f);
            label.text = textValue;
            label.enabled = true;
            label.gameObject.SetActive(true);

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
            label.enabled = true;
            label.gameObject.SetActive(true);
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
