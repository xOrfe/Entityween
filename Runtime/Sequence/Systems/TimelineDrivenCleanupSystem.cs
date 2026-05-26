using Unity.Burst;
using Unity.Entities;

namespace XO.Entityween
{
    [BurstCompile]
    [UpdateInGroup(typeof(EntityweenSequenceGroup))]
    [UpdateAfter(typeof(SequencePlaybackSystem))]
    internal partial struct TimelineDrivenCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sequenceLookup = SystemAPI.GetComponentLookup<Sequence>(true);
            var ownerLookup = SystemAPI.GetComponentLookup<SequenceActionOwner>(true);
            var tweenLookup = SystemAPI.GetComponentLookup<TweenControl>(false);
            var positionLookup = SystemAPI.GetComponentLookup<ChasePosition>(false);
            var rotationLookup = SystemAPI.GetComponentLookup<ChaseRotation>(false);
            var lookLookup = SystemAPI.GetComponentLookup<Look>(false);
            var scaleLookup = SystemAPI.GetComponentLookup<ChaseScale>(false);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (timelineDriven, entity) in SystemAPI.Query<RefRO<TimelineDriven>>().WithEntityAccess())
            {
                var sequenceEntity = timelineDriven.ValueRO.SequenceEntity;
                if (sequenceEntity != Entity.Null && sequenceLookup.HasComponent(sequenceEntity))
                    continue;

                if (ownerLookup.TryGetComponent(entity, out var owner) && owner.DestroyWithSequence)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (tweenLookup.HasComponent(entity))
                    tweenLookup.SetComponentEnabled(entity, false);
                if (positionLookup.HasComponent(entity))
                    positionLookup.SetComponentEnabled(entity, false);
                if (rotationLookup.HasComponent(entity))
                    rotationLookup.SetComponentEnabled(entity, false);
                if (lookLookup.HasComponent(entity))
                    lookLookup.SetComponentEnabled(entity, false);
                if (scaleLookup.HasComponent(entity))
                    scaleLookup.SetComponentEnabled(entity, false);

                ecb.RemoveComponent<TimelineDriven>(entity);
                if (ownerLookup.HasComponent(entity))
                    ecb.RemoveComponent<SequenceActionOwner>(entity);
            }
        }
    }
}
