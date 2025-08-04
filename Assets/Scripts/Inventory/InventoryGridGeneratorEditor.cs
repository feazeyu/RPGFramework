using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory
{
    [CustomEditor(typeof(InventoryGridGenerator))]
    public class InventoryGridGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GenerateEditorUI();
            if (GUILayout.Button("Generate Inventory UI"))
            {
                ((InventoryGridGenerator)target).GenerateUI();
            }
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