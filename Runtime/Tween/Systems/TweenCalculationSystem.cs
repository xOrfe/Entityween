using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using XO.Curve;

namespace XO.Entityween
{

    [BurstCompile]
    [UpdateInGroup(typeof(EntityweenTweenGroup))]
    [RequireMatchingQueriesForUpdate]
    internal partial struct TweenCalculationSystem : ISystem
    {
        private EntityQuery _queryFloat;
        private EntityQuery _queryFloat2;
        private EntityQuery _queryFloat3;
        private EntityQuery _queryQuat;
        private EntityQuery _resolvePositionQuery;
        private EntityQuery _resolveRotationQuery;
        private EntityQuery _resolveLookQuery;
        private EntityQuery _resolveScaleQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryFloat = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenValue<float>>().Build();
            _queryFloat2 = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenValue<float2>>().Build();
            _queryFloat3 = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenValue<float3>>().Build();
            _queryQuat = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenValue<quaternion>>().Build();

            _resolvePositionQuery = SystemAPI.QueryBuilder().WithAllRW<ChasePosition, ChasePositionTweenSource>().Build();
            _resolveRotationQuery = SystemAPI.QueryBuilder().WithAllRW<ChaseRotation, ChaseRotationTweenSource>().Build();
            _resolveLookQuery = SystemAPI.QueryBuilder().WithAllRW<Look, LookTweenSource>().Build();
            _resolveScaleQuery = SystemAPI.QueryBuilder().WithAllRW<ChaseScale, ChaseScaleTweenSource>().Build();

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var unscaledDt = UnityEngine.Time.unscaledDeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var entityType = SystemAPI.GetEntityTypeHandle();
            var controlHandle = SystemAPI.GetComponentTypeHandle<TweenControl>();
            var progressHandle = SystemAPI.GetComponentTypeHandle<PlaybackProgress>();
            var sequenceDrivenHandle = SystemAPI.GetComponentTypeHandle<TweenSequenceDriven>(true);
            var splineStateHandle = SystemAPI.GetComponentTypeHandle<SplineState>(true);
            var splineBlobRefFloatHandle = SystemAPI.GetComponentTypeHandle<SplineBlobRef<float>>(true);
            var splineBlobRefFloat2Handle = SystemAPI.GetComponentTypeHandle<SplineBlobRef<float2>>(true);
            var splineBlobRefFloat3Handle = SystemAPI.GetComponentTypeHandle<SplineBlobRef<float3>>(true);
            var splineBlobRefQuatHandle = SystemAPI.GetComponentTypeHandle<SplineBlobRef<quaternion>>(true);

            if (!_queryFloat.IsEmptyIgnoreFilter)
            {
                state.Dependency = new TweenCalculationJob<float, FloatMath>
                {
                    DeltaTime = dt,
                    UnscaledDeltaTime = unscaledDt,
                    Ecb = ecb,
                    Math = default,
                    EntityType = entityType,
                    ControlHandle = controlHandle,
                    ProgressHandle = progressHandle,
                    SequenceDrivenHandle = sequenceDrivenHandle,
                    SplineStateHandle = splineStateHandle,
                    SplineBlobRefHandle = splineBlobRefFloatHandle,
                    ValueHandle = SystemAPI.GetComponentTypeHandle<TweenValue<float>>(),
                    SplineHandle = SystemAPI.GetBufferTypeHandle<SplineElement<float>>(true)
                }.ScheduleParallel(_queryFloat, state.Dependency);
            }

            if (!_queryFloat2.IsEmptyIgnoreFilter)
            {
                state.Dependency = new TweenCalculationJob<float2, Float2Math>
                {
                    DeltaTime = dt,
                    UnscaledDeltaTime = unscaledDt,
                    Ecb = ecb,
                    Math = default,
                    EntityType = entityType,
                    ControlHandle = controlHandle,
                    ProgressHandle = progressHandle,
                    SequenceDrivenHandle = sequenceDrivenHandle,
                    SplineStateHandle = splineStateHandle,
                    SplineBlobRefHandle = splineBlobRefFloat2Handle,
                    ValueHandle = SystemAPI.GetComponentTypeHandle<TweenValue<float2>>(),
                    SplineHandle = SystemAPI.GetBufferTypeHandle<SplineElement<float2>>(true)
                }.ScheduleParallel(_queryFloat2, state.Dependency);
            }

