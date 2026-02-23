using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FinishLine : MonoBehaviour
    {
        [Header("References")]
        public EnemyGroup enemyGroup;
        public TextMesh enemyCountLabel;
        public TextMesh tankRequirementLabel;

        [Header("Label Animation")]
        public float labelPulseAmplitude = 0.08f;
        public float labelPulseSpeed = 2.5f;

        [Header("Enemy Placement")]
        public Vector3 enemyGroupLocalOffset = new Vector3(0f, 0f, 6.6f);
        public Vector3 enemyGroupLocalScale = Vector3.one;
        public float minEnemyDistanceBehindLine = 5.4f;

        [Header("Mini-Boss Visual")]
        public Vector3 bossLocalOffset = new Vector3(0f, 0.08f, 10.4f);
        public Vector3 bossLocalScale = new Vector3(3.7f, 3.7f, 3.7f);
        public float bossPulseAmplitude = 0.045f;
        public float bossPulseSpeed = 1.9f;
        public float bossRootSwayAmplitude = 0.18f;
        public float bossRootSwaySpeed = 1.15f;
        public float bossSlamDropDistance = 0.95f;
        public float bossSlamRecoverSpeed = 4.8f;
        public float bossSlamWindupDuration = 0.22f;
        public float bossSlamStrikeDuration = 0.14f;
        public float bossSlamRecoverDuration = 0.34f;
        public float bossArmWindupDegrees = 58f;
        public float bossArmStrikeDegrees = 116f;
        public float bossArmAsymmetry = 0.44f;
        public float bossStepForwardOnStrike = 0.36f;
        public float bossJawOpenDegrees = 28f;
        public float bossHeadRoarDegrees = 12f;
        public float bossImpactRingMaxScale = 4.4f;
        public float bossImpactRingExpandSpeed = 12f;
        public float bossImpactRingFadeSpeed = 3.2f;
        public Color bossBodyColor = new Color(0.18f, 0.2f, 0.26f, 1f);
        public Color bossAccentColor = new Color(0.9f, 0.36f, 0.24f, 1f);
        public Color bossEyeColor = new Color(1f, 0.72f, 0.4f, 1f);

        [Header("Mini-Boss Rock Barrage")]
        public bool enableRockBarrage = true;
        [Range(1, 12)]
        public int maxActiveRocks = 6;
        public float rockThrowBaseInterval = 1.95f;
        public float rockThrowMinInterval = 0.72f;
        public float rockThrowIntervalJitter = 0.28f;
        public float rockThrowLeadTime = 0.45f;
        public float rockThrowAimJitterX = 0.85f;
        public float rockTravelSpeedBase = 12f;
        public float rockTravelSpeedPerLevel = 0.09f;
        public float rockTravelSpeedHardBonus = 2.2f;
        public float rockArcHeightBase = 1.8f;
        public float rockArcHeightPerLevel = 0.025f;
        public float rockRadiusBase = 0.55f;
        public float rockRadiusPerLevel = 0.012f;
        public float rockRadiusHardBonus = 0.12f;
        public int rockLossBase = 4;
        public float rockLossPerLevel = 0.2f;
        public int rockLossHardBonus = 4;
        public float rockTelegraphScalePulse = 0.22f;
        public Color rockColor = new Color(0.4f, 0.36f, 0.34f, 1f);
        public Color rockTelegraphColor = new Color(1f, 0.56f, 0.36f, 0.34f);
        public Color rockShadowColor = new Color(0f, 0f, 0f, 0.26f);

        private BoxCollider _trigger;
        private bool _isTriggered;
        private int _enemyCount;
        private int _tankRequirement;
        private bool _isMiniBoss;
        private Vector3 _labelBaseScale = Vector3.one;
        private Vector3 _tankLabelBaseScale = Vector3.one;

        private Transform _bossVisual;
        private Transform _bossCore;
        private Transform _bossHeadPivot;
        private Transform _bossJawPivot;
        private Transform _bossLeftArmPivot;
        private Transform _bossRightArmPivot;
        private Transform _bossLeftForearmPivot;
        private Transform _bossRightForearmPivot;
        private Transform _bossLeftHandPivot;
        private Transform _bossRightHandPivot;
        private Transform _bossAimPoint;
        private Transform _bossImpactRing;
        private Transform _legacyBossVisual;

        private Vector3 _bossBaseScale = Vector3.one;
        private Vector3 _bossBaseLocalPosition = Vector3.zero;
        private Vector3 _bossCoreBaseLocalPosition = Vector3.zero;

        private Quaternion _bossHeadBaseRotation = Quaternion.identity;
        private Quaternion _bossJawBaseRotation = Quaternion.identity;
        private Quaternion _bossLeftArmBaseRotation = Quaternion.identity;
        private Quaternion _bossRightArmBaseRotation = Quaternion.identity;
        private Quaternion _bossLeftForearmBaseRotation = Quaternion.identity;
        private Quaternion _bossRightForearmBaseRotation = Quaternion.identity;
        private Quaternion _bossLeftHandBaseRotation = Quaternion.identity;
        private Quaternion _bossRightHandBaseRotation = Quaternion.identity;

        private bool _bossSlamActive;
        private bool _bossSlamImpactTriggered;
        private float _bossSlamElapsed;
        private float _bossSlamStrength = 1f;
        private float _bossSlamImpactFlash;
        private bool _bossStrikeLeft;
        private float _bossImpactRingAlpha;

        private Material _bossBodyMaterial;
        private Material _bossAccentMaterial;
        private Material _bossEyeMaterial;
        private Material _bossImpactRingMaterial;

        private ParticleSystem _bossSlamFx;

        private sealed class RockProjectile
        {
            public Transform root;
            public Transform visual;
            public Transform shadow;
            public Transform telegraph;
            public bool isActive;
            public Vector3 startWorld;
            public Vector3 targetWorld;
            public float duration;
            public float elapsed;
            public float arcHeight;
            public float radius;
            public int maxUnitLoss;
        }

        private readonly List<RockProjectile> _rockProjectiles = new List<RockProjectile>(10);
        private Transform _rockProjectileRoot;
        private Material _rockMaterial;
        private Material _rockTelegraphMaterial;
        private Material _rockShadowMaterial;
        private ParticleSystem _rockImpactFx;
        private CrowdController _harassCrowd;
        private bool _harassActive;
        private int _harassLevelIndex = 1;
        private DifficultyMode _harassDifficulty = DifficultyMode.Normal;
        private float _harassThrowTimer;
        private bool _hasCrowdVelocity;
        private Vector3 _crowdLastPosition;
        private Vector3 _crowdVelocityWorld;
        private bool _bossThrowFromLeft;

        public int EnemyCount => _enemyCount;
        public int TankRequirement => _tankRequirement;
        public bool IsMiniBoss => _isMiniBoss;
        public Transform BossVisual => _bossAimPoint != null ? _bossAimPoint : _bossVisual;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            EnsureLabels();
            EnsureBossVisual();
            if (enemyCountLabel != null)
            {
                _labelBaseScale = enemyCountLabel.transform.localScale;
            }

            if (tankRequirementLabel != null)
            {
                _tankLabelBaseScale = tankRequirementLabel.transform.localScale;
            }

            if (_bossVisual != null)
            {
                _bossVisual.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            StopMiniBossHarass();
        }

        private void OnEnable()
        {
            _isTriggered = false;
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            StopMiniBossHarass();
        }

        private void Update()
        {
            if (enemyCountLabel != null && enemyCountLabel.gameObject.activeInHierarchy)
            {
                var pulse = 1f + Mathf.Sin(Time.time * labelPulseSpeed) * labelPulseAmplitude;
                enemyCountLabel.transform.localScale = _labelBaseScale * pulse;
            }

            if (tankRequirementLabel != null && tankRequirementLabel.gameObject.activeInHierarchy)
            {
                var pulse = 1f + Mathf.Sin(Time.time * (labelPulseSpeed * 1.12f) + 0.8f) * (labelPulseAmplitude * 0.9f);
                tankRequirementLabel.transform.localScale = _tankLabelBaseScale * pulse;
            }

            if (_bossVisual != null && _bossVisual.gameObject.activeInHierarchy)
            {
                UpdateBossAnimation(Time.deltaTime);
            }

            UpdateRockBarrage(Time.deltaTime);
        }

        public void Configure(int enemyCount, int tankRequirement, bool isMiniBoss)
        {
            StopMiniBossHarass();
            EnsureLabels();
            EnsureBossVisual();
            if (_legacyBossVisual != null)
            {
                _legacyBossVisual.gameObject.SetActive(false);
            }

            _enemyCount = Mathf.Max(1, enemyCount);
            _tankRequirement = isMiniBoss ? Mathf.Max(0, tankRequirement) : 0;
            _isMiniBoss = isMiniBoss;

            var distanceBehindLine = Mathf.Max(Mathf.Abs(enemyGroupLocalOffset.z), Mathf.Max(1f, minEnemyDistanceBehindLine));
            if (enemyGroup != null)
            {
                if (isMiniBoss)
                {
                    enemyGroup.gameObject.SetActive(false);
                    enemyGroup.EndCombat();
                }
                else
                {
                    var safeOffset = enemyGroupLocalOffset;
                    var formationDepth = enemyGroup.EstimateFormationDepth(_enemyCount);
                    distanceBehindLine = Mathf.Max(distanceBehindLine, formationDepth * 0.7f + Mathf.Max(1f, minEnemyDistanceBehindLine));
                    var worldPosition = new Vector3(
                        transform.position.x + safeOffset.x,
                        transform.position.y + safeOffset.y,
                        transform.position.z + distanceBehindLine);

                    enemyGroup.gameObject.SetActive(true);
                    enemyGroup.transform.SetParent(transform, true);
                    enemyGroup.transform.position = worldPosition;
                    enemyGroup.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    enemyGroup.transform.localScale = enemyGroupLocalScale;
                    enemyGroup.SetCount(_enemyCount);
                }
            }

            if (enemyCountLabel != null)
            {
                enemyCountLabel.text = isMiniBoss ? "BOSS HP " + _enemyCount : "Enemy " + _enemyCount;
                enemyCountLabel.color = isMiniBoss ? new Color(1f, 0.7f, 0.55f, 1f) : Color.white;
                enemyCountLabel.transform.localPosition = isMiniBoss
                    ? new Vector3(0f, 3.25f, 1.4f)
                    : new Vector3(0f, 2.05f, -0.44f);
            }

            if (tankRequirementLabel != null)
            {
                tankRequirementLabel.gameObject.SetActive(false);
            }

            if (_bossVisual != null)
            {
                _bossVisual.gameObject.SetActive(isMiniBoss);
                if (isMiniBoss)
                {
                    var safeBossZ = Mathf.Max(Mathf.Abs(bossLocalOffset.z), distanceBehindLine + 1.7f);
                    _bossBaseLocalPosition = new Vector3(bossLocalOffset.x, bossLocalOffset.y, safeBossZ);
                    _bossVisual.localPosition = _bossBaseLocalPosition;
                    _bossVisual.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    _bossVisual.localScale = new Vector3(
                        Mathf.Max(3.2f, bossLocalScale.x),
                        Mathf.Max(3.2f, bossLocalScale.y),
                        Mathf.Max(3.2f, bossLocalScale.z));
                    _bossBaseScale = _bossVisual.localScale;
                    ResetBossAttackPose();
                }
                else
                {
                    ResetBossAttackPose();
                }
            }
        }

        public void TryTrigger(CrowdController crowd)
        {
            if (_isTriggered || crowd == null)
            {
                return;
            }

            _isTriggered = true;
            StopMiniBossHarass();
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            crowd.NotifyFinishReached(_enemyCount);
        }

        public void SetBossVisualActive(bool active)
        {
            if (_legacyBossVisual != null)
            {
                _legacyBossVisual.gameObject.SetActive(false);
            }

            if (_bossVisual == null)
            {
                return;
            }

            _bossVisual.gameObject.SetActive(active);
            if (active)
            {
                _bossVisual.localPosition = _bossBaseLocalPosition;
                _bossVisual.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            ResetBossAttackPose();
        }

        public void BeginMiniBossHarass(CrowdController crowd, int levelIndex, DifficultyMode difficultyMode)
        {
            StopMiniBossHarass();
            if (!enableRockBarrage || !_isMiniBoss || crowd == null || _bossVisual == null)
            {
                return;
            }

            EnsureRockBarrageAssets();
            _harassCrowd = crowd;
            _harassLevelIndex = Mathf.Max(1, levelIndex);
            _harassDifficulty = difficultyMode;
            _harassActive = true;
            _hasCrowdVelocity = false;
            _crowdVelocityWorld = Vector3.zero;
            _crowdLastPosition = crowd.transform.position;
            _harassThrowTimer = Mathf.Max(0.18f, ComputeRockThrowInterval() * 0.68f);
        }

        public void StopMiniBossHarass()
        {
            _harassActive = false;
            _harassCrowd = null;
            _hasCrowdVelocity = false;
            _crowdVelocityWorld = Vector3.zero;
            _harassThrowTimer = 0f;
            for (var i = 0; i < _rockProjectiles.Count; i++)
            {
                var projectile = _rockProjectiles[i];
                if (projectile == null || projectile.root == null)
                {
                    continue;
                }

                projectile.isActive = false;
                projectile.root.gameObject.SetActive(false);
            }
        }

        public void TriggerBossSlam(float strength = 1f)
        {
            if (_bossVisual == null || !_bossVisual.gameObject.activeInHierarchy)
            {
                return;
            }

            _bossSlamStrength = Mathf.Clamp(strength, 0.55f, 1.85f);
            _bossSlamElapsed = 0f;
            _bossSlamActive = true;
            _bossSlamImpactTriggered = false;
            _bossStrikeLeft = !_bossStrikeLeft;
        }

        private void UpdateBossAnimation(float deltaTime)
        {
            var safeDelta = Mathf.Max(0f, deltaTime);
            var idlePulse = 1f + Mathf.Sin(Time.time * bossPulseSpeed) * bossPulseAmplitude;
            var idleBob = Mathf.Sin(Time.time * (bossPulseSpeed * 0.64f)) * 0.08f;
            var idleSway = Mathf.Sin(Time.time * Mathf.Max(0.2f, bossRootSwaySpeed)) * bossRootSwayAmplitude;

            var coreYOffset = idleBob;
            var rootZOffset = 0f;
            var headPitch = Mathf.Sin(Time.time * 1.24f) * 3.2f;
            var jawPitch = Mathf.Abs(Mathf.Sin(Time.time * 1.12f)) * 2.8f;
            var armPitch = Mathf.Sin(Time.time * 1.86f) * 8f;
            var forearmPitch = Mathf.Sin(Time.time * 1.74f + 0.35f) * 7f;
            var handPitch = Mathf.Sin(Time.time * 2.18f + 0.9f) * 9f;
            var armTwist = Mathf.Sin(Time.time * 1.58f + 0.6f) * 5f;

            if (_bossSlamActive)
            {
                _bossSlamElapsed += safeDelta;
                var windup = Mathf.Max(0.04f, bossSlamWindupDuration);
                var strike = Mathf.Max(0.03f, bossSlamStrikeDuration);
                var recover = Mathf.Max(0.08f, bossSlamRecoverDuration);
                var slamDuration = windup + strike + recover;
                var t = _bossSlamElapsed;

                if (t < windup)
                {
                    var phase = EaseOutCubic(t / windup);
                    coreYOffset = Mathf.Lerp(coreYOffset, bossSlamDropDistance * 0.42f * _bossSlamStrength, phase);
                    rootZOffset = Mathf.Lerp(0f, -bossStepForwardOnStrike * 0.35f, phase);
                    armPitch = Mathf.Lerp(armPitch, -bossArmWindupDegrees * _bossSlamStrength, phase);
                    forearmPitch = Mathf.Lerp(forearmPitch, -bossArmWindupDegrees * 0.55f * _bossSlamStrength, phase);
                    handPitch = Mathf.Lerp(handPitch, -bossArmWindupDegrees * 0.36f * _bossSlamStrength, phase);
                    headPitch = Mathf.Lerp(headPitch, -bossHeadRoarDegrees * 0.7f * _bossSlamStrength, phase);
                    jawPitch = Mathf.Lerp(jawPitch, bossJawOpenDegrees * _bossSlamStrength, phase);
                }
                else if (t < windup + strike)
                {
                    var phase = EaseInCubic((t - windup) / strike);
                    coreYOffset = Mathf.Lerp(
                        bossSlamDropDistance * 0.42f * _bossSlamStrength,
                        -bossSlamDropDistance * _bossSlamStrength,
                        phase);
                    rootZOffset = Mathf.Lerp(-bossStepForwardOnStrike * 0.35f, bossStepForwardOnStrike * _bossSlamStrength, phase);
                    armPitch = Mathf.Lerp(
                        -bossArmWindupDegrees * _bossSlamStrength,
                        bossArmStrikeDegrees * _bossSlamStrength,
                        phase);
                    forearmPitch = Mathf.Lerp(
                        -bossArmWindupDegrees * 0.55f * _bossSlamStrength,
                        bossArmStrikeDegrees * 0.72f * _bossSlamStrength,
                        phase);
                    handPitch = Mathf.Lerp(
                        -bossArmWindupDegrees * 0.36f * _bossSlamStrength,
                        bossArmStrikeDegrees * 0.48f * _bossSlamStrength,
                        phase);
                    headPitch = Mathf.Lerp(
                        -bossHeadRoarDegrees * 0.7f * _bossSlamStrength,
                        bossHeadRoarDegrees * 0.22f,
                        phase);
                    jawPitch = Mathf.Lerp(bossJawOpenDegrees * _bossSlamStrength, bossJawOpenDegrees * 0.18f, phase);

                    if (!_bossSlamImpactTriggered && phase >= 0.42f)
                    {
                        _bossSlamImpactTriggered = true;
                        _bossSlamImpactFlash = 1f;
                        _bossImpactRingAlpha = 1f;
                        if (_bossImpactRing != null)
                        {
                            _bossImpactRing.localScale = new Vector3(0.55f, 1f, 0.55f);
                        }
                        EmitBossSlamFx();
                    }
                }
                else if (t < slamDuration)
                {
                    var phase = EaseOutCubic((t - windup - strike) / recover);
                    coreYOffset = Mathf.Lerp(-bossSlamDropDistance * _bossSlamStrength, idleBob, phase);
                    rootZOffset = Mathf.Lerp(bossStepForwardOnStrike * _bossSlamStrength, 0f, phase);
                    armPitch = Mathf.Lerp(bossArmStrikeDegrees * _bossSlamStrength, Mathf.Sin(Time.time * 1.86f) * 8f, phase);
                    forearmPitch = Mathf.Lerp(bossArmStrikeDegrees * 0.72f * _bossSlamStrength, Mathf.Sin(Time.time * 1.74f + 0.35f) * 7f, phase);
                    handPitch = Mathf.Lerp(bossArmStrikeDegrees * 0.48f * _bossSlamStrength, Mathf.Sin(Time.time * 2.18f + 0.9f) * 9f, phase);
                    headPitch = Mathf.Lerp(bossHeadRoarDegrees * 0.22f, Mathf.Sin(Time.time * 1.24f) * 3.2f, phase);
                    jawPitch = Mathf.Lerp(bossJawOpenDegrees * 0.18f, Mathf.Abs(Mathf.Sin(Time.time * 1.12f)) * 2.8f, phase);
                }
                else
                {
                    _bossSlamActive = false;
                    _bossSlamImpactTriggered = false;
                }
            }

            if (_bossVisual != null)
            {
                _bossVisual.localScale = _bossBaseScale * idlePulse;
                _bossVisual.localPosition = _bossBaseLocalPosition + new Vector3(idleSway * 0.08f, 0f, rootZOffset);
            }

            if (_bossCore != null)
            {
                _bossCore.localPosition = _bossCoreBaseLocalPosition + new Vector3(idleSway, coreYOffset, 0f);
            }

            if (_bossHeadPivot != null)
            {
                _bossHeadPivot.localRotation = _bossHeadBaseRotation * Quaternion.Euler(headPitch, -idleSway * 1.8f, 0f);
            }

            if (_bossJawPivot != null)
            {
                _bossJawPivot.localRotation = _bossJawBaseRotation * Quaternion.Euler(jawPitch, 0f, 0f);
            }

            if (_bossLeftArmPivot != null)
            {
                var attackBias = _bossStrikeLeft ? 1f : 1f - bossArmAsymmetry;
                _bossLeftArmPivot.localRotation =
                    _bossLeftArmBaseRotation * Quaternion.Euler(armPitch * attackBias, armTwist * attackBias, -12f);
            }

            if (_bossRightArmPivot != null)
            {
                var attackBias = _bossStrikeLeft ? 1f - bossArmAsymmetry : 1f;
                _bossRightArmPivot.localRotation =
                    _bossRightArmBaseRotation * Quaternion.Euler(armPitch * attackBias, -armTwist * attackBias, 12f);
            }

            if (_bossLeftForearmPivot != null)
            {
                var attackBias = _bossStrikeLeft ? 1f : 1f - bossArmAsymmetry;
                _bossLeftForearmPivot.localRotation =
                    _bossLeftForearmBaseRotation * Quaternion.Euler(forearmPitch * attackBias, armTwist * 0.72f, -6f);
            }

            if (_bossRightForearmPivot != null)
            {
                var attackBias = _bossStrikeLeft ? 1f - bossArmAsymmetry : 1f;
                _bossRightForearmPivot.localRotation =
                    _bossRightForearmBaseRotation * Quaternion.Euler(forearmPitch * attackBias, -armTwist * 0.72f, 6f);
            }

            if (_bossLeftHandPivot != null)
            {
                _bossLeftHandPivot.localRotation = _bossLeftHandBaseRotation * Quaternion.Euler(handPitch, 0f, -3f);
            }

            if (_bossRightHandPivot != null)
            {
                _bossRightHandPivot.localRotation = _bossRightHandBaseRotation * Quaternion.Euler(handPitch, 0f, 3f);
            }

            if (_bossImpactRing != null)
            {
                var currentScale = _bossImpactRing.localScale.x;
                var nextScale = currentScale + (safeDelta * bossImpactRingExpandSpeed);
                var clamped = Mathf.Min(bossImpactRingMaxScale, nextScale);
                _bossImpactRing.localScale = new Vector3(clamped, 1f, clamped);
            }

            if (_bossImpactRingMaterial != null)
            {
                _bossImpactRingAlpha = Mathf.MoveTowards(_bossImpactRingAlpha, 0f, safeDelta * bossImpactRingFadeSpeed);
                var ringColor = bossAccentColor;
                ringColor.a = 0.34f * _bossImpactRingAlpha;
                if (_bossImpactRingMaterial.HasProperty("_BaseColor"))
                {
                    _bossImpactRingMaterial.SetColor("_BaseColor", ringColor);
                }

                if (_bossImpactRingMaterial.HasProperty("_Color"))
                {
                    _bossImpactRingMaterial.SetColor("_Color", ringColor);
                }
            }

            _bossSlamImpactFlash = Mathf.MoveTowards(_bossSlamImpactFlash, 0f, safeDelta * 3.4f);
            UpdateBossEyeEmission();
        }

        private void ResetBossAttackPose()
        {
            _bossSlamActive = false;
            _bossSlamElapsed = 0f;
            _bossSlamImpactTriggered = false;
            _bossSlamImpactFlash = 0f;
            _bossImpactRingAlpha = 0f;

            if (_bossCore != null)
            {
                _bossCore.localPosition = _bossCoreBaseLocalPosition;
            }

            if (_bossVisual != null)
            {
                _bossVisual.localPosition = _bossBaseLocalPosition;
            }

            if (_bossHeadPivot != null)
            {
                _bossHeadPivot.localRotation = _bossHeadBaseRotation;
            }

            if (_bossJawPivot != null)
            {
                _bossJawPivot.localRotation = _bossJawBaseRotation;
            }

            if (_bossLeftArmPivot != null)
            {
                _bossLeftArmPivot.localRotation = _bossLeftArmBaseRotation;
            }

            if (_bossRightArmPivot != null)
            {
                _bossRightArmPivot.localRotation = _bossRightArmBaseRotation;
            }

            if (_bossLeftForearmPivot != null)
            {
                _bossLeftForearmPivot.localRotation = _bossLeftForearmBaseRotation;
            }

            if (_bossRightForearmPivot != null)
            {
                _bossRightForearmPivot.localRotation = _bossRightForearmBaseRotation;
            }

            if (_bossLeftHandPivot != null)
            {
                _bossLeftHandPivot.localRotation = _bossLeftHandBaseRotation;
            }

            if (_bossRightHandPivot != null)
            {
                _bossRightHandPivot.localRotation = _bossRightHandBaseRotation;
            }

            if (_bossImpactRing != null)
            {
                _bossImpactRing.localScale = new Vector3(0.55f, 1f, 0.55f);
            }

            UpdateBossEyeEmission();
        }

        private void EmitBossSlamFx()
        {
            EnsureBossSlamFx();
            if (_bossSlamFx == null)
            {
                return;
            }

            var burst = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(18f, 42f, Mathf.InverseLerp(0.55f, 1.85f, _bossSlamStrength))), 12, 52);
            var emit = new ParticleSystem.EmitParams
            {
                position = _bossVisual != null
                    ? _bossVisual.position + (_bossVisual.forward * 1.45f) + (Vector3.up * 0.18f)
                    : transform.position + (Vector3.forward * 1.2f),
                startColor = Color.Lerp(new Color(1f, 0.66f, 0.4f, 1f), new Color(1f, 0.42f, 0.24f, 1f), Random.value)
            };
            _bossSlamFx.Emit(emit, burst);

            if (_bossVisual != null)
            {
                emit.position = _bossVisual.position + (_bossVisual.forward * 1.85f) + (Vector3.up * 0.22f);
                emit.startColor = new Color(1f, 0.84f, 0.58f, 1f);
                _bossSlamFx.Emit(emit, Mathf.Max(6, burst / 3));
            }
        }

        private void EnsureBossSlamFx()
        {
            if (_bossVisual == null)
            {
                return;
            }

            if (_bossSlamFx == null)
            {
                var existing = _bossVisual.Find("BossSlamFx");
                if (existing != null)
                {
                    _bossSlamFx = existing.GetComponent<ParticleSystem>();
                }
            }

            if (_bossSlamFx != null)
            {
                return;
            }

            var fxObject = new GameObject("BossSlamFx");
            fxObject.transform.SetParent(_bossVisual, false);
            fxObject.transform.localPosition = new Vector3(0f, 0.16f, 1.25f);
            _bossSlamFx = fxObject.AddComponent<ParticleSystem>();
            _bossSlamFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = fxObject.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = CreateRuntimeMaterial("BossSlamFxMaterial", new Color(1f, 0.58f, 0.32f, 1f), 0.08f, 0.72f);
            }

            var main = _bossSlamFx.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.22f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 5.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.maxParticles = 128;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.4f, 1f));

            var emission = _bossSlamFx.emission;
            emission.enabled = false;

            var shape = _bossSlamFx.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.42f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            var colorOverLifetime = _bossSlamFx.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.74f, 0.46f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.38f, 0.24f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var velocityOverLifetime = _bossSlamFx.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2.8f, 7.4f);
        }

        private void UpdateBossEyeEmission()
        {
            if (_bossEyeMaterial == null)
            {
                return;
            }

            if (!_bossEyeMaterial.HasProperty("_EmissionColor"))
            {
                return;
            }

            var pulse = 0.8f + Mathf.Abs(Mathf.Sin(Time.time * 4.8f)) * 0.56f;
            var impactBoost = _bossSlamImpactFlash * 2.4f;
            _bossEyeMaterial.EnableKeyword("_EMISSION");
            _bossEyeMaterial.SetColor("_EmissionColor", bossEyeColor * (pulse + impactBoost));
        }

        private void UpdateRockBarrage(float deltaTime)
        {
            if (!_harassActive)
            {
                return;
            }

            if (!_isMiniBoss || !enableRockBarrage || _harassCrowd == null || !_harassCrowd.IsRunning)
            {
                StopMiniBossHarass();
                return;
            }

            if (_bossVisual == null || !_bossVisual.gameObject.activeInHierarchy)
            {
                return;
            }

            if (deltaTime > 0f)
            {
                var crowdPosition = _harassCrowd.transform.position;
                if (_hasCrowdVelocity)
                {
                    _crowdVelocityWorld = (crowdPosition - _crowdLastPosition) / Mathf.Max(0.001f, deltaTime);
                }
                else
                {
                    _crowdVelocityWorld = Vector3.zero;
                    _hasCrowdVelocity = true;
                }

                _crowdLastPosition = crowdPosition;
                _harassThrowTimer -= deltaTime;
                if (_harassThrowTimer <= 0f)
                {
                    if (TryLaunchRock(crowdPosition))
                    {
                        _harassThrowTimer = ComputeRockThrowInterval();
                    }
                    else
                    {
                        _harassThrowTimer = Mathf.Max(0.2f, ComputeRockThrowInterval() * 0.45f);
                    }
                }
            }

            for (var i = 0; i < _rockProjectiles.Count; i++)
            {
                var projectile = _rockProjectiles[i];
                if (projectile == null || !projectile.isActive)
                {
                    continue;
                }

                UpdateRockProjectile(projectile, deltaTime);
            }
        }

        private bool TryLaunchRock(Vector3 crowdPosition)
        {
            if (_harassCrowd == null)
            {
                return false;
            }

            var projectile = GetOrCreateRockProjectile();
            if (projectile == null || projectile.root == null)
            {
                return false;
            }

            var throwSource = _bossAimPoint != null
                ? _bossAimPoint.position
                : _bossVisual.position + (_bossVisual.forward * 1.2f) + (Vector3.up * 1.3f);

            if (_bossLeftHandPivot != null && _bossRightHandPivot != null)
            {
                throwSource = _bossThrowFromLeft ? _bossLeftHandPivot.position : _bossRightHandPivot.position;
                _bossThrowFromLeft = !_bossThrowFromLeft;
            }

            var predictedTarget = crowdPosition;
            predictedTarget += _crowdVelocityWorld * Mathf.Clamp(rockThrowLeadTime, 0f, 1.2f);
            predictedTarget.x += Random.Range(-rockThrowAimJitterX, rockThrowAimJitterX);
            var trackHalfWidth = Mathf.Max(1.2f, _harassCrowd.trackHalfWidth * 0.96f);
            predictedTarget.x = Mathf.Clamp(predictedTarget.x, -trackHalfWidth, trackHalfWidth);
            predictedTarget.y = Mathf.Max(transform.position.y - 0.08f, crowdPosition.y - 0.05f);
            predictedTarget.z = Mathf.Clamp(predictedTarget.z, crowdPosition.z - 1.2f, crowdPosition.z + 1.4f);

            var distance = Vector3.Distance(throwSource, predictedTarget);
            var travelSpeed = ComputeRockTravelSpeed();
            var duration = Mathf.Clamp(distance / Mathf.Max(6f, travelSpeed), 0.42f, 2.4f);
            var radius = ComputeRockRadius();
            var arcHeight = Mathf.Max(0.8f, rockArcHeightBase + ((_harassLevelIndex - 1) * rockArcHeightPerLevel));
            var maxLoss = ComputeRockMaxLoss(radius);

            projectile.startWorld = throwSource;
            projectile.targetWorld = predictedTarget;
            projectile.duration = duration;
            projectile.elapsed = 0f;
            projectile.arcHeight = arcHeight;
            projectile.radius = radius;
            projectile.maxUnitLoss = maxLoss;
            projectile.isActive = true;

            var scale = radius * 2f;
            projectile.root.gameObject.SetActive(true);
            projectile.root.position = throwSource;
            projectile.root.rotation = Quaternion.identity;
            if (projectile.visual != null)
            {
                projectile.visual.localScale = new Vector3(scale, scale * 0.92f, scale);
            }

            if (projectile.shadow != null)
            {
                projectile.shadow.position = new Vector3(throwSource.x, predictedTarget.y + 0.03f, throwSource.z);
                projectile.shadow.localScale = new Vector3(scale * 0.36f, 1f, scale * 0.36f);
                projectile.shadow.gameObject.SetActive(true);
            }

            if (projectile.telegraph != null)
            {
                projectile.telegraph.position = new Vector3(predictedTarget.x, predictedTarget.y + 0.02f, predictedTarget.z);
                projectile.telegraph.localScale = new Vector3(scale * 0.22f, 1f, scale * 0.22f);
                projectile.telegraph.gameObject.SetActive(true);
            }

            return true;
        }

        private void UpdateRockProjectile(RockProjectile projectile, float deltaTime)
        {
            if (projectile == null || projectile.root == null)
            {
                return;
            }

            projectile.elapsed += Mathf.Max(0f, deltaTime);
            var t = projectile.duration > 0f
                ? Mathf.Clamp01(projectile.elapsed / projectile.duration)
                : 1f;
            var basePosition = Vector3.Lerp(projectile.startWorld, projectile.targetWorld, t);
            var arc = Mathf.Sin(t * Mathf.PI) * projectile.arcHeight;
            var worldPosition = basePosition + (Vector3.up * arc);
            projectile.root.position = worldPosition;

            var direction = projectile.targetWorld - projectile.startWorld;
            if (direction.sqrMagnitude > 0.0001f)
            {
                projectile.root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (projectile.shadow != null)
            {
                var shadowSize = Mathf.Lerp(0.34f, 1f, t);
                var baseScale = projectile.radius * 2f;
                projectile.shadow.position = new Vector3(worldPosition.x, projectile.targetWorld.y + 0.02f, worldPosition.z);
                projectile.shadow.localScale = new Vector3(baseScale * shadowSize, 1f, baseScale * shadowSize);
            }

            if (projectile.telegraph != null)
            {
                var pulse = 1f + (Mathf.Sin(Time.time * 7.5f) * rockTelegraphScalePulse);
                var telegraphScale = projectile.radius * 2.1f * Mathf.Lerp(0.36f, 1f, t) * pulse;
                projectile.telegraph.localScale = new Vector3(telegraphScale, 1f, telegraphScale);
            }

            if (t < 1f)
            {
                return;
            }

            ImpactRock(projectile);
        }

        private void ImpactRock(RockProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            var impactPoint = projectile.targetWorld;
            EmitRockImpactFx(impactPoint, projectile.radius);
            if (_harassCrowd != null)
            {
                var overlapBounds = new Bounds(
                    impactPoint + new Vector3(0f, 0.55f, 0f),
                    new Vector3(projectile.radius * 2f, 2.2f, projectile.radius * 2f));
                var removed = _harassCrowd.ApplyCrushOverlapLoss(overlapBounds, projectile.maxUnitLoss, impactPoint);
                if (removed > 0)
                {
                    AudioDirector.Instance?.PlaySfx(AudioSfxCue.BattleHit, 0.62f, Random.Range(0.72f, 0.88f));
                    HapticsDirector.Instance?.Play(HapticCue.MediumImpact);
                }
            }

            projectile.isActive = false;
            if (projectile.root != null)
            {
                projectile.root.gameObject.SetActive(false);
            }
        }

        private float ComputeRockThrowInterval()
        {
            var levelFactor = Mathf.Clamp01((_harassLevelIndex - 1) / 90f);
            var difficultyFactor = _harassDifficulty == DifficultyMode.Hard
                ? 1f
                : _harassDifficulty == DifficultyMode.Easy ? 0.38f : 0.7f;
            var interval = Mathf.Lerp(rockThrowBaseInterval, rockThrowMinInterval, Mathf.Clamp01((levelFactor * 0.72f) + (difficultyFactor * 0.52f)));
            interval += Random.Range(-rockThrowIntervalJitter, rockThrowIntervalJitter);
            return Mathf.Max(rockThrowMinInterval, interval);
        }

        private float ComputeRockTravelSpeed()
        {
            var speed = rockTravelSpeedBase + ((_harassLevelIndex - 1) * rockTravelSpeedPerLevel);
            if (_harassDifficulty == DifficultyMode.Hard)
            {
                speed += Mathf.Abs(rockTravelSpeedHardBonus);
            }
            else if (_harassDifficulty == DifficultyMode.Easy)
            {
                speed *= 0.9f;
            }

            return Mathf.Max(6f, speed);
        }

        private float ComputeRockRadius()
        {
            var radius = rockRadiusBase + ((_harassLevelIndex - 1) * rockRadiusPerLevel);
            if (_harassDifficulty == DifficultyMode.Hard)
            {
                radius += Mathf.Abs(rockRadiusHardBonus);
            }
            else if (_harassDifficulty == DifficultyMode.Easy)
            {
                radius *= 0.9f;
            }

            return Mathf.Clamp(radius, 0.4f, 2.8f);
        }

        private int ComputeRockMaxLoss(float radius)
        {
            var baseLoss = rockLossBase + ((_harassLevelIndex - 1) * rockLossPerLevel) + (radius * 3.2f);
            if (_harassDifficulty == DifficultyMode.Hard)
            {
                baseLoss += Mathf.Max(0, rockLossHardBonus);
            }
            else if (_harassDifficulty == DifficultyMode.Easy)
            {
                baseLoss *= 0.72f;
            }

            return Mathf.Clamp(Mathf.RoundToInt(baseLoss), 2, 120);
        }

        private RockProjectile GetOrCreateRockProjectile()
        {
            for (var i = 0; i < _rockProjectiles.Count; i++)
            {
                var existing = _rockProjectiles[i];
                if (existing != null && !existing.isActive && existing.root != null)
                {
                    return existing;
                }
            }

            var safeLimit = Mathf.Clamp(maxActiveRocks, 1, 12);
            if (_rockProjectiles.Count >= safeLimit)
            {
                return null;
            }

            EnsureRockBarrageAssets();
            if (_rockProjectileRoot == null)
            {
                return null;
            }

            var projectileRoot = new GameObject("BossRock_" + _rockProjectiles.Count).transform;
            projectileRoot.SetParent(_rockProjectileRoot, false);
            projectileRoot.gameObject.SetActive(false);

            var visual = CreateBossPart(
                projectileRoot,
                "Visual",
                PrimitiveType.Sphere,
                _rockMaterial,
                Vector3.zero,
                Vector3.one);
            CreateBossPart(
                visual,
                "ChunkA",
                PrimitiveType.Cube,
                _rockMaterial,
                new Vector3(0.3f, 0.08f, -0.16f),
                new Vector3(0.36f, 0.24f, 0.34f),
                new Vector3(14f, 22f, -8f));
            CreateBossPart(
                visual,
                "ChunkB",
                PrimitiveType.Cube,
                _rockMaterial,
                new Vector3(-0.28f, -0.1f, 0.18f),
                new Vector3(0.28f, 0.22f, 0.3f),
                new Vector3(-10f, -16f, 9f));

            var shadow = CreateBossPart(
                projectileRoot,
                "Shadow",
                PrimitiveType.Cylinder,
                _rockShadowMaterial,
                Vector3.zero,
                new Vector3(0.6f, 0.01f, 0.6f));
            var telegraph = CreateBossPart(
                projectileRoot,
                "Telegraph",
                PrimitiveType.Cylinder,
                _rockTelegraphMaterial,
                Vector3.zero,
                new Vector3(0.3f, 0.015f, 0.3f));

            var projectile = new RockProjectile
            {
                root = projectileRoot,
                visual = visual,
                shadow = shadow,
                telegraph = telegraph,
                isActive = false
            };
            _rockProjectiles.Add(projectile);
            return projectile;
        }

        private void EnsureRockBarrageAssets()
        {
            if (_rockProjectileRoot == null)
            {
                _rockProjectileRoot = transform.Find("BossRockProjectiles");
                if (_rockProjectileRoot == null)
                {
                    _rockProjectileRoot = new GameObject("BossRockProjectiles").transform;
                    _rockProjectileRoot.SetParent(transform, false);
                    _rockProjectileRoot.localPosition = Vector3.zero;
                    _rockProjectileRoot.localRotation = Quaternion.identity;
                    _rockProjectileRoot.localScale = Vector3.one;
                }
            }

            if (_rockMaterial == null)
            {
                _rockMaterial = CreateRuntimeMaterial("BossRockMaterial", rockColor, 0.06f, 0.08f);
            }

            if (_rockTelegraphMaterial == null)
            {
                _rockTelegraphMaterial = CreateRuntimeMaterial("BossRockTelegraphMaterial", rockTelegraphColor, 0f, 0.46f);
            }

            if (_rockShadowMaterial == null)
            {
                _rockShadowMaterial = CreateRuntimeMaterial("BossRockShadowMaterial", rockShadowColor, 0f, 0f);
            }

            EnsureRockImpactFx();
        }

        private void EnsureRockImpactFx()
        {
            if (_rockImpactFx != null)
            {
                return;
            }

            var impactFxTransform = transform.Find("BossRockImpactFx");
            if (impactFxTransform == null)
            {
                impactFxTransform = new GameObject("BossRockImpactFx").transform;
                impactFxTransform.SetParent(transform, false);
                impactFxTransform.localPosition = Vector3.zero;
                impactFxTransform.localRotation = Quaternion.identity;
                impactFxTransform.localScale = Vector3.one;
            }

            _rockImpactFx = impactFxTransform.GetComponent<ParticleSystem>();
            if (_rockImpactFx == null)
            {
                _rockImpactFx = impactFxTransform.gameObject.AddComponent<ParticleSystem>();
            }

            _rockImpactFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = impactFxTransform.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = CreateRuntimeMaterial("BossRockImpactFxMaterial", new Color(1f, 0.62f, 0.42f, 1f), 0.1f, 0.78f);
            }

            var main = _rockImpactFx.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 4.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.44f, 1f));

            var emission = _rockImpactFx.emission;
            emission.enabled = false;

            var shape = _rockImpactFx.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.34f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            var velocityOverLifetime = _rockImpactFx.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2.2f, 6.1f);
        }

        private void EmitRockImpactFx(Vector3 impactPoint, float radius)
        {
            EnsureRockImpactFx();
            if (_rockImpactFx == null)
            {
                return;
            }

            var emitParams = new ParticleSystem.EmitParams
            {
                position = impactPoint + new Vector3(0f, 0.04f, 0f),
                startColor = Color.Lerp(new Color(1f, 0.76f, 0.48f, 1f), new Color(1f, 0.52f, 0.32f, 1f), Random.value)
            };
            var count = Mathf.Clamp(Mathf.RoundToInt(14f + (radius * 14f)), 10, 56);
            _rockImpactFx.Emit(emitParams, count);
        }

        private static float EaseOutCubic(float t)
        {
            var clamped = Mathf.Clamp01(t);
            var inv = 1f - clamped;
            return 1f - (inv * inv * inv);
        }

        private static float EaseInCubic(float t)
        {
            var clamped = Mathf.Clamp01(t);
            return clamped * clamped * clamped;
        }

        private void EnsureLabels()
        {
            if (enemyCountLabel == null)
            {
                var enemyLabelTransform = transform.Find("EnemyLabel");
                if (enemyLabelTransform != null)
                {
                    enemyCountLabel = enemyLabelTransform.GetComponent<TextMesh>();
                }
            }

            if (tankRequirementLabel != null)
            {
                return;
            }

            var tankLabelTransform = transform.Find("TankLabel");
            if (tankLabelTransform == null)
            {
                tankLabelTransform = new GameObject("TankLabel").transform;
                tankLabelTransform.SetParent(transform, false);
            }

            tankRequirementLabel = tankLabelTransform.GetComponent<TextMesh>();
            if (tankRequirementLabel == null)
            {
                tankRequirementLabel = tankLabelTransform.gameObject.AddComponent<TextMesh>();
            }

            tankLabelTransform.localPosition = new Vector3(0f, 2.6f, -0.48f);
            tankLabelTransform.localRotation = Quaternion.identity;
            tankLabelTransform.localScale = Vector3.one * 0.18f;

            tankRequirementLabel.text = string.Empty;
            tankRequirementLabel.anchor = TextAnchor.MiddleCenter;
            tankRequirementLabel.alignment = TextAlignment.Center;
            tankRequirementLabel.characterSize = 0.22f;
            tankRequirementLabel.fontSize = 130;
            tankRequirementLabel.gameObject.SetActive(false);
        }

        private void EnsureBossVisual()
        {
            if (_bossVisual != null)
            {
                return;
            }

            var legacyTank = transform.Find("BossUnit");
            if (legacyTank != null)
            {
                _legacyBossVisual = legacyTank;
                legacyTank.gameObject.SetActive(false);
            }

            var existing = transform.Find("BossMonster");
            if (existing != null)
            {
                var isCurrentRig = existing.Find("RigVersion_3") != null;
                if (!isCurrentRig)
                {
                    existing.name = "BossMonster_Obsolete";
                    if (Application.isPlaying)
                    {
                        Destroy(existing.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(existing.gameObject);
                    }

                    BuildBossMonster();
                    return;
                }

                _bossVisual = existing;
                ResolveBossRigReferences();
                return;
            }

            BuildBossMonster();
        }

        private void BuildBossMonster()
        {
            EnsureBossMaterials();

            var root = new GameObject("BossMonster").transform;
            root.SetParent(transform, false);
            root.localPosition = bossLocalOffset;
            root.localRotation = Quaternion.Euler(0f, 180f, 0f);
            root.localScale = bossLocalScale;

            var rigMarker = new GameObject("RigVersion_3").transform;
            rigMarker.SetParent(root, false);

            _bossCore = new GameObject("Core").transform;
            _bossCore.SetParent(root, false);
            _bossCore.localPosition = new Vector3(0f, 0.86f, 0f);
            _bossCore.localRotation = Quaternion.identity;
            _bossCore.localScale = Vector3.one;

            CreateBossPart(_bossCore, "Torso", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0f, 1.08f, -0.08f), new Vector3(1.24f, 1.66f, 1.1f));
            CreateBossPart(_bossCore, "ShoulderPlateLeft", PrimitiveType.Sphere, _bossAccentMaterial, new Vector3(-0.78f, 1.72f, -0.02f), new Vector3(0.46f, 0.38f, 0.52f));
            CreateBossPart(_bossCore, "ShoulderPlateRight", PrimitiveType.Sphere, _bossAccentMaterial, new Vector3(0.78f, 1.72f, -0.02f), new Vector3(0.46f, 0.38f, 0.52f));
            CreateBossPart(_bossCore, "ChestPlate", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 1.2f, 0.6f), new Vector3(1f, 0.58f, 0.34f));
            CreateBossPart(_bossCore, "RibPlateLeft", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(-0.38f, 0.86f, 0.56f), new Vector3(0.22f, 0.46f, 0.22f), new Vector3(0f, 0f, 20f));
            CreateBossPart(_bossCore, "RibPlateRight", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0.38f, 0.86f, 0.56f), new Vector3(0.22f, 0.46f, 0.22f), new Vector3(0f, 0f, -20f));
            CreateBossPart(_bossCore, "Belly", PrimitiveType.Sphere, _bossBodyMaterial, new Vector3(0f, 0.66f, 0.18f), new Vector3(1.08f, 0.8f, 0.96f));
            CreateBossPart(_bossCore, "BackSpine", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 1.2f, -0.66f), new Vector3(0.42f, 0.58f, 0.2f));
            CreateBossPart(_bossCore, "TailBase", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0f, 0.46f, -0.88f), new Vector3(0.28f, 0.46f, 0.28f), new Vector3(-52f, 0f, 0f));
            CreateBossPart(_bossCore, "TailTip", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(0f, 0.36f, -1.16f), new Vector3(0.12f, 0.36f, 0.12f), new Vector3(-88f, 0f, 0f));

            CreateBossPart(_bossCore, "HipLeft", PrimitiveType.Cube, _bossBodyMaterial, new Vector3(-0.48f, 0.12f, 0.12f), new Vector3(0.42f, 0.48f, 0.46f));
            CreateBossPart(_bossCore, "HipRight", PrimitiveType.Cube, _bossBodyMaterial, new Vector3(0.48f, 0.12f, 0.12f), new Vector3(0.42f, 0.48f, 0.46f));
            CreateBossPart(_bossCore, "LegLeft", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(-0.48f, -0.46f, 0.16f), new Vector3(0.34f, 0.66f, 0.34f));
            CreateBossPart(_bossCore, "LegRight", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0.48f, -0.46f, 0.16f), new Vector3(0.34f, 0.66f, 0.34f));
            CreateBossPart(_bossCore, "FootLeft", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(-0.48f, -0.92f, 0.34f), new Vector3(0.44f, 0.18f, 0.66f));
            CreateBossPart(_bossCore, "FootRight", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0.48f, -0.92f, 0.34f), new Vector3(0.44f, 0.18f, 0.66f));

            _bossHeadPivot = new GameObject("HeadPivot").transform;
            _bossHeadPivot.SetParent(_bossCore, false);
            _bossHeadPivot.localPosition = new Vector3(0f, 2.02f, 0.3f);
            _bossHeadPivot.localRotation = Quaternion.identity;

            CreateBossPart(_bossHeadPivot, "Head", PrimitiveType.Sphere, _bossBodyMaterial, new Vector3(0f, 0f, 0.04f), new Vector3(1.02f, 0.84f, 1.1f));
            CreateBossPart(_bossHeadPivot, "Snout", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, -0.04f, 0.48f), new Vector3(0.8f, 0.24f, 0.56f));
            CreateBossPart(_bossHeadPivot, "Brow", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 0.2f, 0.36f), new Vector3(0.82f, 0.14f, 0.2f));
            CreateBossPart(_bossHeadPivot, "Crest", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 0.32f, -0.18f), new Vector3(0.32f, 0.28f, 0.22f));

            _bossJawPivot = new GameObject("JawPivot").transform;
            _bossJawPivot.SetParent(_bossHeadPivot, false);
            _bossJawPivot.localPosition = new Vector3(0f, -0.2f, 0.46f);
            _bossJawPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossJawPivot, "Jaw", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, -0.06f, 0.18f), new Vector3(0.74f, 0.18f, 0.62f));
            CreateBossPart(_bossJawPivot, "FangLeft", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(-0.22f, -0.14f, 0.38f), new Vector3(0.08f, 0.18f, 0.08f), new Vector3(18f, 0f, 0f));
            CreateBossPart(_bossJawPivot, "FangRight", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(0.22f, -0.14f, 0.38f), new Vector3(0.08f, 0.18f, 0.08f), new Vector3(18f, 0f, 0f));

            CreateBossPart(_bossHeadPivot, "HornLeft", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(-0.36f, 0.36f, -0.18f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(-40f, 0f, -24f));
            CreateBossPart(_bossHeadPivot, "HornRight", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(0.36f, 0.36f, -0.18f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(-40f, 0f, 24f));
            CreateBossPart(_bossHeadPivot, "EyeLeft", PrimitiveType.Sphere, _bossEyeMaterial, new Vector3(-0.22f, 0.06f, 0.48f), new Vector3(0.14f, 0.14f, 0.14f));
            CreateBossPart(_bossHeadPivot, "EyeRight", PrimitiveType.Sphere, _bossEyeMaterial, new Vector3(0.22f, 0.06f, 0.48f), new Vector3(0.14f, 0.14f, 0.14f));

            _bossLeftArmPivot = new GameObject("LeftArmPivot").transform;
            _bossLeftArmPivot.SetParent(_bossCore, false);
            _bossLeftArmPivot.localPosition = new Vector3(-1f, 1.44f, 0.12f);
            _bossLeftArmPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossLeftArmPivot, "LeftUpperArm", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0f, -0.6f, 0f), new Vector3(0.42f, 0.96f, 0.42f));
            CreateBossPart(_bossLeftArmPivot, "LeftShoulderSpike", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(-0.04f, -0.08f, 0.32f), new Vector3(0.08f, 0.24f, 0.08f), new Vector3(80f, 0f, 0f));

            _bossLeftForearmPivot = new GameObject("LeftForearmPivot").transform;
            _bossLeftForearmPivot.SetParent(_bossLeftArmPivot, false);
            _bossLeftForearmPivot.localPosition = new Vector3(0f, -1.02f, 0.12f);
            _bossLeftForearmPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossLeftForearmPivot, "LeftForearm", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0f, -0.56f, 0f), new Vector3(0.38f, 0.94f, 0.38f));

            _bossLeftHandPivot = new GameObject("LeftHandPivot").transform;
            _bossLeftHandPivot.SetParent(_bossLeftForearmPivot, false);
            _bossLeftHandPivot.localPosition = new Vector3(0f, -1f, 0.16f);
            _bossLeftHandPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossLeftHandPivot, "LeftHand", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, -0.14f, 0.18f), new Vector3(0.52f, 0.24f, 0.72f));
            CreateBossPart(_bossLeftHandPivot, "LeftClawA", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(-0.18f, -0.18f, 0.44f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(28f, 0f, -8f));
            CreateBossPart(_bossLeftHandPivot, "LeftClawB", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(0f, -0.18f, 0.48f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(28f, 0f, 0f));
            CreateBossPart(_bossLeftHandPivot, "LeftClawC", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(0.18f, -0.18f, 0.44f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(28f, 0f, 8f));

            _bossRightArmPivot = new GameObject("RightArmPivot").transform;
            _bossRightArmPivot.SetParent(_bossCore, false);
            _bossRightArmPivot.localPosition = new Vector3(1f, 1.44f, 0.12f);
            _bossRightArmPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossRightArmPivot, "RightUpperArm", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0f, -0.6f, 0f), new Vector3(0.42f, 0.96f, 0.42f));
            CreateBossPart(_bossRightArmPivot, "RightShoulderSpike", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(0.04f, -0.08f, 0.32f), new Vector3(0.08f, 0.24f, 0.08f), new Vector3(80f, 0f, 0f));

            _bossRightForearmPivot = new GameObject("RightForearmPivot").transform;
            _bossRightForearmPivot.SetParent(_bossRightArmPivot, false);
            _bossRightForearmPivot.localPosition = new Vector3(0f, -1.02f, 0.12f);
            _bossRightForearmPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossRightForearmPivot, "RightForearm", PrimitiveType.Capsule, _bossBodyMaterial, new Vector3(0f, -0.56f, 0f), new Vector3(0.38f, 0.94f, 0.38f));

            _bossRightHandPivot = new GameObject("RightHandPivot").transform;
            _bossRightHandPivot.SetParent(_bossRightForearmPivot, false);
            _bossRightHandPivot.localPosition = new Vector3(0f, -1f, 0.16f);
            _bossRightHandPivot.localRotation = Quaternion.identity;
            CreateBossPart(_bossRightHandPivot, "RightHand", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, -0.14f, 0.18f), new Vector3(0.52f, 0.24f, 0.72f));
            CreateBossPart(_bossRightHandPivot, "RightClawA", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(-0.18f, -0.18f, 0.44f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(28f, 0f, -8f));
            CreateBossPart(_bossRightHandPivot, "RightClawB", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(0f, -0.18f, 0.48f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(28f, 0f, 0f));
            CreateBossPart(_bossRightHandPivot, "RightClawC", PrimitiveType.Cube, _bossEyeMaterial, new Vector3(0.18f, -0.18f, 0.44f), new Vector3(0.1f, 0.34f, 0.1f), new Vector3(28f, 0f, 8f));

            for (var i = 0; i < 7; i++)
            {
                var z = Mathf.Lerp(-0.76f, 0.56f, i / 6f);
                var y = 1.42f + Mathf.Abs(i - 3) * 0.05f;
                CreateBossPart(
                    _bossCore,
                    "BackSpike_" + i,
                    PrimitiveType.Cylinder,
                    _bossAccentMaterial,
                    new Vector3(0f, y, z),
                    new Vector3(0.06f, 0.24f, 0.06f),
                    new Vector3(-8f, 0f, 0f));
            }

            _bossAimPoint = new GameObject("AimPoint").transform;
            _bossAimPoint.SetParent(_bossCore, false);
            _bossAimPoint.localPosition = new Vector3(0f, 1.62f, 0.62f);
            _bossAimPoint.localRotation = Quaternion.identity;

            _bossImpactRingMaterial = CreateRuntimeMaterial(
                "BossImpactRingMaterial",
                new Color(bossAccentColor.r, bossAccentColor.g, bossAccentColor.b, 0.12f),
                0f,
                0.35f);
            _bossImpactRing = CreateBossPart(
                root,
                "ImpactRing",
                PrimitiveType.Cylinder,
                _bossImpactRingMaterial,
                new Vector3(0f, 0.05f, 1.4f),
                new Vector3(0.55f, 0.02f, 0.55f));

            _bossVisual = root;
            _bossBaseScale = root.localScale;
            _bossBaseLocalPosition = root.localPosition;

            ResolveBossRigReferences();
            EnsureBossSlamFx();
            _bossVisual.gameObject.SetActive(false);
        }

        private void ResolveBossRigReferences()
        {
            if (_bossVisual == null)
            {
                return;
            }

            _bossCore = _bossVisual.Find("Core");
            _bossHeadPivot = _bossCore != null ? _bossCore.Find("HeadPivot") : null;
            _bossJawPivot = _bossHeadPivot != null ? _bossHeadPivot.Find("JawPivot") : null;
            _bossLeftArmPivot = _bossCore != null ? _bossCore.Find("LeftArmPivot") : null;
            _bossRightArmPivot = _bossCore != null ? _bossCore.Find("RightArmPivot") : null;
            _bossLeftForearmPivot = _bossLeftArmPivot != null ? _bossLeftArmPivot.Find("LeftForearmPivot") : null;
            _bossRightForearmPivot = _bossRightArmPivot != null ? _bossRightArmPivot.Find("RightForearmPivot") : null;
            _bossLeftHandPivot = _bossLeftForearmPivot != null ? _bossLeftForearmPivot.Find("LeftHandPivot") : null;
            _bossRightHandPivot = _bossRightForearmPivot != null ? _bossRightForearmPivot.Find("RightHandPivot") : null;
            _bossAimPoint = _bossCore != null ? _bossCore.Find("AimPoint") : null;
            _bossImpactRing = _bossVisual.Find("ImpactRing");

            if (_bossImpactRing != null)
            {
                var ringRenderer = _bossImpactRing.GetComponent<MeshRenderer>();
                if (ringRenderer != null)
                {
                    _bossImpactRingMaterial = ringRenderer.sharedMaterial;
                }
            }

            _bossBaseScale = _bossVisual.localScale;
            _bossBaseLocalPosition = _bossVisual.localPosition;
            _bossCoreBaseLocalPosition = _bossCore != null ? _bossCore.localPosition : Vector3.zero;
            _bossHeadBaseRotation = _bossHeadPivot != null ? _bossHeadPivot.localRotation : Quaternion.identity;
            _bossJawBaseRotation = _bossJawPivot != null ? _bossJawPivot.localRotation : Quaternion.identity;
            _bossLeftArmBaseRotation = _bossLeftArmPivot != null ? _bossLeftArmPivot.localRotation : Quaternion.identity;
            _bossRightArmBaseRotation = _bossRightArmPivot != null ? _bossRightArmPivot.localRotation : Quaternion.identity;
            _bossLeftForearmBaseRotation = _bossLeftForearmPivot != null ? _bossLeftForearmPivot.localRotation : Quaternion.identity;
            _bossRightForearmBaseRotation = _bossRightForearmPivot != null ? _bossRightForearmPivot.localRotation : Quaternion.identity;
            _bossLeftHandBaseRotation = _bossLeftHandPivot != null ? _bossLeftHandPivot.localRotation : Quaternion.identity;
            _bossRightHandBaseRotation = _bossRightHandPivot != null ? _bossRightHandPivot.localRotation : Quaternion.identity;
        }

        private void EnsureBossMaterials()
        {
            if (_bossBodyMaterial == null)
            {
                _bossBodyMaterial = CreateRuntimeMaterial("BossBodyMaterial", bossBodyColor, 0.22f, 0.2f);
            }

            if (_bossAccentMaterial == null)
            {
                _bossAccentMaterial = CreateRuntimeMaterial("BossAccentMaterial", bossAccentColor, 0.32f, 0.44f);
            }

            if (_bossEyeMaterial == null)
            {
                _bossEyeMaterial = CreateRuntimeMaterial("BossEyeMaterial", bossEyeColor, 0.46f, 1.3f);
            }
        }

        private static Transform CreateBossPart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler = default)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return part.transform;
        }

        private static Material CreateRuntimeMaterial(string name, Color color, float smoothness, float emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Clamp(emission, 0f, 2f));
            }

            if (color.a < 0.995f)
            {
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                }

                if (material.HasProperty("_Blend"))
                {
                    material.SetFloat("_Blend", 0f);
                }

                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                }

                if (material.HasProperty("_DstBlend"))
                {
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }

                if (material.HasProperty("_ZWrite"))
                {
                    material.SetFloat("_ZWrite", 0f);
                }

                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return material;
        }
    }
}
