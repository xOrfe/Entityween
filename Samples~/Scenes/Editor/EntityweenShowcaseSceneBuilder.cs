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


namespace Entityween.Editor
{
    public static class EntityweenShowcaseSceneBuilder
    {
        [MenuItem("XO/Entityween/Generate Showcase Scene")]
        public static void GenerateShowcaseScene()
        {
            string outputDir = "Assets/Samples/Entityween/1.0.0/Scenes";
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

            // Create main scene first as Single to avoid untitled scene issue
            var mainScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Reposition main camera
            var mainCamera = GameObject.FindWithTag("MainCamera");
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 18f, -42f);
                mainCamera.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            }
            EditorSceneManager.SaveScene(mainScene, mainScenePath);

            // Create showcase entities subscene additively
            var showcaseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            
            BuildShowcaseEntities(showcaseScene, outputDir);

            EditorSceneManager.SaveScene(showcaseScene, showcaseSubScenePath);
            EditorSceneManager.CloseScene(showcaseScene, true);

            // Create ease gallery entities subscene additively
            var easeGalleryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            BuildEaseGalleryEntities(easeGalleryScene, outputDir);
            EditorSceneManager.SaveScene(easeGalleryScene, easeGallerySubScenePath);
            EditorSceneManager.CloseScene(easeGalleryScene, true);

            // Create benchmark entities subscene additively
            var benchmarkScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            BuildBenchmarkEntities(benchmarkScene, outputDir);
            EditorSceneManager.SaveScene(benchmarkScene, benchmarkSubScenePath);
            EditorSceneManager.CloseScene(benchmarkScene, true);

            // Set main scene active
            EditorSceneManager.SetActiveScene(mainScene);
            
            // Import asset database to recognize the new subscene asset
            AssetDatabase.Refresh();
            
            var showcaseSubScene = CreateSubSceneObject("ShowcaseSubScene", showcaseSubScenePath, true);
            var easeGallerySubScene = CreateSubSceneObject("EaseGallerySubScene", easeGallerySubScenePath, false);
            var benchmarkSubScene = CreateSubSceneObject("BenchmarkSubScene", benchmarkSubScenePath, false);

            var runtimeOrbitGo = new GameObject("RuntimeOrbitShowcase");
            var runtimeOrbit = runtimeOrbitGo.AddComponent<ShowcaseRuntimeOrbit>();
            var cameraRig = runtimeOrbitGo.AddComponent<ShowcaseRuntimeCameraRig>();

            GameObject benchmarkControllerGo = new GameObject("BenchmarkController");
            var benchmarkController = benchmarkControllerGo.AddComponent<EntityweenBenchmark>();
            benchmarkController.enabled = false;

            var switcherGo = new GameObject("EntityweenSubSceneSwitcher");
            var switcher = switcherGo.AddComponent<EntityweenSubSceneSwitcher>();
            ConfigureSwitcher(switcher, showcaseSubScene, easeGallerySubScene, benchmarkSubScene, runtimeOrbit, cameraRig, benchmarkController);

            if (easeGallerySubScene != null)
            {
                easeGallerySubScene.gameObject.SetActive(false);
            }

            if (benchmarkSubScene != null)
            {
                benchmarkSubScene.gameObject.SetActive(false);
            }

            // Save main scene again with the subscene configured
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
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

            var showcaseItems = new System.Collections.Generic.List<GameObject>();

            var moveLocal = CreateShowcaseItem(scene, PrimitiveType.Cube, "MoveLocal_Showcase", new Vector3(-36f, 0.5f, 20f), magentaMat, ShowcasePreset.MoveLocal, "Local Move / PingPong");
            moveLocal.moveOffset = new float3(0f, 4f, 0f);
            moveLocal.duration = 2f;
            moveLocal.ease = EaseType.InOutSine;
            showcaseItems.Add(moveLocal.gameObject);

            var moveWorld = CreateShowcaseItem(scene, PrimitiveType.Capsule, "MoveWorld_Showcase", new Vector3(-24f, 1f, 20f), cyanMat, ShowcasePreset.MoveWorld, "World Move / FromCurrent");
            moveWorld.moveOffset = new float3(0f, 0f, -5f);
            moveWorld.duration = 2.4f;
            moveWorld.ease = EaseType.OutCubic;
            showcaseItems.Add(moveWorld.gameObject);

