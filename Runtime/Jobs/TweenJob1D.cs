using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using XO.Entityween.Utils;

namespace XO.Entityween.Jobs
{
    [BurstCompile]
    public struct TweenJob1D : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Tween<float>> Tweens;
        public NativeArray<TweenRuntimeData<float>> TweensRuntimeData;
        public float Time;
        
        public void Execute(int index)
        {
            if (TweenUtils.IsTweenCompleted(TweensRuntimeData[index].CurrentPosition)) return;
            
            var tween = Tweens[index];
            var newPosition = TweenUtils.CalculateTweenPosition(tween.StartTime, tween.SecondsToPlay, Time);
            var sampledValue = 0f;
            
            TweenUtils.SampleTween(tween.IsSpline, TweensRuntimeData[index].CurrentPosition, tween.EaseType, tween.StartPoint,
                tween.EndPoint, tween.Spline, ref sampledValue);
            TweensRuntimeData[index] = new TweenRuntimeData<float>(sampledValue, newPosition,TweensRuntimeData[index].CurrentLoopIndex);
            tween.Update();
        }
    }
}