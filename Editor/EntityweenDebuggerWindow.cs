using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using XO.Curve;

namespace XO.Entityween.Editor
{
    public class EntityweenDebuggerWindow : EditorWindow
    {
        private enum Tab { Tweens, Chases }
        private Tab _tab = Tab.Tweens;
        private string _searchFilter = "";
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.05; // 20 FPS high-responsiveness live updates

        private EntityManager _em;

        // Custom Color Palette for Dark Premium Theme
        private static readonly Color BgColor = new Color(0.12f, 0.12f, 0.13f, 1f);
        private static readonly Color CardBgEven = new Color(0.18f, 0.19f, 0.21f, 1f);
        private static readonly Color CardBgOdd = new Color(0.15f, 0.16f, 0.18f, 1f);
        private static readonly Color DarkBorder = new Color(0.24f, 0.25f, 0.27f, 1f);
        private static readonly Color AccentBlue = new Color(0.22f, 0.78f, 1f, 1f);
        private static readonly Color AccentGreen = new Color(0.3f, 1f, 0.48f, 1f);
        private static readonly Color AccentGold = new Color(1f, 0.8f, 0.3f, 1f);
        private static readonly Color AccentRed = new Color(1f, 0.3f, 0.3f, 1f);

        // Core UI Elements
        private VisualElement _rootContainer;
        private VisualElement _headerStatsRow;
        private Label _statTotalTweens;
        private Label _statActiveTweens;
        private Label _statPausedTweens;
        private Label _statTotalChases;

        private VisualElement _tabTweens;
        private VisualElement _tabChases;
        private ScrollView _scrollView;
        private VisualElement _playModeBanner;
        private Label _playModeBannerText;

        // Caching & Live Update Structures
        private struct TweenInfo
        {
            public Entity Target;
            public Entity Ghost;
            public string TargetName;
            public string Category;
            public string EaseType;
            public bool IsSpline;
            public int SplinePoints;
            public bool IsLoop;
            public string LoopDetails;
        }

        private struct ChaseInfo
        {
            public Entity Entity;
            public string Name;
            public string Category;
            public string Mode;
            public float SmoothTime;
            public float MaxSpeed;
        }

        private class TweenRowView
        {
            public Entity TargetEntity;
            public Entity GhostEntity;
            public string CategoryKey;
            public VisualElement Root;
            public VisualElement StatusDot;
            public Label TargetNameLabel;
            public Label InfoLabel;
            public Label ProgressPercentLabel;
            public Label ProgressTimeLabel;
            public VisualElement ProgressFill;
            public Label ValuesLabel;
            public Button PauseButton;
        }

        private class ChaseRowView
        {
            public Entity Entity;
            public string CategoryKey;
            public VisualElement Root;
            public VisualElement StatusDot;
            public Label TargetNameLabel;
            public Label InfoLabel;
            public Label ValuesLabel;
            public Toggle EnableToggle;
        }

        private List<TweenInfo> _lastTweens = new();
        private List<ChaseInfo> _lastChases = new();

        private readonly List<TweenRowView> _activeTweenViews = new();
        private readonly List<ChaseRowView> _activeChaseViews = new();

        private readonly Dictionary<string, VisualElement> _categoryContainers = new();
        private readonly Dictionary<string, VisualElement> _categoryBodies = new();
        private readonly Dictionary<string, Label> _categoryBadges = new();
        private readonly Dictionary<string, Label> _categoryArrows = new();
        private readonly Dictionary<string, bool> _folds = new();

        [MenuItem("XO/Entityween/Debugger Window")]
        public static void Open()
        {
            var window = GetWindow<EntityweenDebuggerWindow>("⚡ Entityween");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        private void CreateGUI()
        {
            _rootContainer = new VisualElement();
            _rootContainer.style.backgroundColor = BgColor;
            _rootContainer.style.flexGrow = 1;
            _rootContainer.style.paddingTop = 10;
            _rootContainer.style.paddingBottom = 10;
            _rootContainer.style.paddingLeft = 12;
            _rootContainer.style.paddingRight = 12;
            rootVisualElement.Add(_rootContainer);

            BuildHeader();
            BuildToolbar();
            BuildMainScrollView();
            BuildPlayModeBanner();

            // Initial Rebuild State
            CheckStateAndRebuild(true);
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.backgroundColor = new Color(0.16f, 0.18f, 0.20f, 0.95f);
            header.style.borderTopWidth = 1;
            header.style.borderBottomWidth = 1;
            header.style.borderLeftWidth = 1;
            header.style.borderRightWidth = 1;
            header.style.borderTopColor = DarkBorder;
            header.style.borderBottomColor = DarkBorder;
            header.style.borderLeftColor = DarkBorder;
            header.style.borderRightColor = DarkBorder;
            header.style.borderTopLeftRadius = 8;
            header.style.borderTopRightRadius = 8;
            header.style.borderBottomLeftRadius = 8;
            header.style.borderBottomRightRadius = 8;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 10;
            header.style.paddingLeft = 14;
            header.style.paddingRight = 14;
            header.style.marginBottom = 10;

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;

            var title = new Label("⚡ ENTITYWEEN DEBUGGER");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = AccentBlue;
            title.style.flexGrow = 1;
            titleRow.Add(title);

            var version = new Label("v2.0 (UI Toolkit)");
            version.style.fontSize = 10;
            version.style.color = new Color(0.5f, 0.5f, 0.5f);
            titleRow.Add(version);

            header.Add(titleRow);

            _headerStatsRow = new VisualElement();
            _headerStatsRow.style.flexDirection = FlexDirection.Row;
            _headerStatsRow.style.justifyContent = Justify.SpaceBetween;
            _headerStatsRow.style.marginTop = 10;

            _statTotalTweens = CreateStatCard(_headerStatsRow, "Total Tweens", "0", AccentBlue);
            _statActiveTweens = CreateStatCard(_headerStatsRow, "Active", "0", AccentGreen);
            _statPausedTweens = CreateStatCard(_headerStatsRow, "Paused", "0", AccentGold);
            _statTotalChases = CreateStatCard(_headerStatsRow, "Active Chases", "0", AccentGreen);

            header.Add(_headerStatsRow);
            _rootContainer.Add(header);
        }

        private Label CreateStatCard(VisualElement parent, string titleText, string valueText, Color accentColor)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.09f, 0.10f, 0.11f, 1f);
            card.style.borderTopLeftRadius = 6;
            card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = 6;
            card.style.borderBottomRightRadius = 6;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            var statCardBorderColor = new Color(0.2f, 0.2f, 0.22f, 1f);
            card.style.borderTopColor = statCardBorderColor;
            card.style.borderBottomColor = statCardBorderColor;
            card.style.borderLeftColor = statCardBorderColor;
            card.style.borderRightColor = statCardBorderColor;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.alignItems = Align.Center;
            card.style.flexGrow = 1;
            card.style.marginLeft = 4;
            card.style.marginRight = 4;

