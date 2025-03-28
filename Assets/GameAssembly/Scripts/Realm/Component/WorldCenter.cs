using Unity.Entities;
using Unity.Mathematics;

namespace GameAssembly.Scripts.Realm.Component
{
    public struct WorldCenter : IComponentData
    {
        public float3 Value;
    }
}