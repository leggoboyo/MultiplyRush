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
        private RectTransform _backdropLayerRect;
        private RectTransform _backdropNebulaARect;
        private RectTransform _backdropNebulaBRect;
        private RectTransform _backdropHorizonRect;
        private readonly List<RectTransform> _backdropStripRects = new List<RectTransform>(16);
        private readonly List<float> _backdropStripSpeeds = new List<float>(16);
        private readonly List<float> _backdropStripPhases = new List<float>(16);
        private readonly List<float> _backdropStripBaseX = new List<float>(16);
        private readonly List<RectTransform> _backdropStarRects = new List<RectTransform>(40);
        private readonly List<float> _backdropStarSpeeds = new List<float>(40);
        private readonly List<float> _backdropStarPhases = new List<float>(40);
        private readonly List<float> _backdropStarBaseX = new List<float>(40);
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
            if (_musicTrackLabel != null && string.IsNullOrWhiteSpace(_musicTrackLabel.text))
            {
                RefreshMusicTrackLabel();
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
                "TopDecorStrip",
                new Color(0.18f, 0.52f, 0.86f, 0.34f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(640f, 44f)).rectTransform;
            if (_badgeRect != null)
            {
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
                _musicRow.sizeDelta = new Vector2(640f, 112f);
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

            _musicTrackLabel = EnsureDifficultyText(
                _musicRow,
                "MusicTrackLabel",
                "Hyper Neon",
                new Vector2(0f, 4f),
                33);
            if (_musicTrackLabel != null)
            {
                _musicTrackLabel.color = new Color(0.88f, 0.98f, 1f, 1f);
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
            var current = ProgressionStore.GetGameplayMusicTrack(0, trackCount);
            var next = (current + delta) % trackCount;
            if (next < 0)
            {
                next += trackCount;
            }

            audio.SetGameplayTrackIndex(next, false);
            audio.PreviewGameplayTrack(1.35f, AudioMusicCue.MainMenu);
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
            var compact = height < 1280f;

            if (_titleRect != null)
            {
                _titleRect.anchoredPosition = new Vector2(0f, compact ? 296f : 332f);
                _titleBasePosition = _titleRect.anchoredPosition;
                if (_titleText != null)
                {
                    _titleText.fontSize = compact ? 88 : 104;
                }
            }

            if (_taglineRect != null)
            {
                _taglineRect.anchoredPosition = new Vector2(0f, compact ? 214f : 244f);
                _taglineRect.sizeDelta = new Vector2(Mathf.Clamp(width - 120f, 420f, 980f), compact ? 66f : 76f);
                if (_taglineText != null)
                {
                    _taglineText.fontSize = compact ? 31 : 36;
                }

                _taglineBasePosition = _taglineRect.anchoredPosition;
            }

            if (_metaCardRect != null)
            {
                _metaCardRect.anchoredPosition = new Vector2(0f, compact ? -6f : 12f);
                _metaCardRect.sizeDelta = new Vector2(Mathf.Clamp(width - 90f, 560f, 840f), compact ? 346f : 380f);
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
                _bestLevelRect.anchoredPosition = new Vector2(0f, compact ? 126f : 136f);
                _bestLevelRect.sizeDelta = new Vector2(460f, 58f);
                if (bestLevelText != null)
                {
                    bestLevelText.fontSize = compact ? 52 : 56;
                }
            }

            if (_musicRow != null)
            {
                _musicRow.sizeDelta = new Vector2(Mathf.Clamp(width - 190f, 540f, 680f), compact ? 122f : 134f);
                _musicRow.anchoredPosition = new Vector2(0f, compact ? 28f : 34f);
            }

            if (_difficultyRow != null)
            {
                _difficultyRow.sizeDelta = new Vector2(Mathf.Clamp(width - 210f, 480f, 590f), compact ? 130f : 144f);
                _difficultyRow.anchoredPosition = new Vector2(0f, compact ? -96f : -108f);
            }

            if (_playButtonRect != null)
            {
                _playButtonRect.sizeDelta = new Vector2(Mathf.Clamp(width - 240f, 360f, 520f), compact ? 132f : 150f);
                _buttonBasePosition = new Vector2(0f, compact ? -262f : -314f);
            }

            if (_playButtonLabel != null)
            {
                _playButtonLabel.fontSize = compact ? 50 : 58;
            }

            if (_badgeRect != null)
            {
                _badgeRect.anchorMin = new Vector2(0.5f, 1f);
                _badgeRect.anchorMax = new Vector2(0.5f, 1f);
                _badgeRect.sizeDelta = new Vector2(Mathf.Clamp(width - 140f, 420f, 760f), compact ? 34f : 40f);
                _badgeRect.anchoredPosition = new Vector2(0f, compact ? -68f : -78f);
            }

            if (_studioRect != null)
            {
                _studioRect.anchoredPosition = new Vector2(0f, compact ? 256f : 286f);
                _studioRect.sizeDelta = new Vector2(Mathf.Clamp(width - 220f, 420f, 780f), compact ? 52f : 56f);
                if (_studioText != null)
                {
                    _studioText.fontSize = compact ? 26 : 30;
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
                _footerRect.sizeDelta = new Vector2(Mathf.Clamp(width - 140f, 460f, 920f), 56f);
                _footerRect.anchoredPosition = new Vector2(0f, 34f);
                _footerRect.gameObject.SetActive(false);
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

            _backdropNebulaARect = FindOrCreateImage(
                _backdropLayerRect,
                "NebulaA",
                new Color(0.24f, 0.55f, 1f, 0.2f),
                new Vector2(0.16f, 0.77f),
                new Vector2(0.16f, 0.77f),
                Vector2.zero,
                new Vector2(820f, 820f)).rectTransform;

            _backdropNebulaBRect = FindOrCreateImage(
                _backdropLayerRect,
                "NebulaB",
                new Color(0.04f, 0.82f, 0.96f, 0.14f),
                new Vector2(0.82f, 0.28f),
                new Vector2(0.82f, 0.28f),
                Vector2.zero,
                new Vector2(670f, 670f)).rectTransform;

            _backdropHorizonRect = FindOrCreateImage(
                _backdropLayerRect,
                "HorizonGlow",
                new Color(0.1f, 0.46f, 0.9f, 0.16f),
                new Vector2(0.5f, 0.56f),
                new Vector2(0.5f, 0.56f),
                Vector2.zero,
                new Vector2(1560f, 210f)).rectTransform;

            const int stripCount = 12;
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
                var width = Mathf.Lerp(44f, 148f, widthHash);
                var height = Mathf.Lerp(360f, 760f, 1f - widthHash);

                var strip = FindOrCreateImage(
                    _backdropLayerRect,
                    "DriftStrip_" + i,
                    new Color(0.4f, 0.78f, 1f, Mathf.Lerp(0.07f, 0.19f, Hash01(i + 317))),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(baseX, baseY),
                    new Vector2(width, height));
                if (strip == null)
                {
                    continue;
                }

                strip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-12f, 12f, Hash01(i + 347)));
                strip.raycastTarget = false;
                _backdropStripRects.Add(strip.rectTransform);
                _backdropStripSpeeds.Add(Mathf.Lerp(0.68f, 1.28f, speedHash));
                _backdropStripPhases.Add(phaseHash * 1000f);
                _backdropStripBaseX.Add(baseX);
            }

            const int starCount = 28;
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

                star.raycastTarget = false;
                _backdropStarRects.Add(star.rectTransform);
                _backdropStarSpeeds.Add(Mathf.Lerp(0.74f, 1.32f, speedHash));
                _backdropStarPhases.Add(phaseHash * 1400f);
                _backdropStarBaseX.Add(baseX);
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
                var x = _backdropStripBaseX[i] + Mathf.Sin(runTime * (0.55f + i * 0.06f)) * 26f;
                rect.anchoredPosition = new Vector2(x, y);
                var alphaPulse = 0.06f + Mathf.Abs(Mathf.Sin(runTime * (1.2f + i * 0.11f))) * 0.14f;
                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.36f, 0.78f, 1f, alphaPulse);
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
        }

        private static float Hash01(int seed)
        {
            var value = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
            return value - Mathf.Floor(value);
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
            if (string.IsNullOrWhiteSpace(trackName))
            {
                trackName = "Track " + (index + 1);
            }

            _musicTrackLabel.text = "#" + (index + 1) + "  " + trackName;
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
