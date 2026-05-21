using System;
using Unity.Mathematics;

namespace XO.Curve
{
    public static class SplineUtility
    {
        public static int GetTargetTangentLength(SplineType splineType, bool isClosed, int pointCount)
        {
            if (splineType == SplineType.CubicBezier)
            {
                return pointCount * 2;
            }
            else if (splineType == SplineType.CatmullRom || splineType == SplineType.BSpline)
            {
                return isClosed ? 0 : 2;
            }
            return 0;
        }

        public static void InitializeOrResizeTangents<T>(SplineType splineType, bool isClosed, T[] points, ref T[] tangents, bool autoCalculate = true) where T : unmanaged
        {
            if (points == null)
            {
                tangents = null;
                return;
            }

            int n = points.Length;
            int targetLength = GetTargetTangentLength(splineType, isClosed, n);

            if (tangents == null || tangents.Length != targetLength)
            {
                var oldTangents = tangents;
                tangents = new T[targetLength];

                if (oldTangents != null && oldTangents.Length > 0)
                {
                    int copyCount = math.min(oldTangents.Length, targetLength);
                    Array.Copy(oldTangents, tangents, copyCount);
                }

                if (autoCalculate)
                {
                    RecalculateAllTangents(splineType, isClosed, points, tangents);
                }
            }
        }

        public static void RecalculateAllTangents<T>(SplineType splineType, bool isClosed, T[] points, T[] tangents) where T : unmanaged
        {
            if (points == null || tangents == null || points.Length == 0) return;
            int n = points.Length;
            if (splineType == SplineType.CubicBezier)
            {
                for (int i = 0; i < n; i++)
                {
                    CalculateDefaultTangents(splineType, isClosed, points, tangents, i);
                }
            }
            else if ((splineType == SplineType.CatmullRom || splineType == SplineType.BSpline) && !isClosed)
            {
                if (tangents.Length == 2)
                {
                    CalculateDefaultTangents(splineType, isClosed, points, tangents, 0);
                    CalculateDefaultTangents(splineType, isClosed, points, tangents, 1);
                }
            }
        }

