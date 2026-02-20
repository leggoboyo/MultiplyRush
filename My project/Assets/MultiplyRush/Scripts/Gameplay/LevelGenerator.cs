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
        public float levelLengthMultiplier = 1.5f;
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
        public float gateWidthAtStart = 2.15f;
        public float gateWidthAtHighDifficulty = 1.3f;
        public float panelWidthAtStart = 2.2f;
        public float panelWidthAtHighDifficulty = 1.6f;
        [Range(0f, 1f)]
        public float movingGateChanceAtStart = 0f;
        [Range(0f, 1f)]
        public float movingGateChanceAtHighDifficulty = 0.75f;
        public float movingGateAmplitudeAtStart = 0.12f;
        public float movingGateAmplitudeAtHighDifficulty = 1.05f;
        public float movingGateSpeedAtStart = 1.1f;
        public float movingGateSpeedAtHighDifficulty = 2.8f;

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
        public float slowHazardMultiplier = 0.74f;
        public float slowHazardDuration = 1.45f;
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
        private readonly List<HazardZone> _activeHazards = new List<HazardZone>(64);
        private readonly Stack<HazardZone> _hazardPool = new Stack<HazardZone>(64);
        private readonly List<float> _cloudSpeeds = new List<float>(64);
        private readonly List<float> _cloudMinX = new List<float>(64);
        private readonly List<float> _cloudMaxX = new List<float>(64);
        private readonly List<float> _cloudBaseY = new List<float>(64);
        private readonly List<float> _cloudPhases = new List<float>(64);
        private readonly float[] _lanePressure = { 0.34f, 0.32f, 0.34f };

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
        private Transform _trackDecorRoot;
        private Transform _backdropRoot;
        private Transform _cloudRoot;
        private Transform _beaconRoot;
        private Transform _hazardRoot;
        private Transform _leftRail;
        private Transform _rightRail;
        private Material _stripeMaterial;
        private Material _railMaterial;
        private Material _backdropMaterial;
        private Material _cloudMaterial;
        private Material _beaconPoleMaterial;
        private Material _beaconCoreMaterial;
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
            var safeLevel = Mathf.Max(1, levelIndex);
            _activeLevelIndex = safeLevel;
            _effectiveLaneSpacing = Mathf.Max(laneSpacing, minLaneSpacing);
            _effectiveTrackHalfWidth = Mathf.Max(trackHalfWidth, _effectiveLaneSpacing + laneToEdgePadding);
            ApplyGraphicsQualitySettings(backdropQuality);
            DecayLanePressureTowardNeutral();
            EnsureRoots();
            PrewarmGatePool();
            PrewarmHazardPool();
            var generated = BuildDefinition(safeLevel);
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

        private void Update()
        {
            AnimateClouds(Time.deltaTime);
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
        }

        private void BuildBackdrop(float trackLength, float effectiveTrackHalfWidth)
        {
            if (_backdropRoot == null || _cloudRoot == null)
            {
                return;
            }

            var density = GetBackdropDensityMultiplier();
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

                for (var side = -1; side <= 1; side += 2)
                {
                    for (var z = zStart; z <= zEnd; z += spacing)
                    {
                        var block = GetBackdropBlock();
                        var width = Mathf.Lerp(minWidth, maxWidth, (float)random.NextDouble());
                        var height = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble()) * Mathf.Lerp(0.92f, 1.14f, Mathf.Clamp01(density - 0.5f));
                        var depth = Mathf.Lerp(minDepth, maxDepth, (float)random.NextDouble());
                        var zJitter = ((float)random.NextDouble() * 2f - 1f) * spacing * 0.36f;
                        var xJitter = ((float)random.NextDouble() * 2f - 1f) * 1.25f;
                        block.position = new Vector3(side * (sideX + xJitter + width * 0.4f), (height * 0.5f) - 0.2f, z + zJitter);
                        block.rotation = Quaternion.identity;
                        block.localScale = new Vector3(width, height, depth);
                        block.gameObject.SetActive(true);
                        _activeBackdropBlocks.Add(block);
                    }
                }
            }

            if (!enableClouds)
            {
                return;
            }

            PrewarmCloudPool();
            var cloudCount = Mathf.RoundToInt(Mathf.Lerp(8f, 20f, Mathf.Clamp01((density - 0.5f) / 0.75f)));
            cloudCount = Mathf.Clamp(cloudCount, 6, 26);
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
                var baseScale = Mathf.Lerp(2.5f, 5.8f, (float)random.NextDouble());
                cloud.position = new Vector3(x, y, z);
                cloud.rotation = Quaternion.identity;
                cloud.localScale = new Vector3(baseScale * 1.9f, baseScale * 0.56f, baseScale);
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
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "BackdropBlock";
            block.transform.SetParent(_backdropRoot, false);
            var collider = block.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = block.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetBackdropMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            block.SetActive(false);
            return block.transform;
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

        private Transform CreateCloud()
        {
            var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "BackdropCloud";
            cloud.transform.SetParent(_cloudRoot, false);
            var collider = cloud.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = cloud.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetCloudMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            cloud.SetActive(false);
            return cloud.transform;
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

            var count = Mathf.Max(0, backdropPoolSize);
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

            var count = Mathf.Max(0, cloudPoolSize);
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
            cloudColor.a = 0.9f;
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
            var hitboxWidth = Mathf.Lerp(gateWidthAtStart, gateWidthAtHighDifficulty, gateDifficulty01);
            var panelWidth = Mathf.Lerp(panelWidthAtStart, panelWidthAtHighDifficulty, gateDifficulty01);
            var edgePadding = 0.95f;
            var trackMinX = -_effectiveTrackHalfWidth + edgePadding;
            var trackMaxX = _effectiveTrackHalfWidth - edgePadding;

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
                hazard.slowColor = _hazardSlowColor;
                hazard.knockbackColor = _hazardKnockbackColor;
                var laneX = Mathf.Clamp(LaneToX(spec.lane), minX, maxX);
                var worldPosition = new Vector3(laneX, -0.01f, spec.z);
                var worldScale = new Vector3(spec.width, 0.04f, spec.depth);
                hazard.Configure(
                    spec.type,
                    spec.slowMultiplier,
                    spec.duration,
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
            label.text = "SLOW";
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

        private GeneratedLevel BuildDefinition(int levelIndex)
        {
            var random = new System.Random(9143 + levelIndex * 101);
            var gateDifficulty01 = EvaluateGateDifficulty(levelIndex);
            var isMiniBoss = miniBossEveryLevels > 0 && levelIndex % Mathf.Max(1, miniBossEveryLevels) == 0;
            var modifier = BuildModifierState(levelIndex);
            var themeName = ResolveTheme(levelIndex).name;

            var generated = new GeneratedLevel
            {
                startCount = Mathf.Clamp(baseStartCount + (levelIndex / 2) + (isMiniBoss ? 3 : 0), baseStartCount, 120),
                forwardSpeed = CalculateForwardSpeed(levelIndex),
                isMiniBoss = isMiniBoss,
                modifierName = themeName + " • " + modifier.label
            };

            var rowCount = isMiniBoss
                ? Mathf.Clamp(15 + (levelIndex / 4), 15, 46)
                : Mathf.Clamp(8 + levelIndex, 8, 40);
            generated.totalRows = rowCount;
            var effectiveRowSpacing = rowSpacing * Mathf.Max(1f, levelLengthMultiplier) * (isMiniBoss ? 1.08f : 1f);
            var baseBadGateChance = Mathf.Clamp01(0.16f + levelIndex * 0.012f + (isMiniBoss ? 0.05f : 0f));
            var addBase = 4 + Mathf.FloorToInt(levelIndex * 1.6f);
            var subtractBase = 3 + Mathf.FloorToInt(levelIndex * 1.2f);

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
                        var hazardType = random.NextDouble() < 0.64 ? HazardType.SlowField : HazardType.KnockbackStrip;
                        var sideSign = lane == 0 ? 1f : lane == 2 ? -1f : (random.NextDouble() < 0.5 ? -1f : 1f);
                        generated.hazards.Add(new HazardSpec
                        {
                            lane = lane,
                            z = hazardZ,
                            type = hazardType,
                            slowMultiplier = Mathf.Clamp(
                                slowHazardMultiplier - (gateDifficulty01 * 0.08f) - (modifier.hazardRush ? 0.06f : 0f),
                                0.45f,
                                0.9f),
                            duration = slowHazardDuration + (gateDifficulty01 * 0.4f) + (modifier.hazardRush ? 0.3f : 0f),
                            knockbackDeltaX = knockbackHazardStrength * sideSign * (modifier.hazardRush ? 1.2f : 1f),
                            width = Mathf.Max(1.1f, hazardWidth * (isMiniBoss ? 1.08f : 1f)),
                            depth = Mathf.Max(1f, hazardDepth * (isMiniBoss ? 1.1f : 1f)),
                            emphasize = isMiniBoss || modifier.hazardRush
                        });
                        lastHazardZ = hazardZ;
                    }
                }
            }

            generated.finishZ = startZ + (rowCount * effectiveRowSpacing) + endPadding;
            var expectedBest = EstimateBestCaseCount(generated.startCount, generated.rows);
            generated.enemyCount = BuildEnemyCount(levelIndex, generated.startCount, expectedBest, isMiniBoss, modifier);
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
                    operation = random.NextDouble() < 0.16 && levelIndex > 12 ? GateOperation.Multiply : GateOperation.Add;
                    value = operation == GateOperation.Multiply
                        ? 2
                        : Mathf.Max(2, addBase / 2 + random.Next(0, Mathf.Max(2, addBase / 3)));
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
                    var multiplier = 3;
                    if (levelIndex > 20 && random.NextDouble() < 0.26)
                    {
                        multiplier = 4;
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

                var safeValue = Mathf.Max(2, addBase / 2 + random.Next(1, Mathf.Max(3, addBase / 3 + 1)));
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
                        gate.value = levelIndex > 18 && random.NextDouble() < 0.28 ? 3 : 2;
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
                    gate.value = Mathf.Max(2, Mathf.Max(gate.value, addBase / 2 + random.Next(1, Mathf.Max(3, addBase / 3 + 1))));
                }
                else if (gate.operation == GateOperation.Multiply)
                {
                    gate.value = Mathf.Clamp(gate.value, 2, levelIndex > 24 && random.NextDouble() < 0.14 ? 3 : 2);
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
            if (betterGate.operation == GateOperation.Add && random.NextDouble() < 0.35 + Mathf.Clamp01(levelIndex / 60f) * 0.3f)
            {
                betterGate.operation = GateOperation.Multiply;
                betterGate.value = levelIndex > 26 && random.NextDouble() < 0.2 ? 3 : 2;
            }
            else if (betterGate.operation == GateOperation.Add)
            {
                betterGate.value = Mathf.Max(betterGate.value, addBase + random.Next(4, Mathf.Max(6, addBase / 2 + 6)));
            }
            else if (betterGate.operation == GateOperation.Multiply)
            {
                betterGate.value = Mathf.Clamp(betterGate.value, 2, levelIndex > 26 && random.NextDouble() < 0.2 ? 3 : 2);
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
                    Mathf.Max(2, addBase / 2 + random.Next(1, Mathf.Max(3, addBase / 3 + 1))),
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

            var multiplyChance = Mathf.Clamp01(0.2f + levelIndex * 0.005f);
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
                    if (levelIndex > 20 && random.NextDouble() < 0.18)
                    {
                        return 3;
                    }

                    return 2;
                }
                case GateOperation.Divide:
                {
                    if (levelIndex > 15 && random.NextDouble() < (biasHarder ? 0.2 : 0.1))
                    {
                        return 3;
                    }

                    return 2;
                }
                default:
                    return 1;
            }
        }

        private int BuildEnemyCount(int levelIndex, int startCount, int expectedBest, bool isMiniBoss, ModifierState modifier)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            var linearTarget = enemyFormulaBase + Mathf.RoundToInt((safeLevel - 1) * enemyFormulaLinear);
            var curvatureBoost = Mathf.RoundToInt(Mathf.Sqrt(safeLevel) * enemyFormulaPowerMultiplier * 1.35f);
            var formulaTarget = linearTarget + curvatureBoost;

            if (isMiniBoss)
            {
                formulaTarget = Mathf.RoundToInt(formulaTarget * 1.16f) + 10;
            }

            if (modifier.tankSurge)
            {
                formulaTarget += Mathf.RoundToInt(levelIndex * 0.32f);
            }

            var linearFloor = enemyFormulaBase + Mathf.RoundToInt((safeLevel - 1) * enemyFormulaLinear * 0.7f);
            var floor = Mathf.Max(startCount + 4, linearFloor);
            if (isMiniBoss)
            {
                floor = Mathf.Max(floor, startCount + 16);
            }

            var bossCeilingBonus = isMiniBoss ? 0.05f : 0.01f;
            var capByBestPath = Mathf.FloorToInt(expectedBest * Mathf.Clamp01(enemyMaxFractionOfBestPath + bossCeilingBonus));
            capByBestPath = Mathf.Max(startCount + 5, capByBestPath);
            var enemyCount = Mathf.Clamp(formulaTarget, floor, Mathf.Max(floor, capByBestPath));

            if (enemyCount >= expectedBest)
            {
                enemyCount = Mathf.Max(startCount + 1, expectedBest - 1);
            }

            return Mathf.Max(5, enemyCount);
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

        private static int ApplyOperation(int source, GateOperation operation, int value)
        {
            var safeValue = Mathf.Max(1, value);
            switch (operation)
            {
                case GateOperation.Add:
                    return source + safeValue;
                case GateOperation.Subtract:
                    return Mathf.Max(1, source - safeValue);
                case GateOperation.Multiply:
                    return source * safeValue;
                case GateOperation.Divide:
                    return Mathf.Max(1, source / safeValue);
                default:
                    return source;
            }
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
            public float slowMultiplier;
            public float duration;
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
