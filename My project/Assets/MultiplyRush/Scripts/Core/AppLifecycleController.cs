using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplyRush
{
    public sealed class AppLifecycleController : MonoBehaviour
    {
        private const float BackgroundTransitionDebounceSeconds = 0.2f;

        private static AppLifecycleController _instance;

        private float _lastLowMemoryCleanupTime = -60f;
        private float _lastBackgroundTransitionTime = -10f;
        private bool _wantsPauseOnFocusLoss = true;
        private bool _isBackgrounded;
        private bool _pendingSystemPause;

        public static AppLifecycleController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AppLifecycleController>();
                }

                return _instance;
            }
        }

        public static AppLifecycleController EnsureInstance()
        {
            if (Instance != null)
            {
                return _instance;
            }

            var go = new GameObject("AppLifecycleController");
            _instance = go.AddComponent<AppLifecycleController>();
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
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _instance = null;
            }
        }

        private void Update()
        {
            if (!_pendingSystemPause)
            {
                return;
            }

            TryApplyPendingSystemPause();
        }

        public void SetPauseOnFocusLoss(bool enabled)
        {
            _wantsPauseOnFocusLoss = enabled;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                HandleBackgroundTransition();
                return;
            }

            _isBackgrounded = false;
            TryApplyPendingSystemPause();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                HandleBackgroundTransition();
                return;
            }

            _isBackgrounded = false;
            TryApplyPendingSystemPause();
        }

        private void OnApplicationQuit()
        {
            ProgressionStore.Flush();
        }

        private void OnApplicationLowMemory()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _lastLowMemoryCleanupTime + 5f)
            {
                return;
            }

            _lastLowMemoryCleanupTime = now;
            // Persist progress/settings before iOS decides to reclaim the process.
            ProgressionStore.Flush();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            Debug.Log("Multiply Rush: low-memory cleanup triggered.");
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Game")
            {
                ProgressionStore.Flush();
                TryApplyPendingSystemPause();
            }
        }

        private void HandleBackgroundTransition()
        {
            var now = Time.realtimeSinceStartup;
            if (_isBackgrounded && now <= _lastBackgroundTransitionTime + BackgroundTransitionDebounceSeconds)
            {
                return;
            }

            _isBackgrounded = true;
            _lastBackgroundTransitionTime = now;
            ProgressionStore.Flush();

            if (!_wantsPauseOnFocusLoss)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "Game")
            {
                return;
            }

            var pauseController = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            if (pauseController != null)
            {
                pauseController.PauseFromSystem();
                _pendingSystemPause = false;
                return;
            }

            _pendingSystemPause = true;
        }

        private void TryApplyPendingSystemPause()
        {
            if (!_pendingSystemPause || !_wantsPauseOnFocusLoss)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "Game")
            {
                _pendingSystemPause = false;
                return;
            }

            var pauseController = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            if (pauseController == null)
            {
                return;
            }

            pauseController.PauseFromSystem();
            _pendingSystemPause = false;
        }
    }
}
