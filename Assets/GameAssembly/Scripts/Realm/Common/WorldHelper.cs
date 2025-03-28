using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GameAssembly.Scripts.Realm.Common
{
    public struct WorldHelper
    {
        public static int3 GetPositionFromBlockIndex(int index, int chunkSize, int chunkHeight)
            => new int3(
                index % chunkSize,
                (index / chunkSize) % chunkHeight,
                index / (chunkSize * chunkHeight)
            );

        public static int3 GetChunkPositionFromCoordinates(float3 coordinates, float3 chunkSize)
            => new int3(
                (int)(floor(coordinates.x / (float)chunkSize.x) * chunkSize.x),
                (int)(floor(coordinates.y / (float)chunkSize.y) * chunkSize.y),
                (int)(floor(coordinates.z / (float)chunkSize.z) * chunkSize.z)
                );

        public static float3 GetNearestChunkPosition(float3 chunkSize, float3 coordinates, float3 offset = default)
        {
            return new float3(
                round(coordinates.x / chunkSize.x) * chunkSize.x + offset.x,
                round(coordinates.y / chunkSize.y) * chunkSize.y + offset.y,
                round(coordinates.z / chunkSize.z) * chunkSize.z + offset.z
                );
        }
        
        
    }
}