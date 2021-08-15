using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

namespace Entityween.Scripts
{
    public struct ChunkFloat3ChangeOrder : ISharedComponentData, IEquatable<ChunkFloat3ChangeOrder>
    {
        public int Value;

        public float Time;

        public float StartTime;

        public float3 ChangeFactor;
        
        public float3 ChangeStart;
        
        public FunctionPointer<EaseJobs.EaseTypeDelegate> MyEaseMethod;
        
        public FunctionPointer<ActionJobs.ActionTypeDelegate> MyActionMethod;

        public ChunkFloat3ChangeOrder()
        {
            
        }
        
        public bool Equals(ChunkFloat3ChangeOrder other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}
    





    