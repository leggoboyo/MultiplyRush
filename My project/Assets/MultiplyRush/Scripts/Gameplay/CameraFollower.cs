using UnityEngine;

namespace MultiplyRush
{
    public sealed class CameraFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 positionOffset = new Vector3(0f, 10f, -14f);
        public Vector3 lookOffset = new Vector3(0f, 0f, 10f);
        public float followLerpSpeed = 8f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.position + positionOffset;
            var blend = 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.LookAt(target.position + lookOffset);
        }
    }
}