            var title = new Label(titleText.ToUpper());
            title.style.fontSize = 9;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.55f, 0.55f, 0.57f, 1f);
            title.style.marginBottom = 2;
            card.Add(title);

            var val = new Label(valueText);
            val.style.fontSize = 16;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            val.style.color = accentColor;
            card.Add(val);

            parent.Add(card);
            return val;
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            toolbar.style.paddingBottom = 6;
            toolbar.style.marginBottom = 8;

            // Tab Switchers
            var tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;

            _tabTweens = CreateTab("🎬 Tweens", Tab.Tweens);
            _tabChases = CreateTab("🎯 Chases", Tab.Chases);
            tabs.Add(_tabTweens);
            tabs.Add(_tabChases);
            toolbar.Add(tabs);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Search Filter
            var search = new TextField();
            search.value = _searchFilter;
            search.style.width = 160;
            search.style.marginRight = 8;
            search.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                CheckStateAndRebuild(true);
            });
            toolbar.Add(search);

            // Auto Refresh Toggle
            var autoToggle = new Toggle();
            autoToggle.value = _autoRefresh;
            autoToggle.text = "Auto";
            autoToggle.style.marginRight = 6;
            autoToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
            toolbar.Add(autoToggle);

            // Manual Refresh Button
            var refreshBtn = new Button(() => CheckStateAndRebuild(true));
            refreshBtn.text = "↺";
            refreshBtn.style.paddingLeft = 8;
            refreshBtn.style.paddingRight = 8;
            refreshBtn.style.height = 20;
            refreshBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            refreshBtn.style.backgroundColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            refreshBtn.style.borderTopColor = DarkBorder;
            refreshBtn.style.borderBottomColor = DarkBorder;
            refreshBtn.style.borderLeftColor = DarkBorder;
            refreshBtn.style.borderRightColor = DarkBorder;
            toolbar.Add(refreshBtn);

            _rootContainer.Add(toolbar);

            UpdateTabVisuals();
        }

        private VisualElement CreateTab(string labelText, Tab tabTarget)
        {
            var tab = new VisualElement();
            tab.style.paddingLeft = 14;
            tab.style.paddingRight = 14;
            tab.style.paddingTop = 6;
            tab.style.paddingBottom = 6;
            tab.style.marginRight = 4;
            tab.style.borderBottomWidth = 2;
            tab.style.borderBottomColor = Color.clear;

            var label = new Label(labelText);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 12;
            tab.Add(label);

            tab.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (_tab != tabTarget)
                {
                    _tab = tabTarget;
                    UpdateTabVisuals();
                    CheckStateAndRebuild(true);
                }
            });

            tab.RegisterCallback<MouseOverEvent>(evt =>
            {
                if (_tab != tabTarget)
                {
                    tab.style.borderBottomColor = new Color(0.22f, 0.78f, 1f, 0.4f);
                }
            });

            tab.RegisterCallback<MouseOutEvent>(evt =>
            {
                if (_tab != tabTarget)
                {
                    tab.style.borderBottomColor = Color.clear;
                }
            });

            return tab;
        }

        private void UpdateTabVisuals()
        {
            if (_tab == Tab.Tweens)
            {
                _tabTweens.style.borderBottomColor = AccentBlue;
                _tabTweens.Q<Label>().style.color = Color.white;

                _tabChases.style.borderBottomColor = Color.clear;
                _tabChases.Q<Label>().style.color = new Color(0.55f, 0.55f, 0.57f, 1f);
            }
            else
            {
                _tabChases.style.borderBottomColor = AccentBlue;
                _tabChases.Q<Label>().style.color = Color.white;

                _tabTweens.style.borderBottomColor = Color.clear;
                _tabTweens.Q<Label>().style.color = new Color(0.55f, 0.55f, 0.57f, 1f);
            }
        }

        private void BuildMainScrollView()
        {
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            _scrollView.style.marginTop = 4;
            _rootContainer.Add(_scrollView);
        }

        private void BuildPlayModeBanner()
        {
            _playModeBanner = new VisualElement();
            _playModeBanner.style.position = Position.Absolute;
            _playModeBanner.style.left = 0;
            _playModeBanner.style.right = 0;
            _playModeBanner.style.top = 110;
            _playModeBanner.style.bottom = 0;
            _playModeBanner.style.backgroundColor = new Color(0.12f, 0.12f, 0.13f, 0.96f);
            _playModeBanner.style.alignItems = Align.Center;
            _playModeBanner.style.justifyContent = Justify.Center;
            _playModeBanner.style.display = DisplayStyle.None;

            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            container.style.paddingTop = 20;
            container.style.paddingBottom = 20;
            container.style.paddingLeft = 30;
            container.style.paddingRight = 30;
            container.style.backgroundColor = new Color(0.18f, 0.19f, 0.21f, 1f);
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            container.style.borderTopWidth = 1;
            container.style.borderBottomWidth = 1;
            container.style.borderLeftWidth = 1;
            container.style.borderRightWidth = 1;
            container.style.borderTopColor = DarkBorder;
            container.style.borderBottomColor = DarkBorder;
            container.style.borderLeftColor = DarkBorder;
            container.style.borderRightColor = DarkBorder;

            var labelIcon = new Label("⚡");
            labelIcon.style.fontSize = 32;
            labelIcon.style.marginBottom = 10;
            container.Add(labelIcon);

            _playModeBannerText = new Label("Enter Play Mode to inspect live DOTS tweens.");
            _playModeBannerText.style.fontSize = 13;
            _playModeBannerText.style.unityFontStyleAndWeight = FontStyle.Bold;
            _playModeBannerText.style.color = new Color(0.7f, 0.7f, 0.73f, 1f);
            container.Add(_playModeBannerText);

            _playModeBanner.Add(container);
            rootVisualElement.Add(_playModeBanner);
        }

        private void SetupCategory(string key, string icon, Color accent)
        {
            if (_categoryContainers.ContainsKey(key)) return;

            var foldout = new VisualElement();
            foldout.style.marginTop = 6;
            foldout.style.marginBottom = 4;

            // Foldout Header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.backgroundColor = new Color(0.2f, 0.21f, 0.23f, 1f);
            header.style.borderTopLeftRadius = 6;
            header.style.borderTopRightRadius = 6;
            header.style.borderBottomLeftRadius = 6;
            header.style.borderBottomRightRadius = 6;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 6;
            header.style.paddingLeft = 10;
            header.style.paddingRight = 10;

            var arrow = new Label("▼ ");
            arrow.style.unityFontStyleAndWeight = FontStyle.Bold;
            arrow.style.color = new Color(0.5f, 0.5f, 0.5f);
            header.Add(arrow);
            _categoryArrows[key] = arrow;

            var label = new Label(icon + "  " + key.ToUpper());
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = accent;
            label.style.fontSize = 11;
            label.style.flexGrow = 1;
            header.Add(label);

            var badge = new Label("0");
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.fontSize = 9;
            badge.style.color = Color.white;
            badge.style.backgroundColor = new Color(0.12f, 0.12f, 0.13f, 1f);
            badge.style.borderTopLeftRadius = 8;
            badge.style.borderTopRightRadius = 8;
            badge.style.borderBottomLeftRadius = 8;
            badge.style.borderBottomRightRadius = 8;
            badge.style.paddingTop = 2;
            badge.style.paddingBottom = 2;
            badge.style.paddingLeft = 8;
            badge.style.paddingRight = 8;
            header.Add(badge);
            _categoryBadges[key] = badge;

            foldout.Add(header);

            // Body
            var body = new VisualElement();
            body.style.paddingTop = 4;
            body.style.paddingBottom = 4;
            body.style.paddingLeft = 4;
            foldout.Add(body);
            _categoryBodies[key] = body;

            // Click Handler
            header.RegisterCallback<MouseDownEvent>(evt =>
            {
                bool currentFold = GetFold(key, true);
                SetFold(key, !currentFold);
                UpdateFoldState(key);
            });

            _scrollView.Add(foldout);
            _categoryContainers[key] = foldout;

            UpdateFoldState(key);
        }

        private void UpdateFoldState(string key)
        {
            bool isExpanded = GetFold(key, true);
            _categoryBodies[key].style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _categoryArrows[key].text = isExpanded ? "▼ " : "▶ ";
        }

        private bool GetFold(string key, bool def) => _folds.TryGetValue(key, out var v) ? v : def;
        private void SetFold(string key, bool val) => _folds[key] = val;

        private void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                CheckStateAndRebuild(false);
                return;
            }

            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                CheckStateAndRebuild(false);
            }
        }

        private void CheckStateAndRebuild(bool forceRebuild)
        {
            if (!EditorApplication.isPlaying)
            {
                _playModeBanner.style.display = DisplayStyle.Flex;
                _playModeBannerText.text = "Enter Play Mode to inspect live DOTS tweens.";
                _scrollView.style.display = DisplayStyle.None;
                _headerStatsRow.style.display = DisplayStyle.None;
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                _playModeBanner.style.display = DisplayStyle.Flex;
                _playModeBannerText.text = "No active DOTS World found.";
                _scrollView.style.display = DisplayStyle.None;
                _headerStatsRow.style.display = DisplayStyle.None;
                return;
            }

            _em = world.EntityManager;
            _playModeBanner.style.display = DisplayStyle.None;
            _scrollView.style.display = DisplayStyle.Flex;
            _headerStatsRow.style.display = DisplayStyle.Flex;

            if (_tab == Tab.Tweens)
            {
                GatherActiveTweens(out var currentTweens);
                bool changed = forceRebuild || CheckTweensChanged(currentTweens);
                if (changed)
                {
                    _lastTweens = currentTweens;
                    RebuildTweensUI();
                }
                else
                {
                    UpdateTweensValues();
                }
                UpdateTweensStats();
            }
            else
            {
                GatherActiveChases(out var currentChases);
                bool changed = forceRebuild || CheckChasesChanged(currentChases);
                if (changed)
                {
                    _lastChases = currentChases;
                    RebuildChasesUI();
                }
                else
                {
                    UpdateChasesValues();
                }
                UpdateChasesStats();
            }
        }

        #region Tweens Processing

        private void GatherActiveTweens(out List<TweenInfo> tweens)
        {
            tweens = new List<TweenInfo>();
            var referencedGhosts = new HashSet<Entity>();

            // 1. Position Tweens
            GatherPositionTweens("📍 Position Tweens", referencedGhosts, tweens);

            // 2. Rotation Tweens
            GatherRotationTweens("🔄 Rotation Tweens", referencedGhosts, tweens);

            // 3. Scale Tweens
            GatherScaleTweens("📐 Scale Tweens", referencedGhosts, tweens);

            // 4. Custom / Generic Tweens
            var customQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<TweenControl>(), ComponentType.ReadOnly<PlaybackProgress>());
            var customEntities = customQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < customEntities.Length; i++)
            {
                var ghost = customEntities[i];
                if (referencedGhosts.Contains(ghost)) continue;

                string name = $"[Generic] Ghost #{ghost.Index}";
                if (!PassesFilter(name)) continue;

                ResolveTweenDetails(Entity.Null, ghost, name, "💾 Custom & Generic Tweens", tweens);
            }
            customEntities.Dispose();
        }

        private void GatherPositionTweens(string category, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<ChasePosition>(), ComponentType.ReadOnly<ChasePositionTweenSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var target = entities[i];
                var source = _em.GetComponentData<ChasePositionTweenSource>(target);
                ResolveSourceTween(target, source.GhostEntity, category, referencedSet, tweens);
            }
            entities.Dispose();
        }

        private void GatherRotationTweens(string category, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<ChaseRotation>(), ComponentType.ReadOnly<ChaseRotationTweenSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var target = entities[i];
                var source = _em.GetComponentData<ChaseRotationTweenSource>(target);
                ResolveSourceTween(target, source.GhostEntity, category, referencedSet, tweens);
            }
            entities.Dispose();
        }

        private void GatherScaleTweens(string category, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<ChaseScale>(), ComponentType.ReadOnly<ChaseScaleTweenSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var target = entities[i];
                var source = _em.GetComponentData<ChaseScaleTweenSource>(target);
                ResolveSourceTween(target, source.GhostEntity, category, referencedSet, tweens);
            }
            entities.Dispose();
        }

        private void ResolveSourceTween(Entity target, Entity ghost, string category, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            if (ghost == Entity.Null || !_em.Exists(ghost)) return;

            referencedSet.Add(ghost);

            string name = _em.GetName(target);
            if (string.IsNullOrEmpty(name)) name = $"Entity #{target.Index}";

            if (!PassesFilter(name)) return;

            ResolveTweenDetails(target, ghost, name, category, tweens);
        }

        private void ResolveTweenDetails(Entity target, Entity ghost, string name, string category, List<TweenInfo> tweens)
        {
            string ease = "Linear";
            bool autoKill = true;
            if (_em.HasComponent<TweenControl>(ghost))
            {
                var control = _em.GetComponentData<TweenControl>(ghost);
                ease = control.EaseType.ToString();
                autoKill = control.AutoKill;
            }

            bool isSpline = _em.HasComponent<SplineState>(ghost);
            int splinePts = 0;
            if (isSpline)
            {
                if (_em.HasComponent<SplineElement<float>>(ghost)) splinePts = _em.GetBuffer<SplineElement<float>>(ghost).Length;
                else if (_em.HasComponent<SplineElement<float2>>(ghost)) splinePts = _em.GetBuffer<SplineElement<float2>>(ghost).Length;
                else if (_em.HasComponent<SplineElement<float3>>(ghost)) splinePts = _em.GetBuffer<SplineElement<float3>>(ghost).Length;
                else if (_em.HasComponent<SplineElement<quaternion>>(ghost)) splinePts = _em.GetBuffer<SplineElement<quaternion>>(ghost).Length;
            }

            bool isLoop = _em.HasComponent<PlaybackLoop>(ghost);
            string loopDetails = "";
            if (isLoop)
            {
                var loop = _em.GetComponentData<PlaybackLoop>(ghost);
                loopDetails = $"{loop.LoopType}{(loop.LoopCount > 0 ? $" (x{loop.LoopCount})" : " (∞)")}";
            }

            tweens.Add(new TweenInfo
            {
                Target = target,
                Ghost = ghost,
                TargetName = name,
                Category = category,
                EaseType = ease,
                IsSpline = isSpline,
                SplinePoints = splinePts,
                IsLoop = isLoop,
                LoopDetails = loopDetails
            });
        }

        private bool CheckTweensChanged(List<TweenInfo> current)
        {
            if (current.Count != _lastTweens.Count) return true;
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].Target != _lastTweens[i].Target ||
                    current[i].Ghost != _lastTweens[i].Ghost ||
                    current[i].TargetName != _lastTweens[i].TargetName ||
                    current[i].Category != _lastTweens[i].Category ||
                    current[i].EaseType != _lastTweens[i].EaseType ||
                    current[i].IsSpline != _lastTweens[i].IsSpline ||
                    current[i].SplinePoints != _lastTweens[i].SplinePoints ||
                    current[i].IsLoop != _lastTweens[i].IsLoop ||
                    current[i].LoopDetails != _lastTweens[i].LoopDetails)
                {
                    return true;
                }
            }
            return false;
        }

        private void RebuildTweensUI()
        {
            _scrollView.Clear();
            _categoryContainers.Clear();
            _categoryBodies.Clear();
            _categoryBadges.Clear();
            _categoryArrows.Clear();
            _activeTweenViews.Clear();

            // Set up all possible Tween sections
            SetupCategory("📍 Position Tweens", "📍", AccentBlue);
            SetupCategory("🔄 Rotation Tweens", "🔄", AccentBlue);
            SetupCategory("📐 Scale Tweens", "📐", AccentBlue);
            SetupCategory("🔲 Scale Uniform Tweens", "🔲", AccentBlue);
            SetupCategory("💾 Custom & Generic Tweens", "💾", AccentGold);

            var categoryCounts = new Dictionary<string, int>();
            foreach (var key in _categoryBodies.Keys) categoryCounts[key] = 0;

            for (int i = 0; i < _lastTweens.Count; i++)
            {
                var tween = _lastTweens[i];
                var body = _categoryBodies[tween.Category];
                categoryCounts[tween.Category]++;

                var card = CreateTweenCard(tween, i % 2 == 0);
                body.Add(card.Root);
                _activeTweenViews.Add(card);
            }

            // Update badge counters and visibility
            foreach (var kp in _categoryBodies)
            {
                int count = categoryCounts[kp.Key];
                _categoryBadges[kp.Key].text = count.ToString();

                // If a section is completely empty, we can hide the whole foldout container
                _categoryContainers[kp.Key].style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private TweenRowView CreateTweenCard(TweenInfo tween, bool isEven)
        {
            var card = new TweenRowView
            {
                TargetEntity = tween.Target,
                GhostEntity = tween.Ghost,
                CategoryKey = tween.Category
            };

            card.Root = new VisualElement();
            card.Root.style.backgroundColor = isEven ? CardBgEven : CardBgOdd;
            card.Root.style.borderTopLeftRadius = 6;
            card.Root.style.borderTopRightRadius = 6;
            card.Root.style.borderBottomLeftRadius = 6;
            card.Root.style.borderBottomRightRadius = 6;
            card.Root.style.borderTopWidth = 1;
            card.Root.style.borderBottomWidth = 1;
            card.Root.style.borderLeftWidth = 1;
            card.Root.style.borderRightWidth = 1;
            card.Root.style.borderTopColor = DarkBorder;
            card.Root.style.borderBottomColor = DarkBorder;
            card.Root.style.borderLeftColor = DarkBorder;
            card.Root.style.borderRightColor = DarkBorder;
            card.Root.style.paddingTop = 8;
            card.Root.style.paddingBottom = 8;
            card.Root.style.paddingLeft = 10;
            card.Root.style.paddingRight = 10;
            card.Root.style.marginBottom = 4;

            // 1. Header Row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;

            // Status indicator dot
            card.StatusDot = new VisualElement();
            card.StatusDot.style.width = 8;
            card.StatusDot.style.height = 8;
            card.StatusDot.style.borderTopLeftRadius = 4;
            card.StatusDot.style.borderTopRightRadius = 4;
            card.StatusDot.style.borderBottomLeftRadius = 4;
            card.StatusDot.style.borderBottomRightRadius = 4;
            card.StatusDot.style.marginRight = 8;
            card.StatusDot.style.backgroundColor = AccentGreen;
            headerRow.Add(card.StatusDot);

            // Target Name Label
            card.TargetNameLabel = new Label(tween.TargetName);
            card.TargetNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.TargetNameLabel.style.fontSize = 11;
            card.TargetNameLabel.style.color = Color.white;
            card.TargetNameLabel.style.marginRight = 6;
            if (tween.Target != Entity.Null)
            {
                card.TargetNameLabel.RegisterCallback<MouseDownEvent>(evt => PingEntity(tween.Target));
                card.TargetNameLabel.RegisterCallback<MouseOverEvent>(evt =>
                {
                    card.TargetNameLabel.style.color = AccentBlue;
                });
                card.TargetNameLabel.RegisterCallback<MouseOutEvent>(evt => card.TargetNameLabel.style.color = Color.white);
            }
            headerRow.Add(card.TargetNameLabel);

            // Ghost Badge
            var ghostBtn = new Label($"Ghost #{tween.Ghost.Index}");
            ghostBtn.style.fontSize = 9;
            ghostBtn.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            ghostBtn.style.backgroundColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            ghostBtn.style.borderTopLeftRadius = 4;
            ghostBtn.style.borderTopRightRadius = 4;
            ghostBtn.style.borderBottomLeftRadius = 4;
            ghostBtn.style.borderBottomRightRadius = 4;
            ghostBtn.style.paddingTop = 1;
            ghostBtn.style.paddingBottom = 1;
            ghostBtn.style.paddingLeft = 5;
            ghostBtn.style.paddingRight = 5;
            ghostBtn.RegisterCallback<MouseDownEvent>(evt => PingEntity(tween.Ghost));
            ghostBtn.RegisterCallback<MouseOverEvent>(evt =>
            {
                ghostBtn.style.color = AccentBlue;
            });
            ghostBtn.RegisterCallback<MouseOutEvent>(evt => ghostBtn.style.color = new Color(0.6f, 0.6f, 0.65f, 1f));
            headerRow.Add(ghostBtn);

            // Badges row for info
            card.InfoLabel = new Label("");
            card.InfoLabel.style.fontSize = 9;
            card.InfoLabel.style.color = AccentBlue;
            card.InfoLabel.style.marginLeft = 8;
            card.InfoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Construct Badge Label Text
            string badgesText = tween.EaseType;
            if (tween.IsSpline) badgesText += $" • 🔀 Spline({tween.SplinePoints})";
            if (tween.IsLoop) badgesText += $" • 🔁 {tween.LoopDetails}";
            card.InfoLabel.text = badgesText;
            headerRow.Add(card.InfoLabel);

            // Spacer
            var headerSpacer = new VisualElement();
            headerSpacer.style.flexGrow = 1;
            headerRow.Add(headerSpacer);

            // Actions Buttons: Pause & Kill
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;

            // Pause/Play Button
            card.PauseButton = new Button();
            card.PauseButton.text = "⏸";
            card.PauseButton.style.paddingLeft = 6;
            card.PauseButton.style.paddingRight = 6;
            card.PauseButton.style.height = 18;
            card.PauseButton.style.fontSize = 9;
            card.PauseButton.style.backgroundColor = new Color(0.24f, 0.25f, 0.27f, 1f);
            card.PauseButton.style.borderTopColor = DarkBorder;
            card.PauseButton.style.borderBottomColor = DarkBorder;
            card.PauseButton.style.borderLeftColor = DarkBorder;
            card.PauseButton.style.borderRightColor = DarkBorder;
            card.PauseButton.clicked += () =>
            {
                if (_em.Exists(tween.Ghost))
                {
                    bool active = _em.IsComponentEnabled<TweenControl>(tween.Ghost);
                    _em.SetComponentEnabled<TweenControl>(tween.Ghost, !active);
                    UpdateTweensValues();
                }
            };
            actions.Add(card.PauseButton);

            // Kill Button
            var killBtn = new Button();
            killBtn.text = "✕";
            killBtn.style.paddingLeft = 6;
            killBtn.style.paddingRight = 6;
            killBtn.style.height = 18;
            killBtn.style.fontSize = 9;
            killBtn.style.backgroundColor = new Color(0.4f, 0.15f, 0.15f, 1f);
            var killBorderColor = new Color(0.5f, 0.2f, 0.2f, 1f);
            killBtn.style.borderTopColor = killBorderColor;
            killBtn.style.borderBottomColor = killBorderColor;
            killBtn.style.borderLeftColor = killBorderColor;
            killBtn.style.borderRightColor = killBorderColor;
            killBtn.style.color = AccentRed;
            killBtn.clicked += () =>
            {
                if (_em.Exists(tween.Ghost))
                {
                    CleanupGhostBindings(tween.Ghost);
                    _em.DestroyEntity(tween.Ghost);
                    CheckStateAndRebuild(true);
                }
            };
            actions.Add(killBtn);

            headerRow.Add(actions);
            card.Root.Add(headerRow);

            // 2. Progress Row
            var progressRow = new VisualElement();
            progressRow.style.flexDirection = FlexDirection.Row;
            progressRow.style.alignItems = Align.Center;
            progressRow.style.marginTop = 6;

            // Progress Bar Container
            var barBg = new VisualElement();
            barBg.style.flexGrow = 1;
            barBg.style.height = 6;
            barBg.style.backgroundColor = new Color(0.09f, 0.09f, 0.10f, 1f);
            barBg.style.borderTopLeftRadius = 3;
            barBg.style.borderTopRightRadius = 3;
            barBg.style.borderBottomLeftRadius = 3;
            barBg.style.borderBottomRightRadius = 3;
            barBg.style.overflow = Overflow.Hidden;
            barBg.style.marginRight = 10;

            card.ProgressFill = new VisualElement();
            card.ProgressFill.style.height = Length.Percent(100);
            card.ProgressFill.style.width = Length.Percent(0);
            card.ProgressFill.style.backgroundColor = AccentBlue;
            barBg.Add(card.ProgressFill);
            progressRow.Add(barBg);

            card.ProgressPercentLabel = new Label("0%");
            card.ProgressPercentLabel.style.fontSize = 9;
            card.ProgressPercentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.ProgressPercentLabel.style.color = Color.white;
            card.ProgressPercentLabel.style.width = 30;
            progressRow.Add(card.ProgressPercentLabel);

            card.ProgressTimeLabel = new Label("0.0s / 0.0s");
            card.ProgressTimeLabel.style.fontSize = 9;
            card.ProgressTimeLabel.style.color = new Color(0.55f, 0.55f, 0.57f, 1f);
            card.ProgressTimeLabel.style.width = 75;
            card.ProgressTimeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            progressRow.Add(card.ProgressTimeLabel);

            card.Root.Add(progressRow);

            // 3. Values details
            card.ValuesLabel = new Label("Start: ...  →  End: ... (Current: ...)");
            card.ValuesLabel.style.fontSize = 9;
            card.ValuesLabel.style.color = new Color(0.6f, 0.6f, 0.62f, 1f);
            card.ValuesLabel.style.unityFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Regular.ttf"); // Monospace style if exists, else fallbacks
            card.ValuesLabel.style.marginTop = 5;
            card.ValuesLabel.style.borderTopWidth = 1;
            card.ValuesLabel.style.borderTopColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            card.ValuesLabel.style.paddingTop = 4;
            card.Root.Add(card.ValuesLabel);

            return card;
        }

        private void CleanupGhostBindings(Entity ghost)
        {
            CleanupGhostBinding<ChasePositionTweenSource, ChasePosition>(ghost);
            CleanupGhostBinding<ChaseRotationTweenSource, ChaseRotation>(ghost);
            CleanupGhostBinding<LookTweenSource, Look>(ghost);
            CleanupGhostBinding<ChaseScaleTweenSource, ChaseScale>(ghost);
        }

        private void CleanupGhostBinding<TSource, TState>(Entity ghost)
            where TSource : unmanaged, IComponentData
            where TState : unmanaged, IComponentData
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<TSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (GetSourceGhost<TSource>(entity) != ghost) continue;

                _em.RemoveComponent<TSource>(entity);
                if (_em.HasComponent<TState>(entity))
                    _em.RemoveComponent<TState>(entity);
            }
            entities.Dispose();
        }

        private Entity GetSourceGhost<TSource>(Entity entity) where TSource : unmanaged, IComponentData
        {
            if (typeof(TSource) == typeof(ChasePositionTweenSource))
                return _em.GetComponentData<ChasePositionTweenSource>(entity).GhostEntity;
            if (typeof(TSource) == typeof(ChaseRotationTweenSource))
                return _em.GetComponentData<ChaseRotationTweenSource>(entity).GhostEntity;
            if (typeof(TSource) == typeof(LookTweenSource))
                return _em.GetComponentData<LookTweenSource>(entity).GhostEntity;
            if (typeof(TSource) == typeof(ChaseScaleTweenSource))
                return _em.GetComponentData<ChaseScaleTweenSource>(entity).GhostEntity;
            return Entity.Null;
        }

        private void UpdateTweensValues()
        {
            for (int i = 0; i < _activeTweenViews.Count; i++)
            {
                var view = _activeTweenViews[i];
                if (!_em.Exists(view.GhostEntity)) continue;

                var control = _em.GetComponentData<TweenControl>(view.GhostEntity);
                var progress = _em.GetComponentData<PlaybackProgress>(view.GhostEntity);

                bool isPlaying = _em.IsComponentEnabled<TweenControl>(view.GhostEntity);

                // Update Status Dot & Button text
                view.StatusDot.style.backgroundColor = isPlaying ? AccentGreen : AccentGold;
                view.PauseButton.text = isPlaying ? "⏸" : "▶";
                view.PauseButton.style.color = isPlaying ? Color.white : AccentGreen;

                // Progress Bar Width
                float t = progress.NormalizedTime;
                view.ProgressFill.style.width = Length.Percent(t * 100f);

                // Progress Text Labels
                view.ProgressPercentLabel.text = $"{(t * 100f):F0}%";
                view.ProgressTimeLabel.text = $"{Mathf.Max(0f, control.ElapsedTime):F1}s / {control.SecondsToPlay:F1}s";

                // Formatted Value Points
                string startVal, endVal, curVal;
                string typeName = FormatTweenValues(view.GhostEntity, out startVal, out endVal, out curVal);
                view.ValuesLabel.text = $"[{typeName}]  Start: {startVal}  →  End: {endVal}   (Cur: {curVal})";
            }
        }

        private string FormatTweenValues(Entity ghostEntity, out string startStr, out string endStr, out string curStr)
        {
            startStr = ""; endStr = ""; curStr = "";
            if (_em.HasComponent<TweenValue<float>>(ghostEntity))
            {
                var v = _em.GetComponentData<TweenValue<float>>(ghostEntity);
                startStr = $"{v.StartPoint:F2}";
                endStr = $"{v.EndPoint:F2}";
                curStr = $"{v.CurrentValue:F2}";
                return "Float";
            }
            if (_em.HasComponent<TweenValue<float2>>(ghostEntity))
            {
                var v = _em.GetComponentData<TweenValue<float2>>(ghostEntity);
                startStr = $"({v.StartPoint.x:F2}, {v.StartPoint.y:F2})";
                endStr = $"({v.EndPoint.x:F2}, {v.EndPoint.y:F2})";
                curStr = $"({v.CurrentValue.x:F2}, {v.CurrentValue.y:F2})";
                return "Float2";
            }
            if (_em.HasComponent<TweenValue<float3>>(ghostEntity))
            {
                var v = _em.GetComponentData<TweenValue<float3>>(ghostEntity);
                startStr = $"({v.StartPoint.x:F2}, {v.StartPoint.y:F2}, {v.StartPoint.z:F2})";
                endStr = $"({v.EndPoint.x:F2}, {v.EndPoint.y:F2}, {v.EndPoint.z:F2})";
                curStr = $"({v.CurrentValue.x:F2}, {v.CurrentValue.y:F2}, {v.CurrentValue.z:F2})";
                return "Float3";
            }
            if (_em.HasComponent<TweenValue<quaternion>>(ghostEntity))
            {
                var v = _em.GetComponentData<TweenValue<quaternion>>(ghostEntity);
                startStr = $"({v.StartPoint.value.x:F2}, {v.StartPoint.value.y:F2}, {v.StartPoint.value.z:F2}, {v.StartPoint.value.w:F2})";
                endStr = $"({v.EndPoint.value.x:F2}, {v.EndPoint.value.y:F2}, {v.EndPoint.value.z:F2}, {v.EndPoint.value.w:F2})";
                curStr = $"({v.CurrentValue.value.x:F2}, {v.CurrentValue.value.y:F2}, {v.CurrentValue.value.z:F2}, {v.CurrentValue.value.w:F2})";
                return "Quat";
            }
            return "Unknown";
        }

        private void UpdateTweensStats()
        {
            int total = _lastTweens.Count;
            int active = 0;
            int paused = 0;

            for (int i = 0; i < _lastTweens.Count; i++)
            {
                var ghost = _lastTweens[i].Ghost;
                if (_em.Exists(ghost))
                {
                    if (_em.IsComponentEnabled<TweenControl>(ghost)) active++;
                    else paused++;
                }
            }

            _statTotalTweens.text = total.ToString();
            _statActiveTweens.text = active.ToString();
            _statPausedTweens.text = paused.ToString();

            // Total Chases stays at 0 or is pulled from world if needed
            var chaseQuery = _em.CreateEntityQuery(
                ComponentType.ReadOnly<ChasePosition>(),
                ComponentType.ReadOnly<ChaseRotation>(),
                ComponentType.ReadOnly<Look>(),
                ComponentType.ReadOnly<ChaseScale>()
            );
            _statTotalChases.text = chaseQuery.CalculateEntityCount().ToString();
        }

        #endregion

        #region Chases Processing

        private void GatherActiveChases(out List<ChaseInfo> chases)
        {
            chases = new List<ChaseInfo>();

            // 1. ChasePosition
            GatherSectionChases<ChasePosition>("📍 Chase Position", chases);

            // 2. ChaseRotation
            GatherSectionChases<ChaseRotation>("🔄 Chase Rotation", chases);

            // 3. Look Target
            GatherSectionChases<Look>("👁 Look Targets", chases);

            // 4. Chase Scale
            GatherSectionChases<ChaseScale>("📐 Chase Scale", chases);
        }

        private void GatherSectionChases<TActive>(string category, List<ChaseInfo> chases)
            where TActive : unmanaged, IComponentData
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<TActive>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                string name = _em.GetName(entity);
                if (string.IsNullOrEmpty(name)) name = $"Entity #{entity.Index}";

                if (!PassesFilter(name)) continue;

                string mode = "Override";
                float smooth = 0f;
                float speed = 0f;

                ReadChaseDamp<TActive>(entity, ref mode, ref smooth, ref speed);

                chases.Add(new ChaseInfo
                {
                    Entity = entity,
                    Name = name,
                    Category = category,
                    Mode = mode,
                    SmoothTime = smooth,
                    MaxSpeed = speed
                });
            }
            entities.Dispose();
        }

        private void ReadChaseDamp<TActive>(Entity entity, ref string mode, ref float smooth, ref float speed)
            where TActive : unmanaged, IComponentData
        {
            var chaseMode = ChaseMode.Snap;

            if (typeof(TActive) == typeof(ChasePosition))
            {
                var chase = _em.GetComponentData<ChasePosition>(entity);
                chaseMode = chase.Mode;
                smooth = chase.SmoothTime;
                speed = chase.MaxSpeed;
            }
            else if (typeof(TActive) == typeof(ChaseRotation))
            {
                var chase = _em.GetComponentData<ChaseRotation>(entity);
                chaseMode = chase.Mode;
                smooth = chase.SmoothTime;
                speed = chase.MaxSpeed;
            }
            else if (typeof(TActive) == typeof(Look))
            {
                var chase = _em.GetComponentData<Look>(entity);
                chaseMode = chase.Mode;
                smooth = chase.SmoothTime;
                speed = chase.MaxSpeed;
            }
            else if (typeof(TActive) == typeof(ChaseScale))
            {
                var chase = _em.GetComponentData<ChaseScale>(entity);
                chaseMode = chase.Mode;
                smooth = chase.SmoothTime;
                speed = chase.MaxSpeed;
            }

            mode = chaseMode switch
            {
                ChaseMode.SmoothDamp => "SmoothDamp",
                ChaseMode.SmoothStep => "SmoothStep",
                _ => "Snap"
            };
        }

        private bool CheckChasesChanged(List<ChaseInfo> current)
        {
            if (current.Count != _lastChases.Count) return true;
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].Entity != _lastChases[i].Entity ||
                    current[i].Name != _lastChases[i].Name ||
                    current[i].Category != _lastChases[i].Category ||
                    current[i].Mode != _lastChases[i].Mode ||
                    current[i].SmoothTime != _lastChases[i].SmoothTime ||
                    current[i].MaxSpeed != _lastChases[i].MaxSpeed)
                {
                    return true;
                }
            }
            return false;
        }

        private void RebuildChasesUI()
        {
            _scrollView.Clear();
            _categoryContainers.Clear();
            _categoryBodies.Clear();
            _categoryBadges.Clear();
            _categoryArrows.Clear();
            _activeChaseViews.Clear();

            // Set up all possible Chase sections
            SetupCategory("📍 Chase Position", "📍", AccentGreen);
            SetupCategory("🔄 Chase Rotation", "🔄", AccentGreen);
            SetupCategory("👁 Look Targets", "👁", AccentGold);

            var categoryCounts = new Dictionary<string, int>();
            foreach (var key in _categoryBodies.Keys) categoryCounts[key] = 0;

            for (int i = 0; i < _lastChases.Count; i++)
            {
                var chase = _lastChases[i];
                var body = _categoryBodies[chase.Category];
                categoryCounts[chase.Category]++;

                var card = CreateChaseCard(chase, i % 2 == 0);
                body.Add(card.Root);
                _activeChaseViews.Add(card);
            }

            // Update badges and visibility
            foreach (var kp in _categoryBodies)
            {
                int count = categoryCounts[kp.Key];
                _categoryBadges[kp.Key].text = count.ToString();
                _categoryContainers[kp.Key].style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private ChaseRowView CreateChaseCard(ChaseInfo chase, bool isEven)
        {
            var card = new ChaseRowView
            {
                Entity = chase.Entity,
                CategoryKey = chase.Category
            };

            card.Root = new VisualElement();
            card.Root.style.backgroundColor = isEven ? CardBgEven : CardBgOdd;
            card.Root.style.borderTopLeftRadius = 6;
            card.Root.style.borderTopRightRadius = 6;
            card.Root.style.borderBottomLeftRadius = 6;
            card.Root.style.borderBottomRightRadius = 6;
            card.Root.style.borderTopWidth = 1;
            card.Root.style.borderBottomWidth = 1;
            card.Root.style.borderLeftWidth = 1;
            card.Root.style.borderRightWidth = 1;
            card.Root.style.borderTopColor = DarkBorder;
            card.Root.style.borderBottomColor = DarkBorder;
            card.Root.style.borderLeftColor = DarkBorder;
            card.Root.style.borderRightColor = DarkBorder;
            card.Root.style.paddingTop = 8;
            card.Root.style.paddingBottom = 8;
            card.Root.style.paddingLeft = 10;
            card.Root.style.paddingRight = 10;
            card.Root.style.marginBottom = 4;

            // 1. Header Row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;

            // Status dot
            card.StatusDot = new VisualElement();
            card.StatusDot.style.width = 8;
            card.StatusDot.style.height = 8;
            card.StatusDot.style.borderTopLeftRadius = 4;
            card.StatusDot.style.borderTopRightRadius = 4;
            card.StatusDot.style.borderBottomLeftRadius = 4;
            card.StatusDot.style.borderBottomRightRadius = 4;
            card.StatusDot.style.marginRight = 8;
            card.StatusDot.style.backgroundColor = AccentGreen;
            headerRow.Add(card.StatusDot);

            // Target Name Label
            card.TargetNameLabel = new Label(chase.Name);
            card.TargetNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.TargetNameLabel.style.fontSize = 11;
            card.TargetNameLabel.style.color = Color.white;
            card.TargetNameLabel.style.marginRight = 6;
            card.TargetNameLabel.RegisterCallback<MouseDownEvent>(evt => PingEntity(chase.Entity));
            card.TargetNameLabel.RegisterCallback<MouseOverEvent>(evt =>
            {
                card.TargetNameLabel.style.color = AccentGreen;
            });
            card.TargetNameLabel.RegisterCallback<MouseOutEvent>(evt => card.TargetNameLabel.style.color = Color.white);
            headerRow.Add(card.TargetNameLabel);

            // Spacer
            var headerSpacer = new VisualElement();
            headerSpacer.style.flexGrow = 1;
            headerRow.Add(headerSpacer);

            // Toggle switch for Active status
            card.EnableToggle = new Toggle();
            card.EnableToggle.style.height = 16;
            card.EnableToggle.style.marginRight = 4;
            card.EnableToggle.RegisterValueChangedCallback(evt =>
            {
                if (_em.Exists(chase.Entity))
                {
                    if (chase.Category == "📍 Chase Position") _em.SetComponentEnabled<ChasePosition>(chase.Entity, evt.newValue);
                    else if (chase.Category == "🔄 Chase Rotation") _em.SetComponentEnabled<ChaseRotation>(chase.Entity, evt.newValue);
                    else if (chase.Category == "👁 Look Targets") _em.SetComponentEnabled<Look>(chase.Entity, evt.newValue);
                    UpdateChasesValues();
                }
            });
            headerRow.Add(card.EnableToggle);

            card.Root.Add(headerRow);

            // 2. Info Row
            card.InfoLabel = new Label($"Mode: {chase.Mode}   •   SmoothTime: {chase.SmoothTime:F2}   •   MaxSpeed: {chase.MaxSpeed:F1}");
            card.InfoLabel.style.fontSize = 9;
            card.InfoLabel.style.color = AccentGreen;
            card.InfoLabel.style.marginTop = 4;
            card.Root.Add(card.InfoLabel);

            // 3. Values Row (Dynamic Values)
            card.ValuesLabel = new Label("Target: ...   Velocity: ...");
            card.ValuesLabel.style.fontSize = 9;
            card.ValuesLabel.style.color = new Color(0.6f, 0.6f, 0.62f, 1f);
            card.ValuesLabel.style.marginTop = 5;
            card.ValuesLabel.style.borderTopWidth = 1;
            card.ValuesLabel.style.borderTopColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            card.ValuesLabel.style.paddingTop = 4;
            card.Root.Add(card.ValuesLabel);

            return card;
        }

        private void UpdateChasesValues()
        {
            for (int i = 0; i < _activeChaseViews.Count; i++)
            {
                var view = _activeChaseViews[i];
                if (!_em.Exists(view.Entity)) continue;

                bool enabled = true;
                string targetText = "...";
                string velocityText = "...";

                if (view.CategoryKey == "📍 Chase Position")
                {
                    enabled = _em.IsComponentEnabled<ChasePosition>(view.Entity);
                    var c = _em.GetComponentData<ChasePosition>(view.Entity);
                    bool isEntity = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText = isEntity ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"({c.TargetPosition.x:F2}, {c.TargetPosition.y:F2}, {c.TargetPosition.z:F2})";
                    velocityText = $"({c.Velocity.x:F2}, {c.Velocity.y:F2}, {c.Velocity.z:F2})";
                }
                else if (view.CategoryKey == "🔄 Chase Rotation")
                {
                    enabled = _em.IsComponentEnabled<ChaseRotation>(view.Entity);
                    var c = _em.GetComponentData<ChaseRotation>(view.Entity);
                    bool isEntity = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText = isEntity ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"({c.TargetQuaternion.value.x:F2}, {c.TargetQuaternion.value.y:F2}, {c.TargetQuaternion.value.z:F2}, {c.TargetQuaternion.value.w:F2})";
                    velocityText = $"({c.Velocity.value.x:F2}, {c.Velocity.value.y:F2}, {c.Velocity.value.z:F2}, {c.Velocity.value.w:F2})";
                }
                else if (view.CategoryKey == "👁 Look Targets")
                {
                    enabled = _em.IsComponentEnabled<Look>(view.Entity);
                    var c = _em.GetComponentData<Look>(view.Entity);
                    bool isEntity = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText = isEntity ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"({c.TargetPosition.x:F2}, {c.TargetPosition.y:F2}, {c.TargetPosition.z:F2})";
                    velocityText = $"({c.Velocity.x:F2}, {c.Velocity.y:F2}, {c.Velocity.z:F2})";
                }

                // Status Dot & Toggle Binding
                view.StatusDot.style.backgroundColor = enabled ? AccentGreen : Color.gray;
                view.EnableToggle.SetValueWithoutNotify(enabled);

                view.ValuesLabel.text = $"Target: {targetText}   •   Velocity: {velocityText}";
            }
        }

        private void UpdateChasesStats()
        {
            // Same stats summary logic
            UpdateTweensStats();
        }

        #endregion

        #region Helpers

        private void PingEntity(Entity entity)
        {
            EntityweenDebugUtility.PingEntity(entity);
            Debug.Log($"⚡ Selected Entity: {entity.Index}:{entity.Version}");
        }

        private bool PassesFilter(string text)
        {
            return string.IsNullOrEmpty(_searchFilter) || text.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
