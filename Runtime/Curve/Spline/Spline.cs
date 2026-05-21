using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Curve
{
    [System.Serializable]
    public enum SplineType
    {
        None,
        Linear,
        Step,
        CubicBezier,
        CatmullRom,
        BSpline,
    }

    [BurstCompile]
    public static partial class Spline
    {
        [BurstCompile]
        public static float Sample(in BlobAssetReference<SplineBlob<float>> blob, float t)
        {
            var provider = new BlobSplineAdapter<float>(blob);
            return SampleGeneric<float, BlobSplineAdapter<float>, FloatMath>(ref provider, t);
        }

        [BurstCompile]
        public static float2 Sample(in BlobAssetReference<SplineBlob<float2>> blob, float t)
        {
            var provider = new BlobSplineAdapter<float2>(blob);
            return SampleGeneric<float2, BlobSplineAdapter<float2>, Float2Math>(ref provider, t);
        }

        [BurstCompile]
        public static float3 Sample(in BlobAssetReference<SplineBlob<float3>> blob, float t)
        {
            var provider = new BlobSplineAdapter<float3>(blob);
            return SampleGeneric<float3, BlobSplineAdapter<float3>, Float3Math>(ref provider, t);
        }

        [BurstCompile]
        public static quaternion Sample(in BlobAssetReference<SplineBlob<quaternion>> blob, float t)
        {
            var provider = new BlobSplineAdapter<quaternion>(blob);
            return SampleGeneric<quaternion, BlobSplineAdapter<quaternion>, QuaternionMath>(ref provider, t);
        }

        [BurstCompile]
        public static float Sample(in SplineState state, in DynamicBuffer<SplineElement<float>> buffer, float t)
        {
            var provider = new BufferSplineAdapter<float>(state, buffer);
            return SampleGeneric<float, BufferSplineAdapter<float>, FloatMath>(ref provider, t);
        }

        [BurstCompile]
        public static float2 Sample(in SplineState state, in DynamicBuffer<SplineElement<float2>> buffer, float t)
        {
            var provider = new BufferSplineAdapter<float2>(state, buffer);
            return SampleGeneric<float2, BufferSplineAdapter<float2>, Float2Math>(ref provider, t);
        }

        [BurstCompile]
        public static float3 Sample(in SplineState state, in DynamicBuffer<SplineElement<float3>> buffer, float t)
        {
            var provider = new BufferSplineAdapter<float3>(state, buffer);
            return SampleGeneric<float3, BufferSplineAdapter<float3>, Float3Math>(ref provider, t);
        }

        [BurstCompile]
        public static quaternion Sample(in SplineState state, in DynamicBuffer<SplineElement<quaternion>> buffer,
            float t)
        {
            var provider = new BufferSplineAdapter<quaternion>(state, buffer);
            return SampleGeneric<quaternion, BufferSplineAdapter<quaternion>, QuaternionMath>(ref provider, t);
        }

        private static void GetSegmentAndLocalT<TProvider>(ref TProvider provider, float t, out int idx,
            out float localT) where TProvider : struct, ISplineAdapterBase
        {
            int segmentCount = provider.SegmentCount;
            if (segmentCount <= 0)
            {
                idx = 0;
                localT = 0f;
                return;
            }

            if (provider.TotalWeight <= 1e-5f)
            {
                float unscaledT = math.clamp(t, 0f, 1f) * segmentCount;
                idx = (int)math.floor(unscaledT);
                localT = unscaledT - idx;
                if (idx >= segmentCount)
                {
                    idx = segmentCount - 1;
                    localT = 1f;
                }

                return;
            }

            float target = math.clamp(t, 0f, 1f) * provider.TotalWeight;
            float accum = 0f;
            idx = 0;

            for (; idx < segmentCount; idx++)
            {
                float weight = provider.GetSegmentWeight(idx);
                float next = accum + weight;
                if (next >= target) break;
                accum = next;
            }

            if (idx >= segmentCount) idx = segmentCount - 1;

            float segmentWeight = provider.GetSegmentWeight(idx);
            if (segmentWeight <= 1e-5f)
                localT = 0f;
            else
                localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);
        }

        public static T SampleGeneric<T, TSplineAdapter, TMath>(ref TSplineAdapter provider, float t,
            TMath mathProvider = default)
            where T : unmanaged
            where TSplineAdapter : struct, ISplineAdapter<T>
            where TMath : struct, ICurveMath<T>
        {
            var type = provider.SplineType;
            var n = provider.PointCount;
            if (n == 0) return default;
            if (n == 1) return provider.GetPoint(0);

            GetSegmentAndLocalT(ref provider, t, out var idx, out var localT);

            switch (type)
            {
                case SplineType.None:
                    return provider.GetPoint(0);
                case SplineType.Step:
                    int pIdx = provider.IsClosed
                        ? (localT >= 1f ? (idx + 1) % n : idx)
                        : (localT >= 1f ? idx + 1 : idx);
                    return provider.GetPoint(pIdx);
                case SplineType.Linear:
                    int i0 = idx;
                    int i1 = provider.IsClosed ? (idx + 1) % n : math.min(idx + 1, n - 1);
                    return mathProvider.Lerp(provider.GetPoint(i0), provider.GetPoint(i1), localT);
                case SplineType.CubicBezier:
                    int i = idx * 3;
                    if (i + 3 >= n) return provider.GetPoint(n - 1);
                    return mathProvider.EvaluateSpline(type, provider.GetPoint(i), provider.GetPoint(i + 1),
                        provider.GetPoint(i + 2), provider.GetPoint(i + 3), localT);
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    int c0 = provider.IsClosed ? (idx) % n : math.clamp(idx, 0, n - 1);
                    int c1 = provider.IsClosed ? (idx + 1) % n : math.clamp(idx + 1, 0, n - 1);
                    int c2 = provider.IsClosed ? (idx + 2) % n : math.clamp(idx + 2, 0, n - 1);
                    int c3 = provider.IsClosed ? (idx + 3) % n : math.clamp(idx + 3, 0, n - 1);
                    return mathProvider.EvaluateSpline(type, provider.GetPoint(c0), provider.GetPoint(c1),
                        provider.GetPoint(c2), provider.GetPoint(c3), localT);
            }

            return provider.GetPoint(0);
        }

        private static int GetSegmentCount(int n, SplineType type, bool isClosed)
        {
            if (n <= 0) return 0;
            switch (type)
            {
                case SplineType.Linear:
                case SplineType.Step:
                    return isClosed ? n : math.max(0, n - 1);
                case SplineType.CubicBezier:
                    return math.max(0, (n - 1) / 3);
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    return isClosed ? n : math.max(0, n - 3);
                default:
                    return isClosed ? n : math.max(0, n - 1);
            }
        }
    }
}