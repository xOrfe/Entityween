using Unity.Burst;
using Unity.Entities;

namespace XO.Entityween
{
    public interface ISequence : IComponentData
    {
        
    }
    public struct Sequence : ISequence
    {
        public bool IsPlaying { get; set; }
        public bool IsLoop { get; set; }
        public LoopType LoopTypeType { get; set; }
        public uint LoopCount { get; set; }
        public uint CurrentLoopIndex { get; set; }
        
        public float TimeScale { get; set; }
        public float SequenceTime { get; set; }

        public FunctionPointer<TweenCallback> OnStart;
        public FunctionPointer<TweenCallback> OnUpdate;
        public FunctionPointer<TweenCallback> OnComplete;
        public FunctionPointer<TweenCallback> OnLoop;

        public readonly float Position()
        {
            return 0;
        }

        public readonly float Duration()
        {
            return 0;
        }
    }

    public struct SequenceTweenElement : IBufferElementData
    {
        public Entity Entity;
        public bool IsCompleted;
    }
}