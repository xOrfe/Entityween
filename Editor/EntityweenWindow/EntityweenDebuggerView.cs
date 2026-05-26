using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using XO.Curve;
using XO.Entityween;

namespace XO.Entityween.Editor
{
    public class EntityweenDebuggerView : IEntityweenView
    {
        private enum Tab { Tweens, Chases, Sequences }
        private Tab _tab = Tab.Tweens;
        private string _searchFilter = "";
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.05; // 20 FPS high-responsiveness live updates

        private EntityweenWindow _window;
        private VisualElement _root;
        private EntityManager _em;

        // UI Elements
        private VisualElement _debuggerViewRoot;
        private VisualElement _headerStatsRow;
        private Label _statTotalTweens;
        private Label _statActiveTweens;
        private Label _statPausedTweens;
        private Label _statTotalChases;
        private Label _statTotalSequences;

        private VisualElement _tabTweens;
        private VisualElement _tabChases;
        private VisualElement _tabSequences;
        private ListView _listView;
        private Label _emptyStateLabel;
        private VisualElement _playModeBanner;
        private Label _playModeBannerText;

        // Caching lists & dicts
        private List<TweenInfo>     _lastTweens    = new();
        private List<ChaseInfo>     _lastChases    = new();
        private List<SequenceInfo>  _lastSequences = new();

        private readonly List<TweenRowView>    _activeTweenViews    = new();
        private readonly List<ChaseRowView>    _activeChaseViews    = new();
        private readonly List<SequenceRowView> _activeSequenceViews = new();

        private readonly Dictionary<string, VisualElement> _categoryContainers = new();
        private readonly Dictionary<string, VisualElement> _categoryBodies     = new();
        private readonly Dictionary<string, Label>         _categoryBadges     = new();
        private readonly Dictionary<string, Label>         _categoryArrows     = new();
        private readonly Dictionary<string, bool>          _folds              = new();

        // Data Structs
        private struct TweenInfo
        {
            public Entity Target;
            public Entity Ghost;
            public string TargetName;
            public string Category;
            public string EaseType;
            public bool   IsSpline;
            public int    SplinePoints;
            public bool   IsLoop;
            public string LoopDetails;
        }

        private struct ChaseInfo
        {
            public Entity Entity;
            public string Name;
            public string Category;
            public string Mode;
            public float  SmoothTime;
            public float  MaxSpeed;
        }

        private struct SequenceInfo
        {
            public Entity  Entity;
            public string  Name;
            public PlaybackState State;
            public float   Time;
            public float   Duration;
            public float   NormalizedTime;
            public bool    IsLoop;
            public string  LoopDetails;
            public float   TimeScale;
            public int     ElementCount;
            public SequenceElementSnapshot[] Elements;
        }

        private struct SequenceElementSnapshot
        {
            public TimelineActionKind Kind;
            public Entity ActionEntity;
            public float StartTime;
            public float Duration;
            public bool  Started;
            public bool  Completed;
            public string CallbackId;
        }

        private class TweenRowView
        {
            public Entity TargetEntity;
            public Entity GhostEntity;
            public string CategoryKey;
            public VisualElement Root;
            public VisualElement StatusDot;
            public Label TargetNameLabel;
            public VisualElement InfoLabel;
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

        private class SequenceRowView
        {
            public Entity SequenceEntity;
            public VisualElement Root;
            public VisualElement StatusDot;
            public Label NameLabel;
            public Label StateLabel;
            public Label TimeLabel;
            public VisualElement ProgressFill;
            public Label ProgressPercentLabel;
            public VisualElement TimelineContainer;
        }

        public void Initialize(EntityweenWindow window, VisualElement root)
        {
            _window = window;
            _root = root;

            BuildDebuggerView();
        }

        public void Cleanup()
        {
            _root.Clear();
        }

        public void Tick()
        {
            TickDebugger();
        }

        private void BuildDebuggerView()
        {
            _debuggerViewRoot = new VisualElement();
            _debuggerViewRoot.style.flexGrow = 1;
            _root.Add(_debuggerViewRoot);

            BuildDebuggerHeader();
            BuildDebuggerToolbar();
            BuildDebuggerMainScrollView();
            BuildDebuggerPlayModeBanner();

            CheckStateAndRebuild(true);
        }

        private void BuildDebuggerHeader()
        {
            var header = new VisualElement();
            header.style.backgroundColor         = new Color(0.16f, 0.18f, 0.20f, 0.95f);
            header.style.borderTopWidth    = header.style.borderBottomWidth = 1;
            header.style.borderLeftWidth   = header.style.borderRightWidth  = 1;
            header.style.borderTopColor    = header.style.borderBottomColor = EntityweenUIStyleUtility.DarkBorder;
            header.style.borderLeftColor   = header.style.borderRightColor  = EntityweenUIStyleUtility.DarkBorder;
            header.style.borderTopLeftRadius     = header.style.borderTopRightRadius    = 8;
            header.style.borderBottomLeftRadius  = header.style.borderBottomRightRadius = 8;
            header.style.paddingTop    = 8;
            header.style.paddingBottom = 8;
            header.style.paddingLeft   = 12;
            header.style.paddingRight  = 12;
            header.style.marginBottom  = 8;

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems    = Align.Center;

            var title = EntityweenUIStyleUtility.CreateLabelWithIcon("", "ENTITYWEEN DEBUGGER", 13, EntityweenUIStyleUtility.AccentBlue, true);
            title.style.flexGrow                    = 1;
            titleRow.Add(title);

            var version = new Label("v2.1 (UI Toolkit)");
            version.style.fontSize = 9;
            version.style.color    = new Color(0.5f, 0.5f, 0.5f);
            titleRow.Add(version);

            header.Add(titleRow);

            _headerStatsRow = new VisualElement();
            _headerStatsRow.style.flexDirection      = FlexDirection.Row;
            _headerStatsRow.style.justifyContent     = Justify.SpaceBetween;
            _headerStatsRow.style.marginTop          = 8;

            _statTotalTweens    = CreateStatCard(_headerStatsRow, "Tweens",    "0", EntityweenUIStyleUtility.AccentBlue);
            _statActiveTweens   = CreateStatCard(_headerStatsRow, "Active",    "0", EntityweenUIStyleUtility.AccentGreen);
            _statPausedTweens   = CreateStatCard(_headerStatsRow, "Paused",    "0", EntityweenUIStyleUtility.AccentGold);
            _statTotalChases    = CreateStatCard(_headerStatsRow, "Chases",    "0", EntityweenUIStyleUtility.AccentGreen);
            _statTotalSequences = CreateStatCard(_headerStatsRow, "Sequences", "0", EntityweenUIStyleUtility.AccentPurple);

            header.Add(_headerStatsRow);
            _debuggerViewRoot.Add(header);
        }

        private Label CreateStatCard(VisualElement parent, string titleText, string valueText, Color accentColor)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.09f, 0.10f, 0.11f, 1f);
            card.style.borderTopLeftRadius     = card.style.borderTopRightRadius    = 6;
            card.style.borderBottomLeftRadius  = card.style.borderBottomRightRadius = 6;
            card.style.borderTopWidth    = card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth   = card.style.borderRightWidth  = 1;
            var bc = new Color(0.2f, 0.2f, 0.22f, 1f);
            card.style.borderTopColor    = card.style.borderBottomColor = bc;
            card.style.borderLeftColor   = card.style.borderRightColor  = bc;
            card.style.paddingTop    = 4;
            card.style.paddingBottom = 4;
            card.style.paddingLeft   = 8;
            card.style.paddingRight  = 8;
            card.style.alignItems    = Align.Center;
            card.style.flexGrow      = 1;
            card.style.marginLeft    = 2;
            card.style.marginRight   = 2;

            var title = new Label(titleText.ToUpper());
            title.style.fontSize                = 8;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color                   = new Color(0.55f, 0.55f, 0.57f, 1f);
            title.style.marginBottom            = 1;
            card.Add(title);

            var val = new Label(valueText);
            val.style.fontSize                = 14;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            val.style.color                   = accentColor;
            card.Add(val);

            parent.Add(card);
            return val;
        }

        private void BuildDebuggerToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection   = FlexDirection.Row;
            toolbar.style.alignItems      = Align.Center;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            toolbar.style.paddingBottom   = 4;
            toolbar.style.marginBottom    = 6;

