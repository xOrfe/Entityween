using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;

namespace Entityween.Scripts
{
    public class OrderProcessSystem : JobComponentSystem
    {
        private EndInitializationEntityCommandBufferSystem _entityCommandBufferSystem;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }
        
        [BurstCompile]
        private struct ChunkChangeJob: IJobChunk
        {
            [ReadOnly] public double JobTime;
            public ComponentTypeHandle<Translation> TransformType;
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public SharedComponentTypeHandle<ChunkFloat3ChangeOrder> MoveByOrderSharedComponentDataType;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public EntityManager EntityManager;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkTransforms = chunk.GetNativeArray(TransformType);
                var chunkEntities = chunk.GetNativeArray(EntityType);
                var order = chunk.GetSharedComponentData(MoveByOrderSharedComponentDataType, EntityManager);
                
                var orderStart = order.StartTime;
                var orderEnd = orderStart + order.Time;
                
                if (JobTime > orderEnd)
                {
                    for (int i = 0;i < chunkEntities.Length;i++)
                    {
                        CommandBuffer.RemoveComponent<ChunkFloat3ChangeOrder>(chunkIndex,chunkEntities[i]);
                    }
                    return;
                }
                
                var myJobTime = ((float)JobTime - orderStart) / order.Time;
                var ease = order.MyEaseMethod.Invoke(myJobTime);
                
                // ReSharper disable once HeapView.BoxingAllocation
                //Debug.Log($"{myJobTime}-------{ease}");
                
                for (var i = 0;i < chunk.Count;i++)
                {
                    float3 trns = chunkTransforms[i].Value;
                    order.MyActionMethod.Invoke(ref trns,ref order.ChangeStart,ref order.ChangeFactor, ease);
                    
                    chunkTransforms[i] = new Translation
                    {
                        Value = trns[i]
                    };
                }
            }
        }
        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityQuery moveByOrderQuery = GetEntityQuery(typeof(Translation) , ComponentType.ReadOnly<ChunkFloat3ChangeOrder>());
            var jobTime = Time.ElapsedTime;
            var entityType = GetEntityTypeHandle();
            var moveByOrderSharedComponentDataType = GetSharedComponentTypeHandle<ChunkFloat3ChangeOrder>();
            var transformType = GetComponentTypeHandle<Translation>();
            
            var job = new ChunkChangeJob
            {
                JobTime = jobTime,
                EntityType = entityType,
                TransformType = transformType,
                MoveByOrderSharedComponentDataType = moveByOrderSharedComponentDataType,
                CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
                EntityManager = EntityManager
            }.Schedule(moveByOrderQuery, inputDeps);
            _entityCommandBufferSystem.AddJobHandleForProducer(job);
            return job;
        }
    }
}