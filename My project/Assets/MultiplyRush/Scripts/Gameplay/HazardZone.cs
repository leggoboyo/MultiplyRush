using UnityEngine;

namespace MultiplyRush
{
    public enum HazardType
    {
        SlowField = 0,
        KnockbackStrip = 1
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class HazardZone : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Visuals")]
        public MeshRenderer zoneRenderer;
        public TextMesh labelText;
        public Color slowColor = new Color(0.98f, 0.74f, 0.14f, 1f);
        public Color knockbackColor = new Color(0.95f, 0.36f, 0.24f, 1f);
        public float pulseAmplitude = 0.08f;
        public float pulseSpeed = 3.8f;

        private BoxCollider _trigger;
        private MaterialPropertyBlock _materialBlock;
        private HazardType _hazardType;
        private float _slowMultiplier = 0.78f;
        private float _duration = 1.2f;
        private float _knockbackDeltaX;
        private bool _consumed;
        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private float _phaseOffset;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            _phaseOffset = Mathf.Repeat(GetInstanceID() * 0.187f, 1f) * Mathf.PI * 2f;
            if (zoneRenderer == null)
            {
                zoneRenderer = GetComponent<MeshRenderer>();
            }
        }

        private void OnEnable()
        {
            _consumed = false;
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            _baseScale = transform.localScale;
            UpdateVisuals();
        }

        private void Update()
        {
            if (_consumed)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin((Time.time * pulseSpeed) + _phaseOffset) * pulseAmplitude;
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y, _baseScale.z * pulse);
        }

        public void Configure(
            HazardType hazardType,
            float slowMultiplier,
            float duration,
            float knockbackDeltaX,
            Vector3 worldPosition,
            Vector3 worldScale,
            bool emphasize)
        {
            _hazardType = hazardType;
            _slowMultiplier = Mathf.Clamp(slowMultiplier, 0.4f, 1f);
            _duration = Mathf.Clamp(duration, 0.2f, 3.2f);
            _knockbackDeltaX = knockbackDeltaX;
            transform.position = worldPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(
                Mathf.Max(0.4f, worldScale.x),
                Mathf.Max(0.02f, worldScale.y),
                Mathf.Max(0.4f, worldScale.z));

            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.isTrigger = true;
            _trigger.center = new Vector3(0f, 0.6f, 0f);
            _trigger.size = new Vector3(1f, 1.2f, 1f);
            _trigger.enabled = true;
            _consumed = false;

            if (labelText != null)
            {
                if (hazardType == HazardType.SlowField)
                {
                    var slowPct = Mathf.RoundToInt((1f - _slowMultiplier) * 100f);
                    labelText.text = "SLOW " + slowPct + "%";
                }
                else
                {
                    labelText.text = _knockbackDeltaX >= 0f ? "PUSH >" : "< PUSH";
                }

                labelText.color = Color.white;
                labelText.fontStyle = FontStyle.Bold;
                labelText.characterSize = 0.22f;
                labelText.fontSize = 120;
                labelText.alignment = TextAlignment.Center;
                labelText.anchor = TextAnchor.MiddleCenter;
                labelText.transform.localPosition = new Vector3(0f, 1.05f, -0.08f);
                labelText.transform.localScale = Vector3.one * (emphasize ? 0.15f : 0.12f);
            }

            _baseScale = transform.localScale;
            _baseColor = hazardType == HazardType.SlowField ? slowColor : knockbackColor;
            if (emphasize)
            {
                _baseColor *= 1.14f;
            }

            UpdateVisuals();
        }

        public bool TryApply(CrowdController crowd)
        {
            if (_consumed || crowd == null)
            {
                return false;
            }

            _consumed = true;
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            if (_hazardType == HazardType.SlowField)
            {
                crowd.ApplySlow(_slowMultiplier, _duration);
            }
            else
            {
                crowd.ApplyLateralImpulse(_knockbackDeltaX);
                crowd.ApplySlow(Mathf.Clamp(_slowMultiplier, 0.65f, 1f), Mathf.Min(0.9f, _duration));
            }

            gameObject.SetActive(false);
            return true;
        }

        private void UpdateVisuals()
        {
            if (zoneRenderer == null)
            {
                return;
            }

            if (_materialBlock == null)
            {
                _materialBlock = new MaterialPropertyBlock();
            }

            zoneRenderer.GetPropertyBlock(_materialBlock);
            _materialBlock.SetColor(BaseColorId, _baseColor);
            _materialBlock.SetColor(ColorId, _baseColor);
            zoneRenderer.SetPropertyBlock(_materialBlock);
        }
    }
}