            var moveChase = CreateShowcaseItem(scene, PrimitiveType.Sphere, "MoveTweenChase_Showcase", new Vector3(-12f, 0.8f, 20f), goldMat, ShowcasePreset.MoveWithChase, "Move Tween + Chase settle");
            moveChase.moveOffset = new float3(0f, 3.5f, 4f);
            moveChase.duration = 2f;
            moveChase.ease = EaseType.OutElastic;
            moveChase.chaseSmoothTime = 0.25f;
            showcaseItems.Add(moveChase.gameObject);

            var rotateWorld = CreateShowcaseItem(scene, PrimitiveType.Cube, "RotateWorld_Showcase", new Vector3(0f, 0.5f, 20f), orangeMat, ShowcasePreset.RotateWorld, "World Rotate / Repeat");
            rotateWorld.duration = 2.8f;
            rotateWorld.ease = EaseType.Linear;
            rotateWorld.loop = LoopType.Repeat;
            rotateWorld.rotationDegrees = new float3(0f, 355f, 0f);
            showcaseItems.Add(rotateWorld.gameObject);

            var rotateLocal = CreateShowcaseItem(scene, PrimitiveType.Cube, "RotateLocal_Showcase", new Vector3(12f, 0.5f, 20f), purpleMat, ShowcasePreset.RotateLocal, "Local Rotate / InOutBack");
            rotateLocal.duration = 2f;
            rotateLocal.ease = EaseType.InOutBack;
            rotateLocal.rotationDegrees = new float3(65f, 210f, 25f);
            showcaseItems.Add(rotateLocal.gameObject);

            var scale = CreateShowcaseItem(scene, PrimitiveType.Cylinder, "ScaleVector_Showcase", new Vector3(24f, 1f, 20f), limeMat, ShowcasePreset.ScalePingPong, "Scale Vector / Back ease");
            scale.duration = 1.5f;
            scale.ease = EaseType.InOutBack;
            scale.scaleTarget = new float3(2.1f);
            showcaseItems.Add(scale.gameObject);

            var uniformScale = CreateShowcaseItem(scene, PrimitiveType.Sphere, "ScaleUniform_Showcase", new Vector3(36f, 0.8f, 20f), yellowMat, ShowcasePreset.ScaleUniform, "Uniform Scale / Bounce");
            uniformScale.duration = 1.6f;
            uniformScale.ease = EaseType.OutBounce;
            uniformScale.uniformScaleTarget = 2.2f;
            showcaseItems.Add(uniformScale.gameObject);

            var closedSpline = CreateShowcaseItem(scene, PrimitiveType.Capsule, "SplineClosed_Showcase", new Vector3(-30f, 3f, 5f), orangeMat, ShowcasePreset.SplinePath, "Closed CatmullRom spline");
            closedSpline.duration = 4f;
            closedSpline.ease = EaseType.Linear;
            closedSpline.loop = LoopType.Repeat;
            closedSpline.splinePath = CreateSpline(SplineType.CatmullRom, true,
                new float3(-30f, 3f, 5f),
                new float3(-27f, 5f, 8f),
                new float3(-23f, 3f, 5f),
                new float3(-27f, 2.6f, 2f));
            showcaseItems.Add(closedSpline.gameObject);

            var bezierSpline = CreateShowcaseItem(scene, PrimitiveType.Capsule, "SplineBezier_Showcase", new Vector3(-12f, 3f, 5f), redMat, ShowcasePreset.SplinePath, "Open Cubic Bezier spline");
            bezierSpline.duration = 3.2f;
            bezierSpline.ease = EaseType.InOutSine;
            bezierSpline.splinePath = CreateSpline(SplineType.CubicBezier, false,
                new float3(-12f, 3f, 5f),
                new float3(-8f, 6f, 8f),
                new float3(-3f, 2.8f, 3f),
                new float3(1f, 4.8f, 6f));
            showcaseItems.Add(bezierSpline.gameObject);