        public static void CalculateDefaultTangents<T>(SplineType type, bool isClosed, T[] points, T[] tangents, int i) where T : unmanaged
        {
            if (points == null || tangents == null || points.Length == 0) return;
            int n = points.Length;

            if (type == SplineType.CubicBezier)
            {
                if (tangents.Length != n * 2) return;

                if (typeof(T) == typeof(float3))
                {
                    var pts = (float3[])(object)points;
                    var tgts = (float3[])(object)tangents;

                    float3 p = pts[i];
                    float3 inT = float3.zero;
                    float3 outT = float3.zero;

                    if (n > 1)
                    {
                        float3 dir;
                        float dist;
                        if (i > 0 && i < n - 1)
                        {
                            dir = math.normalizesafe(pts[i + 1] - pts[i - 1]);
                            float d1 = math.distance(p, pts[i - 1]);
                            float d2 = math.distance(p, pts[i + 1]);
                            dist = (d1 + d2) * 0.5f;
                        }
                        else if (i == 0)
                        {
                            if (isClosed)
                            {
                                dir = math.normalizesafe(pts[1] - pts[n - 1]);
                                float d1 = math.distance(p, pts[n - 1]);
                                float d2 = math.distance(p, pts[1]);
                                dist = (d1 + d2) * 0.5f;
                            }
                            else
                            {
                                dir = math.normalizesafe(pts[1] - p);
                                dist = math.distance(p, pts[1]);
                            }
                        }
                        else
                        {
                            if (isClosed)
                            {
                                dir = math.normalizesafe(pts[0] - pts[n - 2]);
                                float d1 = math.distance(p, pts[n - 2]);
                                float d2 = math.distance(p, pts[0]);
                                dist = (d1 + d2) * 0.5f;
                            }
                            else
                            {
                                dir = math.normalizesafe(p - pts[n - 2]);
                                dist = math.distance(p, pts[n - 2]);
                            }
                        }

                        if (math.lengthsq(dir) < 0.001f)
                        {
                            dir = new float3(0f, 0f, 1f);
                        }

                        float strength = dist * 0.25f;
                        inT = -dir * strength;
                        outT = dir * strength;
                    }
                    else
                    {
                        inT = new float3(-1f, 0f, 0f);
                        outT = new float3(1f, 0f, 0f);
                    }

                    tgts[i * 2] = inT;
                    tgts[i * 2 + 1] = outT;
                }
                else if (typeof(T) == typeof(float2))
                {
                    var pts = (float2[])(object)points;
                    var tgts = (float2[])(object)tangents;

                    float2 p = pts[i];
                    float2 inT = float2.zero;
                    float2 outT = float2.zero;

                    if (n > 1)
                    {
                        float2 dir;
                        float dist;
                        if (i > 0 && i < n - 1)
                        {
                            dir = math.normalizesafe(pts[i + 1] - pts[i - 1]);
                            float d1 = math.distance(p, pts[i - 1]);
                            float d2 = math.distance(p, pts[i + 1]);
                            dist = (d1 + d2) * 0.5f;
                        }
                        else if (i == 0)
                        {
                            if (isClosed)
                            {
                                dir = math.normalizesafe(pts[1] - pts[n - 1]);
                                float d1 = math.distance(p, pts[n - 1]);
                                float d2 = math.distance(p, pts[1]);
                                dist = (d1 + d2) * 0.5f;
                            }
                            else
                            {
                                dir = math.normalizesafe(pts[1] - p);
                                dist = math.distance(p, pts[1]);
                            }
                        }
                        else
                        {
                            if (isClosed)
                            {
                                dir = math.normalizesafe(pts[0] - pts[n - 2]);
                                float d1 = math.distance(p, pts[n - 2]);
                                float d2 = math.distance(p, pts[0]);
                                dist = (d1 + d2) * 0.5f;
                            }
                            else
                            {
                                dir = math.normalizesafe(p - pts[n - 2]);
                                dist = math.distance(p, pts[n - 2]);
                            }
                        }

                        if (math.lengthsq(dir) < 0.001f)
                        {
                            dir = new float2(1f, 0f);
                        }

                        float strength = dist * 0.25f;
                        inT = -dir * strength;
                        outT = dir * strength;
                    }
                    else
                    {
                        inT = new float2(-1f, 0f);
                        outT = new float2(1f, 0f);
                    }

                    tgts[i * 2] = inT;
                    tgts[i * 2 + 1] = outT;
                }
                else if (typeof(T) == typeof(float))
                {
                    var pts = (float[])(object)points;
                    var tgts = (float[])(object)tangents;

                    float p = pts[i];
                    float inT = 0f;
                    float outT = 0f;

                    if (n > 1)
                    {
                        float dir;
                        float dist;
                        if (i > 0 && i < n - 1)
                        {
                            dir = math.sign(pts[i + 1] - pts[i - 1]);
                            float d1 = math.abs(p - pts[i - 1]);
                            float d2 = math.abs(p - pts[i + 1]);
                            dist = (d1 + d2) * 0.5f;
                        }
                        else if (i == 0)
                        {
                            if (isClosed)
                            {
                                dir = math.sign(pts[1] - pts[n - 1]);
                                float d1 = math.abs(p - pts[n - 1]);
                                float d2 = math.abs(p - pts[1]);
                                dist = (d1 + d2) * 0.5f;
                            }
                            else
                            {
                                dir = math.sign(pts[1] - p);
                                dist = math.abs(p - pts[1]);
                            }
                        }
                        else
                        {
                            if (isClosed)
                            {
                                dir = math.sign(pts[0] - pts[n - 2]);
                                float d1 = math.abs(p - pts[n - 2]);
                                float d2 = math.abs(p - pts[0]);
                                dist = (d1 + d2) * 0.5f;
                            }
                            else
                            {
                                dir = math.sign(p - pts[n - 2]);
                                dist = math.abs(p - pts[n - 2]);
                            }
                        }

                        float strength = dist * 0.25f;
                        inT = -dir * strength;
                        outT = dir * strength;
                    }
                    else
                    {
                        inT = -1f;
                        outT = 1f;
                    }

                    tgts[i * 2] = inT;
                    tgts[i * 2 + 1] = outT;
                }
            }
            else if (type == SplineType.CatmullRom || type == SplineType.BSpline)
            {
                if (isClosed || tangents.Length != 2) return;

                if (typeof(T) == typeof(float3))
                {
                    var pts = (float3[])(object)points;
                    var tgts = (float3[])(object)tangents;
                    if (i == 0)
                    {
                        tgts[0] = n > 1 ? (pts[0] - pts[1]) * 0.5f : new float3(-1f, 0f, 0f);
                    }
                    else if (i == 1)
                    {
                        tgts[1] = n > 1 ? (pts[n - 1] - pts[n - 2]) * 0.5f : new float3(1f, 0f, 0f);
                    }
                }
                else if (typeof(T) == typeof(float2))
                {
                    var pts = (float2[])(object)points;
                    var tgts = (float2[])(object)tangents;
                    if (i == 0)
                    {
                        tgts[0] = n > 1 ? (pts[0] - pts[1]) * 0.5f : new float2(-1f, 0f);
                    }
                    else if (i == 1)
                    {
                        tgts[1] = n > 1 ? (pts[n - 1] - pts[n - 2]) * 0.5f : new float2(1f, 0f);
                    }
                }
                else if (typeof(T) == typeof(float))
                {
                    var pts = (float[])(object)points;
                    var tgts = (float[])(object)tangents;
                    if (i == 0)
                    {
                        tgts[0] = n > 1 ? (pts[0] - pts[1]) * 0.5f : -1f;
                    }
                    else if (i == 1)
                    {
                        tgts[1] = n > 1 ? (pts[n - 1] - pts[n - 2]) * 0.5f : 1f;
                    }
                }
            }
        }

