using System;
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
        public Button reinforcementButton;
        public Button shieldButton;
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

        public event Action OnRetryRequested;
        public event Action OnNextRequested;
        public event Action OnUseReinforcementRequested;
        public event Action OnUseShieldRequested;

        private CanvasGroup _canvasGroup;
        private RectTransform _panelRect;
        private Vector3 _panelBaseScale = Vector3.one;
        private RectTransform _titleRect;
        private Vector3 _titleBaseScale = Vector3.one;
        private RectTransform _scanlineRect;
        private Image _panelImage;
        private Image _headerGlow;
        private Image _dimImage;
        private ParticleSystem _winBurst;
        private bool _isVisible;
        private bool _isAnimatingIn;
        private bool _isAnimatingOut;
        private float _animTimer;
        private bool _lastDidWin;

        private void Awake()
        {
            if (rootPanel == null)
            {
                rootPanel = gameObject;
            }

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
                }
            }

            if (_panelRect == null && rootPanel != null)
            {
                _panelRect = rootPanel.GetComponent<RectTransform>();
                _panelBaseScale = _panelRect != null ? _panelRect.localScale : Vector3.one;
            }

            EnsureVisualPolish();
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
                titleText.color = didWin ? new Color(0.2f, 0.97f, 0.46f, 1f) : new Color(1f, 0.3f, 0.35f, 1f);
            }

            if (detailText != null)
            {
                detailText.text = BuildDetailText(levelIndex, playerCount, enemyCount, tankRequirement, extraDetail);
            }

            if (_panelImage != null)
            {
                _panelImage.color = didWin
                    ? new Color(0.06f, 0.1f, 0.2f, 0.95f)
                    : new Color(0.2f, 0.08f, 0.11f, 0.95f);
            }

            if (_headerGlow != null)
            {
                _headerGlow.color = didWin
                    ? new Color(0.16f, 0.95f, 0.56f, 0.18f)
                    : new Color(1f, 0.3f, 0.3f, 0.16f);
            }

            if (_dimImage != null)
            {
                _dimImage.color = new Color(0f, 0f, 0f, didWin ? 0.72f : 0.8f);
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(!didWin);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(didWin);
            }

            SetBuffOptions(0, 0);
            RefreshPrimaryButtonStyles();
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
        }

        private static string BuildDetailText(int levelIndex, int playerCount, int enemyCount, int tankRequirement, string extraDetail)
        {
            var detail = "Level " + levelIndex +
                         "\nYou: " + NumberFormatter.ToCompact(playerCount) +
                         "  Enemy: " + NumberFormatter.ToCompact(enemyCount);

            if (tankRequirement > 0)
            {
                detail += "\nTank Burst: " + NumberFormatter.ToCompact(tankRequirement);
            }

            if (!string.IsNullOrWhiteSpace(extraDetail))
            {
                var normalized = extraDetail.Trim().Replace(" • ", "\n");
                detail += "\n" + normalized;
            }

            return detail;
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
            if (titleText != null && _titleRect != null)
            {
                var pulse = 1f + Mathf.Sin(runTime * titlePulseSpeed) * titlePulseScale;
                _titleRect.localScale = _titleBaseScale * pulse;
            }

            if (_scanlineRect != null)
            {
                var y = Mathf.PingPong(runTime * 130f, 500f) - 250f;
                _scanlineRect.anchoredPosition = new Vector2(0f, y);
                var scanlineImage = _scanlineRect.GetComponent<Image>();
                if (scanlineImage != null)
                {
                    scanlineImage.color = new Color(0.62f, 0.92f, 1f, 0.08f + Mathf.Sin(runTime * 3f) * 0.03f);
                }
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

            _panelImage = _panelRect.GetComponent<Image>();
            if (_panelImage != null)
            {
                _panelImage.color = new Color(0.07f, 0.11f, 0.2f, 0.95f);
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
                titleText.fontSize = 112;
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

                _titleRect.anchoredPosition = new Vector2(0f, -90f);
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
                detailText.fontSize = 38;
                detailText.fontStyle = FontStyle.Normal;
                detailText.alignment = TextAnchor.UpperCenter;
                detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
                detailText.verticalOverflow = VerticalWrapMode.Overflow;
                detailText.lineSpacing = 1.08f;
                detailText.color = new Color(0.91f, 0.95f, 1f, 1f);

                var rect = detailText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 36f);
                rect.sizeDelta = new Vector2(660f, 320f);

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
                new Vector2(0f, -112f),
                new Vector2(620f, 172f));
            _headerGlow.transform.SetAsFirstSibling();

            _scanlineRect = EnsureImage(
                _panelRect,
                "Scanline",
                new Color(0.58f, 0.95f, 1f, 0.09f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 42f)).rectTransform;
            _scanlineRect.SetAsLastSibling();

            if (nextButton != null)
            {
                StylePrimaryButton(nextButton, new Color(0.15f, 0.72f, 1f, 1f), "NEXT LEVEL");
            }

            if (retryButton != null)
            {
                StylePrimaryButton(retryButton, new Color(0.88f, 0.3f, 0.34f, 1f), "RETRY");
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
                rect.sizeDelta = new Vector2(320f, 110f);
                rect.anchoredPosition = new Vector2(0f, -178f);
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
                label.fontSize = 44;
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
            if (_panelRect == null || _winBurst != null)
            {
                return;
            }

            var burstObject = new GameObject("WinBurst", typeof(RectTransform));
            burstObject.transform.SetParent(_panelRect, false);
            var rect = burstObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 64f);
            rect.sizeDelta = new Vector2(40f, 40f);

            _winBurst = burstObject.AddComponent<ParticleSystem>();
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
                    new Vector2(-174f, -270f),
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
                    new Vector2(174f, -270f),
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
                reinforcementButton.onClick.AddListener(() => OnUseReinforcementRequested?.Invoke());
                reinforcementButton.gameObject.SetActive(false);
            }

            if (shieldButton != null)
            {
                shieldButton.onClick.RemoveAllListeners();
                shieldButton.onClick.AddListener(() => OnUseShieldRequested?.Invoke());
                shieldButton.gameObject.SetActive(false);
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
            rect.sizeDelta = new Vector2(288f, 76f);

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
            label.fontSize = 30;
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
