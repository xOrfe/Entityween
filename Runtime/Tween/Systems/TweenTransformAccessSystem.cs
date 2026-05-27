using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        private EntityQuery _transformTargetQuery;
        private readonly Dictionary<Entity, int> _indices = new(1024);
        private readonly HashSet<Entity> _seen = new();
        private readonly List<Entity> _managedEntities = new(1024);
        private TransformAccessArray _transforms;
        private NativeList<Entity> _entities;
        private NativeList<TweenTransformBinding> _bindings;
        private NativeList<TweenSpace> _spaces;
        private NativeList<float3> _float3Values;
        private NativeList<quaternion> _quaternionValues;
        private NativeList<float> _floatValues;
        private NativeParallelHashMap<Entity, int> _entityToIndex;
        private int _lastOrderVersion = -1;

        protected override void OnCreate()
        {
            _transformTargetQuery = SystemAPI.QueryBuilder()
                .WithAll<TweenTransformTarget, TweenTransformReference>()
                .Build();

            _transforms = new TransformAccessArray(1024);
            _entities = new NativeList<Entity>(1024, Allocator.Persistent);
            _bindings = new NativeList<TweenTransformBinding>(1024, Allocator.Persistent);
            _spaces = new NativeList<TweenSpace>(1024, Allocator.Persistent);
            _float3Values = new NativeList<float3>(1024, Allocator.Persistent);
            _quaternionValues = new NativeList<quaternion>(1024, Allocator.Persistent);
            _floatValues = new NativeList<float>(1024, Allocator.Persistent);
            _entityToIndex = new NativeParallelHashMap<Entity, int>(1024, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            Dependency.Complete();
            if (_transforms.isCreated) _transforms.Dispose();
            if (_entities.IsCreated) _entities.Dispose();
            if (_bindings.IsCreated) _bindings.Dispose();
            if (_spaces.IsCreated) _spaces.Dispose();
            if (_float3Values.IsCreated) _float3Values.Dispose();
            if (_quaternionValues.IsCreated) _quaternionValues.Dispose();
            if (_floatValues.IsCreated) _floatValues.Dispose();
            if (_entityToIndex.IsCreated) _entityToIndex.Dispose();
        }

        protected override void OnUpdate()
        {
            if (_transformTargetQuery.IsEmptyIgnoreFilter)
            {
                if (_transforms.length > 0)
                {
                    Dependency.Complete();
                    ClearCachedTargets();
                    _lastOrderVersion = -1;
                }
                return;
            }

            var orderVersion = _transformTargetQuery.GetCombinedComponentOrderVersion(true);
            if (orderVersion != _lastOrderVersion)
            {
                Dependency.Complete();
                SyncCachedTargets();
                _lastOrderVersion = orderVersion;
            }

            if (_transforms.length == 0)
                return;

            var dependency = Dependency;
            var entityToIndex = _entityToIndex;

            dependency = new CopyFloatTransformValuesJob
            {
                EntityToIndex = entityToIndex,
                FloatValues = _floatValues.AsArray()
            }.ScheduleParallel(dependency);

            dependency = new CopyFloat3TransformValuesJob
            {
                EntityToIndex = entityToIndex,
                Float3Values = _float3Values.AsArray()
            }.ScheduleParallel(dependency);

            dependency = new CopyQuaternionTransformValuesJob
            {
                EntityToIndex = entityToIndex,
                QuaternionValues = _quaternionValues.AsArray()
            }.ScheduleParallel(dependency);

            var job = new ApplyTweenTransformJob
            {
                Bindings = _bindings.AsArray(),
                Spaces = _spaces.AsArray(),
                Float3Values = _float3Values.AsArray(),
                QuaternionValues = _quaternionValues.AsArray(),
                FloatValues = _floatValues.AsArray()
            };

            Dependency = job.ScheduleByRef(_transforms, dependency);
        }

        private void SyncCachedTargets()
        {
            _seen.Clear();
            using var entities = _transformTargetQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                _seen.Add(entity);

                var reference = EntityManager.GetComponentObject<TweenTransformReference>(entity);
                var transform = reference.Transform;
                if (transform == null)
                {
                    PlaybackControlInternal.KillInternal(entity, EntityManager);
                    continue;
                }

                var target = EntityManager.GetComponentData<TweenTransformTarget>(entity);
                if (_indices.TryGetValue(entity, out var index))
                {
                    _transforms[index] = transform;
                    _bindings[index] = target.Binding;
                    _spaces[index] = target.Space;
                    continue;
                }

                AddCachedTarget(entity, transform, target);
            }

            for (int i = _managedEntities.Count - 1; i >= 0; i--)
            {
                var entity = _managedEntities[i];
                if (!_seen.Contains(entity) ||
                    !EntityManager.Exists(entity) ||
                    !EntityManager.HasComponent<TweenTransformTarget>(entity) ||
                    !EntityManager.HasComponent<TweenTransformReference>(entity))
                {
                    RemoveCachedTargetAt(i);
                }
            }
        }

        private void AddCachedTarget(Entity entity, UnityEngine.Transform transform, TweenTransformTarget target)
        {
            var index = _managedEntities.Count;
            _indices.Add(entity, index);
            _managedEntities.Add(entity);
            _transforms.Add(transform);
            _entities.Add(entity);
            _bindings.Add(target.Binding);
            _spaces.Add(target.Space);
            _float3Values.Add(default);
            _quaternionValues.Add(quaternion.identity);
            _floatValues.Add(1f);
            _entityToIndex.Add(entity, index);
        }

        private void RemoveCachedTargetAt(int index)
        {
            var last = _managedEntities.Count - 1;
            var removed = _managedEntities[index];
            _indices.Remove(removed);
            _entityToIndex.Remove(removed);

            _transforms.RemoveAtSwapBack(index);
            _entities.RemoveAtSwapBack(index);
            _bindings.RemoveAtSwapBack(index);
            _spaces.RemoveAtSwapBack(index);
            _float3Values.RemoveAtSwapBack(index);
            _quaternionValues.RemoveAtSwapBack(index);
            _floatValues.RemoveAtSwapBack(index);

            if (index != last)
            {
                var moved = _managedEntities[last];
                _managedEntities[index] = moved;
                _indices[moved] = index;
                _entityToIndex[moved] = index;
            }

            _managedEntities.RemoveAt(last);
        }

        private void ClearCachedTargets()
        {
            _indices.Clear();
            _managedEntities.Clear();
            for (int i = _transforms.length - 1; i >= 0; i--)
                _transforms.RemoveAtSwapBack(i);
            _entities.Clear();
            _bindings.Clear();
            _spaces.Clear();
            _float3Values.Clear();
            _quaternionValues.Clear();
            _floatValues.Clear();
            _entityToIndex.Clear();
        }

        [BurstCompile]
        private partial struct CopyFloatTransformValuesJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<Entity, int> EntityToIndex;
            [NativeDisableParallelForRestriction] public NativeArray<float> FloatValues;

            private void Execute(Entity entity, in TweenRuntime<float> runtime, in TweenTransformTarget target)
            {
                if (target.Binding != TweenTransformBinding.ScaleUniform) return;
                if (!EntityToIndex.TryGetValue(entity, out var index)) return;
                if ((uint)index >= (uint)FloatValues.Length) return;
                FloatValues[index] = runtime.CurrentValue;
            }
        }

        [BurstCompile]
        private partial struct CopyFloat3TransformValuesJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<Entity, int> EntityToIndex;
            [NativeDisableParallelForRestriction] public NativeArray<float3> Float3Values;

            private void Execute(Entity entity, in TweenRuntime<float3> runtime, in TweenTransformTarget target)
            {
                if (!EntityToIndex.TryGetValue(entity, out var index)) return;
                if ((uint)index >= (uint)Float3Values.Length) return;
                Float3Values[index] = runtime.CurrentValue;
            }
        }

        [BurstCompile]
        private partial struct CopyQuaternionTransformValuesJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<Entity, int> EntityToIndex;
            [NativeDisableParallelForRestriction] public NativeArray<quaternion> QuaternionValues;

            private void Execute(Entity entity, in TweenRuntime<quaternion> runtime, in TweenTransformTarget target)
            {
                if (target.Binding != TweenTransformBinding.Rotation) return;
                if (!EntityToIndex.TryGetValue(entity, out var index)) return;
                if ((uint)index >= (uint)QuaternionValues.Length) return;
                QuaternionValues[index] = runtime.CurrentValue;
            }
        }

        [BurstCompile]
        private struct ApplyTweenTransformJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<TweenTransformBinding> Bindings;
            [ReadOnly] public NativeArray<TweenSpace> Spaces;
            [ReadOnly] public NativeArray<float3> Float3Values;
            [ReadOnly] public NativeArray<quaternion> QuaternionValues;
            [ReadOnly] public NativeArray<float> FloatValues;

            public void Execute(int index, TransformAccess transform)
            {
                switch (Bindings[index])
                {
                    case TweenTransformBinding.Position:
                        if (Spaces[index] == TweenSpace.World)
                            transform.position = Float3Values[index];
                        else
                            transform.localPosition = Float3Values[index];
                        break;

                    case TweenTransformBinding.Rotation:
                        if (Spaces[index] == TweenSpace.World)
                            transform.rotation = QuaternionValues[index];
                        else
                            transform.localRotation = QuaternionValues[index];
                        break;

                    case TweenTransformBinding.Scale:
                        transform.localScale = Float3Values[index];
                        break;

                    case TweenTransformBinding.ScaleUniform:
                        transform.localScale = new float3(FloatValues[index]);
                        break;
                }
            }
        }
    }
}
