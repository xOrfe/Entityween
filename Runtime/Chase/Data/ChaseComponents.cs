using Unity.Entities;
using Unity.Mathematics;
using System.ComponentModel;

namespace XO.Entityween
{
    public enum ChaseMode : byte
    {
        Snap,
        SmoothDamp,
        SmoothStep
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ChasePosition : IComponentData, IEnableableComponent
    {
        public float3 TargetPosition;
        public float3 Velocity;
        public TweenSpace Space;
        public ChaseMode Mode;
        public float SmoothTime;
        public float MaxSpeed;
        public bool KillOnChase;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ChaseRotation : IComponentData, IEnableableComponent
    {
        public quaternion TargetQuaternion;
        public quaternion Velocity;
        public TweenSpace Space;
        public ChaseMode Mode;
        public float SmoothTime;
        public float MaxSpeed;
        public bool KillOnChase;
    }

    internal struct Look : IComponentData, IEnableableComponent
    {
        public float3 TargetPosition;
        public float3 Velocity;
        public ChaseMode Mode;
        public float SmoothTime;
        public float MaxSpeed;
        public bool KillOnChase;
    }

    internal struct ChaseScale : IComponentData, IEnableableComponent
    {
        public float3 TargetScale;
        public bool IsUniform;
        public float3 Velocity;
        public ChaseMode Mode;
        public float SmoothTime;
        public float MaxSpeed;
        public bool KillOnChase;
    }

    /// <summary>
    /// Tag and settings component to apply damping (SmoothDamp or SmoothStep).
    /// If NOT attached to the entity, the chase will instantly override/snap to the target.
    /// </summary>
    internal struct ChaseDamp : IComponentData
    {
        public float SmoothTime;
        public float MaxSpeed;
        public ChaseMode Mode;
    }

    /// <summary>
    /// Reference component attached when chasing a dynamic Entity.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ChaseTargetEntity : IComponentData
    {
        public Entity Target;
    }

    internal struct ChasePositionTweenSource : IComponentData
    {
        public Entity GhostEntity;
        public TweenSpace Space;
        public bool SourceCompleted;
    }

    internal struct ChaseRotationTweenSource : IComponentData
    {
        public Entity GhostEntity;
        public TweenSpace Space;
        public bool SourceCompleted;
    }

    internal struct LookTweenSource : IComponentData
    {
        public Entity GhostEntity;
        public bool SourceCompleted;
    }

    internal struct ChaseScaleTweenSource : IComponentData
    {
        public Entity GhostEntity;
        public TweenSpace Space;
        public bool SourceCompleted;
    }
}
