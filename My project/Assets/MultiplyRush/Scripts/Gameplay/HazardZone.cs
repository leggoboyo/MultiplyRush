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
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Visuals")]
        public MeshRenderer zoneRenderer;
        public TextMesh labelText;
        public Color pitColor = new Color(0.1f, 0.12f, 0.18f, 1f);
        public Color knockbackColor = new Color(0.95f, 0.36f, 0.24f, 1f);
        public float pulseAmplitude = 0.08f;
        public float pulseSpeed = 3.8f;
        public float pitSpinOuterSpeed = 58f;
        public float pitSpinInnerSpeed = -86f;
        public float pitBreatheStrength = 0.12f;
        public float pitBreatheSpeed = 4.4f;

        [Header("Pit Drain")]
        [Range(0.02f, 0.24f)]
        public float pitTickInterval = 0.08f;
        [Range(1, 72)]
        public int pitMaxUnitsPerTick = 30;
        [Range(0f, 0.28f)]
        public float pitBoundsInset = 0.06f;

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
        private Color _pitSwirlColor = new Color(0.32f, 0.38f, 0.48f, 1f);
        private float _phaseOffset;
        private Transform _pitVisualRoot;
        private MeshRenderer _pitVoidRenderer;
        private MeshRenderer _pitRimRenderer;
        private MeshRenderer _pitWallRenderer;
        private MeshRenderer _pitCoreRenderer;
        private Transform _pitSwirlOuter;
        private Transform _pitSwirlInner;
        private MeshRenderer _pitSwirlOuterRenderer;
        private MeshRenderer _pitSwirlInnerRenderer;
        private MeshRenderer _pitGlowRenderer;
        private MaterialPropertyBlock _pitVoidBlock;
        private MaterialPropertyBlock _pitRimBlock;
        private MaterialPropertyBlock _pitWallBlock;
        private MaterialPropertyBlock _pitCoreBlock;
        private MaterialPropertyBlock _pitSwirlOuterBlock;
        private MaterialPropertyBlock _pitSwirlInnerBlock;
        private MaterialPropertyBlock _pitGlowBlock;
        private ParticleSystem _pitSwallowSystem;
        private float _pitTickTimer;
        private int _pitUnitsPerTick = 6;
        private Vector3 _pitRimBaseScale = new Vector3(1.08f, 0.04f, 1.08f);
        private Vector3 _pitVoidBaseScale = new Vector3(0.96f, 0.02f, 0.96f);
        private Vector3 _pitWallBaseScale = new Vector3(0.9f, 0.36f, 0.9f);
        private Vector3 _pitCoreBaseScale = new Vector3(0.58f, 0.48f, 0.58f);
        private Vector3 _pitSwirlOuterBaseScale = new Vector3(0.78f, 0.01f, 0.78f);
        private Vector3 _pitSwirlInnerBaseScale = new Vector3(0.56f, 0.01f, 0.56f);
        private Vector3 _pitGlowBaseScale = new Vector3(1.12f, 0.012f, 1.12f);

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
            _pitTickTimer = 0f;
            UpdateVisuals();
        }

        private void Update()
        {
            if (_consumed)
            {
                return;
            }

            if (_hazardType == HazardType.UnitPit)
            {
                AnimatePitVisuals();
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
                _trigger.center = new Vector3(0f, 0.2f, 0f);
                _trigger.size = new Vector3(1f, 0.4f, 1f);
            }
            else
            {
                _trigger.center = new Vector3(0f, 0.6f, 0f);
                _trigger.size = new Vector3(1f, 1.2f, 1f);
            }

            _trigger.enabled = true;
            _consumed = false;
            _pitTickTimer = 0f;
            _pitUnitsPerTick = ComputePitUnitsPerTick(transform.localScale.x, transform.localScale.z);

            if (labelText != null)
            {
                if (hazardType == HazardType.UnitPit)
                {
                    labelText.text = string.Empty;
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
                    ? new Vector3(0f, 0.48f, -0.08f)
                    : new Vector3(0f, 1.05f, -0.08f);
                labelText.transform.localScale = Vector3.one * (emphasize ? 0.17f : 0.14f);
                labelText.gameObject.SetActive(hazardType != HazardType.UnitPit);
            }

            _baseScale = transform.localScale;
            _baseColor = hazardType == HazardType.UnitPit ? pitColor : knockbackColor;
            if (emphasize)
            {
                _baseColor *= 1.14f;
            }

            _pitVoidColor = Color.Lerp(new Color(0.02f, 0.02f, 0.03f, 1f), _baseColor, 0.08f);
            _pitRimColor = Color.Lerp(_baseColor, new Color(0.08f, 0.09f, 0.12f, 1f), 0.74f);
            _pitSwirlColor = Color.Lerp(_pitRimColor, new Color(0.52f, 0.62f, 0.72f, 1f), 0.28f);
            ApplyPitVisualState();
            UpdateVisuals();
        }

        public bool TryApply(CrowdController crowd)
        {
            if (_consumed || crowd == null)
            {
                return false;
            }

            if (_hazardType == HazardType.UnitPit)
            {
                return false;
            }

            _consumed = true;
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            crowd.ApplyLateralImpulse(_knockbackDeltaX);
            crowd.ApplySlow(0.85f, 0.45f);
            gameObject.SetActive(false);
            return true;
        }

        public bool TryApplyContinuous(CrowdController crowd, float deltaTime)
        {
            if (_hazardType != HazardType.UnitPit || crowd == null || _consumed)
            {
                return false;
            }

            var tickDelta = Mathf.Max(0.005f, deltaTime);
            _pitTickTimer -= tickDelta;
            if (_pitTickTimer > 0f)
            {
                return false;
            }

            _pitTickTimer = Mathf.Max(0.02f, pitTickInterval);
            var pitBounds = GetPitInnerBounds();
            if (pitBounds.size.x <= 0f || pitBounds.size.z <= 0f)
            {
                return false;
            }

            var removed = crowd.ApplyPitOverlapLoss(pitBounds, _pitUnitsPerTick, pitBounds.center);
            if (removed > 0)
            {
                EmitPitSwallowBurst(pitBounds.center, removed);
            }

            return removed > 0;
        }

        private Bounds GetPitInnerBounds()
        {
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            var bounds = _trigger != null ? _trigger.bounds : new Bounds(transform.position, Vector3.zero);
            var inset = Mathf.Max(0f, pitBoundsInset);
            if (inset <= 0f)
            {
                return bounds;
            }

            var min = bounds.min + new Vector3(inset, 0f, inset);
            var max = bounds.max - new Vector3(inset, 0f, inset);
            if (max.x <= min.x || max.z <= min.z)
            {
                return bounds;
            }

            var adjustedBounds = new Bounds();
            adjustedBounds.SetMinMax(min, max);
            return adjustedBounds;
        }

        private int ComputePitUnitsPerTick(float width, float depth)
        {
            var laneUnitsAcross = Mathf.Max(1, Mathf.RoundToInt(width / 0.55f));
            var laneUnitsDeep = Mathf.Max(1, Mathf.RoundToInt(depth / 0.65f));
            var areaEstimate = laneUnitsAcross * laneUnitsDeep;
            var fractionEstimate = Mathf.RoundToInt(_unitLossFraction * 40f);
            var flatEstimate = Mathf.RoundToInt(_flatUnitLoss * 0.2f);
            return Mathf.Clamp(Mathf.Max(areaEstimate, Mathf.Max(fractionEstimate, flatEstimate)), 1, Mathf.Max(1, pitMaxUnitsPerTick));
        }

        private void AnimatePitVisuals()
        {
            EnsurePitVisuals();
            var time = Time.time;
            var breathe = 1f + Mathf.Sin((time * pitBreatheSpeed) + _phaseOffset) * pitBreatheStrength;
            if (_pitRimRenderer != null)
            {
                _pitRimRenderer.transform.localScale = _pitRimBaseScale * breathe;
            }

            if (_pitVoidRenderer != null)
            {
                var voidPulse = 1f + Mathf.Sin((time * pitBreatheSpeed * 1.35f) + (_phaseOffset * 0.8f)) * (pitBreatheStrength * 0.6f);
                var wobbleX = 1f + Mathf.Sin((time * 2.2f) + _phaseOffset) * 0.08f;
                var wobbleZ = 1f + Mathf.Cos((time * 2.5f) + (_phaseOffset * 1.3f)) * 0.08f;
                var scale = _pitVoidBaseScale * voidPulse;
                scale.x *= wobbleX;
                scale.z *= wobbleZ;
                _pitVoidRenderer.transform.localScale = scale;
            }

            if (_pitWallRenderer != null)
            {
                var wallPulse = 1f + Mathf.Sin((time * pitBreatheSpeed * 0.82f) + (_phaseOffset * 1.1f)) * 0.08f;
                _pitWallRenderer.transform.localScale = _pitWallBaseScale * wallPulse;
                _pitWallRenderer.transform.localPosition = new Vector3(0f, -0.19f - (Mathf.Abs(Mathf.Sin(time * 1.7f + _phaseOffset)) * 0.05f), 0f);
            }

            if (_pitCoreRenderer != null)
            {
                var corePulse = 1f + Mathf.Sin((time * pitBreatheSpeed * 1.7f) + (_phaseOffset * 1.8f)) * 0.12f;
                _pitCoreRenderer.transform.localScale = _pitCoreBaseScale * corePulse;
                _pitCoreRenderer.transform.localPosition = new Vector3(0f, -0.32f - (Mathf.Abs(Mathf.Sin(time * 2.3f + _phaseOffset)) * 0.08f), 0f);
            }

            if (_pitSwirlOuter != null)
            {
                _pitSwirlOuter.localRotation = Quaternion.Euler(0f, (time * pitSpinOuterSpeed) + (_phaseOffset * Mathf.Rad2Deg), 0f);
                _pitSwirlOuter.localScale = _pitSwirlOuterBaseScale * (1f + Mathf.Sin((time * 3.2f) + _phaseOffset) * 0.08f);
            }

            if (_pitSwirlInner != null)
            {
                _pitSwirlInner.localRotation = Quaternion.Euler(0f, (time * pitSpinInnerSpeed) - (_phaseOffset * Mathf.Rad2Deg), 0f);
                _pitSwirlInner.localScale = _pitSwirlInnerBaseScale * (1f + Mathf.Sin((time * 4.1f) + (_phaseOffset * 1.3f)) * 0.09f);
            }

            if (_pitGlowRenderer != null)
            {
                var glowScale = 1f + Mathf.Sin((time * 2.6f) + _phaseOffset) * 0.07f;
                _pitGlowRenderer.transform.localScale = _pitGlowBaseScale * glowScale;
            }
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

            ApplyRendererColor(_pitVoidRenderer, ref _pitVoidBlock, _pitVoidColor);
            ApplyRendererColor(_pitRimRenderer, ref _pitRimBlock, _pitRimColor);
            ApplyRendererColor(_pitWallRenderer, ref _pitWallBlock, Color.Lerp(_pitVoidColor, Color.black, 0.28f));
            ApplyRendererColor(_pitCoreRenderer, ref _pitCoreBlock, Color.Lerp(_pitVoidColor, Color.black, 0.46f));
            ApplyRendererColor(_pitSwirlOuterRenderer, ref _pitSwirlOuterBlock, _pitSwirlColor);
            ApplyRendererColor(_pitSwirlInnerRenderer, ref _pitSwirlInnerBlock, Color.Lerp(_pitSwirlColor, Color.black, 0.22f));
            ApplyRendererColor(_pitGlowRenderer, ref _pitGlowBlock, Color.Lerp(_pitRimColor, Color.white, 0.24f));
        }

        private void ApplyRendererColor(MeshRenderer renderer, ref MaterialPropertyBlock block, Color color)
        {
            if (renderer == null || !renderer.enabled)
            {
                return;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            block.SetColor(EmissionColorId, color * 0.18f);
            renderer.SetPropertyBlock(block);
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
            rim.transform.localScale = _pitRimBaseScale;
            StripCollider(rim);
            _pitRimRenderer = rim.GetComponent<MeshRenderer>();

            var voidObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidObj.name = "PitVoid";
            voidObj.transform.SetParent(_pitVisualRoot, false);
            voidObj.transform.localPosition = new Vector3(0f, -0.035f, 0f);
            voidObj.transform.localRotation = Quaternion.identity;
            voidObj.transform.localScale = _pitVoidBaseScale;
            StripCollider(voidObj);
            _pitVoidRenderer = voidObj.GetComponent<MeshRenderer>();

            var wallObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wallObj.name = "PitWall";
            wallObj.transform.SetParent(_pitVisualRoot, false);
            wallObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            wallObj.transform.localRotation = Quaternion.identity;
            wallObj.transform.localScale = _pitWallBaseScale;
            StripCollider(wallObj);
            _pitWallRenderer = wallObj.GetComponent<MeshRenderer>();

            var coreObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coreObj.name = "PitCore";
            coreObj.transform.SetParent(_pitVisualRoot, false);
            coreObj.transform.localPosition = new Vector3(0f, -0.34f, 0f);
            coreObj.transform.localRotation = Quaternion.identity;
            coreObj.transform.localScale = _pitCoreBaseScale;
            StripCollider(coreObj);
            _pitCoreRenderer = coreObj.GetComponent<MeshRenderer>();

            var swirlOuter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            swirlOuter.name = "PitSwirlOuter";
            swirlOuter.transform.SetParent(_pitVisualRoot, false);
            swirlOuter.transform.localPosition = new Vector3(0f, -0.048f, 0f);
            swirlOuter.transform.localRotation = Quaternion.identity;
            swirlOuter.transform.localScale = _pitSwirlOuterBaseScale;
            StripCollider(swirlOuter);
            _pitSwirlOuter = swirlOuter.transform;
            _pitSwirlOuterRenderer = swirlOuter.GetComponent<MeshRenderer>();

            var swirlInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            swirlInner.name = "PitSwirlInner";
            swirlInner.transform.SetParent(_pitVisualRoot, false);
            swirlInner.transform.localPosition = new Vector3(0f, -0.053f, 0f);
            swirlInner.transform.localRotation = Quaternion.identity;
            swirlInner.transform.localScale = _pitSwirlInnerBaseScale;
            StripCollider(swirlInner);
            _pitSwirlInner = swirlInner.transform;
            _pitSwirlInnerRenderer = swirlInner.GetComponent<MeshRenderer>();

            var glowObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowObj.name = "PitGlow";
            glowObj.transform.SetParent(_pitVisualRoot, false);
            glowObj.transform.localPosition = new Vector3(0f, -0.012f, 0f);
            glowObj.transform.localRotation = Quaternion.identity;
            glowObj.transform.localScale = _pitGlowBaseScale;
            StripCollider(glowObj);
            _pitGlowRenderer = glowObj.GetComponent<MeshRenderer>();

            var swallowObj = new GameObject("PitSwallowFX");
            swallowObj.transform.SetParent(_pitVisualRoot, false);
            swallowObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            _pitSwallowSystem = swallowObj.AddComponent<ParticleSystem>();
            var swallowRenderer = swallowObj.GetComponent<ParticleSystemRenderer>();
            if (swallowRenderer != null && zoneRenderer != null)
            {
                swallowRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                swallowRenderer.material = zoneRenderer.sharedMaterial;
            }

            ConfigurePitSwallowSystem(_pitSwallowSystem);

            if (zoneRenderer != null)
            {
                var shared = zoneRenderer.sharedMaterial;
                if (shared != null)
                {
                    if (_pitRimRenderer != null)
                    {
                        _pitRimRenderer.sharedMaterial = shared;
                    }

                    if (_pitVoidRenderer != null)
                    {
                        _pitVoidRenderer.sharedMaterial = shared;
                    }

                    if (_pitWallRenderer != null)
                    {
                        _pitWallRenderer.sharedMaterial = shared;
                    }

                    if (_pitCoreRenderer != null)
                    {
                        _pitCoreRenderer.sharedMaterial = shared;
                    }

                    if (_pitSwirlOuterRenderer != null)
                    {
                        _pitSwirlOuterRenderer.sharedMaterial = shared;
                    }

                    if (_pitSwirlInnerRenderer != null)
                    {
                        _pitSwirlInnerRenderer.sharedMaterial = shared;
                    }

                    if (_pitGlowRenderer != null)
                    {
                        _pitGlowRenderer.sharedMaterial = shared;
                    }
                }
            }
        }

        private static void StripCollider(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private static void ConfigurePitSwallowSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.42f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.16f, 0.22f, 0.28f, 1f));
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.38f, 0.5f, 0.62f, 1f), 0f),
                    new GradientColorKey(new Color(0.12f, 0.16f, 0.2f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.88f, 0f),
                    new GradientAlphaKey(0.6f, 0.32f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void EmitPitSwallowBurst(Vector3 center, int removedUnits)
        {
            if (_pitSwallowSystem == null || removedUnits <= 0)
            {
                return;
            }

            var emitCount = Mathf.Clamp(removedUnits * 2, 4, 64);
            for (var i = 0; i < emitCount; i++)
            {
                var radial = Random.insideUnitCircle * 0.42f;
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = new Vector3(center.x + radial.x, center.y + 0.02f, center.z + radial.y),
                    velocity = new Vector3(
                        (center.x - (center.x + radial.x)) * Random.Range(1.1f, 2.3f),
                        Random.Range(-0.18f, 0.24f),
                        (center.z - (center.z + radial.y)) * Random.Range(1.1f, 2.3f)),
                    startSize = Random.Range(0.045f, 0.11f),
                    startLifetime = Random.Range(0.12f, 0.26f)
                };
                _pitSwallowSystem.Emit(emitParams, 1);
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
