using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;
using DialogueGraph.Editor;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// The main editor window. Mirrors the layout of Unity's BehaviorWindow:
    ///   ┌───────────────────────────────────────────────────────┐
    ///   │  Toolbar (asset name | Save | Auto-save | Debug)      │
    ///   ├──────────────┬────────────────────────┬───────────────┤
    ///   │  Blackboard  │     Graph Canvas        │   Inspector   │
    ///   │  (240 px)    │     (fills rest)        │   (280 px)    │
    ///   └──────────────┴────────────────────────┴───────────────┘
    ///
    /// Open via:  Window → Dialogue Graph
    ///       or:  double-clicking a DialogueGraphAsset in the Project view.
    /// </summary>
    public class DialogueGraphWindow : EditorWindow
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const string WindowTitle    = "Dialogue Graph";
        private const float  BlackboardW    = 240f;
        private const float  InspectorW     = 280f;
        private const float  ToolbarH       = 26f;

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueGraphAsset m_Asset;

        // Sub-panels
        private DialogueGraphView  m_GraphView;
        private BlackboardPanel    m_BlackboardPanel;
        private InspectorPanel     m_InspectorPanel;

        // UI containers
        private VisualElement m_Toolbar;
        private VisualElement m_Body;
        private Label         m_AssetLabel;
        private Toggle        m_AutoSaveToggle;

        private bool m_AutoSave = true;

        // ── Open ──────────────────────────────────────────────────────────────

        [MenuItem("Window/Dialogue Graph", priority = 200)]
        public static DialogueGraphWindow Open()
            => GetWindow<DialogueGraphWindow>(WindowTitle);

        /// <summary>Open with a specific asset (called from asset double-click).</summary>
        public static void Open(DialogueGraphAsset asset)
        {
            var win = GetWindow<DialogueGraphWindow>(WindowTitle);
            win.LoadAsset(asset);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle, EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            minSize = new Vector2(900, 500);
            BuildUI();

            // Re-open the last used asset after domain reload.
            if (m_Asset == null)
            {
                var path = EditorPrefs.GetString(PrefKey("lastAssetPath"), "");
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);
                    if (asset) LoadAsset(asset);
                }
            }

            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            if (m_AutoSave) SaveAsset();
        }

        private void OnSelectionChanged()
        {
            // Sync inspector when a node is selected inside the GraphView.
            // The GraphView raises its own event; this handles external selections.
        }

        // ── UI Construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            rootVisualElement.Clear();
            ApplyRootStyle();

            BuildToolbar();
            BuildBody();
        }

        private void ApplyRootStyle()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = StyleColor(0.11f, 0.12f, 0.15f);
        }

        private void BuildToolbar()
        {
            m_Toolbar = new VisualElement();
            m_Toolbar.style.flexDirection     = FlexDirection.Row;
            m_Toolbar.style.alignItems        = Align.Center;
            m_Toolbar.style.height            = ToolbarH;
            m_Toolbar.style.paddingLeft       = 6;
            m_Toolbar.style.paddingRight      = 8;
            m_Toolbar.style.backgroundColor  = StyleColor(0.14f, 0.15f, 0.19f);
            m_Toolbar.style.borderBottomWidth = 1;
            m_Toolbar.style.borderBottomColor = StyleColor(0.08f, 0.09f, 0.12f);

            // Window icon/title
            var iconLabel = new Label("◈");
            iconLabel.style.fontSize   = 13;
            iconLabel.style.color      = StyleColor(0.32f, 0.72f, 0.58f);
            iconLabel.style.marginRight = 6;
            m_Toolbar.Add(iconLabel);

            // Asset name label
            m_AssetLabel = new Label("No Graph Loaded");
            m_AssetLabel.style.fontSize   = 11;
            m_AssetLabel.style.color      = StyleColor(0.75f, 0.80f, 0.90f);
            m_AssetLabel.style.flexGrow   = 1;
            m_Toolbar.Add(m_AssetLabel);

            // New asset button
            m_Toolbar.Add(MakeToolbarButton("New", () => CreateNewAsset()));

            // Open button
            m_Toolbar.Add(MakeToolbarButton("Open", () => OpenAssetPicker()));

            // Separator
            m_Toolbar.Add(MakeToolbarSeparator());

            // Save button
            m_Toolbar.Add(MakeToolbarButton("Save", () => SaveAsset(), isPrimary: true));

            // Auto-save toggle
            m_AutoSaveToggle = new Toggle("Auto Save");
            m_AutoSaveToggle.value    = m_AutoSave;
            m_AutoSaveToggle.style.fontSize   = 10;
            m_AutoSaveToggle.style.color      = StyleColor(0.60f, 0.65f, 0.75f);
            m_AutoSaveToggle.style.marginLeft = 8;
            m_AutoSaveToggle.RegisterValueChangedCallback(evt => m_AutoSave = evt.newValue);
            m_Toolbar.Add(m_AutoSaveToggle);

            rootVisualElement.Add(m_Toolbar);
        }

        private void BuildBody()
        {
            m_Body = new VisualElement();
            m_Body.style.flexDirection = FlexDirection.Row;
            m_Body.style.flexGrow      = 1;
            rootVisualElement.Add(m_Body);

            // ── Blackboard (left panel) ─────────────────────────────────────
            m_BlackboardPanel = new BlackboardPanel();
            m_BlackboardPanel.style.width           = BlackboardW;
            m_BlackboardPanel.style.minWidth        = 160;
            m_BlackboardPanel.style.maxWidth        = 360;
            m_BlackboardPanel.style.borderRightWidth = 1;
            m_BlackboardPanel.style.borderRightColor = StyleColor(0.08f, 0.09f, 0.12f);
            m_Body.Add(m_BlackboardPanel);

            // ── Graph canvas (centre, fills remaining space) ────────────────
            m_GraphView = new DialogueGraphView(this);
            m_GraphView.style.flexGrow = 1;
            m_GraphView.OnNodeSelected   = OnNodeSelected;
            m_GraphView.OnNodeDeselected = OnNodeDeselected;
            m_Body.Add(m_GraphView);

            // ── Inspector (right panel) ─────────────────────────────────────
            m_InspectorPanel = new InspectorPanel();
            m_InspectorPanel.style.width            = InspectorW;
            m_InspectorPanel.style.minWidth         = 200;
            m_InspectorPanel.style.maxWidth         = 400;
            m_InspectorPanel.style.borderLeftWidth  = 1;
            m_InspectorPanel.style.borderLeftColor  = StyleColor(0.08f, 0.09f, 0.12f);
            m_Body.Add(m_InspectorPanel);
        }

        // ── Asset Management ──────────────────────────────────────────────────

        public void LoadAsset(DialogueGraphAsset asset)
        {
            if (asset == null) return;

            // Invalidate cached SerializedObject for any previously loaded asset.
            if (m_Asset != null) BlackboardPropertyBridge.Invalidate(m_Asset);

            m_Asset = asset;

            m_AssetLabel.text = asset.name;
            EditorPrefs.SetString(PrefKey("lastAssetPath"), AssetDatabase.GetAssetPath(asset));

            m_GraphView.Populate(asset);
            m_BlackboardPanel.Populate(asset);
            m_InspectorPanel.Clear();
        }

        private void SaveAsset()
        {
            if (m_Asset == null) return;
            // Flush pending view-transform back to the asset.
            m_GraphView.FlushViewState();
            EditorUtility.SetDirty(m_Asset);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Saved"), 0.8f);
        }

        private void CreateNewAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "New Dialogue Graph", "NewDialogueGraph", "asset",
                "Choose a location for the new Dialogue Graph asset.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<DialogueGraphAsset>();
            // Seed with a Start node.
            asset.AddNode(NodeRegistry.TypeStart, "Start", new Vector2(80, 160));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            LoadAsset(asset);
        }

        private void OpenAssetPicker()
        {
            var path = EditorUtility.OpenFilePanel(
                "Open Dialogue Graph", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);
            var asset = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);
            if (asset) LoadAsset(asset);
            else Debug.LogWarning($"[DialogueGraph] Could not load asset at {path}");
        }

        // ── Node selection callbacks ──────────────────────────────────────────

        private void OnNodeSelected(NodeData node)
        {
            if (m_Asset != null)
                m_InspectorPanel.InspectNode(node, m_Asset, m_GraphView);
        }

        private void OnNodeDeselected()
        {
            m_InspectorPanel.InspectAsset(m_Asset);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static StyleColor StyleColor(float r, float g, float b, float a = 1f)
            => new StyleColor(new Color(r, g, b, a));

        private static VisualElement MakeToolbarSeparator()
        {
            var sep = new VisualElement();
            sep.style.width           = 1;
            sep.style.height          = 14;
            sep.style.backgroundColor = StyleColor(0.22f, 0.24f, 0.30f);
            sep.style.marginLeft      = 6;
            sep.style.marginRight     = 6;
            return sep;
        }

        private static Button MakeToolbarButton(string text, System.Action onClick, bool isPrimary = false)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.fontSize         = 10;
            btn.style.paddingLeft      = 8;
            btn.style.paddingRight     = 8;
            btn.style.paddingTop       = 2;
            btn.style.paddingBottom    = 2;
            btn.style.marginLeft       = 3;
            btn.style.borderTopLeftRadius     = 3;
            btn.style.borderTopRightRadius    = 3;
            btn.style.borderBottomLeftRadius  = 3;
            btn.style.borderBottomRightRadius = 3;
            btn.style.color = isPrimary
                ? StyleColor(0.2f, 0.85f, 0.6f)
                : StyleColor(0.70f, 0.75f, 0.85f);
            btn.style.backgroundColor = isPrimary
                ? StyleColor(0.12f, 0.28f, 0.22f)
                : StyleColor(0.18f, 0.20f, 0.26f);
            btn.style.borderTopColor     = StyleColor(0.08f, 0.09f, 0.12f);
            btn.style.borderBottomColor  = StyleColor(0.08f, 0.09f, 0.12f);
            btn.style.borderLeftColor    = StyleColor(0.08f, 0.09f, 0.12f);
            btn.style.borderRightColor   = StyleColor(0.08f, 0.09f, 0.12f);
            btn.style.borderTopWidth     = 1;
            btn.style.borderBottomWidth  = 1;
            btn.style.borderLeftWidth    = 1;
            btn.style.borderRightWidth   = 1;
            return btn;
        }

        private static string PrefKey(string key) => $"DialogueGraph.Editor.{key}";
    }

    // ── Asset double-click handler ────────────────────────────────────────────

    [UnityEditor.Callbacks.OnOpenAsset]
    public static class DialogueGraphAssetOpener
    {
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as DialogueGraphAsset;
            if (asset == null) return false;
            DialogueGraphWindow.Open(asset);
            return true;
        }
    }
}
