using UnityEngine;

namespace MultiplyRush
{
    public static class ProgressionStore
    {
        private const string UnlockedLevelKey = "mr_unlocked_level";
        private const string BestLevelKey = "mr_best_level";

        public static int GetUnlockedLevel()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelKey, 1));
        }

        public static int GetBestLevel()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(BestLevelKey, 1));
        }

        public static void MarkLevelWon(int levelIndex)
        {
            var unlocked = Mathf.Max(GetUnlockedLevel(), levelIndex + 1);
            var best = Mathf.Max(GetBestLevel(), levelIndex);

            PlayerPrefs.SetInt(UnlockedLevelKey, unlocked);
            PlayerPrefs.SetInt(BestLevelKey, best);
            PlayerPrefs.Save();
        }
    }
}
