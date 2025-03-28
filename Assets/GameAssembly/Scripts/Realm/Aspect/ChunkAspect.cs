using GameAssembly.Scripts.Realm.Component;
using GameUtilities.Runtime;
using Unity.Entities;
using Unity.Transforms;
// ReSharper disable MemberCanBePrivate.Global

namespace GameAssembly.Scripts.Realm.Aspect
{
    public readonly partial struct ChunkAspect : IAspect
    {
        public readonly Entity Self;
        public readonly RefRO<ChunkTag> ChunkTag;
        public readonly RefRW<LocalTransform> LocalTransform;
        public readonly RefRW<LocalToWorld> LocalToWorld;
        public readonly RefRW<ChunkBoundingBox> ChunkBoundingBox;

        public void SetTransform(LocalTransform transform)
        {
            LocalTransform.ValueRW = transform;
        }

        public void SetWorld(LocalToWorld world)
        {
            LocalToWorld.ValueRW = world;
        }

        public void SetBoundingBox(ChunkBoundingBox boundingBox)
        {
            ChunkBoundingBox.ValueRW = boundingBox;
        }

        public BoundingBox GetBoundingBox()
        {
            var center = ChunkBoundingBox.ValueRW.Center;
            var size = ChunkBoundingBox.ValueRW.Size;
            return new BoundingBox(center, size);
        }
    }
}