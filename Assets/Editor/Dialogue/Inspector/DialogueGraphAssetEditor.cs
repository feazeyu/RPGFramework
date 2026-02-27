using UnityEditor;
using UnityEngine;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Custom Inspector for DialogueGraphAsset (shown in the Project window
    /// when the asset is selected). Adds an "Open in Graph Editor" button and
    /// shows a quick summary of nodes and variables.
    /// </summary>
    [CustomEditor(typeof(DialogueGraphAsset))]
    public class DialogueGraphAssetEditor : UnityEditor.Editor
    {
        private static readonly Color AccentGreen = new Color(0.20f, 0.78f, 0.55f);

        public override void OnInspectorGUI()
        {
            var asset = (DialogueGraphAsset)target;

            // ── Open button ───────────────────────────────────────────────────

            EditorGUILayout.Space(4);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize       = 12,
                fontStyle      = FontStyle.Bold,
                fixedHeight    = 32,
                normal         = { textColor = AccentGreen },
                hover          = { textColor = Color.white },
            };

            if (GUILayout.Button("Open in Dialogue Graph Editor", btnStyle))
                DialogueGraphWindow.Open(asset);

            EditorGUILayout.Space(8);

            // ── Summary ───────────────────────────────────────────────────────

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.LabelField("Graph Summary", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Nodes",  asset.Nodes.Count);
            EditorGUILayout.IntField("Edges",  asset.Edges.Count);

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

            EditorGUILayout.Space(8);

            // ── Danger zone ───────────────────────────────────────────────────

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (GUILayout.Button("Regenerate Runtime Asset (clear + rebuild)"))
            {
                if (EditorUtility.DisplayDialog(
                        "Regenerate Runtime Asset",
                        "This will clear all nodes, edges and variables from the asset. Are you sure?",
                        "Yes, regenerate", "Cancel"))
                {
                    // Implement if you add a runtime graph separate from the authoring graph.
                    Debug.Log("[DialogueGraph] Regenerate triggered. Implement as needed.");
                }
            }
        }
    }
}
