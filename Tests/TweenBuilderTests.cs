using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using XO.Curve;

namespace XO.Entityween.Tests
{
    [TestFixture]
    public class TweenBuilderTests
    {
        [Test]
        public void MoveTo_RemainsLocalSpaceAlias()
        {
            var tween = Entity.Null.MoveTo(new float3(1f, 2f, 3f), 1f);

            Assert.AreEqual(TweenType.MoveTo, tween.TweenType);
            Assert.AreEqual(TweenSpace.Local, tween.Space);
            Assert.AreEqual(new float3(1f, 2f, 3f), tween.EndPoint);
        }

        [Test]
        public void MoveToWorld_SetsWorldSpace()
        {
            var tween = Entity.Null.MoveToWorld(new float3(3f, 2f, 1f), 1f);

            Assert.AreEqual(TweenType.MoveTo, tween.TweenType);
            Assert.AreEqual(TweenSpace.World, tween.Space);
            Assert.AreEqual(new float3(3f, 2f, 1f), tween.EndPoint);
        }

        [Test]
        public void FloatValueTweens_Overloads()
        {
            var fTween = Entity.Null.FloatTo(5f, 2f);
            Assert.AreEqual(TweenType.FloatTo, fTween.TweenType);
            Assert.IsTrue(fTween.StartFromCurrent);
            Assert.AreEqual(2f, fTween.SecondsToPlay);

            var f3Tween = Entity.Null.Float3To(new float3(1f, 2f, 3f), 4f);
            Assert.AreEqual(TweenType.Float3To, f3Tween.TweenType);
            Assert.IsTrue(f3Tween.StartFromCurrent);
            Assert.AreEqual(4f, f3Tween.SecondsToPlay);
        }

        [Test]
        public void Chase_DefaultsKillOnChaseToFalse()
        {
            var tween = Entity.Null.MoveToWorld(new float3(1f, 0f, 0f), 1f)
                .Chase(0.25f);

            Assert.IsTrue(tween.UseChase);
            Assert.IsFalse(tween.Chase.KillOnChase);
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

            var tween = target.MoveToWorld(new float3(1f, 0f, 0f), 1f)
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
                .Append(target.MoveToWorld(new float3(1f, 0f, 0f), 1f))
                .AppendWait(0.5f)
                .AppendCallback("Done")
                .Loop(LoopType.Repeat, 1)
                .Play(em);

            Assert.IsTrue(em.HasComponent<Sequence>(sequence));
            Assert.AreEqual(PlaybackState.Playing, em.GetComponentData<Sequence>(sequence).State);
            Assert.IsTrue(em.HasComponent<PlaybackProgress>(sequence));
            Assert.AreEqual(LoopType.Repeat, em.GetComponentData<PlaybackProgress>(sequence).LoopType);

            var elements = em.GetBuffer<SequenceElement>(sequence);
            Assert.AreEqual(3, elements.Length);
            Assert.AreEqual(TimelineActionKind.Tween, elements[0].Kind);
            Assert.AreEqual(TimelineActionKind.Wait, elements[1].Kind);
            Assert.AreEqual(TimelineActionKind.Callback, elements[2].Kind);

            Assert.IsTrue(em.HasComponent<TweenSequenceDriven>(elements[0].ActionEntity));
            Assert.IsTrue(em.HasComponent<TimelineDriven>(elements[0].ActionEntity));
            Assert.AreEqual(TweenSpace.World, em.GetComponentData<SequenceTweenBinding>(elements[0].ActionEntity).Space);
            Assert.IsFalse(em.IsComponentEnabled<TweenControl>(elements[0].ActionEntity));
        }

