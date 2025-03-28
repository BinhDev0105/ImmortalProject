using GameUtilities.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace GameAssembly.Scripts.GameDebug.System
{
    public struct VisualPrefab : IComponentData
    {
        
    }
    
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    public partial struct NoiseVisualDebugSystem : ISystem
    {
        
        private NativeArray<float> _falloffFloats;
        private NativeArray<Color> _biomeColors;
        private NativeArray<Color> _moistureNoiseColors;
        
        private uint _seed;
        private int _size;
        private float _temperatureScale;
        private float3 _temperatureOffset;
        
        private float _moistureScale;
        private float3 _moistureOffset;
        
        private NativeArray<BurstGradient.ColorKey> _gradientColorKeys;
        private NativeArray<BurstGradient.AlphaKey> _gradientAlphaKeys;
        private BurstGradient _gradient;

        public void Initialize(uint seed, int size, float temperatureScale, float3 temperatureOffset, float moistureScale, float3 moistureOffset)
        {
            _seed = seed;
            _size = size;
            _temperatureScale = temperatureScale;
            _temperatureOffset = temperatureOffset;
            _moistureScale = moistureScale;
            _moistureOffset = moistureOffset;
        }
        
        public NativeArray<float> GetFalloffColors() => _falloffFloats;
        
        public NativeArray<Color> GetBiomeColors() => _biomeColors;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _seed = 1;
            _size = 1024;
            _temperatureScale = 100;
            _temperatureOffset = new float3(0, 0, 0);
            _moistureScale = 100;
            _moistureOffset = new float3(0, 0, 0);
            _falloffFloats = new NativeArray<float>(_size*_size, Allocator.Persistent);
            _biomeColors = new NativeArray<Color>(_size*_size, Allocator.Persistent);
            _moistureNoiseColors = new NativeArray<Color>(_size*_size, Allocator.Persistent);

            _gradientColorKeys = new NativeArray<BurstGradient.ColorKey>(5, Allocator.Persistent);
            _gradientColorKeys[0] = new BurstGradient.ColorKey(new Color(0f,0.5f,1f,1f), 0f);
            _gradientColorKeys[1] = new BurstGradient.ColorKey(new Color(0f,1f,1f,1f), 0.25f);
            _gradientColorKeys[2] = new BurstGradient.ColorKey(new Color(0.5f,1f,0f,1f), 0.5f);
            _gradientColorKeys[3] = new BurstGradient.ColorKey(new Color(1f,1f,0f,1f), 0.75f);
            _gradientColorKeys[4] = new BurstGradient.ColorKey(new Color(1f,0.25f,0f,1f), 1f);
            
            _gradientAlphaKeys = new NativeArray<BurstGradient.AlphaKey>(2, Allocator.Persistent);
            _gradientAlphaKeys[0] = new BurstGradient.AlphaKey(1f, 0f);
            _gradientAlphaKeys[1] = new BurstGradient.AlphaKey(1f, 1f);
            _gradient = new BurstGradient(_gradientColorKeys, _gradientAlphaKeys);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var falloffJob = new FalloffGeneratorJob
            {
                Size = _size,
                //Gradient = _gradient,
                FalloffFloats = _falloffFloats,
            };
            var falloffHandle = falloffJob.Schedule(_size * _size, 128);
            state.Dependency = falloffHandle;
            falloffHandle.Complete();
            var biomeColorsJob = new BiomeColorsJob
            {
                Seed = _seed,
                Size = _size,
                TemperatureScale = _temperatureScale,
                TemperatureOffset = _temperatureOffset,
                MoistureScale = _moistureScale,
                MoistureOffset = _moistureOffset,
                Gradient = _gradient,
                FalloffFloats = _falloffFloats,
                BiomeColors = _biomeColors,
            };
            var biomeColorsHandle = biomeColorsJob.Schedule(_size * _size, 128);
            state.Dependency = biomeColorsHandle;
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_falloffFloats.IsCreated) _falloffFloats.Dispose();
            if (_biomeColors.IsCreated) _biomeColors.Dispose();
            if (_moistureNoiseColors.IsCreated) _moistureNoiseColors.Dispose();
            _gradient.Dispose();
        }
        
        [BurstCompile]
        private struct BiomeColorsJob: IJobParallelFor
        {
            [ReadOnly]
            public uint Seed;
            [ReadOnly]
            public int Size;
            [ReadOnly]
            public float TemperatureScale;
            [ReadOnly]
            public float3 TemperatureOffset;
            [ReadOnly]
            public float MoistureScale;
            [ReadOnly]
            public float3 MoistureOffset;
            [ReadOnly]
            public BurstGradient Gradient;
            [ReadOnly]
            public NativeArray<float> FalloffFloats;
            [NativeDisableParallelForRestriction]
            public NativeArray<Color> BiomeColors;
            
            public void Execute(int index)
            {
                // Lấy vị trí
                var z = index % Size;
                var x = index / Size;
                var pos = new float3(x, 0, z);
                // Lấy tâm
                var radius = Size * 0.5f;
                var center = new float3(radius, 0, radius);
                // Tính khoảng cách từ tâm
                var distance = length(pos - center);
                // Chuẩn hóa khảng cách thành [0,1]
                var normalDistance = saturate(distance/ radius);
                // Tính nhiệt độ cơ bản (giảm từ tâm ra ngoài)
                var baseTempe = 1f - normalDistance;
                // Nhiễu tần số thấp
                var tempeNoise1 = SimplexNoise.FractalNoise(pos / TemperatureScale + TemperatureOffset, Seed);
                // Nhiễu tần số trung bình
                var tempeNoise2 = SimplexNoise.FractalNoise(pos / TemperatureScale*3 + TemperatureOffset, Seed+123);
                // Nhiễu tần số cao
                var tempeNoise3 = SimplexNoise.FractalNoise(pos / TemperatureScale*8 + TemperatureOffset, Seed+345);
                // Kết hợp nhiễu với trọng số
                var combineNoise = 0.6f * tempeNoise1 + 0.3f * tempeNoise2 + 0.1f * tempeNoise3;
                // Điều chỉnh ảnh hưởng của nhiệt độ theo trục oz
                // neutral: điều chỉnh vùng ảnh hưởng nhiệt theo trục oz
                const float neutral = 0.35f;
                // exponent: điều chỉnh cường độ của trung tâm
                const float exponent = 1.5f;
                // intensity: điều chỉnh tổng cường độ ảnh hưởng nhiệt (rìa vùng nhiệt ảnh hưởng)
                const float intensity = 0.35f;
                var directionalBias = saturate((center.z - pos.z)/(Size/2f)+ neutral);
                directionalBias = pow(directionalBias, exponent) * intensity;
                var inverseDirectionalBias = saturate((pos.z - center.z)/(Size/2f)+ neutral);
                inverseDirectionalBias = pow(inverseDirectionalBias, exponent) * intensity;
                var hotspot = center + new float3(radius * 0.3f, 0, radius * -0.2f);
                var hotspotInfluence = exp(-length(pos - hotspot) / (radius * 0.15f));
                const float threshold = 0.55f;
                var sharpNoise = tempeNoise2 >  threshold ? (tempeNoise2 - threshold) / (1 - threshold) * 0.15f : 0;
                var temperature = baseTempe;
                temperature = lerp(temperature, temperature * combineNoise, 0.4f);
                temperature += directionalBias;
                temperature += inverseDirectionalBias;
                //hotspot scale
                const float hotspotScale = 0.225f;
                temperature += hotspotInfluence * hotspotScale;
                temperature += sharpNoise;
                temperature = saturate(temperature);
                if (temperature > 0.8f)
                    BiomeColors[index] = Gradient.Evaluate(0.8f);
            }
        }
        
        [BurstCompile]
        private struct FalloffGeneratorJob : IJobParallelFor
        {
            [ReadOnly]
            public int Size;
            //[ReadOnly]
            //public BurstGradient Gradient;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> FalloffFloats;
            public void Execute(int index)
            {
                var i = index % Size;
                var j = index / Size;
                var x = i / (float)Size * 2f - 1;
                var z = j/ (float)Size * 2f - 1;
                var value = max(abs(x), abs(z));
                FalloffFloats[i * Size + j] = Evaluate(value);
            }

            private static float Evaluate(float value)
            {
                const float a = 3;
                const float b = 2.2f;
                return pow(value,a) / (pow(value,a) + pow(b - (b * value),a));
            }
        }
    }
}