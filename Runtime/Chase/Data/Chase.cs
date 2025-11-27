using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    public delegate void ChaseCallback();

    public readonly struct Chase<T> where T : unmanaged
    {
        public Entity TargetEntity { get; }
        public float2 TStepMinMax { get; }
        public float MaxStepTime { get; }
        public bool IsOverride { get; }
        public T Transform { get; }
        private FunctionPointer<ChaseCallback>? OnStart { get; }
        private FunctionPointer<ChaseCallback>? OnUpdate { get; }
        private FunctionPointer<ChaseCallback>? OnChased { get; }

        public void Start()
        {
            OnStart?.Invoke();
        }

        public void Update()
        {
            OnUpdate?.Invoke();
        }

        public void Chased()
        {
            OnChased?.Invoke();
        }

        public void Dispose()
        {
        }

        public Chase(ChaseBlueprint<T> blueprint)
        {
            TargetEntity = blueprint.Target;
            TStepMinMax = blueprint.TStepMinMax;
            MaxStepTime = blueprint.MaxStepTime;
            IsOverride = blueprint.IsOverride;
            Transform = blueprint.Transform;
            OnStart = blueprint.OnStart;
            OnUpdate = blueprint.OnUpdate;
            OnChased = blueprint.OnChased;
        }
    }

    public readonly struct ChaseRuntimeData<T> where T : unmanaged
    {
        public readonly float Time;

        public ChaseRuntimeData(float time)
        {
            Time = time;
        }
    }
}