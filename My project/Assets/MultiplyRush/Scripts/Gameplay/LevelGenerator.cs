using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplyRush
{
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

        private readonly List<Gate> _activeGates = new List<Gate>(128);
        private readonly Stack<Gate> _gatePool = new Stack<Gate>(128);
        private readonly List<Transform> _activeStripes = new List<Transform>(128);
        private readonly Stack<Transform> _stripePool = new Stack<Transform>(128);

        private FinishLine _activeFinish;
        private float _effectiveLaneSpacing;
        private float _effectiveTrackHalfWidth;
        private bool _gatePoolPrewarmed;
        private bool _stripePoolPrewarmed;
        private Transform _trackDecorRoot;
        private Transform _leftRail;
        private Transform _rightRail;
        private Material _stripeMaterial;
        private Material _railMaterial;

        public LevelBuildResult Generate(int levelIndex)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
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
            if (!enableTrackDecor || _trackDecorRoot == null)
            {
                return;
            }

            PrewarmStripePool();
            ClearTrackDecor();
            EnsureRails();

            if (_leftRail != null)
            {
                _leftRail.gameObject.SetActive(true);
                _leftRail.position = new Vector3(-(effectiveTrackHalfWidth + railInset), -0.05f + (railHeight * 0.5f), trackLength * 0.5f);
                _leftRail.localScale = new Vector3(railWidth, railHeight, trackLength + 4f);
            }

            if (_rightRail != null)
            {
                _rightRail.gameObject.SetActive(true);
                _rightRail.position = new Vector3(effectiveTrackHalfWidth + railInset, -0.05f + (railHeight * 0.5f), trackLength * 0.5f);
                _rightRail.localScale = new Vector3(railWidth, railHeight, trackLength + 4f);
            }

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
                    gate.Configure(gateSpec.operation, gateSpec.value);
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
