using UnityEditor;
using UnityEngine;

namespace XO.Curve.Editor
{
    public static class SerializedPropertyExtensions
    {
        public static Vector3 GetVector3Val(this SerializedProperty element)
        {
            if (element == null) return Vector3.zero;

            var xProp = element.FindPropertyRelative("x");
            var yProp = element.FindPropertyRelative("y");
            var zProp = element.FindPropertyRelative("z");

            if (xProp != null && yProp != null)
            {
                float x = xProp.floatValue;
                float y = yProp.floatValue;
                float z = zProp != null ? zProp.floatValue : 0f;
                return new Vector3(x, y, z);
            }

            if (element.propertyType == SerializedPropertyType.Float)
            {
                return new Vector3(element.floatValue, 0f, 0f);
            }

            try
            {
                return element.vector3Value;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        public static void SetVector3Val(this SerializedProperty element, Vector3 value)
        {
            if (element == null) return;

            var xProp = element.FindPropertyRelative("x");
            var yProp = element.FindPropertyRelative("y");
            var zProp = element.FindPropertyRelative("z");

            if (xProp != null && yProp != null)
            {
                xProp.floatValue = value.x;
                yProp.floatValue = value.y;
                if (zProp != null)
                {
                    zProp.floatValue = value.z;
                }
                return;
            }

            if (element.propertyType == SerializedPropertyType.Float)
            {
                element.floatValue = value.x;
                return;
            }

            try
            {
                element.vector3Value = value;
            }
            catch
            {

            }
        }
    }
}
