using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace XO.Entityween
{
    [BurstCompile]
    [UpdateInGroup(typeof(EntityweenSequenceGroup))]
    internal partial struct SequencePlaybackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Sequence>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var deltaTime = SystemAPI.Time.DeltaTime;
            var unscaledDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var controlLookup = SystemAPI.GetComponentLookup<TweenControl>(false);
            var progressLookup = SystemAPI.GetComponentLookup<PlaybackProgress>(false);
            var tweenBindingLookup = SystemAPI.GetComponentLookup<SequenceTweenBinding>(true);
            var valueFloatLookup = SystemAPI.GetComponentLookup<TweenValue<float>>(false);
            var valueFloat3Lookup = SystemAPI.GetComponentLookup<TweenValue<float3>>(false);
            var valueQuatLookup = SystemAPI.GetComponentLookup<TweenValue<quaternion>>(false);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var positionLookup = SystemAPI.GetComponentLookup<ChasePosition>(false);
            var rotationLookup = SystemAPI.GetComponentLookup<ChaseRotation>(false);
            var lookLookup = SystemAPI.GetComponentLookup<Look>(false);
            var scaleLookup = SystemAPI.GetComponentLookup<ChaseScale>(false);
            var positionSourceLookup = SystemAPI.GetComponentLookup<ChasePositionTweenSource>(true);
            var rotationSourceLookup = SystemAPI.GetComponentLookup<ChaseRotationTweenSource>(true);
            var scaleSourceLookup = SystemAPI.GetComponentLookup<ChaseScaleTweenSource>(true);
            var chaseTargetLookup = SystemAPI.GetComponentLookup<ChaseTargetEntity>(true);
            var dynamicTimeLookup = SystemAPI.GetComponentLookup<SequenceDynamicTime>(false);
            var elementLookup = SystemAPI.GetBufferLookup<SequenceElement>(false);

            foreach (var (sequenceRef, progressRef, sequenceEntity) in SystemAPI.Query<RefRW<Sequence>, RefRW<PlaybackProgress>>().WithEntityAccess())
            {
                var sequence = sequenceRef.ValueRO;
                if (sequence.State != PlaybackState.Playing) continue;
                if (!elementLookup.HasBuffer(sequenceEntity)) continue;

                var progress = progressRef.ValueRO;
                var elements = elementLookup[sequenceEntity];

                if (sequence.Duration <= 0f)
                {
                    EmitInstantCallbacks(sequenceEntity, elements, ecb);
                    sequence.State = PlaybackState.Completed;
                    sequenceRef.ValueRW = sequence;
                    progressRef.ValueRW.NormalizedTime = 1f;
                    continue;
                }

                var progressVal = progressRef.ValueRW;
                var direction = progressVal.Direction == 0 ? 1 : progressVal.Direction;
                var sequenceDeltaTime = PlaybackUtilities.GetDeltaTime(progressVal.TimeType, deltaTime, unscaledDeltaTime) * direction;
                sequence.Time += sequenceDeltaTime * math.max(0f, sequence.TimeScale);

                var oldLoopIndex = progressVal.LoopIndex;
                PlaybackUtilities.CalculateProgress(
                    ref sequence.Time,
                    sequence.Duration,
                    ref progressVal,
                    out float normalizedTime,
                    out bool isFinished);

                if (progressVal.LoopType != LoopType.None)
                {
                    if (progressVal.LoopIndex != oldLoopIndex)
                    {
                        var loopDelta = progressVal.LoopIndex - oldLoopIndex;
                        if (progressVal.LoopType == LoopType.PingPong && (math.abs(loopDelta) & 1) == 1)
                            sequence.Direction *= -1;

                        for (int i = 0; i < elements.Length; i++)
                        {
                            var element = elements[i];
                            CleanupElement(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
                            element.Started = false;
                            element.Completed = false;
                            element.TimeShift = 0f;
                            elements[i] = element;
                        }
                        if (dynamicTimeLookup.HasComponent(sequenceEntity))
                            dynamicTimeLookup[sequenceEntity] = default;
                    }
                }

                progressVal.NormalizedTime = normalizedTime;
                progressRef.ValueRW = progressVal;

                var effectiveTime = sequence.Direction >= 0 ? sequence.Time : sequence.Duration - sequence.Time;
                effectiveTime = math.clamp(effectiveTime, 0f, sequence.Duration);
                var useDynamicTime = dynamicTimeLookup.HasComponent(sequenceEntity);
                var dynamicTime = useDynamicTime ? dynamicTimeLookup[sequenceEntity] : default;

                var effectiveDirection = sequence.Direction * direction;

                for (int i = 0; i < elements.Length; i++)
                {
                    var element = elements[i];
                    ProcessElement(sequenceEntity, effectiveDirection, effectiveTime, ref element, ecb, controlLookup, progressLookup, tweenBindingLookup,
                        valueFloatLookup, valueFloat3Lookup, valueQuatLookup, localTransformLookup, localToWorldLookup,
                        positionLookup, rotationLookup, lookLookup, scaleLookup, positionSourceLookup, rotationSourceLookup, scaleSourceLookup,
                        chaseTargetLookup, useDynamicTime, dynamicTime.Consumed, out var savedTime);
                    elements[i] = element;

                    if (useDynamicTime && savedTime > 0f)
                        dynamicTime.Consumed += savedTime;
                }

                if (useDynamicTime)
                    dynamicTimeLookup[sequenceEntity] = dynamicTime;

                if (isFinished)
                    sequence.State = PlaybackState.Completed;

                sequenceRef.ValueRW = sequence;
            }
        }

        private static void ProcessElement(Entity sequenceEntity, int direction, float effectiveTime, ref SequenceElement element,
            EntityCommandBuffer ecb,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<PlaybackProgress> progressLookup,
            ComponentLookup<SequenceTweenBinding> tweenBindingLookup,
            ComponentLookup<TweenValue<float>> valueFloatLookup,
            ComponentLookup<TweenValue<float3>> valueFloat3Lookup,
            ComponentLookup<TweenValue<quaternion>> valueQuatLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup,
            ComponentLookup<ChasePositionTweenSource> positionSourceLookup,
            ComponentLookup<ChaseRotationTweenSource> rotationSourceLookup,
            ComponentLookup<ChaseScaleTweenSource> scaleSourceLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup,
            bool useDynamicTime,
            float dynamicShift,
            out float savedTime)
        {
            savedTime = 0f;

            if (direction >= 0)
            {
                if (element.Completed)
                {
                    CleanupElement(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
                    return;
                }
            }
            else
            {
                var elemStart = element.StartTime - (element.Started ? element.TimeShift : dynamicShift);
                var elemEnd = elemStart + element.Duration;

                if (effectiveTime > elemEnd)
                {
                    element.Completed = true;
                    element.Started = true;
                    CleanupElement(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
                    return;
                }
                else if (effectiveTime < elemStart)
                {
                    element.Completed = false;
                    element.Started = false;
                    CleanupElement(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
                    return;
                }
                else
                {
                    element.Completed = false;
                    element.Started = true;
                }
            }

            var start = element.StartTime - (element.Started ? element.TimeShift : dynamicShift);

            if (element.Kind == TimelineActionKind.Callback)
            {
                if (!element.Started && HasReached(direction, effectiveTime, start))
                {
                    if (useDynamicTime)
                        element.TimeShift = dynamicShift;
                    var callbackEntity = ecb.CreateEntity();
                    ecb.AddComponent(callbackEntity, new SequenceCallbackEvent { SequenceEntity = sequenceEntity, CallbackId = element.CallbackId });
                    element.Started = true;
                    element.Completed = true;
                }
                return;
            }

            if (element.Kind == TimelineActionKind.Wait)
            {
                if (!element.Started && HasReached(direction, effectiveTime, start))
                {
                    if (useDynamicTime)
                        element.TimeShift = dynamicShift;
                    element.Started = true;
                }

                var waitEnd = direction >= 0 ? start + element.Duration : start;
                if (HasReached(direction, effectiveTime, waitEnd))
                    element.Completed = true;
                return;
            }

            var end = start + element.Duration;
            var reachedStart = direction >= 0 ? effectiveTime >= start : effectiveTime <= end;
            if (!element.Started && !reachedStart) return;

            if (!element.Started)
            {
                if (useDynamicTime)
                    element.TimeShift = dynamicShift;

                if (element.Kind == TimelineActionKind.Tween && tweenBindingLookup.HasComponent(element.ActionEntity))
                {
                    var binding = tweenBindingLookup[element.ActionEntity];
                    ResolveStartFromCurrent(element.ActionEntity, binding, valueFloatLookup, valueFloat3Lookup, valueQuatLookup, localTransformLookup, localToWorldLookup);
                    if (binding.TargetEntity != Entity.Null)
                    {
                        BindTweenAction(element.ActionEntity, binding, ecb, positionLookup, rotationLookup, scaleLookup,
                        positionSourceLookup, rotationSourceLookup, scaleSourceLookup);
                    }
                }

                StartAction(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
                element.Started = true;
            }

            if (ActionWasRemovedOrDisabled(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup))
            {
                element.Completed = true;
                savedTime = useDynamicTime ? RemainingDuration(effectiveTime, start, end, direction) : 0f;
                return;
            }

            var localElapsed = math.clamp(effectiveTime - start, 0f, element.Duration);
            if (direction < 0)
                localElapsed = math.clamp(effectiveTime - start, 0f, element.Duration);

            if (element.Kind == TimelineActionKind.Tween)
                StartOrUpdateTween(element.ActionEntity, element.Duration, localElapsed, direction, controlLookup, progressLookup);

            if (useDynamicTime && ActionCompletedEarly(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup,
                    localTransformLookup, localToWorldLookup, chaseTargetLookup))
            {
                element.Completed = true;
                CleanupElement(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
                savedTime = RemainingDuration(effectiveTime, start, end, direction);
                return;
            }

            var reachedEnd = direction >= 0 ? effectiveTime >= end : effectiveTime <= start;
            if (reachedEnd)
            {
                element.Completed = true;
                CompleteAction(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
            }
        }

        private static float RemainingDuration(float effectiveTime, float start, float end, int direction)
        {
            return direction >= 0
                ? math.max(0f, end - effectiveTime)
                : math.max(0f, effectiveTime - start);
        }

        private static bool ActionWasRemovedOrDisabled(SequenceElement element,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup)
        {
            if (!element.Started) return false;

            switch (element.Kind)
            {
                case TimelineActionKind.Tween:
                    return !controlLookup.HasComponent(element.ActionEntity);
                case TimelineActionKind.Chase:
                    return !HasAnyChaseComponent(element.ActionEntity, positionLookup, rotationLookup, lookLookup, scaleLookup);
                default:
                    return false;
            }
        }

        private static bool ActionCompletedEarly(SequenceElement element,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup)
        {
            switch (element.Kind)
            {
                case TimelineActionKind.Tween:
                    return controlLookup.TryGetComponent(element.ActionEntity, out var control) && control.Completed;
                case TimelineActionKind.Chase:
                    return IsTimelineChaseComplete(element.ActionEntity, positionLookup, rotationLookup, lookLookup, scaleLookup,
                        localTransformLookup, localToWorldLookup, chaseTargetLookup);
                default:
                    return false;
            }
        }

        private static bool IsTimelineChaseComplete(Entity entity,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup)
        {
            var hasAny = false;
            var allSettled = true;

            if (positionLookup.HasComponent(entity))
            {
                hasAny = true;
                if (positionLookup.IsComponentEnabled(entity))
                    allSettled &= IsPositionChaseSettled(entity, positionLookup[entity], localTransformLookup, localToWorldLookup, chaseTargetLookup);
            }

            if (rotationLookup.HasComponent(entity))
            {
                hasAny = true;
                if (rotationLookup.IsComponentEnabled(entity))
                    allSettled &= IsRotationChaseSettled(entity, rotationLookup[entity], localTransformLookup, localToWorldLookup, chaseTargetLookup);
            }

            if (lookLookup.HasComponent(entity))
            {
                hasAny = true;
                if (lookLookup.IsComponentEnabled(entity))
                    allSettled &= IsLookSettled(entity, lookLookup[entity], localToWorldLookup, chaseTargetLookup);
            }

            if (scaleLookup.HasComponent(entity))
            {
                hasAny = true;
                if (scaleLookup.IsComponentEnabled(entity))
                    allSettled &= IsScaleChaseSettled(entity, scaleLookup[entity], localTransformLookup, localToWorldLookup, chaseTargetLookup);
            }

            return hasAny && allSettled;
        }

        private static bool IsPositionChaseSettled(Entity entity, ChasePosition chase,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup)
        {
            if (!localTransformLookup.TryGetComponent(entity, out var transform)) return false;
            var current = chase.Space == TweenSpace.Local
                ? transform.Position
                : localToWorldLookup.TryGetComponent(entity, out var ltw) ? ltw.Position : transform.Position;
            var target = chase.TargetPosition;
            if (chaseTargetLookup.TryGetComponent(entity, out var targetEntity) &&
                localToWorldLookup.TryGetComponent(targetEntity.Target, out var targetLtw))
                target = targetLtw.Position;

            return math.lengthsq(current - target) <= math.EPSILON &&
                   math.lengthsq(chase.Velocity) <= math.EPSILON;
        }

        private static bool IsRotationChaseSettled(Entity entity, ChaseRotation chase,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup)
        {
            if (!localTransformLookup.TryGetComponent(entity, out var transform)) return false;
            var current = chase.Space == TweenSpace.Local
                ? transform.Rotation
                : localToWorldLookup.TryGetComponent(entity, out var ltw) ? ltw.Rotation : transform.Rotation;
            var target = chase.TargetQuaternion;
            if (chaseTargetLookup.TryGetComponent(entity, out var targetEntity) &&
                localToWorldLookup.TryGetComponent(targetEntity.Target, out var targetLtw))
                target = targetLtw.Rotation;

            return math.abs(math.dot(current, target)) >= 0.99999f &&
                   math.lengthsq(chase.Velocity.value) <= math.EPSILON;
        }

        private static bool IsLookSettled(Entity entity, Look look,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup)
        {
            if (!localToWorldLookup.TryGetComponent(entity, out var ltw)) return false;
            var target = look.TargetPosition;
            if (chaseTargetLookup.TryGetComponent(entity, out var targetEntity) &&
                localToWorldLookup.TryGetComponent(targetEntity.Target, out var targetLtw))
                target = targetLtw.Position;

            var diff = target - ltw.Position;
            var distSq = math.lengthsq(diff);
            if (distSq <= 1e-12f) return true;

            var desired = quaternion.LookRotationSafe(diff / math.sqrt(distSq), math.up());
            var current = math.quaternion(ltw.Value);
            return math.abs(math.dot(current, desired)) >= 0.99999f &&
                   math.lengthsq(look.Velocity) <= math.EPSILON;
        }

        private static bool IsScaleChaseSettled(Entity entity, ChaseScale chase,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChaseTargetEntity> chaseTargetLookup)
        {
            if (!localTransformLookup.TryGetComponent(entity, out var transform)) return false;
            var current = new float3(transform.Scale);
            var target = chase.TargetScale;
            if (chaseTargetLookup.TryGetComponent(entity, out var targetEntity) &&
                localToWorldLookup.TryGetComponent(targetEntity.Target, out var targetLtw))
                target = new float3(math.length(targetLtw.Value.c0.xyz));

            return math.lengthsq(current - target) <= math.EPSILON &&
                   math.lengthsq(chase.Velocity) <= math.EPSILON;
        }

        private static bool HasAnyChaseComponent(Entity entity,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup)
        {
            return positionLookup.HasComponent(entity) ||
                   rotationLookup.HasComponent(entity) ||
                   lookLookup.HasComponent(entity) ||
                   scaleLookup.HasComponent(entity);
        }

        private static bool IsAnyChaseEnabled(Entity entity,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup)
        {
            return positionLookup.HasComponent(entity) && positionLookup.IsComponentEnabled(entity) ||
                   rotationLookup.HasComponent(entity) && rotationLookup.IsComponentEnabled(entity) ||
                   lookLookup.HasComponent(entity) && lookLookup.IsComponentEnabled(entity) ||
                   scaleLookup.HasComponent(entity) && scaleLookup.IsComponentEnabled(entity);
        }

        private static void StartAction(SequenceElement element,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup)
        {
            if (element.Kind == TimelineActionKind.Tween)
            {
                if (controlLookup.HasComponent(element.ActionEntity))
                    controlLookup.SetComponentEnabled(element.ActionEntity, true);
                return;
            }

            if (element.Kind != TimelineActionKind.Chase) return;

            if (positionLookup.HasComponent(element.ActionEntity))
                positionLookup.SetComponentEnabled(element.ActionEntity, true);
            if (rotationLookup.HasComponent(element.ActionEntity))
                rotationLookup.SetComponentEnabled(element.ActionEntity, true);
            if (lookLookup.HasComponent(element.ActionEntity))
                lookLookup.SetComponentEnabled(element.ActionEntity, true);
            if (scaleLookup.HasComponent(element.ActionEntity))
                scaleLookup.SetComponentEnabled(element.ActionEntity, true);
        }

        private static void CompleteAction(SequenceElement element,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup)
        {
            if (element.Kind == TimelineActionKind.Chase)
                CleanupElement(element, controlLookup, positionLookup, rotationLookup, lookLookup, scaleLookup);
        }

        private static void StartOrUpdateTween(Entity actionEntity, float duration, float elapsedTime, int effectiveDirection,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<PlaybackProgress> progressLookup)
        {
            if (!controlLookup.HasComponent(actionEntity)) return;

            var control = controlLookup[actionEntity];
            control.ElapsedTime = elapsedTime;
            control.AutoKill = false;
            control.Completed = false;
            controlLookup[actionEntity] = control;
            controlLookup.SetComponentEnabled(actionEntity, true);

            if (progressLookup.HasComponent(actionEntity))
            {
                var progress = progressLookup[actionEntity];
                progress.NormalizedTime = duration > 0f ? math.saturate(elapsedTime / duration) : 1f;
                progress.Direction = effectiveDirection;
                progressLookup[actionEntity] = progress;
            }
        }

        private static void BindTweenAction(Entity actionEntity, SequenceTweenBinding binding,
            EntityCommandBuffer ecb,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<ChaseScale> scaleLookup,
            ComponentLookup<ChasePositionTweenSource> positionSourceLookup,
            ComponentLookup<ChaseRotationTweenSource> rotationSourceLookup,
            ComponentLookup<ChaseScaleTweenSource> scaleSourceLookup)
        {
            switch (binding.TweenType)
            {
                case TweenType.MoveTo:
                    var position = new ChasePosition
                    {
                        TargetPosition = float3.zero,
                        Velocity = float3.zero,
                        Space = binding.Space,
                        Mode = binding.ChaseMode,
                        SmoothTime = binding.ChaseSmoothTime,
                        MaxSpeed = binding.ChaseMaxSpeed,
                        KillOnChase = !binding.UseChase || binding.KillOnChase
                    };
                    if (positionLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, position);
                    else ecb.AddComponent(binding.TargetEntity, position);
                    var positionSource = new ChasePositionTweenSource
                    {
                        GhostEntity = actionEntity,
                        Space = binding.Space
                    };
                    if (positionSourceLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, positionSource);
                    else ecb.AddComponent(binding.TargetEntity, positionSource);
                    break;
                case TweenType.RotateTo:
                    var rotation = new ChaseRotation
                    {
                        TargetQuaternion = quaternion.identity,
                        Velocity = new quaternion(0f, 0f, 0f, 0f),
                        Space = binding.Space,
                        Mode = binding.ChaseMode,
                        SmoothTime = binding.ChaseSmoothTime,
                        MaxSpeed = binding.ChaseMaxSpeed,
                        KillOnChase = !binding.UseChase || binding.KillOnChase
                    };
                    if (rotationLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, rotation);
                    else ecb.AddComponent(binding.TargetEntity, rotation);
                    var rotationSource = new ChaseRotationTweenSource
                    {
                        GhostEntity = actionEntity,
                        Space = binding.Space
                    };
                    if (rotationSourceLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, rotationSource);
                    else ecb.AddComponent(binding.TargetEntity, rotationSource);
                    break;
                case TweenType.ScaleTo:
                    var scale = new ChaseScale
                    {
                        TargetScale = float3.zero,
                        Velocity = float3.zero,
                        IsUniform = false,
                        Mode = binding.ChaseMode,
                        SmoothTime = binding.ChaseSmoothTime,
                        MaxSpeed = binding.ChaseMaxSpeed,
                        KillOnChase = !binding.UseChase || binding.KillOnChase
                    };
                    if (scaleLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, scale);
                    else ecb.AddComponent(binding.TargetEntity, scale);
                    var scaleSource = new ChaseScaleTweenSource
                    {
                        GhostEntity = actionEntity,
                        Space = binding.Space
                    };
                    if (scaleSourceLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, scaleSource);
                    else ecb.AddComponent(binding.TargetEntity, scaleSource);
                    break;
                case TweenType.ScaleToUniform:
                    var uniformScale = new ChaseScale
                    {
                        TargetScale = float3.zero,
                        Velocity = float3.zero,
                        IsUniform = true,
                        Mode = binding.ChaseMode,
                        SmoothTime = binding.ChaseSmoothTime,
                        MaxSpeed = binding.ChaseMaxSpeed,
                        KillOnChase = !binding.UseChase || binding.KillOnChase
                    };
                    if (scaleLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, uniformScale);
                    else ecb.AddComponent(binding.TargetEntity, uniformScale);
                    var uniformScaleSource = new ChaseScaleTweenSource
                    {
                        GhostEntity = actionEntity,
                        Space = binding.Space
                    };
                    if (scaleSourceLookup.HasComponent(binding.TargetEntity)) ecb.SetComponent(binding.TargetEntity, uniformScaleSource);
                    else ecb.AddComponent(binding.TargetEntity, uniformScaleSource);
                    break;
            }
        }

        private static void CleanupElement(SequenceElement element,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<Look> lookLookup,
            ComponentLookup<ChaseScale> scaleLookup)
        {
            if (controlLookup.HasComponent(element.ActionEntity))
                controlLookup.SetComponentEnabled(element.ActionEntity, false);

            if (positionLookup.HasComponent(element.ActionEntity))
                positionLookup.SetComponentEnabled(element.ActionEntity, false);
            if (rotationLookup.HasComponent(element.ActionEntity))
                rotationLookup.SetComponentEnabled(element.ActionEntity, false);
            if (lookLookup.HasComponent(element.ActionEntity))
                lookLookup.SetComponentEnabled(element.ActionEntity, false);
            if (scaleLookup.HasComponent(element.ActionEntity))
                scaleLookup.SetComponentEnabled(element.ActionEntity, false);
        }

        private static void ResolveStartFromCurrent(Entity actionEntity, SequenceTweenBinding binding,
            ComponentLookup<TweenValue<float>> valueFloatLookup,
            ComponentLookup<TweenValue<float3>> valueFloat3Lookup,
            ComponentLookup<TweenValue<quaternion>> valueQuatLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            if (!binding.StartFromCurrent) return;
            if (!localTransformLookup.TryGetComponent(binding.TargetEntity, out var transform)) return;

            if (binding.TweenType == TweenType.MoveTo && valueFloat3Lookup.HasComponent(actionEntity))
            {
                var start = binding.Space == TweenSpace.World && localToWorldLookup.TryGetComponent(binding.TargetEntity, out var ltw)
                    ? ltw.Position
                    : transform.Position;
                var value = valueFloat3Lookup[actionEntity];
                value.StartPoint = start;
                value.CurrentValue = start;
                valueFloat3Lookup[actionEntity] = value;
            }
            else if (binding.TweenType == TweenType.RotateTo && valueQuatLookup.HasComponent(actionEntity))
            {
                var start = binding.Space == TweenSpace.World && localToWorldLookup.TryGetComponent(binding.TargetEntity, out var ltw)
                    ? ltw.Rotation
                    : transform.Rotation;
                var value = valueQuatLookup[actionEntity];
                value.StartPoint = start;
                value.CurrentValue = start;
                valueQuatLookup[actionEntity] = value;
            }
            else if (binding.TweenType == TweenType.ScaleTo && valueFloat3Lookup.HasComponent(actionEntity))
            {
                var start = new float3(transform.Scale);
                var value = valueFloat3Lookup[actionEntity];
                value.StartPoint = start;
                value.CurrentValue = start;
                valueFloat3Lookup[actionEntity] = value;
            }
            else if (binding.TweenType == TweenType.ScaleToUniform && valueFloatLookup.HasComponent(actionEntity))
            {
                var value = valueFloatLookup[actionEntity];
                value.StartPoint = transform.Scale;
                value.CurrentValue = transform.Scale;
                valueFloatLookup[actionEntity] = value;
            }
        }

        private static bool HasReached(int direction, float time, float target)
        {
            return direction >= 0 ? time >= target : time <= target;
        }

        private static void EmitInstantCallbacks(Entity sequenceEntity, DynamicBuffer<SequenceElement> elements, EntityCommandBuffer ecb)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                if (element.Kind != TimelineActionKind.Callback || element.Started) continue;

                var callbackEntity = ecb.CreateEntity();
                ecb.AddComponent(callbackEntity, new SequenceCallbackEvent { SequenceEntity = sequenceEntity, CallbackId = element.CallbackId });
                element.Started = true;
                element.Completed = true;
                elements[i] = element;
            }
        }
    }
}
