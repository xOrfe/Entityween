using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace Entityween.Samples
{
    public class EntityweenSubSceneSwitcher : MonoBehaviour
    {
        private enum SceneMode
        {
            Showcase,
            EaseGallery,
            Benchmark
        }

        [SerializeField] private SubScene showcaseSubScene;
        [SerializeField] private SubScene easeGallerySubScene;
        [SerializeField] private SubScene benchmarkSubScene;
        [SerializeField] private MonoBehaviour[] showcaseBehaviours;
        [SerializeField] private MonoBehaviour[] benchmarkBehaviours;

        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;
        private SceneMode currentMode;

        private void Awake()
        {
            ShowShowcase();
        }

        private void OnGUI()
        {
            EnsureStyles();

            const float panelWidth = 380f;
            const float panelHeight = 104f;
            const float bottomMargin = 20f;

            float width = Mathf.Min(panelWidth, Screen.width - 30f);
            float height = panelHeight;

            Rect panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - height - bottomMargin,
                width,
                height
            );

            float buttonWidth = (panelRect.width - 50f) / 3f;
            float buttonY = panelRect.y + 48f;

            GUI.Box(panelRect, "Entityween Scenes", panelStyle);

            if (GUI.Button(
                    new Rect(panelRect.x + 15f, buttonY, buttonWidth, 34f),
                    "Showcase",
                    IsActive(SceneMode.Showcase) ? activeButtonStyle : buttonStyle))
            {
                ShowShowcase();
            }

            if (GUI.Button(
                    new Rect(panelRect.x + 25f + buttonWidth, buttonY, buttonWidth, 34f),
                    "Eases",
                    IsActive(SceneMode.EaseGallery) ? activeButtonStyle : buttonStyle))
            {
                ShowEaseGallery();
            }

            if (GUI.Button(
                    new Rect(panelRect.x + 35f + buttonWidth * 2f, buttonY, buttonWidth, 34f),
                    "Benchmark",
                    IsActive(SceneMode.Benchmark) ? activeButtonStyle : buttonStyle))
            {
                ShowBenchmark();
            }
        }

        public void ShowShowcase()
        {
            currentMode = SceneMode.Showcase;
            ClearBenchmarkEntities();
            SetMode(showcaseSubScene, true, showcaseBehaviours);
            SetMode(easeGallerySubScene, false, null);
            SetMode(benchmarkSubScene, false, benchmarkBehaviours);
            SetCamera(new Vector3(0f, 18f, -42f), Quaternion.Euler(25f, 0f, 0f));
        }

        public void ShowEaseGallery()
        {
            currentMode = SceneMode.EaseGallery;
            ClearBenchmarkEntities();
            SetMode(showcaseSubScene, false, showcaseBehaviours);
            SetMode(easeGallerySubScene, true, null);
            SetMode(benchmarkSubScene, false, benchmarkBehaviours);
            SetCamera(new Vector3(0f, 23f, -55f), Quaternion.Euler(25f, 0f, 0f));
        }

        public void ShowBenchmark()
        {
            currentMode = SceneMode.Benchmark;
            SetMode(showcaseSubScene, false, showcaseBehaviours);
            SetMode(easeGallerySubScene, false, null);
            SetMode(benchmarkSubScene, true, benchmarkBehaviours);
            SetCamera(new Vector3(0f, 40f, -120f), Quaternion.Euler(20f, 0f, 0f));
        }

        private bool IsActive(SceneMode mode)
        {
            return currentMode == mode;
        }

        private static void SetMode(SubScene subScene, bool active, MonoBehaviour[] behaviours)
        {
            if (subScene != null)
            {
                subScene.AutoLoadScene = active;
                subScene.gameObject.SetActive(active);
            }

            if (behaviours == null) return;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = active;
                }
            }
        }

        private static void ClearBenchmarkEntities()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(typeof(BenchmarkTag));
            if (!query.IsEmptyIgnoreFilter)
            {
                em.DestroyEntity(query);
            }
        }

        private static void SetCamera(Vector3 position, Quaternion rotation)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            mainCamera.transform.SetPositionAndRotation(position, rotation);
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };
            panelStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            activeButtonStyle = new GUIStyle(buttonStyle);
            activeButtonStyle.normal.textColor = Color.cyan;
        }
    }
}
