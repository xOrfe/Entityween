using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using XO.Entityween.Jobs;

namespace XO.Entityween
{
    [BurstCompile]
    [DisableAutoCreation]
    public partial class TweenHandlingSystem : SystemBase
    {
        private TweenContainer<float> _tweens1D;
        private TweenContainer<float2> _tweens2D;
        private TweenContainer<float3> _tweens3D;
        private TweenContainer<quaternion> _tweens4D;

        [BurstCompile]
        protected override void OnCreate()
        {
            InitializeContainers();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            Dependency.Complete();
            var time = SystemAPI.Time.ElapsedTime;
            var localToWorldLookup = GetComponentLookup<LocalToWorld>(isReadOnly: true);

            NativeList<JobHandle> handles = new NativeList<JobHandle>(Allocator.Temp);
            handles.Add(Dependency);
            if (_tweens1D.Calculate)
            {
                var tweenJob1D = new TweenJob1D
                {
                    Tweens = _tweens1D.Tweens.AsDeferredJobArray(),
                    TweensRuntimeData = _tweens1D.TweensRuntimeData.AsDeferredJobArray(),
                    Time = (float)time
                }.Schedule(_tweens1D.Tweens.Length, 64, Dependency);
                handles.Add(tweenJob1D);
            }

            if (_tweens2D.Calculate)
            {
                var tweenJob2D = new TweenJob2D
                {
                    Tweens = _tweens2D.Tweens.AsDeferredJobArray(),
                    TweensRuntimeData = _tweens2D.TweensRuntimeData.AsDeferredJobArray(),
                    Time = (float)time
                }.Schedule(_tweens2D.Tweens.Length, 64, Dependency);
                handles.Add(tweenJob2D);
            }

            if (_tweens3D.Calculate)
            {
                var tweenJob3D = new TweenJob3D
                {
                    Tweens = _tweens3D.Tweens.AsDeferredJobArray(),
                    TweensRuntimeData = _tweens3D.TweensRuntimeData.AsDeferredJobArray(),
                    Time = (float)time
                }.Schedule(_tweens3D.Tweens.Length, 64, Dependency);
                handles.Add(tweenJob3D);
            }

            if (_tweens4D.Calculate)
            {
                var tweenJob4D = new TweenJob4D
                {
                    Tweens = _tweens4D.Tweens.AsDeferredJobArray(),
                    TweensRuntimeData = _tweens4D.TweensRuntimeData.AsDeferredJobArray(),
                    Time = (float)time
                }.Schedule(_tweens4D.Tweens.Length, 64, Dependency);
                handles.Add(tweenJob4D);
            }

            var tweensHandle = JobHandle.CombineDependencies(handles.ToArray(Allocator.Temp));
            var moveToJob = new MoveToJob()
            {
                TweensRuntimeData = _tweens3D.TweensRuntimeData.AsDeferredJobArray(),
            }.Schedule(tweensHandle);
            var moveToWorldJob = new MoveToWorldJob()
            {
                TweensRuntimeData = _tweens3D.TweensRuntimeData.AsDeferredJobArray(),
                LocalToWorldLookup = localToWorldLookup,
            }.Schedule(moveToJob);
            var rotateToJob = new RotateToJob()
            {
                TweensRuntimeData = _tweens4D.TweensRuntimeData.AsDeferredJobArray(),
            }.Schedule(moveToWorldJob);

            Dependency = rotateToJob;
        }

        [BurstCompile]
        protected override void OnDestroy()
        {
            DisposeContainers();
        }

