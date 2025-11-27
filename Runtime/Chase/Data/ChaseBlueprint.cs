using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween
{
    [BurstCompile]
    public sealed class ChaseBlueprint<T> where T : unmanaged
    {
        public Entity Entity;
        public Entity Target;

        public float2 TStepMinMax;
        public float MaxStepTime;
        public bool IsOverride;
        public T Transform;
        public FunctionPointer<ChaseCallback>? OnStart;
        public FunctionPointer<ChaseCallback>? OnUpdate;
        public FunctionPointer<ChaseCallback>? OnChased;

        public readonly ChaseType ChaseType;
        public readonly bool Error;

        public ChaseBlueprint(Entity entity, Entity target, ChaseType chaseType)
        {
            Entity = entity;
            Target = target;
            TStepMinMax = new float2(0.01f, 0.075f);
            MaxStepTime = 1.0f;
            Transform = default;
            IsOverride = false;
            OnStart = null;
            OnUpdate = null;
            OnChased = null;
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
        public static ChaseBlueprint<float3> ChasePosition(this Entity entity, Entity target)
        {
            return new ChaseBlueprint<float3>(entity, target, ChaseType.ChasePosition);
        }

        public static ChaseBlueprint<quaternion> ChaseRotation(this Entity entity, Entity target)
        {
            return new ChaseBlueprint<quaternion>(entity, target, ChaseType.ChaseRotation);
        }

        public static ChaseBlueprint<quaternion> Look(this Entity entity, Entity target)
        {
            return new ChaseBlueprint<quaternion>(entity, target, ChaseType.Look);
        }

        //ChasePositionAndRotation and ChasePositionAndLook are not functional yet.
        private static ChaseBlueprint<float4x4> ChasePositionAndRotation(this Entity entity, Entity target)
        {
            return new ChaseBlueprint<float4x4>(entity, target, ChaseType.ChasePositionAndRotation);
        }

        private static ChaseBlueprint<float4x4> ChasePositionAndLook(this Entity entity, Entity target)
        {
            return new ChaseBlueprint<float4x4>(entity, target, ChaseType.ChasePositionAndLook);
        }

        public static ChaseBlueprint<float3> TStep(this ChaseBlueprint<float3> blueprint, float2 tStepMinMax,
            float maxStepTime)
        {
            blueprint.TStepMinMax = tStepMinMax;
            blueprint.MaxStepTime = maxStepTime;
            return blueprint;
        }

        public static ChaseBlueprint<quaternion> TStep(this ChaseBlueprint<quaternion> blueprint, float2 tStepMinMax,
            float maxStepTime)
        {
            blueprint.TStepMinMax = tStepMinMax;
            blueprint.MaxStepTime = maxStepTime;
            return blueprint;
        }

        public static ChaseBlueprint<float3> SetOverride(this ChaseBlueprint<float3> blueprint, bool isOverride = false)
        {
            blueprint.IsOverride = isOverride;
            return blueprint;
        }

        public static ChaseBlueprint<quaternion> SetOverride(this ChaseBlueprint<quaternion> blueprint,
            bool isOverride = false)
        {
            blueprint.IsOverride = isOverride;
            return blueprint;
        }

        public static ChaseBlueprint<float3> OnStart(this ChaseBlueprint<float3> blueprint, ChaseCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint<quaternion> OnStart(this ChaseBlueprint<quaternion> blueprint, ChaseCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint<float3> OnUpdate(this ChaseBlueprint<float3> blueprint, ChaseCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint<quaternion> OnUpdate(this ChaseBlueprint<quaternion> blueprint, ChaseCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint<float3> OnChased(this ChaseBlueprint<float3> blueprint, ChaseCallback func)
        {
            blueprint.OnChased = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint<quaternion> OnChased(this ChaseBlueprint<quaternion> blueprint, ChaseCallback func)
        {
            blueprint.OnChased = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static void Play(this ChaseBlueprint<float3> blueprint, World world, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            var handle = world.GetOrCreateSystemManaged<ChaseHandlingSystem>();
            handle.AttachChase(blueprint, world, ecb);
        }

        public static void Play(this ChaseBlueprint<quaternion> blueprint, World world, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            var handle = world.GetOrCreateSystemManaged<ChaseHandlingSystem>();
            handle.AttachChase(blueprint, world, ecb);
        }

        public static void Play(this ChaseBlueprint<float4x4> blueprint, World world, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            var handle = world.GetOrCreateSystemManaged<ChaseHandlingSystem>();
            handle.AttachChase(blueprint, world, ecb);
        }
    }
}