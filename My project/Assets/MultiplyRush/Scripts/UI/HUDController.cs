using UnityEngine;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class HUDController : MonoBehaviour
    {
        public Text levelText;
        public Text countText;
        public Text enemyCountText;
        public Text countDeltaText;
        public Text progressText;
        public Image progressFill;
        public float progressLerpSpeed = 10f;
        public float countLerpSpeed = 14f;
        public float countPulseScale = 0.1f;
        public float flashFadeSpeed = 6f;
        public float deltaRiseDistance = 18f;
        public float deltaLifetime = 0.5f;
        public float deltaFadeSpeed = 5f;

        private int _lastProgressPercent = -1;
        private RectTransform _countRect;
        private RectTransform _enemyCountRect;
        private RectTransform _enemyBadgeRect;
        private Vector3 _countBaseScale = Vector3.one;
        private Color _countBaseColor = Color.white;
        private Color _enemyCountBaseColor = new Color(1f, 0.42f, 0.38f, 1f);
        private Color _enemyBadgeBaseColor = new Color(0.28f, 0.04f, 0.08f, 0.72f);
        private Image _enemyBadgeImage;
        private Color _countFlashColor = Color.white;
        private int _targetCount;
        private float _displayCount;
        private bool _countInitialized;
        private float _countFlash;
        private int _targetEnemyCount;
        private float _displayEnemyCount;
        private bool _enemyCountInitialized;
        private bool _enemyCountVisible;
        private float _enemyCountFlash;
        private float _targetProgress;
        private float _displayProgress;
        private int _levelIndex = 1;
        private bool _isMiniBossLevel;
        private int _reinforcementKits;
        private int _shieldCharges;
        private DifficultyMode _difficultyMode = DifficultyMode.Normal;
        private RectTransform _deltaRect;
        private Vector2 _deltaBasePosition;
        private Color _deltaBaseColor = Color.white;
        private float _deltaTimer;

        private void Awake()
        {
            if (countText != null)
            {
                _countRect = countText.rectTransform;
                _countBaseScale = _countRect.localScale;
                _countBaseColor = countText.color;
            }

            EnsureEnemyCountLabel();
            EnsureEnemyCountBackdrop();
            if (enemyCountText != null)
            {
                _enemyCountRect = enemyCountText.rectTransform;
                _enemyCountBaseColor = enemyCountText.color;
                enemyCountText.gameObject.SetActive(false);
            }

            if (_enemyBadgeImage != null)
            {
                _enemyBadgeRect = _enemyBadgeImage.rectTransform;
                _enemyBadgeImage.gameObject.SetActive(false);
            }

            ApplyTextStyle(levelText, 20, FontStyle.Bold, new Color(0.94f, 0.97f, 1f, 1f));
            ApplyTextStyle(countText, 44, FontStyle.Bold, Color.white);
            ApplyTextStyle(enemyCountText, 34, FontStyle.Bold, new Color(1f, 0.42f, 0.38f, 1f));
            ApplyTextStyle(progressText, 22, FontStyle.Bold, new Color(0.92f, 0.96f, 1f, 1f));
            if (enemyCountText != null)
            {
                _enemyCountBaseColor = enemyCountText.color;
            }
            if (levelText != null)
            {
                levelText.lineSpacing = 1.05f;
                levelText.alignment = TextAnchor.UpperLeft;
                levelText.horizontalOverflow = HorizontalWrapMode.Wrap;
                levelText.verticalOverflow = VerticalWrapMode.Overflow;
                var levelRect = levelText.rectTransform;
                levelRect.sizeDelta = new Vector2(860f, 112f);
                levelRect.anchoredPosition = new Vector2(24f, -50f);
            }
            SetNonInteractive(levelText);
            SetNonInteractive(countText);
            SetNonInteractive(enemyCountText);
            SetNonInteractive(progressText);
            SetNonInteractive(progressFill);
            if (progressFill != null)
            {
                var progressBackground = progressFill.transform.parent.GetComponent<Image>();
                SetNonInteractive(progressBackground);
            }
            EnsureDeltaLabel();
            ApplyTextStyle(countDeltaText, 28, FontStyle.Bold, Color.white);
            SetNonInteractive(countDeltaText);

            if (countDeltaText != null)
            {
                _deltaRect = countDeltaText.rectTransform;
                _deltaBasePosition = _deltaRect.anchoredPosition;
                _deltaBaseColor = countDeltaText.color;
                countDeltaText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            AnimateProgress(Time.deltaTime);
            AnimateCount(Time.deltaTime);
            AnimateEnemyCount(Time.deltaTime);
            AnimateDeltaLabel(Time.deltaTime);
        }

        public void SetLevel(int levelIndex, string modifierName = null, bool isMiniBoss = false)
        {
            _levelIndex = Mathf.Max(1, levelIndex);
            _isMiniBossLevel = isMiniBoss;
            RefreshLevelLabel();
        }

        public void SetInventory(int reinforcementKits, int shieldCharges)
        {
            _reinforcementKits = Mathf.Max(0, reinforcementKits);
            _shieldCharges = Mathf.Max(0, shieldCharges);
            RefreshLevelLabel();
        }

        public void SetDifficulty(DifficultyMode mode)
        {
            _difficultyMode = mode;
            RefreshLevelLabel();
        }

        public void SetCount(int count)
        {
            var safeCount = Mathf.Max(0, count);
            if (!_countInitialized)
            {
                _targetCount = safeCount;
                _displayCount = safeCount;
                _countInitialized = true;
                if (countText != null)
                {
                    countText.text = "Count: " + NumberFormatter.ToCompact(safeCount);
                }
                return;
            }

            if (safeCount != _targetCount)
            {
                var delta = safeCount - _targetCount;
                _countFlash = 1f;
                _countFlashColor = safeCount >= _targetCount
                    ? new Color(0.45f, 1f, 0.45f, 1f)
                    : new Color(1f, 0.45f, 0.45f, 1f);
                ShowCountDelta(delta);
            }

            _targetCount = safeCount;
        }

        public void SetProgress(float progress01)
        {
            _targetProgress = Mathf.Clamp01(progress01);
        }

        public void SetEnemyCount(int count, bool visible = true)
        {
            var safeCount = Mathf.Max(0, count);
            _enemyCountVisible = visible;
            if (enemyCountText != null)
            {
                enemyCountText.gameObject.SetActive(visible);
            }
            if (_enemyBadgeImage != null)
            {
                _enemyBadgeImage.gameObject.SetActive(visible);
            }

            if (!_enemyCountInitialized)
            {
                _targetEnemyCount = safeCount;
                _displayEnemyCount = safeCount;
                _enemyCountInitialized = true;
                if (enemyCountText != null)
                {
                    enemyCountText.text = "Enemy: " + NumberFormatter.ToCompact(safeCount);
                }
                return;
            }

            if (safeCount != _targetEnemyCount)
            {
                _enemyCountFlash = 1f;
            }

            _targetEnemyCount = safeCount;
        }

        public void SetEnemyCountVisible(bool visible)
        {
            _enemyCountVisible = visible;
            if (enemyCountText != null)
            {
                enemyCountText.gameObject.SetActive(visible);
            }
            if (_enemyBadgeImage != null)
            {
                _enemyBadgeImage.gameObject.SetActive(visible);
            }
        }

        private void AnimateProgress(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-progressLerpSpeed * deltaTime);
            _displayProgress = Mathf.Lerp(_displayProgress, _targetProgress, blend);

            if (progressFill != null)
            {
                progressFill.fillAmount = _displayProgress;
            }

            var percent = Mathf.RoundToInt(_displayProgress * 100f);
            if (percent == _lastProgressPercent)
            {
                return;
            }

            _lastProgressPercent = percent;
            if (progressText != null)
            {
                progressText.text = percent + "%";
            }
        }

        private void AnimateCount(float deltaTime)
        {
            if (!_countInitialized || deltaTime <= 0f)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-countLerpSpeed * deltaTime);
            _displayCount = Mathf.Lerp(_displayCount, _targetCount, blend);
            var shownCount = Mathf.RoundToInt(_displayCount);
            if (countText != null)
            {
                countText.text = "Count: " + NumberFormatter.ToCompact(shownCount);
            }

            _countFlash = Mathf.MoveTowards(_countFlash, 0f, flashFadeSpeed * deltaTime);

            if (_countRect != null)
            {
                var scale = 1f + (_countFlash * countPulseScale);
                _countRect.localScale = _countBaseScale * scale;
            }

            if (countText != null)
            {
                countText.color = Color.Lerp(_countBaseColor, _countFlashColor, _countFlash);
            }
        }

        private void AnimateEnemyCount(float deltaTime)
        {
            if (!_enemyCountInitialized || !_enemyCountVisible || deltaTime <= 0f)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-Mathf.Max(8f, countLerpSpeed) * deltaTime);
            _displayEnemyCount = Mathf.Lerp(_displayEnemyCount, _targetEnemyCount, blend);
            var shownEnemyCount = Mathf.RoundToInt(_displayEnemyCount);
            if (enemyCountText != null)
            {
                enemyCountText.text = "Enemy: " + NumberFormatter.ToCompact(shownEnemyCount);
            }

            _enemyCountFlash = Mathf.MoveTowards(_enemyCountFlash, 0f, flashFadeSpeed * deltaTime);
            if (_enemyCountRect != null)
            {
                var pulse = 1f + (_enemyCountFlash * (countPulseScale * 0.78f));
                _enemyCountRect.localScale = Vector3.one * pulse;
            }

            if (enemyCountText != null)
            {
                enemyCountText.color = Color.Lerp(_enemyCountBaseColor, Color.white, _enemyCountFlash * 0.5f);
            }

            if (_enemyBadgeRect != null)
            {
                var pulse = 1f + (_enemyCountFlash * 0.08f);
                _enemyBadgeRect.localScale = Vector3.one * pulse;
            }

            if (_enemyBadgeImage != null)
            {
                _enemyBadgeImage.color = Color.Lerp(_enemyBadgeBaseColor, new Color(0.48f, 0.08f, 0.12f, 0.9f), _enemyCountFlash * 0.65f);
            }
        }

        private void EnsureDeltaLabel()
        {
            if (countDeltaText != null || countText == null)
            {
                return;
            }

            var deltaObject = new GameObject("CountDelta");
            deltaObject.transform.SetParent(countText.transform.parent, false);
            var deltaRect = deltaObject.AddComponent<RectTransform>();
            deltaRect.anchorMin = countText.rectTransform.anchorMin;
            deltaRect.anchorMax = countText.rectTransform.anchorMax;
            deltaRect.pivot = countText.rectTransform.pivot;
            deltaRect.sizeDelta = new Vector2(420f, 52f);
            deltaRect.anchoredPosition = countText.rectTransform.anchoredPosition + new Vector2(0f, -44f);

            countDeltaText = deltaObject.AddComponent<Text>();
            countDeltaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countDeltaText.alignment = TextAnchor.MiddleCenter;
            countDeltaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            countDeltaText.verticalOverflow = VerticalWrapMode.Overflow;
            countDeltaText.text = string.Empty;
            countDeltaText.raycastTarget = false;
        }

        private void EnsureEnemyCountLabel()
        {
            if (enemyCountText != null || countText == null)
            {
                return;
            }

            var enemyObject = new GameObject("EnemyCount");
            enemyObject.transform.SetParent(countText.transform.parent, false);
            var enemyRect = enemyObject.AddComponent<RectTransform>();
            enemyRect.anchorMin = countText.rectTransform.anchorMin;
            enemyRect.anchorMax = countText.rectTransform.anchorMax;
            enemyRect.pivot = countText.rectTransform.pivot;
            enemyRect.sizeDelta = new Vector2(520f, 66f);
            enemyRect.anchoredPosition = countText.rectTransform.anchoredPosition + new Vector2(0f, -112f);

            enemyCountText = enemyObject.AddComponent<Text>();
            enemyCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            enemyCountText.alignment = TextAnchor.MiddleCenter;
            enemyCountText.horizontalOverflow = HorizontalWrapMode.Overflow;
            enemyCountText.verticalOverflow = VerticalWrapMode.Overflow;
            enemyCountText.text = "Enemy: 0";
            enemyCountText.raycastTarget = false;
        }

        private void EnsureEnemyCountBackdrop()
        {
            if (enemyCountText == null)
            {
                return;
            }

            var parent = enemyCountText.transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find("EnemyCountBadge");
            if (existing != null)
            {
                _enemyBadgeImage = existing.GetComponent<Image>();
                if (_enemyBadgeImage == null)
                {
                    _enemyBadgeImage = existing.gameObject.AddComponent<Image>();
                }
            }
            else
            {
                var badgeObject = new GameObject("EnemyCountBadge", typeof(RectTransform), typeof(Image));
                badgeObject.transform.SetParent(parent, false);
                _enemyBadgeImage = badgeObject.GetComponent<Image>();
            }

            if (_enemyBadgeImage == null)
            {
                return;
            }

            _enemyBadgeImage.color = _enemyBadgeBaseColor;
            _enemyBadgeImage.raycastTarget = false;

            var badgeRect = _enemyBadgeImage.rectTransform;
            badgeRect.anchorMin = enemyCountText.rectTransform.anchorMin;
            badgeRect.anchorMax = enemyCountText.rectTransform.anchorMax;
            badgeRect.pivot = enemyCountText.rectTransform.pivot;
            badgeRect.anchoredPosition = enemyCountText.rectTransform.anchoredPosition + new Vector2(0f, 0f);
            badgeRect.sizeDelta = new Vector2(560f, 72f);

            var outline = _enemyBadgeImage.GetComponent<Outline>();
            if (outline == null)
            {
                outline = _enemyBadgeImage.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);
            var enemyIndex = enemyCountText.transform.GetSiblingIndex();
            _enemyBadgeImage.transform.SetSiblingIndex(Mathf.Max(0, enemyIndex - 1));
        }

        private void ShowCountDelta(int delta)
        {
            if (countDeltaText == null || delta == 0)
            {
                return;
            }

            _deltaTimer = Mathf.Max(0.08f, deltaLifetime);
            countDeltaText.gameObject.SetActive(true);
            countDeltaText.text = NumberFormatter.ToSignedCompact(delta);
            _deltaBaseColor = delta > 0
                ? new Color(0.4f, 1f, 0.5f, 1f)
                : new Color(1f, 0.52f, 0.46f, 1f);
            countDeltaText.color = _deltaBaseColor;

            if (_deltaRect != null)
            {
                _deltaRect.anchoredPosition = _deltaBasePosition;
                _deltaRect.localScale = Vector3.one;
            }
        }

        private void AnimateDeltaLabel(float deltaTime)
        {
            if (countDeltaText == null || _deltaTimer <= 0f || deltaTime <= 0f)
            {
                return;
            }

            _deltaTimer = Mathf.Max(0f, _deltaTimer - deltaTime);
            var duration = Mathf.Max(0.08f, deltaLifetime);
            var t = 1f - (_deltaTimer / duration);
            var eased = 1f - Mathf.Pow(1f - t, 2f);

            if (_deltaRect != null)
            {
                _deltaRect.anchoredPosition = _deltaBasePosition + new Vector2(0f, deltaRiseDistance * eased);
                var scale = 1f + Mathf.Sin(eased * Mathf.PI) * 0.08f;
                _deltaRect.localScale = new Vector3(scale, scale, 1f);
            }

            var alpha = Mathf.Clamp01(1f - (t * Mathf.Max(0.1f, deltaFadeSpeed / 8f)));
            countDeltaText.color = new Color(_deltaBaseColor.r, _deltaBaseColor.g, _deltaBaseColor.b, alpha);

            if (_deltaTimer <= 0f)
            {
                countDeltaText.gameObject.SetActive(false);
                if (_deltaRect != null)
                {
                    _deltaRect.anchoredPosition = _deltaBasePosition;
                    _deltaRect.localScale = Vector3.one;
                }
            }
        }

        private void RefreshLevelLabel()
        {
            if (levelText == null)
            {
                return;
            }

            var bossTag = _isMiniBossLevel ? " • Mini-Boss" : string.Empty;
            var modeLabel = GetModeLabel(_difficultyMode);
            levelText.text =
                "Level " + Mathf.Max(1, _levelIndex) + bossTag + " • " + modeLabel +
                "\nKits " + _reinforcementKits + " • Shields " + _shieldCharges;
        }

        private static string GetModeLabel(DifficultyMode mode)
        {
            switch (mode)
            {
                case DifficultyMode.Easy:
                    return "Easy";
                case DifficultyMode.Hard:
                    return "Hard";
                default:
                    return "Normal";
            }
        }

        private static void ApplyTextStyle(Text label, int fontSize, FontStyle style, Color color)
        {
            if (label == null)
            {
                return;
            }

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;

            var outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
        }

        private static void SetNonInteractive(Graphic graphic)
        {
            if (graphic == null)
            {
                return;
            }

            graphic.raycastTarget = false;
        }
    }
}
