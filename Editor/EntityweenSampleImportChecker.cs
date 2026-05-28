using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XO.Entityween.Editor
{
    [InitializeOnLoad]
    public static class EntityweenSampleImportChecker
    {
        private static string Version => PackageVersionHelper.Version;
        private const string PackageName = "Entityween";
        private static string SamplesRoot => "Assets/Samples/" + PackageName;
        private static string SampleScenesDir => $"{SamplesRoot}/{Version}/Scenes";
        private static string MainScenePath => $"{SampleScenesDir}/EntityweenShowcase.unity";
        private static string PrefKey => $"Entityween.ShowcaseGenerated.{Application.dataPath.GetHashCode()}.{Version}";

        static EntityweenSampleImportChecker()
        {
            EditorApplication.delayCall += CheckAndGenerateShowcases;
        }

        private static void CheckAndGenerateShowcases()
        {
            CleanupStaleVersions();

            if (Directory.Exists(SampleScenesDir))
            {
                if (!EditorPrefs.GetBool(PrefKey, false) && !File.Exists(MainScenePath))
                {
                    RunShowcaseGeneration();
                    EditorPrefs.SetBool(PrefKey, true);
                }
            }
            else
            {
                if (EditorPrefs.GetBool(PrefKey, false))
                {
                    EditorPrefs.DeleteKey(PrefKey);
                }
            }
        }

        /// <summary>
        /// Removes sample folders from older package versions to prevent duplicate type definitions.
        /// </summary>
        private static void CleanupStaleVersions()
        {
            if (!Directory.Exists(SamplesRoot)) return;

            foreach (var versionDir in Directory.GetDirectories(SamplesRoot))
            {
                string dirName = Path.GetFileName(versionDir);
                if (dirName == Version) continue;

                Debug.Log($"[Entityween] Removing stale sample version: {dirName}");
                AssetDatabase.DeleteAsset($"{SamplesRoot}/{dirName}");
            }
        }

        public static void RunShowcaseGeneration()
        {
            Debug.Log("[Entityween] Detecting imported samples. Automatically generating showcase scenes...");

            var builderType = Type.GetType("Entityween.Editor.EntityweenShowcaseSceneBuilder, Assembly-CSharp-Editor");

            bool generatedAny = false;

            if (builderType != null)
            {
                var method = builderType.GetMethod("GenerateShowcaseScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    try
                    {
                        Debug.Log("[Entityween] Generating unified combined showcase scene...");
                        method.Invoke(null, null);
                        generatedAny = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Entityween] Failed to generate combined showcase: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Entityween] EntityweenShowcaseSceneBuilder type not found in Assembly-CSharp-Editor.");
            }

            if (generatedAny)
            {
                Debug.Log("[Entityween] Showcase scenes generated successfully!");
            }
        }
    }
}
