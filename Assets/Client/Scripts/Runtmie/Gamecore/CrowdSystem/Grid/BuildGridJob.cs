using Client.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Client
{
    [BurstCompile]
    public struct BuildGridJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Agent> agents;
        public NativeParallelMultiHashMap<int, int>.ParallelWriter cellMap;
        public float cellSize;

        public void Execute(int index)
        {
            Agent agent = agents[index];

            int2 cell = JobHelper.WorldToCell(agent.position.xz, cellSize);
            int key = JobHelper.Hash(cell);

            cellMap.Add(key, index);
        }
    }
}