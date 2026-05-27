using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace Entityween.Samples
{
    public class EntityweenShowcaseSwitcher : MonoBehaviour
    {
        public enum SceneMode
        {
            Showcase,
            EaseGallery,
            Benchmark
        }

        public enum PlaybackType
        {
            Entities,
            GameObjects
        }

        private const float PanelWidth = 380f;
        private const float PanelHeight = 140f;
        private const float BottomMargin = 20f;

        [Header("Entity SubScenes")]
        [SerializeField] private SubScene showcaseSubScene;
        [SerializeField] private SubScene easeGallerySubScene;
        [SerializeField] private SubScene benchmarkSubScene;

        [Header("GameObject Roots")]
        [SerializeField] private GameObject showcaseRoot;
        [SerializeField] private GameObject easeGalleryRoot;
        [SerializeField] private GameObject benchmarkRoot;

        [Header("Shared Behaviours")]
        [SerializeField] private MonoBehaviour[] showcaseBehaviours;
        [SerializeField] private MonoBehaviour[] benchmarkBehaviours;
        [SerializeField] private MonoBehaviour[] goBenchmarkBehaviours;

        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;

        private SceneMode currentMode = (SceneMode)(-1);
        private PlaybackType currentType = PlaybackType.Entities;

        private void Awake()
        {
            SetState(SceneMode.Showcase, PlaybackType.Entities, force: true);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float width = Mathf.Min(PanelWidth, Screen.width - 30f);
            Rect panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - PanelHeight - BottomMargin,
                width,
                PanelHeight
            );

            GUI.Box(panelRect, "Entityween Showcase", panelStyle);

            float toggleWidth = (panelRect.width - 30f) * 0.5f;
            float toggleY = panelRect.y + 40f;

            if (GUI.Button(new Rect(panelRect.x + 10f, toggleY, toggleWidth, 30f), "Entities (DOTS)", currentType == PlaybackType.Entities ? activeButtonStyle : buttonStyle))
            {
                SetState(currentMode, PlaybackType.Entities);
            }
            if (GUI.Button(new Rect(panelRect.x + 20f + toggleWidth, toggleY, toggleWidth, 30f), "GameObjects", currentType == PlaybackType.GameObjects ? activeButtonStyle : buttonStyle))
            {
                SetState(currentMode, PlaybackType.GameObjects);
            }

            float buttonWidth = (panelRect.width - 40f) / 3f;
            float buttonY = panelRect.y + 85f;

            DrawModeButton(new Rect(panelRect.x + 10f, buttonY, buttonWidth, 34f), "Showcase", SceneMode.Showcase);
            DrawModeButton(new Rect(panelRect.x + 20f + buttonWidth, buttonY, buttonWidth, 34f), "Eases", SceneMode.EaseGallery);
            DrawModeButton(new Rect(panelRect.x + 30f + buttonWidth * 2f, buttonY, buttonWidth, 34f), "Benchmark", SceneMode.Benchmark);
        }

        private void DrawModeButton(Rect rect, string label, SceneMode mode)
        {
            if (GUI.Button(rect, label, currentMode == mode ? activeButtonStyle : buttonStyle))
            {
                SetState(mode, currentType);
            }
        }

        public void SetState(SceneMode mode, PlaybackType type, bool force = false)
        {
            if (!force && currentMode == mode && currentType == type) return;

            currentMode = mode;
            currentType = type;

            ClearAllActiveStates();

            ConfigureCamera(mode);
            
            SetGameObjectActive(showcaseRoot, false);
            SetGameObjectActive(easeGalleryRoot, false);
            SetGameObjectActive(benchmarkRoot, false);
            UnloadSubScene(showcaseSubScene);
            UnloadSubScene(easeGallerySubScene);
            UnloadSubScene(benchmarkSubScene);
            
            if (type == PlaybackType.Entities)
            {
                switch (mode)
                {
                    case SceneMode.Showcase:
                        LoadSubScene(showcaseSubScene);
                        SetBehaviours(showcaseBehaviours, true);
                        break;
                    case SceneMode.EaseGallery:
                        LoadSubScene(easeGallerySubScene);
                        break;
                    case SceneMode.Benchmark:
                        LoadSubScene(benchmarkSubScene);
                        SetBehaviours(benchmarkBehaviours, true);
                        break;
                }
            }
            else
            {
                switch (mode)
                {
                    case SceneMode.Showcase:
                        SetGameObjectActive(showcaseRoot, true);
                        SetBehaviours(showcaseBehaviours, true);
                        break;
                    case SceneMode.EaseGallery:
                        SetGameObjectActive(easeGalleryRoot, true);
                        break;
                    case SceneMode.Benchmark:
                        SetGameObjectActive(benchmarkRoot, true);
                        SetBehaviours(goBenchmarkBehaviours, true);
                        break;
                }
            }
        }

        private void ClearAllActiveStates()
        {
            ClearBenchmarkEntities();

            SetBehaviours(showcaseBehaviours, false);
            SetBehaviours(benchmarkBehaviours, false);
            SetBehaviours(goBenchmarkBehaviours, false);

            UnloadSubScene(showcaseSubScene);
            UnloadSubScene(easeGallerySubScene);
            UnloadSubScene(benchmarkSubScene);
        }

        private void ConfigureCamera(SceneMode mode)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            switch (mode)
            {
                case SceneMode.Showcase:
                    mainCamera.transform.SetPositionAndRotation(new Vector3(0f, 18f, -42f), Quaternion.Euler(25f, 0f, 0f));
                    mainCamera.farClipPlane = 1000f;
                    SetShadowsEnabled(mainCamera, true);
                    break;
                case SceneMode.EaseGallery:
                    mainCamera.transform.SetPositionAndRotation(new Vector3(0f, 23f, -55f), Quaternion.Euler(25f, 0f, 0f));
                    mainCamera.farClipPlane = 1000f;
                    SetShadowsEnabled(mainCamera, true);
                    break;
                case SceneMode.Benchmark:
                    mainCamera.transform.SetPositionAndRotation(new Vector3(0f, 40f, -120f), Quaternion.Euler(20f, 0f, 0f));
                    mainCamera.farClipPlane = 300f;
                    SetShadowsEnabled(mainCamera, false);
                    break;
            }
        }

        private static void SetShadowsEnabled(Camera camera, bool enabled)
        {
#if UNITY_2019_3_OR_NEWER
            var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                cameraData.renderShadows = enabled;
            }
#endif
        }

        private static void SetGameObjectActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static void LoadSubScene(SubScene subScene)
        {
            if (subScene != null)
            {
                subScene.gameObject.SetActive(true);
            }
        }

        private static void UnloadSubScene(SubScene subScene)
        {
            if (subScene != null)
            {
                subScene.gameObject.SetActive(false);
            }
        }

        private static void SetBehaviours(MonoBehaviour[] behaviours, bool enabled)
        {
            if (behaviours == null) return;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = enabled;
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
