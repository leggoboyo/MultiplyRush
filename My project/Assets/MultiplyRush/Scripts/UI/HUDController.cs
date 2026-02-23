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
        private RectTransform _bossHealthRootRect;
        private Image _bossHealthFillImage;
        private Text _bossHealthLabel;
        private Vector2 _lastLayoutCanvasSize = new Vector2(-1f, -1f);
        private Vector3 _countBaseScale = Vector3.one;
        private Color _countBaseColor = Color.white;
        private Color _enemyCountBaseColor = new Color(1f, 0.42f, 0.38f, 1f);
        private Color _enemyBadgeBaseColor = new Color(0.2f, 0.05f, 0.08f, 0.66f);
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
        private bool _bossHealthVisible;
        private bool _bossHealthInitialized;
        private int _bossHealthMax;
        private int _targetBossHealth;
        private float _displayBossHealth;
        private float _targetBossHealth01;
        private float _displayBossHealth01;
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
            EnsureBossHealthBar();
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
            SetBossHealthVisible(false);

            ApplyTextStyle(levelText, 20, FontStyle.Bold, new Color(0.94f, 0.97f, 1f, 1f));
            ApplyTextStyle(countText, 44, FontStyle.Bold, Color.white);
            ApplyTextStyle(enemyCountText, 32, FontStyle.Bold, new Color(1f, 0.42f, 0.38f, 1f));
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

            RefreshEnemyCountLayout(true);
        }

        private void Update()
        {
            RefreshEnemyCountLayout();
            AnimateProgress(Time.deltaTime);
            AnimateCount(Time.deltaTime);
            AnimateEnemyCount(Time.deltaTime);
            AnimateBossHealth(Time.deltaTime);
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

        public void SetBossHealth(int currentHealth, int maxHealth, bool visible = true)
        {
            var safeMax = Mathf.Max(1, maxHealth);
            var safeCurrent = Mathf.Clamp(currentHealth, 0, safeMax);
            _bossHealthMax = safeMax;
            _targetBossHealth = safeCurrent;
            _targetBossHealth01 = safeCurrent / (float)safeMax;
            if (!_bossHealthInitialized)
            {
                _displayBossHealth = safeCurrent;
                _displayBossHealth01 = _targetBossHealth01;
                _bossHealthInitialized = true;
            }

            SetBossHealthVisible(visible);
        }

        public void SetBossHealthVisible(bool visible)
        {
            _bossHealthVisible = visible;
            if (_bossHealthRootRect != null)
            {
                _bossHealthRootRect.gameObject.SetActive(visible);
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

        private void AnimateBossHealth(float deltaTime)
        {
            if (!_bossHealthInitialized || !_bossHealthVisible || deltaTime <= 0f)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-Mathf.Max(7f, countLerpSpeed) * deltaTime);
            _displayBossHealth = Mathf.Lerp(_displayBossHealth, _targetBossHealth, blend);
            _displayBossHealth01 = Mathf.Lerp(_displayBossHealth01, _targetBossHealth01, blend);
            var shownHp = Mathf.Clamp(Mathf.RoundToInt(_displayBossHealth), 0, Mathf.Max(1, _bossHealthMax));
            if (_bossHealthFillImage != null)
            {
                _bossHealthFillImage.fillAmount = Mathf.Clamp01(_displayBossHealth01);
            }

            if (_bossHealthLabel != null)
            {
                _bossHealthLabel.text = "BOSS HP " + NumberFormatter.ToCompact(shownHp);
            }
        }

        private void RefreshEnemyCountLayout(bool force = false)
        {
            if (countText == null || enemyCountText == null)
            {
                return;
            }

            var canvas = countText.canvas;
            var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            var canvasSize = canvasRect != null
                ? canvasRect.rect.size
                : new Vector2(Screen.width, Screen.height);
            if (!force && (canvasSize - _lastLayoutCanvasSize).sqrMagnitude < 1f)
            {
                return;
            }

            _lastLayoutCanvasSize = canvasSize;

            var countRect = countText.rectTransform;
            var enemyRect = enemyCountText.rectTransform;
            enemyRect.anchorMin = countRect.anchorMin;
            enemyRect.anchorMax = countRect.anchorMax;
            enemyRect.pivot = countRect.pivot;

            var width = Mathf.Clamp(canvasSize.x * 0.43f, 320f, 430f);
            var height = Mathf.Clamp(canvasSize.y * 0.047f, 54f, 66f);
            var y = countRect.anchoredPosition.y - 154f;
            if (progressFill != null)
            {
                var progressRect = progressFill.transform.parent as RectTransform;
                if (progressRect != null)
                {
                    var progressBottom = progressRect.anchoredPosition.y - (progressRect.sizeDelta.y * 0.5f);
                    y = Mathf.Min(y, progressBottom - 34f);
                }
            }

            enemyRect.sizeDelta = new Vector2(width, height);
            enemyRect.anchoredPosition = new Vector2(countRect.anchoredPosition.x, y);

            if (_enemyBadgeRect != null)
            {
                _enemyBadgeRect.anchorMin = enemyRect.anchorMin;
                _enemyBadgeRect.anchorMax = enemyRect.anchorMax;
                _enemyBadgeRect.pivot = enemyRect.pivot;
                _enemyBadgeRect.anchoredPosition = enemyRect.anchoredPosition;
                _enemyBadgeRect.sizeDelta = enemyRect.sizeDelta + new Vector2(26f, 12f);
            }

            if (_bossHealthRootRect != null)
            {
                var widthBoss = Mathf.Clamp(canvasSize.x * 0.54f, 360f, 520f);
                _bossHealthRootRect.anchorMin = countRect.anchorMin;
                _bossHealthRootRect.anchorMax = countRect.anchorMax;
                _bossHealthRootRect.pivot = countRect.pivot;
                _bossHealthRootRect.sizeDelta = new Vector2(widthBoss, 56f);
                _bossHealthRootRect.anchoredPosition = new Vector2(countRect.anchoredPosition.x, countRect.anchoredPosition.y - 114f);
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
            enemyRect.sizeDelta = new Vector2(380f, 58f);
            enemyRect.anchoredPosition = countText.rectTransform.anchoredPosition + new Vector2(0f, -154f);

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
            badgeRect.sizeDelta = new Vector2(406f, 70f);

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

        private void EnsureBossHealthBar()
        {
            if (_bossHealthRootRect != null || countText == null)
            {
                return;
            }

            var parent = countText.transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var rootObj = new GameObject("BossHealthRoot", typeof(RectTransform), typeof(Image));
            rootObj.transform.SetParent(parent, false);
            _bossHealthRootRect = rootObj.GetComponent<RectTransform>();
            _bossHealthRootRect.anchorMin = countText.rectTransform.anchorMin;
            _bossHealthRootRect.anchorMax = countText.rectTransform.anchorMax;
            _bossHealthRootRect.pivot = countText.rectTransform.pivot;
            _bossHealthRootRect.sizeDelta = new Vector2(450f, 56f);
            _bossHealthRootRect.anchoredPosition = countText.rectTransform.anchoredPosition + new Vector2(0f, -114f);

            var rootImage = rootObj.GetComponent<Image>();
            rootImage.color = new Color(0.18f, 0.05f, 0.08f, 0.84f);
            rootImage.raycastTarget = false;
            var rootOutline = rootObj.GetComponent<Outline>();
            if (rootOutline == null)
            {
                rootOutline = rootObj.AddComponent<Outline>();
            }

            rootOutline.effectColor = new Color(0f, 0f, 0f, 0.62f);
            rootOutline.effectDistance = new Vector2(1.8f, -1.8f);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(_bossHealthRootRect, false);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(6f, 6f);
            fillRect.offsetMax = new Vector2(-6f, -6f);
            _bossHealthFillImage = fillObj.GetComponent<Image>();
            _bossHealthFillImage.type = Image.Type.Filled;
            _bossHealthFillImage.fillMethod = Image.FillMethod.Horizontal;
            _bossHealthFillImage.fillOrigin = 0;
            _bossHealthFillImage.fillAmount = 1f;
            _bossHealthFillImage.color = new Color(0.92f, 0.2f, 0.24f, 0.92f);
            _bossHealthFillImage.raycastTarget = false;

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(_bossHealthRootRect, false);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _bossHealthLabel = labelObj.GetComponent<Text>();
            _bossHealthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bossHealthLabel.fontSize = 30;
            _bossHealthLabel.fontStyle = FontStyle.Bold;
            _bossHealthLabel.alignment = TextAnchor.MiddleCenter;
            _bossHealthLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bossHealthLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _bossHealthLabel.color = new Color(1f, 0.86f, 0.86f, 1f);
            _bossHealthLabel.raycastTarget = false;
            _bossHealthLabel.text = "BOSS HP 0";
            var labelOutline = _bossHealthLabel.GetComponent<Outline>();
            if (labelOutline == null)
            {
                labelOutline = _bossHealthLabel.gameObject.AddComponent<Outline>();
            }

            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            labelOutline.effectDistance = new Vector2(1.2f, -1.2f);

            _bossHealthRootRect.gameObject.SetActive(false);
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
