using Unity.Entities;
using Unity.Mathematics;

namespace GameAssembly.Scripts.Realm.Component
{
    public struct ChunkSize : IComponentData
    {
        public float3 Value;
    }
}