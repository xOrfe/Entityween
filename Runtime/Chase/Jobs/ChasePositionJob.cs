using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    [BurstCompile]
    public partial struct ChasePositionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public NativeArray<Chase<float3>> Chases;
        public NativeArray<ChaseRuntimeData<float3>> ChasesRuntimeData;

        private void Execute(in Entity entity, in ChasePositionTag chaseTag, in LocalToWorld ltw,
            ref LocalTransform localTransform)
        {
            var chase = Chases[chaseTag.Index];
            if (!LocalToWorldLookup.HasComponent(chaseTag.Target))
                return;
            var targetLtw = LocalToWorldLookup[chaseTag.Target];

            var diff = targetLtw.Position - ltw.Position;
            var dist = math.length(diff);

            if (chase.IsOverride)
            {
                SetWorldPosition(entity, targetLtw.Position, ref localTransform);
                return;
            }

            var chaseRuntimeData = ChasesRuntimeData[chaseTag.Index];
            var time = chaseRuntimeData.Time;

            if (dist > 0.001f)
            {
                float3 newWorldPos = float3.zero;
                var t = 0.0f;
                Ease.Sample(chase.TStepMinMax.x, chase.TStepMinMax.y, time, EaseType.Linear, ref t);
                Ease.Sample(ltw.Position, targetLtw.Position, t, EaseType.Linear, ref newWorldPos);
                SetWorldPosition(entity, newWorldPos, ref localTransform);
                time += (DeltaTime / chase.MaxStepTime) * 1;
            }
            else
            {
                time = 0;
            }

            ChasesRuntimeData[chaseTag.Index] = new ChaseRuntimeData<float3>(time);
        }

        [BurstCompile]
        private void SetWorldPosition(in Entity entity, in float3 newWorldPos, ref LocalTransform localTransform)
        {
            if (ParentLookup.HasComponent(entity))
            {
                var parentEntity = ParentLookup[entity].Value;
                if (LocalToWorldLookup.HasComponent(parentEntity))
                {
                    var parentLtw = LocalToWorldLookup[parentEntity];
                    var rel = newWorldPos - parentLtw.Position;
                    var localPos = math.mul(math.inverse(parentLtw.Rotation), rel);
                    localTransform.Position = localPos;
                }
                else
                {
                    localTransform.Position = newWorldPos;
                }
            }
            else
            {
                localTransform.Position = newWorldPos;
            }
        }
    }
}