using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace XO.Entityween.Jobs
{
    public partial struct MoveToWorldJob : IJobEntity
    {
        [ReadOnly] public NativeArray<TweenRuntimeData<float3>> TweensRuntimeData;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        private void Execute(in MoveToWorld moveToWorld,ref LocalTransform localTransform,in Parent parent)
        {
            var pos = TweensRuntimeData[moveToWorld.Index].CurrentValue;
            var parentLocalToWorld = LocalToWorldLookup[parent.Value].Value;
            var worldToLocal = math.inverse(parentLocalToWorld);
            pos = math.transform(worldToLocal, pos);
            localTransform.Position = pos;
        }
    }
}