using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenChaseGroup))]
    internal partial class TweenSyncSystem : SystemBase
    {
        private EntityQuery _floatMemberHookQuery;
        private EntityQuery _floatCallbackHookQuery;
        private EntityQuery _floatGameObjectTargetQuery;

        private EntityQuery _float2MemberHookQuery;
        private EntityQuery _float2CallbackHookQuery;

        private EntityQuery _float3MemberHookQuery;
        private EntityQuery _float3CallbackHookQuery;
        private EntityQuery _float3GameObjectTargetQuery;

        private EntityQuery _quaternionMemberHookQuery;
        private EntityQuery _quaternionCallbackHookQuery;
        private EntityQuery _quaternionGameObjectTargetQuery;

        protected override void OnCreate()
        {
            _floatMemberHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float>, TweenMemberHook<float>>().Build();
            _floatCallbackHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float>, TweenCallbackHook<float>>().Build();
            _floatGameObjectTargetQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float>, TweenGameObjectTarget>().Build();

            _float2MemberHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float2>, TweenMemberHook<float2>>().Build();
            _float2CallbackHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float2>, TweenCallbackHook<float2>>().Build();

            _float3MemberHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float3>, TweenMemberHook<float3>>().Build();
            _float3CallbackHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float3>, TweenCallbackHook<float3>>().Build();
            _float3GameObjectTargetQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<float3>, TweenGameObjectTarget>().Build();

            _quaternionMemberHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<quaternion>, TweenMemberHook<quaternion>>().Build();
            _quaternionCallbackHookQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<quaternion>, TweenCallbackHook<quaternion>>().Build();
            _quaternionGameObjectTargetQuery = SystemAPI.QueryBuilder().WithAll<TweenValue<quaternion>, TweenGameObjectTarget>().Build();
        }

        protected override void OnUpdate()
        {
            bool hasFloat = !_floatMemberHookQuery.IsEmptyIgnoreFilter || !_floatCallbackHookQuery.IsEmptyIgnoreFilter || !_floatGameObjectTargetQuery.IsEmptyIgnoreFilter;
            bool hasFloat2 = !_float2MemberHookQuery.IsEmptyIgnoreFilter || !_float2CallbackHookQuery.IsEmptyIgnoreFilter;
            bool hasFloat3 = !_float3MemberHookQuery.IsEmptyIgnoreFilter || !_float3CallbackHookQuery.IsEmptyIgnoreFilter || !_float3GameObjectTargetQuery.IsEmptyIgnoreFilter;
            bool hasQuat = !_quaternionMemberHookQuery.IsEmptyIgnoreFilter || !_quaternionCallbackHookQuery.IsEmptyIgnoreFilter || !_quaternionGameObjectTargetQuery.IsEmptyIgnoreFilter;

            if (!hasFloat && !hasFloat2 && !hasFloat3 && !hasQuat)
            {
                return;
            }

            Dependency.Complete();

            if (hasFloat) SyncFloat();
            if (hasFloat2) SyncFloat2();
            if (hasFloat3) SyncFloat3();
            if (hasQuat) SyncQuaternion();
        }

        private static bool IsDestroyed(object obj)
        {
            if (obj == null) return true;
            if (obj is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }
            return false;
        }

        private void SyncFloat()
        {
            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<float>>>().WithAll<TweenMemberHook<float>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetMember<float>(World, entity, out var hook)) continue;
                if (IsDestroyed(hook.Target)) continue;
                try
                {
                    hook.Setter(hook.Target, valueRef.ValueRO.CurrentValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenMemberHook<float> for '{hook.MemberName}': {ex.Message}");
                }
            }

            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<float>>>().WithAll<TweenCallbackHook<float>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetCallback<float>(World, entity, out var hook)) continue;
                try
                {
                    if (hook.StateCallback != null)
                    {
                        hook.StateCallback(hook.State, valueRef.ValueRO.CurrentValue);
                    }
                    else if (hook.Callback != null)
                    {
                        hook.Callback(valueRef.ValueRO.CurrentValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenCallbackHook<float>: {ex.Message}");
                }
            }

            foreach (var (valueRef, targetRef, entity) in SystemAPI.Query<RefRO<TweenValue<float>>, RefRO<TweenGameObjectTarget>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetGameObject(World, entity, out var target)) continue;
                if (target == null) continue;
                if (targetRef.ValueRO.Binding == TweenGameObjectBinding.Scale)
                {
                    target.localScale = Vector3.one * valueRef.ValueRO.CurrentValue;
                }
            }
        }

        private void SyncFloat2()
        {
            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<float2>>>().WithAll<TweenMemberHook<float2>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetMember<float2>(World, entity, out var hook)) continue;
                if (IsDestroyed(hook.Target)) continue;
                try
                {
                    hook.Setter(hook.Target, valueRef.ValueRO.CurrentValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenMemberHook<float2> for '{hook.MemberName}': {ex.Message}");
                }
            }

            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<float2>>>().WithAll<TweenCallbackHook<float2>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetCallback<float2>(World, entity, out var hook)) continue;
                try
                {
                    if (hook.StateCallback != null)
                    {
                        hook.StateCallback(hook.State, valueRef.ValueRO.CurrentValue);
                    }
                    else if (hook.Callback != null)
                    {
                        hook.Callback(valueRef.ValueRO.CurrentValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenCallbackHook<float2>: {ex.Message}");
                }
            }
        }

        private void SyncFloat3()
        {
            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<float3>>>().WithAll<TweenMemberHook<float3>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetMember<float3>(World, entity, out var hook)) continue;
                if (IsDestroyed(hook.Target)) continue;
                try
                {
                    hook.Setter(hook.Target, valueRef.ValueRO.CurrentValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenMemberHook<float3> for '{hook.MemberName}': {ex.Message}");
                }
            }

            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<float3>>>().WithAll<TweenCallbackHook<float3>>().WithEntityAccess())
            {
                if (TweenManagedRegistry.TryGetCallback<float3>(World, entity, out var hook))
                {
                    try
                    {
                        if (hook.StateCallback != null)
                        {
                            hook.StateCallback(hook.State, valueRef.ValueRO.CurrentValue);
                        }
                        else if (hook.Callback != null)
                        {
                            hook.Callback(valueRef.ValueRO.CurrentValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in TweenCallbackHook<float3>: {ex.Message}");
                    }
                }
            }

            foreach (var (valueRef, targetRef, entity) in SystemAPI.Query<RefRO<TweenValue<float3>>, RefRO<TweenGameObjectTarget>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetGameObject(World, entity, out var target)) continue;
                if (target == null) continue;
                var targetVal = targetRef.ValueRO;
                switch (targetVal.Binding)
                {
                    case TweenGameObjectBinding.Position when targetVal.Space == TweenSpace.World:
                        target.position = valueRef.ValueRO.CurrentValue;
                        break;
                    case TweenGameObjectBinding.Position:
                        target.localPosition = valueRef.ValueRO.CurrentValue;
                        break;
                    case TweenGameObjectBinding.Scale:
                        target.localScale = valueRef.ValueRO.CurrentValue;
                        break;
                }
            }
        }

        private void SyncQuaternion()
        {
            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<quaternion>>>().WithAll<TweenMemberHook<quaternion>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetMember<quaternion>(World, entity, out var hook)) continue;
                if (IsDestroyed(hook.Target)) continue;
                try
                {
                    hook.Setter(hook.Target, valueRef.ValueRO.CurrentValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenMemberHook<quaternion> for '{hook.MemberName}': {ex.Message}");
                }
            }

            foreach (var (valueRef, entity) in SystemAPI.Query<RefRO<TweenValue<quaternion>>>().WithAll<TweenCallbackHook<quaternion>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetCallback<quaternion>(World, entity, out var hook)) continue;
                try
                {
                    if (hook.StateCallback != null)
                    {
                        hook.StateCallback(hook.State, valueRef.ValueRO.CurrentValue);
                    }
                    else if (hook.Callback != null)
                    {
                        hook.Callback(valueRef.ValueRO.CurrentValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in TweenCallbackHook<quaternion>: {ex.Message}");
                }
            }

            foreach (var (valueRef, targetRef, entity) in SystemAPI.Query<RefRO<TweenValue<quaternion>>, RefRO<TweenGameObjectTarget>>().WithEntityAccess())
            {
                if (!TweenManagedRegistry.TryGetGameObject(World, entity, out var target)) continue;
                if (target == null) continue;
                var targetVal = targetRef.ValueRO;
                if (targetVal.Binding != TweenGameObjectBinding.Rotation) continue;
                if (targetVal.Space == TweenSpace.World)
                    target.rotation = valueRef.ValueRO.CurrentValue;
                else
                    target.localRotation = valueRef.ValueRO.CurrentValue;
            }
        }
    }
}
