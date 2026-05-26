using Unity.Burst;
using Unity.Collections;
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
        public static void Sample(in BlobAssetReference<SplineBlob<float>> blob, float t, ref float result)
        {
            if (!blob.IsCreated) return;
            var provider = new BlobSplineAdapter<float>(blob);
            result = SampleGeneric<float, BlobSplineAdapter<float>, FloatMath>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<float2>> blob, float t, ref float2 result)
        {
            if (!blob.IsCreated) return;
            var provider = new BlobSplineAdapter<float2>(blob);
            result = SampleGeneric<float2, BlobSplineAdapter<float2>, Float2Math>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<float3>> blob, float t, ref float3 result)
        {
            if (!blob.IsCreated) return;
            var provider = new BlobSplineAdapter<float3>(blob);
            result = SampleGeneric<float3, BlobSplineAdapter<float3>, Float3Math>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<quaternion>> blob, float t, ref quaternion result)
        {
            if (!blob.IsCreated) return;
            var provider = new BlobSplineAdapter<quaternion>(blob);
            result = SampleGeneric<quaternion, BlobSplineAdapter<quaternion>, QuaternionMath>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in SplineState state, in DynamicBuffer<SplineElement<float>> buffer, float t, ref float result)
        {
            var provider = new BufferSplineAdapter<float>(state, buffer);
            result = SampleGeneric<float, BufferSplineAdapter<float>, FloatMath>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in SplineState state, in DynamicBuffer<SplineElement<float2>> buffer, float t, ref float2 result)
        {
            var provider = new BufferSplineAdapter<float2>(state, buffer);
            result = SampleGeneric<float2, BufferSplineAdapter<float2>, Float2Math>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in SplineState state, in DynamicBuffer<SplineElement<float3>> buffer, float t, ref float3 result)
        {
            var provider = new BufferSplineAdapter<float3>(state, buffer);
            result = SampleGeneric<float3, BufferSplineAdapter<float3>, Float3Math>(ref provider, t);
        }

        [BurstCompile]
        public static void Sample(in SplineState state, in DynamicBuffer<SplineElement<quaternion>> buffer,
            float t, ref quaternion result)
        {
            var provider = new BufferSplineAdapter<quaternion>(state, buffer);
            result = SampleGeneric<quaternion, BufferSplineAdapter<quaternion>, QuaternionMath>(ref provider, t);
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
            var n = provider.ElementCount;
            if (n == 0) return default;
            if (n == 1) return provider.GetElement(0);

            GetSegmentAndLocalT(ref provider, t, out var idx, out var localT);

            switch (type)
            {
                case SplineType.None:
                    return provider.GetElement(0);
                case SplineType.Step:
                    int pIdx = provider.IsClosed
                        ? (localT >= 1f ? (idx + 1) % n : idx)
                        : (localT >= 1f ? idx + 1 : idx);
                    return provider.GetElement(pIdx);
                case SplineType.Linear:
                    int i0 = idx;
                    int i1 = provider.IsClosed ? (idx + 1) % n : math.min(idx + 1, n - 1);
                    return mathProvider.Lerp(provider.GetElement(i0), provider.GetElement(i1), localT);
                case SplineType.CubicBezier:
                    int i = idx * 3;
                    if (i + 3 >= n) return provider.GetElement(n - 1);
                    return mathProvider.EvaluateSpline(type, provider.GetElement(i), provider.GetElement(i + 1),
                        provider.GetElement(i + 2), provider.GetElement(i + 3), localT);
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    int c0 = provider.IsClosed ? (idx) % n : math.clamp(idx, 0, n - 1);
                    int c1 = provider.IsClosed ? (idx + 1) % n : math.clamp(idx + 1, 0, n - 1);
                    int c2 = provider.IsClosed ? (idx + 2) % n : math.clamp(idx + 2, 0, n - 1);
                    int c3 = provider.IsClosed ? (idx + 3) % n : math.clamp(idx + 3, 0, n - 1);
                    return mathProvider.EvaluateSpline(type, provider.GetElement(c0), provider.GetElement(c1),
                        provider.GetElement(c2), provider.GetElement(c3), localT);
            }

            return provider.GetElement(0);
        }

        public static void PopulateSplineBuffer<T, TMath>(
            SplineType splineType,
            bool isClosed,
            NativeArray<T> points,
            ref SplineState splineState,
            DynamicBuffer<SplineElement<T>> buffer,
            TMath mathProvider = default
        ) where T : unmanaged
          where TMath : struct, ICurveMath<T>
        {
            int n = points.IsCreated ? points.Length : 0;
            if (n == 0) return;

            int segmentCount = 0;
            switch (splineType)
            {
                case SplineType.Linear:
                case SplineType.Step:
                    segmentCount = isClosed ? n : math.max(0, n - 1);
                    break;
                case SplineType.CubicBezier:
                    segmentCount = math.max(0, (n - 1) / 3);
                    break;
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    segmentCount = isClosed ? n : math.max(0, n - 3);
                    break;
            }

            buffer.Clear();
            for (int i = 0; i < n; i++)
            {
                buffer.Add(new SplineElement<T> { Element = points[i], SegmentWeight = 0f });
            }

            float totalWeight = 0f;
            if (segmentCount > 0)
            {
                for (int i = 0; i < segmentCount; i++)
                {
                    float w = 1f;
                    switch (splineType)
                    {
                        case SplineType.Linear:
                        case SplineType.Step:
                            {
                                int i1 = i;
                                int i2 = isClosed ? (i + 1) % n : i + 1;
                                w = math.max(mathProvider.GetDistance(points[i1], points[i2]), 1e-5f);
                            }
                            break;
                        case SplineType.CubicBezier:
                            {
                                int p0 = i * 3;
                                int p3 = i * 3 + 3;
                                if (p3 < n)
                                {
                                    w = math.max(mathProvider.GetDistance(points[p0], points[p3]), 1e-5f);
                                }
                            }
                            break;
                        case SplineType.CatmullRom:
                        case SplineType.BSpline:
                            {
                                int i1 = isClosed ? (i + 1) % n : i + 1;
                                int i2 = isClosed ? (i + 2) % n : i + 2;
                                w = math.max(mathProvider.GetDistance(points[i1], points[i2]), 1e-5f);
                            }
                            break;
                    }
                    var elem = buffer[i];
                    elem.SegmentWeight = w;
                    buffer[i] = elem;
                    totalWeight += w;
                }
            }

            splineState.Type = splineType;
            splineState.IsClosed = isClosed;
            splineState.TotalWeight = totalWeight;
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
