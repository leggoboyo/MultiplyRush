using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public float battleDurationNormal = 4f;
        public float battleDurationMiniBoss = 4.8f;
        public float bossBattleDuration = 10f;
        public float bossSlamInterval = 1f;
        public float bossSlamUnitLossFraction = 0.1f;
        public int bossSlamMinimumLoss = 1;
        [Range(0.35f, 0.85f)]
        public float bossTargetDurationEasy = 0.52f;
        [Range(0.35f, 0.85f)]
        public float bossTargetDurationNormal = 0.62f;
        [Range(0.35f, 0.85f)]
        public float bossTargetDurationHard = 0.72f;
        public float bossHealthSafetyMultiplier = 1f;
        public float bossDamagePerUnitPerSecond = 0.32f;
        public float bossDamageSqrtPerSecond = 2f;
        public float postBattleDelay = 0.24f;
        public float playerBattlePowerPerUnit = 0.055f;
        public float enemyBattlePowerPerUnit = 0.05f;
        public float playerBattlePowerSqrt = 0.9f;
        public float enemyBattlePowerSqrt = 0.86f;
        public float winnerPowerBias = 1.2f;
        public float loserPowerBias = 0.84f;
        public float battleHitSfxInterval = 0.09f;
        public float preBattleCenterTime = 0.46f;

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
            AppLifecycleController.EnsureInstance().SetPauseOnFocusLoss(true);
            HapticsDirector.EnsureInstance();

            if (resultsOverlay != null)
            {
                resultsOverlay.OnRetryRequested += RetryLevel;
                resultsOverlay.OnNextRequested += NextLevel;
                resultsOverlay.OnUseReinforcementRequested += UseReinforcementAndRetry;
                resultsOverlay.OnUseShieldRequested += UseShieldAndRetry;
                resultsOverlay.OnMainMenuRequested += ReturnToMainMenuFromResult;
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
            var requestedLevel = ProgressionStore.ConsumeRequestedStartLevel();
            _currentLevelIndex = requestedLevel > 0
                ? requestedLevel
                : ProgressionStore.GetUnlockedLevel();
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
                resultsOverlay.OnMainMenuRequested -= ReturnToMainMenuFromResult;
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
            _difficultyMode = ProgressionStore.GetDifficultyMode(_difficultyMode);
            _currentBuild = levelGenerator.Generate(_currentLevelIndex, _difficultyMode);

            var finishLine = levelGenerator.GetActiveFinishLine();
            if (finishLine != null && finishLine.enemyGroup != null)
            {
                finishLine.enemyGroup.EndCombat();
            }

            finishLine?.StopMiniBossHarass();

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
                _currentBuild.totalRows,
                _currentLevelIndex);
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
                hud.SetEnemyCount(_currentBuild.enemyCount, false);
                hud.SetEnemyCountVisible(false);
                hud.SetBossHealthVisible(false);
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

            AudioDirector.Instance?.StopGameplayPreview();
            AudioDirector.Instance?.SetMusicCue(AudioMusicCue.Gameplay, false);
            _roundActive = true;
            _state = GameFlowState.Running;
            if (_currentBuild.isMiniBoss && finishLine != null)
            {
                finishLine.BeginMiniBossHarass(playerCrowd, _currentLevelIndex, _difficultyMode);
            }
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
            var finishLine = levelGenerator != null ? levelGenerator.GetActiveFinishLine() : null;
            finishLine?.StopMiniBossHarass();
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

            _battleRoutine = StartCoroutine(RunFinishBattle(enemyBaseCount));
        }

        private IEnumerator RunFinishBattle(int enemyCount)
        {
            if (_currentBuild.isMiniBoss)
            {
                yield return RunMiniBossBattle(enemyCount);
                yield break;
            }

            var finishLine = levelGenerator != null ? levelGenerator.GetActiveFinishLine() : null;
            var enemyGroup = finishLine != null ? finishLine.enemyGroup : null;
            var battleStartPlayer = playerCrowd != null ? Mathf.Max(0, playerCrowd.Count) : 0;
            var battleStartEnemy = Mathf.Max(0, enemyCount);
            var didWin = battleStartPlayer >= battleStartEnemy;
            var targetPlayerRemaining = Mathf.Max(0, battleStartPlayer - battleStartEnemy);
            var targetEnemyRemaining = Mathf.Max(0, battleStartEnemy - battleStartPlayer);
            if (hud != null)
            {
                hud.SetEnemyCount(battleStartEnemy, true);
                hud.SetEnemyCountVisible(true);
            }

            if (enemyGroup != null)
            {
                enemyGroup.SetCount(battleStartEnemy);
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
            AudioDirector.Instance?.SetMusicCue(AudioMusicCue.Battle, false);

            var centerTime = Mathf.Clamp(preBattleCenterTime, 0f, 1.2f);
            if (centerTime > 0f)
            {
                var centerElapsed = 0f;
                while (centerElapsed < centerTime)
                {
                    var deltaTime = Time.deltaTime;
                    if (deltaTime <= 0f)
                    {
                        yield return null;
                        continue;
                    }

                    centerElapsed += deltaTime;
                    yield return null;
                }
            }

            var duration = _currentBuild.isMiniBoss ? battleDurationMiniBoss : battleDurationNormal;
            duration = Mathf.Max(1.8f, duration);
            var elapsed = 0f;
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

                var progress = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - progress, 2.2f);
                var desiredPlayer = Mathf.RoundToInt(Mathf.Lerp(battleStartPlayer, targetPlayerRemaining, eased));
                var desiredEnemy = Mathf.RoundToInt(Mathf.Lerp(battleStartEnemy, targetEnemyRemaining, eased));
                desiredPlayer = Mathf.Clamp(desiredPlayer, targetPlayerRemaining, battleStartPlayer);
                desiredEnemy = Mathf.Clamp(desiredEnemy, targetEnemyRemaining, battleStartEnemy);

                var playerLoss = Mathf.Max(0, playerAlive - desiredPlayer);
                var enemyLoss = Mathf.Max(0, enemyAlive - desiredEnemy);
                var pacingScale = Mathf.Max(1f, duration);
                var maxPlayerLossPerStep = Mathf.Clamp(
                    Mathf.CeilToInt((Mathf.Max(1f, battleStartPlayer) * deltaTime / pacingScale) * 2.3f),
                    2,
                    36);
                var maxEnemyLossPerStep = Mathf.Clamp(
                    Mathf.CeilToInt((Mathf.Max(1f, battleStartEnemy) * deltaTime / pacingScale) * 2.3f),
                    2,
                    36);
                playerLoss = Mathf.Min(playerLoss, maxPlayerLossPerStep);
                enemyLoss = Mathf.Min(enemyLoss, maxEnemyLossPerStep);
                if (enemyLoss > 0)
                {
                    if (enemyGroup != null)
                    {
                        enemyGroup.ApplyBattleLosses(enemyLoss);
                    }
                }

                if (playerLoss > 0)
                {
                    if (playerCrowd != null)
                    {
                        playerCrowd.ApplyBattleLosses(playerLoss);
                    }
                }

                if (hud != null)
                {
                    var hudEnemyCount = enemyGroup != null ? enemyGroup.Count : desiredEnemy;
                    hud.SetEnemyCount(hudEnemyCount, true);
                }

                if ((enemyLoss > 0 || playerLoss > 0) && _battleHitSfxTimer <= 0f)
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleHit, 0.48f, Random.Range(0.9f, 1.1f));
                    _battleHitSfxTimer = Mathf.Max(0.02f, battleHitSfxInterval);
                }

                yield return null;
            }

            if (enemyGroup != null)
            {
                var correction = Mathf.Max(0, enemyGroup.Count - targetEnemyRemaining);
                enemyGroup.ApplyBattleLosses(correction);
            }

            if (playerCrowd != null)
            {
                var correction = Mathf.Max(0, playerCrowd.Count - targetPlayerRemaining);
                playerCrowd.ApplyBattleLosses(correction);
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

            if (hud != null)
            {
                var finalEnemyCount = enemyGroup != null ? enemyGroup.Count : targetEnemyRemaining;
                hud.SetEnemyCount(finalEnemyCount, true);
            }
            ShowResultAfterBattle(didWin, battleStartEnemy);
            _battleRoutine = null;
        }

        private IEnumerator RunMiniBossBattle(int bossHealth)
        {
            var finishLine = levelGenerator != null ? levelGenerator.GetActiveFinishLine() : null;
            var battleStartPlayer = playerCrowd != null ? Mathf.Max(0, playerCrowd.Count) : 0;
            var duration = Mathf.Max(8f, bossBattleDuration);
            var targetDuration = duration * GetMiniBossTargetDurationFraction();
            var estimatedDps = EstimateBossDamagePerSecond(Mathf.Max(1, battleStartPlayer));
            var healthEfficiency = GetMiniBossHealthEfficiency();
            var tunedBossHealth = Mathf.Max(
                1,
                Mathf.RoundToInt(estimatedDps * targetDuration * healthEfficiency * Mathf.Clamp(bossHealthSafetyMultiplier, 0.5f, 1.15f)));
            var battleStartBoss = Mathf.Max(1, bossHealth, _currentBuild.tankRequirement, tunedBossHealth);
            var currentBossHealth = battleStartBoss;
            var didWin = false;
            var slamLossFromStart = Mathf.Max(
                Mathf.Max(1, bossSlamMinimumLoss),
                Mathf.CeilToInt(Mathf.Max(1, battleStartPlayer) * Mathf.Clamp(bossSlamUnitLossFraction, 0.01f, 0.95f)));

            if (finishLine != null)
            {
                finishLine.SetBossVisualActive(true);
            }

            if (hud != null)
            {
                hud.SetEnemyCountVisible(false);
                hud.SetBossHealth(currentBossHealth, battleStartBoss, true);
            }

            if (playerCrowd != null)
            {
                var target = finishLine != null && finishLine.BossVisual != null
                    ? finishLine.BossVisual
                    : finishLine != null ? finishLine.transform : null;
                playerCrowd.BeginCombat(target);
            }

            AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleStart, 0.9f, 0.95f);
            AudioDirector.Instance?.SetMusicCue(AudioMusicCue.Battle, false);

            var centerTime = Mathf.Clamp(preBattleCenterTime, 0f, 1.2f);
            if (centerTime > 0f)
            {
                var centerElapsed = 0f;
                while (centerElapsed < centerTime)
                {
                    var deltaTime = Time.deltaTime;
                    if (deltaTime <= 0f)
                    {
                        yield return null;
                        continue;
                    }

                    centerElapsed += deltaTime;
                    yield return null;
                }
            }

            var elapsed = 0f;
            var slamTimer = Mathf.Clamp(bossSlamInterval, 0.45f, 2.2f);
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
                if (playerAlive <= 0)
                {
                    break;
                }

                var modeDamageScale = _difficultyMode == DifficultyMode.Easy
                    ? 1.04f
                    : _difficultyMode == DifficultyMode.Hard ? 0.76f : 0.9f;
                var dps = ((playerAlive * bossDamagePerUnitPerSecond) + (Mathf.Sqrt(playerAlive) * bossDamageSqrtPerSecond)) * modeDamageScale;
                var damage = Mathf.Max(0.6f, dps * deltaTime);
                var damageInt = Mathf.Max(1, Mathf.RoundToInt(damage));
                currentBossHealth = Mathf.Max(0, currentBossHealth - damageInt);

                if (hud != null)
                {
                    hud.SetBossHealth(currentBossHealth, battleStartBoss, true);
                }

                if (_battleHitSfxTimer <= 0f)
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleHit, 0.4f, Random.Range(0.92f, 1.08f));
                    _battleHitSfxTimer = Mathf.Max(0.025f, battleHitSfxInterval);
                }

                if (currentBossHealth <= 0)
                {
                    didWin = true;
                    break;
                }

                slamTimer -= deltaTime;
                while (slamTimer <= 0f)
                {
                    slamTimer += Mathf.Max(0.45f, bossSlamInterval);
                    var slamLoss = Mathf.Clamp(slamLossFromStart, 1, Mathf.Max(1, playerAlive));
                    var removed = playerCrowd != null ? playerCrowd.ApplyBattleLosses(slamLoss) : 0;
                    finishLine?.TriggerBossSlam(1f + Mathf.Clamp01(removed / 24f));
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleHit, 0.7f, Random.Range(0.72f, 0.86f));
                    HapticsDirector.Instance?.Play(HapticCue.MediumImpact);
                    playerAlive = playerCrowd != null ? playerCrowd.Count : 0;
                    if (playerAlive <= 0)
                    {
                        break;
                    }
                }

                yield return null;
            }

            var survivors = playerCrowd != null ? playerCrowd.Count : 0;
            if (!didWin)
            {
                didWin = currentBossHealth <= 0 && survivors > 0;
            }

            if (playerCrowd != null)
            {
                playerCrowd.EndCombat();
            }

            if (finishLine != null && didWin)
            {
                finishLine.SetBossVisualActive(false);
            }

            if (postBattleDelay > 0f)
            {
                yield return new WaitForSeconds(postBattleDelay);
            }

            if (hud != null)
            {
                hud.SetBossHealthVisible(false);
                hud.SetEnemyCountVisible(false);
            }

            ShowResultAfterBattle(didWin, battleStartBoss);
            _battleRoutine = null;
        }

        private float GetMiniBossTargetDurationFraction()
        {
            switch (_difficultyMode)
            {
                case DifficultyMode.Easy:
                    return Mathf.Clamp(bossTargetDurationEasy, 0.38f, 0.68f);
                case DifficultyMode.Hard:
                    return Mathf.Clamp(bossTargetDurationHard, 0.55f, 0.82f);
                default:
                    return Mathf.Clamp(bossTargetDurationNormal, 0.46f, 0.76f);
            }
        }

        private float EstimateBossDamagePerSecond(int playerCount)
        {
            var safeCount = Mathf.Max(1, playerCount);
            var modeDamageScale = _difficultyMode == DifficultyMode.Easy
                ? 1.04f
                : _difficultyMode == DifficultyMode.Hard ? 0.76f : 0.9f;
            return ((safeCount * bossDamagePerUnitPerSecond) + (Mathf.Sqrt(safeCount) * bossDamageSqrtPerSecond)) * modeDamageScale;
        }

        private float GetMiniBossHealthEfficiency()
        {
            switch (_difficultyMode)
            {
                case DifficultyMode.Easy:
                    return 0.58f;
                case DifficultyMode.Hard:
                    return 0.78f;
                default:
                    return 0.68f;
            }
        }

        private void ShowResultAfterBattle(bool didWin, int enemyCount)
        {
            var playerCount = playerCrowd != null ? playerCrowd.Count : 0;
            var detailLine = string.Empty;

            if (didWin)
            {
                ProgressionStore.MarkLevelWon(_currentLevelIndex);
                ProgressionStore.RecordBestSurvivorsForLevel(_currentLevelIndex, playerCount);
                if (_currentBuild.isMiniBoss)
                {
                    var reward = ProgressionStore.GrantMiniBossReward(_currentLevelIndex);
                    if (!reward.IsEmpty)
                    {
                        detailLine = "Mini-Boss Reward: +" + reward.reinforcementKits + " Kit";
                        if (reward.shieldCharges > 0)
                        {
                            detailLine += "  +" + reward.shieldCharges + " Shield";
                        }
                    }
                }
            }

            RefreshInventoryHud();
            if (hud != null)
            {
                hud.SetEnemyCountVisible(false);
            }

            if (resultsOverlay != null)
            {
                resultsOverlay.ShowResult(
                    didWin,
                    _currentLevelIndex,
                    playerCount,
                    enemyCount,
                    0,
                    string.IsNullOrWhiteSpace(detailLine) ? null : detailLine);

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
            AudioDirector.Instance?.PlayResultSequence(didWin);
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

        private void ReturnToMainMenuFromResult()
        {
            if (!CanAcceptResultInput())
            {
                return;
            }

            SceneManager.LoadScene("MainMenu");
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

    }
}
