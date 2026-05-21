using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using XO.Curve;

namespace XO.Entityween.Tests
{
    [TestFixture]
    public class TweenBlueprintTests
    {
        [Test]
        public void MoveTo_RemainsLocalSpaceAlias()
        {
            var tween = Entity.Null.MoveTo(1f, float3.zero).Destination(new float3(1f, 2f, 3f));

            Assert.AreEqual(TweenType.MoveTo, tween.TweenType);
            Assert.AreEqual(TweenSpace.Local, tween.Space);
            Assert.AreEqual(new float3(1f, 2f, 3f), tween.EndPoint);
        }

        [Test]
        public void MoveToWorld_SetsWorldSpace()
        {
            var tween = Entity.Null.MoveToWorld(1f, float3.zero).Destination(new float3(3f, 2f, 1f));

            Assert.AreEqual(TweenType.MoveTo, tween.TweenType);
            Assert.AreEqual(TweenSpace.World, tween.Space);
            Assert.AreEqual(new float3(3f, 2f, 1f), tween.EndPoint);
        }

        [Test]
        public void FloatValueTweens_Overloads()
        {
            var fTween = Entity.Null.FloatTo(2f, 5f);
            Assert.AreEqual(TweenType.FloatTo, fTween.TweenType);
            Assert.IsFalse(fTween.StartFromCurrent);
            Assert.AreEqual(5f, fTween.StartPoint);
            Assert.AreEqual(2f, fTween.SecondsToPlay);

            var f3Tween = Entity.Null.Float3To(4f, new float3(1f, 2f, 3f));
            Assert.AreEqual(TweenType.Float3To, f3Tween.TweenType);
            Assert.IsFalse(f3Tween.StartFromCurrent);
            Assert.AreEqual(new float3(1f, 2f, 3f), f3Tween.StartPoint);
            Assert.AreEqual(4f, f3Tween.SecondsToPlay);
        }

        [Test]
        public void Chase_DefaultsKillOnChaseToFalse()
        {
            var tween = Entity.Null.MoveToWorld(1f, float3.zero)
                .Destination(new float3(1f, 0f, 0f))
                .Chase(0.25f);

            Assert.IsTrue(tween.UseChase);
            Assert.IsFalse(tween.KillOnChase);
        }

