using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(EntityweenSequenceGroup))]
    [UpdateBefore(typeof(TweenStartFromCurrentSystem))]
    internal partial class TweenTransformStartFromCurrentSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TweenStartFromCurrent>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (rangeRef, runtimeRef, startFromCurrent, transformTargetRef, entity) in SystemAPI.Query<RefRW<TweenRange<float3>>, RefRW<TweenRuntime<float3>>, RefRO<TweenStartFromCurrent>, RefRO<TweenTransformTarget>>().WithEntityAccess())
            {
                var targetVal = transformTargetRef.ValueRO;
                if (TryReadFloat3(entity, targetVal.Binding, targetVal.Space, out var start))
                {
                    rangeRef.ValueRW.StartPoint = start;
                    runtimeRef.ValueRW.CurrentValue = start;
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            foreach (var (rangeRef, runtimeRef, startFromCurrent, transformTargetRef, entity) in SystemAPI.Query<RefRW<TweenRange<quaternion>>, RefRW<TweenRuntime<quaternion>>, RefRO<TweenStartFromCurrent>, RefRO<TweenTransformTarget>>().WithEntityAccess())
            {
                if (TryReadQuaternion(entity, transformTargetRef.ValueRO.Space, out var start))
                {
                    rangeRef.ValueRW.StartPoint = start;
                    runtimeRef.ValueRW.CurrentValue = start;
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            foreach (var (rangeRef, runtimeRef, startFromCurrent, transformTargetRef, entity) in SystemAPI.Query<RefRW<TweenRange<float>>, RefRW<TweenRuntime<float>>, RefRO<TweenStartFromCurrent>, RefRO<TweenTransformTarget>>().WithEntityAccess())
            {
                if (TryReadFloat(entity, out var start))
                {
                    rangeRef.ValueRW.StartPoint = start;
                    runtimeRef.ValueRW.CurrentValue = start;
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private bool TryReadFloat3(Entity entity, TweenTransformBinding binding, TweenSpace space, out float3 value)
        {
            value = default;
            if (!TryGetTransform(entity, out var transform))
                return false;

            value = binding is TweenTransformBinding.Scale or TweenTransformBinding.ScaleUniform
                ? transform.localScale
                : space == TweenSpace.World
                    ? transform.position
                    : transform.localPosition;
            return true;
        }

        private bool TryReadQuaternion(Entity entity, TweenSpace space, out quaternion value)
        {
            value = quaternion.identity;
            if (!TryGetTransform(entity, out var transform))
                return false;

            value = space == TweenSpace.World
                ? transform.rotation
                : transform.localRotation;
            return true;
        }

        private bool TryReadFloat(Entity entity, out float value)
        {
            value = 1f;
            if (!TryGetTransform(entity, out var transform))
                return false;

            value = transform.localScale.x;
            return true;
        }

        private bool TryGetTransform(Entity entity, out UnityEngine.Transform transform)
        {
            transform = null;
            if (!EntityManager.HasComponent<TweenTransformReference>(entity))
                return false;

            transform = EntityManager.GetComponentObject<TweenTransformReference>(entity).Transform;
            return transform != null;
        }
    }
}
