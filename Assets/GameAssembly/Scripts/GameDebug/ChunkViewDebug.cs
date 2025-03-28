using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace GameAssembly.Scripts.GameDebug
{
    [CreateAssetMenu(fileName = "NewChunkViewDebug", menuName = "Debug/ChunkViewDebug", order = 0)]
    public class ChunkViewDebug : ScriptableObject
    {
        [Header("General Settings")]
        public int chunkSize = 500;
        public GameObject chunkViewPrefab;
        [Header("Temperature Settings")]
        public uint seed = 0;
        public float temperatureBaseScale = 0.4f;
        public float3 temperatureOffset = float3.zero;
        public List<TemperatureData> temperatureData;
        public Gradient temperatureGradient;
        [Header("Directional Bias Settings")]
        public float biasOffset = 0.35f;
        public float biasPowerExponent = 1.5f;
        public float biasIntensity = 0.35f;
        [Header("Hotspot Settings")]
        public List<HotspotData> hotspotData;
        [Header("Sharpness Settings")]
        public float threshold = 0.55f;
        public float sharpness = 0.15f;
    }

    [Serializable]
    public struct TemperatureData
    {
        [Range(0, 1024)]
        public float Scale;
        [Range(0, 8)]
        public float Octaves;
        [Range(0,100)]
        public float Weight;
    }

    [Serializable]
    public struct HotspotData
    {
        [Range(-1, 1)]
        public float NormalXPoint;
        [Range(-1, 1)]
        public float NormalZPoint;
        [Range(0, 1)]
        public float Scale;
        [Range(0, 1)]
        public float Weight;
    }
}