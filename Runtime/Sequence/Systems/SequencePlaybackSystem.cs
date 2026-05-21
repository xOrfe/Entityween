using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Curve;

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
            var valueFloatLookup = SystemAPI.GetComponentLookup<TweenValue<float>>(false);
            var valueFloat3Lookup = SystemAPI.GetComponentLookup<TweenValue<float3>>(false);
            var valueQuatLookup = SystemAPI.GetComponentLookup<TweenValue<quaternion>>(false);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var positionLookup = SystemAPI.GetComponentLookup<ChasePosition>(true);
            var rotationLookup = SystemAPI.GetComponentLookup<ChaseRotation>(true);
            var scaleLookup = SystemAPI.GetComponentLookup<ChaseScale>(true);
            var positionSourceLookup = SystemAPI.GetComponentLookup<ChasePositionTweenSource>(true);
            var rotationSourceLookup = SystemAPI.GetComponentLookup<ChaseRotationTweenSource>(true);
            var scaleSourceLookup = SystemAPI.GetComponentLookup<ChaseScaleTweenSource>(true);
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

                var sequenceDeltaTime = PlaybackUtilities.GetDeltaTime(progress.TimeType, deltaTime, unscaledDeltaTime);
                sequence.Time += sequenceDeltaTime * math.max(0f, sequence.TimeScale);

                bool hasLoop = SystemAPI.HasComponent<PlaybackLoop>(sequenceEntity);
                var loop = hasLoop ? SystemAPI.GetComponent<PlaybackLoop>(sequenceEntity) : default;
                float normalizedTime;
                bool isFinished = PlaybackUtilities.CalculateProgress(
                    ref sequence.Time,
                    sequence.Duration,
                    ref loop,
                    hasLoop,
                    out normalizedTime);

                if (hasLoop)
                {
                    var oldLoop = SystemAPI.GetComponent<PlaybackLoop>(sequenceEntity);
                    if (loop.LoopIndex != oldLoop.LoopIndex)
                    {
                        var loopDelta = loop.LoopIndex - oldLoop.LoopIndex;
                        if (loop.LoopType == LoopType.PingPong && (loopDelta & 1) == 1)
                            sequence.Direction *= -1;

                        for (int i = 0; i < elements.Length; i++)
                        {
                            var element = elements[i];
                            CleanupElement(element, controlLookup);
                            element.Started = false;
                            element.Completed = false;
                            elements[i] = element;
                        }
                    }
                    SystemAPI.SetComponent(sequenceEntity, loop);
                }

                progressRef.ValueRW.NormalizedTime = normalizedTime;

                var effectiveTime = sequence.Direction >= 0 ? sequence.Time : sequence.Duration - sequence.Time;
                effectiveTime = math.clamp(effectiveTime, 0f, sequence.Duration);

                for (int i = 0; i < elements.Length; i++)
                {
                    var element = elements[i];
                    ProcessElement(sequenceEntity, sequence.Direction, effectiveTime, ref element, ecb, controlLookup, progressLookup,
                        valueFloatLookup, valueFloat3Lookup, valueQuatLookup, localTransformLookup, localToWorldLookup,
                        positionLookup, rotationLookup, scaleLookup, positionSourceLookup, rotationSourceLookup, scaleSourceLookup);
                    elements[i] = element;
                }

                if (isFinished)
                    sequence.State = PlaybackState.Completed;

                sequenceRef.ValueRW = sequence;
            }
        }

        private static void ProcessElement(Entity sequenceEntity, int direction, float effectiveTime, ref SequenceElement element,
            EntityCommandBuffer ecb,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<PlaybackProgress> progressLookup,
            ComponentLookup<TweenValue<float>> valueFloatLookup,
            ComponentLookup<TweenValue<float3>> valueFloat3Lookup,
            ComponentLookup<TweenValue<quaternion>> valueQuatLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<ChaseScale> scaleLookup,
            ComponentLookup<ChasePositionTweenSource> positionSourceLookup,
            ComponentLookup<ChaseRotationTweenSource> rotationSourceLookup,
            ComponentLookup<ChaseScaleTweenSource> scaleSourceLookup)
        {
            if (element.Completed)
            {
                CleanupElement(element, controlLookup);
                return;
            }

            if (element.Kind == SequenceElementKind.Callback)
            {
                if (!element.Started && HasReached(direction, effectiveTime, element.StartTime))
                {
                    var callbackEntity = ecb.CreateEntity();
                    ecb.AddComponent(callbackEntity, new SequenceCallbackEvent { SequenceEntity = sequenceEntity, CallbackId = element.CallbackId });
                    element.Started = true;
                    element.Completed = true;
                }
                return;
            }

            if (element.Kind == SequenceElementKind.Wait)
            {
                var waitEnd = direction >= 0 ? element.StartTime + element.Duration : element.StartTime;
                if (HasReached(direction, effectiveTime, waitEnd))
                    element.Completed = true;
                return;
            }

            if (!controlLookup.HasComponent(element.GhostEntity)) return;

            var start = element.StartTime;
            var end = element.StartTime + element.Duration;
            var reachedStart = direction >= 0 ? effectiveTime >= start : effectiveTime <= end;
            if (!element.Started && !reachedStart) return;

            if (!element.Started)
            {
                ResolveStartFromCurrent(element, valueFloatLookup, valueFloat3Lookup, valueQuatLookup, localTransformLookup, localToWorldLookup);
                BindElement(element, ecb, positionLookup, rotationLookup, scaleLookup,
                    positionSourceLookup, rotationSourceLookup, scaleSourceLookup);
                element.Started = true;
            }

            var localElapsed = math.clamp(effectiveTime - start, 0f, element.Duration);
            if (direction < 0)
                localElapsed = math.clamp(effectiveTime - start, 0f, element.Duration);

            StartOrUpdateTween(element, localElapsed, controlLookup, progressLookup);

            var reachedEnd = direction >= 0 ? effectiveTime >= end : effectiveTime <= start;
            if (reachedEnd)
                element.Completed = true;
        }

        private static void StartOrUpdateTween(SequenceElement element, float elapsedTime,
            ComponentLookup<TweenControl> controlLookup,
            ComponentLookup<PlaybackProgress> progressLookup)
        {
            var control = controlLookup[element.GhostEntity];
            control.ElapsedTime = elapsedTime;
            control.AutoKill = false;
            control.Completed = false;
            controlLookup[element.GhostEntity] = control;
            controlLookup.SetComponentEnabled(element.GhostEntity, true);

            if (progressLookup.HasComponent(element.GhostEntity))
            {
                var progress = progressLookup[element.GhostEntity];
                progress.NormalizedTime = element.Duration > 0f ? math.saturate(elapsedTime / element.Duration) : 1f;
                progressLookup[element.GhostEntity] = progress;
            }
        }

        private static void BindElement(SequenceElement element,
            EntityCommandBuffer ecb,
            ComponentLookup<ChasePosition> positionLookup,
            ComponentLookup<ChaseRotation> rotationLookup,
            ComponentLookup<ChaseScale> scaleLookup,
            ComponentLookup<ChasePositionTweenSource> positionSourceLookup,
            ComponentLookup<ChaseRotationTweenSource> rotationSourceLookup,
            ComponentLookup<ChaseScaleTweenSource> scaleSourceLookup)
        {
            switch (element.TweenType)
            {
                case TweenType.MoveTo:
                    var position = new ChasePosition
                    {
                        TargetPosition = float3.zero,
                        Velocity = float3.zero,
                        Space = element.Space,
                        Mode = element.ChaseMode,
                        SmoothTime = element.ChaseSmoothTime,
                        MaxSpeed = element.ChaseMaxSpeed,
                        KillOnChase = !element.UseChase || element.KillOnChase
                    };
                    if (positionLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, position);
                    else ecb.AddComponent(element.TargetEntity, position);
                    var positionSource = new ChasePositionTweenSource
                    {
                        GhostEntity = element.GhostEntity,
                        Space = element.Space
                    };
                    if (positionSourceLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, positionSource);
                    else ecb.AddComponent(element.TargetEntity, positionSource);
                    break;
                case TweenType.RotateTo:
                    var rotation = new ChaseRotation
                    {
                        TargetQuaternion = quaternion.identity,
                        Velocity = new quaternion(0f, 0f, 0f, 0f),
                        Space = element.Space,
                        Mode = element.ChaseMode,
                        SmoothTime = element.ChaseSmoothTime,
                        MaxSpeed = element.ChaseMaxSpeed,
                        KillOnChase = !element.UseChase || element.KillOnChase
                    };
                    if (rotationLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, rotation);
                    else ecb.AddComponent(element.TargetEntity, rotation);
                    var rotationSource = new ChaseRotationTweenSource
                    {
                        GhostEntity = element.GhostEntity,
                        Space = element.Space
                    };
                    if (rotationSourceLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, rotationSource);
                    else ecb.AddComponent(element.TargetEntity, rotationSource);
                    break;
                case TweenType.ScaleTo:
                    var scale = new ChaseScale
                    {
                        TargetScale = float3.zero,
                        Velocity = float3.zero,
                        IsUniform = false,
                        Mode = element.ChaseMode,
                        SmoothTime = element.ChaseSmoothTime,
                        MaxSpeed = element.ChaseMaxSpeed,
                        KillOnChase = !element.UseChase || element.KillOnChase
                    };
                    if (scaleLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, scale);
                    else ecb.AddComponent(element.TargetEntity, scale);
                    var scaleSource = new ChaseScaleTweenSource
                    {
                        GhostEntity = element.GhostEntity,
                        Space = element.Space
                    };
                    if (scaleSourceLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, scaleSource);
                    else ecb.AddComponent(element.TargetEntity, scaleSource);
                    break;
                case TweenType.ScaleToUniform:
                    var uniformScale = new ChaseScale
                    {
                        TargetScale = float3.zero,
                        Velocity = float3.zero,
                        IsUniform = true,
                        Mode = element.ChaseMode,
                        SmoothTime = element.ChaseSmoothTime,
                        MaxSpeed = element.ChaseMaxSpeed,
                        KillOnChase = !element.UseChase || element.KillOnChase
                    };
                    if (scaleLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, uniformScale);
                    else ecb.AddComponent(element.TargetEntity, uniformScale);
                    var uniformScaleSource = new ChaseScaleTweenSource
                    {
                        GhostEntity = element.GhostEntity,
                        Space = element.Space
                    };
                    if (scaleSourceLookup.HasComponent(element.TargetEntity)) ecb.SetComponent(element.TargetEntity, uniformScaleSource);
                    else ecb.AddComponent(element.TargetEntity, uniformScaleSource);
                    break;
            }
        }

        private static void CleanupElement(SequenceElement element, ComponentLookup<TweenControl> controlLookup)
        {
            if (controlLookup.HasComponent(element.GhostEntity))
                controlLookup.SetComponentEnabled(element.GhostEntity, false);
        }

        private static void ResolveStartFromCurrent(SequenceElement element,
            ComponentLookup<TweenValue<float>> valueFloatLookup,
            ComponentLookup<TweenValue<float3>> valueFloat3Lookup,
            ComponentLookup<TweenValue<quaternion>> valueQuatLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            if (!element.StartFromCurrent) return;
            if (!localTransformLookup.TryGetComponent(element.TargetEntity, out var transform)) return;

            if (element.TweenType == TweenType.MoveTo && valueFloat3Lookup.HasComponent(element.GhostEntity))
            {
                var start = element.Space == TweenSpace.World && localToWorldLookup.TryGetComponent(element.TargetEntity, out var ltw)
                    ? ltw.Position
                    : transform.Position;
                var value = valueFloat3Lookup[element.GhostEntity];
                value.StartPoint = start;
                value.CurrentValue = start;
                valueFloat3Lookup[element.GhostEntity] = value;
            }
            else if (element.TweenType == TweenType.RotateTo && valueQuatLookup.HasComponent(element.GhostEntity))
            {
                var start = element.Space == TweenSpace.World && localToWorldLookup.TryGetComponent(element.TargetEntity, out var ltw)
                    ? ltw.Rotation
                    : transform.Rotation;
                var value = valueQuatLookup[element.GhostEntity];
                value.StartPoint = start;
                value.CurrentValue = start;
                valueQuatLookup[element.GhostEntity] = value;
            }
            else if (element.TweenType == TweenType.ScaleTo && valueFloat3Lookup.HasComponent(element.GhostEntity))
            {
                var start = new float3(transform.Scale);
                var value = valueFloat3Lookup[element.GhostEntity];
                value.StartPoint = start;
                value.CurrentValue = start;
                valueFloat3Lookup[element.GhostEntity] = value;
            }
            else if (element.TweenType == TweenType.ScaleToUniform && valueFloatLookup.HasComponent(element.GhostEntity))
            {
                var value = valueFloatLookup[element.GhostEntity];
                value.StartPoint = transform.Scale;
                value.CurrentValue = transform.Scale;
                valueFloatLookup[element.GhostEntity] = value;
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
                if (element.Kind != SequenceElementKind.Callback || element.Started) continue;

                var callbackEntity = ecb.CreateEntity();
                ecb.AddComponent(callbackEntity, new SequenceCallbackEvent { SequenceEntity = sequenceEntity, CallbackId = element.CallbackId });
                element.Started = true;
                element.Completed = true;
                elements[i] = element;
            }
        }
    }
}
