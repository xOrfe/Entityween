using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using XO.Curve;
using XO.Entityween;
using Unity.Transforms;

namespace Entityween.Samples
{
    public struct BenchmarkSettings : IComponentData
    {
        public Entity Prefab;
    }

    public struct BenchmarkTag : IComponentData {}

    public class EntityweenBenchmark : MonoBehaviour
    {
        private int selectedCount = 10000;
        private int selectedType = 0; // 0 = Move, 1 = Rotate, 2 = Scale, 3 = Mixed
        private int spawnedCount = 0;
        private float fps = 0.0f;
        private float fpsTimer = 0f;
        private int fpsFrames = 0;
        private bool pendingInitialRun = true;

        // Custom GUI styles
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle btnStyle;
        private GUIStyle activeBtnStyle;
        private GUIStyle runBtnStyle;

        private void Start()
        {
            pendingInitialRun = true;
        }

        private void OnEnable()
        {
            pendingInitialRun = true;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            ClearSpawnedBenchmarkEntities();
            spawnedCount = 0;
        }

        private void Update()
        {
            fpsTimer += Time.unscaledDeltaTime;
            fpsFrames++;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsFrames / fpsTimer;
                fpsTimer = 0f;
                fpsFrames = 0;
            }

            if (pendingInitialRun && HasBenchmarkSettings())
            {
                pendingInitialRun = false;
                RunBenchmark();
            }
        }

