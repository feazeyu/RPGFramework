
using UnityEditor;
using UnityEngine;
namespace Game.Inventory
{
    [CustomEditor(typeof(InventoryManager))]
    class InventoryManagerEditor : Editor
    {
        [Tooltip("Path to the folder containing item prefabs")]
        public string resourcePath = "Items";
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button($"Reload items in Resources/{resourcePath}"))
            {
                ((InventoryManager)target).ReloadItems();
            }
        }
    }
}

