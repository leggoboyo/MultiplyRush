using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Gate : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Gate Data")]
        public GateOperation operation = GateOperation.Add;
        public int value = 5;

        [Header("Visuals")]
        public MeshRenderer panelRenderer;
        public TextMesh labelText;
        public Color positiveColor = new Color(0.2f, 0.85f, 0.35f);
        public Color negativeColor = new Color(0.9f, 0.25f, 0.25f);

        private BoxCollider _trigger;
        private MaterialPropertyBlock _materialBlock;
        private bool _isConsumed;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            _isConsumed = false;
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            RefreshVisuals();
        }

        public void Configure(GateOperation gateOperation, int gateValue)
        {
            operation = gateOperation;
            value = Mathf.Max(1, gateValue);
            _isConsumed = false;

            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            RefreshVisuals();
        }

        public void TryApply(CrowdController crowd)
        {
            if (_isConsumed || crowd == null)
            {
                return;
            }

            _isConsumed = true;
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            crowd.ApplyGate(operation, value);
            gameObject.SetActive(false);
        }

        private void RefreshVisuals()
        {
            if (labelText != null)
            {
                labelText.text = GetLabel(operation, value);
            }

            SetPanelColor(IsPositive(operation) ? positiveColor : negativeColor);
        }

        private void SetPanelColor(Color color)
        {
            if (panelRenderer == null)
            {
                return;
            }

            if (_materialBlock == null)
            {
                _materialBlock = new MaterialPropertyBlock();
            }

            panelRenderer.GetPropertyBlock(_materialBlock);
            _materialBlock.SetColor(BaseColorId, color);
            _materialBlock.SetColor(ColorId, color);
            panelRenderer.SetPropertyBlock(_materialBlock);
        }

        private static bool IsPositive(GateOperation gateOperation)
        {
            return gateOperation == GateOperation.Add || gateOperation == GateOperation.Multiply;
        }

        private static string GetLabel(GateOperation gateOperation, int gateValue)
        {
            switch (gateOperation)
            {
                case GateOperation.Add:
                    return "+" + gateValue;
                case GateOperation.Subtract:
                    return "-" + gateValue;
                case GateOperation.Multiply:
                    return "x" + gateValue;
                case GateOperation.Divide:
                    return "/" + gateValue;
                default:
                    return "?";
            }
        }
    }
}
