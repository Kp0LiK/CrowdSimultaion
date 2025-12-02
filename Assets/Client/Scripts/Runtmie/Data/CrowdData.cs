using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "CrowdData", menuName = "Data/Crowd/Create Crowd Data", order = 0)]
    public class CrowdData : ScriptableObject
    {
        [Header("Agent Settings")] public int AgentCount = 1500;
        public float MinSpeed = 1.5f;
        public float MaxSpeed = 3.5f;
        public float MinRadius = 0.3f;
        public float MaxRadius = 0.6f;

        [Header("World Settings")] public float WorldRadius = 25f;
        [Range(0f, 1f)] public float SpawnRadiusFactor = 0.8f;
        public float DirectionUpdateInterval = 2f;

        [Header("Render Settings")] public GameObject AgentPrefab;
        public Mesh AgentMesh;
        public Material AgentMaterial;

        [Header("Separation Settings")] public float SeparationRadiusMultiplier = 1.5f;
        public float SeparationWeight = 1.0f;

        [Header("Grid Settings")] public float CellSize = 2.0f;

        [Header("Forbidden Zone")] public Vector3 ForbiddenCenter = Vector3.zero;
        public float ForbiddenRadius = 5f;
        public float ForbiddenFalloff = 2f;
        public float ObstacleAvoidWeight = 2f;
    }
}