            if (!_queryFloat3.IsEmptyIgnoreFilter)
            {
                state.Dependency = new TweenCalculationJob<float3, Float3Math>
                {
                    DeltaTime = dt,
                    UnscaledDeltaTime = unscaledDt,
                    Ecb = ecb,
                    Math = default,
                    EntityType = entityType,
                    ControlHandle = controlHandle,
                    ProgressHandle = progressHandle,
                    SequenceDrivenHandle = sequenceDrivenHandle,
                    SplineStateHandle = splineStateHandle,
                    SplineBlobRefHandle = splineBlobRefFloat3Handle,
                    ValueHandle = SystemAPI.GetComponentTypeHandle<TweenValue<float3>>(),
                    SplineHandle = SystemAPI.GetBufferTypeHandle<SplineElement<float3>>(true)
                }.ScheduleParallel(_queryFloat3, state.Dependency);
            }

            if (!_queryQuat.IsEmptyIgnoreFilter)
            {
                state.Dependency = new TweenCalculationJob<quaternion, QuaternionMath>
                {
                    DeltaTime = dt,
                    UnscaledDeltaTime = unscaledDt,
                    Ecb = ecb,
                    Math = default,
                    EntityType = entityType,
                    ControlHandle = controlHandle,
                    ProgressHandle = progressHandle,
                    SequenceDrivenHandle = sequenceDrivenHandle,
                    SplineStateHandle = splineStateHandle,
                    SplineBlobRefHandle = splineBlobRefQuatHandle,
                    ValueHandle = SystemAPI.GetComponentTypeHandle<TweenValue<quaternion>>(),
                    SplineHandle = SystemAPI.GetBufferTypeHandle<SplineElement<quaternion>>(true)
                }.ScheduleParallel(_queryQuat, state.Dependency);
            }

            var controlLookup = SystemAPI.GetComponentLookup<TweenControl>(true);
            var valueFloatLookup = SystemAPI.GetComponentLookup<TweenValue<float>>(true);
            var valueFloat3Lookup = SystemAPI.GetComponentLookup<TweenValue<float3>>(true);
            var valueQuatLookup = SystemAPI.GetComponentLookup<TweenValue<quaternion>>(true);

            if (!_resolvePositionQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseResolvePositionJob
                {
                    ValueFloat3Lookup = valueFloat3Lookup,
                    ControlLookup = controlLookup,
                    Ecb = ecb,
                    ChaseHandle = SystemAPI.GetComponentTypeHandle<ChasePosition>(),
                    SourceHandle = SystemAPI.GetComponentTypeHandle<ChasePositionTweenSource>(),
                    EntityHandle = entityType
                }.ScheduleParallel(_resolvePositionQuery, state.Dependency);
            }

            if (!_resolveRotationQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseResolveRotationJob
                {
                    ValueQuatLookup = valueQuatLookup,
                    ControlLookup = controlLookup,
                    Ecb = ecb,
                    ChaseHandle = SystemAPI.GetComponentTypeHandle<ChaseRotation>(),
                    SourceHandle = SystemAPI.GetComponentTypeHandle<ChaseRotationTweenSource>(),
                    EntityHandle = entityType
                }.ScheduleParallel(_resolveRotationQuery, state.Dependency);
            }

            if (!_resolveLookQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseResolveLookJob
                {
                    ValueFloat3Lookup = valueFloat3Lookup,
                    ControlLookup = controlLookup,
                    Ecb = ecb,
                    LookHandle = SystemAPI.GetComponentTypeHandle<Look>(),
                    SourceHandle = SystemAPI.GetComponentTypeHandle<LookTweenSource>(),
                    EntityHandle = entityType
                }.ScheduleParallel(_resolveLookQuery, state.Dependency);
            }

