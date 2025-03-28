// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace GameUtilities.Runtime
{
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using System.Runtime.CompilerServices;
    using Unity.Burst;

    /// <summary>
    /// Struct Gradient tương thích với Burst Compiler, sử dụng Color của Unity
    /// để thuận tiện trong việc tích hợp với hệ thống có sẵn của Unity.
    /// </summary>
    [BurstCompile]
    public struct BurstGradient
    {
        // Cấu trúc cho các điểm màu
        public struct ColorKey
        {
            public Color Color;  // Sử dụng Color của Unity
            public float Time;   // Thời điểm trong khoảng [0,1]

            public ColorKey(Color color, float time)
            {
                Color = color;
                Time = math.clamp(time, 0f, 1f);
            }
        }

        // Cấu trúc cho các điểm alpha
        public struct AlphaKey
        {
            public float Alpha;
            public float Time;   // Thời điểm trong khoảng [0,1]

            public AlphaKey(float alpha, float time)
            {
                Alpha = alpha;
                Time = math.clamp(time, 0f, 1f);
            }
        }

        // Mảng các điểm màu và alpha
        [ReadOnly] private NativeArray<ColorKey> colorKeys;
        [ReadOnly] private NativeArray<AlphaKey> alphaKeys;
        
        // Khởi tạo Gradient với các mảng native
        public BurstGradient(NativeArray<ColorKey> colorKeys, NativeArray<AlphaKey> alphaKeys)
        {
            this.colorKeys = colorKeys;
            this.alphaKeys = alphaKeys;
        }

        // Tạo từ mảng thông thường và chuyển đổi sang NativeArray
        public BurstGradient(ColorKey[] colorKeys, AlphaKey[] alphaKeys, Allocator allocator = Allocator.Temp)
        {
            this.colorKeys = new NativeArray<ColorKey>(colorKeys, allocator);
            this.alphaKeys = new NativeArray<AlphaKey>(alphaKeys, allocator);
        }

        // Tạo từ Gradient thông thường của Unity
        public BurstGradient(Gradient unityGradient, Allocator allocator = Allocator.Temp)
        {
            GradientColorKey[] unityColorKeys = unityGradient.colorKeys;
            GradientAlphaKey[] unityAlphaKeys = unityGradient.alphaKeys;
            
            colorKeys = new NativeArray<ColorKey>(unityColorKeys.Length, allocator);
            for (int i = 0; i < unityColorKeys.Length; i++)
            {
                colorKeys[i] = new ColorKey(unityColorKeys[i].color, unityColorKeys[i].time);
            }
            
            alphaKeys = new NativeArray<AlphaKey>(unityAlphaKeys.Length, allocator);
            for (int i = 0; i < unityAlphaKeys.Length; i++)
            {
                alphaKeys[i] = new AlphaKey(unityAlphaKeys[i].alpha, unityAlphaKeys[i].time);
            }
        }

        // Giải phóng bộ nhớ khi không cần nữa
        public void Dispose()
        {
            if (colorKeys.IsCreated) colorKeys.Dispose();
            if (alphaKeys.IsCreated) alphaKeys.Dispose();
        }

        // Phương thức chính để lấy màu tại một thời điểm
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color Evaluate(float time)
        {
            time = math.clamp(time, 0f, 1f);
            
            // Tính toán giá trị màu sắc
            Color color = EvaluateColor(time);
            
            // Tính toán giá trị alpha
            float alpha = EvaluateAlpha(time);
            
            // Kết hợp màu và alpha
            color.a = alpha;
            
            return color;
        }

        // Phương thức hỗ trợ để nội suy màu
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Color EvaluateColor(float time)
        {
            if (colorKeys.Length == 0)
                return Color.black;
            
            if (colorKeys.Length == 1 || time <= colorKeys[0].Time)
                return colorKeys[0].Color;
            
            if (time >= colorKeys[^1].Time)
                return colorKeys[^1].Color;
            
            // Tìm 2 điểm màu gần nhất với thời điểm cần tính
            int leftIndex = 0;
            for (int i = 0; i < colorKeys.Length - 1; i++)
            {
                if (colorKeys[i].Time <= time && time <= colorKeys[i + 1].Time)
                {
                    leftIndex = i;
                    break;
                }
            }
            
            // Nội suy tuyến tính giữa 2 điểm màu
            float t = Mathf.InverseLerp(colorKeys[leftIndex].Time, colorKeys[leftIndex + 1].Time, time);
            return Color.Lerp(colorKeys[leftIndex].Color, colorKeys[leftIndex + 1].Color, t);
        }

        // Phương thức hỗ trợ để nội suy alpha
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EvaluateAlpha(float time)
        {
            if (alphaKeys.Length == 0)
                return 1f;
            
            if (alphaKeys.Length == 1 || time <= alphaKeys[0].Time)
                return alphaKeys[0].Alpha;
            
            if (time >= alphaKeys[^1].Time)
                return alphaKeys[^1].Alpha;
            
            // Tìm 2 điểm alpha gần nhất với thời điểm cần tính
            int leftIndex = 0;
            for (int i = 0; i < alphaKeys.Length - 1; i++)
            {
                if (alphaKeys[i].Time <= time && time <= alphaKeys[i + 1].Time)
                {
                    leftIndex = i;
                    break;
                }
            }
            
            // Nội suy tuyến tính giữa 2 điểm alpha
            float t = Mathf.InverseLerp(alphaKeys[leftIndex].Time, alphaKeys[leftIndex + 1].Time, time);
            return Mathf.Lerp(alphaKeys[leftIndex].Alpha, alphaKeys[leftIndex + 1].Alpha, t);
        }

        // Chuyển đổi sang Gradient thông thường của Unity
        public Gradient ToUnityGradient()
        {
            Gradient unityGradient = new Gradient();
            
            GradientColorKey[] unityColorKeys = new GradientColorKey[colorKeys.Length];
            for (int i = 0; i < colorKeys.Length; i++)
            {
                unityColorKeys[i] = new GradientColorKey(colorKeys[i].Color, colorKeys[i].Time);
            }
            
            GradientAlphaKey[] unityAlphaKeys = new GradientAlphaKey[alphaKeys.Length];
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                unityAlphaKeys[i] = new GradientAlphaKey(alphaKeys[i].Alpha, alphaKeys[i].Time);
            }
            
            unityGradient.SetKeys(unityColorKeys, unityAlphaKeys);
            return unityGradient;
        }
    }
}