        public static T[] GetFlatPointsArray<T, TMath>(SplineType splineType, bool isClosed, T[] points, T[] tangents, TMath mathProvider = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            if (points == null || points.Length == 0) return new T[0];
            int N = points.Length;

            if (splineType == SplineType.CubicBezier)
            {
                if (tangents == null || tangents.Length < N * 2) return points;

                if (isClosed)
                {
                    int flatSize = 3 * N + 1;
                    var flat = new T[flatSize];
                    for (int i = 0; i < N; i++)
                    {
                        flat[i * 3] = points[i];
                        flat[i * 3 + 1] = mathProvider.Add(points[i], tangents[i * 2 + 1]);
                        if (i < N - 1)
                        {
                            flat[i * 3 + 2] = mathProvider.Add(points[i + 1], tangents[(i + 1) * 2]);
                        }
                        else
                        {
                            flat[i * 3 + 2] = mathProvider.Add(points[0], tangents[0]);
                            flat[i * 3 + 3] = points[0];
                        }
                    }
                    return flat;
                }
                else
                {
                    int flatSize = 3 * N - 2;
                    var flat = new T[flatSize];
                    for (int i = 0; i < N; i++)
                    {
                        flat[i * 3] = points[i];
                        if (i < N - 1)
                        {
                            flat[i * 3 + 1] = mathProvider.Add(points[i], tangents[i * 2 + 1]);
                            flat[i * 3 + 2] = mathProvider.Add(points[i + 1], tangents[(i + 1) * 2]);
                        }
                    }
                    return flat;
                }
            }
            else if ((splineType == SplineType.CatmullRom || splineType == SplineType.BSpline) && !isClosed)
            {
                if (tangents == null || tangents.Length < 2) return points;
                int flatSize = N + 2;
                var flat = new T[flatSize];
                flat[0] = mathProvider.Add(points[0], tangents[0]);
                for (int i = 0; i < N; i++)
                {
                    flat[i + 1] = points[i];
                }
                flat[N + 1] = mathProvider.Add(points[N - 1], tangents[1]);
                return flat;
            }

            var result = new T[points.Length];
            Array.Copy(points, result, points.Length);
            return result;
        }
    }
}
