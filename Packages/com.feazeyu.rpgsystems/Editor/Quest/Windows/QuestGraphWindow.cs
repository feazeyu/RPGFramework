using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.EditorTools;
using Feazeyu.RPGSystems.Quest;

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Quest-system graph editor window. Sibling of
    /// <see cref="DialogueGraphWindow"/>; inherits from
    /// <see cref="GraphEditorWindow"/> and differs in:
    ///   • the palette source (<see cref="QuestNodeRegistry"/>)
    ///   • the theme sheet (<c>QuestGraph.uss</c>)
    ///   • the CSS class (<c>quest-graph-view</c>)
    ///   • the asset factory (<see cref="QuestGraphAsset"/>)
    ///
    /// Unique behaviour: the palette filters by
    /// <see cref="QuestGraphAsset.Kind"/>. A <see cref="QuestKind.Single"/>
    /// asset sees the Objective/Reward/Complete/Fail palette;
    /// a <see cref="QuestKind.Chain"/> asset sees RunSubgraph + flow
    /// nodes only. The filter runs on every
    /// <see cref="GraphEditorWindow.LoadAsset"/> via the
    /// <see cref="GraphEditorWindow.GetNodeRegistryForAsset"/> hook.
    /// </summary>
    public class QuestGraphWindow : GraphEditorWindow
    {
        /// <inheritdoc/>
        protected override string WindowTitle     => "Quest Graph";
        /// <inheritdoc/>
        protected override string PrefKeyPrefix   => "QuestGraph.Editor";
        /// <inheritdoc/>
        protected override string NewAssetName    => "NewQuestGraph";
        /// <inheritdoc/>
        protected override string SaveDialogTitle => "New Quest Graph";
        /// <inheritdoc/>
        protected override string WindowIcon      => "✦";

        /// <inheritdoc/>
        protected override IReadOnlyDictionary<string, NodeInfo> NodeRegistrySource
            => QuestNodeRegistry.ForKind(QuestKind.Single);

        /// <summary>Narrow the palette to what's valid for this asset's kind.</summary>
        protected override IReadOnlyDictionary<string, NodeInfo> GetNodeRegistryForAsset(GraphAsset asset)
        {
            var kind = (asset is QuestGraphAsset qga) ? qga.Kind : QuestKind.Single;
            return QuestNodeRegistry.ForKind(kind);
        }

        /// <inheritdoc/>
        protected override StyleSheet ThemeStyleSheet => QuestGraphStyleSheet.Get();

        /// <inheritdoc/>
        protected override string GraphViewCssClass => "quest-graph-view";

        /// <inheritdoc/>
        protected override GraphAsset CreateAssetInstance()
            => CreateInstance<QuestGraphAsset>();


        /// <summary>Open.</summary>
        [MenuItem("Window/Quest Graph", priority = 201)]
        public static QuestGraphWindow Open()
            => GetWindow<QuestGraphWindow>();

        /// <summary>Open.</summary>
        public static void Open(QuestGraphAsset asset)
        {
            var win = GetWindow<QuestGraphWindow>();
            win.LoadAsset(asset);
        }

        /// <summary>
        /// Seed a new quest with the minimum viable happy-path structure
        /// for its <see cref="QuestKind"/>:
        ///   • Single → Start + CompleteQuest
        ///   • Chain  → Start only (user drags in Quest References)
        /// </summary>
        protected override void OnSeedNewAsset(GraphAsset asset)
        {
            var kind = (asset is QuestGraphAsset qga) ? qga.Kind : QuestKind.Single;

            var start = asset.AddNode(QuestNodeRegistry.TypeStart, "Start", new Vector2(80, 160));
            CopyDefaults(start, QuestNodeRegistry.Get(start.NodeType));

            if (kind == QuestKind.Single)
            {
                var complete = asset.AddNode(QuestNodeRegistry.TypeCompleteQuest,
                                             "Complete Quest", new Vector2(460, 160));
                CopyDefaults(complete, QuestNodeRegistry.Get(complete.NodeType));
            }
        }

        private static void CopyDefaults(NodeData node, NodeInfo info)
        {
            if (info == null) return;
            node.Ports.AddRange(info.DefaultPorts);
            node.Fields.AddRange(info.DefaultFields);
        }
    }


    /// <summary>Opens a <see cref="QuestGraphAsset"/> in the graph window when double-clicked in the Project view.</summary>
    public static class QuestGraphAssetOpener
    {
        /// <summary>On open asset.</summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.EntityIdToObject(instanceID) as QuestGraphAsset;
            if (asset == null) return false;
            QuestGraphWindow.Open(asset);
            return true;
        }
    }
}
