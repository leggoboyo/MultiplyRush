using UnityEngine;

namespace MultiplyRush
{
    public sealed class DeviceRuntimeSettings : MonoBehaviour
    {
        private const int PowerSaveFrameRateCap = 60;
        private const float LowBatteryThreshold = 0.2f;
        private const float PowerStatePollIntervalSeconds = 5f;

        [Range(30, 120)]
        public int targetFrameRate = 60;
        public bool disableVSync = true;
        public bool keepScreenAwake = true;
        public bool forcePortraitOrientation = true;
        public bool autoDetectDeviceTier = true;
        [Range(0.6f, 0.95f)]
        public float lowTierRenderScale = 0.85f;
        [Range(0.7f, 1f)]
        public float midTierRenderScale = 0.94f;

        private static bool _applied;
        private int _baseTargetFrameRate = 60;
        private int _appliedTargetFrameRate = -1;
        private float _nextPowerStatePollTime;
        private bool _lastLowPowerMode;
        private bool _lastLowBattery;

        private void Awake()
        {
            if (_applied)
            {
                return;
            }

            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }

            var resolvedTargetFps = Mathf.Clamp(targetFrameRate, 30, 120);
            if (autoDetectDeviceTier)
            {
                ResolveDeviceTier(out var tierFps, out var renderScale);
                resolvedTargetFps = tierFps;
                ApplyRenderScale(renderScale);
            }

            _baseTargetFrameRate = resolvedTargetFps;
            ApplyPowerAwareFrameRate(forceApply: true);

            if (keepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            if (forcePortraitOrientation)
            {
                ApplyPortraitOrientation();
            }

            _applied = true;
        }

        private void Update()
        {
            if (!_applied || Time.unscaledTime < _nextPowerStatePollTime)
            {
                return;
            }

            _nextPowerStatePollTime = Time.unscaledTime + PowerStatePollIntervalSeconds;
            ApplyPowerAwareFrameRate(forceApply: false);
            ApplyPortraitOrientationIfNeeded();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !_applied)
            {
                return;
            }

            _nextPowerStatePollTime = Time.unscaledTime + PowerStatePollIntervalSeconds;
            ApplyPowerAwareFrameRate(forceApply: false);
            ApplyPortraitOrientationIfNeeded();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus || !_applied)
            {
                return;
            }

            _nextPowerStatePollTime = Time.unscaledTime + PowerStatePollIntervalSeconds;
            ApplyPowerAwareFrameRate(forceApply: false);
        }

        private void ResolveDeviceTier(out int fps, out float renderScale)
        {
            var systemMemory = Mathf.Max(1, SystemInfo.systemMemorySize);
            var graphicsMemory = SystemInfo.graphicsMemorySize > 0 ? SystemInfo.graphicsMemorySize : (systemMemory / 2);
            var cores = Mathf.Max(1, SystemInfo.processorCount);
            var refreshRate = Screen.currentResolution.refreshRateRatio.value > 0f
                ? Screen.currentResolution.refreshRateRatio.value
                : 60f;

            if (systemMemory <= 3000 || graphicsMemory < 1400 || cores <= 4)
            {
                fps = 30;
                renderScale = Mathf.Clamp(lowTierRenderScale, 0.6f, 0.95f);
                return;
            }

            if (systemMemory <= 5000 || graphicsMemory < 2600 || cores <= 6)
            {
                fps = 60;
                renderScale = Mathf.Clamp(midTierRenderScale, 0.7f, 1f);
                return;
            }

            fps = refreshRate >= 100f ? 120 : 60;
            renderScale = 1f;
        }

        private void ApplyPowerAwareFrameRate(bool forceApply)
        {
            var targetFrameRate = _baseTargetFrameRate;
            ResolvePowerConstraints(out var lowPowerMode, out var lowBattery);
            if (lowPowerMode || lowBattery)
            {
                targetFrameRate = Mathf.Min(targetFrameRate, PowerSaveFrameRateCap);
            }

            var hasStateChange = lowPowerMode != _lastLowPowerMode || lowBattery != _lastLowBattery;
            var hasRateChange = targetFrameRate != _appliedTargetFrameRate;
            if (!forceApply && !hasStateChange && !hasRateChange)
            {
                return;
            }

            _lastLowPowerMode = lowPowerMode;
            _lastLowBattery = lowBattery;
            _appliedTargetFrameRate = targetFrameRate;
            Application.targetFrameRate = targetFrameRate;
        }

        private static void ResolvePowerConstraints(out bool lowPowerMode, out bool lowBattery)
        {
            lowPowerMode = false;
#if UNITY_IOS && !UNITY_EDITOR
            lowPowerMode = UnityEngine.iOS.Device.lowPowerModeEnabled;
#endif

            var batteryLevel = SystemInfo.batteryLevel;
            lowBattery = SystemInfo.batteryStatus == BatteryStatus.Discharging &&
                         batteryLevel >= 0f &&
                         batteryLevel <= LowBatteryThreshold;
        }

        private static void ApplyRenderScale(float scale)
        {
            var clamped = Mathf.Clamp(scale, 0.6f, 1f);
            var urpAsset = QualitySettings.renderPipeline;
            if (urpAsset == null)
            {
                return;
            }

            // Avoid compile dependency on URP assembly type in script asm.
            var urpType = urpAsset.GetType();
            var renderScaleProperty = urpType.GetProperty("renderScale");
            if (renderScaleProperty == null || !renderScaleProperty.CanWrite)
            {
                return;
            }

            renderScaleProperty.SetValue(urpAsset, clamped, null);
        }

        private void ApplyPortraitOrientationIfNeeded()
        {
            if (!forcePortraitOrientation)
            {
                return;
            }

            ApplyPortraitOrientation();
        }

        private static void ApplyPortraitOrientation()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = true;
        }
    }
}
