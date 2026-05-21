using UnityEditor;
using UnityEngine;
using XO.Curve;

namespace XO.Curve.Editor
{
    [CustomPropertyDrawer(typeof(SerializableTransformSpline))]
    public class TransformSplineDataDrawer : PropertyDrawer
    {
        private const float Spacing = 4f;
        private const float Padding = 8f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var usePositionProp = property.FindPropertyRelative("usePosition");
            var useRotationProp = property.FindPropertyRelative("useRotation");
            var useScaleProp = property.FindPropertyRelative("useScale");
            var typeProp = property.FindPropertyRelative("splineType");
            var closedProp = property.FindPropertyRelative("isClosed");
            var pointsProp = property.FindPropertyRelative("controlPoints");
            var positionalProp = property.FindPropertyRelative("positional");
            var rotationalProp = property.FindPropertyRelative("rotational");
            var scalingProp = property.FindPropertyRelative("scaling");

            PullControlPointsFromSplinesIfEmpty(pointsProp, positionalProp, rotationalProp, scalingProp);
            SyncSplineSettings(typeProp, closedProp, positionalProp, rotationalProp, scalingProp);
            SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePositionProp.boolValue, useRotationProp.boolValue, useScaleProp.boolValue);

            Rect bgRect = new Rect(position.x - 4, position.y + 2, position.width + 8, position.height - 4);
            GUI.Box(bgRect, GUIContent.none, EditorStyles.helpBox);

