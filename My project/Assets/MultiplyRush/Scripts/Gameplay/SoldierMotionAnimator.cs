using UnityEngine;

namespace MultiplyRush
{
    public sealed class SoldierMotionAnimator : MonoBehaviour
    {
        [Range(0.4f, 2.8f)]
        public float intensity = 1f;

        private Transform _model;
        private Transform _torso;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftForearm;
        private Transform _rightForearm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _leftShin;
        private Transform _rightShin;
        private Transform _rifleBody;
        private Transform _rifleBarrel;

        private Vector3 _modelBasePos;
        private Quaternion _modelBaseRot = Quaternion.identity;
        private Vector3 _torsoBasePos;
        private Quaternion _torsoBaseRot = Quaternion.identity;
        private Quaternion _headBaseRot = Quaternion.identity;
        private Quaternion _leftArmBaseRot = Quaternion.identity;
        private Quaternion _rightArmBaseRot = Quaternion.identity;
        private Quaternion _leftForearmBaseRot = Quaternion.identity;
        private Quaternion _rightForearmBaseRot = Quaternion.identity;
        private Quaternion _leftLegBaseRot = Quaternion.identity;
        private Quaternion _rightLegBaseRot = Quaternion.identity;
        private Quaternion _leftShinBaseRot = Quaternion.identity;
        private Quaternion _rightShinBaseRot = Quaternion.identity;
        private Vector3 _rifleBasePos;
        private Quaternion _rifleBodyBaseRot = Quaternion.identity;
        private Quaternion _rifleBarrelBaseRot = Quaternion.identity;

        private float _phaseOffset;
        private float _fireKick;
        private float _movingBlend;
        private float _combatBlend;
        private bool _isMoving;
        private bool _isInCombat;

        private void Awake()
        {
            CacheBones();
        }

        private void LateUpdate()
        {
            if (_model == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                deltaTime = 1f / 60f;
            }

            _movingBlend = Mathf.MoveTowards(_movingBlend, _isMoving ? 1f : 0f, deltaTime * 6.5f);
            _combatBlend = Mathf.MoveTowards(_combatBlend, _isInCombat ? 1f : 0f, deltaTime * 8f);
            _fireKick = Mathf.MoveTowards(_fireKick, 0f, deltaTime * 9f);

            var motion = _movingBlend * Mathf.Clamp(intensity, 0.4f, 2.8f);
            var cycleRate = Mathf.Lerp(4.6f, 6.8f, motion * 0.75f);
            var cycleTime = (Time.time + _phaseOffset) * cycleRate;
            var stride = Mathf.Sin(cycleTime);
            var strideOpposite = Mathf.Sin(cycleTime + Mathf.PI);
            var leftLift = Mathf.Max(0f, stride);
            var rightLift = Mathf.Max(0f, strideOpposite);
            var sway = Mathf.Sin((Time.time + _phaseOffset) * 3.6f);
            var shoulderLead = Mathf.Sin(cycleTime * 0.5f);
            var aimBlend = _combatBlend * 13f;
            var recoil = _fireKick;

            // Keep vertical bob very subtle so units read as walking/running, not hopping.
            var torsoLean = 2.2f * motion + aimBlend;
            var bodyBob = Mathf.Sin(cycleTime * 2f) * 0.0016f * motion;

            _model.localPosition = _modelBasePos + new Vector3(0f, bodyBob, 0f);
            _model.localRotation = _modelBaseRot * Quaternion.Euler(0f, sway * 0.9f * motion, 0f);

            if (_torso != null)
            {
                _torso.localPosition = _torsoBasePos + new Vector3(0f, bodyBob * 0.55f, 0f);
                _torso.localRotation = _torsoBaseRot * Quaternion.Euler(
                    -torsoLean - (recoil * 7f),
                    shoulderLead * 1f * motion,
                    sway * 1.15f * motion);
            }

            if (_head != null)
            {
                _head.localRotation = _headBaseRot * Quaternion.Euler(
                    stride * 1.4f * motion - (recoil * 4f),
                    sway * 1.2f * motion + (_combatBlend * 2f),
                    0f);
            }

            if (_leftArm != null)
            {
                _leftArm.localRotation = _leftArmBaseRot * Quaternion.Euler(
                    -28f - (strideOpposite * 5f * motion) - (aimBlend * 0.36f) - (recoil * 4f),
                    -9f * _combatBlend,
                    4f * motion);
            }

            if (_rightArm != null)
            {
                _rightArm.localRotation = _rightArmBaseRot * Quaternion.Euler(
                    -34f - (stride * 3.8f * motion) - aimBlend - (recoil * 11f),
                    7f * _combatBlend,
                    -4f * motion);
            }

            if (_leftForearm != null)
            {
                _leftForearm.localRotation = _leftForearmBaseRot * Quaternion.Euler(
                    -58f - (aimBlend * 0.24f) - (recoil * 6f),
                    -4f * _combatBlend,
                    3f * motion);
            }

            if (_rightForearm != null)
            {
                _rightForearm.localRotation = _rightForearmBaseRot * Quaternion.Euler(
                    -65f - (aimBlend * 0.22f) - (recoil * 10f),
                    3f * _combatBlend,
                    -3f * motion);
            }

            if (_leftLeg != null)
            {
                _leftLeg.localRotation = _leftLegBaseRot * Quaternion.Euler(stride * 22f * motion, 0f, 0f);
            }

            if (_rightLeg != null)
            {
                _rightLeg.localRotation = _rightLegBaseRot * Quaternion.Euler(strideOpposite * 22f * motion, 0f, 0f);
            }

            if (_leftShin != null)
            {
                _leftShin.localRotation = _leftShinBaseRot * Quaternion.Euler(-leftLift * 34f * motion, 0f, 0f);
            }

            if (_rightShin != null)
            {
                _rightShin.localRotation = _rightShinBaseRot * Quaternion.Euler(-rightLift * 34f * motion, 0f, 0f);
            }

            if (_rifleBody != null)
            {
                _rifleBody.localPosition = _rifleBasePos + new Vector3(0f, recoil * 0.009f, -recoil * 0.026f);
                _rifleBody.localRotation = _rifleBodyBaseRot * Quaternion.Euler(-aimBlend * 0.65f - (recoil * 12f), 0f, 0f);
            }

            if (_rifleBarrel != null)
            {
                _rifleBarrel.localRotation = _rifleBarrelBaseRot * Quaternion.Euler(-aimBlend * 0.3f - (recoil * 16f), 0f, 0f);
            }
        }

