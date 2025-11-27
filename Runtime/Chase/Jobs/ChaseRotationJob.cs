using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Curve;

namespace XO.Entityween
{
    [BurstCompile]
    public partial struct ChaseRotationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public NativeArray<Chase<quaternion>> Chases;
        public NativeArray<ChaseRuntimeData<quaternion>> ChasesRuntimeData;

        public void Execute(in Entity entity, in ChaseRotationTag chaseTag, in LocalToWorld ltw,
            ref LocalTransform localTransform)
        {
            var chase = Chases[chaseTag.Index];
            if (!LocalToWorldLookup.HasComponent(chase.TargetEntity))
                return;
            var targetLtw = LocalToWorldLookup[chase.TargetEntity];
            var diff = targetLtw.Position - ltw.Position;
            var dist = math.length(diff);

            quaternion desiredRot = targetLtw.Rotation;
            if (chase.IsOverride)
            {
                SetWorldRotation(entity, desiredRot, ref localTransform);
                return;
            }

            var chaseRuntimeData = ChasesRuntimeData[chaseTag.Index];
            var time = chaseRuntimeData.Time;

            var angularDot = math.dot(desiredRot, ltw.Rotation);
            angularDot = math.abs(angularDot);
            if (angularDot < 0.99999f)
            {
                quaternion newWorldRot = quaternion.identity;

                var t = 0.0f;
                Ease.Sample(chase.TStepMinMax.x, chase.TStepMinMax.y, time, EaseType.Linear, ref t);
                Ease.Sample(ltw.Rotation, desiredRot, t, EaseType.Linear, ref newWorldRot);
                SetWorldRotation(entity, newWorldRot, ref localTransform);
                time += (DeltaTime / chase.MaxStepTime) * 1;
            }
            else
            {
                time = 0;
            }

            ChasesRuntimeData[chaseTag.Index] = new ChaseRuntimeData<quaternion>(time);
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