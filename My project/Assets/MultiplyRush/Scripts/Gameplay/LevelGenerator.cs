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
        public float trackHalfWidth = 4.5f;
        public float rowSpacing = 12f;
        public float startZ = 18f;
        public float endPadding = 18f;

        [Header("Difficulty")]
        public int baseStartCount = 20;
        public float baseForwardSpeed = 8f;
        public float maxForwardSpeed = 12.5f;

        private readonly List<Gate> _activeGates = new List<Gate>(128);
        private readonly Stack<Gate> _gatePool = new Stack<Gate>(128);

        private FinishLine _activeFinish;

        public LevelBuildResult Generate(int levelIndex)
        {
            var safeLevel = Mathf.Max(1, levelIndex);
            var generated = BuildDefinition(safeLevel);

            ClearGeneratedObjects();
            EnsureRoots();
            BuildTrackVisual(generated.finishZ);
            SpawnGates(generated.rows);
            SpawnFinish(generated.finishZ, generated.enemyCount);

            return new LevelBuildResult
            {
                levelIndex = safeLevel,
                startCount = generated.startCount,
                enemyCount = generated.enemyCount,
                finishZ = generated.finishZ,
                trackHalfWidth = trackHalfWidth,
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
        }

        private void BuildTrackVisual(float finishZ)
        {
            if (trackVisual == null)
            {
                return;
            }

            var length = finishZ + 24f;
            trackVisual.position = new Vector3(0f, -0.55f, length * 0.5f);
            trackVisual.localScale = new Vector3(trackHalfWidth * 2.5f, 1f, length);
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

        private float LaneToX(int lane)
        {
            switch (lane)
            {
                case 0:
                    return -laneSpacing;
                case 1:
                    return 0f;
                case 2:
                    return laneSpacing;
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
                    z = startZ + rowIndex * rowSpacing,
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

            generated.finishZ = startZ + (rowCount * rowSpacing) + endPadding;
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
