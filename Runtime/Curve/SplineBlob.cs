using Unity.Collections;
using Unity.Entities;

namespace XO.Curve
{
    public static partial class Spline
    {
        [System.Serializable]
        public class SplineData<T> where T : unmanaged
        {
            public SplineType splineType = SplineType.CubicBezier;
            public bool isClosed = false;
            public bool autoEnd = true;
            public T[] points;
        }

        public struct SplineBlob<T> where T : unmanaged
        {
            public SplineType SplineType;
            public bool IsClosed;
            public bool AutoEnd;
            public BlobArray<T> Points;
        }

        public static BlobAssetReference<SplineBlob<T>> CreateSplineBlob<T>(SplineData<T> splineData)
            where T : unmanaged
        {
            NativeArray<T> points = new NativeArray<T>(splineData.points, Allocator.Temp);
            return CreateSplineBlob(splineData.splineType, splineData.isClosed, splineData.autoEnd, points);
        }
        public static BlobAssetReference<SplineBlob<T>> CreateSplineBlob<T>(
            SplineType splineType,
            bool isClosed,
            bool autoEnd,
            NativeArray<T> points
        ) where T : unmanaged
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref SplineBlob<T> root = ref builder.ConstructRoot<SplineBlob<T>>();

            BlobBuilderArray<T> pointArray = builder.Allocate(ref root.Points, points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                pointArray[i] = points[i];
            }

            root.SplineType = splineType;
            root.IsClosed = isClosed;
            root.AutoEnd = autoEnd;

            var blobRef = builder.CreateBlobAssetReference<SplineBlob<T>>(Allocator.Persistent);
            builder.Dispose();
            return blobRef;
        }
    }
}