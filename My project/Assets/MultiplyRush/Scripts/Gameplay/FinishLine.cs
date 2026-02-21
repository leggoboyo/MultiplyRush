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
        public float labelPulseAmplitude = 0.08f;
        public float labelPulseSpeed = 2.5f;
        public Vector3 enemyGroupLocalOffset = new Vector3(0f, 0f, 6.6f);
        public Vector3 enemyGroupLocalScale = Vector3.one;
        public float minEnemyDistanceBehindLine = 5.4f;

        private BoxCollider _trigger;
        private bool _isTriggered;
        private int _enemyCount;
        private int _tankRequirement;
        private bool _isMiniBoss;
        private Vector3 _labelBaseScale = Vector3.one;
        private Vector3 _tankLabelBaseScale = Vector3.one;

        public int EnemyCount => _enemyCount;
        public int TankRequirement => _tankRequirement;
        public bool IsMiniBoss => _isMiniBoss;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            EnsureLabels();
            if (enemyCountLabel != null)
            {
                _labelBaseScale = enemyCountLabel.transform.localScale;
            }

            if (tankRequirementLabel != null)
            {
                _tankLabelBaseScale = tankRequirementLabel.transform.localScale;
            }
        }

        private void OnEnable()
        {
            _isTriggered = false;
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
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
        }

        public void Configure(int enemyCount, int tankRequirement, bool isMiniBoss)
        {
            EnsureLabels();

            _enemyCount = Mathf.Max(1, enemyCount);
            _tankRequirement = Mathf.Max(0, tankRequirement);
            _isMiniBoss = isMiniBoss;

            if (enemyGroup != null)
            {
                var safeOffset = enemyGroupLocalOffset;
                var formationDepth = enemyGroup.EstimateFormationDepth(_enemyCount);
                var distanceBehindLine = Mathf.Max(Mathf.Abs(safeOffset.z), formationDepth * 0.7f + Mathf.Max(1f, minEnemyDistanceBehindLine));
                var worldPosition = new Vector3(
                    transform.position.x + safeOffset.x,
                    transform.position.y + safeOffset.y,
                    transform.position.z + distanceBehindLine);

                enemyGroup.transform.SetParent(transform, true);
                enemyGroup.transform.position = worldPosition;
                enemyGroup.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                enemyGroup.transform.localScale = enemyGroupLocalScale * (isMiniBoss ? 1.16f : 1f);
                enemyGroup.SetCount(_enemyCount);
            }

            if (enemyCountLabel != null)
            {
                enemyCountLabel.text = isMiniBoss ? "BOSS " + _enemyCount : "Enemy " + _enemyCount;
                enemyCountLabel.color = isMiniBoss ? new Color(1f, 0.52f, 0.3f, 1f) : Color.white;
            }

            if (tankRequirementLabel != null)
            {
                var showTank = _tankRequirement > 0;
                tankRequirementLabel.gameObject.SetActive(showTank);
                if (showTank)
                {
                    tankRequirementLabel.text = "Tank Burst " + _tankRequirement;
                    tankRequirementLabel.color = new Color(1f, 0.92f, 0.45f, 1f);
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
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            crowd.NotifyFinishReached(_enemyCount);
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
    }
}
