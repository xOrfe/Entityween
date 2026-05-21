using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    [BurstCompile]
    public sealed class ChaseBlueprint<T> where T : unmanaged
    {
        internal Entity Entity;
        internal Entity Target;
        internal bool IsEntity;
        internal T TargetData;

        internal ChaseMode Mode;
        internal float SmoothTime;
        internal float MaxSpeed;
        internal bool IsOverride;

        internal readonly ChaseType ChaseType;
        internal readonly bool Error;

        internal ChaseBlueprint(Entity entity, Entity target, ChaseType chaseType)
        {
            Entity = entity;
            Target = target;
            IsEntity = true;
            TargetData = default;

            Mode = ChaseMode.SmoothStep;
            SmoothTime = 0.15f;
            MaxSpeed = float.PositiveInfinity;
            IsOverride = false;

            ChaseType = chaseType;
            Error = false;
        }

        internal ChaseBlueprint(Entity entity, T target, ChaseType chaseType)
        {
            Entity = entity;
            Target = default;
            IsEntity = false;
            TargetData = target;

            Mode = ChaseMode.SmoothStep;
            SmoothTime = 0.15f;
            MaxSpeed = float.PositiveInfinity;
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
    public static class ChaseBlueprintExtensions
    {
        /// <summary>
        /// Creates a chase blueprint for the entity to follow the target entity's position.
        /// </summary>
        /// <param name="entity">The entity that will chase.</param>
        /// <param name="target">The target entity to follow.</param>
        /// <returns>A ChaseBlueprint to further configure the behavior.</returns>
        public static ChaseBlueprint<float3> ChasePosition(this Entity entity, Entity target) => new(entity, target, ChaseType.ChasePosition);
        public static ChaseBlueprint<float3> ChasePosition(this Entity entity, float3 target) => new(entity, target, ChaseType.ChasePosition);
        public static ChaseBlueprint<quaternion> ChaseRotation(this Entity entity, Entity target) => new(entity, target, ChaseType.ChaseRotation);
        public static ChaseBlueprint<quaternion> ChaseRotation(this Entity entity, quaternion target) => new(entity, target, ChaseType.ChaseRotation);
        public static ChaseBlueprint<float3> Look(this Entity entity, Entity target) => new(entity, target, ChaseType.Look);
        public static ChaseBlueprint<float3> Look(this Entity entity, float3 target) => new(entity, target, ChaseType.Look);
        public static ChaseBlueprint<float4x4> ChasePositionAndRotation(this Entity entity, Entity target) => new(entity, target, ChaseType.ChasePositionAndRotation);
        public static ChaseBlueprint<float4x4> ChasePositionAndRotation(this Entity entity, float4x4 target) => new(entity, target, ChaseType.ChasePositionAndRotation);
        public static ChaseBlueprint<float4x4> ChasePositionAndLook(this Entity entity, Entity target) => new(entity, target, ChaseType.ChasePositionAndLook);
        public static ChaseBlueprint<float4x4> ChasePositionAndLook(this Entity entity, float4x4 target) => new(entity, target, ChaseType.ChasePositionAndLook);

        public static ChaseBlueprint<T> Override<T>(this ChaseBlueprint<T> blueprint, bool isOverride = true) where T : unmanaged
        {
            blueprint.IsOverride = isOverride;
            if (isOverride) blueprint.Mode = ChaseMode.Snap;
            return blueprint;
        }

        /// <summary>
        /// Configures the chase to use SmoothDamp interpolation.
        /// </summary>
        /// <param name="blueprint">The chase blueprint.</param>
        /// <param name="smoothTime">Approximately the time it will take to reach the target.</param>
        /// <param name="maxSpeed">Optionally allows you to clamp the maximum speed.</param>
        /// <returns>The updated ChaseBlueprint.</returns>
        public static ChaseBlueprint<T> SmoothDamp<T>(this ChaseBlueprint<T> blueprint, float smoothTime = 0.15f, float maxSpeed = float.PositiveInfinity) where T : unmanaged
        {
            blueprint.Mode = ChaseMode.SmoothDamp;
            blueprint.SmoothTime = smoothTime;
            blueprint.MaxSpeed = maxSpeed;
            blueprint.IsOverride = false;
            return blueprint;
        }

        /// <summary>
        /// Configures the chase to use SmoothStep easing instead of SmoothDamp.
        /// </summary>
        /// <param name="blueprint">The chase blueprint.</param>
        /// <param name="ease">The easing function to use.</param>
        /// <returns>The updated ChaseBlueprint.</returns>
        public static ChaseBlueprint<T> Ease<T>(this ChaseBlueprint<T> blueprint, EaseType ease) where T : unmanaged
        {
            blueprint.Mode = ChaseMode.SmoothStep;
            blueprint.IsOverride = false;
            return blueprint;
        }

        /// <summary>
        /// Applies the chase configuration to the entity via an EntityCommandBuffer.
        /// </summary>
        /// <param name="blueprint">The chase blueprint.</param>
        /// <param name="ecb">The command buffer to record the changes.</param>
        public static void Play<T>(this ChaseBlueprint<T> blueprint, EntityCommandBuffer ecb) where T : unmanaged
        {
            var adapter = new EntityCommandBufferAdapter { ECB = ecb };
            PlayInternal(blueprint, adapter);
        }

        public static void Play<T>(this ChaseBlueprint<T> blueprint, int sortKey, ref EntityCommandBuffer.ParallelWriter ecb) where T : unmanaged
        {
            var adapter = new ParallelWriterAdapter { SortKey = sortKey, ECB = ecb };
            PlayInternal(blueprint, adapter);
        }

        public static void Play<T>(this ChaseBlueprint<T> blueprint, EntityManager em) where T : unmanaged
        {
            var adapter = new EntityManagerAdapter { Em = em };
            PlayInternal(blueprint, adapter);
        }

        public static void Play<T, TAuth>(this ChaseBlueprint<T> blueprint, Baker<TAuth> baker)
            where T : unmanaged
            where TAuth : MonoBehaviour
        {
            var adapter = new BakerAdapter<TAuth> { Baker = baker };
            PlayInternal(blueprint, adapter);
        }

        internal static void PlayInternal<T, TAdapter>(ChaseBlueprint<T> blueprint, TAdapter adapter)
            where T : unmanaged
            where TAdapter : struct, IEntityCommandAdapter
        {
            if (blueprint.Error)
            {
                UnityEngine.Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            if (blueprint.IsEntity)
            {
                adapter.AddComponent(blueprint.Entity, new ChaseTargetEntity { Target = blueprint.Target });
            }

            if (blueprint.ChaseType is ChaseType.ChasePosition or ChaseType.ChasePositionAndRotation or ChaseType.ChasePositionAndLook)
            {
                var targetPos = float3.zero;
                if (!blueprint.IsEntity)
                {
                    if (blueprint.TargetData is float3 f3) targetPos = f3;
                    else if (blueprint.TargetData is float4x4 f4x4) targetPos = f4x4.c3.xyz;
                }

                var comp = new ChasePosition
                {
                    TargetPosition = targetPos,
                    Velocity = float3.zero,
                    Space = TweenSpace.World,
                    Mode = blueprint.Mode,
                    SmoothTime = blueprint.SmoothTime,
                    MaxSpeed = blueprint.MaxSpeed
                };
                adapter.AddComponent(blueprint.Entity, comp);
                adapter.SetComponentEnabled<ChasePosition>(blueprint.Entity, true);
            }

            if (blueprint.ChaseType is ChaseType.ChaseRotation or ChaseType.ChasePositionAndRotation)
            {
                var targetRot = quaternion.identity;
                if (!blueprint.IsEntity)
                {
                    if (blueprint.TargetData is quaternion q) targetRot = q;
                    else if (blueprint.TargetData is float4x4 f4x4) targetRot = new quaternion(f4x4);
                }

                var comp = new ChaseRotation
                {
                    TargetQuaternion = targetRot,
                    Velocity = new quaternion(0f, 0f, 0f, 0f),
                    Space = TweenSpace.World,
                    Mode = blueprint.Mode,
                    SmoothTime = blueprint.SmoothTime,
                    MaxSpeed = blueprint.MaxSpeed
                };
                adapter.AddComponent(blueprint.Entity, comp);
                adapter.SetComponentEnabled<ChaseRotation>(blueprint.Entity, true);
            }

            if (blueprint.ChaseType is ChaseType.Look or ChaseType.ChasePositionAndLook)
            {
                var targetPos = float3.zero;
                if (!blueprint.IsEntity)
                {
                    if (blueprint.TargetData is float3 f3) targetPos = f3;
                    else if (blueprint.TargetData is float4x4 f4x4) targetPos = f4x4.c3.xyz;
                }

                var comp = new Look
                {
                    TargetPosition = targetPos,
                    Velocity = float3.zero,
                    Mode = blueprint.Mode,
                    SmoothTime = blueprint.SmoothTime,
                    MaxSpeed = blueprint.MaxSpeed
                };
                adapter.AddComponent(blueprint.Entity, comp);
                adapter.SetComponentEnabled<Look>(blueprint.Entity, true);
            }

        }
    }
}