        [Test]
        public void ChasePositionAndRotation_AddsSharedTargetReferenceOnce()
        {
            using var world = new World("Entityween Combined Chase Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            var chaser = em.CreateEntity();
            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            chaser.ChasePositionAndRotation(target)
                .SmoothDamp(0.2f)
                .Play(ecb);

            ecb.Playback(em);

            Assert.IsTrue(em.HasComponent<ChasePosition>(chaser));
            Assert.IsTrue(em.HasComponent<ChaseRotation>(chaser));
            Assert.IsTrue(em.HasComponent<ChaseTargetEntity>(chaser));
            Assert.AreEqual(target, em.GetComponentData<ChaseTargetEntity>(chaser).Target);
        }

        [Test]
        public void Play_ChaseMoveToCreatesPositionSourceWithGhostTerminology()
        {
            using var world = new World("Entityween Chase Source Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var tween = target.MoveToWorld(1f, float3.zero)
                .Destination(new float3(1f, 0f, 0f))
                .Chase(0.25f, killOnChase: true);
            var ghost = tween.Play(em);

            Assert.IsTrue(em.HasComponent<TweenControl>(ghost));
            Assert.IsTrue(em.HasComponent<ChasePosition>(target));
            Assert.IsTrue(em.HasComponent<ChasePositionTweenSource>(target));

            var chase = em.GetComponentData<ChasePosition>(target);
            var source = em.GetComponentData<ChasePositionTweenSource>(target);
            Assert.AreEqual(ChaseMode.SmoothStep, chase.Mode);
            Assert.AreEqual(0.25f, chase.SmoothTime);
            Assert.AreEqual(ghost, source.GhostEntity);
            Assert.IsTrue(chase.KillOnChase);
        }

        [Test]
        public void SequencePlay_CreatesSequenceEntityWithScheduledElements()
        {
            using var world = new World("Entityween Sequence Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var sequence = Sequence.Create()
                .Append(target.MoveToWorld(1f, float3.zero).Destination(new float3(1f, 0f, 0f)))
                .Append(target.Wait(0.5f))
                .AppendCallback("Done")
                .Loop(LoopType.Repeat, 1)
                .Play(em);

            Assert.IsTrue(em.HasComponent<Sequence>(sequence));
            Assert.AreEqual(PlaybackState.Playing, em.GetComponentData<Sequence>(sequence).State);
            Assert.IsTrue(em.HasComponent<PlaybackLoop>(sequence));
            Assert.AreEqual(LoopType.Repeat, em.GetComponentData<PlaybackLoop>(sequence).LoopType);

            var elements = em.GetBuffer<SequenceElement>(sequence);
            Assert.AreEqual(3, elements.Length);
            Assert.AreEqual(SequenceElementKind.Tween, elements[0].Kind);
            Assert.AreEqual(TweenSpace.World, elements[0].Space);
            Assert.AreEqual(SequenceElementKind.Wait, elements[1].Kind);
            Assert.AreEqual(SequenceElementKind.Callback, elements[2].Kind);

            Assert.IsTrue(em.HasComponent<TweenSequenceDriven>(elements[0].GhostEntity));
            Assert.IsFalse(em.IsComponentEnabled<TweenControl>(elements[0].GhostEntity));
        }

        [Test]
        public void SequencePlay_PreservesChaseMetadata()
        {
            using var world = new World("Entityween Sequence Chase Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var sequence = Sequence.Create()
                .Append(target.MoveToWorld(1f, float3.zero).Destination(new float3(1f, 0f, 0f)).Chase(0.2f, ChaseMode.SmoothDamp, 10f, true))
                .Play(em);

            var element = em.GetBuffer<SequenceElement>(sequence)[0];
            Assert.IsTrue(element.UseChase);
            Assert.AreEqual(ChaseMode.SmoothDamp, element.ChaseMode);
            Assert.AreEqual(0.2f, element.ChaseSmoothTime);
            Assert.AreEqual(10f, element.ChaseMaxSpeed);
            Assert.IsTrue(element.KillOnChase);
        }

        [Test]
        public void TestChainingPlay()
        {
            using var world = new World("Chaining Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var ghost = target.ScaleTo(new float3(2f), 1f)
                .Ease(EaseType.OutBounce)
                .Play(em);

            Assert.IsTrue(em.HasComponent<TweenControl>(ghost));
        }

        [Test]
        public void CalculateProgress_RepeatHandlesLargeDeltaTime()
        {
            var elapsed = 2.25f;
            var loop = new PlaybackLoop { LoopType = LoopType.Repeat, LoopCount = 0, LoopIndex = 0 };

            var finished = PlaybackUtilities.CalculateProgress(ref elapsed, 1f, ref loop, true, out var normalizedTime);

            Assert.IsFalse(finished);
            Assert.AreEqual(2, loop.LoopIndex);
            Assert.AreEqual(0.25f, elapsed, 0.0001f);
            Assert.AreEqual(0.25f, normalizedTime, 0.0001f);
        }

        [Test]
        public void CalculateProgress_FinitePingPongEndsOnExpectedSide()
        {
            var elapsed = 2.1f;
            var loop = new PlaybackLoop { LoopType = LoopType.PingPong, LoopCount = 1, LoopIndex = 0 };

            var finished = PlaybackUtilities.CalculateProgress(ref elapsed, 1f, ref loop, true, out var normalizedTime);

            Assert.IsTrue(finished);
            Assert.AreEqual(1, loop.LoopIndex);
            Assert.AreEqual(0f, normalizedTime, 0.0001f);
        }

        [Test]
        public void TestAlongManagedArray()
        {
            using var world = new World("Along Managed Array Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            float3[] points = new float3[] 
            { 
                new float3(0f, 0f, 0f), 
                new float3(2f, 5f, 0f), 
                new float3(5f, 0f, 0f) 
            };
            float3[] tangents = null;

            SplineUtility.InitializeOrResizeTangents(
                SplineType.CatmullRom, 
                isClosed: false, 
                points, 
                ref tangents, 
                autoCalculate: true
            );

            float3[] flatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(
                SplineType.CatmullRom, 
                isClosed: false, 
                points, 
                tangents
            );

            var ghost = target.MoveToWorld(1f, float3.zero)
                .Along(flatPoints, SplineType.CatmullRom)
                .Play(em);

            Assert.IsTrue(em.HasComponent<TweenControl>(ghost));
            Assert.IsTrue(em.HasComponent<SplineState>(ghost));
        }

        [Test]
        public void VerifyReadmeExamples()
        {
            // Set up context variables
            Entity entity = Entity.Null;
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            var startPos = new float3(0f, 0f, 0f);

            // Snippet 1: Quick Start
            var points = new float3[]
            {
                startPos + new float3(-1f, 0f, 0f),
                startPos,
                startPos + new float3(3f, 2f, 0f),
                startPos + new float3(6f, 0f, 0f),
                startPos + new float3(7f, 0f, 0f)
            };
            float3[] quickStartTangents = null;
            SplineUtility.InitializeOrResizeTangents(SplineType.CatmullRom, false, points, ref quickStartTangents);
            var quickStartFlatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(SplineType.CatmullRom, false, points, quickStartTangents);

            Sequence.Create()
                .Append(entity.MoveToWorld(2.0f, startPos).Along(quickStartFlatPoints, SplineType.CatmullRom))
                .Join(entity.RotateToWorld(2.0f, quaternion.identity).To(quaternion.RotateY(math.PI / 2f)))
                .Append(entity.ScaleTo(0.5f, new float3(1f)).To(new float3(1.5f)).Loop(LoopType.PingPong))
                .Play(ecb);

            // Snippet 2: Implicit (Destination-Only)
            entity
                .ScaleTo(new float3(2f, 2f, 2f), 1.0f)
                .Ease(EaseType.OutBounce)
                .Play(ecb);

            // Snippet 3: Explicit Start
            entity
                .ScaleTo(1.0f, float3.zero)
                .To(new float3(1f, 1f, 1f))
                .Ease(EaseType.OutCubic)
                .Play(ecb);

            // Snippet 4: Loops & Time Types
            entity
                .MoveToWorld(new float3(0f, 10f, 0f), 2.0f)
                .Loop(LoopType.PingPong, count: 4)
                .TimeType(PlaybackTimeType.Unscaled)
                .Play(ecb);

            // Snippet 5: Spline Paths
            var splinePoints = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(2f, 5f, 0f),
                new float3(5f, 5f, 0f),
                new float3(7f, 0f, 0f)
            };
            float3[] splineTangents = null;
            SplineUtility.InitializeOrResizeTangents(SplineType.CatmullRom, false, splinePoints, ref splineTangents);
            var flatSplinePoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(SplineType.CatmullRom, false, splinePoints, splineTangents);

            entity
                .MoveToWorld(3.0f, startPos)
                .Along(flatSplinePoints, SplineType.CatmullRom, isClosed: false)
                .Ease(EaseType.InOutQuad)
                .Visualize()
                .Play(ecb);

            // Snippet 6: Sequences
            Sequence.Create()
                .Append(entity.MoveToWorld(new float3(0f, 5f, 0f), 0.5f).Ease(EaseType.OutQuad))
                .Append(entity.Wait(0.2f))
                .Join(entity.ScaleTo(new float3(1.5f), 0.3f))
                .Append(entity.MoveToWorld(new float3(5f, 5f, 0f), 0.5f))
                .AppendCallback("Done")
                .Play(ecb);

            // Snippet 8: Curve & Spline Utilities - Tangent Generation Example
            float3[] pointsArray = new float3[] 
            { 
                new float3(0f, 0f, 0f), 
                new float3(2f, 5f, 0f), 
                new float3(5f, 0f, 0f) 
            };
            float3[] tangentsArray = null;

            SplineUtility.InitializeOrResizeTangents(
                SplineType.CatmullRom, 
                isClosed: false, 
                pointsArray, 
                ref tangentsArray, 
                autoCalculate: true
            );

            float3[] flatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(
                SplineType.CatmullRom, 
                isClosed: false, 
                pointsArray, 
                tangentsArray
            );
        }
    }

    [BurstCompile]
    public partial struct CallbackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (cb, eventEntity) in SystemAPI.Query<RefRO<SequenceCallbackEvent>>()
                         .WithEntityAccess())
            {
                if (cb.ValueRO.CallbackId == "Done")
                {
                    // Trigger your custom logic here
                }
                ecb.DestroyEntity(eventEntity); // Always clean up the event entity
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    public class MovingPlatformAuthoring : UnityEngine.MonoBehaviour
    {
        public SerializableSpline<float3> splinePath = new SerializableSpline<float3>();
        public float duration = 4.0f;
        public EaseType easeType = EaseType.InOutQuad;
        public LoopType loopType = LoopType.PingPong;
    }

    public class MovingPlatformBaker : Baker<MovingPlatformAuthoring>
    {
        public override void Bake(MovingPlatformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.splinePath == null || authoring.splinePath.points == null || authoring.splinePath.points.Length == 0)
                return;

            authoring.splinePath.ValidatePoints();

            float3[] flatPoints = SplineUtility.GetFlatPointsArray<float3, Float3Math>(
                authoring.splinePath.splineType,
                authoring.splinePath.isClosed,
                authoring.splinePath.points,
                authoring.splinePath.tangents
            );

            entity.MoveToWorld(authoring.duration, authoring.splinePath.points[0])
                .Along(flatPoints, authoring.splinePath.splineType, authoring.splinePath.isClosed)
                .Ease(authoring.easeType)
                .Loop(authoring.loopType)
                .Visualize()
                .Play(this);
        }
    }

    public class SecurityDroneAuthoring : UnityEngine.MonoBehaviour
    {
        public UnityEngine.GameObject targetObject;
        public float chaseSmoothTime = 0.3f;
        public float lookSmoothTime = 0.15f;
    }

    public class SecurityDroneBaker : Baker<SecurityDroneAuthoring>
    {
        public override void Bake(SecurityDroneAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var targetEntity = GetEntity(authoring.targetObject, TransformUsageFlags.Dynamic);

            if (targetEntity == Entity.Null)
                return;

            entity.ChasePosition(targetEntity)
                .SmoothDamp(authoring.chaseSmoothTime)
                .Play(this);

            entity.Look(targetEntity)
                .SmoothDamp(authoring.lookSmoothTime)
                .Play(this);
        }
    }

    public enum ShowcasePreset
    {
        MoveLocal,
        RotateWorld,
        ScalePingPong,
        SplinePath,
        ChaseTarget,
        SequenceShowcase,
        LookAtTarget
    }

    public struct ShowcaseText : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public class EntityweenShowcaseItem : UnityEngine.MonoBehaviour
    {
        public string description;
        public ShowcasePreset preset;
        public float duration = 2.0f;
        public EaseType ease = EaseType.InOutSine;
        public LoopType loop = LoopType.PingPong;
        public float3 moveOffset = new float3(0f, 3f, 0f);
        public SerializableSpline<float3> splinePath = new SerializableSpline<float3>();
        public UnityEngine.GameObject chaseTarget;
        public float chaseSmoothTime = 0.3f;
        public UnityEngine.GameObject lookTarget;
        public float lookSmoothTime = 0.15f;
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
                    entity.MoveToLocal(authoring.duration, localStart)
                        .To(localStart + authoring.moveOffset)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.RotateWorld:
                    entity.RotateToWorld(authoring.duration, quaternion.identity)
                        .To(quaternion.RotateY(math.PI * 2f))
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.ScalePingPong:
                    entity.ScaleTo(authoring.duration, new float3(1f))
                        .To(new float3(1.8f))
                        .Ease(authoring.ease)
                        .Loop(LoopType.PingPong)
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

    public struct BenchmarkSettings : IComponentData
    {
        public Entity Prefab;
    }

    public class EntityweenBenchmark : UnityEngine.MonoBehaviour
    {
        public UnityEngine.GameObject prefab;
    }

    public class EntityweenBenchmarkBaker : Baker<EntityweenBenchmark>
    {
        public override void Bake(EntityweenBenchmark authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BenchmarkSettings
            {
                Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}
