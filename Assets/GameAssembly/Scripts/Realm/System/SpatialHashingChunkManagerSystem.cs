using GameAssembly.Scripts.Block.Utilities;
using GameAssembly.Scripts.Realm.Common;
using GameAssembly.Scripts.Realm.Component;
using GameAssembly.Scripts.Utilities;
using GameUtilities.Runtime;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace GameAssembly.Scripts.Realm.System
{
    /// <summary>
    /// Đại diện cho một section trong spatial hash grid
    /// </summary>
    public struct SpatialSection
    {
        /// <summary> Tâm của section </summary>
        public float3 Center;
        /// <summary> Kích thước section theo các trục </summary>
        public float3 Size;
    }

    public struct BlockData
    {
        public BlockType Type;
        public float3 Position;
    }
    
    /// <summary>
    /// Hệ thống quản lý spatial hashing để tối ưu truy vấn không gian
    /// </summary>
    public partial struct SpatialHashingChunkManagerSystem : ISystem
    {
        // Các thành phần dữ liệu và truy vấn
        private EntityQuery _chunkQuery;
        private EntityManager _entityManager;
        
        // Handles cho các component
        private EntityTypeHandle _chunkEntityHandle;
        private ComponentTypeHandle<ChunkBoundingBox> _chunkBoundHandle;
        
        // Cấu trúc dữ liệu lưu trữ spatial hash
        private NativeParallelHashMap<int, Entity> _spatialHashingChunks;
        private NativeParallelHashMap<int, SpatialSection> _spatialHashingSections;
        private NativeParallelHashMap<int, int> _sectionToChunkMap;
        private NativeParallelHashMap<int, int> _blockToSectionMap;
        private NativeParallelHashMap<int, BlockData> _spatialHashingBlockTypes;
        
        // Các thành phần dữ liệu chủa chunk
        private float3 _chunkSize;

        /// <summary>
        /// Khởi tạo hệ thống - tạo các HashMap và chuẩn bị handles
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { 
            state.RequireForUpdate<ChunkTag>();
            _entityManager = state.EntityManager;
            AllocateHashMap(ref state, Allocator.Persistent);
            GetTypeHandle(ref state);
        }

        /// <summary>
        /// Cập nhật hệ thống mỗi frame - làm mới dữ liệu spatial hash
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ClearHashMap(ref state);
            UpdateTypeHandle(ref state);
            AutoAdd(ref state);
        }

        /// <summary>
        /// Hủy hệ thống - giải phóng bộ nhớ
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            DeallocateHashMap(ref state);
        }

        /// <summary>
        /// Cấp phát bộ nhớ cho các HashMap
        /// </summary>
        private void AllocateHashMap(ref SystemState state, Allocator allocator)
        {
            // Khởi tạo các NativeHashMap với allocator được chỉ định
            _spatialHashingChunks = new NativeParallelHashMap<int, Entity>(0, allocator);
            _spatialHashingSections = new NativeParallelHashMap<int, SpatialSection>(0, allocator);
            _sectionToChunkMap = new NativeParallelHashMap<int, int>(0, allocator);
            _blockToSectionMap = new NativeParallelHashMap<int, int>(0, allocator);
            _spatialHashingBlockTypes = new NativeParallelHashMap<int, BlockData>(0, allocator);
        }

        /// <summary>
        /// Giải phóng bộ nhớ của các HashMap
        /// </summary>
        private void DeallocateHashMap(ref SystemState state)
        {
            if (_spatialHashingChunks.IsCreated) _spatialHashingChunks.Dispose();
            if (_spatialHashingSections.IsCreated) _spatialHashingSections.Dispose();
            if (_sectionToChunkMap.IsCreated) _sectionToChunkMap.Dispose();
            if (_blockToSectionMap.IsCreated) _blockToSectionMap.Dispose();
            if (_spatialHashingBlockTypes.IsCreated) _spatialHashingBlockTypes.Dispose();
        }

        /// <summary>
        /// Điều chỉnh kích thước HashMap dựa trên số lượng chunk và section
        /// </summary>
        private void ResizeHashMap(ref SystemState state, int capacity, int sectionCount)
        {
            _spatialHashingChunks.Capacity = capacity;
            _spatialHashingSections.Capacity = sectionCount * capacity;
            _sectionToChunkMap.Capacity = sectionCount * capacity;
        }

        /// <summary>
        /// Xóa toàn bộ dữ liệu trong các HashMap
        /// </summary>
        private void ClearHashMap(ref SystemState state)
        {
            _spatialHashingChunks.Clear();
            _spatialHashingSections.Clear();
            _sectionToChunkMap.Clear();
            _blockToSectionMap.Clear();
            _spatialHashingBlockTypes.Clear();
        }

        /// <summary>
        /// Lấy các component handles cần thiết
        /// </summary>
        private void GetTypeHandle(ref SystemState state)
        {
            _chunkEntityHandle = state.GetEntityTypeHandle();
            _chunkBoundHandle = state.GetComponentTypeHandle<ChunkBoundingBox>();
        }

        /// <summary>
        /// Cập nhật handles trước khi sử dụng
        /// </summary>
        private void UpdateTypeHandle(ref SystemState state)
        {
            _chunkEntityHandle.Update(ref state);
            _chunkBoundHandle.Update(ref state);
        }
        
        /// <summary>
        /// Lấy chunk entity tại vị trí chỉ định
        /// </summary>
        /// <param name="position">Vị trí cần kiểm tra</param>
        /// <returns>Entity của chunk hoặc default nếu không tìm thấy</returns>
        public Entity GetChunk(float3 position)
        {
            var key = SpatialHashing.GetHashKey((int3)position);
            return _spatialHashingChunks.TryGetValue(key, out var entity) ? entity : Entity.Null;
        }

        /// <summary>
        /// Lấy thông tin spatial section tại vị trí chỉ định
        /// </summary>
        /// <param name="position">Vị trí cần kiểm tra</param>
        /// <returns>SpatialSection hoặc default nếu không tìm thấy</returns>
        public SpatialSection GetSection(float3 position)
        {
            var key = SpatialHashing.GetHashKey((int3)position);
            return _spatialHashingSections.TryGetValue(key, out var section) ? section: default;
        }
        
        /// <summary>
        /// Lấy tất cả spatial sections hiện có
        /// </summary>
        /// <returns>NativeArray chứa tất cả sections</returns>
        public NativeArray<SpatialSection> Sections
            => _spatialHashingSections.GetValueArray(Allocator.Temp);
        
        public NativeArray<BlockData> Blocks 
            => _spatialHashingBlockTypes.GetValueArray(Allocator.Temp);

        /// <summary>
        /// Tự động thêm chunk và các sections vào spatial hash
        /// </summary>
        private void AutoAdd(ref SystemState state)
        {
            // Query các chunk có đủ components cần thiết
            _chunkQuery = SystemAPI.QueryBuilder().WithAll<ChunkTag, LocalTransform, LocalToWorld, ChunkBoundingBox>().Build();
            if (_chunkQuery.IsEmpty) return;
            
            state.Dependency.Complete();
            
            // Tính toán số lượng chunk và section
            var chunkCount = _chunkQuery.CalculateEntityCount();
            var boundingBox = _chunkQuery.ToComponentDataArray<ChunkBoundingBox>(Allocator.Temp);
            _chunkSize = boundingBox[0].Size;
            var sectionCount = (int)(_chunkSize.y / _chunkSize.x);

            // Điều chỉnh kích thước HashMap nếu cần
            if (chunkCount < _spatialHashingChunks.Capacity)
            {
                DeallocateHashMap(ref state);
                AllocateHashMap(ref state, Allocator.Persistent);
            }
            
            ResizeHashMap(ref state, chunkCount, sectionCount);
            
            // Schedule job để thêm sections
            var addSectionJob = new AddSectionJob
            {
                ChunkEntityHandle = _chunkEntityHandle,
                ChunkBoundHandle = _chunkBoundHandle,
                SpatialHashingChunks = _spatialHashingChunks.AsParallelWriter(),
                SpatialHashingSections = _spatialHashingSections.AsParallelWriter(),
                SectionToChunkMap = _sectionToChunkMap.AsParallelWriter(),
            };
            var addSectionHandle = addSectionJob.ScheduleParallel(_chunkQuery, state.Dependency);
            state.Dependency = addSectionHandle;
            state.Dependency.Complete();
            boundingBox.Dispose();
        }
        
        /// <summary>
        /// Job để thêm các sections từ chunk vào spatial hash
        /// </summary>
        [BurstCompile]
        private struct AddSectionJob : IJobChunk
        {
            [NativeDisableParallelForRestriction] public EntityTypeHandle ChunkEntityHandle;
            [NativeDisableParallelForRestriction] public ComponentTypeHandle<ChunkBoundingBox> ChunkBoundHandle;
            [WriteOnly] public NativeParallelHashMap<int, Entity>.ParallelWriter SpatialHashingChunks;
            [WriteOnly] public NativeParallelHashMap<int, SpatialSection>.ParallelWriter SpatialHashingSections;
            [WriteOnly] public NativeParallelHashMap<int, int>.ParallelWriter SectionToChunkMap;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Lặp qua tất cả entities trong chunk
                var entities = chunk.GetNativeArray(ChunkEntityHandle);
                var chunkBounds = chunk.GetNativeArray(ref ChunkBoundHandle);
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var entity = entities[i];
                    var bound = chunkBounds[i];
                    
                    // Thêm chunk vào spatial hash
                    var chunkKey = SpatialHashing.GetHashKey((int3)bound.Center);
                    SpatialHashingChunks.TryAdd(chunkKey, entity);
                    
                    // Chia chunk thành các sections và thêm vào hash
                    var sectionCount = bound.Size.y / bound.Size.x;
                    for (var j = 0; j < sectionCount; j++)
                    {
                        var position = bound.Center + new float3(0, (j - (int)(sectionCount/2f)) * bound.Size.x, 0);
                        var sectionKey = SpatialHashing.GetHashKey((int3)position);
                        var spatialSection = new SpatialSection { Center = position, Size = new float3(bound.Size.x) };
                        SpatialHashingSections.TryAdd(sectionKey, spatialSection);
                        SectionToChunkMap.TryAdd(sectionKey, chunkKey);
                    }
                }
            }
        }

        public void Insert(float3 position, BlockType blockType)
        {
            var centerOffset = new float3(_chunkSize.x/2f - 0.5f, 0, _chunkSize.z/2f - 0.5f);
            var nearestChunk = WorldHelper.GetNearestChunkPosition(_chunkSize,position, centerOffset);
            var chunkKey = SpatialHashing.GetHashKey((int3)nearestChunk);
            var isContainsChunkKey = _spatialHashingChunks.ContainsKey(chunkKey);
            if (!isContainsChunkKey) return;

            var sectionSize = new float3(_chunkSize.x);
            var nearestSection = WorldHelper.GetNearestChunkPosition(sectionSize,position, centerOffset);
            var sectionKey = SpatialHashing.GetHashKey((int3)nearestSection);
            var isContainsSectionKey = _spatialHashingSections.ContainsKey(sectionKey);
            if (!isContainsSectionKey) return;
            
            var dataKey = SpatialHashing.GetHashKey((int3)position);
            var isContainsDataKey = _spatialHashingBlockTypes.ContainsKey(dataKey);
            if (isContainsDataKey)
            {
                noise.snoise(new float3());
                Log.Debug($"{dataKey} is already in spatial hash section");
                return;
            }

            var blockData = new BlockData
            {
                Type = blockType,
                Position = position,
            };
            _spatialHashingBlockTypes.TryAdd(dataKey, blockData);
            _spatialHashingBlockTypes.TryGetValue(dataKey,out var type);
            _blockToSectionMap.TryAdd(dataKey, sectionKey);
            Log.Debug($"Insert {_spatialHashingBlockTypes[dataKey].Type} at {position}");
        }

        public BlockData GetBlockData(float3 position)
        {
            var defaultData = new BlockData
            {
                Type = BlockType.Nothing,
                Position = position,
            };
            var centerOffset = new float3(_chunkSize.x/2f - 0.5f, 0, _chunkSize.z/2f - 0.5f);
            var nearestChunk = WorldHelper.GetNearestChunkPosition(_chunkSize,position, centerOffset);
            var chunkKey = SpatialHashing.GetHashKey((int3)nearestChunk);
            var isContainsChunkKey = _spatialHashingChunks.ContainsKey(chunkKey);
            if (!isContainsChunkKey) return defaultData;

            var sectionSize = new float3(_chunkSize.x);
            var nearestSection = WorldHelper.GetNearestChunkPosition(sectionSize,position, centerOffset);
            var sectionKey = SpatialHashing.GetHashKey((int3)nearestSection);
            var isContainsSectionKey = _spatialHashingSections.ContainsKey(sectionKey);
            if (!isContainsSectionKey) return defaultData;
            
            var dataKey = SpatialHashing.GetHashKey((int3)position);
            return _spatialHashingBlockTypes.TryGetValue(dataKey, out var blockData) ? blockData : defaultData;
        }

        /// <summary>
        /// Tìm đường đi giữa các điểm bằng cách kiểm tra giao nhau với các chunk
        /// </summary>
        /// <returns>Map chứa index tia và các vị trí chunk mà tia đi qua</returns>
        public NativeParallelMultiHashMap<int,float3> FindPath(ref SystemState state, NativeArray<float3> starts, NativeArray<float3> ends, Allocator allocator)
        {
            // Chuẩn bị dữ liệu tia
            var rays = new NativeList<PrecomputedRay>(starts.Length, allocator);
            var lengths = new NativeList<float>(starts.Length, allocator);
            var rayJob = new CalculateRayJob
            {
                Starts = starts,
                Ends = ends,
                Rays = rays.AsParallelWriter(),
                Lengths = lengths.AsParallelWriter(),
            };
            var rayHandle = rayJob.Schedule(starts.Length, 64, state.Dependency);
            state.Dependency = rayHandle;
            rayHandle.Complete();
            
            // Kiểm tra giao nhau với các chunk
            var chunkPaths = new NativeParallelMultiHashMap<int, float3>(0, allocator);
            var boundTypeHandle = state.GetComponentTypeHandle<ChunkBoundingBox>();
            var chunkPathJob = new IntersectChunkJob
            {
                Rays = rays.AsArray(),
                Lengths = lengths.AsArray(),
                ChunkBoundHandle = boundTypeHandle,
                ChunkPaths = chunkPaths,
            };
            
            // Chạy job trên tất cả chunks
            var chunkQuery = SystemAPI.QueryBuilder().WithAll<ChunkTag, LocalTransform, LocalToWorld, ChunkBoundingBox>().Build();
            var chunkPathHandle = chunkPathJob.ScheduleParallel(chunkQuery, state.Dependency);
            state.Dependency = chunkPathHandle;
            state.Dependency.Complete();
            
            // Dọn dẹp
            lengths.Dispose();
            rays.Dispose();
            
            return chunkPaths;
        }
        
        /// <summary>
        /// Job tính toán các tia từ điểm bắt đầu đến kết thúc
        /// </summary>
        [BurstCompile]
        private struct CalculateRayJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> Starts;
            [ReadOnly] public NativeArray<float3> Ends;
            [WriteOnly] public NativeList<PrecomputedRay>.ParallelWriter Rays;
            [WriteOnly] public NativeList<float>.ParallelWriter Lengths;
            
            public void Execute(int index)
            {
                // Tính toán vector hướng và độ dài tia
                var entityPosition = Starts[index];
                var targetPosition = Ends[index];
                var ray = (PrecomputedRay)(new Ray
                {
                    Origin = entityPosition,
                    Displacement = targetPosition - entityPosition
                });
                Rays.AddNoResize(ray);
                Lengths.AddNoResize(length(targetPosition - entityPosition));
            }
        }
        
        /// <summary>
        /// Job kiểm tra giao nhau giữa tia và chunk
        /// </summary>
        [BurstCompile]
        private struct IntersectChunkJob : IJobChunk
        {
            [ReadOnly] public NativeArray<PrecomputedRay> Rays;
            [ReadOnly] public NativeArray<float> Lengths;
            [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, float3> ChunkPaths;
            [NativeDisableParallelForRestriction] public ComponentTypeHandle<ChunkBoundingBox> ChunkBoundHandle;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Kiểm tra từng chunk
                var bounds = chunk.GetNativeArray(ref ChunkBoundHandle);
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var bound = bounds[i];
                    var box = new BoundingBox { center = bound.Center, size = bound.Size };
                    
                    // Kiểm tra giao nhau với từng tia
                    for (var j = 0; j < Rays.Length; j++)
                    {
                        var ray = Rays[j];
                        if (box.Intersects(ray, out float length) && length <= Lengths[j])
                        {
                            ChunkPaths.Add(j, bound.Center);
                        }
                    }
                }
            }
        }
    }
}