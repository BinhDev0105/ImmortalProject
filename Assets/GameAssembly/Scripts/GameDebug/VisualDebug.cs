using System;
using System.Collections.Generic;
using System.Linq;
using GameAssembly.Scripts.Block.Utilities;
using GameAssembly.Scripts.GameDebug.System;
using GameAssembly.Scripts.Realm.Common;
using GameAssembly.Scripts.Realm.Component;
using GameAssembly.Scripts.Realm.System;
using GameAssembly.Scripts.SceneManager;
using GameUtilities.Runtime;
using GameUtilities.Runtime.Collection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace GameAssembly.Scripts.GameDebug
{
    public class VisualDebug : MonoBehaviour
    {

    }
    // //[ExecuteInEditMode]
    //
    // [Serializable]
    // public struct BlockDebug
    // {
    //     public BlockType Type;
    //     public float3 Position;
    // }
    //
    // public class VisualDebug : MonoBehaviour
    // {
    //     // [SerializeField] private bool enableDebug;
    //     // [Header("Visual debug color")]
    //     // [SerializeField] private Color lineColor;
    //     // [SerializeField] private Color chunkCenterColor;
    //     // [SerializeField] private Color chunkColor;
    //     // [SerializeField] private Color sectionColor;
    //     // [SerializeField] private Color blockColor;
    //     //
    //     // [Header("Block insertion debug")]
    //     // [SerializeField] private List<BlockDebug> blocks;
    //     // [SerializeField] private uint seed = 1;
    //     // [SerializeField] private int size = 1024;
    //     // [SerializeField] private float tempeScale = 1f;
    //     // [SerializeField] private float3 tempeOffset = new float3(0, 0, 0);
    //     // [SerializeField] private float moiScale = 1f;
    //     // [SerializeField] private float3 moiOffset = new float3(0, 0, 0);
    //     [SerializeField] private Renderer textureRenderer;
    //     // [SerializeField] private Gradient gradient;
    //     // [SerializeField, Range(0,1)] private float value;
    //     // [SerializeField] private Color color;
    //     //
    //     // [Header("Path finding debug")]
    //     // [SerializeField] private Transform startTransform;
    //     // [SerializeField] private Transform endTransform;
    //     // [SerializeField] private List<string> targetChunks;
    //     
    //     public ChunkViewDebug chunkViewDebug;
    //     
    //     private static ComponentType[] ChunkComponents()
    //     {
    //         return new[]
    //         {
    //             ComponentType.ReadWrite<ChunkTag>(),
    //             ComponentType.ReadWrite<LocalTransform>(),
    //             ComponentType.ReadWrite<LocalToWorld>(),
    //             ComponentType.ReadWrite<ChunkBoundingBox>()
    //         };
    //     }
    //     
    //     private EntityManager _entityManager;
    //
    //     void OnDrawGizmos()
    //     {
    //         // if (!enableDebug) return;
    //         // _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    //         // targetChunks.Clear();
    //         // RayDrawing();
    //         // ChunkDrawing();
    //         // SectionDrawing();
    //         // color = gradient.Evaluate(value);
    //         
    //         //Debug.Log("Color "+gradient.Evaluate(value));
    //         //Log.Debug($"Size {sizeof(BlockType)}");
    //         // var targetPositions = new NativeArray<float3>(1, Allocator.TempJob);
    //         // var entityPositions = new NativeArray<float3>(1, Allocator.TempJob);
    //         // entityPositions[0] = entityPosition;
    //         // targetPositions[0] = targetPosition;
    //         // Gizmos.color = Color.red;
    //         // Gizmos.DrawLine(entityPosition, targetPosition);
    //         // var paths = octreeSystem.FindPath(ref state, entityPositions, targetPositions, Allocator.TempJob);
    //         // var values = paths.GetValueArray(Allocator.Temp);
    //         // foreach (var value in values)
    //         // {
    //         //     targetChunks.Add(octreeSystem.GetChunk(value).ToString());
    //         //     Gizmos.color = Color.blue - new Color(0, 0, 0, 0.75f);
    //         //     Gizmos.DrawCube(value, chunkSize.Value);
    //         // }
    //         // values.Dispose();
    //         // paths.Dispose();
    //         // entityPositions.Dispose();
    //         // targetPositions.Dispose();
    //     }
    //
    //     private void RayDrawing()
    //     {
    //         //Gizmos.color = lineColor;
    //         //Gizmos.DrawLine(startTransform.position, endTransform.position);
    //     }
    //
    //     private void ChunkDrawing()
    //     {
    //         // Gizmos.color = chunkColor;
    //         // var chunkQuery = _entityManager.CreateEntityQuery(ChunkComponents());
    //         // var boundingBoxes = chunkQuery.ToComponentDataArray<ChunkBoundingBox>(Allocator.Temp);
    //         // foreach (var box in boundingBoxes)
    //         // {
    //         //     Gizmos.DrawWireCube(box.Center, box.Size);
    //         // }
    //         //
    //         // Gizmos.color = chunkCenterColor;
    //         // var centerIndex = (int)(boundingBoxes.Length/2f);
    //         // var centerChunk = boundingBoxes[centerIndex];
    //         // Gizmos.DrawCube(centerChunk.Center, centerChunk.Size);
    //         // boundingBoxes.Dispose();
    //     }
    //
    //     private void SectionDrawing()
    //     {
    //         // Gizmos.color = sectionColor;
    //         // var state = _entityManager.WorldUnmanaged.GetExistingSystemState<SpatialHashingManagerSystem>();
    //         // var systemHandle = state.World.GetOrCreateSystem<SpatialHashingManagerSystem>();
    //         // ref var spatialHashingManagerSystem = ref state.WorldUnmanaged.GetUnsafeSystemRef<SpatialHashingManagerSystem>(systemHandle);
    //         // var sectionArray = spatialHashingManagerSystem.Sections;
    //         // foreach (var section in sectionArray)
    //         // {
    //         //     Gizmos.DrawWireCube(section.Center, section.Size);
    //         // }
    //         // sectionArray.Dispose();
    //         //
    //         // foreach (var block in blocks)
    //         // {
    //         //     spatialHashingManagerSystem.Insert(block.Position, block.Type);
    //         // }
    //         // Gizmos.color = blockColor;
    //         // var blockArray = spatialHashingManagerSystem.Blocks;
    //         // foreach (var block in blockArray)
    //         // {
    //         //     Gizmos.DrawCube(block.Position, new float3(1));
    //         // }
    //
    //     }
    //
    //     private void GenerateNoise()
    //     {
    //         _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    //         var state = _entityManager.WorldUnmanaged.GetExistingSystemState<NoiseVisualDebugSystem>();
    //         var systemHandle = state.World.GetOrCreateSystem<NoiseVisualDebugSystem>();
    //         ref var noiseSystem = ref state.WorldUnmanaged.GetUnsafeSystemRef<NoiseVisualDebugSystem>(systemHandle);
    //         noiseSystem.Initialize(chunkViewDebug.seed, chunkViewDebug.chunkSize, chunkViewDebug.temperatureBaseScale,
    //             chunkViewDebug.temperatureOffset, 1, 1);
    //         var texture = new Texture2D(chunkViewDebug.chunkSize, chunkViewDebug.chunkSize);
    //         var colors = noiseSystem.GetBiomeColors().ToArray();
    //         texture.filterMode = FilterMode.Point;
    //         texture.SetPixels(colors);
    //         texture.Apply();
    //         textureRenderer.sharedMaterial.mainTexture = texture;
    //     }
    //     
    //     public void Generate()
    //     {
    //         var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    //         var state = entityManager.WorldUnmanaged.GetExistingSystemState<NoiseVisualDebugSystem>();
    //         var systemHandle = entityManager.World.GetOrCreateSystem<NoiseVisualDebugSystem>();
    //         ref var system = ref entityManager.WorldUnmanaged.GetUnsafeSystemRef<NoiseVisualDebugSystem>(systemHandle);
    //         Debug.Log($"Generating");
    //         GenerateNoise();
    //     }
    // }
    
}