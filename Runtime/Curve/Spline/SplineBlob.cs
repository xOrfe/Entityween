using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Curve
{
    public struct SplineBlob<T> where T : unmanaged
    {
        public SplineType SplineType;
        public bool IsClosed;
        public float TotalWeight;
        public BlobArray<T> Elements;
        public BlobArray<float> SegmentWeights;
    }
    
    public static partial class Spline
    {
        public static BlobAssetReference<SplineBlob<T>> CreateSplineBlob<T, TMath>(SerializableSpline<T> serializableSpline, TMath mathProvider = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            if (serializableSpline.points == null || serializableSpline.points.Length == 0)
            {
                using (var emptyPoints = new NativeArray<T>(0, Allocator.Temp))
                {
                    return CreateSplineBlob<T, TMath>(serializableSpline.splineType, serializableSpline.isClosed, emptyPoints, mathProvider);
                }
            }

            if (serializableSpline.splineType == SplineType.CubicBezier ||
                ((serializableSpline.splineType == SplineType.CatmullRom || serializableSpline.splineType == SplineType.BSpline) && !serializableSpline.isClosed))
            {
                serializableSpline.InitializeOrResizeTangents();
                var flatArray = SplineUtility.GetFlatPointsArray(serializableSpline.splineType, serializableSpline.isClosed, serializableSpline.points, serializableSpline.tangents, mathProvider);
                if (flatArray != null)
                {
                    var flatPoints = new NativeArray<T>(flatArray.Length, Allocator.Temp);
                    try
                    {
                        flatPoints.CopyFrom(flatArray);
                        return CreateSplineBlob<T, TMath>(serializableSpline.splineType, serializableSpline.isClosed, flatPoints, mathProvider);
                    }
                    finally
                    {
                        flatPoints.Dispose();
                    }
                }
            }

            using (var points = new NativeArray<T>(serializableSpline.points, Allocator.Temp))
            {
                return CreateSplineBlob<T, TMath>(serializableSpline.splineType, serializableSpline.isClosed, points, mathProvider);
            }
        }

        public static BlobAssetReference<SplineBlob<T>> CreateSplineBlob<T, TMath>(
            SplineType splineType,
            bool isClosed,
            NativeArray<T> points,
            TMath mathProvider = default
        ) where T : unmanaged
          where TMath : struct, ICurveMath<T>
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref SplineBlob<T> root = ref builder.ConstructRoot<SplineBlob<T>>();

            root.SplineType = splineType;
            root.IsClosed = isClosed;

            var pointArray = builder.Allocate(ref root.Elements, points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                pointArray[i] = points[i];
            }

            int n = points.Length;
            int segmentCount = 0;

            if (n > 0)
            {
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
            }

            var weightArray = builder.Allocate(ref root.SegmentWeights, segmentCount);
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
                    weightArray[i] = w;
                    totalWeight += w;
                }
            }

            root.TotalWeight = totalWeight;

            var blobRef = builder.CreateBlobAssetReference<SplineBlob<T>>(Allocator.Persistent);
            builder.Dispose();
            return blobRef;
        }
    }
}
