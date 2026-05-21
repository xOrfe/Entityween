using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public enum ShowcasePreset
    {
        MoveLocal,
        MoveWorld,
        MoveWithChase,
        RotateWorld,
        RotateLocal,
        ScalePingPong,
        ScaleUniform,
        SplinePath,
        ChaseTarget,
        ChasePositionAndRotation,
        ChasePositionAndLook,
        SequenceShowcase,
        LookAtTarget
    }

    public struct ShowcaseText : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public class EntityweenShowcaseItem : MonoBehaviour
    {
        [Header("Showcase Text")]
        [Tooltip("The description text displayed floating above the object.")]
        public string description;

        [Header("Tween Configuration")]
        [Tooltip("The preset animation behavior for this showcase item.")]
        public ShowcasePreset preset;
        public float duration = 2.0f;
        public EaseType ease = EaseType.InOutSine;
        public LoopType loop = LoopType.PingPong;

        [Header("Preset Details")]
        [Tooltip("Offset for MoveLocal and Sequence presets.")]
        public float3 moveOffset = new float3(0f, 3f, 0f);

        [Tooltip("Euler destination for rotation presets.")]
        public float3 rotationDegrees = new float3(0f, 178f, 0f);

        [Tooltip("Destination scale for scale presets.")]
        public float3 scaleTarget = new float3(1.8f);

        [Tooltip("Uniform scale destination for ScaleUniform.")]
        public float uniformScaleTarget = 2f;

        [Tooltip("Spline path configuration (used for SplinePath preset).")]
        public SerializableSpline<float3> splinePath = new SerializableSpline<float3>();

        [Tooltip("The target GameObject to chase (used for ChaseTarget preset).")]
        public GameObject chaseTarget;
        public float chaseSmoothTime = 0.3f;

        [Tooltip("The target GameObject to look at (used for LookAtTarget preset).")]
        public GameObject lookTarget;
        public float lookSmoothTime = 0.15f;
    }

    public class EntityweenShowcaseItemBaker : Baker<EntityweenShowcaseItem>
    {
        public override void Bake(EntityweenShowcaseItem authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add the floating text description component
            AddComponent(entity, new ShowcaseText { Value = authoring.description ?? "" });

            switch (authoring.preset)
            {
                case ShowcasePreset.MoveLocal:
                    var localStart = (float3)authoring.transform.localPosition;
                    entity.MoveToLocal(authoring.duration, localStart)
                        .To(localStart + authoring.moveOffset)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.MoveWorld:
                    entity.MoveToWorld((float3)authoring.transform.position + authoring.moveOffset, authoring.duration)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.MoveWithChase:
                    entity.MoveToWorld((float3)authoring.transform.position + authoring.moveOffset, authoring.duration)
                        .Ease(authoring.ease)
                        .Chase(authoring.chaseSmoothTime, ChaseMode.SmoothDamp, killOnChase: true)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.RotateWorld:
                    entity.RotateToWorld(authoring.duration, quaternion.identity)
                        .To(quaternion.EulerXYZ(math.radians(authoring.rotationDegrees)))
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.RotateLocal:
                    entity.RotateToLocal(authoring.duration, quaternion.identity)
                        .To(quaternion.EulerXYZ(math.radians(authoring.rotationDegrees)))
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.ScalePingPong:
                    entity.ScaleTo(authoring.duration, new float3(1f))
                        .To(authoring.scaleTarget)
                        .Ease(authoring.ease)
                        .Loop(LoopType.PingPong)
                        .Play(this);
                    break;

                case ShowcasePreset.ScaleUniform:
                    entity.ScaleToUniform(authoring.duration, authoring.transform.localScale.x)
                        .To(authoring.uniformScaleTarget)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.SplinePath:
                    if (authoring.splinePath != null && authoring.splinePath.points != null && authoring.splinePath.points.Length > 0)
                    {
                        authoring.splinePath.ValidatePoints();
                        float3[] flatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(
                            authoring.splinePath.splineType,
                            authoring.splinePath.isClosed,
                            authoring.splinePath.points,
                            authoring.splinePath.tangents
                        );

                        entity.MoveToWorld(authoring.duration, authoring.splinePath.points[0])
                            .Along(flatPoints, authoring.splinePath.splineType, authoring.splinePath.isClosed)
                            .Ease(authoring.ease)
                            .Loop(authoring.loop)
                            .Visualize()
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.ChaseTarget:
                    var targetEntity = GetEntity(authoring.chaseTarget, TransformUsageFlags.Dynamic);
                    if (targetEntity != Entity.Null)
                    {
                        entity.ChasePosition(targetEntity)
                            .SmoothDamp(authoring.chaseSmoothTime)
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.ChasePositionAndRotation:
                    var chasePoseTarget = GetEntity(authoring.chaseTarget, TransformUsageFlags.Dynamic);
                    if (chasePoseTarget != Entity.Null)
                    {
                        entity.ChasePositionAndRotation(chasePoseTarget)
                            .SmoothDamp(authoring.chaseSmoothTime)
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.ChasePositionAndLook:
                    var chaseLookTarget = GetEntity(authoring.chaseTarget, TransformUsageFlags.Dynamic);
                    if (chaseLookTarget != Entity.Null)
                    {
                        entity.ChasePositionAndLook(chaseLookTarget)
                            .SmoothDamp(authoring.chaseSmoothTime)
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.SequenceShowcase:
                    var sequenceStart = (float3)authoring.transform.localPosition;
                    Sequence.Create()
                        .Append(entity.MoveToLocal(authoring.duration * 0.5f, sequenceStart).To(sequenceStart + authoring.moveOffset).Ease(EaseType.OutQuad))
                        .Append(entity.RotateToLocal(authoring.duration * 0.5f, quaternion.identity).To(quaternion.RotateY(math.PI)))
                        .Append(entity.MoveToLocal(authoring.duration * 0.5f, sequenceStart + authoring.moveOffset).To(sequenceStart).Ease(EaseType.InQuad))
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.LookAtTarget:
                    var lookTargetEntity = GetEntity(authoring.lookTarget, TransformUsageFlags.Dynamic);
                    if (lookTargetEntity != Entity.Null)
                    {
                        entity.Look(lookTargetEntity)
                            .SmoothDamp(authoring.lookSmoothTime)
                            .Play(this);
                    }
                    break;
            }
        }
    }
}
