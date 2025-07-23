using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace XO.Entityween.Jobs
{
    public partial struct RotateToJob : IJobEntity
    {
        [ReadOnly] public NativeArray<TweenRuntimeData<quaternion>> TweensRuntimeData;
        private void Execute(in RotateTo rotateTo,ref LocalTransform localTransform)
        {
            localTransform.Rotation = TweensRuntimeData[rotateTo.Index].CurrentValue;
        }
    }
}