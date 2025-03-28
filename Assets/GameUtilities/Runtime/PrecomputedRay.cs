using Unity.Mathematics;
using MonoRay = UnityEngine.Ray;
using PhysicsRay = Unity.Physics.Ray;

// ReSharper disable InconsistentNaming

namespace GameUtilities.Runtime
{
    public readonly struct PrecomputedRay
    {
        public readonly float3 origin;
        public readonly float3 direction;
        public readonly float3 inverse_direction;

        public PrecomputedRay(MonoRay ray)
        {
            this.origin = ray.origin;
            this.direction = ray.direction;
            this.inverse_direction = 1 / direction;
        }

        public PrecomputedRay(PhysicsRay ray)
        {
            this.origin = ray.Origin;
            this.direction = math.normalize(ray.Displacement);
            this.inverse_direction = 1 / this.direction;
        }

        public PrecomputedRay(in PrecomputedRay source, in float3 new_origin)
        {
            this.origin = new_origin;
            this.direction = source.direction;
            this.inverse_direction = source.inverse_direction;
        }
        
        public static explicit operator PrecomputedRay(in MonoRay source) 
            => new PrecomputedRay(source);
        public static explicit operator MonoRay(in PrecomputedRay source)
            => new MonoRay(source.origin, source.direction);

        public static explicit operator PrecomputedRay(in PhysicsRay source)
            => new PrecomputedRay(source);
        public static explicit operator PhysicsRay(in PrecomputedRay source)
            => new PhysicsRay { Origin = source.origin, Displacement = source.direction};
    }
}