using Unity.Burst;
using Unity.Mathematics;

namespace XO.Curve
{
    [BurstCompile]
    public static class Ease
    {
        [BurstCompile]
        public static void Sample(in float from, in float to, float t, EaseType easeType, ref float result)
        {
            SampleGeneric<float, FloatMath>(from, to, t, easeType, ref result);
        }
        [BurstCompile]
        public static void Sample(in float2 from, in float2 to, float t, EaseType easeType, ref float2 result)
        {
            SampleGeneric<float2, Float2Math>(from, to, t, easeType, ref result);
        }

        [BurstCompile]
        public static void Sample(in float3 from, in float3 to, float t, EaseType easeType, ref float3 result)
        {
            SampleGeneric<float3, Float3Math>(from, to, t, easeType, ref result);
        }
        [BurstCompile]
        public static void Sample(in quaternion from, in quaternion to, float t, EaseType easeType, ref quaternion result)
        {
            SampleGeneric<quaternion, QuaternionMath>(from, to, t, easeType, ref result);
        }

        [BurstCompile]
        public static void SampleGeneric<T, TMath>(in T from, in T to, float t, EaseType easeType, ref T result,
            TMath mathProvider = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            var easedT = EasedT(t, easeType);
            result = mathProvider.Lerp(from, to, easedT);
        }

        [BurstCompile]
        public static float EasedT(float t, EaseType easeType)
        {
            t = math.clamp(t, 0f, 1f);
            float easedT;
            switch (easeType)
            {
                case EaseType.None:
                    easedT = t;
                    break;

                case EaseType.Linear:
                    easedT = t;
                    break;

                case EaseType.InSine:
                    easedT = 1f - math.cos((t * math.PI) / 2f);
                    break;

                case EaseType.OutSine:
                    easedT = math.sin((t * math.PI) / 2f);
                    break;

                case EaseType.InOutSine:
                    easedT = -(math.cos(math.PI * t) - 1f) / 2f;
                    break;

                case EaseType.InQuad:
                    easedT = t * t;
                    break;

                case EaseType.OutQuad:
                    easedT = 1f - (1f - t) * (1f - t);
                    break;

                case EaseType.InOutQuad:
                    easedT = t < 0.5f ? 2f * t * t : 1f - math.pow(-2f * t + 2f, 2f) / 2f;
                    break;

                case EaseType.InCubic:
                    easedT = t * t * t;
                    break;

                case EaseType.OutCubic:
                    easedT = 1f - math.pow(1f - t, 3f);
                    break;

                case EaseType.InOutCubic:
                    easedT = t < 0.5f ? 4f * t * t * t : 1f - math.pow(-2f * t + 2f, 3f) / 2f;
                    break;

                case EaseType.InQuart:
                    easedT = t * t * t * t;
                    break;

                case EaseType.OutQuart:
                    easedT = 1f - math.pow(1f - t, 4f);
                    break;

                case EaseType.InOutQuart:
                    easedT = t < 0.5f ? 8f * t * t * t * t : 1f - math.pow(-2f * t + 2f, 4f) / 2f;
                    break;

                case EaseType.InQuint:
                    easedT = t * t * t * t * t;
                    break;

                case EaseType.OutQuint:
                    easedT = 1f - math.pow(1f - t, 5f);
                    break;

                case EaseType.InOutQuint:
                    easedT = t < 0.5f ? 16f * t * t * t * t * t : 1f - math.pow(-2f * t + 2f, 5f) / 2f;
                    break;

                case EaseType.InExpo:
                    easedT = t == 0f ? 0f : math.pow(2f, 10f * t - 10f);
                    break;

                case EaseType.OutExpo:
                    easedT = t == 1f ? 1f : 1f - math.pow(2f, -10f * t);
                    break;

                case EaseType.InOutExpo:
                    easedT = t == 0f ? 0f :
                        t == 1f ? 1f :
                        t < 0.5f ? math.pow(2f, 20f * t - 10f) / 2f :
                        (2f - math.pow(2f, -20f * t + 10f)) / 2f;
                    break;

                case EaseType.InCirc:
                    easedT = 1f - math.sqrt(1f - t * t);
                    break;

                case EaseType.OutCirc:
                    easedT = math.sqrt(1f - math.pow(t - 1f, 2f));
                    break;

                case EaseType.InOutCirc:
                    easedT = t < 0.5f
                        ? (1f - math.sqrt(1f - 4f * t * t)) / 2f
                        : (math.sqrt(1f - math.pow(-2f * t + 2f, 2f)) + 1f) / 2f;
                    break;

                case EaseType.InBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    easedT = c3 * t * t * t - c1 * t * t;
                    break;

                case EaseType.OutBack:
                    const float c2 = 1.70158f;
                    const float c4 = c2 + 1f;
                    easedT = 1f + c4 * math.pow(t - 1f, 3f) + c2 * math.pow(t - 1f, 2f);
                    break;

                case EaseType.InOutBack:
                    const float c5 = 1.70158f * 1.525f;
                    easedT = t < 0.5f
                        ? (math.pow(2f * t, 2f) * ((c5 + 1f) * 2f * t - c5)) / 2f
                        : (math.pow(2f * t - 2f, 2f) * ((c5 + 1f) * (t * 2f - 2f) + c5) + 2f) / 2f;
                    break;

                case EaseType.InBounce:
                    easedT = 1f - BounceOut(1f - t);
                    break;

                case EaseType.OutBounce:
                    easedT = BounceOut(t);
                    break;

                case EaseType.InOutBounce:
                    easedT = t < 0.5f
                        ? (1f - BounceOut(1f - 2f * t)) / 2f
                        : (1f + BounceOut(2f * t - 1f)) / 2f;
                    break;
                case EaseType.InElastic:
                {
                    const float c7 = (2f * math.PI) / 3f;
                    easedT = t == 0f ? 0f : t == 1f ? 1f : -math.pow(2f, 10f * t - 10f) * math.sin((t * 10f - 10.75f) * c7);
                    break;
                }
                case EaseType.OutElastic:
                {
                    const float c7 = (2f * math.PI) / 3f;
                    easedT = t == 0f ? 0f : t == 1f ? 1f : math.pow(2f, -10f * t) * math.sin((t * 10f - 0.75f) * c7) + 1f;
                    break;
                }
                case EaseType.InOutElastic:
                {
                    const float c6 = (2f * math.PI) / 4.5f;
                    easedT = t == 0f ? 0f : t == 1f ? 1f : t < 0.5f
                        ? -(math.pow(2f, 20f * t - 10f) * math.sin((20f * t - 11.125f) * c6)) / 2f
                        : (math.pow(2f, -20f * t + 10f) * math.sin((20f * t - 11.125f) * c6)) / 2f + 1f;
                    break;
                }
                default:
                    easedT = t;
                    break;
            }
            return easedT;
        }

        [BurstCompile]
        private static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            switch (t)
            {
                case < 1f / d1:
                    return n1 * t * t;
                case < 2f / d1:
                    t -= 1.5f / d1;
                    return n1 * t * t + 0.75f;
                case < 2.5f / d1:
                    t -= 2.25f / d1;
                    return n1 * t * t + 0.9375f;
                default:
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
            }
        }
    }

    [System.Serializable]
    public enum EaseType
    {
        None,
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce
    }
}
