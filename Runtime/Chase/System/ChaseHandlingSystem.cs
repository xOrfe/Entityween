using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace XO.Entityween
{
    [BurstCompile]
    [DisableAutoCreation]
    public partial class ChaseHandlingSystem : SystemBase
    {
        private ChaseContainer<float3> _chasePositionContainers;
        private ChaseContainer<quaternion> _chaseRotationContainers;
        private ChaseContainer<quaternion> _lookContainers;

        [BurstCompile]
        protected override void OnCreate()
        {
            InitializeContainers();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            Dependency.Complete();
            var localToWorldLookup = GetComponentLookup<LocalToWorld>(isReadOnly: true);
            var parentLookup = GetComponentLookup<Parent>(isReadOnly: true);
            var deltaTime = SystemAPI.Time.DeltaTime;

            var positionHandle = new ChasePositionJob()
            {
                ParentLookup = parentLookup,
                LocalToWorldLookup = localToWorldLookup,
                DeltaTime = deltaTime,
                Chases = _chasePositionContainers.Chases.AsDeferredJobArray(),
                ChasesRuntimeData = _chasePositionContainers.ChasesRuntimeData.AsDeferredJobArray()
            }.Schedule(Dependency);

            var rotationHandle = new ChaseRotationJob()
            {
                ParentLookup = parentLookup,
                LocalToWorldLookup = localToWorldLookup,
                DeltaTime = deltaTime,
                Chases = _chaseRotationContainers.Chases.AsDeferredJobArray(),
                ChasesRuntimeData = _chaseRotationContainers.ChasesRuntimeData.AsDeferredJobArray()
            }.Schedule(positionHandle);

            var lookHandle = new LookJob()
            {
                ParentLookup = parentLookup,
                LocalToWorldLookup = localToWorldLookup,
                DeltaTime = deltaTime,
                Chases = _lookContainers.Chases.AsDeferredJobArray(),
                ChasesRuntimeData = _lookContainers.ChasesRuntimeData.AsDeferredJobArray()
            }.Schedule(rotationHandle);

            Dependency = lookHandle;
        }

        [BurstCompile]
        protected override void OnDestroy()
        {
            DisposeContainers();
        }

        [BurstCompile]
        public void AttachChase(ChaseBlueprint<float3> blueprint, World world, EntityCommandBuffer ecb)
        {
            var entityManager = world.EntityManager;
            var index = -1;
            if (entityManager.HasComponent<ChasePositionTag>(blueprint.Entity))
            {
                index = entityManager.GetComponentData<ChasePositionTag>(blueprint.Entity).Index;
                ecb.SetComponent(blueprint.Entity, new ChasePositionTag(blueprint.Target, index));
                _chasePositionContainers.Override(index, blueprint);
            }
            else
            {
                index = _chasePositionContainers.Attach(blueprint);
                ecb.AddComponent(blueprint.Entity, new ChasePositionTag(blueprint.Target, index));
            }
        }

        [BurstCompile]
        public void AttachChase(ChaseBlueprint<quaternion> blueprint, World world, EntityCommandBuffer ecb)
        {
            var entityManager = world.EntityManager;
            var index = -1;

            switch (blueprint.ChaseType)
            {
                case ChaseType.ChasePosition:
                    break;
                case ChaseType.ChaseRotation:
                    if (entityManager.HasComponent<ChaseRotationTag>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<ChaseRotationTag>(blueprint.Entity).Index;
                        ecb.SetComponent(blueprint.Entity, new ChaseRotationTag(blueprint.Target, index));
                        _chaseRotationContainers.Override(index, blueprint);
                    }
                    else
                    {
                        index = _chaseRotationContainers.Attach(blueprint);
                        ecb.AddComponent(blueprint.Entity, new ChaseRotationTag(blueprint.Target, index));
                    }

                    break;
                case ChaseType.Look:
                    if (entityManager.HasComponent<LookTag>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<LookTag>(blueprint.Entity).Index;
                        ecb.SetComponent(blueprint.Entity, new LookTag(blueprint.Target, index));
                        _lookContainers.Override(index, blueprint);
                    }
                    else
                    {
                        index = _lookContainers.Attach(blueprint);
                        ecb.AddComponent(blueprint.Entity, new LookTag(blueprint.Target, index));
                    }

                    break;
                case ChaseType.ChasePositionAndRotation:
                    break;
                case ChaseType.ChasePositionAndLook:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [BurstCompile]
        public void AttachChase(ChaseBlueprint<float4x4> blueprint, World world, EntityCommandBuffer ecb)
        {
        }

        [BurstCompile]
        private void InitializeContainers()
        {
            _chasePositionContainers = new ChaseContainer<float3>(64);
            _chaseRotationContainers = new ChaseContainer<quaternion>(64);
            _lookContainers = new ChaseContainer<quaternion>(64);
        }

        [BurstCompile]
        private void DisposeContainers()
        {
            _chasePositionContainers.Dispose();
            _chaseRotationContainers.Dispose();
            _lookContainers.Dispose();
        }
    }
}