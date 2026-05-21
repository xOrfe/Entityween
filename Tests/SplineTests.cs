using NUnit.Framework;
using Unity.Mathematics;
using XO.Curve;
using XO.Entityween;

namespace XO.Curve.Tests
{
    [TestFixture]
    public class SplineTests
    {
        [Test]
        public void SplineUtility_AutoTangent_CalculatesReasonableTangents()
        {

            var splineData = new SerializableSpline<float3>
            {
                splineType = SplineType.CubicBezier,
                isClosed = false,
                points = new float3[]
                {
                    new float3(0f, 0f, 0f),
                    new float3(2f, 0f, 0f),
                    new float3(4f, 0f, 0f)
                }
            };

            splineData.InitializeOrResizeTangents();

            Assert.AreEqual(6, splineData.tangents.Length);

            splineData.AutoCalculateTangents(0);
            splineData.AutoCalculateTangents(1);
            splineData.AutoCalculateTangents(2);

            Assert.AreNotEqual(float3.zero, splineData.tangents[0]);
            Assert.AreNotEqual(float3.zero, splineData.tangents[1]);
            Assert.AreNotEqual(float3.zero, splineData.tangents[2]);
            Assert.AreNotEqual(float3.zero, splineData.tangents[3]);
            Assert.AreNotEqual(float3.zero, splineData.tangents[4]);
            Assert.AreNotEqual(float3.zero, splineData.tangents[5]);
        }

        [Test]
        public void SplineUtility_GetFlatPointsArray_ConstructsCorrectBezierLayout()
        {
            var points = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(2f, 0f, 0f),
                new float3(4f, 0f, 0f)
            };

            var tangents = new float3[]
            {
                new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f),
                new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f),
                new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f)
            };

            var flat = SplineUtility.GetFlatPointsArray(SplineType.CubicBezier, false, points, tangents, default(Float3Math));

            Assert.AreEqual(7, flat.Length);

            Assert.AreEqual(points[0], flat[0]);

            Assert.AreEqual(points[0] + tangents[1], flat[1]);

            Assert.AreEqual(points[1] + tangents[2], flat[2]);

            Assert.AreEqual(points[1], flat[3]);

            Assert.AreEqual(points[1] + tangents[3], flat[4]);

            Assert.AreEqual(points[2] + tangents[4], flat[5]);

            Assert.AreEqual(points[2], flat[6]);
        }

        [Test]
        public void SplineUtility_GetFlatPointsArray_CatmullRom_PadsEnds()
        {
            var points = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(2f, 0f, 0f),
                new float3(4f, 0f, 0f),
                new float3(6f, 0f, 0f)
            };

            var tangents = new float3[]
            {
                new float3(-1f, 0f, 0f),
                new float3(1f, 0f, 0f)
            };

            var flat = SplineUtility.GetFlatPointsArray(SplineType.CatmullRom, false, points, tangents, default(Float3Math));

            Assert.AreEqual(6, flat.Length);
            Assert.AreEqual(points[0] + tangents[0], flat[0]);
            Assert.AreEqual(points[0], flat[1]);
            Assert.AreEqual(points[1], flat[2]);
            Assert.AreEqual(points[2], flat[3]);
            Assert.AreEqual(points[3], flat[4]);
            Assert.AreEqual(points[3] + tangents[1], flat[5]);
        }

        [Test]
        public void Spline_CatmullRomFlattenedPath_SamplesOriginalEndpoints()
        {
            var points = new float3[]
            {
                new float3(0f, 0f, 0f),
                new float3(2f, 5f, 0f),
                new float3(5f, 5f, 0f),
                new float3(7f, 0f, 0f)
            };

            float3[] tangents = null;
            SplineUtility.InitializeOrResizeTangents(SplineType.CatmullRom, false, points, ref tangents);
            var flat = SplineUtility.GetFlatPointsArray<float3, Float3Math>(SplineType.CatmullRom, false, points, tangents);
            var adapter = new Spline.EditorSplineAdapter(new System.Collections.Generic.List<float3>(flat), SplineType.CatmullRom, false);

            Assert.AreEqual(points[0], Spline.SampleGeneric<float3, Spline.EditorSplineAdapter, Float3Math>(ref adapter, 0f));
            Assert.AreEqual(points[points.Length - 1], Spline.SampleGeneric<float3, Spline.EditorSplineAdapter, Float3Math>(ref adapter, 1f));
        }

        [Test]
        public void Ease_BackAndElasticPreserveOvershoot()
        {
            Assert.AreEqual(0f, Ease.EasedT(0f, EaseType.OutBack), 0.0001f);
            Assert.Greater(Ease.EasedT(0.7f, EaseType.OutBack), 1f);
            Assert.Less(Ease.EasedT(0.25f, EaseType.InBack), 0f);
            Assert.Greater(Ease.EasedT(0.2f, EaseType.OutElastic), 1f);
        }

        [Test]
        public void SerializableTransformSpline_Validate_SplitsControlPointsIntoEnabledSplines()
        {
            var transformSpline = new SerializableTransformSpline
            {
                splineType = SplineType.Linear,
                isClosed = false,
                controlPoints = new[]
                {
                    new SerializableTransformSplinePoint(new float3(1f, 2f, 3f), new float3(0f, 90f, 0f), new float3(1f, 2f, 3f)),
                    new SerializableTransformSplinePoint(new float3(4f, 5f, 6f), new float3(0f, 180f, 0f), new float3(2f, 3f, 4f))
                }
            };

            transformSpline.Validate();

            Assert.AreEqual(2, transformSpline.positional.points.Length);
            Assert.AreEqual(new float3(1f, 2f, 3f), transformSpline.positional.points[0]);
            Assert.AreEqual(new float3(0f, 90f, 0f), transformSpline.rotational.points[0]);
            Assert.AreEqual(new float3(1f, 2f, 3f), transformSpline.scaling.points[0]);

            var rotationSpline = transformSpline.GetRotationSpline();
            quaternion expected = quaternion.EulerXYZ(math.radians(new float3(0f, 90f, 0f)));
            Assert.Less(math.distance(rotationSpline.points[0].value, expected.value), 0.0001f);
        }
    }
}
