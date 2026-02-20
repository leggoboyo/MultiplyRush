using UnityEngine;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class HUDController : MonoBehaviour
    {
        public Text levelText;
        public Text countText;
        public Text progressText;
        public Image progressFill;
        public float progressLerpSpeed = 10f;
        public float countLerpSpeed = 14f;
        public float countPulseScale = 0.1f;
        public float flashFadeSpeed = 6f;

        private int _lastProgressPercent = -1;
        private RectTransform _countRect;
        private Vector3 _countBaseScale = Vector3.one;
        private Color _countBaseColor = Color.white;
        private Color _countFlashColor = Color.white;
        private int _targetCount;
        private float _displayCount;
        private bool _countInitialized;
        private float _countFlash;
        private float _targetProgress;
        private float _displayProgress;
        private int _levelIndex = 1;
        private string _modifierName = "Core Rush";
        private bool _isMiniBossLevel;
        private int _reinforcementKits;
        private int _shieldCharges;

        private void Awake()
        {
            if (countText != null)
            {
                _countRect = countText.rectTransform;
                _countBaseScale = _countRect.localScale;
                _countBaseColor = countText.color;
            }

            ApplyTextStyle(levelText, 24, FontStyle.Bold, new Color(0.94f, 0.97f, 1f, 1f));
            ApplyTextStyle(countText, 44, FontStyle.Bold, Color.white);
            ApplyTextStyle(progressText, 22, FontStyle.Bold, new Color(0.92f, 0.96f, 1f, 1f));
        }

        private void Update()
        {
            AnimateProgress(Time.deltaTime);
            AnimateCount(Time.deltaTime);
        }

        public void SetLevel(int levelIndex, string modifierName = null, bool isMiniBoss = false)
        {
            _levelIndex = Mathf.Max(1, levelIndex);
            _modifierName = string.IsNullOrWhiteSpace(modifierName) ? "Core Rush" : modifierName;
            _isMiniBossLevel = isMiniBoss;
            RefreshLevelLabel();
        }

        public void SetInventory(int reinforcementKits, int shieldCharges)
        {
            _reinforcementKits = Mathf.Max(0, reinforcementKits);
            _shieldCharges = Mathf.Max(0, shieldCharges);
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
                    countText.text = "Count: " + safeCount;
                }
                return;
            }

            if (safeCount != _targetCount)
            {
                _countFlash = 1f;
                _countFlashColor = safeCount >= _targetCount
                    ? new Color(0.45f, 1f, 0.45f, 1f)
                    : new Color(1f, 0.45f, 0.45f, 1f);
            }

            _targetCount = safeCount;
        }

        public void SetProgress(float progress01)
        {
            _targetProgress = Mathf.Clamp01(progress01);
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
                countText.text = "Count: " + shownCount;
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

        private void RefreshLevelLabel()
        {
            if (levelText == null)
            {
                return;
            }

            var bossTag = _isMiniBossLevel ? " • Mini-Boss" : string.Empty;
            levelText.text =
                "L" + Mathf.Max(1, _levelIndex) + bossTag +
                " • " + AbbreviateModifier(_modifierName) +
                " • K" + _reinforcementKits +
                " S" + _shieldCharges;
        }

        private static string AbbreviateModifier(string modifierName)
        {
            if (string.IsNullOrWhiteSpace(modifierName))
            {
                return "Core";
            }

            switch (modifierName.Trim())
            {
                case "Core Rush":
                    return "Core";
                case "Drift Gates":
                    return "Drift";
                case "Tempo Lock":
                    return "Tempo";
                case "Hazard Surge":
                    return "Hazard";
                case "Pressure AI":
                    return "Pressure";
                case "Tank Legion":
                    return "Tank";
                default:
                    return modifierName;
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
    }
}
