using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

namespace GameAssembly.Scripts.Utilities
{
    public struct SpatialHashing
    {
        public static int GetHashKey(int3 position)
            => position.x * 73856093 ^ position.y * 19349663 ^ position.z * 83492791;
    }
}