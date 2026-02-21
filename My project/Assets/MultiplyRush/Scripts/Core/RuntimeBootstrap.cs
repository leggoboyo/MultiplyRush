using UnityEngine;

namespace MultiplyRush
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnSceneLoad()
        {
            AudioDirector.EnsureInstance();
            HapticsDirector.EnsureInstance();
            AppLifecycleController.EnsureInstance();
        }
    }
}
