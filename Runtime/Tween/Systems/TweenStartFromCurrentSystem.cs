using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace XO.Entityween
{
    [BurstCompile]
    [UpdateInGroup(typeof(EntityweenSequenceGroup))]
    internal partial struct TweenStartFromCurrentSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TweenStartFromCurrent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (range, runtime, current, entity) in SystemAPI.Query<RefRW<TweenRange<float3>>, RefRW<TweenRuntime<float3>>, RefRO<TweenStartFromCurrent>>().WithNone<TweenSequenceDriven>().WithEntityAccess())
            {
                var currentValue = current.ValueRO;
                if (!TryGetFloat3(in currentValue, ref localTransformLookup, ref localToWorldLookup, out var start)) continue;

                range.ValueRW.StartPoint = start;
                runtime.ValueRW.CurrentValue = start;
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            foreach (var (range, runtime, current, entity) in SystemAPI.Query<RefRW<TweenRange<quaternion>>, RefRW<TweenRuntime<quaternion>>, RefRO<TweenStartFromCurrent>>().WithNone<TweenSequenceDriven>().WithEntityAccess())
            {
                var currentValue = current.ValueRO;
                if (!TryGetQuaternion(in currentValue, ref localTransformLookup, ref localToWorldLookup, out var start)) continue;

                range.ValueRW.StartPoint = start;
                runtime.ValueRW.CurrentValue = start;
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            foreach (var (range, runtime, current, entity) in SystemAPI.Query<RefRW<TweenRange<float>>, RefRW<TweenRuntime<float>>, RefRO<TweenStartFromCurrent>>().WithNone<TweenSequenceDriven>().WithEntityAccess())
            {
                if (current.ValueRO.TweenType != TweenType.ScaleToUniform ||
                    !localTransformLookup.TryGetComponent(current.ValueRO.TargetEntity, out var transform))
                    continue;

                range.ValueRW.StartPoint = transform.Scale;
                runtime.ValueRW.CurrentValue = transform.Scale;
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        private static bool TryGetFloat3(in TweenStartFromCurrent current,
            ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<LocalToWorld> localToWorldLookup,
            out float3 value)
        {
            value = default;
            if (!localTransformLookup.TryGetComponent(current.TargetEntity, out var transform)) return false;

            if (current.TweenType == TweenType.MoveTo)
            {
                if (current.Space == TweenSpace.World && localToWorldLookup.TryGetComponent(current.TargetEntity, out var localToWorld))
                    value = localToWorld.Position;
                else
                    value = transform.Position;
                return true;
            }

            if (current.TweenType == TweenType.ScaleTo)
            {
                value = new float3(transform.Scale);
                return true;
            }

            return false;
        }

        [BurstCompile(DisableDirectCall = true)]
        private static bool TryGetQuaternion(in TweenStartFromCurrent current,
            ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<LocalToWorld> localToWorldLookup,
            out quaternion value)
        {
            value = default;
            if (!localTransformLookup.TryGetComponent(current.TargetEntity, out var transform)) return false;

            if (current.TweenType != TweenType.RotateTo) return false;

            if (current.Space == TweenSpace.World && localToWorldLookup.TryGetComponent(current.TargetEntity, out var localToWorld))
                value = localToWorld.Rotation;
            else
                value = transform.Rotation;

            return true;
        }
    }
}
