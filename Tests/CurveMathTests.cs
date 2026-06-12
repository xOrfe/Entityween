using NUnit.Framework;
using Unity.Mathematics;
using XO.Curve;

namespace XO.Curve.Tests
{
    [TestFixture]
    public class CurveMathTests
    {
        [Test]
        public void FloatMath_SmoothStep_InterpolatesCorrectly()
        {
            var floatMath = default(FloatMath);
            float current = 0f;
            float target = 10f;
            float smoothTime = 0.5f;
            float deltaTime = 0.1f;

            float next = floatMath.SmoothStep(current, target, smoothTime, deltaTime);
            Assert.Greater(next, current);
            Assert.Less(next, target);

            float closeToTarget = floatMath.SmoothStep(current, target, smoothTime, 10f);
            Assert.AreEqual(target, closeToTarget, 0.001f);
        }

        [Test]
        public void Float3Math_SmoothStep_InterpolatesCorrectly()
        {
            var float3Math = default(Float3Math);
            float3 current = new float3(0f, 0f, 0f);
            float3 target = new float3(10f, 20f, -5f);
            float smoothTime = 0.5f;
            float deltaTime = 0.1f;

            float3 next = float3Math.SmoothStep(current, target, smoothTime, deltaTime);
            Assert.IsTrue(math.all(next > current == target > current));
            Assert.IsTrue(math.all(math.abs(next - target) < math.abs(current - target)));

            float3 closeToTarget = float3Math.SmoothStep(current, target, smoothTime, 10f);
            Assert.IsTrue(math.distance(closeToTarget, target) < 0.001f);
        }

        [Test]
        public void FloatMath_MoveTowards_ClampsToMaxDelta()
        {
            var floatMath = default(FloatMath);
            float current = 1f;
            float target = 5f;
            float maxDelta = 2f;

            float result1 = floatMath.MoveTowards(current, target, maxDelta);
            Assert.AreEqual(3f, result1);

            float result2 = floatMath.MoveTowards(current, target, 10f);
            Assert.AreEqual(5f, result2);
        }

        [Test]
        public void Float3Math_MoveTowards_ClampsToMaxDelta()
        {
            var float3Math = default(Float3Math);
            float3 current = new float3(0f, 0f, 0f);
            float3 target = new float3(0f, 10f, 0f);
            float maxDelta = 3f;

            float3 result1 = float3Math.MoveTowards(current, target, maxDelta);
            Assert.AreEqual(new float3(0f, 3f, 0f), result1);

            float3 result2 = float3Math.MoveTowards(current, target, 20f);
            Assert.AreEqual(target, result2);
        }

        [Test]
        public void QuaternionMath_SmoothStep_InterpolatesCorrectly()
        {
            var quaternionMath = default(QuaternionMath);
            quaternion current = quaternion.identity;
            quaternion target = quaternion.EulerXYZ(0f, math.radians(90f), 0f);
            float smoothTime = 0.5f;
            float deltaTime = 0.1f;

            quaternion result = quaternionMath.SmoothStep(current, target, smoothTime, deltaTime);
            float currentDot = math.abs(math.dot(current.value, target.value));
            float resultDot = math.abs(math.dot(result.value, target.value));

            Assert.Greater(resultDot, currentDot);

            quaternion closeToTarget = quaternionMath.SmoothStep(current, target, smoothTime, 10f);
            Assert.GreaterOrEqual(math.abs(math.dot(closeToTarget.value, target.value)), 0.9999f);
        }

        [Test]
        public void QuaternionMath_SmoothStep_DoubleCover_TakesShortestPath()
        {
            var quaternionMath = default(QuaternionMath);
            quaternion current = quaternion.identity;
            quaternion target = quaternion.EulerXYZ(0f, math.radians(90f), 0f);
            quaternion negativeTarget = target;
            negativeTarget.value = -negativeTarget.value;

            quaternion result = quaternionMath.SmoothStep(current, negativeTarget, 0.5f, 0.1f);
            float currentDot = math.abs(math.dot(current.value, target.value));
            float resultDot = math.abs(math.dot(result.value, target.value));

            Assert.Greater(resultDot, currentDot);

            quaternion closeToTarget = quaternionMath.SmoothStep(current, negativeTarget, 0.5f, 10f);
            Assert.GreaterOrEqual(math.abs(math.dot(closeToTarget.value, target.value)), 0.9999f);
        }

        [Test]
        public void QuaternionMath_SmoothDamp_DoubleCover_ConvergesWithoutOscillations()
        {
            var quaternionMath = default(QuaternionMath);
            quaternion current = quaternion.identity;
            quaternion target = quaternion.EulerXYZ(0f, math.radians(90f), 0f);
            quaternion negativeTarget = target;
            negativeTarget.value = -negativeTarget.value;

            quaternion velocity = new quaternion(0f, 0f, 0f, 0f);
            float smoothTime = 0.5f;
            float maxSpeed = float.PositiveInfinity;

            for (int i = 0; i < 50; i++)
            {
                quaternion t = (i % 2 == 0) ? target : negativeTarget;
                current = quaternionMath.SmoothDamp(current, t, ref velocity, smoothTime, maxSpeed, 0.02f);
            }

            Assert.GreaterOrEqual(math.abs(math.dot(current.value, target.value)), 0.99f);
        }

        [Test]
        public void QuaternionMath_MoveTowards_ClampsToMaxDelta()
        {
            var quaternionMath = default(QuaternionMath);
            quaternion current = quaternion.identity;
            quaternion target = quaternion.EulerXYZ(0f, math.radians(90f), 0f);

            float maxDelta = math.radians(30f);
            quaternion result1 = quaternionMath.MoveTowards(current, target, maxDelta);

            float angleResult = math.degrees(math.acos(math.clamp(math.abs(math.dot(current.value, result1.value)), -1f, 1f)) * 2f);
            Assert.AreEqual(30f, angleResult, 0.01f);

            quaternion result2 = quaternionMath.MoveTowards(current, target, math.radians(120f));
            Assert.AreEqual(target.value, result2.value);
        }
    }
}
