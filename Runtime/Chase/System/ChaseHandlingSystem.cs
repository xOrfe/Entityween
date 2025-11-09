using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace XO.Entityween
{
    [BurstCompile]
    public partial class ChaseHandlingSystem : SystemBase
    {
        private ChaseContainer _chases;

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
            if (_chases.Calculate)
            {
                var chaseHandle = new ChaseJob()
                {
                    ParentLookup = parentLookup,
                    LocalToWorldLookup = localToWorldLookup,
                    deltaTime = deltaTime,
                    Chases = _chases.chases.AsDeferredJobArray(),
                    ChasesRuntimeData = _chases.ChasesRuntimeData.AsDeferredJobArray()
                }.Schedule(Dependency);
                Dependency = chaseHandle;
            }
        }

        [BurstCompile]
        protected override void OnDestroy()
        {
            DisposeContainers();
        }

        [BurstCompile]
        public void AttachChase(ChaseBlueprint blueprint, World world, EntityCommandBuffer ecb)
        {
            var entityManager = world.EntityManager;

            var index = -1;
            if (entityManager.HasComponent<ChaseTag>(blueprint.Entity))
            {
                index = entityManager.GetComponentData<ChaseTag>(blueprint.Entity).Index;
                _chases.Override(index, blueprint);
            }
            else
            {
                index = _chases.Attach(blueprint);
                ecb.AddComponent(blueprint.Entity, new ChaseTag(index));
            }
        }

        [BurstCompile]
        public void AttachChase(ChaseBlueprint blueprint, EntityManager em)
        {
            var index = -1;
            if (em.HasComponent<ChaseTag>(blueprint.Entity))
            {
                index = em.GetComponentData<ChaseTag>(blueprint.Entity).Index;
                _chases.Override(index, blueprint);
            }
            else
            {
                index = _chases.Attach(blueprint);
                em.AddComponent<ChaseTag>(blueprint.Entity);
                em.SetComponentData(blueprint.Entity, new ChaseTag(index));
            }
        }

        [BurstCompile]
        private void InitializeContainers()
        {
            _chases = new ChaseContainer(64);
        }

        [BurstCompile]
        private void DisposeContainers()
        {
            _chases.Dispose();
        }
    }
}