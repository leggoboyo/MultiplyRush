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
        public static DifficultyRouteProfile BuildRouteProfile(DifficultyMode mode, bool isMiniBoss, int totalRows)
        {
            var rows = Mathf.Max(1, totalRows);
            float betterRatio;
            float worseRatio;

            if (!isMiniBoss)
            {
                switch (mode)
                {
                    case DifficultyMode.Easy:
                        // Easy: can still win while taking roughly 7 red + 8 worse out of 15 rows.
                        betterRatio = 0f;
                        worseRatio = 8f / 15f;
                        break;
                    case DifficultyMode.Hard:
                        // Hard: tuned around at least 10 better + up to 5 worse (scaled by row count).
                        betterRatio = 10f / 15f;
                        worseRatio = 5f / 15f;
                        break;
                    default:
                        // Normal: tuned so taking the worse-green route each row can still beat the enemy count.
                        betterRatio = 0f;
                        worseRatio = 1f;
                        break;
                }
            }
            else
            {
                switch (mode)
                {
                    case DifficultyMode.Easy:
                        // Mini-boss easy: around 5 red + 10 worse out of 15 rows.
                        betterRatio = 0f;
                        worseRatio = 10f / 15f;
                        break;
                    case DifficultyMode.Hard:
                        // Mini-boss hard: all better-route execution.
                        betterRatio = 1f;
                        worseRatio = 0f;
                        break;
                    default:
                        // Mini-boss normal: around 13 better + 2 worse out of 15 rows.
                        betterRatio = 13f / 15f;
                        worseRatio = 2f / 15f;
                        break;
                }
            }

            var better = Mathf.Clamp(Mathf.RoundToInt(rows * betterRatio), 0, rows);
            var worse = Mathf.Clamp(Mathf.RoundToInt(rows * worseRatio), 0, rows - better);
            var red = rows - better - worse;
            red = Mathf.Clamp(red, 0, rows);

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
