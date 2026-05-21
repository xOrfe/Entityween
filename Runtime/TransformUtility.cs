using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace XO.Entityween
{
    [BurstCompile]
    internal static class TransformUtility
    {
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetWorldPosition(in Entity entity, in float3 newWorldPos, ref LocalTransform localTransform,
            in ComponentLookup<Parent> parentLookup, in ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            if (parentLookup.HasComponent(entity))
            {
                var parentEntity = parentLookup[entity].Value;
                if (localToWorldLookup.HasComponent(parentEntity))
                {
                    var parentLtw = localToWorldLookup[parentEntity];
                    var rel = newWorldPos - parentLtw.Position;
                    localTransform.Position = math.rotate(math.conjugate(math.quaternion(parentLtw.Value)), rel);
                    return;
                }
            }

            localTransform.Position = newWorldPos;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetWorldRotation(in Entity entity, in quaternion newWorldRot,
            ref LocalTransform localTransform, in ComponentLookup<Parent> parentLookup,
            in ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            if (parentLookup.HasComponent(entity))
            {
                var parentEntity = parentLookup[entity].Value;
                if (localToWorldLookup.HasComponent(parentEntity))
                {
                    var parentLtw = localToWorldLookup[parentEntity];
                    localTransform.Rotation = math.mul(math.conjugate(math.quaternion(parentLtw.Value)), newWorldRot);
                    return;
                }
            }

            localTransform.Rotation = newWorldRot;
        }
    }
}
