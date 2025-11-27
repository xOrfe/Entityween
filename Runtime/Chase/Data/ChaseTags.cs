using Unity.Entities;

namespace XO.Entityween
{
    public readonly struct ChasePositionTag : IComponentData, IEnableableComponent
    {
        public readonly int Index;
        public ChasePositionTag(int index)
        {
            Index = index;
        }
    }
    public readonly struct ChaseRotationTag : IComponentData, IEnableableComponent
    {
        public readonly int Index;
        public ChaseRotationTag(int index)
        {
            Index = index;
        }
    }
    public readonly struct LookTag : IComponentData, IEnableableComponent
    {
        public readonly int Index;
        public LookTag(int index)
        {
            Index = index;
        }
    }
}