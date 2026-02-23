using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class ResultOverlayController : MonoBehaviour
    {
        [Header("References")]
        public GameObject rootPanel;
        public Text titleText;
        public Text detailText;
        public Button retryButton;
        public Button nextButton;
        public Button mainMenuButton;
        public Button reinforcementButton;
        public Button shieldButton;
        public Text mainMenuButtonLabel;
        public Text reinforcementButtonLabel;
        public Text shieldButtonLabel;

        [Header("Animation")]
        public float fadeInDuration = 0.2f;
        public float fadeOutDuration = 0.12f;
        public float startScale = 0.86f;
        public float overshootScale = 1.05f;
        public float settleSpeed = 14f;
        public float titlePulseScale = 0.04f;
        public float titlePulseSpeed = 3.6f;

        [Header("Win Celebration FX")]
        public float winBackdropPulseSpeed = 0.76f;
        public float winBackdropStarDrift = 48f;
        public float winBackdropRaySweepSpeed = 190f;

        public event Action OnRetryRequested;
        public event Action OnNextRequested;
        public event Action OnMainMenuRequested;
        public event Action OnUseReinforcementRequested;
        public event Action OnUseShieldRequested;

        private CanvasGroup _canvasGroup;
        private RectTransform _panelRect;
        private Vector3 _panelBaseScale = Vector3.one;
        private Vector2 _panelBasePosition;
        private RectTransform _layoutRootRect;
        private Vector2 _lastLayoutSize = new Vector2(-1f, -1f);
        private RectTransform _titleRect;
        private Vector3 _titleBaseScale = Vector3.one;
        private RectTransform _scanlineRect;
        private Image _panelImage;
        private Image _headerGlow;
        private Image _dimImage;
        private Image _winSweepImage;
        private RectTransform _winSweepRect;
        private Image _loseBandPrimary;
        private Image _loseBandSecondary;
        private Image _loseNoiseBand;
        private RectTransform _loseBandPrimaryRect;
        private RectTransform _loseBandSecondaryRect;
        private RectTransform _loseNoiseBandRect;
        private ParticleSystem _winBurst;
        private Image _winAuraOuter;
        private RectTransform _winAuraOuterRect;
        private Image _winAuraCore;
        private RectTransform _winAuraCoreRect;
        private Image _winSilhouetteBand;
        private RectTransform _winSilhouetteBandRect;
        private readonly List<RectTransform> _winStarRects = new List<RectTransform>(28);
        private readonly List<float> _winStarBaseX = new List<float>(28);
        private readonly List<float> _winStarBaseY = new List<float>(28);
        private readonly List<float> _winStarSpeeds = new List<float>(28);
        private readonly List<float> _winStarPhases = new List<float>(28);
        private readonly List<RectTransform> _winRayRects = new List<RectTransform>(5);
        private readonly List<float> _winRayBaseY = new List<float>(5);
        private readonly List<float> _winRaySpeeds = new List<float>(5);
        private readonly List<float> _winRayPhases = new List<float>(5);
        private Texture2D _winStarTexture;
        private Texture2D _winGlowTexture;
        private Sprite _winStarSprite;
        private Sprite _winGlowSprite;
        private Color _scanlineBaseColor = new Color(0.58f, 0.95f, 1f, 0.09f);
        private bool _isVisible;
        private bool _isAnimatingIn;
        private bool _isAnimatingOut;
        private float _animTimer;
        private bool _lastDidWin;
        private float _loseImpact;

        private void Awake()
        {
            if (rootPanel == null)
            {
                rootPanel = gameObject;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() =>
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.82f, 0.98f);
                    OnRetryRequested?.Invoke();
                });
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() =>
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.84f, 1.02f);
                    OnNextRequested?.Invoke();
                });
            }

            _canvasGroup = rootPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = rootPanel.AddComponent<CanvasGroup>();
            }

            var panelTransform = rootPanel.transform.Find("Panel");
            if (panelTransform != null)
            {
                _panelRect = panelTransform.GetComponent<RectTransform>();
                if (_panelRect != null)
                {
                    _panelBaseScale = _panelRect.localScale;
                    _panelBasePosition = _panelRect.anchoredPosition;
                }
            }

            if (_panelRect == null && rootPanel != null)
            {
                _panelRect = rootPanel.GetComponent<RectTransform>();
                _panelBaseScale = _panelRect != null ? _panelRect.localScale : Vector3.one;
            }

            if (_panelRect != null)
            {
                _layoutRootRect = _panelRect.parent as RectTransform;
            }

            EnsureVisualPolish();
            RefreshResponsiveLayout(true);
            EnsureBuffButtons();

            if (rootPanel != null && !rootPanel.activeSelf)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            RefreshResponsiveLayout();

            if (_isAnimatingIn)
            {
                AnimateIn(Time.unscaledDeltaTime);
            }
            else if (_isAnimatingOut)
            {
                AnimateOut(Time.unscaledDeltaTime);
            }

            if (_isVisible)
            {
                AnimatePolish(Time.unscaledTime, Time.unscaledDeltaTime);
            }
        }

        public void ShowResult(
            bool didWin,
            int levelIndex,
            int playerCount,
            int enemyCount,
            int tankRequirement = 0,
            string extraDetail = null)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            _lastDidWin = didWin;
            _loseImpact = didWin ? 0f : 1f;
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
                _panelRect.anchoredPosition = _panelBasePosition;
            }

            if (titleText != null)
            {
                titleText.text = didWin ? "WIN" : "LOSE";
                titleText.color = didWin ? new Color(0.2f, 0.97f, 0.46f, 1f) : new Color(1f, 0.3f, 0.35f, 1f);
            }

            if (detailText != null)
            {
                detailText.text = BuildDetailText(
                    didWin,
                    levelIndex,
                    playerCount,
                    enemyCount,
                    tankRequirement,
                    extraDetail);
                var lineCount = 1;
                var detail = detailText.text;
                for (var i = 0; i < detail.Length; i++)
                {
                    if (detail[i] == '\n')
                    {
                        lineCount++;
                    }
                }

                if (lineCount > 12)
                {
                    detailText.fontSize = 27;
                    detailText.lineSpacing = 1.02f;
                }
                else if (lineCount > 9)
                {
                    detailText.fontSize = 30;
                    detailText.lineSpacing = 1.05f;
                }
                else
                {
                    detailText.fontSize = 33;
                    detailText.lineSpacing = 1.08f;
                }
            }

            if (_panelImage != null)
            {
                _panelImage.color = didWin
                    ? new Color(0.06f, 0.1f, 0.2f, 0.95f)
                    : new Color(0.16f, 0.04f, 0.08f, 0.96f);
            }

            if (_headerGlow != null)
            {
                _headerGlow.color = didWin
                    ? new Color(0.16f, 0.95f, 0.56f, 0.18f)
                    : new Color(1f, 0.24f, 0.28f, 0.24f);
            }

            if (_dimImage != null)
            {
                _dimImage.color = new Color(0f, 0f, 0f, didWin ? 0.72f : 0.84f);
            }

            _scanlineBaseColor = didWin
                ? new Color(0.58f, 0.95f, 1f, 0.07f)
                : new Color(1f, 0.32f, 0.42f, 0.08f);

            if (_winSweepImage != null)
            {
                _winSweepImage.gameObject.SetActive(false);
                _winSweepImage.color = didWin
                    ? new Color(0.72f, 0.98f, 1f, 0.08f)
                    : new Color(1f, 0.42f, 0.42f, 0.14f);
            }

            HapticsDirector.Instance?.Play(didWin ? HapticCue.Success : HapticCue.Failure);

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(!didWin);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(didWin);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
            }

            SetBuffOptions(0, 0);
            RefreshPrimaryButtonStyles();
            RefreshResponsiveLayout(true);
            TriggerWinBurst(didWin);
        }

        public void Hide()
        {
            if (!_isVisible)
            {
                if (rootPanel != null)
                {
                    rootPanel.SetActive(false);
                }

                SetBuffOptions(0, 0);
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

            TriggerWinBurst(false);
        }

        public void SetBuffOptions(int reinforcementCount, int shieldCount)
        {
            EnsureBuffButtons();
            var showBuffs = _isVisible && !_lastDidWin;
            var canUseReinforcement = showBuffs && reinforcementCount > 0;
            var canUseShield = showBuffs && shieldCount > 0;

            if (reinforcementButton != null)
            {
                reinforcementButton.gameObject.SetActive(canUseReinforcement);
                reinforcementButton.interactable = canUseReinforcement;
            }

            if (shieldButton != null)
            {
                shieldButton.gameObject.SetActive(canUseShield);
                shieldButton.interactable = canUseShield;
            }

            if (reinforcementButtonLabel != null)
            {
                reinforcementButtonLabel.text = "Use Kit (" + reinforcementCount + ")";
            }

            if (shieldButtonLabel != null)
            {
                shieldButtonLabel.text = "Use Shield (" + shieldCount + ")";
            }

            RefreshResponsiveLayout(true);
        }

        private static string BuildDetailText(
            bool didWin,
            int levelIndex,
            int playerCount,
            int enemyCount,
            int tankRequirement,
            string extraDetail)
        {
            var builder = new StringBuilder(256);
            builder.Append("<size=44><b>LEVEL ");
            builder.Append(levelIndex);
            builder.Append("</b></size>\n");
            builder.Append("<size=33>YOU <b>");
            builder.Append(NumberFormatter.ToCompact(playerCount));
            builder.Append("</b>   VS   ENEMY <b>");
            builder.Append(NumberFormatter.ToCompact(enemyCount));
            builder.Append("</b></size>");

            if (didWin)
            {
                builder.Append("\n<size=30><color=#90FFB6>Survivors ");
                builder.Append(NumberFormatter.ToCompact(Mathf.Max(0, playerCount)));
                builder.Append("</color></size>");
            }
            else
            {
                var needed = Mathf.Max(1, enemyCount - playerCount + 1);
                builder.Append("\n<size=30><color=#FFC0C9>Need ");
                builder.Append(NumberFormatter.ToCompact(needed));
                builder.Append(" more units</color></size>");
            }

            if (!string.IsNullOrWhiteSpace(extraDetail))
            {
                builder.Append("\n\n<size=29><color=#E4ECFF>");
                builder.Append(extraDetail.Trim());
                builder.Append("</color></size>");
            }

            return builder.ToString();
        }

        private void AnimateIn(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

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
            if (deltaTime <= 0f)
            {
                return;
            }

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

                SetBuffOptions(0, 0);
            }
        }

        private void AnimatePolish(float runTime, float deltaTime)
        {
            var panelOffset = Vector2.zero;
            AnimateWinBackdrop(runTime, deltaTime);
            if (titleText != null && _titleRect != null)
            {
                var pulse = 1f + Mathf.Sin(runTime * titlePulseSpeed) * (_lastDidWin ? titlePulseScale : titlePulseScale * 0.82f);
                _titleRect.localScale = _titleBaseScale * pulse;
            }

            if (_scanlineRect != null)
            {
                var y = Mathf.PingPong(runTime * 130f, 500f) - 250f;
                _scanlineRect.anchoredPosition = new Vector2(0f, y);
                var scanlineImage = _scanlineRect.GetComponent<Image>();
                if (scanlineImage != null)
                {
                    var alphaPulse = _scanlineBaseColor.a + Mathf.Sin(runTime * 3f) * (_lastDidWin ? 0.025f : 0.018f);
                    scanlineImage.color = new Color(
                        _scanlineBaseColor.r,
                        _scanlineBaseColor.g,
                        _scanlineBaseColor.b,
                        Mathf.Clamp(alphaPulse, 0.01f, _lastDidWin ? 0.12f : 0.1f));
                }
            }

            if (_winSweepRect != null && _winSweepImage != null)
            {
                if (_lastDidWin)
                {
                    var width = _panelRect != null ? _panelRect.rect.width : 860f;
                    var x = Mathf.PingPong(runTime * 420f, width + 240f) - ((width * 0.5f) + 120f);
                    _winSweepRect.anchoredPosition = new Vector2(x, 8f);
                    _winSweepRect.localScale = new Vector3(1f, 1f + Mathf.Sin(runTime * 3.3f) * 0.08f, 1f);
                    var alpha = 0.14f + Mathf.Abs(Mathf.Sin(runTime * 2.3f)) * 0.13f;
                    _winSweepImage.color = new Color(0.72f, 0.98f, 1f, alpha);
                }
                else
                {
                    _winSweepRect.anchoredPosition = new Vector2(-1200f, 8f);
                }
            }

            if (!_lastDidWin)
            {
                _loseImpact = Mathf.MoveTowards(_loseImpact, 0f, deltaTime * 1.85f);
                var shakeX = Mathf.Sin(runTime * 15f) * (1.2f + _loseImpact * 0.8f);
                var shakeY = Mathf.Cos(runTime * 11f + 0.5f) * (0.65f + _loseImpact * 0.55f);
                panelOffset = new Vector2(shakeX, shakeY);

                if (_loseBandPrimaryRect != null && _loseBandPrimary != null)
                {
                    var width = _panelRect != null ? _panelRect.rect.width : 860f;
                    var x = Mathf.PingPong(runTime * 220f, width + 280f) - (width * 0.5f + 140f);
                    _loseBandPrimaryRect.anchoredPosition = new Vector2(x, 286f);
                    _loseBandPrimary.color = new Color(1f, 0.36f, 0.44f, 0.025f + Mathf.Abs(Mathf.Sin(runTime * 3.8f)) * 0.02f);
                }

                if (_loseBandSecondaryRect != null && _loseBandSecondary != null)
                {
                    var width = _panelRect != null ? _panelRect.rect.width : 860f;
                    var x = Mathf.PingPong(runTime * 175f + 120f, width + 260f) - (width * 0.5f + 130f);
                    _loseBandSecondaryRect.anchoredPosition = new Vector2(x, -300f);
                    _loseBandSecondary.color = new Color(1f, 0.24f, 0.3f, 0.02f + Mathf.Abs(Mathf.Sin(runTime * 2.9f + 0.9f)) * 0.02f);
                }

                if (_loseNoiseBandRect != null && _loseNoiseBand != null)
                {
                    var noiseY = Mathf.PingPong(runTime * 160f, 420f) - 210f;
                    _loseNoiseBandRect.anchoredPosition = new Vector2(0f, noiseY);
                    _loseNoiseBand.color = new Color(1f, 0.45f, 0.5f, 0.01f + Mathf.Abs(Mathf.Sin(runTime * 5.2f)) * 0.012f);
                }
            }
            else
            {
                if (_loseBandPrimaryRect != null)
                {
                    _loseBandPrimaryRect.anchoredPosition = new Vector2(-1800f, 48f);
                }

                if (_loseBandSecondaryRect != null)
                {
                    _loseBandSecondaryRect.anchoredPosition = new Vector2(1800f, -24f);
                }

                if (_loseNoiseBandRect != null)
                {
                    _loseNoiseBandRect.anchoredPosition = new Vector2(0f, 1200f);
                }
            }

            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = Vector2.Lerp(_panelRect.anchoredPosition, _panelBasePosition + panelOffset, 1f - Mathf.Exp(-16f * deltaTime));
            }

            var buttonPulse = 1f + Mathf.Sin(runTime * 4f) * 0.03f;
            ApplyButtonPulse(nextButton, buttonPulse, deltaTime);
            ApplyButtonPulse(retryButton, buttonPulse, deltaTime);
        }

        private static void ApplyButtonPulse(Button button, float pulse, float deltaTime)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            var target = Vector3.one * pulse;
            var blend = 1f - Mathf.Exp(-14f * Mathf.Max(0f, deltaTime));
            rect.localScale = Vector3.Lerp(rect.localScale, target, blend);
        }

        private void EnsureVisualPolish()
        {
            if (rootPanel == null)
            {
                return;
            }

            var dimTransform = rootPanel.transform.Find("Dim");
            if (dimTransform != null)
            {
                _dimImage = dimTransform.GetComponent<Image>();
            }

            if (_panelRect == null)
            {
                return;
            }

            EnsureMainMenuButton();
            _panelRect.sizeDelta = new Vector2(920f, 900f);

            _panelImage = _panelRect.GetComponent<Image>();
            if (_panelImage != null)
            {
                _panelImage.color = new Color(0.08f, 0.1f, 0.18f, 0.95f);
                var outline = _panelImage.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = _panelImage.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
                outline.effectDistance = new Vector2(2.4f, -2.4f);
            }

            if (titleText == null)
            {
                var title = _panelRect.Find("Title");
                if (title != null)
                {
                    titleText = title.GetComponent<Text>();
                }
            }

            if (titleText != null)
            {
                titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                titleText.fontSize = 102;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
                titleText.verticalOverflow = VerticalWrapMode.Overflow;

                _titleRect = titleText.rectTransform;
                _titleBaseScale = _titleRect.localScale;

                var outline = titleText.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = titleText.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.84f);
                outline.effectDistance = new Vector2(2.2f, -2.2f);

                _titleRect.anchoredPosition = new Vector2(0f, -96f);
            }

            if (detailText == null)
            {
                var detail = _panelRect.Find("Detail");
                if (detail != null)
                {
                    detailText = detail.GetComponent<Text>();
                }
            }

            if (detailText != null)
            {
                detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                detailText.fontSize = 33;
                detailText.fontStyle = FontStyle.Normal;
                detailText.alignment = TextAnchor.UpperCenter;
                detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
                detailText.verticalOverflow = VerticalWrapMode.Overflow;
                detailText.lineSpacing = 1.08f;
                detailText.color = new Color(0.91f, 0.95f, 1f, 1f);
                detailText.supportRichText = true;

                var rect = detailText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 20f);
                rect.sizeDelta = new Vector2(736f, 330f);

                var outline = detailText.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = detailText.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.62f);
                outline.effectDistance = new Vector2(1.4f, -1.4f);
            }

            _headerGlow = EnsureImage(
                _panelRect,
                "HeaderGlow",
                new Color(0.2f, 0.95f, 0.4f, 0.18f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -106f),
                new Vector2(700f, 182f));
            _headerGlow.transform.SetAsFirstSibling();

            _loseBandPrimary = EnsureImage(
                _panelRect,
                "LoseBandPrimary",
                new Color(1f, 0.35f, 0.43f, 0.06f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-1800f, 48f),
                new Vector2(120f, 360f));
            _loseBandPrimaryRect = _loseBandPrimary.rectTransform;
            _loseBandPrimaryRect.localRotation = Quaternion.Euler(0f, 0f, 12f);
            _loseBandPrimary.raycastTarget = false;
            _loseBandPrimaryRect.SetAsFirstSibling();

            _loseBandSecondary = EnsureImage(
                _panelRect,
                "LoseBandSecondary",
                new Color(1f, 0.22f, 0.3f, 0.05f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(1800f, -24f),
                new Vector2(110f, 340f));
            _loseBandSecondaryRect = _loseBandSecondary.rectTransform;
            _loseBandSecondaryRect.localRotation = Quaternion.Euler(0f, 0f, -9f);
            _loseBandSecondary.raycastTarget = false;
            _loseBandSecondaryRect.SetAsFirstSibling();

            _loseNoiseBand = EnsureImage(
                _panelRect,
                "LoseNoiseBand",
                new Color(1f, 0.45f, 0.5f, 0.02f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 1200f),
                new Vector2(760f, 36f));
            _loseNoiseBandRect = _loseNoiseBand.rectTransform;
            _loseNoiseBand.raycastTarget = false;
            _loseNoiseBandRect.SetAsFirstSibling();

            _scanlineRect = EnsureImage(
                _panelRect,
                "Scanline",
                new Color(0.58f, 0.95f, 1f, 0.09f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 42f)).rectTransform;
            _scanlineRect.SetAsFirstSibling();

            _winSweepImage = EnsureImage(
                _panelRect,
                "WinSweep",
                new Color(0.72f, 0.98f, 1f, 0.2f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-1000f, 8f),
                new Vector2(190f, 560f));
            _winSweepRect = _winSweepImage.rectTransform;
            _winSweepRect.localRotation = Quaternion.Euler(0f, 0f, 14f);
            _winSweepImage.raycastTarget = false;
            _winSweepImage.gameObject.SetActive(false);
            _winSweepRect.SetAsFirstSibling();

            EnsureWinBackdropEffects();

            if (nextButton != null)
            {
                StylePrimaryButton(nextButton, new Color(0.15f, 0.72f, 1f, 1f), "NEXT LEVEL");
            }

            if (retryButton != null)
            {
                StylePrimaryButton(retryButton, new Color(0.88f, 0.3f, 0.34f, 1f), "RETRY");
            }

            if (mainMenuButton != null)
            {
                StyleSecondaryButton(mainMenuButton, "MAIN MENU");
            }

            EnsureWinBurst();
        }

        private void RefreshPrimaryButtonStyles()
        {
            if (_lastDidWin)
            {
                if (nextButton != null)
                {
                    StylePrimaryButton(nextButton, new Color(0.12f, 0.7f, 1f, 1f), "NEXT LEVEL");
                }
            }
            else
            {
                if (retryButton != null)
                {
                    StylePrimaryButton(retryButton, new Color(0.94f, 0.34f, 0.38f, 1f), "TRY AGAIN");
                }
            }

            if (mainMenuButton != null)
            {
                StyleSecondaryButton(mainMenuButton, "MAIN MENU");
            }
        }

        private void RefreshResponsiveLayout(bool force = false)
        {
            if (_panelRect == null)
            {
                return;
            }

            if (_layoutRootRect == null)
            {
                _layoutRootRect = _panelRect.parent as RectTransform;
            }

            var container = _layoutRootRect != null ? _layoutRootRect.rect.size : _panelRect.rect.size;
            if (!force && (container - _lastLayoutSize).sqrMagnitude < 1f)
            {
                return;
            }

            _lastLayoutSize = container;
            var width = Mathf.Max(320f, container.x);
            var height = Mathf.Max(560f, container.y);
            var layoutProfile = IPhoneLayoutCatalog.ResolveCurrent();

            var compact = layoutProfile.compact || height < 1220f;
            var ultraCompact = layoutProfile.ultraCompact || height < 920f;
            var panelScale = Mathf.Clamp(layoutProfile.resultScale, 0.84f, 1.12f);

            var panelWidth = Mathf.Clamp((width - (ultraCompact ? 34f : 50f)) * panelScale, 520f, 920f);
            var panelHeight = Mathf.Clamp((height - (ultraCompact ? 200f : 230f)) * panelScale, ultraCompact ? 620f : 700f, 920f);
            _panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            if (_titleRect != null && titleText != null)
            {
                _titleRect.anchoredPosition = new Vector2(0f, -(panelHeight * 0.12f));
                titleText.fontSize = ultraCompact ? 74 : (compact ? 86 : 102);
            }

            if (detailText != null)
            {
                var detailRect = detailText.rectTransform;
                if (detailRect != null)
                {
                    detailRect.anchorMin = new Vector2(0.5f, 0.5f);
                    detailRect.anchorMax = new Vector2(0.5f, 0.5f);
                    detailRect.anchoredPosition = new Vector2(0f, panelHeight * 0.02f);
                    detailRect.sizeDelta = new Vector2(
                        Mathf.Clamp(panelWidth - 140f, 400f, 780f),
                        Mathf.Clamp(panelHeight * 0.42f, 230f, 390f));
                }
            }

            if (_headerGlow != null)
            {
                _headerGlow.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                _headerGlow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                _headerGlow.rectTransform.anchoredPosition = new Vector2(0f, -(panelHeight * 0.12f));
                _headerGlow.rectTransform.sizeDelta = new Vector2(panelWidth - 140f, Mathf.Clamp(panelHeight * 0.2f, 130f, 190f));
            }

            if (_scanlineRect != null)
            {
                _scanlineRect.sizeDelta = new Vector2(panelWidth - 140f, Mathf.Clamp(panelHeight * 0.045f, 30f, 44f));
            }

            if (_loseNoiseBandRect != null)
            {
                _loseNoiseBandRect.sizeDelta = new Vector2(panelWidth - 120f, Mathf.Clamp(panelHeight * 0.044f, 30f, 42f));
            }

            if (_winSweepRect != null)
            {
                _winSweepRect.sizeDelta = new Vector2(Mathf.Clamp(panelWidth * 0.21f, 140f, 210f), Mathf.Clamp(panelHeight * 0.64f, 430f, 600f));
            }

            if (_loseBandPrimaryRect != null)
            {
                _loseBandPrimaryRect.sizeDelta = new Vector2(Mathf.Clamp(panelWidth * 0.15f, 92f, 130f), Mathf.Clamp(panelHeight * 0.44f, 280f, 400f));
            }

            if (_loseBandSecondaryRect != null)
            {
                _loseBandSecondaryRect.sizeDelta = new Vector2(Mathf.Clamp(panelWidth * 0.14f, 88f, 122f), Mathf.Clamp(panelHeight * 0.42f, 260f, 380f));
            }

            if (_winBurst != null)
            {
                var burstRect = _winBurst.GetComponent<RectTransform>();
                if (burstRect != null)
                {
                    burstRect.anchoredPosition = new Vector2(0f, panelHeight * 0.07f);
                }
            }

            ConfigureWinBackdropLayout(panelWidth, panelHeight);
            ApplyResultLayout(panelWidth, panelHeight, compact, ultraCompact);
        }

        private void ApplyResultLayout(float panelWidth, float panelHeight, bool compact, bool ultraCompact)
        {
            var showBuffButtons = !_lastDidWin &&
                                  ((reinforcementButton != null && reinforcementButton.gameObject.activeSelf) ||
                                   (shieldButton != null && shieldButton.gameObject.activeSelf));

            var primaryButtonSize = new Vector2(
                Mathf.Clamp(panelWidth * 0.38f, 300f, 344f),
                ultraCompact ? 74f : (compact ? 80f : 86f));
            var secondaryButtonSize = new Vector2(
                Mathf.Clamp(panelWidth * 0.3f, 240f, 286f),
                ultraCompact ? 60f : 68f);

            var primaryY = -(panelHeight * (showBuffButtons ? 0.34f : 0.35f));
            var secondaryY = primaryY - (ultraCompact ? 66f : 74f);

            if (_lastDidWin)
            {
                ApplyButtonRect(nextButton, primaryButtonSize, new Vector2(0f, primaryY));
                ApplyButtonRect(mainMenuButton, secondaryButtonSize, new Vector2(0f, secondaryY));
                return;
            }

            if (showBuffButtons)
            {
                var buffWidth = Mathf.Clamp(panelWidth * 0.34f, 220f, 262f);
                var buffHeight = ultraCompact ? 56f : 64f;
                var buffOffset = Mathf.Clamp(panelWidth * 0.19f, 124f, 162f);
                var buffY = -(panelHeight * 0.24f);
                ApplyButtonRect(reinforcementButton, new Vector2(buffWidth, buffHeight), new Vector2(-buffOffset, buffY));
                ApplyButtonRect(shieldButton, new Vector2(buffWidth, buffHeight), new Vector2(buffOffset, buffY));
                ApplyButtonRect(mainMenuButton, secondaryButtonSize, new Vector2(0f, -(panelHeight * 0.36f)));
                ApplyButtonRect(retryButton, primaryButtonSize, new Vector2(0f, -(panelHeight * 0.45f)));
                return;
            }

            ApplyButtonRect(retryButton, primaryButtonSize, new Vector2(0f, primaryY));
            ApplyButtonRect(mainMenuButton, secondaryButtonSize, new Vector2(0f, secondaryY));
        }

        private static void ApplyButtonRect(Button button, Vector2 size, Vector2 anchoredPosition)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = Mathf.RoundToInt(Mathf.Clamp(size.y * 0.45f, 25f, 38f));
            }
        }

        private static void StylePrimaryButton(Button button, Color color, string labelText)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(330f, 86f);
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                var outline = image.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = image.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.text = labelText;
                label.fontSize = 38;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.95f, 1f);
            button.colors = colors;
        }

        private static void StyleSecondaryButton(Button button, string labelText)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(270f, 68f);
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.19f, 0.31f, 0.48f, 1f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.text = labelText;
                label.fontSize = 30;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
            }
        }

        private void EnsureWinBackdropEffects()
        {
            if (_panelRect == null)
            {
                return;
            }

            EnsureWinBackdropSprites();

            _winAuraOuter = EnsureImage(
                _panelRect,
                "WinAuraOuter",
                new Color(0.22f, 0.92f, 1f, 0.2f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 22f),
                new Vector2(760f, 520f));
            _winAuraOuterRect = _winAuraOuter.rectTransform;
            _winAuraOuter.raycastTarget = false;
            if (_winGlowSprite != null)
            {
                _winAuraOuter.sprite = _winGlowSprite;
                _winAuraOuter.type = Image.Type.Simple;
                _winAuraOuter.preserveAspect = true;
            }

            _winAuraCore = EnsureImage(
                _panelRect,
                "WinAuraCore",
                new Color(0.18f, 0.86f, 1f, 0.17f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 14f),
                new Vector2(520f, 340f));
            _winAuraCoreRect = _winAuraCore.rectTransform;
            _winAuraCore.raycastTarget = false;
            if (_winGlowSprite != null)
            {
                _winAuraCore.sprite = _winGlowSprite;
                _winAuraCore.type = Image.Type.Simple;
                _winAuraCore.preserveAspect = true;
            }

            _winSilhouetteBand = EnsureImage(
                _panelRect,
                "WinSilhouetteBand",
                new Color(0.64f, 0.96f, 1f, 0.04f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f),
                new Vector2(720f, 92f));
            _winSilhouetteBandRect = _winSilhouetteBand.rectTransform;
            _winSilhouetteBand.raycastTarget = false;
            if (_winGlowSprite != null)
            {
                _winSilhouetteBand.sprite = _winGlowSprite;
                _winSilhouetteBand.type = Image.Type.Simple;
                _winSilhouetteBand.preserveAspect = true;
            }

            const int rayCount = 0;
            _winRayRects.Clear();
            _winRayBaseY.Clear();
            _winRaySpeeds.Clear();
            _winRayPhases.Clear();
            for (var i = 0; i < rayCount; i++)
            {
                var ray = EnsureImage(
                    _panelRect,
                    "WinRay_" + i,
                    new Color(0.7f, 0.96f, 1f, 0.08f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(120f, 760f));
                if (ray == null)
                {
                    continue;
                }

                ray.raycastTarget = false;
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -16f + (i * 8f));
                _winRayRects.Add(ray.rectTransform);
                _winRayBaseY.Add(Mathf.Lerp(-50f, 260f, i / Mathf.Max(1f, rayCount - 1f)));
                _winRaySpeeds.Add(Mathf.Lerp(0.72f, 1.28f, i / Mathf.Max(1f, rayCount - 1f)));
                _winRayPhases.Add(Mathf.Repeat(i * 0.37f, 1f));
            }

            const int starCount = 28;
            _winStarRects.Clear();
            _winStarBaseX.Clear();
            _winStarBaseY.Clear();
            _winStarSpeeds.Clear();
            _winStarPhases.Clear();
            for (var i = 0; i < starCount; i++)
            {
                var star = EnsureImage(
                    _panelRect,
                    "WinStar_" + i,
                    new Color(0.7f, 0.96f, 1f, 0.32f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(18f, 18f));
                if (star == null)
                {
                    continue;
                }

                if (_winStarSprite != null)
                {
                    star.sprite = _winStarSprite;
                    star.type = Image.Type.Simple;
                    star.preserveAspect = true;
                }

                star.raycastTarget = false;
                var nx = Mathf.Repeat((i * 0.38197f) + 0.13f, 1f) * 2f - 1f;
                var ny = Mathf.Repeat((i * 0.61803f) + 0.29f, 1f) * 2f - 1f;
                _winStarRects.Add(star.rectTransform);
                _winStarBaseX.Add(nx);
                _winStarBaseY.Add(ny);
                _winStarSpeeds.Add(Mathf.Lerp(0.7f, 1.5f, Mathf.Repeat(i * 0.173f, 1f)));
                _winStarPhases.Add(Mathf.Repeat(i * 0.347f, 1f));
            }

            HideLegacyWinRays();

            if (_winAuraOuterRect != null)
            {
                _winAuraOuterRect.SetAsFirstSibling();
            }

            if (_winAuraCoreRect != null)
            {
                _winAuraCoreRect.SetAsFirstSibling();
            }

            if (_winSilhouetteBandRect != null)
            {
                _winSilhouetteBandRect.SetAsFirstSibling();
            }

            for (var i = 0; i < _winRayRects.Count; i++)
            {
                if (_winRayRects[i] != null)
                {
                    _winRayRects[i].SetAsFirstSibling();
                }
            }

            for (var i = 0; i < _winStarRects.Count; i++)
            {
                if (_winStarRects[i] != null)
                {
                    _winStarRects[i].SetAsFirstSibling();
                }
            }
        }

        private void ConfigureWinBackdropLayout(float panelWidth, float panelHeight)
        {
            if (_winAuraOuterRect != null)
            {
                _winAuraOuterRect.anchoredPosition = new Vector2(0f, panelHeight * 0.03f);
                _winAuraOuterRect.sizeDelta = new Vector2(panelWidth * 0.95f, panelHeight * 0.72f);
            }

            if (_winAuraCoreRect != null)
            {
                _winAuraCoreRect.anchoredPosition = new Vector2(0f, panelHeight * 0.02f);
                _winAuraCoreRect.sizeDelta = new Vector2(panelWidth * 0.66f, panelHeight * 0.42f);
            }

            if (_winSilhouetteBandRect != null)
            {
                _winSilhouetteBandRect.anchoredPosition = new Vector2(0f, panelHeight * 0.28f);
                _winSilhouetteBandRect.sizeDelta = new Vector2(panelWidth * 0.76f, panelHeight * 0.08f);
            }

            var rayCount = Mathf.Min(_winRayRects.Count, Mathf.Min(_winRayBaseY.Count, _winRaySpeeds.Count));
            for (var i = 0; i < rayCount; i++)
            {
                var rayRect = _winRayRects[i];
                if (rayRect == null)
                {
                    continue;
                }

                rayRect.sizeDelta = new Vector2(Mathf.Clamp(panelWidth * 0.15f, 88f, 150f), panelHeight * 0.86f);
                var yNorm = Mathf.Lerp(-0.2f, 0.34f, i / Mathf.Max(1f, rayCount - 1f));
                _winRayBaseY[i] = yNorm * panelHeight;
            }

            var starCount = Mathf.Min(_winStarRects.Count, Mathf.Min(_winStarBaseX.Count, _winStarBaseY.Count));
            for (var i = 0; i < starCount; i++)
            {
                var starRect = _winStarRects[i];
                if (starRect == null)
                {
                    continue;
                }

                var sizeT = Mathf.Repeat(i * 0.271f, 1f);
                var size = Mathf.Lerp(panelWidth * 0.01f, panelWidth * 0.026f, sizeT);
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(
                    _winStarBaseX[i] * (panelWidth * 0.42f),
                    _winStarBaseY[i] * (panelHeight * 0.35f));
            }
        }

        private void AnimateWinBackdrop(float runTime, float deltaTime)
        {
            var alphaBlend = 1f - Mathf.Exp(-Mathf.Max(1f, 9f) * Mathf.Max(0f, deltaTime));

            if (_winAuraOuter != null)
            {
                var current = _winAuraOuter.color.a;
                var target = _lastDidWin
                    ? (0.1f + Mathf.Abs(Mathf.Sin(runTime * winBackdropPulseSpeed * 1.3f)) * 0.11f)
                    : 0f;
                var alpha = Mathf.Lerp(current, target, alphaBlend);
                _winAuraOuter.color = new Color(0.2f, 0.92f, 1f, alpha);
                if (_winAuraOuterRect != null)
                {
                    var scale = 1f + Mathf.Sin(runTime * (winBackdropPulseSpeed * 1.15f)) * 0.05f;
                    _winAuraOuterRect.localScale = Vector3.one * scale;
                }
            }

            if (_winAuraCore != null)
            {
                var current = _winAuraCore.color.a;
                var target = _lastDidWin
                    ? (0.08f + Mathf.Abs(Mathf.Sin((runTime * winBackdropPulseSpeed * 1.8f) + 0.6f)) * 0.1f)
                    : 0f;
                var alpha = Mathf.Lerp(current, target, alphaBlend);
                _winAuraCore.color = new Color(0.18f, 0.82f, 1f, alpha);
                if (_winAuraCoreRect != null)
                {
                    var scale = 1f + Mathf.Sin((runTime * winBackdropPulseSpeed * 1.42f) + 1.1f) * 0.08f;
                    _winAuraCoreRect.localScale = Vector3.one * scale;
                }
            }

            if (_winSilhouetteBand != null)
            {
                var current = _winSilhouetteBand.color.a;
                var target = _lastDidWin
                    ? (0.02f + Mathf.Abs(Mathf.Sin((runTime * winBackdropPulseSpeed * 2.5f) + 0.2f)) * 0.04f)
                    : 0f;
                var alpha = Mathf.Lerp(current, target, alphaBlend);
                _winSilhouetteBand.color = new Color(0.66f, 0.98f, 1f, alpha);
            }

            var rayCount = Mathf.Min(_winRayRects.Count, Mathf.Min(_winRayBaseY.Count, Mathf.Min(_winRaySpeeds.Count, _winRayPhases.Count)));
            var panelWidth = _panelRect != null ? _panelRect.rect.width : 860f;
            for (var i = 0; i < rayCount; i++)
            {
                var rayRect = _winRayRects[i];
                if (rayRect == null)
                {
                    continue;
                }

                var image = rayRect.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                var travel = panelWidth + 340f;
                var x = Mathf.PingPong(
                    (runTime * winBackdropRaySweepSpeed * _winRaySpeeds[i]) + (_winRayPhases[i] * travel),
                    travel) - (travel * 0.5f);
                rayRect.anchoredPosition = new Vector2(x, _winRayBaseY[i]);
                var targetAlpha = _lastDidWin
                    ? (0.035f + Mathf.Abs(Mathf.Sin((runTime * 2.8f) + _winRayPhases[i] * 4f)) * 0.05f)
                    : 0f;
                image.color = new Color(0.72f, 0.98f, 1f, Mathf.Lerp(image.color.a, targetAlpha, alphaBlend));
            }

            var starCount = Mathf.Min(_winStarRects.Count, Mathf.Min(_winStarBaseX.Count, Mathf.Min(_winStarBaseY.Count, Mathf.Min(_winStarSpeeds.Count, _winStarPhases.Count))));
            var panelHeight = _panelRect != null ? _panelRect.rect.height : 860f;
            for (var i = 0; i < starCount; i++)
            {
                var starRect = _winStarRects[i];
                if (starRect == null)
                {
                    continue;
                }

                var image = starRect.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                var phase = _winStarPhases[i] * Mathf.PI * 2f;
                var driftX = Mathf.Sin((runTime * _winStarSpeeds[i] * 0.92f) + phase) * 18f;
                var driftY = Mathf.Cos((runTime * _winStarSpeeds[i] * 1.14f) + phase) * 22f;
                starRect.anchoredPosition = new Vector2(
                    (_winStarBaseX[i] * (panelWidth * 0.42f)) + driftX,
                    (_winStarBaseY[i] * (panelHeight * 0.35f)) + driftY);
                var twinkle = 0.12f + Mathf.Abs(Mathf.Sin((runTime * winBackdropStarDrift * 0.03f * _winStarSpeeds[i]) + phase)) * 0.35f;
                var targetAlpha = _lastDidWin ? twinkle : 0f;
                image.color = new Color(0.78f, 0.98f, 1f, Mathf.Lerp(image.color.a, targetAlpha, alphaBlend));
                var scalePulse = 0.9f + Mathf.Abs(Mathf.Sin((runTime * 2.4f * _winStarSpeeds[i]) + phase)) * 0.4f;
                starRect.localScale = Vector3.Lerp(starRect.localScale, Vector3.one * scalePulse, alphaBlend);
            }
        }

        private void HideLegacyWinRays()
        {
            if (_panelRect == null)
            {
                return;
            }

            for (var i = 0; i < _panelRect.childCount; i++)
            {
                var child = _panelRect.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (!child.name.StartsWith("WinRay_", StringComparison.Ordinal))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private void EnsureWinBackdropSprites()
        {
            if (_winGlowTexture == null)
            {
                _winGlowTexture = BuildRadialTexture(196, 0.88f, 3.2f);
            }

            if (_winStarTexture == null)
            {
                _winStarTexture = BuildRadialTexture(84, 1f, 6f);
            }

            if (_winGlowTexture != null && _winGlowSprite == null)
            {
                _winGlowSprite = Sprite.Create(
                    _winGlowTexture,
                    new Rect(0f, 0f, _winGlowTexture.width, _winGlowTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            if (_winStarTexture != null && _winStarSprite == null)
            {
                _winStarSprite = Sprite.Create(
                    _winStarTexture,
                    new Rect(0f, 0f, _winStarTexture.width, _winStarTexture.height),
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

        private void TriggerWinBurst(bool shouldPlay)
        {
            if (_winBurst == null)
            {
                return;
            }

            if (shouldPlay)
            {
                _winBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _winBurst.Play();
            }
            else
            {
                _winBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void EnsureWinBurst()
        {
            if (_panelRect == null)
            {
                return;
            }

            if (_winBurst == null)
            {
                var existing = _panelRect.Find("WinBurst");
                if (existing != null)
                {
                    _winBurst = existing.GetComponent<ParticleSystem>();
                }
            }

            if (_winBurst == null)
            {
                var burstObject = new GameObject("WinBurst", typeof(RectTransform));
                burstObject.transform.SetParent(_panelRect, false);
                _winBurst = burstObject.AddComponent<ParticleSystem>();
            }

            var rectTransform = _winBurst.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = _winBurst.gameObject.AddComponent<RectTransform>();
            }

            var rect = rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 64f);
            rect.sizeDelta = new Vector2(40f, 40f);

            _winBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _winBurst.Clear(true);

            var main = _winBurst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.42f;
            main.startLifetime = 0.44f;
            main.startSpeed = 4.8f;
            main.startSize = 0.16f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.25f, 0.95f, 0.45f, 1f),
                new Color(0.18f, 0.76f, 1f, 1f));
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _winBurst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = _winBurst.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.28f;

            var velocity = _winBurst.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(1.8f);

            var colorOverLifetime = _winBurst.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.27f, 0.96f, 0.5f, 1f), 0f),
                    new GradientColorKey(new Color(0.22f, 0.8f, 1f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = colorGradient;
        }

        private void EnsureBuffButtons()
        {
            if (_panelRect == null)
            {
                return;
            }

            if (reinforcementButton == null)
            {
                reinforcementButton = CreateBuffButton(
                    "UseReinforcementButton",
                    new Vector2(-152f, -236f),
                    new Color(0.2f, 0.58f, 0.95f, 1f),
                    _panelRect,
                    out reinforcementButtonLabel);
            }
            else if (reinforcementButtonLabel == null)
            {
                reinforcementButtonLabel = reinforcementButton.GetComponentInChildren<Text>();
            }

            if (shieldButton == null)
            {
                shieldButton = CreateBuffButton(
                    "UseShieldButton",
                    new Vector2(152f, -236f),
                    new Color(0.25f, 0.75f, 0.4f, 1f),
                    _panelRect,
                    out shieldButtonLabel);
            }
            else if (shieldButtonLabel == null)
            {
                shieldButtonLabel = shieldButton.GetComponentInChildren<Text>();
            }

            if (reinforcementButton != null)
            {
                reinforcementButton.onClick.RemoveAllListeners();
                reinforcementButton.onClick.AddListener(() =>
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.82f, 1.04f);
                    OnUseReinforcementRequested?.Invoke();
                });
                reinforcementButton.gameObject.SetActive(false);
            }

            if (shieldButton != null)
            {
                shieldButton.onClick.RemoveAllListeners();
                shieldButton.onClick.AddListener(() =>
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.8f, 1f);
                    OnUseShieldRequested?.Invoke();
                });
                shieldButton.gameObject.SetActive(false);
            }
        }

        private void EnsureMainMenuButton()
        {
            if (_panelRect == null)
            {
                return;
            }

            if (mainMenuButton == null)
            {
                mainMenuButton = CreateBuffButton(
                    "MainMenuButton",
                    new Vector2(0f, -398f),
                    new Color(0.2f, 0.3f, 0.46f, 1f),
                    _panelRect,
                    out mainMenuButtonLabel);
            }
            else if (mainMenuButtonLabel == null)
            {
                mainMenuButtonLabel = mainMenuButton.GetComponentInChildren<Text>();
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() =>
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.ButtonTap, 0.82f, 1f);
                    OnMainMenuRequested?.Invoke();
                });
            }
        }

        private static Button CreateBuffButton(
            string objectName,
            Vector2 anchoredPosition,
            Color color,
            RectTransform parent,
            out Text label)
        {
            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(248f, 64f);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            label = labelObject.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.text = objectName;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return button;
        }

        private static Image EnsureImage(
            RectTransform parent,
            string objectName,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var existing = parent.Find(objectName);
            GameObject imageObject;
            if (existing == null)
            {
                imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
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

        private void OnDestroy()
        {
            if (_winStarSprite != null)
            {
                Destroy(_winStarSprite);
                _winStarSprite = null;
            }

            if (_winGlowSprite != null)
            {
                Destroy(_winGlowSprite);
                _winGlowSprite = null;
            }

            if (_winStarTexture != null)
            {
                Destroy(_winStarTexture);
                _winStarTexture = null;
            }

            if (_winGlowTexture != null)
            {
                Destroy(_winGlowTexture);
                _winGlowTexture = null;
            }
        }

        private void OnTransformParentChanged()
        {
            if (_panelRect == null)
            {
                return;
            }

            if (reinforcementButton != null && reinforcementButton.transform.parent != _panelRect)
            {
                reinforcementButton.transform.SetParent(_panelRect, false);
            }

            if (shieldButton != null && shieldButton.transform.parent != _panelRect)
            {
                shieldButton.transform.SetParent(_panelRect, false);
            }
        }
    }
}
