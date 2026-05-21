using Unity.Entities;

namespace XO.Curve
{
    public struct SplineState : IComponentData
    {
        public SplineType Type;
        public bool IsClosed;
        public float TotalWeight;
    }

    [InternalBufferCapacity(5)]
    public struct SplineElement<T> : IBufferElementData where T : unmanaged
    {
        public T Point;
        public float SegmentWeight;
    }
}
