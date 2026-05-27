using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public class GameObjectBenchmark : MonoBehaviour
    {
        private enum BenchmarkTweenType
        {
            Move,
            Rotate,
            Scale,
            Mixed
        }

        private static readonly int[] SpawnCounts = { 1000, 5000, 10000, 50000 };
        private static readonly string[] SpawnLabels = { "1k", "5k", "10k", "50k" };
        private static readonly BenchmarkTweenType[] TweenTypes =
        {
            BenchmarkTweenType.Move,
            BenchmarkTweenType.Rotate,
            BenchmarkTweenType.Scale,
            BenchmarkTweenType.Mixed
        };

        [Header("Prefab Reference")]
        public GameObject prefab;

        private int selectedCount = 1000;
        private BenchmarkTweenType selectedTweenType = BenchmarkTweenType.Move;
        private int spawnedCount = 0;
        private float fps = 0.0f;
        private float fpsTimer = 0f;
        private int fpsFrames = 0;

        private readonly List<GameObject> spawnedObjects = new();
        private readonly List<Entity> tweenEntities = new();

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle btnStyle;
        private GUIStyle activeBtnStyle;
        private GUIStyle runBtnStyle;

        private void Start()
        {
            RunBenchmark();
        }

        private void OnEnable()
        {
            RunBenchmark();
        }

        private void OnDisable()
        {
            ClearBenchmark();
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

            GUI.Box(new Rect(15, 15, 240, 310), "GO Tween Benchmark", titleStyle);

            GUI.Label(new Rect(25, 45, 220, 20), $"Active GameObjects: {spawnedCount}", labelStyle);
            GUI.Label(new Rect(25, 65, 220, 20), $"FPS: {fps:F1}", labelStyle);

            GUI.Label(new Rect(25, 95, 220, 20), "Spawn Count (WARNING: GOs slow):", labelStyle);
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
            ClearBenchmark();

            if (prefab == null)
            {
                Debug.LogWarning("Benchmark prefab not assigned!");
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("DOTS Default World not initialized yet.");
                return;
            }
            var em = world.EntityManager;

            int count = selectedCount;
            float3 range = new float3(120f, 60f, 120f);

            for (int i = 0; i < count; i++)
            {
                float3 startPos = new float3(
                    UnityEngine.Random.Range(-range.x, range.x),
                    UnityEngine.Random.Range(2f, range.y),
                    UnityEngine.Random.Range(-range.z, range.z)
                );

                var go = Instantiate(prefab);
                if (go.TryGetComponent<Collider>(out var col))
                {
                    Destroy(col);
                }
                if (go.TryGetComponent<Renderer>(out var ren))
                {
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    ren.receiveShadows = false;
                }
                float3 worldStartPos = (float3)transform.position + startPos;
                go.transform.SetPositionAndRotation(worldStartPos, Quaternion.identity);
                spawnedObjects.Add(go);

                float duration = UnityEngine.Random.Range(1.5f, 4.0f);
                Entity tweenEntity = PlayBenchmarkTween(em, go.transform, worldStartPos, duration, PickTweenType());
                if (tweenEntity != Entity.Null)
                {
                    tweenEntities.Add(tweenEntity);
                }
            }

            spawnedCount = count;
        }

        private BenchmarkTweenType PickTweenType()
        {
            return selectedTweenType == BenchmarkTweenType.Mixed
                ? TweenTypes[UnityEngine.Random.Range(0, 3)]
                : selectedTweenType;
        }

        private static Entity PlayBenchmarkTween(EntityManager em, Transform target, float3 startPosition, float duration, BenchmarkTweenType tweenType)
        {
            switch (tweenType)
            {
                case BenchmarkTweenType.Move:
                    float3 offset = new float3(0f, UnityEngine.Random.Range(4f, 15f), 0f);
                    return target.MoveTo(startPosition + offset, duration)
                        .From(startPosition)
                        .Ease(EaseType.InOutSine)
                        .Loop(LoopType.PingPong)
                        .Play(em);

                case BenchmarkTweenType.Rotate:
                    return target.RotateTo(quaternion.RotateY(math.PI * 0.99f), duration)
                        .From(quaternion.identity)
                        .Ease(EaseType.Linear)
                        .Loop(LoopType.Repeat)
                        .Play(em);

                case BenchmarkTweenType.Scale:
                    return target.ScaleTo(new float3(UnityEngine.Random.Range(1.5f, 3.0f)), duration)
                        .From(new float3(1f))
                        .Ease(EaseType.InOutBack)
                        .Loop(LoopType.PingPong)
                        .Play(em);

                default:
                    return Entity.Null;
            }
        }

        private void ClearBenchmark()
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
            spawnedCount = 0;
        }
    }
}
