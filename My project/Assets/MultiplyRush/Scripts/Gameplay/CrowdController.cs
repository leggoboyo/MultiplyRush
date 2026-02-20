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

        [Header("References")]
        public TouchDragInput dragInput;

        private readonly List<Transform> _activeUnits = new List<Transform>(160);
        private readonly Stack<Transform> _unitPool = new Stack<Transform>(160);

        private Transform _poolRoot;
        private bool _isRunning;
        private bool _finishTriggered;
        private float _targetX;
        private float _forwardSpeed;
        private float _finishZ = 1f;
        private int _count;
        private float _progress01;

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
            if (!_isRunning)
            {
                return;
            }

            var dragDelta = dragInput != null ? dragInput.GetHorizontalDeltaNormalized() : 0f;
            _targetX += dragDelta * dragSensitivity * trackHalfWidth;
            _targetX = Mathf.Clamp(_targetX, -trackHalfWidth, trackHalfWidth);

            var pos = transform.position;
            var blend = 1f - Mathf.Exp(-xSmoothSpeed * Time.deltaTime);
            pos.x = Mathf.Lerp(pos.x, _targetX, blend);
            pos.z += _forwardSpeed * Time.deltaTime;
            transform.position = pos;

            _progress01 = _finishZ > 0f ? Mathf.Clamp01(pos.z / _finishZ) : 0f;
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
            }

            while (_activeUnits.Count > targetVisible)
            {
                var lastIndex = _activeUnits.Count - 1;
                var unit = _activeUnits[lastIndex];
                _activeUnits.RemoveAt(lastIndex);
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
                _activeUnits[i].localPosition = new Vector3(x, unitYOffset, z);
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
    }
}
