using System;
using Unity.Mathematics;

namespace XO.Curve
{
    [Serializable]
    public struct SerializableTransformSplinePoint
    {
        public float3 position;
        public float3 rotation;
        public float3 scale;

        public SerializableTransformSplinePoint(float3 position, float3 rotation, float3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    [Serializable]
    public class SerializableTransformSpline
    {
        public bool usePosition = true;
        public bool useRotation = true;
        public bool useScale = true;

        public SplineType splineType = SplineType.CubicBezier;
        public bool isClosed = false;

        public SerializableTransformSplinePoint[] controlPoints;

        public SerializableSpline<float3> positional = new SerializableSpline<float3>();
        public SerializableSpline<float3> rotational = new SerializableSpline<float3>();
        public SerializableSpline<float3> scaling = new SerializableSpline<float3>();

#if UNITY_EDITOR
        [NonSerialized] public bool showInternalSplines = false;
#endif

        public void Validate()
        {
            EnsureSplines();
            PullControlPointsFromSplinesIfNeeded();
            ApplySettingsToEnabledSplines();
            ApplyControlPointsToSplines();
            ValidateEnabledSplines();
        }

        public SerializableSpline<float3> GetPositionSpline()
        {
            Validate();
            return usePosition ? positional : null;
        }

        public SerializableSpline<quaternion> GetRotationSpline()
        {
            Validate();
            return useRotation ? CreateQuaternionSpline(rotational) : null;
        }

        public SerializableSpline<float3> GetScaleSpline()
        {
            Validate();
            return useScale ? scaling : null;
        }

        public void ApplyControlPointsToSplines()
        {
            EnsureSplines();

            int count = controlPoints?.Length ?? 0;
            if (usePosition)
            {
                positional.points = new float3[count];
            }
            if (useRotation)
            {
                rotational.points = new float3[count];
            }
            if (useScale)
            {
                scaling.points = new float3[count];
            }

            for (int i = 0; i < count; i++)
            {
                var point = controlPoints[i];
                if (usePosition) positional.points[i] = point.position;
                if (useRotation) rotational.points[i] = point.rotation;
                if (useScale) scaling.points[i] = point.scale;
            }
        }

        public void PullControlPointsFromSplinesIfNeeded()
        {
            EnsureSplines();
            if (controlPoints != null) return;

            int count = math.max(positional.points?.Length ?? 0, rotational.points?.Length ?? 0);
            count = math.max(count, scaling.points?.Length ?? 0);
            controlPoints = new SerializableTransformSplinePoint[count];

            for (int i = 0; i < count; i++)
            {
                controlPoints[i] = new SerializableTransformSplinePoint(
                    ReadPoint(positional, i, float3.zero),
                    ReadPoint(rotational, i, float3.zero),
                    ReadPoint(scaling, i, new float3(1f, 1f, 1f))
                );
            }
        }

        public SerializableSpline<quaternion> CreateQuaternionSpline(SerializableSpline<float3> eulerSpline)
        {
            if (eulerSpline == null) return null;

            eulerSpline.InitializeOrResizeTangents();

            var result = new SerializableSpline<quaternion>
            {
                splineType = eulerSpline.splineType,
                isClosed = eulerSpline.isClosed,
                autoTangent = false,
                points = ConvertEulerArray(eulerSpline.points)
            };

            result.tangents = ConvertEulerTangents(eulerSpline);
            return result;
        }

        private void EnsureSplines()
        {
            positional ??= new SerializableSpline<float3>();
            rotational ??= new SerializableSpline<float3>();
            scaling ??= new SerializableSpline<float3>();
        }

        private void ApplySettingsToEnabledSplines()
        {
            if (usePosition) ApplySettings(positional);
            if (useRotation) ApplySettings(rotational);
            if (useScale) ApplySettings(scaling);
        }

        private void ValidateEnabledSplines()
        {
            if (usePosition) positional.ValidatePoints();
            if (useRotation) rotational.ValidatePoints();
            if (useScale) scaling.ValidatePoints();
        }

        private void ApplySettings(SerializableSpline<float3> spline)
        {
            spline.splineType = splineType;
            spline.isClosed = isClosed;
        }

        private static float3 ReadPoint(SerializableSpline<float3> spline, int index, float3 fallback)
        {
            if (spline?.points == null || index < 0 || index >= spline.points.Length) return fallback;
            return spline.points[index];
        }

        private static quaternion[] ConvertEulerArray(float3[] points)
        {
            if (points == null) return null;

            var result = new quaternion[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                result[i] = EulerToQuaternion(points[i]);
            }
            return result;
        }

        private static quaternion[] ConvertEulerTangents(SerializableSpline<float3> eulerSpline)
        {
            if (eulerSpline.tangents == null || eulerSpline.tangents.Length == 0 || eulerSpline.points == null) return null;

            var result = new quaternion[eulerSpline.tangents.Length];
            if (eulerSpline.splineType == SplineType.CubicBezier)
            {
                for (int i = 0; i < eulerSpline.points.Length; i++)
                {
                    int tangentIndex = i * 2;
                    if (tangentIndex + 1 >= eulerSpline.tangents.Length) break;

                    result[tangentIndex] = EulerToQuaternion(eulerSpline.points[i] + eulerSpline.tangents[tangentIndex]);
                    result[tangentIndex + 1] = EulerToQuaternion(eulerSpline.points[i] + eulerSpline.tangents[tangentIndex + 1]);
                }
                return result;
            }

            if ((eulerSpline.splineType == SplineType.CatmullRom || eulerSpline.splineType == SplineType.BSpline) && !eulerSpline.isClosed)
            {
                if (eulerSpline.points.Length == 0 || eulerSpline.tangents.Length < 2) return result;

                result[0] = EulerToQuaternion(eulerSpline.points[0] + eulerSpline.tangents[0]);
                result[1] = EulerToQuaternion(eulerSpline.points[eulerSpline.points.Length - 1] + eulerSpline.tangents[1]);
                return result;
            }

            for (int i = 0; i < eulerSpline.tangents.Length; i++)
            {
                result[i] = EulerToQuaternion(eulerSpline.tangents[i]);
            }
            return result;
        }

        private static quaternion EulerToQuaternion(float3 eulerDegrees)
        {
            return quaternion.EulerXYZ(math.radians(eulerDegrees));
        }
    }
}
