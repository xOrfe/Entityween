using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace XO.Entityween.Jobs
{
    public partial struct MoveToJob : IJobEntity
    {
        [ReadOnly] public NativeArray<TweenRuntimeData<float3>> TweensRuntimeData;
        private void Execute(in MoveTo moveTo,ref LocalTransform localTransform)
        {
            localTransform.Position = TweensRuntimeData[moveTo.Index].CurrentValue;
        }
    }
}