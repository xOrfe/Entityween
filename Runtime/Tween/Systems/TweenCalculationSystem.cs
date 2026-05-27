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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryFloat = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenRange<float>, TweenRuntime<float>>().Build();
            _queryFloat2 = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenRange<float2>, TweenRuntime<float2>>().Build();
            _queryFloat3 = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenRange<float3>, TweenRuntime<float3>>().Build();
            _queryQuat = SystemAPI.QueryBuilder().WithAll<TweenControl, PlaybackProgress, TweenRange<quaternion>, TweenRuntime<quaternion>>().Build();

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var unscaledDt = UnityEngine.Time.unscaledDeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var entityType = SystemAPI.GetEntityTypeHandle();
            var controlHandle = SystemAPI.GetComponentTypeHandle<TweenControl>(true);
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
                    RangeHandle = SystemAPI.GetComponentTypeHandle<TweenRange<float>>(true),
                    RuntimeHandle = SystemAPI.GetComponentTypeHandle<TweenRuntime<float>>(),
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
                    RangeHandle = SystemAPI.GetComponentTypeHandle<TweenRange<float2>>(true),
                    RuntimeHandle = SystemAPI.GetComponentTypeHandle<TweenRuntime<float2>>(),
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
                    RangeHandle = SystemAPI.GetComponentTypeHandle<TweenRange<float3>>(true),
                    RuntimeHandle = SystemAPI.GetComponentTypeHandle<TweenRuntime<float3>>(),
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
                    RangeHandle = SystemAPI.GetComponentTypeHandle<TweenRange<quaternion>>(true),
                    RuntimeHandle = SystemAPI.GetComponentTypeHandle<TweenRuntime<quaternion>>(),
                    SplineHandle = SystemAPI.GetBufferTypeHandle<SplineElement<quaternion>>(true)
                }.ScheduleParallel(_queryQuat, state.Dependency);
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
        [ReadOnly] public ComponentTypeHandle<TweenControl> ControlHandle;
        public ComponentTypeHandle<PlaybackProgress> ProgressHandle;

        [ReadOnly] public ComponentTypeHandle<TweenSequenceDriven> SequenceDrivenHandle;
        [ReadOnly] public ComponentTypeHandle<SplineState> SplineStateHandle;
        [ReadOnly] public ComponentTypeHandle<SplineBlobRef<T>> SplineBlobRefHandle;

        [ReadOnly] public ComponentTypeHandle<TweenRange<T>> RangeHandle;
        public ComponentTypeHandle<TweenRuntime<T>> RuntimeHandle;
        [ReadOnly] public BufferTypeHandle<SplineElement<T>> SplineHandle;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityType);
            var controls = chunk.GetNativeArray(ref ControlHandle);
            var progresses = chunk.GetNativeArray(ref ProgressHandle);
            var ranges = chunk.GetNativeArray(ref RangeHandle);
            var runtimes = chunk.GetNativeArray(ref RuntimeHandle);

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
                var range = ranges[i];
                var runtime = runtimes[i];

                var timeDir = progress.Direction == 0 ? 1 : progress.Direction;
                if (!isSequenceDriven)
                    progress.ElapsedTime += PlaybackUtilities.GetDeltaTime(progress.TimeType, DeltaTime, UnscaledDeltaTime) * timeDir;

                PlaybackUtilities.CalculateProgress(
                    ref progress.ElapsedTime,
                    control.SecondsToPlay,
                    ref progress,
                    out var normalizedTime,
                    out var isFinished);

                runtime.Completed = isFinished;
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
                        runtime.CurrentValue = spline.IsBend && provider.KnotCount >= 2
                            ? Math.Bend(
                                range.StartPoint,
                                range.EndPoint,
                                provider.GetKnot(0),
                                provider.GetKnot(provider.KnotCount - 1),
                                sample,
                                easedT)
                            : sample;
                    }
                    else
                    {
                        runtime.CurrentValue = Math.Lerp(range.StartPoint, range.EndPoint, easedT);
                    }
                }
                else if (hasSplineState && splineAccessor.Length > 0)
                {
                    var provider = new Spline.BufferSplineAdapter<T>(splineStates[i], splineAccessor[i]);
                    runtime.CurrentValue = Spline.SampleGeneric<T, Spline.BufferSplineAdapter<T>, TMath>(ref provider, easedT, Math);
                }
                else
                {
                    runtime.CurrentValue = Math.Lerp(range.StartPoint, range.EndPoint, easedT);
                }

                progresses[i] = progress;
                runtimes[i] = runtime;

                if (isFinished)
                {
                    if (control.AutoKill) Ecb.DestroyEntity(unfilteredChunkIndex, entities[i]);
                    else Ecb.SetComponentEnabled<TweenControl>(unfilteredChunkIndex, entities[i], false);
                }
            }
        }
    }
}
