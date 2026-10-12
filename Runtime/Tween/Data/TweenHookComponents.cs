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

    internal struct TweenTransformReference : IComponentData
    {
        public UnityObjectRef<Transform> Transform;
    }

    internal struct TweenTransformCleanup : ICleanupComponentData
    {
    }
}
