using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween
{
    public readonly struct ChaseAttachCall : IComponentData
    {
        public readonly Entity target;
        public readonly float2 tStepMinMax;
        public readonly float maxStepTime;
        public readonly bool isOverride;
        public readonly bool chasePosition;
        public readonly bool chaseRotation;
        public readonly bool lookRotation;
        public readonly short updateOrder;

        public ChaseAttachCall(Entity target,float2 tStepMinMax,float maxStepTime, bool isOverride, bool chasePosition, bool chaseRotation, bool lookRotation, short updateOrder)
        {
            this.target = target;
            this.tStepMinMax = tStepMinMax;
            this.maxStepTime = maxStepTime;
            this.isOverride = isOverride;
            this.chasePosition = chasePosition;
            this.chaseRotation = chaseRotation;
            this.lookRotation = lookRotation;
            this.updateOrder = updateOrder;
        }
    }
}