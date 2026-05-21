using Unity.Mathematics;

namespace XO.Curve
{

    public interface ICurveMath<T> where T : unmanaged
    {
        T Lerp(T a, T b, float t);
        float GetDistance(T a, T b);
        T EvaluateSpline(SplineType type, T p0, T p1, T p2, T p3, float t);
        T Add(T a, T b);
        T SmoothDamp(T current, T target, ref T currentVelocity, float smoothTime, float maxSpeed, float deltaTime);
    }

    public struct FloatMath : ICurveMath<float>
    {
        public float Lerp(float a, float b, float t) => math.lerp(a, b, t);
        public float GetDistance(float a, float b) => math.abs(a - b);

        public float EvaluateSpline(SplineType type, float p0, float p1, float p2, float p3, float t)
        {
            switch (type)
            {
                case SplineType.Linear: return math.lerp(p0, p1, t);
                case SplineType.Step: return t >= 1f ? p1 : p0;
                case SplineType.CubicBezier:
                    float u = 1f - t; float tt = t * t; float uu = u * u;
                    return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
                case SplineType.CatmullRom:
                    float t2 = t * t; float t3 = t2 * t;
                    return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                case SplineType.BSpline:
                    float bt2 = t * t; float bt3 = bt2 * t;
                    return (1f / 6f) * ((-p0 + 3f * p1 - 3f * p2 + p3) * bt3 + (3f * p0 - 6f * p1 + 3f * p2) * bt2 + (-3f * p0 + 3f * p2) * t + (p0 + 4f * p1 + p2));
                default: return p0;
            }
        }
        public float Add(float a, float b) => a + b;

