using UnityEngine;

namespace MultiplyRush
{
    public sealed class DeviceRuntimeSettings : MonoBehaviour
    {
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

            Application.targetFrameRate = resolvedTargetFps;

            if (keepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            if (forcePortraitOrientation)
            {
                Screen.orientation = ScreenOrientation.Portrait;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToPortrait = true;
            }

            _applied = true;
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
    }
}
