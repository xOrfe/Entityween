#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Entities;
using Unity.Scenes;
using Unity.Mathematics;
using XO.Curve;
using XO.Entityween;
using Entityween.Samples;
using XO.Entityween.Editor;

namespace Entityween.Editor
{
    public static class EntityweenShowcaseSceneBuilder
    {
        [MenuItem("XO/Entityween/Generate Showcase Scene", false, 10)]
        public static void GenerateShowcaseScene()
        {
            string version = EntityweenVersionHelper.Version;
            string outputDir = $"Assets/Samples/Entityween/{version}/Scenes";
            string subSceneDir = $"{outputDir}/SubScenes";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            if (!Directory.Exists(subSceneDir))
            {
                Directory.CreateDirectory(subSceneDir);
            }

            GenerateCombinedShowcaseSceneInternal(outputDir);
            DeleteLegacySceneFiles(outputDir);
            CopyScenesToPackage(outputDir);

            Debug.Log("Successfully generated Entityween combined showcase scene!");
        }

        private static void GenerateCombinedShowcaseSceneInternal(string outputDir)
        {
            string mainScenePath = $"{outputDir}/EntityweenShowcase.unity";
            string showcaseSubScenePath = $"{outputDir}/SubScenes/EntityweenShowcase_Entities.unity";
            string easeGallerySubScenePath = $"{outputDir}/SubScenes/EntityweenEaseGallery_Entities.unity";
            string benchmarkSubScenePath = $"{outputDir}/SubScenes/EntityweenBenchmark_Entities.unity";

            var mainScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            var mainCamera = GameObject.FindWithTag("MainCamera");
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 18f, -42f);
                mainCamera.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            }
            EditorSceneManager.SaveScene(mainScene, mainScenePath);

            var showcaseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            BuildShowcaseEntities(showcaseScene, outputDir);
            EditorSceneManager.SaveScene(showcaseScene, showcaseSubScenePath);
            EditorSceneManager.CloseScene(showcaseScene, true);

            var easeGalleryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            BuildEaseGalleryEntities(easeGalleryScene, outputDir);
            EditorSceneManager.SaveScene(easeGalleryScene, easeGallerySubScenePath);
            EditorSceneManager.CloseScene(easeGalleryScene, true);

            var benchmarkScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            BuildBenchmarkEntities(benchmarkScene, outputDir);
            EditorSceneManager.SaveScene(benchmarkScene, benchmarkSubScenePath);
            EditorSceneManager.CloseScene(benchmarkScene, true);

            EditorSceneManager.SetActiveScene(mainScene);
            
            AssetDatabase.Refresh();

            var showcaseSubSceneObj = CreateSubSceneObject("ShowcaseSubScene", showcaseSubScenePath, true);
            var easeGallerySubSceneObj = CreateSubSceneObject("EaseGallerySubScene", easeGallerySubScenePath, true);
            var benchmarkSubSceneObj = CreateSubSceneObject("BenchmarkSubScene", benchmarkSubScenePath, true);

            easeGallerySubSceneObj.gameObject.SetActive(false);
            benchmarkSubSceneObj.gameObject.SetActive(false);

            string matFolder = $"{outputDir}/Materials";
            string prefabFolder = $"{outputDir}/Prefabs";

            var showcaseRoot = new GameObject("ShowcaseRoot");
            var easeGalleryRoot = new GameObject("EaseGalleryRoot");
            var benchmarkRoot = new GameObject("BenchmarkRoot");

            EditorSceneManager.MoveGameObjectToScene(showcaseRoot, mainScene);
            EditorSceneManager.MoveGameObjectToScene(easeGalleryRoot, mainScene);
            EditorSceneManager.MoveGameObjectToScene(benchmarkRoot, mainScene);

            BuildShowcaseGameObjects(mainScene, showcaseRoot.transform, matFolder);
            BuildEaseGalleryGameObjects(mainScene, easeGalleryRoot.transform, matFolder);
            var goBenchmarkController = BuildBenchmarkGameObjects(mainScene, benchmarkRoot.transform, matFolder, prefabFolder);

            var runtimeOrbitGo = new GameObject("RuntimeOrbitShowcase");
            EditorSceneManager.MoveGameObjectToScene(runtimeOrbitGo, mainScene);
            var runtimeOrbit = runtimeOrbitGo.AddComponent<ShowcaseRuntimeOrbit>();
            var cameraRig = runtimeOrbitGo.AddComponent<ShowcaseRuntimeCameraRig>();



            GameObject benchmarkControllerGo = new GameObject("BenchmarkController");
            EditorSceneManager.MoveGameObjectToScene(benchmarkControllerGo, mainScene);
            var benchmarkController = benchmarkControllerGo.AddComponent<EntityweenBenchmark>();
            benchmarkController.enabled = false;

            var switcherGo = new GameObject("EntityweenShowcaseSwitcher");
            EditorSceneManager.MoveGameObjectToScene(switcherGo, mainScene);
            var switcher = switcherGo.AddComponent<EntityweenShowcaseSwitcher>();

