using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Curve
{
    [BurstCompile]
    public static partial class Spline
    {
        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<float>> splineBlobRef, float t, ref float result)
        {
            ref var splineBlob = ref splineBlobRef.Value;
            var n = splineBlob.Points.Length;
            switch (splineBlob.SplineType)
            {
                case SplineType.None:
                    result = t;
                    break;
                case SplineType.Linear:
                    result = t;
                    break;
                case SplineType.CubicBezier:
                    if (n < 4 || (n - 1) % 3 != 0)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segmentCount = (n - 1) / 3;

                    var weights = new NativeArray<float>(segmentCount, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    var totalWeight = 0f;
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var p0 = splineBlob.Points[i * 3 + 0];
                        var p3 = splineBlob.Points[i * 3 + 3];
                        var w = math.distance(p0, p3);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    var target = t * totalWeight;
                    var accum = 0f;
                    var segIdx = 0;
                    for (; segIdx < segmentCount; segIdx++)
                    {
                        if (accum + weights[segIdx] >= target) break;
                        accum += weights[segIdx];
                    }

                    var segWeight = weights[math.clamp(segIdx, 0, segmentCount - 1)];
                    var localT = math.clamp((target - accum) / segWeight, 0f, 1f);
                    weights.Dispose();

                    var idx = segIdx * 3;
                    var point0 = splineBlob.Points[idx];
                    var point1 = splineBlob.Points[idx + 1];
                    var point2 = splineBlob.Points[idx + 2];
                    var point3 = splineBlob.Points[idx + 3];

                    var u = 1f - localT;
                    var tt = localT * localT;
                    var uu = u * u;
                    var uuu = uu * u;
                    var ttt = tt * localT;

                    var p = uuu * point0;
                    p += 3f * uu * localT * point1;
                    p += 3f * u * tt * point2;
                    p += ttt * point3;

                    result = p;
                    break;
                case SplineType.CatmullRom:
                    if (n < 4)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    segmentCount = splineBlob.IsClosed ? n : n - 3;
                    totalWeight = 0f;
                    weights = new NativeArray<float>(segmentCount, Allocator.Temp);
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var i1 = (i + 1) % n;
                        var i2 = (i + 2) % n;
                        var w = math.distance(splineBlob.Points[i1], splineBlob.Points[i2]);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    target = t * totalWeight;
                    idx = 0;
                    accum = 0f;
                    for (; idx < segmentCount; idx++)
                    {
                        if (accum + weights[idx] >= target) break;
                        accum += weights[idx];
                    }

                    var segmentWeight = weights[idx];
                    localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);

                    point0 = splineBlob.Points[(idx + 0) % n];
                    point1 = splineBlob.Points[(idx + 1) % n];
                    point2 = splineBlob.Points[(idx + 2) % n];
                    point3 = splineBlob.Points[(idx + 3) % n];

                    var t2 = localT * localT;
                    var t3 = t2 * localT;

                    result = 0.5f * (
                        (2f * point1) +
                        (-point0 + point2) * localT +
                        (2f * point0 - 5f * point1 + 4f * point2 - point3) * t2 +
                        (-point0 + 3f * point1 - 3f * point2 + point3) * t3);
                    weights.Dispose();
                    break;
                case SplineType.BSpline:
                    if (n < 4)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segments = splineBlob.IsClosed ? n : n - 3;
                    totalWeight = 0f;
                    weights = new NativeArray<float>(segments, Allocator.Temp);
                    for (int i = 0; i < segments; i++)
                    {
                        int i1 = (i + 1) % n;
                        int i2 = (i + 2) % n;
                        float w = math.distance(splineBlob.Points[i1], splineBlob.Points[i2]);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    target = t * totalWeight;
                    idx = 0;
                    accum = 0f;
                    for (; idx < segments; idx++)
                    {
                        if (accum + weights[idx] >= target) break;
                        accum += weights[idx];
                    }

                    segmentWeight = weights[idx];
                    localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);

                    point0 = splineBlob.Points[(idx + 0) % n];
                    point1 = splineBlob.Points[(idx + 1) % n];
                    point2 = splineBlob.Points[(idx + 2) % n];
                    point3 = splineBlob.Points[(idx + 3) % n];

                    t2 = localT * localT;
                    t3 = t2 * localT;

                    result = (1f / 6f) * (
                        (-point0 + 3f * point1 - 3f * point2 + point3) * t3 +
                        (3f * point0 - 6f * point1 + 3f * point2) * t2 +
                        (-3f * point0 + 3f * point2) * localT +
                        (point0 + 4f * point1 + point2));
                    weights.Dispose();
                    break;
                default:
                    result = splineBlob.Points[0];
                    break;
            }
        }

        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<float2>> splineBlobRef, float t, ref float2 result)
        {
            ref var splineBlob = ref splineBlobRef.Value;
            var n = splineBlob.Points.Length;
            switch (splineBlob.SplineType)
            {
                case SplineType.None:
                    result = t;
                    break;
                case SplineType.Linear:
                    result = t;
                    break;
                case SplineType.CubicBezier:
                    if (n < 4 || (n - 1) % 3 != 0)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segmentCount = (n - 1) / 3;

                    var weights = new NativeArray<float>(segmentCount, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    var totalWeight = 0f;
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var p0 = splineBlob.Points[i * 3 + 0];
                        var p3 = splineBlob.Points[i * 3 + 3];
                        var w = math.distance(p0, p3);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    var target = t * totalWeight;
                    var accum = 0f;
                    var segIdx = 0;
                    for (; segIdx < segmentCount; segIdx++)
                    {
                        if (accum + weights[segIdx] >= target) break;
                        accum += weights[segIdx];
                    }

                    var segWeight = weights[math.clamp(segIdx, 0, segmentCount - 1)];
                    var localT = math.clamp((target - accum) / segWeight, 0f, 1f);
                    weights.Dispose();

                    var idx = segIdx * 3;
                    var point0 = splineBlob.Points[idx];
                    var point1 = splineBlob.Points[idx + 1];
                    var point2 = splineBlob.Points[idx + 2];
                    var point3 = splineBlob.Points[idx + 3];

                    var u = 1f - localT;
                    var tt = localT * localT;
                    var uu = u * u;
                    var uuu = uu * u;
                    var ttt = tt * localT;

                    var p = uuu * point0;
                    p += 3f * uu * localT * point1;
                    p += 3f * u * tt * point2;
                    p += ttt * point3;

                    result = p;
                    break;
                case SplineType.CatmullRom:
                    if (n < 4)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    segmentCount = splineBlob.IsClosed ? n : n - 3;
                    totalWeight = 0f;
                    weights = new NativeArray<float>(segmentCount, Allocator.Temp);
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var i1 = (i + 1) % n;
                        var i2 = (i + 2) % n;
                        var w = math.distance(splineBlob.Points[i1], splineBlob.Points[i2]);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    target = t * totalWeight;
                    idx = 0;
                    accum = 0f;
                    for (; idx < segmentCount; idx++)
                    {
                        if (accum + weights[idx] >= target) break;
                        accum += weights[idx];
                    }

                    var segmentWeight = weights[idx];
                    localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);

                    point0 = splineBlob.Points[(idx + 0) % n];
                    point1 = splineBlob.Points[(idx + 1) % n];
                    point2 = splineBlob.Points[(idx + 2) % n];
                    point3 = splineBlob.Points[(idx + 3) % n];

                    var t2 = localT * localT;
                    var t3 = t2 * localT;

                    result = 0.5f * (
                        (2f * point1) +
                        (-point0 + point2) * localT +
                        (2f * point0 - 5f * point1 + 4f * point2 - point3) * t2 +
                        (-point0 + 3f * point1 - 3f * point2 + point3) * t3);
                    weights.Dispose();
                    break;
                case SplineType.BSpline:
                    if (n < 4)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segments = splineBlob.IsClosed ? n : n - 3;
                    totalWeight = 0f;
                    weights = new NativeArray<float>(segments, Allocator.Temp);
                    for (int i = 0; i < segments; i++)
                    {
                        int i1 = (i + 1) % n;
                        int i2 = (i + 2) % n;
                        float w = math.distance(splineBlob.Points[i1], splineBlob.Points[i2]);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    target = t * totalWeight;
                    idx = 0;
                    accum = 0f;
                    for (; idx < segments; idx++)
                    {
                        if (accum + weights[idx] >= target) break;
                        accum += weights[idx];
                    }

                    segmentWeight = weights[idx];
                    localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);

                    point0 = splineBlob.Points[(idx + 0) % n];
                    point1 = splineBlob.Points[(idx + 1) % n];
                    point2 = splineBlob.Points[(idx + 2) % n];
                    point3 = splineBlob.Points[(idx + 3) % n];

                    t2 = localT * localT;
                    t3 = t2 * localT;

                    result = (1f / 6f) * (
                        (-point0 + 3f * point1 - 3f * point2 + point3) * t3 +
                        (3f * point0 - 6f * point1 + 3f * point2) * t2 +
                        (-3f * point0 + 3f * point2) * localT +
                        (point0 + 4f * point1 + point2));
                    weights.Dispose();
                    break;
                default:
                    result = splineBlob.Points[0];
                    break;
            }
        }

        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<float3>> splineBlobRef, float t, ref float3 result)
        {
            ref var splineBlob = ref splineBlobRef.Value;
            var n = splineBlob.Points.Length;
            switch (splineBlob.SplineType)
            {
                case SplineType.None:
                    result = t;
                    break;
                case SplineType.Linear:
                    result = t;
                    break;
                case SplineType.CubicBezier:
                    if (n < 4 || (n - 1) % 3 != 0)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segmentCount = (n - 1) / 3;

                    var weights = new NativeArray<float>(segmentCount, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    var totalWeight = 0f;
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var p0 = splineBlob.Points[i * 3 + 0];
                        var p3 = splineBlob.Points[i * 3 + 3];
                        var w = math.distance(p0, p3);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    var target = t * totalWeight;
                    var accum = 0f;
                    var segIdx = 0;
                    for (; segIdx < segmentCount; segIdx++)
                    {
                        if (accum + weights[segIdx] >= target) break;
                        accum += weights[segIdx];
                    }

                    var segWeight = weights[math.clamp(segIdx, 0, segmentCount - 1)];
                    var localT = math.clamp((target - accum) / segWeight, 0f, 1f);
                    weights.Dispose();

                    var idx = segIdx * 3;
                    var point0 = splineBlob.Points[idx];
                    var point1 = splineBlob.Points[idx + 1];
                    var point2 = splineBlob.Points[idx + 2];
                    var point3 = splineBlob.Points[idx + 3];

                    var u = 1f - localT;
                    var tt = localT * localT;
                    var uu = u * u;
                    var uuu = uu * u;
                    var ttt = tt * localT;

                    var p = uuu * point0;
                    p += 3f * uu * localT * point1;
                    p += 3f * u * tt * point2;
                    p += ttt * point3;

                    result = p;
                    break;
                case SplineType.CatmullRom:
                    if (n < 4)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    segmentCount = splineBlob.IsClosed ? n : n - 3;
                    totalWeight = 0f;
                    weights = new NativeArray<float>(segmentCount, Allocator.Temp);
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var i1 = (i + 1) % n;
                        var i2 = (i + 2) % n;
                        var w = math.distance(splineBlob.Points[i1], splineBlob.Points[i2]);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    target = t * totalWeight;
                    idx = 0;
                    accum = 0f;
                    for (; idx < segmentCount; idx++)
                    {
                        if (accum + weights[idx] >= target) break;
                        accum += weights[idx];
                    }

                    var segmentWeight = weights[idx];
                    localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);

                    point0 = splineBlob.Points[(idx + 0) % n];
                    point1 = splineBlob.Points[(idx + 1) % n];
                    point2 = splineBlob.Points[(idx + 2) % n];
                    point3 = splineBlob.Points[(idx + 3) % n];

                    var t2 = localT * localT;
                    var t3 = t2 * localT;

                    result = 0.5f * (
                        (2f * point1) +
                        (-point0 + point2) * localT +
                        (2f * point0 - 5f * point1 + 4f * point2 - point3) * t2 +
                        (-point0 + 3f * point1 - 3f * point2 + point3) * t3);
                    weights.Dispose();
                    break;
                case SplineType.BSpline:
                    if (n < 4)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segments = splineBlob.IsClosed ? n : n - 3;
                    totalWeight = 0f;
                    weights = new NativeArray<float>(segments, Allocator.Temp);
                    for (int i = 0; i < segments; i++)
                    {
                        int i1 = (i + 1) % n;
                        int i2 = (i + 2) % n;
                        float w = math.distance(splineBlob.Points[i1], splineBlob.Points[i2]);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    target = t * totalWeight;
                    idx = 0;
                    accum = 0f;
                    for (; idx < segments; idx++)
                    {
                        if (accum + weights[idx] >= target) break;
                        accum += weights[idx];
                    }

                    segmentWeight = weights[idx];
                    localT = math.clamp((target - accum) / segmentWeight, 0f, 1f);

                    point0 = splineBlob.Points[(idx + 0) % n];
                    point1 = splineBlob.Points[(idx + 1) % n];
                    point2 = splineBlob.Points[(idx + 2) % n];
                    point3 = splineBlob.Points[(idx + 3) % n];

                    t2 = localT * localT;
                    t3 = t2 * localT;

                    result = (1f / 6f) * (
                        (-point0 + 3f * point1 - 3f * point2 + point3) * t3 +
                        (3f * point0 - 6f * point1 + 3f * point2) * t2 +
                        (-3f * point0 + 3f * point2) * localT +
                        (point0 + 4f * point1 + point2));
                    weights.Dispose();
                    break;
                default:
                    result = splineBlob.Points[0];
                    break;
            }
        }

        [BurstCompile]
        public static void Sample(in BlobAssetReference<SplineBlob<quaternion>> splineBlobRef, float t,
            ref quaternion result)
        {
            ref var splineBlob = ref splineBlobRef.Value;
            var n = splineBlob.Points.Length;
            switch (splineBlob.SplineType)
            {
                case SplineType.None:
                    result = splineBlob.Points[0];
                    break;
                case SplineType.Linear:
                    result = splineBlob.Points[0];

                    break;
                case SplineType.CubicBezier:
                    if (n < 4 || (n - 1) % 3 != 0)
                    {
                        result = splineBlob.Points[0];
                        return;
                    }

                    var segmentCount = (n - 1) / 3;

                    var weights = new NativeArray<float>(segmentCount, Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    var totalWeight = 0f;
                    for (var i = 0; i < segmentCount; i++)
                    {
                        var p0 = splineBlob.Points[i * 3 + 0];
                        var p3 = splineBlob.Points[i * 3 + 3];
                        var w = math.dot(p0, p3);
                        weights[i] = w;
                        totalWeight += w;
                    }

                    var target = t * totalWeight;
                    var accum = 0f;
                    var segIdx = 0;
                    for (; segIdx < segmentCount; segIdx++)
                    {
                        if (accum + weights[segIdx] >= target) break;
                        accum += weights[segIdx];
                    }

                    var segWeight = weights[math.clamp(segIdx, 0, segmentCount - 1)];
                    var localT = math.clamp((target - accum) / segWeight, 0f, 1f);
                    weights.Dispose();

                    var idx = segIdx * 3;
                    var point0 = splineBlob.Points[idx];
                    var point1 = splineBlob.Points[idx + 1];
                    var point2 = splineBlob.Points[idx + 2];
                    var point3 = splineBlob.Points[idx + 3];

                    var a = math.slerp(point0, point1, t);
                    var b = math.slerp(point1, point2, t);
                    var c = math.slerp(point2, point3, t);

                    var d = math.slerp(a, b, t);
                    var e = math.slerp(b, c, t);
                    result = math.slerp(d, e, t);
                    break;
                case SplineType.CatmullRom:
                    break;
                case SplineType.BSpline:
                    break;
                default:
                    result = splineBlob.Points[0];
                    break;
            }
        }
    }

    [System.Serializable]
    public enum SplineType
    {
        None,
        Linear,
        CubicBezier,
        CatmullRom,
        BSpline,
    }
}