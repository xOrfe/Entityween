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
    public partial struct ChaseJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<Chase> Chases;
        public NativeArray<ChaseRuntimeData> ChasesRuntimeData;

        private void Execute(in Entity entity, in ChaseTag chaseTag, in LocalToWorld ltw,
            ref LocalTransform localTransform)
        {
            var chase = Chases[chaseTag.Index];
            if (!LocalToWorldLookup.HasComponent(chase.TargetEntity))
                return;
            var targetLtw = LocalToWorldLookup[chase.TargetEntity];

            var diff = targetLtw.Position - ltw.Position;
            var dist = math.length(diff);
            if (chase.IsOverride)
            {
                if (chase.ChasePosition) SetWorldPosition(entity, targetLtw.Position, ref localTransform);
                if (chase.ChaseRotation) SetWorldRotation(entity, targetLtw.Rotation, ref localTransform);
                else if (chase.LookRotation)
                    SetWorldRotation(entity,
                        dist > 1e-6f ? quaternion.LookRotationSafe(diff / dist, math.up()) : ltw.Rotation,
                        ref localTransform);
                return;
            }

            var chaseRuntimeData = ChasesRuntimeData[chaseTag.Index];
            var positionalT = chaseRuntimeData.positionalT;
            var angularT = chaseRuntimeData.angularT;
            if (chase.ChasePosition)
            {
                if (dist > 0.001f)
                {
                    float3 newWorldPos = float3.zero;
                    var t = 0.0f;
                    Ease.Sample(chase.TStepMinMax.x, chase.TStepMinMax.y, positionalT, EaseType.Linear, ref t);
                    Ease.Sample(ltw.Position, targetLtw.Position, t, EaseType.Linear, ref newWorldPos);
                    SetWorldPosition(entity, newWorldPos, ref localTransform);
                    positionalT += (deltaTime / chase.MaxStepTime) * 1;
                }
                else
                {
                    positionalT = 0;
                }
            }

            if (chase.ChaseRotation || chase.LookRotation)
            {
                quaternion desiredRot = ltw.Rotation;
                bool setRotation = true;

                if (chase.ChaseRotation)
                    desiredRot = targetLtw.Rotation;
                else if (chase.LookRotation)
                    desiredRot = dist > 1e-6f ? quaternion.LookRotationSafe(diff / dist, math.up()) : desiredRot;
                else
                    setRotation = false;

                var angularDot = math.dot(targetLtw.Rotation, ltw.Rotation);
                angularDot = math.abs(angularDot);
                if (setRotation && angularDot < 0.99999f)
                {
                    quaternion newWorldRot = quaternion.identity;

                    var t = 0.0f;
                    Ease.Sample(chase.TStepMinMax.x, chase.TStepMinMax.y, angularT, EaseType.Linear, ref t);
                    Ease.Sample(ltw.Rotation, desiredRot, t, EaseType.Linear, ref newWorldRot);
                    SetWorldRotation(entity, newWorldRot, ref localTransform);
                    angularT += (deltaTime / chase.MaxStepTime) * 1;
                }
                else
                {
                    angularT = 0;
                }
            }

            ChasesRuntimeData[chaseTag.Index] = new ChaseRuntimeData(positionalT, angularT);
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

        [BurstCompile]
        private void SetWorldRotation(in Entity entity, in quaternion newWorldRot, ref LocalTransform localTransform)
        {
            if (ParentLookup.HasComponent(entity))
            {
                var parentEntity = ParentLookup[entity].Value;
                if (LocalToWorldLookup.HasComponent(parentEntity))
                {
                    var parentLtw = LocalToWorldLookup[parentEntity];
                    localTransform.Rotation = math.mul(math.inverse(parentLtw.Rotation), newWorldRot);
                }
                else
                {
                    localTransform.Rotation = newWorldRot;
                }
            }
            else
            {
                localTransform.Rotation = newWorldRot;
            }
        }
    }
}