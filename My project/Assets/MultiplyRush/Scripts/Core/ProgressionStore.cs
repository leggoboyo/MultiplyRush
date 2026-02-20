using UnityEngine;

namespace MultiplyRush
{
    public struct MiniBossReward
    {
        public int reinforcementKits;
        public int shieldCharges;

        public bool IsEmpty => reinforcementKits <= 0 && shieldCharges <= 0;
    }

    public static class ProgressionStore
    {
        private const string UnlockedLevelKey = "mr_unlocked_level";
        private const string BestLevelKey = "mr_best_level";
        private const string ReinforcementKitKey = "mr_reinforcement_kits";
        private const string ShieldChargeKey = "mr_shield_charges";
        private const string LastMiniBossRewardLevelKey = "mr_last_miniboss_reward_level";

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

        public static int GetReinforcementKits()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(ReinforcementKitKey, 0));
        }

        public static int GetShieldCharges()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(ShieldChargeKey, 0));
        }

        public static bool TryConsumeReinforcementKit()
        {
            var current = GetReinforcementKits();
            if (current <= 0)
            {
                return false;
            }

            PlayerPrefs.SetInt(ReinforcementKitKey, current - 1);
            PlayerPrefs.Save();
            return true;
        }

        public static bool TryConsumeShieldCharge()
        {
            var current = GetShieldCharges();
            if (current <= 0)
            {
                return false;
            }

            PlayerPrefs.SetInt(ShieldChargeKey, current - 1);
            PlayerPrefs.Save();
            return true;
        }

        public static MiniBossReward GrantMiniBossReward(int levelIndex)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            var lastRewardedLevel = Mathf.Max(0, PlayerPrefs.GetInt(LastMiniBossRewardLevelKey, 0));
            if (safeLevel <= lastRewardedLevel)
            {
                return default;
            }

            var reward = new MiniBossReward
            {
                reinforcementKits = 1,
                shieldCharges = safeLevel % 20 == 0 ? 1 : 0
            };

            var reinforcementTotal = GetReinforcementKits() + reward.reinforcementKits;
            var shieldTotal = GetShieldCharges() + reward.shieldCharges;
            PlayerPrefs.SetInt(ReinforcementKitKey, reinforcementTotal);
            PlayerPrefs.SetInt(ShieldChargeKey, shieldTotal);
            PlayerPrefs.SetInt(LastMiniBossRewardLevelKey, safeLevel);
            PlayerPrefs.Save();

            return reward;
        }
    }
}
