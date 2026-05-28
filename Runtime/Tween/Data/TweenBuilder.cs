using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    internal enum TweenSplineSourceKind : byte
    {
        None,
        NativeArray,
        Blob
    }

    internal struct TweenSplineOptions<T> where T : unmanaged
    {
        public TweenSplineSourceKind SourceKind;
        public SplineType Type;
        public bool IsClosed;
        public NativeArray<T> NativePoints;
        public BlobAssetReference<SplineBlob<T>> BlobRef;
        public bool IsBend;

        public bool IsEnabled => SourceKind != TweenSplineSourceKind.None;
    }

    internal struct TweenTransformBindingOptions
    {
        public Transform BoundTransform;
        public bool HasTransformBinding;
        public TweenTransformBinding Binding;
    }

    public struct TweenBuilder<T> : ISequenceActionBlueprint where T : unmanaged
    {
        public TimelineActionKind Kind => TimelineActionKind.Tween;
        public float Duration => SecondsToPlay;
        public FixedString64Bytes CallbackId => default;

        public Entity CreateEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
            where TAdapter : IEntityCommandAdapter
        {
            var entity = TweenBuilderExtensions.CreateGhostEntity(in this, adapter, false, false);
            if (HasTransformBinding)
            {
                TweenBuilderExtensions.BindTransformTarget(in this, entity, adapter);
            }
            adapter.AddComponent(entity, new TweenSequenceDriven());
            adapter.AddComponent(entity, new TimelineDriven { SequenceEntity = sequenceEntity });
            adapter.AddComponent(entity, new SequenceActionOwner { DestroyWithSequence = true });
            adapter.AddComponent(entity, new SequenceTweenBinding
            {
                TargetEntity = Entity,
                TweenType = TweenType,
                Space = Space,
                UseChase = UseChase,
                ChaseMode = Chase.Mode,
                ChaseSmoothTime = Chase.SmoothTime,
                ChaseMaxSpeed = Chase.MaxSpeed,
                KillOnChase = Chase.KillOnChase,
                StartFromCurrent = StartFromCurrent
            });
            return entity;
        }

        internal Entity Entity;
        internal TweenType TweenType;
        internal TweenSpace Space;
        internal T StartPoint;
        internal T EndPoint;
        internal bool StartFromCurrent;

        internal EaseType EaseType;
        internal PlaybackTimeType TimeType;
        internal float SecondsToPlay;

        internal LoopConfig Loop;

        internal TweenSplineOptions<T> Spline;

        internal bool UseChase;
        internal ChaseConfig Chase;

        internal TweenTransformBindingOptions TransformBindingOptions;

        internal bool IsLoop => Loop.Type != LoopType.None;

        internal bool IsSpline { get => Spline.IsEnabled; set { if (!value) Spline = default; } }
        internal SplineType SplineType { get => Spline.Type; set => Spline.Type = value; }
        internal bool SplineIsClosed { get => Spline.IsClosed; set => Spline.IsClosed = value; }
        internal NativeArray<T> SplineNativePoints { get => Spline.NativePoints; set { Spline.NativePoints = value; Spline.SourceKind = value.IsCreated ? TweenSplineSourceKind.NativeArray : TweenSplineSourceKind.None; } }
        internal BlobAssetReference<SplineBlob<T>> SplineBlobRef { get => Spline.BlobRef; set { Spline.BlobRef = value; Spline.SourceKind = value.IsCreated ? TweenSplineSourceKind.Blob : TweenSplineSourceKind.None; } }

        internal bool VisualizePath;

        internal Transform BoundTransform { get => TransformBindingOptions.BoundTransform; set => TransformBindingOptions.BoundTransform = value; }
        internal bool HasTransformBinding { get => TransformBindingOptions.HasTransformBinding; set => TransformBindingOptions.HasTransformBinding = value; }
        internal TweenTransformBinding TransformBinding { get => TransformBindingOptions.Binding; set => TransformBindingOptions.Binding = value; }

        internal bool Error;
    }

    public static class TweenBuilderExtensions
    {

        public static TweenBuilder<float3> MoveTo(this Entity e, float3 destination, float duration)
            => new() { Entity = e, TweenType = TweenType.MoveTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<float3> MoveToLocal(this Entity e, float3 destination, float duration)
            => e.MoveTo(destination, duration);

        public static TweenBuilder<float3> MoveToWorld(this Entity e, float3 destination, float duration)
            => new() { Entity = e, TweenType = TweenType.MoveTo, Space = TweenSpace.World, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<quaternion> RotateTo(this Entity e, quaternion destination, float duration)
            => new() { Entity = e, TweenType = TweenType.RotateTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<quaternion> RotateToLocal(this Entity e, quaternion destination, float duration)
            => e.RotateTo(destination, duration);

        public static TweenBuilder<quaternion> RotateToWorld(this Entity e, quaternion destination, float duration)
            => new() { Entity = e, TweenType = TweenType.RotateTo, Space = TweenSpace.World, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<float3> ScaleTo(this Entity e, float3 destination, float duration)
            => new() { Entity = e, TweenType = TweenType.ScaleTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<float> ScaleToUniform(this Entity e, float destination, float duration)
            => new() { Entity = e, TweenType = TweenType.ScaleToUniform, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<float> FloatTo(this Entity e, float destination, float duration)
            => new() { Entity = e, TweenType = TweenType.FloatTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<float2> Float2To(this Entity e, float2 destination, float duration)
            => new() { Entity = e, TweenType = TweenType.Float2To, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<float3> Float3To(this Entity e, float3 destination, float duration)
            => new() { Entity = e, TweenType = TweenType.Float3To, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };

        public static TweenBuilder<quaternion> QuaternionTo(this Entity e, quaternion destination, float duration)
            => new() { Entity = e, TweenType = TweenType.QuaternionTo, Space = TweenSpace.Local, EaseType = EaseType.Linear, TimeType = PlaybackTimeType.Fixed, SecondsToPlay = duration, EndPoint = destination, StartFromCurrent = true };


        public static TweenBuilder<float3> MoveTo(this Transform transform, float3 destination, float duration)
            => Entity.Null.MoveToWorld(destination, duration).BindTransform(transform);

        public static TweenBuilder<float3> MoveToLocal(this Transform transform, float3 destination, float duration)
            => Entity.Null.MoveToLocal(destination, duration).BindTransform(transform);

        public static TweenBuilder<quaternion> RotateTo(this Transform transform, quaternion destination, float duration)
            => Entity.Null.RotateToWorld(destination, duration).BindTransform(transform);

        public static TweenBuilder<quaternion> RotateToLocal(this Transform transform, quaternion destination, float duration)
            => Entity.Null.RotateToLocal(destination, duration).BindTransform(transform);

        public static TweenBuilder<float3> ScaleTo(this Transform transform, float3 destination, float duration)
            => Entity.Null.ScaleTo(destination, duration).BindTransform(transform);

        public static TweenBuilder<float> ScaleToUniform(this Transform transform, float destination, float duration)
            => Entity.Null.ScaleToUniform(destination, duration).BindTransform(transform);



        public static TweenBuilder<float3> MoveTo(this GameObject go, float3 destination, float duration)
            => go.transform.MoveTo(destination, duration);

        public static TweenBuilder<float3> MoveToLocal(this GameObject go, float3 destination, float duration)
            => go.transform.MoveToLocal(destination, duration);

        public static TweenBuilder<quaternion> RotateTo(this GameObject go, quaternion destination, float duration)
            => go.transform.RotateTo(destination, duration);

        public static TweenBuilder<quaternion> RotateToLocal(this GameObject go, quaternion destination, float duration)
            => go.transform.RotateToLocal(destination, duration);

        public static TweenBuilder<float3> ScaleTo(this GameObject go, float3 destination, float duration)
            => go.transform.ScaleTo(destination, duration);

        public static TweenBuilder<float> ScaleToUniform(this GameObject go, float destination, float duration)
            => go.transform.ScaleToUniform(destination, duration);


        public static TweenBuilder<T> From<T>(this TweenBuilder<T> bp, T start) where T : unmanaged
        {
            bp.StartPoint = start;
            bp.StartFromCurrent = false;
            return bp;
        }


        public static TweenBuilder<T> To<T>(this TweenBuilder<T> bp, T target) where T : unmanaged
        {
            bp.EndPoint = target;
            return bp;
        }

        /// <summary>
        /// Tweens along a path backed by a <see cref="NativeArray{T}"/> without copying it to a managed array.
        /// The array must remain created until the tween or sequence is played, because points are materialized into
        /// the entity's spline buffer during Play.
        /// </summary>
        public static TweenBuilder<T> Along<T>(this TweenBuilder<T> bp, NativeArray<T> pts, SplineType type, bool isClosed = false) where T : unmanaged
        {
            if (!pts.IsCreated || !SplineAllowed(bp.TweenType)) return bp;
            bp.Spline = new TweenSplineOptions<T>
            {
                SourceKind = TweenSplineSourceKind.NativeArray,
                Type = type,
                IsClosed = isClosed,
                NativePoints = pts
            };
            return bp;
        }

        /// <summary>
        /// Tweens along the path defined by the user-provided <see cref="BlobAssetReference{SplineBlob{T}}"/>.
        /// Note: The user is responsible for managing and disposing of the blob reference asset.
        /// </summary>
        public static TweenBuilder<T> Along<T>(this TweenBuilder<T> bp, BlobAssetReference<SplineBlob<T>> blob) where T : unmanaged
        {
            if (!SplineAllowed(bp.TweenType)) return bp;
            bp.Spline = new TweenSplineOptions<T>
            {
                SourceKind = blob.IsCreated ? TweenSplineSourceKind.Blob : TweenSplineSourceKind.None,
                BlobRef = blob
            };
            return bp;
        }

        /// <summary>
        /// Bends the tween's default start-to-end line by the user-provided spline without modifying the spline.
        /// Note: The user is responsible for managing and disposing of the blob reference asset.
        /// </summary>
        public static TweenBuilder<T> Bend<T>(this TweenBuilder<T> bp, BlobAssetReference<SplineBlob<T>> blob) where T : unmanaged
        {
            if (!SplineAllowed(bp.TweenType)) return bp;
            bp.Spline = new TweenSplineOptions<T>
            {
                SourceKind = blob.IsCreated ? TweenSplineSourceKind.Blob : TweenSplineSourceKind.None,
                BlobRef = blob,
                IsBend = true
            };
            return bp;
        }

        public static TweenBuilder<T> Ease<T>(this TweenBuilder<T> bp, EaseType ease) where T : unmanaged
        {
            bp.EaseType = ease;
            return bp;
        }

        public static TweenBuilder<T> Loop<T>(this TweenBuilder<T> bp, LoopType loopType = LoopType.PingPong, uint count = 0, LoopEaseMode easeMode = LoopEaseMode.Mirror) where T : unmanaged
        {
            if (loopType == LoopType.Random)
            {
                Debug.LogWarning("Tween does not support LoopType.Random yet; falling back to Repeat.");
                loopType = LoopType.Repeat;
            }

            bp.Loop.Type = loopType;
            bp.Loop.Count = count;
            bp.Loop.EaseMode = easeMode;
            return bp;
        }

        public static TweenBuilder<T> TimeType<T>(this TweenBuilder<T> bp, PlaybackTimeType timeType) where T : unmanaged
        {
            bp.TimeType = timeType;
            return bp;
        }

        public static TweenBuilder<T> FromCurrent<T>(this TweenBuilder<T> bp) where T : unmanaged
        {
            bp.StartFromCurrent = true;
            return bp;
        }

        public static TweenBuilder<float3> Visualize(this TweenBuilder<float3> bp)
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

        public static TweenBuilder<T> Chase<T>(this TweenBuilder<T> bp, float smoothTime = 0.15f, ChaseMode mode = ChaseMode.SmoothStep, float maxSpeed = float.PositiveInfinity, bool killOnChase = false) where T : unmanaged
        {
            bp.UseChase = true;
            bp.Chase.Mode = mode;
            bp.Chase.SmoothTime = smoothTime;
            bp.Chase.MaxSpeed = maxSpeed;
            bp.Chase.KillOnChase = killOnChase;
            return bp;
        }

        public static Entity Play<T>(this TweenBuilder<T> bp) where T : unmanaged
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("No active DefaultGameObjectInjectionWorld found to play the tween. Please specify an EntityManager or EntityCommandBuffer.");
                return Entity.Null;
            }
            return bp.Play(world.EntityManager);
        }

        public static Entity Play<T>(this TweenBuilder<T> bp, EntityCommandBuffer ecb) where T : unmanaged
        {
            var adapter = new EntityCommandBufferAdapter { ECB = ecb };
            return PlayInternal(in bp, adapter);
        }

        public static Entity Play<T>(this TweenBuilder<T> bp, int sortKey, ref EntityCommandBuffer.ParallelWriter ecb) where T : unmanaged
        {
            var adapter = new ParallelWriterAdapter { SortKey = sortKey, ECB = ecb };
            return PlayInternal(in bp, adapter);
        }

        public static Entity Play<T>(this TweenBuilder<T> bp, EntityManager em) where T : unmanaged
        {
            var adapter = new EntityManagerAdapter { Em = em };
            return PlayInternal(in bp, adapter);
        }

        public static Entity Play<T, TAuth>(this TweenBuilder<T> bp, Baker<TAuth> baker)
            where T : unmanaged
            where TAuth : MonoBehaviour
        {
            var adapter = new BakerAdapter<TAuth> { Baker = baker };
            return PlayInternal(in bp, adapter);
        }

        internal static Entity PlayInternal<T, TAdapter>(in TweenBuilder<T> bp, TAdapter adapter)
            where T : unmanaged
            where TAdapter : IEntityCommandAdapter
        {
            if (bp.Error) return Entity.Null;

            if (bp.HasTransformBinding && adapter.World == null)
            {
                Debug.LogError("The current EntityCommandAdapter does not support Transform bindings because its World context is null. Play aborted.");
                return Entity.Null;
            }

            var ghost = CreateGhostEntity(in bp, adapter, true, true);

            if (bp.Entity != Entity.Null)
            {
                BindEntityTween(in bp, ghost, adapter);
            }

            if (bp.HasTransformBinding)
            {
                if (!BindTransformTarget(in bp, ghost, adapter))
                {
                    adapter.DestroyEntity(ghost);
                    return Entity.Null;
                }
            }

            return ghost;
        }

        internal static Entity CreateGhostEntity<T, TAdapter>(in TweenBuilder<T> bp, TAdapter adapter, bool autoKill, bool startEnabled)
            where T : unmanaged
            where TAdapter : IEntityCommandAdapter
        {
            var ghost = adapter.CreateTweenEntity<T>();
            adapter.SetComponent(ghost, new TweenControl { SecondsToPlay = bp.SecondsToPlay, EaseType = bp.EaseType, AutoKill = autoKill });
            adapter.SetComponentEnabled<TweenControl>(ghost, startEnabled);
            adapter.SetComponent(ghost, new PlaybackProgress
            {
                ElapsedTime = 0f,
                NormalizedTime = 0f,
                TimeType = bp.TimeType,
                Direction = 1,
                LoopType = bp.IsLoop ? bp.Loop.Type : LoopType.None,
                LoopCount = bp.IsLoop ? bp.Loop.Count : 1,
                LoopIndex = 0,
                LoopEaseMode = bp.IsLoop ? bp.Loop.EaseMode : LoopEaseMode.Mirror
            });
            adapter.SetComponent(ghost, new TweenTarget { Entity = bp.Entity, TweenType = bp.TweenType, Space = bp.Space });

            adapter.SetComponent(ghost, new TweenRange<T> { StartPoint = bp.StartPoint, EndPoint = bp.EndPoint });
            adapter.SetComponent(ghost, new TweenRuntime<T> { Completed = false });
            if (bp.StartFromCurrent)
                adapter.AddComponent(ghost, new TweenStartFromCurrent { TargetEntity = bp.Entity, TweenType = bp.TweenType, Space = bp.Space });

            if (bp.IsSpline)
            {
                if (bp.Spline.SourceKind == TweenSplineSourceKind.Blob && bp.SplineBlobRef.IsCreated)
                {
                    adapter.AddComponent(ghost, new SplineBlobRef<T> { Blob = bp.SplineBlobRef, IsBend = bp.Spline.IsBend });
                }
                else if (bp.Spline.SourceKind == TweenSplineSourceKind.NativeArray && bp.SplineNativePoints.IsCreated && bp.SplineNativePoints.Length > 0)
                {
                    var splineState = new SplineState();
                    var buf = adapter.AddBuffer<SplineElement<T>>(ghost);
                    if (typeof(T) == typeof(float))
                    {
                        Spline.PopulateSplineBuffer<float, FloatMath>(bp.SplineType, bp.SplineIsClosed, (NativeArray<float>)(object)bp.SplineNativePoints, ref splineState, (DynamicBuffer<SplineElement<float>>)(object)buf);
                    }
                    else if (typeof(T) == typeof(float2))
                    {
                        Spline.PopulateSplineBuffer<float2, Float2Math>(bp.SplineType, bp.SplineIsClosed, (NativeArray<float2>)(object)bp.SplineNativePoints, ref splineState, (DynamicBuffer<SplineElement<float2>>)(object)buf);
                    }
                    else if (typeof(T) == typeof(float3))
                    {
                        Spline.PopulateSplineBuffer<float3, Float3Math>(bp.SplineType, bp.SplineIsClosed, (NativeArray<float3>)(object)bp.SplineNativePoints, ref splineState, (DynamicBuffer<SplineElement<float3>>)(object)buf);
                    }
                    else if (typeof(T) == typeof(quaternion))
                    {
                        Spline.PopulateSplineBuffer<quaternion, QuaternionMath>(bp.SplineType, bp.SplineIsClosed, (NativeArray<quaternion>)(object)bp.SplineNativePoints, ref splineState, (DynamicBuffer<SplineElement<quaternion>>)(object)buf);
                    }
                    adapter.AddComponent(ghost, splineState);
                }
            }

#if UNITY_EDITOR
            if (bp.VisualizePath && bp.TweenType == TweenType.MoveTo)
                adapter.AddComponent(ghost, new TweenDebugVisualize { TargetEntity = bp.Entity, TweenType = bp.TweenType, Space = bp.Space });
#endif

            return ghost;
        }

        internal static void BindEntityTween<T, TAdapter>(in TweenBuilder<T> bp, Entity ghostEntity, TAdapter adapter)
            where T : unmanaged
            where TAdapter : IEntityCommandAdapter
        {
            switch (bp.TweenType)
            {
                case TweenType.MoveTo:
                    adapter.AddComponent(bp.Entity, new ChasePosition
                    {
                        TargetPosition = float3.zero,
                        Velocity = float3.zero,
                        Space = bp.Space,
                        Mode = bp.Chase.Mode,
                        SmoothTime = bp.Chase.SmoothTime,
                        MaxSpeed = bp.Chase.MaxSpeed,
                        KillOnChase = !bp.UseChase || bp.Chase.KillOnChase
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
                        Mode = bp.Chase.Mode,
                        SmoothTime = bp.Chase.SmoothTime,
                        MaxSpeed = bp.Chase.MaxSpeed,
                        KillOnChase = !bp.UseChase || bp.Chase.KillOnChase
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
                        Mode = bp.Chase.Mode,
                        SmoothTime = bp.Chase.SmoothTime,
                        MaxSpeed = bp.Chase.MaxSpeed,
                        KillOnChase = !bp.UseChase || bp.Chase.KillOnChase
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
                        Mode = bp.Chase.Mode,
                        SmoothTime = bp.Chase.SmoothTime,
                        MaxSpeed = bp.Chase.MaxSpeed,
                        KillOnChase = !bp.UseChase || bp.Chase.KillOnChase
                    });
                    adapter.AddComponent(bp.Entity, new ChaseScaleTweenSource
                    {
                        GhostEntity = ghostEntity,
                        Space = bp.Space
                    });
                    break;
            }
        }

        internal static bool BindTransformTarget<T, TAdapter>(in TweenBuilder<T> bp, Entity ghost, TAdapter adapter)
            where T : unmanaged
            where TAdapter : IEntityCommandAdapter
        {
            var world = adapter.World;
            if (world == null)
            {
                Debug.LogError("Adapter World is null. Cannot bind Transform target.");
                return false;
            }

            if (bp.BoundTransform == null)
            {
                Debug.LogError("BindTransform targetTransform is null.");
                return false;
            }

            if (!adapter.SupportsManagedComponents)
            {
                Debug.LogError("BindTransform requires an EntityManager-backed Play call because Transform references are stored as managed components.");
                return false;
            }

            adapter.AddComponentObject(ghost, new TweenTransformReference { Transform = bp.BoundTransform });
            adapter.AddComponent(ghost, new TweenTransformTarget
            {
                Binding = bp.TransformBinding,
                Space = bp.Space
            });
            return true;
        }

        public static TweenBuilder<float3> BindTransform(
            this TweenBuilder<float3> bp,
            Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogError("BindTransform targetTransform is null.");
                bp.Error = true;
                return bp;
            }

            if (bp.TweenType == TweenType.MoveTo)
            {
                bp.BoundTransform = targetTransform;
                bp.HasTransformBinding = true;
                bp.TransformBinding = TweenTransformBinding.Position;
            }
            else if (bp.TweenType == TweenType.ScaleTo)
            {
                bp.BoundTransform = targetTransform;
                bp.HasTransformBinding = true;
                bp.TransformBinding = TweenTransformBinding.Scale;
            }
            else
            {
                Debug.LogError($"Incompatible tween type '{bp.TweenType}' for BindTransform<float3>.");
                bp.Error = true;
            }
            return bp;
        }

        public static TweenBuilder<quaternion> BindTransform(
            this TweenBuilder<quaternion> bp,
            Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogError("BindTransform targetTransform is null.");
                bp.Error = true;
                return bp;
            }

            if (bp.TweenType == TweenType.RotateTo)
            {
                bp.BoundTransform = targetTransform;
                bp.HasTransformBinding = true;
                bp.TransformBinding = TweenTransformBinding.Rotation;
            }
            else
            {
                Debug.LogError($"Incompatible tween type '{bp.TweenType}' for BindTransform<quaternion>.");
                bp.Error = true;
            }
            return bp;
        }

        public static TweenBuilder<float> BindTransform(
            this TweenBuilder<float> bp,
            Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogError("BindTransform targetTransform is null.");
                bp.Error = true;
                return bp;
            }

            if (bp.TweenType == TweenType.ScaleToUniform)
            {
                bp.BoundTransform = targetTransform;
                bp.HasTransformBinding = true;
                bp.TransformBinding = TweenTransformBinding.ScaleUniform;
            }
            else
            {
                Debug.LogError($"Incompatible tween type '{bp.TweenType}' for BindTransform<float>.");
                bp.Error = true;
            }
            return bp;
        }

        private static bool SplineAllowed(TweenType t)
        {
            var ok = t is not (TweenType.None or TweenType.Wait or TweenType.Callback);
            if (!ok) Debug.LogWarning($"TweenType {t} does not support splines.");
            return ok;
        }
    }
}
