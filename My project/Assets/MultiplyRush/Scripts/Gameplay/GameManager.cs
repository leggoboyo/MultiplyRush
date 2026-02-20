using UnityEngine;

namespace MultiplyRush
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("References")]
        public CrowdController playerCrowd;
        public LevelGenerator levelGenerator;
        public HUDController hud;
        public ResultOverlayController resultsOverlay;
        public Transform crowdStartPoint;

        [Header("Flow")]
        [Range(0f, 1f)]
        public float resultInputDelaySeconds = 0.2f;

        private enum GameFlowState
        {
            Booting,
            Transitioning,
            Running,
            ShowingResult
        }

        private int _currentLevelIndex;
        private bool _roundActive;
        private GameFlowState _state = GameFlowState.Booting;
        private float _resultShownAt;

        private void Awake()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();

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

            _state = GameFlowState.Transitioning;
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
            _state = GameFlowState.Running;
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
            if (_state != GameFlowState.Running)
            {
                return;
            }

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

            _resultShownAt = Time.unscaledTime;
            _state = GameFlowState.ShowingResult;
        }

        private void RetryLevel()
        {
            if (!CanAcceptResultInput())
            {
                return;
            }

            StartLevel(_currentLevelIndex);
        }

        private void NextLevel()
        {
            if (!CanAcceptResultInput())
            {
                return;
            }

            StartLevel(_currentLevelIndex + 1);
        }

        private bool CanAcceptResultInput()
        {
            if (_state != GameFlowState.ShowingResult)
            {
                return false;
            }

            var delay = Mathf.Max(0f, resultInputDelaySeconds);
            return Time.unscaledTime >= _resultShownAt + delay;
        }
    }
}
