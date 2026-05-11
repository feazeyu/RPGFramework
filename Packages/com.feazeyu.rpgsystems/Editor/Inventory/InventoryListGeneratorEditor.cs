using UnityEditor;
using UnityEngine;
namespace Feazeyu.RPGSystems.Inventory
{
    [CustomEditor(typeof(InventoryListGenerator))]
    class InventoryListGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Generate Inventory UI"))
            {
                ((InventoryListGenerator)target).DrawContents();
                InventoryHelper.GenerateDragLayer(((InventoryListGenerator)target).targetCanvas);
            }
        }
    }
}
