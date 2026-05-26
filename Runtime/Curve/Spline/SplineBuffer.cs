using Unity.Entities;

namespace XO.Curve
{
    public struct SplineState : IComponentData
    {
        public SplineType Type;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U1)]
        public bool IsClosed;
        public float TotalWeight;
    }

    [InternalBufferCapacity(5)]
    public struct SplineElement<T> : IBufferElementData where T : unmanaged
    {
        public T Element;
        public float SegmentWeight;
    }
}
