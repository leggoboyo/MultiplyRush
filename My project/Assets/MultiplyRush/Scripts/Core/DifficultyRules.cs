using UnityEngine;

namespace MultiplyRush
{
    public struct DifficultyRouteProfile
    {
        public int totalRows;
        public int betterRows;
        public int worseRows;
        public int redRows;
        public bool isMiniBoss;
    }

    public static class DifficultyRules
    {
        public static DifficultyRouteProfile BuildRouteProfile(
            DifficultyMode mode,
            bool isMiniBoss,
            int totalRows,
            int levelIndex)
        {
            var rows = Mathf.Max(1, totalRows);
            var level = Mathf.Max(1, levelIndex);
            var progression01 = Mathf.Clamp01((level - 1f) / 120f);
            float betterRatio;
            float worseRatio;
            float redRatio;

            if (!isMiniBoss)
            {
                switch (mode)
                {
                    case DifficultyMode.Easy:
                        betterRatio = Mathf.Lerp(0.12f, 0.22f, progression01);
                        redRatio = Mathf.Lerp(0.24f, 0.3f, progression01);
                        break;
                    case DifficultyMode.Hard:
                        betterRatio = Mathf.Lerp(0.58f, 0.72f, progression01);
                        redRatio = Mathf.Lerp(0.08f, 0.12f, progression01);
                        break;
                    default:
                        betterRatio = Mathf.Lerp(0.32f, 0.5f, progression01);
                        redRatio = Mathf.Lerp(0.16f, 0.2f, progression01);
                        break;
                }
            }
            else
            {
                switch (mode)
                {
                    case DifficultyMode.Easy:
                        betterRatio = Mathf.Lerp(0.22f, 0.36f, progression01);
                        redRatio = Mathf.Lerp(0.22f, 0.26f, progression01);
                        break;
                    case DifficultyMode.Hard:
                        betterRatio = Mathf.Lerp(0.74f, 0.88f, progression01);
                        redRatio = Mathf.Lerp(0.04f, 0.08f, progression01);
                        break;
                    default:
                        betterRatio = Mathf.Lerp(0.52f, 0.68f, progression01);
                        redRatio = Mathf.Lerp(0.1f, 0.14f, progression01);
                        break;
                }
            }

            betterRatio = Mathf.Clamp01(betterRatio);
            redRatio = Mathf.Clamp01(redRatio);
            worseRatio = Mathf.Clamp01(1f - betterRatio - redRatio);

            var better = Mathf.Clamp(Mathf.RoundToInt(rows * betterRatio), 0, rows);
            var red = Mathf.Clamp(Mathf.RoundToInt(rows * redRatio), 0, rows - better);
            var worse = Mathf.Clamp(rows - better - red, 0, rows);

            return new DifficultyRouteProfile
            {
                totalRows = rows,
                betterRows = better,
                worseRows = worse,
                redRows = red,
                isMiniBoss = isMiniBoss
            };
        }

        public static string BuildRoutePlanLabel(DifficultyRouteProfile profile)
        {
            return "Route Plan B" + profile.betterRows +
                   " / W" + profile.worseRows +
                   " / R" + profile.redRows;
        }

        public static string BuildRouteHitLabel(int betterHits, int worseHits, int redHits)
        {
            return "Route Hits B" + Mathf.Max(0, betterHits) +
                   " / W" + Mathf.Max(0, worseHits) +
                   " / R" + Mathf.Max(0, redHits);
        }

        public static string GetModeShortLabel(DifficultyMode mode)
        {
            switch (mode)
            {
                case DifficultyMode.Easy:
                    return "EASY";
                case DifficultyMode.Hard:
                    return "HARD";
                default:
                    return "NORM";
            }
        }
    }
}
