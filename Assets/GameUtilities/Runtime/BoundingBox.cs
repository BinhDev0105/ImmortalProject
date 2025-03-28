using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace GameUtilities.Runtime
{
    public struct BoundingBox
    {
        public float3 min => center - extents;
        public float3 max => center + extents;

        public float3 center;

        public float3 size;
        
        public float3 extents => size / 2f;

        public BoundingBox(float3 center, float3 size)
        {
            this.center = center;
            this.size = size;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in BoundingBox other) =>
            all(max >= other.min) && 
            all(other.max >= min);
        
        /// <summary>
        /// Kiểm tra xem AABB có giao với hình cầu không
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in BoundingSphere sphere)
        {
            // Tìm điểm gần nhất trên AABB với tâm hình cầu
            float3 closestPoint = ClosestPoint(sphere.center);
            
            // Tính bình phương khoảng cách từ tâm hình cầu đến điểm gần nhất
            float distanceSquared = distancesq(sphere.center, closestPoint);
            
            // Hình cầu giao với AABB nếu bình phương khoảng cách nhỏ hơn hoặc bằng bình phương bán kính
            return distanceSquared <= sphere.radius * sphere.radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float3 point) => all(point >= min) && all(point <= max);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in BoundingBox other)
        {
            return all(min <= other.min) && 
                   all(max >= other.max);
        }
        
        /// <summary>
        /// Kiểm tra xem AABB có chứa hình cầu hoàn toàn không
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in BoundingSphere sphere)
        {
            // AABB chứa hình cầu nếu khoảng cách từ tâm hình cầu đến mỗi mặt của AABB
            // lớn hơn hoặc bằng bán kính của hình cầu
            
            // Tính khoảng cách từ tâm hình cầu đến gần nhất của các mặt AABB
            float3 distToFace = min(
                abs(sphere.center - min), // Khoảng cách đến các mặt âm
                abs(sphere.center - max)  // Khoảng cách đến các mặt dương
            );
            
            // AABB chứa hình cầu nếu khoảng cách nhỏ nhất đến mặt lớn hơn bán kính
            return cmin(distToFace) >= sphere.radius;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 ClosestPoint(float3 point) => clamp(point, min, max);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float DistanceSquared(float3 point) => distancesq(point, ClosestPoint(point));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in Ray ray) => Intersects((PrecomputedRay) ray);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in PrecomputedRay ray) => Intersects(ray.origin, ray.inverse_direction, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in PrecomputedRay ray, out float3 point)
        {
            if (Intersects(ray.origin, ray.inverse_direction, out float tMin))
            {
                point = ray.origin + ray.direction * tMin;
                return true;
            }

            point = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in PrecomputedRay ray, out float tMin) => Intersects(ray.origin, ray.inverse_direction, out tMin);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in float3 rayPos, in float3 rayInvDir, out float tMin) 
        {
            float3 t1 = (min - rayPos) * rayInvDir;
            float3 t2 = (max - rayPos) * rayInvDir;

            float3 tMin1 = min(t1, t2);
            float3 tMax1 = max(t1, t2);

            tMin = max(0, cmax(tMin1));
            float tMax = cmin(tMax1);
            
            return tMax >= tMin;
        }
        
        public bool IsValid => all(max >= min);

        public static explicit operator Bounds(BoundingBox boundingBox) => new Bounds(boundingBox.center, boundingBox.size);
        public static implicit operator BoundingBox(Bounds bounds) => new BoundingBox(bounds.center, bounds.size);
    }
}