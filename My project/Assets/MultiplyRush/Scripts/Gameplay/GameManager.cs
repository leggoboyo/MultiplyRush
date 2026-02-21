using System.Collections;
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
        public PauseMenuController pauseMenu;
        public Transform crowdStartPoint;

        [Header("Flow")]
        [Range(0f, 1f)]
        public float resultInputDelaySeconds = 0.2f;

        [Header("Finish Battle")]
        public float battleDurationNormal = 3f;
        public float battleDurationMiniBoss = 4.6f;
        public float postBattleDelay = 0.24f;
        public float playerBattlePowerPerUnit = 0.055f;
        public float enemyBattlePowerPerUnit = 0.05f;
        public float playerBattlePowerSqrt = 0.9f;
        public float enemyBattlePowerSqrt = 0.86f;
        public float winnerPowerBias = 1.2f;
        public float loserPowerBias = 0.84f;
        public float battleHitSfxInterval = 0.09f;

        private enum GameFlowState
        {
            Booting,
            Transitioning,
            Running,
            Battling,
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
        private Coroutine _battleRoutine;
        private float _battleHitSfxTimer;

        private void Awake()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            SceneVisualTuning.ApplyGameLook();
            AudioDirector.EnsureInstance().SetMusicCue(AudioMusicCue.Gameplay, true);

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
            EnsurePauseMenu();
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

            if (_battleRoutine != null)
            {
                StopCoroutine(_battleRoutine);
                _battleRoutine = null;
            }

            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        private void StartLevel(int levelIndex)
        {
            if (levelGenerator == null || playerCrowd == null)
            {
                Debug.LogError("GameManager is missing required references.");
                return;
            }

            if (_battleRoutine != null)
            {
                StopCoroutine(_battleRoutine);
                _battleRoutine = null;
            }

            _state = GameFlowState.Transitioning;
            _currentLevelIndex = Mathf.Max(1, levelIndex);
            _currentBuild = levelGenerator.Generate(_currentLevelIndex);

            var finishLine = levelGenerator.GetActiveFinishLine();
            if (finishLine != null && finishLine.enemyGroup != null)
            {
                finishLine.enemyGroup.EndCombat();
            }

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
            playerCrowd.EndCombat();
            if (_pendingShieldCharge)
            {
                if (playerCrowd.ActivateShield())
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.Shield, 0.78f, 1.02f);
                }

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
            if (pauseMenu != null)
            {
                pauseMenu.ForceResume(true);
                pauseMenu.SetPauseAvailable(true);
            }

            if (resultsOverlay != null)
            {
                resultsOverlay.Hide();
            }

            AudioDirector.Instance?.SetMusicCue(AudioMusicCue.Gameplay, false);
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
            _state = GameFlowState.Battling;
            if (pauseMenu != null)
            {
                pauseMenu.ForceResume(true);
                pauseMenu.SetPauseAvailable(false);
            }

            if (playerCrowd != null && levelGenerator != null)
            {
                playerCrowd.GetLaneUsage(out var left, out var center, out var right);
                levelGenerator.ReportLaneUsage(left, center, right);
            }

            var playerCount = playerCrowd != null ? playerCrowd.Count : 0;
            var enemyBaseCount = Mathf.Max(_currentBuild.enemyCount, Mathf.Max(1, enemyCount));
            _currentBuild.enemyCount = enemyBaseCount;

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

            var requiredCount = Mathf.Max(enemyBaseCount, _currentBuild.tankRequirement);
            var didWin = playerCount >= requiredCount && gateObjectivePassed;
            var detailLine =
                "Mode " + GetModeLabel(_difficultyMode) +
                " • " + gateObjectiveLine;
            if (!gateObjectivePassed)
            {
                detailLine += "\nGate objective not met.";
            }

            _battleRoutine = StartCoroutine(RunFinishBattle(didWin, detailLine, enemyBaseCount));
        }

        private IEnumerator RunFinishBattle(bool expectedWin, string detailLine, int enemyCount)
        {
            var finishLine = levelGenerator != null ? levelGenerator.GetActiveFinishLine() : null;
            var enemyGroup = finishLine != null ? finishLine.enemyGroup : null;

            if (enemyGroup != null)
            {
                enemyGroup.SetCount(enemyCount);
                enemyGroup.BeginCombat(playerCrowd != null ? playerCrowd.transform : null);
            }

            if (playerCrowd != null)
            {
                var target = enemyGroup != null
                    ? enemyGroup.transform
                    : finishLine != null
                        ? finishLine.transform
                        : null;
                playerCrowd.BeginCombat(target);
            }

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleStart, 0.85f, 1f);

            var duration = _currentBuild.isMiniBoss ? battleDurationMiniBoss : battleDurationNormal;
            duration = Mathf.Max(0.8f, duration);
            var elapsed = 0f;
            var enemyDamageCarry = 0f;
            var playerDamageCarry = 0f;
            _battleHitSfxTimer = 0f;

            while (elapsed < duration)
            {
                var deltaTime = Time.deltaTime;
                if (deltaTime <= 0f)
                {
                    yield return null;
                    continue;
                }

                elapsed += deltaTime;
                _battleHitSfxTimer = Mathf.Max(0f, _battleHitSfxTimer - deltaTime);

                var playerAlive = playerCrowd != null ? playerCrowd.Count : 0;
                var enemyAlive = enemyGroup != null ? enemyGroup.Count : 0;
                if (playerAlive <= 0 || enemyAlive <= 0)
                {
                    break;
                }

                var playerPower = (playerAlive * Mathf.Max(0.01f, playerBattlePowerPerUnit)) +
                                  (Mathf.Sqrt(playerAlive) * Mathf.Max(0.01f, playerBattlePowerSqrt));
                var enemyPower = (enemyAlive * Mathf.Max(0.01f, enemyBattlePowerPerUnit)) +
                                 (Mathf.Sqrt(enemyAlive) * Mathf.Max(0.01f, enemyBattlePowerSqrt));

                if (expectedWin)
                {
                    playerPower *= Mathf.Max(1f, winnerPowerBias);
                    enemyPower *= Mathf.Clamp(loserPowerBias, 0.2f, 1f);
                }
                else
                {
                    playerPower *= Mathf.Clamp(loserPowerBias, 0.2f, 1f);
                    enemyPower *= Mathf.Max(1f, winnerPowerBias);
                }

                if (_currentBuild.isMiniBoss)
                {
                    enemyPower *= 1.08f;
                    playerPower *= 1.02f;
                }

                enemyDamageCarry += playerPower * deltaTime;
                playerDamageCarry += enemyPower * deltaTime;

                var enemyLoss = Mathf.FloorToInt(enemyDamageCarry);
                var playerLoss = Mathf.FloorToInt(playerDamageCarry);
                if (enemyLoss > 0)
                {
                    enemyDamageCarry -= enemyLoss;
                    if (enemyGroup != null)
                    {
                        enemyGroup.ApplyBattleLosses(enemyLoss);
                    }
                }

                if (playerLoss > 0)
                {
                    playerDamageCarry -= playerLoss;
                    if (playerCrowd != null)
                    {
                        playerCrowd.ApplyBattleLosses(playerLoss);
                    }
                }

                if ((enemyLoss > 0 || playerLoss > 0) && _battleHitSfxTimer <= 0f)
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleHit, 0.48f, Random.Range(0.9f, 1.1f));
                    _battleHitSfxTimer = Mathf.Max(0.02f, battleHitSfxInterval);
                }

                yield return null;
            }

            if (expectedWin)
            {
                if (enemyGroup != null)
                {
                    enemyGroup.ApplyBattleLosses(enemyGroup.Count);
                }
            }
            else if (playerCrowd != null)
            {
                playerCrowd.ApplyBattleLosses(playerCrowd.Count);
            }

            if (playerCrowd != null)
            {
                playerCrowd.EndCombat();
            }

            if (enemyGroup != null)
            {
                enemyGroup.EndCombat();
            }

            if (postBattleDelay > 0f)
            {
                yield return new WaitForSeconds(postBattleDelay);
            }

            ShowResultAfterBattle(expectedWin, detailLine, enemyCount, enemyGroup);
            _battleRoutine = null;
        }

        private void ShowResultAfterBattle(bool didWin, string detailLine, int enemyCount, EnemyGroup enemyGroup)
        {
            var playerCount = playerCrowd != null ? playerCrowd.Count : 0;
            var enemyRemaining = enemyGroup != null ? enemyGroup.Count : 0;
            detailLine += "\nBattle End: You " + NumberFormatter.ToCompact(playerCount) + " • Enemy " + NumberFormatter.ToCompact(enemyRemaining);

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

            RefreshInventoryHud();

            if (resultsOverlay != null)
            {
                resultsOverlay.ShowResult(
                    didWin,
                    _currentLevelIndex,
                    playerCount,
                    enemyCount,
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
            AudioDirector.Instance?.SetMusicCue(AudioMusicCue.Gameplay, false);
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

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.Reinforcement, 0.8f, 1f);
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

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.Shield, 0.82f, 1.05f);
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

        public void RetryCurrentLevelFromPauseMenu()
        {
            if (_state != GameFlowState.Running)
            {
                return;
            }

            StartLevel(_currentLevelIndex);
        }

        private void EnsurePauseMenu()
        {
            if (pauseMenu == null)
            {
                pauseMenu = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
                if (pauseMenu == null)
                {
                    var pauseObject = new GameObject("PauseMenuController");
                    pauseMenu = pauseObject.AddComponent<PauseMenuController>();
                }
            }

            RectTransform safeAreaRoot = null;
            if (hud != null)
            {
                safeAreaRoot = hud.transform.parent as RectTransform;
            }

            var mainCamera = Camera.main;
            CameraFollower cameraFollower = null;
            if (mainCamera != null)
            {
                cameraFollower = mainCamera.GetComponent<CameraFollower>();
            }

            pauseMenu.Initialize(this, levelGenerator, cameraFollower, safeAreaRoot);
            pauseMenu.SetPauseAvailable(false);
        }

        private static string GetModeLabel(DifficultyMode mode)
        {
            switch (mode)
            {
                case DifficultyMode.Easy:
                    return "Easy";
                case DifficultyMode.Hard:
                    return "Hard";
                default:
                    return "Normal";
            }
        }
    }
}
