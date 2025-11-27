using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween
{
    public class ChaseAuthoring : MonoBehaviour
    {
        public GameObject target;
        public Vector2 tStepMinMax = new Vector2(0.01f, 0.075f);
        public float maxStepTime = 1f;
        public bool isOverride = false;
        public ChaseType chaseType = ChaseType.ChasePosition;

        public short updateOrder = 0;

        private class ChaseAuthoringBaker : Baker<ChaseAuthoring>
        {
            public override void Bake(ChaseAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                var target = GetEntity(authoring.target, TransformUsageFlags.None);
                var chaseAttachCall = new ChaseAttachCall(target, authoring.chaseType,
                    new float2(authoring.tStepMinMax.x, authoring.tStepMinMax.y), authoring.maxStepTime,
                    authoring.isOverride,
                    authoring.updateOrder);
                AddComponent(entity, chaseAttachCall);
            }
        }
    }
}