using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{
    [BurstCompile]
    public static class Entityween
    {
        public static SequenceAspect Sequence(World world)
        {
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<Sequence>(entity);
            entityManager.AddBuffer<SequenceTweenElement>(entity);
            entityManager.SetName(entity, "Sequence");
            return entityManager.GetAspect<SequenceAspect>(entity);
        }
    }
}