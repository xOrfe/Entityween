using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using XO.Entityween.Utils;

namespace XO.Entityween.Jobs
{
    [BurstCompile]
    public struct TweenJob2D : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Tween<float2>> Tweens;
        public NativeArray<TweenRuntimeData<float2>> TweensRuntimeData;
        public float Time;
        
        public void Execute(int index)
        {
            if (TweenUtils.IsTweenCompleted(TweensRuntimeData[index].CurrentPosition)) return;
            
            var tween = Tweens[index];
            var newPosition = TweenUtils.CalculateTweenPosition(tween.StartTime, tween.SecondsToPlay, Time);
            var sampledValue = float2.zero;
            
            TweenUtils.SampleTween(tween.IsSpline, TweensRuntimeData[index].CurrentPosition, tween.EaseType, tween.StartPoint,
                tween.EndPoint, tween.Spline, ref sampledValue);
            TweensRuntimeData[index] = new TweenRuntimeData<float2>(sampledValue, newPosition,TweensRuntimeData[index].CurrentLoopIndex);
            tween.Update();
        }
    }
}