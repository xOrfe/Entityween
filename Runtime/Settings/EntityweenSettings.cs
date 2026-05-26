using UnityEngine;

namespace XO.Entityween
{
    public class EntityweenSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/EntityweenSettings.asset";
        private static EntityweenSettings _instance;

        public static EntityweenSettings Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_EDITOR
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<EntityweenSettings>(SettingsPath);
                    if (_instance == null)
                    {
                        _instance = CreateInstance<EntityweenSettings>();
                        _instance.hideFlags = HideFlags.None;
                        string directory = System.IO.Path.GetDirectoryName(SettingsPath);
                        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                        {
                            System.IO.Directory.CreateDirectory(directory);
                        }
                        UnityEditor.AssetDatabase.CreateAsset(_instance, SettingsPath);
                        UnityEditor.AssetDatabase.SaveAssets();

                        var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets();
                        if (preloadedAssets == null)
                        {
                            preloadedAssets = new Object[0];
                        }
                        bool exists = false;
                        foreach (var asset in preloadedAssets)
                        {
                            if (asset is EntityweenSettings)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            var newList = new Object[preloadedAssets.Length + 1];
                            preloadedAssets.CopyTo(newList, 0);
                            newList[newList.Length - 1] = _instance;
                            UnityEditor.PlayerSettings.SetPreloadedAssets(newList);
                        }
                    }
                    else if (_instance.hideFlags != HideFlags.None)
                    {
                        _instance.hideFlags = HideFlags.None;
                        UnityEditor.EditorUtility.SetDirty(_instance);
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
#endif
                }
                return _instance;
            }
        }

        [Header("General Settings")]
        [SerializeField] private float _defaultDuration = 1.0f;
        public float DefaultDuration => _defaultDuration;

        [SerializeField] private bool _enableLogs = false;
        public bool EnableLogs => _enableLogs;

        private void OnEnable()
        {
            _instance = this;
#if UNITY_EDITOR
            hideFlags = HideFlags.None;
#endif
        }
    }
}
