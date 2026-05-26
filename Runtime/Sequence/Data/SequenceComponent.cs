using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    public struct Sequence : IComponentData
    {
        public PlaybackState State;
        public float TimeScale;
        public float Time;
        public float Duration;
        public int Direction;

        public readonly float Position() => Time;
        public readonly float TotalDuration() => Duration;

        public static SequenceBuilder Create() => SequenceBuilder.Create();
        public static SequenceBuilder Create(EntityManager entityManager) => SequenceBuilder.Create(entityManager);
    }

    public enum TimelineActionKind : byte
    {
        Tween,
        Chase,
        Wait,
        Callback
    }
    
    //
    //TODO : Can we do same thing with one TimeShift per sequence? Do we need TimeShift per element in any edge case?
    //
    [InternalBufferCapacity(8)]
    internal struct SequenceElement : IBufferElementData
    {
        public TimelineActionKind Kind;
        public Entity ActionEntity;
        public float StartTime;
        public float Duration;
        public float TimeShift;
        public FixedString64Bytes CallbackId;
        public bool Started;
        public bool Completed;
    }

    internal struct SequenceDynamicTime : IComponentData
    {
        public float Consumed;
    }

    public struct SequenceCallbackEvent : IComponentData
    {
        public Entity SequenceEntity;
        public FixedString64Bytes CallbackId;
    }

    internal struct TimelineDriven : IComponentData
    {
        public Entity SequenceEntity;
    }

    internal struct SequenceActionOwner : IComponentData
    {
        public bool DestroyWithSequence;
    }

    internal struct SequenceTweenBinding : IComponentData
    {
        public Entity TargetEntity;
        public TweenType TweenType;
        public TweenSpace Space;
        public bool UseChase;
        public ChaseMode ChaseMode;
        public float ChaseSmoothTime;
        public float ChaseMaxSpeed;
        public bool KillOnChase;
        public bool StartFromCurrent;
    }
}
