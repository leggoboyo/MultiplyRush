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
        private LevelBuildResult _currentBuild;
        private bool _pendingReinforcementKit;
        private bool _pendingShieldCharge;
        private DifficultyMode _difficultyMode = DifficultyMode.Normal;

        private void Awake()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            SceneVisualTuning.ApplyGameLook();

            if (resultsOverlay != null)
            {
                resultsOverlay.OnRetryRequested += RetryLevel;
                resultsOverlay.OnNextRequested += NextLevel;
                resultsOverlay.OnUseReinforcementRequested += UseReinforcementAndRetry;
                resultsOverlay.OnUseShieldRequested += UseShieldAndRetry;
            }

            if (playerCrowd != null)
            {
                playerCrowd.CountChanged += OnCountChanged;
                playerCrowd.FinishReached += OnFinishReached;
            }

            _difficultyMode = ProgressionStore.GetDifficultyMode(DifficultyMode.Normal);
            RefreshInventoryHud();
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
                resultsOverlay.OnUseReinforcementRequested -= UseReinforcementAndRetry;
                resultsOverlay.OnUseShieldRequested -= UseShieldAndRetry;
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
            _currentBuild = levelGenerator.Generate(_currentLevelIndex);

            var startCount = _currentBuild.startCount;
            if (_pendingReinforcementKit)
            {
                startCount += CalculateReinforcementBonus(_currentBuild);
                _pendingReinforcementKit = false;
            }

            var startPosition = crowdStartPoint != null ? crowdStartPoint.position : Vector3.zero;
            playerCrowd.StartRun(
                startPosition,
                startCount,
                _currentBuild.forwardSpeed,
                _currentBuild.trackHalfWidth,
                _currentBuild.finishZ,
                _currentBuild.totalRows);
            if (_pendingShieldCharge)
            {
                playerCrowd.ActivateShield();
                _pendingShieldCharge = false;
            }

            if (hud != null)
            {
                hud.SetDifficulty(_difficultyMode);
                hud.SetLevel(_currentLevelIndex, _currentBuild.modifierName, _currentBuild.isMiniBoss);
                hud.SetCount(startCount);
                hud.SetProgress(0f);
            }

            RefreshInventoryHud();

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
            if (_currentBuild.enemyCount <= 0)
            {
                _currentBuild.enemyCount = Mathf.Max(1, enemyCount);
            }

            if (playerCrowd != null && levelGenerator != null)
            {
                playerCrowd.GetLaneUsage(out var left, out var center, out var right);
                levelGenerator.ReportLaneUsage(left, center, right);
            }

            var requiredCount = Mathf.Max(_currentBuild.enemyCount, _currentBuild.tankRequirement);
            var betterHits = 0;
            var worseHits = 0;
            var redHits = 0;
            var totalRows = _currentBuild.totalRows;
            if (playerCrowd != null)
            {
                playerCrowd.GetGateHitStats(out betterHits, out worseHits, out redHits, out totalRows);
            }

            var objective = DifficultyRules.BuildObjective(
                _difficultyMode,
                _currentBuild.isMiniBoss,
                Mathf.Max(_currentBuild.totalRows, totalRows));
            var gateObjectivePassed = DifficultyRules.EvaluateObjective(
                objective,
                betterHits,
                worseHits,
                redHits,
                out var gateObjectiveLine);

            var didWin = playerCount >= requiredCount && gateObjectivePassed;
            var detailLine =
                "Mode " + DifficultyRules.GetModeShortLabel(_difficultyMode) +
                " • " + gateObjectiveLine;

            if (didWin)
            {
                ProgressionStore.MarkLevelWon(_currentLevelIndex);
                if (_currentBuild.isMiniBoss)
                {
                    var reward = ProgressionStore.GrantMiniBossReward(_currentLevelIndex);
                    if (!reward.IsEmpty)
                    {
                        detailLine += "\nMini-Boss Reward: +" + reward.reinforcementKits + " Kit";
                        if (reward.shieldCharges > 0)
                        {
                            detailLine += "  +" + reward.shieldCharges + " Shield";
                        }
                    }
                }
            }
            else if (!gateObjectivePassed)
            {
                detailLine += "\nGate objective not met.";
            }

            RefreshInventoryHud();

            if (resultsOverlay != null)
            {
                resultsOverlay.ShowResult(
                    didWin,
                    _currentLevelIndex,
                    playerCount,
                    _currentBuild.enemyCount,
                    _currentBuild.tankRequirement,
                    detailLine);

                if (didWin)
                {
                    resultsOverlay.SetBuffOptions(0, 0);
                }
                else
                {
                    resultsOverlay.SetBuffOptions(
                        ProgressionStore.GetReinforcementKits(),
                        ProgressionStore.GetShieldCharges());
                }
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

        private void UseReinforcementAndRetry()
        {
            if (!CanAcceptResultInput())
            {
                return;
            }

            if (!ProgressionStore.TryConsumeReinforcementKit())
            {
                return;
            }

            _pendingReinforcementKit = true;
            RefreshInventoryHud();
            StartLevel(_currentLevelIndex);
        }

        private void UseShieldAndRetry()
        {
            if (!CanAcceptResultInput())
            {
                return;
            }

            if (!ProgressionStore.TryConsumeShieldCharge())
            {
                return;
            }

            _pendingShieldCharge = true;
            RefreshInventoryHud();
            StartLevel(_currentLevelIndex);
        }

        private int CalculateReinforcementBonus(LevelBuildResult build)
        {
            var scaled = Mathf.RoundToInt(Mathf.Max(12f, build.enemyCount * 0.16f));
            return Mathf.Clamp(scaled, 12, 260);
        }

        private void RefreshInventoryHud()
        {
            if (hud == null)
            {
                return;
            }

            hud.SetInventory(
                ProgressionStore.GetReinforcementKits(),
                ProgressionStore.GetShieldCharges());
        }
    }
}
