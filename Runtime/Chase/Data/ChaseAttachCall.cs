using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween
{
    public readonly struct ChaseAttachCall : IComponentData
    {
        public readonly Entity Target;
        public readonly ChaseType ChaseType;
        public readonly float2 TStepMinMax;
        public readonly float MaxStepTime;
        public readonly bool IsOverride;
        public readonly short UpdateOrder;

        public ChaseAttachCall(Entity target, ChaseType chaseType, float2 tStepMinMax, float maxStepTime,
            bool isOverride, short updateOrder)
        {
            this.Target = target;
            this.ChaseType = chaseType;
            this.TStepMinMax = tStepMinMax;
            this.MaxStepTime = maxStepTime;
            this.IsOverride = isOverride;
            this.UpdateOrder = updateOrder;
        }
    }
}