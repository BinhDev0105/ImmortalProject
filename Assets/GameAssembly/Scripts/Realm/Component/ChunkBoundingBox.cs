using Unity.Entities;
using Unity.Mathematics;

namespace GameAssembly.Scripts.Realm.Component
{
    public struct ChunkBoundingBox : IComponentData
    {
        public float3 Center;
        public float3 Size;
    }
}