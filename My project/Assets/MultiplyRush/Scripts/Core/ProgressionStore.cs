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
        private const string DifficultyModeKey = "mr_difficulty_mode";
        private const string MasterVolumeKey = "mr_master_volume";
        private const string GraphicsFidelityKey = "mr_graphics_fidelity";
        private const string CameraMotionKey = "mr_camera_motion";

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

        public static DifficultyMode GetDifficultyMode(DifficultyMode fallback = DifficultyMode.Normal)
        {
            var stored = PlayerPrefs.GetInt(DifficultyModeKey, (int)fallback);
            if (stored < (int)DifficultyMode.Easy || stored > (int)DifficultyMode.Hard)
            {
                return fallback;
            }

            return (DifficultyMode)stored;
        }

        public static void SetDifficultyMode(DifficultyMode mode)
        {
            PlayerPrefs.SetInt(DifficultyModeKey, (int)mode);
            PlayerPrefs.Save();
        }

        public static float GetMasterVolume(float fallback = 0.85f)
        {
            var safeFallback = Mathf.Clamp01(fallback);
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, safeFallback));
        }

        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        public static BackdropQuality GetGraphicsFidelity(BackdropQuality fallback = BackdropQuality.Auto)
        {
            var stored = PlayerPrefs.GetInt(GraphicsFidelityKey, (int)fallback);
            if (stored < (int)BackdropQuality.Auto || stored > (int)BackdropQuality.High)
            {
                return fallback;
            }

            return (BackdropQuality)stored;
        }

        public static void SetGraphicsFidelity(BackdropQuality quality)
        {
            var safeQuality = Mathf.Clamp((int)quality, (int)BackdropQuality.Auto, (int)BackdropQuality.High);
            PlayerPrefs.SetInt(GraphicsFidelityKey, safeQuality);
            PlayerPrefs.Save();
        }

        public static float GetCameraMotionIntensity(float fallback = 0.45f)
        {
            var safeFallback = Mathf.Clamp01(fallback);
            return Mathf.Clamp01(PlayerPrefs.GetFloat(CameraMotionKey, safeFallback));
        }

        public static void SetCameraMotionIntensity(float intensity)
        {
            PlayerPrefs.SetFloat(CameraMotionKey, Mathf.Clamp01(intensity));
            PlayerPrefs.Save();
        }
    }
}
