using XO.Curve;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween.Utils
{
    [BurstCompile]
    public static class TweenUtils
    {
        [BurstCompile]
        public static bool IsTweenCompleted(float currentPosition)
        {
            return currentPosition >= 1;
        }
        [BurstCompile]
        public static float CalculateTweenPosition(float startTime,float secondsToPlay, float time)
        {
            return math.clamp((time - startTime) / secondsToPlay, 0, 1);
        }
        
        [BurstCompile]
        public static void SampleTween(bool isSpline,float currentPosition, EaseType easeType, float startPoint, float endPoint,in BlobAssetReference<Spline.SplineBlob<float>> splineBlobRef,ref float sampledValue)
        {
            if (isSpline)
            {
                var easedT = Ease.EasedT(currentPosition, easeType);
                Spline.Sample(splineBlobRef,easedT,ref sampledValue);
            }
            else
            {
                Ease.Sample(startPoint, endPoint, currentPosition, easeType, ref sampledValue);
            }
        }
        public static void SampleTween(bool isSpline,float currentPosition, EaseType easeType,in float2 startPoint,in float2 endPoint,in BlobAssetReference<Spline.SplineBlob<float2>> splineBlobRef,ref float2 sampledValue)
        {
            if (isSpline)
            {
                var easedT = Ease.EasedT(currentPosition, easeType);
                Spline.Sample(splineBlobRef,easedT,ref sampledValue);
            }
            else
            {
                Ease.Sample(startPoint, endPoint, currentPosition, easeType, ref sampledValue);
            }
        }
        public static void SampleTween(bool isSpline,float currentPosition, EaseType easeType,in float3 startPoint,in float3 endPoint,in BlobAssetReference<Spline.SplineBlob<float3>> splineBlobRef,ref float3 sampledValue)
        {
            if (isSpline)
            {
                var easedT = Ease.EasedT(currentPosition, easeType);
                Spline.Sample(splineBlobRef,easedT,ref sampledValue);
            }
            else
            {
                Ease.Sample(startPoint, endPoint, currentPosition, easeType, ref sampledValue);
            }
        }
        public static void SampleTween(bool isSpline,float currentPosition, EaseType easeType,in quaternion startPoint,in quaternion endPoint,in BlobAssetReference<Spline.SplineBlob<quaternion>> splineBlobRef,ref quaternion sampledValue)
        {
            if (isSpline)
            {
                var easedT = Ease.EasedT(currentPosition, easeType);
                Spline.Sample(splineBlobRef,easedT,ref sampledValue);
            }
            else
            {
                Ease.Sample(startPoint, endPoint, currentPosition, easeType, ref sampledValue);
            }
        }

    }
}