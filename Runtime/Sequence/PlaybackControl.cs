using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    internal static class PlaybackControlInternal
    {
        public static void PauseInternal(Entity entity, EntityManager em)
        {
            if (!em.Exists(entity)) return;

            if (em.HasComponent<Sequence>(entity))
            {
                var seq = em.GetComponentData<Sequence>(entity);
                seq.State = PlaybackState.Paused;
                em.SetComponentData(entity, seq);

                if (em.HasBuffer<SequenceElement>(entity))
                {
                    var elements = em.GetBuffer<SequenceElement>(entity);
                    for (int i = 0; i < elements.Length; i++)
                    {
                        if (elements[i].ActionEntity != Entity.Null)
                            PauseInternal(elements[i].ActionEntity, em);
                    }
                }
            }

            if (em.HasComponent<TweenControl>(entity))
            {
                em.SetComponentEnabled<TweenControl>(entity, false);
            }

            if (em.HasComponent<ChasePosition>(entity)) em.SetComponentEnabled<ChasePosition>(entity, false);
            if (em.HasComponent<ChaseRotation>(entity)) em.SetComponentEnabled<ChaseRotation>(entity, false);
            if (em.HasComponent<Look>(entity)) em.SetComponentEnabled<Look>(entity, false);
            if (em.HasComponent<ChaseScale>(entity)) em.SetComponentEnabled<ChaseScale>(entity, false);
        }

        public static void ResumeInternal(Entity entity, EntityManager em)
        {
            if (!em.Exists(entity)) return;

            if (em.HasComponent<Sequence>(entity))
            {
                var seq = em.GetComponentData<Sequence>(entity);
                seq.State = PlaybackState.Playing;
                em.SetComponentData(entity, seq);

                if (em.HasBuffer<SequenceElement>(entity))
                {
                    var elements = em.GetBuffer<SequenceElement>(entity);
                    for (int i = 0; i < elements.Length; i++)
                    {
                        var elem = elements[i];
                        if (elem.Started && !elem.Completed && elem.ActionEntity != Entity.Null)
                            ResumeInternal(elem.ActionEntity, em);
                    }
                }
            }

            if (em.HasComponent<TweenControl>(entity))
            {
                em.SetComponentEnabled<TweenControl>(entity, true);
            }

            if (em.HasComponent<ChasePosition>(entity)) em.SetComponentEnabled<ChasePosition>(entity, true);
            if (em.HasComponent<ChaseRotation>(entity)) em.SetComponentEnabled<ChaseRotation>(entity, true);
            if (em.HasComponent<Look>(entity)) em.SetComponentEnabled<Look>(entity, true);
            if (em.HasComponent<ChaseScale>(entity)) em.SetComponentEnabled<ChaseScale>(entity, true);
        }

        public static void KillInternal(Entity entity, EntityManager em)
        {
            if (!em.Exists(entity)) return;

            if (em.HasComponent<Sequence>(entity))
            {
                em.DestroyEntity(entity);
            }
            else if (em.HasComponent<TweenControl>(entity))
            {
                CleanupTargetChase(entity, em);
                em.DestroyEntity(entity);
            }
            else
            {
                CleanupChaseComponentsOnEntity(entity, em);
            }
        }

        public static void CompleteInternal(Entity entity, EntityManager em)
        {
            if (!em.Exists(entity)) return;

            if (em.HasComponent<Sequence>(entity))
            {
                if (em.HasBuffer<SequenceElement>(entity))
                {
                    var elements = em.GetBuffer<SequenceElement>(entity);
                    for (int i = 0; i < elements.Length; i++)
                    {
                        var elem = elements[i];
                        if (elem.Kind == TimelineActionKind.Callback && !elem.Started)
                        {
                            var cbEntity = em.CreateEntity();
                            em.AddComponentData(cbEntity, new SequenceCallbackEvent { SequenceEntity = entity, CallbackId = elem.CallbackId });
                        }
                        else if (elem.ActionEntity != Entity.Null)
                        {
                            CompleteInternal(elem.ActionEntity, em);
                        }
                    }
                }

                em.DestroyEntity(entity);
            }
            else if (em.HasComponent<TweenControl>(entity))
            {
                if (em.HasComponent<TweenRuntime<float>>(entity)) CompleteTween<float, FloatMath>(entity, em);
                else if (em.HasComponent<TweenRuntime<float2>>(entity)) CompleteTween<float2, Float2Math>(entity, em);
                else if (em.HasComponent<TweenRuntime<float3>>(entity)) CompleteTween<float3, Float3Math>(entity, em);
                else if (em.HasComponent<TweenRuntime<quaternion>>(entity)) CompleteTween<quaternion, QuaternionMath>(entity, em);
            }
            else
            {
                SnapChaseAndCleanup(entity, em);
            }
        }

        public static void RewindInternal(Entity entity, EntityManager em)
        {
            if (!em.Exists(entity)) return;

            if (em.HasComponent<PlaybackProgress>(entity))
            {
                var progress = em.GetComponentData<PlaybackProgress>(entity);
                progress.Direction = -1;
                em.SetComponentData(entity, progress);
            }

            if (em.HasComponent<Sequence>(entity))
            {
                var seq = em.GetComponentData<Sequence>(entity);
                if (seq.State == PlaybackState.Completed)
                {
                    seq.Time = seq.Duration;
                }
                seq.State = PlaybackState.Playing;
                em.SetComponentData(entity, seq);

                if (em.HasBuffer<SequenceElement>(entity))
                {
                    var elements = em.GetBuffer<SequenceElement>(entity);
                    for (int i = 0; i < elements.Length; i++)
                    {
                        var elem = elements[i];
                        if (elem.ActionEntity != Entity.Null)
                        {
                            if (em.HasComponent<TweenControl>(elem.ActionEntity))
                            {
                                em.SetComponentEnabled<TweenControl>(elem.ActionEntity, true);
                                var childProgress = em.GetComponentData<PlaybackProgress>(elem.ActionEntity);
                                childProgress.Direction = -1;
                                em.SetComponentData(elem.ActionEntity, childProgress);
                            }
                        }
                    }
                }
            }

            if (em.HasComponent<TweenControl>(entity))
            {
                var control = em.GetComponentData<TweenControl>(entity);
                if (IsTweenComplete(entity, em))
                {
                    ResetTweenCompleted(entity, em);
                    if (em.HasComponent<PlaybackProgress>(entity))
                    {
                        var progress = em.GetComponentData<PlaybackProgress>(entity);
                        progress.ElapsedTime = control.SecondsToPlay;
                        em.SetComponentData(entity, progress);
                    }
                }
                em.SetComponentEnabled<TweenControl>(entity, true);
            }
        }

        private static void CompleteTween<T, TMath>(Entity ghost, EntityManager em)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            if (!em.HasComponent<TweenControl>(ghost) || !em.HasComponent<TweenRange<T>>(ghost) || !em.HasComponent<TweenRuntime<T>>(ghost)) return;

            var control = em.GetComponentData<TweenControl>(ghost);
            var range = em.GetComponentData<TweenRange<T>>(ghost);
            var runtime = em.GetComponentData<TweenRuntime<T>>(ghost);

            float easedT = Ease.EasedT(1f, control.EaseType);
            T endValue = range.EndPoint;

            if (em.HasComponent<SplineBlobRef<T>>(ghost))
            {
                var spline = em.GetComponentData<SplineBlobRef<T>>(ghost);
                var blobRef = spline.Blob;
                if (blobRef.IsCreated)
                {
                    var provider = new Spline.BlobSplineAdapter<T>(blobRef);
                    var sample = Spline.SampleGeneric<T, Spline.BlobSplineAdapter<T>, TMath>(ref provider, easedT);
                    TMath mathProvider = default;
                    endValue = spline.IsBend && provider.KnotCount >= 2
                        ? mathProvider.Bend(
                            range.StartPoint,
                            range.EndPoint,
                            provider.GetKnot(0),
                            provider.GetKnot(provider.KnotCount - 1),
                            sample,
                            easedT)
                        : sample;
                }
            }
            else if (em.HasComponent<SplineState>(ghost) && em.HasBuffer<SplineElement<T>>(ghost))
            {
                var state = em.GetComponentData<SplineState>(ghost);
                var buffer = em.GetBuffer<SplineElement<T>>(ghost);
                var provider = new Spline.BufferSplineAdapter<T>(state, buffer);
                endValue = Spline.SampleGeneric<T, Spline.BufferSplineAdapter<T>, TMath>(ref provider, easedT);
            }

            runtime.CurrentValue = endValue;
            em.SetComponentData(ghost, runtime);

            ApplyEndValueToTarget<T>(ghost, endValue, em);

            if (control.AutoKill)
            {
                em.DestroyEntity(ghost);
            }
            else
            {
                runtime.Completed = true;
                em.SetComponentData(ghost, runtime);

                if (em.HasComponent<PlaybackProgress>(ghost))
                {
                    var progress = em.GetComponentData<PlaybackProgress>(ghost);
                    progress.ElapsedTime = control.SecondsToPlay;
                    progress.NormalizedTime = 1f;
                    em.SetComponentData(ghost, progress);
                }

                em.SetComponentEnabled<TweenControl>(ghost, false);
            }
        }

        private static bool IsTweenComplete(Entity entity, EntityManager em)
        {
            if (em.HasComponent<TweenRuntime<float>>(entity)) return em.GetComponentData<TweenRuntime<float>>(entity).Completed;
            if (em.HasComponent<TweenRuntime<float2>>(entity)) return em.GetComponentData<TweenRuntime<float2>>(entity).Completed;
            if (em.HasComponent<TweenRuntime<float3>>(entity)) return em.GetComponentData<TweenRuntime<float3>>(entity).Completed;
            if (em.HasComponent<TweenRuntime<quaternion>>(entity)) return em.GetComponentData<TweenRuntime<quaternion>>(entity).Completed;
            return false;
        }

        private static void ResetTweenCompleted(Entity entity, EntityManager em)
        {
            if (em.HasComponent<TweenRuntime<float>>(entity))
            {
                var runtime = em.GetComponentData<TweenRuntime<float>>(entity);
                runtime.Completed = false;
                em.SetComponentData(entity, runtime);
            }
            else if (em.HasComponent<TweenRuntime<float2>>(entity))
            {
                var runtime = em.GetComponentData<TweenRuntime<float2>>(entity);
                runtime.Completed = false;
                em.SetComponentData(entity, runtime);
            }
            else if (em.HasComponent<TweenRuntime<float3>>(entity))
            {
                var runtime = em.GetComponentData<TweenRuntime<float3>>(entity);
                runtime.Completed = false;
                em.SetComponentData(entity, runtime);
            }
            else if (em.HasComponent<TweenRuntime<quaternion>>(entity))
            {
                var runtime = em.GetComponentData<TweenRuntime<quaternion>>(entity);
                runtime.Completed = false;
                em.SetComponentData(entity, runtime);
            }
        }

        private static void ApplyEndValueToTarget<T>(Entity ghost, T endValue, EntityManager em)
            where T : unmanaged
        {
            if (typeof(T) == typeof(float))
            {
                float val = (float)(object)endValue;
                if (em.HasComponent<TweenTransformTarget>(ghost))
                {
                    var targetData = em.GetComponentData<TweenTransformTarget>(ghost);
                    ApplyImmediateToTransform(ghost, val, targetData, em);
                }
                if (em.HasComponent<TweenTarget>(ghost))
                {
                    var targetData = em.GetComponentData<TweenTarget>(ghost);
                    var target = targetData.Entity;
                    if (target != Entity.Null && em.Exists(target) && em.HasComponent<LocalTransform>(target))
                    {
                        var transform = em.GetComponentData<LocalTransform>(target);
                        transform.Scale = val;
                        em.SetComponentData(target, transform);
                    }
                }
            }
            else if (typeof(T) == typeof(float3))
            {
                float3 val = (float3)(object)endValue;
                if (em.HasComponent<TweenTransformTarget>(ghost))
                {
                    var targetData = em.GetComponentData<TweenTransformTarget>(ghost);
                    ApplyImmediateToTransform(ghost, val, targetData, em);
                }
                if (em.HasComponent<TweenTarget>(ghost))
                {
                    var targetData = em.GetComponentData<TweenTarget>(ghost);
                    var target = targetData.Entity;
                    if (target != Entity.Null && em.Exists(target) && em.HasComponent<LocalTransform>(target))
                    {
                        var transform = em.GetComponentData<LocalTransform>(target);
                        if (targetData.TweenType == TweenType.MoveTo)
                        {
                            if (targetData.Space == TweenSpace.World && em.HasComponent<Parent>(target))
                            {
                                var parent = em.GetComponentData<Parent>(target).Value;
                                if (em.Exists(parent) && em.HasComponent<LocalToWorld>(parent))
                                {
                                    var parentLtw = em.GetComponentData<LocalToWorld>(parent).Value;
                                    transform.Position = math.transform(math.inverse(parentLtw), val);
                                }
                                else transform.Position = val;
                            }
                            else transform.Position = val;
                        }
                        else if (targetData.TweenType == TweenType.ScaleTo)
                        {
                            transform.Scale = math.cmax(val);
                        }
                        em.SetComponentData(target, transform);
                    }
                }
            }
            else if (typeof(T) == typeof(quaternion))
            {
                quaternion val = (quaternion)(object)endValue;
                if (em.HasComponent<TweenTransformTarget>(ghost))
                {
                    var targetData = em.GetComponentData<TweenTransformTarget>(ghost);
                    ApplyImmediateToTransform(ghost, val, targetData, em);
                }
                if (em.HasComponent<TweenTarget>(ghost))
                {
                    var targetData = em.GetComponentData<TweenTarget>(ghost);
                    var target = targetData.Entity;
                    if (target != Entity.Null && em.Exists(target) && em.HasComponent<LocalTransform>(target))
                    {
                        var transform = em.GetComponentData<LocalTransform>(target);
                        if (targetData.Space == TweenSpace.World && em.HasComponent<Parent>(target))
                        {
                            var parent = em.GetComponentData<Parent>(target).Value;
                            if (em.Exists(parent) && em.HasComponent<LocalToWorld>(parent))
                            {
                                var parentLtw = em.GetComponentData<LocalToWorld>(parent).Value;
                                var parentRot = math.quaternion(parentLtw);
                                transform.Rotation = math.mul(math.conjugate(parentRot), val);
                            }
                            else transform.Rotation = val;
                        }
                        else transform.Rotation = val;
                        em.SetComponentData(target, transform);
                    }
                }
            }

            CleanupTargetChase(ghost, em);
        }

        private static void ApplyImmediateToTransform<T>(Entity ghost, T value, TweenTransformTarget target, EntityManager em)
            where T : unmanaged
        {
            if (!em.HasComponent<TweenTransformReference>(ghost))
                return;

            var transform = em.GetComponentObject<TweenTransformReference>(ghost).Transform;
            if (transform == null)
                return;

            if (typeof(T) == typeof(float))
            {
                transform.localScale = Vector3.one * (float)(object)value;
            }
            else if (typeof(T) == typeof(float3))
            {
                var v = (float3)(object)value;
                if (target.Binding == TweenTransformBinding.Scale)
                    transform.localScale = v;
                else if (target.Space == TweenSpace.World)
                    transform.position = v;
                else
                    transform.localPosition = v;
            }
            else if (typeof(T) == typeof(quaternion))
            {
                var q = (quaternion)(object)value;
                if (target.Space == TweenSpace.World)
                    transform.rotation = q;
                else
                    transform.localRotation = q;
            }
        }

        private static void CleanupTargetChase(Entity ghost, EntityManager em)
        {
            if (!em.HasComponent<TweenTarget>(ghost)) return;
            var target = em.GetComponentData<TweenTarget>(ghost).Entity;
            if (target == Entity.Null || !em.Exists(target)) return;

            if (em.HasComponent<ChasePositionTweenSource>(target) && em.GetComponentData<ChasePositionTweenSource>(target).GhostEntity == ghost)
            {
                em.RemoveComponent<ChasePositionTweenSource>(target);
                em.RemoveComponent<ChasePosition>(target);
            }
            if (em.HasComponent<ChaseRotationTweenSource>(target) && em.GetComponentData<ChaseRotationTweenSource>(target).GhostEntity == ghost)
            {
                em.RemoveComponent<ChaseRotationTweenSource>(target);
                em.RemoveComponent<ChaseRotation>(target);
            }
            if (em.HasComponent<LookTweenSource>(target) && em.GetComponentData<LookTweenSource>(target).GhostEntity == ghost)
            {
                em.RemoveComponent<LookTweenSource>(target);
                em.RemoveComponent<Look>(target);
            }
            if (em.HasComponent<ChaseScaleTweenSource>(target) && em.GetComponentData<ChaseScaleTweenSource>(target).GhostEntity == ghost)
            {
                em.RemoveComponent<ChaseScaleTweenSource>(target);
                em.RemoveComponent<ChaseScale>(target);
            }
        }

        private static void SetWorldPosition(Entity entity, float3 newWorldPos, ref LocalTransform localTransform, EntityManager em)
        {
            if (em.HasComponent<Parent>(entity))
            {
                var parentEntity = em.GetComponentData<Parent>(entity).Value;
                if (em.Exists(parentEntity) && em.HasComponent<LocalToWorld>(parentEntity))
                {
                    var parentLtw = em.GetComponentData<LocalToWorld>(parentEntity);
                    var rel = newWorldPos - parentLtw.Position;
                    localTransform.Position = math.rotate(math.conjugate(math.quaternion(parentLtw.Value)), rel);
                    return;
                }
            }

            localTransform.Position = newWorldPos;
        }

        private static void SetWorldRotation(Entity entity, quaternion newWorldRot, ref LocalTransform localTransform, EntityManager em)
        {
            if (em.HasComponent<Parent>(entity))
            {
                var parentEntity = em.GetComponentData<Parent>(entity).Value;
                if (em.Exists(parentEntity) && em.HasComponent<LocalToWorld>(parentEntity))
                {
                    var parentLtw = em.GetComponentData<LocalToWorld>(parentEntity);
                    localTransform.Rotation = math.mul(math.conjugate(math.quaternion(parentLtw.Value)), newWorldRot);
                    return;
                }
            }

            localTransform.Rotation = newWorldRot;
        }

        private static void SnapChaseAndCleanup(Entity target, EntityManager em)
        {
            if (!em.Exists(target)) return;

            if (em.HasComponent<ChasePosition>(target))
            {
                var chase = em.GetComponentData<ChasePosition>(target);
                var targetPos = chase.TargetPosition;
                if (em.HasComponent<ChaseTargetEntity>(target))
                {
                    var chaseTarget = em.GetComponentData<ChaseTargetEntity>(target).Target;
                    if (em.Exists(chaseTarget) && em.HasComponent<LocalToWorld>(chaseTarget))
                        targetPos = em.GetComponentData<LocalToWorld>(chaseTarget).Position;
                }

                if (em.HasComponent<LocalTransform>(target))
                {
                    var localTransform = em.GetComponentData<LocalTransform>(target);
                    if (chase.Space == TweenSpace.Local)
                    {
                        localTransform.Position = targetPos;
                    }
                    else
                    {
                        SetWorldPosition(target, targetPos, ref localTransform, em);
                    }
                    em.SetComponentData(target, localTransform);
                }
            }

            if (em.HasComponent<ChaseRotation>(target))
            {
                var chase = em.GetComponentData<ChaseRotation>(target);
                var targetRot = chase.TargetQuaternion;
                if (em.HasComponent<ChaseTargetEntity>(target))
                {
                    var chaseTarget = em.GetComponentData<ChaseTargetEntity>(target).Target;
                    if (em.Exists(chaseTarget) && em.HasComponent<LocalToWorld>(chaseTarget))
                        targetRot = em.GetComponentData<LocalToWorld>(chaseTarget).Rotation;
                }

                if (em.HasComponent<LocalTransform>(target))
                {
                    var localTransform = em.GetComponentData<LocalTransform>(target);
                    if (chase.Space == TweenSpace.Local)
                    {
                        localTransform.Rotation = targetRot;
                    }
                    else
                    {
                        SetWorldRotation(target, targetRot, ref localTransform, em);
                    }
                    em.SetComponentData(target, localTransform);
                }
            }

            if (em.HasComponent<Look>(target))
            {
                var chase = em.GetComponentData<Look>(target);
                var targetPos = chase.TargetPosition;
                if (em.HasComponent<ChaseTargetEntity>(target))
                {
                    var chaseTarget = em.GetComponentData<ChaseTargetEntity>(target).Target;
                    if (em.Exists(chaseTarget) && em.HasComponent<LocalToWorld>(chaseTarget))
                        targetPos = em.GetComponentData<LocalToWorld>(chaseTarget).Position;
                }

                if (em.HasComponent<LocalToWorld>(target) && em.HasComponent<LocalTransform>(target))
                {
                    var ltw = em.GetComponentData<LocalToWorld>(target);
                    var diff = targetPos - ltw.Position;
                    var distSq = math.lengthsq(diff);
                    if (distSq > 1e-12f)
                    {
                        var localTransform = em.GetComponentData<LocalTransform>(target);
                        var desiredRot = quaternion.LookRotationSafe(diff / math.sqrt(distSq), math.up());
                        SetWorldRotation(target, desiredRot, ref localTransform, em);
                        em.SetComponentData(target, localTransform);
                    }
                }
            }

            if (em.HasComponent<ChaseScale>(target))
            {
                var chase = em.GetComponentData<ChaseScale>(target);
                var targetScale = chase.IsUniform ? chase.TargetScale.x : math.cmax(chase.TargetScale);
                if (em.HasComponent<ChaseTargetEntity>(target))
                {
                    var chaseTarget = em.GetComponentData<ChaseTargetEntity>(target).Target;
                    if (em.Exists(chaseTarget) && em.HasComponent<LocalToWorld>(chaseTarget))
                        targetScale = math.length(em.GetComponentData<LocalToWorld>(chaseTarget).Value.c0.xyz);
                }

                if (em.HasComponent<LocalTransform>(target))
                {
                    var localTransform = em.GetComponentData<LocalTransform>(target);
                    localTransform.Scale = targetScale;
                    em.SetComponentData(target, localTransform);
                }
            }

            CleanupChaseComponentsOnEntity(target, em);
        }

        private static void CleanupChaseComponentsOnEntity(Entity target, EntityManager em)
        {
            if (em.HasComponent<ChasePosition>(target)) em.RemoveComponent<ChasePosition>(target);
            if (em.HasComponent<ChasePositionTweenSource>(target)) em.RemoveComponent<ChasePositionTweenSource>(target);
            if (em.HasComponent<ChaseRotation>(target)) em.RemoveComponent<ChaseRotation>(target);
            if (em.HasComponent<ChaseRotationTweenSource>(target)) em.RemoveComponent<ChaseRotationTweenSource>(target);
            if (em.HasComponent<Look>(target)) em.RemoveComponent<Look>(target);
            if (em.HasComponent<LookTweenSource>(target)) em.RemoveComponent<LookTweenSource>(target);
            if (em.HasComponent<ChaseScale>(target)) em.RemoveComponent<ChaseScale>(target);
            if (em.HasComponent<ChaseScaleTweenSource>(target)) em.RemoveComponent<ChaseScaleTweenSource>(target);
            if (em.HasComponent<ChaseTargetEntity>(target)) em.RemoveComponent<ChaseTargetEntity>(target);
        }
    }
}
