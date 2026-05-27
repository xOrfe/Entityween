using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{
    internal enum TweenTransformBinding : byte
    {
        Position,
        Rotation,
        Scale,
        ScaleUniform
    }

    internal struct TweenTransformTarget : IComponentData
    {
        public TweenTransformBinding Binding;
        public TweenSpace Space;
    }

    internal sealed class TweenTransformReference : IComponentData
    {
        public Transform Transform;
    }
}
