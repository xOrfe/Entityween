using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XO.Curve.Editor
{
    [InitializeOnLoad]
    public static class SplineSceneEditorManager
    {
        private const string SESSION_KEY_ACTIVE_GLOBAL_ID = "Entityween.SplineEditor.ActiveGlobalId";
        private const string SESSION_KEY_ACTIVE_PROPERTY_PATH = "Entityween.SplineEditor.ActivePropertyPath";

        private static string _activePropertyPath = "";
        private static EntityId _activeTargetId = EntityId.None;

        public static bool MirrorTangents = true;
        public static bool EditRotationMode = false;
        private static readonly Vector3 _defaultTangentOffset = new Vector3(1f, 0f, 1f);
        private static bool _isRotating = false;
        private static Quaternion _lastFrameRotation = Quaternion.identity;

        static SplineSceneEditorManager()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RestoreSessionState();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                _activePropertyPath = "";
                _activeTargetId = EntityId.None;
                _isRotating = false;
                _lastFrameRotation = Quaternion.identity;
            }
        }

        private static void RestoreSessionState()
        {
            var globalIdStr = SessionState.GetString(SESSION_KEY_ACTIVE_GLOBAL_ID, "");
            _activePropertyPath = SessionState.GetString(SESSION_KEY_ACTIVE_PROPERTY_PATH, "");

            if (string.IsNullOrEmpty(globalIdStr) || string.IsNullOrEmpty(_activePropertyPath))
            {
                _activeTargetId = UnityEngine.EntityId.None;
                _activePropertyPath = "";
                return;
            }

            if (GlobalObjectId.TryParse(globalIdStr, out var globalId))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                if (obj != null)
                {
                    _activeTargetId = obj.GetEntityId();
                    Selection.activeObject = obj;
                    Tools.current = Tool.None;
                    return;
                }
            }

            _activeTargetId = UnityEngine.EntityId.None;
            _activePropertyPath = "";
            Tools.current = Tool.Move;
            ClearSessionState();
        }

        private static void SaveSessionState()
        {
            if (_activeTargetId == UnityEngine.EntityId.None || string.IsNullOrEmpty(_activePropertyPath))
            {
                ClearSessionState();
                return;
            }

            var targetObject = EditorUtility.EntityIdToObject(_activeTargetId);
            if (targetObject != null)
            {
                var globalId = GlobalObjectId.GetGlobalObjectIdSlow(targetObject);
                SessionState.SetString(SESSION_KEY_ACTIVE_GLOBAL_ID, globalId.ToString());
                SessionState.SetString(SESSION_KEY_ACTIVE_PROPERTY_PATH, _activePropertyPath);
            }
            else
            {
                ClearSessionState();
            }
        }

        private static void ClearSessionState()
        {
            SessionState.EraseString(SESSION_KEY_ACTIVE_GLOBAL_ID);
            SessionState.EraseString(SESSION_KEY_ACTIVE_PROPERTY_PATH);
        }

        public static bool IsEditingProperty(SerializedProperty property)
        {
            if (property == null || property.serializedObject == null) return false;
            var target = property.serializedObject.targetObject;
            if (target == null) return false;

            return _activeTargetId == target.GetEntityId() && _activePropertyPath == property.propertyPath;
        }

        public static void TogglePropertyEditing(SerializedProperty property)
        {
            if (property == null || property.serializedObject == null) return;
            var target = property.serializedObject.targetObject;
            if (target == null) return;

            if (IsEditingProperty(property))
            {

                _activePropertyPath = "";
                _activeTargetId = UnityEngine.EntityId.None;
                Tools.current = Tool.Move;
                ClearSessionState();
            }
            else
            {

                _activePropertyPath = property.propertyPath;
                _activeTargetId = target.GetEntityId();

                EditRotationMode = property.name.Contains("rotational") || property.propertyPath.Contains("rotational");

                Selection.activeObject = target;
                Tools.current = Tool.None;
                SaveSessionState();
            }
            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (_activeTargetId == UnityEngine.EntityId.None || string.IsNullOrEmpty(_activePropertyPath)) return;

            var targetObject = EditorUtility.EntityIdToObject(_activeTargetId);
            GameObject targetGameObject = null;
            if (targetObject is Component splineComp)
            {
                targetGameObject = splineComp.gameObject;
            }

            if (targetObject == null || (Selection.activeObject != targetObject && Selection.activeObject != targetGameObject))
            {

                if (_activeTargetId != UnityEngine.EntityId.None)
                {
                    _activeTargetId = UnityEngine.EntityId.None;
                    _activePropertyPath = "";
                    Tools.current = Tool.Move;
                    ClearSessionState();
                }
                return;
            }

            if (Tools.current != Tool.None)
            {
                Tools.current = Tool.None;
            }

            var serializedObject = new SerializedObject(targetObject);
            serializedObject.Update();

            var property = serializedObject.FindProperty(_activePropertyPath);
            if (property == null) return;

            var splineTypeProp = property.FindPropertyRelative("splineType");
            var isClosedProp = property.FindPropertyRelative("isClosed");
            var pointsProp = property.FindPropertyRelative("points");
            var tangentsProp = property.FindPropertyRelative("tangents");
            var selectedIdxProp = property.FindPropertyRelative("selectedPointIndex");
            var autoTangentProp = property.FindPropertyRelative("autoTangent");

            if (splineTypeProp == null || isClosedProp == null || pointsProp == null) return;

            var type = (SplineType)splineTypeProp.enumValueIndex;
            bool isClosed = isClosedProp.boolValue;
            int n = pointsProp.arraySize;
            int selectedIdx = selectedIdxProp != null ? selectedIdxProp.intValue : -1;

            if (type == SplineType.CatmullRom || type == SplineType.BSpline)
            {
                if (autoTangentProp != null && !autoTangentProp.boolValue)
                {
                    autoTangentProp.boolValue = true;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            string parentPath = property.propertyPath;
            object splineDataObj = GetFieldValue(targetObject, parentPath);
            if (splineDataObj != null)
            {
                var initMethod = splineDataObj.GetType().GetMethod("InitializeOrResizeTangents");
                if (initMethod != null)
                {
                    initMethod.Invoke(splineDataObj, null);
                }
            }
            serializedObject.Update();

            Transform localTransform = null;
            if (targetObject is Component comp)
            {
                localTransform = comp.transform;
            }

            var pts = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                pts.Add(pointsProp.GetArrayElementAtIndex(i).GetVector3Val());
            }

            var evalPts = new List<Vector3>();
            if (type == SplineType.CubicBezier && n >= 2 && tangentsProp != null && tangentsProp.arraySize == n * 2)
            {
                if (isClosed)
                {
                    for (int i = 0; i < n; i++)
                    {
                        evalPts.Add(pts[i]);
                        evalPts.Add(pts[i] + tangentsProp.GetArrayElementAtIndex(i * 2 + 1).GetVector3Val());
                        if (i < n - 1)
                        {
                            evalPts.Add(pts[i + 1] + tangentsProp.GetArrayElementAtIndex((i + 1) * 2).GetVector3Val());
                        }
                        else
                        {
                            evalPts.Add(pts[0] + tangentsProp.GetArrayElementAtIndex(0).GetVector3Val());
                            evalPts.Add(pts[0]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        evalPts.Add(pts[i]);
                        if (i < n - 1)
                        {
                            evalPts.Add(pts[i] + tangentsProp.GetArrayElementAtIndex(i * 2 + 1).GetVector3Val());
                            evalPts.Add(pts[i + 1] + tangentsProp.GetArrayElementAtIndex((i + 1) * 2).GetVector3Val());
                        }
                    }
                }
            }
            else if ((type == SplineType.CatmullRom || type == SplineType.BSpline) && !isClosed && tangentsProp != null && tangentsProp.arraySize == 2 && n >= 2)
            {
                evalPts.Add(pts[0] + tangentsProp.GetArrayElementAtIndex(0).GetVector3Val());
                evalPts.AddRange(pts);
                evalPts.Add(pts[n - 1] + tangentsProp.GetArrayElementAtIndex(1).GetVector3Val());
            }
            else
            {
                evalPts.AddRange(pts);
            }

            var worldPts = new List<Vector3>();
            foreach (var pt in pts)
            {
                worldPts.Add(localTransform != null ? localTransform.TransformPoint(pt) : pt);
            }

            DrawSplinePath(type, evalPts, isClosed, localTransform);

            HandleExtensionInput(Event.current, pointsProp, tangentsProp, localTransform, type, isClosed, selectedIdxProp);

            serializedObject.Update();
            bool changed = DrawHandlesAndPoints(type, pointsProp, tangentsProp, worldPts, isClosed, selectedIdxProp, selectedIdx, localTransform);
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }

            DrawHUD(property, splineTypeProp, isClosedProp, pointsProp, tangentsProp, selectedIdxProp, selectedIdx);
        }

        private static void DrawSplinePath(SplineType type, List<Vector3> pts, bool isClosed, Transform localTransform)
        {
            int n = pts.Count;
            if (n < 2) return;

            var mathProvider = CurveMathUtility.GetMathProvider<Unity.Mathematics.float3>();
            if (mathProvider == null) return;

            var evalPoints = new List<Unity.Mathematics.float3>();
            foreach (var p in pts) evalPoints.Add(p);

            int segmentsCount = isClosed ? n : n - 1;
            if (type == SplineType.CubicBezier)
            {
                segmentsCount = (n - 1) / 3;
                if (segmentsCount <= 0) return;
            }
            else if (type == SplineType.CatmullRom || type == SplineType.BSpline)
            {
                if (!isClosed)
                {
                    segmentsCount = n - 3;
                }
            }

            if (segmentsCount <= 0) return;

            var linePoints = new List<Vector3>();
            int samplesPerSegment = 20;

            var curveProvider = new Spline.EditorSplineAdapter(evalPoints, type, isClosed);

            for (int seg = 0; seg < segmentsCount; seg++)
            {
                for (int s = 0; s <= samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    if (seg == segmentsCount - 1 && s == samplesPerSegment && !isClosed) t = 1f;

                    var globalT = (seg + t) / (float)segmentsCount;
                    var sample = Spline.SampleGeneric<Unity.Mathematics.float3, Spline.EditorSplineAdapter, Float3Math>(ref curveProvider, globalT, (Float3Math)mathProvider);
                    var worldSample = localTransform != null ? localTransform.TransformPoint(sample) : (Vector3)sample;
                    linePoints.Add(worldSample);
                }
            }

            Handles.color = new Color(0.2f, 0.7f, 1f, 0.9f);
            Handles.DrawAAPolyLine(4f, linePoints.ToArray());
        }

        private static bool DrawHandlesAndPoints(
            SplineType type,
            SerializedProperty pointsProp,
            SerializedProperty tangentsProp,
            List<Vector3> worldPts,
            bool isClosed,
            SerializedProperty selectedIdxProp,
            int selectedIdx,
            Transform localTransform)
        {
            bool changed = false;
            int n = worldPts.Count;

            SerializedProperty siblingPositional = null;
            SerializedProperty siblingRotational = null;
            List<Vector3> siblingWorldPositions = null;
            if (EditRotationMode)
            {
                siblingPositional = FindSiblingPointsProperty(pointsProp, "positional");
                siblingRotational = FindSiblingPointsProperty(pointsProp, "rotational");
                if (siblingPositional != null && siblingPositional.isArray)
                {
                    siblingWorldPositions = new List<Vector3>();
                    int siblingSize = siblingPositional.arraySize;
                    for (int i = 0; i < siblingSize; i++)
                    {
                        var pt = siblingPositional.GetArrayElementAtIndex(i).GetVector3Val();
                        siblingWorldPositions.Add(localTransform != null ? localTransform.TransformPoint(pt) : pt);
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                Vector3 wPos = worldPts[i];

                if (EditRotationMode && siblingWorldPositions != null && i < siblingWorldPositions.Count)
                {
                    wPos = siblingWorldPositions[i];
                }

                Color knotColor = new Color(0.1f, 0.8f, 0.9f, 0.9f);
                float sizeMultiplier = 1f;

                if (type == SplineType.CubicBezier)
                {
                    knotColor = Color.white;
                    sizeMultiplier = 1.3f;
                }

                float handleSize = HandleUtility.GetHandleSize(wPos) * 0.12f * sizeMultiplier;
                Handles.color = knotColor;

                if (i != selectedIdx)
                {
                    if (type == SplineType.CubicBezier)
                    {
                        if (Handles.Button(wPos, Quaternion.identity, handleSize, handleSize, Handles.RectangleHandleCap))
                        {
                            if (selectedIdxProp != null)
                            {
                                selectedIdxProp.intValue = i;
                                changed = true;
                            }
                            RepaintAllViews();
                        }
                    }
                    else
                    {
                        if (Handles.Button(wPos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                        {
                            if (selectedIdxProp != null)
                            {
                                selectedIdxProp.intValue = i;
                                changed = true;
                            }
                            RepaintAllViews();
                        }
                    }
                }

                if (i == selectedIdx)
                {

                    Handles.color = new Color(0.2f, 1f, 0.2f, 1.0f);
                    float activeSize = handleSize * 1.1f;
                    if (type == SplineType.CubicBezier)
                    {
                        Handles.RectangleHandleCap(0, wPos, Quaternion.identity, activeSize, EventType.Repaint);
                    }
                    else
                    {
                        Handles.SphereHandleCap(0, wPos, Quaternion.identity, activeSize, EventType.Repaint);
                    }

                    EditorGUI.BeginChangeCheck();

                    if (EditRotationMode)
                    {

                        bool isRotationalSpline = IsSplinePointsProperty(pointsProp, "rotational") ||
                                                  (siblingRotational != null && siblingRotational.isArray && i < siblingRotational.arraySize);

                        if (isRotationalSpline)
                        {

                            SerializedProperty rotationPointsProp = IsSplinePointsProperty(pointsProp, "rotational") ? pointsProp : siblingRotational;
                            if (rotationPointsProp == null || i >= rotationPointsProp.arraySize)
                            {
                                continue;
                            }

                            Vector3 euler = rotationPointsProp.GetArrayElementAtIndex(i).GetVector3Val();
                            Quaternion targetRot = Quaternion.Euler(euler);

                            if (GUIUtility.hotControl != 0)
                            {
                                if (!_isRotating)
                                {
                                    _isRotating = true;
                                    _lastFrameRotation = targetRot;
                                }
                            }
                            else
                            {
                                _isRotating = false;
                            }

                            Quaternion currentRot = _isRotating ? _lastFrameRotation : targetRot;

                            float axisLength = HandleUtility.GetHandleSize(wPos) * 0.5f;
                            Handles.color = Color.red; Handles.DrawLine(wPos, wPos + currentRot * Vector3.right * axisLength);
                            Handles.color = Color.green; Handles.DrawLine(wPos, wPos + currentRot * Vector3.up * axisLength);
                            Handles.color = Color.blue; Handles.DrawLine(wPos, wPos + currentRot * Vector3.forward * axisLength);

                            Quaternion newRot = Handles.RotationHandle(currentRot, wPos);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(pointsProp.serializedObject.targetObject, "Rotate Spline Point");
                                Vector3 newEuler = newRot.eulerAngles;
                                rotationPointsProp.GetArrayElementAtIndex(i).SetVector3Val(newEuler);

                                if (_isRotating)
                                {
                                    _lastFrameRotation = newRot;
                                }

                                changed = true;
                                EditorUtility.SetDirty(pointsProp.serializedObject.targetObject);
                            }
                        }
                        else
                        {

                            Quaternion targetTangentRot = localTransform != null ? localTransform.rotation : Quaternion.identity;

                            Vector3 localInT = Vector3.zero;
                            Vector3 localOutT = Vector3.zero;
                            bool hasInTangent = false;
                            bool hasOutTangent = false;

                            if (tangentsProp != null)
                            {
                                if (type == SplineType.CubicBezier)
                                {
                                    if (i * 2 + 1 < tangentsProp.arraySize)
                                    {
                                        localInT = tangentsProp.GetArrayElementAtIndex(i * 2).GetVector3Val();
                                        localOutT = tangentsProp.GetArrayElementAtIndex(i * 2 + 1).GetVector3Val();
                                        hasInTangent = true;
                                        hasOutTangent = true;
                                    }
                                }
                                else if ((type == SplineType.CatmullRom || type == SplineType.BSpline) && !isClosed)
                                {
                                    if (i == 0 && tangentsProp.arraySize > 0)
                                    {
                                        localOutT = tangentsProp.GetArrayElementAtIndex(0).GetVector3Val();
                                        hasOutTangent = true;
                                    }
                                    else if (i == n - 1 && tangentsProp.arraySize > 1)
                                    {
                                        localOutT = tangentsProp.GetArrayElementAtIndex(1).GetVector3Val();
                                        hasOutTangent = true;
                                    }
                                }
                            }

                            Vector3 referenceDir = Vector3.zero;
                            if (hasOutTangent && localOutT.sqrMagnitude > 0.001f)
                            {
                                referenceDir = localTransform != null ? localTransform.TransformDirection(localOutT) : localOutT;
                            }
                            else if (hasInTangent && localInT.sqrMagnitude > 0.001f)
                            {
                                referenceDir = localTransform != null ? localTransform.TransformDirection(-localInT) : -localInT;
                            }

                            if (referenceDir.sqrMagnitude > 0.001f)
                            {
                                Vector3 upDir = localTransform != null ? localTransform.up : Vector3.up;
                                if (Mathf.Abs(Vector3.Dot(referenceDir.normalized, upDir)) > 0.99f)
                                {
                                    upDir = localTransform != null ? localTransform.forward : Vector3.forward;
                                }
                                targetTangentRot = Quaternion.LookRotation(referenceDir.normalized, upDir);
                            }

                            if (GUIUtility.hotControl != 0)
                            {
                                if (!_isRotating)
                                {
                                    _isRotating = true;
                                    _lastFrameRotation = targetTangentRot;
                                }
                            }
                            else
                            {
                                _isRotating = false;
                            }

                            Quaternion currentRot = _isRotating ? _lastFrameRotation : targetTangentRot;

                            float axisLength = HandleUtility.GetHandleSize(wPos) * 0.5f;
                            Handles.color = Color.red; Handles.DrawLine(wPos, wPos + currentRot * Vector3.right * axisLength);
                            Handles.color = Color.green; Handles.DrawLine(wPos, wPos + currentRot * Vector3.up * axisLength);
                            Handles.color = Color.blue; Handles.DrawLine(wPos, wPos + currentRot * Vector3.forward * axisLength);

                            Quaternion newRot = Handles.RotationHandle(currentRot, wPos);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(pointsProp.serializedObject.targetObject, "Rotate Spline Tangents");
                                Quaternion deltaRot = newRot * Quaternion.Inverse(currentRot);

                                if (type == SplineType.CubicBezier)
                                {
                                    if (hasInTangent)
                                    {
                                        Vector3 worldInT = localTransform != null ? localTransform.TransformDirection(localInT) : localInT;
                                        Vector3 rotatedWorldInT = deltaRot * worldInT;
                                        Vector3 newLocalInT = localTransform != null ? localTransform.InverseTransformDirection(rotatedWorldInT) : rotatedWorldInT;
                                        tangentsProp.GetArrayElementAtIndex(i * 2).SetVector3Val(newLocalInT);
                                    }
                                    if (hasOutTangent)
                                    {
                                        Vector3 worldOutT = localTransform != null ? localTransform.TransformDirection(localOutT) : localOutT;
                                        Vector3 rotatedWorldOutT = deltaRot * worldOutT;
                                        Vector3 newLocalOutT = localTransform != null ? localTransform.InverseTransformDirection(rotatedWorldOutT) : rotatedWorldOutT;
                                        tangentsProp.GetArrayElementAtIndex(i * 2 + 1).SetVector3Val(newLocalOutT);
                                    }
                                    if (MirrorTangents && hasInTangent && hasOutTangent)
                                    {
                                        Vector3 newLocalOutT = tangentsProp.GetArrayElementAtIndex(i * 2 + 1).GetVector3Val();
                                        tangentsProp.GetArrayElementAtIndex(i * 2).SetVector3Val(-newLocalOutT);
                                    }
                                }
                                else if ((type == SplineType.CatmullRom || type == SplineType.BSpline) && !isClosed)
                                {
                                    if (i == 0 && tangentsProp.arraySize > 0)
                                    {
                                        Vector3 worldT = localTransform != null ? localTransform.TransformDirection(localOutT) : localOutT;
                                        Vector3 rotatedWorldT = deltaRot * worldT;
                                        Vector3 newLocalT = localTransform != null ? localTransform.InverseTransformDirection(rotatedWorldT) : rotatedWorldT;
                                        tangentsProp.GetArrayElementAtIndex(0).SetVector3Val(newLocalT);
                                    }
                                    else if (i == n - 1 && tangentsProp.arraySize > 1)
                                    {
                                        Vector3 worldT = localTransform != null ? localTransform.TransformDirection(localOutT) : localOutT;
                                        Vector3 rotatedWorldT = deltaRot * worldT;
                                        Vector3 newLocalT = localTransform != null ? localTransform.InverseTransformDirection(rotatedWorldT) : rotatedWorldT;
                                        tangentsProp.GetArrayElementAtIndex(1).SetVector3Val(newLocalT);
                                    }
                                }

                                if (_isRotating)
                                {
                                    _lastFrameRotation = newRot;
                                }

                                var autoTangentProp = pointsProp.serializedObject.FindProperty(pointsProp.propertyPath.Replace(".points", ".autoTangent"));
                                if (autoTangentProp != null)
                                {
                                    autoTangentProp.boolValue = false;
                                }

                                changed = true;
                                EditorUtility.SetDirty(pointsProp.serializedObject.targetObject);
                            }
                        }
                    }
                    else
                    {

                        Vector3 newWorldPos = Handles.PositionHandle(wPos, Quaternion.identity);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(pointsProp.serializedObject.targetObject, "Move Spline Point");

                            Vector3 newLocalPos = localTransform != null ? localTransform.InverseTransformPoint(newWorldPos) : newWorldPos;
                            pointsProp.GetArrayElementAtIndex(i).SetVector3Val(newLocalPos);

                            pointsProp.serializedObject.ApplyModifiedProperties();

                            var autoTangentProp = pointsProp.serializedObject.FindProperty(pointsProp.propertyPath.Replace(".points", ".autoTangent"));
                            if (autoTangentProp != null && autoTangentProp.boolValue)
                            {
                                string parentPath = pointsProp.propertyPath.Replace(".points", "");
                                object splineDataObj = GetFieldValue(pointsProp.serializedObject.targetObject, parentPath);
                                if (splineDataObj != null)
                                {
                                    var method = splineDataObj.GetType().GetMethod("RecalculateAllTangents");
                                    if (method != null)
                                    {
                                        method.Invoke(splineDataObj, null);
                                    }
                                }

                                pointsProp.serializedObject.Update();
                            }

                            changed = true;
                            EditorUtility.SetDirty(pointsProp.serializedObject.targetObject);
                        }
                    }
                }

                if (type == SplineType.CubicBezier && i == selectedIdx && tangentsProp != null && tangentsProp.arraySize == n * 2)
                {
                    Vector3 localKnot = pointsProp.GetArrayElementAtIndex(i).GetVector3Val();

                    Vector3 localInT = tangentsProp.GetArrayElementAtIndex(i * 2).GetVector3Val();
                    Vector3 worldInT = localTransform != null ? localTransform.TransformPoint(localKnot + localInT) : (wPos + localInT);

                    Handles.color = new Color(1f, 0.65f, 0f, 0.6f);
                    Handles.DrawDottedLine(wPos, worldInT, 4f);

                    Handles.color = new Color(1f, 0.65f, 0f, 1f);
                    float inSize = HandleUtility.GetHandleSize(worldInT) * 0.08f;

                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldInT = Handles.FreeMoveHandle(worldInT, inSize, Vector3.zero, (controlID, pos, rot, size, eventType) =>
                    {
                        Quaternion camRot = Camera.current != null ? Camera.current.transform.rotation : Quaternion.identity;
                        Handles.CircleHandleCap(controlID, pos, camRot, size, eventType);
                    });
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(pointsProp.serializedObject.targetObject, "Move Spline Tangent");
                        Vector3 newLocalInT = localTransform != null ? localTransform.InverseTransformPoint(newWorldInT) - localKnot : (newWorldInT - wPos);
                        tangentsProp.GetArrayElementAtIndex(i * 2).SetVector3Val(newLocalInT);

                        if (MirrorTangents)
                        {
                            tangentsProp.GetArrayElementAtIndex(i * 2 + 1).SetVector3Val(-newLocalInT);
                        }

                        var autoTangentProp = pointsProp.serializedObject.FindProperty(pointsProp.propertyPath.Replace(".points", ".autoTangent"));
                        if (autoTangentProp != null)
                        {
                            autoTangentProp.boolValue = false;
                        }

                        changed = true;
                        EditorUtility.SetDirty(pointsProp.serializedObject.targetObject);
                    }

                    Vector3 localOutT = tangentsProp.GetArrayElementAtIndex(i * 2 + 1).GetVector3Val();
                    Vector3 worldOutT = localTransform != null ? localTransform.TransformPoint(localKnot + localOutT) : (wPos + localOutT);

                    Handles.color = new Color(1f, 0.65f, 0f, 0.6f);
                    Handles.DrawDottedLine(wPos, worldOutT, 4f);

                    Handles.color = new Color(1f, 0.65f, 0f, 1f);
                    float outSize = HandleUtility.GetHandleSize(worldOutT) * 0.08f;

                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldOutT = Handles.FreeMoveHandle(worldOutT, outSize, Vector3.zero, (controlID, pos, rot, size, eventType) =>
                    {
                        Quaternion camRot = Camera.current != null ? Camera.current.transform.rotation : Quaternion.identity;
                        Handles.CircleHandleCap(controlID, pos, camRot, size, eventType);
                    });
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(pointsProp.serializedObject.targetObject, "Move Spline Tangent");
                        Vector3 newLocalOutT = localTransform != null ? localTransform.InverseTransformPoint(newWorldOutT) - localKnot : (newWorldOutT - wPos);
                        tangentsProp.GetArrayElementAtIndex(i * 2 + 1).SetVector3Val(newLocalOutT);

                        if (MirrorTangents)
                        {
                            tangentsProp.GetArrayElementAtIndex(i * 2).SetVector3Val(-newLocalOutT);
                        }

                        var autoTangentProp = pointsProp.serializedObject.FindProperty(pointsProp.propertyPath.Replace(".points", ".autoTangent"));
                        if (autoTangentProp != null)
                        {
                            autoTangentProp.boolValue = false;
                        }

                        changed = true;
                        EditorUtility.SetDirty(pointsProp.serializedObject.targetObject);
                    }
                }
            }

            if (changed)
            {
                SyncTransformControlPointsIfPresent(pointsProp);
            }

            return changed;
        }

        private static void HandleExtensionInput(
            Event e,

            SerializedProperty pointsProp,

            SerializedProperty tangentsProp,
            Transform localTransform,

            SplineType type,

            bool isClosed,

            SerializedProperty selectedIdxProp)
        {
            if (e.shift && e.type == EventType.MouseDown && e.button == 0)
            {
                e.Use();

                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 worldHit = Vector3.zero;
                bool foundHit = false;

                if (Physics.Raycast(ray, out var hit))
                {
                    worldHit = hit.point;
                    foundHit = true;
                }
                else
                {

                    int size = pointsProp.arraySize;
                    Vector3 lastLocal = size > 0 ? pointsProp.GetArrayElementAtIndex(size - 1).GetVector3Val() : Vector3.zero;
                    Vector3 lastWorld = localTransform != null ? localTransform.TransformPoint(lastLocal) : lastLocal;

                    var plane = new Plane(Vector3.up, lastWorld);
                    if (plane.Raycast(ray, out float dist))
                    {
                        worldHit = ray.GetPoint(dist);
                        foundHit = true;
                    }
                }

                if (foundHit)
                {
                    Vector3 localHit = localTransform != null ? localTransform.InverseTransformPoint(worldHit) : worldHit;

                    Undo.RecordObject(pointsProp.serializedObject.targetObject, "Extend Spline");

                    int size = pointsProp.arraySize;
                    if (type == SplineType.CubicBezier && size > 0)
                    {
                        Vector3 lastAnchor = pointsProp.GetArrayElementAtIndex(size - 1).GetVector3Val();

                        pointsProp.arraySize++;
                        pointsProp.GetArrayElementAtIndex(size).SetVector3Val(localHit);

                        if (tangentsProp != null)
                        {
                            tangentsProp.arraySize = pointsProp.arraySize * 2;
                            int knotIdx = pointsProp.arraySize - 1;

                            int prevKnotIdx = knotIdx - 1;
                            Vector3 dir = (localHit - lastAnchor).normalized;
                            float d = Vector3.Distance(localHit, lastAnchor);
                            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

                            tangentsProp.GetArrayElementAtIndex(prevKnotIdx * 2 + 1).SetVector3Val(dir * (d * 0.25f));

                            tangentsProp.GetArrayElementAtIndex(knotIdx * 2).SetVector3Val(-dir * (d * 0.25f));
                            tangentsProp.GetArrayElementAtIndex(knotIdx * 2 + 1).SetVector3Val(dir * (d * 0.25f));
                        }

                        if (selectedIdxProp != null) selectedIdxProp.intValue = size;
                    }
                    else
                    {

                        pointsProp.arraySize++;
                        pointsProp.GetArrayElementAtIndex(size).SetVector3Val(localHit);
                        if (type == SplineType.CubicBezier && tangentsProp != null)
                        {
                            tangentsProp.arraySize = pointsProp.arraySize * 2;
                            int knotIdx = pointsProp.arraySize - 1;
                            tangentsProp.GetArrayElementAtIndex(knotIdx * 2).SetVector3Val(new Vector3(-1f, 0f, 0f));
                            tangentsProp.GetArrayElementAtIndex(knotIdx * 2 + 1).SetVector3Val(new Vector3(1f, 0f, 0f));
                        }
                        if (selectedIdxProp != null) selectedIdxProp.intValue = size;
                    }

                    SyncTransformControlPointsIfPresent(pointsProp);
                    pointsProp.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(pointsProp.serializedObject.targetObject);
                    RepaintAllViews();
                }
            }
        }

        private static void DrawHUD(
            SerializedProperty property,

            SerializedProperty splineTypeProp,

            SerializedProperty isClosedProp,

            SerializedProperty pointsProp,

            SerializedProperty tangentsProp,
            SerializedProperty selectedIdxProp,

            int selectedIdx)
        {
            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(15, 30, 240, 300));

            var style = new GUIStyle(GUI.skin.box);
            style.padding = new RectOffset(12, 12, 10, 10);
            style.normal.background = Texture2D.whiteTexture;

            var bgCol = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
            GUILayout.BeginVertical(style);
            GUI.color = bgCol;

            var titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 13;
            titleStyle.normal.textColor = new Color(0.95f, 0.6f, 0.1f, 1f);
            GUILayout.Label("✨ Entityween Spline HUD", titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label);
            subStyle.fontSize = 10;
            subStyle.normal.textColor = Color.gray;
            GUILayout.Label("Target: " + property.name, subStyle);

            GUILayout.Space(8);

            GUILayout.Label("Edit Axis Mode:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!EditRotationMode, "Position", "Button", GUILayout.ExpandWidth(true))) EditRotationMode = false;
            if (GUILayout.Toggle(EditRotationMode, "Rotation", "Button", GUILayout.ExpandWidth(true))) EditRotationMode = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            var type = (SplineType)splineTypeProp.enumValueIndex;
            if (type == SplineType.CubicBezier)
            {
                GUILayout.Label("Bezier Tangents Mode:", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(MirrorTangents, "Symmetric", "Button", GUILayout.ExpandWidth(true))) MirrorTangents = true;
                if (GUILayout.Toggle(!MirrorTangents, "Broken/Free", "Button", GUILayout.ExpandWidth(true))) MirrorTangents = false;
                GUILayout.EndHorizontal();

                var autoTangentProp = property.FindPropertyRelative("autoTangent");
                if (autoTangentProp != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool autoTVal = GUILayout.Toggle(autoTangentProp.boolValue, " Auto Tangents", GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "Toggle Auto Tangents");
                        autoTangentProp.boolValue = autoTVal;
                        if (autoTVal)
                        {

                            string parentPath = property.propertyPath;
                            object splineDataObj = GetFieldValue(property.serializedObject.targetObject, parentPath);
                            if (splineDataObj != null)
                            {
                                var method = splineDataObj.GetType().GetMethod("RecalculateAllTangents");
                                if (method != null)
                                {
                                    method.Invoke(splineDataObj, null);
                                }
                            }
                        }
                        property.serializedObject.ApplyModifiedProperties();
                        RepaintAllViews();
                    }
                }
                GUILayout.Space(6);
            }

            if (selectedIdx >= 0 && selectedIdx < pointsProp.arraySize)
            {
                GUILayout.Label($"Selected Knot [{selectedIdx}]", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("🗑️ Delete Node", GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "Delete Spline Point");

                    if (type == SplineType.CubicBezier)
                    {
                        pointsProp.DeleteArrayElementAtIndex(selectedIdx);
                        if (tangentsProp != null && tangentsProp.arraySize > selectedIdx * 2 + 1)
                        {
                            tangentsProp.DeleteArrayElementAtIndex(selectedIdx * 2 + 1);
                            tangentsProp.DeleteArrayElementAtIndex(selectedIdx * 2);
                        }
                    }
                    else
                    {
                        pointsProp.DeleteArrayElementAtIndex(selectedIdx);
                    }

                    if (selectedIdxProp != null) selectedIdxProp.intValue = -1;
                    SyncTransformControlPointsIfPresent(pointsProp);
                    property.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    RepaintAllViews();
                }

                if (GUILayout.Button("Deselect", GUILayout.ExpandWidth(true)))
                {
                    if (selectedIdxProp != null) selectedIdxProp.intValue = -1;
                    RepaintAllViews();
                }

                GUILayout.EndHorizontal();

                if (type == SplineType.CubicBezier)
                {
                    GUILayout.Space(2);
                    if (GUILayout.Button("🔄 Reset Selected Point Controls", GUILayout.ExpandWidth(true)))
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "Reset Selected Point Controls");
                        string parentPath = property.propertyPath;
                        object splineDataObj = GetFieldValue(property.serializedObject.targetObject, parentPath);
                        if (splineDataObj != null)
                        {
                            var autoCalcMethod = splineDataObj.GetType().GetMethod("AutoCalculateTangents");
                            if (autoCalcMethod != null)
                            {
                                autoCalcMethod.Invoke(splineDataObj, new object[] { selectedIdx });
                            }
                        }
                        property.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                        RepaintAllViews();
                    }
                }
            }
            else
            {
                GUILayout.Label("💡 Extension Tip:", EditorStyles.boldLabel);
                var tipStyle = new GUIStyle(GUI.skin.label);
                tipStyle.wordWrap = true;
                tipStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f, 1f);
                GUILayout.Label("Hold Shift and Left-Click anywhere in Scene to extend spline or place new points!", tipStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private static void RepaintAllViews()
        {
            SceneView.RepaintAll();
        }

        private static SerializedProperty FindSiblingPointsProperty(SerializedProperty pointsProp, string siblingName)
        {
            if (pointsProp == null || pointsProp.serializedObject == null) return null;

            string path = pointsProp.propertyPath;
            int pointsSuffix = path.LastIndexOf(".points", System.StringComparison.Ordinal);
            if (pointsSuffix < 0) return null;

            int siblingStart = path.LastIndexOf('.', pointsSuffix - 1);
            string prefix = siblingStart >= 0 ? path.Substring(0, siblingStart + 1) : string.Empty;
            string siblingPath = prefix + siblingName + ".points";
            return pointsProp.serializedObject.FindProperty(siblingPath);
        }

        private static bool IsSplinePointsProperty(SerializedProperty pointsProp, string splineName)
        {
            if (pointsProp == null) return false;
            return pointsProp.propertyPath.EndsWith("." + splineName + ".points", System.StringComparison.Ordinal) ||
                   pointsProp.propertyPath == splineName + ".points";
        }

        private static void SyncTransformControlPointsIfPresent(SerializedProperty pointsProp)
        {
            if (pointsProp == null || pointsProp.serializedObject == null) return;

            SerializedProperty controlPoints = FindSiblingControlPointsProperty(pointsProp);
            if (controlPoints == null || !controlPoints.isArray) return;

            SerializedProperty positionPoints = FindSiblingPointsProperty(pointsProp, "positional");
            SerializedProperty rotationPoints = FindSiblingPointsProperty(pointsProp, "rotational");
            SerializedProperty scalePoints = FindSiblingPointsProperty(pointsProp, "scaling");

            int count = MaxInt(positionPoints?.arraySize ?? 0, rotationPoints?.arraySize ?? 0, scalePoints?.arraySize ?? 0);
            controlPoints.arraySize = count;

            for (int i = 0; i < count; i++)
            {
                var point = controlPoints.GetArrayElementAtIndex(i);
                point.FindPropertyRelative("position").SetVector3Val(ReadPoint(positionPoints, i, Vector3.zero));
                point.FindPropertyRelative("rotation").SetVector3Val(ReadPoint(rotationPoints, i, Vector3.zero));
                point.FindPropertyRelative("scale").SetVector3Val(ReadPoint(scalePoints, i, Vector3.one));
            }
        }

        private static SerializedProperty FindSiblingControlPointsProperty(SerializedProperty pointsProp)
        {
            string path = pointsProp.propertyPath;
            int pointsSuffix = path.LastIndexOf(".points", System.StringComparison.Ordinal);
            if (pointsSuffix < 0) return null;

            int siblingStart = path.LastIndexOf('.', pointsSuffix - 1);
            string prefix = siblingStart >= 0 ? path.Substring(0, siblingStart + 1) : string.Empty;
            return pointsProp.serializedObject.FindProperty(prefix + "controlPoints");
        }

        private static Vector3 ReadPoint(SerializedProperty pointsProp, int index, Vector3 fallback)
        {
            if (pointsProp == null || index < 0 || index >= pointsProp.arraySize) return fallback;
            return pointsProp.GetArrayElementAtIndex(index).GetVector3Val();
        }

        private static int MaxInt(int a, int b, int c)
        {
            return Mathf.Max(Mathf.Max(a, b), c);
        }

        private static object GetFieldValue(object source, string path)
        {
            if (source == null) return null;

            object current = source;
            var parts = path.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (part == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
                {
                    string dataIndexStr = parts[i + 1].Substring(5, parts[i + 1].Length - 6);
                    if (int.TryParse(dataIndexStr, out int index))
                    {
                        if (current is System.Collections.IList list && index >= 0 && index < list.Count)
                        {
                            current = list[index];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    i++;
                    continue;
                }

                var type = current.GetType();
                var field = GetFieldIncludingBaseTypes(type, part);
                if (field == null)
                {

                    var prop = type.GetProperty(part, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prop == null) return null;
                    current = prop.GetValue(current);
                }
                else
                {
                    current = field.GetValue(current);
                }
            }

            return current;
        }

        private static System.Reflection.FieldInfo GetFieldIncludingBaseTypes(System.Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

    }
}
