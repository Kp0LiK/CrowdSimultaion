using Client.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Client
{
    [BurstCompile]
    public struct MoveAgentsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Agent> agents;
        public NativeArray<Agent> agentsNext;

        public float deltaTime;
        public float worldRadius;
        public float directionUpdateInterval;

        public float3 forbiddenCenter;
        public float forbiddenRadius;
        public float forbiddenFalloff;
        public float obstacleAvoidWeight;

        [ReadOnly] public NativeParallelMultiHashMap<int, int> cellMap;
        public float cellSize;
        public float separationRadiusMultiplier;
        public float separationWeight;

        private const int NEIGHBOUR_RANGE = 1;

        public void Execute(int index)
        {
            Agent agent = agents[index];

            var random = new Random(agent.randomSeed);

            agent.directionTimer -= deltaTime;
            if (agent.directionTimer <= 0f)
            {
                float2 dir2d = random.NextFloat2Direction();
                agent.desiredDirection = math.normalizesafe(new float3(dir2d.x, 0f, dir2d.y));
                agent.directionTimer = directionUpdateInterval;
                agent.randomSeed = random.state;
            }

            float3 separation = float3.zero;
            int2 selfCell = JobHelper.WorldToCell(agent.position.xz, cellSize);

            for (int dy = -NEIGHBOUR_RANGE; dy <= NEIGHBOUR_RANGE; dy++)
            {
                for (int dx = -NEIGHBOUR_RANGE; dx <= NEIGHBOUR_RANGE; dx++)
                {
                    int2 neighbourCell = new int2(selfCell.x + dx, selfCell.y + dy);
                    int key = JobHelper.Hash(neighbourCell);

                    var it = cellMap.GetValuesForKey(key);

                    while (it.MoveNext())
                    {
                        int otherIndex = it.Current;
                        if (otherIndex == index) continue;

                        Agent other = agents[otherIndex];

                        float3 delta = agent.position - other.position;
                        float distSq = math.lengthsq(delta);
                        if (distSq == 0f) continue;

                        float minDist = (agent.radius + other.radius) * separationRadiusMultiplier;
                        float minDistSq = minDist * minDist;

                        if (distSq < minDistSq)
                        {
                            float distance = math.sqrt(distSq);
                            float3 dir = delta / distance;
                            float strength = (minDist - distance) / minDist;

                            separation += dir * strength;
                        }
                    }
                }
            }

            float3 obstacleAvoid = float3.zero;

            float3 toCenter = agent.position - forbiddenCenter;
            float distanceToCenter = math.length(toCenter);
            float safeRadius = forbiddenRadius + forbiddenFalloff;

            if (distanceToCenter < safeRadius && distanceToCenter > 0f)
            {
                float3 away = toCenter / distanceToCenter;
                float t = (safeRadius - distanceToCenter) / safeRadius;
                obstacleAvoid = away * t * obstacleAvoidWeight;
            }

            float3 moveDirection = math.normalizesafe(
                agent.desiredDirection
                + separation * separationWeight
                + obstacleAvoid
            );

            agent.velocity = moveDirection * agent.speed;
            agent.position += agent.velocity * deltaTime;

            float dist = math.length(agent.position.xz);
            if (dist > worldRadius)
            {
                float2 directionBack = -math.normalizesafe(agent.position.xz);
                float3 newPosition = new float3(directionBack.x, 0f, directionBack.y) * worldRadius * 0.95f;
                agent.position = newPosition;
                agent.desiredDirection = -agent.desiredDirection;
            }

            agentsNext[index] = agent;
        }
    }
}