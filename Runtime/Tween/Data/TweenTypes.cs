using Unity.Entities;

namespace XO.Entityween
{
    public enum TweenSpace : byte
    {
        Local,
        World
    }

    public enum TweenType : byte
    {
        None,
        Wait,
        Callback,
        MoveTo,
        RotateTo,
        ScaleTo,
        ScaleToUniform,
        FloatTo,
        Float2To,
        Float3To,
        QuaternionTo
    }

    internal enum TweenValueKind : byte
    {
        None,
        Float,
        Float2,
        Float3,
        Quaternion
    }

    internal struct TweenTarget : IComponentData
    {
        public Entity Entity;
        public TweenType TweenType;
        public TweenSpace Space;
    }
}
