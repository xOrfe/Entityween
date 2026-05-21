using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Curve;

namespace XO.Entityween
{
    [BurstCompile]
    [UpdateInGroup(typeof(EntityweenChaseGroup))]
    internal partial struct ChaseHandlingSystem : ISystem
    {
        private EntityQuery _chasePositionQuery;
        private EntityQuery _chaseRotationQuery;
        private EntityQuery _chaseLookQuery;
        private EntityQuery _chaseScaleQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _chasePositionQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChasePosition, LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            _chaseRotationQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChaseRotation, LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            _chaseLookQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Look, LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            _chaseScaleQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChaseScale, LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true);
            var parentLookup = SystemAPI.GetComponentLookup<Parent>(isReadOnly: true);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            if (!_chasePositionQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChasePositionJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    DeltaTime = deltaTime,
                    EntityType = SystemAPI.GetEntityTypeHandle(),
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<ChasePosition>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<ChasePositionTweenSource>(true),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chasePositionQuery, state.Dependency);
            }

            if (!_chaseRotationQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseRotationJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    DeltaTime = deltaTime,
                    EntityType = SystemAPI.GetEntityTypeHandle(),
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<ChaseRotation>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<ChaseRotationTweenSource>(true),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chaseRotationQuery, state.Dependency);
            }

            if (!_chaseLookQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseLookJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    DeltaTime = deltaTime,
                    EntityType = SystemAPI.GetEntityTypeHandle(),
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<Look>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<LookTweenSource>(true),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chaseLookQuery, state.Dependency);
            }

            if (!_chaseScaleQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new ChaseScaleJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    DeltaTime = deltaTime,
                    EntityType = SystemAPI.GetEntityTypeHandle(),
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<ChaseScale>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<ChaseScaleTweenSource>(true),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chaseScaleQuery, state.Dependency);
            }
        }
    }

    [BurstCompile]
    internal struct ChasePositionJob : IJobChunk
    {
        private const float Epsilon = math.EPSILON;

        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChasePosition> ChaseType;
        [ReadOnly] public ComponentTypeHandle<ChasePositionTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var float3Math = default(Float3Math);
            var transforms = chunk.GetNativeArray(ref TransformType);
            var ltws = chunk.GetNativeArray(ref ChunkLtwType);
            var chases = chunk.GetNativeArray(ref ChaseType);
            var entities = chunk.GetNativeArray(EntityType);
            var enabledMask = chunk.GetEnabledMask(ref ChaseType);
            var hasSource = chunk.Has(ref SourceType);
            var sources = hasSource ? chunk.GetNativeArray(ref SourceType) : default;
            var isEntity = chunk.Has(ref TargetEntityType);
            var targetEntities = isEntity ? chunk.GetNativeArray(ref TargetEntityType) : default;
            var sortKey = unfilteredChunkIndex;

            for (var i = 0; i < chunk.Count; i++)
            {
                if (!enabledMask[i]) continue;

                var chase = chases[i];
                var entity = entities[i];
                var targetPos = chase.TargetPosition;

                if (!hasSource && isEntity)
                {
                    var target = targetEntities[i].Target;
                    if (!LocalToWorldLookup.HasComponent(target)) continue;
                    targetPos = LocalToWorldLookup[target].Position;
                }

                var localTransform = transforms[i];
                var currentPos = chase.Space == TweenSpace.Local ? localTransform.Position : ltws[i].Position;
                var velocity = chase.Velocity;
                var newPos = targetPos;

                switch (chase.Mode)
                {
                    case ChaseMode.SmoothStep:
                    {
                        var step = CurveMathUtility.GetSmoothStep(chase.SmoothTime, DeltaTime);
                        var diff = targetPos - currentPos;
                        newPos = math.lengthsq(diff) > Epsilon ? math.lerp(currentPos, targetPos, step) : targetPos;
                        break;
                    }
                    case ChaseMode.SmoothDamp:
                    {
                        newPos = float3Math.SmoothDamp(currentPos, targetPos, ref velocity, chase.SmoothTime, chase.MaxSpeed, DeltaTime);
                        break;
                    }
                    default:
                    {
                        velocity = float3.zero;
                        break;
                    }
                }

                var settled = IsSettled(newPos, targetPos, velocity);
                if (settled)
                {
                    newPos = targetPos;
                    velocity = float3.zero;
                }

                chase.Velocity = velocity;

                if (chase.Space == TweenSpace.Local)
                    localTransform.Position = newPos;
                else
                    TransformUtility.SetWorldPosition(entity, newPos, ref localTransform, ParentLookup, LocalToWorldLookup);

                transforms[i] = localTransform;
                chases[i] = chase;

                if (settled && hasSource && sources[i].SourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChasePositionTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<ChasePosition>(sortKey, entity);
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChasePosition>(sortKey, entity);
                }
            }
        }

        private static bool IsSettled(float3 current, float3 target, float3 velocity)
        {
            return math.lengthsq(current - target) <= Epsilon && math.lengthsq(velocity) <= Epsilon;
        }
    }

    [BurstCompile]
    internal struct ChaseRotationJob : IJobChunk
    {
        private const float Epsilon = math.EPSILON;
        private const float RotationDotEpsilon = 0.99999f;

        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChaseRotation> ChaseType;
        [ReadOnly] public ComponentTypeHandle<ChaseRotationTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var quaternionMath = default(QuaternionMath);
            var transforms = chunk.GetNativeArray(ref TransformType);
            var ltws = chunk.GetNativeArray(ref ChunkLtwType);
            var chases = chunk.GetNativeArray(ref ChaseType);
            var entities = chunk.GetNativeArray(EntityType);
            var enabledMask = chunk.GetEnabledMask(ref ChaseType);
            var hasSource = chunk.Has(ref SourceType);
            var sources = hasSource ? chunk.GetNativeArray(ref SourceType) : default;
            var isEntity = chunk.Has(ref TargetEntityType);
            var targetEntities = isEntity ? chunk.GetNativeArray(ref TargetEntityType) : default;
            var zeroQuat = new quaternion(0f, 0f, 0f, 0f);
            var sortKey = unfilteredChunkIndex;

            for (var i = 0; i < chunk.Count; i++)
            {
                if (!enabledMask[i]) continue;

                var chase = chases[i];
                var entity = entities[i];
                var targetRot = chase.TargetQuaternion;

                if (!hasSource && isEntity)
                {
                    var target = targetEntities[i].Target;
                    if (!LocalToWorldLookup.HasComponent(target)) continue;
                    targetRot = LocalToWorldLookup[target].Rotation;
                }

                var localTransform = transforms[i];
                var currentRot = chase.Space == TweenSpace.Local ? localTransform.Rotation : math.quaternion(ltws[i].Value);
                var velocity = chase.Velocity;
                var newRot = targetRot;

                switch (chase.Mode)
                {
                    case ChaseMode.SmoothStep:
                    {
                        var angularDot = math.abs(math.dot(targetRot, currentRot));
                        newRot = angularDot < RotationDotEpsilon
                            ? math.slerp(currentRot, targetRot, CurveMathUtility.GetSmoothStep(chase.SmoothTime, DeltaTime))
                            : targetRot;
                        velocity = zeroQuat;
                        break;
                    }
                    case ChaseMode.SmoothDamp:
                    {
                        newRot = quaternionMath.SmoothDamp(currentRot, targetRot, ref velocity, chase.SmoothTime, chase.MaxSpeed, DeltaTime);
                        break;
                    }
                    default:
                    {
                        velocity = zeroQuat;
                        break;
                    }
                }

                var settled = IsSettled(newRot, targetRot, velocity);
                if (settled)
                {
                    newRot = targetRot;
                    velocity = zeroQuat;
                }

                chase.Velocity = velocity;

                if (chase.Space == TweenSpace.Local)
                    localTransform.Rotation = newRot;
                else
                    TransformUtility.SetWorldRotation(entity, newRot, ref localTransform, ParentLookup, LocalToWorldLookup);

                transforms[i] = localTransform;
                chases[i] = chase;

                if (settled && hasSource && sources[i].SourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseRotationTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<ChaseRotation>(sortKey, entity);
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseRotation>(sortKey, entity);
                }
            }
        }

        private static bool IsSettled(quaternion current, quaternion target, quaternion velocity)
        {
            return math.abs(math.dot(current, target)) >= RotationDotEpsilon &&
                   math.lengthsq(velocity.value) <= Epsilon;
        }
    }

    [BurstCompile]
    internal struct ChaseLookJob : IJobChunk
    {
        private const float Epsilon = math.EPSILON;
        private const float RotationDotEpsilon = 0.99999f;

        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<Look> ChaseType;
        [ReadOnly] public ComponentTypeHandle<LookTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var float3Math = default(Float3Math);
            var transforms = chunk.GetNativeArray(ref TransformType);
            var ltws = chunk.GetNativeArray(ref ChunkLtwType);
            var chases = chunk.GetNativeArray(ref ChaseType);
            var entities = chunk.GetNativeArray(EntityType);
            var enabledMask = chunk.GetEnabledMask(ref ChaseType);
            var hasSource = chunk.Has(ref SourceType);
            var sources = hasSource ? chunk.GetNativeArray(ref SourceType) : default;
            var isEntity = chunk.Has(ref TargetEntityType);
            var targetEntities = isEntity ? chunk.GetNativeArray(ref TargetEntityType) : default;
            var sortKey = unfilteredChunkIndex;

            for (var i = 0; i < chunk.Count; i++)
            {
                if (!enabledMask[i]) continue;

                var chase = chases[i];
                var entity = entities[i];
                var targetPos = chase.TargetPosition;

                if (!hasSource && isEntity)
                {
                    var target = targetEntities[i].Target;
                    if (!LocalToWorldLookup.HasComponent(target)) continue;
                    targetPos = LocalToWorldLookup[target].Position;
                }

                var localTransform = transforms[i];
                var diff = targetPos - ltws[i].Position;
                var distSq = math.lengthsq(diff);
                var nextRot = localTransform.Rotation;
                var desiredRot = quaternion.LookRotationSafe(diff / math.sqrt(distSq), math.up());
                var settled = false;

                if (distSq > 1e-12f)
                {
                    var currentRot = math.quaternion(ltws[i].Value);

                    switch (chase.Mode)
                    {
                        case ChaseMode.SmoothStep:
                        {
                            var angularDot = math.abs(math.dot(desiredRot, currentRot));
                            nextRot = angularDot < RotationDotEpsilon
                                ? math.slerp(currentRot, desiredRot, CurveMathUtility.GetSmoothStep(chase.SmoothTime, DeltaTime))
                                : desiredRot;
                            chase.Velocity = float3.zero;
                            break;
                        }
                        case ChaseMode.SmoothDamp:
                        {
                            var currentForward = ltws[i].Value.c2.xyz;
                            var targetForward = diff / math.sqrt(distSq);
                            var velocity = chase.Velocity;
                            var nextForward = float3Math.SmoothDamp(currentForward, targetForward, ref velocity, chase.SmoothTime, chase.MaxSpeed, DeltaTime);
                            nextRot = quaternion.LookRotationSafe(nextForward, math.up());
                            chase.Velocity = velocity;
                            break;
                        }
                        default:
                        {
                            nextRot = desiredRot;
                            chase.Velocity = float3.zero;
                            break;
                        }
                    }

                    TransformUtility.SetWorldRotation(entity, nextRot, ref localTransform, ParentLookup, LocalToWorldLookup);
                    settled = math.abs(math.dot(nextRot, desiredRot)) >= RotationDotEpsilon && IsSettled(chase.Velocity);
                }
                else
                {
                    settled = true;
                }

                transforms[i] = localTransform;
                chases[i] = chase;

                if (settled && hasSource && sources[i].SourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<LookTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<Look>(sortKey, entity);
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<Look>(sortKey, entity);
                }
            }
        }

        private static bool IsSettled(float3 velocity)
        {
            return math.lengthsq(velocity) <= Epsilon;
        }
    }

    [BurstCompile]
    internal struct ChaseScaleJob : IJobChunk
    {
        private const float Epsilon = math.EPSILON;

        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChaseScale> ChaseType;
        [ReadOnly] public ComponentTypeHandle<ChaseScaleTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var floatMath = default(FloatMath);
            var transforms = chunk.GetNativeArray(ref TransformType);
            var chases = chunk.GetNativeArray(ref ChaseType);
            var entities = chunk.GetNativeArray(EntityType);
            var enabledMask = chunk.GetEnabledMask(ref ChaseType);
            var hasSource = chunk.Has(ref SourceType);
            var sources = hasSource ? chunk.GetNativeArray(ref SourceType) : default;
            var isEntity = chunk.Has(ref TargetEntityType);
            var targetEntities = isEntity ? chunk.GetNativeArray(ref TargetEntityType) : default;
            var sortKey = unfilteredChunkIndex;

            for (var i = 0; i < chunk.Count; i++)
            {
                if (!enabledMask[i]) continue;

                var chase = chases[i];
                var entity = entities[i];
                var targetScale = chase.IsUniform ? chase.TargetScale.x : math.cmax(chase.TargetScale);

                if (!hasSource && isEntity)
                {
                    var target = targetEntities[i].Target;
                    if (!LocalToWorldLookup.HasComponent(target)) continue;
                    targetScale = math.length(LocalToWorldLookup[target].Value.c0.xyz);
                }

                var localTransform = transforms[i];
                var currentScale = localTransform.Scale;
                var velocity = chase.Velocity.x;
                var newScale = targetScale;

                switch (chase.Mode)
                {
                    case ChaseMode.SmoothStep:
                    {
                        newScale = math.abs(targetScale - currentScale) > Epsilon
                            ? math.lerp(currentScale, targetScale, CurveMathUtility.GetSmoothStep(chase.SmoothTime, DeltaTime))
                            : targetScale;
                        velocity = 0f;
                        break;
                    }
                    case ChaseMode.SmoothDamp:
                    {
                        newScale = floatMath.SmoothDamp(currentScale, targetScale, ref velocity, chase.SmoothTime, chase.MaxSpeed, DeltaTime);
                        break;
                    }
                    default:
                    {
                        velocity = 0f;
                        break;
                    }
                }

                var settled = math.abs(newScale - targetScale) <= Epsilon && math.abs(velocity) <= Epsilon;
                if (settled)
                {
                    newScale = targetScale;
                    velocity = 0f;
                }

                chase.Velocity = new float3(velocity, 0f, 0f);
                localTransform.Scale = newScale;

                transforms[i] = localTransform;
                chases[i] = chase;

                if (settled && hasSource && sources[i].SourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseScaleTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<ChaseScale>(sortKey, entity);
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseScale>(sortKey, entity);
                }
            }
        }
    }
}
