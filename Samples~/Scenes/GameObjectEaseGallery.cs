using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public class GameObjectEaseGallery : MonoBehaviour
    {
        private const int Columns = 8;

        [Header("Animation Options")]
        public float duration = 2.0f;
        public float spacingX = 10f;
        public float spacingZ = 8f;
        public float bounceHeight = 3.2f;

        private readonly List<GameObject> spawnedObjects = new();
        private readonly List<Entity> tweenEntities = new();

        private void OnEnable()
        {
            SpawnGallery();
        }

        private void OnDisable()
        {
            ClearGallery();
        }

        private void SpawnGallery()
        {
            ClearGallery();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;

            var easeTypes = (EaseType[])System.Enum.GetValues(typeof(EaseType));
            float hueStep = 1f / Mathf.Max(1, easeTypes.Length);

            for (int i = 0; i < easeTypes.Length; i++)
            {
                EaseType easeType = easeTypes[i];
                Vector3 position = GetGridPosition(i);
                GameObject sphere = CreateEaseSphere(i, easeType, position, hueStep);
                spawnedObjects.Add(sphere);

                float3 startPos = position;
                float3 endPos = startPos + new float3(0f, bounceHeight, 0f);

                Entity tweenEntity = sphere.transform.MoveTo(endPos, duration)
                    .From(startPos)
                    .Ease(easeType)
                    .Loop(LoopType.PingPong)
                    .Play(em);

                tweenEntities.Add(tweenEntity);
            }
        }

        private Vector3 GetGridPosition(int index)
        {
            int column = index % Columns;
            int row = index / Columns;

            float x = (column - (Columns - 1) * 0.5f) * spacingX;
            float z = (1.5f - row) * spacingZ;
            return new Vector3(x, 0.85f, z);
        }

        private GameObject CreateEaseSphere(int index, EaseType easeType, Vector3 position, float hueStep)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"GameObjectEase_{index:00}_{easeType}";
            sphere.transform.SetParent(transform);
            sphere.transform.position = position;

            Color color = Color.HSVToRGB(index * hueStep, 0.78f, 0.95f);
            SetMaterialColor(sphere, color);
            CreateLabel(sphere.transform, easeType.ToString());

            return sphere;
        }

        private static void SetMaterialColor(GameObject go, Color color)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (litShader == null) return;

            var material = new Material(litShader);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateLabel(Transform parent, string text)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent);

            var tmp = labelGo.AddComponent<TMPro.TextMeshPro>();
            tmp.text = text;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize = 5.0f;
            tmp.color = Color.white;

            labelGo.AddComponent<ShowcaseLabelLook>();
        }

        private void ClearGallery()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                foreach (var tweenEntity in tweenEntities)
                {
                    if (tweenEntity != Entity.Null && em.Exists(tweenEntity))
                    {
                        em.DestroyEntity(tweenEntity);
                    }
                }
            }
            tweenEntities.Clear();

            foreach (var go in spawnedObjects)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            spawnedObjects.Clear();
        }

        private class ShowcaseLabelLook : MonoBehaviour
        {
            private void Start()
            {
                transform.localPosition = new Vector3(0f, 1.15f, 0f);
            }

            private void Update()
            {
                if (Camera.main != null)
                {
                    transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
                }
            }
        }
    }
}
