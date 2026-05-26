using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace XO.Entityween.Editor
{
    public class EntityweenWindow : EditorWindow
    {
        public enum ViewType { Dashboard, Debugger, Settings }
        private ViewType _currentView = ViewType.Dashboard;

        private SerializedObject _serializedSettings;
        public SerializedObject SerializedSettings => _serializedSettings;

        // Views
        private IEntityweenView _dashboardView = new EntityweenDashboardView();
        private IEntityweenView _debuggerView = new EntityweenDebuggerView();
        private IEntityweenView _settingsView = new EntityweenSettingsView();
        private IEntityweenView _activeView;

        // Sidebar UI Elements
        private VisualElement _sidebar;
        private VisualElement _contentContainer;
        private VisualElement _navBtnDashboard;
        private VisualElement _navBtnDebugger;
        private VisualElement _navBtnSettings;

        // Window entry point
        [MenuItem("XO/Entityween/Open Dashboard", false, 0)]
        public static void OpenWindow()
        {
            var window = GetWindow<EntityweenWindow>("Entityween Unified");
            window.minSize = new Vector2(750, 550);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            if (_activeView != null)
            {
                _activeView.Cleanup();
                _activeView = null;
            }
        }

        public void LoadSettings()
        {
            var settings = EntityweenSettings.Instance;
            if (settings != null)
            {
                _serializedSettings = new SerializedObject(settings);
            }
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Row;
            root.style.backgroundColor = EntityweenUIStyleUtility.BgColor;

            // 1. Sidebar Container
            _sidebar = new VisualElement();
            _sidebar.style.width = 190;
            _sidebar.style.backgroundColor = new Color(0.09f, 0.10f, 0.11f, 1f);
            _sidebar.style.borderRightColor = EntityweenUIStyleUtility.DarkBorder;
            _sidebar.style.borderRightWidth = 1;
            _sidebar.style.paddingTop = 15;
            _sidebar.style.paddingLeft = 10;
            _sidebar.style.paddingRight = 10;
            _sidebar.style.paddingBottom = 15;
            root.Add(_sidebar);

            // Sidebar Header / Logo
            var logoRow = new VisualElement();
            logoRow.style.alignItems = Align.Center;
            logoRow.style.marginBottom = 20;

            var logoTitleRow = EntityweenUIStyleUtility.CreateLabelWithIcon("", "ENTITYWEEN", 15, EntityweenUIStyleUtility.AccentBlue);
            logoRow.Add(logoTitleRow);

            string packageVersion = EntityweenVersionHelper.Version;

            var versionLabel = new Label($"v{packageVersion}");
            versionLabel.style.fontSize = 9;
            versionLabel.style.color = new Color(0.5f, 0.5f, 0.52f);
            versionLabel.style.marginTop = 2;
            logoRow.Add(versionLabel);

            _sidebar.Add(logoRow);

            // Navigation Buttons
            _navBtnDashboard = CreateSidebarButton("🏠", "Dashboard", ViewType.Dashboard);
            _navBtnDebugger  = CreateSidebarButton("🎬", "Debugger", ViewType.Debugger);
            _navBtnSettings  = CreateSidebarButton("⚙", "Settings", ViewType.Settings);

            _sidebar.Add(_navBtnDashboard);
            _sidebar.Add(_navBtnDebugger);
            _sidebar.Add(_navBtnSettings);

            // Sidebar Footer spacer
            var sidebarSpacer = new VisualElement();
            sidebarSpacer.style.flexGrow = 1;
            _sidebar.Add(sidebarSpacer);

            var footerLabel = new Label("xOrfe/Entityween");
            footerLabel.style.fontSize = 8;
            footerLabel.style.color = new Color(0.4f, 0.4f, 0.43f);
            footerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _sidebar.Add(footerLabel);

            // 2. Content Container
            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1;
            _contentContainer.style.paddingTop = 15;
            _contentContainer.style.paddingBottom = 15;
            _contentContainer.style.paddingLeft = 20;
            _contentContainer.style.paddingRight = 20;
            root.Add(_contentContainer);

            // Set initial view
            SwitchView(ViewType.Dashboard);
        }

        private VisualElement CreateSidebarButton(string iconText, string labelText, ViewType viewTarget)
        {
            var btn = new VisualElement();
            btn.style.paddingLeft = 14;
            btn.style.paddingTop = 10;
            btn.style.paddingBottom = 10;
            btn.style.marginBottom = 6;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 6;
            btn.style.flexDirection = FlexDirection.Row;
            btn.style.alignItems = Align.Center;
            btn.style.borderLeftWidth = 4;
            btn.style.borderLeftColor = Color.clear;

            var labelRow = EntityweenUIStyleUtility.CreateLabelWithIcon(iconText, labelText, 11, new Color(0.55f, 0.55f, 0.57f));
            btn.Add(labelRow);

            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                SwitchView(viewTarget);
            });

            btn.RegisterCallback<MouseOverEvent>(evt =>
            {
                if (_currentView != viewTarget)
                {
                    btn.style.backgroundColor = new Color(0.18f, 0.19f, 0.21f, 1f);
                    labelRow.Query<Label>().ForEach(l => l.style.color = Color.white);
                }
            });

            btn.RegisterCallback<MouseOutEvent>(evt =>
            {
                if (_currentView != viewTarget)
                {
                    btn.style.backgroundColor = Color.clear;
                    labelRow.Query<Label>().ForEach(l => l.style.color = new Color(0.55f, 0.55f, 0.57f));
                }
            });

            return btn;
        }

        public void SwitchView(ViewType target)
        {
            if (_activeView != null)
            {
                _activeView.Cleanup();
            }

            _currentView = target;

            void StyleButton(VisualElement btn, bool active, Color activeColor)
            {
                btn.style.backgroundColor = active ? new Color(0.15f, 0.24f, 0.35f, 0.8f) : Color.clear;
                btn.style.borderLeftColor = active ? activeColor : Color.clear;
                var color = active ? Color.white : new Color(0.55f, 0.55f, 0.57f);
                btn.Query<Label>().ForEach(l => l.style.color = color);
            }


            StyleButton(_navBtnDashboard, _currentView == ViewType.Dashboard, EntityweenUIStyleUtility.AccentBlue);
            StyleButton(_navBtnDebugger,  _currentView == ViewType.Debugger,  EntityweenUIStyleUtility.AccentGreen);
            StyleButton(_navBtnSettings,  _currentView == ViewType.Settings,  EntityweenUIStyleUtility.AccentPurple);

            _contentContainer.Clear();
            switch (_currentView)
            {
                case ViewType.Dashboard:
                    _activeView = _dashboardView;
                    break;
                case ViewType.Debugger:
                    _activeView = _debuggerView;
                    break;
                case ViewType.Settings:
                    _activeView = _settingsView;
                    break;
            }

            if (_activeView != null)
            {
                _activeView.Initialize(this, _contentContainer);
            }
        }

        private void Tick()
        {
            if (_activeView != null)
            {
                _activeView.Tick();
            }
        }

        public void ImportSamples()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EntityweenWindow).Assembly);
            if (packageInfo != null)
            {
                string sourcePath = Path.Combine(packageInfo.resolvedPath, "Samples~");
                string packageVersion = EntityweenVersionHelper.Version;
                string targetPath = $"Assets/Samples/Entityween/{packageVersion}";
                try
                {
                    if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, targetPath);
                        AssetDatabase.Refresh();
                        Debug.Log($"[Entityween] Samples imported successfully into {targetPath}");
                        EditorUtility.DisplayDialog("Entityween", "Samples successfully imported! Please wait for Unity to compile.", "OK");
                    }
                    else
                    {
                        Debug.LogError($"[Entityween] Samples source folder not found at: {sourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Entityween] Failed to import samples: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError("[Entityween] Failed to locate package directory for assembly.");
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
                string dest = Path.Combine(targetDir, Path.GetFileName(folder));
                CopyDirectory(folder, dest);
            }
        }
    }
}
