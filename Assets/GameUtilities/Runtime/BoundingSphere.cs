using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace GameUtilities.Runtime
{
    public struct BoundingSphere
    {
        public float3 center;
        public float radius;

        public BoundingSphere(float3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in BoundingSphere other)
        {
            var distanceSquared = distancesq(center, other.center);
            var sumRadii = radius + other.radius;
            return distanceSquared <= sumRadii * sumRadii;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in BoundingBox other)
        {
            // Find the closest point on the AABB to the sphere center
            float3 closestPoint = other.ClosestPoint(center);
            
            // If the distance from the sphere center to this closest point is less than the radius,
            // then the sphere and AABB overlap
            return distancesq(center, closestPoint) <= radius * radius;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float3 point)
        {
            return distancesq(center, point) <= radius * radius;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in BoundingSphere other)
        {
            // A sphere contains another sphere if the distance between centers plus
            // the radius of the other sphere is less than or equal to this sphere's radius
            float distanceSquared = distancesq(center, other.center);
            return distanceSquared <= (radius - other.radius) * (radius - other.radius) && radius >= other.radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in BoundingBox other)
        {
            // Check all 8 corners of the AABB
            float3 size = other.size / 2;
            float3 c = other.center;
            
            float radiusSq = radius * radius;
            
            // Check all 8 corners
            return distancesq(center, c + new float3(size.x, size.y, size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(size.x, size.y, -size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(size.x, -size.y, size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(size.x, -size.y, -size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(-size.x, size.y, size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(-size.x, size.y, -size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(-size.x, -size.y, size.z)) <= radiusSq &&
                   distancesq(center, c + new float3(-size.x, -size.y, -size.z)) <= radiusSq;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float DistanceSquared(float3 point)
        {
            float distSq = distancesq(center, point);
            return max(0, distSq - radius * radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in Ray ray) => Intersects((PrecomputedRay)ray);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in PrecomputedRay ray) => Intersects(ray.origin, ray.inverse_direction, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in PrecomputedRay ray, out float3 point)
        {
            if (Intersects(ray.origin, ray.inverse_direction, out float t))
            {
                point = ray.origin + ray.direction * t;
                return true;
            }

            point = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in float3 rayOrigin, in float3 rayInvDir, out float t)
        {
            // Vector from ray origin to sphere center
            float3 oc = rayOrigin - center;
            
            float a = dot(rayInvDir, rayInvDir);
            float b = 2.0f * dot(oc, rayInvDir);
            float c = dot(oc, oc) - radius * radius;
            
            float discriminant = b * b - 4 * a * c;
            
            if (discriminant < 0)
            {
                t = 0;
                return false;
            }
            
            float sqrtDiscriminant = sqrt(discriminant);
            
            // Find the nearest intersection point
            float t0 = (-b - sqrtDiscriminant) / (2 * a);
            float t1 = (-b + sqrtDiscriminant) / (2 * a);
            
            if (t0 > 0)
            {
                t = t0;
                return true;
            }
            
            if (t1 > 0)
            {
                t = t1;
                return true;
            }
            
            t = 0;
            return false;
        }
        
        public bool IsValid => radius > 0;

        // Convert from AABB to Sphere (creating the smallest sphere that contains the AABB)
        public static implicit operator BoundingSphere(BoundingBox aabb)
        {
            return new BoundingSphere(aabb.center, length(aabb.size) / 2);
        }
        
        // Convert from Unity's SphereCollider
        public static implicit operator BoundingSphere(SphereCollider collider)
        {
            return new BoundingSphere(collider.transform.TransformPoint(collider.center), collider.radius * collider.transform.lossyScale.x);
        }
    }
}