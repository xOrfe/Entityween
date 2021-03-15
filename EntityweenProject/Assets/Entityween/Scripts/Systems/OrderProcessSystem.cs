using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.PlayerLoop;

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
        
        //[BurstCompile]
        private struct ChunkChangeJob: IJobChunk
        {
            [ReadOnly] public double JobTime;
            public ArchetypeChunkComponentType<Translation> TransformType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkSharedComponentType<ChunkFloat3ChangeOrder> MoveByOrderSharedComponentDataType;
            public EntityCommandBuffer.Concurrent CommandBuffer;
            
            
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkTransforms = chunk.GetNativeArray(TransformType);
                var chunkEntities = chunk.GetNativeArray(EntityType);
                var order = chunk.GetSharedComponentData(MoveByOrderSharedComponentDataType, World.DefaultGameObjectInjectionWorld.EntityManager);
                
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
            var entityType = GetArchetypeChunkEntityType();
            var moveByOrderSharedComponentDataType = GetArchetypeChunkSharedComponentType<ChunkFloat3ChangeOrder>();
            var transformType = GetArchetypeChunkComponentType<Translation>();
            
            var job = new ChunkChangeJob
            {
                JobTime = jobTime,
                EntityType = entityType,
                TransformType = transformType,
                MoveByOrderSharedComponentDataType = moveByOrderSharedComponentDataType,
                CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(moveByOrderQuery, inputDeps);
            _entityCommandBufferSystem.AddJobHandleForProducer(job);
            return job;
        }
    }
}