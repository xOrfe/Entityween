using Unity.Entities;

namespace XO.Entityween
{
    internal enum TweenGameObjectBinding : byte
    {
        Position,
        Rotation,
        Scale
    }

    internal struct TweenGameObjectTarget : IComponentData
    {
        public TweenGameObjectBinding Binding;
        public TweenSpace Space;
    }

    internal struct TweenMemberHook<T> : IComponentData where T : unmanaged
    {
    }

    internal struct TweenCallbackHook<T> : IComponentData where T : unmanaged
    {
    }
}
