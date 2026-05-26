using Unity.Mathematics;

namespace XO.Curve
{

    public interface ICurveMath<T> where T : unmanaged
    {
        T Lerp(T a, T b, float t);
        float GetDistance(T a, T b);
        T EvaluateSpline(SplineType type, T p0, T p1, T p2, T p3, float t);
        T Bend(T start, T end, T bendStart, T bendEnd, T bendSample, float t);
        T Add(T a, T b);
        T SmoothDamp(T current, T target, ref T currentVelocity, float smoothTime, float maxSpeed, float deltaTime);
        T SmoothStep(T current, T target, float smoothTime, float deltaTime);
        T MoveTowards(T current, T target, float maxDelta);

        T Zero { get; }
        T Subtract(T a, T b);
        T Multiply(T a, float scalar);
        float LengthSq(T a);
        float Length(T a);
        float Dot(T a, T b);
    }

    public struct FloatMath : ICurveMath<float>
    {
        public float Zero => 0f;
        public float Add(float a, float b) => a + b;
        public float Subtract(float a, float b) => a - b;
        public float Multiply(float a, float scalar) => a * scalar;
        public float LengthSq(float a) => a * a;
        public float Length(float a) => math.abs(a);
        public float Dot(float a, float b) => a * b;
        public float Lerp(float a, float b, float t) => math.lerp(a, b, t);
        public float GetDistance(float a, float b) => math.abs(a - b);

        public float EvaluateSpline(SplineType type, float p0, float p1, float p2, float p3, float t) =>
            CurveMathUtility.EvaluateSpline<float, FloatMath>(type, p0, p1, p2, p3, t);

        public float Bend(float start, float end, float bendStart, float bendEnd, float bendSample, float t)
        {
            float splineForward = bendEnd - bendStart;
            if (math.abs(splineForward) <= 1e-5f) return Lerp(start, end, t);

            float baseValue = Lerp(start, end, t);
            float bendLine = math.lerp(bendStart, bendEnd, t);
            float scale = (end - start) / splineForward;
            return baseValue + (bendSample - bendLine) * scale;
        }

        public float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed,
            float deltaTime) =>
            CurveMathUtility.SmoothDamp<float, FloatMath>(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);

        public float SmoothStep(float current, float target, float smoothTime, float deltaTime) =>
            CurveMathUtility.SmoothStep<float, FloatMath>(current, target, smoothTime, deltaTime);

