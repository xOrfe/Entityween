using NUnit.Framework;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween.Tests
{
    [TestFixture]
    public class TweenHookTests
    {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TweenHookTestsWorld");
            _em = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void BindTransform_MoveTo_WritesPosition()
        {
            var go = new GameObject("TestGO");
            try
            {
                var ghost = Entity.Null.MoveToWorld(new float3(1f, 2f, 3f), 1f).From(float3.zero)
                    .BindTransform(go.transform)
                    .Play(_em);

                Assert.IsTrue(_em.HasComponent<TweenTransformTarget>(ghost));

                var value = _em.GetComponentData<TweenRuntime<float3>>(ghost);
                value.CurrentValue = new float3(1f, 2f, 3f);
                _em.SetComponentData(ghost, value);

                var syncSystem = _world.GetOrCreateSystemManaged<TweenTransformAccessSystem>();
                syncSystem.Update();
                _em.CompleteAllTrackedJobs();

                Assert.AreEqual(new Vector3(1f, 2f, 3f), go.transform.position);
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void BindTransform_RotateTo_WritesRotation()
        {
            var go = new GameObject("TestGO");
            try
            {
                var targetRot = quaternion.RotateY(math.PI / 2f);
                var ghost = Entity.Null.RotateToWorld(targetRot, 1f).From(quaternion.identity)
                    .BindTransform(go.transform)
                    .Play(_em);

                var value = _em.GetComponentData<TweenRuntime<quaternion>>(ghost);
                value.CurrentValue = targetRot;
                _em.SetComponentData(ghost, value);

                var syncSystem = _world.GetOrCreateSystemManaged<TweenTransformAccessSystem>();
                syncSystem.Update();
                _em.CompleteAllTrackedJobs();

                var expected = new Quaternion(targetRot.value.x, targetRot.value.y, targetRot.value.z, targetRot.value.w);
                Assert.IsTrue(Quaternion.Angle(expected, go.transform.rotation) < 0.01f);
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void BindTransform_ScaleTo_WritesLocalScale()
        {
            var go = new GameObject("TestGO");
            try
            {
                var ghost = Entity.Null.ScaleTo(new float3(2f), 1f)
                    .BindTransform(go.transform)
                    .Play(_em);

                var value = _em.GetComponentData<TweenRuntime<float3>>(ghost);
                value.CurrentValue = new float3(2f, 3f, 4f);
                _em.SetComponentData(ghost, value);

                var syncSystem = _world.GetOrCreateSystemManaged<TweenTransformAccessSystem>();
                syncSystem.Update();
                _em.CompleteAllTrackedJobs();

                Assert.AreEqual(new Vector3(2f, 3f, 4f), go.transform.localScale);
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void FromCurrent_Transform_ReadsInitialValue()
        {
            var go = new GameObject("TestGO");
            go.transform.position = new Vector3(10f, 20f, 30f);
            try
            {
                var ghost = Entity.Null.MoveToWorld(new float3(100f), 1f)
                    .BindTransform(go.transform)
                    .FromCurrent()
                    .Play(_em);

                var startSystem = _world.GetOrCreateSystemManaged<TweenTransformStartFromCurrentSystem>();
                startSystem.Update();

                var range = _em.GetComponentData<TweenRange<float3>>(ghost);
                var value = _em.GetComponentData<TweenRuntime<float3>>(ghost);
                Assert.AreEqual(new float3(10f, 20f, 30f), range.StartPoint);
                Assert.AreEqual(new float3(10f, 20f, 30f), value.CurrentValue);
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [BurstCompile]
        private partial struct TestThrottledSystem : ISystem
        {
            public SystemThrottler Throttler;
            public int RunCount;

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                if (Throttler.ShouldUpdateFrame(3))
                {
                    RunCount++;
                }
            }
        }

        [Test]
        public void Throttler_WorksInISystem_BurstCompiled()
        {
            var systemHandle = _world.AddSystem<TestThrottledSystem>();
            
            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(0, sysRef.RunCount);
            }

            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(0, sysRef.RunCount);
            }

            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(1, sysRef.RunCount);
            }

            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(1, sysRef.RunCount);
            }
        }
    }
}
