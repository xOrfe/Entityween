using Unity.Entities;

namespace XO.Entityween
{
    public readonly partial struct SequenceAspect : IAspect
    {
        public readonly Entity Entity;

        public readonly RefRW<Sequence> Sequence;

        public float Position => Sequence.ValueRO.Position();
        public float Duration => Sequence.ValueRO.Duration();

        public float TotalTime()
        {
            return 0;
        }

        public SequenceAspect Join()
        {
            //blueprint.IsJoin = true;

            return this;
        }

        public SequenceAspect Append()
        {
            return this;
        }

        public SequenceAspect Prepend()
        {
            return this;
        }

        public SequenceAspect Insert()
        {
            return this;
        }

        public SequenceAspect Callback(TweenCallback callback)
        {
            return this;
        }

        public SequenceAspect WaitForSeconds(float waitTime)
        {
            return this;
        }

        public SequenceAspect Play()
        {
            return this;
        }

        public SequenceAspect Pause()
        {
            return this;
        }

        public SequenceAspect Kill()
        {
            return this;
        }

        public SequenceAspect Complete()
        {
            return this;
        }

        public SequenceAspect Restart()
        {
            return this;
        }
    }
}