        [BurstCompile]
        public void AttachTween<T>(TweenBlueprint<T> blueprint, EntityCommandBuffer ecb) where T : unmanaged
        {
            var entityManager = blueprint.World.EntityManager;
            var index = -1;
            switch (blueprint.TweenType)
            {
                case TweenType.None:
                case TweenType.Wait:
                case TweenType.Callback:
                    break;
                case TweenType.MoveTo:
                    if (entityManager.HasComponent<MoveTo>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<MoveTo>(blueprint.Entity).Index;
                        _tweens3D.Override(index, blueprint as TweenBlueprint<float3>);
                    }
                    else
                    {
                        index = _tweens3D.Attach(blueprint as TweenBlueprint<float3>);
                        ecb.AddComponent(blueprint.Entity, new MoveTo(index));
                    }
                    break;
                case TweenType.MoveToWorld:
                    if (entityManager.HasComponent<MoveToWorld>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<MoveToWorld>(blueprint.Entity).Index;
                        _tweens3D.Override(index, blueprint as TweenBlueprint<float3>);
                    }
                    else
                    {
                        index = _tweens3D.Attach(blueprint as TweenBlueprint<float3>);
                        ecb.AddComponent(blueprint.Entity, new MoveToWorld(index));
                    }
                    break;
                case TweenType.RotateTo:
                    if (entityManager.HasComponent<RotateTo>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<RotateTo>(blueprint.Entity).Index;
                        _tweens3D.Override(index, blueprint as TweenBlueprint<float3>);
                    }
                    else
                    {
                        index = _tweens3D.Attach(blueprint as TweenBlueprint<float3>);
                        ecb.AddComponent(blueprint.Entity, new RotateTo(index));
                    }
                    break;
                case TweenType.RotateToWorld:
                    if (entityManager.HasComponent<RotateToWorld>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<RotateToWorld>(blueprint.Entity).Index;
                        _tweens4D.Override(index, blueprint as TweenBlueprint<quaternion>);
                    }
                    else
                    {
                        index = _tweens4D.Attach(blueprint as TweenBlueprint<quaternion>);
                        ecb.AddComponent(blueprint.Entity, new RotateToWorld(index));
                    }
                    break;
                case TweenType.ScaleTo:
                    if (entityManager.HasComponent<ScaleTo>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<ScaleTo>(blueprint.Entity).Index;
                        _tweens3D.Override(index, blueprint as TweenBlueprint<float3>);
                    }
                    else
                    {
                        index = _tweens3D.Attach(blueprint as TweenBlueprint<float3>);
                        ecb.AddComponent(blueprint.Entity, new ScaleTo(index));
                    }
                    break;
                case TweenType.ScaleToUniform:
                    if (entityManager.HasComponent<ScaleToUniform>(blueprint.Entity))
                    {
                        index = entityManager.GetComponentData<ScaleToUniform>(blueprint.Entity).Index;
                        _tweens1D.Override(index, blueprint as TweenBlueprint<float>);
                    }
                    else
                    {
                        index = _tweens1D.Attach(blueprint as TweenBlueprint<float>);
                        ecb.AddComponent(blueprint.Entity, new ScaleToUniform(index));
                    }
                    break;
                case TweenType.FloatTo:
                    index = _tweens1D.Attach(blueprint as TweenBlueprint<float>);
                    break;
                case TweenType.Float2To:
                    index = _tweens2D.Attach(blueprint as TweenBlueprint<float2>);
                    break;
                case TweenType.Float3To:
                    index = _tweens3D.Attach(blueprint as TweenBlueprint<float3>);
                    break;
                case TweenType.QuaternionTo:
                    index = _tweens4D.Attach(blueprint as TweenBlueprint<quaternion>);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (index != -1)
                blueprint.OnStart?.Invoke.Invoke();
        }

        [BurstCompile]
        private void InitializeContainers()
        {
            _tweens1D = new TweenContainer<float>(64);
            _tweens2D = new TweenContainer<float2>(64);
            _tweens3D = new TweenContainer<float3>(64);
            _tweens4D = new TweenContainer<quaternion>(64);
        }

        [BurstCompile]
        private void DisposeContainers()
        {
            _tweens1D.Dispose();
            _tweens2D.Dispose();
            _tweens3D.Dispose();
            _tweens4D.Dispose();
        }
    }
}