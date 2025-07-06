using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Inventory
{
    [CustomEditor(typeof(InventoryUIGenerator))]
    public class InventoryUIGeneratorEditor : Editor
    {
        private InventoryUIGenerator generator;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GenerateEditorUI();
            generator = (InventoryUIGenerator)target;
            if (GUILayout.Button("Generate Inventory UI"))
            {
                generator.GenerateUI();
            }
        }

        public void GenerateEditorUI()
        {
            GUILayout.Label("Inventory slot prefabs", EditorStyles.boldLabel);
            GUILayout.Space(10);
            Dictionary<string, SlotUIDefinition> temp = new();
            if (generator == null) { 
                generator = target as InventoryUIGenerator;
            }
            foreach (var entry in generator.slotDefinitions)
            {
                var slotDefinition = entry.Value;
                GUILayout.Label($"{entry.Key}", EditorStyles.boldLabel);
                slotDefinition.cellPrefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Slot prefab",
                    slotDefinition.cellPrefab,
                    typeof(GameObject),
                    false
                );
                slotDefinition.disabledCellPrefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Disabled slot prefab",
                    slotDefinition.disabledCellPrefab,
                    typeof(GameObject),
                    false
                );
                temp[entry.Key] = slotDefinition;
                GUILayout.Space(10);
            }
            generator.slotDefinitions = temp;
        }
    }
}