            var stepSpline = CreateShowcaseItem(scene, PrimitiveType.Cube, "SplineStep_Showcase", new Vector3(9f, 3f, 5f), blueMat, ShowcasePreset.SplinePath, "Step spline path");
            stepSpline.duration = 3f;
            stepSpline.ease = EaseType.Linear;
            stepSpline.splinePath = CreateSpline(SplineType.Step, false,
                new float3(9f, 3f, 5f),
                new float3(13f, 5.5f, 5f),
                new float3(17f, 3f, 8f),
                new float3(21f, 4.5f, 3f));
            showcaseItems.Add(stepSpline.gameObject);

            var bounceMove = CreateShowcaseItem(scene, PrimitiveType.Sphere, "EaseBounce_Showcase", new Vector3(33f, 0.8f, 5f), magentaMat, ShowcasePreset.MoveWorld, "OutBounce move");
            bounceMove.moveOffset = new float3(0f, 5f, 0f);
            bounceMove.duration = 1.8f;
            bounceMove.ease = EaseType.OutBounce;
            showcaseItems.Add(bounceMove.gameObject);

            var chaseTarget = CreateShowcaseItem(scene, PrimitiveType.Cube, "ChaseTarget_Obj", new Vector3(-33f, 0.5f, -10f), purpleMat, ShowcasePreset.MoveLocal, "Moving chase target");
            chaseTarget.moveOffset = new float3(0f, 0f, -7f);
            chaseTarget.duration = 3f;
            chaseTarget.ease = EaseType.InOutQuad;

            var chaser = CreateShowcaseItem(scene, PrimitiveType.Sphere, "ChasePosition_Showcase", new Vector3(-33f, 0.6f, -15f), goldMat, ShowcasePreset.ChaseTarget, "ChasePosition entity");
            chaser.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            chaser.chaseTarget = chaseTarget.gameObject;
            chaser.chaseSmoothTime = 0.3f;
            showcaseItems.Add(chaseTarget.gameObject);
            showcaseItems.Add(chaser.gameObject);

            var poseTarget = CreateShowcaseItem(scene, PrimitiveType.Cube, "ChasePose_Target", new Vector3(-10f, 0.5f, -10f), cyanMat, ShowcasePreset.SequenceShowcase, "Moving pose target");
            poseTarget.duration = 2.6f;
            poseTarget.ease = EaseType.InOutSine;
            poseTarget.loop = LoopType.PingPong;
            poseTarget.moveOffset = new float3(0f, 3f, -5f);

            var poseChaser = CreateShowcaseItem(scene, PrimitiveType.Capsule, "ChasePositionRotation_Showcase", new Vector3(-10f, 1f, -16f), limeMat, ShowcasePreset.ChasePositionAndRotation, "Chase position + rotation");
            poseChaser.chaseTarget = poseTarget.gameObject;
            poseChaser.chaseSmoothTime = 0.35f;
            showcaseItems.Add(poseTarget.gameObject);
            showcaseItems.Add(poseChaser.gameObject);

            var lookTargetGo = CreateShowcaseItem(scene, PrimitiveType.Sphere, "LookTarget_Obj", new Vector3(12f, 1f, -10f), yellowMat, ShowcasePreset.MoveLocal, "Moving look target");
            lookTargetGo.moveOffset = new float3(6f, 0f, 0f);
            lookTargetGo.duration = 2.5f;
            lookTargetGo.ease = EaseType.InOutQuad;

            var lookChaser = CreateShowcaseItem(scene, PrimitiveType.Cube, "LookAt_Showcase", new Vector3(15f, 1f, -16f), redMat, ShowcasePreset.LookAtTarget, "Look at target");
            lookChaser.transform.localScale = new Vector3(0.5f, 0.5f, 2.5f);
            lookChaser.lookTarget = lookTargetGo.gameObject;
            lookChaser.lookSmoothTime = 0.15f;
            showcaseItems.Add(lookTargetGo.gameObject);
            showcaseItems.Add(lookChaser.gameObject);

