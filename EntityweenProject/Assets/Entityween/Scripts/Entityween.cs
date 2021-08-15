using Entityween.Scripts;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Entityween
{
    public static class Entityween
    {
        public static void Move(
            in EntityCommandBuffer.ParallelWriter parallelWriter, 
            in int sortKey, 
            in Entity entity, 
            in float3 start, 
            in float3 end, 
            in float duration,
            in FunctionPointer<OnCompleteJobs.OnCompleteDelegate> onComplete)
        {
            parallelWriter.AddComponent(sortKey, entity, new ChunkFloat3ChangeOrder(start,end,duration,onComplete));
        }
        public static void Move(
            in EntityCommandBuffer entityCommandBuffer, 
            in Entity entity, 
            in float3 start, 
            in float3 end, 
            in float duration,
            in OnCompleteJobs.OnCompleteJobTypes onCompleteType)
        {
            entityCommandBuffer.AddComponent(entity,new MoveOrder(start,end,duration, OnCompleteJobs.CompileOnCompleteJob(onCompleteType)) );
            entityCommandBuffer.AddComponent<MoveOrderOnStart>(entity);
        }
        public static void Move(
            in EntityManager entityManager, 
            in Entity entity, 
            in float3 start, 
            in float3 end, 
            in float duration,
            in FunctionPointer<OnCompleteJobs.OnCompleteDelegate> onComplete)
        {
            entityManager.AddComponent<MoveOrder>(entity);
            entityManager.SetComponentData(entity,new MoveOrder(start,end,duration,onComplete));
            entityManager.AddComponent<MoveOrderOnStart>(entity);
        }
    }
}
