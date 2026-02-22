using UnityEngine;

namespace MultiplyRush
{
    public static class IPhoneLayoutCatalog
    {
        public struct LayoutProfile
        {
            public string name;
            public bool matchedKnownDevice;
            public bool compact;
            public bool ultraCompact;
            public float menuScale;
            public float pauseScale;
            public float resultScale;
            public float topInsetRatio;
            public float sideInsetRatio;
        }

        private struct DeviceSignature
        {
            public readonly string name;
            public readonly int shortSide;
            public readonly int longSide;
            public readonly float menuScale;
            public readonly float pauseScale;
            public readonly float resultScale;
            public readonly bool compact;
            public readonly bool ultraCompact;

            public DeviceSignature(
                string name,
                int shortSide,
                int longSide,
                float menuScale,
                float pauseScale,
                float resultScale,
                bool compact,
                bool ultraCompact)
            {
                this.name = name;
                this.shortSide = shortSide;
                this.longSide = longSide;
                this.menuScale = menuScale;
                this.pauseScale = pauseScale;
                this.resultScale = resultScale;
                this.compact = compact;
                this.ultraCompact = ultraCompact;
            }
        }

        // Known iPhone portrait resolutions (points + pixels), grouped by behavior.
        // This lets UI tuning stay stable across Unity Simulator and real-device runtime.
        private static readonly DeviceSignature[] KnownDevices =
        {
            // Legacy small
            new DeviceSignature("iPhone 4/4S", 320, 480, 0.86f, 0.84f, 0.84f, true, true),
            new DeviceSignature("iPhone 5/5S/SE", 320, 568, 0.9f, 0.88f, 0.88f, true, true),
            new DeviceSignature("iPhone 4/4S", 640, 960, 0.86f, 0.84f, 0.84f, true, true),
            new DeviceSignature("iPhone 5/5S/SE", 640, 1136, 0.9f, 0.88f, 0.88f, true, true),

            // Classic
            new DeviceSignature("iPhone 6/7/8/SE2/SE3", 375, 667, 0.93f, 0.9f, 0.9f, true, true),
            new DeviceSignature("iPhone 6/7/8 Plus", 414, 736, 0.96f, 0.93f, 0.93f, true, false),
            new DeviceSignature("iPhone 6/7/8/SE2/SE3", 750, 1334, 0.93f, 0.9f, 0.9f, true, true),
            new DeviceSignature("iPhone 6/7/8 Plus", 1080, 1920, 0.96f, 0.93f, 0.93f, true, false),

            // Notch compact
            new DeviceSignature("iPhone X/XS/11 Pro/12 mini/13 mini", 375, 812, 0.98f, 0.96f, 0.96f, false, false),
            new DeviceSignature("iPhone X/XS/11 Pro", 1125, 2436, 0.98f, 0.96f, 0.96f, false, false),
            new DeviceSignature("iPhone 12 mini/13 mini", 1080, 2340, 0.98f, 0.96f, 0.96f, false, false),

            // Notch regular
            new DeviceSignature("iPhone XR/11/11 Pro Max", 414, 896, 1f, 0.98f, 0.98f, false, false),
            new DeviceSignature("iPhone 12/13/14/16e", 390, 844, 1f, 1f, 1f, false, false),
            new DeviceSignature("iPhone 14 Pro/15/15 Pro/16", 393, 852, 1.01f, 1f, 1f, false, false),
            new DeviceSignature("iPhone XR/11", 828, 1792, 1f, 0.98f, 0.98f, false, false),
            new DeviceSignature("iPhone 11 Pro Max", 1242, 2688, 1f, 0.98f, 0.98f, false, false),
            new DeviceSignature("iPhone 12/13/14", 1170, 2532, 1f, 1f, 1f, false, false),
            new DeviceSignature("iPhone 14 Pro/15/15 Pro/16", 1179, 2556, 1.01f, 1f, 1f, false, false),

            // Large + Pro Max class
            new DeviceSignature("iPhone 12/13 Pro Max, 14 Plus", 428, 926, 1.03f, 1.04f, 1.04f, false, false),
            new DeviceSignature("iPhone 14 Pro Max/15 Plus/15 Pro Max/16 Plus", 430, 932, 1.04f, 1.05f, 1.05f, false, false),
            new DeviceSignature("iPhone 16 Pro", 402, 874, 1.02f, 1.02f, 1.02f, false, false),
            new DeviceSignature("iPhone 16 Pro Max", 440, 956, 1.06f, 1.07f, 1.07f, false, false),
            new DeviceSignature("iPhone 12/13 Pro Max, 14 Plus", 1284, 2778, 1.03f, 1.04f, 1.04f, false, false),
            new DeviceSignature("iPhone 14 Pro Max/15 Plus/15 Pro Max/16 Plus", 1290, 2796, 1.04f, 1.05f, 1.05f, false, false),
            new DeviceSignature("iPhone 16 Pro", 1206, 2622, 1.02f, 1.02f, 1.02f, false, false),
            new DeviceSignature("iPhone 16 Pro Max", 1320, 2868, 1.06f, 1.07f, 1.07f, false, false)
        };

