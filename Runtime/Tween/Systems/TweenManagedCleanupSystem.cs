using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(EntityweenSystemGroup), OrderFirst = true)]
    internal partial class TweenManagedCleanupSystem : SystemBase
    {
        private EntityQuery _cleanupQuery;
        private SystemThrottler _throttler;

        protected override void OnCreate()
        {
            _cleanupQuery = GetEntityQuery(typeof(TweenTarget), typeof(TweenControl));
        }

        protected override void OnUpdate()
        {
            if (!_throttler.ShouldUpdateFrame(60) || _cleanupQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var em = EntityManager;

            using (var entities = _cleanupQuery.ToEntityArray(Allocator.Temp))
            using (var targets = _cleanupQuery.ToComponentDataArray<TweenTarget>(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var ghost = entities[i];
                    var targetData = targets[i];
                    bool isOrphaned = false;

                    if (targetData.Entity != Entity.Null)
                    {
                        if (!em.Exists(targetData.Entity))
                        {
                            isOrphaned = true;
                        }
                    }
                    else if (em.HasComponent<TweenGameObjectTarget>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetGameObject(World, ghost, out var go))
                        {
                            if (go == null)
                            {
                                isOrphaned = true;
                            }
                        }
                        else
                        {
                            isOrphaned = true;
                        }
                    }
                    else if (em.HasComponent<TweenMemberHook<float>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetMember<float>(World, ghost, out var record) && IsDestroyed(record.Target))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenMemberHook<float2>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetMember<float2>(World, ghost, out var record) && IsDestroyed(record.Target))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenMemberHook<float3>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetMember<float3>(World, ghost, out var record) && IsDestroyed(record.Target))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenMemberHook<quaternion>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetMember<quaternion>(World, ghost, out var record) && IsDestroyed(record.Target))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenCallbackHook<float>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetCallback<float>(World, ghost, out var record) && IsCallbackOrphaned(record))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenCallbackHook<float2>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetCallback<float2>(World, ghost, out var record) && IsCallbackOrphaned(record))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenCallbackHook<float3>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetCallback<float3>(World, ghost, out var record) && IsCallbackOrphaned(record))
                            isOrphaned = true;
                    }
                    else if (em.HasComponent<TweenCallbackHook<quaternion>>(ghost))
                    {
                        if (TweenManagedRegistry.TryGetCallback<quaternion>(World, ghost, out var record) && IsCallbackOrphaned(record))
                            isOrphaned = true;
                    }

                    if (isOrphaned)
                    {
                        PlaybackControlInternal.KillInternal(ghost, em);
                    }
                }
            }

            TweenManagedRegistry.Cleanup(World, EntityManager);
        }

        private static bool IsDestroyed(object obj)
        {
            if (obj == null) return false;
            if (obj is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }
            return false;
        }

        private static bool IsCallbackOrphaned<T>(in TweenManagedRegistry.Callback<T>.Record record) where T : unmanaged
        {
            if (record.State != null && IsDestroyed(record.State))
                return true;

            if (record.Callback != null && record.Callback.Target != null && IsDestroyed(record.Callback.Target))
                return true;

            if (record.StateCallback != null && record.StateCallback.Target != null && IsDestroyed(record.StateCallback.Target))
                return true;

            return false;
        }
    }
}
