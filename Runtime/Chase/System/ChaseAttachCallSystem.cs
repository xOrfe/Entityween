using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
                entity.Chase(call.target, call.chasePosition, call.chaseRotation,call.lookRotation)
                    .Settings(call.isOverride)
                    .TStep(call.tStepMinMax, call.maxStepTime)
                    .Play(world, ecb);
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