using UnityEngine;

namespace MultiplyRush
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FinishLine : MonoBehaviour
    {
        [Header("References")]
        public EnemyGroup enemyGroup;
        public TextMesh enemyCountLabel;
        public TextMesh tankRequirementLabel;

        [Header("Label Animation")]
        public float labelPulseAmplitude = 0.08f;
        public float labelPulseSpeed = 2.5f;

        [Header("Enemy Placement")]
        public Vector3 enemyGroupLocalOffset = new Vector3(0f, 0f, 6.6f);
        public Vector3 enemyGroupLocalScale = Vector3.one;
        public float minEnemyDistanceBehindLine = 5.4f;

        [Header("Mini-Boss Visual")]
        public Vector3 bossLocalOffset = new Vector3(0f, 0.24f, 9.8f);
        public Vector3 bossLocalScale = new Vector3(2.35f, 2.35f, 2.35f);
        public float bossPulseAmplitude = 0.04f;
        public float bossPulseSpeed = 2.1f;
        public float bossSlamDropDistance = 0.95f;
        public float bossSlamRecoverSpeed = 4.8f;
        public Color bossBodyColor = new Color(0.42f, 0.44f, 0.48f, 1f);
        public Color bossAccentColor = new Color(1f, 0.58f, 0.24f, 1f);

        private BoxCollider _trigger;
        private bool _isTriggered;
        private int _enemyCount;
        private int _tankRequirement;
        private bool _isMiniBoss;
        private Vector3 _labelBaseScale = Vector3.one;
        private Vector3 _tankLabelBaseScale = Vector3.one;
        private Transform _bossVisual;
        private Transform _bossTurret;
        private Vector3 _bossBaseScale = Vector3.one;
        private Vector3 _bossBaseLocalPosition = Vector3.zero;
        private float _bossSlamOffset;
        private Material _bossBodyMaterial;
        private Material _bossAccentMaterial;

        public int EnemyCount => _enemyCount;
        public int TankRequirement => _tankRequirement;
        public bool IsMiniBoss => _isMiniBoss;
        public Transform BossVisual => _bossVisual;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            EnsureLabels();
            EnsureBossVisual();
            if (enemyCountLabel != null)
            {
                _labelBaseScale = enemyCountLabel.transform.localScale;
            }

            if (tankRequirementLabel != null)
            {
                _tankLabelBaseScale = tankRequirementLabel.transform.localScale;
            }

            if (_bossVisual != null)
            {
                _bossVisual.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _isTriggered = false;
            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider>();
            }

            _trigger.enabled = true;
        }

        private void Update()
        {
            if (enemyCountLabel != null && enemyCountLabel.gameObject.activeInHierarchy)
            {
                var pulse = 1f + Mathf.Sin(Time.time * labelPulseSpeed) * labelPulseAmplitude;
                enemyCountLabel.transform.localScale = _labelBaseScale * pulse;
            }

            if (tankRequirementLabel != null && tankRequirementLabel.gameObject.activeInHierarchy)
            {
                var pulse = 1f + Mathf.Sin(Time.time * (labelPulseSpeed * 1.12f) + 0.8f) * (labelPulseAmplitude * 0.9f);
                tankRequirementLabel.transform.localScale = _tankLabelBaseScale * pulse;
            }

            if (_bossVisual != null && _bossVisual.gameObject.activeInHierarchy)
            {
                var pulse = 1f + Mathf.Sin(Time.time * bossPulseSpeed) * bossPulseAmplitude;
                _bossVisual.localScale = _bossBaseScale * pulse;
                _bossSlamOffset = Mathf.MoveTowards(_bossSlamOffset, 0f, Mathf.Max(0.25f, bossSlamRecoverSpeed) * Time.deltaTime);
                _bossVisual.localPosition = _bossBaseLocalPosition + Vector3.down * _bossSlamOffset;
                if (_bossTurret != null)
                {
                    _bossTurret.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.92f) * 16f, 0f);
                }
            }
        }

        public void Configure(int enemyCount, int tankRequirement, bool isMiniBoss)
        {
            EnsureLabels();
            EnsureBossVisual();

            _enemyCount = Mathf.Max(1, enemyCount);
            // Tank requirement is only meaningful on mini-boss levels.
            _tankRequirement = isMiniBoss ? Mathf.Max(0, tankRequirement) : 0;
            _isMiniBoss = isMiniBoss;

            var distanceBehindLine = Mathf.Max(Mathf.Abs(enemyGroupLocalOffset.z), Mathf.Max(1f, minEnemyDistanceBehindLine));
            if (enemyGroup != null)
            {
                if (isMiniBoss)
                {
                    enemyGroup.gameObject.SetActive(false);
                    enemyGroup.EndCombat();
                }
                else
                {
                    var safeOffset = enemyGroupLocalOffset;
                    var formationDepth = enemyGroup.EstimateFormationDepth(_enemyCount);
                    distanceBehindLine = Mathf.Max(distanceBehindLine, formationDepth * 0.7f + Mathf.Max(1f, minEnemyDistanceBehindLine));
                    var worldPosition = new Vector3(
                        transform.position.x + safeOffset.x,
                        transform.position.y + safeOffset.y,
                        transform.position.z + distanceBehindLine);

                    enemyGroup.gameObject.SetActive(true);
                    enemyGroup.transform.SetParent(transform, true);
                    enemyGroup.transform.position = worldPosition;
                    enemyGroup.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    enemyGroup.transform.localScale = enemyGroupLocalScale;
                    enemyGroup.SetCount(_enemyCount);
                }
            }

            if (enemyCountLabel != null)
            {
                enemyCountLabel.text = isMiniBoss ? "BOSS HP " + _enemyCount : "Enemy " + _enemyCount;
                enemyCountLabel.color = isMiniBoss ? new Color(1f, 0.52f, 0.3f, 1f) : Color.white;
                enemyCountLabel.transform.localPosition = isMiniBoss
                    ? new Vector3(0f, 2.6f, 0.8f)
                    : new Vector3(0f, 2.05f, -0.44f);
            }

            if (tankRequirementLabel != null)
            {
                tankRequirementLabel.gameObject.SetActive(false);
            }

            if (_bossVisual != null)
            {
                _bossVisual.gameObject.SetActive(isMiniBoss);
                if (isMiniBoss)
                {
                    var safeBossZ = Mathf.Max(Mathf.Abs(bossLocalOffset.z), distanceBehindLine + 1.3f);
                    _bossBaseLocalPosition = new Vector3(bossLocalOffset.x, bossLocalOffset.y, safeBossZ);
                    _bossVisual.localPosition = _bossBaseLocalPosition;
                    _bossVisual.localRotation = Quaternion.identity;
                    _bossVisual.localScale = bossLocalScale;
                    _bossBaseScale = _bossVisual.localScale;
                    _bossSlamOffset = 0f;
                }
            }
        }

        public void TryTrigger(CrowdController crowd)
        {
            if (_isTriggered || crowd == null)
            {
                return;
            }

            _isTriggered = true;
            if (_trigger != null)
            {
                _trigger.enabled = false;
            }

            crowd.NotifyFinishReached(_enemyCount);
        }

        public void SetBossVisualActive(bool active)
        {
            if (_bossVisual == null)
            {
                return;
            }

            _bossVisual.gameObject.SetActive(active);
            if (active)
            {
                _bossVisual.localPosition = _bossBaseLocalPosition;
            }
            else
            {
                _bossSlamOffset = 0f;
            }
        }

        public void TriggerBossSlam(float strength = 1f)
        {
            if (_bossVisual == null || !_bossVisual.gameObject.activeInHierarchy)
            {
                return;
            }

            var safeStrength = Mathf.Clamp(strength, 0.4f, 1.6f);
            _bossSlamOffset = Mathf.Max(_bossSlamOffset, bossSlamDropDistance * safeStrength);
        }

        private void EnsureLabels()
        {
            if (enemyCountLabel == null)
            {
                var enemyLabelTransform = transform.Find("EnemyLabel");
                if (enemyLabelTransform != null)
                {
                    enemyCountLabel = enemyLabelTransform.GetComponent<TextMesh>();
                }
            }

            if (tankRequirementLabel != null)
            {
                return;
            }

            var tankLabelTransform = transform.Find("TankLabel");
            if (tankLabelTransform == null)
            {
                tankLabelTransform = new GameObject("TankLabel").transform;
                tankLabelTransform.SetParent(transform, false);
            }

            tankRequirementLabel = tankLabelTransform.GetComponent<TextMesh>();
            if (tankRequirementLabel == null)
            {
                tankRequirementLabel = tankLabelTransform.gameObject.AddComponent<TextMesh>();
            }

            tankLabelTransform.localPosition = new Vector3(0f, 2.6f, -0.48f);
            tankLabelTransform.localRotation = Quaternion.identity;
            tankLabelTransform.localScale = Vector3.one * 0.18f;

            tankRequirementLabel.text = string.Empty;
            tankRequirementLabel.anchor = TextAnchor.MiddleCenter;
            tankRequirementLabel.alignment = TextAlignment.Center;
            tankRequirementLabel.characterSize = 0.22f;
            tankRequirementLabel.fontSize = 130;
            tankRequirementLabel.gameObject.SetActive(false);
        }

        private void EnsureBossVisual()
        {
            if (_bossVisual != null)
            {
                return;
            }

            var existing = transform.Find("BossUnit");
            if (existing != null)
            {
                _bossVisual = existing;
                _bossTurret = _bossVisual.Find("Turret");
                _bossBaseScale = _bossVisual.localScale;
                _bossBaseLocalPosition = _bossVisual.localPosition;
                return;
            }

            EnsureBossMaterials();
            var bossRoot = new GameObject("BossUnit").transform;
            bossRoot.SetParent(transform, false);

            CreateBossPart(bossRoot, "Hull", PrimitiveType.Cube, _bossBodyMaterial, new Vector3(0f, 0.58f, 0f), new Vector3(1.2f, 0.44f, 1.58f));
            CreateBossPart(bossRoot, "FrontPlate", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 0.62f, 0.78f), new Vector3(0.9f, 0.28f, 0.12f));
            CreateBossPart(bossRoot, "TrackLeft", PrimitiveType.Cube, _bossBodyMaterial, new Vector3(-0.62f, 0.3f, 0f), new Vector3(0.34f, 0.38f, 1.66f));
            CreateBossPart(bossRoot, "TrackRight", PrimitiveType.Cube, _bossBodyMaterial, new Vector3(0.62f, 0.3f, 0f), new Vector3(0.34f, 0.38f, 1.66f));
            CreateBossPart(bossRoot, "WheelGlowLeft", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(-0.62f, 0.18f, 0.24f), new Vector3(0.08f, 0.08f, 0.08f), new Vector3(90f, 0f, 0f));
            CreateBossPart(bossRoot, "WheelGlowRight", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(0.62f, 0.18f, 0.24f), new Vector3(0.08f, 0.08f, 0.08f), new Vector3(90f, 0f, 0f));

            _bossTurret = CreateBossPart(bossRoot, "Turret", PrimitiveType.Cylinder, _bossBodyMaterial, new Vector3(0f, 0.94f, 0f), new Vector3(0.66f, 0.26f, 0.66f));
            CreateBossPart(_bossTurret, "TurretTop", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 0.24f, 0f), new Vector3(0.46f, 0.12f, 0.46f));
            var barrel = CreateBossPart(_bossTurret, "Barrel", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0f, 0.01f, 1.02f), new Vector3(0.12f, 0.12f, 1.32f));
            CreateBossPart(barrel, "Muzzle", PrimitiveType.Cylinder, _bossAccentMaterial, new Vector3(0f, 0f, 0.54f), new Vector3(0.12f, 0.08f, 0.12f), new Vector3(90f, 0f, 0f));
            CreateBossPart(bossRoot, "Antenna", PrimitiveType.Cube, _bossAccentMaterial, new Vector3(0.34f, 1.28f, -0.22f), new Vector3(0.03f, 0.44f, 0.03f));
            CreateBossPart(bossRoot, "AntennaTip", PrimitiveType.Sphere, _bossAccentMaterial, new Vector3(0.34f, 1.52f, -0.22f), new Vector3(0.08f, 0.08f, 0.08f));

            bossRoot.localPosition = bossLocalOffset;
            bossRoot.localRotation = Quaternion.identity;
            bossRoot.localScale = bossLocalScale;
            _bossVisual = bossRoot;
            _bossBaseScale = bossRoot.localScale;
            _bossBaseLocalPosition = bossRoot.localPosition;
            _bossVisual.gameObject.SetActive(false);
        }

        private void EnsureBossMaterials()
        {
            if (_bossBodyMaterial == null)
            {
                _bossBodyMaterial = CreateRuntimeMaterial("BossBodyMaterial", bossBodyColor, 0.2f, 0.12f);
            }

            if (_bossAccentMaterial == null)
            {
                _bossAccentMaterial = CreateRuntimeMaterial("BossAccentMaterial", bossAccentColor, 0.34f, 0.42f);
            }
        }

        private static Transform CreateBossPart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler = default)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return part.transform;
        }

        private static Material CreateRuntimeMaterial(string name, Color color, float smoothness, float emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Clamp(emission, 0f, 2f));
            }

            return material;
        }
    }
}
