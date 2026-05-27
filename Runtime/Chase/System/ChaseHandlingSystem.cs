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
    [RequireMatchingQueriesForUpdate]
    internal partial struct ChaseHandlingSystem : ISystem
    {
        private EntityQuery _chasePositionQuery;
        private EntityQuery _chaseRotationQuery;
        private EntityQuery _chasePositionRotationQuery;
        private EntityQuery _chaseLookQuery;
        private EntityQuery _chaseScaleQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _chasePositionQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChasePosition>()
                .WithAllRW<LocalTransform>()
                .WithAll<LocalToWorld>()
                .WithNone<ChaseRotation>()
                .Build();

            _chaseRotationQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChaseRotation>()
                .WithAllRW<LocalTransform>()
                .WithAll<LocalToWorld>()
                .WithNone<ChasePosition>()
                .Build();

            _chasePositionRotationQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChasePosition, ChaseRotation>()
                .WithAllRW<LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            _chaseLookQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Look>()
                .WithAllRW<LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            _chaseScaleQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ChaseScale>()
                .WithAllRW<LocalTransform>()
                .WithAll<LocalToWorld>()
                .Build();

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hasPositionChase = !_chasePositionQuery.IsEmptyIgnoreFilter;
            var hasRotationChase = !_chaseRotationQuery.IsEmptyIgnoreFilter;
            var hasPositionRotationChase = !_chasePositionRotationQuery.IsEmptyIgnoreFilter;
            var hasLookChase = !_chaseLookQuery.IsEmptyIgnoreFilter;
            var hasScaleChase = !_chaseScaleQuery.IsEmptyIgnoreFilter;

            if (!hasPositionChase && !hasRotationChase && !hasPositionRotationChase && !hasLookChase && !hasScaleChase)
                return;

            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true);
            var parentLookup = SystemAPI.GetComponentLookup<Parent>(isReadOnly: true);
            var runtimeFloatLookup = SystemAPI.GetComponentLookup<TweenRuntime<float>>(isReadOnly: true);
            var runtimeFloat3Lookup = SystemAPI.GetComponentLookup<TweenRuntime<float3>>(isReadOnly: true);
            var runtimeQuatLookup = SystemAPI.GetComponentLookup<TweenRuntime<quaternion>>(isReadOnly: true);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var entityType = SystemAPI.GetEntityTypeHandle();

            if (hasPositionChase)
            {
                state.Dependency = new ChasePositionJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    RuntimeFloat3Lookup = runtimeFloat3Lookup,
                    DeltaTime = deltaTime,
                    EntityType = entityType,
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<ChasePosition>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<ChasePositionTweenSource>(false),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chasePositionQuery, state.Dependency);
            }

            if (hasRotationChase)
            {
                state.Dependency = new ChaseRotationJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    RuntimeQuatLookup = runtimeQuatLookup,
                    DeltaTime = deltaTime,
                    EntityType = entityType,
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<ChaseRotation>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<ChaseRotationTweenSource>(false),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chaseRotationQuery, state.Dependency);
            }

            if (hasPositionRotationChase)
            {
                state.Dependency = new ChasePositionRotationJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    RuntimeFloat3Lookup = runtimeFloat3Lookup,
                    RuntimeQuatLookup = runtimeQuatLookup,
                    DeltaTime = deltaTime,
                    EntityType = entityType,
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    PositionType = SystemAPI.GetComponentTypeHandle<ChasePosition>(false),
                    RotationType = SystemAPI.GetComponentTypeHandle<ChaseRotation>(false),
                    PositionSourceType = SystemAPI.GetComponentTypeHandle<ChasePositionTweenSource>(false),
                    RotationSourceType = SystemAPI.GetComponentTypeHandle<ChaseRotationTweenSource>(false),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chasePositionRotationQuery, state.Dependency);
            }

            if (hasLookChase)
            {
                state.Dependency = new ChaseLookJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    RuntimeFloat3Lookup = runtimeFloat3Lookup,
                    DeltaTime = deltaTime,
                    EntityType = entityType,
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<Look>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<LookTweenSource>(false),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chaseLookQuery, state.Dependency);
            }

            if (hasScaleChase)
            {
                state.Dependency = new ChaseScaleJob
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    RuntimeFloatLookup = runtimeFloatLookup,
                    RuntimeFloat3Lookup = runtimeFloat3Lookup,
                    DeltaTime = deltaTime,
                    EntityType = entityType,
                    TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                    ChunkLtwType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                    ChaseType = SystemAPI.GetComponentTypeHandle<ChaseScale>(false),
                    SourceType = SystemAPI.GetComponentTypeHandle<ChaseScaleTweenSource>(false),
                    TargetEntityType = SystemAPI.GetComponentTypeHandle<ChaseTargetEntity>(true),
                    Ecb = ecb
                }.ScheduleParallel(_chaseScaleQuery, state.Dependency);
            }
        }
    }

    [BurstCompile]
    internal struct ChasePositionRotationJob : IJobChunk
    {
        private const float Epsilon = math.EPSILON;
        private const float RotationDotEpsilon = 0.99999f;

        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public ComponentLookup<TweenRuntime<float3>> RuntimeFloat3Lookup;
        [ReadOnly] public ComponentLookup<TweenRuntime<quaternion>> RuntimeQuatLookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChasePosition> PositionType;
        public ComponentTypeHandle<ChaseRotation> RotationType;
        public ComponentTypeHandle<ChasePositionTweenSource> PositionSourceType;
        public ComponentTypeHandle<ChaseRotationTweenSource> RotationSourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var float3Math = default(Float3Math);
            var quaternionMath = default(QuaternionMath);
            var transforms = chunk.GetNativeArray(ref TransformType);
            var ltws = chunk.GetNativeArray(ref ChunkLtwType);
            var positions = chunk.GetNativeArray(ref PositionType);
            var rotations = chunk.GetNativeArray(ref RotationType);
            var entities = chunk.GetNativeArray(EntityType);
            var positionEnabledMask = chunk.GetEnabledMask(ref PositionType);
            var rotationEnabledMask = chunk.GetEnabledMask(ref RotationType);
            var hasPositionSource = chunk.Has(ref PositionSourceType);
            var hasRotationSource = chunk.Has(ref RotationSourceType);
            var positionSources = hasPositionSource ? chunk.GetNativeArray(ref PositionSourceType) : default;
            var rotationSources = hasRotationSource ? chunk.GetNativeArray(ref RotationSourceType) : default;
            var isEntity = chunk.Has(ref TargetEntityType);
            var targetEntities = isEntity ? chunk.GetNativeArray(ref TargetEntityType) : default;
            var sortKey = unfilteredChunkIndex;
            var zeroQuat = new quaternion(0f, 0f, 0f, 0f);

            for (var i = 0; i < chunk.Count; i++)
            {
                var positionEnabled = positionEnabledMask[i];
                var rotationEnabled = rotationEnabledMask[i];
                if (!positionEnabled && !rotationEnabled) continue;

                var entity = entities[i];
                var localTransform = transforms[i];
                var ltw = ltws[i];

                if (positionEnabled)
                {
                    var chase = positions[i];
                    var targetPos = chase.TargetPosition;
                    var sourceCompleted = false;

                    if (hasPositionSource)
                    {
                        var source = positionSources[i];
                        if (RuntimeFloat3Lookup.TryGetComponent(source.GhostEntity, out var runtime))
                        {
                            sourceCompleted = source.SourceCompleted || runtime.Completed;
                            targetPos = runtime.CurrentValue;
                            chase.TargetPosition = targetPos;
                            chase.Space = source.Space;
                        }
                        else
                        {
                            sourceCompleted = source.SourceCompleted;
                        }
                    }
                    else if (isEntity)
                    {
                        var target = targetEntities[i].Target;
                        if (!LocalToWorldLookup.HasComponent(target))
                            goto WritePosition;
                        targetPos = LocalToWorldLookup[target].Position;
                    }

                    var currentPos = chase.Space == TweenSpace.Local ? localTransform.Position : ltw.Position;
                    var velocity = chase.Velocity;
                    var newPos = targetPos;

                    switch (chase.Mode)
                    {
                        case ChaseMode.SmoothStep:
                            newPos = float3Math.SmoothStep(currentPos, targetPos, chase.SmoothTime, DeltaTime);
                            break;
                        case ChaseMode.SmoothDamp:
                            newPos = float3Math.SmoothDamp(currentPos, targetPos, ref velocity, chase.SmoothTime, chase.MaxSpeed, DeltaTime);
                            break;
                        default:
                            velocity = float3.zero;
                            break;
                    }

                    var settled = IsPositionSettled(in newPos, in targetPos, in velocity);
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

                    positions[i] = chase;

                    if (settled && hasPositionSource && sourceCompleted && chase.KillOnChase)
                    {
                        Ecb.RemoveComponent<ChasePositionTweenSource>(sortKey, entity);
                        Ecb.RemoveComponent<ChasePosition>(sortKey, entity);
                    }
                    else if (hasPositionSource && sourceCompleted && !chase.KillOnChase)
                    {
                        Ecb.RemoveComponent<ChasePositionTweenSource>(sortKey, entity);
                    }
                    else if (hasPositionSource)
                    {
                        var source = positionSources[i];
                        source.SourceCompleted = sourceCompleted;
                        positionSources[i] = source;
                    }
                    else if (settled && !hasPositionSource && chase.KillOnChase)
                    {
                        Ecb.RemoveComponent<ChasePosition>(sortKey, entity);
                    }
                }

WritePosition:
                if (rotationEnabled)
                {
                    var chase = rotations[i];
                    var targetRot = chase.TargetQuaternion;
                    var sourceCompleted = false;

                    if (hasRotationSource)
                    {
                        var source = rotationSources[i];
                        if (RuntimeQuatLookup.TryGetComponent(source.GhostEntity, out var runtime))
                        {
                            sourceCompleted = source.SourceCompleted || runtime.Completed;
                            targetRot = runtime.CurrentValue;
                            chase.TargetQuaternion = targetRot;
                            chase.Space = source.Space;
                        }
                        else
                        {
                            sourceCompleted = source.SourceCompleted;
                        }
                    }
                    else if (isEntity)
                    {
                        var target = targetEntities[i].Target;
                        if (!LocalToWorldLookup.HasComponent(target))
                            goto WriteTransform;
                        targetRot = LocalToWorldLookup[target].Rotation;
                    }

                    var currentRot = chase.Space == TweenSpace.Local ? localTransform.Rotation : math.quaternion(ltw.Value);
                    var velocity = chase.Velocity;
                    var newRot = targetRot;

                    switch (chase.Mode)
                    {
                        case ChaseMode.SmoothStep:
                            newRot = quaternionMath.SmoothStep(currentRot, targetRot, chase.SmoothTime, DeltaTime);
                            velocity = zeroQuat;
                            break;
                        case ChaseMode.SmoothDamp:
                            newRot = quaternionMath.SmoothDamp(currentRot, targetRot, ref velocity, chase.SmoothTime, chase.MaxSpeed, DeltaTime);
                            break;
                        default:
                            velocity = zeroQuat;
                            break;
                    }

                    var settled = IsRotationSettled(in newRot, in targetRot, in velocity);
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

                    rotations[i] = chase;

                    if (settled && hasRotationSource && sourceCompleted && chase.KillOnChase)
                    {
                        Ecb.RemoveComponent<ChaseRotationTweenSource>(sortKey, entity);
                        Ecb.RemoveComponent<ChaseRotation>(sortKey, entity);
                    }
                    else if (hasRotationSource && sourceCompleted && !chase.KillOnChase)
                    {
                        Ecb.RemoveComponent<ChaseRotationTweenSource>(sortKey, entity);
                    }
                    else if (hasRotationSource)
                    {
                        var source = rotationSources[i];
                        source.SourceCompleted = sourceCompleted;
                        rotationSources[i] = source;
                    }
                    else if (settled && !hasRotationSource && chase.KillOnChase)
                    {
                        Ecb.RemoveComponent<ChaseRotation>(sortKey, entity);
                    }
                }

WriteTransform:
                transforms[i] = localTransform;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        private static bool IsPositionSettled(in float3 current, in float3 target, in float3 velocity)
        {
            return math.lengthsq(current - target) <= Epsilon && math.lengthsq(velocity) <= Epsilon;
        }

        [BurstCompile(DisableDirectCall = true)]
        private static bool IsRotationSettled(in quaternion current, in quaternion target, in quaternion velocity)
        {
            return math.abs(math.dot(current, target)) >= RotationDotEpsilon &&
                   math.lengthsq(velocity.value) <= Epsilon;
        }
    }

    [BurstCompile]
    internal struct ChasePositionJob : IJobChunk
    {
        private const float Epsilon = math.EPSILON;

        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public ComponentLookup<TweenRuntime<float3>> RuntimeFloat3Lookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChasePosition> ChaseType;
        public ComponentTypeHandle<ChasePositionTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        
        public EntityCommandBuffer.ParallelWriter Ecb;

        [BurstCompile]
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
                var sourceCompleted = false;

                if (hasSource)
                {
                    var source = sources[i];
                    var ghost = source.GhostEntity;
                    if (RuntimeFloat3Lookup.TryGetComponent(ghost, out var runtime))
                    {
                        sourceCompleted = source.SourceCompleted || runtime.Completed;
                        targetPos = runtime.CurrentValue;
                        chase.TargetPosition = targetPos;
                        chase.Space = source.Space;
                    }
                    else
                    {
                        sourceCompleted = source.SourceCompleted;
                    }
                }
                else if (isEntity)
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
                        newPos = float3Math.SmoothStep(currentPos, targetPos, chase.SmoothTime, DeltaTime);
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

                var settled = IsSettled(in newPos, in targetPos, in velocity);
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

                if (settled && hasSource && sourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChasePositionTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<ChasePosition>(sortKey, entity);
                }
                else if (hasSource && sourceCompleted && !chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChasePositionTweenSource>(sortKey, entity);
                }
                else if (hasSource)
                {
                    var source = sources[i];
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChasePosition>(sortKey, entity);
                }
            }
        }
        
        [BurstCompile(DisableDirectCall = true)]
        private static bool IsSettled(in float3 current, in float3 target, in float3 velocity)
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
        [ReadOnly] public ComponentLookup<TweenRuntime<quaternion>> RuntimeQuatLookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChaseRotation> ChaseType;
        public ComponentTypeHandle<ChaseRotationTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [BurstCompile]
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
                var sourceCompleted = false;

                if (hasSource)
                {
                    var source = sources[i];
                    var ghost = source.GhostEntity;
                    if (RuntimeQuatLookup.TryGetComponent(ghost, out var runtime))
                    {
                        sourceCompleted = source.SourceCompleted || runtime.Completed;
                        targetRot = runtime.CurrentValue;
                        chase.TargetQuaternion = targetRot;
                        chase.Space = source.Space;
                    }
                    else
                    {
                        sourceCompleted = source.SourceCompleted;
                    }
                }
                else if (isEntity)
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
                        newRot = quaternionMath.SmoothStep(currentRot, targetRot, chase.SmoothTime, DeltaTime);
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

                var settled = IsSettled(in newRot, in targetRot, in velocity);
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

                if (settled && hasSource && sourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseRotationTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<ChaseRotation>(sortKey, entity);
                }
                else if (hasSource && sourceCompleted && !chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseRotationTweenSource>(sortKey, entity);
                }
                else if (hasSource)
                {
                    var source = sources[i];
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseRotation>(sortKey, entity);
                }
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        private static bool IsSettled(in quaternion current, in quaternion target, in quaternion velocity)
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
        [ReadOnly] public ComponentLookup<TweenRuntime<float3>> RuntimeFloat3Lookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<Look> ChaseType;
        public ComponentTypeHandle<LookTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var float3Math = default(Float3Math);
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
            var sortKey = unfilteredChunkIndex;

            for (var i = 0; i < chunk.Count; i++)
            {
                if (!enabledMask[i]) continue;

                var chase = chases[i];
                var entity = entities[i];
                var targetPos = chase.TargetPosition;
                var sourceCompleted = false;

                if (hasSource)
                {
                    var source = sources[i];
                    var ghost = source.GhostEntity;
                    if (RuntimeFloat3Lookup.TryGetComponent(ghost, out var runtime))
                    {
                        sourceCompleted = source.SourceCompleted || runtime.Completed;
                        targetPos = runtime.CurrentValue;
                        chase.TargetPosition = targetPos;
                    }
                    else
                    {
                        sourceCompleted = source.SourceCompleted;
                    }
                }
                else if (isEntity)
                {
                    var target = targetEntities[i].Target;
                    if (!LocalToWorldLookup.HasComponent(target)) continue;
                    targetPos = LocalToWorldLookup[target].Position;
                }

                var localTransform = transforms[i];
                var diff = targetPos - ltws[i].Position;
                var distSq = math.lengthsq(diff);
                var nextRot = localTransform.Rotation;
                var settled = false;

                if (distSq > 1e-12f)
                {
                    var desiredRot = quaternion.LookRotationSafe(diff / math.sqrt(distSq), math.up());
                    var currentRot = math.quaternion(ltws[i].Value);

                    switch (chase.Mode)
                    {
                        case ChaseMode.SmoothStep:
                        {
                            nextRot = quaternionMath.SmoothStep(currentRot, desiredRot, chase.SmoothTime, DeltaTime);
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
                    var settledVelocity = chase.Velocity;
                    settled = math.abs(math.dot(nextRot, desiredRot)) >= RotationDotEpsilon && IsSettled(in settledVelocity);
                }
                else
                {
                    settled = true;
                }

                transforms[i] = localTransform;
                chases[i] = chase;

                if (settled && hasSource && sourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<LookTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<Look>(sortKey, entity);
                }
                else if (hasSource && sourceCompleted && !chase.KillOnChase)
                {
                    Ecb.RemoveComponent<LookTweenSource>(sortKey, entity);
                }
                else if (hasSource)
                {
                    var source = sources[i];
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<Look>(sortKey, entity);
                }
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        private static bool IsSettled(in float3 velocity)
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
        [ReadOnly] public ComponentLookup<TweenRuntime<float>> RuntimeFloatLookup;
        [ReadOnly] public ComponentLookup<TweenRuntime<float3>> RuntimeFloat3Lookup;
        public float DeltaTime;

        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ChunkLtwType;
        public ComponentTypeHandle<ChaseScale> ChaseType;
        public ComponentTypeHandle<ChaseScaleTweenSource> SourceType;
        [ReadOnly] public ComponentTypeHandle<ChaseTargetEntity> TargetEntityType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [BurstCompile]
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
                var sourceCompleted = false;

                if (hasSource)
                {
                    var source = sources[i];
                    var ghost = source.GhostEntity;
                    sourceCompleted = source.SourceCompleted;

                    if (chase.IsUniform && RuntimeFloatLookup.TryGetComponent(ghost, out var uniformValue))
                    {
                        sourceCompleted |= uniformValue.Completed;
                        chase.TargetScale = new float3(uniformValue.CurrentValue);
                        targetScale = uniformValue.CurrentValue;
                    }
                    else if (RuntimeFloat3Lookup.TryGetComponent(ghost, out var value))
                    {
                        sourceCompleted |= value.Completed;
                        chase.TargetScale = value.CurrentValue;
                        targetScale = chase.IsUniform ? value.CurrentValue.x : math.cmax(value.CurrentValue);
                    }
                }
                else if (isEntity)
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
                        newScale = floatMath.SmoothStep(currentScale, targetScale, chase.SmoothTime, DeltaTime);
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

                if (settled && hasSource && sourceCompleted && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseScaleTweenSource>(sortKey, entity);
                    Ecb.RemoveComponent<ChaseScale>(sortKey, entity);
                }
                else if (hasSource && sourceCompleted && !chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseScaleTweenSource>(sortKey, entity);
                }
                else if (hasSource)
                {
                    var source = sources[i];
                    source.SourceCompleted = sourceCompleted;
                    sources[i] = source;
                }
                else if (settled && !hasSource && chase.KillOnChase)
                {
                    Ecb.RemoveComponent<ChaseScale>(sortKey, entity);
                }
            }
        }
    }
}
