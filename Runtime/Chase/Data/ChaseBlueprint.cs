using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween
{
    [BurstCompile]
    public sealed class ChaseBlueprint
    {
        public Entity Entity;
        public Entity Target;

        public float2 TStepMinMax;
        public float MaxStepTime;
        public bool IsOverride;

        public bool chasePosition;
        public bool chaseRotation;
        public bool lookRotation;

        public FunctionPointer<ChaseCallback>? OnStart;
        public FunctionPointer<ChaseCallback>? OnUpdate;
        public FunctionPointer<ChaseCallback>? OnChased;

        public bool Error;

        public ChaseBlueprint(Entity entity, Entity target, float2 tStepMinMax, float maxStepTime, bool isOverride,
            bool chasePosition, bool chaseRotation, bool lookRotation,
            FunctionPointer<ChaseCallback>? onStart, FunctionPointer<ChaseCallback>? onUpdate,
            FunctionPointer<ChaseCallback>? onChased)
        {
            Entity = entity;
            Target = target;
            TStepMinMax = tStepMinMax;
            MaxStepTime = maxStepTime;
            IsOverride = isOverride;
            this.chasePosition = chasePosition;
            this.chaseRotation = chaseRotation;
            this.lookRotation = lookRotation;
            OnStart = onStart;
            OnUpdate = onUpdate;
            OnChased = onChased;
            Error = false;
        }
    }

    [BurstCompile]
    public static class ChaseBlueprintExtensions
    {
        public static ChaseBlueprint Chase(this Entity entity, Entity target, bool chasePosition, bool chaseRotation,
            bool lookRotation)
        {
            return new ChaseBlueprint(entity, target, new float2(0.01f, 0.075f), 1.0f, false, chasePosition,
                chaseRotation, lookRotation, null, null, null);
        }

        public static ChaseBlueprint ChasePosition(this Entity entity, Entity target)
        {
            return new ChaseBlueprint(entity, target, new float2(0.01f, 0.075f), 1.0f, false, true, false, false, null,
                null, null);
        }

        public static ChaseBlueprint ChaseRotation(this Entity entity, Entity target)
        {
            return new ChaseBlueprint(entity, target, new float2(0.01f, 0.075f), 1.0f, false, false, true, false, null,
                null, null);
        }

        public static ChaseBlueprint ChaseLook(this Entity entity, Entity target)
        {
            return new ChaseBlueprint(entity, target, new float2(0.01f, 0.075f), 1.0f, false, false, false, true, null,
                null, null);
        }

        public static ChaseBlueprint ChasePositionAndRotation(this Entity entity, Entity target)
        {
            return new ChaseBlueprint(entity, target, new float2(0.01f, 0.075f), 1.0f, false, true, true, false, null,
                null, null);
        }

        public static ChaseBlueprint ChasePositionAndLook(this Entity entity, Entity target)
        {
            return new ChaseBlueprint(entity, target, new float2(0.01f, 0.075f), 1.0f, false, true, false, true, null,
                null, null);
        }

        public static ChaseBlueprint Settings(this ChaseBlueprint blueprint, bool isOverride = false)
        {
            blueprint.IsOverride = isOverride;
            return blueprint;
        }

        public static ChaseBlueprint TStep(this ChaseBlueprint blueprint, float2 tStepMinMax, float maxStepTime)
        {
            blueprint.TStepMinMax = tStepMinMax;
            blueprint.MaxStepTime = maxStepTime;
            return blueprint;
        }

        public static ChaseBlueprint SetOverride(this ChaseBlueprint blueprint, bool isOverride = true)
        {
            blueprint.IsOverride = true;
            return blueprint;
        }

        public static ChaseBlueprint OnStart(this ChaseBlueprint blueprint, ChaseCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint OnUpdate(this ChaseBlueprint blueprint, ChaseCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static ChaseBlueprint OnChased(this ChaseBlueprint blueprint, ChaseCallback func)
        {
            blueprint.OnChased = BurstCompiler.CompileFunctionPointer<ChaseCallback>(func);
            return blueprint;
        }

        public static void Play(this ChaseBlueprint blueprint, World world, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            var handle = world.GetOrCreateSystemManaged<ChaseHandlingSystem>();
            handle.AttachChase(blueprint, world, ecb);
        }

        public static void Play(this ChaseBlueprint blueprint, EntityManager em)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Chase play attempt.");
                return;
            }

            var handle = em.World.GetOrCreateSystemManaged<ChaseHandlingSystem>();
            handle.AttachChase(blueprint, em);
        }
    }
}