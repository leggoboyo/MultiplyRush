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

        private int _lastProgressPercent = -1;

        public void SetLevel(int levelIndex)
        {
            if (levelText != null)
            {
                levelText.text = "Level " + Mathf.Max(1, levelIndex);
            }
        }

        public void SetCount(int count)
        {
            if (countText != null)
            {
                countText.text = "Count: " + Mathf.Max(0, count);
            }
        }

        public void SetProgress(float progress01)
        {
            var clamped = Mathf.Clamp01(progress01);
            if (progressFill != null)
            {
                progressFill.fillAmount = clamped;
            }

            var percent = Mathf.RoundToInt(clamped * 100f);
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
    }
}
