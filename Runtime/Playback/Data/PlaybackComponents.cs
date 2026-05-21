using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{
    internal struct PlaybackProgress : IComponentData
    {
        public PlaybackTimeType TimeType;
        public float NormalizedTime;
    }

    internal struct PlaybackLoop : IComponentData
    {
        /// <summary>
        /// Defines how many times the loop will execute. Set to 0 for infinite looping.
        /// </summary>
        [Tooltip("Defines how many times the loop will execute. Set to 0 for infinite looping.")]
        public uint LoopCount;
        public LoopType LoopType;
        
        public int LoopIndex;
    }
}
