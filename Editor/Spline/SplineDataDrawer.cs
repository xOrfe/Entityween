using UnityEditor;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace XO.Curve.Editor
{
    [CustomPropertyDrawer(typeof(SerializableSpline<>), true)]
    public class SplineDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeProp = property.FindPropertyRelative("splineType");
            SerializedProperty closedProp = property.FindPropertyRelative("isClosed");
            SerializedProperty pointsProp = property.FindPropertyRelative("points");
            SerializedProperty tangentsProp = property.FindPropertyRelative("tangents");
            SerializedProperty selectedPointIndexProp = property.FindPropertyRelative("selectedPointIndex");

            if (tangentsProp != null)
            {
                int targetSize = 0;
                SplineType currentType = (SplineType)typeProp.enumValueIndex;
                bool isClosed = closedProp.boolValue;
                int pointsSize = pointsProp.arraySize;

                if (currentType == SplineType.CubicBezier)
                {
                    targetSize = pointsSize * 2;
                }
                else if (currentType == SplineType.CatmullRom || currentType == SplineType.BSpline)
                {
                    targetSize = isClosed ? 0 : 2;
                }

                if (tangentsProp.arraySize != targetSize)
                {
                    tangentsProp.arraySize = targetSize;

                    if (currentType == SplineType.CubicBezier)
                    {
                        for (int i = 0; i < pointsSize; i++)
                        {
                            var inTProp = tangentsProp.GetArrayElementAtIndex(i * 2);
                            var outTProp = tangentsProp.GetArrayElementAtIndex(i * 2 + 1);
                            if (inTProp.GetVector3Val() == Vector3.zero && outTProp.GetVector3Val() == Vector3.zero)
                            {
                                Vector3 inT = new Vector3(-1f, 0f, 0f);
                                Vector3 outT = new Vector3(1f, 0f, 0f);
                                if (pointsSize > 1)
                                {
                                    int knotIdx = i;
                                    Vector3 p = pointsProp.GetArrayElementAtIndex(knotIdx).GetVector3Val();
                                    Vector3 prev = pointsProp.GetArrayElementAtIndex(knotIdx > 0 ? knotIdx - 1 : 0).GetVector3Val();
                                    Vector3 next = pointsProp.GetArrayElementAtIndex(knotIdx < pointsSize - 1 ? knotIdx + 1 : pointsSize - 1).GetVector3Val();
                                    Vector3 dir = (next - prev).normalized;
                                    float d = Vector3.Distance(p, prev);
                                    if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
                                    inT = -dir * (d * 0.25f);
                                    outT = dir * (d * 0.25f);
                                }
                                inTProp.SetVector3Val(inT);
                                outTProp.SetVector3Val(outT);
                            }
                        }
                    }
                    else if ((currentType == SplineType.CatmullRom || currentType == SplineType.BSpline) && !isClosed)
                    {
                        if (targetSize == 2)
                        {
                            var startPadProp = tangentsProp.GetArrayElementAtIndex(0);
                            var endPadProp = tangentsProp.GetArrayElementAtIndex(1);
                            if (startPadProp.GetVector3Val() == Vector3.zero && endPadProp.GetVector3Val() == Vector3.zero)
                            {
                                if (pointsSize > 1)
                                {
                                    Vector3 p0 = pointsProp.GetArrayElementAtIndex(0).GetVector3Val();
                                    Vector3 p1 = pointsProp.GetArrayElementAtIndex(1).GetVector3Val();
                                    Vector3 pn1 = pointsProp.GetArrayElementAtIndex(pointsSize - 1).GetVector3Val();
                                    Vector3 pn2 = pointsProp.GetArrayElementAtIndex(pointsSize - 2).GetVector3Val();

                                    startPadProp.SetVector3Val((p0 - p1) * 0.5f);
                                    endPadProp.SetVector3Val((pn1 - pn2) * 0.5f);
                                }
                                else
                                {
                                    startPadProp.SetVector3Val(new Vector3(-1f, 0f, 0f));
                                    endPadProp.SetVector3Val(new Vector3(1f, 0f, 0f));
                                }
                            }
                        }
                    }
                }
            }

            Rect bgRect = new Rect(position.x - 4, position.y + 2, position.width + 8, position.height - 4);
            GUI.Box(bgRect, GUIContent.none, EditorStyles.helpBox);

            float contentX = position.x + 8;
            float contentWidth = position.width - 16;
            float y = position.y + 6;

            Rect headerRect = new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.95f, 0.6f, 0.1f, 1f);
            GUI.Label(headerRect, "✨ Entityween Spline Config", headerStyle);
            y += EditorGUIUtility.singleLineHeight + 6;

            Rect foldoutRect = new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, "Show Settings & Points", true);
            y += EditorGUIUtility.singleLineHeight + 6;

            if (property.isExpanded)
            {

                Rect typeRect = new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Spline Type"));
                y += EditorGUIUtility.singleLineHeight + 4;

                Rect closedRect = new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(closedRect, closedProp, new GUIContent("Closed Loop"));
                y += EditorGUIUtility.singleLineHeight + 6;

                float buttonWidth = (contentWidth - 6) / 2;

                Rect editRect = new Rect(contentX, y, buttonWidth, EditorGUIUtility.singleLineHeight + 6);
                bool isEditing = SplineSceneEditorManager.IsEditingProperty(property);
                string editBtnText = isEditing ? "🟢 Stop Scene Edit" : "✏️ Edit in Scene";

                Color originalBg = GUI.backgroundColor;
                if (isEditing)
                {
                    GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f, 1f);
                }
                else
                {
                    GUI.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
                }

                if (GUI.Button(editRect, editBtnText))
                {
                    SplineSceneEditorManager.TogglePropertyEditing(property);
                }
                GUI.backgroundColor = originalBg;

                Rect addRect = new Rect(contentX + buttonWidth + 6, y, buttonWidth, EditorGUIUtility.singleLineHeight + 6);
                GUI.backgroundColor = new Color(0.2f, 0.5f, 0.85f, 1f);
                if (GUI.Button(addRect, "➕ Add Point"))
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "Add Spline Point");
                    pointsProp.arraySize++;
                    int size = pointsProp.arraySize;
                    if (size > 1)
                    {
                        var prevPointProp = pointsProp.GetArrayElementAtIndex(size - 2);
                        var newPointProp = pointsProp.GetArrayElementAtIndex(size - 1);
                        newPointProp.SetVector3Val(prevPointProp.GetVector3Val() + new Vector3(2f, 0f, 0f));
                    }
                    property.serializedObject.ApplyModifiedProperties();
                }
                GUI.backgroundColor = originalBg;
                y += EditorGUIUtility.singleLineHeight + 12;

                Rect clearRect = new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight + 4);
                GUI.backgroundColor = new Color(0.8f, 0.25f, 0.25f, 0.8f);
                if (GUI.Button(clearRect, "🗑️ Clear All Points"))
                {
                    if (EditorUtility.DisplayDialog("Clear Spline", "Are you sure you want to clear all points?", "Yes", "No"))
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "Clear Spline Points");
                        pointsProp.arraySize = 0;
                        if (tangentsProp != null) tangentsProp.arraySize = 0;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                GUI.backgroundColor = originalBg;
                y += EditorGUIUtility.singleLineHeight + 10;

                SplineType type = (SplineType)typeProp.enumValueIndex;
                bool closed = closedProp.boolValue;
                int selectedIdx = selectedPointIndexProp != null ? selectedPointIndexProp.intValue : -1;
                bool canReset = false;
                if (type == SplineType.CubicBezier && selectedIdx >= 0 && selectedIdx < pointsProp.arraySize)
                {
                    canReset = true;
                }
                else if ((type == SplineType.CatmullRom || type == SplineType.BSpline) && !closed && pointsProp.arraySize >= 2)
                {
                    canReset = true;
                }

                if (canReset)
                {
                    Rect resetRect = new Rect(contentX, y, contentWidth, EditorGUIUtility.singleLineHeight + 4);
                    GUI.backgroundColor = new Color(0.95f, 0.5f, 0.1f, 0.95f);
                    if (GUI.Button(resetRect, "🔄 Reset Selected Point Controls"))
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "Reset Selected Point Controls");
                        int n = pointsProp.arraySize;

                        if (type == SplineType.CubicBezier)
                        {
                            Vector3 p = pointsProp.GetArrayElementAtIndex(selectedIdx).GetVector3Val();
                            Vector3 inT = new Vector3(-1f, 0f, 0f);
                            Vector3 outT = new Vector3(1f, 0f, 0f);

                            if (n > 1)
                            {
                                Vector3 dir = Vector3.right;
                                float dist = 2f;
                                if (selectedIdx > 0 && selectedIdx < n - 1)
                                {
                                    Vector3 prev = pointsProp.GetArrayElementAtIndex(selectedIdx - 1).GetVector3Val();
                                    Vector3 next = pointsProp.GetArrayElementAtIndex(selectedIdx + 1).GetVector3Val();
                                    dir = (next - prev).normalized;
                                    dist = (Vector3.Distance(p, prev) + Vector3.Distance(p, next)) * 0.5f;
                                }
                                else if (selectedIdx == 0)
                                {
                                    Vector3 next = pointsProp.GetArrayElementAtIndex(1).GetVector3Val();
                                    dir = (next - p).normalized;
                                    dist = Vector3.Distance(p, next);
                                }
                                else
                                {
                                    Vector3 prev = pointsProp.GetArrayElementAtIndex(n - 2).GetVector3Val();
                                    dir = (p - prev).normalized;
                                    dist = Vector3.Distance(p, prev);
                                }

                                if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
                                inT = -dir * (dist * 0.25f);
                                outT = dir * (dist * 0.25f);
                            }

                            if (tangentsProp != null && tangentsProp.arraySize == n * 2)
                            {
                                tangentsProp.GetArrayElementAtIndex(selectedIdx * 2).SetVector3Val(inT);
                                tangentsProp.GetArrayElementAtIndex(selectedIdx * 2 + 1).SetVector3Val(outT);
                            }
                        }
                        else if ((type == SplineType.CatmullRom || type == SplineType.BSpline) && !closed)
                        {
                            if (tangentsProp != null && tangentsProp.arraySize == 2)
                            {
                                Vector3 p0 = pointsProp.GetArrayElementAtIndex(0).GetVector3Val();
                                Vector3 p1 = pointsProp.GetArrayElementAtIndex(1).GetVector3Val();
                                Vector3 pn1 = pointsProp.GetArrayElementAtIndex(n - 1).GetVector3Val();
                                Vector3 pn2 = pointsProp.GetArrayElementAtIndex(n - 2).GetVector3Val();

                                tangentsProp.GetArrayElementAtIndex(0).SetVector3Val((p0 - p1) * 0.5f);
                                tangentsProp.GetArrayElementAtIndex(1).SetVector3Val((pn1 - pn2) * 0.5f);
                            }
                        }

                        property.serializedObject.ApplyModifiedProperties();
                    }
                    GUI.backgroundColor = originalBg;
                    y += EditorGUIUtility.singleLineHeight + 10;
                }

                Rect pointsRect = new Rect(contentX, y, contentWidth, EditorGUI.GetPropertyHeight(pointsProp));
                EditorGUI.PropertyField(pointsRect, pointsProp, new GUIContent("Control Points List"), true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUIUtility.singleLineHeight + 12;
            baseHeight += EditorGUIUtility.singleLineHeight + 6;

            if (!property.isExpanded)
            {
                return baseHeight;
            }

            SerializedProperty typeProp = property.FindPropertyRelative("splineType");
            SerializedProperty closedProp = property.FindPropertyRelative("isClosed");
            SerializedProperty pointsProp = property.FindPropertyRelative("points");
            SerializedProperty selectedPointIndexProp = property.FindPropertyRelative("selectedPointIndex");

            float h = baseHeight;
            h += EditorGUIUtility.singleLineHeight + 4;
            h += EditorGUIUtility.singleLineHeight + 6;
            h += EditorGUIUtility.singleLineHeight + 12;
            h += EditorGUIUtility.singleLineHeight + 10;

            SplineType type = (SplineType)typeProp.enumValueIndex;
            bool closed = closedProp.boolValue;
            int selectedIdx = selectedPointIndexProp != null ? selectedPointIndexProp.intValue : -1;
            bool canReset = false;
            if (type == SplineType.CubicBezier && selectedIdx >= 0 && selectedIdx < pointsProp.arraySize)
            {
                canReset = true;
            }
            else if ((type == SplineType.CatmullRom || type == SplineType.BSpline) && !closed && pointsProp.arraySize >= 2)
            {
                canReset = true;
            }

            if (canReset)
            {
                h += EditorGUIUtility.singleLineHeight + 10;
            }

            h += EditorGUI.GetPropertyHeight(pointsProp) + 8;

            return h;
        }
    }
}
