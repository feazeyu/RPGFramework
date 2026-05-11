using UnityEditor;
using UnityEngine;
using QuestGraph.Runtime;

namespace QuestGraph.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="QuestGraphAsset"/> (shown in the
    /// Project window when a quest asset is selected). Mirrors
    /// <c>DialogueGraphAssetEditor</c>; adds:
    ///   • A Kind selector (Single / Chain) with a warning about content
    ///     loss when switching on a non-empty graph.
    ///   • An Open button whose label adapts to the kind.
    ///   • A summary distinguishing objective nodes (singles) from
    ///     quest-reference nodes (chains).
    /// </summary>
    [CustomEditor(typeof(QuestGraphAsset))]
    public class QuestGraphAssetEditor : UnityEditor.Editor
    {
        private static readonly Color AccentAmber  = new Color(0.95f, 0.72f, 0.24f);
        private static readonly Color AccentViolet = new Color(0.62f, 0.55f, 0.88f);

        private SerializedProperty m_KindProp;

        private void OnEnable()
        {
            m_KindProp = serializedObject.FindProperty("m_Kind");
        }

        public override void OnInspectorGUI()
        {
            var asset = (QuestGraphAsset)target;
            serializedObject.Update();

            EditorGUILayout.Space(4);

            // ── Kind selector ────────────────────────────────────────────────

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_KindProp, new GUIContent(
                "Kind",
                "Single: the quest's internal logic (Objective/Reward/etc.).\n" +
                "Chain:  a dependency graph of quest references."));
            bool kindChanged = EditorGUI.EndChangeCheck();

            if (kindChanged && asset.Nodes.Count > 1)
            {
                EditorGUILayout.HelpBox(
                    "This asset contains nodes that may not be valid in the new kind. " +
                    "Switching does not auto-remove them, but they will appear as " +
                    "unknown node types in the wrong palette. Consider clearing the graph " +
                    "before switching, or keep this asset's kind fixed.",
                    MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);

            // ── Open button ──────────────────────────────────────────────────

            var accent = asset.Kind == QuestKind.Chain ? AccentViolet : AccentAmber;
            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize    = 12,
                fontStyle   = FontStyle.Bold,
                fixedHeight = 32,
                normal      = { textColor = accent },
                hover       = { textColor = Color.white },
            };

            var openLabel = asset.Kind == QuestKind.Chain
                ? "Open in Quest Chain Editor"
                : "Open in Quest Graph Editor";

            if (GUILayout.Button(openLabel, btnStyle))
                QuestGraphWindow.Open(asset);

            EditorGUILayout.Space(8);

            // ── Summary ──────────────────────────────────────────────────────

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.LabelField("Graph Summary", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Nodes", asset.Nodes.Count);
            EditorGUILayout.IntField("Edges", asset.Edges.Count);

            int objectiveCount = 0, rewardCount = 0, questRefCount = 0;
            foreach (var n in asset.Nodes)
            {
                if (n.NodeType == QuestNodeRegistry.TypeObjective)   objectiveCount++;
                else if (n.NodeType == QuestNodeRegistry.TypeReward) rewardCount++;
                else if (n.NodeType == QuestNodeRegistry.TypeRunSubgraph) questRefCount++;
            }

            if (asset.Kind == QuestKind.Single)
            {
                EditorGUILayout.IntField("  Objectives", objectiveCount);
                EditorGUILayout.IntField("  Rewards",    rewardCount);
            }
            else
            {
                EditorGUILayout.IntField("  Quest References", questRefCount);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Blackboard", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Variables", asset.Blackboard.Variables.Count);

            int exposed = 0, shared = 0;
            foreach (var v in asset.Blackboard.Variables)
            {
                if (v.Exposed) exposed++;
                if (v.Shared)  shared++;
            }
            EditorGUILayout.IntField("  Exposed", exposed);
            EditorGUILayout.IntField("  Shared",  shared);

            EditorGUI.EndDisabledGroup();
        }
    }
}
