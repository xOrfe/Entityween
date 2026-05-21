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
        int PointCount { get; }
        int SegmentCount { get; }
        float GetSegmentWeight(int index);
    }

    public interface ISplineAdapter<T> : ISplineAdapterBase where T : unmanaged
    {
        T GetPoint(int index);
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
            public int PointCount => _blob.Value.Points.Length;
            public int SegmentCount => _blob.Value.SegmentWeights.Length;
            public float GetSegmentWeight(int index) => _blob.Value.SegmentWeights[index];
            public T GetPoint(int index) => _blob.Value.Points[index];
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
            public int PointCount => _buffer.Length;
            public int SegmentCount => GetSegmentCount(_buffer.Length, _state.Type, _state.IsClosed);
            public float GetSegmentWeight(int index) => _buffer[index].SegmentWeight;
            public T GetPoint(int index) => _buffer[index].Point;
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
            public int PointCount => _list.Count;
            public int SegmentCount => Spline.GetSegmentCount(_list.Count, _type, _isClosed);
            public float GetSegmentWeight(int index) => 1.0f;
            public float3 GetPoint(int index) => _list[index];
        }
    }
}
