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
        private enum BenchmarkTweenType
        {
            Move,
            Rotate,
            Scale,
            Mixed
        }

        private static readonly int[] SpawnCounts = { 1000, 10000, 50000, 100000 };
        private static readonly string[] SpawnLabels = { "1k", "10k", "50k", "100k" };
        private static readonly BenchmarkTweenType[] TweenTypes =
        {
            BenchmarkTweenType.Move,
            BenchmarkTweenType.Rotate,
            BenchmarkTweenType.Scale,
            BenchmarkTweenType.Mixed
        };

        private int selectedCount = 10000;
        private BenchmarkTweenType selectedTweenType = BenchmarkTweenType.Move;
        private int spawnedCount = 0;
        private float fps = 0.0f;
        private float fpsTimer = 0f;
        private int fpsFrames = 0;
        private bool pendingInitialRun = true;

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

            GUI.Box(new Rect(15, 15, 240, 310), "Entityween Benchmark", titleStyle);

            GUI.Label(new Rect(25, 45, 220, 20), $"Active Tweens: {spawnedCount}", labelStyle);
            GUI.Label(new Rect(25, 65, 220, 20), $"FPS: {fps:F1}", labelStyle);

            GUI.Label(new Rect(25, 95, 220, 20), "Spawn Count:", labelStyle);
            DrawSpawnCountButtons(new Rect(25, 115, 204, 24));

            GUI.Label(new Rect(25, 150, 220, 20), "Tween Type:", labelStyle);
            DrawTweenTypeButtons(new Rect(25, 170, 210, 54));

            if (GUI.Button(new Rect(25, 250, 220, 40), "RUN BENCHMARK", runBtnStyle))
            {
                RunBenchmark();
            }
        }

        private void DrawSpawnCountButtons(Rect rect)
        {
            const float buttonWidth = 48f;
            const float gap = 4f;

            for (int i = 0; i < SpawnCounts.Length; i++)
            {
                var buttonRect = new Rect(rect.x + i * (buttonWidth + gap), rect.y, buttonWidth, rect.height);
                if (GUI.Button(buttonRect, SpawnLabels[i], selectedCount == SpawnCounts[i] ? activeBtnStyle : btnStyle))
                {
                    selectedCount = SpawnCounts[i];
                }
            }
        }

        private void DrawTweenTypeButtons(Rect rect)
        {
            for (int i = 0; i < TweenTypes.Length; i++)
            {
                var type = TweenTypes[i];
                float x = rect.x + (i % 2) * 110f;
                float y = rect.y + (i / 2) * 30f;
                if (GUI.Button(new Rect(x, y, 100f, 24f), type.ToString(), selectedTweenType == type ? activeBtnStyle : btnStyle))
                {
                    selectedTweenType = type;
                }
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

            ClearSpawnedBenchmarkEntities();

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

            int count = selectedCount;
            NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.TempJob);
            em.Instantiate(settings.Prefab, entities);

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

                float duration = UnityEngine.Random.Range(1.5f, 4.0f);
                PlayBenchmarkTween(em, ent, startPos, duration, PickTweenType());
            }

            spawnedCount = count;
            entities.Dispose();
        }

        private BenchmarkTweenType PickTweenType()
        {
            return selectedTweenType == BenchmarkTweenType.Mixed
                ? TweenTypes[UnityEngine.Random.Range(0, 3)]
                : selectedTweenType;
        }

        private static void PlayBenchmarkTween(EntityManager em, Entity entity, float3 startPosition, float duration, BenchmarkTweenType tweenType)
        {
            switch (tweenType)
            {
                case BenchmarkTweenType.Move:
                    float3 offset = new float3(0f, UnityEngine.Random.Range(4f, 15f), 0f);
                    entity.MoveToLocal(startPosition + offset, duration)
                        .From(startPosition)
                        .Ease(EaseType.InOutSine)
                        .Loop(LoopType.PingPong)
                        .Play(em);
                    break;

                case BenchmarkTweenType.Rotate:
                    entity.RotateToLocal(quaternion.RotateY(math.PI * 0.99f), duration)
                        .From(quaternion.identity)
                        .Ease(EaseType.Linear)
                        .Loop(LoopType.Repeat)
                        .Play(em);
                    break;

                case BenchmarkTweenType.Scale:
                    entity.ScaleTo(new float3(UnityEngine.Random.Range(1.5f, 3.0f)), duration)
                        .From(new float3(1f))
                        .Ease(EaseType.InOutBack)
                        .Loop(LoopType.PingPong)
                        .Play(em);
                    break;
            }
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
