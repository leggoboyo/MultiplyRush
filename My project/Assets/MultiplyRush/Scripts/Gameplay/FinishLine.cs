using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FinishLine : MonoBehaviour
    {
        [Header("References")]
        public EnemyGroup enemyGroup;
        public TextMesh enemyCountLabel;
        public float labelPulseAmplitude = 0.08f;
        public float labelPulseSpeed = 2.5f;

        private BoxCollider _trigger;
        private bool _isTriggered;
        private int _enemyCount;
        private Vector3 _labelBaseScale = Vector3.one;

        public int EnemyCount => _enemyCount;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            if (enemyCountLabel != null)
            {
                _labelBaseScale = enemyCountLabel.transform.localScale;
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
            if (enemyCountLabel == null || !enemyCountLabel.gameObject.activeInHierarchy)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(Time.time * labelPulseSpeed) * labelPulseAmplitude;
            enemyCountLabel.transform.localScale = _labelBaseScale * pulse;
        }

        public void Configure(int enemyCount)
        {
            _enemyCount = Mathf.Max(1, enemyCount);

            if (enemyGroup != null)
            {
                enemyGroup.SetCount(_enemyCount);
            }

            if (enemyCountLabel != null)
            {
                enemyCountLabel.text = "Enemy " + _enemyCount;
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
    }
}
