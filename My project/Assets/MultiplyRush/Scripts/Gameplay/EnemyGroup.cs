using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    public sealed class EnemyGroup : MonoBehaviour
    {
        [Header("Visuals")]
        public GameObject enemyUnitPrefab;
        public Transform unitsRoot;
        public TextMesh countLabel;
        public int maxVisibleUnits = 120;
        public int initialPoolSize = 70;
        public int maxColumns = 8;
        public float spacingX = 0.74f;
        public float spacingZ = 0.82f;
        public float formationLerpSpeed = 14f;
        public float bobAmplitude = 0.06f;
        public float bobFrequency = 7.8f;
        public float tiltDegrees = 5f;
        [Range(0.8f, 2.5f)]
        public float unitVisualScale = 1.9f;

        [Header("Weapon VFX")]
        public bool enableWeaponVfx = true;
        public float baseShotsPerSecond = 11f;
        public float shotsPerUnit = 2.1f;
        public float maxShotsPerSecond = 260f;
        public float tracerSpeed = 31f;
        public float tracerLifetime = 0.2f;
        public float tracerSpread = 0.08f;
        public Color tracerColor = new Color(1f, 0.5f, 0.38f, 1f);

        [Header("Death FX")]
        public bool enableDeathFx = true;
        public int maxDeathFxPerLossWave = 18;
        public float deathFxDuration = 0.42f;
        public float deathFxRandomImpulse = 0.85f;
        public float deathFxGravity = 10.5f;

        private readonly List<Transform> _activeUnits = new List<Transform>(120);
        private readonly List<Transform> _unitMuzzles = new List<Transform>(120);
        private readonly List<SoldierMotionAnimator> _unitAnimators = new List<SoldierMotionAnimator>(120);
        private readonly Stack<Transform> _pool = new Stack<Transform>(120);
        private readonly List<Vector3> _slots = new List<Vector3>(120);
        private readonly List<float> _phaseOffsets = new List<float>(120);
        private Transform _poolRoot;
        private int _count;
        private bool _combatActive;
        private Transform _combatTarget;
        private ParticleSystem _tracerSystem;
        private ParticleSystem _lossBurstSystem;
        private float _shotAccumulator;
        private int _pendingDeathFxBudget;
        private int _nextShooterIndex;
        private TextMesh _countLabelShadow;

        public int Count => _count;

        private void Awake()
        {
            if (unitsRoot == null)
            {
                var root = new GameObject("EnemyUnits").transform;
                root.SetParent(transform, false);
                unitsRoot = root;
            }

            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("EnemyPool").transform;
                _poolRoot.SetParent(transform, false);
            }

            maxVisibleUnits = Mathf.Clamp(maxVisibleUnits, 36, 140);
            unitVisualScale = Mathf.Clamp(unitVisualScale, 1.35f, 2.35f);
            spacingX = Mathf.Max(0.82f, spacingX);
            spacingZ = Mathf.Max(0.86f, spacingZ);
            baseShotsPerSecond = Mathf.Clamp(baseShotsPerSecond, 4f, 28f);
            shotsPerUnit = Mathf.Clamp(shotsPerUnit, 0.1f, 4.4f);
            maxShotsPerSecond = Mathf.Clamp(maxShotsPerSecond, 24f, 420f);
            tracerLifetime = Mathf.Clamp(tracerLifetime, 0.12f, 0.38f);

            ConfigureCountLabel();
            PrewarmPool();
            EnsureWeaponEffects();
            EnsureLossBurstEffects();
        }

        private void Update()
        {
            var count = _activeUnits.Count;
            if (count == 0)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-formationLerpSpeed * deltaTime);
            var runTime = Time.time;

            for (var i = 0; i < count; i++)
            {
                var unit = _activeUnits[i];
                if (unit == null)
                {
                    continue;
                }

                var phase = runTime * bobFrequency + _phaseOffsets[i];
                var target = _slots[i];
                target.y += Mathf.Sin(phase) * bobAmplitude;
                unit.localPosition = Vector3.Lerp(unit.localPosition, target, blend);
                unit.localRotation = Quaternion.Euler(Mathf.Sin(phase + 0.95f) * tiltDegrees, 0f, 0f);
                unit.localScale = Vector3.one * unitVisualScale;
                if (i < _unitAnimators.Count)
                {
                    _unitAnimators[i]?.SetState(_combatActive, _combatActive);
                }
            }

            UpdateWeaponEffects(deltaTime);
            UpdateCountLabelPose();
        }

        public void SetCount(int count)
        {
            SetCountInternal(count, false);
        }

        public int ApplyBattleLosses(int amount)
        {
            var safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0 || _count <= 0)
            {
                return 0;
            }

            var before = _count;
            _pendingDeathFxBudget = enableDeathFx
                ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(safeAmount) * 2.3f), 1, Mathf.Max(1, maxDeathFxPerLossWave))
                : 0;
            SetCountInternal(_count - safeAmount, true);
            var removed = before - _count;
            _pendingDeathFxBudget = 0;
            EmitLossBurst(removed);
            return removed;
        }

        public void BeginCombat(Transform target)
        {
            _combatTarget = target;
            _combatActive = true;
            _shotAccumulator = 0f;
            _nextShooterIndex = 0;
        }

        public void EndCombat()
        {
            _combatTarget = null;
            _combatActive = false;
            _shotAccumulator = 0f;
        }

        public float EstimateFormationDepth(int count)
        {
            var safeCount = Mathf.Max(1, count);
            var visibleCount = Mathf.Min(safeCount, Mathf.Max(1, maxVisibleUnits));
            var columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(visibleCount)), 1, Mathf.Max(1, maxColumns));
            var rows = Mathf.CeilToInt(visibleCount / (float)columns);
            return Mathf.Max(1.2f, rows * spacingZ + 1.4f);
        }

        private void SetCountInternal(int count, bool allowZero)
        {
            _count = allowZero ? Mathf.Max(0, count) : Mathf.Max(1, count);
            if (countLabel != null)
            {
                countLabel.text = "ENEMY: " + NumberFormatter.ToCompact(_count);
            }

            if (_countLabelShadow != null)
            {
                _countLabelShadow.text = countLabel != null ? countLabel.text : ("ENEMY: " + NumberFormatter.ToCompact(_count));
            }

            var targetVisible = Mathf.Min(_count, maxVisibleUnits);
            while (_activeUnits.Count < targetVisible)
            {
                var unit = GetUnit();
                unit.gameObject.SetActive(true);
                unit.SetParent(unitsRoot, false);
                _activeUnits.Add(unit);
                _unitMuzzles.Add(ResolveUnitMuzzle(unit));
                _unitAnimators.Add(ResolveUnitAnimator(unit, _activeUnits.Count - 1));
                _slots.Add(Vector3.zero);
                _phaseOffsets.Add(CalculatePhaseOffset(_activeUnits.Count - 1));
            }

            while (_activeUnits.Count > targetVisible)
            {
                var last = _activeUnits.Count - 1;
                var unit = _activeUnits[last];
                _activeUnits.RemoveAt(last);
                _unitMuzzles.RemoveAt(last);
                _unitAnimators.RemoveAt(last);
                _slots.RemoveAt(last);
                _phaseOffsets.RemoveAt(last);
                TrySpawnDeathFx(unit);
                ReturnUnit(unit);
            }

            if (_nextShooterIndex >= _activeUnits.Count)
            {
                _nextShooterIndex = 0;
            }

            Relayout();
            UpdateCountLabelPose();
        }

        private Transform GetUnit()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            return CreateUnitInstance();
        }

        private Transform CreateUnitInstance()
        {
            if (enemyUnitPrefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "EnemyUnit";
                var collider = fallback.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                fallback.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
                enemyUnitPrefab = fallback;
                fallback.SetActive(false);
            }

            var instance = Instantiate(enemyUnitPrefab, _poolRoot);
            instance.name = "EnemyUnit";
            UnitVisualFactory.ApplySoldierVisual(instance.transform, true);
            var animator = instance.GetComponent<SoldierMotionAnimator>();
            if (animator == null)
            {
                animator = instance.gameObject.AddComponent<SoldierMotionAnimator>();
            }

            animator.Configure(CalculatePhaseOffset(_activeUnits.Count), true);
            return instance.transform;
        }

        private void ReturnUnit(Transform unit)
        {
            unit.gameObject.SetActive(false);
            unit.SetParent(_poolRoot, false);
            _pool.Push(unit);
        }

        private void Relayout()
        {
            var count = _activeUnits.Count;
            if (count == 0)
            {
                return;
            }

            var columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count)), 1, maxColumns);
            var centeredOffset = (columns - 1) * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var column = i % columns;
                var x = (column - centeredOffset) * spacingX;
                var z = row * spacingZ;
                _slots[i] = new Vector3(x, 0f, z);
            }
        }

        private void EnsureWeaponEffects()
        {
            if (!enableWeaponVfx || _tracerSystem != null)
            {
                return;
            }

            var tracerObject = new GameObject("EnemyTracerFX");
            tracerObject.transform.SetParent(transform, false);
            tracerObject.SetActive(false);
            _tracerSystem = tracerObject.AddComponent<ParticleSystem>();
            var renderer = tracerObject.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 0.66f;
                renderer.velocityScale = 0.12f;
                renderer.material = CreateEffectMaterial("EnemyTracerMaterial", tracerColor, 0.18f, 0.9f);
            }

            ConfigureTracerSystem(_tracerSystem);
            tracerObject.SetActive(true);
        }

        private static void ConfigureTracerSystem(ParticleSystem particleSystem)
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
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 34f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.024f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.36f, 1f));
            main.maxParticles = 280;
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
                    new GradientColorKey(new Color(1f, 0.62f, 0.42f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.22f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void EnsureLossBurstEffects()
        {
            if (_lossBurstSystem != null)
            {
                return;
            }

            var burstObject = new GameObject("EnemyLossBurstFX");
            burstObject.transform.SetParent(transform, false);
            burstObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            burstObject.SetActive(false);
            _lossBurstSystem = burstObject.AddComponent<ParticleSystem>();
            var renderer = burstObject.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = CreateEffectMaterial("EnemyLossBurstMaterial", new Color(1f, 0.5f, 0.3f, 1f), 0.14f, 0.85f);
            }

            ConfigureLossBurstSystem(_lossBurstSystem);
            burstObject.SetActive(true);
        }

        private static void ConfigureLossBurstSystem(ParticleSystem particleSystem)
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
            main.duration = 0.24f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;
        }

        private void EmitLossBurst(int removedUnits)
        {
            if (removedUnits <= 0)
            {
                return;
            }

            EnsureLossBurstEffects();
            if (_lossBurstSystem == null)
            {
                return;
            }

            var burstCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(removedUnits) * 4f), 4, 28);
            var emitParams = new ParticleSystem.EmitParams
            {
                position = transform.position + new Vector3(
                    Random.Range(-0.6f, 0.6f),
                    Random.Range(0.35f, 0.8f),
                    Random.Range(-0.4f, 0.7f)),
                startColor = Color.Lerp(new Color(1f, 0.8f, 0.55f, 1f), new Color(1f, 0.42f, 0.28f, 1f), Random.value)
            };
            _lossBurstSystem.Emit(emitParams, burstCount);
        }

        private void ConfigureCountLabel()
        {
            if (countLabel == null)
            {
                var labelObject = new GameObject("EnemyCountLabel");
                labelObject.transform.SetParent(transform, false);
                countLabel = labelObject.AddComponent<TextMesh>();
            }

            countLabel.fontSize = 92;
            countLabel.characterSize = 0.105f;
            countLabel.anchor = TextAnchor.MiddleCenter;
            countLabel.alignment = TextAlignment.Center;
            countLabel.color = new Color(1f, 0.38f, 0.34f, 1f);
            countLabel.text = "ENEMY: 0";

            var shadowTransform = countLabel.transform.parent != null
                ? countLabel.transform.parent.Find("EnemyCountShadow")
                : null;
            if (shadowTransform == null)
            {
                var shadowObject = new GameObject("EnemyCountShadow");
                shadowObject.transform.SetParent(countLabel.transform.parent != null ? countLabel.transform.parent : transform, false);
                _countLabelShadow = shadowObject.AddComponent<TextMesh>();
            }
            else
            {
                _countLabelShadow = shadowTransform.GetComponent<TextMesh>();
                if (_countLabelShadow == null)
                {
                    _countLabelShadow = shadowTransform.gameObject.AddComponent<TextMesh>();
                }
            }

            if (_countLabelShadow != null)
            {
                _countLabelShadow.fontSize = countLabel.fontSize;
                _countLabelShadow.characterSize = countLabel.characterSize;
                _countLabelShadow.anchor = countLabel.anchor;
                _countLabelShadow.alignment = countLabel.alignment;
                _countLabelShadow.color = new Color(0f, 0f, 0f, 0.68f);
                _countLabelShadow.text = countLabel.text;
            }

            UpdateCountLabelPose();
        }

        private void UpdateCountLabelPose()
        {
            if (countLabel == null)
            {
                return;
            }

            var depth = EstimateFormationDepth(Mathf.Max(1, _count));
            countLabel.transform.localPosition = new Vector3(
                0f,
                3.05f + Mathf.Clamp(depth * 0.16f, 0.22f, 1.5f),
                Mathf.Clamp((depth * 0.82f) + 0.82f, 2.2f, 7.6f));

            countLabel.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.26f, Mathf.Clamp01(_count / 260f));
            var camera = Camera.main;
            if (camera != null)
            {
                var lookDirection = countLabel.transform.position - camera.transform.position;
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    countLabel.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                }
            }

            if (_countLabelShadow != null)
            {
                _countLabelShadow.transform.localPosition = countLabel.transform.localPosition + new Vector3(0.02f, -0.04f, 0.03f);
                _countLabelShadow.transform.localRotation = countLabel.transform.localRotation;
                _countLabelShadow.transform.localScale = countLabel.transform.localScale;
            }
        }

        private void TrySpawnDeathFx(Transform unit)
        {
            if (!enableDeathFx || _pendingDeathFxBudget <= 0 || unit == null)
            {
                return;
            }

            _pendingDeathFxBudget--;
            var directionalImpulse = (-transform.forward * 0.9f) + (Vector3.up * 0.22f);
            UnitDeathFx.Spawn(
                this,
                unit,
                deathFxDuration,
                directionalImpulse,
                deathFxRandomImpulse,
                deathFxGravity,
                0.55f,
                0.06f,
                EmitDeathPoof);
        }

        private void EmitDeathPoof(Vector3 position)
        {
            EnsureLossBurstEffects();
            if (_lossBurstSystem == null)
            {
                return;
            }

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position + new Vector3(
                    Random.Range(-0.05f, 0.05f),
                    Random.Range(0.05f, 0.18f),
                    Random.Range(-0.05f, 0.05f)),
                startColor = Color.Lerp(new Color(1f, 0.8f, 0.58f, 1f), new Color(1f, 0.42f, 0.28f, 1f), Random.value)
            };
            _lossBurstSystem.Emit(emitParams, Random.Range(6, 14));
        }

        private void UpdateWeaponEffects(float deltaTime)
        {
            if (!enableWeaponVfx || !_combatActive || _count <= 0 || deltaTime <= 0f)
            {
                return;
            }

            EnsureWeaponEffects();
            if (_tracerSystem == null)
            {
                return;
            }

            var shotsPerSecond = Mathf.Clamp(
                baseShotsPerSecond + (_activeUnits.Count * Mathf.Max(0.01f, shotsPerUnit)),
                2f,
                Mathf.Max(2f, Mathf.Max(maxShotsPerSecond, _activeUnits.Count * 5.1f)));
            _shotAccumulator += deltaTime * shotsPerSecond;
            var maxShotsPerFrame = Mathf.Clamp(Mathf.RoundToInt(_activeUnits.Count * 0.45f), 12, 120);
            var shotCount = Mathf.Clamp(Mathf.FloorToInt(_shotAccumulator), 0, maxShotsPerFrame);
            var minShotsPerFrame = Mathf.Clamp(Mathf.RoundToInt(_activeUnits.Count * 0.18f), 2, maxShotsPerFrame);
            if (shotCount < minShotsPerFrame)
            {
                shotCount = minShotsPerFrame;
            }

            if (shotCount <= 0)
            {
                return;
            }

            _shotAccumulator = Mathf.Max(0f, _shotAccumulator - shotCount);
            for (var i = 0; i < shotCount; i++)
            {
                if (!TryGetShotPose(out var origin, out var direction))
                {
                    continue;
                }

                direction = (direction + new Vector3(
                    UnityEngine.Random.Range(-tracerSpread, tracerSpread),
                    UnityEngine.Random.Range(-tracerSpread * 0.16f, tracerSpread * 0.16f),
                    0f)).normalized;
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = origin,
                    velocity = direction * Mathf.Max(4f, tracerSpeed * UnityEngine.Random.Range(0.84f, 1.16f)),
                    startLifetime = UnityEngine.Random.Range(tracerLifetime * 0.85f, tracerLifetime * 1.2f),
                    startSize = UnityEngine.Random.Range(0.022f, 0.048f),
                    startColor = Color.Lerp(tracerColor, Color.white, UnityEngine.Random.value * 0.34f)
                };
                _tracerSystem.Emit(emitParams, 1);
            }
        }

        private bool TryGetShotPose(out Vector3 origin, out Vector3 direction)
        {
            if (_activeUnits.Count > 0)
            {
                if (_nextShooterIndex >= _activeUnits.Count)
                {
                    _nextShooterIndex = 0;
                }

                var index = _nextShooterIndex++;
                var unit = _activeUnits[index];
                if (unit != null)
                {
                    var muzzle = index < _unitMuzzles.Count ? _unitMuzzles[index] : null;
                    if (muzzle == null)
                    {
                        muzzle = ResolveUnitMuzzle(unit);
                        if (index < _unitMuzzles.Count)
                        {
                            _unitMuzzles[index] = muzzle;
                        }
                    }

                    origin = muzzle != null
                        ? muzzle.position
                        : unit.TransformPoint(new Vector3(
                            UnityEngine.Random.Range(-0.03f, 0.03f),
                            0.5f + UnityEngine.Random.Range(-0.03f, 0.07f),
                            0.17f + UnityEngine.Random.Range(-0.02f, 0.05f)));
                    origin.z = Mathf.Max(origin.z, unit.position.z + 0.32f);
                    direction = _combatTarget != null
                        ? (_combatTarget.position + Vector3.up * 0.55f - origin).normalized
                        : -transform.forward;
                    if (index < _unitAnimators.Count)
                    {
                        _unitAnimators[index]?.TriggerShot(0.82f);
                    }

                    return true;
                }
            }

            origin = transform.position + Vector3.up * 0.55f;
            origin.z = Mathf.Max(origin.z, transform.position.z + 0.3f);
            direction = _combatTarget != null
                ? (_combatTarget.position + Vector3.up * 0.5f - origin).normalized
                : Vector3.back;
            return true;
        }

        private static Material CreateEffectMaterial(string materialName, Color color, float smoothness, float emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = materialName
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

        private void PrewarmPool()
        {
            var prewarmCount = Mathf.Max(0, initialPoolSize);
            for (var i = 0; i < prewarmCount; i++)
            {
                var unit = CreateUnitInstance();
                ReturnUnit(unit);
            }
        }

        private static float CalculatePhaseOffset(int index)
        {
            return Mathf.Repeat(index * 0.7548777f, 1f) * Mathf.PI * 2f;
        }

        private static Transform ResolveUnitMuzzle(Transform unit)
        {
            if (unit == null)
            {
                return null;
            }

            var muzzle = unit.Find("SoldierModel/RifleBarrel/MuzzlePoint");
            if (muzzle != null)
            {
                return muzzle;
            }

            muzzle = unit.Find("SoldierModel/MuzzlePoint");
            if (muzzle != null)
            {
                return muzzle;
            }

            var model = unit.Find("SoldierModel");
            if (model == null)
            {
                return null;
            }

            var runtimeMuzzle = new GameObject("MuzzlePoint").transform;
            runtimeMuzzle.SetParent(model, false);
            runtimeMuzzle.localPosition = new Vector3(0f, 0.55f, 0.4f);
            runtimeMuzzle.localRotation = Quaternion.identity;
            runtimeMuzzle.localScale = Vector3.one;
            return runtimeMuzzle;
        }

        private static SoldierMotionAnimator ResolveUnitAnimator(Transform unit, int index)
        {
            if (unit == null)
            {
                return null;
            }

            var animator = unit.GetComponent<SoldierMotionAnimator>();
            if (animator == null)
            {
                animator = unit.gameObject.AddComponent<SoldierMotionAnimator>();
            }

            animator.Configure(CalculatePhaseOffset(index), true);
            return animator;
        }
    }
}
