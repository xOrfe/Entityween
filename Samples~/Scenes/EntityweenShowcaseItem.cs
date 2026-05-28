using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public struct ShowcaseText : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public class EntityweenShowcaseItem : BaseShowcaseItem
    {
    }

    public class EntityweenShowcaseItemBaker : Baker<EntityweenShowcaseItem>
    {
        public override void Bake(EntityweenShowcaseItem authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ShowcaseText { Value = authoring.description ?? "" });

            switch (authoring.preset)
            {
                case ShowcasePreset.MoveLocal:
                    var localStart = (float3)authoring.transform.localPosition;
                    entity.MoveToLocal(localStart + authoring.moveOffset, authoring.duration)
                        .From(localStart)
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
                    entity.RotateToWorld(quaternion.EulerXYZ(math.radians(authoring.rotationDegrees)), authoring.duration)
                        .From(quaternion.identity)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.RotateLocal:
                    entity.RotateToLocal(quaternion.EulerXYZ(math.radians(authoring.rotationDegrees)), authoring.duration)
                        .From(quaternion.identity)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.ScalePingPong:
                    entity.ScaleTo(authoring.scaleTarget, authoring.duration)
                        .From(new float3(1f))
                        .Ease(authoring.ease)
                        .Loop(LoopType.PingPong)
                        .Play(this);
                    break;

                case ShowcasePreset.ScaleUniform:
                    entity.ScaleToUniform(authoring.uniformScaleTarget, authoring.duration)
                        .From(authoring.transform.localScale.x)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.SplinePath:
                    if (TryGetSplinePoints(authoring, out var flatPoints))
                    {
                        using (var nativeFlatPoints = new NativeArray<float3>(flatPoints, Allocator.Temp))
                        {
                            entity.MoveToWorld(authoring.splinePath.points[0], authoring.duration)
                                .Along(nativeFlatPoints, authoring.splinePath.splineType, authoring.splinePath.isClosed)
                                .Ease(authoring.ease)
                                .Loop(authoring.loop)
                                .Visualize()
                                .Play(this);
                        }
                    }
                    break;

                case ShowcasePreset.ChaseTarget:
                    if (TryGetEntity(authoring.chaseTarget, out var chasePositionTarget))
                    {
                        entity.ChasePosition(chasePositionTarget)
                            .SmoothDamp(authoring.chaseSmoothTime)
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.ChasePositionAndRotation:
                    if (TryGetEntity(authoring.chaseTarget, out var chasePoseTarget))
                    {
                        entity.ChasePositionAndRotation(chasePoseTarget)
                            .SmoothDamp(authoring.chaseSmoothTime)
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.ChasePositionAndLook:
                    if (TryGetEntity(authoring.chaseTarget, out var chaseLookTarget))
                    {
                        entity.ChasePositionAndLook(chaseLookTarget)
                            .SmoothDamp(authoring.chaseSmoothTime)
                            .Play(this);
                    }
                    break;

                case ShowcasePreset.SequenceShowcase:
                    var sequenceStart = (float3)authoring.transform.localPosition;
                    Sequence.Create()
                        .Append(entity.MoveToLocal(sequenceStart + authoring.moveOffset, authoring.duration * 0.5f).From(sequenceStart).Ease(EaseType.OutQuad))
                        .Append(entity.RotateToLocal(quaternion.RotateY(math.PI), authoring.duration * 0.5f).From(quaternion.identity))
                        .Append(entity.MoveToLocal(sequenceStart, authoring.duration * 0.5f).From(sequenceStart + authoring.moveOffset).Ease(EaseType.InQuad))
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.LookAtTarget:
                    if (TryGetEntity(authoring.lookTarget, out var lookTargetEntity))
                    {
                        entity.Look(lookTargetEntity)
                            .SmoothDamp(authoring.lookSmoothTime)
                            .Play(this);
                    }
                    break;
            }
        }

        private bool TryGetEntity(GameObject source, out Entity entity)
        {
            entity = source == null
                ? Entity.Null
                : GetEntity(source, TransformUsageFlags.Dynamic);

            return entity != Entity.Null;
        }

        private static bool TryGetSplinePoints(EntityweenShowcaseItem authoring, out float3[] flatPoints)
        {
            flatPoints = null;
            if (authoring.splinePath == null ||
                authoring.splinePath.points == null ||
                authoring.splinePath.points.Length == 0)
            {
                return false;
            }

            authoring.splinePath.ValidatePoints();
            flatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(
                authoring.splinePath.splineType,
                authoring.splinePath.isClosed,
                authoring.splinePath.points,
                authoring.splinePath.tangents);

            return flatPoints != null && flatPoints.Length > 0;
        }
    }
}
