using Unity.Entities;

namespace XO.Entityween
{
    internal struct PlaybackProgress : IComponentData
    {
        public PlaybackTimeType TimeType;
        public float NormalizedTime;

        public LoopType LoopType;
        public uint LoopCount;
        public int LoopIndex;
        public LoopEaseMode LoopEaseMode;

        public int Direction;
    }
}