        private Texture2D CreateTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void InitStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.box);
            titleStyle.normal.background = CreateTexture(2, 2, new Color(0.12f, 0.12f, 0.14f, 0.9f));
            titleStyle.normal.textColor = Color.cyan;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.fontSize = 14;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;

            btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.normal.background = CreateTexture(2, 2, new Color(0.2f, 0.2f, 0.22f, 1f));
            btnStyle.normal.textColor = Color.white;
            btnStyle.hover.background = CreateTexture(2, 2, new Color(0.3f, 0.3f, 0.35f, 1f));
            btnStyle.hover.textColor = Color.cyan;

            activeBtnStyle = new GUIStyle(btnStyle);
            activeBtnStyle.normal.background = CreateTexture(2, 2, new Color(0f, 0.55f, 0.8f, 1f));
            activeBtnStyle.normal.textColor = Color.white;

            runBtnStyle = new GUIStyle(btnStyle);
            runBtnStyle.normal.background = CreateTexture(2, 2, new Color(0.1f, 0.65f, 0.25f, 1f));
            runBtnStyle.normal.textColor = Color.white;
            runBtnStyle.fontStyle = FontStyle.Bold;
            runBtnStyle.fontSize = 13;
        }

        private void OnGUI()
        {
            InitStyles();

            // Benchmark UI container
            GUI.Box(new Rect(15, 15, 240, 310), "Entityween Benchmark", titleStyle);

            GUI.Label(new Rect(25, 45, 220, 20), $"Active Tweens: {spawnedCount}", labelStyle);
            GUI.Label(new Rect(25, 65, 220, 20), $"FPS: {fps:F1}", labelStyle);

            GUI.Label(new Rect(25, 95, 220, 20), "Spawn Count:", labelStyle);
            if (GUI.Button(new Rect(25, 115, 48, 24), "1k", selectedCount == 1000 ? activeBtnStyle : btnStyle)) selectedCount = 1000;
            if (GUI.Button(new Rect(77, 115, 48, 24), "10k", selectedCount == 10000 ? activeBtnStyle : btnStyle)) selectedCount = 10000;
            if (GUI.Button(new Rect(129, 115, 48, 24), "50k", selectedCount == 50000 ? activeBtnStyle : btnStyle)) selectedCount = 50000;
            if (GUI.Button(new Rect(181, 115, 48, 24), "100k", selectedCount == 100000 ? activeBtnStyle : btnStyle)) selectedCount = 100000;

            GUI.Label(new Rect(25, 150, 220, 20), "Tween Type:", labelStyle);
            if (GUI.Button(new Rect(25, 170, 100, 24), "Move", selectedType == 0 ? activeBtnStyle : btnStyle)) selectedType = 0;
            if (GUI.Button(new Rect(135, 170, 100, 24), "Rotate", selectedType == 1 ? activeBtnStyle : btnStyle)) selectedType = 1;
            if (GUI.Button(new Rect(25, 200, 100, 24), "Scale", selectedType == 2 ? activeBtnStyle : btnStyle)) selectedType = 2;
            if (GUI.Button(new Rect(135, 200, 100, 24), "Mixed", selectedType == 3 ? activeBtnStyle : btnStyle)) selectedType = 3;

            if (GUI.Button(new Rect(25, 250, 220, 40), "RUN BENCHMARK", runBtnStyle))
            {
                RunBenchmark();
            }
        }

        private void RunBenchmark()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                Debug.LogWarning("DOTS Default World not initialized yet.");
                return;
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            // 1. Clean existing spawned benchmark entities
            ClearSpawnedBenchmarkEntities();

            // 2. Query the prefab settings
            var settingsQuery = em.CreateEntityQuery(typeof(BenchmarkSettings));
            if (!settingsQuery.HasSingleton<BenchmarkSettings>())
            {
                Debug.LogWarning("BenchmarkSettings singleton not found. Make sure the authoring is baked.");
                settingsQuery.Dispose();
                return;
            }

            var settings = settingsQuery.GetSingleton<BenchmarkSettings>();
            settingsQuery.Dispose();

            if (settings.Prefab == Entity.Null)
            {
                Debug.LogWarning("Baked prefab Entity is Null!");
                return;
            }

            // 3. Spawn entities
            int count = selectedCount;
            NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.TempJob);
            em.Instantiate(settings.Prefab, entities);

            // 4. Set positions and apply tweens
            float3 range = new float3(120f, 60f, 120f);
            for (int i = 0; i < entities.Length; i++)
            {
                var ent = entities[i];
                em.AddComponent<BenchmarkTag>(ent);

                float3 startPos = new float3(
                    UnityEngine.Random.Range(-range.x, range.x),
                    UnityEngine.Random.Range(2f, range.y),
                    UnityEngine.Random.Range(-range.z, range.z)
                );

                em.SetComponentData(ent, LocalTransform.FromPosition(startPos));

                // Determine tween type to apply
                int tType = selectedType;
                if (tType == 3) // Mixed
                {
                    tType = UnityEngine.Random.Range(0, 3);
                }

                float duration = UnityEngine.Random.Range(1.5f, 4.0f);

                if (tType == 0) // Move
                {
                    float3 offset = new float3(0f, UnityEngine.Random.Range(4f, 15f), 0f);
                    ent.MoveToLocal(duration, startPos)
                       .To(startPos + offset)
                       .Ease(EaseType.InOutSine)
                       .Loop(LoopType.PingPong)
                       .Play(em);
                }
                else if (tType == 1) // Rotate
                {
                    ent.RotateToLocal(duration, quaternion.identity)
                       .To(quaternion.RotateY(math.PI * 0.99f))
                       .Ease(EaseType.Linear)
                       .Loop(LoopType.Repeat)
                       .Play(em);
                }
                else if (tType == 2) // Scale
                {
                    ent.ScaleTo(duration, new float3(1f))
                       .To(new float3(UnityEngine.Random.Range(1.5f, 3.0f)))
                       .Ease(EaseType.InOutBack)
                       .Loop(LoopType.PingPong)
                       .Play(em);
                }
            }

            spawnedCount = count;
            entities.Dispose();
        }

        private static void ClearSpawnedBenchmarkEntities()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(BenchmarkTag));
            em.DestroyEntity(query);
            query.Dispose();
        }

        private bool HasBenchmarkSettings()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return false;
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var settingsQuery = em.CreateEntityQuery(typeof(BenchmarkSettings));
            bool hasSettings = settingsQuery.HasSingleton<BenchmarkSettings>();
            settingsQuery.Dispose();
            return hasSettings;
        }
    }

}
