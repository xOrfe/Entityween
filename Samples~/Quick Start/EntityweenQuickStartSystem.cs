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
    /// Add this tag to any entity with LocalTransform to play a single tween.
    /// The system removes the tag after scheduling, so the tween starts only once.
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

            foreach (var (localTransform, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<EntityweenQuickStartTag>()
                         .WithEntityAccess())
            {
                float3 startPosition = localTransform.ValueRO.Position;
                float3 endPosition = startPosition + new float3(0f, 2f, 0f);

                entity
                    .MoveToWorld(endPosition, duration: 1f)
                    .From(startPosition)
                    .Ease(EaseType.InOutSine)
                    .Play(ecb);

                ecb.RemoveComponent<EntityweenQuickStartTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
