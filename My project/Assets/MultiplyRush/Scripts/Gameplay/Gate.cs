using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Gate : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Gate Data")]
        public GateOperation operation = GateOperation.Add;
        public int value = 5;
        public int rowId = -1;
        public GatePickTier pickTier = GatePickTier.WorseGood;

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

        [Header("Motion")]
        public bool enableHorizontalMotion;
        public float horizontalAmplitude = 0.2f;
        public float horizontalSpeed = 1f;
        public float horizontalPhase;
        public float laneCenterX;
        public float laneMinX;
        public float laneMaxX;

        [Header("Tempo")]
        public bool enableTempoWindow;
        public float tempoCycleSeconds = 2.1f;
        [Range(0.08f, 0.92f)]
        public float tempoOpenRatio = 0.55f;
        public float tempoPhaseOffset;
        [Range(1f, 1.2f)]
        public float tempoPanelPulseScale = 1.05f;
        [Range(1f, 1.25f)]
        public float tempoLabelPulseScale = 1.08f;

        [Header("Shot Upgrade")]
        public bool allowShotUpgrades = true;
        public int shotsForFirstUpgrade = 12;
        public int shotsPerUpgradeGrowth = 4;
        public int addUpgradeMaxBonus = 8;
        public int multiplierUpgradeTenthsCap = 30;
        public float shotUpgradePulseDuration = 0.2f;
        public float shotUpgradePulseScale = 1.18f;

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
        private Color _basePanelColor = Color.white;
        private float _tempoPanelScale = 1f;
        private float _tempoLabelScale = 1f;
        private int _baseValue;
        private int _runtimeAddValue;
        private int _runtimeMultiplierTenths;
        private int _baseMultiplierTenths;
        private int _shotUpgradeCounter;
        private int _shotUpgradeStep;
        private int _nextShotUpgradeThreshold;
        private float _shotUpgradePulseTimer;

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
            laneCenterX = transform.position.x;
            transform.localScale = _rootBaseScale;
            transform.rotation = Quaternion.identity;
            _tempoPanelScale = 1f;
            _tempoLabelScale = 1f;
            ResetRuntimeGateValues();
            NormalizeLayout();
            RefreshVisuals();
        }

        public void Configure(
            GateOperation gateOperation,
            int gateValue,
            int gateRowId,
            GatePickTier gatePickTier,
            bool allowHorizontalMotion,
            float motionAmplitude,
            float motionSpeed,
            float motionPhase,
            float centerX,
            float minX,
            float maxX,
            bool useTempoWindow,
            float tempoCycle,
            float openRatio,
            float tempoPhase)
        {
            operation = gateOperation;
            value = Mathf.Max(1, gateValue);
            rowId = gateRowId;
            pickTier = gatePickTier;
            enableHorizontalMotion = allowHorizontalMotion;
            horizontalAmplitude = Mathf.Max(0f, motionAmplitude);
            horizontalSpeed = Mathf.Max(0.1f, motionSpeed);
            horizontalPhase = motionPhase;
            laneCenterX = centerX;
            laneMinX = Mathf.Min(minX, maxX);
            laneMaxX = Mathf.Max(minX, maxX);
            enableTempoWindow = useTempoWindow;
            tempoCycleSeconds = Mathf.Max(0.25f, tempoCycle);
            tempoOpenRatio = Mathf.Clamp(openRatio, 0.08f, 0.92f);
            tempoPhaseOffset = tempoPhase;
            _isConsumed = false;
            _isConsuming = false;
            _consumeElapsed = 0f;
            _baseY = transform.position.y;
            _tempoPanelScale = 1f;
            _tempoLabelScale = 1f;
            ResetRuntimeGateValues();

            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
            NormalizeLayout();
            RefreshVisuals();
        }

        public void ConfigureShotUpgrade(
            bool enabled,
            int firstThreshold,
            int growthPerUpgrade,
            int maxAddBonus,
            int multiplierTenthsCap)
        {
            allowShotUpgrades = enabled;
            shotsForFirstUpgrade = Mathf.Max(1, firstThreshold);
            shotsPerUpgradeGrowth = Mathf.Max(1, growthPerUpgrade);
            addUpgradeMaxBonus = Mathf.Max(0, maxAddBonus);
            multiplierUpgradeTenthsCap = Mathf.Max(10, multiplierTenthsCap);
            ResetRuntimeGateValues();
            RefreshVisuals();
        }

        public bool TryApply(CrowdController crowd)
        {
            if (_isConsumed || crowd == null)
            {
                return false;
            }

            _isConsumed = true;
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            crowd.ApplyGate(operation, GetEffectiveIntValue(), GetEffectiveMultiplierValue());
            if (consumeDuration <= 0f)
            {
                gameObject.SetActive(false);
                return true;
            }

            _isConsuming = true;
            _consumeElapsed = 0f;
            return true;
        }

        public bool TryRegisterShotUpgrade(int levelIndex)
        {
            if (_isConsumed || !allowShotUpgrades || !IsPositive(operation))
            {
                return false;
            }

            if (_nextShotUpgradeThreshold <= 0)
            {
                var fallback = Mathf.Max(4, 8 + Mathf.FloorToInt(Mathf.Max(1, levelIndex) * 0.7f));
                _nextShotUpgradeThreshold = fallback;
            }

            _shotUpgradeCounter++;
            var upgraded = false;
            var guard = 0;
            while (_shotUpgradeCounter >= _nextShotUpgradeThreshold && guard < 6)
            {
                guard++;
                if (operation == GateOperation.Add)
                {
                    var cap = _baseValue + Mathf.Max(0, addUpgradeMaxBonus);
                    if (_runtimeAddValue >= cap)
                    {
                        break;
                    }

                    _runtimeAddValue++;
                    upgraded = true;
                }
                else if (operation == GateOperation.Multiply)
                {
                    var capTenths = Mathf.Max(_baseMultiplierTenths, multiplierUpgradeTenthsCap);
                    if (_runtimeMultiplierTenths >= capTenths)
                    {
                        break;
                    }

                    _runtimeMultiplierTenths++;
                    upgraded = true;
                }

                _shotUpgradeStep++;
                _nextShotUpgradeThreshold += Mathf.Max(1, shotsPerUpgradeGrowth + _shotUpgradeStep);
            }

            if (upgraded)
            {
                _shotUpgradePulseTimer = Mathf.Max(_shotUpgradePulseTimer, Mathf.Max(0.08f, shotUpgradePulseDuration));
                RefreshVisuals();
            }

            return upgraded;
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

            if (_shotUpgradePulseTimer > 0f)
            {
                _shotUpgradePulseTimer = Mathf.Max(0f, _shotUpgradePulseTimer - Time.deltaTime);
            }

            UpdateTempoWindow(Time.time);
            AnimateIdle(Time.time);
        }

        private void RefreshVisuals()
        {
            if (labelText != null)
            {
                labelText.text = GetLabel(operation, GetEffectiveIntValue(), GetEffectiveMultiplierValue());
                labelText.color = IsPositive(operation) ? new Color(0.05f, 0.05f, 0.05f) : Color.white;
                labelText.fontStyle = FontStyle.Bold;
            }

            _basePanelColor = IsPositive(operation) ? positiveColor : negativeColor;
            SetPanelColor(_basePanelColor);
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
                var safeHitboxWidth = Mathf.Max(1.2f, hitboxWidth);
                _trigger.size = new Vector3(safeHitboxWidth, hitboxHeight, hitboxDepth);
            }

            var safePanelWidth = Mathf.Max(1.4f, panelWidth);
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
            _materialBlock.SetColor(EmissionColorId, color * 0.42f);
            panelRenderer.SetPropertyBlock(_materialBlock);
        }

        private void ResetRuntimeGateValues()
        {
            _baseValue = Mathf.Max(1, value);
            _runtimeAddValue = _baseValue;
            _baseMultiplierTenths = Mathf.Max(10, Mathf.RoundToInt(_baseValue * 10f));
            _runtimeMultiplierTenths = _baseMultiplierTenths;
            _shotUpgradeCounter = 0;
            _shotUpgradeStep = 0;
            _nextShotUpgradeThreshold = Mathf.Max(1, shotsForFirstUpgrade);
            _shotUpgradePulseTimer = 0f;
        }

        private int GetEffectiveIntValue()
        {
            switch (operation)
            {
                case GateOperation.Add:
                    return Mathf.Max(1, _runtimeAddValue);
                case GateOperation.Multiply:
                    return Mathf.Max(1, Mathf.RoundToInt(GetEffectiveMultiplierValue()));
                default:
                    return Mathf.Max(1, value);
            }
        }

        private float GetEffectiveMultiplierValue()
        {
            if (operation != GateOperation.Multiply)
            {
                return 0f;
            }

            return Mathf.Max(1f, _runtimeMultiplierTenths * 0.1f);
        }

        private float EvaluateShotUpgradePulse()
        {
            if (_shotUpgradePulseTimer <= 0f || shotUpgradePulseDuration <= 0f)
            {
                return 1f;
            }

            var normalized = 1f - Mathf.Clamp01(_shotUpgradePulseTimer / shotUpgradePulseDuration);
            return Mathf.Lerp(1f, Mathf.Max(1f, shotUpgradePulseScale), Mathf.Sin(normalized * Mathf.PI));
        }

        private static bool IsPositive(GateOperation gateOperation)
        {
            return gateOperation == GateOperation.Add || gateOperation == GateOperation.Multiply;
        }

        private static string GetLabel(GateOperation gateOperation, int gateValue, float multiplierValue)
        {
            switch (gateOperation)
            {
                case GateOperation.Add:
                    return "+" + gateValue;
                case GateOperation.Subtract:
                    return "-" + gateValue;
                case GateOperation.Multiply:
                {
                    var formatted = Mathf.Max(1f, multiplierValue);
                    var roundedTenths = Mathf.RoundToInt(formatted * 10f);
                    var whole = roundedTenths / 10;
                    var tenths = roundedTenths % 10;
                    return tenths == 0 ? "x" + whole : "x" + whole + "." + tenths;
                }
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
            pulse *= EvaluateShotUpgradePulse();

            var position = transform.position;
            if (enableHorizontalMotion)
            {
                var horizontalPhaseValue = (runTime * horizontalSpeed) + horizontalPhase;
                var x = laneCenterX + (Mathf.Sin(horizontalPhaseValue) * horizontalAmplitude);
                position.x = Mathf.Clamp(x, laneMinX, laneMaxX);
            }
            else
            {
                position.x = laneCenterX;
            }

            position.y = _baseY + Mathf.Sin(phase) * idleFloatAmplitude;
            transform.position = position;

            if (_panel != null)
            {
                _panel.localScale = new Vector3(
                    _panelBaseScale.x * pulse * _tempoPanelScale,
                    _panelBaseScale.y * pulse * _tempoPanelScale,
                    _panelBaseScale.z);
            }

            if (labelText != null)
            {
                labelText.transform.localScale = _labelBaseScale * (1f + Mathf.Sin(phase * 1.9f) * (idlePulseAmplitude * 0.42f)) * _tempoLabelScale;
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

        private void UpdateTempoWindow(float runTime)
        {
            if (_trigger == null)
            {
                return;
            }

            if (!_trigger.enabled)
            {
                _trigger.enabled = true;
            }

            if (!enableTempoWindow)
            {
                _tempoPanelScale = 1f;
                _tempoLabelScale = 1f;
                SetPanelColor(_basePanelColor);
                if (labelText != null)
                {
                    labelText.color = IsPositive(operation) ? new Color(0.05f, 0.05f, 0.05f) : Color.white;
                }

                return;
            }

            var cycle = Mathf.Max(0.25f, tempoCycleSeconds);
            var ratio = Mathf.Clamp(tempoOpenRatio, 0.08f, 0.92f);
            var phaseT = Mathf.Repeat((runTime + tempoPhaseOffset) / cycle, 1f);
            var syncPulse = 0.5f + (Mathf.Sin(phaseT * Mathf.PI * 2f) * 0.5f);
            var accent = Mathf.SmoothStep(0f, 1f, syncPulse * ratio + (1f - ratio) * 0.5f);
            _tempoPanelScale = Mathf.Lerp(1f, Mathf.Max(1f, tempoPanelPulseScale), accent);
            _tempoLabelScale = Mathf.Lerp(1f, Mathf.Max(1f, tempoLabelPulseScale), accent);

            SetPanelColor(Color.Lerp(_basePanelColor * 0.84f, _basePanelColor * 1.18f, accent));
            if (labelText != null)
            {
                var baseColor = IsPositive(operation) ? new Color(0.05f, 0.05f, 0.05f) : Color.white;
                labelText.color = baseColor;
            }
        }
    }
}
