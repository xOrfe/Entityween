using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public class GameObjectShowcaseItem : MonoBehaviour
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

        private Entity tweenEntity;
        private Entity targetEntity;
        private Entity chaserEntity;
        private GameObject labelGo;

        private void Start()
        {
            CreateLabel();
        }

        private void OnEnable()
        {
            PlayTween();
        }

        private void OnDisable()
        {
            StopTween();
        }

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;

            SyncTargetEntity(em);
            SyncGameObjectFromChaser(em);
            UpdateLabel();
        }

        private void OnDestroy()
        {
            if (labelGo != null)
            {
                Destroy(labelGo);
            }
        }

        private void StopTween()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                DestroyIfAlive(em, tweenEntity);
                DestroyIfAlive(em, chaserEntity);
                DestroyIfAlive(em, targetEntity);
            }

            tweenEntity = Entity.Null;
            chaserEntity = Entity.Null;
            targetEntity = Entity.Null;
        }

        private void PlayTween()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;

            StopTween();

            switch (preset)
            {
                case ShowcasePreset.MoveLocal:
                    var localStart = (float3)transform.localPosition;
                    tweenEntity = transform.MoveToLocal(localStart + moveOffset, duration)
                        .From(localStart)
                        .Ease(ease)
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.MoveWorld:
                    tweenEntity = transform.MoveTo((float3)transform.position + moveOffset, duration)
                        .Ease(ease)
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.MoveWithChase:
                    tweenEntity = transform.MoveTo((float3)transform.position + moveOffset, duration)
                        .Ease(ease)
                        .Chase(chaseSmoothTime, ChaseMode.SmoothDamp, killOnChase: true)
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.RotateWorld:
                    tweenEntity = transform.RotateTo(quaternion.EulerXYZ(math.radians(rotationDegrees)), duration)
                        .From(quaternion.identity)
                        .Ease(ease)
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.RotateLocal:
                    tweenEntity = transform.RotateToLocal(quaternion.EulerXYZ(math.radians(rotationDegrees)), duration)
                        .From(quaternion.identity)
                        .Ease(ease)
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.ScalePingPong:
                    tweenEntity = transform.ScaleTo(scaleTarget, duration)
                        .From(new float3(1f))
                        .Ease(ease)
                        .Loop(LoopType.PingPong)
                        .Play(em);
                    break;

                case ShowcasePreset.ScaleUniform:
                    tweenEntity = transform.ScaleToUniform(uniformScaleTarget, duration)
                        .From(transform.localScale.x)
                        .Ease(ease)
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.SplinePath:
                    if (splinePath != null && splinePath.points != null && splinePath.points.Length > 0)
                    {
                        splinePath.ValidatePoints();
                        float3[] flatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(
                            splinePath.splineType,
                            splinePath.isClosed,
                            splinePath.points,
                            splinePath.tangents
                        );

                        using (var nativeFlatPoints = new NativeArray<float3>(flatPoints, Allocator.Temp))
                        {
                            tweenEntity = transform.MoveTo(splinePath.points[0], duration)
                                .Along(nativeFlatPoints, splinePath.splineType, splinePath.isClosed)
                                .Ease(ease)
                                .Loop(loop)
                                .Play(em);
                        }
                    }
                    break;

                case ShowcasePreset.ChaseTarget:
                    if (chaseTarget != null)
                    {
                        CreateChasePair(em, transform.position, transform.rotation, chaseTarget.transform.position);

                        chaserEntity.ChasePosition(targetEntity)
                            .SmoothDamp(chaseSmoothTime)
                            .Play(em);
                    }
                    break;

                case ShowcasePreset.ChasePositionAndRotation:
                    if (chaseTarget != null)
                    {
                        CreateChasePair(em, transform.position, transform.rotation, chaseTarget.transform.position);

                        chaserEntity.ChasePositionAndRotation(targetEntity)
                            .SmoothDamp(chaseSmoothTime)
                            .Play(em);
                    }
                    break;

                case ShowcasePreset.ChasePositionAndLook:
                    if (chaseTarget != null)
                    {
                        CreateChasePair(em, transform.position, transform.rotation, chaseTarget.transform.position);

                        chaserEntity.ChasePositionAndLook(targetEntity)
                            .SmoothDamp(chaseSmoothTime)
                            .Play(em);
                    }
                    break;

                case ShowcasePreset.SequenceShowcase:
                    var sequenceStart = (float3)transform.localPosition;
                    tweenEntity = Sequence.Create()
                        .Append(transform.MoveToLocal(sequenceStart + moveOffset, duration * 0.5f).From(sequenceStart).Ease(EaseType.OutQuad))
                        .Append(transform.RotateToLocal(quaternion.RotateY(math.PI), duration * 0.5f).From(quaternion.identity))
                        .Append(transform.MoveToLocal(sequenceStart, duration * 0.5f).From(sequenceStart + moveOffset).Ease(EaseType.InQuad))
                        .Loop(loop)
                        .Play(em);
                    break;

                case ShowcasePreset.LookAtTarget:
                    if (lookTarget != null)
                    {
                        CreateChasePair(em, transform.position, transform.rotation, lookTarget.transform.position);

                        chaserEntity.Look(targetEntity)
                            .SmoothDamp(lookSmoothTime)
                            .Play(em);
                    }
                    break;
            }
        }

        private void CreateChasePair(EntityManager em, Vector3 startPosition, Quaternion startRotation, Vector3 targetPosition)
        {
            chaserEntity = em.CreateEntity();
            targetEntity = em.CreateEntity();

            em.AddComponentData(chaserEntity, LocalTransform.FromPositionRotation(startPosition, startRotation));
            em.AddComponentData(targetEntity, LocalTransform.FromPosition(targetPosition));
        }

        private void SyncTargetEntity(EntityManager em)
        {
            if (targetEntity == Entity.Null || !em.Exists(targetEntity)) return;

            Transform target = preset == ShowcasePreset.LookAtTarget
                ? lookTarget != null ? lookTarget.transform : null
                : chaseTarget != null ? chaseTarget.transform : null;

            if (target != null)
            {
                em.SetComponentData(targetEntity, LocalTransform.FromPosition(target.position));
            }
        }

        private void SyncGameObjectFromChaser(EntityManager em)
        {
            if (chaserEntity == Entity.Null || !em.Exists(chaserEntity)) return;

            var currentTransform = em.GetComponentData<LocalTransform>(chaserEntity);
            if (preset != ShowcasePreset.LookAtTarget)
            {
                transform.position = currentTransform.Position;
            }

            if (preset != ShowcasePreset.ChaseTarget)
            {
                transform.rotation = currentTransform.Rotation;
            }
        }

        private void UpdateLabel()
        {
            if (labelGo == null) return;

            if (Camera.main != null)
            {
                labelGo.transform.rotation = Quaternion.LookRotation(labelGo.transform.position - Camera.main.transform.position);
            }
        }

        private static void DestroyIfAlive(EntityManager em, Entity entity)
        {
            if (entity != Entity.Null && em.Exists(entity))
            {
                em.DestroyEntity(entity);
            }
        }

        private void CreateLabel()
        {
            if (string.IsNullOrEmpty(description)) return;
            labelGo = new GameObject($"GameObjectShowcaseLabel_{gameObject.name}");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            var tmp = labelGo.AddComponent<TMPro.TextMeshPro>();
            tmp.text = description;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize = 5.0f;
            tmp.color = Color.white;
        }
    }
}
