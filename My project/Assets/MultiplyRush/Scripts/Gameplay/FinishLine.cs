using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FinishLine : MonoBehaviour
    {
        [Header("References")]
        public EnemyGroup enemyGroup;
        public TextMesh enemyCountLabel;

        private BoxCollider _trigger;
        private bool _isTriggered;
        private int _enemyCount;

        public int EnemyCount => _enemyCount;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
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
