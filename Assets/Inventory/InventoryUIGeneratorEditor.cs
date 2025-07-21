using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory
{
    [CustomEditor(typeof(InventoryUIGenerator))]
    public class InventoryUIGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GenerateEditorUI();
            if (GUILayout.Button("Generate Inventory UI"))
            {
                ((InventoryUIGenerator)target).GenerateUI();
            }
        }

        public void GenerateEditorUI()
        {
            GUILayout.Label("Inventory slot prefabs", EditorStyles.boldLabel);
            GUILayout.Space(10);
            Dictionary<string, SlotUIDefinition> temp = new();

            var generator = (InventoryUIGenerator)target;
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
