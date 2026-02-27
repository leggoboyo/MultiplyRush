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
        private const float FloatSaveEpsilon = 0.0001f;
        private const string UnlockedLevelKey = "mr_unlocked_level";
        private const string BestLevelKey = "mr_best_level";
        private const string ReinforcementKitKey = "mr_reinforcement_kits";
        private const string ShieldChargeKey = "mr_shield_charges";
        private const string LastMiniBossRewardLevelKey = "mr_last_miniboss_reward_level";
        private const string DifficultyModeKey = "mr_difficulty_mode";
        private const string MasterVolumeKey = "mr_master_volume";
        private const string GraphicsFidelityKey = "mr_graphics_fidelity";
        private const string CameraMotionKey = "mr_camera_motion";
        private const string HapticsEnabledKey = "mr_haptics_enabled";
        private const string GameplayMusicTrackKey = "mr_gameplay_music_track";
        private const string RequestedStartLevelKey = "mr_requested_start_level";
        private const string LevelBestSurvivorsPrefix = "mr_level_best_survivors_";

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
            var changed = SetIntIfChanged(UnlockedLevelKey, unlocked);
            changed |= SetIntIfChanged(BestLevelKey, best);
            SaveIfChanged(changed);
        }

        public static int GetBestSurvivorsForLevel(int levelIndex)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            return Mathf.Max(0, PlayerPrefs.GetInt(LevelBestSurvivorsPrefix + safeLevel, 0));
        }

        public static void RecordBestSurvivorsForLevel(int levelIndex, int survivors)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            var safeSurvivors = Mathf.Max(0, survivors);
            var key = LevelBestSurvivorsPrefix + safeLevel;
            var existing = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
            if (safeSurvivors <= existing)
            {
                return;
            }

            var changed = SetIntIfChanged(key, safeSurvivors);
            SaveIfChanged(changed);
        }

        public static void SetRequestedStartLevel(int levelIndex)
        {
            var changed = SetIntIfChanged(RequestedStartLevelKey, Mathf.Max(0, levelIndex));
            SaveIfChanged(changed);
        }

        public static void ClearRequestedStartLevel()
        {
            if (!PlayerPrefs.HasKey(RequestedStartLevelKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(RequestedStartLevelKey);
            PlayerPrefs.Save();
        }

        public static int ConsumeRequestedStartLevel()
        {
            var level = Mathf.Max(0, PlayerPrefs.GetInt(RequestedStartLevelKey, 0));
            if (level > 0)
            {
                PlayerPrefs.DeleteKey(RequestedStartLevelKey);
                PlayerPrefs.Save();
            }

            return level;
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

            var changed = SetIntIfChanged(ReinforcementKitKey, current - 1);
            SaveIfChanged(changed);
            return true;
        }

        public static bool TryConsumeShieldCharge()
        {
            var current = GetShieldCharges();
            if (current <= 0)
            {
                return false;
            }

            var changed = SetIntIfChanged(ShieldChargeKey, current - 1);
            SaveIfChanged(changed);
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
            var changed = SetIntIfChanged(ReinforcementKitKey, reinforcementTotal);
            changed |= SetIntIfChanged(ShieldChargeKey, shieldTotal);
            changed |= SetIntIfChanged(LastMiniBossRewardLevelKey, safeLevel);
            SaveIfChanged(changed);

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
            var changed = SetIntIfChanged(DifficultyModeKey, (int)mode);
            SaveIfChanged(changed);
        }

        public static float GetMasterVolume(float fallback = 0.85f)
        {
            var safeFallback = Mathf.Clamp01(fallback);
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, safeFallback));
        }

        public static void SetMasterVolume(float volume)
        {
            var changed = SetFloatIfChanged(MasterVolumeKey, Mathf.Clamp01(volume));
            SaveIfChanged(changed);
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
            var changed = SetIntIfChanged(GraphicsFidelityKey, safeQuality);
            SaveIfChanged(changed);
        }

        public static float GetCameraMotionIntensity(float fallback = 0.45f)
        {
            var safeFallback = Mathf.Clamp01(fallback);
            return Mathf.Clamp01(PlayerPrefs.GetFloat(CameraMotionKey, safeFallback));
        }

        public static void SetCameraMotionIntensity(float intensity)
        {
            var changed = SetFloatIfChanged(CameraMotionKey, Mathf.Clamp01(intensity));
            SaveIfChanged(changed);
        }

        public static bool GetHapticsEnabled(bool fallback = true)
        {
            var fallbackValue = fallback ? 1 : 0;
            return PlayerPrefs.GetInt(HapticsEnabledKey, fallbackValue) != 0;
        }

        public static void SetHapticsEnabled(bool enabled)
        {
            var changed = SetIntIfChanged(HapticsEnabledKey, enabled ? 1 : 0);
            SaveIfChanged(changed);
        }

        public static int GetGameplayMusicTrack(int fallback = 0, int maxExclusive = 6)
        {
            var safeMax = Mathf.Max(1, maxExclusive);
            var safeFallback = Mathf.Clamp(fallback, 0, safeMax - 1);
            return Mathf.Clamp(PlayerPrefs.GetInt(GameplayMusicTrackKey, safeFallback), 0, safeMax - 1);
        }

        public static void SetGameplayMusicTrack(int index, int maxExclusive = 6)
        {
            var safeMax = Mathf.Max(1, maxExclusive);
            var changed = SetIntIfChanged(GameplayMusicTrackKey, Mathf.Clamp(index, 0, safeMax - 1));
            SaveIfChanged(changed);
        }

        public static void Flush()
        {
            PlayerPrefs.Save();
        }

        private static void SaveIfChanged(bool changed)
        {
            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        private static bool SetIntIfChanged(string key, int value)
        {
            var current = PlayerPrefs.GetInt(key, value);
            if (current == value)
            {
                return false;
            }

            PlayerPrefs.SetInt(key, value);
            return true;
        }

        private static bool SetFloatIfChanged(string key, float value)
        {
            var current = PlayerPrefs.GetFloat(key, value);
            if (Mathf.Abs(current - value) <= FloatSaveEpsilon)
            {
                return false;
            }

            PlayerPrefs.SetFloat(key, value);
            return true;
        }
    }
}
