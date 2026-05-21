using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    public struct TweenBlueprint<T> where T : unmanaged
    {
        internal Entity Entity;
        internal TweenType TweenType;
        internal TweenSpace Space;
        internal EaseType EaseType;
        internal PlaybackTimeType TimeType;
        internal float SecondsToPlay;

        internal bool IsLoop;
        
        /// <summary>
        /// Defines how many times the loop will execute. Set to 0 for infinite looping.
        /// </summary>
        [Tooltip("Defines how many times the loop will execute. Set to 0 for infinite looping.")]
        internal uint LoopCount;
        internal LoopType LoopType;

        internal T StartPoint;
        internal T EndPoint;
        internal bool StartFromCurrent;

        internal bool IsSpline;
        internal SplineType SplineType;
        internal bool SplineIsClosed;
        internal BlobAssetReference<SplineBlob<T>> SplineBlobRef;

        internal bool VisualizePath;

        internal bool UseChase;
        internal ChaseMode ChaseMode;
        internal float ChaseSmoothTime;
        internal float ChaseMaxSpeed;
        internal bool KillOnChase;

        internal bool Error;

        public void Dispose()
        {
            if (SplineBlobRef.IsCreated) SplineBlobRef.Dispose();
        }
    }

    public static class TweenBlueprintExtensions
    {
        /// <summary>
        /// Creates a tween blueprint to move an entity starting from a specific position.
        /// </summary>
        /// <param name="e">The entity to tween.</param>
        /// <param name="time">Duration of the tween in seconds.</param>
        /// <param name="start">The starting position.</param>
        /// <returns>A TweenBlueprint to further configure the tween.</returns>
        public static TweenBlueprint<float3> MoveTo(this Entity e, float time, float3 start)
            => e.MoveToLocal(time, start);

        public static TweenBlueprint<float3> MoveTo(this Entity e, float3 destination, float time)
            => e.MoveToLocal(destination, time);

        public static TweenBlueprint<float3> MoveToLocal(this Entity e, float time, float3 start)
            => new() { Entity = e, TweenType = TweenType.MoveTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<float3> MoveToLocal(this Entity e, float3 destination, float time)
            => e.MoveToLocal(time, default(float3)).Destination(destination).FromCurrent();

        public static TweenBlueprint<float3> MoveToWorld(this Entity e, float time, float3 start)
            => new() { Entity = e, TweenType = TweenType.MoveTo, Space = TweenSpace.World, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<float3> MoveToWorld(this Entity e, float3 destination, float time)
            => e.MoveToWorld(time, default(float3)).Destination(destination).FromCurrent();

        /// <summary>
        /// Creates a tween blueprint to rotate an entity starting from a specific rotation.
        /// </summary>
        /// <param name="e">The entity to tween.</param>
        /// <param name="time">Duration of the tween in seconds.</param>
        /// <param name="start">The starting rotation.</param>
        /// <returns>A TweenBlueprint to further configure the tween.</returns>
        public static TweenBlueprint<quaternion> RotateTo(this Entity e, float time, quaternion start)
            => e.RotateToLocal(time, start);

        public static TweenBlueprint<quaternion> RotateTo(this Entity e, quaternion destination, float time)
            => e.RotateToLocal(destination, time);

        public static TweenBlueprint<quaternion> RotateToLocal(this Entity e, float time, quaternion start)
            => new() { Entity = e, TweenType = TweenType.RotateTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<quaternion> RotateToLocal(this Entity e, quaternion destination, float time)
            => e.RotateToLocal(time, default(quaternion)).Destination(destination).FromCurrent();

        public static TweenBlueprint<quaternion> RotateToWorld(this Entity e, float time, quaternion start)
            => new() { Entity = e, TweenType = TweenType.RotateTo, Space = TweenSpace.World, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<quaternion> RotateToWorld(this Entity e, quaternion destination, float time)
            => e.RotateToWorld(time, default(quaternion)).Destination(destination).FromCurrent();

        public static TweenBlueprint<float3> ScaleTo(this Entity e, float time, float3 start)
            => new() { Entity = e, TweenType = TweenType.ScaleTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<float3> ScaleTo(this Entity e, float3 destination, float time)
            => e.ScaleTo(time, default(float3)).Destination(destination).FromCurrent();

        public static TweenBlueprint<float> ScaleToUniform(this Entity e, float time, float start)
            => new() { Entity = e, TweenType = TweenType.ScaleToUniform, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<float> FloatTo(this Entity e, float time, float start)
            => new() { Entity = e, TweenType = TweenType.FloatTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<float2> Float2To(this Entity e, float time, float2 start)
            => new() { Entity = e, TweenType = TweenType.Float2To, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<float3> Float3To(this Entity e, float time, float3 start)
            => new() { Entity = e, TweenType = TweenType.Float3To, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        public static TweenBlueprint<quaternion> QuaternionTo(this Entity e, float time, quaternion start)
            => new() { Entity = e, TweenType = TweenType.QuaternionTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = time, StartPoint = start };

        /// <summary>
        /// Sets the final destination value for the tween.
        /// </summary>
        /// <param name="bp">The tween blueprint.</param>
        /// <param name="target">The target end value.</param>
        /// <returns>The updated TweenBlueprint.</returns>
        public static TweenBlueprint<T> Destination<T>(this TweenBlueprint<T> bp, T target) where T : unmanaged
        {
            bp.EndPoint = target;
            return bp;
        }

        public static TweenBlueprint<T> To<T>(this TweenBlueprint<T> bp, T target) where T : unmanaged
            => bp.Destination(target);

        public static TweenBlueprint<T> From<T>(this TweenBlueprint<T> bp, T start) where T : unmanaged
        {
            bp.StartPoint = start;
            bp.StartFromCurrent = false;
            return bp;
        }

        public static TweenBlueprint<T> Along<T>(this TweenBlueprint<T> bp, NativeArray<T> pts, SplineType type, bool isClosed = false) where T : unmanaged
        {
            if (!SplineAllowed(bp.TweenType)) return bp;
            bp.IsSpline = true;
            bp.SplineType = type;
            bp.SplineIsClosed = isClosed;

            if (bp.SplineBlobRef.IsCreated)
                bp.SplineBlobRef.Dispose();

            if (typeof(T) == typeof(float))
            {
                var blob = Spline.CreateSplineBlob<float, FloatMath>(type, isClosed, (NativeArray<float>)(object)pts);
                bp.SplineBlobRef = (BlobAssetReference<SplineBlob<T>>)(object)blob;
            }
            else if (typeof(T) == typeof(float2))
            {
                var blob = Spline.CreateSplineBlob<float2, Float2Math>(type, isClosed, (NativeArray<float2>)(object)pts);
                bp.SplineBlobRef = (BlobAssetReference<SplineBlob<T>>)(object)blob;
            }
            else if (typeof(T) == typeof(float3))
            {
                var blob = Spline.CreateSplineBlob<float3, Float3Math>(type, isClosed, (NativeArray<float3>)(object)pts);
                bp.SplineBlobRef = (BlobAssetReference<SplineBlob<T>>)(object)blob;
            }
            else if (typeof(T) == typeof(quaternion))
            {
                var blob = Spline.CreateSplineBlob<quaternion, QuaternionMath>(type, isClosed, (NativeArray<quaternion>)(object)pts);
                bp.SplineBlobRef = (BlobAssetReference<SplineBlob<T>>)(object)blob;
            }
            else
            {
                Debug.LogWarning($"Spline destination type {typeof(T)} is not supported.");
            }

            return bp;
        }

        public static TweenBlueprint<T> Along<T>(this TweenBlueprint<T> bp, T[] pts, SplineType type, bool isClosed = false) where T : unmanaged
        {
            if (pts == null) return bp;
            using var nativePts = new NativeArray<T>(pts, Allocator.Temp);
            return bp.Along(nativePts, type, isClosed);
        }

        /// <summary>
        /// Sets the easing function for the tween.
        /// </summary>
        /// <param name="bp">The tween blueprint.</param>
        /// <param name="ease">The easing type to apply.</param>
        /// <returns>The updated TweenBlueprint.</returns>
        public static TweenBlueprint<T> Ease<T>(this TweenBlueprint<T> bp, EaseType ease) where T : unmanaged
        {
            bp.EaseType = ease;
            return bp;
        }

        public static TweenBlueprint<T> Loop<T>(this TweenBlueprint<T> bp, LoopType loopType = LoopType.PingPong, uint count = 0) where T : unmanaged
        {
            if (loopType == LoopType.Random)
            {
                Debug.LogWarning("Tween does not support LoopType.Random yet; falling back to Repeat.");
                loopType = LoopType.Repeat;
            }

            bp.IsLoop = true;
            bp.LoopType = loopType;
            bp.LoopCount = count;
            return bp;
        }

        public static TweenBlueprint<T> TimeType<T>(this TweenBlueprint<T> bp, PlaybackTimeType timeType) where T : unmanaged
        {
            bp.TimeType = timeType;
            return bp;
        }

        public static TweenBlueprint<T> FromCurrent<T>(this TweenBlueprint<T> bp) where T : unmanaged
        {
            bp.StartFromCurrent = true;
            return bp;
        }

        /// <summary>
        /// Draws the move tween path in the editor Scene View while the tween is alive.
        /// </summary>
        /// <param name="bp">The positional tween blueprint.</param>
        /// <returns>The updated TweenBlueprint.</returns>
        public static TweenBlueprint<float3> Visualize(this TweenBlueprint<float3> bp)
        {
#if UNITY_EDITOR
            if (bp.TweenType != TweenType.MoveTo)
            {
                Debug.LogWarning("Visualize() is only supported for positional MoveTo tweens.");
                return bp;
            }

            bp.VisualizePath = true;
#endif
            return bp;
        }

        /// <summary>
        /// Enables smooth damping/interpolation (chasing) for this tween blueprint.
        /// </summary>
        /// <param name="smoothTime">Approximately the time it will take to reach the target.</param>
        /// <param name="mode">Chase mode: SmoothDamp (velocity-based) or SmoothStep (easing-based).</param>
        /// <param name="maxSpeed">Optionally clamps the maximum speed for SmoothDamp.</param>
        public static TweenBlueprint<T> Chase<T>(this TweenBlueprint<T> bp, float smoothTime = 0.15f, ChaseMode mode = ChaseMode.SmoothStep, float maxSpeed = float.PositiveInfinity, bool killOnChase = false) where T : unmanaged
        {
            bp.UseChase = true;
            bp.ChaseMode = mode;
            bp.ChaseSmoothTime = smoothTime;
            bp.ChaseMaxSpeed = maxSpeed;
            bp.KillOnChase = killOnChase;
            return bp;
        }

        [Obsolete("Use Chase(smoothTime, ChaseMode, maxSpeed, killOnChase) instead.")]
        public static TweenBlueprint<T> Chase<T>(this TweenBlueprint<T> bp, float smoothTime, bool isLerp, float maxSpeed = float.PositiveInfinity, bool killOnChase = false) where T : unmanaged
            => bp.Chase(smoothTime, isLerp ? ChaseMode.SmoothStep : ChaseMode.SmoothDamp, maxSpeed, killOnChase);

        /// <summary>
        /// Executes the tween by scheduling it via an EntityCommandBuffer.
        /// </summary>
        /// <param name="bp">The tween blueprint.</param>
        /// <param name="ecb">The command buffer to record structural changes.</param>
        /// <returns>The calculation entity created for this tween.</returns>
        public static Entity Play<T>(this TweenBlueprint<T> bp, EntityCommandBuffer ecb) where T : unmanaged
        {
            var adapter = new EntityCommandBufferAdapter { ECB = ecb };
            return PlayInternal(ref bp, adapter);
        }

        public static Entity Play<T>(this TweenBlueprint<T> bp, int sortKey, ref EntityCommandBuffer.ParallelWriter ecb) where T : unmanaged
        {
            var adapter = new ParallelWriterAdapter { SortKey = sortKey, ECB = ecb };
            return PlayInternal(ref bp, adapter);
        }

        public static Entity Play<T>(this TweenBlueprint<T> bp, EntityManager em) where T : unmanaged
        {
            var adapter = new EntityManagerAdapter { Em = em };
            return PlayInternal(ref bp, adapter);
        }

        public static Entity Play<T, TAuth>(this TweenBlueprint<T> bp, Baker<TAuth> baker)
            where T : unmanaged
            where TAuth : MonoBehaviour
        {
            var adapter = new BakerAdapter<TAuth> { Baker = baker };
            return PlayInternal(ref bp, adapter);
        }

        internal static Entity PlayInternal<T, TAdapter>(ref TweenBlueprint<T> bp, TAdapter adapter)
            where T : unmanaged
            where TAdapter : struct, IEntityCommandAdapter
        {
            if (bp.Error) return Entity.Null;

            var ghost = CreateGhostEntity(ref bp, adapter, true, true);
            BindTweenTarget(ref bp, ghost, adapter);
            return ghost;
        }

        internal static Entity CreateGhostEntity<T, TAdapter>(ref TweenBlueprint<T> bp, TAdapter adapter, bool autoKill, bool startEnabled)
            where T : unmanaged
            where TAdapter : struct, IEntityCommandAdapter
        {
            var ghost = adapter.CreateEntity();
            adapter.AddComponent(ghost, new TweenControl { ElapsedTime = -1f, SecondsToPlay = bp.SecondsToPlay, EaseType = bp.EaseType, AutoKill = autoKill, Completed = false });
            adapter.SetComponentEnabled<TweenControl>(ghost, startEnabled);
            adapter.AddComponent(ghost, new PlaybackProgress { NormalizedTime = 0f, TimeType = bp.TimeType });
            adapter.AddComponent(ghost, new TweenTarget { Entity = bp.Entity, TweenType = bp.TweenType, Space = bp.Space });

            if (bp.IsLoop)
                adapter.AddComponent(ghost, new PlaybackLoop { LoopCount = bp.LoopCount, LoopType = bp.LoopType, LoopIndex = 0 });

            adapter.AddComponent(ghost, new TweenValue<T> { StartPoint = bp.StartPoint, EndPoint = bp.EndPoint });
            if (bp.StartFromCurrent)
                adapter.AddComponent(ghost, new TweenStartFromCurrent { TargetEntity = bp.Entity, TweenType = bp.TweenType, Space = bp.Space });

            if (bp.IsSpline && bp.SplineBlobRef.IsCreated)
            {
                ref var spline = ref bp.SplineBlobRef.Value;
                adapter.AddComponent(ghost, new SplineState { Type = spline.SplineType, IsClosed = spline.IsClosed, TotalWeight = spline.TotalWeight });
                var buf = adapter.AddBuffer<SplineElement<T>>(ghost);
                for (int i = 0; i < spline.Points.Length; i++)
                {
                    var weight = i < spline.SegmentWeights.Length ? spline.SegmentWeights[i] : 0f;
                    buf.Add(new SplineElement<T> { Point = spline.Points[i], SegmentWeight = weight });
                }

                bp.SplineBlobRef.Dispose();
                bp.SplineBlobRef = default;
            }

#if UNITY_EDITOR
            if (bp.VisualizePath && bp.TweenType == TweenType.MoveTo)
                adapter.AddComponent(ghost, new TweenDebugVisualize { TargetEntity = bp.Entity, TweenType = bp.TweenType, Space = bp.Space });
#endif

            return ghost;
        }

        internal static Entity CreateGhostEntity<TAdapter>(in SequenceBuildItem item, TAdapter adapter, bool autoKill, bool startEnabled)
            where TAdapter : struct, IEntityCommandAdapter
        {
            var ghost = adapter.CreateEntity();
            adapter.AddComponent(ghost, new TweenControl { ElapsedTime = -1f, SecondsToPlay = item.SecondsToPlay, EaseType = item.EaseType, AutoKill = autoKill, Completed = false });
            adapter.SetComponentEnabled<TweenControl>(ghost, startEnabled);
            adapter.AddComponent(ghost, new PlaybackProgress { NormalizedTime = 0f, TimeType = item.TimeType });
            adapter.AddComponent(ghost, new TweenTarget { Entity = item.TargetEntity, TweenType = item.TweenType, Space = item.Space });

            if (item.IsLoop)
                adapter.AddComponent(ghost, new PlaybackLoop { LoopCount = item.LoopCount, LoopType = item.LoopType, LoopIndex = 0 });

            switch (item.ValueKind)
            {
                case TweenValueKind.Float:
                    adapter.AddComponent(ghost, new TweenValue<float> { StartPoint = item.StartPoint.x, EndPoint = item.EndPoint.x });
                    break;
                case TweenValueKind.Float2:
                    adapter.AddComponent(ghost, new TweenValue<float2> { StartPoint = item.StartPoint.xy, EndPoint = item.EndPoint.xy });
                    break;
                case TweenValueKind.Float3:
                    adapter.AddComponent(ghost, new TweenValue<float3> { StartPoint = item.StartPoint.xyz, EndPoint = item.EndPoint.xyz });
                    break;
                case TweenValueKind.Quaternion:
                    adapter.AddComponent(ghost, new TweenValue<quaternion> { StartPoint = new quaternion(item.StartPoint), EndPoint = new quaternion(item.EndPoint) });
                    break;
                default:
                    Debug.LogWarning($"Sequence tween value kind {item.ValueKind} is not supported.");
                    break;
            }

            if (item.StartFromCurrent)
                adapter.AddComponent(ghost, new TweenStartFromCurrent { TargetEntity = item.TargetEntity, TweenType = item.TweenType, Space = item.Space });

            if (item.IsSpline)
            {
                switch (item.ValueKind)
                {
                    case TweenValueKind.Float:
                        if (item.SplineBlobFloat.IsCreated)
                        {
                            ref var spline = ref item.SplineBlobFloat.Value;
                            adapter.AddComponent(ghost, new SplineState { Type = spline.SplineType, IsClosed = spline.IsClosed, TotalWeight = spline.TotalWeight });
                            var buf = adapter.AddBuffer<SplineElement<float>>(ghost);
                            for (int i = 0; i < spline.Points.Length; i++)
                            {
                                var weight = i < spline.SegmentWeights.Length ? spline.SegmentWeights[i] : 0f;
                                buf.Add(new SplineElement<float> { Point = spline.Points[i], SegmentWeight = weight });
                            }
                        }
                        break;
                    case TweenValueKind.Float2:
                        if (item.SplineBlobFloat2.IsCreated)
                        {
                            ref var spline = ref item.SplineBlobFloat2.Value;
                            adapter.AddComponent(ghost, new SplineState { Type = spline.SplineType, IsClosed = spline.IsClosed, TotalWeight = spline.TotalWeight });
                            var buf = adapter.AddBuffer<SplineElement<float2>>(ghost);
                            for (int i = 0; i < spline.Points.Length; i++)
                            {
                                var weight = i < spline.SegmentWeights.Length ? spline.SegmentWeights[i] : 0f;
                                buf.Add(new SplineElement<float2> { Point = spline.Points[i], SegmentWeight = weight });
                            }
                        }
                        break;
                    case TweenValueKind.Float3:
                        if (item.SplineBlobFloat3.IsCreated)
                        {
                            ref var spline = ref item.SplineBlobFloat3.Value;
                            adapter.AddComponent(ghost, new SplineState { Type = spline.SplineType, IsClosed = spline.IsClosed, TotalWeight = spline.TotalWeight });
                            var buf = adapter.AddBuffer<SplineElement<float3>>(ghost);
                            for (int i = 0; i < spline.Points.Length; i++)
                            {
                                var weight = i < spline.SegmentWeights.Length ? spline.SegmentWeights[i] : 0f;
                                buf.Add(new SplineElement<float3> { Point = spline.Points[i], SegmentWeight = weight });
                            }
                        }
                        break;
                    case TweenValueKind.Quaternion:
                        if (item.SplineBlobQuaternion.IsCreated)
                        {
                            ref var spline = ref item.SplineBlobQuaternion.Value;
                            adapter.AddComponent(ghost, new SplineState { Type = spline.SplineType, IsClosed = spline.IsClosed, TotalWeight = spline.TotalWeight });
                            var buf = adapter.AddBuffer<SplineElement<quaternion>>(ghost);
                            for (int i = 0; i < spline.Points.Length; i++)
                            {
                                var weight = i < spline.SegmentWeights.Length ? spline.SegmentWeights[i] : 0f;
                                buf.Add(new SplineElement<quaternion> { Point = spline.Points[i], SegmentWeight = weight });
                            }
                        }
                        break;
                }
            }

#if UNITY_EDITOR
            if (item.VisualizePath && item.TweenType == TweenType.MoveTo)
                adapter.AddComponent(ghost, new TweenDebugVisualize { TargetEntity = item.TargetEntity, TweenType = item.TweenType, Space = item.Space });
#endif

            return ghost;
        }

        internal static void BindTweenTarget<T, TAdapter>(ref TweenBlueprint<T> bp, Entity ghostEntity, TAdapter adapter)
            where T : unmanaged
            where TAdapter : struct, IEntityCommandAdapter
        {
            switch (bp.TweenType)
            {
                case TweenType.MoveTo:
                    adapter.AddComponent(bp.Entity, new ChasePosition
                    {
                        TargetPosition = float3.zero,
                        Velocity = float3.zero,
                        Space = bp.Space,
                        Mode = bp.ChaseMode,
                        SmoothTime = bp.ChaseSmoothTime,
                        MaxSpeed = bp.ChaseMaxSpeed,
                        KillOnChase = !bp.UseChase || bp.KillOnChase
                    });
                    adapter.AddComponent(bp.Entity, new ChasePositionTweenSource
                    {
                        GhostEntity = ghostEntity,
                        Space = bp.Space
                    });
                    break;
                case TweenType.RotateTo:
                    adapter.AddComponent(bp.Entity, new ChaseRotation
                    {
                        TargetQuaternion = quaternion.identity,
                        Velocity = new quaternion(0f, 0f, 0f, 0f),
                        Space = bp.Space,
                        Mode = bp.ChaseMode,
                        SmoothTime = bp.ChaseSmoothTime,
                        MaxSpeed = bp.ChaseMaxSpeed,
                        KillOnChase = !bp.UseChase || bp.KillOnChase
                    });
                    adapter.AddComponent(bp.Entity, new ChaseRotationTweenSource
                    {
                        GhostEntity = ghostEntity,
                        Space = bp.Space
                    });
                    break;
                case TweenType.ScaleTo:
                    adapter.AddComponent(bp.Entity, new ChaseScale
                    {
                        TargetScale = float3.zero,
                        Velocity = float3.zero,
                        IsUniform = false,
                        Mode = bp.ChaseMode,
                        SmoothTime = bp.ChaseSmoothTime,
                        MaxSpeed = bp.ChaseMaxSpeed,
                        KillOnChase = !bp.UseChase || bp.KillOnChase
                    });
                    adapter.AddComponent(bp.Entity, new ChaseScaleTweenSource
                    {
                        GhostEntity = ghostEntity,
                        Space = bp.Space
                    });
                    break;
                case TweenType.ScaleToUniform:
                    adapter.AddComponent(bp.Entity, new ChaseScale
                    {
                        TargetScale = float3.zero,
                        Velocity = float3.zero,
                        IsUniform = true,
                        Mode = bp.ChaseMode,
                        SmoothTime = bp.ChaseSmoothTime,
                        MaxSpeed = bp.ChaseMaxSpeed,
                        KillOnChase = !bp.UseChase || bp.KillOnChase
                    });
                    adapter.AddComponent(bp.Entity, new ChaseScaleTweenSource
                    {
                        GhostEntity = ghostEntity,
                        Space = bp.Space
                    });
                    break;
            }
        }

        private static bool SplineAllowed(TweenType t)
        {
            var ok = t is not (TweenType.None or TweenType.Wait or TweenType.Callback);
            if (!ok) Debug.LogWarning($"TweenType {t} does not support splines.");
            return ok;
        }
    }
}