            var tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;

            _tabTweens    = CreateTab("🎬", "Tweens",    Tab.Tweens);
            _tabChases    = CreateTab("🎯", "Chases",    Tab.Chases);
            _tabSequences = CreateTab("🎞", "Sequences", Tab.Sequences);
            tabs.Add(_tabTweens);
            tabs.Add(_tabChases);
            tabs.Add(_tabSequences);
            toolbar.Add(tabs);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            var search = new TextField();
            search.value = _searchFilter;
            search.style.width       = 140;
            search.style.marginRight = 6;
            search.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                CheckStateAndRebuild(true);
            });
            toolbar.Add(search);

            var autoToggle = new Toggle();
            autoToggle.value       = _autoRefresh;
            autoToggle.text        = "Auto";
            autoToggle.style.marginRight = 4;
            autoToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
            toolbar.Add(autoToggle);

            var refreshBtn = new Button(() => CheckStateAndRebuild(true));
            refreshBtn.text = "↺";
            refreshBtn.style.paddingLeft  = 6;
            refreshBtn.style.paddingRight = 6;
            refreshBtn.style.height       = 18;
            refreshBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            refreshBtn.style.backgroundColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            refreshBtn.style.borderTopColor    = refreshBtn.style.borderBottomColor = EntityweenUIStyleUtility.DarkBorder;
            refreshBtn.style.borderLeftColor   = refreshBtn.style.borderRightColor  = EntityweenUIStyleUtility.DarkBorder;
            toolbar.Add(refreshBtn);

            _debuggerViewRoot.Add(toolbar);
            UpdateTabVisuals();
        }

        private VisualElement CreateTab(string iconText, string labelText, Tab tabTarget)
        {
            var tab = new VisualElement();
            tab.style.paddingLeft   = 10;
            tab.style.paddingRight  = 10;
            tab.style.paddingTop    = 4;
            tab.style.paddingBottom = 4;
            tab.style.marginRight   = 3;
            tab.style.borderBottomWidth = 2;
            tab.style.borderBottomColor = Color.clear;
            tab.style.flexDirection = FlexDirection.Row;
            tab.style.alignItems = Align.Center;

            var labelRow = EntityweenUIStyleUtility.CreateLabelWithIcon(iconText, labelText, 11, new Color(0.55f, 0.55f, 0.57f), true);
            tab.Add(labelRow);

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
                    labelRow.Query<Label>().ForEach(l => l.style.color = Color.white);
                }
            });

            tab.RegisterCallback<MouseOutEvent>(evt =>
            {
                if (_tab != tabTarget)
                {
                    tab.style.borderBottomColor = Color.clear;
                    labelRow.Query<Label>().ForEach(l => l.style.color = new Color(0.55f, 0.55f, 0.57f, 1f));
                }
            });

            return tab;
        }

        private void UpdateTabVisuals()
        {
            void SetActive(VisualElement t, bool active, Color activeColor)
            {
                if (t == null) return;
                t.style.borderBottomColor = active ? activeColor : Color.clear;
                var color = active ? Color.white : new Color(0.55f, 0.55f, 0.57f, 1f);
                t.Query<Label>().ForEach(l => l.style.color = color);
            }

            SetActive(_tabTweens,    _tab == Tab.Tweens,    EntityweenUIStyleUtility.AccentBlue);
            SetActive(_tabChases,    _tab == Tab.Chases,    EntityweenUIStyleUtility.AccentGreen);
            SetActive(_tabSequences, _tab == Tab.Sequences, EntityweenUIStyleUtility.AccentPurple);
        }

        private void BuildDebuggerMainScrollView()
        {
            _listView = new ListView();
            _listView.selectionType = SelectionType.None;
            _listView.reorderable = false;
            _listView.showBorder = false;
            _listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _listView.style.flexGrow = 1;
            _listView.style.marginTop = 2;
            _listView.style.backgroundColor = Color.clear;
            _debuggerViewRoot.Add(_listView);

            _emptyStateLabel = new Label();
            _emptyStateLabel.style.display = DisplayStyle.None;
            _emptyStateLabel.style.color = new Color(0.5f, 0.5f, 0.52f);
            _emptyStateLabel.style.fontSize = 11;
            _emptyStateLabel.style.marginTop = 15;
            _emptyStateLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _debuggerViewRoot.Add(_emptyStateLabel);
        }

        private void BuildDebuggerPlayModeBanner()
        {
            _playModeBanner = new VisualElement();
            _playModeBanner.style.position  = Position.Absolute;
            _playModeBanner.style.left      = 0;
            _playModeBanner.style.right     = 0;
            _playModeBanner.style.top       = 100;
            _playModeBanner.style.bottom    = 0;
            _playModeBanner.style.backgroundColor = new Color(0.12f, 0.12f, 0.13f, 0.96f);
            _playModeBanner.style.alignItems      = Align.Center;
            _playModeBanner.style.justifyContent  = Justify.Center;
            _playModeBanner.style.display         = DisplayStyle.None;

            var container = new VisualElement();
            container.style.alignItems        = Align.Center;
            container.style.paddingTop        = 15;
            container.style.paddingBottom     = 15;
            container.style.paddingLeft       = 25;
            container.style.paddingRight      = 25;
            container.style.backgroundColor   = new Color(0.18f, 0.19f, 0.21f, 1f);
            container.style.borderTopLeftRadius    = container.style.borderTopRightRadius    = 8;
            container.style.borderBottomLeftRadius = container.style.borderBottomRightRadius = 8;
            container.style.borderTopWidth    = container.style.borderBottomWidth = 1;
            container.style.borderLeftWidth   = container.style.borderRightWidth  = 1;
            container.style.borderTopColor    = container.style.borderBottomColor = EntityweenUIStyleUtility.DarkBorder;
            container.style.borderLeftColor   = container.style.borderRightColor  = EntityweenUIStyleUtility.DarkBorder;


            _playModeBannerText = new Label("Enter Play Mode to inspect live DOTS tweens.");
            _playModeBannerText.style.fontSize                = 12;
            _playModeBannerText.style.unityFontStyleAndWeight = FontStyle.Bold;
            _playModeBannerText.style.color                   = new Color(0.7f, 0.7f, 0.73f, 1f);
            container.Add(_playModeBannerText);

            _playModeBanner.Add(container);
            _debuggerViewRoot.Add(_playModeBanner);
        }

        private void ConfigureEmptyState(bool isEmpty, string message)
        {
            if (_emptyStateLabel != null)
            {
                _emptyStateLabel.text = message;
                _emptyStateLabel.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_listView != null)
            {
                _listView.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private void TickDebugger()
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
            if (_playModeBanner == null) return;

            if (!EditorApplication.isPlaying)
            {
                _playModeBanner.style.display      = DisplayStyle.Flex;
                _playModeBannerText.text           = "Enter Play Mode to inspect live DOTS tweens.";
                _listView.style.display            = DisplayStyle.None;
                _emptyStateLabel.style.display     = DisplayStyle.None;
                _headerStatsRow.style.display      = DisplayStyle.None;
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                _playModeBanner.style.display  = DisplayStyle.Flex;
                _playModeBannerText.text       = "No active DOTS World found.";
                _listView.style.display        = DisplayStyle.None;
                _emptyStateLabel.style.display = DisplayStyle.None;
                _headerStatsRow.style.display  = DisplayStyle.None;
                return;
            }

            _em = world.EntityManager;
            _playModeBanner.style.display  = DisplayStyle.None;
            _listView.style.display        = DisplayStyle.Flex;
            _emptyStateLabel.style.display = DisplayStyle.None;
            _headerStatsRow.style.display  = DisplayStyle.Flex;

            UpdateGlobalStats();

            switch (_tab)
            {
                case Tab.Tweens:
                    GatherActiveTweens(out var currentTweens);
                    bool tweensChanged = forceRebuild || CheckTweensChanged(currentTweens);
                    if (tweensChanged) { _lastTweens = currentTweens; RebuildTweensUI(); }
                    else UpdateTweensValues();
                    break;

                case Tab.Chases:
                    GatherActiveChases(out var currentChases);
                    bool chasesChanged = forceRebuild || CheckChasesChanged(currentChases);
                    if (chasesChanged) { _lastChases = currentChases; RebuildChasesUI(); }
                    else UpdateChasesValues();
                    break;

                case Tab.Sequences:
                    GatherActiveSequences(out var currentSequences);
                    bool seqChanged = forceRebuild || CheckSequencesChanged(currentSequences);
                    if (seqChanged) { _lastSequences = currentSequences; RebuildSequencesUI(); }
                    else UpdateSequencesValues();
                    break;
            }
        }

        private void UpdateGlobalStats()
        {
            int total = CountTweensForDisplay();
            int active = CountActiveTweensForStats();
            int paused = Mathf.Max(0, total - active);
            int chaseCount = CountChasesForDisplay();
            int seqCount = CountSequencesForDisplay();

            if (_statTotalTweens != null)    _statTotalTweens.text    = total.ToString();
            if (_statActiveTweens != null)   _statActiveTweens.text   = active.ToString();
            if (_statPausedTweens != null)   _statPausedTweens.text   = paused.ToString();
            if (_statTotalChases != null)    _statTotalChases.text    = chaseCount.ToString();
            if (_statTotalSequences != null) _statTotalSequences.text = seqCount.ToString();
        }

        private int CountTweensForDisplay()
        {
            using var query = _em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<TweenControl>(),
                    ComponentType.ReadOnly<PlaybackProgress>()
                },
                Options = EntityQueryOptions.IgnoreComponentEnabledState
            });
            return query.CalculateEntityCount();
        }

        private int CountActiveTweensForStats()
        {
            using var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<TweenControl>(),
                ComponentType.ReadOnly<PlaybackProgress>());
            return query.CalculateEntityCount();
        }

        private int CountChasesForDisplay()
        {
            using var positionQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<ChasePosition>());
            using var rotationQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<ChaseRotation>());
            using var lookQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<Look>());
            using var scaleQuery = _em.CreateEntityQuery(ComponentType.ReadOnly<ChaseScale>());

            return positionQuery.CalculateEntityCount() +
                   rotationQuery.CalculateEntityCount() +
                   lookQuery.CalculateEntityCount() +
                   scaleQuery.CalculateEntityCount();
        }

        private int CountSequencesForDisplay()
        {
            using var query = _em.CreateEntityQuery(ComponentType.ReadOnly<Sequence>());
            return query.CalculateEntityCount();
        }

        private HashSet<Entity> GetTimelineDrivenGhosts()
        {
            var set = new HashSet<Entity>();
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<TimelineDriven>());
            var arr = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < arr.Length; i++) set.Add(arr[i]);
            arr.Dispose();
            return set;
        }

        private HashSet<Entity> GetSequenceOwnedEntities()
        {
            var set = new HashSet<Entity>();
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<SequenceActionOwner>());
            var arr = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < arr.Length; i++) set.Add(arr[i]);
            arr.Dispose();
            return set;
        }

        private void GatherActiveTweens(out List<TweenInfo> tweens)
        {
            tweens = new List<TweenInfo>();
            var sequenceGhosts  = GetTimelineDrivenGhosts();
            var referencedGhosts = new HashSet<Entity>();

            GatherPositionTweens("📍 Position Tweens", sequenceGhosts, referencedGhosts, tweens);
            GatherRotationTweens("🔄 Rotation Tweens", sequenceGhosts, referencedGhosts, tweens);
            GatherScaleTweens(   "📐 Scale Tweens",    sequenceGhosts, referencedGhosts, tweens);

            var customQuery    = _em.CreateEntityQuery(ComponentType.ReadOnly<TweenControl>(), ComponentType.ReadOnly<PlaybackProgress>());
            var customEntities = customQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < customEntities.Length; i++)
            {
                var ghost = customEntities[i];
                if (referencedGhosts.Contains(ghost)) continue;
                if (sequenceGhosts.Contains(ghost))   continue;

                string name = $"[Generic] Ghost #{ghost.Index}";
                if (!PassesFilter(name)) continue;

                ResolveTweenDetails(Entity.Null, ghost, name, "💾 Custom & Generic Tweens", tweens);
            }
            customEntities.Dispose();
        }

        private void GatherPositionTweens(string category, HashSet<Entity> sequenceGhosts, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<ChasePosition>(), ComponentType.ReadOnly<ChasePositionTweenSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var target = entities[i];
                var source = _em.GetComponentData<ChasePositionTweenSource>(target);
                if (sequenceGhosts.Contains(source.GhostEntity)) continue;
                ResolveSourceTween(target, source.GhostEntity, category, referencedSet, tweens);
            }
            entities.Dispose();
        }

        private void GatherRotationTweens(string category, HashSet<Entity> sequenceGhosts, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<ChaseRotation>(), ComponentType.ReadOnly<ChaseRotationTweenSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var target = entities[i];
                var source = _em.GetComponentData<ChaseRotationTweenSource>(target);
                if (sequenceGhosts.Contains(source.GhostEntity)) continue;
                ResolveSourceTween(target, source.GhostEntity, category, referencedSet, tweens);
            }
            entities.Dispose();
        }

        private void GatherScaleTweens(string category, HashSet<Entity> sequenceGhosts, HashSet<Entity> referencedSet, List<TweenInfo> tweens)
        {
            var query = _em.CreateEntityQuery(ComponentType.ReadOnly<ChaseScale>(), ComponentType.ReadOnly<ChaseScaleTweenSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var target = entities[i];
                var source = _em.GetComponentData<ChaseScaleTweenSource>(target);
                if (sequenceGhosts.Contains(source.GhostEntity)) continue;
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
            string ease    = "Linear";
            if (_em.HasComponent<TweenControl>(ghost))
            {
                var control = _em.GetComponentData<TweenControl>(ghost);
                ease = control.EaseType.ToString();
            }

            bool isSpline = _em.HasComponent<SplineState>(ghost);
            int  splinePts = 0;
            if (isSpline)
            {
                if (_em.HasComponent<SplineElement<float>>(ghost))      splinePts = _em.GetBuffer<SplineElement<float>>(ghost).Length;
                else if (_em.HasComponent<SplineElement<float2>>(ghost)) splinePts = _em.GetBuffer<SplineElement<float2>>(ghost).Length;
                else if (_em.HasComponent<SplineElement<float3>>(ghost)) splinePts = _em.GetBuffer<SplineElement<float3>>(ghost).Length;
                else if (_em.HasComponent<SplineElement<quaternion>>(ghost)) splinePts = _em.GetBuffer<SplineElement<quaternion>>(ghost).Length;
            }

            bool   isLoop     = false;
            string loopDetails = "";
            if (_em.HasComponent<PlaybackProgress>(ghost))
            {
                var progress = _em.GetComponentData<PlaybackProgress>(ghost);
                isLoop = progress.LoopType != LoopType.None;
                if (isLoop)
                    loopDetails = $"{progress.LoopType}{(progress.LoopCount > 0 ? $" (x{progress.LoopCount})" : " (∞)")}";
            }

            tweens.Add(new TweenInfo
            {
                Target      = target,
                Ghost       = ghost,
                TargetName  = name,
                Category    = category,
                EaseType    = ease,
                IsSpline    = isSpline,
                SplinePoints = splinePts,
                IsLoop      = isLoop,
                LoopDetails = loopDetails
            });
        }

        private bool CheckTweensChanged(List<TweenInfo> current)
        {
            if (current.Count != _lastTweens.Count) return true;
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].Target     != _lastTweens[i].Target     ||
                    current[i].Ghost      != _lastTweens[i].Ghost       ||
                    current[i].TargetName != _lastTweens[i].TargetName  ||
                    current[i].Category   != _lastTweens[i].Category    ||
                    current[i].EaseType   != _lastTweens[i].EaseType    ||
                    current[i].IsSpline   != _lastTweens[i].IsSpline    ||
                    current[i].SplinePoints != _lastTweens[i].SplinePoints ||
                    current[i].IsLoop     != _lastTweens[i].IsLoop      ||
                    current[i].LoopDetails != _lastTweens[i].LoopDetails)
                    return true;
            }
            return false;
        }

        private void RebuildTweensUI()
        {
            _categoryContainers.Clear();
            _categoryBodies.Clear();
            _categoryBadges.Clear();
            _categoryArrows.Clear();
            _activeTweenViews.Clear();

            ConfigureEmptyState(_lastTweens.Count == 0, "No active tweens.");

            _listView.itemsSource = _lastTweens;
            _listView.makeItem = () => new VisualElement();
            _listView.bindItem = (element, index) =>
            {
                element.Clear();
                if (index < 0 || index >= _lastTweens.Count) return;

                var card = CreateTweenCard(_lastTweens[index], index % 2 == 0);
                element.Add(card.Root);
                _activeTweenViews.Add(card);
            };
            _listView.unbindItem = (element, index) => element.Clear();
            _listView.RefreshItems();
            UpdateTweensValues(false);
        }

        private TweenRowView CreateTweenCard(TweenInfo tween, bool isEven)
        {
            var card = new TweenRowView
            {
                TargetEntity = tween.Target,
                GhostEntity  = tween.Ghost,
                CategoryKey  = tween.Category
            };

            card.Root = EntityweenUIStyleUtility.MakeCardRoot(isEven);

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems    = Align.Center;

            card.StatusDot = EntityweenUIStyleUtility.MakeStatusDot(EntityweenUIStyleUtility.AccentGreen);
            headerRow.Add(card.StatusDot);

            card.TargetNameLabel = new Label(tween.TargetName);
            card.TargetNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.TargetNameLabel.style.fontSize  = 11;
            card.TargetNameLabel.style.color     = Color.white;
            card.TargetNameLabel.style.marginRight = 6;
            if (tween.Target != Entity.Null)
            {
                card.TargetNameLabel.RegisterCallback<MouseDownEvent>(evt => PingEntity(tween.Target));
                card.TargetNameLabel.RegisterCallback<MouseOverEvent>(evt => card.TargetNameLabel.style.color = EntityweenUIStyleUtility.AccentBlue);
                card.TargetNameLabel.RegisterCallback<MouseOutEvent>(evt => card.TargetNameLabel.style.color = Color.white);
            }
            headerRow.Add(card.TargetNameLabel);

            var ghostBtn = new Label($"Ghost #{tween.Ghost.Index}");
            EntityweenUIStyleUtility.StyleMiniChip(ghostBtn, new Color(0.22f, 0.23f, 0.25f, 1f), new Color(0.6f, 0.6f, 0.65f, 1f));
            ghostBtn.RegisterCallback<MouseDownEvent>(evt => PingEntity(tween.Ghost));
            ghostBtn.RegisterCallback<MouseOverEvent>(evt => ghostBtn.style.color = EntityweenUIStyleUtility.AccentBlue);
            ghostBtn.RegisterCallback<MouseOutEvent>(evt => ghostBtn.style.color = new Color(0.6f, 0.6f, 0.65f, 1f));
            headerRow.Add(ghostBtn);

            card.InfoLabel = new VisualElement();
            card.InfoLabel.style.flexDirection = FlexDirection.Row;
            card.InfoLabel.style.marginLeft = 8;
            card.InfoLabel.style.alignItems = Align.Center;

            var easeChip = EntityweenUIStyleUtility.CreateMiniChipWithIcon("🎬", tween.EaseType, new Color(0.15f, 0.24f, 0.35f, 0.8f), EntityweenUIStyleUtility.AccentBlue);
            card.InfoLabel.Add(easeChip);

            if (tween.IsSpline)
            {
                var splineChip = EntityweenUIStyleUtility.CreateMiniChipWithIcon("🔀", $"Spline({tween.SplinePoints})", new Color(0.12f, 0.25f, 0.15f, 0.8f), EntityweenUIStyleUtility.AccentGreen);
                splineChip.style.marginLeft = 4;
                card.InfoLabel.Add(splineChip);
            }

            if (tween.IsLoop)
            {
                var loopChip = EntityweenUIStyleUtility.CreateMiniChipWithIcon("🔁", tween.LoopDetails, new Color(0.25f, 0.2f, 0.05f, 0.8f), EntityweenUIStyleUtility.AccentGold);
                loopChip.style.marginLeft = 4;
                card.InfoLabel.Add(loopChip);
            }

            headerRow.Add(card.InfoLabel);

            var headerSpacer = new VisualElement();
            headerSpacer.style.flexGrow = 1;
            headerRow.Add(headerSpacer);

            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;

            card.PauseButton = new Button();
            card.PauseButton.text = "⏸";
            EntityweenUIStyleUtility.StyleActionButton(card.PauseButton, new Color(0.24f, 0.25f, 0.27f, 1f), EntityweenUIStyleUtility.DarkBorder, Color.white);
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

            var killBtn = new Button();
            killBtn.text = "✕";
            EntityweenUIStyleUtility.StyleActionButton(killBtn, new Color(0.4f, 0.15f, 0.15f, 1f), new Color(0.5f, 0.2f, 0.2f, 1f), EntityweenUIStyleUtility.AccentRed);
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

            var progressRow = new VisualElement();
            progressRow.style.flexDirection = FlexDirection.Row;
            progressRow.style.alignItems    = Align.Center;
            progressRow.style.marginTop     = 6;

            var barBg = EntityweenUIStyleUtility.MakeProgressBarBg();
            card.ProgressFill = EntityweenUIStyleUtility.MakeProgressFill(EntityweenUIStyleUtility.AccentBlue);
            barBg.Add(card.ProgressFill);
            progressRow.Add(barBg);

            card.ProgressPercentLabel = new Label("0%");
            EntityweenUIStyleUtility.StyleProgressLabel(card.ProgressPercentLabel, 30, TextAnchor.MiddleLeft);
            progressRow.Add(card.ProgressPercentLabel);

            card.ProgressTimeLabel = new Label("0.0s / 0.0s");
            card.ProgressTimeLabel.style.fontSize  = 9;
            card.ProgressTimeLabel.style.color     = new Color(0.55f, 0.55f, 0.57f, 1f);
            card.ProgressTimeLabel.style.width     = 75;
            card.ProgressTimeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            progressRow.Add(card.ProgressTimeLabel);

            card.Root.Add(progressRow);

            card.ValuesLabel = new Label("Start: ...  →  End: ... (Current: ...)");
            EntityweenUIStyleUtility.StyleValuesLabel(card.ValuesLabel);
            card.Root.Add(card.ValuesLabel);

            return card;
        }

        private void UpdateTweensValues(bool refreshList = true)
        {
            if (refreshList)
            {
                _activeTweenViews.Clear();
                _listView.RefreshItems();
            }

            for (int i = 0; i < _activeTweenViews.Count; i++)
            {
                var view = _activeTweenViews[i];
                if (!_em.Exists(view.GhostEntity)) continue;

                var control  = _em.GetComponentData<TweenControl>(view.GhostEntity);
                var progress = _em.GetComponentData<PlaybackProgress>(view.GhostEntity);
                bool isPlaying = _em.IsComponentEnabled<TweenControl>(view.GhostEntity);

                view.StatusDot.style.backgroundColor = isPlaying ? EntityweenUIStyleUtility.AccentGreen : EntityweenUIStyleUtility.AccentGold;
                view.PauseButton.text  = isPlaying ? "⏸" : "▶";
                view.PauseButton.style.color = isPlaying ? Color.white : EntityweenUIStyleUtility.AccentGreen;

                float t = progress.NormalizedTime;
                view.ProgressFill.style.width = Length.Percent(t * 100f);
                view.ProgressPercentLabel.text = $"{(t * 100f):F0}%";
                view.ProgressTimeLabel.text    = $"{Mathf.Max(0f, control.ElapsedTime):F1}s / {control.SecondsToPlay:F1}s";

                string startVal, endVal, curVal;
                string typeName = FormatTweenValues(view.GhostEntity, out startVal, out endVal, out curVal);
                view.ValuesLabel.text = $"[{typeName}]  Start: {startVal}  →  End: {endVal}   (Cur: {curVal})";
            }
        }

        private string FormatTweenValues(Entity ghost, out string startStr, out string endStr, out string curStr)
        {
            startStr = ""; endStr = ""; curStr = "";
            if (_em.HasComponent<TweenValue<float>>(ghost))
            {
                var v = _em.GetComponentData<TweenValue<float>>(ghost);
                startStr = $"{v.StartPoint:F2}"; endStr = $"{v.EndPoint:F2}"; curStr = $"{v.CurrentValue:F2}";
                return "Float";
            }
            if (_em.HasComponent<TweenValue<float2>>(ghost))
            {
                var v = _em.GetComponentData<TweenValue<float2>>(ghost);
                startStr = $"({v.StartPoint.x:F2}, {v.StartPoint.y:F2})";
                endStr   = $"({v.EndPoint.x:F2}, {v.EndPoint.y:F2})";
                curStr   = $"({v.CurrentValue.x:F2}, {v.CurrentValue.y:F2})";
                return "Float2";
            }
            if (_em.HasComponent<TweenValue<float3>>(ghost))
            {
                var v = _em.GetComponentData<TweenValue<float3>>(ghost);
                startStr = $"({v.StartPoint.x:F2}, {v.StartPoint.y:F2}, {v.StartPoint.z:F2})";
                endStr   = $"({v.EndPoint.x:F2}, {v.EndPoint.y:F2}, {v.EndPoint.z:F2})";
                curStr   = $"({v.CurrentValue.x:F2}, {v.CurrentValue.y:F2}, {v.CurrentValue.z:F2})";
                return "Float3";
            }
            if (_em.HasComponent<TweenValue<quaternion>>(ghost))
            {
                var v = _em.GetComponentData<TweenValue<quaternion>>(ghost);
                startStr = $"({v.StartPoint.value.x:F2}, {v.StartPoint.value.y:F2}, {v.StartPoint.value.z:F2}, {v.StartPoint.value.w:F2})";
                endStr   = $"({v.EndPoint.value.x:F2}, {v.EndPoint.value.y:F2}, {v.EndPoint.value.z:F2}, {v.EndPoint.value.w:F2})";
                curStr   = $"({v.CurrentValue.value.x:F2}, {v.CurrentValue.value.y:F2}, {v.CurrentValue.value.z:F2}, {v.CurrentValue.value.w:F2})";
                return "Quat";
            }
            return "Unknown";
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
            where TState  : unmanaged, IComponentData
        {
            var query    = _em.CreateEntityQuery(ComponentType.ReadOnly<TSource>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (GetSourceGhost<TSource>(entity) != ghost) continue;
                _em.RemoveComponent<TSource>(entity);
                if (_em.HasComponent<TState>(entity)) _em.RemoveComponent<TState>(entity);
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

        private void GatherActiveChases(out List<ChaseInfo> chases)
        {
            chases = new List<ChaseInfo>();
            var sequenceOwned = GetSequenceOwnedEntities();

            GatherSectionChases<ChasePosition>("📍 Chase Position", sequenceOwned, chases);
            GatherSectionChases<ChaseRotation>("🔄 Chase Rotation", sequenceOwned, chases);
            GatherSectionChases<Look>(         "👁 Look Targets",   sequenceOwned, chases);
            GatherSectionChases<ChaseScale>(   "📐 Chase Scale",    sequenceOwned, chases);
        }

        private void GatherSectionChases<TActive>(string category, HashSet<Entity> exclude, List<ChaseInfo> chases)
            where TActive : unmanaged, IComponentData
        {
            var query    = _em.CreateEntityQuery(ComponentType.ReadOnly<TActive>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (exclude.Contains(entity)) continue;

                string name = _em.GetName(entity);
                if (string.IsNullOrEmpty(name)) name = $"Entity #{entity.Index}";
                if (!PassesFilter(name)) continue;

                string mode  = "Override";
                float  smooth = 0f;
                float  speed  = 0f;
                ReadChaseDamp<TActive>(entity, ref mode, ref smooth, ref speed);

                chases.Add(new ChaseInfo
                {
                    Entity    = entity,
                    Name      = name,
                    Category  = category,
                    Mode      = mode,
                    SmoothTime = smooth,
                    MaxSpeed  = speed
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
                var c = _em.GetComponentData<ChasePosition>(entity);
                chaseMode = c.Mode; smooth = c.SmoothTime; speed = c.MaxSpeed;
            }
            else if (typeof(TActive) == typeof(ChaseRotation))
            {
                var c = _em.GetComponentData<ChaseRotation>(entity);
                chaseMode = c.Mode; smooth = c.SmoothTime; speed = c.MaxSpeed;
            }
            else if (typeof(TActive) == typeof(Look))
            {
                var c = _em.GetComponentData<Look>(entity);
                chaseMode = c.Mode; smooth = c.SmoothTime; speed = c.MaxSpeed;
            }
            else if (typeof(TActive) == typeof(ChaseScale))
            {
                var c = _em.GetComponentData<ChaseScale>(entity);
                chaseMode = c.Mode; smooth = c.SmoothTime; speed = c.MaxSpeed;
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
                if (current[i].Entity     != _lastChases[i].Entity     ||
                    current[i].Name       != _lastChases[i].Name        ||
                    current[i].Category   != _lastChases[i].Category    ||
                    current[i].Mode       != _lastChases[i].Mode        ||
                    current[i].SmoothTime != _lastChases[i].SmoothTime  ||
                    current[i].MaxSpeed   != _lastChases[i].MaxSpeed)
                    return true;
            }
            return false;
        }

        private void RebuildChasesUI()
        {
            _categoryContainers.Clear();
            _categoryBodies.Clear();
            _categoryBadges.Clear();
            _categoryArrows.Clear();
            _activeChaseViews.Clear();

            ConfigureEmptyState(_lastChases.Count == 0, "No active chases.");

            _listView.itemsSource = _lastChases;
            _listView.makeItem = () => new VisualElement();
            _listView.bindItem = (element, index) =>
            {
                element.Clear();
                if (index < 0 || index >= _lastChases.Count) return;

                var card = CreateChaseCard(_lastChases[index], index % 2 == 0);
                element.Add(card.Root);
                _activeChaseViews.Add(card);
            };
            _listView.unbindItem = (element, index) => element.Clear();
            _listView.RefreshItems();
            UpdateChasesValues(false);
        }

        private ChaseRowView CreateChaseCard(ChaseInfo chase, bool isEven)
        {
            var card = new ChaseRowView { Entity = chase.Entity, CategoryKey = chase.Category };
            card.Root = EntityweenUIStyleUtility.MakeCardRoot(isEven);

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems    = Align.Center;

            card.StatusDot = EntityweenUIStyleUtility.MakeStatusDot(EntityweenUIStyleUtility.AccentGreen);
            headerRow.Add(card.StatusDot);

            card.TargetNameLabel = new Label(chase.Name);
            card.TargetNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.TargetNameLabel.style.fontSize   = 11;
            card.TargetNameLabel.style.color      = Color.white;
            card.TargetNameLabel.style.marginRight = 6;
            card.TargetNameLabel.RegisterCallback<MouseDownEvent>(evt => PingEntity(chase.Entity));
            card.TargetNameLabel.RegisterCallback<MouseOverEvent>(evt => card.TargetNameLabel.style.color = EntityweenUIStyleUtility.AccentGreen);
            card.TargetNameLabel.RegisterCallback<MouseOutEvent>(evt => card.TargetNameLabel.style.color = Color.white);
            headerRow.Add(card.TargetNameLabel);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            headerRow.Add(spacer);

            card.EnableToggle = new Toggle();
            card.EnableToggle.style.height     = 16;
            card.EnableToggle.style.marginRight = 4;
            card.EnableToggle.RegisterValueChangedCallback(evt =>
            {
                if (_em.Exists(chase.Entity))
                {
                    if (chase.Category == "📍 Chase Position") _em.SetComponentEnabled<ChasePosition>(chase.Entity, evt.newValue);
                    else if (chase.Category == "🔄 Chase Rotation") _em.SetComponentEnabled<ChaseRotation>(chase.Entity, evt.newValue);
                    else if (chase.Category == "👁 Look Targets") _em.SetComponentEnabled<Look>(chase.Entity, evt.newValue);
                    else if (chase.Category == "📐 Chase Scale") _em.SetComponentEnabled<ChaseScale>(chase.Entity, evt.newValue);
                    UpdateChasesValues();
                }
            });
            headerRow.Add(card.EnableToggle);
            card.Root.Add(headerRow);

            card.InfoLabel = new Label($"Mode: {chase.Mode}   •   SmoothTime: {chase.SmoothTime:F2}   •   MaxSpeed: {chase.MaxSpeed:F1}");
            card.InfoLabel.style.fontSize  = 9;
            card.InfoLabel.style.color     = EntityweenUIStyleUtility.AccentGreen;
            card.InfoLabel.style.marginTop = 4;
            card.Root.Add(card.InfoLabel);

            card.ValuesLabel = new Label("Target: ...   Velocity: ...");
            EntityweenUIStyleUtility.StyleValuesLabel(card.ValuesLabel);
            card.Root.Add(card.ValuesLabel);

            return card;
        }

        private void UpdateChasesValues(bool refreshList = true)
        {
            if (refreshList)
            {
                _activeChaseViews.Clear();
                _listView.RefreshItems();
            }

            for (int i = 0; i < _activeChaseViews.Count; i++)
            {
                var view = _activeChaseViews[i];
                if (!_em.Exists(view.Entity)) continue;

                bool   enabled      = true;
                string targetText   = "...";
                string velocityText = "...";

                if (view.CategoryKey == "📍 Chase Position")
                {
                    enabled = _em.IsComponentEnabled<ChasePosition>(view.Entity);
                    var c = _em.GetComponentData<ChasePosition>(view.Entity);
                    bool isEntityTarget = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText    = isEntityTarget ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"({c.TargetPosition.x:F2}, {c.TargetPosition.y:F2}, {c.TargetPosition.z:F2})";
                    velocityText  = $"({c.Velocity.x:F2}, {c.Velocity.y:F2}, {c.Velocity.z:F2})";
                }
                else if (view.CategoryKey == "🔄 Chase Rotation")
                {
                    enabled = _em.IsComponentEnabled<ChaseRotation>(view.Entity);
                    var c = _em.GetComponentData<ChaseRotation>(view.Entity);
                    bool isEntityTarget = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText    = isEntityTarget ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"({c.TargetQuaternion.value.x:F2}, {c.TargetQuaternion.value.y:F2}, {c.TargetQuaternion.value.z:F2}, {c.TargetQuaternion.value.w:F2})";
                    velocityText  = $"({c.Velocity.value.x:F2}, {c.Velocity.value.y:F2}, {c.Velocity.value.z:F2}, {c.Velocity.value.w:F2})";
                }
                else if (view.CategoryKey == "👁 Look Targets")
                {
                    enabled = _em.IsComponentEnabled<Look>(view.Entity);
                    var c = _em.GetComponentData<Look>(view.Entity);
                    bool isEntityTarget = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText   = isEntityTarget ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"({c.TargetPosition.x:F2}, {c.TargetPosition.y:F2}, {c.TargetPosition.z:F2})";
                    velocityText = $"({c.Velocity.x:F2}, {c.Velocity.y:F2}, {c.Velocity.z:F2})";
                }
                else if (view.CategoryKey == "📐 Chase Scale")
                {
                    enabled = _em.IsComponentEnabled<ChaseScale>(view.Entity);
                    var c = _em.GetComponentData<ChaseScale>(view.Entity);
                    bool isEntityTarget = _em.HasComponent<ChaseTargetEntity>(view.Entity);
                    targetText   = isEntityTarget ? $"Entity #{_em.GetComponentData<ChaseTargetEntity>(view.Entity).Target.Index}" : $"{c.TargetScale.x:F2}";
                    velocityText = $"{c.Velocity.x:F2}";
                }

                view.StatusDot.style.backgroundColor = enabled ? EntityweenUIStyleUtility.AccentGreen : Color.gray;
                view.EnableToggle.SetValueWithoutNotify(enabled);
                view.ValuesLabel.text = $"Target: {targetText}   •   Velocity: {velocityText}";
            }
        }

        private void GatherActiveSequences(out List<SequenceInfo> sequences)
        {
            sequences = new List<SequenceInfo>();

            var query    = _em.CreateEntityQuery(ComponentType.ReadOnly<Sequence>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var seq    = _em.GetComponentData<Sequence>(entity);

                string name = _em.GetName(entity);
                if (string.IsNullOrEmpty(name)) name = $"Sequence #{entity.Index}";
                if (!PassesFilter(name)) continue;

                float normalizedTime = 0f;
                bool  isLoop         = false;
                string loopDetails   = "";

                if (_em.HasComponent<PlaybackProgress>(entity))
                {
                    var progress  = _em.GetComponentData<PlaybackProgress>(entity);
                    normalizedTime = progress.NormalizedTime;
                    isLoop        = progress.LoopType != LoopType.None;
                    if (isLoop)
                        loopDetails = $"{progress.LoopType}{(progress.LoopCount > 0 ? $" (x{progress.LoopCount})" : " (∞)")}";
                }

                SequenceElementSnapshot[] snapshots = Array.Empty<SequenceElementSnapshot>();
                if (_em.HasBuffer<SequenceElement>(entity))
                {
                    var buf = _em.GetBuffer<SequenceElement>(entity);
                    snapshots = new SequenceElementSnapshot[buf.Length];
                    for (int j = 0; j < buf.Length; j++)
                    {
                        var elem = buf[j];
                        snapshots[j] = new SequenceElementSnapshot
                        {
                            Kind         = elem.Kind,
                            ActionEntity = elem.ActionEntity,
                            StartTime    = elem.StartTime,
                            Duration     = elem.Duration,
                            Started      = elem.Started,
                            Completed    = elem.Completed,
                            CallbackId   = elem.CallbackId.ToString()
                        };
                    }
                }

                sequences.Add(new SequenceInfo
                {
                    Entity         = entity,
                    Name           = name,
                    State          = seq.State,
                    Time           = seq.Time,
                    Duration       = seq.Duration,
                    NormalizedTime = normalizedTime,
                    IsLoop         = isLoop,
                    LoopDetails    = loopDetails,
                    TimeScale      = seq.TimeScale,
                    ElementCount   = snapshots.Length,
                    Elements       = snapshots
                });
            }
            entities.Dispose();
        }

        private bool CheckSequencesChanged(List<SequenceInfo> current)
        {
            if (current.Count != _lastSequences.Count) return true;
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].Entity       != _lastSequences[i].Entity       ||
                    current[i].Name         != _lastSequences[i].Name         ||
                    current[i].State        != _lastSequences[i].State        ||
                    current[i].ElementCount != _lastSequences[i].ElementCount ||
                    current[i].IsLoop       != _lastSequences[i].IsLoop)
                    return true;
            }
            return false;
        }

        private void RebuildSequencesUI()
        {
            _categoryContainers.Clear();
            _categoryBodies.Clear();
            _categoryBadges.Clear();
            _categoryArrows.Clear();
            _activeSequenceViews.Clear();

            ConfigureEmptyState(_lastSequences.Count == 0, "No active sequences.");

            _listView.itemsSource = _lastSequences;
            _listView.makeItem = () => new VisualElement();
            _listView.bindItem = (element, index) =>
            {
                element.Clear();
                if (index < 0 || index >= _lastSequences.Count) return;

                var card = CreateSequenceCard(_lastSequences[index], index % 2 == 0);
                element.Add(card.Root);
                _activeSequenceViews.Add(card);
            };
            _listView.unbindItem = (element, index) => element.Clear();
            _listView.RefreshItems();
            UpdateSequencesValues(false);
        }

        private SequenceRowView CreateSequenceCard(SequenceInfo seq, bool isEven)
        {
            var card = new SequenceRowView { SequenceEntity = seq.Entity };

            card.Root = new VisualElement();
            card.Root.style.marginBottom = 6;
            card.Root.style.marginTop    = 2;

            var headerBar = new VisualElement();
            headerBar.style.flexDirection  = FlexDirection.Row;
            headerBar.style.alignItems     = Align.Center;
            headerBar.style.backgroundColor = new Color(0.20f, 0.21f, 0.24f, 1f);
            headerBar.style.borderTopLeftRadius     = headerBar.style.borderTopRightRadius    = 8;
            headerBar.style.borderBottomLeftRadius  = headerBar.style.borderBottomRightRadius = 8;
            headerBar.style.borderTopWidth    = headerBar.style.borderBottomWidth = 1;
            headerBar.style.borderLeftWidth   = headerBar.style.borderRightWidth  = 1;
            headerBar.style.borderTopColor    = headerBar.style.borderBottomColor = new Color(0.35f, 0.28f, 0.5f, 0.7f);
            headerBar.style.borderLeftColor   = headerBar.style.borderRightColor  = new Color(0.35f, 0.28f, 0.5f, 0.7f);
            headerBar.style.paddingTop    = 8;
            headerBar.style.paddingBottom = 8;
            headerBar.style.paddingLeft   = 12;
            headerBar.style.paddingRight  = 12;

            var accentStrip = new VisualElement();
            accentStrip.style.width            = 3;
            accentStrip.style.height           = 28;
            accentStrip.style.backgroundColor  = EntityweenUIStyleUtility.AccentPurple;
            accentStrip.style.borderTopLeftRadius    = accentStrip.style.borderBottomLeftRadius = 2;
            accentStrip.style.marginRight      = 10;
            headerBar.Add(accentStrip);

            card.StatusDot = EntityweenUIStyleUtility.MakeStatusDot(EntityweenUIStyleUtility.StateColor(seq.State));
            headerBar.Add(card.StatusDot);

            card.NameLabel = new Label(seq.Name);
            card.NameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.NameLabel.style.fontSize   = 12;
            card.NameLabel.style.color      = Color.white;
            card.NameLabel.style.marginRight = 8;
            card.NameLabel.RegisterCallback<MouseDownEvent>(evt => PingEntity(seq.Entity));
            card.NameLabel.RegisterCallback<MouseOverEvent>(evt => card.NameLabel.style.color = EntityweenUIStyleUtility.AccentPurple);
            card.NameLabel.RegisterCallback<MouseOutEvent>(evt => card.NameLabel.style.color = Color.white);
            headerBar.Add(card.NameLabel);

            var entityBadge = new Label($"#{seq.Entity.Index}");
            EntityweenUIStyleUtility.StyleMiniChip(entityBadge, new Color(0.22f, 0.23f, 0.25f, 1f), new Color(0.6f, 0.6f, 0.65f, 1f));
            entityBadge.RegisterCallback<MouseDownEvent>(evt => PingEntity(seq.Entity));
            headerBar.Add(entityBadge);

            card.StateLabel = new Label(seq.State.ToString().ToUpper());
            EntityweenUIStyleUtility.StyleMiniChip(card.StateLabel, EntityweenUIStyleUtility.StateBgColor(seq.State), EntityweenUIStyleUtility.StateColor(seq.State));
            card.StateLabel.style.marginLeft = 6;
            headerBar.Add(card.StateLabel);

            if (seq.IsLoop)
            {
                var loopBadge = EntityweenUIStyleUtility.CreateMiniChipWithIcon("🔁", seq.LoopDetails, new Color(0.22f, 0.23f, 0.25f, 1f), EntityweenUIStyleUtility.AccentGold);
                loopBadge.style.marginLeft = 4;
                headerBar.Add(loopBadge);
            }

            if (Mathf.Abs(seq.TimeScale - 1f) > 0.001f)
            {
                var tsBadge = EntityweenUIStyleUtility.CreateMiniChipWithIcon("⏱", $"×{seq.TimeScale:F2}", new Color(0.22f, 0.23f, 0.25f, 1f), new Color(0.9f, 0.7f, 0.3f));
                tsBadge.style.marginLeft = 4;
                headerBar.Add(tsBadge);
            }

            var elemBadge = new Label($"{seq.ElementCount} steps");
            EntityweenUIStyleUtility.StyleMiniChip(elemBadge, new Color(0.18f, 0.18f, 0.22f, 1f), EntityweenUIStyleUtility.AccentPurple);
            elemBadge.style.marginLeft = 4;
            headerBar.Add(elemBadge);

            var hSpacer = new VisualElement();
            hSpacer.style.flexGrow = 1;
            headerBar.Add(hSpacer);

            card.TimeLabel = new Label($"{seq.Time:F2}s / {seq.Duration:F2}s");
            card.TimeLabel.style.fontSize = 9;
            card.TimeLabel.style.color    = new Color(0.6f, 0.6f, 0.65f);
            card.TimeLabel.style.width    = 80;
            card.TimeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            headerBar.Add(card.TimeLabel);

            var pauseBtn = new Button();
            pauseBtn.text = seq.State == PlaybackState.Paused ? "▶" : "⏸";
            EntityweenUIStyleUtility.StyleActionButton(pauseBtn, new Color(0.24f, 0.25f, 0.27f, 1f), EntityweenUIStyleUtility.DarkBorder,
                seq.State == PlaybackState.Paused ? EntityweenUIStyleUtility.AccentGreen : Color.white);
            pauseBtn.style.marginLeft = 6;
            pauseBtn.clicked += () =>
            {
                if (!_em.Exists(seq.Entity)) return;
                var s = _em.GetComponentData<Sequence>(seq.Entity);
                if (s.State == PlaybackState.Paused)
                    PlaybackControlInternal.ResumeInternal(seq.Entity, _em);
                else
                    PlaybackControlInternal.PauseInternal(seq.Entity, _em);
                CheckStateAndRebuild(true);
            };
            headerBar.Add(pauseBtn);

            var killBtn = new Button();
            killBtn.text = "✕";
            EntityweenUIStyleUtility.StyleActionButton(killBtn, new Color(0.4f, 0.15f, 0.15f, 1f), new Color(0.5f, 0.2f, 0.2f, 1f), EntityweenUIStyleUtility.AccentRed);
            killBtn.style.marginLeft = 3;
            killBtn.clicked += () =>
            {
                if (_em.Exists(seq.Entity))
                {
                    _em.DestroyEntity(seq.Entity);
                    CheckStateAndRebuild(true);
                }
            };
            headerBar.Add(killBtn);

            card.Root.Add(headerBar);

            var progressRow = new VisualElement();
            progressRow.style.flexDirection = FlexDirection.Row;
            progressRow.style.alignItems    = Align.Center;
            progressRow.style.marginTop     = 4;
            progressRow.style.paddingLeft   = 4;
            progressRow.style.paddingRight  = 4;

            var barBg = EntityweenUIStyleUtility.MakeProgressBarBg();
            barBg.style.height = 5;
            card.ProgressFill = EntityweenUIStyleUtility.MakeProgressFill(EntityweenUIStyleUtility.AccentPurple);
            card.ProgressFill.style.width = Length.Percent(seq.NormalizedTime * 100f);
            barBg.Add(card.ProgressFill);
            progressRow.Add(barBg);

            card.ProgressPercentLabel = new Label($"{seq.NormalizedTime * 100f:F0}%");
            EntityweenUIStyleUtility.StyleProgressLabel(card.ProgressPercentLabel, 28, TextAnchor.MiddleRight);
            progressRow.Add(card.ProgressPercentLabel);

            card.Root.Add(progressRow);

            card.TimelineContainer = new VisualElement();
            card.TimelineContainer.style.marginTop  = 6;
            card.TimelineContainer.style.paddingLeft = 4;
            PopulateTimelineElements(seq, card.TimelineContainer);
            card.Root.Add(card.TimelineContainer);

            return card;
        }

        private void PopulateTimelineElements(SequenceInfo seq, VisualElement container)
        {
            container.Clear();
            if (seq.Elements == null || seq.Elements.Length == 0) return;

            float totalDuration = seq.Duration > 0f ? seq.Duration : 1f;

            var rulerRow = new VisualElement();
            rulerRow.style.flexDirection  = FlexDirection.Row;
            rulerRow.style.alignItems     = Align.Center;
            rulerRow.style.marginBottom   = 4;

            var rulerLabel = new Label("TIMELINE");
            rulerLabel.style.fontSize   = 8;
            rulerLabel.style.color      = new Color(0.45f, 0.45f, 0.48f);
            rulerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            rulerLabel.style.marginRight = 6;
            rulerRow.Add(rulerLabel);

            var rulerLine = new VisualElement();
            rulerLine.style.flexGrow         = 1;
            rulerLine.style.height           = 1;
            rulerLine.style.backgroundColor  = new Color(0.25f, 0.25f, 0.28f);
            rulerRow.Add(rulerLine);

            var durationLabel = new Label($"{totalDuration:F2}s");
            durationLabel.style.fontSize  = 8;
            durationLabel.style.color     = new Color(0.45f, 0.45f, 0.48f);
            durationLabel.style.marginLeft = 6;
            rulerRow.Add(durationLabel);

            container.Add(rulerRow);

            var ganttContainer = new VisualElement();
            ganttContainer.style.position        = Position.Relative;
            ganttContainer.style.height          = 20;
            ganttContainer.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f);
            ganttContainer.style.borderTopLeftRadius    = ganttContainer.style.borderTopRightRadius    = 4;
            ganttContainer.style.borderBottomLeftRadius = ganttContainer.style.borderBottomRightRadius = 4;
            ganttContainer.style.overflow        = Overflow.Hidden;
            ganttContainer.style.marginBottom    = 6;

            foreach (var elem in seq.Elements)
            {
                if (elem.Kind == TimelineActionKind.Callback) continue;

                float startPct    = (elem.StartTime / totalDuration) * 100f;
                float durationPct = Mathf.Max(0.5f, (elem.Duration / totalDuration) * 100f);

                var bar = new VisualElement();
                bar.style.position    = Position.Absolute;
                bar.style.left        = Length.Percent(startPct);
                bar.style.width       = Length.Percent(durationPct);
                bar.style.top         = 2;
                bar.style.bottom      = 2;
                bar.style.backgroundColor = EntityweenUIStyleUtility.ElementKindColor(elem.Kind, elem.Completed, elem.Started);
                bar.style.borderTopLeftRadius    = bar.style.borderTopRightRadius    = 3;
                bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 3;
                ganttContainer.Add(bar);
            }

            var playhead = new VisualElement();
            playhead.style.position         = Position.Absolute;
            playhead.style.width            = 2;
            playhead.style.top              = 0;
            playhead.style.bottom           = 0;
            playhead.style.backgroundColor  = EntityweenUIStyleUtility.AccentPurple;
            playhead.style.left             = Length.Percent(seq.NormalizedTime * 100f);
            ganttContainer.Add(playhead);

            container.Add(ganttContainer);

            for (int j = 0; j < seq.Elements.Length; j++)
            {
                var elem = seq.Elements[j];
                var row  = CreateTimelineElementRow(elem, j, totalDuration);
                container.Add(row);
            }
        }

        private VisualElement CreateTimelineElementRow(SequenceElementSnapshot elem, int index, float totalDuration)
        {
            var row = new VisualElement();
            row.style.flexDirection   = FlexDirection.Row;
            row.style.alignItems      = Align.Center;
            row.style.paddingTop      = 3;
            row.style.paddingBottom   = 3;
            row.style.paddingLeft     = 6;
            row.style.paddingRight    = 6;
            row.style.marginBottom    = 2;
            row.style.backgroundColor = index % 2 == 0
                ? new Color(0.15f, 0.15f, 0.18f, 0.5f)
                : new Color(0.13f, 0.13f, 0.16f, 0.5f);
            row.style.borderTopLeftRadius    = row.style.borderTopRightRadius    = 4;
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 4;

            Color kindAccent = EntityweenUIStyleUtility.ElementKindAccentColor(elem.Kind);
            var dot = new VisualElement();
            dot.style.width  = 6;
            dot.style.height = 6;
            dot.style.borderTopLeftRadius    = dot.style.borderTopRightRadius    = 3;
            dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 3;
            dot.style.backgroundColor = elem.Completed ? new Color(0.35f, 0.35f, 0.38f) : kindAccent;
            dot.style.marginRight = 6;
            row.Add(dot);

            string iconText = elem.Kind switch
            {
                TimelineActionKind.Tween    => "🎬",
                TimelineActionKind.Chase    => "🎯",
                TimelineActionKind.Wait     => "⏳",
                TimelineActionKind.Callback => "📣",
                _ => "?"
            };
            string labelText = elem.Kind.ToString();
            var kindLabel = EntityweenUIStyleUtility.CreateLabelWithIcon(
                iconText,
                labelText,
                9,
                elem.Completed ? new Color(0.4f, 0.4f, 0.43f) : kindAccent,
                true,
                4f
            );
            kindLabel.style.width = 70;
            row.Add(kindLabel);

            var timingLabel = new Label($"@{elem.StartTime:F2}s  +{elem.Duration:F2}s");
            timingLabel.style.fontSize = 9;
            timingLabel.style.color    = new Color(0.55f, 0.55f, 0.58f);
            timingLabel.style.width    = 90;
            row.Add(timingLabel);

            if (elem.Kind == TimelineActionKind.Callback && !string.IsNullOrEmpty(elem.CallbackId))
            {
                var cbLabel = EntityweenUIStyleUtility.CreateLabelWithIcon("📣", $"\"{elem.CallbackId}\"", 9, EntityweenUIStyleUtility.AccentGold, true, 4f);
                cbLabel.style.flexGrow  = 1;
                row.Add(cbLabel);
            }
            else if (elem.ActionEntity != Entity.Null)
            {
                var entityRef = new Label($"Ghost #{elem.ActionEntity.Index}");
                entityRef.style.fontSize  = 9;
                entityRef.style.color     = new Color(0.5f, 0.5f, 0.55f);
                entityRef.style.flexGrow  = 1;
                row.Add(entityRef);
            }
            else
            {
                var filler = new VisualElement();
                filler.style.flexGrow = 1;
                row.Add(filler);
            }

            if (elem.Started && !elem.Completed && elem.Duration > 0f && elem.ActionEntity != Entity.Null)
            {
                var miniBarBg = new VisualElement();
                miniBarBg.style.width           = 60;
                miniBarBg.style.height          = 4;
                miniBarBg.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f);
                miniBarBg.style.borderTopLeftRadius    = miniBarBg.style.borderTopRightRadius    = 2;
                miniBarBg.style.borderBottomLeftRadius = miniBarBg.style.borderBottomRightRadius = 2;
                miniBarBg.style.overflow        = Overflow.Hidden;
                miniBarBg.style.marginLeft      = 6;

                var miniBarFill = new VisualElement();
                miniBarFill.style.height          = Length.Percent(100);
                miniBarFill.style.backgroundColor = kindAccent;

                float localProgress = 0f;
                if (_em.Exists(elem.ActionEntity) && _em.HasComponent<PlaybackProgress>(elem.ActionEntity))
                    localProgress = _em.GetComponentData<PlaybackProgress>(elem.ActionEntity).NormalizedTime;
                else if (_em.Exists(elem.ActionEntity) && _em.HasComponent<ChasePosition>(elem.ActionEntity))
                    localProgress = 0.5f;

                miniBarFill.style.width = Length.Percent(localProgress * 100f);
                miniBarBg.Add(miniBarFill);
                row.Add(miniBarBg);
            }

            string statusText = elem.Completed ? "✓ Done" : elem.Started ? "▶ Active" : "○ Pending";
            Color  statusColor = elem.Completed
                ? new Color(0.35f, 0.35f, 0.38f)
                : elem.Started
                    ? kindAccent
                    : new Color(0.45f, 0.45f, 0.48f);

            var statusLabel = new Label(statusText);
            statusLabel.style.fontSize  = 8;
            statusLabel.style.color     = statusColor;
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusLabel.style.width     = 52;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(statusLabel);

            return row;
        }

        private void UpdateSequencesValues(bool refreshList = true)
        {
            if (refreshList)
            {
                _activeSequenceViews.Clear();
                _listView.RefreshItems();
            }

            GatherActiveSequences(out var current);

            for (int i = 0; i < _activeSequenceViews.Count && i < current.Count; i++)
            {
                var view = _activeSequenceViews[i];
                var seq = default(SequenceInfo);
                var found = false;
                for (int j = 0; j < current.Count; j++)
                {
                    if (current[j].Entity != view.SequenceEntity) continue;
                    seq = current[j];
                    found = true;
                    break;
                }
                if (!found) continue;

                if (!_em.Exists(view.SequenceEntity)) continue;

                view.StatusDot.style.backgroundColor = EntityweenUIStyleUtility.StateColor(seq.State);
                view.StateLabel.text  = seq.State.ToString().ToUpper();
                view.StateLabel.style.color = EntityweenUIStyleUtility.StateColor(seq.State);
                EntityweenUIStyleUtility.StyleMiniChipBg(view.StateLabel, EntityweenUIStyleUtility.StateBgColor(seq.State));
                view.TimeLabel.text = $"{seq.Time:F2}s / {seq.Duration:F2}s";
                view.ProgressFill.style.width = Length.Percent(seq.NormalizedTime * 100f);
                view.ProgressPercentLabel.text = $"{seq.NormalizedTime * 100f:F0}%";

                PopulateTimelineElements(seq, view.TimelineContainer);
            }
        }

        private void PingEntity(Entity entity)
        {
            EntityweenDebugUtility.PingEntity(entity);
            Debug.Log($"[Entityween] Selected Entity: {entity.Index}:{entity.Version}");
        }

        private bool PassesFilter(string text)
            => string.IsNullOrEmpty(_searchFilter) ||
               text.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
