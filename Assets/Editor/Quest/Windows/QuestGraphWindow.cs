using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using DialogueGraph.Runtime;
using DialogueGraph.Editor;
using QuestGraph.Runtime;

namespace QuestGraph.Editor
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
        protected override string WindowTitle     => "Quest Graph";
        protected override string PrefKeyPrefix   => "QuestGraph.Editor";
        protected override string NewAssetName    => "NewQuestGraph";
        protected override string SaveDialogTitle => "New Quest Graph";
        protected override string WindowIcon      => "✦";

        // Fallback palette used only when no asset is loaded (the "New"
        // button creates a Single by default, so Single is the sensible
        // fallback).
        protected override IReadOnlyDictionary<string, DialogueNodeInfo> NodeRegistrySource
            => QuestNodeRegistry.ForKind(QuestKind.Single);

        /// <summary>Narrow the palette to what's valid for this asset's kind.</summary>
        protected override IReadOnlyDictionary<string, DialogueNodeInfo> GetNodeRegistryForAsset(GraphAsset asset)
        {
            var kind = (asset is QuestGraphAsset qga) ? qga.Kind : QuestKind.Single;
            return QuestNodeRegistry.ForKind(kind);
        }

        protected override StyleSheet ThemeStyleSheet => QuestGraphStyleSheet.Get();

        protected override string GraphViewCssClass => "quest-graph-view";

        protected override GraphAsset CreateAssetInstance()
            => CreateInstance<QuestGraphAsset>();

        // ── Open ─────────────────────────────────────────────────────────────

        [MenuItem("Window/Quest Graph", priority = 201)]
        public static QuestGraphWindow Open()
            => GetWindow<QuestGraphWindow>();

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

        private static void CopyDefaults(NodeData node, DialogueNodeInfo info)
        {
            if (info == null) return;
            node.Ports.AddRange(info.DefaultPorts);
            node.Fields.AddRange(info.DefaultFields);
        }
    }

    // ── Asset double-click handler ───────────────────────────────────────────

    public static class QuestGraphAssetOpener
    {
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as QuestGraphAsset;
            if (asset == null) return false;
            QuestGraphWindow.Open(asset);
            return true;
        }
    }
}
