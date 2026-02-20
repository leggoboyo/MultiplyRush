using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    public sealed class EnemyGroup : MonoBehaviour
    {
        [Header("Visuals")]
        public GameObject enemyUnitPrefab;
        public Transform unitsRoot;
        public TextMesh countLabel;
        public int maxVisibleUnits = 120;
        public int initialPoolSize = 70;
        public int maxColumns = 10;
        public float spacingX = 0.55f;
        public float spacingZ = 0.55f;
        public float formationLerpSpeed = 14f;
        public float bobAmplitude = 0.06f;
        public float bobFrequency = 7.8f;
        public float tiltDegrees = 5f;

        private readonly List<Transform> _activeUnits = new List<Transform>(120);
        private readonly Stack<Transform> _pool = new Stack<Transform>(120);
        private readonly List<Vector3> _slots = new List<Vector3>(120);
        private readonly List<float> _phaseOffsets = new List<float>(120);
        private Transform _poolRoot;
        private int _count;

        public int Count => _count;

        private void Awake()
        {
            if (unitsRoot == null)
            {
                var root = new GameObject("EnemyUnits").transform;
                root.SetParent(transform, false);
                unitsRoot = root;
            }

            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("EnemyPool").transform;
                _poolRoot.SetParent(transform, false);
            }

            maxVisibleUnits = Mathf.Min(maxVisibleUnits, 100);

            PrewarmPool();
        }

        private void Update()
        {
            var count = _activeUnits.Count;
            if (count == 0)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-formationLerpSpeed * deltaTime);
            var runTime = Time.time;

            for (var i = 0; i < count; i++)
            {
                var unit = _activeUnits[i];
                if (unit == null)
                {
                    continue;
                }

                var phase = runTime * bobFrequency + _phaseOffsets[i];
                var target = _slots[i];
                target.y += Mathf.Sin(phase) * bobAmplitude;
                unit.localPosition = Vector3.Lerp(unit.localPosition, target, blend);
                unit.localRotation = Quaternion.Euler(Mathf.Sin(phase + 0.95f) * tiltDegrees, 0f, 0f);
            }
        }

        public void SetCount(int count)
        {
            _count = Mathf.Max(1, count);
            if (countLabel != null)
            {
                countLabel.text = _count.ToString();
            }

            var targetVisible = Mathf.Min(_count, maxVisibleUnits);
            while (_activeUnits.Count < targetVisible)
            {
                var unit = GetUnit();
                unit.gameObject.SetActive(true);
                unit.SetParent(unitsRoot, false);
                _activeUnits.Add(unit);
                _slots.Add(Vector3.zero);
                _phaseOffsets.Add(CalculatePhaseOffset(_activeUnits.Count - 1));
            }

            while (_activeUnits.Count > targetVisible)
            {
                var last = _activeUnits.Count - 1;
                var unit = _activeUnits[last];
                _activeUnits.RemoveAt(last);
                _slots.RemoveAt(last);
                _phaseOffsets.RemoveAt(last);
                ReturnUnit(unit);
            }

            Relayout();
        }

        private Transform GetUnit()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            return CreateUnitInstance();
        }

        private Transform CreateUnitInstance()
        {
            if (enemyUnitPrefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "EnemyUnit";
                var collider = fallback.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                fallback.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
                enemyUnitPrefab = fallback;
                fallback.SetActive(false);
            }

            var instance = Instantiate(enemyUnitPrefab, _poolRoot);
            instance.name = "EnemyUnit";
            UnitVisualFactory.ApplySoldierVisual(instance.transform, true);
            return instance.transform;
        }

        private void ReturnUnit(Transform unit)
        {
            unit.gameObject.SetActive(false);
            unit.SetParent(_poolRoot, false);
            _pool.Push(unit);
        }

        private void Relayout()
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
                var x = (column - centeredOffset) * spacingX;
                var z = row * spacingZ;
                _slots[i] = new Vector3(x, 0f, z);
            }
        }

        private void PrewarmPool()
        {
            var prewarmCount = Mathf.Max(0, initialPoolSize);
            for (var i = 0; i < prewarmCount; i++)
            {
                var unit = CreateUnitInstance();
                ReturnUnit(unit);
            }
        }

        private static float CalculatePhaseOffset(int index)
        {
            return Mathf.Repeat(index * 0.7548777f, 1f) * Mathf.PI * 2f;
        }
    }
}
