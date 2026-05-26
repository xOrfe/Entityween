using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    /// <summary>
    /// Add this tag to an entity with LocalTransform to play a short timeline.
    /// A sequence can mix tweens, waits, callbacks, and other timeline actions.
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

            foreach (var (localTransform, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<EntityweenSequenceSampleTag>()
                         .WithEntityAccess())
            {
                float3 start = localTransform.ValueRO.Position;
                float3 up = start + new float3(0f, 2f, 0f);
                float3 right = start + new float3(2f, 2f, 0f);

                Sequence.Create()
                    .Append(entity.MoveToWorld(up, 0.5f).From(start).Ease(EaseType.OutCubic))
                    .AppendWait(0.25f)
                    .Append(entity.MoveToWorld(right, 0.5f).From(up).Ease(EaseType.InOutSine))
                    .AppendCallback("SequenceFinished")
                    .Play(ecb);

                ecb.RemoveComponent<EntityweenSequenceSampleTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