        public float MoveTowards(float current, float target, float maxDelta) =>
            CurveMathUtility.MoveTowards<float, FloatMath>(current, target, maxDelta);
    }

    public struct Float2Math : ICurveMath<float2>
    {
        public float2 Zero => float2.zero;
        public float2 Add(float2 a, float2 b) => a + b;
        public float2 Subtract(float2 a, float2 b) => a - b;
        public float2 Multiply(float2 a, float scalar) => a * scalar;
        public float LengthSq(float2 a) => math.lengthsq(a);
        public float Length(float2 a) => math.length(a);
        public float Dot(float2 a, float2 b) => math.dot(a, b);
        public float2 Lerp(float2 a, float2 b, float t) => math.lerp(a, b, t);
        public float GetDistance(float2 a, float2 b) => math.distance(a, b);

        public float2 EvaluateSpline(SplineType type, float2 p0, float2 p1, float2 p2, float2 p3, float t) =>
            CurveMathUtility.EvaluateSpline<float2, Float2Math>(type, p0, p1, p2, p3, t);

        public float2 Bend(float2 start, float2 end, float2 bendStart, float2 bendEnd, float2 bendSample, float t)
        {
            float2 defaultForward = end - start;
            float2 splineForward = bendEnd - bendStart;
            float defaultLength = math.length(defaultForward);
            float splineLength = math.length(splineForward);
            float2 baseValue = math.lerp(start, end, t);
            if (defaultLength <= 1e-5f || splineLength <= 1e-5f) return baseValue;

            float2 defaultDir = defaultForward / defaultLength;
            float2 splineDir = splineForward / splineLength;
            float cos = math.dot(splineDir, defaultDir);
            float sin = splineDir.x * defaultDir.y - splineDir.y * defaultDir.x;
            float2 offset = bendSample - math.lerp(bendStart, bendEnd, t);
            float2 rotatedOffset = new float2(
                offset.x * cos - offset.y * sin,
                offset.x * sin + offset.y * cos);

            return baseValue + rotatedOffset * (defaultLength / splineLength);
        }

        public float2 SmoothDamp(float2 current, float2 target, ref float2 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime) =>
            CurveMathUtility.SmoothDamp<float2, Float2Math>(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);

        public float2 SmoothStep(float2 current, float2 target, float smoothTime, float deltaTime) =>
            CurveMathUtility.SmoothStep<float2, Float2Math>(current, target, smoothTime, deltaTime);

        public float2 MoveTowards(float2 current, float2 target, float maxDelta) =>
            CurveMathUtility.MoveTowards<float2, Float2Math>(current, target, maxDelta);
    }

    public struct Float3Math : ICurveMath<float3>
    {
        public float3 Zero => float3.zero;
        public float3 Add(float3 a, float3 b) => a + b;
        public float3 Subtract(float3 a, float3 b) => a - b;
        public float3 Multiply(float3 a, float scalar) => a * scalar;
        public float LengthSq(float3 a) => math.lengthsq(a);
        public float Length(float3 a) => math.length(a);
        public float Dot(float3 a, float3 b) => math.dot(a, b);
        public float3 Lerp(float3 a, float3 b, float t) => math.lerp(a, b, t);
        public float GetDistance(float3 a, float3 b) => math.distance(a, b);

        public float3 EvaluateSpline(SplineType type, float3 p0, float3 p1, float3 p2, float3 p3, float t) =>
            CurveMathUtility.EvaluateSpline<float3, Float3Math>(type, p0, p1, p2, p3, t);

        public float3 Bend(float3 start, float3 end, float3 bendStart, float3 bendEnd, float3 bendSample, float t)
        {
            float3 defaultForward = end - start;
            float3 splineForward = bendEnd - bendStart;
            float defaultLength = math.length(defaultForward);
            float splineLength = math.length(splineForward);
            float3 baseValue = math.lerp(start, end, t);
            if (defaultLength <= 1e-5f || splineLength <= 1e-5f) return baseValue;

            quaternion rotation = CurveMathUtility.FromToRotation(splineForward / splineLength, defaultForward / defaultLength);
            float3 offset = bendSample - math.lerp(bendStart, bendEnd, t);
            return baseValue + math.rotate(rotation, offset) * (defaultLength / splineLength);
        }

        public float3 SmoothDamp(float3 current, float3 target, ref float3 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime) =>
            CurveMathUtility.SmoothDamp<float3, Float3Math>(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);

        public float3 SmoothStep(float3 current, float3 target, float smoothTime, float deltaTime) =>
            CurveMathUtility.SmoothStep<float3, Float3Math>(current, target, smoothTime, deltaTime);

        public float3 MoveTowards(float3 current, float3 target, float maxDelta) =>
            CurveMathUtility.MoveTowards<float3, Float3Math>(current, target, maxDelta);
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

        public quaternion Bend(quaternion start, quaternion end, quaternion bendStart, quaternion bendEnd, quaternion bendSample, float t)
        {
            quaternion baseRotation = math.slerp(start, end, t);
            quaternion bendLine = math.slerp(bendStart, bendEnd, t);
            quaternion delta = math.mul(bendSample, math.inverse(bendLine));
            return math.normalize(math.mul(delta, baseRotation));
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

        public quaternion SmoothStep(quaternion current, quaternion target, float smoothTime, float deltaTime)
        {
            float angularDot = math.abs(math.dot(target, current));
            return angularDot < 0.99999f
                ? math.slerp(current, target, CurveMathUtility.GetSmoothStep(smoothTime, deltaTime))
                : target;
        }

        public quaternion MoveTowards(quaternion current, quaternion target, float maxDelta)
        {
            float dot = math.dot(current, target);
            if (dot < 0f)
            {
                target.value = -target.value;
                dot = -dot;
            }
            float angle = math.acos(math.clamp(dot, -1f, 1f)) * 2f;
            if (angle <= maxDelta || angle < math.EPSILON)
            {
                return target;
            }
            float t = maxDelta / angle;
            return math.slerp(current, target, t);
        }

        public quaternion Zero => new quaternion(0f, 0f, 0f, 0f);
        public quaternion Subtract(quaternion a, quaternion b) => new quaternion(a.value - b.value);
        public quaternion Multiply(quaternion a, float scalar) => new quaternion(a.value * scalar);
        public float LengthSq(quaternion a) => math.lengthsq(a.value);
        public float Length(quaternion a) => math.length(a.value);
        public float Dot(quaternion a, quaternion b) => math.dot(a.value, b.value);
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

        public static T EvaluateSpline<T, TMath>(SplineType type, T p0, T p1, T p2, T p3, float t, TMath math = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            switch (type)
            {
                case SplineType.Linear: return math.Lerp(p0, p1, t);
                case SplineType.Step: return t >= 1f ? p1 : p0;
                case SplineType.CubicBezier:
                    float u = 1f - t; float tt = t * t; float uu = u * u;
                    T term1 = math.Multiply(p0, uu * u);
                    T term2 = math.Multiply(p1, 3f * uu * t);
                    T term3 = math.Multiply(p2, 3f * u * tt);
                    T term4 = math.Multiply(p3, tt * t);
                    return math.Add(math.Add(math.Add(term1, term2), term3), term4);
                case SplineType.CatmullRom:
                    float t2 = t * t; float t3 = t2 * t;
                    T crTerm1 = math.Multiply(p1, 2f);
                    T crTerm2 = math.Multiply(math.Subtract(p2, p0), t);
                    T crTerm3 = math.Multiply(math.Subtract(math.Add(math.Multiply(p0, 2f), math.Multiply(p2, 4f)), math.Add(math.Multiply(p1, 5f), p3)), t2);
                    T crTerm4 = math.Multiply(math.Add(math.Subtract(math.Multiply(p1, 3f), p0), math.Subtract(p3, math.Multiply(p2, 3f))), t3);
                    return math.Multiply(math.Add(math.Add(math.Add(crTerm1, crTerm2), crTerm3), crTerm4), 0.5f);
                case SplineType.BSpline:
                    float bt2 = t * t; float bt3 = bt2 * t;
                    T bsTerm1 = math.Multiply(math.Add(math.Subtract(math.Multiply(p1, 3f), p0), math.Subtract(p3, math.Multiply(p2, 3f))), bt3);
                    T bsTerm2 = math.Multiply(math.Subtract(math.Add(math.Multiply(p0, 3f), math.Multiply(p2, 3f)), math.Multiply(p1, 6f)), bt2);
                    T bsTerm3 = math.Multiply(math.Subtract(math.Multiply(p2, 3f), math.Multiply(p0, 3f)), t);
                    T bsTerm4 = math.Add(math.Add(p0, math.Multiply(p1, 4f)), p2);
                    return math.Multiply(math.Add(math.Add(math.Add(bsTerm1, bsTerm2), bsTerm3), bsTerm4), 1f / 6f);
                default: return p0;
            }
        }

        public static quaternion FromToRotation(float3 from, float3 to)
        {
            float d = math.clamp(math.dot(from, to), -1f, 1f);
            if (d > 0.99999f) return quaternion.identity;
            if (d < -0.99999f)
            {
                float3 axis = math.abs(from.x) < 0.9f
                    ? math.normalize(math.cross(from, new float3(1f, 0f, 0f)))
                    : math.normalize(math.cross(from, new float3(0f, 1f, 0f)));
                return quaternion.AxisAngle(axis, math.PI);
            }

            float3 c = math.cross(from, to);
            float s = math.sqrt((1f + d) * 2f);
            float invS = 1f / s;
            return math.normalize(new quaternion(c.x * invS, c.y * invS, c.z * invS, s * 0.5f));
        }

        public static T SmoothDamp<T, TMath>(T current, T target, ref T currentVelocity, float smoothTime, float maxSpeed, float deltaTime, TMath math = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            smoothTime = Unity.Mathematics.math.max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            T change = math.Subtract(current, target);
            T originalTo = target;

            float maxChange = maxSpeed * smoothTime;
            float sqrmag = math.LengthSq(change);
            if (sqrmag > maxChange * maxChange)
            {
                float mag = math.Length(change);
                change = math.Multiply(change, maxChange / mag);
            }

            target = math.Subtract(current, change);
            T temp = math.Multiply(math.Add(currentVelocity, math.Multiply(change, omega)), deltaTime);
            currentVelocity = math.Multiply(math.Subtract(currentVelocity, math.Multiply(temp, omega)), exp);
            T output = math.Add(target, math.Multiply(math.Add(change, temp), exp));

            if (math.Dot(math.Subtract(originalTo, current), math.Subtract(output, originalTo)) > 0)
            {
                output = originalTo;
                currentVelocity = math.Multiply(math.Subtract(output, originalTo), 1f / deltaTime);
            }

            return output;
        }

        public static T SmoothStep<T, TMath>(T current, T target, float smoothTime, float deltaTime, TMath math = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            float step = GetSmoothStep(smoothTime, deltaTime);
            T diff = math.Subtract(target, current);
            return math.LengthSq(diff) > Unity.Mathematics.math.EPSILON ? math.Lerp(current, target, step) : target;
        }

        public static T MoveTowards<T, TMath>(T current, T target, float maxDelta, TMath math = default)
            where T : unmanaged
            where TMath : struct, ICurveMath<T>
        {
            T diff = math.Subtract(target, current);
            float dist = math.Length(diff);
            if (dist <= maxDelta || dist < Unity.Mathematics.math.EPSILON)
            {
                return target;
            }
            return math.Add(current, math.Multiply(diff, maxDelta / dist));
        }
    }
}
