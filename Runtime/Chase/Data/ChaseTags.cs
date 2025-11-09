using Unity.Entities;

namespace XO.Entityween
{
    public readonly struct ChaseTag : IComponentData
    {
        public readonly int Index;
        public ChaseTag(int index)
        {
            Index = index;
        }
    }
}