using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenChaseGroup))]
    internal partial class TweenTransformAccessSystem : SystemBase
    {
        private EntityQuery _createdQuery;
        private EntityQuery _destroyedQuery;

        private TransformAccessArray _transforms;
        private NativeList<Entity> _entities;
        private NativeHashMap<Entity, int> _entityToIndex;

        private ComponentLookup<TweenTransformTarget> _targetLookup;
        private ComponentLookup<TweenRuntime<float3>> _runtimeFloat3Lookup;
        private ComponentLookup<TweenRuntime<quaternion>> _runtimeQuaternionLookup;
        private ComponentLookup<TweenRuntime<float>> _runtimeFloatLookup;

        protected override void OnCreate()
        {
            _createdQuery = SystemAPI.QueryBuilder()
                .WithAll<TweenTransformTarget, TweenTransformReference>()
                .WithNone<TweenTransformCleanup>()
                .Build();

            _destroyedQuery = SystemAPI.QueryBuilder()
                .WithAll<TweenTransformCleanup>()
                .WithNone<TweenTransformTarget>()
                .Build();

            _transforms = new TransformAccessArray(128);
            _entities = new NativeList<Entity>(128, Allocator.Persistent);
            _entityToIndex = new NativeHashMap<Entity, int>(128, Allocator.Persistent);

            _targetLookup = GetComponentLookup<TweenTransformTarget>(true);
            _runtimeFloat3Lookup = GetComponentLookup<TweenRuntime<float3>>(true);
            _runtimeQuaternionLookup = GetComponentLookup<TweenRuntime<quaternion>>(true);
            _runtimeFloatLookup = GetComponentLookup<TweenRuntime<float>>(true);
        }

        protected override void OnDestroy()
        {
            Dependency.Complete();
            if (_transforms.isCreated) _transforms.Dispose();
            if (_entities.IsCreated) _entities.Dispose();
            if (_entityToIndex.IsCreated) _entityToIndex.Dispose();
        }

        protected override void OnUpdate()
        {
            bool hasDestroyed = !_destroyedQuery.IsEmptyIgnoreFilter;
            bool hasCreated = !_createdQuery.IsEmptyIgnoreFilter;

            if (hasDestroyed || hasCreated)
            {
                Dependency.Complete();

                if (hasDestroyed)
                {
                    using var destroyedEntities = _destroyedQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < destroyedEntities.Length; i++)
                    {
                        var entity = destroyedEntities[i];
                        if (_entityToIndex.TryGetValue(entity, out var index))
                        {
                            _transforms.RemoveAtSwapBack(index);
                            _entities.RemoveAtSwapBack(index);
                            _entityToIndex.Remove(entity);

                            if (index < _entities.Length)
                            {
                                var moved = _entities[index];
                                _entityToIndex[moved] = index;
                            }
                        }
                    }
                    EntityManager.RemoveComponent<TweenTransformCleanup>(_destroyedQuery);
                }

                if (hasCreated)
                {
                    using var createdEntities = _createdQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < createdEntities.Length; i++)
                    {
                        var entity = createdEntities[i];
                        var reference = EntityManager.GetComponentData<TweenTransformReference>(entity);
                        var transform = reference.Transform.Value;
                        if (transform == null)
                        {
                            PlaybackControlInternal.KillInternal(entity, EntityManager);
                            continue;
                        }

                        var index = _entities.Length;
                        _transforms.Add(transform);
                        _entities.Add(entity);
                        _entityToIndex.Add(entity, index);
                        EntityManager.AddComponent<TweenTransformCleanup>(entity);
                    }
                }
            }

            if (_transforms.length == 0)
                return;

            _targetLookup.Update(this);
            _runtimeFloat3Lookup.Update(this);
            _runtimeQuaternionLookup.Update(this);
            _runtimeFloatLookup.Update(this);

            var job = new ApplyTweenTransformJob
            {
                Entities = _entities.AsArray(),
                TargetLookup = _targetLookup,
                RuntimeFloat3Lookup = _runtimeFloat3Lookup,
                RuntimeQuaternionLookup = _runtimeQuaternionLookup,
                RuntimeFloatLookup = _runtimeFloatLookup
            };

            Dependency = job.ScheduleByRef(_transforms, Dependency);
        }

        [BurstCompile]
        private struct ApplyTweenTransformJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<Entity> Entities;
            [ReadOnly] public ComponentLookup<TweenTransformTarget> TargetLookup;
            [ReadOnly] public ComponentLookup<TweenRuntime<float3>> RuntimeFloat3Lookup;
            [ReadOnly] public ComponentLookup<TweenRuntime<quaternion>> RuntimeQuaternionLookup;
            [ReadOnly] public ComponentLookup<TweenRuntime<float>> RuntimeFloatLookup;

            public void Execute(int index, TransformAccess transform)
            {
                if (!transform.isValid) return;

                var entity = Entities[index];
                if (!TargetLookup.HasComponent(entity)) return;

                var target = TargetLookup[entity];
                switch (target.Binding)
                {
                    case TweenTransformBinding.Position:
                        if (RuntimeFloat3Lookup.HasComponent(entity))
                        {
                            var pos = RuntimeFloat3Lookup[entity].CurrentValue;
                            if (target.Space == TweenSpace.World)
                                transform.position = pos;
                            else
                                transform.localPosition = pos;
                        }
                        break;

                    case TweenTransformBinding.Rotation:
                        if (RuntimeQuaternionLookup.HasComponent(entity))
                        {
                            var rot = RuntimeQuaternionLookup[entity].CurrentValue;
                            if (target.Space == TweenSpace.World)
                                transform.rotation = rot;
                            else
                                transform.localRotation = rot;
                        }
                        break;

                    case TweenTransformBinding.Scale:
                        if (RuntimeFloat3Lookup.HasComponent(entity))
                        {
                            transform.localScale = RuntimeFloat3Lookup[entity].CurrentValue;
                        }
                        break;

                    case TweenTransformBinding.ScaleUniform:
                        if (RuntimeFloatLookup.HasComponent(entity))
                        {
                            transform.localScale = new float3(RuntimeFloatLookup[entity].CurrentValue);
                        }
                        break;
                }
            }
        }
    }
}
