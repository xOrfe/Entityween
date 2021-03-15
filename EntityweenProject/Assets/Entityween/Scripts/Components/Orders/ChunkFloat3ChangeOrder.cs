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

        //public string EaseTypeMethod;

        //public string OnFinishTriggerMethod;

        //public string OnFinishTriggerGameobject;

        public FunctionPointer<EaseJobs.EaseTypeDelegate> MyEaseMethod;
        
        public FunctionPointer<ActionJobs.ActionTypeDelegate> MyActionMethod;

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
    





    