using GameAssembly.Scripts.Realm.Component;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace GameAssembly.Scripts.Realm.Authoring
{
    public class MortalRealmAuthoring : MonoBehaviour
    {
        public int inRadius;
        public float3 worldCenter;
        public int3 chunkSize;
        private class MortalRealmAuthoringBaker : Baker<MortalRealmAuthoring>
        {
            public override void Bake(MortalRealmAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<MortalRealmTag>(entity);
                AddComponent(entity, new InRadius{ Value = authoring.inRadius });
                AddComponent(entity, new WorldCenter{ Value = authoring.worldCenter });
                AddComponent(entity, new ChunkSize{ Value = authoring.chunkSize });
            }
        }
    }
}