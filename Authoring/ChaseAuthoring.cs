using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace XO.Entityween
{
    public class ChaseAuthoring : MonoBehaviour
    {
        public GameObject target;
        public Vector2 tStepMinMax = new Vector2(0.01f, 0.075f);
        public float maxStepTime = 1f;
        public bool isOverride = false;
        public bool chasePosition = true;
        public bool chaseRotation = true;
        public bool lookRotation = false;

        public short updateOrder = 0;

        private class ChaseAuthoringBaker : Baker<ChaseAuthoring>
        {
            public override void Bake(ChaseAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                var target = GetEntity(authoring.target, TransformUsageFlags.Dynamic);
                var chaseAttachCall = new ChaseAttachCall(target,
                    new float2(authoring.tStepMinMax.x, authoring.tStepMinMax.y), authoring.maxStepTime,
                    authoring.isOverride,
                    authoring.chasePosition, authoring.chaseRotation, authoring.lookRotation,
                    authoring.updateOrder);
                AddComponent(entity, chaseAttachCall);
            }
        }
    }
}