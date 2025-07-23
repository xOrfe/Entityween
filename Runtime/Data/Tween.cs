using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    public delegate void TweenCallback();
    
    public readonly struct Tween<T> where T : unmanaged
    {
        public float StartTime { get; }
        public  float SecondsToPlay { get; }
        public  EaseType EaseType { get; }
        
        public  T StartPoint { get; }
        public  T EndPoint { get; }
        
        public  bool IsSpline { get; }
        public  bool DisposeSpline { get; }
        public  BlobAssetReference<Spline.SplineBlob<T>> Spline { get; }
        
        public  bool IsLoop { get; }
        public  uint LoopCount { get; }
        public  LoopType LoopType { get; }
        
        private FunctionPointer<TweenCallback>? OnStart { get; }
        private FunctionPointer<TweenCallback>? OnUpdate { get; }
        private FunctionPointer<TweenCallback>? OnLoop { get; }
        private FunctionPointer<TweenCallback>? OnComplete { get; }
        
        public  void Start()
        {
            OnStart?.Invoke();
        }

        public  void Update()
        {
            OnUpdate?.Invoke();
        }

        public  void Loop()
        {
            OnLoop?.Invoke();
        }

        public  void Complete()
        {
            OnComplete?.Invoke();
        }

        public  void Dispose()
        {
            if(DisposeSpline)
                Spline.Dispose();
        }
        
        public Tween(TweenBlueprint<T> blueprint)
        {
            StartPoint = blueprint.StartPoint;
            EndPoint = blueprint.EndPoint;
            
            StartTime = Time.time;
            SecondsToPlay = blueprint.SecondsToPlay;
            
            IsSpline = blueprint.IsSpline;
            DisposeSpline = blueprint.DisposeSpline;
            Spline = blueprint.Spline;
            
            IsLoop = blueprint.IsLoop;
            LoopCount = blueprint.LoopCount;
            LoopType = blueprint.LoopType;

            EaseType = blueprint.EaseType;

            OnStart = blueprint.OnStart;
            OnUpdate = blueprint.OnUpdate;
            OnComplete = blueprint.OnComplete;
            OnLoop = blueprint.OnLoop;
        }
    }
    
    public  readonly struct TweenRuntimeData<T> where T : unmanaged
    {
        public T CurrentValue { get; }
        public float CurrentPosition { get; }
        public  int CurrentLoopIndex { get; }
        public TweenRuntimeData(T currentValue = default,float currentPosition = 0,int currentLoopIndex = 0)
        {
            CurrentPosition = currentPosition;
            CurrentLoopIndex = currentLoopIndex;
            CurrentValue = currentValue;
        }
    }
}