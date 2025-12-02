using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Client
{
    [BurstCompile]
    public struct BuildMatricesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Agent> agents;
        public NativeArray<Matrix4x4> matrices;

        public void Execute(int index)
        {
            Agent agent = agents[index];

            Vector3 velocity = new Vector3(agent.velocity.x, 0f, agent.velocity.z);

            Quaternion rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            Vector3 position = new Vector3(agent.position.x, agent.radius, agent.position.z);
            Vector3 scale = new Vector3(agent.radius, agent.radius, agent.radius);

            matrices[index] = Matrix4x4.TRS(position, rotation, scale);
        }
    }
}