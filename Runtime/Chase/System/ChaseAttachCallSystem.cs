using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    [UpdateAfter(typeof(EntityweenSystemGroup))]
    public partial struct ChaseAttachCallSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var em = world.EntityManager;
            foreach (var (chaseAttachCall, entity) in SystemAPI.Query<RefRO<ChaseAttachCall>>().WithEntityAccess())
            {
                var call = chaseAttachCall.ValueRO;
                if (call.ChaseType is ChaseType.ChasePosition or ChaseType.ChasePositionAndLook
                    or ChaseType.ChasePositionAndRotation)
                {
                    entity.ChasePosition(call.Target)
                        .TStep(call.TStepMinMax, call.MaxStepTime)
                        .SetOverride(call.IsOverride)
                        .Play(world, ecb);
                }
                switch (call.ChaseType)
                {
                    case ChaseType.ChaseRotation or ChaseType.ChasePositionAndRotation:
                        entity.ChaseRotation(call.Target)
                            .TStep(call.TStepMinMax, call.MaxStepTime)
                            .SetOverride(call.IsOverride)
                            .Play(world, ecb);
                        break;
                    case ChaseType.Look or ChaseType.ChasePositionAndLook:
                        entity.Look(call.Target)
                            .TStep(call.TStepMinMax, call.MaxStepTime)
                            .SetOverride(call.IsOverride)
                            .Play(world, ecb);
                        break;
                }

                ecb.RemoveComponent<ChaseAttachCall>(entity);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}