            if (!_resolveScaleQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseResolveScaleJob
                {
                    ValueFloatLookup = valueFloatLookup,
                    ValueFloat3Lookup = valueFloat3Lookup,
                    ControlLookup = controlLookup,
                    Ecb = ecb,
                    ChaseHandle = SystemAPI.GetComponentTypeHandle<ChaseScale>(),
                    SourceHandle = SystemAPI.GetComponentTypeHandle<ChaseScaleTweenSource>(),
                    EntityHandle = entityType
                }.ScheduleParallel(_resolveScaleQuery, state.Dependency);
            }
        }
    }

    [BurstCompile]
    internal struct TweenCalculationJob<T, TMath> : IJobChunk where T : unmanaged where TMath : struct, ICurveMath<T>
    {
        public float DeltaTime;
        public float UnscaledDeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public TMath Math;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<TweenControl> ControlHandle;
        public ComponentTypeHandle<PlaybackProgress> ProgressHandle;

        [ReadOnly] public ComponentTypeHandle<TweenSequenceDriven> SequenceDrivenHandle;
        [ReadOnly] public ComponentTypeHandle<SplineState> SplineStateHandle;
        [ReadOnly] public ComponentTypeHandle<SplineBlobRef<T>> SplineBlobRefHandle;

        public ComponentTypeHandle<TweenValue<T>> ValueHandle;
        [ReadOnly] public BufferTypeHandle<SplineElement<T>> SplineHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityType);
            var controls = chunk.GetNativeArray(ref ControlHandle);
            var progresses = chunk.GetNativeArray(ref ProgressHandle);
            var values = chunk.GetNativeArray(ref ValueHandle);

            var isSequenceDriven = chunk.Has(ref SequenceDrivenHandle);
            var hasSplineState = chunk.Has(ref SplineStateHandle);
            var hasBlobSpline = chunk.Has(ref SplineBlobRefHandle);

            var splineStates = hasSplineState ? chunk.GetNativeArray(ref SplineStateHandle) : default;
            var splineAccessor = hasSplineState ? chunk.GetBufferAccessor(ref SplineHandle) : default;
            var blobSplines = hasBlobSpline ? chunk.GetNativeArray(ref SplineBlobRefHandle) : default;

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out int i))
            {
                var control = controls[i];
                var progress = progresses[i];
                var value = values[i];

                var timeDir = progress.Direction == 0 ? 1 : progress.Direction;
                if (!isSequenceDriven)
                    control.ElapsedTime += PlaybackUtilities.GetDeltaTime(progress.TimeType, DeltaTime, UnscaledDeltaTime) * timeDir;

                PlaybackUtilities.CalculateProgress(
                    ref control.ElapsedTime,
                    control.SecondsToPlay,
                    ref progress,
                    out var normalizedTime,
                    out var isFinished);

                control.Completed = isFinished;
                progress.NormalizedTime = normalizedTime;

                float easedT;
                if (progress.LoopType == LoopType.PingPong && progress.LoopEaseMode == LoopEaseMode.Repeat && (progress.LoopIndex % 2 == 1))
                {
                    var progressValue = 1f - normalizedTime;
                    var easedTForward = Ease.EasedT(progressValue, control.EaseType);
                    easedT = 1f - easedTForward;
                }
                else
                {
                    easedT = Ease.EasedT(normalizedTime, control.EaseType);
                }

                if (hasBlobSpline)
                {
                    var spline = blobSplines[i];
                    var blobRef = spline.Blob;
                    if (blobRef.IsCreated)
                    {
                        var provider = new Spline.BlobSplineAdapter<T>(blobRef);
                        var sample = Spline.SampleGeneric<T, Spline.BlobSplineAdapter<T>, TMath>(ref provider, easedT, Math);
                        value.CurrentValue = spline.IsBend && provider.KnotCount >= 2
                            ? Math.Bend(
                                value.StartPoint,
                                value.EndPoint,
                                provider.GetKnot(0),
                                provider.GetKnot(provider.KnotCount - 1),
                                sample,
                                easedT)
                            : sample;
                    }
                    else
                    {
                        value.CurrentValue = Math.Lerp(value.StartPoint, value.EndPoint, easedT);
                    }
                }
                else if (hasSplineState && splineAccessor.Length > 0)
                {
                    var provider = new Spline.BufferSplineAdapter<T>(splineStates[i], splineAccessor[i]);
                    value.CurrentValue = Spline.SampleGeneric<T, Spline.BufferSplineAdapter<T>, TMath>(ref provider, easedT, Math);
                }
                else
                {
                    value.CurrentValue = Math.Lerp(value.StartPoint, value.EndPoint, easedT);
                }

                controls[i] = control;
                progresses[i] = progress;
                values[i] = value;

                if (isFinished)
                {
                    if (control.AutoKill) Ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
                    else Ecb.SetComponentEnabled<TweenControl>(unfilteredChunkIndex, entities[i], false);
                }
            }
        }
    }

    [BurstCompile]
    internal struct ChaseResolvePositionJob : IJobChunk
    {
        [ReadOnly] public ComponentLookup<TweenValue<float3>> ValueFloat3Lookup;
        [ReadOnly] public ComponentLookup<TweenControl> ControlLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public ComponentTypeHandle<ChasePosition> ChaseHandle;
        public ComponentTypeHandle<ChasePositionTweenSource> SourceHandle;
        public EntityTypeHandle EntityHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var chasePositions = chunk.GetNativeArray(ref ChaseHandle);
            var sources = chunk.GetNativeArray(ref SourceHandle);
            var entities = chunk.GetNativeArray(EntityHandle);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out int i))
            {
                var source = sources[i];
                var ghost = source.GhostEntity;

                bool sourceCompleted = source.SourceCompleted ||
                    (ControlLookup.TryGetComponent(ghost, out var ctrl) && ctrl.Completed);

                if (ValueFloat3Lookup.TryGetComponent(ghost, out var value))
                {
                    var cp = chasePositions[i];
                    cp.TargetPosition = value.CurrentValue;
                    cp.Space = source.Space;
                    chasePositions[i] = cp;
                }

                if (sourceCompleted && !chasePositions[i].KillOnChase)
                {
                    Ecb.RemoveComponent<ChasePositionTweenSource>(unfilteredChunkIndex, entities[i]);
                }
                else
                {
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
            }
        }
    }

    [BurstCompile]
    internal struct ChaseResolveRotationJob : IJobChunk
    {
        [ReadOnly] public ComponentLookup<TweenValue<quaternion>> ValueQuatLookup;
        [ReadOnly] public ComponentLookup<TweenControl> ControlLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public ComponentTypeHandle<ChaseRotation> ChaseHandle;
        public ComponentTypeHandle<ChaseRotationTweenSource> SourceHandle;
        public EntityTypeHandle EntityHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var chaseRotations = chunk.GetNativeArray(ref ChaseHandle);
            var sources = chunk.GetNativeArray(ref SourceHandle);
            var entities = chunk.GetNativeArray(EntityHandle);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out int i))
            {
                var source = sources[i];
                var ghost = source.GhostEntity;

                bool sourceCompleted = source.SourceCompleted ||
                    (ControlLookup.TryGetComponent(ghost, out var ctrl) && ctrl.Completed);

                if (ValueQuatLookup.TryGetComponent(ghost, out var value))
                {
                    var cr = chaseRotations[i];
                    cr.TargetQuaternion = value.CurrentValue;
                    cr.Space = source.Space;
                    chaseRotations[i] = cr;
                }

                if (sourceCompleted && !chaseRotations[i].KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseRotationTweenSource>(unfilteredChunkIndex, entities[i]);
                }
                else
                {
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
            }
        }
    }

    [BurstCompile]
    internal struct ChaseResolveLookJob : IJobChunk
    {
        [ReadOnly] public ComponentLookup<TweenValue<float3>> ValueFloat3Lookup;
        [ReadOnly] public ComponentLookup<TweenControl> ControlLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public ComponentTypeHandle<Look> LookHandle;
        public ComponentTypeHandle<LookTweenSource> SourceHandle;
        public EntityTypeHandle EntityHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var looks = chunk.GetNativeArray(ref LookHandle);
            var sources = chunk.GetNativeArray(ref SourceHandle);
            var entities = chunk.GetNativeArray(EntityHandle);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out int i))
            {
                var source = sources[i];
                var ghost = source.GhostEntity;

                bool sourceCompleted = source.SourceCompleted ||
                    (ControlLookup.TryGetComponent(ghost, out var ctrl) && ctrl.Completed);

                if (ValueFloat3Lookup.TryGetComponent(ghost, out var value))
                {
                    var lk = looks[i];
                    lk.TargetPosition = value.CurrentValue;
                    looks[i] = lk;
                }

                if (sourceCompleted && !looks[i].KillOnChase)
                {
                    Ecb.RemoveComponent<LookTweenSource>(unfilteredChunkIndex, entities[i]);
                }
                else
                {
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
            }
        }
    }

    [BurstCompile]
    internal struct ChaseResolveScaleJob : IJobChunk
    {
        [ReadOnly] public ComponentLookup<TweenValue<float>> ValueFloatLookup;
        [ReadOnly] public ComponentLookup<TweenValue<float3>> ValueFloat3Lookup;
        [ReadOnly] public ComponentLookup<TweenControl> ControlLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public ComponentTypeHandle<ChaseScale> ChaseHandle;
        public ComponentTypeHandle<ChaseScaleTweenSource> SourceHandle;
        public EntityTypeHandle EntityHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var chaseScales = chunk.GetNativeArray(ref ChaseHandle);
            var sources = chunk.GetNativeArray(ref SourceHandle);
            var entities = chunk.GetNativeArray(EntityHandle);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out int i))
            {
                var source = sources[i];
                var ghost = source.GhostEntity;

                bool sourceCompleted = source.SourceCompleted ||
                    (ControlLookup.TryGetComponent(ghost, out var ctrl) && ctrl.Completed);

                if (chaseScales[i].IsUniform && ValueFloatLookup.TryGetComponent(ghost, out var uniformValue))
                {
                    var cs = chaseScales[i];
                    cs.TargetScale = new float3(uniformValue.CurrentValue);
                    chaseScales[i] = cs;
                }
                else if (ValueFloat3Lookup.TryGetComponent(ghost, out var value))
                {
                    var cs = chaseScales[i];
                    cs.TargetScale = value.CurrentValue;
                    chaseScales[i] = cs;
                }

                if (sourceCompleted && !chaseScales[i].KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseScaleTweenSource>(unfilteredChunkIndex, entities[i]);
                }
                else
                {
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
            }
        }
    }
}
