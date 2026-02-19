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
        public int maxColumns = 10;
        public float spacingX = 0.55f;
        public float spacingZ = 0.55f;

        private readonly List<Transform> _activeUnits = new List<Transform>(120);
        private readonly Stack<Transform> _pool = new Stack<Transform>(120);
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
            }

            while (_activeUnits.Count > targetVisible)
            {
                var last = _activeUnits.Count - 1;
                var unit = _activeUnits[last];
                _activeUnits.RemoveAt(last);
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
                _activeUnits[i].localPosition = new Vector3(x, 0f, z);
            }
        }
    }
}