            var chaseLook = CreateShowcaseItem(scene, PrimitiveType.Capsule, "ChasePositionLook_Showcase", new Vector3(30f, 1f, -16f), blueMat, ShowcasePreset.ChasePositionAndLook, "Chase position + look");
            chaseLook.chaseTarget = lookTargetGo.gameObject;
            chaseLook.chaseSmoothTime = 0.4f;
            showcaseItems.Add(chaseLook.gameObject);

            var sequence = CreateShowcaseItem(scene, PrimitiveType.Cube, "Sequence_Showcase", new Vector3(-18f, 0.5f, -29f), blueMat, ShowcasePreset.SequenceShowcase, "Sequence / Move-Rotate-Move");
            sequence.moveOffset = new float3(0f, 4f, 0f);
            sequence.duration = 3f;
            sequence.loop = LoopType.PingPong;
            showcaseItems.Add(sequence.gameObject);

            var lookTargetGoObj = new GameObject("CameraLookTarget");
            lookTargetGoObj.transform.position = Vector3.zero;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lookTargetGoObj, scene);

            var cameraTargetGo = new GameObject("ShowcaseCameraTarget");
            cameraTargetGo.transform.position = new Vector3(0f, 18f, -38f);
            var cameraAuthoring = cameraTargetGo.AddComponent<ShowcaseCameraAuthoring>();
            cameraAuthoring.lookTargetObject = lookTargetGoObj;
            cameraAuthoring.showcaseItems.AddRange(showcaseItems);

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
            const float moveDistance = 5.5f;
            float hueStep = 1f / Mathf.Max(1, easeTypes.Length);

            for (int i = 0; i < easeTypes.Length; i++)
            {
                EaseType easeType = easeTypes[i];
                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * spacingX;
                float z = (1.5f - row) * spacingZ;
                float direction = i % 2 == 0 ? 1f : -1f;

                var material = GetOrCreateMaterial(
                    matFolder,
                    $"Ease_{easeType}",
                    Color.HSVToRGB(i * hueStep, 0.78f, 0.95f));

                var item = CreateShowcaseItem(
                    scene,
                    PrimitiveType.Sphere,
                    $"Ease_{i:00}_{easeType}",
                    new Vector3(x, 0.85f, z),
                    material,
                    ShowcasePreset.MoveWorld,
                    easeType.ToString());

                item.moveOffset = new float3(direction * moveDistance, 0f, 0f);
                item.duration = 2f;
                item.ease = easeType;
                item.loop = LoopType.PingPong;
            }
        }

        private static EntityweenShowcaseItem CreateShowcaseItem(
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
            EntityweenSubSceneSwitcher switcher,
            SubScene showcaseSubScene,
            SubScene easeGallerySubScene,
            SubScene benchmarkSubScene,
            ShowcaseRuntimeOrbit runtimeOrbit,
            ShowcaseRuntimeCameraRig cameraRig,
            EntityweenBenchmark benchmarkController)
        {
            var serialized = new SerializedObject(switcher);
            serialized.FindProperty("showcaseSubScene").objectReferenceValue = showcaseSubScene;
            serialized.FindProperty("easeGallerySubScene").objectReferenceValue = easeGallerySubScene;
            serialized.FindProperty("benchmarkSubScene").objectReferenceValue = benchmarkSubScene;

            var showcaseBehaviours = serialized.FindProperty("showcaseBehaviours");
            showcaseBehaviours.arraySize = 2;
            showcaseBehaviours.GetArrayElementAtIndex(0).objectReferenceValue = runtimeOrbit;
            showcaseBehaviours.GetArrayElementAtIndex(1).objectReferenceValue = cameraRig;

            var benchmarkBehaviours = serialized.FindProperty("benchmarkBehaviours");
            benchmarkBehaviours.arraySize = 1;
            benchmarkBehaviours.GetArrayElementAtIndex(0).objectReferenceValue = benchmarkController;

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
                "EntityweenBenchmark.unity.meta",
                "EntityweenBenchmark_Entities.unity",
                "EntityweenBenchmark_Entities.unity.meta",
                "EntityweenShowcase_Entities.unity",
                "EntityweenShowcase_Entities.unity.meta"
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
