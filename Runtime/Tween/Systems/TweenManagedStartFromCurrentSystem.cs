using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(EntityweenSequenceGroup))]
    [UpdateBefore(typeof(TweenStartFromCurrentSystem))]
    internal partial class TweenManagedStartFromCurrentSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TweenStartFromCurrent>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // 1. Members StartFromCurrent
            // TweenValue<float>
            foreach (var (valueRef, startFromCurrent, entity) in SystemAPI.Query<RefRW<TweenValue<float>>, RefRO<TweenStartFromCurrent>>().WithAll<TweenMemberHook<float>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetMember<float>(World, entity, out var memberHook))
                {
                    if (TweenMemberBinder.TryCreateGetter<float>(memberHook.Target, memberHook.MemberName, out var getter, out var error))
                    {
                        float start = getter(memberHook.Target);
                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                    else
                    {
                        Debug.LogError($"FromCurrent failed for member '{memberHook.MemberName}' on '{memberHook.Target?.GetType().Name}': {error}");
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            // TweenValue<float2>
            foreach (var (valueRef, startFromCurrent, entity) in SystemAPI.Query<RefRW<TweenValue<float2>>, RefRO<TweenStartFromCurrent>>().WithAll<TweenMemberHook<float2>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetMember<float2>(World, entity, out var memberHook))
                {
                    if (TweenMemberBinder.TryCreateGetter<float2>(memberHook.Target, memberHook.MemberName, out var getter, out var error))
                    {
                        float2 start = getter(memberHook.Target);
                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                    else
                    {
                        Debug.LogError($"FromCurrent failed for member '{memberHook.MemberName}' on '{memberHook.Target?.GetType().Name}': {error}");
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            // TweenValue<float3>
            foreach (var (valueRef, startFromCurrent, entity) in SystemAPI.Query<RefRW<TweenValue<float3>>, RefRO<TweenStartFromCurrent>>().WithAll<TweenMemberHook<float3>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetMember<float3>(World, entity, out var memberHook))
                {
                    if (TweenMemberBinder.TryCreateGetter<float3>(memberHook.Target, memberHook.MemberName, out var getter, out var error))
                    {
                        float3 start = getter(memberHook.Target);
                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                    else
                    {
                        Debug.LogError($"FromCurrent failed for member '{memberHook.MemberName}' on '{memberHook.Target?.GetType().Name}': {error}");
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            // TweenValue<quaternion>
            foreach (var (valueRef, startFromCurrent, entity) in SystemAPI.Query<RefRW<TweenValue<quaternion>>, RefRO<TweenStartFromCurrent>>().WithAll<TweenMemberHook<quaternion>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetMember<quaternion>(World, entity, out var memberHook))
                {
                    if (TweenMemberBinder.TryCreateGetter<quaternion>(memberHook.Target, memberHook.MemberName, out var getter, out var error))
                    {
                        quaternion start = getter(memberHook.Target);
                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                    else
                    {
                        Debug.LogError($"FromCurrent failed for member '{memberHook.MemberName}' on '{memberHook.Target?.GetType().Name}': {error}");
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            // 2. Transform StartFromCurrent
            // Transform position (float3)
            foreach (var (valueRef, startFromCurrent, transformTargetRef, entity) in SystemAPI.Query<RefRW<TweenValue<float3>>, RefRO<TweenStartFromCurrent>, RefRO<TweenGameObjectTarget>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetGameObject(World, entity, out var target))
                {
                    if (target != null)
                    {
                        float3 start = default;
                        var targetVal = transformTargetRef.ValueRO;
                        if (targetVal.Binding == TweenGameObjectBinding.Position)
                        {
                            start = targetVal.Space == TweenSpace.World
                                ? target.position
                                : target.localPosition;
                        }
                        else if (targetVal.Binding == TweenGameObjectBinding.Scale)
                        {
                            start = target.localScale;
                        }

                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            // Transform rotation (quaternion)
            foreach (var (valueRef, startFromCurrent, transformTargetRef, entity) in SystemAPI.Query<RefRW<TweenValue<quaternion>>, RefRO<TweenStartFromCurrent>, RefRO<TweenGameObjectTarget>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetGameObject(World, entity, out var target))
                {
                    if (target != null)
                    {
                        quaternion start = default;
                        var targetVal = transformTargetRef.ValueRO;
                        if (targetVal.Binding == TweenGameObjectBinding.Rotation)
                        {
                            start = targetVal.Space == TweenSpace.World
                                ? target.rotation
                                : target.localRotation;
                        }

                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            // Transform scale (float)
            foreach (var (valueRef, startFromCurrent, transformTargetRef, entity) in SystemAPI.Query<RefRW<TweenValue<float>>, RefRO<TweenStartFromCurrent>, RefRO<TweenGameObjectTarget>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetGameObject(World, entity, out var target))
                {
                    if (target != null)
                    {
                        float start = default;
                        var targetVal = transformTargetRef.ValueRO;
                        if (targetVal.Binding == TweenGameObjectBinding.Scale)
                        {
                            start = target.localScale.x;
                        }

                        valueRef.ValueRW.StartPoint = start;
                        valueRef.ValueRW.CurrentValue = start;
                    }
                }
                ecb.RemoveComponent<TweenStartFromCurrent>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
