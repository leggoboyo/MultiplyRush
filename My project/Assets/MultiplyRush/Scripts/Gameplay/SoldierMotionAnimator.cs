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
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _rifleBody;
        private Transform _rifleBarrel;

        private Vector3 _modelBasePos;
        private Quaternion _modelBaseRot = Quaternion.identity;
        private Vector3 _torsoBasePos;
        private Quaternion _torsoBaseRot = Quaternion.identity;
        private Quaternion _headBaseRot = Quaternion.identity;
        private Quaternion _leftArmBaseRot = Quaternion.identity;
        private Quaternion _rightArmBaseRot = Quaternion.identity;
        private Quaternion _leftLegBaseRot = Quaternion.identity;
        private Quaternion _rightLegBaseRot = Quaternion.identity;
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

            var runTime = Time.time + _phaseOffset;
            var walk = Mathf.Sin(runTime * 8.4f);
            var bounce = Mathf.Sin(runTime * 16.8f);
            var sway = Mathf.Sin(runTime * 4.2f);
            var motion = _movingBlend * Mathf.Clamp(intensity, 0.4f, 2.8f);
            var combatAim = _combatBlend * 11f;

            _model.localPosition = _modelBasePos + new Vector3(0f, bounce * 0.008f * motion, 0f);
            _model.localRotation = _modelBaseRot * Quaternion.Euler(0f, sway * 1.3f * motion, 0f);

            if (_torso != null)
            {
                var torsoPitch = (walk * -4.4f * motion) - (_fireKick * 7f) - combatAim;
                var torsoRoll = sway * 2.2f * motion;
                _torso.localPosition = _torsoBasePos + new Vector3(0f, bounce * 0.004f * motion, 0f);
                _torso.localRotation = _torsoBaseRot * Quaternion.Euler(torsoPitch, 0f, torsoRoll);
            }

            if (_head != null)
            {
                _head.localRotation = _headBaseRot * Quaternion.Euler(
                    walk * 2f * motion - (_fireKick * 4.5f),
                    sway * 1.5f * motion,
                    0f);
            }

            if (_leftArm != null)
            {
                _leftArm.localRotation = _leftArmBaseRot * Quaternion.Euler(
                    walk * 19f * motion - combatAim * 0.55f - (_fireKick * 4f),
                    -8f * _combatBlend,
                    0f);
            }

            if (_rightArm != null)
            {
                _rightArm.localRotation = _rightArmBaseRot * Quaternion.Euler(
                    walk * -17f * motion - combatAim - (_fireKick * 11f),
                    6f * _combatBlend,
                    0f);
            }

            if (_leftLeg != null)
            {
                _leftLeg.localRotation = _leftLegBaseRot * Quaternion.Euler(walk * 27f * motion, 0f, 0f);
            }

            if (_rightLeg != null)
            {
                _rightLeg.localRotation = _rightLegBaseRot * Quaternion.Euler(walk * -27f * motion, 0f, 0f);
            }

            if (_rifleBody != null)
            {
                _rifleBody.localPosition = _rifleBasePos + new Vector3(0f, _fireKick * 0.01f, -_fireKick * 0.028f);
                _rifleBody.localRotation = _rifleBodyBaseRot * Quaternion.Euler(-combatAim - (_fireKick * 12f), 0f, 0f);
            }

            if (_rifleBarrel != null)
            {
                _rifleBarrel.localRotation = _rifleBarrelBaseRot * Quaternion.Euler(-combatAim * 0.4f - (_fireKick * 18f), 0f, 0f);
            }
        }

        public void Configure(float phaseOffset, bool isEnemy)
        {
            _phaseOffset = phaseOffset;
            intensity = isEnemy ? 0.92f : 1.08f;
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
            _leftLeg = _model.Find("LeftLeg");
            _rightLeg = _model.Find("RightLeg");
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

            if (_leftLeg != null)
            {
                _leftLegBaseRot = _leftLeg.localRotation;
            }

            if (_rightLeg != null)
            {
                _rightLegBaseRot = _rightLeg.localRotation;
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
