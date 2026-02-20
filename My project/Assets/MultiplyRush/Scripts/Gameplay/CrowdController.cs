using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CrowdController : MonoBehaviour
    {
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

        public event Action<int> CountChanged;
        public event Action<int> FinishReached;

        public int Count => _count;
        public float Progress01 => _progress01;
        public bool IsRunning => _isRunning;

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
                _leaderBaseScale = _leaderVisual.localScale;
                _leaderBaseLocalPosition = _leaderVisual.localPosition;
            }

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

                var pos = transform.position;
                var blend = 1f - Mathf.Exp(-xSmoothSpeed * deltaTime);
                pos.x = Mathf.Lerp(pos.x, _targetX, blend);
                pos.z += _forwardSpeed * deltaTime;
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
            }
            else
            {
                var settleBlend = 1f - Mathf.Exp(-12f * deltaTime);
                _smoothedStrafeVelocity = Mathf.Lerp(_smoothedStrafeVelocity, 0f, settleBlend);
            }

            AnimateFormation(deltaTime);
            AnimateLeader(deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isRunning)
            {
                return;
            }

            if (other.TryGetComponent<Gate>(out var gate))
            {
                gate.TryApply(this);
                return;
            }

            if (other.TryGetComponent<FinishLine>(out var finishLine))
            {
                finishLine.TryTrigger(this);
            }
        }

        public void StartRun(Vector3 startPosition, int initialCount, float forwardSpeed, float newTrackHalfWidth, float finishZ)
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

            SetCount(initialCount);
        }

        public void StopRun()
        {
            _isRunning = false;
        }

        public void ApplyGate(GateOperation operation, int value)
        {
            var safeValue = Mathf.Max(1, value);
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

        private void SetCount(int value)
        {
            _count = Mathf.Max(minCount, value);
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
            for (var i = 0; i < count; i++)
            {
                var slot = _formationSlots[i];
                var unit = _activeUnits[i];
                if (unit == null)
                {
                    continue;
                }

                if (_isRunning)
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
            if (_isRunning)
            {
                localPosition.y += Mathf.Sin(Time.time * runBobFrequency * 0.9f) * leaderBobAmplitude;
            }

            _leaderVisual.localPosition = localPosition;
            _leaderVisual.localRotation = Quaternion.Euler(0f, 0f, _leaderTilt);

            var pulse = _isRunning ? 1f + Mathf.Sin(Time.time * runBobFrequency * 1.2f) * leaderScalePulse : 1f;
            _leaderVisual.localScale = _leaderBaseScale * pulse;
        }

        private static float CalculatePhaseOffset(int index)
        {
            return Mathf.Repeat(index * 0.6180339f, 1f) * Mathf.PI * 2f;
        }
    }
}
