using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    [BurstCompile]
    [UpdateInGroup(typeof(EntityweenTweenGroup))]
    [UpdateBefore(typeof(TweenCalculationSystem))]
    [RequireMatchingQueriesForUpdate]
    internal partial struct TweenTargetCleanupSystem : ISystem
    {
        private EntityQuery _cleanupQuery;
        private SystemThrottler _throttler;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _cleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<TweenTarget>()
                .Build();
                
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _throttler.Reset();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_throttler.ShouldUpdateFrame(60)) return;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var storageInfoLookup = state.GetEntityStorageInfoLookup();

            var job = new TweenTargetCleanupJob
            {
                Ecb = ecb,
                EntityStorageInfoLookup = storageInfoLookup
            };

            state.Dependency = job.ScheduleParallel(_cleanupQuery, state.Dependency);
        }

        [BurstCompile]
        internal partial struct TweenTargetCleanupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;

            private void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, in TweenTarget target)
            {
                if (target.Entity != Entity.Null && !EntityStorageInfoLookup.Exists(target.Entity))
                {
                    Ecb.DestroyEntity(chunkIndex, entity);
                }
            }
        }
    }
}
