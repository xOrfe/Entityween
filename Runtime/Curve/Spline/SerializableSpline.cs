using System;

namespace XO.Curve
{
    [Serializable]
    public class SerializableSpline<T> where T : unmanaged
    {
        public SplineType splineType = SplineType.CubicBezier;

        public bool isClosed = false;

        public T[] points;

        public bool autoTangent = true;

        public T[] tangents;

#if UNITY_EDITOR
        [NonSerialized] public bool ShowInSceneView = true;
        [UnityEngine.SerializeField, UnityEngine.HideInInspector] public int selectedPointIndex = -1;
#endif

        public void ValidatePoints()
        {
            if (splineType == SplineType.CatmullRom || splineType == SplineType.BSpline)
            {
                autoTangent = true;
            }
            InitializeOrResizeTangents();
            if (autoTangent)
            {
                RecalculateAllTangents();
            }
        }

        public void InitializeOrResizeTangents()
        {
            if (splineType == SplineType.CatmullRom || splineType == SplineType.BSpline)
            {
                autoTangent = true;
            }
            SplineUtility.InitializeOrResizeTangents(splineType, isClosed, points, ref tangents, autoTangent);
        }

        public void RecalculateAllTangents()
        {
            SplineUtility.RecalculateAllTangents(splineType, isClosed, points, tangents);
        }

        public void AutoCalculateTangents(int i)
        {
            SplineUtility.CalculateDefaultTangents(splineType, isClosed, points, tangents, i);
        }
    }
}
