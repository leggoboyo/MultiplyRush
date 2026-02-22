using UnityEngine;

namespace MultiplyRush
{
    public enum HazardType
    {
        UnitPit = 0,
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
        public Color pitColor = new Color(0.1f, 0.12f, 0.18f, 1f);
        public Color knockbackColor = new Color(0.95f, 0.36f, 0.24f, 1f);
        public float pulseAmplitude = 0.08f;
        public float pulseSpeed = 3.8f;

        private BoxCollider _trigger;
        private MaterialPropertyBlock _materialBlock;
        private HazardType _hazardType;
        private float _unitLossFraction = 0.12f;
        private int _flatUnitLoss = 8;
        private float _knockbackDeltaX;
        private bool _consumed;
        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private Color _pitVoidColor = new Color(0.03f, 0.04f, 0.06f, 1f);
        private Color _pitRimColor = new Color(0.18f, 0.2f, 0.26f, 1f);
        private float _phaseOffset;
        private Transform _pitVisualRoot;
        private MeshRenderer _pitVoidRenderer;
        private MeshRenderer _pitRimRenderer;
        private MaterialPropertyBlock _pitVoidBlock;
        private MaterialPropertyBlock _pitRimBlock;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            _phaseOffset = Mathf.Repeat(GetInstanceID() * 0.187f, 1f) * Mathf.PI * 2f;
            if (zoneRenderer == null)
            {
                zoneRenderer = GetComponent<MeshRenderer>();
            }

