using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace XO.Entityween
{
    public class ChaseAuthoring : MonoBehaviour
    {
        public GameObject target;
        public ChaseType chaseType = ChaseType.ChasePosition;
        [FormerlySerializedAs("dampMode")]
        public ChaseMode mode = ChaseMode.SmoothStep;
        public float smoothTime = 0.15f;
        public float maxSpeed = float.PositiveInfinity;

        private class ChaseAuthoringBaker : Baker<ChaseAuthoring>
        {
            public override void Bake(ChaseAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                var target = GetEntity(authoring.target, TransformUsageFlags.Dynamic);

                switch (authoring.chaseType)
                {
                    case ChaseType.ChasePosition:
                        {
                            var bp = authoring.target == null
                                ? entity.ChasePosition((float3)authoring.transform.localPosition)
                                : entity.ChasePosition(target);
                            ApplyParams(bp, authoring);
                            bp.Play(this);
                        }
                        break;
                    case ChaseType.ChaseRotation:
                        {
                            var bp = authoring.target == null
                                ? entity.ChaseRotation((quaternion)authoring.transform.localRotation)
                                : entity.ChaseRotation(target);
                            ApplyParams(bp, authoring);
                            bp.Play(this);
                        }
                        break;
                    case ChaseType.Look:
                        {
                            if (authoring.target != null)
                            {
                                var bp = entity.Look(target);
                                ApplyParams(bp, authoring);
                                bp.Play(this);
                            }
                        }
                        break;
                    case ChaseType.ChasePositionAndRotation:
                        {
                            var bp = authoring.target == null
                                ? entity.ChasePositionAndRotation(new float4x4(authoring.transform.localRotation, authoring.transform.localPosition))
                                : entity.ChasePositionAndRotation(target);
                            ApplyParams(bp, authoring);
                            bp.Play(this);
                        }
                        break;
                    case ChaseType.ChasePositionAndLook:
                        {
                            var bp = authoring.target == null
                                ? entity.ChasePositionAndLook(new float4x4(authoring.transform.localRotation, authoring.transform.localPosition))
                                : entity.ChasePositionAndLook(target);
                            ApplyParams(bp, authoring);
                            bp.Play(this);
                        }
                        break;
                }
            }

            private void ApplyParams<T>(ChaseBuilder<T> bp, ChaseAuthoring authoring) where T : unmanaged
            {
                bp.Chase.Mode = authoring.mode;
                bp.Chase.SmoothTime = authoring.smoothTime;
                bp.Chase.MaxSpeed = authoring.maxSpeed;
            }
        }
    }
}
