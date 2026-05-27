using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Curve
{
    public interface ISplineAdapterBase
    {
        SplineType SplineType { get; }
        bool IsClosed { get; }
        float TotalWeight { get; }
        int ElementCount { get; }
        int SegmentCount { get; }
        float GetSegmentWeight(int index);
    }

    public interface ISplineAdapter<T> : ISplineAdapterBase where T : unmanaged
    {
        int KnotCount { get; }
        T GetElement(int index);
        T GetKnot(int index);
    }
    
    public static partial class Spline
    {
        public struct BlobSplineAdapter<T> : ISplineAdapter<T> where T : unmanaged
        {
            private BlobAssetReference<SplineBlob<T>> _blob;

            public BlobSplineAdapter(BlobAssetReference<SplineBlob<T>> blob)
            {
                _blob = blob;
            }
            
            public SplineType SplineType => _blob.Value.SplineType;
            public bool IsClosed => _blob.Value.IsClosed;
            public float TotalWeight => _blob.Value.TotalWeight;
            public int ElementCount => _blob.Value.Elements.Length;
            public int KnotCount => GetKnotCount(ElementCount, SplineType, IsClosed);
            public int SegmentCount => _blob.Value.SegmentWeights.Length;
            public float GetSegmentWeight(int index) => _blob.Value.SegmentWeights[index];
            public T GetElement(int index) => _blob.Value.Elements[index];
            public T GetKnot(int index) => _blob.Value.Elements[GetKnotElementIndex(index, ElementCount, SplineType, IsClosed)];
        }

        public struct BufferSplineAdapter<T> : ISplineAdapter<T> where T : unmanaged
        {
            private readonly SplineState _state;
            private DynamicBuffer<SplineElement<T>> _buffer;
            
            public BufferSplineAdapter(in SplineState state, in DynamicBuffer<SplineElement<T>> buffer)
            {
                _state = state;
                _buffer = buffer;
            }
            
            public SplineType SplineType => _state.Type;
            public bool IsClosed => _state.IsClosed;
            public float TotalWeight => _state.TotalWeight;
            public int ElementCount => _buffer.Length;
            public int KnotCount => GetKnotCount(ElementCount, SplineType, IsClosed);
            public int SegmentCount => GetSegmentCount(_buffer.Length, _state.Type, _state.IsClosed);
            public float GetSegmentWeight(int index) => _buffer[index].SegmentWeight;
            public T GetElement(int index) => _buffer[index].Element;
            public T GetKnot(int index) => _buffer[GetKnotElementIndex(index, ElementCount, SplineType, IsClosed)].Element;
        }

        public readonly struct EditorSplineAdapter : ISplineAdapter<float3>
        {
            private readonly List<float3> _list;
            private readonly SplineType _type;
            private readonly bool _isClosed;

            public EditorSplineAdapter(List<float3> list, SplineType type, bool isClosed)
            {
                this._list = list;
                this._type = type;
                this._isClosed = isClosed;
            }

            public SplineType SplineType => _type;
            public bool IsClosed => _isClosed;
            public float TotalWeight => SegmentCount;
            public int ElementCount => _list.Count;
            public int KnotCount => _list.Count;
            public int SegmentCount => Spline.GetSegmentCount(_list.Count, _type, _isClosed);
            public float GetSegmentWeight(int index) => 1.0f;
            public float3 GetElement(int index) => _list[index];
            public float3 GetKnot(int index) => _list[index];
        }

        private static int GetKnotCount(int elementCount, SplineType type, bool isClosed)
        {
            if (elementCount <= 0) return 0;
            switch (type)
            {
                case SplineType.CubicBezier:
                    return isClosed ? math.max(0, elementCount / 3) : math.max(0, (elementCount + 2) / 3);
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    return isClosed ? elementCount : math.max(0, elementCount - 2);
                default:
                    return elementCount;
            }
        }

        private static int GetKnotElementIndex(int knotIndex, int elementCount, SplineType type, bool isClosed)
        {
            int knotCount = GetKnotCount(elementCount, type, isClosed);
            if (knotCount <= 0) return 0;

            knotIndex = math.clamp(knotIndex, 0, knotCount - 1);
            switch (type)
            {
                case SplineType.CubicBezier:
                    return knotIndex * 3;
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    return isClosed ? knotIndex : knotIndex + 1;
                default:
                    return knotIndex;
            }
        }
    }
}
