using UnityEngine;

namespace MultiplyRush
{
    public struct DifficultyGateObjective
    {
        public int totalRows;
        public int maxRedHits;
        public int minBetterHits;
        public int maxWorseHits;
        public bool requirePerfect;
    }

    public static class DifficultyRules
    {
        public static DifficultyGateObjective BuildObjective(DifficultyMode mode, bool isMiniBoss, int totalRows)
        {
            var rows = Mathf.Max(1, totalRows);
            var objective = new DifficultyGateObjective
            {
                totalRows = rows,
                maxRedHits = rows,
                minBetterHits = 0,
                maxWorseHits = rows,
                requirePerfect = false
            };

            if (!isMiniBoss)
            {
                switch (mode)
                {
                    case DifficultyMode.Easy:
                        objective.maxRedHits = ScaleRows(rows, 7f / 15f);
                        break;
                    case DifficultyMode.Normal:
                        objective.maxRedHits = 0;
                        break;
                    case DifficultyMode.Hard:
                        objective.maxRedHits = 0;
                        objective.minBetterHits = ScaleRowsCeil(rows, 10f / 15f);
                        objective.maxWorseHits = Mathf.Max(0, rows - objective.minBetterHits);
                        break;
                }

                return objective;
            }

            switch (mode)
            {
                case DifficultyMode.Easy:
                    objective.maxRedHits = ScaleRows(rows, 5f / 15f);
                    objective.maxWorseHits = ScaleRows(rows, 10f / 15f);
                    break;
                case DifficultyMode.Normal:
                    objective.maxRedHits = 0;
                    objective.minBetterHits = ScaleRowsCeil(rows, 13f / 15f);
                    objective.maxWorseHits = Mathf.Max(0, rows - objective.minBetterHits);
                    break;
                case DifficultyMode.Hard:
                    objective.requirePerfect = true;
                    objective.maxRedHits = 0;
                    objective.minBetterHits = rows;
                    objective.maxWorseHits = 0;
                    break;
            }

            return objective;
        }

        public static bool EvaluateObjective(
            DifficultyGateObjective objective,
            int betterHits,
            int worseHits,
            int redHits,
            out string statusLine)
        {
            var safeBetter = Mathf.Max(0, betterHits);
            var safeWorse = Mathf.Max(0, worseHits);
            var safeRed = Mathf.Max(0, redHits);
            var rows = Mathf.Max(1, objective.totalRows);
            var hits = safeBetter + safeWorse + safeRed;

            var pass = true;
            if (safeRed > objective.maxRedHits)
            {
                pass = false;
            }

            if (safeBetter < objective.minBetterHits)
            {
                pass = false;
            }

            if (safeWorse > objective.maxWorseHits)
            {
                pass = false;
            }

            if (objective.requirePerfect && (safeRed > 0 || safeWorse > 0 || safeBetter < rows))
            {
                pass = false;
            }

            statusLine =
                "Gate Objective " + (pass ? "PASS" : "FAIL") +
                " • Better " + safeBetter + "/" + rows +
                " • Worse " + safeWorse + "/" + objective.maxWorseHits +
                " • Red " + safeRed + "/" + objective.maxRedHits +
                " • Hit " + hits + "/" + rows;
            return pass;
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

        private static int ScaleRows(int rows, float ratio)
        {
            return Mathf.Clamp(Mathf.RoundToInt(rows * ratio), 0, rows);
        }

        private static int ScaleRowsCeil(int rows, float ratio)
        {
            return Mathf.Clamp(Mathf.CeilToInt(rows * ratio), 0, rows);
        }
    }
}