        [Test]
        public void SequenceCreate_WithEntityManagerPreparesActionEntityDuringAppend()
        {
            using var world = new World("Entityween Eager Sequence Builder Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var builder = Sequence.Create(em)
                .Append(target.MoveToWorld(new float3(1f, 0f, 0f), 1f));

            var sequenceQuery = em.CreateEntityQuery(typeof(Sequence), typeof(SequenceElement));
            Assert.AreEqual(1, sequenceQuery.CalculateEntityCount());
            var sequence = sequenceQuery.GetSingletonEntity();
            Assert.AreEqual(PlaybackState.Paused, em.GetComponentData<Sequence>(sequence).State);

            var elements = em.GetBuffer<SequenceElement>(sequence);
            Assert.AreEqual(1, elements.Length);
            Assert.AreNotEqual(Entity.Null, elements[0].ActionEntity);
            Assert.IsTrue(em.HasComponent<TweenControl>(elements[0].ActionEntity));
            Assert.IsFalse(em.IsComponentEnabled<TweenControl>(elements[0].ActionEntity));

            var played = builder.Play(em);
            Assert.AreEqual(sequence, played);
            Assert.AreEqual(PlaybackState.Playing, em.GetComponentData<Sequence>(played).State);
        }

        [Test]
        public void TimelineDrivenCleanup_DestroysOwnedTweenGhostWhenSequenceIsDestroyed()
        {
            using var world = new World("Entityween Timeline Cleanup Tween Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var sequence = Sequence.Create()
                .Append(target.MoveToWorld(new float3(1f, 0f, 0f), 1f))
                .Play(em);

            var action = em.GetBuffer<SequenceElement>(sequence)[0].ActionEntity;
            em.DestroyEntity(sequence);

            var ecbSystem = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var cleanupSystem = world.CreateSystem<TimelineDrivenCleanupSystem>();
            var group = world.GetOrCreateSystemManaged<EntityweenSequenceGroup>();
            group.AddSystemToUpdateList(cleanupSystem);

            group.Update();
            ecbSystem.Update();

            Assert.IsFalse(em.Exists(action));
        }

        [Test]
        public void TimelineDrivenCleanup_ReleasesChaseEntityWhenSequenceIsDestroyed()
        {
            using var world = new World("Entityween Timeline Cleanup Chase Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            var chaser = em.CreateEntity();

            var sequence = Sequence.Create()
                .Append(chaser.ChasePosition(target).For(1f))
                .Play(em);

            em.DestroyEntity(sequence);

            var ecbSystem = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var cleanupSystem = world.CreateSystem<TimelineDrivenCleanupSystem>();
            var group = world.GetOrCreateSystemManaged<EntityweenSequenceGroup>();
            group.AddSystemToUpdateList(cleanupSystem);

            group.Update();
            ecbSystem.Update();

            Assert.IsTrue(em.Exists(chaser));
            Assert.IsTrue(em.HasComponent<ChasePosition>(chaser));
            Assert.IsFalse(em.IsComponentEnabled<ChasePosition>(chaser));
            Assert.IsFalse(em.HasComponent<TimelineDriven>(chaser));
        }

        [Test]
        public void SequencePlay_PreservesChaseMetadata()
        {
            using var world = new World("Entityween Sequence Chase Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            var sequence = Sequence.Create()
                .Append(target.MoveToWorld(new float3(1f, 0f, 0f), 1f).Chase(0.2f, ChaseMode.SmoothDamp, 10f, true))
                .Play(em);

            var element = em.GetBuffer<SequenceElement>(sequence)[0];
            var binding = em.GetComponentData<SequenceTweenBinding>(element.ActionEntity);
            Assert.IsTrue(binding.UseChase);
            Assert.AreEqual(ChaseMode.SmoothDamp, binding.ChaseMode);
            Assert.AreEqual(0.2f, binding.ChaseSmoothTime);
            Assert.AreEqual(10f, binding.ChaseMaxSpeed);
            Assert.IsTrue(binding.KillOnChase);
        }

        [Test]
        public void SequencePlay_CanScheduleChaseAsTimelineAction()
        {
            using var world = new World("Entityween Timeline Chase Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            var chaser = em.CreateEntity();

            var sequence = Sequence.Create()
                .Append(chaser.ChasePosition(target).SmoothDamp(0.3f).For(1f))
                .Play(em);

            var element = em.GetBuffer<SequenceElement>(sequence)[0];

            Assert.AreEqual(TimelineActionKind.Chase, element.Kind);
            Assert.AreEqual(chaser, element.ActionEntity);
            Assert.IsTrue(em.HasComponent<TimelineDriven>(chaser));
            Assert.IsTrue(em.HasComponent<ChaseTargetEntity>(chaser));
            Assert.IsTrue(em.HasComponent<ChasePosition>(chaser));
            Assert.IsFalse(em.IsComponentEnabled<ChasePosition>(chaser));

            var chase = em.GetComponentData<ChasePosition>(chaser);
            Assert.AreEqual(ChaseMode.SmoothDamp, chase.Mode);
            Assert.AreEqual(0.3f, chase.SmoothTime);
        }

        [Test]
        public void SequenceDynamicTime_RemovedChaseAdvancesToNextAction()
        {
            using var world = new World("Entityween Dynamic Timeline Chase Test");
            var em = world.EntityManager;
            var chaser = em.CreateEntity();

            var sequence = Sequence.Create()
                .DynamicTime()
                .Append(chaser.ChasePosition(float3.zero).Override().For(1f))
                .AppendCallback("Removed")
                .Play(em);

            em.RemoveComponent<ChasePosition>(chaser);

            var sequenceData = em.GetComponentData<Sequence>(sequence);
            sequenceData.Time = 0.01f;
            em.SetComponentData(sequence, sequenceData);

            var ecbSystem = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var sequenceSystem = world.CreateSystem<SequencePlaybackSystem>();
            var group = world.GetOrCreateSystemManaged<EntityweenSequenceGroup>();
            group.AddSystemToUpdateList(sequenceSystem);

            group.Update();
            ecbSystem.Update();

            var query = em.CreateEntityQuery(typeof(SequenceCallbackEvent));
            Assert.AreEqual(1, query.CalculateEntityCount());
            using var events = query.ToComponentDataArray<SequenceCallbackEvent>(Allocator.Temp);
            Assert.AreEqual(new FixedString64Bytes("Removed"), events[0].CallbackId);
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
            var loop = new PlaybackProgress { LoopType = LoopType.Repeat, LoopCount = 0, LoopIndex = 0 };

            PlaybackUtilities.CalculateProgress(ref elapsed, 1f, ref loop, out var normalizedTime, out var finished);

            Assert.IsFalse(finished);
            Assert.AreEqual(2, loop.LoopIndex);
            Assert.AreEqual(0.25f, elapsed, 0.0001f);
            Assert.AreEqual(0.25f, normalizedTime, 0.0001f);
        }

        [Test]
        public void CalculateProgress_FinitePingPongEndsOnExpectedSide()
        {
            var elapsed = 2.1f;
            var loop = new PlaybackProgress { LoopType = LoopType.PingPong, LoopCount = 1, LoopIndex = 0 };

            PlaybackUtilities.CalculateProgress(ref elapsed, 1f, ref loop, out var normalizedTime, out var finished);

            Assert.IsTrue(finished);
            Assert.AreEqual(1, loop.LoopIndex);
            Assert.AreEqual(0f, normalizedTime, 0.0001f);
        }

        [Test]
        public void TestAlongNativeArray()
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
            using var nativeFlatPoints = new NativeArray<float3>(flatPoints, Allocator.Temp);

            var ghost = target.MoveToWorld(new float3(5f, 0f, 0f), 1f)
                .Along(nativeFlatPoints, SplineType.CatmullRom)
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
            using var nativeQuickStartFlatPoints = new NativeArray<float3>(quickStartFlatPoints, Allocator.Temp);

            Sequence.Create()
                .Append(entity.MoveToWorld(startPos, 2.0f).Along(nativeQuickStartFlatPoints, SplineType.CatmullRom))
                .Join(entity.RotateToWorld(quaternion.identity, 2.0f).To(quaternion.RotateY(math.PI / 2f)))
                .Append(entity.ScaleTo(new float3(1.5f), 0.5f).From(new float3(1f)).Loop(LoopType.PingPong))
                .Play(ecb);

            // Snippet 2: Implicit (Destination-Only)
            entity
                .ScaleTo(new float3(2f, 2f, 2f), 1.0f)
                .Ease(EaseType.OutBounce)
                .Play(ecb);

            // Snippet 3: Explicit Start
            entity
                .ScaleTo(new float3(1f, 1f, 1f), 1.0f)
                .From(float3.zero)
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
            using var nativeFlatSplinePoints = new NativeArray<float3>(flatSplinePoints, Allocator.Temp);

            entity
                .MoveToWorld(startPos, 3.0f)
                .Along(nativeFlatSplinePoints, SplineType.CatmullRom, isClosed: false)
                .Ease(EaseType.InOutQuad)
                .Visualize()
                .Play(ecb);

            // Snippet 6: Sequences
            Sequence.Create()
                .Append(entity.MoveToWorld(new float3(0f, 5f, 0f), 0.5f).Ease(EaseType.OutQuad))
                .AppendWait(0.2f)
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

        [Test]
        public void LoopEaseMode_MirrorMode_EasesReversedProgress()
        {
            using var world = new World("MirrorEaseTest");
            var em = world.EntityManager;

            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Ease(EaseType.InQuad)
                .Loop(LoopType.PingPong, 0, LoopEaseMode.Mirror)
                .Play(em);

            var ecbSystem = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var system = world.CreateSystem<TweenCalculationSystem>();
            var group = world.GetOrCreateSystemManaged<EntityweenTweenGroup>();
            group.AddSystemToUpdateList(system);

            var control = em.GetComponentData<TweenControl>(ghost);
            control.ElapsedTime = 1.5f;
            em.SetComponentData(ghost, control);

            ecbSystem.Update();
            group.Update();

            var value = em.GetComponentData<TweenValue<float>>(ghost);
            Assert.AreEqual(2.5f, value.CurrentValue, 0.0001f);
        }

        [Test]
        public void LoopEaseMode_RepeatMode_EasesForwardProgressAndReversesEased()
        {
            using var world = new World("RepeatEaseTest");
            var em = world.EntityManager;

            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Ease(EaseType.InQuad)
                .Loop(LoopType.PingPong, 0, LoopEaseMode.Repeat)
                .Play(em);

            var ecbSystem = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var system = world.CreateSystem<TweenCalculationSystem>();
            var group = world.GetOrCreateSystemManaged<EntityweenTweenGroup>();
            group.AddSystemToUpdateList(system);

            var control = em.GetComponentData<TweenControl>(ghost);
            control.ElapsedTime = 1.5f;
            em.SetComponentData(ghost, control);

            ecbSystem.Update();
            group.Update();

            var value = em.GetComponentData<TweenValue<float>>(ghost);
            Assert.AreEqual(7.5f, value.CurrentValue, 0.0001f);
        }

        [Test]
        public void TestAlongBlobAssetReference()
        {
            using var world = new World("Along Blob Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            float3[] points = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(2f, 5f, 0f),
                new float3(5f, 0f, 0f)
            };

            using var nativePts = new NativeArray<float3>(points, Allocator.Temp);
            var blob = Spline.CreateSplineBlob<float3, Float3Math>(SplineType.Linear, false, nativePts);

            var ghost = target.MoveToWorld(new float3(5f, 0f, 0f), 1f)
                .Along(blob)
                .Play(em);

            Assert.IsTrue(em.HasComponent<SplineBlobRef<float3>>(ghost));
            Assert.AreEqual(blob, em.GetComponentData<SplineBlobRef<float3>>(ghost).Blob);

            var ecbSystem = world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var system = world.CreateSystem<TweenCalculationSystem>();
            var group = world.GetOrCreateSystemManaged<EntityweenTweenGroup>();
            group.AddSystemToUpdateList(system);

            var control = em.GetComponentData<TweenControl>(ghost);
            control.ElapsedTime = 0.5f;
            em.SetComponentData(ghost, control);

            ecbSystem.Update();
            group.Update();

            var value = em.GetComponentData<TweenValue<float3>>(ghost);
            Assert.AreNotEqual(float3.zero, value.CurrentValue);

            Assert.IsTrue(blob.IsCreated);
            blob.Dispose();
        }

        [Test]
        public void AlongNativeArray_UsesNativeSourceWithoutManagedArrayCopy()
        {
            using var world = new World("Along Native Array Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            using var points = new NativeArray<float3>(new[]
            {
                new float3(0f, 0f, 0f),
                new float3(1f, 0f, 0f),
                new float3(2f, 0f, 0f),
                new float3(3f, 0f, 0f)
            }, Allocator.Temp);

            var tween = target.MoveToWorld(new float3(3f, 0f, 0f), 1f)
                .Along(points, SplineType.CatmullRom);

            Assert.AreEqual(TweenSplineSourceKind.NativeArray, tween.Spline.SourceKind);
            Assert.IsTrue(tween.SplineNativePoints.IsCreated);

            var ghost = tween.Play(em);

            Assert.IsTrue(em.HasComponent<SplineState>(ghost));
            Assert.AreEqual(points.Length, em.GetBuffer<SplineElement<float3>>(ghost).Length);
        }

        [Test]
        public void SequencePlay_AlongNativeArrayCreatesSplineBuffer()
        {
            using var world = new World("Sequence Along Native Array Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();

            using var points = new NativeArray<float3>(new[]
            {
                new float3(0f, 0f, 0f),
                new float3(1f, 0f, 0f),
                new float3(2f, 0f, 0f),
                new float3(3f, 0f, 0f)
            }, Allocator.Temp);

            var sequence = Sequence.Create()
                .Append(target.MoveToWorld(new float3(3f, 0f, 0f), 1f)
                    .Along(points, SplineType.CatmullRom))
                .Play(em);

            var ghost = em.GetBuffer<SequenceElement>(sequence)[0].ActionEntity;

            Assert.IsTrue(em.HasComponent<SplineState>(ghost));
            Assert.AreEqual(points.Length, em.GetBuffer<SplineElement<float3>>(ghost).Length);
        }

        [Test]
        public void TestSplineAlongWorld_SetsWorldSpace()
        {
            using var world = new World("Spline World Space Test");
            var em = world.EntityManager;

            float3[] points = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(0f, 10f, 0f)
            };
            using var nativePoints = new NativeArray<float3>(points, Allocator.Temp);

            var targetEntity = em.CreateEntity();
            var ghost = targetEntity.MoveToWorld(new float3(0f, 10f, 0f), 1f)
                .Along(nativePoints, SplineType.Linear)
                .Play(em);

            Assert.AreNotEqual(Entity.Null, ghost);
            // ChasePosition is added to the target entity and is public — verify Space is World
            var chasePos = em.GetComponentData<ChasePosition>(targetEntity);
            Assert.AreEqual(TweenSpace.World, chasePos.Space,
                "Along() with MoveToWorld should store TweenSpace.World on the ChasePosition component.");

            em.DestroyEntity(ghost);
        }

        [Test]
        public void TestSplineAlongLocal_SetsLocalSpace()
        {
            using var world = new World("Spline Local Space Test");
            var em = world.EntityManager;

            float3[] points = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(0f, 10f, 0f)
            };
            using var nativePoints = new NativeArray<float3>(points, Allocator.Temp);

            var targetEntity = em.CreateEntity();
            var ghost = targetEntity.MoveTo(new float3(0f, 10f, 0f), 1f)
                .Along(nativePoints, SplineType.Linear)
                .Play(em);

            Assert.AreNotEqual(Entity.Null, ghost);
            // ChasePosition is added to the target entity and is public — verify Space is Local
            var chasePos = em.GetComponentData<ChasePosition>(targetEntity);
            Assert.AreEqual(TweenSpace.Local, chasePos.Space,
                "Along() with MoveTo (local) should store TweenSpace.Local on the ChasePosition component.");

            em.DestroyEntity(ghost);
        }

        [Test]
        public void TestPlaybackControl_PauseAndResume()
        {
            using var world = new World("PauseResume Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            em.AddComponentData(target, new LocalTransform { Position = float3.zero, Scale = 1f, Rotation = quaternion.identity });

            // Create a tween
            var ghost = target.MoveTo(new float3(10f, 0f, 0f), 1f).Play(em);

            // Pause it
            Entityween.Pause(ghost, em);
            Assert.IsFalse(em.IsComponentEnabled<TweenControl>(ghost));

            // Resume it
            Entityween.Resume(ghost, em);
            Assert.IsTrue(em.IsComponentEnabled<TweenControl>(ghost));

            // Complete it
            Entityween.Complete(ghost, em);
            Assert.IsFalse(em.Exists(ghost));
            Assert.AreEqual(new float3(10f, 0f, 0f), em.GetComponentData<LocalTransform>(target).Position);
        }

        [Test]
        public void TestPlaybackControl_Kill()
        {
            using var world = new World("Kill Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            em.AddComponentData(target, new LocalTransform { Position = float3.zero, Scale = 1f, Rotation = quaternion.identity });

            var ghost = target.MoveTo(new float3(10f, 0f, 0f), 1f).Play(em);

            // Kill it
            Entityween.Kill(ghost, em);
            Assert.IsFalse(em.Exists(ghost));
            // ChasePosition and ChasePositionTweenSource should be removed
            Assert.IsFalse(em.HasComponent<ChasePosition>(target));
            Assert.IsFalse(em.HasComponent<ChasePositionTweenSource>(target));
        }

        [Test]
        public void TestPlaybackControl_RewindTween()
        {
            using var world = new World("Rewind Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            em.AddComponentData(target, new LocalTransform { Position = float3.zero, Scale = 1f, Rotation = quaternion.identity });

            var ghost = target.MoveTo(new float3(10f, 0f, 0f), 1f).Play(em);

            // Let it play forward a bit by setting ElapsedTime manually
            var control = em.GetComponentData<TweenControl>(ghost);
            control.ElapsedTime = 0.5f;
            em.SetComponentData(ghost, control);

            // Rewind it
            Entityween.Rewind(ghost, em);
            var progress = em.GetComponentData<PlaybackProgress>(ghost);
            Assert.AreEqual(-1, progress.Direction);
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
            using var nativeFlatPoints = new NativeArray<float3>(flatPoints, Allocator.Temp);

            entity.MoveToWorld(authoring.splinePath.points[0], authoring.duration)
                .Along(nativeFlatPoints, authoring.splinePath.splineType, authoring.splinePath.isClosed)
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
                    entity.MoveToLocal(localStart + authoring.moveOffset, authoring.duration)
                        .From(localStart)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.RotateWorld:
                    entity.RotateToWorld(quaternion.RotateY(math.PI * 2f), authoring.duration)
                        .From(quaternion.identity)
                        .Ease(authoring.ease)
                        .Loop(authoring.loop)
                        .Play(this);
                    break;

                case ShowcasePreset.ScalePingPong:
                    entity.ScaleTo(new float3(1.8f), authoring.duration)
                        .From(new float3(1f))
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
                        using var nativeFlatPoints = new NativeArray<float3>(flatPoints, Allocator.Temp);

                        entity.MoveToWorld(authoring.splinePath.points[0], authoring.duration)
                            .Along(nativeFlatPoints, authoring.splinePath.splineType, authoring.splinePath.isClosed)
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
                        .Append(entity.MoveToLocal(sequenceStart + authoring.moveOffset, authoring.duration * 0.5f).From(sequenceStart).Ease(EaseType.OutQuad))
                        .Append(entity.RotateToLocal(quaternion.RotateY(math.PI), authoring.duration * 0.5f).From(quaternion.identity))
                        .Append(entity.MoveToLocal(sequenceStart, authoring.duration * 0.5f).From(sequenceStart + authoring.moveOffset).Ease(EaseType.InQuad))
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
