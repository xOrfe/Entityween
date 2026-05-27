using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public class ShowcaseRuntimeOrbit : MonoBehaviour
    {
        private const int OrbitPointCount = 48;
        private const int OrbitEntityCount = 18;

        private readonly List<Entity> spawnedEntities = new();
        private LineRenderer orbitLine;
        private Mesh[] orbitMeshes;
        private Material[] orbitMaterials;
        private RenderMeshArray renderMeshArray;
        private bool initialized;

        private static readonly float3[] OrbitPoints = CreateOrbitPoints();

        private void OnEnable()
        {
            initialized = false;
        }

        private void Update()
        {
            AnimateOrbitLine();

            if (!initialized && World.DefaultGameObjectInjectionWorld != null)
            {
                initialized = true;
                CreateOrbitSpline();
                CreateOrbitEntities();
            }
        }

        private void CreateOrbitSpline()
        {
            var lineGo = new GameObject("Runtime Neon Orbit Spline");
            lineGo.transform.SetParent(transform, false);
            orbitLine = lineGo.AddComponent<LineRenderer>();
            orbitLine.useWorldSpace = true;
            orbitLine.loop = true;
            orbitLine.positionCount = OrbitPoints.Length;
            orbitLine.widthMultiplier = 0.26f;
            orbitLine.numCornerVertices = 12;
            orbitLine.numCapVertices = 12;
            orbitLine.shadowCastingMode = ShadowCastingMode.Off;
            orbitLine.receiveShadows = false;

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            material.color = Color.white;
            orbitLine.sharedMaterial = material;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.4f, 0.25f, 1f), 0.18f),
                    new GradientColorKey(new Color(1f, 0.15f, 0.85f), 0.38f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.1f), 0.62f),
                    new GradientColorKey(new Color(0.1f, 1f, 0.45f), 0.82f),
                    new GradientColorKey(new Color(0f, 0.95f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            orbitLine.colorGradient = gradient;

            for (int i = 0; i < OrbitPoints.Length; i++)
            {
                orbitLine.SetPosition(i, OrbitPoints[i]);
            }
        }

        private void AnimateOrbitLine()
        {
            if (orbitLine == null) return;

            float pulse = 0.22f + math.sin(Time.time * 4.25f) * 0.07f;
            orbitLine.widthMultiplier = pulse;
            for (int i = 0; i < OrbitPoints.Length; i++)
            {
                float angle = i / (float)OrbitPoints.Length * math.PI * 2f;
                float radialBreath = math.sin(Time.time * 0.85f + angle * 3f) * 1.15f;
                var point = OrbitPoints[i];
                var radial = math.normalizesafe(new float3(point.x, 0f, point.z));
                point += radial * radialBreath;
                point.y += math.sin(Time.time * 1.9f + angle * 5f) * 1.15f;
                orbitLine.SetPosition(i, point);
            }

            if (orbitLine.sharedMaterial != null)
            {
                orbitLine.sharedMaterial.mainTextureOffset = new Vector2(-Time.time * 0.5f, 0f);
            }
        }

        private void CreateOrbitEntities()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;

            orbitMeshes = new[]
            {
                CreatePrimitiveMesh(PrimitiveType.Sphere),
                CreatePrimitiveMesh(PrimitiveType.Capsule),
                CreatePrimitiveMesh(PrimitiveType.Cube),
                CreatePrimitiveMesh(PrimitiveType.Cylinder)
            };

            orbitMaterials = new Material[OrbitEntityCount];
            for (int i = 0; i < orbitMaterials.Length; i++)
            {
                orbitMaterials[i] = CreateMaterial($"Orbit Prism {i:00}", Color.HSVToRGB(i / (float)orbitMaterials.Length, 0.86f, 1f));
            }

            renderMeshArray = new RenderMeshArray(orbitMaterials, orbitMeshes);
            var renderDesc = new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false);

            for (int i = 0; i < OrbitEntityCount; i++)
            {
                int materialIndex = i % orbitMaterials.Length;
                int meshIndex = i % orbitMeshes.Length;
                int pointIndex = i * OrbitPoints.Length / OrbitEntityCount;
                float scale = 0.7f + (i % 6) * 0.16f;
                float moveDuration = 8.5f + (i % 7) * 0.65f;

                var entity = em.CreateEntity();
                RenderMeshUtility.AddComponents(
                    entity,
                    em,
                    renderDesc,
                    renderMeshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(materialIndex, meshIndex));

                em.AddComponentData(entity, LocalTransform.FromPositionRotationScale(
                    OrbitPoints[pointIndex],
                    quaternion.identity,
                    scale));
                em.AddComponentData(entity, new LocalToWorld());

                float3[] shiftedPoints = ShiftedOrbitPoints(pointIndex, i % 2 == 1);
                using (var orbitPath = new NativeArray<float3>(shiftedPoints, Allocator.Temp))
                {
                    entity.MoveToWorld(OrbitPoints[pointIndex], moveDuration)
                        .Along(orbitPath, SplineType.CatmullRom, isClosed: true)
                        .Ease(EaseType.Linear)
                        .Loop(LoopType.Repeat)
                        .Play(em);
                }

                entity.RotateToLocal(quaternion.EulerXYZ(new float3(math.PI * 0.85f, math.PI * 1.25f, math.PI * 0.55f)), 1.4f + (i % 5) * 0.22f)
                    .From(quaternion.identity)
                    .Ease(EaseType.Linear)
                    .Loop(LoopType.Repeat)
                    .Play(em);

                entity.ScaleTo(new float3(scale * (1.25f + (i % 3) * 0.12f)), 1.8f + (i % 4) * 0.25f)
                    .From(new float3(scale))
                    .Ease(EaseType.InOutSine)
                    .Loop(LoopType.PingPong)
                    .Play(em);

                spawnedEntities.Add(entity);
            }
        }

        private static float3[] ShiftedOrbitPoints(int offset, bool reverse)
        {
            var shifted = new float3[OrbitPoints.Length];
            for (int i = 0; i < OrbitPoints.Length; i++)
            {
                int sourceIndex = reverse
                    ? (offset - i + OrbitPoints.Length * 2) % OrbitPoints.Length
                    : (i + offset) % OrbitPoints.Length;
                shifted[i] = OrbitPoints[sourceIndex];
            }

            return shifted;
        }

        private static float3[] CreateOrbitPoints()
        {
            var points = new float3[OrbitPointCount];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)points.Length;
                float angle = t * math.PI * 2f;
                float radius = 33f
                    + math.sin(angle * 3f) * 2.8f
                    + math.cos(angle * 7f) * 1.4f;
                float x = math.sin(angle) * radius;
                float z = math.cos(angle) * (radius + math.sin(angle * 5f) * 1.8f);
                float y = 4.2f
                    + math.sin(angle * 4f) * 2.2f
                    + math.cos(angle * 9f) * 0.75f;

                points[i] = new float3(x, y, z);
            }

            return points;
        }

        private static Mesh CreatePrimitiveMesh(PrimitiveType primitiveType)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            return mesh;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
            material.enableInstancing = true;
            return material;
        }

        private void OnDisable()
        {
            CleanupRuntimeObjects();
            initialized = false;
        }

        private void OnDestroy()
        {
            CleanupRuntimeObjects();
        }

        private void CleanupRuntimeObjects()
        {
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                foreach (var entity in spawnedEntities)
                {
                    if (em.Exists(entity))
                    {
                        em.DestroyEntity(entity);
                    }
                }
            }

            spawnedEntities.Clear();

            if (orbitLine != null)
            {
                Destroy(orbitLine.gameObject);
                orbitLine = null;
            }
        }
    }
}
