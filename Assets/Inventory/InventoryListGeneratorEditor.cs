using UnityEditor;
using UnityEngine;
namespace Game.Inventory
{
    [CustomEditor(typeof(InventoryListGenerator))]
    class InventoryListGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Generate Inventory UI"))
            {
                ((InventoryListGenerator)target).RedrawContents();
                InventoryHelper.GenerateDragLayer(((InventoryListGenerator)target).targetCanvas);
            }
        }
    }
}