        public void Configure(float phaseOffset, bool isEnemy)
        {
            _phaseOffset = phaseOffset;
            intensity = isEnemy ? 0.95f : 1.08f;
            CacheBones();
        }

        public void SetState(bool isMoving, bool inCombat)
        {
            _isMoving = isMoving;
            _isInCombat = inCombat;
        }

        public void TriggerShot(float strength = 1f)
        {
            _fireKick = Mathf.Clamp01(_fireKick + Mathf.Clamp(strength, 0.08f, 1.35f) * 0.42f);
        }

        private void CacheBones()
        {
            _model = transform.Find("SoldierModel");
            if (_model == null)
            {
                return;
            }

            _torso = _model.Find("Torso");
            _head = _model.Find("Head");
            _leftArm = _model.Find("LeftArm");
            _rightArm = _model.Find("RightArm");
            _leftForearm = _model.Find("LeftForearm");
            _rightForearm = _model.Find("RightForearm");
            _leftLeg = _model.Find("LeftLeg");
            _rightLeg = _model.Find("RightLeg");
            _leftShin = _model.Find("LeftShin");
            _rightShin = _model.Find("RightShin");
            _rifleBody = _model.Find("RifleBody");
            _rifleBarrel = _model.Find("RifleBarrel");

            _modelBasePos = _model.localPosition;
            _modelBaseRot = _model.localRotation;
            if (_torso != null)
            {
                _torsoBasePos = _torso.localPosition;
                _torsoBaseRot = _torso.localRotation;
            }

            if (_head != null)
            {
                _headBaseRot = _head.localRotation;
            }

            if (_leftArm != null)
            {
                _leftArmBaseRot = _leftArm.localRotation;
            }

            if (_rightArm != null)
            {
                _rightArmBaseRot = _rightArm.localRotation;
            }

            if (_leftForearm != null)
            {
                _leftForearmBaseRot = _leftForearm.localRotation;
            }

            if (_rightForearm != null)
            {
                _rightForearmBaseRot = _rightForearm.localRotation;
            }

            if (_leftLeg != null)
            {
                _leftLegBaseRot = _leftLeg.localRotation;
            }

            if (_rightLeg != null)
            {
                _rightLegBaseRot = _rightLeg.localRotation;
            }

            if (_leftShin != null)
            {
                _leftShinBaseRot = _leftShin.localRotation;
            }

            if (_rightShin != null)
            {
                _rightShinBaseRot = _rightShin.localRotation;
            }

            if (_rifleBody != null)
            {
                _rifleBasePos = _rifleBody.localPosition;
                _rifleBodyBaseRot = _rifleBody.localRotation;
            }

            if (_rifleBarrel != null)
            {
                _rifleBarrelBaseRot = _rifleBarrel.localRotation;
            }
        }
    }
}
