using Unity.Burst;
using Unity.Entities;

namespace GameAssembly.Scripts.Realm.System
{
    public partial struct BiomeManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RealmSystem>();
            state.RequireForUpdate<SpatialHashingChunkManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}