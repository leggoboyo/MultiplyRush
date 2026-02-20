using System;

namespace MultiplyRush
{
    public static class NumberFormatter
    {
        public static string ToCompact(int value)
        {
            var absValue = Math.Abs((long)value);
            if (absValue >= 1_000_000_000L)
            {
                return ToCompactWithSuffix(value, 1_000_000_000f, "B");
            }

            if (absValue >= 1_000_000L)
            {
                return ToCompactWithSuffix(value, 1_000_000f, "M");
            }

            if (absValue >= 1_000L)
            {
                return ToCompactWithSuffix(value, 1_000f, "K");
            }

            return value.ToString();
        }

        public static string ToSignedCompact(int delta)
        {
            if (delta > 0)
            {
                return "+" + ToCompact(delta);
            }

            if (delta < 0)
            {
                var abs = (int)Math.Min(int.MaxValue, Math.Abs((long)delta));
                return "-" + ToCompact(abs);
            }

            return "0";
        }

        private static string ToCompactWithSuffix(int value, float divisor, string suffix)
        {
            var scaled = value / divisor;
            if (Math.Abs(scaled) >= 100f)
            {
                return Math.Round(scaled).ToString("0") + suffix;
            }

            if (Math.Abs(scaled) >= 10f)
            {
                return scaled.ToString("0.0") + suffix;
            }

            return scaled.ToString("0.##") + suffix;
        }
    }
}
