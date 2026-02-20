using UnityEngine;

namespace MultiplyRush
{
    public sealed class DeviceRuntimeSettings : MonoBehaviour
    {
        [Range(30, 120)]
        public int targetFrameRate = 60;
        public bool disableVSync = true;
        public bool keepScreenAwake = true;

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

            Application.targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);

            if (keepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            _applied = true;
        }
    }
}
