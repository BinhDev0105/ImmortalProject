using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;
using static Unity.Mathematics.Random;
// ReSharper disable InconsistentNaming
// ReSharper disable MethodOverloadWithOptionalParameter
// ReSharper disable MemberCanBePrivate.Global

namespace GameUtilities.Runtime
{
    public struct SimplexNoise
    {
        private uint m_seed;
        private float m_frequency;
        private float m_amplitude;
        
        public static float Noise(float3 value, uint seed = 1, float frequency = 1.0f, float amplitude= 1.0f)
            => snoise((value + CreateFromIndex(seed).NextFloat3(-100000, 100000)) * frequency) * amplitude;

        //Tự nhiên
        public static float FractalNoise(float3 value, uint seed = 1, float frequency = 1.0f, float amplitude = 1.0f
            , int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f)
        {
            var _total = 0f;
            var _frequency = frequency;
            var _amplitude = amplitude;
            var _max = 0f;

            for (var i = 0; i < octaves; i++)
            {
                _total += Noise(value, seed, _frequency, _amplitude);
                _max += _amplitude;
                _amplitude *= persistence;
                _frequency *= lacunarity;
            }
            return _total / _max;
        }

        //Núi
        public static float RidgeNoise(float3 value, uint seed = 1, float frequency = 1.0f, float amplitude = 1.0f
            , int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f)
        {
            var _total = 0f;
            var _frequency = frequency;
            var _amplitude = amplitude;
            var _max = 0f;

            for (var i = 0; i < octaves; i++)
            {
                var _signal = 1 - abs(snoise((value + CreateFromIndex(seed).NextFloat3(-100000, 100000)) * _frequency));
                _signal *= _signal;
                _total += _signal * _amplitude;
                _max += _amplitude;
                _amplitude *= persistence;
                _frequency *= lacunarity;
            }
            return _total / _max;
        }

        //Mây
        public static float BillowNoise(float3 value, uint seed = 1, float frequency = 1.0f, float amplitude = 1.0f
            , int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f)
        {
            var _total = 0f;
            var _frequency = frequency;
            var _amplitude = amplitude;
            var _max = 0f;

            for (var i = 0; i < octaves; i++)
            {
                var _signal = 2f * abs(snoise((value + CreateFromIndex(seed).NextFloat3(-100000, 100000)) * _frequency)) - 1;
                _total += _signal * _amplitude;
                _max += _amplitude;
                _amplitude *= persistence;
                _frequency *= lacunarity;
            }
            return _total / _max;
        }

        //Biến dạng phức tạp
        public static float WrapNoise(float3 value, float strength = 0.5f, uint seed = 1, float frequency = 1.0f,
            float amplitude = 1.0f
            , int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f)
        {
            var warp = new float3(
                FractalNoise(value, seed, frequency, amplitude, octaves, persistence, lacunarity),
                FractalNoise(value, seed, frequency, amplitude, octaves, persistence, lacunarity),
                FractalNoise(value, seed, frequency, amplitude, octaves, persistence, lacunarity)
                );
            return FractalNoise(value + warp * strength, seed, frequency, amplitude, octaves, persistence, lacunarity);
        }
        
        //Trộn 2 noise
        public static float BlendNoise(float noiseFirst, float noiseSecond, float factor)
            => lerp(noiseFirst, noiseSecond, factor);

        //Bậc thang
        public static float TerracedNoise(float3 value, int terrace = 10, float strength = 1f, uint seed = 1, float frequency = 1.0f,
            float amplitude = 1.0f
            , int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f)
        {
            var noise = FractalNoise(value, seed, frequency, amplitude, octaves, persistence, lacunarity);
            var step = floor(noise * terrace) / terrace;
            return lerp(noise, step, strength);
        }
        
        //Hỗn loạn
        public static float TurbulenceNoise(float3 value, uint seed = 1, float frequency = 1.0f, float amplitude = 1.0f
            , int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f)
        {
            var _total = 0f;
            var _amplitude = amplitude;
            var _frequency = frequency;
            var _max = 0f;

            for (var i = 0; i < octaves; i++)
            {
                _total += abs(snoise((value + CreateFromIndex(seed).NextFloat3(-100000, 100000)) * _frequency)) * _amplitude;
                _max += _amplitude;
                _amplitude *= persistence;
                _frequency *= lacunarity;
            }
            return _total / _max;
        }
        
        //Tế bào
        public static float CellularNoise(float3 value, uint seed, float jitter = 1.0f)
        {
            var _cell = floor(value );
            var _local = value - _cell;

            var _min = 8f;

            for (var z = -1; z <= 1; z++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        var _neighbor = new float3(x, y, z);
                        var _neighborCell = _cell + _neighbor;
                        var _cellSeed = hash((int3)(new float3(_neighborCell.x, _neighborCell.y, _neighborCell.z)));
                        var _rand = CreateFromIndex(_cellSeed ^ seed);
                        
                        var _offset = _rand.NextFloat3();
                        var _featurePoint = _neighbor + _offset * clamp(jitter, 0f, 1f);
                        var _distance = length(_featurePoint - _local);
                        _min = min(_min, _distance);
                    }
                }
            }
            return saturate(_min);
        }
        
        
    }
}