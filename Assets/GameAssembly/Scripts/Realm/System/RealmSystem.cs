using GameAssembly.Scripts.GameDebug;
using GameAssembly.Scripts.Realm.Aspect;
using GameAssembly.Scripts.Realm.Common;
using GameAssembly.Scripts.Realm.Component;
using GameAssembly.Scripts.Utilities;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Logging.Sinks;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Transforms;
using UnityEngine;
using float3 = Unity.Mathematics.float3;
using NotImplementedException = System.NotImplementedException;

namespace GameAssembly.Scripts.Realm.System
{
    //[UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RealmSystem : ISystem, ISystemStartStop
    {
        private EntityManager _entityManager;
        private EntityCommandBuffer _commandBuffer;
        
        private EntityArchetype _chunkArchetype;
        
        private EntityQuery _chunkQuery;
        
        private EntityTypeHandle _chunkEntityHandle;
        private ComponentTypeHandle<ChunkBoundingBox> _chunkBoundHandle;
        private ComponentTypeHandle<LocalTransform> _chunkLocalTransformHandle;
        
        private int _inRadius;
        private int _previousInRadius;
        private bool _needsRegeneration;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MortalRealmTag>();
            InitializeVariable(ref state);
            InitializeArchetype(ref state);
        }
        
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            UpdateTypeHandle(ref state);
            InitializeSystem(ref state);
            InitializeQuery(ref state);
            
