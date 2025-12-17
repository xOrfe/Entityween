using Unity.Entities;

namespace XO.Entityween
{
    public readonly struct ChasePositionTag : IComponentData, IEnableableComponent
    {
        public readonly Entity Target;
        public readonly int Index;
        
        public ChasePositionTag(Entity target, int index)
        {
            Index = index;
            this.Target = target;
        }
        public ChasePositionTag FromEntity(Entity entity)
        {
            return new ChasePositionTag(entity, Index);
        }
    }

    public readonly struct ChaseRotationTag : IComponentData, IEnableableComponent
    {
        public readonly Entity Target;
        public readonly int Index;

        public ChaseRotationTag(Entity target, int index)
        {
            Target = target;
            Index = index;
        }
        public ChaseRotationTag FromEntity(Entity entity)
        {
            return new ChaseRotationTag(entity, Index);
        }
    }

    public readonly struct LookTag : IComponentData, IEnableableComponent
    {
        public readonly Entity Target;
        public readonly int Index;

        public LookTag(Entity target, int index)
        {
            Target = target;
            Index = index;
        }
        public LookTag FromEntity(Entity entity)
        {
            return new LookTag(entity, Index);
        }
    }
}