            float contentX = position.x + Padding;
            float contentWidth = position.width - Padding * 2f;
            float y = position.y + 6f;

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.95f, 0.6f, 0.1f, 1f);
            GUI.Label(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), "✨ Entityween Transform Spline", headerStyle);
            y += EditorGUIUtility.singleLineHeight + 6f;

            property.isExpanded = EditorGUI.Foldout(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), property.isExpanded, "Show Settings & Transform Points", true);
            y += EditorGUIUtility.singleLineHeight + 6f;

            if (property.isExpanded)
            {
                EditorGUI.BeginChangeCheck();
                DrawAxisToggles(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), usePositionProp, useRotationProp, useScaleProp);
                if (EditorGUI.EndChangeCheck())
                {
                    SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePositionProp.boolValue, useRotationProp.boolValue, useScaleProp.boolValue);
                }
                y += EditorGUIUtility.singleLineHeight + Spacing;

                EditorGUI.PropertyField(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), typeProp, new GUIContent("Spline Type"));
                y += EditorGUIUtility.singleLineHeight + Spacing;

                EditorGUI.PropertyField(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), closedProp, new GUIContent("Closed Loop"));
                y += EditorGUIUtility.singleLineHeight + 6f;

                SyncSplineSettings(typeProp, closedProp, positionalProp, rotationalProp, scalingProp);

                DrawButtons(ref y, contentX, contentWidth, property, pointsProp, positionalProp, rotationalProp, scalingProp, usePositionProp.boolValue, useRotationProp.boolValue, useScaleProp.boolValue);

                EditorGUI.LabelField(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), "Control Points List", EditorStyles.boldLabel);
                y += EditorGUIUtility.singleLineHeight + Spacing;

                DrawPointSizeField(ref y, contentX, contentWidth, property, pointsProp, positionalProp, rotationalProp, scalingProp, usePositionProp.boolValue, useRotationProp.boolValue, useScaleProp.boolValue);
                y += Spacing;

                DrawControlPoints(ref y, contentX, contentWidth, property, pointsProp, positionalProp, rotationalProp, scalingProp, usePositionProp.boolValue, useRotationProp.boolValue, useScaleProp.boolValue);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 12f;
            height += EditorGUIUtility.singleLineHeight + 6f;

            if (!property.isExpanded) return height;

            var usePositionProp = property.FindPropertyRelative("usePosition");
            var useRotationProp = property.FindPropertyRelative("useRotation");
            var useScaleProp = property.FindPropertyRelative("useScale");
            var pointsProp = property.FindPropertyRelative("controlPoints");

            height += EditorGUIUtility.singleLineHeight + Spacing;
            height += EditorGUIUtility.singleLineHeight + Spacing;
            height += EditorGUIUtility.singleLineHeight + 6f;
            height += EditorGUIUtility.singleLineHeight + 12f;
            height += EditorGUIUtility.singleLineHeight + 10f;
            height += EditorGUIUtility.singleLineHeight + Spacing;
            height += EditorGUIUtility.singleLineHeight + Spacing;

            int activeCount = GetActiveFieldCount(usePositionProp.boolValue, useRotationProp.boolValue, useScaleProp.boolValue);
            int pointCount = pointsProp?.arraySize ?? 0;
            for (int i = 0; i < pointCount; i++)
            {
                height += EditorGUIUtility.singleLineHeight + Spacing;
                height += MaxInt(1, activeCount) * (EditorGUIUtility.singleLineHeight + Spacing);
                height += 2f;
            }

            return height + 8f;
        }

        private static void DrawAxisToggles(Rect rect, SerializedProperty usePositionProp, SerializedProperty useRotationProp, SerializedProperty useScaleProp)
        {
            float width = (rect.width - Spacing * 2f) / 3f;
            usePositionProp.boolValue = GUI.Toggle(new Rect(rect.x, rect.y, width, rect.height), usePositionProp.boolValue, "Position", "Button");
            useRotationProp.boolValue = GUI.Toggle(new Rect(rect.x + width + Spacing, rect.y, width, rect.height), useRotationProp.boolValue, "Rotation", "Button");
            useScaleProp.boolValue = GUI.Toggle(new Rect(rect.x + (width + Spacing) * 2f, rect.y, width, rect.height), useScaleProp.boolValue, "Scale", "Button");
        }

        private static void DrawButtons(ref float y, float contentX, float contentWidth, SerializedProperty wrapperProp, SerializedProperty pointsProp,
            SerializedProperty positionalProp, SerializedProperty rotationalProp, SerializedProperty scalingProp, bool usePosition, bool useRotation, bool useScale)
        {
            float buttonWidth = (contentWidth - 6f) / 2f;
            var primarySpline = GetPrimarySpline(positionalProp, rotationalProp, scalingProp, usePosition, useRotation, useScale);
            bool canEdit = primarySpline != null;
            bool isEditing = canEdit && SplineSceneEditorManager.IsEditingProperty(primarySpline);
            string editText = isEditing ? "🟢 Stop Scene Edit" : "✏️ Edit in Scene";

            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = isEditing ? new Color(0.2f, 0.7f, 0.3f, 1f) : new Color(0.35f, 0.35f, 0.35f, 1f);
            EditorGUI.BeginDisabledGroup(!canEdit);
            if (GUI.Button(new Rect(contentX, y, buttonWidth, EditorGUIUtility.singleLineHeight + 6f), editText))
            {
                SplineSceneEditorManager.TogglePropertyEditing(primarySpline);
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = originalBg;

            GUI.backgroundColor = new Color(0.2f, 0.5f, 0.85f, 1f);
            if (GUI.Button(new Rect(contentX + buttonWidth + 6f, y, buttonWidth, EditorGUIUtility.singleLineHeight + 6f), "➕ Add Point"))
            {
                Undo.RecordObject(wrapperProp.serializedObject.targetObject, "Add Transform Spline Point");
                AddPoint(pointsProp);
                SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePosition, useRotation, useScale);
                wrapperProp.serializedObject.ApplyModifiedProperties();
            }
            GUI.backgroundColor = originalBg;
            y += EditorGUIUtility.singleLineHeight + 12f;

            GUI.backgroundColor = new Color(0.8f, 0.25f, 0.25f, 0.8f);
            if (GUI.Button(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight + 4f), "🗑️ Clear All Points"))
            {
                if (EditorUtility.DisplayDialog("Clear Transform Spline", "Are you sure you want to clear all transform spline points?", "Yes", "No"))
                {
                    Undo.RecordObject(wrapperProp.serializedObject.targetObject, "Clear Transform Spline Points");
                    pointsProp.arraySize = 0;
                    SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePosition, useRotation, useScale);
                    wrapperProp.serializedObject.ApplyModifiedProperties();
                }
            }
            GUI.backgroundColor = originalBg;
            y += EditorGUIUtility.singleLineHeight + 10f;
        }

        private static void DrawPointSizeField(ref float y, float contentX, float contentWidth, SerializedProperty wrapperProp, SerializedProperty pointsProp,
            SerializedProperty positionalProp, SerializedProperty rotationalProp, SerializedProperty scalingProp, bool usePosition, bool useRotation, bool useScale)
        {
            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.IntField(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight), "Size", pointsProp.arraySize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(wrapperProp.serializedObject.targetObject, "Resize Transform Spline Points");
                ResizePoints(pointsProp, Mathf.Max(0, newSize));
                SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePosition, useRotation, useScale);
            }
            y += EditorGUIUtility.singleLineHeight + Spacing;
        }

        private static void DrawControlPoints(ref float y, float contentX, float contentWidth, SerializedProperty wrapperProp, SerializedProperty pointsProp,
            SerializedProperty positionalProp, SerializedProperty rotationalProp, SerializedProperty scalingProp, bool usePosition, bool useRotation, bool useScale)
        {
            int activeCount = GetActiveFieldCount(usePosition, useRotation, useScale);
            if (activeCount == 0)
            {
                EditorGUI.HelpBox(new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight * 2f), "Enable at least one channel to author transform spline data.", MessageType.Info);
                y += EditorGUIUtility.singleLineHeight * 2f + Spacing;
                return;
            }

            for (int i = 0; i < pointsProp.arraySize; i++)
            {
                var pointProp = pointsProp.GetArrayElementAtIndex(i);
                var positionProp = pointProp.FindPropertyRelative("position");
                var rotationProp = pointProp.FindPropertyRelative("rotation");
                var scaleProp = pointProp.FindPropertyRelative("scale");

                Rect headerRect = new Rect(contentX, y, contentWidth - 28f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(headerRect, $"Element {i}", EditorStyles.boldLabel);
                if (GUI.Button(new Rect(contentX + contentWidth - 24f, y, 24f, EditorGUIUtility.singleLineHeight), "-"))
                {
                    Undo.RecordObject(wrapperProp.serializedObject.targetObject, "Remove Transform Spline Point");
                    pointsProp.DeleteArrayElementAtIndex(i);
                    SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePosition, useRotation, useScale);
                    wrapperProp.serializedObject.ApplyModifiedProperties();
                    return;
                }
                y += EditorGUIUtility.singleLineHeight + Spacing;

                EditorGUI.BeginChangeCheck();
                if (usePosition)
                {
                    DrawFloat3Field(new Rect(contentX + 8f, y, contentWidth - 8f, EditorGUIUtility.singleLineHeight), "Position", positionProp);
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                }
                if (useRotation)
                {
                    DrawFloat3Field(new Rect(contentX + 8f, y, contentWidth - 8f, EditorGUIUtility.singleLineHeight), "Rotation", rotationProp);
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                }
                if (useScale)
                {
                    DrawFloat3Field(new Rect(contentX + 8f, y, contentWidth - 8f, EditorGUIUtility.singleLineHeight), "Scale", scaleProp);
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SyncControlPointsToSplines(pointsProp, positionalProp, rotationalProp, scalingProp, usePosition, useRotation, useScale);
                }
                y += 2f;
            }
        }

        private static void DrawFloat3Field(Rect rect, string label, SerializedProperty prop)
        {
            var value = prop.GetVector3Val();
            float labelWidth = 68f;
            float fieldWidth = (rect.width - labelWidth - 34f) / 3f;
            float previousLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), label);
            EditorGUIUtility.labelWidth = 14f;
            value.x = EditorGUI.FloatField(new Rect(rect.x + labelWidth, rect.y, fieldWidth, rect.height), "X", value.x);
            value.y = EditorGUI.FloatField(new Rect(rect.x + labelWidth + fieldWidth + 10f, rect.y, fieldWidth, rect.height), "Y", value.y);
            value.z = EditorGUI.FloatField(new Rect(rect.x + labelWidth + (fieldWidth + 10f) * 2f, rect.y, fieldWidth, rect.height), "Z", value.z);
            EditorGUIUtility.labelWidth = previousLabelWidth;
            prop.SetVector3Val(value);
        }

        private static void AddPoint(SerializedProperty pointsProp)
        {
            int index = pointsProp.arraySize;
            pointsProp.arraySize++;

            var newPoint = pointsProp.GetArrayElementAtIndex(index);
            if (index > 0)
            {
                var previous = pointsProp.GetArrayElementAtIndex(index - 1);
                newPoint.FindPropertyRelative("position").SetVector3Val(previous.FindPropertyRelative("position").GetVector3Val() + new Vector3(2f, 0f, 0f));
                newPoint.FindPropertyRelative("rotation").SetVector3Val(previous.FindPropertyRelative("rotation").GetVector3Val());
                newPoint.FindPropertyRelative("scale").SetVector3Val(previous.FindPropertyRelative("scale").GetVector3Val());
            }
            else
            {
                newPoint.FindPropertyRelative("position").SetVector3Val(Vector3.zero);
                newPoint.FindPropertyRelative("rotation").SetVector3Val(Vector3.zero);
                newPoint.FindPropertyRelative("scale").SetVector3Val(Vector3.one);
            }
        }

        private static void ResizePoints(SerializedProperty pointsProp, int newSize)
        {
            int oldSize = pointsProp.arraySize;
            pointsProp.arraySize = newSize;
            for (int i = oldSize; i < newSize; i++)
            {
                var point = pointsProp.GetArrayElementAtIndex(i);
                point.FindPropertyRelative("position").SetVector3Val(i > 0 ? pointsProp.GetArrayElementAtIndex(i - 1).FindPropertyRelative("position").GetVector3Val() + new Vector3(2f, 0f, 0f) : Vector3.zero);
                point.FindPropertyRelative("rotation").SetVector3Val(i > 0 ? pointsProp.GetArrayElementAtIndex(i - 1).FindPropertyRelative("rotation").GetVector3Val() : Vector3.zero);
                point.FindPropertyRelative("scale").SetVector3Val(i > 0 ? pointsProp.GetArrayElementAtIndex(i - 1).FindPropertyRelative("scale").GetVector3Val() : Vector3.one);
            }
        }

        private static void SyncSplineSettings(SerializedProperty typeProp, SerializedProperty closedProp, params SerializedProperty[] splines)
        {
            foreach (var spline in splines)
            {
                if (spline == null) continue;
                var splineType = spline.FindPropertyRelative("splineType");
                var isClosed = spline.FindPropertyRelative("isClosed");
                if (splineType != null) splineType.enumValueIndex = typeProp.enumValueIndex;
                if (isClosed != null) isClosed.boolValue = closedProp.boolValue;
            }
        }

        private static void SyncControlPointsToSplines(SerializedProperty pointsProp, SerializedProperty positionalProp, SerializedProperty rotationalProp, SerializedProperty scalingProp, bool usePosition, bool useRotation, bool useScale)
        {
            if (usePosition) SyncChannel(pointsProp, positionalProp, "position");
            if (useRotation) SyncChannel(pointsProp, rotationalProp, "rotation");
            if (useScale) SyncChannel(pointsProp, scalingProp, "scale");
        }

        private static void PullControlPointsFromSplinesIfEmpty(SerializedProperty controlPointsProp, SerializedProperty positionalProp, SerializedProperty rotationalProp, SerializedProperty scalingProp)
        {
            if (controlPointsProp == null || controlPointsProp.arraySize > 0) return;

            var positionPoints = positionalProp?.FindPropertyRelative("points");
            var rotationPoints = rotationalProp?.FindPropertyRelative("points");
            var scalePoints = scalingProp?.FindPropertyRelative("points");
            int count = MaxInt(positionPoints?.arraySize ?? 0, rotationPoints?.arraySize ?? 0, scalePoints?.arraySize ?? 0);
            if (count <= 0) return;

            controlPointsProp.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                var point = controlPointsProp.GetArrayElementAtIndex(i);
                point.FindPropertyRelative("position").SetVector3Val(ReadPoint(positionPoints, i, Vector3.zero));
                point.FindPropertyRelative("rotation").SetVector3Val(ReadPoint(rotationPoints, i, Vector3.zero));
                point.FindPropertyRelative("scale").SetVector3Val(ReadPoint(scalePoints, i, Vector3.one));
            }
        }

        private static void SyncChannel(SerializedProperty controlPointsProp, SerializedProperty splineProp, string fieldName)
        {
            var splinePoints = splineProp?.FindPropertyRelative("points");
            if (splinePoints == null) return;

            splinePoints.arraySize = controlPointsProp.arraySize;
            for (int i = 0; i < controlPointsProp.arraySize; i++)
            {
                var source = controlPointsProp.GetArrayElementAtIndex(i).FindPropertyRelative(fieldName);
                splinePoints.GetArrayElementAtIndex(i).SetVector3Val(source.GetVector3Val());
            }
        }

        private static SerializedProperty GetPrimarySpline(SerializedProperty positionalProp, SerializedProperty rotationalProp, SerializedProperty scalingProp, bool usePosition, bool useRotation, bool useScale)
        {
            if (usePosition) return positionalProp;
            if (useRotation) return rotationalProp;
            return useScale ? scalingProp : null;
        }

        private static Vector3 ReadPoint(SerializedProperty pointsProp, int index, Vector3 fallback)
        {
            if (pointsProp == null || index < 0 || index >= pointsProp.arraySize) return fallback;
            return pointsProp.GetArrayElementAtIndex(index).GetVector3Val();
        }

        private static int GetActiveFieldCount(bool usePosition, bool useRotation, bool useScale)
        {
            int count = 0;
            if (usePosition) count++;
            if (useRotation) count++;
            if (useScale) count++;
            return count;
        }

        private static int MaxInt(int a, int b)
        {
            return a > b ? a : b;
        }

        private static int MaxInt(int a, int b, int c)
        {
            return MaxInt(MaxInt(a, b), c);
        }
    }
}
