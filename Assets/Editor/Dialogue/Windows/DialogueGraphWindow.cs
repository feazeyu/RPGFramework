using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Dialogue-system graph editor window.
    ///
    /// All layout, toolbar, blackboard and inspector UI — including the
    /// stylesheet loading pipeline — lives in <see cref="GraphEditorWindow"/>.
    /// This subclass only supplies the dialogue-specific window title,
    /// node palette, theme sheet, CSS class, asset factory and menu hooks.
    /// </summary>
    public class DialogueGraphWindow : GraphEditorWindow
    {
        // ── Template-method overrides ────────────────────────────────────────

        protected override string WindowTitle     => "Dialogue Graph";
        protected override string PrefKeyPrefix   => "DialogueGraph.Editor";
        protected override string NewAssetName    => "NewDialogueGraph";
        protected override string SaveDialogTitle => "New Dialogue Graph";
        protected override string WindowIcon      => "◈";

        protected override IReadOnlyDictionary<string, DialogueNodeInfo> NodeRegistrySource
            => NodeRegistry.All;

        protected override StyleSheet ThemeStyleSheet => DialogueGraphStyleSheet.Get();

        protected override string GraphViewCssClass => "dialogue-graph-view";

        protected override GraphAsset CreateAssetInstance()
            => CreateInstance<DialogueGraphAsset>();

        // ── Open ─────────────────────────────────────────────────────────────

        [MenuItem("Window/Dialogue Graph", priority = 200)]
        public static DialogueGraphWindow Open()
            => GetWindow<DialogueGraphWindow>();

        /// <summary>Open with a specific asset (called from asset double-click).</summary>
        public static void Open(DialogueGraphAsset asset)
        {
            var win = GetWindow<DialogueGraphWindow>();
            win.LoadAsset(asset);
        }
    }

    // ── Asset double-click handler ───────────────────────────────────────────

    public static class DialogueGraphAssetOpener
    {
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as DialogueGraphAsset;
            if (asset == null) return false;
            DialogueGraphWindow.Open(asset);
            return true;
        }
    }
}
