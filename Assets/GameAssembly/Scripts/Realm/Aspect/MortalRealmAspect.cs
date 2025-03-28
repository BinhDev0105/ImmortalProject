using GameAssembly.Scripts.Realm.Component;
using Unity.Entities;
using Unity.Mathematics;

namespace GameAssembly.Scripts.Realm.Aspect
{
    public readonly partial struct MortalRealmAspect : IAspect
    {
        public readonly Entity Self;
        public readonly RefRO<MortalRealmTag> MortalRealmTag;
        private readonly RefRW<WorldCenter> _worldCenter;
        private readonly RefRW<ChunkSize> _chunkSize;
        private readonly RefRW<InRadius> _inRadius;

        public void SetInRadius(int radius)
        {
            _inRadius.ValueRW.Value = radius;
        }
        public float GetInRadius() => _inRadius.ValueRO.Value;

        public void SetWorldCenter(float3 center)
        {
            _worldCenter.ValueRW.Value = center;
        }
        public float3 GetWorldCenter() => _worldCenter.ValueRO.Value;

        public void SetChunkSize(float3 size)
        {
            _chunkSize.ValueRW.Value = size;
        }
        public float3 GetChunkSize() => _chunkSize.ValueRO.Value;
        
    }
}