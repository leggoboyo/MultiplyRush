using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    public enum BackdropQuality
    {
        Auto = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    [Serializable]
    public struct LevelBuildResult
    {
        public int levelIndex;
        public int startCount;
        public int enemyCount;
        public int tankRequirement;
        public int referenceRouteCount;
        public int referenceBetterRows;
        public int referenceWorseRows;
        public int referenceRedRows;
        public int totalRows;
        public float finishZ;
        public float trackHalfWidth;
        public float forwardSpeed;
        public bool isMiniBoss;
        public string modifierName;
    }

    public sealed class LevelGenerator : MonoBehaviour
    {
        [Header("References")]
        public Gate gatePrefab;
        public FinishLine finishPrefab;
        public Transform levelRoot;
        public Transform gateRoot;
        public Transform trackVisual;

        [Header("Layout")]
        public float laneSpacing = 2.8f;
        public float minLaneSpacing = 3.6f;
        public float laneToEdgePadding = 1.4f;
        public float trackHalfWidth = 4.5f;
        public float rowSpacing = 12f;
        [Range(1f, 2f)]
        public float levelLengthMultiplier = 1f;
        [Range(0.3f, 1f)]
        public float rowCountCompression = 0.5f;
        public float startZ = 18f;
        public float endPadding = 18f;

        [Header("Difficulty")]
        public int baseStartCount = 20;
        public float baseForwardSpeed = 8f;
        public float maxForwardSpeed = 12.5f;
        public float forwardSpeedPerLevel = 0.035f;
        public bool useForwardSpeedCap = false;

        [Header("Enemy Formula")]
        public int enemyFormulaBase = 26;
        public float enemyFormulaLinear = 6.2f;
        public float enemyFormulaPowerMultiplier = 1.65f;
        public float enemyFormulaPower = 1.09f;
        [Range(0.4f, 0.98f)]
        public float enemyMaxFractionOfBestPath = 0.9f;
        [Range(0.1f, 0.8f)]
        public float enemyMinFractionOfBestPath = 0.34f;

        [Header("Gate Difficulty")]
        [Range(0f, 0.2f)]
        public float gateDifficultyRamp = 0.028f;
        public float gateWidthAtStart = 2.5f;
        public float gateWidthAtHighDifficulty = 2f;
        public float panelWidthAtStart = 2.6f;
        public float panelWidthAtHighDifficulty = 2.2f;
        [Range(0f, 1f)]
        public float movingGateChanceAtStart = 0f;
        [Range(0f, 1f)]
        public float movingGateChanceAtHighDifficulty = 0.75f;
        public float movingGateAmplitudeAtStart = 0.12f;
        public float movingGateAmplitudeAtHighDifficulty = 1.05f;
        public float movingGateSpeedAtStart = 1.1f;
        public float movingGateSpeedAtHighDifficulty = 2.8f;

        [Header("Gate Shot Upgrades")]
        public bool enableGateShotUpgrades = true;
        public int gateUpgradeShotsAtLevel1 = 11;
        public float gateUpgradeShotsPerLevel = 0.65f;
        public int gateUpgradeShotsGrowthPerStep = 4;
        public int gateAddUpgradeBonusCapAtLevel1 = 7;
        public int gateAddUpgradeBonusCapGrowthPer10Levels = 1;
        [Range(2.1f, 3f)]
        public float gateMultiplyUpgradeCapAtLevel1 = 2.6f;
        [Range(0f, 0.4f)]
        public float gateMultiplyUpgradeCapGrowthPer20Levels = 0.08f;

        [Header("Special Modes")]
        public int miniBossEveryLevels = 10;
        [Range(0f, 1f)]
        public float riskRewardChanceAtStart = 0.08f;
        [Range(0f, 1f)]
        public float riskRewardChanceAtHighDifficulty = 0.3f;
        [Range(0f, 1f)]
        public float tempoRowChanceAtStart = 0.06f;
        [Range(0f, 1f)]
        public float tempoRowChanceAtHighDifficulty = 0.42f;
        public float tempoCycleAtStart = 2.2f;
        public float tempoCycleAtHighDifficulty = 1.25f;
        [Range(0.08f, 0.92f)]
        public float tempoOpenRatioAtStart = 0.62f;
        [Range(0.08f, 0.92f)]
        public float tempoOpenRatioAtHighDifficulty = 0.3f;

        [Header("Adaptive Pressure")]
        public bool enableAdaptiveLanePressure = true;
        [Range(0f, 1f)]
        public float lanePressureLerp = 0.34f;
        [Range(0f, 2f)]
        public float lanePressureStrength = 0.8f;
        [Range(0f, 0.3f)]
        public float lanePressureDecay = 0.06f;

        [Header("Hazards")]
        public bool enableHazards = true;
        public int initialHazardPoolSize = 36;
        [Range(0f, 1f)]
        public float hazardChanceAtStart = 0.04f;
        [Range(0f, 1f)]
        public float hazardChanceAtHighDifficulty = 0.34f;
        public float hazardWidth = 2.6f;
        public float hazardDepth = 2.4f;
        [Range(0.02f, 0.95f)]
        public float pitLossFractionAtStart = 0.08f;
        [Range(0.02f, 0.95f)]
        public float pitLossFractionAtHighDifficulty = 0.24f;
        public int pitFlatLossAtStart = 6;
        public int pitFlatLossAtHighDifficulty = 26;
        public float knockbackHazardStrength = 1.9f;

        [Header("Modifier Milestones")]
        public int modifierUnlockEveryLevels = 20;

        public int initialGatePoolSize = 120;

        [Header("Track Decor")]
        public bool enableTrackDecor = true;
        public int stripePoolSize = 180;
        public float stripeLength = 1.8f;
        public float stripeGap = 1.45f;
        public float stripeWidth = 0.16f;
        public float sideStripeInsetFactor = 0.58f;
        public float railInset = 0.85f;
        public float railWidth = 0.24f;
        public float railHeight = 0.52f;
        public Color stripeColor = new Color(0.95f, 0.84f, 0.32f, 1f);
        public Color railColor = new Color(0.09f, 0.13f, 0.2f, 1f);

        [Header("Side Beacons")]
        public bool enableSideBeacons = true;
        public int beaconPoolSize = 96;
        public float beaconSpacing = 8f;
        public float beaconOffsetFromRail = 1.05f;
        public float beaconMinHeight = 1.9f;
        public float beaconMaxHeight = 3.6f;
        public Color beaconPoleColor = new Color(0.08f, 0.12f, 0.18f, 1f);
        public Color beaconCoreColor = new Color(0.36f, 0.88f, 1f, 1f);

        [Header("Street Ambience")]
        public bool enableStreetAmbience = true;
        public int sidewalkTilePoolSize = 120;
        public float sidewalkTileLength = 5.2f;
        public float sidewalkWidth = 2.2f;
        public float sidewalkHeight = 0.08f;
        public float sidewalkOffsetFromRail = 1.15f;
        public int pedestrianPoolSize = 56;
        public float pedestrianWalkSpeedMin = 0.45f;
        public float pedestrianWalkSpeedMax = 0.9f;
        public Color sidewalkColor = new Color(0.29f, 0.33f, 0.4f, 1f);
        public Color curbColor = new Color(0.68f, 0.74f, 0.82f, 1f);

        [Header("Ambient Pulse")]
        public bool enableDecorPulse = true;
        public float decorPulseSpeed = 1.7f;
        [Range(0f, 0.4f)]
        public float decorPulseStrength = 0.14f;

        [Header("Backdrop")]
        public BackdropQuality backdropQuality = BackdropQuality.Auto;
        public bool enableBackdrop = true;
        public int backdropPoolSize = 140;
        public float backdropSpacing = 5.2f;
        public float backdropDistance = 10f;
        public float backdropMinHeight = 2.3f;
        public float backdropMaxHeight = 9.5f;
        public float backdropMinWidth = 1.4f;
        public float backdropMaxWidth = 4.8f;
        public float backdropMinDepth = 2.6f;
        public float backdropMaxDepth = 6.4f;
        public Color backdropColor = new Color(0.23f, 0.3f, 0.4f, 1f);
        public bool enableClouds = true;
        public int cloudPoolSize = 28;
        public float cloudDriftSpeedMin = 0.32f;
        public float cloudDriftSpeedMax = 0.7f;
        public float cloudMinHeight = 8f;
        public float cloudMaxHeight = 15f;
        public float cloudTrackExclusionPadding = 3.2f;
        public Color cloudColor = new Color(0.93f, 0.97f, 1f, 0.9f);

        private readonly List<Gate> _activeGates = new List<Gate>(128);
        private readonly Stack<Gate> _gatePool = new Stack<Gate>(128);
        private readonly List<Transform> _activeStripes = new List<Transform>(128);
        private readonly Stack<Transform> _stripePool = new Stack<Transform>(128);
        private readonly List<Transform> _activeBackdropBlocks = new List<Transform>(256);
        private readonly Stack<Transform> _backdropPool = new Stack<Transform>(256);
        private readonly List<Transform> _activeClouds = new List<Transform>(64);
        private readonly Stack<Transform> _cloudPool = new Stack<Transform>(64);
        private readonly List<Transform> _activeBeacons = new List<Transform>(160);
        private readonly Stack<Transform> _beaconPool = new Stack<Transform>(160);
        private readonly List<Transform> _activeSidewalkTiles = new List<Transform>(256);
        private readonly Stack<Transform> _sidewalkTilePool = new Stack<Transform>(256);
        private readonly List<Transform> _activePedestrians = new List<Transform>(96);
        private readonly Stack<Transform> _pedestrianPool = new Stack<Transform>(96);
        private readonly List<HazardZone> _activeHazards = new List<HazardZone>(64);
        private readonly Stack<HazardZone> _hazardPool = new Stack<HazardZone>(64);
        private readonly List<float> _cloudSpeeds = new List<float>(64);
        private readonly List<float> _cloudMinX = new List<float>(64);
        private readonly List<float> _cloudMaxX = new List<float>(64);
        private readonly List<float> _cloudBaseY = new List<float>(64);
        private readonly List<float> _cloudPhases = new List<float>(64);
        private readonly List<float> _pedestrianSpeed = new List<float>(96);
        private readonly List<float> _pedestrianMinZ = new List<float>(96);
        private readonly List<float> _pedestrianMaxZ = new List<float>(96);
        private readonly List<float> _pedestrianPhase = new List<float>(96);
        private readonly List<float> _pedestrianYBase = new List<float>(96);
        private readonly List<Transform> _pedestrianLeftArm = new List<Transform>(96);
        private readonly List<Transform> _pedestrianRightArm = new List<Transform>(96);
        private readonly List<Transform> _pedestrianLeftLeg = new List<Transform>(96);
        private readonly List<Transform> _pedestrianRightLeg = new List<Transform>(96);
        private readonly float[] _lanePressure = { 0.34f, 0.32f, 0.34f };
        private const int CountClamp = 2000000000;
        private const int CloudLobeCount = 14;

        private FinishLine _activeFinish;
        private float _effectiveLaneSpacing;
        private float _effectiveTrackHalfWidth;
        private int _activeLevelIndex = 1;
        private bool _gatePoolPrewarmed;
        private bool _hazardPoolPrewarmed;
        private bool _stripePoolPrewarmed;
        private bool _backdropPoolPrewarmed;
        private bool _cloudPoolPrewarmed;
        private bool _beaconPoolPrewarmed;
        private bool _sidewalkPoolPrewarmed;
        private bool _pedestrianPoolPrewarmed;
        private Transform _trackDecorRoot;
        private Transform _backdropRoot;
        private Transform _cloudRoot;
        private Transform _beaconRoot;
        private Transform _streetRoot;
        private Transform _sidewalkRoot;
        private Transform _pedestrianRoot;
        private Transform _hazardRoot;
        private Transform _leftRail;
        private Transform _rightRail;
        private Material _stripeMaterial;
        private Material _railMaterial;
        private Material _backdropMaterial;
        private Material _cloudMaterial;
        private Material _beaconPoleMaterial;
        private Material _beaconCoreMaterial;
        private Material _sidewalkMaterial;
        private Material _curbMaterial;
        private Material _pedestrianClothMaterial;
        private Material _pedestrianAccentMaterial;
        private Material _pedestrianSkinMaterial;
        private Material _hazardMaterial;
        private Color _trackColor = new Color(0.18f, 0.22f, 0.29f, 1f);
        private Color _gatePositiveColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        private Color _gateNegativeColor = new Color(0.9f, 0.25f, 0.25f, 1f);
        private Color _hazardSlowColor = new Color(0.98f, 0.74f, 0.14f, 1f);
        private Color _hazardKnockbackColor = new Color(0.95f, 0.36f, 0.24f, 1f);
        private float _lastTrackLength;
        private float _lastTrackHalfWidth;

        public LevelBuildResult Generate(int levelIndex)
        {
            return Generate(levelIndex, ProgressionStore.GetDifficultyMode(DifficultyMode.Normal), -1);
        }

        public LevelBuildResult Generate(int levelIndex, DifficultyMode difficultyMode)
        {
            return Generate(levelIndex, difficultyMode, -1);
        }

        public LevelBuildResult Generate(int levelIndex, DifficultyMode difficultyMode, int forcedStartCount)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            _activeLevelIndex = safeLevel;
            _effectiveLaneSpacing = Mathf.Max(laneSpacing, minLaneSpacing);
            _effectiveTrackHalfWidth = Mathf.Max(trackHalfWidth, _effectiveLaneSpacing + laneToEdgePadding);
            ApplyGraphicsQualitySettings(backdropQuality);
            DecayLanePressureTowardNeutral();
            EnsureRoots();
            PrewarmGatePool();
            PrewarmHazardPool();
            var generated = BuildDefinition(safeLevel, difficultyMode, forcedStartCount);
            UpdateDynamicPalette(safeLevel, generated.isMiniBoss);

            ClearGeneratedObjects();
            BuildTrackVisual(generated.finishZ, _effectiveTrackHalfWidth);
            SpawnGates(generated.rows, generated.gateDifficulty01);
            SpawnHazards(generated.hazards);
            SpawnFinish(generated.finishZ, generated.enemyCount, generated.tankRequirement, generated.isMiniBoss);

            return new LevelBuildResult
            {
                levelIndex = safeLevel,
                startCount = generated.startCount,
                enemyCount = generated.enemyCount,
                tankRequirement = generated.tankRequirement,
                referenceRouteCount = generated.referenceRouteCount,
                referenceBetterRows = generated.referenceBetterRows,
                referenceWorseRows = generated.referenceWorseRows,
                referenceRedRows = generated.referenceRedRows,
                totalRows = generated.totalRows,
                finishZ = generated.finishZ,
                trackHalfWidth = _effectiveTrackHalfWidth,
                forwardSpeed = generated.forwardSpeed,
                isMiniBoss = generated.isMiniBoss,
                modifierName = generated.modifierName
            };
        }

        public void ReportLaneUsage(float leftSeconds, float centerSeconds, float rightSeconds)
        {
            if (!enableAdaptiveLanePressure)
            {
                return;
            }

            var total = leftSeconds + centerSeconds + rightSeconds;
            if (total <= 0.01f)
            {
                return;
            }

            var inverse = 1f / total;
            var targetLeft = Mathf.Clamp01(leftSeconds * inverse);
            var targetCenter = Mathf.Clamp01(centerSeconds * inverse);
            var targetRight = Mathf.Clamp01(rightSeconds * inverse);
            var blend = Mathf.Clamp01(lanePressureLerp);

            _lanePressure[0] = Mathf.Lerp(_lanePressure[0], targetLeft, blend);
            _lanePressure[1] = Mathf.Lerp(_lanePressure[1], targetCenter, blend);
            _lanePressure[2] = Mathf.Lerp(_lanePressure[2], targetRight, blend);
            NormalizeLanePressure();
        }

        public void ApplyGraphicsFidelity(BackdropQuality quality, bool refreshDecor = true)
        {
            backdropQuality = quality;
            ApplyGraphicsQualitySettings(backdropQuality);

            if (!refreshDecor || _lastTrackLength <= 0.1f || _lastTrackHalfWidth <= 0.1f)
            {
                return;
            }

            EnsureRoots();
            BuildTrackDecor(_lastTrackLength, _lastTrackHalfWidth);
        }

        public BackdropQuality GetGraphicsFidelity()
        {
            return backdropQuality;
        }

        public FinishLine GetActiveFinishLine()
        {
            return _activeFinish;
        }

        private void Update()
        {
            AnimateClouds(Time.deltaTime);
            AnimatePedestrians(Time.deltaTime);
            AnimateDecorPulse(Time.time);
        }

        private void EnsureRoots()
        {
            if (levelRoot == null)
            {
                var root = new GameObject("LevelRoot").transform;
                root.SetParent(transform, false);
                levelRoot = root;
            }

            if (gateRoot == null)
            {
                var root = new GameObject("GateRoot").transform;
                root.SetParent(levelRoot, false);
                gateRoot = root;
            }

            if (_trackDecorRoot == null)
            {
                _trackDecorRoot = levelRoot.Find("TrackDecor");
                if (_trackDecorRoot == null)
                {
                    _trackDecorRoot = new GameObject("TrackDecor").transform;
                    _trackDecorRoot.SetParent(levelRoot, false);
                }
            }

            if (_backdropRoot == null)
            {
                _backdropRoot = _trackDecorRoot.Find("Backdrop");
                if (_backdropRoot == null)
                {
                    _backdropRoot = new GameObject("Backdrop").transform;
                    _backdropRoot.SetParent(_trackDecorRoot, false);
                }
            }

            if (_cloudRoot == null)
            {
                _cloudRoot = _trackDecorRoot.Find("Clouds");
                if (_cloudRoot == null)
                {
                    _cloudRoot = new GameObject("Clouds").transform;
                    _cloudRoot.SetParent(_trackDecorRoot, false);
                }
            }

            if (_beaconRoot == null)
            {
                _beaconRoot = _trackDecorRoot.Find("Beacons");
                if (_beaconRoot == null)
                {
                    _beaconRoot = new GameObject("Beacons").transform;
                    _beaconRoot.SetParent(_trackDecorRoot, false);
                }
            }

            if (_streetRoot == null)
            {
                _streetRoot = _trackDecorRoot.Find("Street");
                if (_streetRoot == null)
                {
                    _streetRoot = new GameObject("Street").transform;
                    _streetRoot.SetParent(_trackDecorRoot, false);
                }
            }

            if (_sidewalkRoot == null)
            {
                _sidewalkRoot = _streetRoot.Find("Sidewalks");
                if (_sidewalkRoot == null)
                {
                    _sidewalkRoot = new GameObject("Sidewalks").transform;
                    _sidewalkRoot.SetParent(_streetRoot, false);
                }
            }

            if (_pedestrianRoot == null)
            {
                _pedestrianRoot = _streetRoot.Find("Pedestrians");
                if (_pedestrianRoot == null)
                {
                    _pedestrianRoot = new GameObject("Pedestrians").transform;
                    _pedestrianRoot.SetParent(_streetRoot, false);
                }
            }

            if (_hazardRoot == null)
            {
                _hazardRoot = levelRoot.Find("HazardRoot");
                if (_hazardRoot == null)
                {
                    _hazardRoot = new GameObject("HazardRoot").transform;
                    _hazardRoot.SetParent(levelRoot, false);
                }
            }
        }

        private void BuildTrackVisual(float finishZ, float effectiveTrackHalfWidth)
        {
            if (trackVisual == null)
            {
                return;
            }

            var length = finishZ + 24f;
            _lastTrackLength = length;
            _lastTrackHalfWidth = effectiveTrackHalfWidth;
            trackVisual.position = new Vector3(0f, -0.55f, length * 0.5f);
            trackVisual.localScale = new Vector3(effectiveTrackHalfWidth * 2.5f, 1f, length);
            ApplyTrackColor();
            BuildTrackDecor(length, effectiveTrackHalfWidth);
        }

        private void ApplyTrackColor()
        {
            if (trackVisual == null)
            {
                return;
            }

            var renderer = trackVisual.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            var material = renderer.sharedMaterial;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", _trackColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", _trackColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.11f);
            }
        }

        private void BuildTrackDecor(float trackLength, float effectiveTrackHalfWidth)
        {
            if (_trackDecorRoot == null)
            {
                return;
            }

            ClearTrackDecor();
            ClearBackdrop();
            ClearClouds();
            ClearBeacons();
            ClearStreetAmbience();

            if (!enableTrackDecor)
            {
                SetRailsActive(false);
                return;
            }

            PrewarmStripePool();
            EnsureRails();

            if (_leftRail != null)
            {
                _leftRail.position = new Vector3(-(effectiveTrackHalfWidth + railInset), -0.05f + (railHeight * 0.5f), trackLength * 0.5f);
                _leftRail.localScale = new Vector3(railWidth, railHeight, trackLength + 4f);
            }

            if (_rightRail != null)
            {
                _rightRail.position = new Vector3(effectiveTrackHalfWidth + railInset, -0.05f + (railHeight * 0.5f), trackLength * 0.5f);
                _rightRail.localScale = new Vector3(railWidth, railHeight, trackLength + 4f);
            }

            SetRailsActive(true);

            var start = Mathf.Max(6f, startZ * 0.35f);
            var end = Mathf.Max(start + 2f, trackLength - 12f);
            var step = Mathf.Max(0.45f, stripeLength + stripeGap);
            var stripeY = -0.045f;
            var safeStripeLength = Mathf.Max(0.4f, stripeLength);
            var safeStripeWidth = Mathf.Max(0.04f, stripeWidth);
            var sideStripeX = Mathf.Clamp(_effectiveLaneSpacing * sideStripeInsetFactor, 0.75f, _effectiveTrackHalfWidth - 0.55f);
            var row = 0;

            for (var z = start; z < end; z += step)
            {
                var stripe = GetStripe();
                stripe.position = new Vector3(0f, stripeY, z);
                stripe.rotation = Quaternion.identity;
                stripe.localScale = new Vector3(safeStripeWidth, 0.012f, safeStripeLength);
                stripe.gameObject.SetActive(true);
                _activeStripes.Add(stripe);

                if (row % 2 == 0)
                {
                    var sideLength = safeStripeLength * 0.72f;
                    var sideWidth = safeStripeWidth * 0.62f;
                    var leftStripe = GetStripe();
                    leftStripe.position = new Vector3(-sideStripeX, stripeY, z + (step * 0.18f));
                    leftStripe.rotation = Quaternion.identity;
                    leftStripe.localScale = new Vector3(sideWidth, 0.011f, sideLength);
                    leftStripe.gameObject.SetActive(true);
                    _activeStripes.Add(leftStripe);

                    var rightStripe = GetStripe();
                    rightStripe.position = new Vector3(sideStripeX, stripeY, z + (step * 0.18f));
                    rightStripe.rotation = Quaternion.identity;
                    rightStripe.localScale = new Vector3(sideWidth, 0.011f, sideLength);
                    rightStripe.gameObject.SetActive(true);
                    _activeStripes.Add(rightStripe);
                }

                row++;
            }

            BuildBackdrop(trackLength, effectiveTrackHalfWidth);
            BuildSideBeacons(trackLength, effectiveTrackHalfWidth);
            BuildStreetAmbience(trackLength, effectiveTrackHalfWidth);
        }

        private void BuildBackdrop(float trackLength, float effectiveTrackHalfWidth)
        {
            if (_backdropRoot == null || _cloudRoot == null)
            {
                return;
            }

            var quality = backdropQuality == BackdropQuality.Auto ? ResolveAutoQuality() : backdropQuality;
            var density = GetBackdropDensityMultiplier();
            var density01 = Mathf.Clamp01((density - 0.5f) / 0.75f);
            var random = new System.Random(12289 + (_activeLevelIndex * 193));
            var zStart = -30f;
            var zEnd = trackLength + 42f;
            var sideX = effectiveTrackHalfWidth + Mathf.Max(6f, backdropDistance);

            if (enableBackdrop)
            {
                PrewarmBackdropPool();
                var spacing = Mathf.Max(2.5f, backdropSpacing / Mathf.Max(0.4f, density));
                var minHeight = Mathf.Max(1f, backdropMinHeight);
                var maxHeight = Mathf.Max(minHeight + 0.5f, backdropMaxHeight);
                var minWidth = Mathf.Max(0.5f, backdropMinWidth);
                var maxWidth = Mathf.Max(minWidth + 0.25f, backdropMaxWidth);
                var minDepth = Mathf.Max(0.8f, backdropMinDepth);
                var maxDepth = Mathf.Max(minDepth + 0.3f, backdropMaxDepth);
                var layerCount = quality == BackdropQuality.Low ? 1 : 2;
                for (var layer = 0; layer < layerCount; layer++)
                {
                    var layerScale = 1f + (layer * 0.38f);
                    var layerSpacing = spacing * (1f + (layer * 0.52f));
                    var layerDepthOffset = layer * Mathf.Lerp(7f, 11f, density01);
                    var layerYawJitter = layer == 0 ? 4f : 8f;
                    var layerHeightBoost = Mathf.Lerp(0.96f, 1.2f, layer / Mathf.Max(1f, layerCount - 1f));
                    for (var side = -1; side <= 1; side += 2)
                    {
                        for (var z = zStart; z <= zEnd; z += layerSpacing)
                        {
                            var block = GetBackdropBlock();
                            var width = Mathf.Lerp(minWidth, maxWidth, (float)random.NextDouble()) * layerScale;
                            var height = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble())
                                * Mathf.Lerp(0.92f, 1.14f, density01)
                                * layerHeightBoost;
                            var depth = Mathf.Lerp(minDepth, maxDepth, (float)random.NextDouble()) * layerScale;
                            var zJitter = ((float)random.NextDouble() * 2f - 1f) * layerSpacing * 0.36f;
                            var xJitter = ((float)random.NextDouble() * 2f - 1f) * (1.25f + layer * 0.5f);
                            block.position = new Vector3(
                                side * (sideX + layerDepthOffset + xJitter + width * 0.4f),
                                (height * 0.5f) - 0.2f,
                                z + zJitter);
                            block.rotation = Quaternion.Euler(0f, ((float)random.NextDouble() * 2f - 1f) * layerYawJitter, 0f);
                            block.localScale = Vector3.one;
                            ConfigureBackdropGeometry(block, width, height, depth, random);
                            block.gameObject.SetActive(true);
                            _activeBackdropBlocks.Add(block);
                        }
                    }
                }
            }

            if (!enableClouds)
            {
                return;
            }

            PrewarmCloudPool();
            var cloudCount = Mathf.RoundToInt(Mathf.Lerp(10f, 26f, density01));
            cloudCount = Mathf.Clamp(cloudCount, 8, 30);
            var cloudMinX = -(sideX + 18f);
            var cloudMaxX = sideX + 18f;
            var cloudBandMinX = Mathf.Max(Mathf.Abs(cloudMinX), _effectiveTrackHalfWidth + Mathf.Max(1.5f, cloudTrackExclusionPadding));
            var cloudBandMaxX = Mathf.Max(cloudBandMinX + 0.5f, cloudMaxX);
            var minY = Mathf.Min(cloudMinHeight, cloudMaxHeight);
            var maxY = Mathf.Max(minY + 0.5f, cloudMaxHeight);

            for (var i = 0; i < cloudCount; i++)
            {
                var cloud = GetCloud();
                var side = random.NextDouble() < 0.5 ? -1f : 1f;
                var xMagnitude = Mathf.Lerp(cloudBandMinX, cloudBandMaxX, (float)random.NextDouble());
                var x = xMagnitude * side;
                var y = Mathf.Lerp(minY, maxY, (float)random.NextDouble());
                var z = Mathf.Lerp(zStart, zEnd, (float)random.NextDouble());
                var baseScale = Mathf.Lerp(2.8f, 6.6f, (float)random.NextDouble());
                cloud.position = new Vector3(x, y, z);
                cloud.rotation = Quaternion.Euler(0f, ((float)random.NextDouble() * 2f - 1f) * 25f, 0f);
                cloud.localScale = Vector3.one;
                ConfigureCloudGeometry(cloud, baseScale, random);
                cloud.gameObject.SetActive(true);
                _activeClouds.Add(cloud);

                var direction = random.NextDouble() < 0.5 ? -1f : 1f;
                var speed = Mathf.Lerp(cloudDriftSpeedMin, cloudDriftSpeedMax, (float)random.NextDouble()) * direction;
                _cloudSpeeds.Add(speed);
                _cloudMinX.Add(cloudMinX);
                _cloudMaxX.Add(cloudMaxX);
                _cloudBaseY.Add(y);
                _cloudPhases.Add((float)random.NextDouble() * Mathf.PI * 2f);
            }
        }

        private void BuildSideBeacons(float trackLength, float effectiveTrackHalfWidth)
        {
            if (!enableSideBeacons || _beaconRoot == null)
            {
                return;
            }

            PrewarmBeaconPool();
            var random = new System.Random(27109 + (_activeLevelIndex * 83));
            var density = GetBackdropDensityMultiplier();
            var spacing = Mathf.Max(4.2f, beaconSpacing / Mathf.Max(0.6f, density));
            var start = Mathf.Max(6f, startZ * 0.35f);
            var end = Mathf.Max(start + 2f, trackLength - 8f);
            var baseX = effectiveTrackHalfWidth + railInset + Mathf.Max(0.2f, beaconOffsetFromRail);
            var minHeight = Mathf.Max(1f, beaconMinHeight);
            var maxHeight = Mathf.Max(minHeight + 0.3f, beaconMaxHeight);

            for (var side = -1; side <= 1; side += 2)
            {
                for (var z = start; z <= end; z += spacing)
                {
                    var beacon = GetBeacon();
                    var jitter = ((float)random.NextDouble() * 2f - 1f) * spacing * 0.25f;
                    var x = side * (baseX + ((float)random.NextDouble() - 0.5f) * 0.45f);
                    var height = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble());
                    var coreScale = Mathf.Lerp(0.28f, 0.44f, (float)random.NextDouble());
                    beacon.position = new Vector3(x, -0.08f, z + jitter);
                    beacon.rotation = Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f);
                    beacon.localScale = Vector3.one;
                    ConfigureBeaconGeometry(beacon, height, coreScale);
                    beacon.gameObject.SetActive(true);
                    _activeBeacons.Add(beacon);
                }
            }
        }

        private void BuildStreetAmbience(float trackLength, float effectiveTrackHalfWidth)
        {
            if (!enableStreetAmbience || _sidewalkRoot == null || _pedestrianRoot == null)
            {
                return;
            }

            var quality = backdropQuality == BackdropQuality.Auto ? ResolveAutoQuality() : backdropQuality;
            var density = GetBackdropDensityMultiplier();
            var random = new System.Random(41299 + (_activeLevelIndex * 71));
            PrewarmSidewalkPool();
            PrewarmPedestrianPool();

            var tileLength = Mathf.Max(3.4f, sidewalkTileLength / Mathf.Max(0.55f, density));
            var tileWidth = Mathf.Max(1.2f, sidewalkWidth);
            var tileHeight = Mathf.Clamp(sidewalkHeight, 0.04f, 0.18f);
            var sidewalkCenterX = effectiveTrackHalfWidth + railInset + Mathf.Max(0.35f, sidewalkOffsetFromRail) + (tileWidth * 0.5f);
            var sidewalkY = -0.06f + (tileHeight * 0.5f);
            var zStart = -8f;
            var zEnd = trackLength + 14f;

            for (var side = -1; side <= 1; side += 2)
            {
                for (var z = zStart; z <= zEnd; z += tileLength * 0.96f)
                {
                    var tile = GetSidewalkTile();
                    var zJitter = ((float)random.NextDouble() * 2f - 1f) * tileLength * 0.1f;
                    tile.position = new Vector3(side * sidewalkCenterX, sidewalkY, z + zJitter);
                    tile.rotation = Quaternion.identity;
                    tile.localScale = Vector3.one;
                    ConfigureSidewalkTileGeometry(tile, tileWidth, tileHeight, tileLength);
                    tile.gameObject.SetActive(true);
                    _activeSidewalkTiles.Add(tile);
                }
            }

            var minZ = Mathf.Max(10f, startZ * 0.44f);
            var maxZ = Mathf.Max(minZ + 6f, trackLength - 10f);
            var pedestrianCount = quality == BackdropQuality.High
                ? Mathf.RoundToInt(Mathf.Lerp(18f, 32f, Mathf.Clamp01((density - 0.5f) / 0.75f)))
                : quality == BackdropQuality.Medium
                    ? Mathf.RoundToInt(Mathf.Lerp(10f, 20f, Mathf.Clamp01((density - 0.5f) / 0.75f)))
                    : Mathf.RoundToInt(Mathf.Lerp(6f, 12f, Mathf.Clamp01((density - 0.5f) / 0.75f)));
            pedestrianCount = Mathf.Clamp(pedestrianCount, 6, 36);

            for (var i = 0; i < pedestrianCount; i++)
            {
                var pedestrian = GetPedestrian();
                var side = random.NextDouble() < 0.5 ? -1f : 1f;
                var sideOffset = tileWidth * (0.18f + ((float)random.NextDouble() * 0.52f));
                var x = side * (sidewalkCenterX + sideOffset - (tileWidth * 0.5f));
                var y = -0.03f;
                var z = Mathf.Lerp(minZ, maxZ, (float)random.NextDouble());
                var direction = random.NextDouble() < 0.5 ? -1f : 1f;
                var speed = Mathf.Lerp(pedestrianWalkSpeedMin, pedestrianWalkSpeedMax, (float)random.NextDouble()) * direction;
                pedestrian.position = new Vector3(x, y, z);
                pedestrian.rotation = Quaternion.Euler(0f, direction > 0f ? 0f : 180f, 0f);
                pedestrian.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.08f, (float)random.NextDouble());
                ConfigurePedestrianGeometry(pedestrian, random, quality);
                pedestrian.gameObject.SetActive(true);
                _activePedestrians.Add(pedestrian);
                _pedestrianSpeed.Add(speed);
                _pedestrianMinZ.Add(minZ);
                _pedestrianMaxZ.Add(maxZ);
                _pedestrianPhase.Add((float)random.NextDouble() * Mathf.PI * 2f);
                _pedestrianYBase.Add(y);
                _pedestrianLeftArm.Add(pedestrian.Find("LeftArm"));
                _pedestrianRightArm.Add(pedestrian.Find("RightArm"));
                _pedestrianLeftLeg.Add(pedestrian.Find("LeftLeg"));
                _pedestrianRightLeg.Add(pedestrian.Find("RightLeg"));
            }
        }

        private void AnimatePedestrians(float deltaTime)
        {
            if (deltaTime <= 0f || _activePedestrians.Count == 0)
            {
                return;
            }

            var runTime = Time.time;
            for (var i = 0; i < _activePedestrians.Count; i++)
            {
                var pedestrian = _activePedestrians[i];
                if (pedestrian == null)
                {
                    continue;
                }

                var speed = _pedestrianSpeed[i];
                var minZ = _pedestrianMinZ[i];
                var maxZ = _pedestrianMaxZ[i];
                var position = pedestrian.position;
                position.z += speed * deltaTime;
                if (position.z > maxZ)
                {
                    position.z = minZ;
                }
                else if (position.z < minZ)
                {
                    position.z = maxZ;
                }

                var phase = runTime * (Mathf.Abs(speed) * 5.6f + 1.1f) + _pedestrianPhase[i];
                var stride = Mathf.Sin(phase);
                var bob = Mathf.Sin(phase * 2f) * 0.006f;
                position.y = _pedestrianYBase[i] + bob;
                pedestrian.position = position;

                var leftLeg = _pedestrianLeftLeg[i];
                if (leftLeg != null)
                {
                    leftLeg.localRotation = Quaternion.Euler(stride * 24f, 0f, 0f);
                }

                var rightLeg = _pedestrianRightLeg[i];
                if (rightLeg != null)
                {
                    rightLeg.localRotation = Quaternion.Euler(-stride * 24f, 0f, 0f);
                }

                var leftArm = _pedestrianLeftArm[i];
                if (leftArm != null)
                {
                    leftArm.localRotation = Quaternion.Euler(-stride * 22f, 0f, 0f);
                }

                var rightArm = _pedestrianRightArm[i];
                if (rightArm != null)
                {
                    rightArm.localRotation = Quaternion.Euler(stride * 22f, 0f, 0f);
                }
            }
        }

        private void AnimateClouds(float deltaTime)
        {
            if (deltaTime <= 0f || _activeClouds.Count == 0)
            {
                return;
            }

            var waveTime = Time.time * 0.6f;
            for (var i = 0; i < _activeClouds.Count; i++)
            {
                var cloud = _activeClouds[i];
                if (cloud == null)
                {
                    continue;
                }

                var position = cloud.position;
                position.x += _cloudSpeeds[i] * deltaTime;

                var minX = _cloudMinX[i];
                var maxX = _cloudMaxX[i];
                if (position.x > maxX)
                {
                    position.x = minX;
                }
                else if (position.x < minX)
                {
                    position.x = maxX;
                }

                position.y = _cloudBaseY[i] + Mathf.Sin(waveTime + _cloudPhases[i]) * 0.18f;
                cloud.position = position;
            }
        }

        private void AnimateDecorPulse(float runTime)
        {
            if (!enableDecorPulse)
            {
                return;
            }

            var pulseStrength = Mathf.Clamp(decorPulseStrength, 0f, 0.4f);
            var speed = Mathf.Max(0.1f, decorPulseSpeed);
            var oscillation = 0.5f + (Mathf.Sin(runTime * speed) * 0.5f);
            var pulse = 1f + (oscillation * pulseStrength);
            ApplyMaterialColor(_stripeMaterial, stripeColor * pulse, 0.42f, 0.42f * pulse);
            ApplyMaterialColor(_railMaterial, railColor * (0.98f + pulseStrength * 0.2f), 0.24f, 0.1f * pulse);
            ApplyMaterialColor(_beaconCoreMaterial, beaconCoreColor * pulse, 0.66f, 0.95f * pulse);
        }

        private float GetBackdropDensityMultiplier()
        {
            switch (backdropQuality)
            {
                case BackdropQuality.Low:
                    return 0.55f;
                case BackdropQuality.Medium:
                    return 0.85f;
                case BackdropQuality.High:
                    return 1.2f;
                case BackdropQuality.Auto:
                default:
                {
                    var systemMemory = Mathf.Max(1, SystemInfo.systemMemorySize);
                    var graphicsMemory = SystemInfo.graphicsMemorySize > 0 ? SystemInfo.graphicsMemorySize : (systemMemory / 2);
                    var cores = Mathf.Max(1, SystemInfo.processorCount);

                    if (graphicsMemory < 1400 || systemMemory < 3000 || cores <= 4)
                    {
                        return 0.55f;
                    }

                    if (graphicsMemory < 2600 || systemMemory < 5000 || cores <= 6)
                    {
                        return 0.85f;
                    }

                    return 1.2f;
                }
            }
        }

        private static void ApplyGraphicsQualitySettings(BackdropQuality quality)
        {
            var resolved = quality;
            if (resolved == BackdropQuality.Auto)
            {
                resolved = ResolveAutoQuality();
            }

            switch (resolved)
            {
                case BackdropQuality.Low:
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.lodBias = 0.65f;
                    break;
                case BackdropQuality.Medium:
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.lodBias = 0.9f;
                    break;
                case BackdropQuality.High:
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.lodBias = 1.15f;
                    break;
            }
        }

        private static BackdropQuality ResolveAutoQuality()
        {
            var systemMemory = Mathf.Max(1, SystemInfo.systemMemorySize);
            var graphicsMemory = SystemInfo.graphicsMemorySize > 0 ? SystemInfo.graphicsMemorySize : (systemMemory / 2);
            var cores = Mathf.Max(1, SystemInfo.processorCount);

            if (graphicsMemory < 1400 || systemMemory < 3000 || cores <= 4)
            {
                return BackdropQuality.Low;
            }

            if (graphicsMemory < 2600 || systemMemory < 5000 || cores <= 6)
            {
                return BackdropQuality.Medium;
            }

            return BackdropQuality.High;
        }

        private void EnsureRails()
        {
            if (_leftRail == null)
            {
                _leftRail = CreateRail("LeftRail");
            }

            if (_rightRail == null)
            {
                _rightRail = CreateRail("RightRail");
            }
        }

        private void SetRailsActive(bool isActive)
        {
            if (_leftRail != null)
            {
                _leftRail.gameObject.SetActive(isActive);
            }

            if (_rightRail != null)
            {
                _rightRail.gameObject.SetActive(isActive);
            }
        }

        private Transform CreateRail(string name)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(_trackDecorRoot, false);
            var collider = rail.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = rail.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetRailMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return rail.transform;
        }

        private Transform GetStripe()
        {
            if (_stripePool.Count > 0)
            {
                return _stripePool.Pop();
            }

            return CreateStripe();
        }

        private Transform CreateStripe()
        {
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "LaneStripe";
            stripe.transform.SetParent(_trackDecorRoot, false);
            var collider = stripe.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = stripe.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetStripeMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            stripe.SetActive(false);
            return stripe.transform;
        }

        private Transform GetBackdropBlock()
        {
            if (_backdropPool.Count > 0)
            {
                return _backdropPool.Pop();
            }

            return CreateBackdropBlock();
        }

        private Transform CreateBackdropBlock()
        {
            var blockRoot = new GameObject("BackdropBlock");
            blockRoot.transform.SetParent(_backdropRoot, false);

            CreateBackdropPart(blockRoot.transform, "Base", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "Mid", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "Top", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "Inset", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "WingLeft", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "WingRight", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "NeonBand", PrimitiveType.Cube, GetBeaconCoreMaterial());
            CreateBackdropPart(blockRoot.transform, "WindowBandA", PrimitiveType.Cube, GetBeaconCoreMaterial());
            CreateBackdropPart(blockRoot.transform, "WindowBandB", PrimitiveType.Cube, GetBeaconCoreMaterial());
            CreateBackdropPart(blockRoot.transform, "WindowBandC", PrimitiveType.Cube, GetBeaconCoreMaterial());
            CreateBackdropPart(blockRoot.transform, "RoofCap", PrimitiveType.Cylinder, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "RoofRim", PrimitiveType.Cube, GetBackdropMaterial());
            CreateBackdropPart(blockRoot.transform, "Antenna", PrimitiveType.Cube, GetBeaconPoleMaterial());
            CreateBackdropPart(blockRoot.transform, "ServiceBox", PrimitiveType.Cube, GetBackdropMaterial());

            blockRoot.SetActive(false);
            return blockRoot.transform;
        }

        private Transform GetCloud()
        {
            if (_cloudPool.Count > 0)
            {
                return _cloudPool.Pop();
            }

            return CreateCloud();
        }

        private Transform GetBeacon()
        {
            if (_beaconPool.Count > 0)
            {
                return _beaconPool.Pop();
            }

            return CreateBeacon();
        }

        private Transform GetSidewalkTile()
        {
            if (_sidewalkTilePool.Count > 0)
            {
                return _sidewalkTilePool.Pop();
            }

            return CreateSidewalkTile();
        }

        private Transform GetPedestrian()
        {
            if (_pedestrianPool.Count > 0)
            {
                return _pedestrianPool.Pop();
            }

            return CreatePedestrian();
        }

        private Transform CreateCloud()
        {
            var cloudRoot = new GameObject("BackdropCloud");
            cloudRoot.transform.SetParent(_cloudRoot, false);
            for (var i = 0; i < CloudLobeCount; i++)
            {
                var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lobe.name = "Lobe" + i;
                lobe.transform.SetParent(cloudRoot.transform, false);
                var collider = lobe.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var renderer = lobe.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = GetCloudMaterial();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            cloudRoot.SetActive(false);
            return cloudRoot.transform;
        }

        private Transform CreateBeacon()
        {
            var beaconRoot = new GameObject("SideBeacon");
            beaconRoot.transform.SetParent(_beaconRoot, false);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Pole";
            pole.transform.SetParent(beaconRoot.transform, false);
            var poleCollider = pole.GetComponent<Collider>();
            if (poleCollider != null)
            {
                Destroy(poleCollider);
            }

            var poleRenderer = pole.GetComponent<MeshRenderer>();
            if (poleRenderer != null)
            {
                poleRenderer.sharedMaterial = GetBeaconPoleMaterial();
                poleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                poleRenderer.receiveShadows = false;
            }

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(beaconRoot.transform, false);
            var coreCollider = core.GetComponent<Collider>();
            if (coreCollider != null)
            {
                Destroy(coreCollider);
            }

            var coreRenderer = core.GetComponent<MeshRenderer>();
            if (coreRenderer != null)
            {
                coreRenderer.sharedMaterial = GetBeaconCoreMaterial();
                coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                coreRenderer.receiveShadows = false;
            }

            beaconRoot.SetActive(false);
            return beaconRoot.transform;
        }

        private Transform CreateSidewalkTile()
        {
            var tileRoot = new GameObject("SidewalkTile");
            tileRoot.transform.SetParent(_sidewalkRoot, false);

            var slab = CreateBackdropPart(tileRoot.transform, "Slab", PrimitiveType.Cube, GetSidewalkMaterial());
            var curb = CreateBackdropPart(tileRoot.transform, "Curb", PrimitiveType.Cube, GetCurbMaterial());
            var seam = CreateBackdropPart(tileRoot.transform, "Seam", PrimitiveType.Cube, GetCurbMaterial());
            if (slab != null)
            {
                slab.localPosition = Vector3.zero;
            }

            if (curb != null)
            {
                curb.localPosition = Vector3.zero;
            }

            if (seam != null)
            {
                seam.localPosition = Vector3.zero;
            }

            tileRoot.SetActive(false);
            return tileRoot.transform;
        }

        private Transform CreatePedestrian()
        {
            var root = new GameObject("Pedestrian");
            root.transform.SetParent(_pedestrianRoot, false);

            CreateBackdropPart(root.transform, "Torso", PrimitiveType.Capsule, GetPedestrianClothMaterial());
            CreateBackdropPart(root.transform, "Head", PrimitiveType.Sphere, GetSkinMaterialForPedestrian());
            CreateBackdropPart(root.transform, "LeftArm", PrimitiveType.Cube, GetPedestrianClothMaterial());
            CreateBackdropPart(root.transform, "RightArm", PrimitiveType.Cube, GetPedestrianClothMaterial());
            CreateBackdropPart(root.transform, "LeftLeg", PrimitiveType.Cube, GetPedestrianClothMaterial());
            CreateBackdropPart(root.transform, "RightLeg", PrimitiveType.Cube, GetPedestrianClothMaterial());
            CreateBackdropPart(root.transform, "Bag", PrimitiveType.Cube, GetPedestrianAccentMaterial());

            root.SetActive(false);
            return root.transform;
        }

        private static void ConfigureBeaconGeometry(Transform beacon, float height, float coreScale)
        {
            if (beacon == null)
            {
                return;
            }

            var safeHeight = Mathf.Max(0.8f, height);
            var safeCoreScale = Mathf.Max(0.12f, coreScale);
            var pole = beacon.Find("Pole");
            if (pole != null)
            {
                pole.localPosition = new Vector3(0f, safeHeight * 0.5f, 0f);
                pole.localScale = new Vector3(0.13f, safeHeight, 0.13f);
            }

            var core = beacon.Find("Core");
            if (core != null)
            {
                core.localPosition = new Vector3(0f, safeHeight + 0.08f, 0f);
                core.localScale = Vector3.one * safeCoreScale;
            }
        }

        private static void ConfigureSidewalkTileGeometry(Transform tile, float width, float height, float depth)
        {
            if (tile == null)
            {
                return;
            }

            var slab = tile.Find("Slab");
            if (slab != null)
            {
                slab.localPosition = Vector3.zero;
                slab.localScale = new Vector3(width, Mathf.Max(0.04f, height), depth);
            }

            var curb = tile.Find("Curb");
            if (curb != null)
            {
                curb.localPosition = new Vector3(-(width * 0.47f), height * 0.18f, 0f);
                curb.localScale = new Vector3(Mathf.Max(0.08f, width * 0.08f), Mathf.Max(0.06f, height * 1.6f), depth * 0.96f);
            }

            var seam = tile.Find("Seam");
            if (seam != null)
            {
                seam.localPosition = new Vector3(width * 0.2f, height * 0.32f, 0f);
                seam.localScale = new Vector3(width * 0.54f, Mathf.Max(0.03f, height * 0.36f), Mathf.Max(0.04f, depth * 0.03f));
            }
        }

        private void ConfigurePedestrianGeometry(Transform pedestrian, System.Random random, BackdropQuality quality)
        {
            if (pedestrian == null)
            {
                return;
            }

            var scale = quality == BackdropQuality.Low ? 0.85f : 1f;
            var torso = pedestrian.Find("Torso");
            if (torso != null)
            {
                torso.localPosition = new Vector3(0f, 0.52f * scale, 0f);
                torso.localScale = new Vector3(0.2f * scale, 0.34f * scale, 0.14f * scale);
            }

            var head = pedestrian.Find("Head");
            if (head != null)
            {
                head.localPosition = new Vector3(0f, 0.88f * scale, 0f);
                head.localScale = Vector3.one * (0.15f * scale);
            }

            var leftArm = pedestrian.Find("LeftArm");
            if (leftArm != null)
            {
                leftArm.localPosition = new Vector3(-0.14f * scale, 0.56f * scale, 0f);
                leftArm.localScale = new Vector3(0.06f * scale, 0.24f * scale, 0.06f * scale);
            }

            var rightArm = pedestrian.Find("RightArm");
            if (rightArm != null)
            {
                rightArm.localPosition = new Vector3(0.14f * scale, 0.56f * scale, 0f);
                rightArm.localScale = new Vector3(0.06f * scale, 0.24f * scale, 0.06f * scale);
            }

            var leftLeg = pedestrian.Find("LeftLeg");
            if (leftLeg != null)
            {
                leftLeg.localPosition = new Vector3(-0.07f * scale, 0.25f * scale, 0f);
                leftLeg.localScale = new Vector3(0.08f * scale, 0.28f * scale, 0.08f * scale);
            }

            var rightLeg = pedestrian.Find("RightLeg");
            if (rightLeg != null)
            {
                rightLeg.localPosition = new Vector3(0.07f * scale, 0.25f * scale, 0f);
                rightLeg.localScale = new Vector3(0.08f * scale, 0.28f * scale, 0.08f * scale);
            }

            var bag = pedestrian.Find("Bag");
            if (bag != null)
            {
                bag.gameObject.SetActive(quality != BackdropQuality.Low && random.NextDouble() > 0.35);
                bag.localPosition = new Vector3(0.1f * scale, 0.56f * scale, -0.09f * scale);
                bag.localScale = new Vector3(0.1f * scale, 0.14f * scale, 0.07f * scale);
            }

        }

        private static Transform CreateBackdropPart(Transform parent, string name, PrimitiveType primitiveType, Material material)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);

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

        private void ConfigureBackdropGeometry(Transform root, float width, float height, float depth, System.Random random)
        {
            if (root == null)
            {
                return;
            }

            var quality = backdropQuality == BackdropQuality.Auto ? ResolveAutoQuality() : backdropQuality;
            var safeWidth = Mathf.Max(0.8f, width);
            var safeHeight = Mathf.Max(1.2f, height);
            var safeDepth = Mathf.Max(0.8f, depth);
            var wingWidth = safeWidth * Mathf.Lerp(0.28f, 0.4f, (float)random.NextDouble());
            var wingHeight = safeHeight * Mathf.Lerp(0.35f, 0.58f, (float)random.NextDouble());
            var wingDepth = safeDepth * Mathf.Lerp(0.28f, 0.45f, (float)random.NextDouble());

            var basePart = root.Find("Base");
            if (basePart != null)
            {
                basePart.gameObject.SetActive(true);
                basePart.localPosition = new Vector3(0f, 0f, 0f);
                basePart.localScale = new Vector3(safeWidth, safeHeight, safeDepth);
            }

            var useMedium = quality == BackdropQuality.Medium || quality == BackdropQuality.High;
            var useHigh = quality == BackdropQuality.High;
            var detailSeed = (float)random.NextDouble();

            var midPart = root.Find("Mid");
            if (midPart != null)
            {
                midPart.gameObject.SetActive(useMedium);
                if (useMedium)
                {
                    midPart.localPosition = new Vector3(0f, safeHeight * 0.14f, safeDepth * -0.04f);
                    midPart.localScale = new Vector3(safeWidth * 0.74f, safeHeight * 0.66f, safeDepth * 0.72f);
                }
            }

            var topPart = root.Find("Top");
            if (topPart != null)
            {
                topPart.gameObject.SetActive(useHigh);
                if (useHigh)
                {
                    topPart.localPosition = new Vector3(0f, safeHeight * 0.35f, 0f);
                    topPart.localScale = new Vector3(safeWidth * 0.48f, safeHeight * 0.34f, safeDepth * 0.5f);
                }
            }

            var inset = root.Find("Inset");
            if (inset != null)
            {
                inset.gameObject.SetActive(useMedium);
                if (useMedium)
                {
                    inset.localPosition = new Vector3(0f, safeHeight * 0.02f, safeDepth * 0.46f);
                    inset.localScale = new Vector3(safeWidth * 0.72f, safeHeight * 0.7f, Mathf.Max(0.05f, safeDepth * 0.05f));
                }
            }

            var wingLeft = root.Find("WingLeft");
            if (wingLeft != null)
            {
                wingLeft.gameObject.SetActive(useMedium);
                if (useMedium)
                {
                    wingLeft.localPosition = new Vector3(-(safeWidth * 0.52f), -(safeHeight * 0.1f), safeDepth * 0.06f);
                    wingLeft.localScale = new Vector3(wingWidth, wingHeight, wingDepth);
                }
            }

            var wingRight = root.Find("WingRight");
            if (wingRight != null)
            {
                wingRight.gameObject.SetActive(useMedium);
                if (useMedium)
                {
                    wingRight.localPosition = new Vector3(safeWidth * 0.52f, -(safeHeight * 0.08f), -safeDepth * 0.04f);
                    wingRight.localScale = new Vector3(wingWidth * 0.95f, wingHeight * 0.9f, wingDepth * 1.04f);
                }
            }

            var windowBandA = root.Find("WindowBandA");
            if (windowBandA != null)
            {
                windowBandA.gameObject.SetActive(useMedium);
                if (useMedium)
                {
                    windowBandA.localPosition = new Vector3(0f, -safeHeight * 0.14f, safeDepth * 0.47f);
                    windowBandA.localScale = new Vector3(safeWidth * 0.62f, Mathf.Max(0.06f, safeHeight * 0.035f), Mathf.Max(0.04f, safeDepth * 0.04f));
                }
            }

            var windowBandB = root.Find("WindowBandB");
            if (windowBandB != null)
            {
                windowBandB.gameObject.SetActive(useHigh || detailSeed > 0.45f);
                if (windowBandB.gameObject.activeSelf)
                {
                    windowBandB.localPosition = new Vector3(0f, safeHeight * 0.02f, safeDepth * 0.47f);
                    windowBandB.localScale = new Vector3(safeWidth * 0.58f, Mathf.Max(0.05f, safeHeight * 0.03f), Mathf.Max(0.04f, safeDepth * 0.04f));
                }
            }

            var windowBandC = root.Find("WindowBandC");
            if (windowBandC != null)
            {
                windowBandC.gameObject.SetActive(useHigh);
                if (useHigh)
                {
                    windowBandC.localPosition = new Vector3(0f, safeHeight * 0.19f, safeDepth * 0.47f);
                    windowBandC.localScale = new Vector3(safeWidth * 0.52f, Mathf.Max(0.05f, safeHeight * 0.03f), Mathf.Max(0.04f, safeDepth * 0.04f));
                }
            }

            var neonBand = root.Find("NeonBand");
            if (neonBand != null)
            {
                neonBand.gameObject.SetActive(useHigh);
                if (useHigh)
                {
                    neonBand.localPosition = new Vector3(0f, safeHeight * 0.08f, safeDepth * 0.44f);
                    neonBand.localScale = new Vector3(safeWidth * 0.84f, Mathf.Max(0.08f, safeHeight * 0.04f), Mathf.Max(0.05f, safeDepth * 0.05f));
                }
            }

            var roofCap = root.Find("RoofCap");
            if (roofCap != null)
            {
                roofCap.gameObject.SetActive(useHigh);
                if (useHigh)
                {
                    roofCap.localPosition = new Vector3(0f, safeHeight * 0.52f, 0f);
                    roofCap.localScale = new Vector3(safeWidth * 0.36f, Mathf.Max(0.08f, safeHeight * 0.05f), safeDepth * 0.36f);
                }
            }

            var roofRim = root.Find("RoofRim");
            if (roofRim != null)
            {
                roofRim.gameObject.SetActive(useHigh);
                if (useHigh)
                {
                    roofRim.localPosition = new Vector3(0f, safeHeight * 0.48f, 0f);
                    roofRim.localScale = new Vector3(safeWidth * 0.64f, Mathf.Max(0.05f, safeHeight * 0.03f), safeDepth * 0.64f);
                }
            }

            var antenna = root.Find("Antenna");
            if (antenna != null)
            {
                antenna.gameObject.SetActive(useHigh);
                if (useHigh)
                {
                    antenna.localPosition = new Vector3(
                        (float)(random.NextDouble() < 0.5 ? -1f : 1f) * safeWidth * 0.16f,
                        safeHeight * 0.74f,
                        safeDepth * -0.08f);
                    antenna.localScale = new Vector3(0.06f, safeHeight * 0.36f, 0.06f);
                }
            }

            var serviceBox = root.Find("ServiceBox");
            if (serviceBox != null)
            {
                serviceBox.gameObject.SetActive(useHigh && detailSeed > 0.28f);
                if (serviceBox.gameObject.activeSelf)
                {
                    serviceBox.localPosition = new Vector3(
                        safeWidth * 0.22f * (detailSeed > 0.65f ? 1f : -1f),
                        safeHeight * 0.54f,
                        safeDepth * 0.08f);
                    serviceBox.localScale = new Vector3(safeWidth * 0.16f, Mathf.Max(0.07f, safeHeight * 0.08f), safeDepth * 0.18f);
                }
            }
        }

        private void ConfigureCloudGeometry(Transform root, float baseScale, System.Random random)
        {
            if (root == null)
            {
                return;
            }

            var quality = backdropQuality == BackdropQuality.Auto ? ResolveAutoQuality() : backdropQuality;
            var safeScale = Mathf.Max(1f, baseScale);
            var baseWidth = safeScale * 2.2f;
            var baseHeight = safeScale * 0.62f;
            var baseDepth = safeScale * 1.35f;
            root.localScale = new Vector3(baseWidth, baseHeight, baseDepth);
            var activeLobes = quality == BackdropQuality.High ? CloudLobeCount : (quality == BackdropQuality.Medium ? 10 : 6);
            for (var i = 0; i < CloudLobeCount; i++)
            {
                var lobe = root.Find("Lobe" + i);
                if (lobe == null)
                {
                    continue;
                }

                var active = i < activeLobes;
                lobe.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var ringIndex = i % 7;
                var layer = i / 7;
                var normalized = ringIndex / 7f;
                var angle = normalized * Mathf.PI * 2f + (((float)random.NextDouble() - 0.5f) * 0.42f);
                var radius = (layer == 0 ? 0.36f : 0.22f) + ((float)random.NextDouble() * 0.1f);
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * (0.52f + (layer * 0.11f));
                var y = (layer == 0 ? 0.04f : 0.18f)
                    + Mathf.Sin(angle * 2f) * 0.03f
                    + (((float)random.NextDouble() - 0.5f) * 0.04f);

                var lobeScale = 0.38f
                    + (layer == 0 ? 0.24f : 0.34f)
                    + ((float)random.NextDouble() * 0.2f);
                var lobeScaleX = lobeScale * (1.12f + ((float)random.NextDouble() * 0.24f));
                var lobeScaleY = lobeScale * (0.66f + ((float)random.NextDouble() * 0.18f));
                var lobeScaleZ = lobeScale * (0.9f + ((float)random.NextDouble() * 0.18f));
                lobe.localPosition = new Vector3(x, y, z);
                lobe.localScale = new Vector3(
                    Mathf.Max(0.28f, lobeScaleX),
                    Mathf.Max(0.22f, lobeScaleY),
                    Mathf.Max(0.24f, lobeScaleZ));
            }
        }

        private void ClearTrackDecor()
        {
            for (var i = 0; i < _activeStripes.Count; i++)
            {
                var stripe = _activeStripes[i];
                if (stripe == null)
                {
                    continue;
                }

                stripe.gameObject.SetActive(false);
                stripe.SetParent(_trackDecorRoot, false);
                _stripePool.Push(stripe);
            }

            _activeStripes.Clear();
        }

        private void ClearBackdrop()
        {
            for (var i = 0; i < _activeBackdropBlocks.Count; i++)
            {
                var block = _activeBackdropBlocks[i];
                if (block == null)
                {
                    continue;
                }

                block.gameObject.SetActive(false);
                block.SetParent(_backdropRoot, false);
                _backdropPool.Push(block);
            }

            _activeBackdropBlocks.Clear();
        }

        private void ClearClouds()
        {
            for (var i = 0; i < _activeClouds.Count; i++)
            {
                var cloud = _activeClouds[i];
                if (cloud == null)
                {
                    continue;
                }

                cloud.gameObject.SetActive(false);
                cloud.SetParent(_cloudRoot, false);
                _cloudPool.Push(cloud);
            }

            _activeClouds.Clear();
            _cloudSpeeds.Clear();
            _cloudMinX.Clear();
            _cloudMaxX.Clear();
            _cloudBaseY.Clear();
            _cloudPhases.Clear();
        }

        private void ClearBeacons()
        {
            if (_beaconRoot == null)
            {
                _activeBeacons.Clear();
                return;
            }

            for (var i = 0; i < _activeBeacons.Count; i++)
            {
                var beacon = _activeBeacons[i];
                if (beacon == null)
                {
                    continue;
                }

                beacon.gameObject.SetActive(false);
                beacon.SetParent(_beaconRoot, false);
                _beaconPool.Push(beacon);
            }

            _activeBeacons.Clear();
        }

        private void ClearStreetAmbience()
        {
            for (var i = 0; i < _activeSidewalkTiles.Count; i++)
            {
                var tile = _activeSidewalkTiles[i];
                if (tile == null)
                {
                    continue;
                }

                tile.gameObject.SetActive(false);
                tile.SetParent(_sidewalkRoot != null ? _sidewalkRoot : _trackDecorRoot, false);
                _sidewalkTilePool.Push(tile);
            }

            for (var i = 0; i < _activePedestrians.Count; i++)
            {
                var pedestrian = _activePedestrians[i];
                if (pedestrian == null)
                {
                    continue;
                }

                pedestrian.gameObject.SetActive(false);
                pedestrian.SetParent(_pedestrianRoot != null ? _pedestrianRoot : _trackDecorRoot, false);
                _pedestrianPool.Push(pedestrian);
            }

            _activeSidewalkTiles.Clear();
            _activePedestrians.Clear();
            _pedestrianSpeed.Clear();
            _pedestrianMinZ.Clear();
            _pedestrianMaxZ.Clear();
            _pedestrianPhase.Clear();
            _pedestrianYBase.Clear();
            _pedestrianLeftArm.Clear();
            _pedestrianRightArm.Clear();
            _pedestrianLeftLeg.Clear();
            _pedestrianRightLeg.Clear();
        }

        private void PrewarmStripePool()
        {
            if (_stripePoolPrewarmed)
            {
                return;
            }

            var count = Mathf.Max(0, stripePoolSize);
            for (var i = 0; i < count; i++)
            {
                var stripe = CreateStripe();
                _stripePool.Push(stripe);
            }

            _stripePoolPrewarmed = true;
        }

        private void PrewarmBackdropPool()
        {
            if (_backdropPoolPrewarmed)
            {
                return;
            }

            var density = Mathf.Clamp(GetBackdropDensityMultiplier(), 0.45f, 1.25f);
            var count = Mathf.RoundToInt(Mathf.Max(0, backdropPoolSize) * density);
            for (var i = 0; i < count; i++)
            {
                var block = CreateBackdropBlock();
                _backdropPool.Push(block);
            }

            _backdropPoolPrewarmed = true;
        }

        private void PrewarmCloudPool()
        {
            if (_cloudPoolPrewarmed)
            {
                return;
            }

            var density = Mathf.Clamp(GetBackdropDensityMultiplier(), 0.45f, 1.25f);
            var count = Mathf.RoundToInt(Mathf.Max(0, cloudPoolSize) * Mathf.Lerp(0.66f, 1f, density));
            for (var i = 0; i < count; i++)
            {
                var cloud = CreateCloud();
                _cloudPool.Push(cloud);
            }

            _cloudPoolPrewarmed = true;
        }

        private void PrewarmBeaconPool()
        {
            if (_beaconPoolPrewarmed)
            {
                return;
            }

            var count = Mathf.Max(0, beaconPoolSize);
            for (var i = 0; i < count; i++)
            {
                var beacon = CreateBeacon();
                _beaconPool.Push(beacon);
            }

            _beaconPoolPrewarmed = true;
        }

        private void PrewarmSidewalkPool()
        {
            if (_sidewalkPoolPrewarmed || !enableStreetAmbience)
            {
                return;
            }

            var count = Mathf.Max(0, sidewalkTilePoolSize);
            for (var i = 0; i < count; i++)
            {
                var tile = CreateSidewalkTile();
                _sidewalkTilePool.Push(tile);
            }

            _sidewalkPoolPrewarmed = true;
        }

        private void PrewarmPedestrianPool()
        {
            if (_pedestrianPoolPrewarmed || !enableStreetAmbience)
            {
                return;
            }

            var count = Mathf.Max(0, pedestrianPoolSize);
            for (var i = 0; i < count; i++)
            {
                var pedestrian = CreatePedestrian();
                _pedestrianPool.Push(pedestrian);
            }

            _pedestrianPoolPrewarmed = true;
        }

        private Material GetStripeMaterial()
        {
            if (_stripeMaterial != null)
            {
                return _stripeMaterial;
            }

            _stripeMaterial = CreateRuntimeMaterial("LaneStripeMaterial", stripeColor, 0.35f, 0.36f);
            return _stripeMaterial;
        }

        private Material GetRailMaterial()
        {
            if (_railMaterial != null)
            {
                return _railMaterial;
            }

            _railMaterial = CreateRuntimeMaterial("SideRailMaterial", railColor, 0.2f, 0.08f);
            return _railMaterial;
        }

        private Material GetBackdropMaterial()
        {
            if (_backdropMaterial != null)
            {
                return _backdropMaterial;
            }

            _backdropMaterial = CreateRuntimeMaterial("BackdropMaterial", backdropColor, 0.05f, 0.06f);
            return _backdropMaterial;
        }

        private Material GetCloudMaterial()
        {
            if (_cloudMaterial != null)
            {
                return _cloudMaterial;
            }

            _cloudMaterial = CreateRuntimeMaterial("CloudMaterial", cloudColor, 0.2f, 0.18f);
            return _cloudMaterial;
        }

        private Material GetBeaconPoleMaterial()
        {
            if (_beaconPoleMaterial != null)
            {
                return _beaconPoleMaterial;
            }

            _beaconPoleMaterial = CreateRuntimeMaterial("BeaconPoleMaterial", beaconPoleColor, 0.22f, 0.04f);
            return _beaconPoleMaterial;
        }

        private Material GetBeaconCoreMaterial()
        {
            if (_beaconCoreMaterial != null)
            {
                return _beaconCoreMaterial;
            }

            _beaconCoreMaterial = CreateRuntimeMaterial("BeaconCoreMaterial", beaconCoreColor, 0.55f, 0.7f);
            return _beaconCoreMaterial;
        }

        private Material GetSidewalkMaterial()
        {
            if (_sidewalkMaterial != null)
            {
                return _sidewalkMaterial;
            }

            _sidewalkMaterial = CreateRuntimeMaterial("SidewalkMaterial", sidewalkColor, 0.18f, 0.06f);
            return _sidewalkMaterial;
        }

        private Material GetCurbMaterial()
        {
            if (_curbMaterial != null)
            {
                return _curbMaterial;
            }

            _curbMaterial = CreateRuntimeMaterial("CurbMaterial", curbColor, 0.22f, 0.08f);
            return _curbMaterial;
        }

        private Material GetPedestrianClothMaterial()
        {
            if (_pedestrianClothMaterial != null)
            {
                return _pedestrianClothMaterial;
            }

            _pedestrianClothMaterial = CreateRuntimeMaterial("PedestrianCloth", new Color(0.28f, 0.38f, 0.52f, 1f), 0.18f, 0.04f);
            return _pedestrianClothMaterial;
        }

        private Material GetPedestrianAccentMaterial()
        {
            if (_pedestrianAccentMaterial != null)
            {
                return _pedestrianAccentMaterial;
            }

            _pedestrianAccentMaterial = CreateRuntimeMaterial("PedestrianAccent", new Color(0.82f, 0.9f, 0.98f, 1f), 0.24f, 0.1f);
            return _pedestrianAccentMaterial;
        }

        private Material GetSkinMaterialForPedestrian()
        {
            if (_pedestrianSkinMaterial != null)
            {
                return _pedestrianSkinMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            _pedestrianSkinMaterial = new Material(shader)
            {
                name = "PedestrianSkin"
            };
            var color = new Color(0.95f, 0.78f, 0.64f, 1f);
            if (_pedestrianSkinMaterial.HasProperty("_BaseColor"))
            {
                _pedestrianSkinMaterial.SetColor("_BaseColor", color);
            }

            if (_pedestrianSkinMaterial.HasProperty("_Color"))
            {
                _pedestrianSkinMaterial.SetColor("_Color", color);
            }

            if (_pedestrianSkinMaterial.HasProperty("_Smoothness"))
            {
                _pedestrianSkinMaterial.SetFloat("_Smoothness", 0.18f);
            }

            return _pedestrianSkinMaterial;
        }

        private Material GetHazardMaterial()
        {
            if (_hazardMaterial != null)
            {
                return _hazardMaterial;
            }

            _hazardMaterial = CreateRuntimeMaterial("HazardZoneMaterial", new Color(0.95f, 0.72f, 0.14f, 1f), 0.08f, 0.32f);
            return _hazardMaterial;
        }

        private static Material CreateRuntimeMaterial(string name, Color color, float smoothness, float emission = 0f)
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

            return material;
        }

        private void UpdateDynamicPalette(int levelIndex, bool isMiniBoss)
        {
            var theme = ResolveTheme(levelIndex);
            var bandProgress = ((Mathf.Max(1, levelIndex) - 1) % 10) / 9f;
            var pulse = 0.95f + Mathf.Sin((levelIndex - 1) * 0.55f) * 0.05f;

            _trackColor = ScaleColor(theme.trackColor, Mathf.Lerp(0.94f, 1.04f, bandProgress));
            stripeColor = ScaleColor(theme.stripeColor, pulse);
            railColor = ScaleColor(theme.railColor, 0.96f + (bandProgress * 0.05f));
            backdropColor = ScaleColor(theme.backdropColor, 0.94f + (bandProgress * 0.08f));
            cloudColor = theme.cloudColor;
            cloudColor.a = 1f;
            sidewalkColor = ScaleColor(theme.groundTint, 0.46f + (bandProgress * 0.12f));
            curbColor = ScaleColor(theme.cloudColor, 0.72f + (bandProgress * 0.08f));
            beaconPoleColor = ScaleColor(theme.beaconPoleColor, 0.95f + (bandProgress * 0.04f));
            beaconCoreColor = ScaleColor(theme.beaconCoreColor, 1f + (bandProgress * 0.12f));

            _gatePositiveColor = ScaleColor(theme.gatePositiveColor, 0.98f + (bandProgress * 0.06f));
            _gateNegativeColor = ScaleColor(theme.gateNegativeColor, 1f + (bandProgress * 0.05f));
            _hazardSlowColor = ScaleColor(theme.hazardSlowColor, 1f + (bandProgress * 0.05f));
            _hazardKnockbackColor = ScaleColor(theme.hazardKnockbackColor, 1f + (bandProgress * 0.08f));

            if (isMiniBoss)
            {
                _gatePositiveColor = ScaleColor(_gatePositiveColor, 1.08f);
                _gateNegativeColor = ScaleColor(_gateNegativeColor, 1.1f);
                _hazardSlowColor = ScaleColor(_hazardSlowColor, 1.08f);
                _hazardKnockbackColor = ScaleColor(_hazardKnockbackColor, 1.12f);
                beaconCoreColor = ScaleColor(beaconCoreColor, 1.18f);
            }

            ApplyMaterialColor(_stripeMaterial, stripeColor, 0.42f, 0.44f);
            ApplyMaterialColor(_railMaterial, railColor, 0.24f, 0.1f);
            ApplyMaterialColor(_backdropMaterial, backdropColor, 0.08f, 0.08f);
            ApplyMaterialColor(_cloudMaterial, cloudColor, 0.22f, 0.2f);
            ApplyMaterialColor(_beaconPoleMaterial, beaconPoleColor, 0.2f, 0.05f);
            ApplyMaterialColor(_beaconCoreMaterial, beaconCoreColor, 0.66f, 0.95f);
            ApplyMaterialColor(_sidewalkMaterial, sidewalkColor, 0.18f, 0.06f);
            ApplyMaterialColor(_curbMaterial, curbColor, 0.22f, 0.08f);
            ApplyMaterialColor(_pedestrianClothMaterial, ScaleColor(theme.ambientEquator, 0.88f), 0.18f, 0.05f);
            ApplyMaterialColor(_pedestrianAccentMaterial, ScaleColor(theme.ambientSky, 1.1f), 0.24f, 0.09f);
            ApplyMaterialColor(_hazardMaterial, _hazardSlowColor, 0.12f, 0.36f);

            SceneVisualTuning.ApplyLevelTheme(
                theme.skyTint,
                theme.groundTint,
                theme.fogColor,
                theme.fogDensity,
                theme.skyExposure,
                theme.atmosphereThickness,
                theme.ambientSky,
                theme.ambientEquator,
                theme.ambientGround,
                theme.sunColor,
                theme.sunIntensity);
        }

        private static Color ScaleColor(Color color, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(color.r * multiplier),
                Mathf.Clamp01(color.g * multiplier),
                Mathf.Clamp01(color.b * multiplier),
                color.a);
        }

        private static ThemeDefinition ResolveTheme(int levelIndex)
        {
            var themeIndex = ((Mathf.Max(1, levelIndex) - 1) / 10) % 6;
            switch (themeIndex)
            {
                case 0:
                    return ThemeDefinition.MetroDawn();
                case 1:
                    return ThemeDefinition.SunsetDunes();
                case 2:
                    return ThemeDefinition.AuroraIce();
                case 3:
                    return ThemeDefinition.VolcanicRift();
                case 4:
                    return ThemeDefinition.EmeraldVista();
                default:
                    return ThemeDefinition.NeoNight();
            }
        }

        private static void ApplyMaterialColor(Material material, Color color, float smoothness, float emission)
        {
            if (material == null)
            {
                return;
            }

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
        }

        private void SpawnGates(List<GateRow> rows, float gateDifficulty01)
        {
            var hitboxWidth = Mathf.Max(2f, Mathf.Lerp(gateWidthAtStart, gateWidthAtHighDifficulty, gateDifficulty01));
            var panelWidth = Mathf.Max(2.2f, Mathf.Lerp(panelWidthAtStart, panelWidthAtHighDifficulty, gateDifficulty01));
            var edgePadding = 0.95f;
            var trackMinX = -_effectiveTrackHalfWidth + edgePadding;
            var trackMaxX = _effectiveTrackHalfWidth - edgePadding;
            var levelFactor = Mathf.Max(1, _activeLevelIndex);
            var baseShotThreshold = Mathf.Max(3, Mathf.RoundToInt(gateUpgradeShotsAtLevel1 + (levelFactor - 1) * Mathf.Max(0f, gateUpgradeShotsPerLevel)));
            var stepGrowth = Mathf.Max(1, gateUpgradeShotsGrowthPerStep + Mathf.FloorToInt((levelFactor - 1) * 0.12f));
            var addCapGrowth = Mathf.Max(0, gateAddUpgradeBonusCapGrowthPer10Levels);
            var addBonusCap = Mathf.Max(1, gateAddUpgradeBonusCapAtLevel1 + (((levelFactor - 1) / 10) * addCapGrowth));
            var multiplierCap = Mathf.Clamp(
                gateMultiplyUpgradeCapAtLevel1 + Mathf.Floor(levelFactor / 20f) * gateMultiplyUpgradeCapGrowthPer20Levels,
                2.1f,
                3f);
            var multiplierCapTenths = Mathf.Max(21, Mathf.RoundToInt(multiplierCap * 10f));

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                for (var j = 0; j < row.gates.Count; j++)
                {
                    var gateSpec = row.gates[j];
                    var gate = GetGate();
                    var laneCenterX = LaneToX(gateSpec.lane);
                    var sideLimit = gateSpec.lane == 1 ? _effectiveLaneSpacing * 0.62f : _effectiveLaneSpacing * 0.34f;
                    var laneMinX = Mathf.Clamp(laneCenterX - sideLimit, trackMinX, trackMaxX);
                    var laneMaxX = Mathf.Clamp(laneCenterX + sideLimit, trackMinX, trackMaxX);
                    var maxAmplitude = Mathf.Min(Mathf.Abs(laneCenterX - laneMinX), Mathf.Abs(laneMaxX - laneCenterX));
                    var moveAmplitude = Mathf.Clamp(gateSpec.moveAmplitude, 0f, maxAmplitude);

                    gate.hitboxWidth = hitboxWidth;
                    gate.panelWidth = panelWidth;
                    gate.positiveColor = _gatePositiveColor;
                    gate.negativeColor = _gateNegativeColor;

                    gate.transform.position = new Vector3(laneCenterX, 0f, row.z);
                    gate.transform.rotation = Quaternion.identity;
                    gate.Configure(
                        gateSpec.operation,
                        gateSpec.value,
                        i,
                        gateSpec.pickTier,
                        gateSpec.movesHorizontally,
                        moveAmplitude,
                        gateSpec.moveSpeed,
                        gateSpec.movePhase,
                        laneCenterX,
                        laneMinX,
                        laneMaxX,
                        gateSpec.useTempoWindow,
                        gateSpec.tempoCycle,
                        gateSpec.tempoOpenRatio,
                        gateSpec.tempoPhase);

                    var tierMultiplier = gateSpec.pickTier == GatePickTier.BetterGood
                        ? 1.1f
                        : gateSpec.pickTier == GatePickTier.RedBad
                            ? 1.35f
                            : 1f;
                    var firstThreshold = Mathf.Max(3, Mathf.RoundToInt(baseShotThreshold * tierMultiplier));
                    gate.ConfigureShotUpgrade(
                        enableGateShotUpgrades && (gateSpec.operation == GateOperation.Add || gateSpec.operation == GateOperation.Multiply),
                        firstThreshold,
                        stepGrowth,
                        addBonusCap,
                        multiplierCapTenths);
                    gate.gameObject.SetActive(true);
                    _activeGates.Add(gate);
                }
            }
        }

        private void SpawnHazards(List<HazardSpec> hazards)
        {
            if (!enableHazards || hazards == null || hazards.Count == 0)
            {
                return;
            }

            var edgePadding = 0.95f;
            var minX = -_effectiveTrackHalfWidth + edgePadding;
            var maxX = _effectiveTrackHalfWidth - edgePadding;
            for (var i = 0; i < hazards.Count; i++)
            {
                var spec = hazards[i];
                var hazard = GetHazard();
                hazard.pitColor = _hazardSlowColor;
                hazard.knockbackColor = _hazardKnockbackColor;
                var laneX = Mathf.Clamp(LaneToX(spec.lane), minX, maxX);
                var worldPosition = new Vector3(laneX, -0.01f, spec.z);
                var worldScale = new Vector3(spec.width, 0.04f, spec.depth);
                hazard.Configure(
                    spec.type,
                    spec.unitLossFraction,
                    spec.flatLoss,
                    spec.knockbackDeltaX,
                    worldPosition,
                    worldScale,
                    spec.emphasize);
                hazard.gameObject.SetActive(true);
                _activeHazards.Add(hazard);
            }
        }

        private void SpawnFinish(float finishZ, int enemyCount, int tankRequirement, bool isMiniBoss)
        {
            if (finishPrefab == null)
            {
                return;
            }

            if (_activeFinish == null)
            {
                _activeFinish = Instantiate(finishPrefab, levelRoot);
            }

            _activeFinish.transform.position = new Vector3(0f, 0f, finishZ);
            _activeFinish.transform.rotation = Quaternion.identity;
            _activeFinish.gameObject.SetActive(true);
            _activeFinish.Configure(enemyCount, tankRequirement, isMiniBoss);
        }

        private void ClearGeneratedObjects()
        {
            for (var i = 0; i < _activeGates.Count; i++)
            {
                var gate = _activeGates[i];
                if (gate == null)
                {
                    continue;
                }

                gate.gameObject.SetActive(false);
                gate.transform.SetParent(gateRoot != null ? gateRoot : transform, false);
                _gatePool.Push(gate);
            }

            for (var i = 0; i < _activeHazards.Count; i++)
            {
                var hazard = _activeHazards[i];
                if (hazard == null)
                {
                    continue;
                }

                hazard.gameObject.SetActive(false);
                hazard.transform.SetParent(_hazardRoot != null ? _hazardRoot : transform, false);
                _hazardPool.Push(hazard);
            }

            _activeGates.Clear();
            _activeHazards.Clear();
            ClearTrackDecor();
            ClearBackdrop();
            ClearClouds();
            ClearBeacons();
            ClearStreetAmbience();
            SetRailsActive(false);

            if (_activeFinish != null)
            {
                _activeFinish.gameObject.SetActive(false);
            }
        }

        private Gate GetGate()
        {
            EnsureRoots();

            if (_gatePool.Count > 0)
            {
                return _gatePool.Pop();
            }

            if (gatePrefab == null)
            {
                throw new InvalidOperationException("LevelGenerator requires a Gate prefab.");
            }

            return Instantiate(gatePrefab, gateRoot);
        }

        private HazardZone GetHazard()
        {
            EnsureRoots();
            if (_hazardPool.Count > 0)
            {
                return _hazardPool.Pop();
            }

            return CreateHazard();
        }

        private HazardZone CreateHazard()
        {
            EnsureRoots();
            var hazardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazardObject.name = "HazardZone";
            hazardObject.transform.SetParent(_hazardRoot, false);

            var renderer = hazardObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetHazardMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var hazard = hazardObject.AddComponent<HazardZone>();
            hazard.zoneRenderer = renderer;

            var labelRoot = new GameObject("Label");
            labelRoot.transform.SetParent(hazardObject.transform, false);
            var label = labelRoot.AddComponent<TextMesh>();
            label.text = "PIT";
            hazard.labelText = label;

            hazardObject.SetActive(false);
            return hazard;
        }

        private void PrewarmGatePool()
        {
            if (_gatePoolPrewarmed || gatePrefab == null)
            {
                return;
            }

            var targetCount = Mathf.Max(0, initialGatePoolSize);
            for (var i = 0; i < targetCount; i++)
            {
                var gate = Instantiate(gatePrefab, gateRoot);
                gate.gameObject.SetActive(false);
                _gatePool.Push(gate);
            }

            _gatePoolPrewarmed = true;
        }

        private void PrewarmHazardPool()
        {
            if (_hazardPoolPrewarmed || !enableHazards)
            {
                return;
            }

            var targetCount = Mathf.Max(0, initialHazardPoolSize);
            for (var i = 0; i < targetCount; i++)
            {
                var hazard = CreateHazard();
                hazard.gameObject.SetActive(false);
                _hazardPool.Push(hazard);
            }

            _hazardPoolPrewarmed = true;
        }

        private float LaneToX(int lane)
        {
            switch (lane)
            {
                case 0:
                    return -_effectiveLaneSpacing;
                case 1:
                    return 0f;
                case 2:
                    return _effectiveLaneSpacing;
                default:
                    return 0f;
            }
        }

        private GeneratedLevel BuildDefinition(int levelIndex, DifficultyMode difficultyMode, int forcedStartCount)
        {
            var random = new System.Random(9143 + levelIndex * 101);
            var gateDifficulty01 = EvaluateGateDifficulty(levelIndex);
            var isMiniBoss = miniBossEveryLevels > 0 && levelIndex % Mathf.Max(1, miniBossEveryLevels) == 0;
            var modifier = BuildModifierState(levelIndex);
            var themeName = ResolveTheme(levelIndex).name;

            var generated = new GeneratedLevel
            {
                startCount = forcedStartCount > 0
                    ? Mathf.Clamp(forcedStartCount, 1, CountClamp)
                    : BuildStartCount(levelIndex, difficultyMode, isMiniBoss),
                forwardSpeed = CalculateForwardSpeed(levelIndex),
                isMiniBoss = isMiniBoss,
                modifierName = themeName + " • " + modifier.label
            };

            var compression = Mathf.Clamp(rowCountCompression, 0.3f, 1f);
            var rowCount = isMiniBoss
                ? Mathf.Clamp(Mathf.RoundToInt((15 + (levelIndex / 4)) * compression), 8, 28)
                : Mathf.Clamp(Mathf.RoundToInt((8 + levelIndex) * compression), 4, 22);
            generated.totalRows = rowCount;
            var effectiveRowSpacing = rowSpacing * Mathf.Max(1f, levelLengthMultiplier) * 0.96f * (isMiniBoss ? 1.05f : 1f);
            var baseBadGateChance = Mathf.Clamp01(0.16f + levelIndex * 0.012f + (isMiniBoss ? 0.05f : 0f));
            var addBase = 3 + Mathf.FloorToInt(levelIndex * 0.85f);
            var subtractBase = 2 + Mathf.FloorToInt(levelIndex * 0.65f);

            var moveChance = Mathf.Lerp(movingGateChanceAtStart, movingGateChanceAtHighDifficulty, gateDifficulty01);
            if (modifier.forceMovingGates)
            {
                moveChance = Mathf.Max(moveChance, 0.46f + (gateDifficulty01 * 0.28f));
            }

            var moveAmplitude = Mathf.Lerp(movingGateAmplitudeAtStart, movingGateAmplitudeAtHighDifficulty, gateDifficulty01) *
                                (modifier.forceMovingGates ? 1.16f : 1f);
            var moveSpeed = Mathf.Lerp(movingGateSpeedAtStart, movingGateSpeedAtHighDifficulty, gateDifficulty01) *
                            (modifier.forceMovingGates ? 1.12f : 1f);
            var riskRewardChance = EvaluateRiskRewardChance(gateDifficulty01, modifier, isMiniBoss);
            var tempoChance = EvaluateTempoChance(gateDifficulty01, modifier, isMiniBoss);
            var hazardChance = EvaluateHazardChance(gateDifficulty01, modifier, isMiniBoss);

            generated.rows = new List<GateRow>(rowCount);
            generated.hazards = new List<HazardSpec>(Mathf.Max(4, rowCount / 2));
            var minHazardGap = effectiveRowSpacing * 1.2f;
            var lastHazardZ = -1000f;

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var row = new GateRow
                {
                    z = startZ + rowIndex * effectiveRowSpacing,
                    gates = new List<GateSpec>(3)
                };

                var rowPattern = PickRowPattern(random, rowIndex, rowCount, isMiniBoss, riskRewardChance, tempoChance);
                switch (rowPattern)
                {
                    case RowPattern.Tempo:
                        BuildTempoRow(
                            random,
                            levelIndex,
                            gateDifficulty01,
                            baseBadGateChance,
                            addBase,
                            subtractBase,
                            moveChance,
                            moveAmplitude,
                            moveSpeed,
                            modifier,
                            row.gates);
                        break;
                    case RowPattern.RiskReward:
                        BuildRiskRewardRow(
                            random,
                            levelIndex,
                            gateDifficulty01,
                            addBase,
                            subtractBase,
                            moveAmplitude,
                            moveSpeed,
                            modifier,
                            isMiniBoss,
                            row.gates);
                        break;
                    default:
                        BuildNormalRow(
                            random,
                            levelIndex,
                            baseBadGateChance,
                            addBase,
                            subtractBase,
                            moveChance,
                            moveAmplitude,
                            moveSpeed,
                            modifier,
                            row.gates);
                        break;
                }

                NormalizeRowForObjective(random, levelIndex, addBase, subtractBase, row.gates);
                generated.rows.Add(row);

                if (enableHazards && rowIndex >= 1 && rowIndex < rowCount - 1)
                {
                    var shouldSpawnHazard = random.NextDouble() < hazardChance;
                    if (isMiniBoss && rowIndex % 3 == 2)
                    {
                        shouldSpawnHazard = shouldSpawnHazard || random.NextDouble() < 0.85;
                    }

                    var hazardZ = row.z + (effectiveRowSpacing * 0.56f);
                    if (shouldSpawnHazard && hazardZ - lastHazardZ >= minHazardGap)
                    {
                        var pressureLane = GetMostPressuredLane();
                        var lane = enableAdaptiveLanePressure && random.NextDouble() < 0.6 ? pressureLane : random.Next(0, 3);
                        var hazardType = HazardType.UnitPit;
                        var sideSign = lane == 0 ? 1f : lane == 2 ? -1f : (random.NextDouble() < 0.5 ? -1f : 1f);
                        var pitFraction = Mathf.Lerp(pitLossFractionAtStart, pitLossFractionAtHighDifficulty, gateDifficulty01);
                        if (modifier.hazardRush)
                        {
                            pitFraction *= 1.12f;
                        }

                        pitFraction = Mathf.Clamp(pitFraction, 0.03f, 0.92f);
                        var pitFlatLoss = Mathf.RoundToInt(Mathf.Lerp(pitFlatLossAtStart, pitFlatLossAtHighDifficulty, gateDifficulty01));
                        if (modifier.hazardRush)
                        {
                            pitFlatLoss = Mathf.RoundToInt(pitFlatLoss * 1.18f);
                        }

                        pitFlatLoss = Mathf.Clamp(pitFlatLoss, 2, 1000000);
                        var pitModeScale = 1f;
                        switch (difficultyMode)
                        {
                            case DifficultyMode.Easy:
                                pitModeScale = 0.86f;
                                break;
                            case DifficultyMode.Hard:
                                pitModeScale = 1.28f;
                                break;
                        }

                        var pitSizeMultiplier = (1f + (gateDifficulty01 * 0.4f) + (modifier.hazardRush ? 0.2f : 0f)) * pitModeScale;
                        generated.hazards.Add(new HazardSpec
                        {
                            lane = lane,
                            z = hazardZ,
                            type = hazardType,
                            unitLossFraction = pitFraction,
                            flatLoss = pitFlatLoss,
                            knockbackDeltaX = knockbackHazardStrength * sideSign * (modifier.hazardRush ? 1.2f : 1f),
                            width = Mathf.Max(1.1f, hazardWidth * pitSizeMultiplier * (isMiniBoss ? 1.08f : 1f)),
                            depth = Mathf.Max(1f, hazardDepth * pitSizeMultiplier * (isMiniBoss ? 1.1f : 1f)),
                            emphasize = isMiniBoss || modifier.hazardRush
                        });
                        lastHazardZ = hazardZ;
                    }
                }
            }

            ApplyMultiplierBudget(
                random,
                levelIndex,
                addBase,
                difficultyMode,
                isMiniBoss,
                generated.rows);

            generated.finishZ = startZ + (rowCount * effectiveRowSpacing) + endPadding;
            var expectedBest = EstimateBestCaseCount(generated.startCount, generated.rows);
            var routeProfile = DifficultyRules.BuildRouteProfile(difficultyMode, isMiniBoss, generated.rows.Count);
            generated.referenceBetterRows = routeProfile.betterRows;
            generated.referenceWorseRows = routeProfile.worseRows;
            generated.referenceRedRows = routeProfile.redRows;
            generated.referenceRouteCount = EstimateRouteReferenceCount(
                generated.startCount,
                generated.rows,
                routeProfile,
                difficultyMode,
                isMiniBoss);
            generated.enemyCount = BuildEnemyCount(
                levelIndex,
                generated.startCount,
                expectedBest,
                generated.referenceRouteCount,
                isMiniBoss,
                modifier,
                difficultyMode);
            generated.tankRequirement = BuildTankRequirement(levelIndex, expectedBest, generated.enemyCount, isMiniBoss, modifier);
            generated.gateDifficulty01 = gateDifficulty01;

            return generated;
        }

        private float CalculateForwardSpeed(int levelIndex)
        {
            var level = Mathf.Max(1, levelIndex);
            var speed = baseForwardSpeed + ((level - 1) * Mathf.Max(0f, forwardSpeedPerLevel));
            if (useForwardSpeedCap && maxForwardSpeed > 0f)
            {
                speed = Mathf.Min(maxForwardSpeed, speed);
            }

            return speed;
        }

        private float EvaluateGateDifficulty(int levelIndex)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            var ramp = Mathf.Max(0.0001f, gateDifficultyRamp);
            return 1f - Mathf.Exp(-(safeLevel - 1) * ramp);
        }

        private int BuildStartCount(int levelIndex, DifficultyMode difficultyMode, bool isMiniBoss)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            int startCount;
            switch (difficultyMode)
            {
                case DifficultyMode.Easy:
                    startCount = 10 + Mathf.FloorToInt((safeLevel - 1) * 0.35f);
                    break;
                case DifficultyMode.Hard:
                    startCount = 1 + Mathf.FloorToInt((safeLevel - 1) * 0.05f);
                    break;
                default:
                    startCount = 4 + Mathf.FloorToInt((safeLevel - 1) * 0.18f);
                    break;
            }

            if (isMiniBoss)
            {
                switch (difficultyMode)
                {
                    case DifficultyMode.Easy:
                        startCount += 4;
                        break;
                    case DifficultyMode.Hard:
                        startCount += 1;
                        break;
                    default:
                        startCount += 2;
                        break;
                }
            }

            return Mathf.Clamp(startCount, 1, 320);
        }

        private float EvaluateRiskRewardChance(float gateDifficulty01, ModifierState modifier, bool isMiniBoss)
        {
            var chance = Mathf.Lerp(riskRewardChanceAtStart, riskRewardChanceAtHighDifficulty, gateDifficulty01);
            if (modifier.forceMovingGates)
            {
                chance += 0.04f;
            }

            if (isMiniBoss)
            {
                chance += 0.2f;
            }

            return Mathf.Clamp01(chance);
        }

        private float EvaluateTempoChance(float gateDifficulty01, ModifierState modifier, bool isMiniBoss)
        {
            var chance = Mathf.Lerp(tempoRowChanceAtStart, tempoRowChanceAtHighDifficulty, gateDifficulty01);
            if (modifier.boostTempoRows)
            {
                chance += 0.18f;
            }

            if (isMiniBoss)
            {
                chance += 0.22f;
            }

            return Mathf.Clamp01(chance);
        }

        private float EvaluateHazardChance(float gateDifficulty01, ModifierState modifier, bool isMiniBoss)
        {
            var chance = Mathf.Lerp(hazardChanceAtStart, hazardChanceAtHighDifficulty, gateDifficulty01);
            if (modifier.hazardRush)
            {
                chance += 0.18f;
            }

            if (isMiniBoss)
            {
                chance += 0.12f;
            }

            return Mathf.Clamp01(chance);
        }

        private ModifierState BuildModifierState(int levelIndex)
        {
            var unlockEvery = Mathf.Max(1, modifierUnlockEveryLevels);
            var tier = Mathf.Max(0, (levelIndex - 1) / unlockEvery);
            return new ModifierState
            {
                forceMovingGates = tier >= 1,
                boostTempoRows = tier >= 2,
                hazardRush = tier >= 3,
                aggressivePressure = tier >= 4,
                tankSurge = tier >= 5,
                label = BuildModifierLabel(tier)
            };
        }

        private static string BuildModifierLabel(int tier)
        {
            if (tier <= 0)
            {
                return "Core Rush";
            }

            if (tier == 1)
            {
                return "Drift Gates";
            }

            if (tier == 2)
            {
                return "Tempo Lock";
            }

            if (tier == 3)
            {
                return "Hazard Surge";
            }

            if (tier == 4)
            {
                return "Pressure AI";
            }

            return "Tank Legion";
        }

        private RowPattern PickRowPattern(
            System.Random random,
            int rowIndex,
            int rowCount,
            bool isMiniBoss,
            float riskRewardChance,
            float tempoChance)
        {
            if (rowIndex < 2 && !isMiniBoss)
            {
                return RowPattern.Normal;
            }

            if (isMiniBoss)
            {
                var cycle = rowIndex % 5;
                if (cycle == 1 || cycle == 3)
                {
                    return RowPattern.Tempo;
                }

                if (cycle == 0 || cycle == 4)
                {
                    return RowPattern.RiskReward;
                }

                return RowPattern.Normal;
            }

            var roll = random.NextDouble();
            if (roll < riskRewardChance)
            {
                return RowPattern.RiskReward;
            }

            if (roll < riskRewardChance + tempoChance)
            {
                return RowPattern.Tempo;
            }

            if (rowIndex > rowCount - 4 && random.NextDouble() < 0.38)
            {
                return RowPattern.Tempo;
            }

            return RowPattern.Normal;
        }

        private void BuildNormalRow(
            System.Random random,
            int levelIndex,
            float baseBadGateChance,
            int addBase,
            int subtractBase,
            float moveChance,
            float moveAmplitude,
            float moveSpeed,
            ModifierState modifier,
            List<GateSpec> gates)
        {
            var gateCount = random.NextDouble() < 0.48 ? 3 : 2;
            var laneOrder = BuildShuffledLanes(random);
            for (var gateIndex = 0; gateIndex < gateCount; gateIndex++)
            {
                var lane = laneOrder[gateIndex];
                var laneBadChance = EvaluateLaneBadChance(baseBadGateChance, lane, modifier);
                var operation = PickOperationForLane(random, laneBadChance, levelIndex);
                var value = PickGateValue(random, operation, addBase, subtractBase, levelIndex, false);
                var movesHorizontally = random.NextDouble() < moveChance;
                gates.Add(CreateGateSpec(
                    lane,
                    operation,
                    value,
                    movesHorizontally,
                    movesHorizontally ? moveAmplitude : 0f,
                    movesHorizontally ? moveSpeed : 0f,
                    (float)random.NextDouble() * Mathf.PI * 2f,
                    false,
                    2f,
                    0.5f,
                    0f));
            }
        }

        private void BuildTempoRow(
            System.Random random,
            int levelIndex,
            float gateDifficulty01,
            float baseBadGateChance,
            int addBase,
            int subtractBase,
            float moveChance,
            float moveAmplitude,
            float moveSpeed,
            ModifierState modifier,
            List<GateSpec> gates)
        {
            var safeLane = random.Next(0, 3);
            var cycle = Mathf.Lerp(tempoCycleAtStart, tempoCycleAtHighDifficulty, gateDifficulty01);
            if (modifier.boostTempoRows)
            {
                cycle *= 0.9f;
            }

            var openRatio = Mathf.Lerp(tempoOpenRatioAtStart, tempoOpenRatioAtHighDifficulty, gateDifficulty01);
            if (modifier.boostTempoRows)
            {
                openRatio -= 0.06f;
            }

            openRatio = Mathf.Clamp(openRatio, 0.12f, 0.82f);

            for (var lane = 0; lane < 3; lane++)
            {
                var laneBadChance = EvaluateLaneBadChance(baseBadGateChance, lane, modifier) * 1.12f;
                GateOperation operation;
                int value;
                if (lane == safeLane)
                {
                    operation = random.NextDouble() < 0.06 && levelIndex > 20 ? GateOperation.Multiply : GateOperation.Add;
                    value = operation == GateOperation.Multiply
                        ? 2
                        : Mathf.Max(2, addBase / 2 + random.Next(0, Mathf.Max(2, addBase / 4)));
                }
                else
                {
                    operation = PickOperationForLane(random, laneBadChance, levelIndex);
                    value = PickGateValue(random, operation, addBase, subtractBase, levelIndex, true);
                }

                var movesHorizontally = random.NextDouble() < Mathf.Clamp01(moveChance + 0.2f);
                var phase = lane * cycle * 0.31f;
                gates.Add(CreateGateSpec(
                    lane,
                    operation,
                    value,
                    movesHorizontally,
                    movesHorizontally ? moveAmplitude * 1.1f : 0f,
                    movesHorizontally ? moveSpeed * 1.05f : 0f,
                    (float)random.NextDouble() * Mathf.PI * 2f,
                    true,
                    cycle,
                    openRatio,
                    phase));
            }
        }

        private void BuildRiskRewardRow(
            System.Random random,
            int levelIndex,
            float gateDifficulty01,
            int addBase,
            int subtractBase,
            float moveAmplitude,
            float moveSpeed,
            ModifierState modifier,
            bool isMiniBoss,
            List<GateSpec> gates)
        {
            var leastPressuredLane = GetLeastPressuredLane();
            var mostPressuredLane = GetMostPressuredLane();
            var jackpotLane = random.NextDouble() < 0.65 ? leastPressuredLane : random.Next(0, 3);
            var trapLane = mostPressuredLane == jackpotLane ? (mostPressuredLane + 1) % 3 : mostPressuredLane;
            var safeLane = 3 - jackpotLane - trapLane;

            for (var lane = 0; lane < 3; lane++)
            {
                if (lane == jackpotLane)
                {
                    var multiplier = 2;
                    if (levelIndex > 28 && random.NextDouble() < 0.08)
                    {
                        multiplier = 3;
                    }

                    var tempoCycle = Mathf.Lerp(tempoCycleAtStart, tempoCycleAtHighDifficulty, gateDifficulty01) * 0.9f;
                    var tempoOpenRatio = Mathf.Lerp(tempoOpenRatioAtStart, tempoOpenRatioAtHighDifficulty, gateDifficulty01) * 0.82f;
                    var useTempo = random.NextDouble() < (0.45 + (modifier.boostTempoRows ? 0.25 : 0.08));
                    gates.Add(CreateGateSpec(
                        lane,
                        GateOperation.Multiply,
                        multiplier,
                        true,
                        moveAmplitude * (isMiniBoss ? 1.28f : 1.16f),
                        moveSpeed * (isMiniBoss ? 1.2f : 1.08f),
                        (float)random.NextDouble() * Mathf.PI * 2f,
                        useTempo,
                        tempoCycle,
                        Mathf.Clamp(tempoOpenRatio, 0.12f, 0.78f),
                        (float)random.NextDouble() * tempoCycle));
                    continue;
                }

                if (lane == trapLane)
                {
                    var trapOperation = random.NextDouble() < 0.57 ? GateOperation.Subtract : GateOperation.Divide;
                    var trapValue = PickGateValue(random, trapOperation, addBase, subtractBase, levelIndex, true);
                    gates.Add(CreateGateSpec(
                        lane,
                        trapOperation,
                        trapValue,
                        random.NextDouble() < 0.48,
                        moveAmplitude * 0.88f,
                        moveSpeed,
                        (float)random.NextDouble() * Mathf.PI * 2f,
                        false,
                        2f,
                        0.5f,
                        0f));
                    continue;
                }

                var safeValue = Mathf.Max(2, addBase / 2 + random.Next(1, Mathf.Max(3, addBase / 4 + 1)));
                gates.Add(CreateGateSpec(
                    safeLane,
                    GateOperation.Add,
                    safeValue,
                    false,
                    0f,
                    0f,
                    0f,
                    false,
                    2f,
                    0.5f,
                    0f));
            }
        }

        private static GateSpec CreateGateSpec(
            int lane,
            GateOperation operation,
            int value,
            bool movesHorizontally,
            float moveAmplitude,
            float moveSpeed,
            float movePhase,
            bool useTempoWindow,
            float tempoCycle,
            float tempoOpenRatio,
            float tempoPhase)
        {
            return new GateSpec
            {
                lane = lane,
                operation = operation,
                value = Mathf.Max(1, value),
                pickTier = GatePickTier.WorseGood,
                movesHorizontally = movesHorizontally,
                moveAmplitude = Mathf.Max(0f, moveAmplitude),
                moveSpeed = Mathf.Max(0f, moveSpeed),
                movePhase = movePhase,
                useTempoWindow = useTempoWindow,
                tempoCycle = Mathf.Max(0.25f, tempoCycle),
                tempoOpenRatio = Mathf.Clamp(tempoOpenRatio, 0.08f, 0.92f),
                tempoPhase = tempoPhase
            };
        }

        private static void NormalizeRowForObjective(
            System.Random random,
            int levelIndex,
            int addBase,
            int subtractBase,
            List<GateSpec> gates)
        {
            if (gates == null)
            {
                return;
            }

            EnsureThreeGateRow(random, addBase, gates);
            if (gates.Count == 0)
            {
                return;
            }

            var redIndex = GetWorstGateIndex(gates, levelIndex);
            for (var i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                if (i == redIndex)
                {
                    if (gate.operation == GateOperation.Add || gate.operation == GateOperation.Multiply)
                    {
                        gate.operation = random.NextDouble() < 0.62 ? GateOperation.Subtract : GateOperation.Divide;
                    }

                    if (gate.operation == GateOperation.Subtract)
                    {
                        var value = subtractBase + random.Next(0, Mathf.Max(2, subtractBase / 2 + 1));
                        gate.value = Mathf.Clamp(value, 2, Mathf.Max(4, addBase + 2));
                    }
                    else
                    {
                        gate.value = levelIndex > 32 && random.NextDouble() < 0.08 ? 3 : 2;
                    }

                    gate.pickTier = GatePickTier.RedBad;
                    gates[i] = gate;
                    continue;
                }

                if (gate.operation == GateOperation.Subtract || gate.operation == GateOperation.Divide)
                {
                    gate.operation = random.NextDouble() < 0.18 && levelIndex > 12
                        ? GateOperation.Multiply
                        : GateOperation.Add;
                }

                if (gate.operation == GateOperation.Add)
                {
                    gate.value = Mathf.Max(2, Mathf.Max(gate.value, addBase / 2 + random.Next(1, Mathf.Max(3, addBase / 4 + 1))));
                }
                else if (gate.operation == GateOperation.Multiply)
                {
                    gate.value = Mathf.Clamp(gate.value, 2, levelIndex > 34 && random.NextDouble() < 0.05 ? 3 : 2);
                }

                gate.pickTier = GatePickTier.WorseGood;
                gates[i] = gate;
            }

            var betterIndex = -1;
            var worseIndex = -1;
            var betterScore = float.NegativeInfinity;
            var worseScore = float.NegativeInfinity;
            for (var i = 0; i < gates.Count; i++)
            {
                if (i == redIndex)
                {
                    continue;
                }

                var score = EvaluateGateBenefit(gates[i], levelIndex);
                if (score > betterScore)
                {
                    worseScore = betterScore;
                    worseIndex = betterIndex;
                    betterScore = score;
                    betterIndex = i;
                }
                else if (score > worseScore)
                {
                    worseScore = score;
                    worseIndex = i;
                }
            }

            if (betterIndex < 0)
            {
                betterIndex = (redIndex + 1) % gates.Count;
            }

            if (worseIndex < 0)
            {
                worseIndex = (betterIndex + 1) % gates.Count;
                if (worseIndex == redIndex)
                {
                    worseIndex = (worseIndex + 1) % gates.Count;
                }
            }

            var betterGate = gates[betterIndex];
            if (betterGate.operation == GateOperation.Add && random.NextDouble() < 0.06f + Mathf.Clamp01(levelIndex / 100f) * 0.08f)
            {
                betterGate.operation = GateOperation.Multiply;
                betterGate.value = levelIndex > 36 && random.NextDouble() < 0.08 ? 3 : 2;
            }
            else if (betterGate.operation == GateOperation.Add)
            {
                betterGate.value = Mathf.Max(betterGate.value, addBase + random.Next(2, Mathf.Max(4, addBase / 3 + 4)));
            }
            else if (betterGate.operation == GateOperation.Multiply)
            {
                betterGate.value = Mathf.Clamp(betterGate.value, 2, levelIndex > 36 && random.NextDouble() < 0.08 ? 3 : 2);
            }

            betterGate.pickTier = GatePickTier.BetterGood;
            gates[betterIndex] = betterGate;

            var worseGate = gates[worseIndex];
            if (worseGate.operation == GateOperation.Multiply)
            {
                worseGate.value = 2;
                if (betterGate.operation == GateOperation.Multiply && betterGate.value <= worseGate.value)
                {
                    worseGate.operation = GateOperation.Add;
                    worseGate.value = Mathf.Max(2, addBase / 2 + random.Next(0, Mathf.Max(2, addBase / 4 + 1)));
                }
            }
            else
            {
                var cap = Mathf.Max(2, addBase + 2);
                worseGate.value = Mathf.Clamp(worseGate.value, 2, cap);
            }

            worseGate.pickTier = GatePickTier.WorseGood;
            gates[worseIndex] = worseGate;

            for (var i = 0; i < gates.Count; i++)
            {
                if (i == redIndex || i == betterIndex || i == worseIndex)
                {
                    continue;
                }

                var gate = gates[i];
                gate.pickTier = GatePickTier.WorseGood;
                gates[i] = gate;
            }
        }

        private static void EnsureThreeGateRow(System.Random random, int addBase, List<GateSpec> gates)
        {
            var usedLane = new bool[3];
            var laneOwner = new[] { -1, -1, -1 };
            var duplicateIndices = new List<int>(3);
            for (var i = 0; i < gates.Count; i++)
            {
                var lane = Mathf.Clamp(gates[i].lane, 0, 2);
                usedLane[lane] = true;
                if (laneOwner[lane] == -1)
                {
                    laneOwner[lane] = i;
                }
                else
                {
                    duplicateIndices.Add(i);
                }
            }

            for (var i = 0; i < duplicateIndices.Count; i++)
            {
                var targetLane = -1;
                for (var lane = 0; lane < 3; lane++)
                {
                    if (laneOwner[lane] == -1)
                    {
                        targetLane = lane;
                        break;
                    }
                }

                if (targetLane < 0)
                {
                    break;
                }

                var duplicateIndex = duplicateIndices[i];
                var duplicateGate = gates[duplicateIndex];
                duplicateGate.lane = targetLane;
                gates[duplicateIndex] = duplicateGate;
                laneOwner[targetLane] = duplicateIndex;
                usedLane[targetLane] = true;
            }

            while (gates.Count < 3)
            {
                var lane = -1;
                for (var i = 0; i < usedLane.Length; i++)
                {
                    if (!usedLane[i])
                    {
                        lane = i;
                        break;
                    }
                }

                if (lane < 0)
                {
                    lane = random.Next(0, 3);
                }

                usedLane[Mathf.Clamp(lane, 0, 2)] = true;
                gates.Add(CreateGateSpec(
                    lane,
                    GateOperation.Add,
                    Mathf.Max(2, addBase / 2 + random.Next(1, Mathf.Max(3, addBase / 4 + 1))),
                    false,
                    0f,
                    0f,
                    0f,
                    false,
                    2f,
                    0.5f,
                    0f));
            }

            while (gates.Count > 3)
            {
                var index = GetWorstGateIndex(gates, 1);
                gates.RemoveAt(Mathf.Clamp(index, 0, gates.Count - 1));
            }
        }

        private static int GetWorstGateIndex(List<GateSpec> gates, int levelIndex)
        {
            var worstIndex = 0;
            var worstScore = float.PositiveInfinity;
            for (var i = 0; i < gates.Count; i++)
            {
                var score = EvaluateGateBenefit(gates[i], levelIndex);
                if (score < worstScore)
                {
                    worstScore = score;
                    worstIndex = i;
                }
            }

            return worstIndex;
        }

        private static float EvaluateGateBenefit(GateSpec gate, int levelIndex)
        {
            switch (gate.operation)
            {
                case GateOperation.Add:
                    return gate.value;
                case GateOperation.Multiply:
                    return 90f + gate.value * 15f + levelIndex * 0.2f;
                case GateOperation.Subtract:
                    return -gate.value;
                case GateOperation.Divide:
                    return -70f - gate.value * 18f;
                default:
                    return 0f;
            }
        }

        private float EvaluateLaneBadChance(float baseChance, int lane, ModifierState modifier)
        {
            var laneChance = baseChance;
            if (enableAdaptiveLanePressure)
            {
                var pressure = _lanePressure[Mathf.Clamp(lane, 0, 2)];
                var neutral = 1f / 3f;
                var pressureDelta = pressure - neutral;
                var strength = lanePressureStrength * (modifier.aggressivePressure ? 1.45f : 1f);
                laneChance *= 1f + (pressureDelta * strength * 2.5f);
            }

            return Mathf.Clamp01(laneChance);
        }

        private static GateOperation PickOperationForLane(System.Random random, float badGateChance, int levelIndex)
        {
            var roll = random.NextDouble();
            if (roll < badGateChance)
            {
                return random.NextDouble() < 0.55 ? GateOperation.Subtract : GateOperation.Divide;
            }

            var multiplyChance = Mathf.Clamp01(0.04f + levelIndex * 0.0015f);
            return random.NextDouble() < multiplyChance ? GateOperation.Multiply : GateOperation.Add;
        }

        private static int PickGateValue(System.Random random, GateOperation operation, int addBase, int subtractBase, int levelIndex, bool biasHarder)
        {
            switch (operation)
            {
                case GateOperation.Add:
                {
                    var variance = Mathf.Max(1, addBase / 2);
                    var bonus = biasHarder ? random.Next(0, Mathf.Max(1, addBase / 4 + 1)) : 0;
                    return Mathf.Max(1, addBase + random.Next(-2, variance + 1) + bonus);
                }
                case GateOperation.Subtract:
                {
                    var variance = Mathf.Max(1, subtractBase / 2);
                    var bonus = biasHarder ? random.Next(1, Mathf.Max(2, subtractBase / 3 + 2)) : 0;
                    return Mathf.Max(1, subtractBase + random.Next(0, variance + 2) + bonus);
                }
                case GateOperation.Multiply:
                {
                    if (levelIndex > 36 && random.NextDouble() < 0.06)
                    {
                        return 3;
                    }

                    return 2;
                }
                case GateOperation.Divide:
                {
                    if (levelIndex > 24 && random.NextDouble() < (biasHarder ? 0.12 : 0.06))
                    {
                        return 3;
                    }

                    return 2;
                }
                default:
                    return 1;
            }
        }

        private static bool IsPositiveMultiplier(GateSpec gate)
        {
            return gate.operation == GateOperation.Multiply && gate.value > 1;
        }

        private static int ComputeMultiplierBudget(int levelIndex, DifficultyMode mode, bool isMiniBoss, int rowCount)
        {
            var safeRows = Mathf.Max(1, rowCount);
            var safeLevel = Mathf.Max(1, levelIndex);
            int baseBudget;
            int growthDivisor;
            switch (mode)
            {
                case DifficultyMode.Easy:
                    baseBudget = 1;
                    growthDivisor = 28;
                    break;
                case DifficultyMode.Hard:
                    baseBudget = 3;
                    growthDivisor = 18;
                    break;
                default:
                    baseBudget = 2;
                    growthDivisor = 22;
                    break;
            }

            var budget = baseBudget + Mathf.FloorToInt((safeLevel - 1) / Mathf.Max(1f, growthDivisor));
            if (isMiniBoss)
            {
                budget += 1;
            }

            var rowCap = Mathf.Max(1, safeRows / 4);
            return Mathf.Clamp(budget, 1, rowCap);
        }

        private static void ApplyMultiplierBudget(
            System.Random random,
            int levelIndex,
            int addBase,
            DifficultyMode mode,
            bool isMiniBoss,
            List<GateRow> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            var budget = ComputeMultiplierBudget(levelIndex, mode, isMiniBoss, rows.Count);
            var kept = 0;

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.gates == null || row.gates.Count == 0)
                {
                    continue;
                }

                for (var gateIndex = 0; gateIndex < row.gates.Count; gateIndex++)
                {
                    var gate = row.gates[gateIndex];
                    if (!IsPositiveMultiplier(gate))
                    {
                        continue;
                    }

                    var keepThisMultiplier = false;
                    if (kept < budget)
                    {
                        if (gate.pickTier == GatePickTier.BetterGood)
                        {
                            keepThisMultiplier = true;
                        }
                        else if (gate.pickTier == GatePickTier.WorseGood &&
                                 kept < Mathf.Max(1, budget - 1) &&
                                 random.NextDouble() < 0.24)
                        {
                            keepThisMultiplier = true;
                        }
                    }

                    if (keepThisMultiplier)
                    {
                        kept++;
                        continue;
                    }

                    gate.operation = GateOperation.Add;
                    var bonus = gate.pickTier == GatePickTier.BetterGood ? 4 : 2;
                    gate.value = Mathf.Clamp(
                        addBase + bonus + random.Next(0, 3),
                        2,
                        addBase + bonus + 4);
                    row.gates[gateIndex] = gate;
                }

                rows[rowIndex] = row;
            }
        }

        private int BuildEnemyCount(
            int levelIndex,
            int startCount,
            int expectedBest,
            int referenceRouteCount,
            bool isMiniBoss,
            ModifierState modifier,
            DifficultyMode difficultyMode)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            var safeStart = Mathf.Max(1, startCount);
            var safeExpectedBest = Mathf.Max(safeStart + 2, expectedBest);
            var safeReference = Mathf.Clamp(referenceRouteCount, safeStart + 2, safeExpectedBest);
            var linearTarget = enemyFormulaBase + Mathf.RoundToInt((safeLevel - 1) * enemyFormulaLinear * 0.9f);
            var curvePower = Mathf.Max(1.01f, enemyFormulaPower * 0.88f);
            var curvatureBoost = Mathf.RoundToInt(Mathf.Pow(safeLevel, curvePower) * enemyFormulaPowerMultiplier * 0.11f);
            var formulaTarget = linearTarget + curvatureBoost;

            float blendToBest;
            float pressure;
            float floorFractionOfBest;
            float ceilingFractionOfBest;
            switch (difficultyMode)
            {
                case DifficultyMode.Easy:
                    blendToBest = 0.18f;
                    pressure = 0.84f;
                    floorFractionOfBest = 0.32f;
                    ceilingFractionOfBest = 0.88f;
                    break;
                case DifficultyMode.Hard:
                    blendToBest = 0.5f;
                    pressure = 0.96f;
                    floorFractionOfBest = 0.62f;
                    ceilingFractionOfBest = 0.97f;
                    break;
                default:
                    blendToBest = 0.32f;
                    pressure = 0.91f;
                    floorFractionOfBest = 0.48f;
                    ceilingFractionOfBest = 0.93f;
                    break;
            }

            if (isMiniBoss)
            {
                pressure += 0.02f;
                floorFractionOfBest += 0.04f;
                ceilingFractionOfBest += 0.015f;
                formulaTarget = Mathf.RoundToInt(formulaTarget * 1.08f) + 8;
            }

            if (modifier.tankSurge)
            {
                formulaTarget += Mathf.RoundToInt(levelIndex * 0.14f);
            }

            var routeBlend = Mathf.Lerp(safeReference, safeExpectedBest, Mathf.Clamp01(blendToBest));
            var skillTarget = Mathf.RoundToInt(routeBlend * Mathf.Clamp(pressure, 0.75f, 1.02f));
            var baseline = Mathf.Max(safeStart + 2, formulaTarget);

            var floorByBest = Mathf.FloorToInt(safeExpectedBest * Mathf.Clamp(floorFractionOfBest, 0.2f, 0.92f));
            var enemyFloor = Mathf.Max(baseline, floorByBest);
            var ceilingConfig = Mathf.Min(
                Mathf.Clamp01(enemyMaxFractionOfBestPath + (isMiniBoss ? 0.04f : 0f)),
                Mathf.Clamp(ceilingFractionOfBest, 0.5f, 0.99f));
            var enemyCeiling = Mathf.Max(enemyFloor + 1, Mathf.FloorToInt(safeExpectedBest * ceilingConfig));
            enemyCeiling = Mathf.Min(enemyCeiling, safeExpectedBest - 1);
            if (enemyCeiling <= enemyFloor)
            {
                enemyCeiling = Mathf.Min(safeExpectedBest - 1, enemyFloor + 1);
            }

            var target = Mathf.Max(baseline, skillTarget);
            var enemyCount = Mathf.Clamp(target, enemyFloor, enemyCeiling);
            var playableCeiling = Mathf.Max(1, safeExpectedBest - 1);
            return Mathf.Clamp(enemyCount, 1, playableCeiling);
        }

        private int BuildTankRequirement(int levelIndex, int expectedBest, int enemyCount, bool isMiniBoss, ModifierState modifier)
        {
            if (levelIndex < 10 && !isMiniBoss)
            {
                return 0;
            }

            if (expectedBest <= enemyCount + 1)
            {
                return 0;
            }

            var ratio = 0.88f + Mathf.Clamp(levelIndex * 0.0022f, 0f, 0.14f);
            if (isMiniBoss)
            {
                ratio += 0.1f;
            }

            if (modifier.tankSurge)
            {
                ratio += 0.08f;
            }

            var target = Mathf.RoundToInt(enemyCount * ratio);
            var minRequirement = enemyCount + (isMiniBoss ? 6 : 2);
            var maxRequirement = Mathf.Min(expectedBest - 1, Mathf.FloorToInt(expectedBest * (isMiniBoss ? 0.96f : 0.9f)));
            if (maxRequirement <= minRequirement)
            {
                return 0;
            }

            var tankRequirement = Mathf.Clamp(target, minRequirement, maxRequirement);
            if (tankRequirement <= enemyCount + 2 && !isMiniBoss && levelIndex < 24)
            {
                return 0;
            }

            return tankRequirement;
        }

        private static int EstimateBestCaseCount(int startCount, List<GateRow> rows)
        {
            var running = Mathf.Max(1, startCount);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var bestAfterRow = 1;
                for (var j = 0; j < row.gates.Count; j++)
                {
                    var gate = row.gates[j];
                    var after = ApplyOperation(running, gate.operation, gate.value);
                    if (after > bestAfterRow)
                    {
                        bestAfterRow = after;
                    }
                }

                running = Mathf.Max(1, bestAfterRow);
            }

            return running;
        }

        private static int EstimateRouteReferenceCount(
            int startCount,
            List<GateRow> rows,
            DifficultyRouteProfile routeProfile,
            DifficultyMode mode,
            bool isMiniBoss)
        {
            if (rows == null || rows.Count == 0)
            {
                return Mathf.Max(1, startCount);
            }

            var rowCount = rows.Count;
            var targetBetter = Mathf.Clamp(routeProfile.betterRows, 0, rowCount);
            var targetWorse = Mathf.Clamp(routeProfile.worseRows, 0, rowCount - targetBetter);
            var targetRed = Mathf.Max(0, rowCount - targetBetter - targetWorse);

            var currentMin = new int[targetBetter + 1, targetWorse + 1, targetRed + 1];
            var nextMin = new int[targetBetter + 1, targetWorse + 1, targetRed + 1];
            var currentMax = new int[targetBetter + 1, targetWorse + 1, targetRed + 1];
            var nextMax = new int[targetBetter + 1, targetWorse + 1, targetRed + 1];
            FillStateGrid(currentMin, -1);
            FillStateGrid(nextMin, -1);
            FillStateGrid(currentMax, -1);
            FillStateGrid(nextMax, -1);
            var initial = Mathf.Max(1, startCount);
            currentMin[0, 0, 0] = initial;
            currentMax[0, 0, 0] = initial;

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                FillStateGrid(nextMin, -1);
                FillStateGrid(nextMax, -1);
                var row = rows[rowIndex];
                var betterGate = SelectGateForTier(row, GatePickTier.BetterGood);
                var worseGate = SelectGateForTier(row, GatePickTier.WorseGood);
                var redGate = SelectGateForTier(row, GatePickTier.RedBad);

                for (var betterUsed = 0; betterUsed <= targetBetter; betterUsed++)
                {
                    for (var worseUsed = 0; worseUsed <= targetWorse; worseUsed++)
                    {
                        for (var redUsed = 0; redUsed <= targetRed; redUsed++)
                        {
                            if (betterUsed + worseUsed + redUsed != rowIndex)
                            {
                                continue;
                            }

                            var sourceMin = currentMin[betterUsed, worseUsed, redUsed];
                            var sourceMax = currentMax[betterUsed, worseUsed, redUsed];
                            if (sourceMin < 0 || sourceMax < 0)
                            {
                                continue;
                            }

                            if (betterUsed < targetBetter)
                            {
                                var nextMinValue = ApplyOperation(sourceMin, betterGate.operation, betterGate.value);
                                var nextMaxValue = ApplyOperation(sourceMax, betterGate.operation, betterGate.value);
                                UpdateMinState(nextMin, betterUsed + 1, worseUsed, redUsed, nextMinValue);
                                UpdateMaxState(nextMax, betterUsed + 1, worseUsed, redUsed, nextMaxValue);
                            }

                            if (worseUsed < targetWorse)
                            {
                                var nextMinValue = ApplyOperation(sourceMin, worseGate.operation, worseGate.value);
                                var nextMaxValue = ApplyOperation(sourceMax, worseGate.operation, worseGate.value);
                                UpdateMinState(nextMin, betterUsed, worseUsed + 1, redUsed, nextMinValue);
                                UpdateMaxState(nextMax, betterUsed, worseUsed + 1, redUsed, nextMaxValue);
                            }

                            if (redUsed < targetRed)
                            {
                                var nextMinValue = ApplyOperation(sourceMin, redGate.operation, redGate.value);
                                var nextMaxValue = ApplyOperation(sourceMax, redGate.operation, redGate.value);
                                UpdateMinState(nextMin, betterUsed, worseUsed, redUsed + 1, nextMinValue);
                                UpdateMaxState(nextMax, betterUsed, worseUsed, redUsed + 1, nextMaxValue);
                            }
                        }
                    }
                }

                var minSwap = currentMin;
                currentMin = nextMin;
                nextMin = minSwap;
                var maxSwap = currentMax;
                currentMax = nextMax;
                nextMax = maxSwap;
            }

            var minReference = currentMin[targetBetter, targetWorse, targetRed];
            var maxReference = currentMax[targetBetter, targetWorse, targetRed];
            if (minReference <= 0 && maxReference <= 0)
            {
                return Mathf.Max(1, startCount);
            }

            if (minReference <= 0)
            {
                minReference = Mathf.Max(1, maxReference);
            }

            if (maxReference <= 0)
            {
                maxReference = Mathf.Max(1, minReference);
            }

            var bandHigh = Mathf.Max(minReference, maxReference);
            var bandLow = Mathf.Min(minReference, maxReference);
            float blend;
            switch (mode)
            {
                case DifficultyMode.Easy:
                    blend = 0.4f;
                    break;
                case DifficultyMode.Hard:
                    blend = 0.82f;
                    break;
                default:
                    blend = 0.6f;
                    break;
            }

            if (isMiniBoss)
            {
                blend = Mathf.Clamp01(blend + 0.06f);
            }

            var reference = Mathf.RoundToInt(Mathf.Lerp(bandLow, bandHigh, Mathf.Clamp01(blend)));
            return Mathf.Clamp(reference, bandLow, bandHigh);
        }

        private static void FillStateGrid(int[,,] grid, int value)
        {
            for (var x = 0; x < grid.GetLength(0); x++)
            {
                for (var y = 0; y < grid.GetLength(1); y++)
                {
                    for (var z = 0; z < grid.GetLength(2); z++)
                    {
                        grid[x, y, z] = value;
                    }
                }
            }
        }

        private static void UpdateMinState(int[,,] states, int better, int worse, int red, int candidate)
        {
            var existing = states[better, worse, red];
            if (existing < 0 || candidate < existing)
            {
                states[better, worse, red] = candidate;
            }
        }

        private static void UpdateMaxState(int[,,] states, int better, int worse, int red, int candidate)
        {
            var existing = states[better, worse, red];
            if (existing < 0 || candidate > existing)
            {
                states[better, worse, red] = candidate;
            }
        }

        private static GateSpec SelectGateForTier(GateRow row, GatePickTier tier)
        {
            if (row.gates != null)
            {
                for (var i = 0; i < row.gates.Count; i++)
                {
                    if (row.gates[i].pickTier == tier)
                    {
                        return row.gates[i];
                    }
                }

                if (row.gates.Count > 0)
                {
                    var fallback = row.gates[0];
                    var bestScore = EvaluateGateBenefit(fallback, 1);
                    for (var i = 1; i < row.gates.Count; i++)
                    {
                        var candidate = row.gates[i];
                        var score = EvaluateGateBenefit(candidate, 1);
                        if (tier == GatePickTier.RedBad)
                        {
                            if (score < bestScore)
                            {
                                fallback = candidate;
                                bestScore = score;
                            }
                        }
                        else if (score > bestScore)
                        {
                            fallback = candidate;
                            bestScore = score;
                        }
                    }

                    return fallback;
                }
            }

            return new GateSpec
            {
                lane = 1,
                operation = GateOperation.Add,
                value = 1,
                pickTier = tier
            };
        }

        private static int ApplyOperation(int source, GateOperation operation, int value)
        {
            var safeValue = Mathf.Max(1, value);
            long result;
            switch (operation)
            {
                case GateOperation.Add:
                    result = (long)source + safeValue;
                    break;
                case GateOperation.Subtract:
                    result = source - safeValue;
                    break;
                case GateOperation.Multiply:
                    result = (long)source * safeValue;
                    break;
                case GateOperation.Divide:
                    result = source / safeValue;
                    break;
                default:
                    result = source;
                    break;
            }

            if (result < 1L)
            {
                result = 1L;
            }
            else if (result > CountClamp)
            {
                result = CountClamp;
            }
            return (int)result;
        }

        private void DecayLanePressureTowardNeutral()
        {
            if (!enableAdaptiveLanePressure)
            {
                return;
            }

            var neutral = 1f / 3f;
            var decay = Mathf.Clamp01(lanePressureDecay);
            for (var i = 0; i < _lanePressure.Length; i++)
            {
                _lanePressure[i] = Mathf.Lerp(_lanePressure[i], neutral, decay);
            }

            NormalizeLanePressure();
        }

        private void NormalizeLanePressure()
        {
            var total = _lanePressure[0] + _lanePressure[1] + _lanePressure[2];
            if (total <= 0.0001f)
            {
                _lanePressure[0] = 1f / 3f;
                _lanePressure[1] = 1f / 3f;
                _lanePressure[2] = 1f / 3f;
                return;
            }

            var inverse = 1f / total;
            _lanePressure[0] = Mathf.Clamp(_lanePressure[0] * inverse, 0.05f, 0.9f);
            _lanePressure[1] = Mathf.Clamp(_lanePressure[1] * inverse, 0.05f, 0.9f);
            _lanePressure[2] = Mathf.Clamp(_lanePressure[2] * inverse, 0.05f, 0.9f);

            total = _lanePressure[0] + _lanePressure[1] + _lanePressure[2];
            inverse = 1f / total;
            _lanePressure[0] *= inverse;
            _lanePressure[1] *= inverse;
            _lanePressure[2] *= inverse;
        }

        private int GetMostPressuredLane()
        {
            var bestLane = 0;
            var bestValue = _lanePressure[0];
            for (var i = 1; i < _lanePressure.Length; i++)
            {
                if (_lanePressure[i] > bestValue)
                {
                    bestValue = _lanePressure[i];
                    bestLane = i;
                }
            }

            return bestLane;
        }

        private int GetLeastPressuredLane()
        {
            var bestLane = 0;
            var bestValue = _lanePressure[0];
            for (var i = 1; i < _lanePressure.Length; i++)
            {
                if (_lanePressure[i] < bestValue)
                {
                    bestValue = _lanePressure[i];
                    bestLane = i;
                }
            }

            return bestLane;
        }

        private static int[] BuildShuffledLanes(System.Random random)
        {
            var lanes = new[] { 0, 1, 2 };
            for (var i = lanes.Length - 1; i > 0; i--)
            {
                var swapIndex = random.Next(0, i + 1);
                var temp = lanes[i];
                lanes[i] = lanes[swapIndex];
                lanes[swapIndex] = temp;
            }

            return lanes;
        }

        private enum RowPattern
        {
            Normal = 0,
            Tempo = 1,
            RiskReward = 2
        }

        [Serializable]
        private struct GateSpec
        {
            public int lane;
            public GateOperation operation;
            public int value;
            public GatePickTier pickTier;
            public bool movesHorizontally;
            public float moveAmplitude;
            public float moveSpeed;
            public float movePhase;
            public bool useTempoWindow;
            public float tempoCycle;
            public float tempoOpenRatio;
            public float tempoPhase;
        }

        [Serializable]
        private struct GateRow
        {
            public float z;
            public List<GateSpec> gates;
        }

        [Serializable]
        private struct HazardSpec
        {
            public int lane;
            public float z;
            public HazardType type;
            public float unitLossFraction;
            public int flatLoss;
            public float knockbackDeltaX;
            public float width;
            public float depth;
            public bool emphasize;
        }

        private struct ModifierState
        {
            public bool forceMovingGates;
            public bool boostTempoRows;
            public bool hazardRush;
            public bool aggressivePressure;
            public bool tankSurge;
            public string label;
        }

        private struct GeneratedLevel
        {
            public int startCount;
            public int enemyCount;
            public int tankRequirement;
            public int referenceRouteCount;
            public int referenceBetterRows;
            public int referenceWorseRows;
            public int referenceRedRows;
            public int totalRows;
            public float finishZ;
            public float forwardSpeed;
            public float gateDifficulty01;
            public bool isMiniBoss;
            public string modifierName;
            public List<GateRow> rows;
            public List<HazardSpec> hazards;
        }

        private struct ThemeDefinition
        {
            public string name;
            public Color trackColor;
            public Color stripeColor;
            public Color railColor;
            public Color backdropColor;
            public Color cloudColor;
            public Color beaconPoleColor;
            public Color beaconCoreColor;
            public Color gatePositiveColor;
            public Color gateNegativeColor;
            public Color hazardSlowColor;
            public Color hazardKnockbackColor;
            public Color skyTint;
            public Color groundTint;
            public Color fogColor;
            public float fogDensity;
            public float skyExposure;
            public float atmosphereThickness;
            public Color ambientSky;
            public Color ambientEquator;
            public Color ambientGround;
            public Color sunColor;
            public float sunIntensity;

            public static ThemeDefinition MetroDawn()
            {
                return new ThemeDefinition
                {
                    name = "Metro Dawn",
                    trackColor = new Color(0.22f, 0.23f, 0.29f, 1f),
                    stripeColor = new Color(0.96f, 0.78f, 0.28f, 1f),
                    railColor = new Color(0.11f, 0.14f, 0.19f, 1f),
                    backdropColor = new Color(0.28f, 0.33f, 0.42f, 1f),
                    cloudColor = new Color(0.95f, 0.98f, 1f, 0.9f),
                    beaconPoleColor = new Color(0.08f, 0.12f, 0.18f, 1f),
                    beaconCoreColor = new Color(0.38f, 0.9f, 1f, 1f),
                    gatePositiveColor = new Color(0.26f, 0.92f, 0.42f, 1f),
                    gateNegativeColor = new Color(0.96f, 0.35f, 0.38f, 1f),
                    hazardSlowColor = new Color(0.98f, 0.78f, 0.24f, 1f),
                    hazardKnockbackColor = new Color(0.95f, 0.36f, 0.24f, 1f),
                    skyTint = new Color(0.45f, 0.66f, 0.9f, 1f),
                    groundTint = new Color(0.83f, 0.89f, 0.94f, 1f),
                    fogColor = new Color(0.7f, 0.8f, 0.92f, 1f),
                    fogDensity = 0.0058f,
                    skyExposure = 1.16f,
                    atmosphereThickness = 0.84f,
                    ambientSky = new Color(0.62f, 0.76f, 0.92f, 1f),
                    ambientEquator = new Color(0.37f, 0.46f, 0.58f, 1f),
                    ambientGround = new Color(0.2f, 0.24f, 0.3f, 1f),
                    sunColor = new Color(1f, 0.95f, 0.86f, 1f),
                    sunIntensity = 1.34f
                };
            }

            public static ThemeDefinition SunsetDunes()
            {
                return new ThemeDefinition
                {
                    name = "Sunset Dunes",
                    trackColor = new Color(0.29f, 0.19f, 0.16f, 1f),
                    stripeColor = new Color(1f, 0.74f, 0.33f, 1f),
                    railColor = new Color(0.17f, 0.12f, 0.11f, 1f),
                    backdropColor = new Color(0.52f, 0.39f, 0.31f, 1f),
                    cloudColor = new Color(1f, 0.91f, 0.84f, 0.9f),
                    beaconPoleColor = new Color(0.18f, 0.11f, 0.1f, 1f),
                    beaconCoreColor = new Color(1f, 0.72f, 0.38f, 1f),
                    gatePositiveColor = new Color(0.3f, 0.92f, 0.48f, 1f),
                    gateNegativeColor = new Color(0.98f, 0.32f, 0.3f, 1f),
                    hazardSlowColor = new Color(1f, 0.83f, 0.3f, 1f),
                    hazardKnockbackColor = new Color(0.97f, 0.39f, 0.24f, 1f),
                    skyTint = new Color(0.83f, 0.56f, 0.4f, 1f),
                    groundTint = new Color(0.95f, 0.82f, 0.7f, 1f),
                    fogColor = new Color(0.84f, 0.65f, 0.53f, 1f),
                    fogDensity = 0.005f,
                    skyExposure = 1.2f,
                    atmosphereThickness = 0.95f,
                    ambientSky = new Color(0.88f, 0.66f, 0.5f, 1f),
                    ambientEquator = new Color(0.57f, 0.38f, 0.3f, 1f),
                    ambientGround = new Color(0.28f, 0.18f, 0.13f, 1f),
                    sunColor = new Color(1f, 0.84f, 0.63f, 1f),
                    sunIntensity = 1.26f
                };
            }

            public static ThemeDefinition AuroraIce()
            {
                return new ThemeDefinition
                {
                    name = "Aurora Ice",
                    trackColor = new Color(0.15f, 0.21f, 0.28f, 1f),
                    stripeColor = new Color(0.69f, 0.94f, 1f, 1f),
                    railColor = new Color(0.11f, 0.16f, 0.24f, 1f),
                    backdropColor = new Color(0.31f, 0.43f, 0.54f, 1f),
                    cloudColor = new Color(0.92f, 0.98f, 1f, 0.9f),
                    beaconPoleColor = new Color(0.08f, 0.16f, 0.22f, 1f),
                    beaconCoreColor = new Color(0.48f, 1f, 0.94f, 1f),
                    gatePositiveColor = new Color(0.32f, 1f, 0.62f, 1f),
                    gateNegativeColor = new Color(1f, 0.38f, 0.54f, 1f),
                    hazardSlowColor = new Color(0.88f, 0.95f, 1f, 1f),
                    hazardKnockbackColor = new Color(0.95f, 0.47f, 0.56f, 1f),
                    skyTint = new Color(0.37f, 0.62f, 0.87f, 1f),
                    groundTint = new Color(0.77f, 0.9f, 0.96f, 1f),
                    fogColor = new Color(0.67f, 0.83f, 0.93f, 1f),
                    fogDensity = 0.0048f,
                    skyExposure = 1.14f,
                    atmosphereThickness = 0.78f,
                    ambientSky = new Color(0.61f, 0.81f, 0.94f, 1f),
                    ambientEquator = new Color(0.33f, 0.5f, 0.6f, 1f),
                    ambientGround = new Color(0.16f, 0.25f, 0.32f, 1f),
                    sunColor = new Color(0.9f, 0.97f, 1f, 1f),
                    sunIntensity = 1.28f
                };
            }

            public static ThemeDefinition VolcanicRift()
            {
                return new ThemeDefinition
                {
                    name = "Volcanic Rift",
                    trackColor = new Color(0.2f, 0.14f, 0.14f, 1f),
                    stripeColor = new Color(1f, 0.5f, 0.18f, 1f),
                    railColor = new Color(0.14f, 0.1f, 0.1f, 1f),
                    backdropColor = new Color(0.4f, 0.24f, 0.2f, 1f),
                    cloudColor = new Color(0.84f, 0.82f, 0.78f, 0.9f),
                    beaconPoleColor = new Color(0.16f, 0.1f, 0.09f, 1f),
                    beaconCoreColor = new Color(1f, 0.43f, 0.16f, 1f),
                    gatePositiveColor = new Color(0.36f, 1f, 0.47f, 1f),
                    gateNegativeColor = new Color(1f, 0.27f, 0.2f, 1f),
                    hazardSlowColor = new Color(1f, 0.62f, 0.14f, 1f),
                    hazardKnockbackColor = new Color(1f, 0.3f, 0.16f, 1f),
                    skyTint = new Color(0.42f, 0.27f, 0.24f, 1f),
                    groundTint = new Color(0.58f, 0.42f, 0.34f, 1f),
                    fogColor = new Color(0.46f, 0.31f, 0.28f, 1f),
                    fogDensity = 0.0064f,
                    skyExposure = 1.1f,
                    atmosphereThickness = 1.06f,
                    ambientSky = new Color(0.5f, 0.36f, 0.32f, 1f),
                    ambientEquator = new Color(0.35f, 0.22f, 0.18f, 1f),
                    ambientGround = new Color(0.18f, 0.11f, 0.1f, 1f),
                    sunColor = new Color(1f, 0.76f, 0.54f, 1f),
                    sunIntensity = 1.24f
                };
            }

            public static ThemeDefinition EmeraldVista()
            {
                return new ThemeDefinition
                {
                    name = "Emerald Vista",
                    trackColor = new Color(0.16f, 0.23f, 0.2f, 1f),
                    stripeColor = new Color(0.98f, 0.9f, 0.33f, 1f),
                    railColor = new Color(0.1f, 0.16f, 0.14f, 1f),
                    backdropColor = new Color(0.26f, 0.39f, 0.31f, 1f),
                    cloudColor = new Color(0.95f, 1f, 0.94f, 0.9f),
                    beaconPoleColor = new Color(0.09f, 0.18f, 0.14f, 1f),
                    beaconCoreColor = new Color(0.51f, 1f, 0.62f, 1f),
                    gatePositiveColor = new Color(0.34f, 1f, 0.52f, 1f),
                    gateNegativeColor = new Color(0.99f, 0.39f, 0.3f, 1f),
                    hazardSlowColor = new Color(0.98f, 0.9f, 0.29f, 1f),
                    hazardKnockbackColor = new Color(0.95f, 0.42f, 0.24f, 1f),
                    skyTint = new Color(0.49f, 0.76f, 0.61f, 1f),
                    groundTint = new Color(0.74f, 0.9f, 0.76f, 1f),
                    fogColor = new Color(0.62f, 0.8f, 0.68f, 1f),
                    fogDensity = 0.0052f,
                    skyExposure = 1.18f,
                    atmosphereThickness = 0.86f,
                    ambientSky = new Color(0.64f, 0.85f, 0.72f, 1f),
                    ambientEquator = new Color(0.34f, 0.52f, 0.42f, 1f),
                    ambientGround = new Color(0.17f, 0.28f, 0.22f, 1f),
                    sunColor = new Color(0.98f, 0.95f, 0.8f, 1f),
                    sunIntensity = 1.31f
                };
            }

            public static ThemeDefinition NeoNight()
            {
                return new ThemeDefinition
                {
                    name = "Neo Night",
                    trackColor = new Color(0.1f, 0.11f, 0.18f, 1f),
                    stripeColor = new Color(0.53f, 0.84f, 1f, 1f),
                    railColor = new Color(0.08f, 0.1f, 0.15f, 1f),
                    backdropColor = new Color(0.18f, 0.24f, 0.34f, 1f),
                    cloudColor = new Color(0.72f, 0.82f, 0.94f, 0.9f),
                    beaconPoleColor = new Color(0.07f, 0.1f, 0.16f, 1f),
                    beaconCoreColor = new Color(0.59f, 0.78f, 1f, 1f),
                    gatePositiveColor = new Color(0.35f, 0.98f, 0.77f, 1f),
                    gateNegativeColor = new Color(1f, 0.35f, 0.7f, 1f),
                    hazardSlowColor = new Color(0.62f, 0.88f, 1f, 1f),
                    hazardKnockbackColor = new Color(0.97f, 0.39f, 0.68f, 1f),
                    skyTint = new Color(0.18f, 0.23f, 0.42f, 1f),
                    groundTint = new Color(0.23f, 0.28f, 0.38f, 1f),
                    fogColor = new Color(0.2f, 0.28f, 0.44f, 1f),
                    fogDensity = 0.0068f,
                    skyExposure = 0.98f,
                    atmosphereThickness = 1.1f,
                    ambientSky = new Color(0.24f, 0.36f, 0.54f, 1f),
                    ambientEquator = new Color(0.16f, 0.23f, 0.36f, 1f),
                    ambientGround = new Color(0.09f, 0.12f, 0.2f, 1f),
                    sunColor = new Color(0.68f, 0.78f, 1f, 1f),
                    sunIntensity = 1.02f
                };
            }
        }
    }
}
