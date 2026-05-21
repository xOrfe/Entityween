using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    /// <summary>
    /// Add this tag to an entity with LocalTransform to start the sample tween once.
    /// </summary>
    public struct EntityweenQuickStartTag : IComponentData
    {
    }

    [BurstCompile]
    public partial struct EntityweenQuickStartSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityweenQuickStartTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<EntityweenQuickStartTag>()
                         .WithEntityAccess())
            {
                entity
                    .MoveToWorld(transform.ValueRO.Position + new float3(0f, 2f, 0f), 1f)
                    .Ease(EaseType.InOutSine)
                    .Play(ecb);

                ecb.RemoveComponent<EntityweenQuickStartTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
