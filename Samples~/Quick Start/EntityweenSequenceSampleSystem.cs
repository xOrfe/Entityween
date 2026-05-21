using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    /// <summary>
    /// Add this tag to an entity with LocalTransform to start the sample sequence once.
    /// </summary>
    public struct EntityweenSequenceSampleTag : IComponentData
    {
    }

    public partial struct EntityweenSequenceSampleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityweenSequenceSampleTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<EntityweenSequenceSampleTag>()
                         .WithEntityAccess())
            {
                var start = transform.ValueRO.Position;

                Sequence.Create()
                    .Append(entity.MoveToWorld(start + new float3(0f, 2f, 0f), 0.5f).Ease(EaseType.OutCubic))
                    .Append(entity.Wait(0.25f))
                    .Append(entity.MoveToWorld(start + new float3(2f, 2f, 0f), 0.5f).Ease(EaseType.InOutSine))
                    .AppendCallback("SequenceFinished")
                    .Play(ecb);

                ecb.RemoveComponent<EntityweenSequenceSampleTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