        public static LayoutProfile ResolveCurrent()
        {
            return Resolve(Screen.width, Screen.height, Screen.safeArea);
        }

        public static LayoutProfile Resolve(int screenWidth, int screenHeight, Rect safeArea)
        {
            var shortSide = Mathf.Min(screenWidth, screenHeight);
            var longSide = Mathf.Max(screenWidth, screenHeight);

            var topInset = GetTopInset(screenWidth, screenHeight, safeArea);
            var sideInset = GetSideInset(screenWidth, screenHeight, safeArea);

            var fallback = BuildFallbackProfile(shortSide, longSide, topInset, sideInset);

            var bestIndex = -1;
            var bestScore = int.MaxValue;
            for (var i = 0; i < KnownDevices.Length; i++)
            {
                var signature = KnownDevices[i];
                var score = Mathf.Abs(signature.shortSide - shortSide) + Mathf.Abs(signature.longSide - longSide);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            // Accept close resolution matches and keep safe-area insets from runtime.
            if (bestIndex >= 0 && bestScore <= 96)
            {
                var signature = KnownDevices[bestIndex];
                fallback.name = signature.name;
                fallback.matchedKnownDevice = true;
                fallback.compact = signature.compact;
                fallback.ultraCompact = signature.ultraCompact;
                fallback.menuScale = signature.menuScale;
                fallback.pauseScale = signature.pauseScale;
                fallback.resultScale = signature.resultScale;
            }

            // Dynamic-island and heavy-notch devices need extra top breathing room.
            if (fallback.topInsetRatio > 0.052f)
            {
                fallback.menuScale *= 0.985f;
            }

            return fallback;
        }

        private static LayoutProfile BuildFallbackProfile(int shortSide, int longSide, float topInset, float sideInset)
        {
            var aspect = shortSide <= 0 ? 2f : longSide / (float)shortSide;

            var compact = longSide <= 1700 || aspect <= 1.84f;
            var ultraCompact = longSide <= 1400 || aspect <= 1.76f;

            var menuScale = 1f;
            var pauseScale = 1f;
            var resultScale = 1f;

            if (ultraCompact)
            {
                menuScale = 0.91f;
                pauseScale = 0.89f;
                resultScale = 0.9f;
            }
            else if (compact)
            {
                menuScale = 0.96f;
                pauseScale = 0.95f;
                resultScale = 0.95f;
            }
            else if (longSide >= 2750 || shortSide >= 428)
            {
                menuScale = 1.04f;
                pauseScale = 1.05f;
                resultScale = 1.05f;
            }

            return new LayoutProfile
            {
                name = "Generic Phone",
                matchedKnownDevice = false,
                compact = compact,
                ultraCompact = ultraCompact,
                menuScale = menuScale,
                pauseScale = pauseScale,
                resultScale = resultScale,
                topInsetRatio = longSide <= 0 ? 0f : topInset / longSide,
                sideInsetRatio = shortSide <= 0 ? 0f : sideInset / shortSide
            };
        }

        private static float GetTopInset(int width, int height, Rect safeArea)
        {
            if (width <= 0 || height <= 0)
            {
                return 0f;
            }

            if (height >= width)
            {
                return Mathf.Max(0f, height - safeArea.yMax);
            }

            return Mathf.Max(0f, width - safeArea.xMax);
        }

        private static float GetSideInset(int width, int height, Rect safeArea)
        {
            if (width <= 0 || height <= 0)
            {
                return 0f;
            }

            if (height >= width)
            {
                var left = Mathf.Max(0f, safeArea.xMin);
                var right = Mathf.Max(0f, width - safeArea.xMax);
                return Mathf.Max(left, right);
            }

            var bottom = Mathf.Max(0f, safeArea.yMin);
            var top = Mathf.Max(0f, height - safeArea.yMax);
            return Mathf.Max(bottom, top);
        }
    }
}