            EnsurePitVisuals();
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
            float unitLossFraction,
            int flatUnitLoss,
            float knockbackDeltaX,
            Vector3 worldPosition,
            Vector3 worldScale,
            bool emphasize)
        {
            _hazardType = hazardType;
            _unitLossFraction = Mathf.Clamp(unitLossFraction, 0.02f, 0.92f);
            _flatUnitLoss = Mathf.Clamp(flatUnitLoss, 1, 2000000);
            _knockbackDeltaX = knockbackDeltaX;
            var verticalScale = hazardType == HazardType.UnitPit
                ? 1f
                : Mathf.Max(0.02f, worldScale.y);
            transform.position = worldPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(
                Mathf.Max(0.4f, worldScale.x),
                verticalScale,
                Mathf.Max(0.4f, worldScale.z));

            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.isTrigger = true;
            if (hazardType == HazardType.UnitPit)
            {
                _trigger.center = new Vector3(0f, 0.18f, 0f);
                _trigger.size = new Vector3(1f, 0.36f, 1f);
            }
            else
            {
                _trigger.center = new Vector3(0f, 0.6f, 0f);
                _trigger.size = new Vector3(1f, 1.2f, 1f);
            }

            _trigger.enabled = true;
            _consumed = false;

            if (labelText != null)
            {
                if (hazardType == HazardType.UnitPit)
                {
                    var lossPct = Mathf.RoundToInt(_unitLossFraction * 100f);
                    labelText.text = "PIT -" + lossPct + "%";
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
                labelText.transform.localPosition = hazardType == HazardType.UnitPit
                    ? new Vector3(0f, 0.45f, -0.08f)
                    : new Vector3(0f, 1.05f, -0.08f);
                labelText.transform.localScale = Vector3.one * (emphasize ? 0.17f : 0.14f);
            }

            _baseScale = transform.localScale;
            _baseColor = hazardType == HazardType.UnitPit ? pitColor : knockbackColor;
            if (emphasize)
            {
                _baseColor *= 1.14f;
            }

            _pitVoidColor = Color.Lerp(_baseColor, Color.black, 0.82f);
            _pitRimColor = Color.Lerp(_baseColor, Color.white, 0.18f);
            ApplyPitVisualState();
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

            if (_hazardType == HazardType.UnitPit)
            {
                var currentCount = Mathf.Max(0, crowd.Count);
                var percentageLoss = Mathf.RoundToInt(currentCount * _unitLossFraction);
                var requestedLoss = Mathf.Max(_flatUnitLoss, percentageLoss);
                var maxLoss = Mathf.Max(0, currentCount - 1);
                var safeLoss = Mathf.Clamp(requestedLoss, 0, maxLoss);
                if (safeLoss > 0)
                {
                    crowd.ApplyGate(GateOperation.Subtract, safeLoss);
                }
            }
            else
            {
                crowd.ApplyLateralImpulse(_knockbackDeltaX);
                crowd.ApplySlow(0.85f, 0.45f);
            }

            gameObject.SetActive(false);
            return true;
        }

        private void UpdateVisuals()
        {
            if (zoneRenderer != null && zoneRenderer.enabled)
            {
                if (_materialBlock == null)
                {
                    _materialBlock = new MaterialPropertyBlock();
                }

                zoneRenderer.GetPropertyBlock(_materialBlock);
                _materialBlock.SetColor(BaseColorId, _baseColor);
                _materialBlock.SetColor(ColorId, _baseColor);
                zoneRenderer.SetPropertyBlock(_materialBlock);
            }

            if (_pitVoidRenderer != null && _pitVoidRenderer.enabled)
            {
                if (_pitVoidBlock == null)
                {
                    _pitVoidBlock = new MaterialPropertyBlock();
                }

                _pitVoidRenderer.GetPropertyBlock(_pitVoidBlock);
                _pitVoidBlock.SetColor(BaseColorId, _pitVoidColor);
                _pitVoidBlock.SetColor(ColorId, _pitVoidColor);
                _pitVoidRenderer.SetPropertyBlock(_pitVoidBlock);
            }

            if (_pitRimRenderer == null || !_pitRimRenderer.enabled)
            {
                return;
            }

            if (_pitRimBlock == null)
            {
                _pitRimBlock = new MaterialPropertyBlock();
            }

            _pitRimRenderer.GetPropertyBlock(_pitRimBlock);
            _pitRimBlock.SetColor(BaseColorId, _pitRimColor);
            _pitRimBlock.SetColor(ColorId, _pitRimColor);
            _pitRimRenderer.SetPropertyBlock(_pitRimBlock);
        }

        private void EnsurePitVisuals()
        {
            if (_pitVisualRoot != null)
            {
                return;
            }

            _pitVisualRoot = new GameObject("PitVisual").transform;
            _pitVisualRoot.SetParent(transform, false);

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PitRim";
            rim.transform.SetParent(_pitVisualRoot, false);
            rim.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            rim.transform.localRotation = Quaternion.identity;
            rim.transform.localScale = new Vector3(1.08f, 0.04f, 1.08f);

            var rimCollider = rim.GetComponent<Collider>();
            if (rimCollider != null)
            {
                Destroy(rimCollider);
            }

            _pitRimRenderer = rim.GetComponent<MeshRenderer>();
            if (_pitRimRenderer != null && zoneRenderer != null)
            {
                _pitRimRenderer.sharedMaterial = zoneRenderer.sharedMaterial;
            }

            var voidObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidObj.name = "PitVoid";
            voidObj.transform.SetParent(_pitVisualRoot, false);
            voidObj.transform.localPosition = new Vector3(0f, -0.035f, 0f);
            voidObj.transform.localRotation = Quaternion.identity;
            voidObj.transform.localScale = new Vector3(0.96f, 0.02f, 0.96f);

            var voidCollider = voidObj.GetComponent<Collider>();
            if (voidCollider != null)
            {
                Destroy(voidCollider);
            }

            _pitVoidRenderer = voidObj.GetComponent<MeshRenderer>();
            if (_pitVoidRenderer != null && zoneRenderer != null)
            {
                _pitVoidRenderer.sharedMaterial = zoneRenderer.sharedMaterial;
            }
        }

        private void ApplyPitVisualState()
        {
            EnsurePitVisuals();
            var isPit = _hazardType == HazardType.UnitPit;
            if (zoneRenderer != null)
            {
                zoneRenderer.enabled = !isPit;
            }

            if (_pitVisualRoot != null)
            {
                _pitVisualRoot.gameObject.SetActive(isPit);
            }
        }
    }
}
