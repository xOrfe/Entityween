using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween.Tests
{
    [TestFixture]
    public class SequenceBuilderTests
    {
        public struct CustomAction : ISequenceActionBlueprint
        {
            public float CustomValue;
            public TimelineActionKind Kind => TimelineActionKind.Tween;
            public float Duration => 1.5f;
            public FixedString64Bytes CallbackId => default;

            public Entity CreateEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
                where TAdapter : IEntityCommandAdapter
            {
                var e = adapter.CreateEntity();
                adapter.AddComponent(e, new CustomActionComponent { Value = CustomValue });
                return e;
            }
        }

        public struct CustomActionComponent : IComponentData
        {
            public float Value;
        }

        [Test]
        public void SequencePlay_AppendsGenericActionsAndCustomBlueprint()
        {
            using var world = new World("Custom Sequence Test");
            var em = world.EntityManager;
            var target = em.CreateEntity();
            var chaser = em.CreateEntity();

            var customAction = new CustomAction { CustomValue = 42f };

            var sequence = Sequence.Create()
                .Append(target.MoveToWorld(new float3(1f, 0f, 0f), 1f))
                .AppendWait(1.0f) // Convenience helper
                .Append(chaser.ChasePosition(target).For(2.0f)) // TimelineChase<float3>
                .Append(customAction) // Custom blueprint
                .AppendCallback("Completed") // Callback helper
                .Play(em);

            Assert.IsTrue(em.HasComponent<Sequence>(sequence));
            var elements = em.GetBuffer<SequenceElement>(sequence);
            Assert.AreEqual(5, elements.Length);

            Assert.AreEqual(TimelineActionKind.Tween, elements[0].Kind);
            Assert.AreEqual(1f, elements[0].Duration);

            Assert.AreEqual(TimelineActionKind.Wait, elements[1].Kind);
            Assert.AreEqual(1f, elements[1].Duration);

            Assert.AreEqual(TimelineActionKind.Chase, elements[2].Kind);
            Assert.AreEqual(2f, elements[2].Duration);

            Assert.AreEqual(TimelineActionKind.Tween, elements[3].Kind);
            Assert.AreEqual(1.5f, elements[3].Duration);
            var customEntity = elements[3].ActionEntity;
            Assert.IsTrue(em.HasComponent<CustomActionComponent>(customEntity));
            Assert.AreEqual(42f, em.GetComponentData<CustomActionComponent>(customEntity).Value);

            Assert.AreEqual(TimelineActionKind.Callback, elements[4].Kind);
            Assert.AreEqual(new FixedString64Bytes("Completed"), elements[4].CallbackId);
        }
    }
}
