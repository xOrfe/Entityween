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

            foreach (var (value, current, entity) in SystemAPI.Query<RefRW<TweenValue<float3>>, RefRO<TweenStartFromCurrent>>().WithNone<TweenSequenceDriven>().WithEntityAccess())
            {
                if (!TryGetFloat3(current.ValueRO, localTransformLookup, localToWorldLookup, out var start)) continue;

                value.ValueRW.StartPoint = start;
                value.ValueRW.CurrentValue = start;
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            foreach (var (value, current, entity) in SystemAPI.Query<RefRW<TweenValue<quaternion>>, RefRO<TweenStartFromCurrent>>().WithNone<TweenSequenceDriven>().WithEntityAccess())
            {
                if (!TryGetQuaternion(current.ValueRO, localTransformLookup, localToWorldLookup, out var start)) continue;

                value.ValueRW.StartPoint = start;
                value.ValueRW.CurrentValue = start;
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            foreach (var (value, current, entity) in SystemAPI.Query<RefRW<TweenValue<float>>, RefRO<TweenStartFromCurrent>>().WithNone<TweenSequenceDriven>().WithEntityAccess())
            {
                if (current.ValueRO.TweenType != TweenType.ScaleToUniform ||
                    !localTransformLookup.TryGetComponent(current.ValueRO.TargetEntity, out var transform))
                    continue;

                value.ValueRW.StartPoint = transform.Scale;
                value.ValueRW.CurrentValue = transform.Scale;
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }
        }

        private static bool TryGetFloat3(TweenStartFromCurrent current,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
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

        private static bool TryGetQuaternion(TweenStartFromCurrent current,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
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
