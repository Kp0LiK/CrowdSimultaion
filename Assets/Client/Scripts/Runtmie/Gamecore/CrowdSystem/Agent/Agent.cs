using Unity.Mathematics;

namespace Client
{
    public struct Agent
    {
        public float3 position;
        public float3 velocity;
        public float3 desiredDirection;
        public float speed;
        public float radius;
        public float directionTimer;
        public uint randomSeed;
    }
}