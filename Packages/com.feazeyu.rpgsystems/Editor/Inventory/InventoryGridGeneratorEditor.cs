using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    [CustomEditor(typeof(InventoryGridGenerator))]
    public class InventoryGridGeneratorEditor : Editor
    {
        private static readonly GUIContent[] s_AnchorContents =
        {
            new("↖", "Upper Left"),   new("↑", "Upper Center"),  new("↗", "Upper Right"),
            new("←", "Middle Left"),  new("●", "Middle Center"), new("→", "Middle Right"),
            new("↙", "Lower Left"),   new("↓", "Lower Center"),  new("↘", "Lower Right"),
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawAnchorGrid();
            GenerateEditorUI();
            if (GUILayout.Button("Generate Inventory UI"))
            {
                ((InventoryGridGenerator)target).GenerateUI();
            }
        }

        private void DrawAnchorGrid()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Anchor", EditorStyles.boldLabel);
            serializedObject.Update();
            var prop = serializedObject.FindProperty("anchorPosition");
            int selected = GUILayout.SelectionGrid(prop.enumValueIndex, s_AnchorContents, 3, GUILayout.Width(90), GUILayout.Height(66));
            if (selected != prop.enumValueIndex)
                prop.enumValueIndex = selected;
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(4);
        }

        public void GenerateEditorUI()
        {
            GUILayout.Label("Inventory slot prefabs", EditorStyles.boldLabel);
            GUILayout.Space(10);
            Dictionary<string, SlotUIDefinition> temp = new();

            var generator = (InventoryGridGenerator)target;
            foreach (var (name, slot) in generator.slotDefinitions)
            {
                GUILayout.Label($"{name}", EditorStyles.boldLabel);

                SlotUIDefinition definition = slot;
                definition.cellPrefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Slot prefab",
                    definition.cellPrefab,
                    typeof(GameObject),
                    false
                );
                definition.disabledCellPrefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Disabled slot prefab",
                    definition.disabledCellPrefab,
                    typeof(GameObject),
                    false
                );
                temp[name] = definition;
                GUILayout.Space(10);
            }
            generator.slotDefinitions = temp;
        }
    }
}