using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    [BurstCompile]
    public sealed class ChaseBuilder<T> : ISequenceActionBlueprint where T : unmanaged
    {
        public TimelineActionKind Kind => TimelineActionKind.Chase;
        public float Duration { get; set; }
        public FixedString64Bytes CallbackId => default;

        public Entity CreateEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
            where TAdapter : IEntityCommandAdapter
        {
            if (adapter.World != null && Entity != Entity.Null && !adapter.World.EntityManager.Exists(Entity))
            {
                return Entity;
            }

            ChaseBuilderExtensions.PlayInternal(this, adapter, false);
            adapter.AddComponent(Entity, new TimelineDriven { SequenceEntity = sequenceEntity });
            adapter.AddComponent(Entity, new SequenceActionOwner { DestroyWithSequence = false });
            return Entity;
        }

        internal Entity Entity;
        internal Entity Target;
        internal bool IsEntity;
        internal T TargetData;

        internal ChaseConfig Chase = ChaseConfig.Default;
        internal bool IsOverride;

        internal readonly ChaseType ChaseType;
        internal readonly bool Error;

        internal ChaseBuilder(Entity entity, Entity target, ChaseType chaseType)
        {
            Entity = entity;
            Target = target;
            IsEntity = true;
            TargetData = default;
            IsOverride = false;
            ChaseType = chaseType;
            Error = false;
        }

        internal ChaseBuilder(Entity entity, T target, ChaseType chaseType)
        {
            Entity = entity;
            Target = default;
            IsEntity = false;
            TargetData = target;
            IsOverride = false;
            ChaseType = chaseType;
            Error = false;
        }
    }

    public enum ChaseType
    {
        ChasePosition,
        ChaseRotation,
        Look,
        ChasePositionAndRotation,
        ChasePositionAndLook
    }

    [BurstCompile]
    public static class ChaseBuilderExtensions
    {
        /// <summary>
        /// Creates a chase builder for the entity to follow the target entity's position.
        /// </summary>
        public static ChaseBuilder<float3> ChasePosition(this Entity entity, Entity target) => new(entity, target, ChaseType.ChasePosition);
        public static ChaseBuilder<float3> ChasePosition(this Entity entity, float3 target) => new(entity, target, ChaseType.ChasePosition);
        public static ChaseBuilder<quaternion> ChaseRotation(this Entity entity, Entity target) => new(entity, target, ChaseType.ChaseRotation);
        public static ChaseBuilder<quaternion> ChaseRotation(this Entity entity, quaternion target) => new(entity, target, ChaseType.ChaseRotation);
        public static ChaseBuilder<float3> Look(this Entity entity, Entity target) => new(entity, target, ChaseType.Look);
        public static ChaseBuilder<float3> Look(this Entity entity, float3 target) => new(entity, target, ChaseType.Look);
        public static ChaseBuilder<float4x4> ChasePositionAndRotation(this Entity entity, Entity target) => new(entity, target, ChaseType.ChasePositionAndRotation);
        public static ChaseBuilder<float4x4> ChasePositionAndRotation(this Entity entity, float4x4 target) => new(entity, target, ChaseType.ChasePositionAndRotation);
        public static ChaseBuilder<float4x4> ChasePositionAndLook(this Entity entity, Entity target) => new(entity, target, ChaseType.ChasePositionAndLook);
        public static ChaseBuilder<float4x4> ChasePositionAndLook(this Entity entity, float4x4 target) => new(entity, target, ChaseType.ChasePositionAndLook);

        public static ChaseBuilder<T> Override<T>(this ChaseBuilder<T> builder, bool isOverride = true) where T : unmanaged
        {
            builder.IsOverride = isOverride;
            if (isOverride) builder.Chase.Mode = ChaseMode.Snap;
            return builder;
        }

        public static ChaseBuilder<T> For<T>(this ChaseBuilder<T> builder, float seconds) where T : unmanaged
        {
            builder.Duration = math.max(0f, seconds);
            return builder;
        }

        /// <summary>
        /// Configures the chase to use SmoothDamp interpolation.
        /// </summary>
        public static ChaseBuilder<T> SmoothDamp<T>(this ChaseBuilder<T> builder, float smoothTime = 0.15f, float maxSpeed = float.PositiveInfinity) where T : unmanaged
        {
            builder.Chase.Mode = ChaseMode.SmoothDamp;
            builder.Chase.SmoothTime = smoothTime;
            builder.Chase.MaxSpeed = maxSpeed;
            builder.IsOverride = false;
            return builder;
        }

        /// <summary>
        /// Configures the chase to use SmoothStep easing instead of SmoothDamp.
        /// </summary>
        public static ChaseBuilder<T> Ease<T>(this ChaseBuilder<T> builder, EaseType ease) where T : unmanaged
        {
            builder.Chase.Mode = ChaseMode.SmoothStep;
            builder.IsOverride = false;
            return builder;
        }

        public static ChaseBuilder<T> KillOnChase<T>(this ChaseBuilder<T> builder, bool killOnChase = true) where T : unmanaged
        {
            builder.Chase.KillOnChase = killOnChase;
            return builder;
        }

        /// <summary>
        /// Applies the chase configuration to the entity via an EntityCommandBuffer.
        /// </summary>
        public static void Play<T>(this ChaseBuilder<T> builder, EntityCommandBuffer ecb) where T : unmanaged
        {
            var adapter = new EntityCommandBufferAdapter { ECB = ecb };
            PlayInternal(builder, adapter);
        }

        public static void Play<T>(this ChaseBuilder<T> builder, int sortKey, ref EntityCommandBuffer.ParallelWriter ecb) where T : unmanaged
        {
            var adapter = new ParallelWriterAdapter { SortKey = sortKey, ECB = ecb };
            PlayInternal(builder, adapter);
        }

        public static void Play<T>(this ChaseBuilder<T> builder, EntityManager em) where T : unmanaged
        {
            var adapter = new EntityManagerAdapter { Em = em };
            PlayInternal(builder, adapter);
        }

        public static void Play<T, TAuth>(this ChaseBuilder<T> builder, Baker<TAuth> baker)
            where T : unmanaged
            where TAuth : MonoBehaviour
        {
            var adapter = new BakerAdapter<TAuth> { Baker = baker };
            PlayInternal(builder, adapter);
        }

        internal static void PlayInternal<T, TAdapter>(ChaseBuilder<T> builder, TAdapter adapter, bool startEnabled = true)
            where T : unmanaged
            where TAdapter : IEntityCommandAdapter
        {
            if (builder.Error)
            {
                UnityEngine.Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            if (builder.IsEntity)
            {
                adapter.AddComponent(builder.Entity, new ChaseTargetEntity { Target = builder.Target });
            }

            if (builder.ChaseType is ChaseType.ChasePosition or ChaseType.ChasePositionAndRotation or ChaseType.ChasePositionAndLook)
            {
                var targetPos = float3.zero;
                if (!builder.IsEntity)
                {
                    if (builder.TargetData is float3 f3) targetPos = f3;
                    else if (builder.TargetData is float4x4 f4x4) targetPos = f4x4.c3.xyz;
                }

                var comp = new ChasePosition
                {
                    TargetPosition = targetPos,
                    Velocity = float3.zero,
                    Space = TweenSpace.World,
                    Mode = builder.Chase.Mode,
                    SmoothTime = builder.Chase.SmoothTime,
                    MaxSpeed = builder.Chase.MaxSpeed,
                    KillOnChase = builder.Chase.KillOnChase
                };
                adapter.AddComponent(builder.Entity, comp);
                adapter.SetComponentEnabled<ChasePosition>(builder.Entity, startEnabled);
            }

            if (builder.ChaseType is ChaseType.ChaseRotation or ChaseType.ChasePositionAndRotation)
            {
                var targetRot = quaternion.identity;
                if (!builder.IsEntity)
                {
                    if (builder.TargetData is quaternion q) targetRot = q;
                    else if (builder.TargetData is float4x4 f4x4) targetRot = new quaternion(f4x4);
                }

                var comp = new ChaseRotation
                {
                    TargetQuaternion = targetRot,
                    Velocity = new quaternion(0f, 0f, 0f, 0f),
                    Space = TweenSpace.World,
                    Mode = builder.Chase.Mode,
                    SmoothTime = builder.Chase.SmoothTime,
                    MaxSpeed = builder.Chase.MaxSpeed,
                    KillOnChase = builder.Chase.KillOnChase
                };
                adapter.AddComponent(builder.Entity, comp);
                adapter.SetComponentEnabled<ChaseRotation>(builder.Entity, startEnabled);
            }

            if (builder.ChaseType is ChaseType.Look or ChaseType.ChasePositionAndLook)
            {
                var targetPos = float3.zero;
                if (!builder.IsEntity)
                {
                    if (builder.TargetData is float3 f3) targetPos = f3;
                    else if (builder.TargetData is float4x4 f4x4) targetPos = f4x4.c3.xyz;
                }

                var comp = new Look
                {
                    TargetPosition = targetPos,
                    Velocity = float3.zero,
                    Mode = builder.Chase.Mode,
                    SmoothTime = builder.Chase.SmoothTime,
                    MaxSpeed = builder.Chase.MaxSpeed,
                    KillOnChase = builder.Chase.KillOnChase
                };
                adapter.AddComponent(builder.Entity, comp);
                adapter.SetComponentEnabled<Look>(builder.Entity, startEnabled);
            }

        }
    }
}
