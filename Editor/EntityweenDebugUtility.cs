using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween.Editor
{
    [InitializeOnLoad]
    internal static class EntityweenDebugUtility
    {
        private const int PathSampleCount = 48;
        private const float PathVisualYOffset = 0.35f;
        private static readonly Color PathColor = new(0.22f, 0.78f, 1f, 0.92f);
        private static readonly Color CurrentColor = new(0.3f, 1f, 0.48f, 1f);
        private static readonly Color EndColor = new(1f, 0.8f, 0.3f, 1f);

        static EntityweenDebugUtility()
        {
            SceneView.duringSceneGui -= DrawVisualizedTweens;
            SceneView.duringSceneGui += DrawVisualizedTweens;
        }

        public static bool TryGetEntityManager(out EntityManager entityManager)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                entityManager = default;
                return false;
            }

            entityManager = world.EntityManager;
            return true;
        }

        public static void PingEntity(Entity entity)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var proxyType = Type.GetType("Unity.Entities.Editor.EntitySelectionProxy, Unity.Entities.Editor");
            var selectMethod = proxyType?.GetMethod("SelectEntity", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (selectMethod != null)
            {
                selectMethod.Invoke(null, new object[] { world, entity });
                return;
            }

            Debug.Log($"Selected Entity: {entity.Index}:{entity.Version}");
        }

        private static void DrawVisualizedTweens(SceneView sceneView)
        {
            if (!EditorApplication.isPlaying) return;
            if (!TryGetEntityManager(out var em)) return;

            using var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<TweenDebugVisualize>(),
                ComponentType.ReadOnly<TweenControl>(),
                ComponentType.ReadOnly<PlaybackProgress>(),
                ComponentType.ReadOnly<TweenRange<float3>>(),
                ComponentType.ReadOnly<TweenRuntime<float3>>());

            if (query.IsEmptyIgnoreFilter) return;

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var ghostEntity = entities[i];
                if (!em.Exists(ghostEntity)) continue;

                var debug = em.GetComponentData<TweenDebugVisualize>(ghostEntity);
                if (debug.TweenType != TweenType.MoveTo) continue;

                DrawMovePath(em, ghostEntity, debug.TargetEntity);
            }
        }

        private static void DrawMovePath(EntityManager em, Entity ghostEntity, Entity targetEntity)
        {
            var value = em.GetComponentData<TweenRuntime<float3>>(ghostEntity);
            var range = em.GetComponentData<TweenRange<float3>>(ghostEntity);
            var progress = em.GetComponentData<PlaybackProgress>(ghostEntity);

            var points = CollectPathPoints(em, ghostEntity, range);
            if (points.Count < 2) return;

            for (int i = 0; i < points.Count; i++)
                points[i] = ToWorldPosition(em, ghostEntity, targetEntity, points[i]) + Vector3.up * PathVisualYOffset;

            Handles.color = PathColor;
            Handles.DrawAAPolyLine(4f, points.ToArray());

            Handles.color = CurrentColor;
            var current = ToWorldPosition(em, ghostEntity, targetEntity, value.CurrentValue) + Vector3.up * PathVisualYOffset;
            var currentSize = HandleUtility.GetHandleSize(current) * 0.08f;
            Handles.SphereHandleCap(0, current, Quaternion.identity, currentSize, EventType.Repaint);

            Handles.color = EndColor;
            var end = points[^1];
            var endSize = HandleUtility.GetHandleSize(end) * 0.07f;
            Handles.SphereHandleCap(0, end, Quaternion.identity, endSize, EventType.Repaint);

            Handles.Label(current, $"{progress.NormalizedTime * 100f:F0}%");
        }

        private static List<Vector3> CollectPathPoints(EntityManager em, Entity ghostEntity, TweenRange<float3> range)
        {
            var points = new List<Vector3>(PathSampleCount + 1);
            if (em.HasComponent<SplineBlobRef<float3>>(ghostEntity))
            {
                var blobRef = em.GetComponentData<SplineBlobRef<float3>>(ghostEntity).Blob;
                if (blobRef.IsCreated)
                {
                    for (int i = 0; i <= PathSampleCount; i++)
                    {
                        float t = i / (float)PathSampleCount;
                        float3 result = default;
                        Spline.Sample(blobRef, t, ref result);
                        points.Add((Vector3)result);
                    }
                }
            }
            else if (em.HasComponent<SplineState>(ghostEntity) && em.HasBuffer<SplineElement<float3>>(ghostEntity))
            {
                var state = em.GetComponentData<SplineState>(ghostEntity);
                var buffer = em.GetBuffer<SplineElement<float3>>(ghostEntity, true);
                for (int i = 0; i <= PathSampleCount; i++)
                {
                    float t = i / (float)PathSampleCount;
                    float3 result = default;
                    Spline.Sample(state, buffer, t, ref result);
                    points.Add((Vector3)result);
                }
            }
            else
            {
                points.Add((Vector3)range.StartPoint);
                points.Add((Vector3)range.EndPoint);
            }

            return points;
        }

        private static Vector3 ToWorldPosition(EntityManager em, Entity ghostEntity, Entity targetEntity, float3 position)
        {
            if (!em.Exists(targetEntity)) return position;

            var space = TweenSpace.Local;
            if (em.Exists(ghostEntity) && em.HasComponent<TweenTarget>(ghostEntity))
                space = em.GetComponentData<TweenTarget>(ghostEntity).Space;

            if (space == TweenSpace.World) return position;

            if (em.HasComponent<Parent>(targetEntity))
            {
                var parent = em.GetComponentData<Parent>(targetEntity).Value;
                if (em.Exists(parent) && em.HasComponent<LocalToWorld>(parent))
                    return math.transform(em.GetComponentData<LocalToWorld>(parent).Value, position);
            }

            return position;
        }
    }
}
