using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrowdController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Movement")]
        public float dragSensitivity = 16f;
        public float xSmoothSpeed = 18f;
        public float trackHalfWidth = 4f;

        [Header("Count")]
        public int minCount = 1;
        public int maxVisibleUnits = 150;
        public int initialPoolSize = 60;

        [Header("Formation")]
        public Transform formationRoot;
        public GameObject soldierUnitPrefab;
        public float unitSpacingX = 0.55f;
        public float unitSpacingZ = 0.62f;
        public int maxColumns = 12;
        public float unitYOffset = 0f;

        [Header("Animation")]
        public float formationLerpSpeed = 18f;
        public float runBobAmplitude = 0.08f;
        public float runBobFrequency = 9.2f;
        public float unitTiltDegrees = 6f;
        public float leaderBobAmplitude = 0.1f;
        public float leaderScalePulse = 0.04f;
        public float leaderStrafeTilt = 0.9f;
        public float gatePunchScale = 1.08f;
        public float gatePunchDuration = 0.16f;

        [Header("Gate FX")]
        public bool enableGateEffects = true;
        [Range(4, 28)]
        public int gateBurstCount = 12;
        public float gateBurstSpeed = 3.8f;
        public float gateAuraDuration = 0.22f;
        public float gateAuraMaxScale = 2.2f;
        public float gateAuraHeight = 0.03f;

        [Header("Weapon VFX")]
        public bool enableWeaponVfx = true;
        public float baseShotsPerSecond = 8f;
        public float shotsPerVisibleUnit = 0.34f;
        public float maxShotsPerSecond = 38f;
        public float tracerSpeed = 35f;
        public float tracerLifetime = 0.24f;
        public float tracerSpread = 0.1f;
        public float tracerMuzzleJitter = 0.07f;

        [Header("Gate Rules")]
        public bool allowOnlyOneGatePerRow = true;

        [Header("Status Effects")]
        public float minSpeedMultiplier = 0.55f;

        [Header("References")]
        public TouchDragInput dragInput;

        private readonly List<Transform> _activeUnits = new List<Transform>(160);
        private readonly Stack<Transform> _unitPool = new Stack<Transform>(160);
        private readonly List<Vector3> _formationSlots = new List<Vector3>(160);
        private readonly List<float> _unitPhaseOffsets = new List<float>(160);

        private Transform _poolRoot;
        private Transform _leaderVisual;
        private Vector3 _leaderBaseScale = Vector3.one;
        private Vector3 _leaderBaseLocalPosition = new Vector3(0f, 0.55f, 0f);
        private bool _isRunning;
        private bool _finishTriggered;
        private float _targetX;
        private float _forwardSpeed;
        private float _finishZ = 1f;
        private int _count;
        private float _progress01;
        private float _smoothedStrafeVelocity;
        private bool _hasLastX;
        private float _lastX;
        private float _leaderTilt;
        private float _gatePunchTimer;
        private int _lastConsumedGateRow = -1;
        private bool _shieldActive;
        private float _speedMultiplier = 1f;
        private float _speedEffectTimer;
        private float _laneTimeLeft;
        private float _laneTimeCenter;
        private float _laneTimeRight;
        private Transform _gateAura;
        private MeshRenderer _gateAuraRenderer;
        private MaterialPropertyBlock _gateAuraBlock;
        private Vector3 _gateAuraBaseScale = new Vector3(0.65f, 0.018f, 0.65f);
        private float _gateAuraTimer;
        private Color _gateAuraColor = new Color(0.42f, 1f, 0.52f, 1f);
        private ParticleSystem _gateBurstSystem;
        private ParticleSystem _weaponTracerSystem;
        private ParticleSystem _weaponFlashSystem;
        private Transform _weaponMuzzle;
        private float _weaponShotAccumulator;
        private bool _combatActive;
        private Transform _combatTarget;
        private int _betterGateHits;
        private int _worseGateHits;
        private int _redGateHits;
        private int _totalGateRows;

        public event Action<int> CountChanged;
        public event Action<int> FinishReached;

        public int Count => _count;
        public float Progress01 => _progress01;
        public bool IsRunning => _isRunning;
        public bool IsCombatActive => _combatActive;

        private void Awake()
        {
            if (formationRoot == null)
            {
                var root = new GameObject("FormationRoot").transform;
                root.SetParent(transform, false);
                formationRoot = root;
            }

            if (dragInput == null)
            {
                dragInput = GetComponent<TouchDragInput>();
            }

            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("SoldierPool").transform;
                _poolRoot.SetParent(transform, false);
            }

            _leaderVisual = transform.Find("LeaderVisual");
            if (_leaderVisual != null)
            {
                UnitVisualFactory.ApplySoldierVisual(_leaderVisual, false);
                var leaderModel = _leaderVisual.Find("SoldierModel");
                if (leaderModel != null)
                {
                    leaderModel.localScale = Vector3.one * 1.28f;
                    leaderModel.localPosition = new Vector3(0f, -0.04f, 0.03f);
                }

                _leaderBaseScale = _leaderVisual.localScale;
                _leaderBaseLocalPosition = _leaderVisual.localPosition;
            }

            EnsureGateEffects();
            EnsureWeaponEffects();

            maxVisibleUnits = Mathf.Min(maxVisibleUnits, 120);

            PrewarmPool();

            var body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var col = GetComponent<Collider>();
            col.isTrigger = false;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_isRunning)
            {
                var dragDelta = dragInput != null ? dragInput.GetHorizontalDeltaNormalized() : 0f;
                _targetX += dragDelta * dragSensitivity * trackHalfWidth;
                _targetX = Mathf.Clamp(_targetX, -trackHalfWidth, trackHalfWidth);

                if (_speedEffectTimer > 0f)
                {
                    _speedEffectTimer = Mathf.Max(0f, _speedEffectTimer - deltaTime);
                    if (_speedEffectTimer <= 0f)
                    {
                        _speedMultiplier = 1f;
                    }
                }

                var pos = transform.position;
                var blend = 1f - Mathf.Exp(-xSmoothSpeed * deltaTime);
                pos.x = Mathf.Lerp(pos.x, _targetX, blend);
                pos.z += _forwardSpeed * _speedMultiplier * deltaTime;
                transform.position = pos;

                if (!_hasLastX)
                {
                    _hasLastX = true;
                    _lastX = pos.x;
                }

                var rawStrafeVelocity = (pos.x - _lastX) / deltaTime;
                _lastX = pos.x;
                var velocityBlend = 1f - Mathf.Exp(-10f * deltaTime);
                _smoothedStrafeVelocity = Mathf.Lerp(_smoothedStrafeVelocity, rawStrafeVelocity, velocityBlend);
                _progress01 = _finishZ > 0f ? Mathf.Clamp01(pos.z / _finishZ) : 0f;
                SampleLaneBias(pos.x, deltaTime);
            }
            else
            {
                var settleBlend = 1f - Mathf.Exp(-12f * deltaTime);
                _smoothedStrafeVelocity = Mathf.Lerp(_smoothedStrafeVelocity, 0f, settleBlend);
            }

            if (_gatePunchTimer > 0f)
            {
                _gatePunchTimer = Mathf.Max(0f, _gatePunchTimer - deltaTime);
            }

            AnimateFormation(deltaTime);
            AnimateLeader(deltaTime);
            AnimateGateEffects(deltaTime);
            UpdateWeaponEffects(deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isRunning)
            {
                return;
            }

            if (other.TryGetComponent<Gate>(out var gate))
            {
                if (allowOnlyOneGatePerRow && gate.rowId >= 0 && gate.rowId == _lastConsumedGateRow)
                {
                    return;
                }

                if (gate.TryApply(this) && gate.rowId >= 0)
                {
                    _lastConsumedGateRow = gate.rowId;
                    RegisterGateOutcome(gate.pickTier);
                }

                return;
            }

            if (other.TryGetComponent<HazardZone>(out var hazard))
            {
                hazard.TryApply(this);
                return;
            }

            if (other.TryGetComponent<FinishLine>(out var finishLine))
            {
                finishLine.TryTrigger(this);
            }
        }

        public void StartRun(Vector3 startPosition, int initialCount, float forwardSpeed, float newTrackHalfWidth, float finishZ)
        {
            StartRun(startPosition, initialCount, forwardSpeed, newTrackHalfWidth, finishZ, 0);
        }

        public void StartRun(
            Vector3 startPosition,
            int initialCount,
            float forwardSpeed,
            float newTrackHalfWidth,
            float finishZ,
            int totalGateRows)
        {
            transform.position = startPosition;
            _targetX = startPosition.x;
            _forwardSpeed = Mathf.Max(0.1f, forwardSpeed);
            trackHalfWidth = Mathf.Max(0.5f, newTrackHalfWidth);
            _finishZ = Mathf.Max(1f, finishZ);
            _finishTriggered = false;
            _progress01 = 0f;
            _isRunning = true;
            _hasLastX = false;
            _leaderTilt = 0f;
            _gatePunchTimer = 0f;
            _lastConsumedGateRow = -1;
            _shieldActive = false;
            _speedMultiplier = 1f;
            _speedEffectTimer = 0f;
            _laneTimeLeft = 0f;
            _laneTimeCenter = 0f;
            _laneTimeRight = 0f;
            _weaponShotAccumulator = 0f;
            _combatActive = false;
            _combatTarget = null;
            _betterGateHits = 0;
            _worseGateHits = 0;
            _redGateHits = 0;
            _totalGateRows = Mathf.Max(0, totalGateRows);

            SetCount(initialCount);
        }

        public void StopRun()
        {
            _isRunning = false;
        }

        public void BeginCombat(Transform target)
        {
            _isRunning = false;
            _combatActive = true;
            _combatTarget = target;
            _weaponShotAccumulator = 0f;
        }

        public void EndCombat()
        {
            _combatActive = false;
            _combatTarget = null;
            _weaponShotAccumulator = 0f;
        }

        public int ApplyBattleLosses(int amount)
        {
            var safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0 || _count <= 0)
            {
                return 0;
            }

            var before = _count;
            SetCount(_count - safeAmount, true);
            return before - _count;
        }

        public void ApplyGate(GateOperation operation, int value)
        {
            var safeValue = Mathf.Max(1, value);

            if (IsNegativeGate(operation) && _shieldActive)
            {
                _shieldActive = false;
                TriggerGatePunch(new Color(0.38f, 0.85f, 1f, 1f));
                return;
            }

            var next = _count;

            switch (operation)
            {
                case GateOperation.Add:
                    next += safeValue;
                    break;
                case GateOperation.Subtract:
                    next -= safeValue;
                    break;
                case GateOperation.Multiply:
                    next *= safeValue;
                    break;
                case GateOperation.Divide:
                    next /= safeValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }

            SetCount(next);
            AudioDirector.Instance?.PlaySfx(
                IsNegativeGate(operation) ? AudioSfxCue.GateNegative : AudioSfxCue.GatePositive,
                0.78f,
                UnityEngine.Random.Range(0.92f, 1.12f));
            TriggerGatePunch(IsNegativeGate(operation)
                ? new Color(1f, 0.52f, 0.44f, 1f)
                : new Color(0.38f, 1f, 0.48f, 1f));
        }

        public void ApplySlow(float speedMultiplier, float duration)
        {
            var safeMultiplier = Mathf.Clamp(speedMultiplier, Mathf.Max(0.05f, minSpeedMultiplier), 1f);
            _speedMultiplier = Mathf.Min(_speedMultiplier, safeMultiplier);
            _speedEffectTimer = Mathf.Max(_speedEffectTimer, Mathf.Max(0.05f, duration));
        }

        public void ApplyLateralImpulse(float deltaX)
        {
            _targetX = Mathf.Clamp(_targetX + deltaX, -trackHalfWidth, trackHalfWidth);
        }

        public void ApplyInstantReinforcement(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetCount(_count + amount);
            TriggerGatePunch(new Color(0.34f, 1f, 0.66f, 1f));
        }

        public bool ActivateShield()
        {
            if (_shieldActive || !_isRunning)
            {
                return false;
            }

            _shieldActive = true;
            return true;
        }

        public void GetLaneUsage(out float left, out float center, out float right)
        {
            left = _laneTimeLeft;
            center = _laneTimeCenter;
            right = _laneTimeRight;
        }

        public void GetGateHitStats(
            out int betterHits,
            out int worseHits,
            out int redHits,
            out int totalRows)
        {
            betterHits = _betterGateHits;
            worseHits = _worseGateHits;
            redHits = _redGateHits;
            totalRows = _totalGateRows;
        }

        public void NotifyFinishReached(int enemyCount)
        {
            if (_finishTriggered)
            {
                return;
            }

            _finishTriggered = true;
            _isRunning = false;
            FinishReached?.Invoke(enemyCount);
        }

        private void SetCount(int value, bool allowZero = false)
        {
            _count = allowZero ? Mathf.Max(0, value) : Mathf.Max(minCount, value);
            var targetVisible = Mathf.Min(_count, maxVisibleUnits);

            while (_activeUnits.Count < targetVisible)
            {
                var unit = GetUnitFromPool();
                unit.gameObject.SetActive(true);
                unit.SetParent(formationRoot, false);
                _activeUnits.Add(unit);
                _formationSlots.Add(Vector3.zero);
                _unitPhaseOffsets.Add(CalculatePhaseOffset(_activeUnits.Count - 1));
            }

            while (_activeUnits.Count > targetVisible)
            {
                var lastIndex = _activeUnits.Count - 1;
                var unit = _activeUnits[lastIndex];
                _activeUnits.RemoveAt(lastIndex);
                _formationSlots.RemoveAt(lastIndex);
                _unitPhaseOffsets.RemoveAt(lastIndex);
                ReturnUnitToPool(unit);
            }

            RelayoutFormation();
            CountChanged?.Invoke(_count);
        }

        private Transform GetUnitFromPool()
        {
            if (_unitPool.Count > 0)
            {
                return _unitPool.Pop();
            }

            return CreateUnitInstance();
        }

        private Transform CreateUnitInstance()
        {
            if (soldierUnitPrefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "SoldierUnit";
                var fallbackCollider = fallback.GetComponent<Collider>();
                if (fallbackCollider != null)
                {
                    Destroy(fallbackCollider);
                }

                fallback.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
                soldierUnitPrefab = fallback;
                fallback.SetActive(false);
            }

            var instance = Instantiate(soldierUnitPrefab, _poolRoot);
            instance.name = "SoldierUnit";
            UnitVisualFactory.ApplySoldierVisual(instance.transform, false);
            return instance.transform;
        }

        private void ReturnUnitToPool(Transform unit)
        {
            unit.gameObject.SetActive(false);
            unit.SetParent(_poolRoot, false);
            _unitPool.Push(unit);
        }

        private void RelayoutFormation()
        {
            var count = _activeUnits.Count;
            if (count == 0)
            {
                return;
            }

            var columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count)), 1, maxColumns);
            var centeredOffset = (columns - 1) * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var column = i % columns;
                var x = (column - centeredOffset) * unitSpacingX;
                var z = -row * unitSpacingZ;
                _formationSlots[i] = new Vector3(x, unitYOffset, z);
            }
        }

        private void PrewarmPool()
        {
            var prewarmCount = Mathf.Max(0, initialPoolSize);
            for (var i = 0; i < prewarmCount; i++)
            {
                var unit = CreateUnitInstance();
                ReturnUnitToPool(unit);
            }
        }

        private void AnimateFormation(float deltaTime)
        {
            var count = _activeUnits.Count;
            if (count == 0)
            {
                return;
            }

            var runTime = Time.time;
            var blend = 1f - Mathf.Exp(-formationLerpSpeed * deltaTime);
            var punchScale = EvaluateGatePunchScale();
            for (var i = 0; i < count; i++)
            {
                var slot = _formationSlots[i];
                var unit = _activeUnits[i];
                if (unit == null)
                {
                    continue;
                }

                if (_isRunning || _combatActive)
                {
                    var phase = runTime * runBobFrequency + _unitPhaseOffsets[i];
                    slot.y += Mathf.Sin(phase) * runBobAmplitude;
                    unit.localRotation = Quaternion.Euler(Mathf.Sin(phase + 1.1f) * unitTiltDegrees, 0f, 0f);
                }
                else
                {
                    unit.localRotation = Quaternion.identity;
                }

                unit.localPosition = Vector3.Lerp(unit.localPosition, slot, blend);
            }

            formationRoot.localScale = Vector3.one * punchScale;
        }

        private void AnimateLeader(float deltaTime)
        {
            if (_leaderVisual == null)
            {
                return;
            }

            var tiltTarget = Mathf.Clamp(-_smoothedStrafeVelocity * leaderStrafeTilt, -18f, 18f);
            var tiltBlend = 1f - Mathf.Exp(-10f * deltaTime);
            _leaderTilt = Mathf.Lerp(_leaderTilt, tiltTarget, tiltBlend);

            var localPosition = _leaderBaseLocalPosition;
            if (_isRunning || _combatActive)
            {
                localPosition.y += Mathf.Sin(Time.time * runBobFrequency * 0.9f) * leaderBobAmplitude;
            }

            _leaderVisual.localPosition = localPosition;
            _leaderVisual.localRotation = Quaternion.Euler(0f, 0f, _leaderTilt);

            var pulse = (_isRunning || _combatActive) ? 1f + Mathf.Sin(Time.time * runBobFrequency * 1.2f) * leaderScalePulse : 1f;
            _leaderVisual.localScale = _leaderBaseScale * (pulse * EvaluateGatePunchScale());
        }

        private static float CalculatePhaseOffset(int index)
        {
            return Mathf.Repeat(index * 0.6180339f, 1f) * Mathf.PI * 2f;
        }

        private void SampleLaneBias(float xPosition, float deltaTime)
        {
            var threshold = Mathf.Max(0.2f, trackHalfWidth * 0.28f);
            if (xPosition <= -threshold)
            {
                _laneTimeLeft += deltaTime;
                return;
            }

            if (xPosition >= threshold)
            {
                _laneTimeRight += deltaTime;
                return;
            }

            _laneTimeCenter += deltaTime;
        }

        private static bool IsNegativeGate(GateOperation operation)
        {
            return operation == GateOperation.Subtract || operation == GateOperation.Divide;
        }

        private void TriggerGatePunch(Color auraColor)
        {
            _gatePunchTimer = Mathf.Max(_gatePunchTimer, Mathf.Max(0.02f, gatePunchDuration));
            if (!enableGateEffects)
            {
                return;
            }

            _gateAuraTimer = Mathf.Max(_gateAuraTimer, Mathf.Max(0.08f, gateAuraDuration));
            _gateAuraColor = auraColor;
            if (_gateAura != null)
            {
                _gateAura.gameObject.SetActive(true);
                _gateAura.localScale = _gateAuraBaseScale;
                _gateAura.localPosition = new Vector3(0f, gateAuraHeight, 0.06f);
                SetGateAuraColor(_gateAuraColor);
            }

            EmitGateBurst(_gateAuraColor);
        }

        private void RegisterGateOutcome(GatePickTier pickTier)
        {
            switch (pickTier)
            {
                case GatePickTier.BetterGood:
                    _betterGateHits++;
                    break;
                case GatePickTier.RedBad:
                    _redGateHits++;
                    break;
                case GatePickTier.WorseGood:
                default:
                    _worseGateHits++;
                    break;
            }
        }

        private float EvaluateGatePunchScale()
        {
            if (_gatePunchTimer <= 0f || gatePunchDuration <= 0f)
            {
                return 1f;
            }

            var normalized = 1f - Mathf.Clamp01(_gatePunchTimer / gatePunchDuration);
            return Mathf.Lerp(1f, Mathf.Max(1f, gatePunchScale), Mathf.Sin(normalized * Mathf.PI));
        }

        private void EnsureGateEffects()
        {
            if (!enableGateEffects)
            {
                return;
            }

            if (_gateAura == null)
            {
                var auraObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                auraObject.name = "GateAura";
                auraObject.transform.SetParent(transform, false);
                auraObject.transform.localPosition = new Vector3(0f, gateAuraHeight, 0.06f);
                auraObject.transform.localScale = _gateAuraBaseScale;
                var auraCollider = auraObject.GetComponent<Collider>();
                if (auraCollider != null)
                {
                    Destroy(auraCollider);
                }

                _gateAuraRenderer = auraObject.GetComponent<MeshRenderer>();
                if (_gateAuraRenderer != null)
                {
                    _gateAuraRenderer.sharedMaterial = CreateEffectMaterial("GateAuraMaterial", _gateAuraColor, 0.5f, 0.7f);
                    _gateAuraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _gateAuraRenderer.receiveShadows = false;
                }

                _gateAura = auraObject.transform;
                _gateAura.gameObject.SetActive(false);
            }

            if (_gateBurstSystem == null)
            {
                var burstObject = new GameObject("GateBurstFX");
                burstObject.transform.SetParent(transform, false);
                burstObject.transform.localPosition = new Vector3(0f, 0.35f, 0.24f);
                burstObject.SetActive(false);
                var particleSystem = burstObject.AddComponent<ParticleSystem>();
                var renderer = burstObject.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.material = CreateEffectMaterial("GateBurstMaterial", new Color(0.9f, 0.98f, 1f, 1f), 0.2f, 0.9f);
                }

                ConfigureGateBurstParticleSystem(particleSystem);
                _gateBurstSystem = particleSystem;
                burstObject.SetActive(true);
            }
        }

        private void ConfigureGateBurstParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.28f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Max(1.2f, gateBurstSpeed * 0.7f), Mathf.Max(1.6f, gateBurstSpeed));
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(_gateAuraColor);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 42f;
            shape.radius = 0.18f;
            shape.length = 0.1f;

            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = false;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.9f, 0.94f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.45f);
            sizeCurve.AddKey(0.35f, 1f);
            sizeCurve.AddKey(1f, 0.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.32f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        private void EmitGateBurst(Color color)
        {
            if (!enableGateEffects || _gateBurstSystem == null)
            {
                return;
            }

            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = color
            };
            _gateBurstSystem.Emit(emitParams, Mathf.Clamp(gateBurstCount, 4, 28));
        }

        private void EnsureWeaponEffects()
        {
            if (!enableWeaponVfx)
            {
                return;
            }

            if (_weaponMuzzle == null)
            {
                var muzzleRoot = new GameObject("WeaponMuzzle");
                muzzleRoot.transform.SetParent(_leaderVisual != null ? _leaderVisual : transform, false);
                muzzleRoot.transform.localPosition = new Vector3(0f, 0.35f, 0.38f);
                muzzleRoot.transform.localRotation = Quaternion.identity;
                _weaponMuzzle = muzzleRoot.transform;
            }

            if (_weaponTracerSystem == null)
            {
                var tracerObject = new GameObject("WeaponTracerFX");
                tracerObject.transform.SetParent(transform, false);
                tracerObject.transform.position = _weaponMuzzle.position;
                tracerObject.transform.rotation = Quaternion.identity;
                tracerObject.SetActive(false);
                _weaponTracerSystem = tracerObject.AddComponent<ParticleSystem>();
                var tracerRenderer = tracerObject.GetComponent<ParticleSystemRenderer>();
                if (tracerRenderer != null)
                {
                    tracerRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                    tracerRenderer.lengthScale = 4.2f;
                    tracerRenderer.velocityScale = 0.55f;
                    tracerRenderer.material = CreateEffectMaterial("WeaponTracerMaterial", new Color(1f, 0.95f, 0.55f, 1f), 0.18f, 1.2f);
                }

                ConfigureWeaponTracerSystem(_weaponTracerSystem);
                tracerObject.SetActive(true);
            }

            if (_weaponFlashSystem == null)
            {
                var flashObject = new GameObject("WeaponFlashFX");
                flashObject.transform.SetParent(transform, false);
                flashObject.transform.position = _weaponMuzzle != null ? _weaponMuzzle.position : transform.position;
                flashObject.transform.rotation = Quaternion.identity;
                flashObject.SetActive(false);
                _weaponFlashSystem = flashObject.AddComponent<ParticleSystem>();
                var flashRenderer = flashObject.GetComponent<ParticleSystemRenderer>();
                if (flashRenderer != null)
                {
                    flashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                    flashRenderer.material = CreateEffectMaterial("WeaponFlashMaterial", new Color(1f, 0.86f, 0.42f, 1f), 0.12f, 1.4f);
                }

                ConfigureWeaponFlashSystem(_weaponFlashSystem);
                flashObject.SetActive(true);
            }
        }

        private static void ConfigureWeaponTracerSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.026f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.56f, 1f));
            main.maxParticles = 320;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.78f, 0.28f, 1f), 0.6f),
                    new GradientColorKey(new Color(1f, 0.48f, 0.14f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.95f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var trails = particleSystem.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.dieWithParticles = true;
            trails.lifetime = 0.08f;
            trails.minVertexDistance = 0.03f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.75f);
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(new Color(1f, 0.84f, 0.36f, 1f));
        }

        private static void ConfigureWeaponFlashSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.14f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.84f, 0.32f, 1f));
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0.15f);
            curve.AddKey(0.2f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private void UpdateWeaponEffects(float deltaTime)
        {
            if (!enableWeaponVfx || (!_isRunning && !_combatActive))
            {
                return;
            }

            EnsureWeaponEffects();
            if (_weaponTracerSystem == null)
            {
                return;
            }

            var activeVisualUnits = Mathf.Max(1, _activeUnits.Count);
            var shotsPerSecond = Mathf.Clamp(
                baseShotsPerSecond + (Mathf.Sqrt(Mathf.Max(1f, _count)) * Mathf.Max(0f, shotsPerVisibleUnit)) + (activeVisualUnits * 0.05f),
                1f,
                Mathf.Max(1f, maxShotsPerSecond));
            _weaponShotAccumulator += deltaTime * shotsPerSecond;
            var shotCount = Mathf.Clamp(Mathf.FloorToInt(_weaponShotAccumulator), 0, 32);
            if (shotCount <= 0)
            {
                return;
            }

            _weaponShotAccumulator -= shotCount;
            for (var i = 0; i < shotCount; i++)
            {
                if (!TryGetWeaponEmissionPose(out var emitPosition, out var emitDirection))
                {
                    continue;
                }

                var jitter = new Vector3(
                    UnityEngine.Random.Range(-tracerMuzzleJitter, tracerMuzzleJitter),
                    UnityEngine.Random.Range(-tracerMuzzleJitter * 0.35f, tracerMuzzleJitter * 0.35f),
                    UnityEngine.Random.Range(-0.02f, 0.04f));
                emitPosition += jitter;
                emitDirection = (emitDirection + new Vector3(
                    UnityEngine.Random.Range(-tracerSpread, tracerSpread),
                    UnityEngine.Random.Range(-tracerSpread * 0.2f, tracerSpread * 0.2f),
                    0f)).normalized;
                var emitVelocity = emitDirection * Mathf.Max(4f, tracerSpeed * UnityEngine.Random.Range(0.84f, 1.18f));

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = emitPosition,
                    velocity = emitVelocity,
                    startLifetime = UnityEngine.Random.Range(tracerLifetime * 0.85f, tracerLifetime * 1.2f),
                    startSize = UnityEngine.Random.Range(0.024f, 0.05f),
                    startColor = Color.Lerp(new Color(1f, 0.92f, 0.5f, 1f), new Color(1f, 0.62f, 0.2f, 1f), UnityEngine.Random.value)
                };

                _weaponTracerSystem.Emit(emitParams, 1);
            }

            if (_weaponFlashSystem != null)
            {
                for (var i = 0; i < Mathf.Clamp(1 + (shotCount / 6), 1, 7); i++)
                {
                    if (!TryGetWeaponEmissionPose(out var flashPosition, out var flashDirection))
                    {
                        continue;
                    }

                    var flashParams = new ParticleSystem.EmitParams
                    {
                        position = flashPosition,
                        velocity = flashDirection * 0.35f,
                        startLifetime = UnityEngine.Random.Range(0.03f, 0.08f),
                        startSize = UnityEngine.Random.Range(0.045f, 0.1f),
                        startColor = new Color(1f, 0.84f, 0.32f, 1f)
                    };
                    _weaponFlashSystem.Emit(flashParams, 1);
                }
            }
        }

        private bool TryGetWeaponEmissionPose(out Vector3 position, out Vector3 direction)
        {
            direction = transform.forward;
            if (_combatTarget != null)
            {
                direction = (_combatTarget.position + Vector3.up * 0.55f) - transform.position;
            }

            if (_activeUnits.Count > 0)
            {
                var unit = _activeUnits[UnityEngine.Random.Range(0, _activeUnits.Count)];
                if (unit != null)
                {
                    position = unit.TransformPoint(new Vector3(
                        UnityEngine.Random.Range(-0.03f, 0.03f),
                        0.5f + UnityEngine.Random.Range(-0.03f, 0.08f),
                        0.18f + UnityEngine.Random.Range(-0.02f, 0.05f)));
                    direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
                    return true;
                }
            }

            if (_weaponMuzzle != null)
            {
                position = _weaponMuzzle.position;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
                return true;
            }

            position = transform.position + Vector3.up * 0.55f + transform.forward * 0.18f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            return true;
        }

        private void AnimateGateEffects(float deltaTime)
        {
            if (!enableGateEffects || _gateAura == null)
            {
                return;
            }

            if (_gateAuraTimer <= 0f)
            {
                if (_gateAura.gameObject.activeSelf)
                {
                    _gateAura.gameObject.SetActive(false);
                }

                return;
            }

            _gateAuraTimer = Mathf.Max(0f, _gateAuraTimer - Mathf.Max(0f, deltaTime));
            var duration = Mathf.Max(0.08f, gateAuraDuration);
            var normalized = 1f - (_gateAuraTimer / duration);
            var eased = 1f - Mathf.Pow(1f - normalized, 2f);
            var scale = Mathf.Lerp(0.65f, Mathf.Max(0.7f, gateAuraMaxScale), eased);
            _gateAura.localScale = new Vector3(scale, _gateAuraBaseScale.y, scale);
            _gateAura.localPosition = new Vector3(0f, gateAuraHeight + Mathf.Sin(eased * Mathf.PI) * 0.03f, 0.06f);
            var pulseStrength = 1f - normalized;
            SetGateAuraColor(_gateAuraColor * (0.38f + (pulseStrength * 0.95f)));
        }

        private void SetGateAuraColor(Color color)
        {
            if (_gateAuraRenderer == null)
            {
                return;
            }

            if (_gateAuraBlock == null)
            {
                _gateAuraBlock = new MaterialPropertyBlock();
            }

            _gateAuraRenderer.GetPropertyBlock(_gateAuraBlock);
            _gateAuraBlock.SetColor(BaseColorId, color);
            _gateAuraBlock.SetColor(ColorId, color);
            _gateAuraRenderer.SetPropertyBlock(_gateAuraBlock);
        }

        private static Material CreateEffectMaterial(string materialName, Color color, float smoothness, float emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = materialName
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
    }
}