            // Check if inRadius has changed
            _inRadius = (int)SystemAPI.GetSingletonRW<InRadius>().ValueRO.Value;
            if (abs(_previousInRadius - _inRadius) > 0.0001f || _needsRegeneration)
            {
                // If inRadius increased, add new chunks
                if (_inRadius > _previousInRadius)
                {
                    if (_needsRegeneration)
                    {
                        _needsRegeneration = false;
                        //Add New Chunk
                        AddNewChunk(ref state, _previousInRadius, _inRadius);
                    }
                    else
                    {
                        _needsRegeneration = true;
                    }
                }
                else if (_inRadius < _previousInRadius)
                {
                    if (_needsRegeneration)
                    {
                        _needsRegeneration = false;
                        //Remove Chunk
                        RemoveOuterChunk(ref state, _inRadius, _previousInRadius);
                    }
                    else
                    {
                        _needsRegeneration = true;
                    }
                }

                if (!_needsRegeneration)
                {
                    _previousInRadius = _inRadius;
                }
            }
            else if (_chunkQuery.IsEmpty && !_needsRegeneration)
            {
                _needsRegeneration = true;
            }
            else if (_needsRegeneration)
            {
                _needsRegeneration = false;
                GenerateChunks(ref state);
                _previousInRadius = _inRadius;
            }
        }
        
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        private void InitializeVariable(ref SystemState state)
        {
            _inRadius = 0;
            _previousInRadius = 0;
            _needsRegeneration = false;
            _entityManager = state.EntityManager;
            _chunkEntityHandle = state.GetEntityTypeHandle();
            _chunkLocalTransformHandle = state.GetComponentTypeHandle<LocalTransform>();
            _chunkBoundHandle = state.GetComponentTypeHandle<ChunkBoundingBox>();
        }

        private void UpdateTypeHandle(ref SystemState state)
        {
            _chunkEntityHandle.Update(ref state);
            _chunkLocalTransformHandle.Update(ref state);
            _chunkBoundHandle.Update(ref state);
        }

        private void InitializeArchetype(ref SystemState state)
        {
            var componentTypes = new NativeArray<ComponentType>(4, Allocator.Temp);
            componentTypes[0] = ComponentType.ReadWrite<ChunkTag>();
            componentTypes[1] = ComponentType.ReadWrite<LocalTransform>();
            componentTypes[2] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypes[3] = ComponentType.ReadWrite<ChunkBoundingBox>();
            _chunkArchetype = _entityManager.CreateArchetype(componentTypes);
            componentTypes.Dispose();
        }
        
        private void InitializeSystem(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW;
            _commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        }
        
        private void InitializeQuery(ref SystemState state)
        {
            _chunkQuery = SystemAPI.QueryBuilder().WithAll<ChunkTag>().Build();
        }

        private void GenerateChunks(ref SystemState state)
        {
            var chunkEntityCount = (int)pow(_inRadius * 2 + 1, 2);
            
            //Log.Debug($"Initial generation of {chunkEntityCount} chunks with inRadius {_inRadius}");
            
            var chunkSize = SystemAPI.GetSingletonRW<ChunkSize>().ValueRO.Value;
            var worldCenter = SystemAPI.GetSingletonRW<WorldCenter>().ValueRW.Value;
            
            var chunkEntities = new NativeArray<Entity>(chunkEntityCount, Allocator.Temp);
            _entityManager.CreateEntity(_chunkArchetype, chunkEntities);
            
            // Update the type handles after creating entities (structural change)
            UpdateTypeHandle(ref state);
            
            // Apply initial positions
            var chunks = _chunkQuery.ToArchetypeChunkArray(Allocator.TempJob);
            if (chunks.Length > 0)
            {
                var chunkTransformsJob = new CalculateChunkEntityTransform
                {
                    FirstChunkIndexCount = chunks[0].Count,
                    InRadius = _inRadius,
                    ChunkSize = chunkSize,
                    WorldCenter = worldCenter,
                    LocalTransformHandle = _chunkLocalTransformHandle,
                    ChunkBoundHandle = _chunkBoundHandle
                };
                var chunkTransformHandle = chunkTransformsJob.ScheduleParallel(_chunkQuery, state.Dependency);
                state.Dependency = chunkTransformHandle;
                chunks.Dispose();
            }
            else
            {
                Log.Error("Failed to generate initial chunks");
            }
            chunkEntities.Dispose();
        }

        private void AddNewChunk(ref SystemState state, float oldInRadius, float newInRadius)
        {
            var oldLength = oldInRadius == 0 ? 0 : (int)oldInRadius * 2 + 1;
            var newLength = (int)newInRadius * 2 + 1;
            var oldChunkEntityCount = (int)pow(oldLength, 2);
            var newChunkEntityCount = (int)pow(newLength, 2);
            var chunksToAdd =  newChunkEntityCount - oldChunkEntityCount;
            
            if (chunksToAdd <= 0)
            {
                //Log.Debug($"No new chunks to add");
                return;
            }
            
            //Log.Debug($"Adding {chunksToAdd} new chunks as inRadius increased from {oldInRadius} to {newInRadius}");
            
            var chunkSize = SystemAPI.GetSingletonRW<ChunkSize>().ValueRO.Value;
            var worldCenter = SystemAPI.GetSingletonRW<WorldCenter>().ValueRW.Value;
            
            // Create new chunks
            var newChunkEntities = new NativeArray<Entity>(chunksToAdd, Allocator.Temp);
            _entityManager.CreateEntity(_chunkArchetype, newChunkEntities);
            
            // Update handles after structural change
            UpdateTypeHandle(ref state);
            
            var existingChunks = _chunkQuery.ToEntityArray(Allocator.Temp);

            try
            {
                // Apply positions to all chunks (new and existing)
                // We need to update all positions because the indexing scheme changes with the new radius
                var chunks = _chunkQuery.ToArchetypeChunkArray(Allocator.TempJob);
                if (chunks.Length > 0)
                {
                    var positionJob = new CalculateChunkEntityTransform
                    {
                        FirstChunkIndexCount = chunks[0].Count,
                        InRadius = newInRadius,
                        ChunkSize = chunkSize,
                        WorldCenter = worldCenter,
                        LocalTransformHandle = _chunkLocalTransformHandle,
                        ChunkBoundHandle = _chunkBoundHandle
                    };
                    
                    var jobHandle = positionJob.ScheduleParallel(_chunkQuery, state.Dependency);
                    state.Dependency = jobHandle;
                    chunks.Dispose();
                }
            }
            finally
            {
                existingChunks.Dispose();
                newChunkEntities.Dispose();
            }
        }

        private void RemoveOuterChunk(ref SystemState state, float newInRadius, float oldInRadius)
        {
            if (newInRadius < 0 || oldInRadius <= newInRadius)
            {
                //Log.Debug($"No chunks to remove or invalid radius values");
                return;
            }
            
            //Log.Debug($"Removing outer chunks as inRadius decreased from {oldInRadius} to {newInRadius}");
            
            var chunkSize = SystemAPI.GetSingletonRW<ChunkSize>().ValueRO.Value;
            var worldCenter = SystemAPI.GetSingletonRW<WorldCenter>().ValueRW.Value;
            
            // Get all existing chunk entities with their positions
            var existingChunks = _chunkQuery.ToEntityArray(Allocator.Temp);
            var chunksToRemove = new NativeList<Entity>(Allocator.TempJob);

            try
            {
                // Update handles after structural change
                UpdateTypeHandle(ref state);
                
                //Resize capacity
                chunksToRemove.Capacity = existingChunks.Length + chunksToRemove.Length;
                
                // Create a temporary job to identify chunks to remove
                var identifyChunksJob = new IdentifyChunksToRemoveJob
                {
                    ChunksToRemove = chunksToRemove.AsParallelWriter(),
                    NewRadius = newInRadius,
                    ChunkSize = chunkSize,
                    WorldCenter = worldCenter,
                    LocalTransformHandle = _chunkLocalTransformHandle,
                    ChunkEntityHandle = _chunkEntityHandle
                };
                
                var jobHandle = identifyChunksJob.ScheduleParallel(_chunkQuery, state.Dependency);
                jobHandle.Complete();
                
                if (chunksToRemove.Length > 0)
                {
                    //Log.Debug($"Removing {chunksToRemove.Length} chunks that are outside the new radius");
                    _entityManager.DestroyEntity(chunksToRemove.AsArray());
                    
                    // Update handles after structural change
                    UpdateTypeHandle(ref state);
                    
                    // Reposition remaining chunks
                    var chunks = _chunkQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    if (chunks.Length > 0)
                    {
                        var positionJob = new CalculateChunkEntityTransform
                        {
                            FirstChunkIndexCount = chunks[0].Count,
                            InRadius = newInRadius,
                            ChunkSize = chunkSize,
                            WorldCenter = worldCenter,
                            LocalTransformHandle = _chunkLocalTransformHandle,
                            ChunkBoundHandle = _chunkBoundHandle
                        };
                        
                        var positionJobHandle = positionJob.ScheduleParallel(_chunkQuery, state.Dependency);
                        state.Dependency = positionJobHandle;
                        chunks.Dispose();
                    }
                }
                else
                {
                    Log.Debug($"No chunks identified for removal");
                }
            }
            finally
            {
                existingChunks.Dispose();
                chunksToRemove.Dispose();
            }
        }
        
        [BurstCompile]
        private struct IdentifyChunksToRemoveJob : IJobChunk
        {
            public NativeList<Entity>.ParallelWriter ChunksToRemove;
            [ReadOnly]
            public int FirstChunkIndexCount;
            [ReadOnly]
            public float NewRadius;
            [ReadOnly]
            public float3 ChunkSize;
            [ReadOnly]
            public float3 WorldCenter;
            [ReadOnly]
            public EntityTypeHandle ChunkEntityHandle;
            [NativeDisableParallelForRestriction]
            public ComponentTypeHandle<LocalTransform> LocalTransformHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(ChunkEntityHandle);
                var transforms = chunk.GetNativeArray(ref LocalTransformHandle);
                
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var position = transforms[i].Position;
                    var entity = entities[i];
                    // Calculate grid position relative to center
                    var offsetX = math.round((position.x - WorldCenter.x + 0.5f - (ChunkSize.x / 2f)) / ChunkSize.x);
                    var offsetZ = math.round((position.z - WorldCenter.z + 0.5f - (ChunkSize.z / 2f)) / ChunkSize.z);
                    
                    // Check if this chunk is outside the new radius
                    if (abs(offsetX) > NewRadius || abs(offsetZ) > NewRadius)
                    {
                        //Log.Debug($"Removing chunk {i} at {offsetX}:{offsetZ}");
                        ChunksToRemove.AddNoResize(entity);
                    }
                }
            }
        }
        
        [BurstCompile]
        private struct CalculateChunkEntityTransform : IJobChunk
        {
            [ReadOnly]
            public int FirstChunkIndexCount;
            [ReadOnly]
            public float InRadius;
            [ReadOnly] 
            public float3 ChunkSize;
            [ReadOnly] 
            public float3 WorldCenter;
            [NativeDisableParallelForRestriction]
            public ComponentTypeHandle<LocalTransform> LocalTransformHandle;
            [NativeDisableParallelForRestriction]
            public ComponentTypeHandle<ChunkBoundingBox> ChunkBoundHandle;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                //var entities = chunk.GetNativeArray(ChunkEntityHandle);
                var transforms = chunk.GetNativeArray(ref LocalTransformHandle);
                var bounds = chunk.GetNativeArray(ref ChunkBoundHandle);
                
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var index = unfilteredChunkIndex * FirstChunkIndexCount + i;
                    var length = (int)InRadius * 2 + 1;
                    // Calculate 2D grid position from linear index
                    var gridZ = index / length;
                    var gridX = index % length;
                    
                    // Adjust to be centered around WorldCenter
                    var offsetX = gridX - InRadius;
                    var offsetZ = gridZ - InRadius;

                    var centerOffset = new float3(ChunkSize.x/2f - 0.5f, 0, ChunkSize.z/2f - 0.5f);
                    var nearestPosition = WorldHelper.GetNearestChunkPosition(ChunkSize,WorldCenter,centerOffset);
                    // // Calculate world position
                    var position = new float3(
                        nearestPosition.x + offsetX * ChunkSize.x,
                        nearestPosition.y,
                        nearestPosition.z + offsetZ * ChunkSize.z
                    );
                    
                    transforms[i] = LocalTransform.FromPosition(position);
                    //Log.Debug($"Chunk at index {index} positioned at ({position.x}, {position.y}, {position.z})");
                    bounds[i] = new ChunkBoundingBox
                    {
                        Center = position,
                        Size = ChunkSize,
                    };
                }
            }
        }
    }
}