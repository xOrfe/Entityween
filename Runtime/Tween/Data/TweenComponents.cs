using Unity.Entities;
using XO.Curve;

namespace XO.Entityween
{

    internal struct TweenControl : IComponentData, IEnableableComponent
    {
        public float ElapsedTime;
        public float SecondsToPlay;
        public EaseType EaseType;
        public bool AutoKill;
        public bool Completed;
    }


    internal struct TweenValue<T> : IComponentData where T : unmanaged
    {
        public T StartPoint;
        public T EndPoint;
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


}
