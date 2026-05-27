using Unity.Entities;
using XO.Curve;

namespace XO.Entityween
{

    internal struct TweenControl : IComponentData, IEnableableComponent
    {
        public float SecondsToPlay;
        public EaseType EaseType;
        public bool AutoKill;
    }

    internal struct TweenRange<T> : IComponentData where T : unmanaged
    {
        public T StartPoint;
        public T EndPoint;
    }

    internal struct TweenRuntime<T> : IComponentData where T : unmanaged
    {
        public bool Completed;
        public T CurrentValue;
    }

#if UNITY_EDITOR
    internal struct TweenDebugVisualize : IComponentData
    {
        public Entity TargetEntity;
        public TweenType TweenType;
        public TweenSpace Space;
    }
#endif

    internal struct TweenStartFromCurrent : IComponentData
    {
        public Entity TargetEntity;
        public TweenType TweenType;
        public TweenSpace Space;
    }

    internal struct TweenSequenceDriven : IComponentData
    {
    }

    internal struct SplineBlobRef<T> : IComponentData where T : unmanaged
    {
        public BlobAssetReference<SplineBlob<T>> Blob;
        public bool IsBend;
    }
}
