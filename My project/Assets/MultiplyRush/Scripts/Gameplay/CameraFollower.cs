using UnityEngine;

namespace MultiplyRush
{
    public sealed class CameraFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 positionOffset = new Vector3(0f, 10.2f, -17.2f);
        public Vector3 lookOffset = new Vector3(0f, 0.5f, 14.4f);
        public float followLerpSpeed = 8f;
        public float lookLerpSpeed = 10f;
        public float baseFieldOfView = 53f;
        public float maxFieldOfView = 60f;
        public float speedForMaxFov = 14f;
        public float rollByLateralVelocity = 0.0075f;
        public float maxRollDegrees = 0.065f;
        public float speedLookAhead = 1.1f;
        public float horizontalFollowFactor = 0.1f;
        public float horizontalLookFactor = 0.045f;
        public float maxHorizontalCameraOffset = 0.42f;
        [Range(0f, 1f)]
        public float minimumMotionIntensity = 0.04f;

        private Camera _camera;
        private Vector3 _smoothedLookOffset;
        private bool _hasLastTargetPosition;
        private Vector3 _lastTargetPosition;
        private float _currentRoll;
        private float _baseRollByLateralVelocity;
        private float _baseMaxRollDegrees;
        private float _baseHorizontalFollowFactor;
        private float _baseHorizontalLookFactor;
        private float _baseMaxHorizontalCameraOffset;
        private float _motionIntensity = 1f;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (positionOffset.y > 11.8f)
            {
                positionOffset.y = 11.8f;
            }
            else if (positionOffset.y < 9.4f)
            {
                positionOffset.y = 9.4f;
            }

            if (positionOffset.z < -19.4f)
            {
                positionOffset.z = -19.4f;
            }
            else if (positionOffset.z > -15.8f)
            {
                positionOffset.z = -15.8f;
            }

            lookOffset.y = Mathf.Clamp(lookOffset.y, 0.2f, 1.35f);
            lookOffset.z = Mathf.Clamp(lookOffset.z, 11.6f, 16.2f);
            lookOffset.z = Mathf.Max(13.4f, lookOffset.z);

            _smoothedLookOffset = lookOffset;
            rollByLateralVelocity = Mathf.Clamp(rollByLateralVelocity, 0.0025f, 0.02f);
            maxRollDegrees = Mathf.Clamp(maxRollDegrees, 0.02f, 0.16f);
            speedLookAhead = Mathf.Clamp(speedLookAhead, 0.35f, 1.35f);
            horizontalFollowFactor = Mathf.Clamp(horizontalFollowFactor, 0.05f, 0.18f);
            horizontalLookFactor = Mathf.Clamp(horizontalLookFactor, 0.02f, 0.1f);
            maxHorizontalCameraOffset = Mathf.Clamp(maxHorizontalCameraOffset, 0.22f, 0.72f);
            horizontalFollowFactor = Mathf.Min(horizontalFollowFactor, 0.11f);
            horizontalLookFactor = Mathf.Min(horizontalLookFactor, 0.05f);
            maxHorizontalCameraOffset = Mathf.Min(maxHorizontalCameraOffset, 0.46f);
            _baseRollByLateralVelocity = rollByLateralVelocity;
            _baseMaxRollDegrees = maxRollDegrees;
            _baseHorizontalFollowFactor = horizontalFollowFactor;
            _baseHorizontalLookFactor = horizontalLookFactor;
            _baseMaxHorizontalCameraOffset = maxHorizontalCameraOffset;
            SetMotionIntensity(ProgressionStore.GetCameraMotionIntensity(0.35f));
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            var targetPosition = target.position;
            if (!_hasLastTargetPosition)
            {
                _lastTargetPosition = targetPosition;
                _hasLastTargetPosition = true;
            }

            var targetVelocity = (targetPosition - _lastTargetPosition) / deltaTime;
            _lastTargetPosition = targetPosition;
            var forwardSpeed = Mathf.Max(0f, targetVelocity.z);
            var speed01 = Mathf.Clamp01(forwardSpeed / Mathf.Max(0.01f, speedForMaxFov));

            var lateralOffset = Mathf.Clamp(target.position.x * horizontalFollowFactor, -maxHorizontalCameraOffset, maxHorizontalCameraOffset);
            var desiredPosition = new Vector3(
                lateralOffset + positionOffset.x,
                target.position.y + positionOffset.y,
                target.position.z + positionOffset.z);
            var blend = 1f - Mathf.Exp(-followLerpSpeed * deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);

            var desiredLookOffset = lookOffset + new Vector3(0f, 0f, speedLookAhead * speed01);
            var lookBlend = 1f - Mathf.Exp(-lookLerpSpeed * deltaTime);
            _smoothedLookOffset = Vector3.Lerp(_smoothedLookOffset, desiredLookOffset, lookBlend);

            var lookTarget = new Vector3(
                target.position.x * horizontalLookFactor,
                target.position.y,
                target.position.z) + _smoothedLookOffset;
            var lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = Vector3.forward;
            }

            var desiredRoll = Mathf.Clamp(-targetVelocity.x * rollByLateralVelocity, -maxRollDegrees, maxRollDegrees);
            _currentRoll = Mathf.Lerp(_currentRoll, desiredRoll, lookBlend);
            var lookRotation = Quaternion.LookRotation(lookDirection, Vector3.up) * Quaternion.Euler(0f, 0f, _currentRoll);
            transform.rotation = lookRotation;

            if (_camera != null)
            {
                var fovTarget = Mathf.Lerp(baseFieldOfView, maxFieldOfView, speed01);
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, fovTarget, lookBlend);
            }
        }

        public void SetMotionIntensity(float intensity01)
        {
            _motionIntensity = Mathf.Clamp01(intensity01);
            var intensity = Mathf.Lerp(minimumMotionIntensity, 1f, _motionIntensity);

            rollByLateralVelocity = _baseRollByLateralVelocity * Mathf.Lerp(0.08f, 1f, intensity);
            maxRollDegrees = _baseMaxRollDegrees * Mathf.Lerp(0.1f, 1f, intensity);
            horizontalFollowFactor = _baseHorizontalFollowFactor * Mathf.Lerp(0.18f, 1f, intensity);
            horizontalLookFactor = _baseHorizontalLookFactor * Mathf.Lerp(0.16f, 1f, intensity);
            maxHorizontalCameraOffset = _baseMaxHorizontalCameraOffset * Mathf.Lerp(0.2f, 1f, intensity);
        }

        public float GetMotionIntensity01()
        {
            return _motionIntensity;
        }
    }
}
