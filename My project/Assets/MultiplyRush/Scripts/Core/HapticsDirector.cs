using UnityEngine;

namespace MultiplyRush
{
    public enum HapticCue
    {
        LightTap = 0,
        MediumImpact = 1,
        HeavyImpact = 2,
        Success = 3,
        Failure = 4
    }

    public sealed class HapticsDirector : MonoBehaviour
    {
        private static HapticsDirector _instance;

        private float _lastHapticTime = -10f;
        private bool _enabled = true;

        public static HapticsDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HapticsDirector>();
                }

                return _instance;
            }
        }

        public static HapticsDirector EnsureInstance()
        {
            if (Instance != null)
            {
                return _instance;
            }

            var go = new GameObject("HapticsDirector");
            _instance = go.AddComponent<HapticsDirector>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _enabled = ProgressionStore.GetHapticsEnabled(true);
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            ProgressionStore.SetHapticsEnabled(enabled);
        }

        public bool GetEnabled()
        {
            return _enabled;
        }

        public void Play(HapticCue cue)
        {
            if (!_enabled)
            {
                return;
            }

#if UNITY_IOS || UNITY_ANDROID
            var now = Time.realtimeSinceStartup;
            var minInterval = ResolveMinInterval(cue);
            if (now < _lastHapticTime + minInterval)
            {
                return;
            }

            _lastHapticTime = now;
            Handheld.Vibrate();
#endif
        }

        private static float ResolveMinInterval(HapticCue cue)
        {
            switch (cue)
            {
                case HapticCue.LightTap:
                    return 0.07f;
                case HapticCue.MediumImpact:
                    return 0.12f;
                case HapticCue.HeavyImpact:
                    return 0.2f;
                case HapticCue.Success:
                case HapticCue.Failure:
                    return 0.28f;
                default:
                    return 0.12f;
            }
        }
    }
}
