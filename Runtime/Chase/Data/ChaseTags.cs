using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween
{
    public readonly struct ChasePositionTag : IComponentData, IEnableableComponent
    {
        public readonly bool IsEntity;
        public readonly Entity TargetEntity;
        public readonly float3 TargetPosition;
        public readonly int Index;

        public ChasePositionTag(Entity target, int index)
        {
            IsEntity = true;
            TargetEntity = target;
            TargetPosition = float3.zero;
            Index = index;
        }

        public ChasePositionTag(float3 target, int index)
        {
            IsEntity = false;
            TargetEntity = Entity.Null;
            TargetPosition = target;
            Index = index;
        }

        public ChasePositionTag FromEntity(Entity target)
        {
            return new ChasePositionTag(target, Index);
        }

        public ChasePositionTag FromPosition(float3 target)
        {
            return new ChasePositionTag(target, Index);
        }
    }

    public readonly struct ChaseRotationTag : IComponentData, IEnableableComponent
    {
        public readonly bool IsEntity;
        public readonly Entity TargetEntity;
        public readonly quaternion TargetQuaternion;
        public readonly int Index;

        public ChaseRotationTag(Entity target, int index)
        {
            IsEntity = true;
            TargetEntity = target;
            TargetQuaternion = quaternion.identity;
            Index = index;
        }

        public ChaseRotationTag(quaternion target, int index)
        {
            IsEntity = false;
            TargetEntity = Entity.Null;
            TargetQuaternion = target;
            Index = index;
        }

        public ChaseRotationTag FromEntity(Entity target)
        {
            return new ChaseRotationTag(target, Index);
        }

        public ChaseRotationTag FromQuaternion(quaternion target)
        {
            return new ChaseRotationTag(target, Index);
        }
    }

    public readonly struct LookTag : IComponentData, IEnableableComponent
    {
        public readonly bool IsEntity;
        public readonly Entity Target;
        public readonly float3 TargetPosition;
        public readonly int Index;

        public LookTag(Entity target, int index)
        {
            IsEntity = true;
            Target = target;
            TargetPosition = float3.zero;
            Index = index;
        }

        public LookTag(float3 target, int index)
        {
            IsEntity = true;
            Target = Entity.Null;
            TargetPosition = target;
            Index = index;
        }

        public LookTag FromEntity(Entity target)
        {
            return new LookTag(target, Index);
        }

        public LookTag FromPosition(float3 target)
        {
            return new LookTag(target, Index);
        }
    }
}