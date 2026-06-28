using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Feazeyu.RPGSystems.Dialogue;

namespace Feazeyu.RPGSystems.EditorTools
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

        /// <inheritdoc/>
        protected override string WindowTitle     => "Dialogue Graph";
        /// <inheritdoc/>
        protected override string PrefKeyPrefix   => "Feazeyu.RPGSystems.Editor";
        /// <inheritdoc/>
        protected override string NewAssetName    => "NewDialogueGraph";
        /// <inheritdoc/>
        protected override string SaveDialogTitle => "New Dialogue Graph";
        /// <inheritdoc/>
        protected override string WindowIcon      => "◈";

        /// <inheritdoc/>
        protected override IReadOnlyDictionary<string, NodeInfo> NodeRegistrySource
            => DialogueNodeRegistry.All;

        /// <inheritdoc/>
        protected override StyleSheet ThemeStyleSheet => DialogueGraphStyleSheet.Get();

        /// <inheritdoc/>
        protected override string GraphViewCssClass => "dialogue-graph-view";

        /// <inheritdoc/>
        protected override GraphAsset CreateAssetInstance()
            => CreateInstance<DialogueGraphAsset>();


        /// <summary>Open.</summary>
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


    /// <summary>Opens a <see cref="DialogueGraphAsset"/> in the graph window when double-clicked in the Project view.</summary>
    public static class DialogueGraphAssetOpener
    {
        /// <summary>On open asset.</summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.EntityIdToObject(instanceID) as DialogueGraphAsset;
            if (asset == null) return false;
            DialogueGraphWindow.Open(asset);
            return true;
        }
    }
}
