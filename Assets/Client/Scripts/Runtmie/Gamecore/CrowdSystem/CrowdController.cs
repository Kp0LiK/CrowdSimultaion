using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Client
{
    public class CrowdController : MonoBehaviour
    {
        [SerializeField] private CrowdData _crowdData;

        private NativeArray<Agent> _agents;
        private NativeArray<Agent> _agentsNext;
        private NativeArray<Matrix4x4> _matrices;
        private Matrix4x4[] _matrixBuffer;

        private NativeParallelMultiHashMap<int, int> _cellMap;

        private float _directionUpdateInterval;
        private bool _initialized;

        private JobHandle _lastFrameHandle;

        private const int BATCH_SIZE = 1023;
        private const int BATCH_COUNT = 64;
        private const uint RANDOM_SEED = 123456u;

        private void Awake()
        {
            _agents = new NativeArray<Agent>(_crowdData.AgentCount, Allocator.Persistent);
            _agentsNext = new NativeArray<Agent>(_crowdData.AgentCount, Allocator.Persistent);
            _matrices = new NativeArray<Matrix4x4>(_crowdData.AgentCount, Allocator.Persistent);
            _matrixBuffer = new Matrix4x4[_crowdData.AgentCount];

            _cellMap = new NativeParallelMultiHashMap<int, int>(_crowdData.AgentCount, Allocator.Persistent);
        }

        private void Start()
        {
            if (_crowdData.AgentMesh == null)
            {
                var meshFilter = _crowdData.AgentPrefab.GetComponent<MeshFilter>();
                _crowdData.AgentMesh = meshFilter.sharedMesh;
            }

            if (_crowdData.AgentMaterial == null)
            {
                var renderComponent = _crowdData.AgentPrefab.GetComponent<Renderer>();
                _crowdData.AgentMaterial = renderComponent.sharedMaterial;
            }

            _directionUpdateInterval = _crowdData.DirectionUpdateInterval;

            _crowdData.CellSize =
                math.max((_crowdData.MinRadius + _crowdData.MaxRadius) * _crowdData.SeparationRadiusMultiplier, 1.0f);

            InitAgents();

            _initialized = true;
            _lastFrameHandle = default;
        }

        private void InitAgents()
        {
            var randomRange = new Random(RANDOM_SEED);

            for (int i = 0; i < _crowdData.AgentCount; i++)
            {
                float angle = randomRange.NextFloat(0f, math.PI * 2f);
                float radius = randomRange.NextFloat(0f, _crowdData.WorldRadius * _crowdData.SpawnRadiusFactor);

                float3 position = new float3(
                    math.cos(angle) * radius,
                    0f,
                    math.sin(angle) * radius
                );

                float speed = randomRange.NextFloat(_crowdData.MinSpeed, _crowdData.MaxSpeed);
                float agentRadius = randomRange.NextFloat(_crowdData.MinRadius, _crowdData.MaxRadius);

                float2 dir2d = randomRange.NextFloat2Direction();
                float3 direction = math.normalizesafe(new float3(dir2d.x, 0f, dir2d.y));

                _agents[i] = new Agent
                {
                    position = position,
                    velocity = direction * speed,
                    desiredDirection = direction,
                    speed = speed,
                    radius = agentRadius,
                    directionTimer = randomRange.NextFloat(0f, _directionUpdateInterval),
                    randomSeed = randomRange.NextUInt()
                };
            }
        }

        private void Update()
        {
            if (!_initialized)
                return;

            _lastFrameHandle.Complete();

            _cellMap.Clear();

            JobHandle moveHandle = SimulateAgents();
            moveHandle.Complete();

            (_agents, _agentsNext) = (_agentsNext, _agents);

            JobHandle matrixHandle = UpdateMatrices();
            matrixHandle.Complete();

            _lastFrameHandle = matrixHandle;

            for (int i = 0; i < _crowdData.AgentCount; i++)
            {
                _matrixBuffer[i] = _matrices[i];
            }

            DrawAgents();
        }

        private JobHandle BuildGrid()
        {
            var buildGridJob = new BuildGridJob
            {
                agents = _agents,
                cellMap = _cellMap.AsParallelWriter(),
                cellSize = _crowdData.CellSize
            };
            return buildGridJob.Schedule(_crowdData.AgentCount, BATCH_COUNT);
        }

        private JobHandle SimulateAgents()
        {
            float dt = Time.deltaTime;

            var moveJob = new MoveAgentsJob
            {
                agents = _agents,
                agentsNext = _agentsNext,
                deltaTime = dt,
                worldRadius = _crowdData.WorldRadius,
                directionUpdateInterval = _directionUpdateInterval,

                forbiddenCenter = _crowdData.ForbiddenCenter,
                forbiddenRadius = _crowdData.ForbiddenRadius,
                forbiddenFalloff = _crowdData.ForbiddenFalloff,
                obstacleAvoidWeight = _crowdData.ObstacleAvoidWeight,

                cellMap = _cellMap,
                cellSize = _crowdData.CellSize,
                separationRadiusMultiplier = _crowdData.SeparationRadiusMultiplier,
                separationWeight = _crowdData.SeparationWeight
            };

            return moveJob.Schedule(_crowdData.AgentCount, BATCH_COUNT, BuildGrid());
        }

        private JobHandle UpdateMatrices()
        {
            var matrixJob = new BuildMatricesJob
            {
                agents = _agents,
                matrices = _matrices
            };

            return matrixJob.Schedule(_crowdData.AgentCount, BATCH_COUNT);
        }


        private void DrawAgents()
        {
            int count = Mathf.Min(_crowdData.AgentCount, BATCH_SIZE);
            Graphics.DrawMeshInstanced(_crowdData.AgentMesh, 0, _crowdData.AgentMaterial, _matrixBuffer, count);
        }

        private void OnDestroy()
        {
            _lastFrameHandle.Complete();

            if (_initialized)
            {
                if (_agents.IsCreated)
                {
                    _agents.Dispose();
                }

                if (_agentsNext.IsCreated)
                {
                    _agentsNext.Dispose();
                }

                if (_matrices.IsCreated)
                {
                    _matrices.Dispose();
                }

                if (_cellMap.IsCreated)
                {
                    _cellMap.Dispose();
                }
            }
        }
    }
}