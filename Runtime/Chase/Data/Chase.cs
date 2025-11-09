using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    public delegate void ChaseCallback();

    public readonly struct Chase
    {
        public Entity TargetEntity { get; }
        public float2 TStepMinMax { get; }
        public float MaxStepTime { get; }
        public bool IsOverride { get; }
        public bool ChasePosition { get; }
        public bool ChaseRotation { get; }
        public bool LookRotation { get; }
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

        public Chase(ChaseBlueprint blueprint)
        {
            TargetEntity = blueprint.Target;
            TStepMinMax = blueprint.TStepMinMax;
            MaxStepTime = blueprint.MaxStepTime;
            IsOverride = blueprint.IsOverride;
            ChasePosition = blueprint.chasePosition;
            ChaseRotation = blueprint.chaseRotation;
            LookRotation = blueprint.lookRotation;
            OnStart = blueprint.OnStart;
            OnUpdate = blueprint.OnUpdate;
            OnChased = blueprint.OnChased;
        }
    }

    public readonly struct ChaseRuntimeData
    {
        public readonly float positionalT;
        public readonly float angularT;

        public ChaseRuntimeData(float positionalT, float angularT)
        {
            this.positionalT = positionalT;
            this.angularT = angularT;
        }
    }
}