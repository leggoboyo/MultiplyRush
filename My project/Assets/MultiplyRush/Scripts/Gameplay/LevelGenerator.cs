using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
    public enum BackdropQuality
    {
        Auto = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    [Serializable]
    public struct LevelBuildResult
    {
        public int levelIndex;
        public int startCount;
        public int enemyCount;
        public float finishZ;
        public float trackHalfWidth;
        public float forwardSpeed;
    }

    public sealed class LevelGenerator : MonoBehaviour
    {
        [Header("References")]
        public Gate gatePrefab;
        public FinishLine finishPrefab;
        public Transform levelRoot;
        public Transform gateRoot;
        public Transform trackVisual;

        [Header("Layout")]
        public float laneSpacing = 2.8f;
        public float minLaneSpacing = 3.6f;
        public float laneToEdgePadding = 1.4f;
        public float trackHalfWidth = 4.5f;
        public float rowSpacing = 12f;
        [Range(1f, 2f)]
        public float levelLengthMultiplier = 1.5f;
        public float startZ = 18f;
        public float endPadding = 18f;

        [Header("Difficulty")]
        public int baseStartCount = 20;
        public float baseForwardSpeed = 8f;
        public float maxForwardSpeed = 12.5f;
        public int initialGatePoolSize = 120;

        [Header("Track Decor")]
        public bool enableTrackDecor = true;
        public int stripePoolSize = 100;
        public float stripeLength = 1.8f;
        public float stripeGap = 1.45f;
        public float stripeWidth = 0.16f;
        public float railInset = 0.85f;
        public float railWidth = 0.24f;
        public float railHeight = 0.52f;
        public Color stripeColor = new Color(0.95f, 0.84f, 0.32f, 1f);
        public Color railColor = new Color(0.09f, 0.13f, 0.2f, 1f);

        [Header("Backdrop")]
        public BackdropQuality backdropQuality = BackdropQuality.Auto;
        public bool enableBackdrop = true;
        public int backdropPoolSize = 140;
        public float backdropSpacing = 5.2f;
        public float backdropDistance = 10f;
        public float backdropMinHeight = 2.3f;
        public float backdropMaxHeight = 9.5f;
        public float backdropMinWidth = 1.4f;
        public float backdropMaxWidth = 4.8f;
        public float backdropMinDepth = 2.6f;
        public float backdropMaxDepth = 6.4f;
        public Color backdropColor = new Color(0.23f, 0.3f, 0.4f, 1f);
        public bool enableClouds = true;
        public int cloudPoolSize = 28;
        public float cloudDriftSpeedMin = 0.32f;
        public float cloudDriftSpeedMax = 0.7f;
        public float cloudMinHeight = 8f;
        public float cloudMaxHeight = 15f;
        public Color cloudColor = new Color(0.93f, 0.97f, 1f, 0.9f);

        private readonly List<Gate> _activeGates = new List<Gate>(128);
        private readonly Stack<Gate> _gatePool = new Stack<Gate>(128);
        private readonly List<Transform> _activeStripes = new List<Transform>(128);
        private readonly Stack<Transform> _stripePool = new Stack<Transform>(128);
        private readonly List<Transform> _activeBackdropBlocks = new List<Transform>(256);
        private readonly Stack<Transform> _backdropPool = new Stack<Transform>(256);
        private readonly List<Transform> _activeClouds = new List<Transform>(64);
        private readonly Stack<Transform> _cloudPool = new Stack<Transform>(64);
        private readonly List<float> _cloudSpeeds = new List<float>(64);
        private readonly List<float> _cloudMinX = new List<float>(64);
        private readonly List<float> _cloudMaxX = new List<float>(64);
        private readonly List<float> _cloudBaseY = new List<float>(64);
        private readonly List<float> _cloudPhases = new List<float>(64);

        private FinishLine _activeFinish;
        private float _effectiveLaneSpacing;
        private float _effectiveTrackHalfWidth;
        private int _activeLevelIndex = 1;
        private bool _gatePoolPrewarmed;
        private bool _stripePoolPrewarmed;
        private bool _backdropPoolPrewarmed;
        private bool _cloudPoolPrewarmed;
        private Transform _trackDecorRoot;
        private Transform _backdropRoot;
        private Transform _cloudRoot;
        private Transform _leftRail;
        private Transform _rightRail;
        private Material _stripeMaterial;
        private Material _railMaterial;
        private Material _backdropMaterial;
        private Material _cloudMaterial;

        public LevelBuildResult Generate(int levelIndex)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            _activeLevelIndex = safeLevel;
            _effectiveLaneSpacing = Mathf.Max(laneSpacing, minLaneSpacing);
            _effectiveTrackHalfWidth = Mathf.Max(trackHalfWidth, _effectiveLaneSpacing + laneToEdgePadding);
            EnsureRoots();
            PrewarmGatePool();
            var generated = BuildDefinition(safeLevel);

            ClearGeneratedObjects();
            BuildTrackVisual(generated.finishZ, _effectiveTrackHalfWidth);
            SpawnGates(generated.rows);
            SpawnFinish(generated.finishZ, generated.enemyCount);

            return new LevelBuildResult
            {
                levelIndex = safeLevel,
                startCount = generated.startCount,
                enemyCount = generated.enemyCount,
                finishZ = generated.finishZ,
                trackHalfWidth = _effectiveTrackHalfWidth,
                forwardSpeed = generated.forwardSpeed
            };
        }

        private void Update()
        {
            AnimateClouds(Time.deltaTime);
        }

        private void EnsureRoots()
        {
            if (levelRoot == null)
            {
                var root = new GameObject("LevelRoot").transform;
                root.SetParent(transform, false);
                levelRoot = root;
            }

            if (gateRoot == null)
            {
                var root = new GameObject("GateRoot").transform;
                root.SetParent(levelRoot, false);
                gateRoot = root;
            }

            if (_trackDecorRoot == null)
            {
                _trackDecorRoot = levelRoot.Find("TrackDecor");
                if (_trackDecorRoot == null)
                {
                    _trackDecorRoot = new GameObject("TrackDecor").transform;
                    _trackDecorRoot.SetParent(levelRoot, false);
                }
            }

            if (_backdropRoot == null)
            {
                _backdropRoot = _trackDecorRoot.Find("Backdrop");
                if (_backdropRoot == null)
                {
                    _backdropRoot = new GameObject("Backdrop").transform;
                    _backdropRoot.SetParent(_trackDecorRoot, false);
                }
            }

            if (_cloudRoot == null)
            {
                _cloudRoot = _trackDecorRoot.Find("Clouds");
                if (_cloudRoot == null)
                {
                    _cloudRoot = new GameObject("Clouds").transform;
                    _cloudRoot.SetParent(_trackDecorRoot, false);
                }
            }
        }

        private void BuildTrackVisual(float finishZ, float effectiveTrackHalfWidth)
        {
            if (trackVisual == null)
            {
                return;
            }

            var length = finishZ + 24f;
            trackVisual.position = new Vector3(0f, -0.55f, length * 0.5f);
            trackVisual.localScale = new Vector3(effectiveTrackHalfWidth * 2.5f, 1f, length);
            ApplyTrackColor();
            BuildTrackDecor(length, effectiveTrackHalfWidth);
        }

        private void ApplyTrackColor()
        {
            if (trackVisual == null)
            {
                return;
            }

            var renderer = trackVisual.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            var material = renderer.sharedMaterial;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.18f, 0.22f, 0.29f, 1f));
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.18f, 0.22f, 0.29f, 1f));
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.08f);
            }
        }

        private void BuildTrackDecor(float trackLength, float effectiveTrackHalfWidth)
        {
            if (_trackDecorRoot == null)
            {
                return;
            }

            ClearTrackDecor();
            ClearBackdrop();
            ClearClouds();

            if (!enableTrackDecor)
            {
                SetRailsActive(false);
                return;
            }

            PrewarmStripePool();
            EnsureRails();

            if (_leftRail != null)
            {
                _leftRail.position = new Vector3(-(effectiveTrackHalfWidth + railInset), -0.05f + (railHeight * 0.5f), trackLength * 0.5f);
                _leftRail.localScale = new Vector3(railWidth, railHeight, trackLength + 4f);
            }

            if (_rightRail != null)
            {
                _rightRail.position = new Vector3(effectiveTrackHalfWidth + railInset, -0.05f + (railHeight * 0.5f), trackLength * 0.5f);
                _rightRail.localScale = new Vector3(railWidth, railHeight, trackLength + 4f);
            }

            SetRailsActive(true);

            var start = Mathf.Max(6f, startZ * 0.35f);
            var end = Mathf.Max(start + 2f, trackLength - 12f);
            var step = Mathf.Max(0.45f, stripeLength + stripeGap);
            var stripeY = -0.045f;
            var safeStripeLength = Mathf.Max(0.4f, stripeLength);
            var safeStripeWidth = Mathf.Max(0.04f, stripeWidth);

            for (var z = start; z < end; z += step)
            {
                var stripe = GetStripe();
                stripe.position = new Vector3(0f, stripeY, z);
                stripe.rotation = Quaternion.identity;
                stripe.localScale = new Vector3(safeStripeWidth, 0.012f, safeStripeLength);
                stripe.gameObject.SetActive(true);
                _activeStripes.Add(stripe);
            }

            BuildBackdrop(trackLength, effectiveTrackHalfWidth);
        }

        private void BuildBackdrop(float trackLength, float effectiveTrackHalfWidth)
        {
            if (_backdropRoot == null || _cloudRoot == null)
            {
                return;
            }

            var density = GetBackdropDensityMultiplier();
            var random = new System.Random(12289 + (_activeLevelIndex * 193));
            var zStart = -30f;
            var zEnd = trackLength + 42f;
            var sideX = effectiveTrackHalfWidth + Mathf.Max(6f, backdropDistance);

            if (enableBackdrop)
            {
                PrewarmBackdropPool();
                var spacing = Mathf.Max(2.5f, backdropSpacing / Mathf.Max(0.4f, density));
                var minHeight = Mathf.Max(1f, backdropMinHeight);
                var maxHeight = Mathf.Max(minHeight + 0.5f, backdropMaxHeight);
                var minWidth = Mathf.Max(0.5f, backdropMinWidth);
                var maxWidth = Mathf.Max(minWidth + 0.25f, backdropMaxWidth);
                var minDepth = Mathf.Max(0.8f, backdropMinDepth);
                var maxDepth = Mathf.Max(minDepth + 0.3f, backdropMaxDepth);

                for (var side = -1; side <= 1; side += 2)
                {
                    for (var z = zStart; z <= zEnd; z += spacing)
                    {
                        var block = GetBackdropBlock();
                        var width = Mathf.Lerp(minWidth, maxWidth, (float)random.NextDouble());
                        var height = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble()) * Mathf.Lerp(0.92f, 1.14f, Mathf.Clamp01(density - 0.5f));
                        var depth = Mathf.Lerp(minDepth, maxDepth, (float)random.NextDouble());
                        var zJitter = ((float)random.NextDouble() * 2f - 1f) * spacing * 0.36f;
                        var xJitter = ((float)random.NextDouble() * 2f - 1f) * 1.25f;
                        block.position = new Vector3(side * (sideX + xJitter + width * 0.4f), (height * 0.5f) - 0.2f, z + zJitter);
                        block.rotation = Quaternion.identity;
                        block.localScale = new Vector3(width, height, depth);
                        block.gameObject.SetActive(true);
                        _activeBackdropBlocks.Add(block);
                    }
                }
            }

            if (!enableClouds)
            {
                return;
            }

            PrewarmCloudPool();
            var cloudCount = Mathf.RoundToInt(Mathf.Lerp(8f, 20f, Mathf.Clamp01((density - 0.5f) / 0.75f)));
            cloudCount = Mathf.Clamp(cloudCount, 6, 26);
            var cloudMinX = -(sideX + 18f);
            var cloudMaxX = sideX + 18f;
            var minY = Mathf.Min(cloudMinHeight, cloudMaxHeight);
            var maxY = Mathf.Max(minY + 0.5f, cloudMaxHeight);

            for (var i = 0; i < cloudCount; i++)
            {
                var cloud = GetCloud();
                var x = Mathf.Lerp(cloudMinX, cloudMaxX, (float)random.NextDouble());
                var y = Mathf.Lerp(minY, maxY, (float)random.NextDouble());
                var z = Mathf.Lerp(zStart, zEnd, (float)random.NextDouble());
                var baseScale = Mathf.Lerp(2.5f, 5.8f, (float)random.NextDouble());
                cloud.position = new Vector3(x, y, z);
                cloud.rotation = Quaternion.identity;
                cloud.localScale = new Vector3(baseScale * 1.9f, baseScale * 0.56f, baseScale);
                cloud.gameObject.SetActive(true);
                _activeClouds.Add(cloud);

                var direction = random.NextDouble() < 0.5 ? -1f : 1f;
                var speed = Mathf.Lerp(cloudDriftSpeedMin, cloudDriftSpeedMax, (float)random.NextDouble()) * direction;
                _cloudSpeeds.Add(speed);
                _cloudMinX.Add(cloudMinX);
                _cloudMaxX.Add(cloudMaxX);
                _cloudBaseY.Add(y);
                _cloudPhases.Add((float)random.NextDouble() * Mathf.PI * 2f);
            }
        }

        private void AnimateClouds(float deltaTime)
        {
            if (deltaTime <= 0f || _activeClouds.Count == 0)
            {
                return;
            }

            var waveTime = Time.time * 0.6f;
            for (var i = 0; i < _activeClouds.Count; i++)
            {
                var cloud = _activeClouds[i];
                if (cloud == null)
                {
                    continue;
                }

                var position = cloud.position;
                position.x += _cloudSpeeds[i] * deltaTime;

                var minX = _cloudMinX[i];
                var maxX = _cloudMaxX[i];
                if (position.x > maxX)
                {
                    position.x = minX;
                }
                else if (position.x < minX)
                {
                    position.x = maxX;
                }

                position.y = _cloudBaseY[i] + Mathf.Sin(waveTime + _cloudPhases[i]) * 0.18f;
                cloud.position = position;
            }
        }

        private float GetBackdropDensityMultiplier()
        {
            switch (backdropQuality)
            {
                case BackdropQuality.Low:
                    return 0.55f;
                case BackdropQuality.Medium:
                    return 0.85f;
                case BackdropQuality.High:
                    return 1.2f;
                case BackdropQuality.Auto:
                default:
                {
                    var systemMemory = Mathf.Max(1, SystemInfo.systemMemorySize);
                    var graphicsMemory = SystemInfo.graphicsMemorySize > 0 ? SystemInfo.graphicsMemorySize : (systemMemory / 2);
                    var cores = Mathf.Max(1, SystemInfo.processorCount);

                    if (graphicsMemory < 1400 || systemMemory < 3000 || cores <= 4)
                    {
                        return 0.55f;
                    }

                    if (graphicsMemory < 2600 || systemMemory < 5000 || cores <= 6)
                    {
                        return 0.85f;
                    }

                    return 1.2f;
                }
            }
        }

        private void EnsureRails()
        {
            if (_leftRail == null)
            {
                _leftRail = CreateRail("LeftRail");
            }

            if (_rightRail == null)
            {
                _rightRail = CreateRail("RightRail");
            }
        }

        private void SetRailsActive(bool isActive)
        {
            if (_leftRail != null)
            {
                _leftRail.gameObject.SetActive(isActive);
            }

            if (_rightRail != null)
            {
                _rightRail.gameObject.SetActive(isActive);
            }
        }

        private Transform CreateRail(string name)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(_trackDecorRoot, false);
            var collider = rail.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = rail.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetRailMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return rail.transform;
        }

        private Transform GetStripe()
        {
            if (_stripePool.Count > 0)
            {
                return _stripePool.Pop();
            }

            return CreateStripe();
        }

        private Transform CreateStripe()
        {
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "LaneStripe";
            stripe.transform.SetParent(_trackDecorRoot, false);
            var collider = stripe.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = stripe.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetStripeMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            stripe.SetActive(false);
            return stripe.transform;
        }

        private Transform GetBackdropBlock()
        {
            if (_backdropPool.Count > 0)
            {
                return _backdropPool.Pop();
            }

            return CreateBackdropBlock();
        }

        private Transform CreateBackdropBlock()
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "BackdropBlock";
            block.transform.SetParent(_backdropRoot, false);
            var collider = block.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = block.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetBackdropMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            block.SetActive(false);
            return block.transform;
        }

        private Transform GetCloud()
        {
            if (_cloudPool.Count > 0)
            {
                return _cloudPool.Pop();
            }

            return CreateCloud();
        }

        private Transform CreateCloud()
        {
            var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "BackdropCloud";
            cloud.transform.SetParent(_cloudRoot, false);
            var collider = cloud.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = cloud.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetCloudMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            cloud.SetActive(false);
            return cloud.transform;
        }

        private void ClearTrackDecor()
        {
            for (var i = 0; i < _activeStripes.Count; i++)
            {
                var stripe = _activeStripes[i];
                if (stripe == null)
                {
                    continue;
                }

                stripe.gameObject.SetActive(false);
                stripe.SetParent(_trackDecorRoot, false);
                _stripePool.Push(stripe);
            }

            _activeStripes.Clear();
        }

        private void ClearBackdrop()
        {
            for (var i = 0; i < _activeBackdropBlocks.Count; i++)
            {
                var block = _activeBackdropBlocks[i];
                if (block == null)
                {
                    continue;
                }

                block.gameObject.SetActive(false);
                block.SetParent(_backdropRoot, false);
                _backdropPool.Push(block);
            }

            _activeBackdropBlocks.Clear();
        }

        private void ClearClouds()
        {
            for (var i = 0; i < _activeClouds.Count; i++)
            {
                var cloud = _activeClouds[i];
                if (cloud == null)
                {
                    continue;
                }

                cloud.gameObject.SetActive(false);
                cloud.SetParent(_cloudRoot, false);
                _cloudPool.Push(cloud);
            }

            _activeClouds.Clear();
            _cloudSpeeds.Clear();
            _cloudMinX.Clear();
            _cloudMaxX.Clear();
            _cloudBaseY.Clear();
            _cloudPhases.Clear();
        }

        private void PrewarmStripePool()
        {
            if (_stripePoolPrewarmed)
            {
                return;
            }

            var count = Mathf.Max(0, stripePoolSize);
            for (var i = 0; i < count; i++)
            {
                var stripe = CreateStripe();
                _stripePool.Push(stripe);
            }

            _stripePoolPrewarmed = true;
        }

        private void PrewarmBackdropPool()
        {
            if (_backdropPoolPrewarmed)
            {
                return;
            }

            var count = Mathf.Max(0, backdropPoolSize);
            for (var i = 0; i < count; i++)
            {
                var block = CreateBackdropBlock();
                _backdropPool.Push(block);
            }

            _backdropPoolPrewarmed = true;
        }

        private void PrewarmCloudPool()
        {
            if (_cloudPoolPrewarmed)
            {
                return;
            }

            var count = Mathf.Max(0, cloudPoolSize);
            for (var i = 0; i < count; i++)
            {
                var cloud = CreateCloud();
                _cloudPool.Push(cloud);
            }

            _cloudPoolPrewarmed = true;
        }

        private Material GetStripeMaterial()
        {
            if (_stripeMaterial != null)
            {
                return _stripeMaterial;
            }

            _stripeMaterial = CreateRuntimeMaterial("LaneStripeMaterial", stripeColor, 0.35f);
            return _stripeMaterial;
        }

        private Material GetRailMaterial()
        {
            if (_railMaterial != null)
            {
                return _railMaterial;
            }

            _railMaterial = CreateRuntimeMaterial("SideRailMaterial", railColor, 0.2f);
            return _railMaterial;
        }

        private Material GetBackdropMaterial()
        {
            if (_backdropMaterial != null)
            {
                return _backdropMaterial;
            }

            _backdropMaterial = CreateRuntimeMaterial("BackdropMaterial", backdropColor, 0.05f);
            return _backdropMaterial;
        }

        private Material GetCloudMaterial()
        {
            if (_cloudMaterial != null)
            {
                return _cloudMaterial;
            }

            _cloudMaterial = CreateRuntimeMaterial("CloudMaterial", cloudColor, 0.2f);
            return _cloudMaterial;
        }

        private static Material CreateRuntimeMaterial(string name, Color color, float smoothness)
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

            return material;
        }

        private void SpawnGates(List<GateRow> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                for (var j = 0; j < row.gates.Count; j++)
                {
                    var gateSpec = row.gates[j];
                    var gate = GetGate();
                    gate.transform.position = new Vector3(LaneToX(gateSpec.lane), 0f, row.z);
                    gate.transform.rotation = Quaternion.identity;
                    gate.Configure(gateSpec.operation, gateSpec.value, i);
                    gate.gameObject.SetActive(true);
                    _activeGates.Add(gate);
                }
            }
        }

        private void SpawnFinish(float finishZ, int enemyCount)
        {
            if (finishPrefab == null)
            {
                return;
            }

            if (_activeFinish == null)
            {
                _activeFinish = Instantiate(finishPrefab, levelRoot);
            }

            _activeFinish.transform.position = new Vector3(0f, 0f, finishZ);
            _activeFinish.transform.rotation = Quaternion.identity;
            _activeFinish.gameObject.SetActive(true);
            _activeFinish.Configure(enemyCount);
        }

        private void ClearGeneratedObjects()
        {
            for (var i = 0; i < _activeGates.Count; i++)
            {
                var gate = _activeGates[i];
                if (gate == null)
                {
                    continue;
                }

                gate.gameObject.SetActive(false);
                gate.transform.SetParent(gateRoot != null ? gateRoot : transform, false);
                _gatePool.Push(gate);
            }

            _activeGates.Clear();
            ClearTrackDecor();
            ClearBackdrop();
            ClearClouds();
            SetRailsActive(false);

            if (_activeFinish != null)
            {
                _activeFinish.gameObject.SetActive(false);
            }
        }

        private Gate GetGate()
        {
            EnsureRoots();

            if (_gatePool.Count > 0)
            {
                return _gatePool.Pop();
            }

            if (gatePrefab == null)
            {
                throw new InvalidOperationException("LevelGenerator requires a Gate prefab.");
            }

            return Instantiate(gatePrefab, gateRoot);
        }

        private void PrewarmGatePool()
        {
            if (_gatePoolPrewarmed || gatePrefab == null)
            {
                return;
            }

            var targetCount = Mathf.Max(0, initialGatePoolSize);
            for (var i = 0; i < targetCount; i++)
            {
                var gate = Instantiate(gatePrefab, gateRoot);
                gate.gameObject.SetActive(false);
                _gatePool.Push(gate);
            }

            _gatePoolPrewarmed = true;
        }

        private float LaneToX(int lane)
        {
            switch (lane)
            {
                case 0:
                    return -_effectiveLaneSpacing;
                case 1:
                    return 0f;
                case 2:
                    return _effectiveLaneSpacing;
                default:
                    return 0f;
            }
        }

        private GeneratedLevel BuildDefinition(int levelIndex)
        {
            var random = new System.Random(9143 + levelIndex * 101);
            var generated = new GeneratedLevel
            {
                startCount = Mathf.Clamp(baseStartCount + (levelIndex / 2), baseStartCount, 80),
                forwardSpeed = Mathf.Min(maxForwardSpeed, baseForwardSpeed + levelIndex * 0.06f)
            };

            var rowCount = Mathf.Clamp(8 + levelIndex, 8, 40);
            var effectiveRowSpacing = rowSpacing * Mathf.Max(1f, levelLengthMultiplier);
            var badGateChance = Mathf.Clamp01(0.16f + levelIndex * 0.012f);
            var addBase = 4 + Mathf.FloorToInt(levelIndex * 1.6f);
            var subtractBase = 3 + Mathf.FloorToInt(levelIndex * 1.2f);

            generated.rows = new List<GateRow>(rowCount);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var gateCount = random.NextDouble() < 0.48 ? 3 : 2;
                var laneOrder = BuildShuffledLanes(random);
                var row = new GateRow
                {
                    z = startZ + rowIndex * effectiveRowSpacing,
                    gates = new List<GateSpec>(gateCount)
                };

                for (var gateIndex = 0; gateIndex < gateCount; gateIndex++)
                {
                    var gateOperation = PickOperation(random, badGateChance, levelIndex);
                    var gateValue = PickGateValue(random, gateOperation, addBase, subtractBase, levelIndex);
                    row.gates.Add(new GateSpec
                    {
                        lane = laneOrder[gateIndex],
                        operation = gateOperation,
                        value = gateValue
                    });
                }

                generated.rows.Add(row);
            }

            generated.finishZ = startZ + (rowCount * effectiveRowSpacing) + endPadding;
            generated.enemyCount = BuildEnemyCount(levelIndex, generated.startCount, generated.rows);

            return generated;
        }

        private static int[] BuildShuffledLanes(System.Random random)
        {
            var lanes = new[] { 0, 1, 2 };
            for (var i = lanes.Length - 1; i > 0; i--)
            {
                var swapIndex = random.Next(0, i + 1);
                var temp = lanes[i];
                lanes[i] = lanes[swapIndex];
                lanes[swapIndex] = temp;
            }

            return lanes;
        }

        private static GateOperation PickOperation(System.Random random, float badGateChance, int levelIndex)
        {
            var roll = random.NextDouble();
            if (roll < badGateChance)
            {
                return random.NextDouble() < 0.55 ? GateOperation.Subtract : GateOperation.Divide;
            }

            var multiplyChance = Mathf.Clamp01(0.22f + levelIndex * 0.005f);
            return random.NextDouble() < multiplyChance ? GateOperation.Multiply : GateOperation.Add;
        }

        private static int PickGateValue(System.Random random, GateOperation operation, int addBase, int subtractBase, int levelIndex)
        {
            switch (operation)
            {
                case GateOperation.Add:
                {
                    var variance = Mathf.Max(1, addBase / 2);
                    return Mathf.Max(1, addBase + random.Next(-2, variance + 1));
                }
                case GateOperation.Subtract:
                {
                    var variance = Mathf.Max(1, subtractBase / 2);
                    return Mathf.Max(1, subtractBase + random.Next(0, variance + 2));
                }
                case GateOperation.Multiply:
                {
                    if (levelIndex > 20 && random.NextDouble() < 0.18)
                    {
                        return 3;
                    }

                    return 2;
                }
                case GateOperation.Divide:
                {
                    if (levelIndex > 15 && random.NextDouble() < 0.1)
                    {
                        return 3;
                    }

                    return 2;
                }
                default:
                    return 1;
            }
        }

        private static int BuildEnemyCount(int levelIndex, int startCount, List<GateRow> rows)
        {
            var expectedBest = EstimateBestCaseCount(startCount, rows);
            var baseline = 24 + (levelIndex * 7) + Mathf.FloorToInt(Mathf.Pow(levelIndex, 1.12f));

            var clampedToBest = Mathf.Min(baseline, Mathf.FloorToInt(expectedBest * 0.86f));
            var floor = Mathf.Max(startCount + 5, Mathf.FloorToInt(expectedBest * 0.35f));
            var enemyCount = Mathf.Clamp(clampedToBest, floor, Mathf.Max(floor, expectedBest - 1));

            if (enemyCount >= expectedBest)
            {
                enemyCount = Mathf.Max(1, expectedBest - 1);
            }

            return Mathf.Max(5, enemyCount);
        }

        private static int EstimateBestCaseCount(int startCount, List<GateRow> rows)
        {
            var running = Mathf.Max(1, startCount);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var bestAfterRow = 1;
                for (var j = 0; j < row.gates.Count; j++)
                {
                    var gate = row.gates[j];
                    var after = ApplyOperation(running, gate.operation, gate.value);
                    if (after > bestAfterRow)
                    {
                        bestAfterRow = after;
                    }
                }

                running = Mathf.Max(1, bestAfterRow);
            }

            return running;
        }

        private static int ApplyOperation(int source, GateOperation operation, int value)
        {
            var safeValue = Mathf.Max(1, value);
            switch (operation)
            {
                case GateOperation.Add:
                    return source + safeValue;
                case GateOperation.Subtract:
                    return Mathf.Max(1, source - safeValue);
                case GateOperation.Multiply:
                    return source * safeValue;
                case GateOperation.Divide:
                    return Mathf.Max(1, source / safeValue);
                default:
                    return source;
            }
        }

        [Serializable]
        private struct GateSpec
        {
            public int lane;
            public GateOperation operation;
            public int value;
        }

        [Serializable]
        private struct GateRow
        {
            public float z;
            public List<GateSpec> gates;
        }

        private struct GeneratedLevel
        {
            public int startCount;
            public int enemyCount;
            public float finishZ;
            public float forwardSpeed;
            public List<GateRow> rows;
        }
    }
}
