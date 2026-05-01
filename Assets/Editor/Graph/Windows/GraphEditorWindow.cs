using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Shared base for every graph-editor window (dialogue, quest, and
    /// anything built on top of the <see cref="GraphAsset"/> pipeline).
    ///
    /// Responsibilities:
    ///   • Build the toolbar / blackboard / canvas / inspector layout
    ///   • Load the base <c>GraphEditor.uss</c> stylesheet
    ///   • Wire node-selection events between the canvas and inspector
    ///   • Handle asset New / Open / Save / Auto-Save
    ///
    /// Subclasses supply what's system-specific via the template-method
    /// properties at the top:
    /// <list type="bullet">
    /// <item><description><see cref="WindowTitle"/> — window title string</description></item>
    /// <item><description><see cref="PrefKeyPrefix"/> — EditorPrefs namespace for last-asset memory</description></item>
    /// <item><description><see cref="NewAssetName"/>, <see cref="SaveDialogTitle"/>, <see cref="WindowIcon"/></description></item>
    /// <item><description><see cref="NodeRegistrySource"/> — the "Add Node" palette</description></item>
    /// <item><description><see cref="ThemeStyleSheet"/> — system-specific colour overrides</description></item>
    /// <item><description><see cref="GraphViewCssClass"/> — root class added to the canvas (drives theme rules)</description></item>
    /// <item><description><see cref="CreateAssetInstance"/> — factory for the right subclass of <see cref="GraphAsset"/></description></item>
    /// </list>
    /// </summary>
    public class GraphEditorWindow : EditorWindow
    {
        // ── Template-method hooks ────────────────────────────────────────────

        protected virtual string WindowTitle     => "Graph Editor";
        protected virtual string PrefKeyPrefix   => "GraphEditor";
        protected virtual string NewAssetName    => "NewGraph";
        protected virtual string SaveDialogTitle => "New Graph";
        protected virtual string WindowIcon      => "◈";

        /// <summary>"Add Node" palette for this graph system.</summary>
        protected virtual IReadOnlyDictionary<string, DialogueNodeInfo> NodeRegistrySource
            => NodeRegistry.All;

        /// <summary>
        /// Per-asset registry hook — called on every <see cref="LoadAsset"/>.
        /// Override when the palette depends on properties of the loaded
        /// asset (e.g. QuestGraphWindow filters by QuestKind). The default
        /// returns <see cref="NodeRegistrySource"/>.
        /// </summary>
        protected virtual IReadOnlyDictionary<string, DialogueNodeInfo> GetNodeRegistryForAsset(GraphAsset asset)
            => NodeRegistrySource;

        /// <summary>System-specific theme sheet, layered on top of <c>GraphEditor.uss</c>.</summary>
        protected virtual StyleSheet ThemeStyleSheet => DialogueGraphStyleSheet.Get();

        /// <summary>Root CSS class added to the canvas so the theme sheet's <c>.xxx-graph-view</c> rules apply.</summary>
        protected virtual string GraphViewCssClass => "dialogue-graph-view";

        /// <summary>Factory for the concrete subclass of <see cref="GraphAsset"/> this window edits.</summary>
        protected virtual GraphAsset CreateAssetInstance()
            => CreateInstance<DialogueGraphAsset>();

        // ── State ────────────────────────────────────────────────────────────

        protected GraphAsset m_Asset;

        private DialogueGraphView m_GraphView;
        private BlackboardPanel   m_BlackboardPanel;
        private InspectorPanel    m_InspectorPanel;
        private Label             m_AssetLabel;
        private bool              m_AutoSave = true;

        // ── Lifecycle ────────────────────────────────────────────────────────

        protected virtual void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle,
                EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            minSize = new Vector2(900, 500);
            BuildUI();

            if (m_Asset == null)
            {
                var path = EditorPrefs.GetString(PrefKey("lastAssetPath"), "");
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GraphAsset>(path);
                    if (asset) LoadAsset(asset);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (m_AutoSave) SaveAsset();
        }

        // ── UI Construction ──────────────────────────────────────────────────

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("graph-editor-window");

            // Shared base stylesheet (structural styles for every system).
            var baseSheet = GraphEditorStyleSheet.Get();
            if (baseSheet != null) rootVisualElement.styleSheets.Add(baseSheet);

            BuildToolbar();
            BuildBody();
        }

        private void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("graph-toolbar");

            var icon = new Label(WindowIcon);
            icon.AddToClassList("graph-toolbar-icon");
            toolbar.Add(icon);

            m_AssetLabel = new Label("No Graph Loaded");
            m_AssetLabel.AddToClassList("graph-toolbar-asset-label");
            toolbar.Add(m_AssetLabel);

            toolbar.Add(MakeToolbarBtn("New",  CreateNewAsset));
            toolbar.Add(MakeToolbarBtn("Open", OpenAssetPicker));
            toolbar.Add(MakeToolbarSeparator());
            toolbar.Add(MakeToolbarBtn("Save", SaveAsset, primary: true));

            var autoSave = new Toggle("Auto Save") { value = m_AutoSave };
            autoSave.AddToClassList("graph-toolbar-autosave");
            autoSave.RegisterValueChangedCallback(evt => m_AutoSave = evt.newValue);
            toolbar.Add(autoSave);

            rootVisualElement.Add(toolbar);
        }

        private void BuildBody()
        {
            var body = new VisualElement();
            body.AddToClassList("graph-body");
            rootVisualElement.Add(body);

            m_BlackboardPanel = new BlackboardPanel();
            m_BlackboardPanel.AddToClassList("graph-blackboard-wrapper");
            body.Add(m_BlackboardPanel);

            m_GraphView = new DialogueGraphView(
                this,
                NodeRegistrySource,
                ThemeStyleSheet,
                GraphViewCssClass);
            m_GraphView.style.flexGrow   = 1;
            m_GraphView.OnNodeSelected   = OnNodeSelected;
            m_GraphView.OnNodeDeselected = OnNodeDeselected;
            body.Add(m_GraphView);

            // Now that the graph view exists, wire its refresh hook into
            // the blackboard panel so variable deletion can cascade into
            // the canvas (stale LinkedVariableGuids on affected nodes
            // are cleared, and the views rebuild). We route through a
            // local method so the inspector also picks up the change
            // when the currently-inspected node is one of the affected
            // set.
            m_BlackboardPanel.SetRefreshNodeViewCallback(OnBlackboardCascadeToNode);

            m_InspectorPanel = new InspectorPanel();
            m_InspectorPanel.AddToClassList("graph-inspector-wrapper");
            body.Add(m_InspectorPanel);
        }

        // ── Asset management ─────────────────────────────────────────────────

        public void LoadAsset(GraphAsset asset)
        {
            if (asset == null) return;
            if (m_Asset != null) BlackboardPropertyBridge.Invalidate(m_Asset);

            m_Asset = asset;
            m_AssetLabel.text = asset.name;
            EditorPrefs.SetString(PrefKey("lastAssetPath"), AssetDatabase.GetAssetPath(asset));

            // Palette may depend on the asset (e.g. QuestKind) — refresh
            // before Populate so any auto-created node views pick it up.
            m_GraphView.SetNodeRegistry(GetNodeRegistryForAsset(asset));

            m_GraphView.Populate(asset);
            m_BlackboardPanel.Populate(asset);
            m_InspectorPanel.Clear();
        }

        protected void SaveAsset()
        {
            if (m_Asset == null) return;
            m_GraphView.FlushViewState();
            EditorUtility.SetDirty(m_Asset);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Saved"), 0.8f);
        }

        private void CreateNewAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                SaveDialogTitle, NewAssetName, "asset",
                $"Choose a location for the new {WindowTitle} asset.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateAssetInstance();
            OnSeedNewAsset(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            LoadAsset(asset);
        }

        /// <summary>Hook for subclasses to insert default nodes into a freshly created asset.</summary>
        protected virtual void OnSeedNewAsset(GraphAsset asset)
        {
            asset.AddNode(NodeRegistry.TypeStart, "Start", new Vector2(80, 160));
        }

        private void OpenAssetPicker()
        {
            var path = EditorUtility.OpenFilePanel($"Open {WindowTitle}", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            var asset = AssetDatabase.LoadAssetAtPath<GraphAsset>(path);
            if (asset) LoadAsset(asset);
            else Debug.LogWarning($"[{WindowTitle}] Could not load asset at {path}");
        }

        private void OnNodeSelected(NodeData node)
        {
            if (m_Asset != null)
                m_InspectorPanel.InspectNode(node, m_Asset, m_GraphView.RefreshNodeView);
        }

        private void OnNodeDeselected()
        {
            m_InspectorPanel.InspectAsset(m_Asset);
        }

        /// <summary>
        /// Invoked once per affected node when the blackboard panel
        /// mutates node fields (currently: variable deletion clearing
        /// stale <c>LinkedVariableGuid</c>s). Refreshes the canvas
        /// node view, and refreshes the inspector too if it's showing
        /// the affected node.
        /// </summary>
        private void OnBlackboardCascadeToNode(string nodeGuid)
        {
            m_GraphView.RefreshNodeView(nodeGuid);
            if (m_InspectorPanel.CurrentNodeGuid == nodeGuid)
                m_InspectorPanel.RefreshCurrent();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        protected string PrefKey(string key) => $"{PrefKeyPrefix}.{key}";

        private static VisualElement MakeToolbarSeparator()
        {
            var sep = new VisualElement();
            sep.AddToClassList("graph-toolbar-separator");
            return sep;
        }

        internal static Button MakeToolbarBtn(string text, System.Action onClick, bool primary = false)
        {
            var btn = new Button(onClick) { text = text };
            btn.AddToClassList("graph-toolbar-btn");
            if (primary) btn.AddToClassList("graph-toolbar-btn-primary");
            return btn;
        }
    }
}