            ConfigureSwitcher(
                switcher,
                showcaseSubSceneObj, easeGallerySubSceneObj, benchmarkSubSceneObj,
                showcaseRoot, easeGalleryRoot, benchmarkRoot,
                runtimeOrbit, cameraRig,
                benchmarkController, goBenchmarkController
            );

            showcaseRoot.SetActive(false);
            easeGalleryRoot.SetActive(false);
            benchmarkRoot.SetActive(false);

            EditorSceneManager.SaveScene(mainScene, mainScenePath);
            AssetDatabase.Refresh();
        }

        private static Material GetOrCreateMaterial(string folder, string name, Color color)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string path = $"{folder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_Color", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_Color", color);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void BuildShowcaseEntities(UnityEngine.SceneManagement.Scene scene, string outputDir)
        {
            string matFolder = $"{outputDir}/Materials";

            var magentaMat = GetOrCreateMaterial(matFolder, "Magenta", new Color(0.9f, 0.1f, 0.6f));
            var cyanMat = GetOrCreateMaterial(matFolder, "Cyan", new Color(0f, 0.7f, 0.9f));
            var limeMat = GetOrCreateMaterial(matFolder, "LimeGreen", new Color(0.3f, 0.85f, 0.2f));
            var orangeMat = GetOrCreateMaterial(matFolder, "Orange", new Color(0.95f, 0.5f, 0.1f));
            var purpleMat = GetOrCreateMaterial(matFolder, "Purple", new Color(0.55f, 0.2f, 0.85f));
            var goldMat = GetOrCreateMaterial(matFolder, "Gold", new Color(0.85f, 0.65f, 0.1f));
            var yellowMat = GetOrCreateMaterial(matFolder, "Yellow", new Color(0.9f, 0.9f, 0f));
            var redMat = GetOrCreateMaterial(matFolder, "Red", new Color(0.9f, 0.1f, 0.15f));
            var blueMat = GetOrCreateMaterial(matFolder, "Blue", new Color(0.1f, 0.4f, 0.9f));

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.position = new Vector3(0f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(9f, 1f, 9f);
            var groundMat = GetOrCreateMaterial(matFolder, "DarkGround", new Color(0.12f, 0.12f, 0.14f));
            var groundRenderer = groundGo.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.sharedMaterial = groundMat;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(groundGo, scene);

            var moveLocal = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "MoveLocal_Showcase", new Vector3(-36f, 0.5f, 20f), magentaMat, ShowcasePreset.MoveLocal, "Local Move / PingPong");
            moveLocal.moveOffset = new float3(0f, 4f, 0f);
            moveLocal.duration = 2f;
            moveLocal.ease = EaseType.InOutSine;

            var moveWorld = CreateShowcaseItemEntity(scene, PrimitiveType.Capsule, "MoveWorld_Showcase", new Vector3(-24f, 1f, 20f), cyanMat, ShowcasePreset.MoveWorld, "World Move / FromCurrent");
            moveWorld.moveOffset = new float3(0f, 0f, -5f);
            moveWorld.duration = 2.4f;
            moveWorld.ease = EaseType.OutCubic;

            var moveChase = CreateShowcaseItemEntity(scene, PrimitiveType.Sphere, "MoveTweenChase_Showcase", new Vector3(-12f, 0.8f, 20f), goldMat, ShowcasePreset.MoveWithChase, "Move Tween + Chase settle");
            moveChase.moveOffset = new float3(0f, 3.5f, 4f);
            moveChase.duration = 2f;
            moveChase.ease = EaseType.OutElastic;
            moveChase.chaseSmoothTime = 0.25f;

            var rotateWorld = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "RotateWorld_Showcase", new Vector3(0f, 0.5f, 20f), orangeMat, ShowcasePreset.RotateWorld, "World Rotate / Repeat");
            rotateWorld.duration = 2.8f;
            rotateWorld.ease = EaseType.Linear;
            rotateWorld.loop = LoopType.Repeat;
            rotateWorld.rotationDegrees = new float3(0f, 178f, 0f);

            var rotateLocal = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "RotateLocal_Showcase", new Vector3(12f, 0.5f, 20f), purpleMat, ShowcasePreset.RotateLocal, "Local Rotate / InOutBack");
            rotateLocal.duration = 2f;
            rotateLocal.ease = EaseType.InOutBack;
            rotateLocal.rotationDegrees = new float3(65f, 210f, 25f);

            var scale = CreateShowcaseItemEntity(scene, PrimitiveType.Cylinder, "ScaleVector_Showcase", new Vector3(24f, 1f, 20f), limeMat, ShowcasePreset.ScalePingPong, "Scale Vector / Back ease");
            scale.duration = 1.5f;
            scale.ease = EaseType.InOutBack;
            scale.scaleTarget = new float3(2.1f);

            var uniformScale = CreateShowcaseItemEntity(scene, PrimitiveType.Sphere, "ScaleUniform_Showcase", new Vector3(36f, 0.8f, 20f), yellowMat, ShowcasePreset.ScaleUniform, "Uniform Scale / Bounce");
            uniformScale.duration = 1.6f;
            uniformScale.ease = EaseType.OutBounce;
            uniformScale.uniformScaleTarget = 2.2f;

            var closedSpline = CreateShowcaseItemEntity(scene, PrimitiveType.Capsule, "SplineClosed_Showcase", new Vector3(-30f, 3f, 5f), orangeMat, ShowcasePreset.SplinePath, "Closed CatmullRom spline");
            closedSpline.duration = 4f;
            closedSpline.ease = EaseType.Linear;
            closedSpline.loop = LoopType.Repeat;
            closedSpline.splinePath = CreateSpline(SplineType.CatmullRom, true,
                new float3(-30f, 3f, 5f),
                new float3(-27f, 5f, 8f),
                new float3(-23f, 3f, 5f),
                new float3(-27f, 2.6f, 2f));

            var bezierSpline = CreateShowcaseItemEntity(scene, PrimitiveType.Capsule, "SplineBezier_Showcase", new Vector3(-12f, 3f, 5f), redMat, ShowcasePreset.SplinePath, "Open Cubic Bezier spline");
            bezierSpline.duration = 3.2f;
            bezierSpline.ease = EaseType.InOutSine;
            bezierSpline.splinePath = CreateSpline(SplineType.CubicBezier, false,
                new float3(-12f, 3f, 5f),
                new float3(-8f, 6f, 8f),
                new float3(-3f, 2.8f, 3f),
                new float3(1f, 4.8f, 6f));

            var stepSpline = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "SplineStep_Showcase", new Vector3(9f, 3f, 5f), blueMat, ShowcasePreset.SplinePath, "Step spline path");
            stepSpline.duration = 3f;
            stepSpline.ease = EaseType.Linear;
            stepSpline.splinePath = CreateSpline(SplineType.Step, false,
                new float3(9f, 3f, 5f),
                new float3(13f, 5.5f, 5f),
                new float3(17f, 3f, 8f),
                new float3(21f, 4.5f, 3f));

            var bounceMove = CreateShowcaseItemEntity(scene, PrimitiveType.Sphere, "EaseBounce_Showcase", new Vector3(33f, 0.8f, 5f), magentaMat, ShowcasePreset.MoveWorld, "OutBounce move");
            bounceMove.moveOffset = new float3(0f, 5f, 0f);
            bounceMove.duration = 1.8f;
            bounceMove.ease = EaseType.OutBounce;

            var chaseTarget = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "ChaseTarget_Obj", new Vector3(-33f, 0.5f, -10f), purpleMat, ShowcasePreset.MoveLocal, "Moving chase target");
            chaseTarget.moveOffset = new float3(0f, 0f, -7f);
            chaseTarget.duration = 3f;
            chaseTarget.ease = EaseType.InOutQuad;

            var chaser = CreateShowcaseItemEntity(scene, PrimitiveType.Sphere, "ChasePosition_Showcase", new Vector3(-33f, 0.6f, -15f), goldMat, ShowcasePreset.ChaseTarget, "ChasePosition entity");
            chaser.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            chaser.chaseTarget = chaseTarget.gameObject;
            chaser.chaseSmoothTime = 0.3f;

            var poseTarget = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "ChasePose_Target", new Vector3(-10f, 0.5f, -10f), cyanMat, ShowcasePreset.SequenceShowcase, "Moving pose target");
            poseTarget.duration = 2.6f;
            poseTarget.ease = EaseType.InOutSine;
            poseTarget.loop = LoopType.PingPong;
            poseTarget.moveOffset = new float3(0f, 3f, -5f);

            var poseChaser = CreateShowcaseItemEntity(scene, PrimitiveType.Capsule, "ChasePositionRotation_Showcase", new Vector3(-10f, 1f, -16f), limeMat, ShowcasePreset.ChasePositionAndRotation, "Chase position + rotation");
            poseChaser.chaseTarget = poseTarget.gameObject;
            poseChaser.chaseSmoothTime = 0.35f;

            var lookTargetGo = CreateShowcaseItemEntity(scene, PrimitiveType.Sphere, "LookTarget_Obj", new Vector3(12f, 1f, -10f), yellowMat, ShowcasePreset.MoveLocal, "Moving look target");
            lookTargetGo.moveOffset = new float3(6f, 0f, 0f);
            lookTargetGo.duration = 2.5f;
            lookTargetGo.ease = EaseType.InOutQuad;

            var lookChaser = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "LookAt_Showcase", new Vector3(15f, 1f, -16f), redMat, ShowcasePreset.LookAtTarget, "Look at target");
            lookChaser.transform.localScale = new Vector3(0.5f, 0.5f, 2.5f);
            lookChaser.lookTarget = lookTargetGo.gameObject;
            lookChaser.lookSmoothTime = 0.15f;

            var chaseLook = CreateShowcaseItemEntity(scene, PrimitiveType.Capsule, "ChasePositionLook_Showcase", new Vector3(30f, 1f, -16f), blueMat, ShowcasePreset.ChasePositionAndLook, "Chase position + look");
            chaseLook.chaseTarget = lookTargetGo.gameObject;
            chaseLook.chaseSmoothTime = 0.4f;

            var sequence = CreateShowcaseItemEntity(scene, PrimitiveType.Cube, "Sequence_Showcase", new Vector3(-18f, 0.5f, -29f), blueMat, ShowcasePreset.SequenceShowcase, "Sequence / Move-Rotate-Move");
            sequence.moveOffset = new float3(0f, 4f, 0f);
            sequence.duration = 3f;
            sequence.loop = LoopType.PingPong;

            var cameraTargetGo = new GameObject("ShowcaseCameraTarget");
            cameraTargetGo.transform.position = new Vector3(0f, 18f, -38f);
            var cameraAuthoring = cameraTargetGo.AddComponent<ShowcaseCameraAuthoring>();
            cameraAuthoring.moveDuration = 48f;
            cameraAuthoring.lookHoldDuration = 3.2f;
            cameraAuthoring.lookTransitionDuration = 1.5f;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraTargetGo, scene);
        }

        private static void BuildEaseGalleryEntities(UnityEngine.SceneManagement.Scene scene, string outputDir)
        {
            string matFolder = $"{outputDir}/Materials";

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "EaseGalleryGround";
            groundGo.transform.position = new Vector3(0f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(9f, 1f, 5f);
            var groundMat = GetOrCreateMaterial(matFolder, "DarkGround", new Color(0.12f, 0.12f, 0.14f));
            var groundRenderer = groundGo.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.sharedMaterial = groundMat;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(groundGo, scene);

            var easeTypes = (EaseType[])System.Enum.GetValues(typeof(EaseType));
            const int columns = 8;
            const float spacingX = 10f;
            const float spacingZ = 8f;
            const float bounceHeight = 3.2f;
            float hueStep = 1f / Mathf.Max(1, easeTypes.Length);

            for (int i = 0; i < easeTypes.Length; i++)
            {
                EaseType easeType = easeTypes[i];
                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * spacingX;
                float z = (1.5f - row) * spacingZ;

                var material = GetOrCreateMaterial(matFolder, $"Ease_{easeType}", Color.HSVToRGB(i * hueStep, 0.78f, 0.95f));

                var item = CreateShowcaseItemEntity(scene, PrimitiveType.Sphere, $"Ease_{i:00}_{easeType}", new Vector3(x, 0.85f, z), material, ShowcasePreset.MoveWorld, easeType.ToString());
                item.moveOffset = new float3(0f, bounceHeight, 0f);
                item.duration = 2f;
                item.ease = easeType;
                item.loop = LoopType.PingPong;
            }
        }

        private static void BuildBenchmarkEntities(UnityEngine.SceneManagement.Scene scene, string outputDir)
        {
            string matFolder = $"{outputDir}/Materials";
            string prefabFolder = $"{outputDir}/Prefabs";

            if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);

            string prefabPath = $"{prefabFolder}/BenchmarkCube.prefab";
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempCube.name = "BenchmarkCube";
            var cubeMat = GetOrCreateMaterial(matFolder, "BenchmarkCyan", new Color(0f, 0.7f, 0.9f));
            var cubeRenderer = tempCube.GetComponent<Renderer>();
            if (cubeRenderer != null) cubeRenderer.sharedMaterial = cubeMat;
            PrefabUtility.SaveAsPrefabAsset(tempCube, prefabPath);
            GameObject.DestroyImmediate(tempCube);

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "BenchmarkGround";
            groundGo.transform.position = new Vector3(0f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(30f, 1f, 30f);
            var groundMat = GetOrCreateMaterial(matFolder, "DarkGround", new Color(0.12f, 0.12f, 0.14f));
            var groundRenderer = groundGo.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.sharedMaterial = groundMat;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(groundGo, scene);

            GameObject cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject benchmarkSettingsGo = new GameObject("BenchmarkSettings");
            var benchmarkSettings = benchmarkSettingsGo.AddComponent<EntityweenBenchmarkSettingsAuthoring>();
            benchmarkSettings.prefab = cubePrefab;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(benchmarkSettingsGo, scene);
        }

        private static void BuildShowcaseGameObjects(UnityEngine.SceneManagement.Scene scene, Transform parent, string matFolder)
        {
            var magentaMat = GetOrCreateMaterial(matFolder, "Magenta", new Color(0.9f, 0.1f, 0.6f));
            var cyanMat = GetOrCreateMaterial(matFolder, "Cyan", new Color(0f, 0.7f, 0.9f));
            var limeMat = GetOrCreateMaterial(matFolder, "LimeGreen", new Color(0.3f, 0.85f, 0.2f));
            var orangeMat = GetOrCreateMaterial(matFolder, "Orange", new Color(0.95f, 0.5f, 0.1f));
            var purpleMat = GetOrCreateMaterial(matFolder, "Purple", new Color(0.55f, 0.2f, 0.85f));
            var goldMat = GetOrCreateMaterial(matFolder, "Gold", new Color(0.85f, 0.65f, 0.1f));
            var yellowMat = GetOrCreateMaterial(matFolder, "Yellow", new Color(0.9f, 0.9f, 0f));
            var redMat = GetOrCreateMaterial(matFolder, "Red", new Color(0.9f, 0.1f, 0.15f));
            var blueMat = GetOrCreateMaterial(matFolder, "Blue", new Color(0.1f, 0.4f, 0.9f));

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.SetParent(parent, false);
            groundGo.transform.position = new Vector3(0f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(9f, 1f, 9f);
            var groundMat = GetOrCreateMaterial(matFolder, "DarkGround", new Color(0.12f, 0.12f, 0.14f));
            var groundRenderer = groundGo.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.sharedMaterial = groundMat;

            var moveLocal = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "MoveLocal_Showcase_GO", new Vector3(-36f, 0.5f, 20f), magentaMat, ShowcasePreset.MoveLocal, "Local Move / PingPong (GO)", parent);
            moveLocal.moveOffset = new float3(0f, 4f, 0f);
            moveLocal.duration = 2f;
            moveLocal.ease = EaseType.InOutSine;

            var moveWorld = CreateShowcaseItemGameObject(scene, PrimitiveType.Capsule, "MoveWorld_Showcase_GO", new Vector3(-24f, 1f, 20f), cyanMat, ShowcasePreset.MoveWorld, "World Move / FromCurrent (GO)", parent);
            moveWorld.moveOffset = new float3(0f, 0f, -5f);
            moveWorld.duration = 2.4f;
            moveWorld.ease = EaseType.OutCubic;

            var moveChase = CreateShowcaseItemGameObject(scene, PrimitiveType.Sphere, "MoveTweenChase_Showcase_GO", new Vector3(-12f, 0.8f, 20f), goldMat, ShowcasePreset.MoveWithChase, "Move Tween + Chase settle (GO)", parent);
            moveChase.moveOffset = new float3(0f, 3.5f, 4f);
            moveChase.duration = 2f;
            moveChase.ease = EaseType.OutElastic;
            moveChase.chaseSmoothTime = 0.25f;

            var rotateWorld = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "RotateWorld_Showcase_GO", new Vector3(0f, 0.5f, 20f), orangeMat, ShowcasePreset.RotateWorld, "World Rotate / Repeat (GO)", parent);
            rotateWorld.duration = 2.8f;
            rotateWorld.ease = EaseType.Linear;
            rotateWorld.loop = LoopType.Repeat;
            rotateWorld.rotationDegrees = new float3(0f, 178f, 0f);

            var rotateLocal = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "RotateLocal_Showcase_GO", new Vector3(12f, 0.5f, 20f), purpleMat, ShowcasePreset.RotateLocal, "Local Rotate / InOutBack (GO)", parent);
            rotateLocal.duration = 2f;
            rotateLocal.ease = EaseType.InOutBack;
            rotateLocal.rotationDegrees = new float3(65f, 210f, 25f);

            var scale = CreateShowcaseItemGameObject(scene, PrimitiveType.Cylinder, "ScaleVector_Showcase_GO", new Vector3(24f, 1f, 20f), limeMat, ShowcasePreset.ScalePingPong, "Scale Vector / Back ease (GO)", parent);
            scale.duration = 1.5f;
            scale.ease = EaseType.InOutBack;
            scale.scaleTarget = new float3(2.1f);

            var uniformScale = CreateShowcaseItemGameObject(scene, PrimitiveType.Sphere, "ScaleUniform_Showcase_GO", new Vector3(36f, 0.8f, 20f), yellowMat, ShowcasePreset.ScaleUniform, "Uniform Scale / Bounce (GO)", parent);
            uniformScale.duration = 1.6f;
            uniformScale.ease = EaseType.OutBounce;
            uniformScale.uniformScaleTarget = 2.2f;

            var closedSpline = CreateShowcaseItemGameObject(scene, PrimitiveType.Capsule, "SplineClosed_Showcase_GO", new Vector3(-30f, 3f, 5f), orangeMat, ShowcasePreset.SplinePath, "Closed CatmullRom spline (GO)", parent);
            closedSpline.duration = 4f;
            closedSpline.ease = EaseType.Linear;
            closedSpline.loop = LoopType.Repeat;
            closedSpline.splinePath = CreateSpline(SplineType.CatmullRom, true,
                new float3(-30f, 3f, 5f),
                new float3(-27f, 5f, 8f),
                new float3(-23f, 3f, 5f),
                new float3(-27f, 2.6f, 2f));

            var bezierSpline = CreateShowcaseItemGameObject(scene, PrimitiveType.Capsule, "SplineBezier_Showcase_GO", new Vector3(-12f, 3f, 5f), redMat, ShowcasePreset.SplinePath, "Open Cubic Bezier spline (GO)", parent);
            bezierSpline.duration = 3.2f;
            bezierSpline.ease = EaseType.InOutSine;
            bezierSpline.splinePath = CreateSpline(SplineType.CubicBezier, false,
                new float3(-12f, 3f, 5f),
                new float3(-8f, 6f, 8f),
                new float3(-3f, 2.8f, 3f),
                new float3(1f, 4.8f, 6f));

            var stepSpline = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "SplineStep_Showcase_GO", new Vector3(9f, 3f, 5f), blueMat, ShowcasePreset.SplinePath, "Step spline path (GO)", parent);
            stepSpline.duration = 3f;
            stepSpline.ease = EaseType.Linear;
            stepSpline.splinePath = CreateSpline(SplineType.Step, false,
                new float3(9f, 3f, 5f),
                new float3(13f, 5.5f, 5f),
                new float3(17f, 3f, 8f),
                new float3(21f, 4.5f, 3f));

            var bounceMove = CreateShowcaseItemGameObject(scene, PrimitiveType.Sphere, "EaseBounce_Showcase_GO", new Vector3(33f, 0.8f, 5f), magentaMat, ShowcasePreset.MoveWorld, "OutBounce move (GO)", parent);
            bounceMove.moveOffset = new float3(0f, 5f, 0f);
            bounceMove.duration = 1.8f;
            bounceMove.ease = EaseType.OutBounce;

            var chaseTarget = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "ChaseTarget_Obj_GO", new Vector3(-33f, 0.5f, -10f), purpleMat, ShowcasePreset.MoveLocal, "Moving chase target (GO)", parent);
            chaseTarget.moveOffset = new float3(0f, 0f, -7f);
            chaseTarget.duration = 3f;
            chaseTarget.ease = EaseType.InOutQuad;

            var chaser = CreateShowcaseItemGameObject(scene, PrimitiveType.Sphere, "ChasePosition_Showcase_GO", new Vector3(-33f, 0.6f, -15f), goldMat, ShowcasePreset.ChaseTarget, "ChasePosition GO", parent);
            chaser.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            chaser.chaseTarget = chaseTarget.gameObject;
            chaser.chaseSmoothTime = 0.3f;

            var poseTarget = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "ChasePose_Target_GO", new Vector3(-10f, 0.5f, -10f), cyanMat, ShowcasePreset.SequenceShowcase, "Moving pose target (GO)", parent);
            poseTarget.duration = 2.6f;
            poseTarget.ease = EaseType.InOutSine;
            poseTarget.loop = LoopType.PingPong;
            poseTarget.moveOffset = new float3(0f, 3f, -5f);

            var poseChaser = CreateShowcaseItemGameObject(scene, PrimitiveType.Capsule, "ChasePositionRotation_Showcase_GO", new Vector3(-10f, 1f, -16f), limeMat, ShowcasePreset.ChasePositionAndRotation, "Chase position + rotation (GO)", parent);
            poseChaser.chaseTarget = poseTarget.gameObject;
            poseChaser.chaseSmoothTime = 0.35f;

            var lookTargetGo = CreateShowcaseItemGameObject(scene, PrimitiveType.Sphere, "LookTarget_Obj_GO", new Vector3(12f, 1f, -10f), yellowMat, ShowcasePreset.MoveLocal, "Moving look target (GO)", parent);
            lookTargetGo.moveOffset = new float3(6f, 0f, 0f);
            lookTargetGo.duration = 2.5f;
            lookTargetGo.ease = EaseType.InOutQuad;

            var lookChaser = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "LookAt_Showcase_GO", new Vector3(15f, 1f, -16f), redMat, ShowcasePreset.LookAtTarget, "Look at target (GO)", parent);
            lookChaser.transform.localScale = new Vector3(0.5f, 0.5f, 2.5f);
            lookChaser.lookTarget = lookTargetGo.gameObject;
            lookChaser.lookSmoothTime = 0.15f;

            var chaseLook = CreateShowcaseItemGameObject(scene, PrimitiveType.Capsule, "ChasePositionLook_Showcase_GO", new Vector3(30f, 1f, -16f), blueMat, ShowcasePreset.ChasePositionAndLook, "Chase position + look (GO)", parent);
            chaseLook.chaseTarget = lookTargetGo.gameObject;
            chaseLook.chaseSmoothTime = 0.4f;

            var sequence = CreateShowcaseItemGameObject(scene, PrimitiveType.Cube, "Sequence_Showcase_GO", new Vector3(-18f, 0.5f, -29f), blueMat, ShowcasePreset.SequenceShowcase, "Sequence / Move-Rotate-Move (GO)", parent);
            sequence.moveOffset = new float3(0f, 4f, 0f);
            sequence.duration = 3f;
            sequence.loop = LoopType.PingPong;
        }

        private static void BuildEaseGalleryGameObjects(UnityEngine.SceneManagement.Scene scene, Transform parent, string matFolder)
        {
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "EaseGalleryGround";
            groundGo.transform.SetParent(parent, false);
            groundGo.transform.position = new Vector3(0f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(9f, 1f, 5f);
            var groundMat = GetOrCreateMaterial(matFolder, "DarkGround", new Color(0.12f, 0.12f, 0.14f));
            var groundRenderer = groundGo.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.sharedMaterial = groundMat;

            var easeGalleryGo = new GameObject("EaseGalleryController");
            easeGalleryGo.transform.SetParent(parent, false);
            easeGalleryGo.AddComponent<GameObjectEaseGallery>();
        }

        private static GameObjectBenchmark BuildBenchmarkGameObjects(UnityEngine.SceneManagement.Scene scene, Transform parent, string matFolder, string prefabFolder)
        {
            if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);

            string prefabPath = $"{prefabFolder}/BenchmarkCube.prefab";
            GameObject cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (cubePrefab == null)
            {
                GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tempCube.name = "BenchmarkCube";
                var cubeMat = GetOrCreateMaterial(matFolder, "BenchmarkCyan", new Color(0f, 0.7f, 0.9f));
                var cubeRenderer = tempCube.GetComponent<Renderer>();
                if (cubeRenderer != null) cubeRenderer.sharedMaterial = cubeMat;
                cubePrefab = PrefabUtility.SaveAsPrefabAsset(tempCube, prefabPath);
                GameObject.DestroyImmediate(tempCube);
            }

            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "BenchmarkGround";
            groundGo.transform.SetParent(parent, false);
            groundGo.transform.position = new Vector3(0f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(30f, 1f, 30f);
            var groundMat = GetOrCreateMaterial(matFolder, "DarkGround", new Color(0.12f, 0.12f, 0.14f));
            var groundRenderer = groundGo.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.sharedMaterial = groundMat;

            GameObject benchmarkControllerGo = new GameObject("BenchmarkControllerGO");
            benchmarkControllerGo.transform.SetParent(parent, false);
            var benchmarkController = benchmarkControllerGo.AddComponent<GameObjectBenchmark>();
            benchmarkController.prefab = cubePrefab;
            benchmarkController.enabled = false;

            return benchmarkController;
        }

        private static EntityweenShowcaseItem CreateShowcaseItemEntity(
            UnityEngine.SceneManagement.Scene scene,
            PrimitiveType primitiveType,
            string name,
            Vector3 position,
            Material material,
            ShowcasePreset preset,
            string description)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
            go.transform.position = position;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            var item = go.AddComponent<EntityweenShowcaseItem>();
            item.description = description;
            item.preset = preset;
            item.duration = 2f;
            item.ease = EaseType.InOutSine;
            item.loop = LoopType.PingPong;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            return item;
        }

        private static GameObjectShowcaseItem CreateShowcaseItemGameObject(
            UnityEngine.SceneManagement.Scene scene,
            PrimitiveType primitiveType,
            string name,
            Vector3 position,
            Material material,
            ShowcasePreset preset,
            string description,
            Transform parent)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            var item = go.AddComponent<GameObjectShowcaseItem>();
            item.description = description;
            item.preset = preset;
            item.duration = 2f;
            item.ease = EaseType.InOutSine;
            item.loop = LoopType.PingPong;

            return item;
        }

        private static SerializableSpline<float3> CreateSpline(SplineType splineType, bool isClosed, params float3[] points)
        {
            var spline = new SerializableSpline<float3>
            {
                splineType = splineType,
                isClosed = isClosed,
                points = points
            };
            spline.ValidatePoints();
            return spline;
        }

        private static SubScene CreateSubSceneObject(string name, string scenePath, bool autoLoad)
        {
            GameObject subSceneGo = new GameObject(name);
            var subScene = subSceneGo.AddComponent<SubScene>();
            subScene.AutoLoadScene = autoLoad;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
            {
                SetSubSceneAsset(subScene, sceneAsset);
            }
            else
            {
                Debug.LogWarning("SubScene asset could not be loaded at: " + scenePath);
            }

            return subScene;
        }

        private static void ConfigureSwitcher(
            EntityweenShowcaseSwitcher switcher,
            SubScene showcaseSubScene,
            SubScene easeGallerySubScene,
            SubScene benchmarkSubScene,
            GameObject showcaseRoot,
            GameObject easeGalleryRoot,
            GameObject benchmarkRoot,
            ShowcaseRuntimeOrbit runtimeOrbit,
            ShowcaseRuntimeCameraRig cameraRig,
            EntityweenBenchmark benchmarkController,
            GameObjectBenchmark goBenchmarkController)
        {
            var serialized = new SerializedObject(switcher);
            serialized.FindProperty("showcaseSubScene").objectReferenceValue = showcaseSubScene;
            serialized.FindProperty("easeGallerySubScene").objectReferenceValue = easeGallerySubScene;
            serialized.FindProperty("benchmarkSubScene").objectReferenceValue = benchmarkSubScene;

            serialized.FindProperty("showcaseRoot").objectReferenceValue = showcaseRoot;
            serialized.FindProperty("easeGalleryRoot").objectReferenceValue = easeGalleryRoot;
            serialized.FindProperty("benchmarkRoot").objectReferenceValue = benchmarkRoot;

            var showcaseBehaviours = serialized.FindProperty("showcaseBehaviours");
            showcaseBehaviours.arraySize = 2;
            showcaseBehaviours.GetArrayElementAtIndex(0).objectReferenceValue = runtimeOrbit;
            showcaseBehaviours.GetArrayElementAtIndex(1).objectReferenceValue = cameraRig;

            var benchmarkBehaviours = serialized.FindProperty("benchmarkBehaviours");
            benchmarkBehaviours.arraySize = 1;
            benchmarkBehaviours.GetArrayElementAtIndex(0).objectReferenceValue = benchmarkController;

            var goBenchmarkBehaviours = serialized.FindProperty("goBenchmarkBehaviours");
            goBenchmarkBehaviours.arraySize = 1;
            goBenchmarkBehaviours.GetArrayElementAtIndex(0).objectReferenceValue = goBenchmarkController;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSubSceneAsset(SubScene subScene, SceneAsset sceneAsset)
        {
            var prop = typeof(SubScene).GetProperty("SceneAsset");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(subScene, sceneAsset);
                return;
            }
            var field = typeof(SubScene).GetField("m_SceneAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(subScene, sceneAsset);
                return;
            }
            SerializedObject so = new SerializedObject(subScene);
            SerializedProperty sceneAssetProp = so.FindProperty("m_SceneAsset");
            if (sceneAssetProp == null)
            {
                sceneAssetProp = so.FindProperty("SceneAsset");
            }
            if (sceneAssetProp != null)
            {
                sceneAssetProp.objectReferenceValue = sceneAsset;
                so.ApplyModifiedProperties();
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (var folder in Directory.GetDirectories(sourceDir))
            {
                if (Path.GetFileName(folder) == "Editor") continue;
                string dest = Path.Combine(targetDir, Path.GetFileName(folder));
                CopyDirectory(folder, dest);
            }
        }

        private static void CopyScenesToPackage(string outputDir)
        {
            string pkgDir = "Packages/Entityween/Samples~/Scenes";
            if (!Directory.Exists(pkgDir)) return;

            try
            {
                DeleteLegacySceneFiles(pkgDir);

                string[] scenes = {
                    "EntityweenShowcase.unity"
                };
                foreach (var scene in scenes)
                {
                    string srcPath = Path.Combine(outputDir, scene);
                    string destPath = Path.Combine(pkgDir, scene);
                    if (File.Exists(srcPath))
                    {
                        File.Copy(srcPath, destPath, true);
                        string srcMeta = srcPath + ".meta";
                        if (File.Exists(srcMeta)) File.Copy(srcMeta, destPath + ".meta", true);
                    }
                }

                string srcSubSceneDir = Path.Combine(outputDir, "SubScenes");
                string destSubSceneDir = Path.Combine(pkgDir, "SubScenes");
                if (Directory.Exists(srcSubSceneDir))
                {
                    CopyDirectory(srcSubSceneDir, destSubSceneDir);
                    string srcMeta = srcSubSceneDir + ".meta";
                    string destMeta = destSubSceneDir + ".meta";
                    if (File.Exists(srcMeta)) File.Copy(srcMeta, destMeta, true);
                }

                string srcMatDir = Path.Combine(outputDir, "Materials");
                string destMatDir = Path.Combine(pkgDir, "Materials");
                if (Directory.Exists(srcMatDir))
                {
                    CopyDirectory(srcMatDir, destMatDir);
                }

                string srcPrefabDir = Path.Combine(outputDir, "Prefabs");
                string destPrefabDir = Path.Combine(pkgDir, "Prefabs");
                if (Directory.Exists(srcPrefabDir))
                {
                    CopyDirectory(srcPrefabDir, destPrefabDir);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Failed to copy generated assets back to package: " + ex.Message);
            }
        }

        private static void DeleteLegacySceneFiles(string sceneFolder)
        {
            string[] legacyFiles =
            {
                "EntityweenBenchmark.unity",
                "EntityweenBenchmark.meta",
                "EntityweenBenchmark.unity.meta",
                "EntityweenBenchmark_Entities.unity",
                "EntityweenBenchmark_Entities.unity.meta",
                "EntityweenShowcase_Entities.unity",
                "EntityweenShowcase_Entities.unity.meta",
                "EntityweenGameObjectShowcase.unity",
                "EntityweenGameObjectShowcase.unity.meta",
                "EntityweenGameObjectShowcase.meta"
            };

            foreach (var fileName in legacyFiles)
            {
                string path = Path.Combine(sceneFolder, fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
#endif