        public float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed,
            float deltaTime)
        {
            return CurveMathUtility.SmoothDampFloat(current, target, ref currentVelocity, smoothTime, maxSpeed,
                deltaTime);
        }
    }

    public struct Float2Math : ICurveMath<float2>
    {
        public float2 Lerp(float2 a, float2 b, float t) => math.lerp(a, b, t);
        public float GetDistance(float2 a, float2 b) => math.distance(a, b);

        public float2 EvaluateSpline(SplineType type, float2 p0, float2 p1, float2 p2, float2 p3, float t)
        {
            switch (type)
            {
                case SplineType.Linear: return math.lerp(p0, p1, t);
                case SplineType.Step: return t >= 1f ? p1 : p0;
                case SplineType.CubicBezier:
                    float u = 1f - t; float tt = t * t; float uu = u * u;
                    return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
                case SplineType.CatmullRom:
                    float t2 = t * t; float t3 = t2 * t;
                    return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                case SplineType.BSpline:
                    float bt2 = t * t; float bt3 = bt2 * t;
                    return (1f / 6f) * ((-p0 + 3f * p1 - 3f * p2 + p3) * bt3 + (3f * p0 - 6f * p1 + 3f * p2) * bt2 + (-3f * p0 + 3f * p2) * t + (p0 + 4f * p1 + p2));
                default: return p0;
            }
        }
        public float2 Add(float2 a, float2 b) => a + b;

        public float2 SmoothDamp(float2 current, float2 target, ref float2 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            smoothTime = math.max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float2 change = current - target;
            float2 originalTo = target;

            float maxChange = maxSpeed * smoothTime;
            float sqrmag = math.lengthsq(change);
            if (sqrmag > maxChange * maxChange)
            {
                float mag = math.sqrt(sqrmag);
                change = change / mag * maxChange;
            }

            target = current - change;
            float2 temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float2 output = target + (change + temp) * exp;

            if (math.dot(originalTo - current, output - originalTo) > 0)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }
    }

    public struct Float3Math : ICurveMath<float3>
    {
        public float3 Lerp(float3 a, float3 b, float t) => math.lerp(a, b, t);
        public float GetDistance(float3 a, float3 b) => math.distance(a, b);

        public float3 EvaluateSpline(SplineType type, float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            switch (type)
            {
                case SplineType.Linear: return math.lerp(p0, p1, t);
                case SplineType.Step: return t >= 1f ? p1 : p0;
                case SplineType.CubicBezier:
                    float u = 1f - t; float tt = t * t; float uu = u * u;
                    return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
                case SplineType.CatmullRom:
                    float t2 = t * t; float t3 = t2 * t;
                    return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                case SplineType.BSpline:
                    float bt2 = t * t; float bt3 = bt2 * t;
                    return (1f / 6f) * ((-p0 + 3f * p1 - 3f * p2 + p3) * bt3 + (3f * p0 - 6f * p1 + 3f * p2) * bt2 + (-3f * p0 + 3f * p2) * t + (p0 + 4f * p1 + p2));
                default: return p0;
            }
        }
        public float3 Add(float3 a, float3 b) => a + b;

        public float3 SmoothDamp(float3 current, float3 target, ref float3 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            smoothTime = math.max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float3 change = current - target;
            float3 originalTo = target;

            float maxChange = maxSpeed * smoothTime;
            float sqrmag = math.lengthsq(change);
            if (sqrmag > maxChange * maxChange)
            {
                float mag = math.sqrt(sqrmag);
                change = change / mag * maxChange;
            }

            target = current - change;
            float3 temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float3 output = target + (change + temp) * exp;

            if (math.dot(originalTo - current, output - originalTo) > 0)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }
    }

    public struct QuaternionMath : ICurveMath<quaternion>
    {
        public quaternion Lerp(quaternion a, quaternion b, float t) => math.slerp(a, b, t);
        public float GetDistance(quaternion a, quaternion b)
        {
            float dot = math.dot(a.value, b.value);
            return math.acos(math.clamp(math.abs(dot), -1f, 1f));
        }

        public quaternion EvaluateSpline(SplineType type, quaternion p0, quaternion p1, quaternion p2, quaternion p3, float t)
        {
            switch (type)
            {
                case SplineType.Linear: return math.slerp(p0, p1, t);
                case SplineType.Step: return t >= 1f ? p1 : p0;
                case SplineType.CubicBezier:
                    var a = math.slerp(p0, p1, t);
                    var b = math.slerp(p1, p2, t);
                    var c = math.slerp(p2, p3, t);
                    var d = math.slerp(a, b, t);
                    var e = math.slerp(b, c, t);
                    return math.slerp(d, e, t);
                case SplineType.CatmullRom:
                case SplineType.BSpline:
                    var v0 = p0.value; var v1 = p1.value; var v2 = p2.value; var v3 = p3.value;
                    if (math.dot(v1, v0) < 0f) v0 = -v0;
                    if (math.dot(v1, v2) < 0f) v2 = -v2;
                    if (math.dot(v2, v3) < 0f) v3 = -v3;
                    float t2 = t * t; float t3 = t2 * t;
                    float4 val;
                    if (type == SplineType.CatmullRom)
                        val = 0.5f * ((2f * v1) + (-v0 + v2) * t + (2f * v0 - 5f * v1 + 4f * v2 - v3) * t2 + (-v0 + 3f * v1 - 3f * v2 + v3) * t3);
                    else
                        val = (1f / 6f) * ((-v0 + 3f * v1 - 3f * v2 + v3) * t3 + (3f * v0 - 6f * v1 + 3f * v2) * t2 + (-3f * v0 + 3f * v2) * t + (v0 + 4f * v1 + v2));
                    return math.normalize(new quaternion(val));
                default: return p0;
            }
        }

        public quaternion Add(quaternion a, quaternion b) => b;

        public quaternion SmoothDamp(quaternion current, quaternion target, ref quaternion currentVelocity,
            float smoothTime, float maxSpeed, float deltaTime)
        {
            if (math.dot(current, target) < 0.0f)
            {
                target.value = -target.value;
            }

            float4 result = new float4(
                CurveMathUtility.SmoothDampFloat(current.value.x, target.value.x, ref currentVelocity.value.x,
                    smoothTime, maxSpeed, deltaTime),
                CurveMathUtility.SmoothDampFloat(current.value.y, target.value.y, ref currentVelocity.value.y,
                    smoothTime, maxSpeed, deltaTime),
                CurveMathUtility.SmoothDampFloat(current.value.z, target.value.z, ref currentVelocity.value.z,
                    smoothTime, maxSpeed, deltaTime),
                CurveMathUtility.SmoothDampFloat(current.value.w, target.value.w, ref currentVelocity.value.w,
                    smoothTime, maxSpeed, deltaTime)
            );

            float length = math.length(result);
            if (length > 0)
            {
                result /= length;
            }

            return new quaternion(result);
        }
    }

    public static class CurveMathUtility
    {
        public static float GetSmoothStep(float smoothTime, float deltaTime)
        {
            float smoothSpeed = smoothTime > 0.0001f ? 1f / smoothTime : 1000f;
            return 1f - math.exp(-smoothSpeed * deltaTime);
        }

        public static float SmoothDampFloat(float current, float target, ref float currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            smoothTime = math.max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;

            float maxChange = maxSpeed * smoothTime;
            change = math.clamp(change, -maxChange, maxChange);

            target = current - change;
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            if ((originalTo - current > 0.0f) == (output > originalTo))
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        public static ICurveMath<T> GetMathProvider<T>() where T : unmanaged
        {
            if (typeof(T) == typeof(float))
                return (ICurveMath<T>)(object)default(FloatMath);
            if (typeof(T) == typeof(float2))
                return (ICurveMath<T>)(object)default(Float2Math);
            if (typeof(T) == typeof(float3))
                return (ICurveMath<T>)(object)default(Float3Math);
            if (typeof(T) == typeof(quaternion))
                return (ICurveMath<T>)(object)default(QuaternionMath);
            throw new System.NotSupportedException($"Type {typeof(T)} is not supported by CurveMath.");
        }
    }
}
