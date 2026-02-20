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

        [Header("Layout")]
        public float hitboxWidth = 2.1f;
        public float hitboxHeight = 2.15f;
        public float hitboxDepth = 1.15f;
        public float panelWidth = 2.2f;
        public float panelHeight = 1.6f;
        public float postWidth = 0.12f;
        public float postDepth = 0.18f;
        public float postHeight = 2f;
        public float labelScale = 0.12f;
        public float labelForwardOffset = -0.42f;

        [Header("Animation")]
        public float idleFloatAmplitude = 0.09f;
        public float idleFloatFrequency = 2.6f;
        public float idlePulseAmplitude = 0.05f;
        public float consumeDuration = 0.12f;
        public float consumeSpinDegrees = 220f;

        private BoxCollider _trigger;
        private MaterialPropertyBlock _materialBlock;
        private bool _isConsumed;
        private bool _isConsuming;
        private Transform _leftPost;
        private Transform _rightPost;
        private Transform _panel;
        private float _baseY;
        private float _phaseOffset;
        private float _consumeElapsed;
        private Vector3 _rootBaseScale = Vector3.one;
        private Vector3 _panelBaseScale = Vector3.one;
        private Vector3 _labelBaseScale = Vector3.one;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            _phaseOffset = Mathf.Repeat(GetInstanceID() * 0.1732f, 1f) * Mathf.PI * 2f;
            CacheReferences();
            NormalizeLayout();
        }

        private void OnEnable()
        {
            _isConsumed = false;
            _isConsuming = false;
            _consumeElapsed = 0f;
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            _baseY = transform.position.y;
            transform.localScale = _rootBaseScale;
            transform.rotation = Quaternion.identity;
            NormalizeLayout();
            RefreshVisuals();
        }

        public void Configure(GateOperation gateOperation, int gateValue)
        {
            operation = gateOperation;
            value = Mathf.Max(1, gateValue);
            _isConsumed = false;
            _isConsuming = false;
            _consumeElapsed = 0f;
            _baseY = transform.position.y;

            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            NormalizeLayout();
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
            if (consumeDuration <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            _isConsuming = true;
            _consumeElapsed = 0f;
        }

        private void Update()
        {
            if (_isConsuming)
            {
                AnimateConsume(Time.deltaTime);
                return;
            }

            if (_isConsumed)
            {
                return;
            }

            AnimateIdle(Time.time);
        }

        private void RefreshVisuals()
        {
            if (labelText != null)
            {
                labelText.text = GetLabel(operation, value);
                labelText.color = IsPositive(operation) ? new Color(0.05f, 0.05f, 0.05f) : Color.white;
                labelText.fontStyle = FontStyle.Bold;
            }

            SetPanelColor(IsPositive(operation) ? positiveColor : negativeColor);
        }

        private void CacheReferences()
        {
            if (_panel == null)
            {
                _panel = transform.Find("Panel");
            }

            if (_leftPost == null)
            {
                _leftPost = transform.Find("LeftPost");
            }

            if (_rightPost == null)
            {
                _rightPost = transform.Find("RightPost");
            }

            if (panelRenderer == null && _panel != null)
            {
                panelRenderer = _panel.GetComponent<MeshRenderer>();
            }

            if (labelText == null)
            {
                var labelTransform = transform.Find("Label");
                if (labelTransform != null)
                {
                    labelText = labelTransform.GetComponent<TextMesh>();
                }
            }
        }

        private void NormalizeLayout()
        {
            CacheReferences();
            _rootBaseScale = Vector3.one;

            if (_trigger != null)
            {
                _trigger.isTrigger = true;
                _trigger.center = new Vector3(0f, 1f, 0f);
                var safeHitboxWidth = Mathf.Max(1.7f, hitboxWidth);
                _trigger.size = new Vector3(safeHitboxWidth, hitboxHeight, hitboxDepth);
            }

            var safePanelWidth = Mathf.Max(1.9f, panelWidth);
            var postOffset = (safePanelWidth * 0.5f) + 0.16f;
            if (_leftPost != null)
            {
                _leftPost.localPosition = new Vector3(-postOffset, 1f, 0f);
                _leftPost.localScale = new Vector3(postWidth, postHeight, postDepth);
            }

            if (_rightPost != null)
            {
                _rightPost.localPosition = new Vector3(postOffset, 1f, 0f);
                _rightPost.localScale = new Vector3(postWidth, postHeight, postDepth);
            }

            if (_panel != null)
            {
                _panel.localPosition = new Vector3(0f, 1f, 0f);
                _panel.localScale = new Vector3(safePanelWidth, panelHeight, 0.22f);
                _panelBaseScale = _panel.localScale;
            }

            if (labelText != null)
            {
                var labelTransform = labelText.transform;
                labelTransform.localPosition = new Vector3(0f, 1.06f, labelForwardOffset);
                labelTransform.localRotation = Quaternion.identity;
                labelTransform.localScale = Vector3.one * labelScale;
                _labelBaseScale = labelTransform.localScale;

                labelText.alignment = TextAlignment.Center;
                labelText.anchor = TextAnchor.MiddleCenter;
                labelText.characterSize = 0.25f;
                labelText.fontSize = 150;
            }
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

        private void AnimateIdle(float runTime)
        {
            var phase = runTime * idleFloatFrequency + _phaseOffset;
            var pulse = 1f + Mathf.Sin(phase * 2.25f) * idlePulseAmplitude;

            var position = transform.position;
            position.y = _baseY + Mathf.Sin(phase) * idleFloatAmplitude;
            transform.position = position;

            if (_panel != null)
            {
                _panel.localScale = new Vector3(_panelBaseScale.x * pulse, _panelBaseScale.y * pulse, _panelBaseScale.z);
            }

            if (labelText != null)
            {
                labelText.transform.localScale = _labelBaseScale * (1f + Mathf.Sin(phase * 1.9f) * (idlePulseAmplitude * 0.42f));
            }
        }

        private void AnimateConsume(float deltaTime)
        {
            _consumeElapsed += Mathf.Max(0f, deltaTime);
            var duration = Mathf.Max(0.02f, consumeDuration);
            var t = Mathf.Clamp01(_consumeElapsed / duration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);

            transform.localScale = Vector3.Lerp(_rootBaseScale, _rootBaseScale * 0.2f, eased);
            transform.Rotate(0f, consumeSpinDegrees * (deltaTime / duration), 0f, Space.Self);

            if (_panel != null)
            {
                _panel.localScale = Vector3.Lerp(_panelBaseScale, _panelBaseScale * 0.25f, eased);
            }

            if (labelText != null)
            {
                labelText.transform.localScale = Vector3.Lerp(_labelBaseScale, _labelBaseScale * 0.25f, eased);
            }

            if (t >= 1f)
            {
                _isConsuming = false;
                gameObject.SetActive(false);
            }
        }
    }
}
