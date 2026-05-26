using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

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

        public class TargetClass
        {
            public float Value { get; set; }
            public float3 VectorValue { get; set; }
            public float FieldValue;
            public float ReadonlyField = 42f;
        }

        [Test]
        public void Bind_PropertyFloat_CreatesManagedHook()
        {
            var target = new TargetClass();
            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, nameof(TargetClass.Value))
                .Play(_em);

            Assert.AreNotEqual(Entity.Null, ghost);
            Assert.IsTrue(_em.HasComponent<TweenMemberHook<float>>(ghost));
            Assert.IsTrue(TweenManagedRegistry.TryGetMember<float>(_world, ghost, out var record));
            Assert.AreEqual(target, record.Target);
            Assert.AreEqual(nameof(TargetClass.Value), record.MemberName);
            Assert.IsNotNull(record.Setter);
        }

        [Test]
        public void Bind_PropertyFloat_UpdatesTarget()
        {
            var target = new TargetClass();
            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, nameof(TargetClass.Value))
                .Play(_em);

            var value = _em.GetComponentData<TweenValue<float>>(ghost);
            value.CurrentValue = 5.5f;
            _em.SetComponentData(ghost, value);

            var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
            syncSystem.Update();

            Assert.AreEqual(5.5f, target.Value);
        }

        [Test]
        public void Bind_PropertyFloat3_UpdatesTarget()
        {
            var target = new TargetClass();
            var ghost = Entity.Null.Float3To(new float3(1f, 2f, 3f), 1f).From(float3.zero)
                .Bind(target, nameof(TargetClass.VectorValue))
                .Play(_em);

            var value = _em.GetComponentData<TweenValue<float3>>(ghost);
            value.CurrentValue = new float3(1f, 1.5f, 2f);
            _em.SetComponentData(ghost, value);

            var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
            syncSystem.Update();

            Assert.AreEqual(new float3(1f, 1.5f, 2f), target.VectorValue);
        }

        [Test]
        public void Bind_FieldFloat_UpdatesTarget()
        {
            var target = new TargetClass();
            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, nameof(TargetClass.FieldValue))
                .Play(_em);

            Assert.IsTrue(_em.HasComponent<TweenMemberHook<float>>(ghost));

            var value = _em.GetComponentData<TweenValue<float>>(ghost);
            value.CurrentValue = 7.2f;
            _em.SetComponentData(ghost, value);

            var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
            syncSystem.Update();

            Assert.AreEqual(7.2f, target.FieldValue);
        }

        [Test]
        public void Bind_InvalidMember_LogsErrorAndReturnsNull()
        {
            var target = new TargetClass();
            
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Failed to bind member"));

            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, "NonExistingMember")
                .Play(_em);

            Assert.AreEqual(Entity.Null, ghost);
        }

        [Test]
        public void Bind_TypeMismatch_LogsErrorAndReturnsNull()
        {
            var target = new TargetClass();

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Failed to bind member"));

            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, nameof(TargetClass.VectorValue))
                .Play(_em);

            Assert.AreEqual(Entity.Null, ghost);
        }

        [Test]
        public void OnUpdate_InvokesCallback()
        {
            float callbackValue = 0f;
            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .OnUpdate(val => callbackValue = val)
                .Play(_em);

            Assert.IsTrue(_em.HasComponent<TweenCallbackHook<float>>(ghost));

            var value = _em.GetComponentData<TweenValue<float>>(ghost);
            value.CurrentValue = 4.2f;
            _em.SetComponentData(ghost, value);

            var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
            syncSystem.Update();

            Assert.AreEqual(4.2f, callbackValue);
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

                Assert.IsTrue(_em.HasComponent<TweenGameObjectTarget>(ghost));

                var value = _em.GetComponentData<TweenValue<float3>>(ghost);
                value.CurrentValue = new float3(1f, 2f, 3f);
                _em.SetComponentData(ghost, value);

                var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
                syncSystem.Update();

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

                var value = _em.GetComponentData<TweenValue<quaternion>>(ghost);
                value.CurrentValue = targetRot;
                _em.SetComponentData(ghost, value);

                var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
                syncSystem.Update();

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

                var value = _em.GetComponentData<TweenValue<float3>>(ghost);
                value.CurrentValue = new float3(2f, 3f, 4f);
                _em.SetComponentData(ghost, value);

                var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();
                syncSystem.Update();

                Assert.AreEqual(new Vector3(2f, 3f, 4f), go.transform.localScale);
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void FromCurrent_Property_ReadsInitialValue()
        {
            var target = new TargetClass { Value = 75f };
            var ghost = Entity.Null.FloatTo(100f, 1f)
                .Bind(target, nameof(TargetClass.Value))
                .FromCurrent()
                .Play(_em);

            var startSystem = _world.GetOrCreateSystemManaged<TweenManagedStartFromCurrentSystem>();
            startSystem.Update();

            var value = _em.GetComponentData<TweenValue<float>>(ghost);
            Assert.AreEqual(75f, value.StartPoint);
            Assert.AreEqual(75f, value.CurrentValue);
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

                var startSystem = _world.GetOrCreateSystemManaged<TweenManagedStartFromCurrentSystem>();
                startSystem.Update();

                var value = _em.GetComponentData<TweenValue<float3>>(ghost);
                Assert.AreEqual(new float3(10f, 20f, 30f), value.StartPoint);
                Assert.AreEqual(new float3(10f, 20f, 30f), value.CurrentValue);
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void ParallelWriter_WithManagedHook_IsRejected()
        {
            var target = new TargetClass();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("does not support managed component bindings"));

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var writer = ecb.AsParallelWriter();

            var bp = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, nameof(TargetClass.Value));

            var ghost = bp.Play(0, ref writer);

            Assert.AreEqual(Entity.Null, ghost);
        }

        [Test]
        public void GC_PropertySetter_DoesNotAllocate()
        {
            var target = new TargetClass();
            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .Bind(target, nameof(TargetClass.Value))
                .Play(_em);

            var value = _em.GetComponentData<TweenValue<float>>(ghost);
            value.CurrentValue = 1.0f;
            _em.SetComponentData(ghost, value);

            var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();

            syncSystem.Update();

            long before = GC.GetTotalMemory(true);
            for (int i = 0; i < 1000; i++)
            {
                value.CurrentValue = i;
                _em.SetComponentData(ghost, value);
                syncSystem.Update();
            }
            long after = GC.GetTotalMemory(false);

            // Using 8KB tolerance to prevent false positives from runners
            long diff = Math.Abs(after - before);
            Assert.LessOrEqual(diff, 8192, $"GC allocations detected: {diff} bytes");
        }

        private class TestCallbackTarget
        {
            public float Value;
            public void OnUpdate(float val) => Value = val;
        }

        [Test]
        public void GC_Callback_DoesNotAllocate()
        {
            var target = new TestCallbackTarget();
            var ghost = Entity.Null.FloatTo(10f, 1f).From(0f)
                .OnUpdate(target.OnUpdate)
                .Play(_em);

            var value = _em.GetComponentData<TweenValue<float>>(ghost);
            value.CurrentValue = 1.0f;
            _em.SetComponentData(ghost, value);

            var syncSystem = _world.GetOrCreateSystemManaged<TweenSyncSystem>();

            syncSystem.Update();

            long before = GC.GetTotalMemory(true);
            for (int i = 0; i < 1000; i++)
            {
                value.CurrentValue = i;
                _em.SetComponentData(ghost, value);
                syncSystem.Update();
            }
            long after = GC.GetTotalMemory(false);

            long diff = Math.Abs(after - before);
            Assert.LessOrEqual(diff, 8192, $"GC allocations detected: {diff} bytes");
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
            
            // Frame 1
            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(0, sysRef.RunCount);
            }

            // Frame 2
            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(0, sysRef.RunCount);
            }

            // Frame 3
            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(1, sysRef.RunCount);
            }

            // Frame 4
            {
                ref var stateRef = ref _world.Unmanaged.ResolveSystemStateRef(systemHandle);
                ref var sysRef = ref _world.Unmanaged.GetUnsafeSystemRef<TestThrottledSystem>(systemHandle);
                sysRef.OnUpdate(ref stateRef);
                Assert.AreEqual(1, sysRef.RunCount);
            }
        }
    }
}
