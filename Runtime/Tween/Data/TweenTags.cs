using Unity.Entities;

namespace XO.Entityween
{
    public readonly struct MoveTo : IComponentData
    {
        public readonly int Index;

        public MoveTo(int index)
        {
            Index = index;
        }
    }
    public readonly struct MoveToWorld : IComponentData
    {
        public readonly int Index;

        public MoveToWorld(int index)
        {
            Index = index;
        }
    }
    public readonly struct RotateTo : IComponentData
    {
        public readonly int Index;

        public RotateTo(int index)
        {
            Index = index;
        }
    }
    public readonly struct RotateToWorld : IComponentData
    {
        public readonly int Index;

        public RotateToWorld(int index)
        {
            Index = index;
        }
    }
    public readonly struct ScaleTo : IComponentData
    {
        public readonly int Index;

        public ScaleTo(int index)
        {
            Index = index;
        }
    }

    public readonly struct ScaleToUniform : IComponentData
    {
        public readonly int Index;

        public ScaleToUniform(int index)
        {
            Index = index;
        }
    }
}