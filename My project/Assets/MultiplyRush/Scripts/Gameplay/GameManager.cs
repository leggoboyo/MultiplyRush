using UnityEngine;

namespace MultiplyRush
{
    public sealed class GameManager : MonoBehaviour
    {
        public CrowdController playerCrowd;
        public LevelGenerator levelGenerator;
        public HUDController hud;
        public ResultOverlayController resultsOverlay;
        public Transform crowdStartPoint;

        private int _currentLevelIndex;
        private bool _roundActive;

        private void Awake()
        {
            if (resultsOverlay != null)
            {
                resultsOverlay.OnRetryRequested += RetryLevel;
                resultsOverlay.OnNextRequested += NextLevel;
            }

            if (playerCrowd != null)
            {
                playerCrowd.CountChanged += OnCountChanged;
                playerCrowd.FinishReached += OnFinishReached;
            }
        }

        private void Start()
        {
            _currentLevelIndex = ProgressionStore.GetUnlockedLevel();
            StartLevel(_currentLevelIndex);
        }

        private void Update()
        {
            if (!_roundActive || hud == null || playerCrowd == null)
            {
                return;
            }

            hud.SetProgress(playerCrowd.Progress01);
        }

        private void OnDestroy()
        {
            if (resultsOverlay != null)
            {
                resultsOverlay.OnRetryRequested -= RetryLevel;
                resultsOverlay.OnNextRequested -= NextLevel;
            }

            if (playerCrowd != null)
            {
                playerCrowd.CountChanged -= OnCountChanged;
                playerCrowd.FinishReached -= OnFinishReached;
            }
        }

        private void StartLevel(int levelIndex)
        {
            if (levelGenerator == null || playerCrowd == null)
            {
                Debug.LogError("GameManager is missing required references.");
                return;
            }

            _currentLevelIndex = Mathf.Max(1, levelIndex);
            var build = levelGenerator.Generate(_currentLevelIndex);

            var startPosition = crowdStartPoint != null ? crowdStartPoint.position : Vector3.zero;
            playerCrowd.StartRun(startPosition, build.startCount, build.forwardSpeed, build.trackHalfWidth, build.finishZ);

            if (hud != null)
            {
                hud.SetLevel(_currentLevelIndex);
                hud.SetCount(build.startCount);
                hud.SetProgress(0f);
            }

            if (resultsOverlay != null)
            {
                resultsOverlay.Hide();
            }

            _roundActive = true;
        }

        private void OnCountChanged(int count)
        {
            if (hud != null)
            {
                hud.SetCount(count);
            }
        }

        private void OnFinishReached(int enemyCount)
        {
            _roundActive = false;
            var playerCount = playerCrowd != null ? playerCrowd.Count : 0;
            var didWin = playerCount >= enemyCount;

            if (didWin)
            {
                ProgressionStore.MarkLevelWon(_currentLevelIndex);
            }

            if (resultsOverlay != null)
            {
                resultsOverlay.ShowResult(didWin, _currentLevelIndex, playerCount, enemyCount);
            }
        }

        private void RetryLevel()
        {
            StartLevel(_currentLevelIndex);
        }

        private void NextLevel()
        {
            StartLevel(_currentLevelIndex + 1);
        }
    }
}
